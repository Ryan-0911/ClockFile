using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace clockFile
{
    internal class EmailSender
    {
        protected string Account { get; set; } = "Dev@sumeeko.com";
        protected string Password { get; set; } = "zisj vaue dfxc zkwj";

        //public EmailSender(string account, string password)
        //{
        //    Account = account;
        //    Password = password;
        //}

        public virtual void SendEmail(List<string> toList, string subject, string body)
        {
            SmtpClient client = new SmtpClient();
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            client.Credentials = new NetworkCredential(Account, Password);
            client.EnableSsl = true;

            try
            {
                foreach (string to in toList)
                {
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(Account);
                    mail.To.Add(to);
                    mail.Subject = subject;
                    mail.SubjectEncoding = Encoding.UTF8;
                    mail.IsBodyHtml = true;
                    mail.Body = body;
                    mail.BodyEncoding = Encoding.UTF8;
                    client.Send(mail);
                    mail.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Handle the exception or log it
                Console.WriteLine("Email sending failed: " + ex.Message);
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
