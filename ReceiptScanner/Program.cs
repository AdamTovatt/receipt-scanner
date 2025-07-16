using ReceiptScanner.Services;
using EasyReasy;
using EasyReasy.ByteShelfProvider;

namespace ReceiptScanner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
                typeof(Resources.Models),
                "https://your-byteshelf-url", // TODO: Replace with real config
                "your-api-key"                // TODO: Replace with real config
            );

            // Register ResourceManager as a singleton with predefined providers
            builder.Services.AddSingleton(_ => ResourceManager.CreateInstance(
                modelsProvider
            ));

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