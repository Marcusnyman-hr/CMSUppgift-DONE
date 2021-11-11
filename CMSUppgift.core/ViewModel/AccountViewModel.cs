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
    public class AccountViewModel
    {
        [DisplayName("Full Name")]
        [Required(ErrorMessage ="Please enter your name!")]
        public string Name { get; set; }
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your name!")]
        [Email(ErrorMessage = "Please provide a valid email")]
        public string Email { get; set; }

        [UIHint("Password")]
        [DisplayName("Password")]
        [MinLength(10)]
        public string Username { get; set; }
        public string Password { get; set; }
        [UIHint("Confirm Password")]
        [DisplayName("Confirm Password")]
        [EqualTo("Password")]
        public string ConfirmPassword { get; set; }
    }
}
