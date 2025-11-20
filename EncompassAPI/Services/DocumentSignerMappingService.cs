using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Dynamo;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Entity;
using EncompassAPI.Repos;
using System.Collections.Generic;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DocumentSignerMappingService : IDocumentSignerMappingService
    {
        private LoggerService<DocumentSignerMappingService> _logger;
        private readonly IDisclosureDataRepo _disclosureDataRepo;
        private readonly IMapper _mapper;

        public DocumentSignerMappingService(LoggerService<DocumentSignerMappingService> logger, IDisclosureDataRepo disclosureDataRepo, IMapper mapper)
        {
            _logger = logger;
            _disclosureDataRepo = disclosureDataRepo;
            _mapper = mapper;
        }

        public async Task<ProcessResult> UpsertDocMapping(DocumentSignerMapping mapping)
        {
            try
            {
                string id = "";

                try
                {

                    id = await _disclosureDataRepo.GetDisclosureSignerMappingId(mapping);
                }
                catch(Exception ex)
                {
                    _logger.Base.LogInformation($"No records found by variant key for {mapping.DocumentName}... proceeding to insert...");
                }

                bool upserted;

                if(string.IsNullOrEmpty(id))
                {
                    upserted = await _disclosureDataRepo.InsertDisclosureSignerMapping(mapping);
                }
                else
                {
                    mapping.Id = id;
                    upserted = await _disclosureDataRepo.UpdateDisclosureSignerMapping(mapping);
                }

                if(!upserted)
                {
                    _logger.Base.LogError($"Failed to insert or update document mapping for {mapping.DocumentName}");
                    return new ProcessResult(false, ResultStatus.InternalError, "Failed to insert or update document mapping");
                }

                _logger.LogVerbose($"document mapping upserted: {mapping.DocumentName}");

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to insert or update document mapping for {mapping.DocumentName}. Exception: {ex}");
                return new ProcessResult(false, ResultStatus.InternalError, "Failed to insert or update document mapping");
            }
        }

        public async Task<ProcessResult<List<DocumentSignerMapping>>> GetDisclosureMappings(string loanType, string state, int borrowerCount)
        {
            try
            {
                List<DocumentSignerMappingEntity> documentSignerMappingEntities = await _disclosureDataRepo.GetDisclosureSignerMappings(loanType, state, borrowerCount);

                if (documentSignerMappingEntities == null || documentSignerMappingEntities.Count == 0)
                {
                    return new ProcessResult<List<DocumentSignerMapping>>(false, $"Disclosure signer mappings not found for loanType: {loanType}, state: {state}, borrowerCount: {borrowerCount}");
                }

                List<DocumentSignerMapping> documentSignerMappings = new List<DocumentSignerMapping>();

                foreach (var entity in documentSignerMappingEntities)
                {
                    DocumentSignerMapping mapping = _mapper.Map<DocumentSignerMapping>(entity);
                    documentSignerMappings.Add(mapping);
                }

                return new ProcessResult<List<DocumentSignerMapping>>(true)
                {
                    Result = documentSignerMappings
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve Disclosure signer mappings for loanType: {loanType}, state: {state}, borrowerCount: {borrowerCount}.{Environment.NewLine} Exception: {ex.Message}");
                return new ProcessResult<List<DocumentSignerMapping>>(false, ResultStatus.InternalError, "Failed to retrieve Disclosure signer mappings");
            }
        }
    }

    public interface IDocumentSignerMappingService
    {
        Task<ProcessResult> UpsertDocMapping(DocumentSignerMapping mapping);
        Task<ProcessResult<List<DocumentSignerMapping>>> GetDisclosureMappings(string loanType, string state, int borrowerCount);
    }
}
