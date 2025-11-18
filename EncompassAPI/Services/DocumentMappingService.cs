using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;

namespace EncompassAPI.Services
{
    public class DocumentMappingService : IDocumentMappingService
    {
        private readonly IDynamoDBContext _dbContext;
        private LoggerService<DocumentMappingService> _logger;

        public DocumentMappingService(IDynamoDBContext dbContext, LoggerService<DocumentMappingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ProcessResult> UpsertDocMapping(DocumentMapping mapping)
        {
            try
            {
                await _dbContext.SaveAsync(mapping);

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to insert or document mapping for {mapping.name}. Exception: {ex.Message}");
                return new ProcessResult(false, "Failed to insert or update document mapping");
            }
        }

        public async Task<ProcessResult<List<DocumentMapping>>> GetDocMappingByName(string name)
        {
            try
            {
                var config = new DynamoDBOperationConfig
                {
                    // Tell the Query method which GSI (Index) to use for the lookup
                    IndexName = "name"
                };

                // ⚡ Simple Query: Context uses the GSI defined on the Name property.
                var search = _dbContext.QueryAsync<DocumentMapping>(name, config);

                // Get all results from the paginated response
                var docMappings = await search.GetRemainingAsync();

                if (docMappings == null || docMappings.Count == 0)
                {
                    return new ProcessResult<List<DocumentMapping>>(false, "Document mapping not found");
                }

                return new ProcessResult<List<DocumentMapping>>(true)
                {
                    Result = docMappings
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve document mapping for {name}. Exception: {ex.Message}");
                return new ProcessResult<List<DocumentMapping>>(false, "Failed to retrieve document mapping");
            }
        }
    }

    public interface IDocumentMappingService
    {
        Task<ProcessResult> UpsertDocMapping(DocumentMapping mapping);
        Task<ProcessResult<List<DocumentMapping>>> GetDocMappingByName(string name);
    }

    [DynamoDBTable("EncompassDisclosuresDocumentData")]
    public class DocumentMapping
    {
        [DynamoDBHashKey]
        public string id { get; set; }
        [DynamoDBGlobalSecondaryIndexHashKey("name")]
        public string name { get; set; }
        public Signer[] signers { get; set; }
    }
}
