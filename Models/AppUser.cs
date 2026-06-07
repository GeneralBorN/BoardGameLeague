using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BoardGameLeague.Models
{
    public class AppUser : IdentityUser
    {
        [Required]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "OIB must contain only digits.")]
        public string OIB { get; set; } = string.Empty;

        [Required]
        [StringLength(10, MinimumLength = 10)]
        [RegularExpression("^[0-9]*$", ErrorMessage = "JMBG must contain only digits.")]
        public string JMBG { get; set; } = string.Empty;
    }
}
