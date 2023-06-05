﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.FilesJoiner
{
    class GroupedFile
    {
        public string FileName { get; set; }
        public List<FileInf> Files { get; set; }
    }

    public class FilesJoiner : Task
    {
        public string DestFolder { get; }
        public bool Overwrite { get; }
        public string SmbComputerName { get; private set; }
        public string SmbDomain { get; private set; }
        public string SmbUsername { get; private set; }
        public string SmbPassword { get; private set; }

        public FilesJoiner(XElement xe, Workflow wf) : base(xe, wf)
        {
            DestFolder = GetSetting("destFolder", string.Empty);
            Overwrite = bool.Parse(GetSetting("overwrite", "false"));
            SmbComputerName = GetSetting("smbComputerName");
            SmbDomain = GetSetting("smbDomain");
            SmbUsername = GetSetting("smbUsername");
            SmbPassword = GetSetting("smbPassword");
        }

        private int GetNumberPartInt(string path)
        {
            int lastUnderscoreIndex = path.LastIndexOf("_", StringComparison.InvariantCulture);
            if (lastUnderscoreIndex == -1) return -1;
            string substring = path.Substring(lastUnderscoreIndex + 1, path.Length - lastUnderscoreIndex - 1);
            int part = int.TryParse(substring, out int result) ? result : -1;
            return part == -1 ? (-1) : part;
        }

        private string GetNumberPartString(string path)
        {
            int lastUnderscoreIndex = path.LastIndexOf("_", StringComparison.InvariantCulture);
            if (lastUnderscoreIndex == -1) return path;
            string substring = path.Substring(lastUnderscoreIndex + 1, path.Length - lastUnderscoreIndex - 1);
            int part = int.TryParse(substring, out int result) ? result : -1;
            return part == -1 ? path : path.Remove(lastUnderscoreIndex);
        }

        private GroupedFile[] GetFiles()
        {
            var files = SelectFiles().Select(f =>
            {
                int infoInt = GetNumberPartInt(f.Path);
                string infoString = GetNumberPartString(f.Path);
                return new
                {
                    infoString,
                    infoInt,
                    FileInf = f
                };
            });

            GroupedFile[] groupedFiles = files
                .GroupBy(f => f.infoString)
                .Select(g =>
                    new GroupedFile
                    {
                        FileName = Path.GetFileName(g.Key),
                        Files = g.ToList()
                        .OrderBy(p => p.infoInt)
                        .Select(parts => parts.FileInf)
                        .ToList()
                    })
                .ToArray();

            return groupedFiles;
        }


        public override TaskStatus Run()
        {
            Info("Concatenating files...");

            bool success = true;
            bool atLeastOneSucceed = false;

            try
            {
                if (!string.IsNullOrEmpty(SmbComputerName) && !string.IsNullOrEmpty(SmbUsername) && !string.IsNullOrEmpty(SmbPassword))
                {
                    using (NetworkShareAccesser.Access(SmbComputerName, SmbDomain, SmbUsername, SmbPassword))
                    {
                        success = JoinFiles(ref atLeastOneSucceed);
                    }
                }
                else
                {
                    success = JoinFiles(ref atLeastOneSucceed);
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while concatenating files.", e);
                success = false;
            }

            Status status = Status.Success;
            if (!success && atLeastOneSucceed)
            {
                status = Status.Warning;
            }
            else if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status, false);
        }

        private bool JoinFiles(ref bool atLeastOneSucceed)
        {
            bool success = true;
            foreach (GroupedFile file in GetFiles())
            {
                if (JoinFiles(file.FileName, file.Files.ToArray()))
                {
                    atLeastOneSucceed = true;
                }
                else
                {
                    success = false;
                }
            }
            return success;
        }

        /// <summary>
        /// Joiner splited files
        /// </summary>
        /// <param name="fileName">Original name to restore file</param>
        /// <param name="files">Ordered parts by numero part path</param>
        /// <returns></returns>
        private bool JoinFiles(string fileName, FileInf[] files)
        {
            bool success = true;
            if (files.Length > 0)
            {
                string tempPath = Path.Combine(Workflow.WorkflowTempFolder, fileName);
                string destFilePath = string.IsNullOrEmpty(DestFolder)
                    ? tempPath
                    : Path.Combine(Path.Combine(DestFolder, fileName));

                if (!string.IsNullOrEmpty(DestFolder) && File.Exists(destFilePath) && !Overwrite)
                {
                    ErrorFormat("Destination file {0} already exists.", destFilePath);
                    return false;
                }

                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                using (FileStream output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    foreach (FileInf file in files)
                    {
                        Info("Joiner " + file.Path);
                        try
                        {
                            using (FileStream input = File.OpenRead(file.Path))
                            {
                                input.CopyTo(output);
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            ErrorFormat("An error occured while concatenating the file {0}", e, file.Path);
                            success = false;
                        }
                    }

                    if (success)
                    {
                        if (!string.IsNullOrEmpty(DestFolder))
                        {
                            if (File.Exists(destFilePath))
                            {
                                if (Overwrite)
                                {
                                    File.Delete(destFilePath);
                                }
                                else
                                {
                                    ErrorFormat("Destination file {0} already exists.", destFilePath);
                                    return false;
                                }
                            }

                            File.Move(tempPath, destFilePath);
                        }
                    }
                }

                if (success)
                {
                    InfoFormat("Concatenation file generated: {0}", fileName);
                }

                Files.Add(new FileInf(destFilePath, Id));
            }

            return success;
        }
    }
}