namespace AiAgentSample.LlmModel
{
    public class UserMessage : IMessage
    {
        public UserMessage(string message)
        {
            Message = message;
            MessageTime = DateTime.Now;
        }

        public string Role => "user";
        public string Message { get; private set; }
        public DateTime MessageTime { get; private set; }
    }
}
