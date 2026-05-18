namespace Plantify.Models.DTOs
{
    public class UserPlantDto
    {
        public int PlantId { get; set; }
        public string? CustomName { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public DateTime LastUserWateringDate { get; set; }
        public DateTime LastFertilizedDate { get; set; }
    }
}
