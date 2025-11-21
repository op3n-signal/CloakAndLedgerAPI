using EncompassAPI.Models;
using EncompassAPI.Models.Entity;
using System.Security.Cryptography;
using System.Text.Json;

namespace EncompassAPI.Repos
{
    public class DisclosureDataRepo : IDisclosureDataRepo
    {
        private readonly IDataRepo _dataRepo;

        public DisclosureDataRepo(IDataRepo dataRepo)
        {
            _dataRepo = dataRepo;
        }

        public async Task<bool> InsertDisclosureSignerMapping(DocumentSignerMapping documentSignerMapping)
        {
            string sql = @"INSERT INTO disclosure_signer_mappings (Id, DocumentName, LoanType, State, BorrowerCount, LoanPurpose, AmortizationType, Signers, CreatedAt)
            Values(@Id, @DocumentName, @LoanType, @State, @BorrowerCount, @LoanPurpose, @AmortizationType, @Signers, @CreatedAt) ";

            object objParams = new
            {
                @Id = documentSignerMapping.Id,
                @DocumentName = documentSignerMapping.DocumentName,
                @LoanType = documentSignerMapping.LoanType,
                @State = documentSignerMapping.State,
                @BorrowerCount = documentSignerMapping.BorrowerCount,
                @LoanPurpose = documentSignerMapping.LoanPurpose,
                @AmortizationType = documentSignerMapping.AmortizationType,
                @Signers = JsonSerializer.Serialize(documentSignerMapping.Signers),
                @CreatedAt = documentSignerMapping.CreatedAt
            };

            int affected = await _dataRepo.Execute(sql, objParams);

            return affected == 1;
        }

        public async Task<bool> UpdateDisclosureSignerMapping(DocumentSignerMapping documentSignerMapping)
        {
            string sql = @"UPDATE disclosure_signer_mappings
            SET DocumentName = @DocumentName, 
                LoanType = @LoanType,
                State = @State, 
                BorrowerCount = @BorrowerCount, 
                LoanPurpose = @LoanPurpose, 
                AmortizationType = @AmortizationType, 
                Signers = @Signers, 
                ModifiedAt = @ModifiedAt 
            WHERE Id = @Id";

            object objParams = new
            {
                @Id = documentSignerMapping.Id,
                @DocumentName = documentSignerMapping.DocumentName,
                @LoanType = documentSignerMapping.LoanType,
                @State = documentSignerMapping.State,
                @BorrowerCount = documentSignerMapping.BorrowerCount,
                @LoanPurpose = documentSignerMapping.LoanPurpose,
                @AmortizationType = documentSignerMapping.AmortizationType,
                @Signers = JsonSerializer.Serialize(documentSignerMapping.Signers),
                @ModifiedAt = documentSignerMapping.ModifiedAt
            };

            int affected = await _dataRepo.Execute(sql, objParams);

            return affected == 1;
        }

        public async Task<string> GetDisclosureSignerMappingId(DocumentSignerMapping mapping)
        {
            string sql = @"SELECT Id
            FROM disclosure_signer_mappings
            WHERE DocumentName = @documentName
            AND LoanType = @loanType 
            AND State = @state 
            AND BorrowerCount = @borrowerCount ";

            object objParams = new
            {
                @documentName = mapping.DocumentName,
                @loanType = mapping.LoanType,
                @state = mapping.State,
                @borrowerCount = mapping.BorrowerCount
            };

            var result = await _dataRepo.SelectSingle<string>(sql, objParams);

            return result;
        }

        public async Task<List<DocumentSignerMappingEntity>> GetDisclosureSignerMappings(string loanType, string state, int borrowerCount)
        {
            string sql = @"SELECT DocumentName, Signers
            FROM disclosure_signer_mappings
            WHERE LoanType = @loanType
            AND State @state 
            AND BorrowerCount = @borrowerCount ";

            object objParams = new
            {
                @loanType = loanType,
                @state = state,
                @borrowerCount = borrowerCount
            };

            var result = await _dataRepo.Select<DocumentSignerMappingEntity>(sql, objParams);
            return result.ToList();
        }
    }

    public interface IDisclosureDataRepo
    {
        Task<bool> InsertDisclosureSignerMapping(DocumentSignerMapping documentSignerMapping);
        Task<bool> UpdateDisclosureSignerMapping(DocumentSignerMapping documentSignerMapping);
        Task<string> GetDisclosureSignerMappingId(DocumentSignerMapping mapping);
        Task<List<DocumentSignerMappingEntity>> GetDisclosureSignerMappings(string loanType, string state, int borrowerCount);
    }
}
