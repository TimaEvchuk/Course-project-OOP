using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Plantify.Messages
{
    public class UserPlantSelectionChangedMessage : ValueChangedMessage<int>
    {
        public UserPlantSelectionChangedMessage(int selectedCount) : base(selectedCount)
        {
        }
    }
}
