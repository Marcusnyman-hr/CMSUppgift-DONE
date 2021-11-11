using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMSUppgift.core.ViewModel
{
    public class RegisterViewModel
    {
        [DisplayName("First Name")]
        [Required(ErrorMessage ="You must provied your name!")]
        public string FirstName { get; set; }
        [DisplayName("Last Name")]
        [Required(ErrorMessage = "You must provied your last name!")]
        public string LastName { get; set; }
        [DisplayName("Username")]
        [Required(ErrorMessage = "You must provied a username!")]
        [MinLength(6)]
        public string Username { get; set; }
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "You must provied your email!")]
        public string EmailAddress { get; set; }
        [UIHint("Password")]
        [DisplayName("Password")]
        [Required(ErrorMessage = "You must provied a password!")]
        [MinLength(10)]
        public string Password { get; set; }
        [UIHint("Confirm Password")]
        [DisplayName("Confirm Password")]
        [Required(ErrorMessage = "You must provied a  confirm password!")]
        [EqualTo("Password")]
        public string ConfirmPassword { get; set; }
    }
}
