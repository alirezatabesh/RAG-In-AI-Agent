using System.Text;
using System.Text.Json;

namespace AiAgentSample.VectorDb
{
    public sealed class VectorService
    {
        private readonly string _modelKey;
        private readonly HttpClient _httpClient;

        public VectorService(string modelKey, string apiBaseUrl)
        {
            _httpClient = HttpClientFactory.Create();
            _httpClient.BaseAddress = new Uri(apiBaseUrl);

            _modelKey = modelKey;
        }

        public async Task<float[]> EmbeddingText(string input)
        {
            var request = new VectorMessage(_modelKey, input);

            var json = JsonSerializer.Serialize(request);

            var response = await _httpClient.PostAsync("/v1/embeddings", new StringContent(json, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);

            var embedding =
                doc.RootElement
                   .GetProperty("data")[0]
                   .GetProperty("embedding")
                   .EnumerateArray()
                   .Select(x => x.GetSingle())
                   .ToArray();

            return embedding;
        }


        public double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Embedding vectors must have the same length.");

            double dot = 0;
            double magA = 0;
            double magB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            var denom = Math.Sqrt(magA) * Math.Sqrt(magB);
            return denom == 0 ? 0 : dot / denom;
        }

        public List<string> SplitText(string text, int chunkSize = 1000)
        {
            var chunks = new List<string>();

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
            }

            return chunks;
        }
    }
}