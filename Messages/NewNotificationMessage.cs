using Plantify.Models;

namespace Plantify.Messages
{
    public class NewNotificationMessage
    {
        public Notification Notification { get; }

        public NewNotificationMessage(Notification notification)
        {
            Notification = notification;
        }
    }
}
