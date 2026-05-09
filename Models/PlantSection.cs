namespace Plantify.Models
{
    public class PlantSection
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int PlantId { get; set; }
        public virtual Plant Plant { get; set; } = null!;
    }
}
