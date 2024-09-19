using System.Collections.Concurrent;
using System.Text;

namespace JmihPojiloyBot.Models
{
    public class StatisticModel
    {
        public string Name {  get; set; }
        public int Tries { get; set; }
        public TimeSpan TotalTime {  get; set; }
        public string Description { get; set; }

        public ConcurrentDictionary<int, (string Status, TimeSpan Time)> Statistic
            = new ConcurrentDictionary<int, (string Status, TimeSpan Time)>();

        public StatisticModel(string name)
        {
            Name = name;
            Tries = 0;
            Description = string.Empty;
            TotalTime = TimeSpan.Zero;
        }

        public override string ToString()
        {
            int count = 1;
            var sb = new StringBuilder();

            sb.AppendLine(
                $">>> {DateTime.Now} * {Name} * {{ tries: {Tries}, time: {TotalTime.ToString(@"hh\:mm\:ss")}, {Description} }}");

            if (Statistic.Keys.Count > 1)
            {
                sb.AppendLine($"\nDetails:\n\t[");

                foreach (var entry in Statistic)
                {
                    var status = entry.Value.Status;   
                    var time = entry.Value.Time;       
                    sb.AppendLine($"\t\tAttempt {count}: Status = {status}, Time = {time.ToString(@"hh\:mm\:ss")}");
                    count++;
                }

                sb.AppendLine("\t]");
            }

            return sb.ToString();
        }
    }
}
