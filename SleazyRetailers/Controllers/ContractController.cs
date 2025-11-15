using Microsoft.AspNetCore.Mvc;
using SleazyRetailers.Services;
using SleazyRetailers.Models;

namespace SleazyRetailers.Controllers
{
    public class ContractController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ContractController> _logger;

        public ContractController(IAzureStorageService storageService, ILogger<ContractController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: /Contract (Admin views all contracts)
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var contracts = await _storageService.GetContractsAsync();
                ViewBag.ContractTypes = ContractTypes.AllTypes;
                return View(contracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contracts");
                TempData["ErrorMessage"] = "Error loading contracts: " + ex.Message;
                return View(new List<Contract>());
            }
        }

        // GET: /Contract/Upload (Customer uploads contract)
        public IActionResult Upload()
        {
            if (HttpContext.Session.GetString("Role") != "Customer")
                return RedirectToAction("Login", "Account");

            ViewBag.ContractTypes = ContractTypes.AllTypes;
            return View(new Contract());
        }

        // POST: /Contract/Upload (Customer uploads contract)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Contract contract)
        {
            if (HttpContext.Session.GetString("Role") != "Customer")
                return RedirectToAction("Login", "Account");

            ViewBag.ContractTypes = ContractTypes.AllTypes;

            if (!ModelState.IsValid)
            {
                return View(contract);
            }

            if (contract.ContractFile == null || contract.ContractFile.Length == 0)
            {
                ModelState.AddModelError("ContractFile", "Please select a contract file to upload");
                return View(contract);
            }

            try
            {
                await _storageService.UploadContractAsync(contract);
                TempData["SuccessMessage"] = $"Contract '{contract.ContractName}' uploaded successfully!";
                return RedirectToAction("Dashboard", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading contract");
                TempData["ErrorMessage"] = $"Error uploading contract: {ex.Message}";
                return View(contract);
            }
        }

        // GET: /Contract/Details/{fileName} (Both roles)
        public async Task<IActionResult> Details(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var contract = await _storageService.GetContractAsync(fileName);
                ViewBag.ContractTypes = ContractTypes.AllTypes;
                return View(contract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contract details");
                TempData["ErrorMessage"] = $"Error loading contract: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: /Contract/Download/{fileName} (Both roles)
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            try
            {
                var stream = await _storageService.DownloadContractAsync(fileName);
                var contentType = GetContentType(fileName);

                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading contract");
                TempData["ErrorMessage"] = $"Error downloading contract: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: /Contract/Delete/{fileName} (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(fileName))
            {
                TempData["ErrorMessage"] = "File name is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _storageService.DeleteContractAsync(fileName);
                TempData["SuccessMessage"] = $"Contract '{fileName}' deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract");
                TempData["ErrorMessage"] = $"Error deleting contract: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}