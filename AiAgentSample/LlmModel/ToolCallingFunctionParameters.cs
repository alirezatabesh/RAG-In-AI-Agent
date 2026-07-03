using System.Text.Json.Serialization;

namespace AiAgentSample.LlmModel
{
    public sealed class ToolCallingFunctionParameters
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public object Properties { get; set; }
    }
}
