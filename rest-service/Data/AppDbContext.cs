using Microsoft.EntityFrameworkCore;
using rest_service.Models;

namespace rest_service.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasIndex(e => e.DeviceId).HasDatabaseName("idx_readings_device_id");
            entity.HasIndex(e => e.Ts).HasDatabaseName("idx_readings_ts");
            entity.HasIndex(e => new { e.DeviceId, e.Ts }).HasDatabaseName("idx_readings_device_ts");
        });
    }
}
