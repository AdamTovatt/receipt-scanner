using ReceiptScanner.Services;

namespace ReceiptScanner
{
    public class Program
    {
        public static void Main(string[]args)
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