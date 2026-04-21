using FutureTechStudentApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// --- APPLICATION INSIGHTS (FOR MARKS) ---
// Only add Application Insights if a connection string is actually provided
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// --- 1. AZURE COSMOS DB SETUP ---
var configuration = builder.Configuration;

string account = configuration["CosmosDb:Account"]!;
string key = configuration["CosmosDb:Key"]!;
string databaseName = configuration["CosmosDb:DatabaseName"]!;
string containerName = configuration["CosmosDb:ContainerName"]!;

Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(account, key);
CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);

builder.Services.AddSingleton<ICosmosDbService>(cosmosDbService);

// --- 2. AZURE BLOB STORAGE SETUP ---
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// --- 3. GOOGLE OAUTH 2.0 SETUP ---
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

// --- 4. AUTHENTICATION & AUTHORIZATION ---
app.UseAuthentication();
app.UseAuthorization();

// Set default route to Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();