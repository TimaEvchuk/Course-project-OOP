using CommunityToolkit.Mvvm.Messaging.Messages;
using Plantify.Models;

namespace Plantify.Messages
{
    public class ShowAddEditPlantOverlayMessage : ValueChangedMessage<Plant?>
    {
        public ShowAddEditPlantOverlayMessage(Plant? plant) : base(plant)
        {
        }
    }
}
