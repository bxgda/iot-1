using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace graphql_service.Models;

[Table("sensor_readings")]
public class SensorReading
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("ts")]
    public DateTime Ts { get; set; }

    [Required]
    [Column("device_id")]
    [MaxLength(20)]
    public string DeviceId { get; set; } = string.Empty;

    [Column("co")]
    public double? Co { get; set; }

    [Column("humidity")]
    public double? Humidity { get; set; }

    [Column("light")]
    public bool? Light { get; set; }

    [Column("lpg")]
    public double? Lpg { get; set; }

    [Column("motion")]
    public bool? Motion { get; set; }

    [Column("smoke")]
    public double? Smoke { get; set; }

    [Column("temp")]
    public double? Temp { get; set; }
}
