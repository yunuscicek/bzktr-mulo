using System.Text;
using System.Text.Json;
using CanTelemetryApp.Hubs;
using CanTelemetryApp.Models;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using CanTelemetryApp.Options;

namespace CanTelemetryApp.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly CanDecoderService _decoder;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMqConsumer(IOptions<RabbitMqOptions> options, IHubContext<TelemetryHub> hubContext, CanDecoderService decoder)
    {
        _options = options.Value;
        _hubContext = hubContext;
        _decoder = decoder;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password
        };

        // Bağlantı Retry Mekanizması
        while (_connection == null && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // 1 Exchange
                _channel.ExchangeDeclare(
                    exchange: "can.exchange",
                    type: ExchangeType.Direct,
                    durable: false,
                    autoDelete: false
                );

                // 2 Queue
                _channel.QueueDeclare(
                    queue: _options.QueueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // 3 Bind
                _channel.QueueBind(
                    queue: _options.QueueName,
                    exchange: "can.exchange",
                    routingKey: "can.raw"
                );

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
                var rawMsg = JsonSerializer.Deserialize<RawCanMessage>(messageJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (rawMsg != null)
                {
                    // GÜNCELLEME: ID kontrolü büyük/küçük harf duyarsız yapıldı.
                    // Decoder servisi ile uyumlu olması için bu önemlidir.
                    bool isTargetMessage = rawMsg.Id.Equals("0x4A0", StringComparison.OrdinalIgnoreCase) || 
                                           rawMsg.Id.Equals("0x4C2", StringComparison.OrdinalIgnoreCase);

                    if (isTargetMessage)
                    {
                        // Konsola bilgi bas (İsterseniz yorum satırı yapabilirsiniz)
                        Console.WriteLine($"RECEIVED: {rawMsg.Id} {rawMsg.Data}");

                        // 1. Decode işlemi (DTO döner)
                        var stats = _decoder.Decode(rawMsg);
                        
                        // 2. SignalR ile UI'a gönder
                        // Yeni DTO yapımız (bool alanlar dahil) UI'a gidecek.
                        await _hubContext.Clients.All.SendAsync("TelemetryUpdated", stats);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Consumer Hatası: {ex.Message}");
            }
        };

        _channel.BasicConsume(queue: _options.QueueName, autoAck: true, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}