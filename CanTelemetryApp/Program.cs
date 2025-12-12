using CanTelemetryApp.Hubs;
using CanTelemetryApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Servisleri ekle
builder.Services.AddControllers();
builder.Services.AddSignalR(); 
builder.Services.AddSingleton<CanDecoderService>(); 
builder.Services.AddHostedService<RabbitMqConsumer>(); 

// CORS Ayarları (Genişletildi: Artık her yerden erişime izin veriyor, geliştirme için daha rahat)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()  // Tüm kaynaklara izin ver
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// --- KRİTİK EKLEME 1: Statik Dosya Sunumu ---
// Bu satır olmazsa wwwroot klasörü çalışmaz!
app.UseStaticFiles(); 

// Middleware ayarları
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();

// Endpointler
app.MapControllers();
app.MapHub<TelemetryHub>("/telemetryHub");

// --- KRİTİK EKLEME 2: Varsayılan Dosya ---
// Kullanıcı http://localhost:5104 adresine girince direkt dashboard açılsın
app.MapFallbackToFile("dashboard.html");

app.Run();