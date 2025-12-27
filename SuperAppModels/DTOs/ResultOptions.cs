using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SuperAppModels.DTOs
{
    public class ResultOptions
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("object")]
        public object? Object { get; set; }
        [JsonPropertyName("data")]
        public List<object>? Data { get; set; }
        [JsonPropertyName("status")]
        public int? Status { get; set; }
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
        [JsonPropertyName("reference2")]
        public string? Reference2 { get; set; }
        [JsonPropertyName("reference3")]
        public string? Reference3 { get; set; }
        [JsonPropertyName("reference4")]
        public string? Reference4 { get; set; }
        [JsonPropertyName("reference5")]
        public string? Reference5 { get; set; }

        /// <summary>
        /// Error message (alias for Message for backward compatibility)
        /// </summary>
        [JsonIgnore]
        public string? ErrorMessage
        {
            get => Message;
            set => Message = value;
        }

        /// <summary>
        /// Creates a failed ResultOptions with error messages
        /// </summary>
        /// <param name="errors">List of error messages</param>
        /// <param name="status">HTTP status code (default: 400)</param>
        /// <returns>ResultOptions with Success = false</returns>
        public static ResultOptions Fail(List<string> errors, int status = 400)
        {
            return new ResultOptions
            {
                Success = false,
                Message = string.Join("; ", errors),
                Data = errors.Cast<object>().ToList(),
                Status = status
            };
        }

        /// <summary>
        /// Creates a failed ResultOptions with a single error message
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="status">HTTP status code (default: 400)</param>
        /// <returns>ResultOptions with Success = false</returns>
        public static ResultOptions Fail(string errorMessage, int status = 400)
        {
            return new ResultOptions
            {
                Success = false,
                Message = errorMessage,
                Status = status
            };
        }

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
