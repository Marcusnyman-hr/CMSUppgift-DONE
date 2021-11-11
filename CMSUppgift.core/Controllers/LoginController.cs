using CMSUppgift.core.Interfaces;
using CMSUppgift.core.ViewModel;
using Umbraco.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace CMSUppgift.core.Controllers
{
    public class LoginController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/Login/";
        private IEmailService _emailService;
        public LoginController(IEmailService emailService)
        {
            _emailService = emailService;
        }
        #region Login
        public LoginController()
        {

        }
        public ActionResult RenderLogin()
        {
            var vm = new LoginViewModel();
            vm.RedirectUrl = HttpContext.Request.Url.AbsolutePath;
            return PartialView(PARTIAL_VIEW_FOLDER + "login.cshtml", vm);
        }

        [HttpPost]

        public ActionResult HandleLogin(LoginViewModel vm)
        {
            //Check the model
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }
            //check if the member exists
            var member = Services.MemberService.GetByUsername(vm.Username);
            if(member == null)
            {
                ModelState.AddModelError("Login", "Cannot find that username in the system");
                return CurrentUmbracoPage();
            }
            //check if the member is locked out
            if (member.IsLockedOut)
            {
                ModelState.AddModelError("Login", "This account has been locked because of to many denied login attempts.");
                return CurrentUmbracoPage();
            }

            //check if they are valdidated
            var emailVerified = member.GetValue<bool>("emailVerified");
            if (!emailVerified)
            {
                ModelState.AddModelError("Login", "This accounts email is not validated yet.");
                return CurrentUmbracoPage();
            }
            //check if creds are ok
            //login
            if(!Members.Login(vm.Username, vm.Password))
            {
                ModelState.AddModelError("Login", "The username or/and the password is incorrect");
                return CurrentUmbracoPage();
            }
            if (!string.IsNullOrEmpty(vm.RedirectUrl))
            {
                return Redirect(vm.RedirectUrl);
            }
            return RedirectToCurrentUmbracoPage();
        }
        #endregion
        #region Forgotten password

        public ActionResult RenderForgottenPassword()
        {
            var vm = new ForgottenPasswordViewModel();

            return PartialView(PARTIAL_VIEW_FOLDER + "ForgottenPassword.cshtml", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleForgottenPassword(ForgottenPasswordViewModel vm)
        {
            //Is the model ok?
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }
            //Do we have a member with this email?
            var member = Services.MemberService.GetByEmail(vm.EmailAddress);
            if(member == null)
            {
                ModelState.AddModelError("Error", "User with this email does not exist");
                return CurrentUmbracoPage();

            }
            //Create reset token
            var resetToken = Guid.NewGuid().ToString();
            //exp date
            var expiryDate = DateTime.Now.AddHours(12);

            //Save to member
            member.SetValue("resetExpiryDate", expiryDate);
            member.SetValue("resetLinkToken", resetToken);
            Services.MemberService.Save(member);

            //send the mail
            _emailService.SendResetPasswordNotification(vm.EmailAddress, resetToken);
            Logger.Info<LoginController>($"Sent a password reset to {vm.EmailAddress}");

            //thank the user
            TempData["status"] = "OK";
            return RedirectToCurrentUmbracoPage();
        }

        public ActionResult RenderResetPassword()
        {
            var vm = new ResetPasswordViewModel();
            return PartialView(PARTIAL_VIEW_FOLDER + "ResetPassword.cshtml", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleResetPassword(ResetPasswordViewModel vm)
        {
            //Get the reset token
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var token = Request.QueryString["token"];
            if (string.IsNullOrEmpty(token))
            {
                Logger.Warn<LoginController>("Reset Password - No token?");
                ModelState.AddModelError("Error", "Reset Password - No token?");
                return CurrentUmbracoPage();
            }
            //Get the member for the token
            var member = Services.MemberService.GetMembersByPropertyValue("resetLinkToken", token).SingleOrDefault();
            if(member == null)
            {
                ModelState.AddModelError("Error", "That token is no longer valid");
                return CurrentUmbracoPage();
            }
            //check if there is time left
            var tokenExpiryDate = member.GetValue<DateTime>("resetExpiryDate");
            var currentTime = DateTime.Now;
            if(currentTime.CompareTo(tokenExpiryDate) >= 0)
            {
                ModelState.AddModelError("Error", "Sorry, this resettoken has expired.");
                return CurrentUmbracoPage();
            }
            //if ok, change the password
            Services.MemberService.SavePassword(member, vm.Password);
            member.SetValue("resetLinkToken", string.Empty);
            member.SetValue("resetExpiryDate", null);
            member.IsLockedOut = false;
            Services.MemberService.Save(member);

            //Send notification
            _emailService.SendPasswordChangedNotification(member.Email);

            //
            TempData["status"] = "OK";
            Logger.Info<LoginController>($"User {member.Username} has changed their password");
            return RedirectToCurrentUmbracoPage();
 
        }
        #endregion
    }
}
