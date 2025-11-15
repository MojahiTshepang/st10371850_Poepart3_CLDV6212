using SleazyRetailers.Models;

namespace SleazyRetailers.Services
{
    public interface IAzureStorageService
    {
        // --- Customer Operations (Table Storage) ---
        Task<IEnumerable<Customer>> GetCustomersAsync();
        Task<Customer> GetCustomerByIdAsync(string customerId);
        Task AddCustomerAsync(Customer customer);
        Task AddCustomerWithFunctionAsync(Customer customer); // NEW
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string customerId);

        // --- Product Operations (Table + Blob Storage) ---
        Task<IEnumerable<Product>> GetProductsAsync();
        Task<Product> GetProductByIdAsync(string productId);
        Task AddProductAsync(Product product, IFormFile imageFile);
        Task ProcessProductImageWithFunctionAsync(string productId, string imageName, string originalFileName); // NEW
        Task UpdateProductAsync(Product product, IFormFile imageFile);
        Task DeleteProductAsync(string productId);

        // --- Order Operations (Table Storage) ---
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<Order> GetOrderByIdAsync(string orderId);
        Task AddOrderAsync(Order order);
        Task ProcessOrderWithFunctionAsync(Order order); // NEW
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string orderId);

        // --- Upload/Other Operations (Blob + Queue + File Share) ---
        Task UploadPaymentProofAndQueueMessageAsync(Upload uploadModel);

        // --- Azure Files Operations ---
        Task<IEnumerable<Contract>> GetContractsAsync();
        Task<Contract> GetContractAsync(string fileName);
        Task UploadContractAsync(Contract contract);
        Task ProcessContractWithFunctionAsync(string fileName, string contractType, long fileSize, string uploadedBy); // NEW
        Task<Stream> DownloadContractAsync(string fileName);
        Task DeleteContractAsync(string fileName);
    }
}