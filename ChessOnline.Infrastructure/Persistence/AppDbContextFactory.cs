using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using ChessOnline.Infrastructure.Persistence;

namespace ChessOnline.Infrastructure
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Xác định file cấu hình appsettings.json
            // Vì migration thời điểm thiết kế được gọi từ thư mục mà EF Tools chạy,
            // bạn cần dẫn đường đến file Web/appsettings.json nếu dùng Web project làm startup.

            // Lấy đường dẫn hiện tại
            string basePath = Directory.GetCurrentDirectory();

            // Nếu project Infrastructure nằm ở dưới solution, bạn có thể cần lên một cấp để tới Web project
            string projectPath = Path.Combine(basePath, "../ChessOnline.Web");
            string settingsPath = Path.Combine(projectPath, "appsettings.json");

            // Nếu bạn muốn hỗ trợ môi trường dev / production
            var builder = new ConfigurationBuilder()
                .SetBasePath(projectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddEnvironmentVariables();

            IConfiguration config = builder.Build();

            string connectionString = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
