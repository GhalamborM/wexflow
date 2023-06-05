﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.Sha256
{
    public class Sha256 : Task
    {
        public Sha256(XElement xe, Workflow wf)
            : base(xe, wf)
        {
        }

        public override TaskStatus Run()
        {
            Info("Generating SHA-256 hashes...");

            bool success = true;
            bool atLeastOneSucceed = false;

            FileInf[] files = SelectFiles();

            if (files.Length > 0)
            {
                string md5Path = Path.Combine(Workflow.WorkflowTempFolder,
                    string.Format("SHA256_{0:yyyy-MM-dd-HH-mm-ss-fff}.xml", DateTime.Now));

                XDocument xdoc = new(new XElement("Files"));
                foreach (FileInf file in files)
                {
                    try
                    {
                        string sha1 = GetSha1(file.Path);
                        xdoc.Root?.Add(new XElement("File",
                                new XAttribute("path", file.Path),
                                new XAttribute("name", file.FileName),
                                new XAttribute("sha256", sha1)));
                        InfoFormat("SHA-256 hash of the file {0} is {1}", file.Path, sha1);

                        if (!atLeastOneSucceed) atLeastOneSucceed = true;
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        ErrorFormat("An error occured while generating the SHA-256 hash of the file {0}", e, file.Path);
                        success = false;
                    }
                }
                xdoc.Save(md5Path);
                Files.Add(new FileInf(md5Path, Id));
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

        private static string GetSha1(string filePath)
        {
            StringBuilder sb = new();
            using (SHA256Managed sha256 = new())
            {
                using FileStream stream = File.OpenRead(filePath);
                byte[] bytes = sha256.ComputeHash(stream);

                foreach (byte bt in bytes)
                {
                    sb.Append(bt.ToString("x2"));
                }
            }
            return sb.ToString();
        }
    }
}
