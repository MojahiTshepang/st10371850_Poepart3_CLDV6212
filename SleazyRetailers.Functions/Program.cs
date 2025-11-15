using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddEnvironmentVariables();

// Add Azure Storage Clients with fallback
builder.Services.AddSingleton(provider =>
{
    var connectionString = builder.Configuration["AzureStorageSettings:ConnectionString"]
                          ?? builder.Configuration["AzureWebJobsStorage"]
                          ?? "UseDevelopmentStorage=true";
    return new TableServiceClient(connectionString);
});

builder.Services.AddSingleton(provider =>
{
    var connectionString = builder.Configuration["AzureStorageSettings:ConnectionString"]
                          ?? builder.Configuration["AzureWebJobsStorage"]
                          ?? "UseDevelopmentStorage=true";
    return new BlobServiceClient(connectionString);
});

builder.Services.AddSingleton(provider =>
{
    var connectionString = builder.Configuration["AzureStorageSettings:ConnectionString"]
                          ?? builder.Configuration["AzureWebJobsStorage"]
                          ?? "UseDevelopmentStorage=true";
    return new QueueServiceClient(connectionString);
});

// Add Functions Worker with proper configuration
builder.Services.AddFunctionsWorkerDefaults();

var host = builder.Build();
host.Run();