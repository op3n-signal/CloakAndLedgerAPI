using EncompassAPI.Models;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Repos;

namespace EncompassAPI.Services
{
    public class CandidDataService : ICandidDataService
    {
        private readonly ICandidDataRepo _candidDataRepo;
        private readonly LoggerService<CandidDataService> _logger;

        public CandidDataService(ICandidDataRepo candidDataRepo, LoggerService<CandidDataService> logger)
        { 
            _candidDataRepo = candidDataRepo;
            _logger = logger;
        }

        public async Task<ProcessResult<OrgInfo>> GetCandidCredentialsByOrg(string orgId)
        {
            try
            {
                OrgInfo orgInfo = await _candidDataRepo.GetCandidCredentials(orgId);

                if (orgInfo == null)
                {
                    _logger.Base.LogError($"org info is null for when attempting to get candid creds for org: {orgId}");
                    return new ProcessResult<OrgInfo>(false, $"org info is null for when attempting to get candid creds for org: {orgId}");
                }

                if(string.IsNullOrEmpty(orgInfo.Username_Client) || 
                   string.IsNullOrEmpty(orgInfo.Password) || 
                   string.IsNullOrEmpty(orgInfo.Password_Token) || 
                   string.IsNullOrEmpty(orgInfo.Client_Id) || 
                   string.IsNullOrEmpty(orgInfo.Client_Secret) || 
                   string.IsNullOrEmpty(orgInfo.Token_Url))
                {
                    _logger.Base.LogError($"One or more required fields are null or empty for org: {orgId}");
                    return new ProcessResult<OrgInfo>(false, $"One or more required fields are null or empty for org: {orgId}");
                }

                return new ProcessResult<OrgInfo>(true)
                {
                    Result = orgInfo
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Error while getting candid org credentials for org: {orgId}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<OrgInfo>(false, $"Error while getting candid org credentials for org: {orgId}");
            }
        }
    }

    public interface ICandidDataService
    {
        Task<ProcessResult<OrgInfo>> GetCandidCredentialsByOrg(string orgId);
    }
}
