using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Repos;
using Microsoft.Extensions.Options;
using Shared.Models;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class SalesforceService : ISalesforceService
    {
        private readonly IHttpService _http;
        private readonly LoggerService<SalesforceService> _logger;
        private readonly SalesforceConfig _salesforceConfig;

        public SalesforceService(IDefaultHttpService http, LoggerService<SalesforceService> logger, IOptionsSnapshot<SalesforceConfig> salesforceOptions)
        {
            _http = http;
            _logger = logger;
            _salesforceConfig = salesforceOptions.Value;
        }

        public async Task<ProcessResult<OrgRequestInfo>> AuthenticateToSalesforce(OrgInfo orgInfo)
        {
            _logger.Base.LogInformation(nameof(AuthenticateToSalesforce));

            try
            {
                FormUrlEncodedContent tokenRequestBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "client_id", orgInfo.Client_Id },
                    { "client_secret", orgInfo.Client_Secret },
                    { "username", orgInfo.Username_Client },
                    { "password", orgInfo.Password + orgInfo.Password_Token }
                });

                OrgRequestInfo requestInfo = await _http.PostAsync<OrgRequestInfo>(orgInfo.Token_Url, tokenRequestBody);

                if (requestInfo == null || string.IsNullOrEmpty(requestInfo.Access_Token) || string.IsNullOrEmpty(requestInfo.Instance_Url))
                    throw new Exception("Could not retrieve request info");

                _logger.LogVerbose("Request info found");
                _logger.LogVerbose(JsonSerializer.Serialize(requestInfo));

                return new ProcessResult<OrgRequestInfo>(true)
                {
                    Result = requestInfo
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Error while authenitcating to salesforce for org: {orgInfo.OrgId}{Environment.NewLine}Exception: {ex}");

                return new ProcessResult<OrgRequestInfo>(false, "Something went wrong. Please check logs. Error while authenitcating to salesforce");
            }
        }

        public async Task<ProcessResult<string>> CreateDocument(CandidIncomingRequest incomingRequest, OrgRequestInfo requestInfo, string disclosuresName)
        {
            _logger.Base.LogInformation(nameof(CreateDocument));

            try
            {
                // Set endpoint to push upload URL to Candid
                string endpoint = requestInfo.Instance_Url + _salesforceConfig.CandidPushEndpoint;
                _logger.LogVerbose(endpoint);

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + requestInfo.Access_Token);

                Document document = new Document
                {
                    documentName = disclosuresName.Replace(".pdf", ""),
                    documentCategory = CommonFields.DocumentCategory,
                    documentStatus = "Archived",
                    isTask = true,
                    taskType = CommonFields.TaskType,
                    prospectId = incomingRequest.ProspectId,
                    esignStatus = CommonFields.EsignStatus,
                    documentAssignedTo = incomingRequest.AssignedTo,
                    borrower = incomingRequest.BorrowerValue
                };

                object objParams = new
                {
                    documents = new Document[] { document },
                    borrowerNames = new string[] { }
                };

                _logger.LogVerbose(JsonSerializer.Serialize(objParams));

                ResponseData responseData = await _http.PostAsync<ResponseData>(endpoint, headers, JsonSerializer.Serialize(objParams));

                if (responseData == null)
                {
                    throw new Exception($"No response while pushing document for org: {incomingRequest.OrgId}");
                }

                if (responseData.response.ToLower() != "success")
                {
                    throw new Exception($"Response indicates failure while pushing document for org: {incomingRequest.OrgId}. Response: {JsonSerializer.Serialize(responseData)}");
                }

                _logger.LogVerbose(JsonSerializer.Serialize(responseData));

                Document uploadedDoc = responseData.itemsNeeded.Find(item => item.documentName == disclosuresName.Replace(".pdf", ""));

                return new ProcessResult<string>(true)
                {
                    Result = uploadedDoc.documentId
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Error while pushing document to salesforce org: {incomingRequest.OrgId}{Environment.NewLine}Exception: {ex}");

                return new ProcessResult<string>(false, $"Error while pushing document to salesforce org: {incomingRequest.OrgId}");
            }
        }

        public async Task<ProcessResult<string>> PushUploadUrlToCandid(string uploadUrl, CandidIncomingRequest incomingRequest, OrgRequestInfo requestInfo, string disclosuresName, string envelopeId, string documentId)
        {
            _logger.Base.LogInformation(nameof(PushUploadUrlToCandid));

            try
            {
                // Set endpoint to push upload URL to Candid
                string endpoint = requestInfo.Instance_Url + _salesforceConfig.CandidPushEndpoint;
                _logger.LogVerbose(endpoint);

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + requestInfo.Access_Token);

                _logger.LogVerbose("requestInfo.Access_Token");
                _logger.LogVerbose(requestInfo.Access_Token);

                Document document = new Document
                {
                    documentId = documentId,
                    documentName = disclosuresName.Replace(".pdf", ""),
                    documentCategory = CommonFields.DocumentCategory,
                    documentStatus = CommonFields.DocumentStatus,
                    isTask = true,
                    taskType = CommonFields.TaskType,
                    esignStatus = CommonFields.EsignStatus,
                    googleDriveFileDownloadableURL = uploadUrl,
                    prospectId = incomingRequest.ProspectId,
                    documentAssignedTo = incomingRequest.AssignedTo,
                    borrower = incomingRequest.BorrowerValue,
                    contactInformation = new ContactInfo()
                    {
                        docusignEnvelopeId = envelopeId,
                        docusingRecipientId = incomingRequest.Recipients[0].RecipientId.ToString(),
                        docusignRecipient2Id = incomingRequest.Recipients.Count > 1 ? incomingRequest.Recipients[1].RecipientId.ToString() : null
                    }
                };

                object objParams = new
                {
                    documents = new Document[] { document },
                    borrowerNames = new string[] {}
                };


                ResponseData responseData = await _http.PostAsync<ResponseData>(endpoint, headers, JsonSerializer.Serialize(objParams));
        
                if (responseData == null)
                {
                    throw new Exception($"No response while pushing document for org: {incomingRequest.OrgId}");
                }

                if(responseData.response.ToLower() != "success")
                {
                    _logger.Base.LogWarning($"Response indicates failure while pushing document for org: {incomingRequest.OrgId}. Response: {JsonSerializer.Serialize(responseData)}");

                    if (responseData.message.Contains("MISSING_ARGUMENT"))
                        return new ProcessResult<string>(true, $"Document updated.");

                    throw new Exception($"Response indicates failure while pushing document for org: {incomingRequest.OrgId}. Response: {JsonSerializer.Serialize(responseData)}");
                }

                _logger.LogVerbose(JsonSerializer.Serialize(responseData));

                Document uploadedDoc = responseData.itemsNeeded.Find(item => item.documentName == disclosuresName.Replace(".pdf", ""));

                _logger.Base.LogInformation($"{nameof(PushUploadUrlToCandid)} Succeeded");

                return new ProcessResult<string>(true)
                {
                    Result = uploadedDoc.documentId
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Error while pushing upload url to salesforce org: {incomingRequest.OrgId}{Environment.NewLine}Exception: {ex}");

                return new ProcessResult<string>(false, $"Error while pushing upload url to salesforce org: {incomingRequest.OrgId}");
            }
        }

        public async Task<ProcessResult<GoogleDriveClone>> GetOrgAWSSettings(string orgId, string loId, OrgRequestInfo requestInfo)
        {
            _logger.Base.LogInformation(nameof(GetOrgAWSSettings));

            try
            {
                string endpoint = requestInfo.Instance_Url + _salesforceConfig.CandidSettingsEndpoint + " WHERE+SetupOwnerId='" + loId + "'";

                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("Authorization", "Bearer " + requestInfo.Access_Token);

                // Get aws settings for lo
                QueryResult<GoogleDriveClone> qrAWSSettings = await _http.GetAsync<QueryResult<GoogleDriveClone>>(endpoint, headers);

                GoogleDriveClone awsSettings;

                // Check if lo has aws settings
                if (qrAWSSettings == null || 
                    qrAWSSettings.Total_Size == 0 || 
                    qrAWSSettings.Records == null || 
                    qrAWSSettings.Records[0] == null || 
                    string.IsNullOrEmpty(qrAWSSettings.Records[0].AWS_Access_Key_Id__c) ||
                    string.IsNullOrEmpty(qrAWSSettings.Records[0].AWS_Secret_Access_Key__c) ||
                    string.IsNullOrEmpty(qrAWSSettings.Records[0].Root_Folder_Name__c))
                {
                    // Try again but get org default aws settings this time
                    endpoint = requestInfo.Instance_Url + _salesforceConfig.CandidSettingsEndpoint + " WHERE+SetupOwnerId='" + orgId + "'";

                    qrAWSSettings = await _http.GetAsync<QueryResult<GoogleDriveClone>>(endpoint, headers);

                    if (qrAWSSettings == null || qrAWSSettings.Total_Size == 0)
                    {
                        throw new Exception($"no default aws settings found for org: {orgId}");
                    }

                    awsSettings = qrAWSSettings.Records[0];

                    // Get org default aws settings
                    if (awsSettings == null ||
                        string.IsNullOrEmpty(awsSettings.AWS_Access_Key_Id__c) ||
                        string.IsNullOrEmpty(awsSettings.AWS_Secret_Access_Key__c) ||
                        string.IsNullOrEmpty(awsSettings.Root_Folder_Name__c))
                    {
                        throw new Exception($"no aws settings found for org: {orgId}");
                    }
                }
                else
                {
                    awsSettings = qrAWSSettings.Records[0];
                }

                _logger.LogVerbose("aws settings found");
                _logger.LogVerbose(JsonSerializer.Serialize(awsSettings));

                return new ProcessResult<GoogleDriveClone>(true)
                {
                    Result = awsSettings
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Error while retrieving settings from salesforce for org: {orgId}{Environment.NewLine}Exception: {ex}");

                return new ProcessResult<GoogleDriveClone>(false, "Something went wrong. Please check logs.");
            }
        }
    }

    public interface ISalesforceService
    {
        Task<ProcessResult<OrgRequestInfo>> AuthenticateToSalesforce(OrgInfo orgInfo);
        Task<ProcessResult<string>> CreateDocument(CandidIncomingRequest incomingRequest, OrgRequestInfo requestInfo, string disclosuresName);
        Task<ProcessResult<string>> PushUploadUrlToCandid(string uploadUrl, CandidIncomingRequest incomingRequest, OrgRequestInfo requestInfo, string disclosuresName, string envelopeId, string documentId);
        Task<ProcessResult<GoogleDriveClone>> GetOrgAWSSettings(string orgId, string loId, OrgRequestInfo requestInfo);
    }
}
