using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JmihPojiloyBot.Loggers
{
    public class Logger 
    {
        public List<Log> Logs { get;}

        private readonly string _logsFilename = $"{DateTime.Now.ToString("yyyy-MM-dd")}_logs.txt";

        private string _path = string.Empty;

        private string _filePath = string.Empty;

        public Logger()
        {
            Logs = new List<Log>();

            _path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            _filePath = Path.Combine(_path, _logsFilename);

            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }

            if (File.Exists(_filePath))
            {
                File.Create(_filePath).Close();
            }
        }

        public void Log(Log log)
        {
            Logs.Add(log);
            SaveLogs(log);
        }

        private void SaveLogs(Log log)
        {

            using (FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.End);

                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine(log.ToString());
                }
            }
        }

    }
}
