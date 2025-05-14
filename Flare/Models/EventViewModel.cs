using System;

namespace Flare.Models
{
    public class EventViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int Capacity { get; set; }
        public string Venue { get; set; }
        public string Status { get; set; }
        public int ParticipantCount { get; set; }
    }
}
