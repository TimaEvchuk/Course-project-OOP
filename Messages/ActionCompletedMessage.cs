namespace Plantify.Messages
{
    public class ActionCompletedMessage
    {
        public string Message { get; }

        public ActionCompletedMessage(string message)
        {
            Message = message;
        }
    }
}
