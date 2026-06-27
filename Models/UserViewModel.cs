using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BoardGameLeague.Models
{
    public class UserViewModel
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public IList<string>? Roles { get; set; }
    }

    public class EditUserViewModel
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public IList<string>? Roles { get; set; }
        public MultiSelectList? AllRoles { get; set; }
    }
}
