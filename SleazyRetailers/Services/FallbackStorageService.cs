using SleazyRetailers.Models;
using Microsoft.AspNetCore.Http;

namespace SleazyRetailers.Services
{
    public class FallbackStorageService
    {
        private static List<Customer> _customers = new List<Customer>();
        private static List<Product> _products = new List<Product>();
        private static List<Order> _orders = new List<Order>();
        private static List<Contract> _contracts = new List<Contract>();

        // Customer Operations
        public Task<IEnumerable<Customer>> GetCustomersAsync()
        {
            return Task.FromResult(_customers.AsEnumerable());
        }

        public Task AddCustomerAsync(Customer customer)
        {
            customer.Id = Guid.NewGuid().ToString();
            _customers.Add(customer);
            return Task.CompletedTask;
        }

        public Task<Customer> GetCustomerByIdAsync(string customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == customerId);
            return Task.FromResult(customer);
        }

        public Task UpdateCustomerAsync(Customer customer)
        {
            var existing = _customers.FirstOrDefault(c => c.Id == customer.Id);
            if (existing != null)
            {
                _customers.Remove(existing);
                _customers.Add(customer);
            }
            return Task.CompletedTask;
        }

        public Task DeleteCustomerAsync(string customerId)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
                _customers.Remove(customer);
            return Task.CompletedTask;
        }

        // Product Operations
        public Task<IEnumerable<Product>> GetProductsAsync()
        {
            return Task.FromResult(_products.AsEnumerable());
        }

        public Task AddProductAsync(Product product, IFormFile imageFile)
        {
            product.Id = Guid.NewGuid().ToString();
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task<Product> GetProductByIdAsync(string productId)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);
            return Task.FromResult(product);
        }

        public Task UpdateProductAsync(Product product, IFormFile imageFile)
        {
            var existing = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existing != null)
            {
                _products.Remove(existing);
                _products.Add(product);
            }
            return Task.CompletedTask;
        }

        public Task DeleteProductAsync(string productId)
        {
            var product = _products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
                _products.Remove(product);
            return Task.CompletedTask;
        }

        // Order Operations
        public Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return Task.FromResult(_orders.AsEnumerable());
        }

        public Task AddOrderAsync(Order order)
        {
            order.Id = Guid.NewGuid().ToString();
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order> GetOrderByIdAsync(string orderId)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            return Task.FromResult(order);
        }

        public Task UpdateOrderAsync(Order order)
        {
            var existing = _orders.FirstOrDefault(o => o.Id == order.Id);
            if (existing != null)
            {
                _orders.Remove(existing);
                _orders.Add(order);
            }
            return Task.CompletedTask;
        }

        public Task DeleteOrderAsync(string orderId)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
                _orders.Remove(order);
            return Task.CompletedTask;
        }

        // Contract Operations
        public Task<IEnumerable<Contract>> GetContractsAsync()
        {
            return Task.FromResult(_contracts.AsEnumerable());
        }

        public Task AddContractAsync(Contract contract)
        {
            contract.Id = Guid.NewGuid().ToString();
            _contracts.Add(contract);
            return Task.CompletedTask;
        }

        public Task<Contract> GetContractAsync(string contractId)
        {
            var contract = _contracts.FirstOrDefault(c => c.Id == contractId);
            return Task.FromResult(contract);
        }

        public Task DeleteContractAsync(string contractId)
        {
            var contract = _contracts.FirstOrDefault(c => c.Id == contractId);
            if (contract != null)
                _contracts.Remove(contract);
            return Task.CompletedTask;
        }
    }
}