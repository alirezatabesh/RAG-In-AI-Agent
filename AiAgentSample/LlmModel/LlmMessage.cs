using System.Text.Json.Serialization;

namespace AiAgentSample.LlmModel
{

    public sealed class LlmMessage
    {
        public LlmMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        [JsonPropertyName("role")]
        public string Role { get; private set; }

        [JsonPropertyName("content")]
        public string Content { get; private set; }
    }
}
