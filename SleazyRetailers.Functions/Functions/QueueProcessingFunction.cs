using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SleazyRetailers.Functions
{
    public class QueueProcessingFunction
    {
        private readonly ILogger<QueueProcessingFunction> _logger;

        public QueueProcessingFunction(ILogger<QueueProcessingFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessOrderQueue")]
        public async Task RunProcessOrderQueue(
            [QueueTrigger("order-queue", Connection = "AzureWebJobsStorage")] string queueItem)
        {
            try
            {
                _logger.LogInformation($"🔔 Queue Processing Function Triggered: {queueItem}");

                var orderData = JsonSerializer.Deserialize<OrderQueueItem>(queueItem);

                _logger.LogInformation($"📦 Processing order queue - Order: {orderData.OrderId}");
                _logger.LogInformation($"👤 Customer: {orderData.CustomerId}, Amount: ${orderData.TotalAmount}");

                // Simulate order processing workflow
                _logger.LogInformation("📦 Step 1: Validating inventory levels...");
                await Task.Delay(300);

                _logger.LogInformation("💰 Step 2: Calculating total and taxes...");
                await Task.Delay(200);

                _logger.LogInformation("💳 Step 3: Processing payment transaction...");
                await Task.Delay(400);

                _logger.LogInformation("📮 Step 4: Generating shipping label...");
                await Task.Delay(300);

                _logger.LogInformation("✅ Step 5: Order finalized and ready for shipping!");
                await Task.Delay(200);

                _logger.LogInformation($"🎉 Order {orderData.OrderId} processed successfully via Queue Function");
                _logger.LogInformation($"📊 Queue Processing Completed - Customer: {orderData.CustomerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Queue Processing Function");
                throw;
            }
        }

        public class OrderQueueItem
        {
            public string OrderId { get; set; }
            public string CustomerId { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }
}