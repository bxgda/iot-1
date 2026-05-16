using Microsoft.EntityFrameworkCore;
using graphql_service.Data;
using graphql_service.DTOs;
using graphql_service.Models;

namespace graphql_service.GraphQL;

public class Query
{
    /// <summary>
    /// Dohvata ocitavanja za uredjaj u vremenskom opsegu.
    /// Klijent bira koja polja zeli u GraphQL upitu — to je sustina GraphQL-a.
    /// </summary>
    public async Task<List<SensorReading>> GetReadings(
        [Service] IDbContextFactory<AppDbContext> factory,
        string deviceId,
        DateTime from,
        DateTime to,
        int limit = 100)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.SensorReadings
            .Where(r => r.DeviceId == deviceId && r.Ts >= from && r.Ts <= to)
            .OrderBy(r => r.Ts)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Agregacije (avg, min, max) za sve numericke senzorske kolone.
    /// Koristi se za Scenario C — teski upiti nad velikim opsegom podataka.
    /// </summary>
    public async Task<AggregationResult> GetAggregation(
        [Service] IDbContextFactory<AppDbContext> factory,
        string deviceId,
        DateTime from,
        DateTime to)
    {
        await using var context = await factory.CreateDbContextAsync();

        var query = context.SensorReadings
            .Where(r => r.DeviceId == deviceId && r.Ts >= from && r.Ts <= to);

        var count = await query.CountAsync();

        if (count == 0)
            throw new GraphQLException("No readings found for the specified device and time range.");

        return await query.GroupBy(r => 1).Select(g => new AggregationResult
        {
            DeviceId = deviceId,
            From = from,
            To = to,
            Count = g.Count(),

            AvgCo = g.Average(r => r.Co),
            MinCo = g.Min(r => r.Co),
            MaxCo = g.Max(r => r.Co),

            AvgHumidity = g.Average(r => r.Humidity),
            MinHumidity = g.Min(r => r.Humidity),
            MaxHumidity = g.Max(r => r.Humidity),

            AvgLpg = g.Average(r => r.Lpg),
            MinLpg = g.Min(r => r.Lpg),
            MaxLpg = g.Max(r => r.Lpg),

            AvgSmoke = g.Average(r => r.Smoke),
            MinSmoke = g.Min(r => r.Smoke),
            MaxSmoke = g.Max(r => r.Smoke),

            AvgTemp = g.Average(r => r.Temp),
            MinTemp = g.Min(r => r.Temp),
            MaxTemp = g.Max(r => r.Temp)
        }).FirstAsync();
    }
}
