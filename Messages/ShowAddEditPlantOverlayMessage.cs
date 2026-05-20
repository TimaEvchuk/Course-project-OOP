using CommunityToolkit.Mvvm.Messaging.Messages;
using Plantify.Models;

namespace Plantify.Messages
{
    // This message can carry a Plant (for editing), a PlantSubmission (for reviewing), or be null (for adding new).
    public class ShowAddEditPlantOverlayMessage : ValueChangedMessage<object?>
    {
        public ShowAddEditPlantOverlayMessage(object? value) : base(value)
        {
        }
    }
}
