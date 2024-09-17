
namespace JmihPojiloyBot.Loggers
{
    public struct Log
    {
        public DateTime Time { get; }
        public string Url { get; }
        public string Content { get; }

        public Log(string url, string content)
        {
            Time = DateTime.Now;
            Url = url;
            Content = content;
        }

        public override string ToString()
        {
            return $"{Time} [{Url}] Content: {Content}";
        }
    }
}
