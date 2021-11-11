using Umbraco.Core.Logging;
using CMSUppgift.core.Interfaces;
using CMSUppgift.core.ViewModel;
using System;
using System.Linq;
using System.Net.Mail;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;
using System.Web;

namespace CMSUppgift.core.Services
{
    public class EmailService : IEmailService
    {
        private UmbracoHelper _umbraco;
        private IContentService _contentService;
        private ILogger _logger;
        public EmailService(UmbracoHelper umbraco, IContentService contentService, ILogger logger)
        {
            _umbraco = umbraco;
            _contentService = contentService;
            _logger = logger;
        }
        public void SendContactNotificationToAdmin(ContactFormViewModel vm)
        {

            //Get email template from umbraco
            var emailTemplate = GetEmailTemplate("New Contact Form Notification");
            if (emailTemplate == null)
            {
                throw new Exception("Template Not Found!");
            }

            //get the template data
            var subject = emailTemplate.Value<string>("emailTemplateSubject");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");
            //Convert mail-fields
            MailMerge("name", vm.Name, ref htmlContent, ref textContent);
            MailMerge("email", vm.Email, ref htmlContent, ref textContent);
            MailMerge("comment", vm.Comment, ref htmlContent, ref textContent);

            //send the email out


            //Get site settings
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if (siteSettings == null)
            {
                throw new Exception("Couldnt access site settings");
            }

            var toAddresses = siteSettings.Value<string>("emailSettingsAdminAccounts");

            if (string.IsNullOrEmpty(toAddresses))
            {
                throw new Exception("to-addresses is empty");
            }

            SendMail(toAddresses, subject, htmlContent, textContent);
        }
        private void SendMail(string toAddresses, string subject, string htmlContent, string textContent)
        {
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if (siteSettings == null)
            {
                throw new Exception("Couldnt access site settings");
            }
            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new Exception("from-address is empty");
            }
            //Debus mode

            var debugMode = siteSettings.Value<bool>("testMode");
            var testEmailAccounts = siteSettings.Value<string>("emailTestAccounts");

            var recipients = toAddresses;
            if (debugMode)
            {
                recipients = testEmailAccounts;
                subject += "(TEST MODE ON) - ";
            }

            //log the email in umbaco
            var emails = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("emails").FirstOrDefault();
            var newEmail = _contentService.Create(toAddresses, emails.Id, "Email");
            newEmail.SetValue("emailSentSubject", subject);
            newEmail.SetValue("emailToAddress", recipients);
            newEmail.SetValue("emailSentHtmlContent", htmlContent);
            newEmail.SetValue("emailSentTextContent", textContent);
            newEmail.SetValue("emailSent", false);
            _contentService.SaveAndPublish(newEmail);


            //Send the email via smtp
            var mimetype = new System.Net.Mime.ContentType("text/html");
            var alternateView = AlternateView.CreateAlternateViewFromString(htmlContent, mimetype);

            var smtpMessage = new MailMessage();
            smtpMessage.AlternateViews.Add(alternateView);

            foreach (var recipient in recipients.Split(','))
            {
                if (!string.IsNullOrEmpty(recipient))
                {
                    smtpMessage.To.Add(recipient);
                }
            }

            smtpMessage.From = new MailAddress(fromAddress);
            smtpMessage.Subject = subject;

            smtpMessage.Body = textContent;

            using (var smtp = new SmtpClient())
            {
                try
                {
                    smtp.Send(smtpMessage);
                    newEmail.SetValue("emailSent", true);
                    newEmail.SetValue("emailSentDate", DateTime.Now);
                    _contentService.SaveAndPublish(newEmail);
                }
                catch (Exception e)
                {
                    _logger.Error<EmailService>("There was an error sending the message", e);
                    throw e;
                }
            }
        }

            

        public void SenderVerifyEmailAddressNotification(string membersEmail, string token)
        {
            //Get the template
            var emailTemplate = GetEmailTemplate("Verify Email");
            if (emailTemplate == null)
            {
                throw new Exception("Template Not Found!");
            }

            //Fields from the template
            var subject = emailTemplate.Value<string>("emailTemplateSubject");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //Mail merge
            var url = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            url += $"/verify?token={token}";
            MailMerge("verify-url", url, ref htmlContent, ref textContent);

            //log the email
            SendMail(membersEmail, subject, htmlContent, textContent);



        }

        private void MailMerge(string token, string value, ref string htmlContent, ref string textContent)
        {
            htmlContent = htmlContent.Replace($"##{token}##", value);
            textContent = textContent.Replace($"##{token}##", value);
        }


        //Returns the emailtemplate where templatename matches
        private IPublishedContent GetEmailTemplate(string templateName)
        {
            var template = _umbraco.ContentAtRoot()
                .DescendantsOrSelfOfType("emailTemplate")
                .Where(w => w.Name == templateName)
                .FirstOrDefault();

            return template;
        }
        
        /// <summary>
        /// Method to send the password reset notification to a user
        /// </summary>
        public void SendResetPasswordNotification(string membersEmail, string resetToken)
        {
            //Get template
            var emailTemplate = GetEmailTemplate("Reset Password");
            if (emailTemplate == null)
            {
                throw new Exception("Template Not Found!");
            }

            //get data
            var subject = emailTemplate.Value<string>("emailTemplateSubject");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //MailMerge 
            var url = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            url += $"/reset-password?token={resetToken}";
            MailMerge("reset-url", url, ref htmlContent, ref textContent);

            SendMail(membersEmail, subject, htmlContent, textContent);
        }
        /// <summary>
        /// Send a note to the  user telling them their password has changed!
        /// </summary>
        /// <param name="membersEmail"></param>
        public void SendPasswordChangedNotification(string membersEmail)
        {
            //Get template
            var emailTemplate = GetEmailTemplate("Password Changed");
            if (emailTemplate == null)
            {
                throw new Exception("Template Not Found!");
            }

            //get data
            var subject = emailTemplate.Value<string>("emailTemplateSubject");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            SendMail(membersEmail, subject, htmlContent, textContent);
        }
    }
}
