namespace graphql_service.DTOs;

public class AggregationResult
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public long Count { get; set; }

    public double? AvgCo { get; set; }
    public double? MinCo { get; set; }
    public double? MaxCo { get; set; }

    public double? AvgHumidity { get; set; }
    public double? MinHumidity { get; set; }
    public double? MaxHumidity { get; set; }

    public double? AvgLpg { get; set; }
    public double? MinLpg { get; set; }
    public double? MaxLpg { get; set; }

    public double? AvgSmoke { get; set; }
    public double? MinSmoke { get; set; }
    public double? MaxSmoke { get; set; }

    public double? AvgTemp { get; set; }
    public double? MinTemp { get; set; }
    public double? MaxTemp { get; set; }
}
