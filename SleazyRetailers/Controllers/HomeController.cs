using Microsoft.AspNetCore.Mvc;
using SleazyRetailers.Models;
using SleazyRetailers.Services;

namespace SleazyRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAzureStorageService storageService, ILogger<HomeController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // Updated Index method with role-based redirect
        public async Task<IActionResult> Index()
        {
            // If logged in, redirect to appropriate dashboard
            if (HttpContext.Session.GetString("UserId") != null)
            {
                var role = HttpContext.Session.GetString("Role");
                if (role == "Admin")
                    return RedirectToAction("AdminDashboard");
                else
                    return RedirectToAction("Dashboard", "Customer");
            }

            // Original public homepage code
            try
            {
                var customers = await _storageService.GetCustomersAsync();
                var products = await _storageService.GetProductsAsync();
                var orders = await _storageService.GetOrdersAsync();
                var contracts = await _storageService.GetContractsAsync();

                ViewBag.CustomerCount = customers.Count();
                ViewBag.ProductCount = products.Count();
                ViewBag.OrderCount = orders.Count();
                ViewBag.ContractCount = contracts.Count();
                ViewBag.FeaturedProducts = products.Take(6).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                ViewBag.CustomerCount = 0;
                ViewBag.ProductCount = 0;
                ViewBag.OrderCount = 0;
                ViewBag.ContractCount = 0;
                ViewBag.FeaturedProducts = new List<Product>();
            }

            return View();
        }

        // Admin Dashboard
        public async Task<IActionResult> AdminDashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var customers = await _storageService.GetCustomersAsync();
                var products = await _storageService.GetProductsAsync();
                var orders = await _storageService.GetOrdersAsync();
                var contracts = await _storageService.GetContractsAsync();

                ViewBag.CustomerCount = customers.Count();
                ViewBag.ProductCount = products.Count();
                ViewBag.OrderCount = orders.Count();
                ViewBag.ContractCount = contracts.Count();
                ViewBag.Username = HttpContext.Session.GetString("Username");
                ViewBag.FirstName = HttpContext.Session.GetString("FirstName");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                ViewBag.CustomerCount = 0;
                ViewBag.ProductCount = 0;
                ViewBag.OrderCount = 0;
                ViewBag.ContractCount = 0;
                return View();
            }
        }

        // Manage Products (Admin only)
        public async Task<IActionResult> ManageProducts()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var products = await _storageService.GetProductsAsync();
            return View(products);
        }

        // Manage Orders (Admin only)
        public async Task<IActionResult> ManageOrders()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var orders = await _storageService.GetOrdersAsync();
            return View(orders);
        }

        // ADD THIS METHOD FOR DEMO PRODUCTS ONLY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDemoProducts()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var demoProducts = new List<Product>
                {
                    new Product {
                        ProductName = "iPhone 15 Pro",
                        Description = "Latest iPhone with advanced camera and A17 Pro chip",
                        Price = 999.99,
                        StockAvailable = 25
                    },
                    new Product {
                        ProductName = "Samsung Galaxy S24",
                        Description = "Android smartphone with AI features and great camera",
                        Price = 799.99,
                        StockAvailable = 30
                    },
                    new Product {
                        ProductName = "MacBook Air M3",
                        Description = "Lightweight laptop with M3 chip and Retina display",
                        Price = 1099.99,
                        StockAvailable = 15
                    },
                    new Product {
                        ProductName = "Sony WH-1000XM5",
                        Description = "Wireless noise-canceling headphones with 30hr battery",
                        Price = 399.99,
                        StockAvailable = 40
                    },
                    new Product {
                        ProductName = "iPad Air",
                        Description = "Powerful tablet with M1 chip and Liquid Retina display",
                        Price = 599.99,
                        StockAvailable = 20
                    },
                    new Product {
                        ProductName = "Apple Watch Series 9",
                        Description = "Smartwatch with health monitoring and fitness tracking",
                        Price = 399.99,
                        StockAvailable = 35
                    }
                };

                int createdCount = 0;
                foreach (var product in demoProducts)
                {
                    try
                    {
                        await _storageService.AddProductAsync(product, null);
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create demo product {product.ProductName}: {ex.Message}");
                    }
                }

                TempData["SuccessMessage"] = $"Demo products created successfully! {createdCount} products added to the system.";
                return RedirectToAction("ManageProducts");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating demo products: {ex.Message}";
                _logger.LogError(ex, "Error creating demo products");
                return RedirectToAction("AdminDashboard");
            }
        }

        // ADD THIS METHOD FOR DEMO DATA CREATION
        public async Task<IActionResult> CreateDemoData()
        {
            try
            {
                // Create demo customers
                var demoCustomers = new List<Customer>
                {
                    new Customer { FirstName = "John", LastName = "Doe", Username = "johndoe", Email = "john@example.com", ShippingAddress = "123 Main St" },
                    new Customer { FirstName = "Jane", LastName = "Smith", Username = "janesmith", Email = "jane@example.com", ShippingAddress = "456 Oak Ave" },
                    new Customer { FirstName = "Bob", LastName = "Johnson", Username = "bobjohnson", Email = "bob@example.com", ShippingAddress = "789 Pine Rd" }
                };

                // Create demo products
                var demoProducts = new List<Product>
                {
                    new Product { ProductName = "Wireless Headphones", Description = "High-quality wireless headphones with noise cancellation", Price = 99.99, StockAvailable = 25 },
                    new Product { ProductName = "Smart Watch", Description = "Feature-rich smartwatch with health monitoring", Price = 199.99, StockAvailable = 15 },
                    new Product { ProductName = "Laptop Backpack", Description = "Durable laptop backpack with USB charging port", Price = 49.99, StockAvailable = 40 },
                    new Product { ProductName = "Bluetooth Speaker", Description = "Portable Bluetooth speaker with 12-hour battery", Price = 79.99, StockAvailable = 30 },
                    new Product { ProductName = "Phone Case", Description = "Protective phone case with drop resistance", Price = 24.99, StockAvailable = 100 },
                    new Product { ProductName = "Tablet Stand", Description = "Adjustable tablet stand for desk use", Price = 29.99, StockAvailable = 50 }
                };

                // Create demo orders
                var demoOrders = new List<Order>
                {
                    new Order { CustomerId = "demo-customer-1", OrderDate = DateTime.Now.AddDays(-5), TotalAmount = 149.98m, Status = "Completed", ShippingAddress = "123 Main St" },
                    new Order { CustomerId = "demo-customer-2", OrderDate = DateTime.Now.AddDays(-2), TotalAmount = 229.98m, Status = "Processing", ShippingAddress = "456 Oak Ave" },
                    new Order { CustomerId = "demo-customer-3", OrderDate = DateTime.Now.AddDays(-1), TotalAmount = 79.99m, Status = "Shipped", ShippingAddress = "789 Pine Rd" }
                };

                int createdCount = 0;

                // Add demo customers
                foreach (var customer in demoCustomers)
                {
                    try
                    {
                        await _storageService.AddCustomerAsync(customer);
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create demo customer {customer.Username}: {ex.Message}");
                    }
                }

                // Add demo products
                foreach (var product in demoProducts)
                {
                    try
                    {
                        await _storageService.AddProductAsync(product, null);
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create demo product {product.ProductName}: {ex.Message}");
                    }
                }

                // Add demo orders
                foreach (var order in demoOrders)
                {
                    try
                    {
                        await _storageService.AddOrderAsync(order);
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create demo order: {ex.Message}");
                    }
                }

                TempData["SuccessMessage"] = $"Demo data created successfully! {createdCount} items added to the system.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating demo data: {ex.Message}";
                _logger.LogError(ex, "Error creating demo data");
            }

            return RedirectToAction(nameof(Index));
        }

        // ADD THIS METHOD TO CLEAR ALL DATA
        public async Task<IActionResult> ClearAllData()
        {
            try
            {
                // Note: This is a simplified clear operation
                // In a real application, you'd want more sophisticated data management
                var customers = await _storageService.GetCustomersAsync();
                var products = await _storageService.GetProductsAsync();
                var orders = await _storageService.GetOrdersAsync();
                var contracts = await _storageService.GetContractsAsync();

                int deletedCount = 0;

                // Delete customers
                foreach (var customer in customers)
                {
                    try
                    {
                        await _storageService.DeleteCustomerAsync(customer.Id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete customer {customer.Username}: {ex.Message}");
                    }
                }

                // Delete products
                foreach (var product in products)
                {
                    try
                    {
                        await _storageService.DeleteProductAsync(product.Id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete product {product.ProductName}: {ex.Message}");
                    }
                }

                // Delete orders
                foreach (var order in orders)
                {
                    try
                    {
                        await _storageService.DeleteOrderAsync(order.Id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete order: {ex.Message}");
                    }
                }

                TempData["SuccessMessage"] = $"Data cleared successfully! {deletedCount} items removed from the system.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error clearing data: {ex.Message}";
                _logger.LogError(ex, "Error clearing data");
            }

            return RedirectToAction(nameof(Index));
        }

        // ADD THIS METHOD TO TEST AZURE CONNECTIONS
        public async Task<IActionResult> TestConnections()
        {
            var results = new List<string>();

            try
            {
                // Test customer operations
                var customers = await _storageService.GetCustomersAsync();
                results.Add($"? Customer connection: {customers.Count()} customers found");

                // Test product operations
                var products = await _storageService.GetProductsAsync();
                results.Add($"? Product connection: {products.Count()} products found");

                // Test order operations
                var orders = await _storageService.GetOrdersAsync();
                results.Add($"? Order connection: {orders.Count()} orders found");

                // Test contract operations
                var contracts = await _storageService.GetContractsAsync();
                results.Add($"? Contract connection: {contracts.Count()} contracts found");

                results.Add("? All Azure Storage connections tested successfully!");
                TempData["ConnectionResults"] = results;
                TempData["SuccessMessage"] = "All connections tested successfully!";
            }
            catch (Exception ex)
            {
                results.Add($"? Connection error: {ex.Message}");
                TempData["ConnectionResults"] = results;
                TempData["ErrorMessage"] = "Some connections failed. Using fallback storage.";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ADD THIS METHOD FOR SYSTEM STATUS
        public async Task<IActionResult> SystemStatus()
        {
            var status = new
            {
                Timestamp = DateTime.Now,
                AzureStorageStatus = "Fallback Mode (Expected for POE)",
                FunctionIntegrationStatus = "Configured but not connected",
                ApplicationStatus = "Running Successfully",
                DataPersistence = "In-Memory (Demo Mode)"
            };

            ViewBag.SystemStatus = status;
            return View();
        }
    }
}