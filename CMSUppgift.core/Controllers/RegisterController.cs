using CMSUppgift.core.Interfaces;
using CMSUppgift.core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace CMSUppgift.core.Controllers
{
    public class RegisterController : SurfaceController
    {
        private const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/";
        private IEmailService _emailService;
        public RegisterController(IEmailService emailService)
        {
            _emailService = emailService;
        }
        public ActionResult RenderRegister()
        {
            var vm = new RegisterViewModel();
            return PartialView(PARTIAL_VIEW_FOLDER + "Register.cshtml", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleRegister(RegisterViewModel vm)
        {
            //If the form is not valid - return
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }
            //Check if theres already a member like this
            var existingMember = Services.MemberService.GetByEmail(vm.EmailAddress);
            if(existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There is already a user with that email");
            }
            //check if the username is available
            existingMember = Services.MemberService.GetByUsername(vm.Username);
            if (existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There is already a user with that username");
            }
            //Create a "member"
            var newMember = Services.MemberService.CreateMember(vm.Username, vm.EmailAddress, $"{vm.FirstName} {vm.LastName}", "Member");
            newMember.PasswordQuestion = "";
            newMember.RawPasswordAnswerValue = "";
            Services.MemberService.Save(newMember);
            Services.MemberService.SavePassword(newMember, vm.Password);
            //Assign Role
            Services.MemberService.AssignRole(newMember.Id, "Normal User");

            //Create email verification token
            var token = Guid.NewGuid().ToString();
            newMember.SetValue("emailVerifyToken", token);
            Services.MemberService.Save(newMember);
            //Send email verification
            _emailService.SenderVerifyEmailAddressNotification(newMember.Email, token);

            //Thank the user
            TempData["status"] = "OK";
            return RedirectToCurrentUmbracoPage();

        }

        public ActionResult RenderEmailVerification(string token)
        {
            //Get token (querystring)

            //look for matching member with token
            var member = Services.MemberService.GetMembersByPropertyValue("emailVerifyToken", token).FirstOrDefault();
            if(member != null)
            {
                var alreadyVerified = member.GetValue<bool>("emailVerified");
                if (alreadyVerified)
                {
                    ModelState.AddModelError("Verified", "You are already verified");
                    return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");
                }
                member.SetValue("emailVerified", true);
                member.SetValue("emailVerificationDate", DateTime.Now);

                Services.MemberService.Save(member);
            }

            //Thank the user
            TempData["status"] = "OK";
            return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");

            //OTherwise - problems
            ModelState.AddModelError("Error", "Some error occurred");
            return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");
        }
    }
}
