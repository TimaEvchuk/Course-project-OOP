namespace Plantify.Models
{
    public class Plant
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int WateringInterval { get; set; } // In days
        public string LightRequirement { get; set; } = null!;
        public string Difficulty { get; set; } = null!;
        public string? ImagePath { get; set; }
    }
}
