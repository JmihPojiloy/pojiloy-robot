namespace JmihPojiloyBot.Loggers
{
    public static class Logger 
    {
        private static readonly string LogsFilename = $"{DateTime.Now:yyyy-MM-dd}_logs.txt";

        private static readonly string FilePath;

        static Logger()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            FilePath = Path.Combine(path, LogsFilename);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(FilePath))
            {
                File.Create(FilePath).Close();
            }
        }

        public static async Task Log(string message)
        {
            await SaveLogs(message);
            //await Console.Out.WriteLineAsync(message);
        }

        private static async Task SaveLogs(string message)
        {
            await using var fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            fs.Seek(0, SeekOrigin.End);

            await using var writer = new StreamWriter(fs);
            await writer.WriteLineAsync(message);
        }
    }
}
