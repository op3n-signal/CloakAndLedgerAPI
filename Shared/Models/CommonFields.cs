namespace Shared.Models
{
    public class CommonFields
    {
        // Key
        public const string IncomingAuthKeyName = "Token";
        public const string AuthKeyRequired = "Auth Token is required.";
        public const string AuthKeyInvalid = "Auth Token is invalid.";
        public const string DisclosureLogIdHeaderName = "x-d-log-id";

        // File
        public const string FailedToDownloadFile = "Failed to download file.";
        public const string FailedToDownloadAllFiles = "Failed to download all files.";
        public const string FileDoesNotExist = "File does not exist";
        public const string FailedToUploadFile = "Failed to upload file.";

        // Error 
        public const string ServerError = "Server error. Please check logs";

        // Secrets
        public const string ErrorCreatingSecret = "Error creating secret";
        public const string ErrorRetrievingSecret = "Error retrieving secret";

        public const string AppName = "Template";
        
        public const string FailedToGetObjectsFromS3 = "Failed to get files from S3.";
    }
}
