using Plantify.Models;

namespace Plantify.Messages
{
    public class UserLoggedInMessage
    {
        public User User { get; }

        public UserLoggedInMessage(User user)
        {
            User = user;
        }
    }
}
