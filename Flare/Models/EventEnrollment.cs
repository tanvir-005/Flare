// Models/EventEnrollment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flare.Models
{
    public class EventEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }

        [Required]
        public string ParticipantId { get; set; }
        [ForeignKey("ParticipantId")]
        public virtual ApplicationUser Participant { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
