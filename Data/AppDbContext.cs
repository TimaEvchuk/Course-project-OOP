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
        public DbSet<Variety> Varieties { get; set; }
        public DbSet<LightRequirement> LightRequirements { get; set; }
        public DbSet<PlantSection> PlantSections { get; set; } = null!;
        public DbSet<UserPlant> UserPlants { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PlantSubmission> PlantSubmissions { get; set; }

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

            // One-to-many relationship between User and PlantSubmission
            modelBuilder.Entity<User>()
                .HasMany(u => u.SubmittedPlants)
                .WithOne(ps => ps.SubmittedByUser)
                .HasForeignKey(ps => ps.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Cascade); // If a user is deleted, their submissions are deleted.

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
