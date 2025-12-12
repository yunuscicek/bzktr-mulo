using System.Text;
using System.Text.Json;
using CanTelemetryApp.Hubs;
using CanTelemetryApp.Models;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CanTelemetryApp.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly CanDecoderService _decoder;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMqConsumer(IHubContext<TelemetryHub> hubContext, CanDecoderService decoder)
    {
        _hubContext = hubContext;
        _decoder = decoder;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        
        // Basit Retry mekanizması
        while (_connection == null && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Kuyruğun var olduğundan emin ol (Publisher zaten oluşturdu ama garanti olsun)
                _channel.QueueDeclare(queue: "can.telemetry", durable: false, exclusive: false, autoDelete: false, arguments: null);
                
                Console.WriteLine("RabbitMQ Bağlantısı Başarılı! Mesajlar bekleniyor...");
            }
            catch
            {
                Console.WriteLine("RabbitMQ'ya bağlanılamadı, 5sn sonra tekrar denenecek...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            try
            {
                // 1. JSON Deserialize
                var rawMsg = JsonSerializer.Deserialize<RawCanMessage>(messageJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (rawMsg != null)
                {
                    // 2. Decode (Sadece hedef ID'ler için)
                    if (rawMsg.Id == "0x4A0" || rawMsg.Id == "0x4C2") 
                    {
                        var stats = _decoder.Decode(rawMsg);
                        
                        // 3. SignalR ile UI'a gönder
                        await _hubContext.Clients.All.SendAsync("TelemetryUpdated", stats);
                        
                        // Debug log (Opsiyonel)
                        // Console.WriteLine($"Broadcast: {rawMsg.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        };

        _channel.BasicConsume(queue: "can.telemetry", autoAck: true, consumer: consumer);

        // Servisi ayakta tut
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}