using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using NLog;


namespace hotel_mini_proxy.mail
{
    class Smtpmail
    {
        private static readonly Logger SmtpRoutinLogger = LogManager.GetLogger("Smtp mailing routine");
        public class MyAttachment : Attachment, IDisposable
        {
            private readonly string _fn;
            private readonly string _path;

            public string AttachFilename
            {
                get
                {
                    return _fn;
                }
            }
            public string AttachFullPath
            {
                get
                {
                    return _path;
                }
            }

            public MyAttachment(string fn, string path) : base(System.IO.Path.Combine(path, fn))
            {
                _fn = fn;
                _path = path;
            }
        }

        public class MyToken
        {
            public MailMessage EMessage { get; set; }
            public List<MyAttachment> EAttachments { get; set; }
            public MyToken()
            {
                EAttachments = new List<MyAttachment>();
            }
        }

        public class SendingMail
        {
            private readonly List<MyAttachment> _attachList;
            private int _numberOfAttemps = 3;
            private string[] _mailList = null;
            public string ListOfmailsAddress
            {
                set
                {
                    if (!string.IsNullOrEmpty(value))
                        _mailList = value.Split(new char[] { ',', ';' });
                }
            }
            private string _mailBody = string.Empty;
            public string BodyOfMail
            {
                set
                {
                    if (_mailBody == null)
                        _mailBody = value;
                    else
                    {
                        _mailBody += Environment.NewLine;
                        _mailBody += value;
                    }
                }
                get
                {
                    return _mailBody;
                }
            }

            public string Subj { get; set; }

            public SendingMail()
            {
                ListOfmailsAddress = null;
                BodyOfMail = null;
                _attachList = new List<MyAttachment>();
            }
            public SendingMail(string mailList, string body)
            {
                ListOfmailsAddress = mailList;
                BodyOfMail = body;
                _attachList = new List<MyAttachment>();
            }

            public SendingMail(string mailList)
            {
                ListOfmailsAddress = mailList;
                BodyOfMail = null;
                _attachList = new List<MyAttachment>();
            }
            public void AddBody(string str)
            {
                if (_mailBody == null)
                    _mailBody = str;
                else
                {
                    _mailBody += Environment.NewLine;
                    _mailBody += str;
                }
            }
            public void AddBody(List<string> listOfBody)
            {
                foreach (string str in listOfBody)
                    AddBody(str);
            }
            public void AddAttachment(string path, string fileName)
            {
                MyAttachment attach = new MyAttachment(fileName, path);

                _attachList.Add(attach);
            }
            public void MakeBaseBody()
            {
                if (_mailBody == null)
                    _mailBody = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                _mailBody += Environment.NewLine;
                _mailBody += $"Computer Name {Environment.MachineName}";
                _mailBody += Environment.NewLine;
            }

            public void SendMail()
            {
                var smtpClient = new SmtpClient();
                var token = new MyToken();
                if (_mailList == null)
                {
                    SmtpRoutinLogger.Error("List of mail recepients is empty ");
                    return;
                }
                try
                {
                    MailMessage mail = new MailMessage();
                    ContentType mimeType = new ContentType("text/html");
                    if (string.IsNullOrEmpty(_mailBody))
                    {
                        MakeBaseBody();
                    }

                    var alterBody = "<!DOCTYPE HTML PUBLIC \"-/W3C/DTD HTML 4.0 Transitional/EN\">";
                    alterBody += "<HTML><HEAD><META http-equiv=Content-Type content=\"text/html; charset=utf-8\">";
                    alterBody += "</HEAD><BODY><PRE><DIV>" + _mailBody.Replace(Environment.NewLine, "<br>");
                    alterBody += "</DIV></PRE></BODY></HTML>";
                    AlternateView alternate = AlternateView.CreateAlternateViewFromString(alterBody, mimeType);
                    mail.BodyEncoding = Encoding.UTF8;
                    mail.Body = _mailBody;
                    mail.Subject = Subj;
                    mail.SubjectEncoding = Encoding.UTF8;
                    mail.Sender = new MailAddress(Program.Config.SenderEmail);
                    mail.From = new MailAddress($"\"{Program.Config.HotelName}\"<no-reply@{mail.Sender.Host}>"); // SenderEmail)) '"Office-PC" + "<" + SenderEmail + ">")
                    mail.AlternateViews.Add(alternate);
                    foreach (var eAddress in _mailList)
                        mail.To.Add(eAddress);
                    if (_attachList.Count > 0)
                    {
                        for (var i = 0; i <= _attachList.Count - 1; i++)
                            // .Attachments.Add(New Attachment(_attachList.Item(i).AttachFullPath + "\" + _attachList.Item(i).AttachFilename))
                            mail.Attachments.Add(_attachList[i]);
                    }
                    //QXqnj019
                    smtpClient.SendCompleted += smtpClient_SendCompleted;
                    token.EMessage = mail;
                    token.EAttachments = _attachList;

                    smtpClient.Host = Program.Config.SmtpServer;
                    smtpClient.Port = Program.Config.SmtpPort;
                    smtpClient.EnableSsl = Program.Config.EnableSsl;
                    smtpClient.UseDefaultCredentials = true;
                    smtpClient.Credentials = new NetworkCredential(Program.Config.SmtpUser, Program.Config.SmtpPassword);
                    smtpClient.Timeout = 30000;
                    smtpClient.SendAsync(mail, token);

                }
                // Try
                // smtpClient.SendAsync(mail, token)
                catch (SmtpException ex)
                {
                    // exept = ex
                    if (_numberOfAttemps > 0)
                    {
                        var innerMsg = ex.InnerException == null ? string.Empty : ex.InnerException.Message;
                        SmtpRoutinLogger.Error($"{ _numberOfAttemps} Issues of sending mail: {ex.Message} {innerMsg}");

                        if (ex.StatusCode == SmtpStatusCode.GeneralFailure)
                        {
                            _numberOfAttemps -= 1;
                            SendMail();
                        }
                    }
                }
            }

            private static void smtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e) // Handles _smtpClient.SendCompleted
            {
                if (e.Error != null)
                {
                    SmtpRoutinLogger.Error($"Send message error:   {e.Error.Message}\n\t\t\t target: {e.Error.TargetSite}\n\t\t\t source: {e.Error.Source}\n\t\t\t " +
                                         $"innerExeption {(e.Error.InnerException != null ? e.Error.InnerException.Message : "")}");
                }
                else
                {
                    SmtpRoutinLogger.Info("Mail sent!");
                }

                MyToken token = (MyToken)e.UserState;
                try
                {
                    token.EMessage.Dispose();
                }
                catch (Exception ex)
                {
                    SmtpRoutinLogger.Error("Error disposing mail: " + ex.ToString() + " err:" + e.Error.Message);
                }
                if (token.EAttachments.Count > 0)
                {
                    for (int i = 0; i <= token.EAttachments.Count - 1; i++)
                    {
                        if (Directory.Exists(token.EAttachments[i].AttachFullPath))
                        {
                            try
                            {
                                Directory.Delete(token.EAttachments[i].AttachFullPath, true);
                            }
                            catch (Exception ex)
                            {
                                SmtpRoutinLogger.Error(ex.ToString() + " err:" + ((e.Error != null) ? e.Error.Message : ""));
                            }
                        }
                    }
                }
                // Call Disposable for net >=4
                var client = sender as SmtpClient;
                var disp = (IDisposable)client;
                disp?.Dispose();
            }
        }

    }
}
