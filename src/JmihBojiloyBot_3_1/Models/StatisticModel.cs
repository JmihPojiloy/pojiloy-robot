using System.Collections.Concurrent;
using System.Text;

namespace JmihPojiloyBot_3_1.Models
{
    public class StatisticModel
    {
        private string Name { get; set; }
        public int Tries { get; set; } = 0;
        public TimeSpan TotalTime { get; set; } = TimeSpan.Zero;
        public string Description { get; set; } = string.Empty;

        public readonly ConcurrentDictionary<int, (string Status, TimeSpan Time)> Statistic
            = new ConcurrentDictionary<int, (string Status, TimeSpan Time)>();

        public StatisticModel(string name)
        {
            Name = name;
        }
        
        public override string ToString()
        {
            var count = 1;
            var sb = new StringBuilder();

            sb.AppendLine(
                $">>> {DateTime.Now} * {Name} * {{ tries: {Tries}, time: {TotalTime.ToString(@"hh\:mm\:ss")}, {Description} }}");

            if (Statistic.Keys.Count <= 1) return sb.ToString();
            sb.AppendLine($"\nDetails:\n\t[");

            foreach (var entry in Statistic)
            {
                var status = entry.Value.Status;
                var time = entry.Value.Time;
                sb.AppendLine($"\t\tAttempt {count}: Status = {status}, Time = {time.ToString(@"hh\:mm\:ss")}");
                count++;
            }

            sb.AppendLine("\t]");

            return sb.ToString();
        }
    }
}
