using System.Text.Json.Serialization;

namespace AiAgentSample.LlmModel
{
    public sealed class ToolCallingFunction
    {
        public ToolCallingFunction()
        {

        }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public ToolCallingFunctionParameters Parameters { get; set; }
    }
}
