using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;
using ReceiptScanner.Providers.Language;
using ReceiptScanner.Providers.Models;
using ReceiptScanner.Services.Ocr;
using ReceiptScanner.Preprocessing;
using ReceiptScanner.Preprocessing.Preprocessors;
using ReceiptScanner.Middleware;
using ReceiptScanner.Services.CornerDetection;
using System.Threading.RateLimiting;

namespace ReceiptScanner
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Validate environment variables
            EnvironmentVariables.ValidateVariableNamesIn(typeof(EnvironmentVariable));

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Add rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    // Get real IP address, handling proxy scenarios
                    string ipAddress = GetClientIpAddress(httpContext);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"{ipAddress}-default",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 200,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                options.RejectionStatusCode = 429; // Too Many Requests
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
                };
            });

            // Break out the creation of the PredefinedResourceProvider for models
            PredefinedResourceProvider modelsProvider = ByteShelfResourceProvider.CreatePredefined(
                resourceCollectionType: typeof(Resources.Models),
                baseUrl: EnvironmentVariables.GetVariable(EnvironmentVariable.ByteShelfUrl),
                apiKey: EnvironmentVariables.GetVariable(EnvironmentVariable.ByteShelfApiKey));

            // Register ResourceManager as a singleton with predefined providers
            ResourceManager resourceManager = await ResourceManager.CreateInstanceAsync(modelsProvider);
            builder.Services.AddSingleton(resourceManager);

            // Register our services with proper constructor parameters
            builder.Services.AddSingleton<IModelProviderService>(serviceProvider =>
                new TesseractModelProviderService(
                    resourceManager: resourceManager,
                    includedModels: new Resource[]
                    {
                        Resources.Models.TesseractEnglishModel,
                        Resources.Models.TesseractSwedishModel,
                        Resources.Models.TesseractOrientationModel
                    }));

            builder.Services.AddSingleton<IlanguageProvider>(serviceProvider =>
                new TesseractLanguageProvider("swe", "osd"));

            builder.Services.AddSingleton<IOcrService>(serviceProvider =>
                new TesseractOcrService(
                    modelProvider: serviceProvider.GetRequiredService<IModelProviderService>(),
                    languageProvider: serviceProvider.GetRequiredService<IlanguageProvider>()));

            // Register corner detection service
            builder.Services.AddSingleton<ICornerDetectionService>(serviceProvider =>
                new HeatmapCornerDetectionService(resourceManager));

            // Register preprocessors with proper dependencies
            builder.Services.AddSingleton<IImagePreprocessor, ThresholdPreprocessor>();
            builder.Services.AddSingleton<IImagePreprocessor, NoOpPreprocessor>();
            builder.Services.AddSingleton<IImagePreprocessor>(serviceProvider =>
                new RotationCorrectionPreprocessor(serviceProvider.GetRequiredService<ICornerDetectionService>(),
                confidenceThreshold: 0.5));

            // Create corner detection crop preprocessor with dependencies
            builder.Services.AddSingleton<IImagePreprocessor>(serviceProvider =>
                new CornerDetectionCropPreprocessor(
                    cornerDetectionService: serviceProvider.GetRequiredService<ICornerDetectionService>(),
                    confidenceThreshold: 0.5));

            builder.Services.AddSingleton<IImagePreprocessor, HorizontalLineDetectionPreprocessor>();

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline
            app.UseHttpsRedirection();
            app.UseRateLimiter();
            app.UseApiKeyAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }

        private static string GetClientIpAddress(HttpContext httpContext)
        {
            // Check for forwarded headers first (X-Forwarded-For, X-Real-IP, etc.)
            string? forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                string firstIp = forwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(firstIp))
                {
                    return firstIp;
                }
            }

            string? realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            string? forwarded = httpContext.Request.Headers["X-Forwarded"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                return forwarded;
            }

            // Fall back to the direct connection IP
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}