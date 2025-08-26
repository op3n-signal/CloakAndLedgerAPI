using Newtonsoft.Json;

namespace EncompassAPI.Models
{
    public class ProcessResult<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

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
    }

    public class ProcessResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

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
    }
}
