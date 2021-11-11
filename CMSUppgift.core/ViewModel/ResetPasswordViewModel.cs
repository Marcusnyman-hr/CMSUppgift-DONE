using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMSUppgift.core.ViewModel
{
    public class ResetPasswordViewModel
    {
        [UIHint("Password")]
        [Required(ErrorMessage ="Please provide a valid password")]
        public string Password { get; set; }
        [UIHint("Password")]
        [Required(ErrorMessage = "Please confirm your password")]
        [EqualTo("Password")]
        public string ConfirmPassword { get; set; }
    }
}
