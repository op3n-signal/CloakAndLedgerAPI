using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal.Transform;
using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Dynamo;
using Shared.Models;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DisclosuresQueueService : IDisclosuresQueueService
    {
        private readonly IDynamoDBContext _dbContext;
        private readonly IAmazonDynamoDB _dynamoClient;
        private readonly LoggerService<DisclosuresQueueService> _logger;    

        public DisclosuresQueueService(IDynamoDBContext dbContext, IAmazonDynamoDB dynamoClient, LoggerService<DisclosuresQueueService> logger)
        {
            _dbContext = dbContext;
            _dynamoClient = dynamoClient;
            _logger = logger;
        }

        public async Task<ProcessResult> UpsertDisclosureQueue(string disclosureLogId, string token, CandidIncomingRequest request, DisclosureStatus status)
        {
            try
            {
                var existing = await _dbContext.LoadAsync<DisclosureQueueItem>(disclosureLogId);
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                DisclosureQueueItem item = new DisclosureQueueItem()
                {
                    id = disclosureLogId,
                    token = token,
                    json_request = JsonSerializer.Serialize(request),
                    status = status.ToString(),
                    created_date = existing?.created_date ?? now,
                    modified_date = now
                };

                await _dbContext.SaveAsync(item);

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to enqueue disclosure item for loan: {request.LoanId} on disclosure log: {disclosureLogId}{Environment.NewLine}{ex}");
                return new ProcessResult(false, "Failed to enqueue disclosure item.");
            }
        }

        public async Task<ProcessResult<DisclosureQueueItem>> GetDisclosureQueueItems()
        {
            try
            {
                var config = new DynamoDBOperationConfig
                {
                    IndexName = "Date_GSI",
                    BackwardQuery = false, // oldest first
                    ConsistentRead = false
                };

                var query = _dbContext.QueryAsync<DisclosureQueueItem>("GLOBAL_DISCLOSURES", config);
                var items = await query.GetNextSetAsync();

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

        public async Task<ProcessResult<DisclosureQueueItem>> GetDisclosureQueueItemById(string disclosureLogId)
        {
            try
            {
                // LoadAsync fetches by primary key (hash key)
                var item = await _dbContext.LoadAsync<DisclosureQueueItem>(disclosureLogId);

                if (item == null)
                {
                    _logger.Base.LogWarning($"Disclosure queue item not found for id: {disclosureLogId}");
                    return new ProcessResult<DisclosureQueueItem>(false, $"Disclosure queue item not found for id: {disclosureLogId}");
                }

                return new ProcessResult<DisclosureQueueItem>(true) { Result = item };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to get disclosure queue item by id {disclosureLogId}: {ex}");
                return new ProcessResult<DisclosureQueueItem>(false, ResultStatus.InternalError, $"Failed to get disclosure queue item by id: {disclosureLogId}");
            }
        }

        public async Task<ProcessResult> DeleteDisclosureQueueItem(string disclosureLogId)
        {
            try
            {
                await _dbContext.DeleteAsync<DisclosureQueueItem>(disclosureLogId);
                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to delete disclosure queue item with id {disclosureLogId}: {ex}");
                return new ProcessResult(false, ResultStatus.InternalError, $"Failed to delete disclosure queue item with id: {disclosureLogId}");
            }
        }

        public async Task<ProcessResult> UpdateStatus(string disclosureLogId, DisclosureStatus status)
        {
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var request = new UpdateItemRequest
                {
                    TableName = "DisclosuresQueue",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = disclosureLogId }
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#s"] = "status",
                        ["#m"] = "modified_date"
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":s"] = new AttributeValue { S = status.ToString() },
                        [":m"] = new AttributeValue { N = now.ToString() }
                    },
                    UpdateExpression = "SET #s = :s, #m = :m"
                };

                if (status == DisclosureStatus.Retrying)
                {
                    request.ExpressionAttributeNames.Add("#r", "retryCount");
                    request.ExpressionAttributeValues.Add(":inc", new AttributeValue { N = "1" }); // increment by 1
                    // Increment the existing retryCount
                    request.UpdateExpression += " ADD #r :inc";
                }

                await _dynamoClient.UpdateItemAsync(request);

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to update status for disclosure queue item with id {disclosureLogId}: {ex}");
                return new ProcessResult(false, ResultStatus.InternalError, $"Failed to update status for disclosure queue item with id: {disclosureLogId}");
            }
        }
    }

    public interface IDisclosuresQueueService
    {
        Task<ProcessResult> UpsertDisclosureQueue(string disclosureLogId, string token, CandidIncomingRequest request, DisclosureStatus status);
        Task<ProcessResult<DisclosureQueueItem>> GetDisclosureQueueItems();
        Task<ProcessResult<DisclosureQueueItem>> GetDisclosureQueueItemById(string disclosureLogId);
        Task<ProcessResult> DeleteDisclosureQueueItem(string disclosureLogId);
        Task<ProcessResult> UpdateStatus(string disclosureLogId, DisclosureStatus status);
    }
}
