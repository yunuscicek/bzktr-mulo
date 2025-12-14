CAN Bus Telemetry Dashboard
Bu proje, araçlardan alınan CAN Bus verilerini (Hız, İvme, Batarya Durumu vb.) RabbitMQ üzerinden okuyan, .NET backend servisinde anlamlandıran ve SignalR kullanarak gerçek zamanlı olarak web arayüzünde görselleştiren bir telemetri sistemidir.

Kurulum ve Çalıştırma
Sistemi ayağa kaldırmak için aşağıdaki adımları sırasıyla takip edin.

1. RabbitMQ Kurulumu
Proje dizininde docker-compose.yml dosyası paylaşılmıştır

docker-compose up -d
RabbitMQ Yönetim Paneline erişmek için: http://localhost:15672

2. .NET Backend Uygulamasını Başlatma
Verileri işleyen ve arayüze basan ana uygulamayı çalıştırın. 

# Uygulamayı başlat
CanTelemetryApp klasörü içerisinde terminalde: 
dotnet run

3. Veri Yayımcısını Başlatma
Sisteme test verisi göndermek için Python scriptini kullanıyoruz.

# Örnek kullanım
CanTelemetryProject klasörü içerisinde terminalde:
python final_publisher.py

4. Arayüze Erişim
Uygulamayı başlattıktan sonra Now listening on: sonrasında yazılan linke tıklayarak da arayüze erişebilirsiniz
Dashboard URL: http://localhost:5104

