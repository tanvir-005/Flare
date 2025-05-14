using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Flare.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        [StringLength(200)]
        public string Venue { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Default status

        [Required]
        public string OrganizerId { get; set; }

        [ForeignKey("OrganizerId")]
        public virtual ApplicationUser Organizer { get; set; }

        public virtual ICollection<ApplicationUser> Participants { get; set; } = new List<ApplicationUser>();
    }
}
