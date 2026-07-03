using System.Text.Json.Serialization;

namespace AiAgentSample.VectorDb
{
    public sealed class VectorMessage
    {
        public VectorMessage(string modelKey, string content)
        {
            Model = modelKey;
            Input = content;
        }

        [JsonPropertyName("model")]
        public string Model { get; private set; }

        [JsonPropertyName("input")]
        public string Input { get; private set; }
    }
}