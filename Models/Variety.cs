namespace Plantify.Models
{
    public class Variety
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public override bool Equals(object? obj)
        {
            if (obj is Variety other)
            {
                return this.Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
