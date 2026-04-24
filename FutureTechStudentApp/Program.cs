using FutureTechStudentApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.GitHub; // <-- Correct Namespace
using Azure.Identity;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Application Insights
var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Services
string account = configuration["CosmosDb:Account"]!;
string key = configuration["CosmosDb:Key"]!;
string databaseName = configuration["CosmosDb:DatabaseName"]!;
string containerName = configuration["CosmosDb:ContainerName"]!;

Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(account, key);
CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);

builder.Services.AddSingleton<ICosmosDbService>(cosmosDbService);
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
})
.AddGoogle(options =>
{
    options.ClientId = configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
})
.AddGitHub(options => // <-- Added this block
{
    options.ClientId = configuration["Authentication:GitHub:ClientId"]!;
    options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"]!;
    options.CallbackPath = "/signin-github";
    options.Scope.Add("user:email");
});

builder.Services.AddControllersWithViews();

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(configuration["BlobStorage:ConnectionString1:blobServiceUri"]!);
    clientBuilder.AddQueueServiceClient(configuration["BlobStorage:ConnectionString1:queueServiceUri"]!);
    clientBuilder.AddTableServiceClient(configuration["BlobStorage:ConnectionString1:tableServiceUri"]!);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();