using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace FEPVOASensor
{
    public class MailService
    {
        public EmailSetting _EmailSetting { set { EmailSetting = value; } }
        private EmailSetting EmailSetting;
        public bool SendTaskMailNet(string mailto, string mailsubject, string mailbody, string AttachmentPath, out string msg)
        {
            MailMessage _mailMessage = new MailMessage();
            msg = "";
            bool rValue = false;
            _mailMessage.From = new MailAddress(EmailSetting.EmailFrom, "FEPV MIS");
            string[] strMailNames = mailto.Split(';');
            foreach (string strMailName in strMailNames)
            {
                _mailMessage.To.Add(new MailAddress(strMailName));
            }
            _mailMessage.Subject = mailsubject;
            _mailMessage.Body = mailbody;
            _mailMessage.Attachments.Add(new Attachment(AttachmentPath));
            _mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
            _mailMessage.IsBodyHtml = true;
            _mailMessage.Priority = MailPriority.High;

            try
            {
                using (SmtpClient _smtpClient = new SmtpClient(EmailSetting.Host, EmailSetting.Port))
                {
                    _smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    _smtpClient.Credentials = new NetworkCredential(EmailSetting.EmailFrom, EmailSetting.Pass, EmailSetting.Domain);
                    _smtpClient.Send(_mailMessage);
                    rValue = true;
                }
            }
            catch (Exception e)
            {
                msg = e.Message;
            }
            return rValue;
        }
    }

    public class EmailSetting 
    {
        public string Host { get; set; }
        public string Domain { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string EmailFrom { get; set; }
    }
}
