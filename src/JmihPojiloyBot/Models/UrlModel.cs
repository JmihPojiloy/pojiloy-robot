using System.Text;
using System.Text.Json.Serialization;

namespace JmihPojiloyBot.Models
{
    public class UrlModel
    {
        public Error? error { get; set; }
        public object? warning { get; set; }
        public string? url { get; set; }

        [JsonIgnore]
        public string? description { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{DateTime.Now} ");

            sb.AppendLine($"Url: {url} {warning} {description}");

            if(error != null)
            {
                sb.AppendLine($"{error.ToString()}\n");
            }
            else
            {
                sb.AppendLine(" - OK");
            }

            return sb.ToString();
        }
    }

    public class Error
    {
        public int code { get; set; }
        public string? description { get; set; }

        public override string ToString()
        {
            return $"Error {code} - {description}";
        }
    }

}
