namespace Plantify.Messages
{
    public class NotificationsUpdatedMessage
    {
        public int WateringCount { get; }
        public int FertilizingCount { get; }

        public NotificationsUpdatedMessage(int wateringCount, int fertilizingCount)
        {
            WateringCount = wateringCount;
            FertilizingCount = fertilizingCount;
        }
    }
}
