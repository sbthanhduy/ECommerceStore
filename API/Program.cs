using API.Middleware;
using API.SignalR;
using Core.Entities;
using Core.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourse => resourse.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing.AddHttpClientInstrumentation().AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics.AddHttpClientInstrumentation().AddAspNetCoreInstrumentation().AddRuntimeInstrumentation())
    .UseOtlpExporter();

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
});

OpenAiSettings openAiSettings = builder.Configuration.GetRequiredSection("OpenAi").Get<OpenAiSettings>();
IKernelBuilder kernelBuilder = builder.Services.AddKernel();
kernelBuilder.AddOpenAIChatCompletion(openAiSettings.ChatCompletionModelId, new Uri(openAiSettings.Endpoint), openAiSettings.ChatCompletionApiKey);
kernelBuilder.AddRedisJsonVectorStoreRecordCollection<ProductVector>(collectionName: "products",
    builder.Configuration.GetConnectionString("RedisVector"));
kernelBuilder.AddVectorStoreTextSearch<ProductVector>(
    new TextSearchStringMapper(result => ((ProductVector)result).Name));

builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAi"));
builder.Services.AddScoped<ITextEmbeddingGenerationService, TextEmbeddingGenerationService>();

builder.Services.AddDbContext<StoreContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddCors();
builder.Services.AddSingleton<IConnectionMultiplexer>(config => 
{
    var connString = builder.Configuration.GetConnectionString("Redis") 
        ?? throw new Exception("Cannot get redis connection string");
    var configuration = ConfigurationOptions.Parse(connString, true);
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddSingleton<IResponseCacheService, ResponseCacheService>();

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<StoreContext>();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    .WithOrigins("http://localhost:4200","https://localhost:4200"));

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapGroup("api").MapIdentityApi<AppUser>(); // api/login
app.MapHub<NotificationHub>("/hub/notifications");
app.MapFallbackToController("Index", "Fallback");

try
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<StoreContext>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var textEmbeddingGenerationService = services.GetRequiredService<ITextEmbeddingGenerationService>();
    var vectorStoreRecordCollection =
        services.GetRequiredService<IVectorStoreRecordCollection<string, ProductVector>>();
    await context.Database.MigrateAsync();
    await StoreContextSeed.SeedAsync(context, userManager, textEmbeddingGenerationService, vectorStoreRecordCollection);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    throw;
}

app.Run();
