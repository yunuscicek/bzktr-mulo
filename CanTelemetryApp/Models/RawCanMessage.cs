//RawCanMessage.cs

namespace CanTelemetryApp.Models;

public class RawCanMessage
{
    public string Id { get; set; } = default!;
    public string Data { get; set; } = default!;
    public double Timestamp { get; set; }
}