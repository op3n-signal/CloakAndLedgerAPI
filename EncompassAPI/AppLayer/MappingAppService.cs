using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Services;
using Newtonsoft.Json.Linq;

namespace EncompassAPI.AppLayer
{
    public class MappingAppService : IMappingAppService
    {
        private readonly IEncompassService _docsService;
        private readonly IDocumentMappingService _mappingService;
        private readonly IMapper _mapper;

        public MappingAppService(IEncompassService docsService, IDocumentMappingService mappingService, IMapper mapper)
        {
            _docsService = docsService;
            _mappingService = mappingService;
            _mapper = mapper;
        }

        public async Task RunAsync(MapRequest request, string token)
        {
            // Get application id from loan because we need this for the audit
            ProcessResult<string> prApplicationId = await _docsService.GetApplicationIdFromLoan(request.OrgId, request.LoanId, token);

            if (!prApplicationId.Success)
            {
                return;
            }

            Enum.TryParse(request.DisclosureStage, out DisclosureStage stage);

            ProcessResult<string> prAuditId = await _docsService.CreateLoanAuditForOrder(request.OrgId, request.LoanId, prApplicationId.Result, token, stage);

            if (!prAuditId.Success)
            {
                return;
            }

            //todo maybe return response of what failed in the audit // send back in response to candid
            ProcessResult prAuditSuccess = await _docsService.GetDocumentAuditForOrder(request.OrgId, request.LoanId, prAuditId.Result, token, stage);

            if (!prAuditSuccess.Success)
            {
                return;
            }

            // Get documents with signature coordinates
            ProcessResult<string> prOrderId = await _docsService.GenerateDocSet(request.OrgId, request.LoanId, prAuditId.Result, token, stage);

            if (!prOrderId.Success)
            {
                return;
            }

            // This only returns a select amount of documents
            ProcessResult<List<Models.Encompass.Document>> prDocuments = await _docsService.GetOrderStatus(request.OrgId, request.LoanId, prOrderId.Result, token, stage);

            if (!prDocuments.Success)
            {
                return;
            }

            foreach(var doc in prDocuments.Result)
            {
                // Map to DocumentMapping model
                DocumentMapping mapping = _mapper.Map<DocumentMapping>(doc);

                // Upsert document mapping
                ProcessResult prUpsert = await _mappingService.UpsertDocMapping(mapping);
            }
        }
    }

    public interface IMappingAppService
    {
        Task RunAsync(MapRequest request, string token);
    }
}
