using CMSUppgift.core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using System.Web.Security;

namespace CMSUppgift.core.Controllers
{
    public class AccountController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/Account/";
        public AccountController()
        {

        }
        public ActionResult RenderMyAccount()
        {

            var vm = new AccountViewModel();
            //Logged in check
            if (!Umbraco.MemberIsLoggedOn())
            {
                ModelState.AddModelError("Error", "You are not logged in!");
                return CurrentUmbracoPage();
            }
            //Get member details
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);
            if(member == null)
            {
                ModelState.AddModelError("Error", "We couldnt find you in the system");
                return CurrentUmbracoPage();
            }
            //populate the viewmodel.
            vm.Username = member.Username;
            vm.Name = member.Name;
            vm.Email = member.Email;

            return PartialView(PARTIAL_VIEW_FOLDER + "MyAccount.cshtml", vm);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleUpdateDetails(AccountViewModel vm)
        {
            //is the model valid?
            if(!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "There has been an error, try again later.");
                return CurrentUmbracoPage();
            }
            //is there a membeR?
            if (!Umbraco.MemberIsLoggedOn() || Membership.GetUser() == null)
            {
                ModelState.AddModelError("Error", "No user logged on!");
                return CurrentUmbracoPage();
            }

            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);
            if(member == null)
            {
                ModelState.AddModelError("Error", "There has been an error, try again later.");
                return CurrentUmbracoPage();
            }

            member.Name = vm.Name;
            member.Email = vm.Email;

            Services.MemberService.Save(member);
            TempData["status"] = "Updated Details";

            return RedirectToCurrentUmbracoPage();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandlePasswordChange(AccountViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "There has been an error, try again later.");
                return CurrentUmbracoPage();
            }
            if (!Umbraco.MemberIsLoggedOn() || Membership.GetUser() == null)
            {
                ModelState.AddModelError("Error", "No user logged on!");
                return CurrentUmbracoPage();
            }
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);
            if (member == null)
            {
                ModelState.AddModelError("Error", "There has been an error, try again later.");
                return CurrentUmbracoPage();
            }
            try
            {
                Services.MemberService.SavePassword(member, vm.Password);

            }
            catch (MembershipPasswordException e)
            {

                ModelState.AddModelError("Error", "There is a problem with the updating of your password." + e.Message);
                return CurrentUmbracoPage();
            }
            TempData["status"] = "Updated Password";
            return RedirectToCurrentUmbracoPage();

        }
    }
}
