using Microsoft.EntityFrameworkCore;
using Plantify.Models;

namespace Plantify.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Plant> Plants { get; set; }
        public DbSet<PlantSection> PlantSections { get; set; } = null!;
        public DbSet<UserPlant> UserPlants { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Many-to-many relationship between User and Role
            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(j => j.ToTable("UserRoles"));
            
            // One-to-many relationship between User and UserPlant
            modelBuilder.Entity<User>()
                .HasMany(u => u.UserPlants)
                .WithOne(up => up.User)
                .HasForeignKey(up => up.UserId);

            // One-to-many relationship between Plant and UserPlant
            modelBuilder.Entity<Plant>()
                .HasMany<UserPlant>()
                .WithOne(up => up.Plant)
                .HasForeignKey(up => up.PlantId);
            
            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>();
        }
    }
}
