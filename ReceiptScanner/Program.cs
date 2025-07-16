using ReceiptScanner.Services;
using EasyReasy;
using EasyReasy.ByteShelfProvider;
using EasyReasy.EnvironmentVariables;

namespace ReceiptScanner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Validate environment variables
            EnvironmentVariable.ValidateVariableNamesIn(typeof(EnvironmentVariables));

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Break out the creation of the PredefinedResourceProvider for models
            PredefinedResourceProvider modelsProvider = ByteShelfResourceProvider.Create(
                resourceCollectionType: typeof(Resources.Models),
                baseUrl: EnvironmentVariable.GetStringValue(EnvironmentVariables.ByteShelfUrl),
                apiKey: EnvironmentVariable.GetStringValue(EnvironmentVariables.ByteShelfApiKey));

            // Register ResourceManager as a singleton with predefined providers
            builder.Services.AddSingleton(_ => ResourceManager.CreateInstance(modelsProvider));

            // Register our services
            builder.Services.AddSingleton<IModelService, ModelService>();
            builder.Services.AddScoped<IReceiptScannerService, ReceiptScannerService>();

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}