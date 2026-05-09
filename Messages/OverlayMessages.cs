using CommunityToolkit.Mvvm.Messaging.Messages;
using Plantify.ViewModels;

namespace Plantify.Messages
{
    public class ShowPlantDetailMessage : ValueChangedMessage<PlantViewModel>
    {
        public ShowPlantDetailMessage(PlantViewModel plant) : base(plant)
        {
        }
    }

    public class CloseOverlayMessage
    {
    }
}
