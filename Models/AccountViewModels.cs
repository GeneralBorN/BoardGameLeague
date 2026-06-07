using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;

namespace BoardGameLeague.Models
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "OIB")]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "OIB must contain only digits.")]
        public string OIB { get; set; } = string.Empty;

        [Required]
        [Display(Name = "JMBG")]
        [StringLength(10, MinimumLength = 10)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG must contain only digits.")]
        public string JMBG { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; } = "/";

        public ICollection<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();
    }

    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "OIB")]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "OIB must contain only digits.")]
        public string OIB { get; set; } = string.Empty;

        [Required]
        [Display(Name = "JMBG")]
        [StringLength(10, MinimumLength = 10)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG must contain only digits.")]
        public string JMBG { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = "/";
        public string LoginProvider { get; set; } = string.Empty;
    }
}
