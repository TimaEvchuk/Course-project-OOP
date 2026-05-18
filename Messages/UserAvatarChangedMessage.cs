using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Plantify.Messages
{
    public class UserAvatarChangedMessage : ValueChangedMessage<string?>
    {
        public UserAvatarChangedMessage(string? value) : base(value)
        {
        }
    }
}
