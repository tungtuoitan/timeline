using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PLMModels.DTOs
{
    public class ResultOptions2<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("data")]
        public T Data { get; set; }
        [JsonPropertyName("reference")]
        public string Reference { get; set; }
        [JsonPropertyName("reference2")]
        public string Reference2 { get; set; }
        [JsonPropertyName("reference3")]
        public string Reference3 { get; set; }
        [JsonPropertyName("reference4")]
        public string Reference4 { get; set; }
        [JsonPropertyName("reference5")]
        public string Reference5 { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Message:{0}", Message);
            sb.AppendLine();

            sb.AppendFormat("Status:{0}", Status);
            sb.AppendLine();

            sb.AppendFormat("Success:{0}", Success);
            sb.AppendLine();

            sb.AppendFormat("Reference:{0}", Reference);
            sb.AppendLine();

            sb.AppendFormat("Reference2:{0}", Reference2);
            sb.AppendLine();

            sb.AppendFormat("Reference3:{0}", Reference3);
            sb.AppendLine();

            sb.AppendFormat("Reference4:{0}", Reference4);
            sb.AppendLine();

            sb.AppendFormat("Reference5:{0}", Reference5);
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
