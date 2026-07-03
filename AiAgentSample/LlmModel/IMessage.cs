namespace AiAgentSample.LlmModel
{
    public interface IMessage
    {
        string Role { get; }
        string Message { get; }
        DateTime MessageTime { get; }
    }
}
