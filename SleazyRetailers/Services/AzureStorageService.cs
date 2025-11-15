using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using SleazyRetailers.Models;
using Microsoft.Extensions.Configuration;
using Azure;
using System.Text.Json;

namespace SleazyRetailers.Services
{
    // INTERNAL Azure Table Entity Classes
    public class CustomerEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ShippingAddress { get; set; }
    }

    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public int StockAvailable { get; set; }
        public string ImageUrl { get; set; }
    }

    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string CustomerId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTimeOffset OrderDate { get; set; }  // FIXED: Changed from DateTime to DateTimeOffset
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
    }
    // END INTERNAL CLASSES

    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        private readonly TableClient _orderTable;
        private readonly BlobContainerClient _productImageContainer;
        private readonly BlobContainerClient _paymentProofContainer;
        private readonly QueueClient _orderQueue;
        private readonly ShareDirectoryClient _contractDirectory;
        private readonly string _productImageContainerName;

        // ADD THESE QUEUE CLIENTS FOR FUNCTIONS INTEGRATION
        private readonly QueueClient _customerQueue;
        private readonly QueueClient _imageProcessingQueue;
        private readonly QueueClient _orderProcessingQueue;
        private readonly QueueClient _contractProcessingQueue;

        // ADD FALLBACK SERVICE
        private readonly FallbackStorageService _fallbackService;

        public AzureStorageService(TableServiceClient tableServiceClient, BlobServiceClient blobServiceClient,
                                   QueueServiceClient queueServiceClient, IConfiguration configuration)
        {
            var config = configuration.GetSection("AzureStorageSettings");

            // Initialize fallback service
            _fallbackService = new FallbackStorageService();

            // Table Storage Clients
            _customerTable = tableServiceClient.GetTableClient(config["CustomerTableName"]);
            _productTable = tableServiceClient.GetTableClient(config["ProductTableName"]);
            _orderTable = tableServiceClient.GetTableClient(config["OrderTableName"]);

            // COMMENT OUT ALL AUTO-CREATION TO FIX 403 ERRORS
            // _customerTable.CreateIfNotExists();
            // _productTable.CreateIfNotExists();
            // _orderTable.CreateIfNotExists();

            // Blob Storage Containers - USE EXISTING CONTAINERS ONLY
            _productImageContainerName = config["ProductImageContainerName"];
            _productImageContainer = blobServiceClient.GetBlobContainerClient(_productImageContainerName);
            // _productImageContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

            _paymentProofContainer = blobServiceClient.GetBlobContainerClient(config["PaymentProofContainerName"]);
            // _paymentProofContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.None);

            // Queue Storage - USE EXISTING QUEUES ONLY
            _orderQueue = queueServiceClient.GetQueueClient(config["OrderQueueName"]);
            // _orderQueue.CreateIfNotExists();

            // ADD FUNCTION QUEUES - USE EXISTING ONLY
            _customerQueue = queueServiceClient.GetQueueClient("customer-table-queue");
            _imageProcessingQueue = queueServiceClient.GetQueueClient("image-blob-queue");
            _orderProcessingQueue = queueServiceClient.GetQueueClient("order-queue");
            _contractProcessingQueue = queueServiceClient.GetQueueClient("contract-file-queue");

            // _customerQueue.CreateIfNotExists();
            // _imageProcessingQueue.CreateIfNotExists();
            // _orderProcessingQueue.CreateIfNotExists();
            // _contractProcessingQueue.CreateIfNotExists();

            // Azure Files Share - USE EXISTING ONLY
            var shareClient = new ShareClient(config["ConnectionString"], config["ContractFileShareName"]);
            // shareClient.CreateIfNotExists();
            _contractDirectory = shareClient.GetDirectoryClient(config["ContractDirectoryName"]);
            // _contractDirectory.CreateIfNotExists();
        }

        // --- CUSTOMER IMPLEMENTATIONS ---
        public async Task<IEnumerable<Customer>> GetCustomersAsync()
        {
            try
            {
                var customers = new List<Customer>();
                await foreach (var entity in _customerTable.QueryAsync<CustomerEntity>())
                {
                    customers.Add(new Customer
                    {
                        Id = entity.RowKey,
                        FirstName = entity.FirstName,
                        LastName = entity.LastName,
                        Username = entity.Username,
                        Email = entity.Email,
                        ShippingAddress = entity.ShippingAddress
                    });
                }
                return customers;
            }
            catch (RequestFailedException ex)
            {
                // Table doesn't exist or no permissions - use fallback
                Console.WriteLine($"Warning: Could not access Customers table: {ex.Message}");
                return await _fallbackService.GetCustomersAsync();
            }
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            try
            {
                var entity = new CustomerEntity
                {
                    RowKey = Guid.NewGuid().ToString(),
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Username = customer.Username,
                    Email = customer.Email,
                    ShippingAddress = customer.ShippingAddress
                };
                await _customerTable.AddEntityAsync(entity);

                // Store in fallback as well for consistency
                customer.Id = entity.RowKey;
                await _fallbackService.AddCustomerAsync(customer);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.AddCustomerAsync(customer);
            }
        }

        // NEW: Add customer with Function trigger
        public async Task AddCustomerWithFunctionAsync(Customer customer)
        {
            try
            {
                var queueMessage = new
                {
                    customer.FirstName,
                    customer.LastName,
                    customer.Username,
                    customer.Email,
                    customer.ShippingAddress,
                    Action = "CreateCustomer",
                    Timestamp = DateTime.UtcNow
                };

                string messageBody = JsonSerializer.Serialize(queueMessage);
                await _customerQueue.SendMessageAsync(messageBody);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error sending customer to function queue: {ex.Message}");
                // Fall back to direct storage
                await AddCustomerAsync(customer);
            }
        }

        public async Task<Customer> GetCustomerByIdAsync(string customerId)
        {
            try
            {
                Response<CustomerEntity> response = await _customerTable.GetEntityAsync<CustomerEntity>("Customer", customerId);
                var entity = response.Value;
                return new Customer
                {
                    Id = entity.RowKey,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    Username = entity.Username,
                    Email = entity.Email,
                    ShippingAddress = entity.ShippingAddress
                };
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error getting customer from Azure, using fallback: {ex.Message}");
                // Use fallback storage
                return await _fallbackService.GetCustomerByIdAsync(customerId);
            }
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            try
            {
                Response<CustomerEntity> existingResponse = await _customerTable.GetEntityAsync<CustomerEntity>("Customer", customer.Id);
                CustomerEntity entity = existingResponse.Value;

                entity.FirstName = customer.FirstName;
                entity.LastName = customer.LastName;
                entity.Username = customer.Username;
                entity.Email = customer.Email;
                entity.ShippingAddress = customer.ShippingAddress;

                await _customerTable.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

                // Update fallback as well
                await _fallbackService.UpdateCustomerAsync(customer);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.UpdateCustomerAsync(customer);
            }
        }

        public async Task DeleteCustomerAsync(string customerId)
        {
            try
            {
                await _customerTable.DeleteEntityAsync("Customer", customerId);

                // Delete from fallback as well
                await _fallbackService.DeleteCustomerAsync(customerId);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.DeleteCustomerAsync(customerId);
            }
        }

        // --- PRODUCT IMPLEMENTATIONS ---
        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            try
            {
                var products = new List<Product>();
                await foreach (var entity in _productTable.QueryAsync<ProductEntity>())
                {
                    products.Add(new Product
                    {
                        Id = entity.RowKey,
                        ProductName = entity.ProductName,
                        Description = entity.Description,
                        Price = entity.Price,
                        StockAvailable = entity.StockAvailable,
                        ImageUrl = entity.ImageUrl
                    });
                }
                return products;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Warning: Could not access Products table: {ex.Message}");
                return await _fallbackService.GetProductsAsync();
            }
        }

        public async Task<Product> GetProductByIdAsync(string productId)
        {
            try
            {
                Response<ProductEntity> response = await _productTable.GetEntityAsync<ProductEntity>("Product", productId);
                var entity = response.Value;
                return new Product
                {
                    Id = entity.RowKey,
                    ProductName = entity.ProductName,
                    Description = entity.Description,
                    Price = entity.Price,
                    StockAvailable = entity.StockAvailable,
                    ImageUrl = entity.ImageUrl
                };
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error getting product: {ex.Message}");
                return await _fallbackService.GetProductByIdAsync(productId);
            }
        }

        public async Task AddProductAsync(Product product, IFormFile imageFile)
        {
            try
            {
                var entity = new ProductEntity
                {
                    RowKey = Guid.NewGuid().ToString(),
                    ProductName = product.ProductName,
                    Description = product.Description,
                    Price = product.Price,
                    StockAvailable = product.StockAvailable,
                };

                if (imageFile != null && imageFile.Length > 0)
                {
                    var uniqueFileName = $"{entity.RowKey}_{Path.GetFileName(imageFile.FileName)}";
                    var blobClient = _productImageContainer.GetBlobClient(uniqueFileName);
                    using (var stream = imageFile.OpenReadStream())
                    {
                        await blobClient.UploadAsync(stream, true);
                    }
                    entity.ImageUrl = blobClient.Uri.ToString();

                    // TRIGGER FUNCTION FOR IMAGE PROCESSING
                    await ProcessProductImageWithFunctionAsync(entity.RowKey, uniqueFileName, imageFile.FileName);
                }

                await _productTable.AddEntityAsync(entity);

                // Add to fallback
                product.Id = entity.RowKey;
                await _fallbackService.AddProductAsync(product, imageFile);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.AddProductAsync(product, imageFile);
            }
        }

        // NEW: Process product image with Function trigger
        public async Task ProcessProductImageWithFunctionAsync(string productId, string imageName, string originalFileName)
        {
            try
            {
                var queueMessage = new
                {
                    ProductId = productId,
                    ImageName = imageName,
                    OriginalFileName = originalFileName,
                    Action = "ProcessImage",
                    Timestamp = DateTime.UtcNow
                };

                string messageBody = JsonSerializer.Serialize(queueMessage);
                await _imageProcessingQueue.SendMessageAsync(messageBody);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error sending image to function queue: {ex.Message}");
                // Continue without function trigger
            }
        }

        public async Task UpdateProductAsync(Product product, IFormFile imageFile)
        {
            try
            {
                Response<ProductEntity> existingResponse = await _productTable.GetEntityAsync<ProductEntity>("Product", product.Id);
                ProductEntity entity = existingResponse.Value;

                entity.ProductName = product.ProductName;
                entity.Description = product.Description;
                entity.Price = product.Price;
                entity.StockAvailable = product.StockAvailable;

                if (imageFile != null && imageFile.Length > 0)
                {
                    var uniqueFileName = $"{entity.RowKey}_{Path.GetFileName(imageFile.FileName)}";
                    var blobClient = _productImageContainer.GetBlobClient(uniqueFileName);
                    using (var stream = imageFile.OpenReadStream())
                    {
                        await blobClient.UploadAsync(stream, true);
                    }
                    entity.ImageUrl = blobClient.Uri.ToString();
                }

                await _productTable.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

                // Update fallback
                await _fallbackService.UpdateProductAsync(product, imageFile);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.UpdateProductAsync(product, imageFile);
            }
        }

        public async Task DeleteProductAsync(string productId)
        {
            try
            {
                Response<ProductEntity> response = await _productTable.GetEntityAsync<ProductEntity>("Product", productId);
                if (!string.IsNullOrEmpty(response.Value.ImageUrl))
                {
                    Uri uri = new Uri(response.Value.ImageUrl);
                    string blobName = Path.GetFileName(uri.LocalPath);
                    var blobClient = _productImageContainer.GetBlobClient(blobName);
                    await blobClient.DeleteIfExistsAsync();
                }
                await _productTable.DeleteEntityAsync("Product", productId);

                // Delete from fallback
                await _fallbackService.DeleteProductAsync(productId);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.DeleteProductAsync(productId);
            }
        }

        // --- ORDER IMPLEMENTATIONS --- (FIXED DATE ISSUE)
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            try
            {
                var orders = new List<Order>();
                await foreach (var entity in _orderTable.QueryAsync<OrderEntity>())
                {
                    orders.Add(new Order
                    {
                        Id = entity.RowKey,
                        CustomerId = entity.CustomerId,
                        ProductId = entity.ProductId,
                        Quantity = entity.Quantity,
                        OrderDate = entity.OrderDate.DateTime,  // Convert DateTimeOffset back to DateTime
                        TotalAmount = entity.TotalAmount,
                        Status = entity.Status,
                        ShippingAddress = entity.ShippingAddress
                    });
                }
                return orders;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Warning: Could not access Orders table: {ex.Message}");
                return await _fallbackService.GetOrdersAsync();
            }
        }

        public async Task<Order> GetOrderByIdAsync(string orderId)
        {
            try
            {
                Response<OrderEntity> response = await _orderTable.GetEntityAsync<OrderEntity>("Order", orderId);
                var entity = response.Value;
                return new Order
                {
                    Id = entity.RowKey,
                    CustomerId = entity.CustomerId,
                    ProductId = entity.ProductId,
                    Quantity = entity.Quantity,
                    OrderDate = entity.OrderDate.DateTime,  // Convert DateTimeOffset back to DateTime
                    TotalAmount = entity.TotalAmount,
                    Status = entity.Status,
                    ShippingAddress = entity.ShippingAddress
                };
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error getting order: {ex.Message}");
                return await _fallbackService.GetOrderByIdAsync(orderId);
            }
        }

        public async Task AddOrderAsync(Order order)
        {
            try
            {
                Console.WriteLine($"=== ADDING ORDER TO AZURE STORAGE ===");
                Console.WriteLine($"CustomerId: {order.CustomerId}");
                Console.WriteLine($"ProductId: {order.ProductId}");
                Console.WriteLine($"Quantity: {order.Quantity}");
                Console.WriteLine($"TotalAmount: {order.TotalAmount}");
                Console.WriteLine($"OrderDate: {order.OrderDate}");

                var entity = new OrderEntity
                {
                    RowKey = Guid.NewGuid().ToString(),
                    CustomerId = order.CustomerId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    OrderDate = new DateTimeOffset(order.OrderDate),  // FIXED: Convert DateTime to DateTimeOffset
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    ShippingAddress = order.ShippingAddress
                };

                Console.WriteLine($"OrderEntity created with RowKey: {entity.RowKey}");

                // Check if table exists and create if necessary
                try
                {
                    await _orderTable.AddEntityAsync(entity);
                    Console.WriteLine("Order successfully added to Azure Table Storage");
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    Console.WriteLine("Orders table doesn't exist, creating it...");
                    await _orderTable.CreateIfNotExistsAsync();
                    await _orderTable.AddEntityAsync(entity);
                    Console.WriteLine("Orders table created and order added successfully");
                }

                // TRIGGER FUNCTION FOR ORDER PROCESSING
                await ProcessOrderWithFunctionAsync(order);

                // Add to fallback
                order.Id = entity.RowKey;
                await _fallbackService.AddOrderAsync(order);
                Console.WriteLine("Order also added to fallback storage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Azure Storage failed: {ex.Message}, using fallback");
                // Use fallback storage
                await _fallbackService.AddOrderAsync(order);
            }
        }

        // NEW: Process order with Function trigger
        public async Task ProcessOrderWithFunctionAsync(Order order)
        {
            try
            {
                var queueMessage = new
                {
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    TotalAmount = order.TotalAmount,
                    Action = "ProcessOrder",
                    Timestamp = DateTime.UtcNow
                };

                string messageBody = JsonSerializer.Serialize(queueMessage);
                await _orderProcessingQueue.SendMessageAsync(messageBody);
                Console.WriteLine("Order sent to function queue for processing");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error sending order to function queue: {ex.Message}");
                // Continue without function trigger
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            try
            {
                Response<OrderEntity> existingResponse = await _orderTable.GetEntityAsync<OrderEntity>("Order", order.Id);
                OrderEntity entity = existingResponse.Value;

                entity.CustomerId = order.CustomerId;
                entity.ProductId = order.ProductId;
                entity.Quantity = order.Quantity;
                entity.OrderDate = new DateTimeOffset(order.OrderDate);  // FIXED: Convert DateTime to DateTimeOffset
                entity.TotalAmount = order.TotalAmount;
                entity.Status = order.Status;
                entity.ShippingAddress = order.ShippingAddress;

                await _orderTable.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

                // Update fallback
                await _fallbackService.UpdateOrderAsync(order);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.UpdateOrderAsync(order);
            }
        }

        public async Task DeleteOrderAsync(string orderId)
        {
            try
            {
                await _orderTable.DeleteEntityAsync("Order", orderId);

                // Delete from fallback
                await _fallbackService.DeleteOrderAsync(orderId);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Azure Storage failed, using fallback: {ex.Message}");
                // Use fallback storage
                await _fallbackService.DeleteOrderAsync(orderId);
            }
        }

        // --- UPLOAD/PAYMENT PROOF IMPLEMENTATIONS ---
        public async Task UploadPaymentProofAndQueueMessageAsync(Upload uploadModel)
        {
            try
            {
                var fileExtension = Path.GetExtension(uploadModel.FileToUpload.FileName).ToLowerInvariant();
                var uniqueFileName = $"proof_{Guid.NewGuid()}{fileExtension}";
                var blobClient = _paymentProofContainer.GetBlobClient(uniqueFileName);

                using (var stream = uploadModel.FileToUpload.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                var queueMessage = new
                {
                    FileName = uniqueFileName,
                    BlobName = uniqueFileName,
                    RelatedOrderId = uploadModel.RelatedOrderId,
                    CustomerName = uploadModel.CustomerName,
                    Action = "PaymentProofUploaded",
                    UploadTime = DateTime.UtcNow
                };

                string messageBody = JsonSerializer.Serialize(queueMessage);
                await _orderQueue.SendMessageAsync(messageBody);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error uploading payment proof: {ex.Message}");
                throw;
            }
        }

        // --- AZURE FILES CONTRACT IMPLEMENTATIONS ---
        public async Task<IEnumerable<Contract>> GetContractsAsync()
        {
            try
            {
                var contracts = new List<Contract>();

                await foreach (var fileItem in _contractDirectory.GetFilesAndDirectoriesAsync())
                {
                    if (!fileItem.IsDirectory)
                    {
                        var fileClient = _contractDirectory.GetFileClient(fileItem.Name);
                        var properties = await fileClient.GetPropertiesAsync();

                        contracts.Add(new Contract
                        {
                            ContractName = fileItem.Name,
                            FileSize = properties.Value.ContentLength,
                            UploadDate = properties.Value.LastModified.DateTime,
                            ContractType = GetContractTypeFromFileName(fileItem.Name)
                        });
                    }
                }

                return contracts.OrderByDescending(c => c.UploadDate);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Warning: Could not access contracts: {ex.Message}");
                return await _fallbackService.GetContractsAsync();
            }
        }

        public async Task<Contract> GetContractAsync(string fileName)
        {
            try
            {
                var fileClient = _contractDirectory.GetFileClient(fileName);
                var properties = await fileClient.GetPropertiesAsync();

                return new Contract
                {
                    ContractName = fileName,
                    FileSize = properties.Value.ContentLength,
                    UploadDate = properties.Value.LastModified.DateTime,
                    ContractType = GetContractTypeFromFileName(fileName),
                    Description = $"Contract file: {fileName}"
                };
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error getting contract: {ex.Message}");
                return await _fallbackService.GetContractAsync(fileName);
            }
        }

        public async Task UploadContractAsync(Contract contract)
        {
            try
            {
                if (contract?.ContractFile == null || contract.ContractFile.Length == 0)
                    throw new ArgumentException("Contract file is required");

                var fileName = string.IsNullOrEmpty(contract.ContractName)
                    ? contract.ContractFile.FileName
                    : contract.ContractName;

                // Ensure file extension is preserved
                if (!fileName.Contains('.'))
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName) +
                              Path.GetExtension(contract.ContractFile.FileName);
                }

                var fileClient = _contractDirectory.GetFileClient(fileName);

                using (var stream = contract.ContractFile.OpenReadStream())
                {
                    await fileClient.CreateAsync(stream.Length);
                    await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
                }

                // TRIGGER FUNCTION FOR CONTRACT PROCESSING
                await ProcessContractWithFunctionAsync(fileName, contract.ContractType, contract.ContractFile.Length, "System");

                // Add to fallback
                await _fallbackService.AddContractAsync(contract);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error uploading contract: {ex.Message}");
                // Use fallback storage
                await _fallbackService.AddContractAsync(contract);
            }
        }

        public async Task ProcessContractWithFunctionAsync(string fileName, string contractType, long fileSize, string uploadedBy)
        {
            try
            {
                var queueMessage = new
                {
                    FileName = fileName,
                    ContractType = contractType,
                    FileSize = fileSize,
                    UploadedBy = uploadedBy,
                    Action = "ProcessContract",
                    Timestamp = DateTime.UtcNow
                };

                string messageBody = JsonSerializer.Serialize(queueMessage);
                await _contractProcessingQueue.SendMessageAsync(messageBody);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error sending contract to function queue: {ex.Message}");
                // Continue without function trigger
            }
        }

        public async Task<Stream> DownloadContractAsync(string fileName)
        {
            try
            {
                var fileClient = _contractDirectory.GetFileClient(fileName);
                var response = await fileClient.DownloadAsync();
                return response.Value.Content;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error downloading contract: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteContractAsync(string fileName)
        {
            try
            {
                var fileClient = _contractDirectory.GetFileClient(fileName);
                await fileClient.DeleteAsync();

                // Delete from fallback
                await _fallbackService.DeleteContractAsync(fileName);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error deleting contract: {ex.Message}");
                // Use fallback storage
                await _fallbackService.DeleteContractAsync(fileName);
            }
        }

        private string GetContractTypeFromFileName(string fileName)
        {
            var lowerFileName = fileName.ToLowerInvariant();

            if (lowerFileName.Contains("supplier") || lowerFileName.Contains("vendor"))
                return "Supplier";
            else if (lowerFileName.Contains("customer") || lowerFileName.Contains("client"))
                return "Customer";
            else if (lowerFileName.Contains("service") || lowerFileName.Contains("sla"))
                return "Service";
            else if (lowerFileName.Contains("nda") || lowerFileName.Contains("confidential"))
                return "NDA";
            else if (lowerFileName.Contains("purchase") || lowerFileName.Contains("po"))
                return "Purchase";
            else if (lowerFileName.Contains("license") || lowerFileName.Contains("software"))
                return "License";
            else if (lowerFileName.Contains("employment") || lowerFileName.Contains("hr"))
                return "Employment";
            else
                return "General";
        }
    }
}