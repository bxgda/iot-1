using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rest_service.Data;
using rest_service.DTOs;
using rest_service.Models;

namespace rest_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReadingsController(AppDbContext context)
    {
        _context = context;
    }

    // POST /api/readings — Unos jednog ocitavanja (Scenario A)
    [HttpPost]
    public async Task<ActionResult<SensorReading>> CreateReading([FromBody] ReadingCreateDto dto)
    {
        var reading = new SensorReading
        {
            Ts = DateTimeOffset.FromUnixTimeMilliseconds((long)(dto.Ts * 1000)).UtcDateTime,
            DeviceId = dto.DeviceId,
            Co = dto.Co,
            Humidity = dto.Humidity,
            Light = dto.Light,
            Lpg = dto.Lpg,
            Motion = dto.Motion,
            Smoke = dto.Smoke,
            Temp = dto.Temp
        };

        _context.SensorReadings.Add(reading);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReadings), new { deviceId = reading.DeviceId }, reading);
    }

    // POST /api/readings/batch — Unos batch ocitavanja (Scenario A)
    [HttpPost("batch")]
    public async Task<ActionResult> CreateBatch([FromBody] BatchCreateDto dto)
    {
        var readings = dto.Readings.Select(r => new SensorReading
        {
            Ts = DateTimeOffset.FromUnixTimeMilliseconds((long)(r.Ts * 1000)).UtcDateTime,
            DeviceId = r.DeviceId,
            Co = r.Co,
            Humidity = r.Humidity,
            Light = r.Light,
            Lpg = r.Lpg,
            Motion = r.Motion,
            Smoke = r.Smoke,
            Temp = r.Temp
        }).ToList();

        _context.SensorReadings.AddRange(readings);
        await _context.SaveChangesAsync();

        return Ok(new { count = readings.Count });
    }

    // GET /api/readings — Citanje po device_id i vremenskom opsegu (Scenario B, C)
    [HttpGet]
    public async Task<ActionResult<List<SensorReading>>> GetReadings(
        [FromQuery] string deviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100)
    {
        var query = _context.SensorReadings.AsQueryable();

        query = query.Where(r => r.DeviceId == deviceId);

        if (from.HasValue)
            query = query.Where(r => r.Ts >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.Ts <= to.Value);

        var readings = await query
            .OrderBy(r => r.Ts)
            .Take(limit)
            .ToListAsync();

        return Ok(readings);
    }

    // GET /api/readings/select — Selekcija specificnih polja (Scenario B)
    [HttpGet("select")]
    public async Task<ActionResult> GetSelectedFields(
        [FromQuery] string deviceId,
        [FromQuery] string fields,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100)
    {
        if (string.IsNullOrEmpty(fields))
            return BadRequest("Parameter 'fields' is required (e.g., fields=temp,humidity)");

        var requestedFields = fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim().ToLower())
            .ToHashSet();

        var query = _context.SensorReadings
            .Where(r => r.DeviceId == deviceId);

        if (from.HasValue)
            query = query.Where(r => r.Ts >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.Ts <= to.Value);

        var readings = await query
            .OrderBy(r => r.Ts)
            .Take(limit)
            .ToListAsync();

        var result = readings.Select(r =>
        {
            var dict = new Dictionary<string, object?>
            {
                ["ts"] = r.Ts,
                ["deviceId"] = r.DeviceId
            };

            if (requestedFields.Contains("co")) dict["co"] = r.Co;
            if (requestedFields.Contains("humidity")) dict["humidity"] = r.Humidity;
            if (requestedFields.Contains("light")) dict["light"] = r.Light;
            if (requestedFields.Contains("lpg")) dict["lpg"] = r.Lpg;
            if (requestedFields.Contains("motion")) dict["motion"] = r.Motion;
            if (requestedFields.Contains("smoke")) dict["smoke"] = r.Smoke;
            if (requestedFields.Contains("temp")) dict["temp"] = r.Temp;

            return dict;
        });

        return Ok(result);
    }

    // GET /api/readings/aggregate — Agregacije avg/min/max (Scenario C)
    [HttpGet("aggregate")]
    public async Task<ActionResult<AggregationResultDto>> GetAggregation(
        [FromQuery] string deviceId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var query = _context.SensorReadings
            .Where(r => r.DeviceId == deviceId && r.Ts >= from && r.Ts <= to);

        var count = await query.CountAsync();

        if (count == 0)
            return NotFound("No readings found for the specified device and time range.");

        var result = await query.GroupBy(r => 1).Select(g => new AggregationResultDto
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

        return Ok(result);
    }
}
