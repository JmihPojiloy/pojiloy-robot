namespace JmihPojiloyBot.Loggers
{
    public class Logger 
    {
        private static readonly string _logsFilename = $"{DateTime.Now.ToString("yyyy-MM-dd")}_logs.txt";

        private static string _path = string.Empty;

        private static string _filePath = string.Empty;

        static Logger()
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            _filePath = Path.Combine(_path, _logsFilename);

            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }

            if (!File.Exists(_filePath))
            {
                File.Create(_filePath).Close();
            }
        }

        public static async Task Log(string message)
        {
            await SaveLogs(message);
            //await Console.Out.WriteLineAsync(message);
        }

        private static async Task SaveLogs(string message)
        {
            using (FileStream fs = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.End);

                using (StreamWriter writer = new StreamWriter(fs))
                {
                    await writer.WriteLineAsync(message);
                }
            }
        }

    }
}
