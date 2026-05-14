namespace rest_service.DTOs;

public class ReadingCreateDto
{
    public double Ts { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public double? Co { get; set; }
    public double? Humidity { get; set; }
    public bool? Light { get; set; }
    public double? Lpg { get; set; }
    public bool? Motion { get; set; }
    public double? Smoke { get; set; }
    public double? Temp { get; set; }
}

public class BatchCreateDto
{
    public List<ReadingCreateDto> Readings { get; set; } = new();
}
