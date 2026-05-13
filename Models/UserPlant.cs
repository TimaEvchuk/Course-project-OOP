using System;

namespace Plantify.Models
{
    public class UserPlant
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public int PlantId { get; set; }
        public virtual Plant Plant { get; set; } = null!;
        public string? CustomName { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public DateTime LastUserWateringDate { get; set; }
        public DateTime LastFertilizedDate { get; set; }
    }
}
