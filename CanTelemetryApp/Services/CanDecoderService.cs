using CanTelemetryApp.Models;

namespace CanTelemetryApp.Services;

public class CanDecoderService
{
    public VehicleStatsDto Decode(RawCanMessage raw)
    {
        var dto = new VehicleStatsDto
        {
            MessageId = raw.Id,
            Timestamp = raw.Timestamp
        };

        // Veri boşsa veya eksikse boş DTO dön
        if (string.IsNullOrEmpty(raw.Data) || raw.Data.Length < 16)
            return dto;

        try
        {
            // Hex string'i byte dizisine çevir
            var bytes = Convert.FromHexString(raw.Data);

            // MESAJ 1: 0x4C2 - ARAÇ GENEL İSTATİSTİK (REAL Değerler)
            if (raw.Id.Equals("0x4C2", StringComparison.OrdinalIgnoreCase))
            {
                // 1. X_Spd_TCU (Byte 0, Length 8) -> Factor 1.0
                dto.SpeedTcu = bytes[0];

                // 2. X_Spd_EBS (Byte 1, Length 8) -> Factor 1.0
                dto.SpeedEbs = bytes[1];

                // 3. X_SOC (Byte 2, Length 8) -> Factor 1.0
                dto.Soc = bytes[2];

                // 4. X_SOH (Byte 3, Length 8) -> Factor 1.0
                dto.Soh = bytes[3];

                // 5. X_Accl (Byte 4, Length 16) -> Factor 0.001, Offset -32
                // Byte 4 ve 5'i birleştirir (Little Endian varsayımıyla)
                ushort rawAccl = BitConverter.ToUInt16(bytes, 4);
                dto.Accl = (rawAccl * 0.001) - 32;

                // 6. X_Cosu_Avg (Byte 6, Length 16) -> Factor 0.001
                // Byte 6 ve 7'yi birleştirir
                ushort rawConsu = BitConverter.ToUInt16(bytes, 6);
                dto.ConsuAvg = rawConsu * 0.001;
            }

            // MESAJ 2: 0x4A0 - ŞARJ DURUM (BOOL/Bit Değerleri)
            else if (raw.Id.Equals("0x4A0", StringComparison.OrdinalIgnoreCase))
            {
                // Bit işlemleri: (Byte >> BitOffset) & 1

                // S_Chrg_Inlt_Conn (Byte 0, Bit 6)
                dto.IsChargingConnected = ((bytes[0] >> 6) & 1) == 1;

                // S_Vh_Mobil (Byte 0, Bit 4)
                dto.IsVehicleMobil = ((bytes[0] >> 4) & 1) == 1;

                // S_EV_Ready (Byte 1, Bit 2)
                dto.IsEvReady = ((bytes[1] >> 2) & 1) == 1;
            }
        }
        catch (Exception ex)
        {
            // Hata durumunda log basılabilir ama akışı bozmamak için boş DTO döner
            Console.WriteLine($"Decode Hatası ({raw.Id}): {ex.Message}");
        }

        return dto;
    }
}