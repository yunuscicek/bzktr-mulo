namespace CanTelemetryApp.Models;

public class VehicleStatsDto
{
    public string MessageId { get; set; } = default!;
    public double Timestamp { get; set; }
    
    // 0x4C2 (Şarj Durumu) Mesajından Gelecekler
    public double Soc { get; set; }       // State of Charge (Batarya %)
    public double Soh { get; set; }       // State of Health (Sağlık %)
    public double ConsuAvg { get; set; }  // Ortalama Tüketim
    
    // 0x4A0 (Araç Genel) Mesajından Gelecekler
    public double SpeedTcu { get; set; }  // Hız (TCU)
    public double SpeedEbs { get; set; }  // Hız (EBS)
    public double Accl { get; set; }      // İvme (Pedal)
}