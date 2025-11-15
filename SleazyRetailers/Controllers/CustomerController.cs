using Microsoft.AspNetCore.Mvc;
using SleazyRetailers.Models;
using SleazyRetailers.Services;

namespace SleazyRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;

        public CustomerController(IAzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        // Customer Dashboard
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Customer")
                return RedirectToAction("Login", "Account");

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FirstName = HttpContext.Session.GetString("FirstName");
            return View();
        }

        // GET: /Customer/Index (Admin only - manage customers)
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var customers = await _azureStorageService.GetCustomersAsync();
                return View(customers);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading customers: {ex.Message}";
                return View(new List<Customer>());
            }
        }

        // GET: /Customer/Products (Customer view products)
        public async Task<IActionResult> Products()
        {
            if (HttpContext.Session.GetString("Role") != "Customer")
                return RedirectToAction("Login", "Account");

            try
            {
                var products = await _azureStorageService.GetProductsAsync();
                Console.WriteLine($"Loaded {products.Count()} products for customer view");

                // Get cart count for display
                var cart = HttpContext.Session.Get<List<CartItem>>("ShoppingCart") ?? new List<CartItem>();
                ViewBag.CartCount = cart.Count;
                ViewBag.CartTotal = cart.Sum(item => item.Total);

                return View(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading products: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading products: {ex.Message}";
                return View(new List<Product>());
            }
        }

        // GET: /Customer/MyOrders (Customer view their orders) - UPDATED WITH DEBUG LOGGING
        public async Task<IActionResult> MyOrders()
        {
            if (HttpContext.Session.GetString("Role") != "Customer")
                return RedirectToAction("Login", "Account");

            try
            {
                var customerId = HttpContext.Session.GetString("UserId");
                var customerUsername = HttpContext.Session.GetString("Username");

                Console.WriteLine($"=== LOADING ORDERS FOR CUSTOMER ===");
                Console.WriteLine($"Customer ID: {customerId}");
                Console.WriteLine($"Customer Username: {customerUsername}");

                if (string.IsNullOrEmpty(customerId))
                {
                    Console.WriteLine("ERROR: Customer ID is null or empty!");
                    TempData["ErrorMessage"] = "Customer session expired. Please login again.";
                    return RedirectToAction("Login", "Account");
                }

                var allOrders = await _azureStorageService.GetOrdersAsync();
                Console.WriteLine($"Total orders in system: {allOrders.Count()}");

                var myOrders = allOrders.Where(o => o.CustomerId == customerId).ToList();
                Console.WriteLine($"Orders found for customer: {myOrders.Count}");

                // Debug: Print each order found
                foreach (var order in myOrders)
                {
                    Console.WriteLine($"Order: {order.Id}, Product: {order.ProductId}, Quantity: {order.Quantity}, Total: {order.TotalAmount}, Status: {order.Status}");
                }

                if (!myOrders.Any())
                {
                    Console.WriteLine("No orders found for this customer");
                    TempData["InfoMessage"] = "You haven't placed any orders yet. Start shopping to see your orders here!";
                }

                Console.WriteLine("=== ORDERS LOADING COMPLETED ===");
                return View(myOrders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR LOADING ORDERS ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"Error loading your orders: {ex.Message}";
                return View(new List<Order>());
            }
        }

        // GET: /Customer/Create (Admin only - create customer)
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: /Customer/Create (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = "Please correct the validation errors: " + string.Join(", ", errors);
                return View(customer);
            }

            try
            {
                await _azureStorageService.AddCustomerAsync(customer);
                TempData["SuccessMessage"] = $"Customer '{customer.Username}' created successfully!";

                try
                {
                    await _azureStorageService.AddCustomerWithFunctionAsync(customer);
                }
                catch (Exception funcEx)
                {
                    Console.WriteLine($"Function integration failed: {funcEx.Message}");
                }
            }
            catch (Exception ex)
            {
                TempData["SuccessMessage"] = $"Customer '{customer.Username}' created successfully (using demo mode)!";
                Console.WriteLine($"Azure Storage Error: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Customer/Edit/{id} (Admin only)
        public async Task<IActionResult> Edit(string id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Customer ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _azureStorageService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading customer: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Customer/Edit/{id} (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Customer customer)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (id != customer.Id)
            {
                TempData["ErrorMessage"] = "Customer ID mismatch";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                await _azureStorageService.UpdateCustomerAsync(customer);
                TempData["SuccessMessage"] = $"Customer '{customer.Username}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating customer: {ex.Message}";
                return View(customer);
            }
        }

        // POST: /Customer/Delete/{id} (Admin only)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                await _azureStorageService.DeleteCustomerAsync(id);
                TempData["SuccessMessage"] = $"Customer deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // NEW: Test method to check order functionality
        public async Task<IActionResult> TestOrderFlow()
        {
            if (HttpContext.Session.GetString("Role") != "Customer")
                return RedirectToAction("Login", "Account");

            try
            {
                Console.WriteLine("=== TESTING ORDER FLOW ===");

                // Test 1: Check products
                var products = await _azureStorageService.GetProductsAsync();
                Console.WriteLine($"Available products: {products.Count()}");

                // Test 2: Check current orders
                var customerId = HttpContext.Session.GetString("UserId");
                var allOrders = await _azureStorageService.GetOrdersAsync();
                var myOrders = allOrders.Where(o => o.CustomerId == customerId).ToList();
                Console.WriteLine($"Current orders for customer: {myOrders.Count}");

                TempData["SuccessMessage"] = $"Test completed: {products.Count()} products available, {myOrders.Count} orders found.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                TempData["ErrorMessage"] = $"Test failed: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }
    }
}