using Newtonsoft.Json;

namespace CloakAndLedger.Api.Models
{
    public class ProcessResult<T>
    {
        public bool Success { get; set; }

        public ResultStatus Status { get; set; }

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

        public ProcessResult(bool success, ResultStatus status, string message)
        {
            Success = success;
            Status = status;
            Message = message;
        }

        public ProcessResult(bool success, T result)
        {
            Success = success;
            Result = result;
        }
    }

    public class ProcessResult
    {
        public bool Success { get; set; }

        public ResultStatus Status { get; set; }

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

        public ProcessResult(bool success, ResultStatus status, string message)
        {
            Success = success;
            Status = status;
            Message = message;
        }
    }

    public enum ResultStatus
    {
        Success,
        ValidationError,
        NotFound,
        InternalError
    }
}
