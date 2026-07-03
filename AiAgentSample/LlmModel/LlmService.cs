using System.Text;
using System.Text.Json;

namespace AiAgentSample.LlmModel
{
    public sealed class LlmService
    {
        private readonly string _modelKey;
        private readonly HttpClient _httpClient;

        public LlmService(string modelKey, string apiBaseUrl)
        {
            _httpClient = HttpClientFactory.Create();
            _httpClient.BaseAddress = new Uri(apiBaseUrl);

            _modelKey = modelKey;
        }

        public async Task<string> SendMessage(UserMessage[] userMessages, SystemMessage[] systemMessages, AssistantMessage[] assistantMessages)
        {
            List<IMessage> messagesList = new List<IMessage>();
            var messages = new List<LlmMessage>();

            messagesList.AddRange(userMessages);
            messagesList.AddRange(systemMessages);
            messagesList.AddRange(assistantMessages);

            messagesList = messagesList.OrderBy(msg => msg.MessageTime).ToList();

            foreach (var message in messagesList)
            {
                messages.Add(new LlmMessage(message.Role, message.Message));
            }

            var request = new LlmRequest(_modelKey, messages);

            var json = JsonSerializer.Serialize(request);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string allResponseText = "";

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith("data: "))
                    continue;

                var jsonPart = line.Substring("data: ".Length);

                if (jsonPart == "[DONE]")
                    break;

                using var doc = JsonDocument.Parse(jsonPart);

                var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");

                if (delta.TryGetProperty("tool_calls", out var toolCall))
                {
                    Console.WriteLine($"=> {toolCall} <=");
                }

                if (delta.TryGetProperty("content", out var content))
                {
                    var text = content.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        allResponseText += text;
                        LlmOutputText.Push(text);
                    }
                }
            }

            return allResponseText;
        }
    }
}
