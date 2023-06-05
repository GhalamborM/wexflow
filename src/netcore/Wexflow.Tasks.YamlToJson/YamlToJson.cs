﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;
using YamlDotNet.Serialization;

namespace Wexflow.Tasks.YamlToJson
{
    public class YamlToJson : Task
    {
        public YamlToJson(XElement xe, Workflow wf) : base(xe, wf)
        {
        }

        public override TaskStatus Run()
        {
            Info("Converting YAML files to JSON files...");

            bool success;
            bool atLeastOneSuccess = false;
            try
            {
                success = ConvertFiles(ref atLeastOneSuccess);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while copying files.", e);
                success = false;
            }

            Status status = Status.Success;

            if (!success && atLeastOneSuccess)
            {
                status = Status.Warning;
            }
            else if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status);
        }

        private bool ConvertFiles(ref bool atLeastOneSuccess)
        {
            bool success = true;
            FileInf[] yamlFiles = SelectFiles();

            foreach (FileInf yamlFile in yamlFiles)
            {
                try
                {
                    string source = File.ReadAllText(yamlFile.Path);

                    Deserializer deserializer = new();
                    object yamlObject = deserializer.Deserialize(new StringReader(source));

                    JsonSerializer serializer = new();
                    StringWriter writer = new();
                    serializer.Serialize(writer, yamlObject);
                    string json = writer.ToString();

                    string destPath = Path.Combine(Workflow.WorkflowTempFolder, Path.GetFileNameWithoutExtension(yamlFile.FileName) + ".json");
                    File.WriteAllText(destPath, json);
                    Files.Add(new FileInf(destPath, Id));
                    InfoFormat("The YAML file {0} has been converted -> {1}", yamlFile.Path, destPath);
                    if (!atLeastOneSuccess) atLeastOneSuccess = true;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ErrorFormat("An error occured while converting the YAML file {0}: {1}", yamlFile.Path, e.Message);
                    success = false;
                }
            }
            return success;
        }
    }
}
