using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Salesforce;
using Microsoft.Extensions.Options;
using Shared;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DocusignService : IDocusignService
    {
        private readonly IDefaultHttpService _http;
        private readonly LoggerService<DocusignService> _logger;
        private readonly AWSConfig _awsConfig;
        private readonly DocusignConfig _docusignConfig;
        private readonly FileConfig _fileConfig;

        public DocusignService(IDefaultHttpService http, LoggerService<DocusignService> logger, IOptionsSnapshot<AWSConfig> awsOptions, IOptionsSnapshot<DocusignConfig> docusignOptions, IOptionsSnapshot<FileConfig> fileOptions)
        {
            _http = http;
            _logger = logger;
            _awsConfig = awsOptions.Value;
            _docusignConfig = docusignOptions.Value;
            _fileConfig = fileOptions.Value;
        }

        public async Task<ProcessResult<string>> RequestDraftEdit(DraftEditRequest draftEditRequest, string disclosuresName)
        {
            _logger.LogVerbose($"Entering {nameof(RequestDraftEdit)}");

            try
            {
                draftEditRequest.S3Path += "/" + disclosuresName;

                DocusignSecret docusignSecret = await SecretService.GetSecret<DocusignSecret>(_awsConfig.DocusignSecretPath);

                if (docusignSecret == null || string.IsNullOrWhiteSpace(docusignSecret.jwt_token) || string.IsNullOrWhiteSpace(docusignSecret.requestDraftForEditingEndpoint))
                {
                    _logger.Base.LogError($"No docusign secret found at path: {_awsConfig.DocusignSecretPath}");
                    return new ProcessResult<string>(false, "No docusign secret found");
                }

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Authorization", "Bearer " + docusignSecret.jwt_token }
                };

                _logger.LogVerbose("Draft edit request:");
                _logger.LogVerbose(JsonSerializer.Serialize(draftEditRequest));
                _logger.LogVerbose(JsonSerializer.Serialize(draftEditRequest));

                DocusignDraftResponse docusignDraftResponse = await _http.PostAsync<DocusignDraftResponse>(docusignSecret.requestDraftForEditingEndpoint, headers, JsonSerializer.Serialize(draftEditRequest));

                if (docusignDraftResponse == null)
                {
                    _logger.Base.LogError("No response from docusign");
                    return new ProcessResult<string>(false, "No response from docusign");
                }

                if (docusignDraftResponse.success == false)
                {
                    _logger.Base.LogError($"Docusign response indicates failure: {JsonSerializer.Serialize(docusignDraftResponse)}");
                    return new ProcessResult<string>(false, docusignDraftResponse.errors[0]);
                }

                _logger.LogVerbose("Request info found");
                _logger.LogVerbose(JsonSerializer.Serialize(docusignDraftResponse));

                return new ProcessResult<string>(true)
                {
                    Result = docusignDraftResponse.envelopeId
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Error while draft edit request to docusign for org: {draftEditRequest.OrganizationId}{Environment.NewLine}Exception: {ex}");

                return new ProcessResult<string>(false, "Something went wrong. Please check logs. Error while draft edit request to docusign");
            }
        }

        public async Task<ProcessResult<string>> LinkForEditorEndpoint(string envelopeId, string orgId)
        {
            _logger.LogVerbose($"Entering {nameof(LinkForEditorEndpoint)}");

            try
            {
                DocusignSecret docusignSecret = await SecretService.GetSecret<DocusignSecret>(_awsConfig.DocusignSecretPath);

                if (docusignSecret == null || string.IsNullOrWhiteSpace(docusignSecret.jwt_token) || string.IsNullOrWhiteSpace(docusignSecret.linkForEditorEndpoint))
                {
                    _logger.Base.LogError($"No docusign secret found at path: {_awsConfig.DocusignSecretPath}");
                    return new ProcessResult<string>(false, "No docusign secret found");
                }

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Authorization", "Bearer " + docusignSecret.jwt_token }
                };

                DocuSignEditLinkRequest docuSignEditLinkRequest = new DocuSignEditLinkRequest()
                {
                    SiteReturnUrl = docusignSecret.redirect
                };

                DocuSignEditLinkResponse docuSignEditLinkResponse = await _http.PostAsync<DocuSignEditLinkResponse>($"{docusignSecret.linkForEditorEndpoint}{envelopeId}", headers, JsonSerializer.Serialize(docuSignEditLinkRequest));

                if (docuSignEditLinkResponse == null)
                {
                    _logger.Base.LogError("No response from docusign");
                    return new ProcessResult<string>(false, "No response from docusign");
                }

                if (docuSignEditLinkResponse.success == false)
                {
                    _logger.Base.LogError($"Docusign response indicates failure: {JsonSerializer.Serialize(docuSignEditLinkResponse)}");
                    return new ProcessResult<string>(false, docuSignEditLinkResponse.errors[0]);
                }

                _logger.LogVerbose("Request info found");
                _logger.LogVerbose(JsonSerializer.Serialize(docuSignEditLinkResponse));

                return new ProcessResult<string>(true)
                {
                    Result = docuSignEditLinkResponse.url
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Error while draft edit request to docusign for org: {orgId}{Environment.NewLine}Exception: {ex}");

                return new ProcessResult<string>(false, "Something went wrong. Please check logs. Error while draft edit request to docusign");
            }
        }
    }

    public interface IDocusignService
    {
        Task<ProcessResult<string>> RequestDraftEdit(DraftEditRequest draftEditRequest, string disclosuresName);
        Task<ProcessResult<string>> LinkForEditorEndpoint(string envelopeId, string orgId);
    }
}
