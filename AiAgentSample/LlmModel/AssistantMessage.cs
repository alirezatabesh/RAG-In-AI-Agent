namespace AiAgentSample.LlmModel
{
    public class AssistantMessage : IMessage
    {
        public AssistantMessage(string message)
        {
            Message = message;
            MessageTime = DateTime.Now;
        }

        public string Role => "assistant";
        public string Message { get; private set; }
        public DateTime MessageTime { get; private set; }
    }
}
