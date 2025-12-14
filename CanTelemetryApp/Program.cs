using CanTelemetryApp.Hubs;
using CanTelemetryApp.Services;
using CanTelemetryApp.Options;

var builder = WebApplication.CreateBuilder(args);

// SERVİSLER

builder.Services.AddControllers();

// RabbitMQ ayarlarını appsettings.json'dan oku
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq")
);

// SignalR
builder.Services.AddSignalR();

// CAN decoder (singleton)
builder.Services.AddSingleton<CanDecoderService>();

// RabbitMQ Consumer (Background Service)
builder.Services.AddHostedService<RabbitMqConsumer>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// MIDDLEWARE

// Statik dosyalar (wwwroot)
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

// ENDPOINTS

app.MapControllers();

app.MapHub<TelemetryHub>("/telemetryHub");

// Varsayılan dosya
app.MapFallbackToFile("dashboard.html");

app.Run();
