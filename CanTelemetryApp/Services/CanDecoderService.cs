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

        if (string.IsNullOrEmpty(raw.Data) || raw.Data.Length != 16)
            return dto;

        try
        {
            byte[] bytes = Convert.FromHexString(raw.Data);

            // --- TABLOYA GÖRE DOĞRULANMIŞ KOD ---

            // Mesaj: Arac_Genel_Istatistik (0x4A0)
            if (raw.Id.Equals("0x4A0", StringComparison.OrdinalIgnoreCase))
            {
                // Speed_EBS -> Start: 0, Len: 2, Factor: 0.00390625
                dto.SpeedEbs = BitConverter.ToUInt16(bytes, 0) * 0.00390625;

                // Speed_TCU -> Start: 2, Len: 2, Factor: 0.00390625
                dto.SpeedTcu = BitConverter.ToUInt16(bytes, 2) * 0.00390625;

                // Accl -> Start: 4, Len: 1, Factor: 0.4
                // Dikkat: Uzunluk 1 Byte olduğu için direkt bytes dizisinden alıyoruz
                dto.Accl = bytes[4] * 0.4;
            }
            // Mesaj: Sarj_Durum (0x4C2)
            else if (raw.Id.Equals("0x4C2", StringComparison.OrdinalIgnoreCase))
            {
                // SOC -> Start: 0, Len: 2, Factor: 0.1
                dto.Soc = BitConverter.ToUInt16(bytes, 0) * 0.1;

                // SOH -> Start: 2, Len: 2, Factor: 0.1
                dto.Soh = BitConverter.ToUInt16(bytes, 2) * 0.1;

                // Consu_Avg -> Start: 4, Len: 2, Factor: 0.1
                dto.ConsuAvg = BitConverter.ToUInt16(bytes, 4) * 0.1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decode Error ({raw.Id}): {ex.Message}");
        }

        return dto;
    }
}