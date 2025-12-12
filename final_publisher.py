import pika
import json
import time
import math

# Ayarlar
RABBIT_HOST = 'localhost'
EXCHANGE_NAME = 'can.exchange'
ROUTING_KEY = 'can.raw'

def main():
    connection = pika.BlockingConnection(pika.ConnectionParameters(host=RABBIT_HOST))
    channel = connection.channel()
    
    # Kuyruk yapısını garantiye al
    channel.exchange_declare(exchange=EXCHANGE_NAME, exchange_type='direct')
    channel.queue_declare(queue='can.telemetry')
    channel.queue_bind(exchange=EXCHANGE_NAME, queue='can.telemetry', routing_key=ROUTING_KEY)

    print("🚀 FİNAL Simülasyon Başladı! (Durdurmak için Ctrl+C)")
    print("Veriler sinüs dalgası şeklinde, kesintisiz akacak...")

    try:
        counter = 0
        while True:
            current_time = time.time()
            counter += 0.1  # Zaman sayacı
            
            # --- 1. HIZ SİMÜLASYONU (Sinüs Dalgası) ---
            # Hız 0 ile 100 arasında yumuşakça gidip gelsin
            # Math.sin -1 ile 1 arasında değer verir.
            # (sin + 1) -> 0 ile 2 arası. * 50 -> 0 ile 100 arası.
            speed_val = int((math.sin(counter) + 1) * 50)
            
            # Hız (TCU) ve Hız (EBS) aynı olsun
            # Factor: 0.00390625 -> Value / Factor = Raw
            raw_speed = int(speed_val / 0.00390625)
            # 2 Byte Hex'e çevir (Little Endian)
            hex_speed = raw_speed.to_bytes(2, byteorder='little').hex()
            
            # Pedal: Hız arttıkça pedal da artsın
            pedal_val = int(speed_val / 1.2) # Max %80 civarı
            hex_pedal = pedal_val.to_bytes(1, byteorder='little').hex()

            # Data: EBS(2) + TCU(2) + Pedal(1) + Boş(3) = 8 Byte
            data_4a0 = hex_speed + hex_speed + hex_pedal + "000000"
            
            msg1 = { "id": "0x4A0", "data": data_4a0, "timestamp": current_time }
            channel.basic_publish(exchange=EXCHANGE_NAME, routing_key=ROUTING_KEY, body=json.dumps(msg1))

            # --- 2. BATARYA SİMÜLASYONU ---
            # Batarya 100'den 0'a çok yavaşça insin, bitince 100'e dönsün
            # Her döngüde 0.1 azalır
            soc_val = 100 - (int(counter) % 100) 
            
            # SOH (Sağlık) sabit %98 kalsın, arada titresin
            soh_val = 98 + int(math.sin(counter*5)) # 97-99 arası oynar
            
            # Tüketim: Hız yüksekse tüketim artsın
            consu_val = 15 + int(speed_val / 5) # 15-35 kWh arası
            
            # Hex Çevirimleri (Factor: 0.1)
            hex_soc = int(soc_val / 0.1).to_bytes(2, byteorder='little').hex()
            hex_soh = int(soh_val / 0.1).to_bytes(2, byteorder='little').hex()
            hex_consu = int(consu_val / 0.1).to_bytes(2, byteorder='little').hex()
            
            # Data: SOC(2) + SOH(2) + Consu(2) + Boş(2) = 8 Byte
            data_4c2 = hex_soc + hex_soh + hex_consu + "0000"

            msg2 = { "id": "0x4C2", "data": data_4c2, "timestamp": current_time }
            channel.basic_publish(exchange=EXCHANGE_NAME, routing_key=ROUTING_KEY, body=json.dumps(msg2))
            
            # Log
            if int(counter) % 2 == 0: # Konsolu çok doldurmasın diye ara sıra yaz
                print(f"Sent -> Hız: {speed_val} km/h | Şarj: %{soc_val} | Tüketim: {consu_val} kWh")
            
            # Akıcı grafik için 0.2 saniye bekle (Saniyede 5 veri)
            time.sleep(0.2)

    except KeyboardInterrupt:
        print("\nSimülasyon durduruldu.")
        connection.close()
    except Exception as e:
        print(f"HATA: {e}")

if __name__ == '__main__':
    main()