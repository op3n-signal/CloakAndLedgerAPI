namespace Shared.Models
{
    public class CommonFields
    {
        // Key
        public const string IncomingAuthKeyName = "Token";
        public const string AuthKeyRequired = "Auth Token is required.";
        public const string AuthKeyInvalid = "Auth Token is invalid.";
        public const string EncompassTokenRequired = "Encompass access token required.";
        public const string EncompassTokenHeaderName = "x-e-Token";

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

        // Encompass
        public const string CannotRetrieveAccessToken = "Cannot retrieve access token from Encompass API.";
        public const string CannotRetrieveIntrospection = "Cannot retrieve introspection from Encompass API.";
        public static readonly Dictionary<string, string> EncompassHeaders = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        };
        public const string LoanEntityType = "urn:elli:encompass:loan";
        public const string BorrowerEntityType = "urn:elli:encompass:loan:borrower";
        public const string PrintMode = "LoanData";
        public const string DocumentCategory = "Disclosures";
        public const string DocumentStatus = "Sent";
        public const string TaskType = "E-Sign";
        public const string EsignStatus = "Pending Signature";
        public const string DisclosedMethod = "Other";
        public const string AppName = "Candid";
    }

    public enum DisclosureStatus
    {
        Completed,
        Failed,
        Processing,
        Pending,
        Retrying,
        Locked
    }
}
