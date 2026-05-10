using Plantify.Models;

namespace Plantify.Messages
{
    public class ShowAddUserPlantOverlayMessage
    {
        public UserPlant? InitialUserPlant { get; }

        public ShowAddUserPlantOverlayMessage(UserPlant? initialUserPlant)
        {
            InitialUserPlant = initialUserPlant;
        }
    }
}
