using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMSUppgift.core.ViewModel
{
    public class ContactFormViewModel
    {
        [Required]
        [MaxLength(80, ErrorMessage = "Maximum 80 chars")]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Comment { get; set; }
        public string RecaptchaSiteKey { get; set; }

    }
}
