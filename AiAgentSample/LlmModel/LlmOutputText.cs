namespace AiAgentSample.LlmModel
{
    public static class LlmOutputText
    {
        private static Queue<string> MessageQueue = new Queue<string>();

        public static void Push(string message)
        {
            MessageQueue.Enqueue(message);
        }

        public static string Pull()
        {
            return MessageQueue.Dequeue();
        }

        public static int QueueMemberCount()
        {
            return MessageQueue.Count;
        }
    }
}
