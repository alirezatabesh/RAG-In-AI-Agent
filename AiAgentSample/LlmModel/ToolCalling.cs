using System.Text.Json.Serialization;

namespace AiAgentSample.LlmModel
{
    public sealed class ToolCalling
    {
        [JsonPropertyName("type")]
        public string Type => "function";

        [JsonPropertyName("function")]
        public ToolCallingFunction Function { get; set; }
    }
}
