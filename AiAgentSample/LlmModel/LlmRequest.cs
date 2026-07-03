using System.Text.Json.Serialization;

namespace AiAgentSample.LlmModel
{
    public sealed class LlmRequest
    {
        public LlmRequest(string modelKey, List<LlmMessage> messages)
        {
            Messages = messages;
            Model = modelKey;

            Tools = new List<ToolCalling>();
        }

        [JsonPropertyName("messages")]
        public List<LlmMessage> Messages { get; private set; }

        [JsonPropertyName("model")]
        public string Model { get; private set; }

        [JsonPropertyName("temperature")]
        public float Temperature => 0.0f;

        [JsonPropertyName("stream")]
        public bool Stream => true;

        [JsonPropertyName("tool_choice")]
        public string ToolChoice => "auto";

        [JsonPropertyName("tools")]
        public List<ToolCalling> Tools { get; set; }
    }
}
