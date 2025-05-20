using DoanKhoaServer.Hubs;
using DoanKhoaServer.Services;
using DoanKhoaServer.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders; // Thêm dòng này
using System.IO; // Thêm dòng này nếu chưa có
var builder = WebApplication.CreateBuilder(args);

// Add MongoDB configuration
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));
builder.Services.AddSingleton<MongoDBService>();

// Đăng ký AuthService
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<OtpService>();
builder.Services.AddSingleton<EmailService>();
// Add controllers and SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Configure CORS (sử dụng một policy duy nhất)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
    Console.WriteLine($"Created Uploads directory at: {uploadsDir}");
}
else
{
    Console.WriteLine($"Uploads directory exists at: {uploadsDir}");
    // Kiểm tra và hiển thị danh sách file
    try
    {
        var files = Directory.GetFiles(uploadsDir);
        Console.WriteLine($"Uploads directory contains {files.Length} files");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error accessing Uploads directory: {ex.Message}");
    }
}
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors("CorsPolicy");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
    RequestPath = "/Uploads",
    OnPrepareResponse = ctx =>
    {
        // Không cache file để luôn tải lại mỗi lần request
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
    }
});
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/chatHub");
});

// Add a simple health check endpoint
app.MapGet("/api/health", () => "Server is running!");

Console.WriteLine($"Server started at: {DateTime.Now}");
Console.WriteLine($"Listening on: {string.Join(", ", builder.WebHost.GetSetting("urls") ?? "http://localhost:5299")}");
// Thêm endpoint này vào trước app.Run();
app.MapGet("/api/check-uploads", () =>
{
    try
    {
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        if (!Directory.Exists(uploadsDir))
        {
            return Results.Json(new
            {
                exists = false,
                message = "Uploads directory does not exist"
            });
        }

        var files = Directory.GetFiles(uploadsDir)
            .Select(f => new
            {
                name = Path.GetFileName(f),
                size = new FileInfo(f).Length,
                lastModified = new FileInfo(f).LastWriteTime
            })
            .ToList();

        return Results.Json(new
        {
            exists = true,
            count = files.Count,
            files = files,
            path = uploadsDir
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            error = ex.Message,
            stackTrace = ex.StackTrace
        });
    }
});
app.Run();