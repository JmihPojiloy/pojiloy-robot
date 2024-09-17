namespace JmihPojiloyBot.Loggers
{
    public struct DownloadResult
    {
        public TimeSpan TotalTime { get; }
        public string Name { get; }
        public string Content { get; }

        public DateTime DateTime { get; }

        public DownloadResult(TimeSpan totalTime, string url, string content)
        { 
            TotalTime = totalTime;
            Name = url;
            Content = content;
            DateTime = DateTime.Now;
        }

        public override string ToString()
        {
            return $">>> {DateTime} {Name} {Content} at {TotalTime.ToString(@"hh\:mm\:ss")}.";
        }
    }
}
