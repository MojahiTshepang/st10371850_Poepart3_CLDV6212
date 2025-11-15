// Controllers/UploadController.cs
using Microsoft.AspNetCore.Mvc;
using SleazyRetailers.Models;
using SleazyRetailers.Services;

public class UploadController : Controller
{
    private readonly IAzureStorageService _storageService;

    public UploadController(IAzureStorageService storageService)
    {
        _storageService = storageService;
    }

    // GET: /Upload (For Proof of Payment)
    public IActionResult Index()
    {
        return View(new Upload());
    }

    // POST: /Upload/SubmitProof
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitProof(Upload uploadModel)
    {
        if (ModelState.IsValid && uploadModel.FileToUpload != null)
        {
            // This single service call handles Blob Upload AND Queue Message
            await _storageService.UploadPaymentProofAndQueueMessageAsync(uploadModel);

            TempData["SuccessMessage"] = "Proof of Payment uploaded and processing message queued successfully!";
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = "Please select a file to upload.";
        return View("Index", uploadModel);
    }
}