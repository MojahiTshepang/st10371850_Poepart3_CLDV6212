using Microsoft.AspNetCore.Mvc;
using SleazyRetailers.Models;
using SleazyRetailers.Services;

namespace SleazyRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;

        public ProductController(IAzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        // GET: /Product/Index (Admin only - manage products)
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var products = await _azureStorageService.GetProductsAsync();
            return View(products);
        }

        // GET: /Product/Create (Admin only)
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: /Product/Create (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                await _azureStorageService.AddProductAsync(product, imageFile);
                TempData["SuccessMessage"] = $"Product '{product.ProductName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: /Product/Edit/{id} (Admin only)
        public async Task<IActionResult> Edit(string id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (id == null)
            {
                return NotFound();
            }

            var product = await _azureStorageService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: /Product/Edit/{id} (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product product, IFormFile imageFile)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _azureStorageService.UpdateProductAsync(product, imageFile);
                    TempData["SuccessMessage"] = $"Product '{product.ProductName}' updated successfully!";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Unable to save changes. Please try again. Error: " + ex.Message);
                    return View(product);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // POST: /Product/Delete/{id} (Admin only)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            await _azureStorageService.DeleteProductAsync(id);
            TempData["SuccessMessage"] = $"Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}