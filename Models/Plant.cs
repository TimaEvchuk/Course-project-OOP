using System;
using System.Collections.Generic;

namespace Plantify.Models
{
    public class Plant
    {
        public int Id { get; set; }
        public string? ImagePath { get; set; }
        public string Name { get; set; } = null!;
        
        public int LightRequirementId { get; set; }
        public virtual LightRequirement LightRequirement { get; set; } = null!;
        
        public int VarietyId { get; set; }
        public virtual Variety Variety { get; set; } = null!;
        
        public int WateringInterval { get; set; }
        public int FertilizingInterval { get; set; }
        public virtual ICollection<PlantSection> Sections { get; set; } = new List<PlantSection>();
    }
}
