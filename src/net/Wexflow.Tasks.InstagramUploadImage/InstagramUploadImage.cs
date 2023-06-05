﻿using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Logger;
using System;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using Wexflow.Core;

namespace Wexflow.Tasks.InstagramUploadImage
{
    public class InstagramUploadImage : Task
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string SmbComputerName { get; private set; }
        public string SmbDomain { get; private set; }
        public string SmbUsername { get; private set; }
        public string SmbPassword { get; private set; }

        public InstagramUploadImage(XElement xe, Workflow wf) : base(xe, wf)
        {
            Username = GetSetting("username");
            Password = GetSetting("password");
            SmbComputerName = GetSetting("smbComputerName");
            SmbDomain = GetSetting("smbDomain");
            SmbUsername = GetSetting("smbUsername");
            SmbPassword = GetSetting("smbPassword");
        }

        public override TaskStatus Run()
        {
            Info("Uploading images...");

            bool success = true;
            bool atLeastOneSuccess = false;

            try
            {
                if (!string.IsNullOrEmpty(SmbComputerName) && !string.IsNullOrEmpty(SmbUsername) && !string.IsNullOrEmpty(SmbPassword))
                {
                    using (NetworkShareAccesser.Access(SmbComputerName, SmbDomain, SmbUsername, SmbPassword))
                    {
                        success = UploadImages(ref atLeastOneSuccess);
                    }
                }
                else
                {
                    success = UploadImages(ref atLeastOneSuccess);
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while uploading images.", e);
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

        private bool UploadImages(ref bool atLeastOneSuccess)
        {
            bool success = true;
            try
            {
                System.Threading.Tasks.Task<IInstaApi> authTask = Authenticate();
                authTask.Wait();

                if (authTask.Result == null)
                {
                    Error("An error occured while authenticating.");
                    success = false;
                    return success;
                }
                else
                {
                    Info("Authentication succeeded.");
                }

                FileInf[] files = SelectFiles();

                foreach (FileInf file in files)
                {
                    try
                    {
                        XDocument xdoc = XDocument.Load(file.Path);

                        foreach (XElement xvideo in xdoc.XPathSelectElements("/Images/Image"))
                        {
                            string filePath = xvideo.Element("FilePath").Value;
                            string caption = xvideo.Element("Caption").Value;

                            System.Threading.Tasks.Task<bool> uploadImageTask = UploadImage(authTask.Result, filePath, caption);
                            uploadImageTask.Wait();
                            success &= uploadImageTask.Result;

                            if (success && !atLeastOneSuccess) atLeastOneSuccess = true;
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        ErrorFormat("An error occured while uploading the image {0}: {1}", file.Path, e.Message);
                        success = false;
                    }
                }

            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while uploading images: {0}", e.Message);
                success = false;
                return success;
            }
            return success;
        }

        private async System.Threading.Tasks.Task<IInstaApi> Authenticate()
        {
            UserSessionData userSession = new UserSessionData
            {
                UserName = Username,
                Password = Password
            };

            IInstaApi instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(InstagramApiSharp.Logger.LogLevel.Exceptions))
                .Build();
            const string stateFile = "state.bin";
            try
            {
                // load session file if exists
                if (File.Exists(stateFile))
                {
                    using (FileStream fs = File.OpenRead(stateFile))
                    {
                        instaApi.LoadStateDataFromStream(fs);
                        // in .net core or uwp apps don't use LoadStateDataFromStream
                        // use this one:
                        // _instaApi.LoadStateDataFromString(new StreamReader(fs).ReadToEnd());
                        // you should pass json string as parameter to this function.
                    }
                }
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while authenticating: {0}", e);
                return null;
            }

            if (!instaApi.IsUserAuthenticated)
            {
                // login
                IResult<InstaLoginResult> logInResult = await instaApi.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    ErrorFormat("Unable to login: {0}", logInResult.Info.Message);
                    return null;
                }
            }
            // save session in file
            Stream state = instaApi.GetStateDataAsStream();
            // in .net core or uwp apps don't use GetStateDataAsStream.
            // use this one:
            // var state = _instaApi.GetStateDataAsString();
            // this returns you session as json string.
            using (FileStream fileStream = File.Create(stateFile))
            {
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(fileStream);
            }

            return instaApi;
        }

        public async System.Threading.Tasks.Task<bool> UploadImage(IInstaApi instaApi, string filePath, string caption)
        {
            try
            {
                InstaImageUpload mediaImage = new InstaImageUpload
                {
                    // leave zero, if you don't know how height and width is it.
                    Height = 0,
                    Width = 0,
                    Uri = filePath
                };

                IResult<InstaMedia> result = await instaApi.MediaProcessor.UploadPhotoAsync(mediaImage, caption);

                if (!result.Succeeded)
                {
                    InfoFormat("Unable to upload image: {0}", result.Info.Message);
                    return false;

                }

                InfoFormat("Media created: {0}, {1}", result.Value.Pk, result.Value.Caption.Text);
                return true;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while uploading the image: {0}", e, filePath);
                return false;
            }

        }

    }
}
