using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Flare.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string? Name { get; set; }
    }
}
