using System.Text;
using System.Text.Json.Serialization;

namespace JmihPojiloyBot_3_1.Models
{
    public class UrlModel
    {
        public Error error { get; set; }
        public object warning { get; set; }
        public string url { get; set; }

        [JsonIgnore]
        public string Description { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{DateTime.Now} ");

            sb.AppendLine($"url: {url} {warning} {Description}");

            if (error != null)
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
        public string description { get; set; }

        public override string ToString()
        {
            return $"error {code} - {description}";
        }
    }
}
