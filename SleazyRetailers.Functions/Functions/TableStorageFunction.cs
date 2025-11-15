using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Text.Json;

namespace SleazyRetailers.Functions
{
    public class TableStorageFunction
    {
        private readonly ILogger<TableStorageFunction> _logger;

        public TableStorageFunction(ILogger<TableStorageFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessCustomerTable")]
        public async Task RunProcessCustomer(
            [QueueTrigger("customer-table-queue", Connection = "AzureWebJobsStorage")] string queueItem)
        {
            try
            {
                _logger.LogInformation($"🔔 Table Storage Function Triggered: {queueItem}");

                var customerData = JsonSerializer.Deserialize<CustomerQueueItem>(queueItem);

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureStorageSettings__ConnectionString");
                var tableServiceClient = new TableServiceClient(connectionString);

                // Write to Table Storage
                var tableClient = tableServiceClient.GetTableClient("Customers");
                await tableClient.CreateIfNotExistsAsync();

                var customerEntity = new TableCustomerEntity
                {
                    PartitionKey = "Customer",
                    RowKey = Guid.NewGuid().ToString(),
                    FirstName = customerData.FirstName,
                    LastName = customerData.LastName,
                    Username = customerData.Username,
                    Email = customerData.Email,
                    ShippingAddress = customerData.ShippingAddress
                };

                await tableClient.AddEntityAsync(customerEntity);

                _logger.LogInformation($"✅ Customer {customerData.Username} written to Table Storage via Function");
                _logger.LogInformation($"📊 Table Storage Operation Completed - RowKey: {customerEntity.RowKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Table Storage Function");
                throw;
            }
        }

        public class CustomerQueueItem
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string ShippingAddress { get; set; }
        }

        public class TableCustomerEntity : ITableEntity
        {
            public string PartitionKey { get; set; } = "Customer";
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public Azure.ETag ETag { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string ShippingAddress { get; set; }
        }
    }
}