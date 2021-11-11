using CMSUppgift.core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;

namespace CMSUppgift.core.Interfaces
{
    public interface IEmailService
    {
        void SendContactNotificationToAdmin(ContactFormViewModel vm);
        void SenderVerifyEmailAddressNotification(string membersEmail, string token);
        void SendResetPasswordNotification(string membersEmail, string resetToken);
        void SendPasswordChangedNotification(string membersEmail);
    }
}
