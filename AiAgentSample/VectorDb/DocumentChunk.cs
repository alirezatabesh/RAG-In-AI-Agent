namespace AiAgentSample.VectorDb
{
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
}
