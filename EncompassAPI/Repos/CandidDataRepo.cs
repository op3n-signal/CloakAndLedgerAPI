using EncompassAPI.Models.Salesforce;

namespace EncompassAPI.Repos
{
    public class CandidDataRepo : ICandidDataRepo
    {
        private readonly IDataRepo _dataRepo;

        public CandidDataRepo(IDataRepo dataRepo)
        {
            _dataRepo = dataRepo;
        }

        public async Task<OrgInfo> GetCandidCredentials(string orgId)
        {
            string sql = @"SELECT orgid, 
                username_client, 
                password, 
                password_token, 
                client_id, 
                client_secret, 
                token_url 
                FROM salesforce_test_org 
                WHERE orgid = @OrgId 
                LIMIT 1";

            object objParams = new
            {
                @OrgId = orgId
            };

            return await _dataRepo.SelectSingle<OrgInfo>(sql, objParams);
        }
    }

    public interface ICandidDataRepo
    {
        Task<OrgInfo> GetCandidCredentials(string orgId);
    }
}
