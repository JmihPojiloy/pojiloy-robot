﻿using System.Text.Json.Serialization;

namespace JmihPojiloyBot.Models
{
    public class UrlModel
    {
        public Error error { get; set; }
        public object warning { get; set; }
        public string url { get; set; }

        [JsonIgnore]
        public string description { get; set; }
    }

    public class Error
    {
        public int code { get; set; }
        public string description { get; set; }
    }

}
