using AiAgentSample.LlmModel;
using AiAgentSample.VectorDb;

namespace AiAgentSample
{
    public sealed class AgentInitializer
    {
        private readonly LlmService _llm;
        private readonly VectorService _vector;

        public AgentInitializer()
        {
            _llm = new LlmService("phi4-mini:latest", "http://127.0.0.1:11434");
            _vector = new VectorService("phi4-mini:latest", "http://127.0.0.1:11434");
        }

        public LlmService GetLlmService()
        {
            return _llm;
        }

        public VectorService GetVectorService()
        {
            return _vector;
        }

        public async Task PrepareVectorDatabase()
        {
            if (!Directory.Exists("books"))
                return;

            Console.WriteLine("***************************");
            var booksSubFolders = Directory.EnumerateDirectories("books");
            foreach (var book in booksSubFolders)
            {
                var textFiles = Directory.GetFiles(book).Where(f => f.EndsWith(".txt") || f.EndsWith(".md"));

                foreach (var textFile in textFiles)
                {
                    Console.Write($"are you confirm to embedding {textFile} ? y/n :");
                    
                    var userResponse = Console.ReadKey();
                    if (userResponse.KeyChar != 'y')
                    {
                        Console.WriteLine();
                        continue;
                    }

                    Console.WriteLine();

                    var bookContent = File.ReadAllText(textFile);

                    var chunks = _vector.SplitText(bookContent, 2048);

                    float index = 0;
                    float count = chunks.Count;

                    foreach (var chunk in chunks)
                    {
                        float[] vectorsOfContent = await _vector.EmbeddingText(chunk);
                        DocumentStorage.AddDocument(new DocumentChunk(chunk, vectorsOfContent));

                        index += 1;
                        var topPos = 0;
                        if (index == 1)
                            topPos = Console.GetCursorPosition().Top;
                        else
                            topPos = Console.GetCursorPosition().Top - 1;

                        Console.SetCursorPosition(0, topPos);
                        Console.WriteLine($"{index / count * 100}% completed...");
                    }

                    Console.WriteLine($"file added to vectors: {textFile}");
                }
            }

            Console.WriteLine("embedding finished.");
            Console.WriteLine("***************************");
        }

        public void StartTextWriter()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (LlmOutputText.QueueMemberCount() > 0)
                        Console.Write(LlmOutputText.Pull());
                }
            });
        }
    }
}