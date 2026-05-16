using Microsoft.EntityFrameworkCore;
using graphql_service.Data;
using graphql_service.DTOs;
using graphql_service.Models;

namespace graphql_service.GraphQL;

public class Mutation
{
    /// <summary>
    /// Unos jednog senzorskog ocitavanja (Scenario A).
    /// </summary>
    public async Task<SensorReading> IngestReading(
        [Service] IDbContextFactory<AppDbContext> factory,
        ReadingInput input)
    {
        await using var context = await factory.CreateDbContextAsync();

        var reading = new SensorReading
        {
            Ts = DateTimeOffset.FromUnixTimeMilliseconds((long)(input.Ts * 1000)).UtcDateTime,
            DeviceId = input.DeviceId,
            Co = input.Co,
            Humidity = input.Humidity,
            Light = input.Light,
            Lpg = input.Lpg,
            Motion = input.Motion,
            Smoke = input.Smoke,
            Temp = input.Temp
        };

        context.SensorReadings.Add(reading);
        await context.SaveChangesAsync();

        return reading;
    }

    /// <summary>
    /// Batch unos vise senzorskih ocitavanja (Scenario A).
    /// </summary>
    public async Task<IngestResult> IngestBatch(
        [Service] IDbContextFactory<AppDbContext> factory,
        List<ReadingInput> inputs)
    {
        await using var context = await factory.CreateDbContextAsync();

        var readings = inputs.Select(input => new SensorReading
        {
            Ts = DateTimeOffset.FromUnixTimeMilliseconds((long)(input.Ts * 1000)).UtcDateTime,
            DeviceId = input.DeviceId,
            Co = input.Co,
            Humidity = input.Humidity,
            Light = input.Light,
            Lpg = input.Lpg,
            Motion = input.Motion,
            Smoke = input.Smoke,
            Temp = input.Temp
        }).ToList();

        context.SensorReadings.AddRange(readings);
        await context.SaveChangesAsync();

        return new IngestResult { Success = true, Count = readings.Count };
    }
}

public class IngestResult
{
    public bool Success { get; set; }
    public int Count { get; set; }
}
