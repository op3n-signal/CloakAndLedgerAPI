using Newtonsoft.Json;

namespace EncompassAPI.Models
{
    public class ProcessResult<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; }
        public string JsonRespone { get; set; }

        public T Result { get; set; }

        public ProcessResult()
        {
        }

        public ProcessResult(bool success)
        {
            Success = success;
        }

        public ProcessResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
        public ProcessResult(HttpResponseMessage response, string responseContent, bool deserialize = false)
        {
            Success = response != null && response.IsSuccessStatusCode;
            Message = Success ? "Request successful" : "Request failed";
            Result = deserialize && Success ? JsonConvert.DeserializeObject<T>(responseContent) : default;
            JsonRespone = responseContent;
        }
    }
}
