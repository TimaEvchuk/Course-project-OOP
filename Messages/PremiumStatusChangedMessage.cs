using Plantify.Models;

namespace Plantify.Messages
{
    public class PremiumStatusChangedMessage
    {
        public User UpdatedUser { get; }

        public PremiumStatusChangedMessage(User updatedUser)
        {
            UpdatedUser = updatedUser;
        }
    }
}
