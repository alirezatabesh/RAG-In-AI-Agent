# RAG with LM Studio and CSharp

As someone who has been a C# .NET developer for years, I wanted to explore Retrieval-Augmented Generation (RAG) while staying within my comfort zone. While most AI development tends to happen in Python, I've chosen to implement the coding portions in C#. However, the core AI concepts remain universal regardless of the programming language.

## What We'll Need

For this implementation, we'll use [LM Studio](https://lmstudio.ai/) - a powerful desktop application that allows us to run both a Language Model (LLM) for text generation and an embedding model for semantic search. A basic understanding of C# fundamentals will help you follow along with the code examples.

## High-Level Overview

Let me provide a bird's-eye view of what we're building (refer to the diagram below):

![[Pasted image 20260702181603.png]]

When conversing with an LLM, we can provide it with pre-existing content to shape its responses. The standard chat completion API uses three roles:

1. **User** - The external user (could be a human)
2. **System** - Provides specific instructions and context to the LLM
3. **Assistant** - The AI model generating responses

The LLM considers all exchanged information to craft appropriate answers.

### Understanding Conversation Flow Through Example

Let's walk through a multi-turn conversation to understand how context builds:

**Turn 1:**
- **User**: "Hello, introduce yourself."
- **System**: "The user's name is James. The assistant's name is Yogi. The assistant always addresses the user as Boss."
- **Assistant**: "Hello, I'm Yogi, Boss."

**Turn 2:**
- **User**: "Hello, introduce yourself."
- **System**: "The user's name is James. The assistant's name is Yogi. The assistant always addresses the user as Boss."
- **Assistant**: "Hello, I'm Yogi, Boss."
- **User**: "Please don't answer any questions about the weather."
- **Assistant**: "Understood, Boss. I won't answer questions about the weather."

**Turn 3:**
- **User**: "Hello, introduce yourself."
- **System**: "The user's name is James. The assistant's name is Yogi. The assistant always addresses the user as Boss."
- **Assistant**: "Hello, I'm Yogi, Boss."
- **User**: "Please don't answer any questions about the weather."
- **Assistant**: "Understood, Boss. I won't answer questions about the weather."
- **User**: "What's the weather like today?"
- **Assistant**: "I'm sorry, I can't answer that question."

Notice how each turn includes the entire conversation history. If we omitted previous messages, the LLM would attempt to provide a weather forecast without knowing it was instructed not to.

This highlights a key requirement: **we need to maintain the conversation history with proper ordering and role attribution**.

## Modeling Messages

Let's examine the code implementation for message models:

```csharp
public interface IMessage
{
    string Role { get; }
    string Message { get; }
    DateTime MessageTime { get; }
}

public class AssistantMessage : IMessage
{
    public AssistantMessage(string message)
    {
        Message = message;
        MessageTime = DateTime.Now;
    }

    public string Role => "assistant";
    public string Message { get; private set; }
    public DateTime MessageTime { get; private set; }
}

public class SystemMessage : IMessage
{
    public SystemMessage(string message)
    {
        Message = message;
        MessageTime = DateTime.Now;
    }

    public string Role => "system";
    public string Message { get; private set; }
    public DateTime MessageTime { get; private set; }
}

public class UserMessage : IMessage
{
    public UserMessage(string message)
    {
        Message = message;
        MessageTime = DateTime.Now;
    }

    public string Role => "user";
    public string Message { get; private set; }
    public DateTime MessageTime { get; private set; }
}
```

## Making API Requests to the LLM

Now let's look at how we structure requests to our LLM:

```csharp
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
    public float Temperature => 0.2f;

    [JsonPropertyName("stream")]
    public bool Stream => true;

    [JsonPropertyName("tool_choice")]
    public string ToolChoice => "auto";

    [JsonPropertyName("tools")]
    public List<ToolCalling> Tools { get; set; }
}
```

This request model includes:
- The model identifier
- The complete message history
- Configuration parameters like temperature (creativity vs. determinism)
- Streaming support for real-time responses
- Tool calling capabilities

## The LLM Service Implementation

Here's the core service that handles communication with LM Studio's API:

```csharp
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
```

Key points about this implementation:
- Messages are ordered chronologically
- The entire conversation history is sent with each request
- Streaming responses are processed token by token
- Tool calls are handled separately

> **Note**: This is a test implementation. In production, you might want to be more selective about which messages to include based on token limits and relevance.

## Understanding Embeddings and Semantic Search

Now let's dive into the heart of RAG: embeddings. Traditional search compares exact words, but embeddings enable semantic comparison - understanding meaning rather than just matching text.

### The Power of Semantic Similarity

Consider comparing a dog, a cat, and a car. Intuitively, dog and cat are semantically similar (both are animals), while car is different. Embeddings capture these relationships mathematically.

Instead of keyword matching, we can now search by meaning. This is similar to how modern search engines understand intent rather than just keywords.

### Creating Embeddings

To convert text to embeddings, we need a dedicated embedding model. For long texts, we split them into smaller chunks and embed each chunk separately.

Here's how we split text into chunks:

```csharp
public List<string> SplitText(string text, int chunkSize = 1000)
{
    var chunks = new List<string>();

    for (int i = 0; i < text.Length; i += chunkSize)
    {
        chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
    }

    return chunks;
}
```

And here's the embedding generation code:

```csharp
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
```

This sends our text to the embedding model and returns a float array representing the vector.

### Measuring Semantic Similarity

To compare two embeddings, we use cosine similarity:

```csharp
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
```

The result is a score between -1 and 1, where higher values indicate greater similarity.

## The Document Storage and Retrieval System

Here's how we store and retrieve relevant document chunks:

```csharp
public sealed class DocumentChunk
{
    public DocumentChunk(string text, float[] embedding)
    {
        Text = text;
        Embedding = embedding;
    }

    public string Text { get; private set; }
    public float[] Embedding { get; private set; }
}

public static class DocumentStorage
{
    private static List<DocumentChunk> documents = new List<DocumentChunk>();

    public static void AddDocument(DocumentChunk document)
    {
        documents.Add(document);
    }

    public static List<DocumentChunk> GetAllDocuments() { return documents; }

    public static bool Any()
    {
        return documents.Any();
    }

    public static List<SelectedChunk> GetBestChunks(VectorService vector, float[] search, int topCount)
    {
        return documents.Select(ch => new SelectedChunk
        {
            Chunk = ch.Text,
            Score = vector.CosineSimilarity(ch.Embedding, search)
        }).Where(x => x.Score >= 0.5)
        .OrderByDescending(x => x.Score).Take(topCount).ToList();
    }
}
```

### How Retrieval Works

The `GetBestChunks` method:
1. Iterates through all stored document chunks
2. Calculates cosine similarity between each chunk's embedding and the user's query embedding
3. Filters results with a similarity score of at least 0.5
4. Orders by score (highest first)
5. Returns the top N chunks (specified by `topCount`)

These selected chunks are then included in the system message when we send our request to the LLM, providing it with relevant context to answer the user's query.

## Putting It All Together

The complete RAG flow works like this:

1. **Document Preparation**: Split documents into chunks and generate embeddings for each
2. **User Query**: When a user asks a question, generate an embedding for their query
3. **Semantic Search**: Find the most semantically similar document chunks
4. **Context Building**: Include these chunks in the system message
5. **LLM Response**: The LLM generates an answer based on the provided context

This approach ensures that the LLM can answer questions based on your specific documents, even if it wasn't trained on that data.

## Conclusion

RAG represents a powerful approach to enhancing LLM responses with your own data. By combining:
- A local LLM running through LM Studio
- Semantic search using embeddings
- C# for the implementation

We've built a complete RAG system without relying on cloud services or Python dependencies. The code we've written handles message history, embedding generation, similarity comparison, and intelligent retrieval of relevant information.

For production scenarios, you might want to consider:
- Implementing a proper vector database instead of in-memory storage
- Adding more sophisticated chunking strategies
- Handling token limits more carefully
- Adding error handling and retry logic

But for a working proof of concept, this implementation demonstrates all the core concepts needed to build a RAG system in C#.
