using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using SleazyRetailers.Models;
using SleazyRetailers.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Entity Framework Core Configuration ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Azure Storage Configuration and Services ---
var storageConnection = builder.Configuration.GetValue<string>("AzureStorageSettings:ConnectionString");

if (!string.IsNullOrEmpty(storageConnection))
{
    // Register Azure Clients
    builder.Services.AddSingleton(new TableServiceClient(storageConnection));
    builder.Services.AddSingleton(new BlobServiceClient(storageConnection));
    builder.Services.AddSingleton(new QueueServiceClient(storageConnection));

    // Register your custom Azure Storage Service
    builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();
}
else
{
    Console.WriteLine("FATAL: Azure Storage Connection String is missing from configuration.");
}

// Add Authentication Service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Session Support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Add Session Middleware
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Change default to Login

app.Run();