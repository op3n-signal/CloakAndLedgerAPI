using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Dynamo;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DisclosuresQueueService : IDisclosuresQueueService
    {
        private readonly IDynamoDBContext _dbContext;
        private readonly LoggerService<DisclosuresQueueService> _logger;    
        private readonly IMapper _mapper;

        public DisclosuresQueueService(IDynamoDBContext dbContext, IMapper mapper, LoggerService<DisclosuresQueueService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProcessResult> UpsertDisclosureQueue(string disclosureLogId, CandidIncomingRequest request)
        {
            try
            {
                var existing = await _dbContext.LoadAsync<DisclosureQueueItem>(disclosureLogId);
                var now = DateTime.UtcNow;

                DisclosureQueueItem item = new DisclosureQueueItem()
                {
                    id = disclosureLogId,
                    json_request = JsonSerializer.Serialize(request),
                    status = "Pending",
                    created_date = existing?.created_date ?? now,
                    modified_date = now
                };

                await _dbContext.SaveAsync(item);

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to enqueue disclosure item for loan: {request.LoanId} on disclosure log: {disclosureLogId}");
                return new ProcessResult(false, "Failed to enqueue disclosure item.");
            }
        }

        public async Task<ProcessResult<DisclosureQueueItem>> GetDisclosureQueueItems()
        {
            try
            {
                var query = _dbContext.QueryAsync<DisclosureQueueItem>(
                    "GLOBAL_DISCLOSURES", // partition key value
                    QueryOperator.Equal,
                    new[] { "GLOBAL_DISCLOSURES" },
                    new DynamoDBOperationConfig
                    {
                        IndexName = "Date_GSI",
                        BackwardQuery = false // ascending = oldest first
                    }
                );

                var items = await query.GetRemainingAsync();

                if(!items.Any())
                {
                    _logger.Base.LogError("No disclosure queue items.");
                    return new ProcessResult<DisclosureQueueItem>(true) { Result = null };
                }

                var orderedItems = items
                .Where(x => x.status != "Processing" && x.status != "Completed")
                .OrderBy(x => x.modified_date)
                .ToList();

                if (!orderedItems.Any())
                {
                    _logger.Base.LogError("No unprocessed disclosure queue items found.");
                    return new ProcessResult<DisclosureQueueItem>(true) { Result = null };
                }

                return new ProcessResult<DisclosureQueueItem>(true, orderedItems[0]);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to get disclosure queue items: {ex.Message}");
                return new ProcessResult<DisclosureQueueItem>(false, ResultStatus.InternalError, "Failed to get disclosure queue items.");
            }
        }
    }

    public interface IDisclosuresQueueService
    {
        Task<ProcessResult> UpsertDisclosureQueue(string disclosureLogId, CandidIncomingRequest request);
        Task<ProcessResult<DisclosureQueueItem>> GetDisclosureQueueItems();
    }
}
