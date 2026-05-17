using System.Collections.Generic;

namespace Plantify.Models
{
    public class PlantSubmission
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string LightRequirement { get; set; } = null!;
        public string Variety { get; set; } = null!;
        public int WateringInterval { get; set; }
        public int FertilizingInterval { get; set; }
        public string? ImagePath { get; set; }
        
        // Description will be stored as a single text field in the submission table
        public string? Description { get; set; }

        // Foreign key to the user who submitted it
        public int SubmittedByUserId { get; set; }
        public virtual User SubmittedByUser { get; set; } = null!;
    }
}
