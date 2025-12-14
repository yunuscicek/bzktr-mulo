# final_publisher.py

import pika
import json
import time
import os

# =====================
# Ayarlar
# =====================
RABBIT_HOST = "localhost"
EXCHANGE_NAME = "can.exchange"
ROUTING_KEY = "can.raw"
QUEUE_NAME = "can.telemetry"

CAN_JSON_PATH = "can.json"  # Proje klasöründeki dosya adı

# =====================
def main():
    if not os.path.exists(CAN_JSON_PATH):
        print(f"HATA: {CAN_JSON_PATH} bulunamadı. Lütfen dosya yolunu kontrol edin.")
        return

    # RabbitMQ bağlantısı
    try:
        connection = pika.BlockingConnection(
            pika.ConnectionParameters(host=RABBIT_HOST)
        )
        channel = connection.channel()

        # Exchange ve Queue tanımları (Consumer ile birebir aynı)
        channel.exchange_declare(
            exchange=EXCHANGE_NAME,
            exchange_type="direct",
            durable=False
        )

        channel.queue_declare(queue=QUEUE_NAME, durable=False)
        channel.queue_bind(
            exchange=EXCHANGE_NAME,
            queue=QUEUE_NAME,
            routing_key=ROUTING_KEY
        )
        print(" RabbitMQ Bağlantısı ve Topoloji Hazır")
    
    except Exception as e:
        print(f" RabbitMQ Bağlantı Hatası: {e}")
        return

    print(" CAN JSON Replay Başlıyor...")

    # =====================
    # JSON Yükleme
    # =====================
    with open(CAN_JSON_PATH, "r", encoding="utf-8") as f:
        try:
            data = json.load(f)  # Array formatı [...]
        except json.JSONDecodeError:
            f.seek(0)
            # Line-delimited JSON desteği (her satır ayrı JSON)
            data = [json.loads(line) for line in f if line.strip()]

    print(f" Toplam {len(data)} CAN frame yüklendi.")

    last_ts = None

    # =====================
    # Replay Döngüsü
    # =====================
    for frame in data:
        try:
            # 1. Validasyon: Data alanı var mı ve uzunluğu 16 mı? (Case Gereksinimi)
            can_data = frame.get('data', '')
            
            if len(can_data) != 16:
                print(f" GEÇERSİZ DATA: Frame ID {frame.get('id')} data uzunluğu {len(can_data)}! (Beklenen: 16). Atlanıyor.")
                continue # Bu kaydı atla, RabbitMQ'ya gönderme

            # 2. Zamanlama: Gerçek zaman farkını koru
            ts = frame.get("timestamp", time.time())

            if last_ts is not None:
                diff = ts - last_ts
                # Eğer fark mantıklı bir aralıktaysa (örn: 0 ile 1 sn arası) bekle
                if 0 < diff < 5: 
                    time.sleep(diff)

            last_ts = ts

            # 3. Yayınlama
            channel.basic_publish(
                exchange=EXCHANGE_NAME,
                routing_key=ROUTING_KEY,
                body=json.dumps(frame)
            )

            print(f"Sent -> ID: {frame.get('id')} | Data: {can_data}")

        except KeyboardInterrupt:
            print("\n Kullanıcı tarafından durduruldu.")
            break
        except Exception as e:
            print(f" Hata: {e}")

    print(" Replay tamamlandı.")
    connection.close()


if __name__ == "__main__":
    main()