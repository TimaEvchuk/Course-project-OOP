using Plantify.Models.Enums;
using System;

namespace Plantify.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public bool IsDismissed { get; set; }
        public NotificationType Type { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
