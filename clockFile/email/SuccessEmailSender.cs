using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clockFile
{
    internal class SuccessEmailSender: EmailSender
    {
        public override void SendEmail(List<string> toList, string subject, string body)
        {
            base.SendEmail(toList, subject, body);
            // Additional actions specific to successful email sending
        }
    }
}
