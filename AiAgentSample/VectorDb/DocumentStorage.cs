namespace AiAgentSample.VectorDb
{
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

        public static List<SelectedChunk> GetBestChuncks(VectorService vector, float[] search, int topCount)
        {
            return documents.Select(ch => new SelectedChunk
            {
                Chunk = ch.Text,
                Score = vector.CosineSimilarity(ch.Embedding, search)
            }).Where(x => x.Score >= 0.5)
            .OrderByDescending(x => x.Score).Take(topCount).ToList();
        }
    }
}
