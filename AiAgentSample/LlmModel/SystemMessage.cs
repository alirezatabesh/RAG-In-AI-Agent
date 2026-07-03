namespace AiAgentSample.LlmModel
{
    public class SystemMessage : IMessage
    {
        public SystemMessage(string message)
        {
            Message = message;
            MessageTime = DateTime.Now;
        }

        public string Role => "system";
        public string Message { get; private set; }
        public DateTime MessageTime { get; private set; }
    }
}
