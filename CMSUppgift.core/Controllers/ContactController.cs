using CMSUppgift.core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;
using System.Net.Mail;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using CMSUppgift.core.Interfaces;

namespace CMSUppgift.core.Controllers
{
    public class ContactController : SurfaceController
    {
        private IEmailService _emailService;
        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;
        }
        public ActionResult RenderContactForm() {
            var vm = new ContactFormViewModel();

            //Set recaptcha site key
            var siteSettings = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if(siteSettings != null)
            {
                var siteKey = siteSettings.Value<string>("recaptchaSiteKey");
                vm.RecaptchaSiteKey = siteKey;
            }
            return PartialView("~/Views/Partials/ContactForm.cshtml", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleContactForm(ContactFormViewModel vm)
        {
            if (!ModelState.IsValid) 
            {
                ModelState.AddModelError("Error", "Please check the form");
                return CurrentUmbracoPage();
            }
            var siteSettings = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if(siteSettings != null)
            {
                var secretKey = siteSettings.Value<string>("recaptchaSiteKeySecret");
                bool captchaValid = isCaptchaValid(Request.Form["g-recaptcha-response"], secretKey);
                if (!captchaValid)
                {
                    ModelState.AddModelError("Captcha", "Captcha is nog valid");
                    return CurrentUmbracoPage();
                }
            }
            try
            {
                //Get handle to contact forms
                var contactForms = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("contactForms").FirstOrDefault();
                //Create contactform in umbraco
                if (contactForms != null)
                {
                    var newContact = Services.ContentService.Create("Contact", contactForms.Id, "contactForm");
                    newContact.SetValue("contactName", vm.Name);
                    newContact.SetValue("contactEmail", vm.Email);
                    newContact.SetValue("contactSubject", vm.Subject);
                    newContact.SetValue("contactComment", vm.Comment);
                    Services.ContentService.SaveAndPublish(newContact);
                }


                //send out an email to site admin
                //SendContactFormRecievedEmail(vm);
                _emailService.SendContactNotificationToAdmin(vm);
                //return confirmation
                TempData["status"] = "OK";
                return RedirectToCurrentUmbracoPage();
            }
            catch (Exception e)
            {

                Logger.Error<ContactController>("There was an error in the submit of contact form", e.Message);
                ModelState.AddModelError("Error", "Sorry, there was a problem posting your comment!");
            }

            return CurrentUmbracoPage();
        }

        private bool isCaptchaValid(string token, string secretKey)
        {
            //Send token to google api
            HttpClient httpClient = new HttpClient();
            var res = httpClient
                .GetAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}")
                .Result;
            if (res.StatusCode != System.Net.HttpStatusCode.OK)
                return false;
            //get res
            string jsonRes = res.Content.ReadAsStringAsync().Result;
            dynamic jsonData = JObject.Parse(jsonRes);
                if (jsonData.success != "true")
            {
                return false;
            }
            //confirmed?
            return true;
        }

        private void SendContactFormRecievedEmail(ContactFormViewModel vm)
        {

            //Get site settings
            var siteSettings = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if (siteSettings == null) {
                throw new Exception("Couldnt access site settings");
            }
            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");
            var toAddresses = siteSettings.Value<string>("emailSettingsAdminAccounts");

            if (string.IsNullOrEmpty(fromAddress)) {
                throw new Exception("from-address is empty");
            }
            if (string.IsNullOrEmpty(toAddresses))
            {
                throw new Exception("to-addresses is empty");
            }
            //construct the email
            var emailSubject = "There has been a contact form submitted!";
            var emailBody = $"A new contact form has been submitted from {vm.Name}. Their comments were: {vm.Comment}";
            var smtpMessage = new MailMessage();
            smtpMessage.Subject = emailSubject;
            smtpMessage.Body = emailBody;
            smtpMessage.From = new MailAddress(fromAddress);

            var toList = toAddresses.Split(',');
            foreach(var address in toList)
            {
                if (!string.IsNullOrEmpty(address))
                {
                    smtpMessage.To.Add(address);
                }

            }


            //Send the email
            using(var smtp = new SmtpClient())
            {
                smtp.Send(smtpMessage);
            }
        }
    }
}
