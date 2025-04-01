using Pear.Models;
using Pear.Configurations;
using MongoDB.Driver;
using Pear.Repositories;
using Pear.Services;
using Pear.Storage; // Tillagt för bildlagringstjänster

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpContextAccessor for URL generation
builder.Services.AddHttpContextAccessor();

// Läs feature flag-konfiguration för MongoDB
bool useMongoDb = builder.Configuration.GetValue<bool>("FeatureFlags:UseMongoDb");

// Läs feature flag-konfiguration för Azure Storage
bool useAzureStorage = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureStorage");

// Konfigurera MongoDB om det är aktiverat
if (useMongoDb)
{
    // Konfigurera MongoDB-optioner
    builder.Services.Configure<MongoDbOptions>(
        builder.Configuration.GetSection(MongoDbOptions.SectionName));

    // Validera MongoDB-konfiguration
    var mongoDbOptions = builder.Configuration.GetSection(MongoDbOptions.SectionName).Get<MongoDbOptions>();
    if (string.IsNullOrEmpty(mongoDbOptions?.ConnectionString) || 
        string.IsNullOrEmpty(mongoDbOptions?.DatabaseName) || 
        string.IsNullOrEmpty(mongoDbOptions?.SubscribersCollectionName))
    {
        throw new InvalidOperationException("MongoDB configuration is incomplete. Please check your settings.");
    }

    // Konfigurera MongoDB-klient
    builder.Services.AddSingleton<IMongoClient>(serviceProvider => {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
        return new MongoClient(options.ConnectionString);
    });

    // Konfigurera MongoDB-kollektion
    builder.Services.AddSingleton<IMongoCollection<Subscriber>>(serviceProvider => {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
        var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
        var database = mongoClient.GetDatabase(options.DatabaseName);
        return database.GetCollection<Subscriber>(options.SubscribersCollectionName);
    });

    // Registrera MongoDB-repository
    builder.Services.AddSingleton<ISubscriberRepository, MongoDbSubscriberRepository>();

    Console.WriteLine("Using MongoDB repository");
}
else
{
    // Registrera in-memory-repository som fallback
    builder.Services.AddSingleton<ISubscriberRepository, InMemorySubscriberRepository>();

    Console.WriteLine("Using in-memory repository");
}

// Registrera nyhetsbrevstjänst med samma livstid som repository för konsistens
builder.Services.AddSingleton<INewsletterService, NewsletterService>();

// Konfigurera Azure Blob optioner
builder.Services.Configure<AzureBlobOptions>(
    builder.Configuration.GetSection(AzureBlobOptions.SectionName));

// Välj rätt bildlagringstjänst baserat på feature flag
if (useAzureStorage)
{
    // Validera Azure Blob-konfiguration
    var azureBlobOptions = builder.Configuration.GetSection(AzureBlobOptions.SectionName).Get<AzureBlobOptions>();
    if (string.IsNullOrEmpty(azureBlobOptions?.ConnectionString) || 
        string.IsNullOrEmpty(azureBlobOptions?.ContainerName))
    {
        throw new InvalidOperationException("Azure Blob Storage configuration is incomplete. Please check your settings.");
    }
    
    // Registrera Azure Blob Storage bildtjänst för produktion
    builder.Services.AddSingleton<IImageService, AzureBlobImageService>();
    Console.WriteLine("Using Azure Blob Storage for images");
}
else
{
    // Registrera lokal bildtjänst för utveckling
    builder.Services.AddSingleton<IImageService, LocalImageService>();
    Console.WriteLine("Using local storage for images");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();