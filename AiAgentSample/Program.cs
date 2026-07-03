using AiAgentSample;
using AiAgentSample.LlmModel;
using AiAgentSample.VectorDb;

var initializer = new AgentInitializer();
var llm = initializer.GetLlmService();
var vector = initializer.GetVectorService();
initializer.StartTextWriter();
await initializer.PrepareVectorDatabase();

var userMessages = new List<UserMessage>();
var systemMessages = new List<SystemMessage>();
var assistantMessage = new List<AssistantMessage>();

systemMessages.Add(new SystemMessage("all messages starts with My Lord. assistant name is Lambo!"));

while (true)
{
    Console.Write("user: ");
    var userMessageText = Console.ReadLine() ?? "";
    if (string.IsNullOrWhiteSpace(userMessageText))
        continue;

    if (DocumentStorage.Any())
    {
        var userMessageEmbedding = await vector.EmbeddingText(userMessageText);
        var bestChunks = DocumentStorage.GetBestChuncks(vector, userMessageEmbedding, 10);

        if (bestChunks.Any())
        {
            Console.WriteLine($"{bestChunks.Count} vector(s) founded related to question context.");

            string docs = "";
            foreach (var chunk in bestChunks)
            {
                docs += $"{chunk.Chunk}\r\n";
            }

            systemMessages.Add(new SystemMessage(docs));
        }
    }

    userMessages.Add(new UserMessage(userMessageText));

    Console.Write("assistant: ");
    var assistantResponse = await llm.SendMessage(userMessages.ToArray(), systemMessages.ToArray(), assistantMessage.ToArray());
    assistantMessage.Add(new AssistantMessage(assistantResponse));

    if (systemMessages.Count > 1)
        systemMessages.RemoveAt(1);

    File.WriteAllText($"C:\\alireza\\ai-response{Guid.NewGuid()}.md", assistantResponse);

    Console.WriteLine("\r\n");
}