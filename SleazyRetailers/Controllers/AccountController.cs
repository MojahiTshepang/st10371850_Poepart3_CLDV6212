using Microsoft.AspNetCore.Mvc;
using SleazyRetailers.Services;
using SleazyRetailers.Models;

namespace SleazyRetailers.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IAzureStorageService _storageService;

        public AccountController(IAuthService authService, IAzureStorageService storageService)
        {
            _authService = authService;
            _storageService = storageService;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            // If already logged in, redirect to appropriate dashboard
            if (HttpContext.Session.GetString("UserId") != null)
            {
                var role = HttpContext.Session.GetString("Role");
                if (role == "Admin")
                    return RedirectToAction("AdminDashboard", "Home");
                else
                    return RedirectToAction("Dashboard", "Customer");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string role)
        {
            try
            {
                // Try to login using AuthService (SQL Database)
                var user = await _authService.LoginAsync(username, password);

                if (user.Role != role)
                {
                    ModelState.AddModelError("", "Selected role does not match user role");
                    return View();
                }

                // Store user in session
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("FirstName", user.FirstName);
                HttpContext.Session.SetString("Email", user.Email);

                // ALSO create/update in Azure Table Storage for backward compatibility
                try
                {
                    var existingCustomer = await _storageService.GetCustomerByIdAsync(user.Id);
                    if (existingCustomer == null)
                    {
                        // Create customer in Azure Table Storage
                        var customer = new Customer
                        {
                            Id = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Username = user.Username,
                            Email = user.Email,
                            ShippingAddress = "Not specified"
                        };
                        await _storageService.AddCustomerAsync(customer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not sync with Azure Table: {ex.Message}");
                    // Continue anyway - this is not critical
                }

                TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";

                if (user.Role == "Admin")
                    return RedirectToAction("AdminDashboard", "Home");
                else
                    return RedirectToAction("Dashboard", "Customer");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword, string role, string firstName, string lastName)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View();
            }

            try
            {
                // Register in SQL Database (AuthService)
                var user = await _authService.RegisterAsync(username, email, password, role, firstName, lastName);

                // ALSO register in Azure Table Storage for backward compatibility
                try
                {
                    var customer = new Customer
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Username = user.Username,
                        Email = user.Email,
                        ShippingAddress = "Not specified yet"
                    };
                    await _storageService.AddCustomerAsync(customer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create customer in Azure Table: {ex.Message}");
                    // Continue anyway - this is not critical
                }

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }
    }
}