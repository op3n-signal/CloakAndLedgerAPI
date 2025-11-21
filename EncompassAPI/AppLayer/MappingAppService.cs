using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Encompass.LoanInfo;
using EncompassAPI.Services;

namespace EncompassAPI.AppLayer
{
    public class MappingAppService : IMappingAppService
    {
        private readonly IEncompassService _docsService;
        private readonly IDocumentSignerMappingService _mappingService;
        private readonly IMapper _mapper;

        public MappingAppService(IEncompassService docsService, IDocumentSignerMappingService mappingService, IMapper mapper)
        {
            _docsService = docsService;
            _mappingService = mappingService;
            _mapper = mapper;
        }

        public async Task<ProcessResult> RunAsync(MapRequest request, string token)
        {
            ProcessResult<LoanInfo> prLoanInfo = await _docsService.GetLoanInfo(request.OrgId, request.LoanId, token);

            if (!prLoanInfo.Success)
            {
                return new ProcessResult(false, prLoanInfo.Status, prLoanInfo.Message);
            }

            string loanType = prLoanInfo.Result.hmda.loanType;
            string state = prLoanInfo.Result.property.state;
            int borrowerCount = prLoanInfo.Result.expectedBorrowerCount;
            string applicationId = prLoanInfo.Result.applications[0].id;;

            Enum.TryParse(request.DisclosureStage, out DisclosureStage stage);

            ProcessResult<string> prAuditId = await _docsService.CreateLoanAuditForOrder(request.OrgId, request.LoanId, applicationId, token, stage);

            if (!prAuditId.Success)
            {
                return new ProcessResult(false, prAuditId.Status, prAuditId.Message);
            }

            //todo maybe return response of what failed in the audit // send back in response to candid
            ProcessResult prAuditSuccess = await _docsService.GetDocumentAuditForOrder(request.OrgId, request.LoanId, prAuditId.Result, token, stage);

            if (!prAuditSuccess.Success)
            {
                return new ProcessResult(false, prAuditSuccess.Status, prAuditSuccess.Message);
            }

            // Get documents with signature coordinates
            ProcessResult<string> prOrderId = await _docsService.GenerateDocSet(request.OrgId, request.LoanId, prAuditId.Result, token, stage);

            if (!prOrderId.Success)
            {
                return new ProcessResult(false, prOrderId.Status, prOrderId.Message);
            }

            // This only returns a select amount of documents
            ProcessResult<List<Models.Encompass.Document>> prDocuments = await _docsService.GetOrderStatus(request.OrgId, request.LoanId, prOrderId.Result, token, stage);

            if (!prDocuments.Success)
            {
                return new ProcessResult(false, prDocuments.Status, prDocuments.Message);
            }

            Dictionary<string, bool> dictDocNamesToFind = prDocuments.Result.Select(doc => doc.name).ToDictionary(name => name, name => false);

            // Retrieve efolder docs
            ProcessResult<List<string>> prEFolderDocumentNames = await _docsService.GetAllEFolderDocuments(request.OrgId, request.LoanId, token);

            if (!prEFolderDocumentNames.Success)
            {
                return new ProcessResult(false, prEFolderDocumentNames.Status, prEFolderDocumentNames.Message);
            }

            prEFolderDocumentNames.Result.RemoveAll(name => dictDocNamesToFind.Keys.Contains(name));

            // Add to list
            ProcessResult<string> prJobId = await _docsService.AddToDocSet(request.OrgId, request.LoanId, prOrderId.Result, applicationId, prEFolderDocumentNames.Result, token, stage);

            if (!prJobId.Success)
            {
                return new ProcessResult(false, prJobId.Status, prJobId.Message);
            }

            prDocuments = await _docsService.GetDocSet(request.OrgId, request.LoanId, prOrderId.Result, prJobId.Result, token, stage);

            if (!prDocuments.Success)
            {
                return new ProcessResult(false, prDocuments.Status, prDocuments.Message);
            }

            foreach (var doc in prDocuments.Result)
            {
                // Map to DocumentMapping model
                //DocumentSignerMapping mapping = _mapper.Map<DocumentSignerMapping>(doc);
                DocumentSignerMapping mapping = new DocumentSignerMapping();
                mapping.DocumentName = doc.name;
                mapping.Signers = doc.signers;
                mapping.LoanType = loanType;
                mapping.AmortizationType = prLoanInfo.Result.loanAmortizationType;
                mapping.LoanPurpose = prLoanInfo.Result.property.loanPurposeType;
                mapping.State = state;
                mapping.BorrowerCount = borrowerCount;
                mapping.UrlaAdditionalBorrowerPage = prLoanInfo.Result.applications[0].coborrower.printOnAdditionalBorrowerPage;

                // Check if there are any signers on this document
                if (mapping.Signers == null || mapping.Signers.Length == 0)
                {
                    // No signers, skip
                    continue;
                }

                mapping.ModifiedAt = DateTime.UtcNow;

                if (borrowerCount == 2)
                {
                    // Credit Score Disclosure documents are generated oddly. 
                    // The first borrower gets "Credit Score Disclosure, 2" and the second borrower gets "Credit Score Disclosure"
                    if (mapping.DocumentName == "Credit Score Disclosure, 2")
                    {
                        // name is generic because it could be used for additional borrowers in future
                        mapping.Signers[0].partyId = "signer2"; // signer 1 is the borrower
                    }
                    else if (mapping.DocumentName == "Credit Score Disclosure")
                    {
                        mapping.Signers[0].partyId = "signer1";
                    }
                    else if (mapping.DocumentName == "1003 - URLA")
                    {
                        // Originator is the 2nd signer on the URLA so we can skip that
                        mapping.Signers[0].partyId = "signer1";
                        mapping.Signers[2].partyId = "signer2";
                    }
                    else
                    {
                        mapping.Signers[0].partyId = "signer1";
                        mapping.Signers[1].partyId = "signer2";
                    }

                    // For joint applications, every file except credit score disclosures get the join-application suffix
                    // We do this because joint applications have additional signature coordinates for the co-borrower
                }
                else
                {
                    mapping.Signers[0].partyId = "signer1";
                }

                // Upsert document mapping
                ProcessResult prUpsert = await _mappingService.UpsertDocMapping(mapping);

                if(!prUpsert.Success)
                    return prUpsert;
            }

            return new ProcessResult(true);
        }

        public async Task<ProcessResult<List<DocumentSignerMapping>>> GetAllDocMappings(string loanType, string state, int borrowerCount, string loanPurpose, string amortizationType, bool urlaAdditionalBorrowerPage)
        {
            return await _mappingService.GetDisclosureMappings(loanType, state, borrowerCount, loanPurpose, amortizationType, urlaAdditionalBorrowerPage);
        }
    }

    public interface IMappingAppService
    {
        Task<ProcessResult> RunAsync(MapRequest request, string token);
        Task<ProcessResult<List<DocumentSignerMapping>>> GetAllDocMappings(string loanType, string state, int borrowerCount, string loanPurpose, string amortizationType, bool urlaAdditionalBorrowerPage);
    }
}
