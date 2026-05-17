using System;
using System.Collections.Generic;

namespace Plantify.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Login { get; set; } = null!;
        public bool IsBlocked { get; set; }
        public bool IsPremium { get; set; }
        public DateTime? PremiumStartDate { get; set; }
        public DateTime? PremiumEndDate { get; set; }
        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
        public virtual ICollection<UserPlant> UserPlants { get; set; } = new List<UserPlant>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<PlantSubmission> SubmittedPlants { get; set; } = new List<PlantSubmission>();
    }
}
