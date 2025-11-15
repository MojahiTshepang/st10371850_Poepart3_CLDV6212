using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Text.Json;

namespace SleazyRetailers.Functions
{
    public class BlobStorageFunction
    {
        private readonly ILogger<BlobStorageFunction> _logger;

        public BlobStorageFunction(ILogger<BlobStorageFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessProductImageBlob")]
        public async Task RunProcessProductImage(
            [QueueTrigger("image-blob-queue", Connection = "AzureWebJobsStorage")] string queueItem)
        {
            try
            {
                _logger.LogInformation($"🔔 Blob Storage Function Triggered: {queueItem}");

                var imageData = JsonSerializer.Deserialize<ImageProcessingItem>(queueItem);

                // Get connection string from environment
                var connectionString = Environment.GetEnvironmentVariable("AzureStorageSettings__ConnectionString");
                var blobServiceClient = new BlobServiceClient(connectionString);

                // Simulate blob operations
                var containerClient = blobServiceClient.GetBlobContainerClient("productimages-processed");
                await containerClient.CreateIfNotExistsAsync();

                _logger.LogInformation($"🖼️ Processing image for Blob Storage - Product: {imageData.ProductId}");
                _logger.LogInformation($"📁 Image will be stored as: {imageData.ImageName}");
                _logger.LogInformation($"📦 Target Container: productimages-processed");

                // Simulate image processing (resize, optimize, etc.)
                _logger.LogInformation("🔄 Step 1: Resizing image...");
                await Task.Delay(300);

                _logger.LogInformation("🎨 Step 2: Optimizing image quality...");
                await Task.Delay(300);

                _logger.LogInformation("💾 Step 3: Uploading to blob storage...");
                await Task.Delay(200);

                _logger.LogInformation($"✅ Image {imageData.ImageName} processed for Blob Storage");
                _logger.LogInformation($"📊 Blob Storage Operation Completed - Container: productimages-processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Blob Storage Function");
                throw;
            }
        }

        public class ImageProcessingItem
        {
            public string ProductId { get; set; }
            public string ImageName { get; set; }
            public string OriginalFileName { get; set; }
        }
    }
}