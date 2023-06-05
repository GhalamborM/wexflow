﻿using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Xml.Linq;
using System.Xml.XPath;
using Wexflow.Core;

namespace Wexflow.Tasks.MailsSender
{
    public class Mail
    {
        public string From { get; set; }
        public string[] To { get; }
        public string[] Cc { get; }
        public string[] Bcc { get; }
        public string Subject { get; }
        public string Body { get; }
        public FileInf[] Attachments { get; }

        public Mail(string from, string[] to, string[] cc, string[] bcc, string subject, string body, FileInf[] attachments)
        {
            From = from;
            To = to;
            Cc = cc;
            Bcc = bcc;
            Subject = subject;
            Body = body;
            Attachments = attachments;
        }

        public void Send(string host, int port, bool enableSsl, string user, string password, bool isBodyHtml)
        {
            SmtpClient smtp = new SmtpClient
            {
                Host = host,
                Port = port,
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, password)
            };

            using (MailMessage msg = new MailMessage())
            {
                msg.From = new MailAddress(From);
                foreach (string to in To) msg.To.Add(new MailAddress(to));
                foreach (string cc in Cc) msg.CC.Add(new MailAddress(cc));
                foreach (string bcc in Bcc) msg.Bcc.Add(new MailAddress(bcc));
                msg.Subject = Subject;
                msg.Body = Body;
                msg.IsBodyHtml = isBodyHtml;

                foreach (FileInf attachment in Attachments)
                {
                    // Create  the file attachment for this e-mail message.
                    Attachment data = new Attachment(attachment.Path, MediaTypeNames.Application.Octet);
                    // Add time stamp information for the file.
                    ContentDisposition disposition = data.ContentDisposition;
                    disposition.CreationDate = File.GetCreationTime(attachment.Path);
                    disposition.ModificationDate = File.GetLastWriteTime(attachment.Path);
                    disposition.ReadDate = File.GetLastAccessTime(attachment.Path);
                    // Add the file attachment to this e-mail message.
                    msg.Attachments.Add(data);
                }

                smtp.Send(msg);
            }
        }

        public static Mail Parse(MailsSender mailsSender, XElement xe, FileInf[] attachments)
        {
            string from = mailsSender.ParseVariables(xe.XPathSelectElement("From").Value);
            string[] to = mailsSender.ParseVariables(xe.XPathSelectElement("To").Value).Split(',');

            string[] cc = { };
            XElement ccElement = xe.XPathSelectElement("Cc");
            if (ccElement != null) cc = mailsSender.ParseVariables(ccElement.Value).Split(',');

            string[] bcc = { };
            XElement bccElement = xe.XPathSelectElement("Bcc");
            if (bccElement != null) bcc = mailsSender.ParseVariables(bccElement.Value).Split(',');

            string subject = mailsSender.ParseVariables(xe.XPathSelectElement("Subject").Value);
            string body = mailsSender.ParseVariables(xe.XPathSelectElement("Body").Value);

            return new Mail(from, to, cc, bcc, subject, body, attachments);
        }
    }
}
