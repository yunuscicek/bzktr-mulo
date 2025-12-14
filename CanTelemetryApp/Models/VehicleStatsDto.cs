// Dosya: Models/VehicleStatsDto.cs

namespace CanTelemetryApp.Models;

public class VehicleStatsDto
{
    public string MessageId { get; set; } = default!;
    public double Timestamp { get; set; }
    
    // 0x4C2 - Arac_Genel_Istatistik Mesajı (REAL Tipler)
    public double SpeedTcu { get; set; }
    public double SpeedEbs { get; set; }
    public double Soc { get; set; }
    public double Soh { get; set; }
    public double Accl { get; set; }
    public double ConsuAvg { get; set; }

    // 0x4A0 - Sarj_Durum Mesajı (BOOL Tipler)
    public bool IsChargingConnected { get; set; }
    public bool IsVehicleMobil { get; set; }
    public bool IsEvReady { get; set; }
}