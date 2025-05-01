using DoanKhoaServer.Hubs;
using DoanKhoaServer.Services;
using DoanKhoaServer.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DoanKhoaServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Đăng ký cấu hình MongoDB
            services.Configure<MongoDBSettings>(
                Configuration.GetSection("MongoDBSettings"));

            // Đăng ký MongoDBService
            services.AddSingleton<MongoDBService>();

            // Đăng ký SignalR
            services.AddSignalR();

            // Cấu hình CORS
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder.WithOrigins("http://localhost:5001") // Cổng WPF client (nếu cần)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });
            services.AddScoped<AuthService>();

            // CORS policy để client có thể gọi API
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            // Cập nhật phương thức Configure
            // Thêm vào trước app.UseEndpoints

            services.AddControllers();
            services.AddSingleton<OtpService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseAuthorization();
            app.UseCors("AllowAll");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/chatHub");
            });
        }
    }
}