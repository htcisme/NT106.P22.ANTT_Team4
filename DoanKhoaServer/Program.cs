using DoanKhoaServer.Hubs;
using DoanKhoaServer.Services;
using DoanKhoaServer.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors("CorsPolicy");

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

app.Run();