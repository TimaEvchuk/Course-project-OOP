namespace Plantify.Messages
{
    public class RegistrationSuccessMessage
    {
        public string Login { get; }
        public string Message { get; }

        public RegistrationSuccessMessage(string login, string message)
        {
            Login = login;
            Message = message;
        }
    }
}
