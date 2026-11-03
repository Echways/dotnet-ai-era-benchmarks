using System.Text.Json.Serialization;

namespace Harness.TrackB.Scenarios;

public sealed class Forecast
{
    public DateTimeOffset Date { get; set; }

    public int TemperatureCelsius { get; set; }

    public string Summary { get; set; } = "";

    public double Humidity { get; set; }

    public bool IsEstimate { get; set; }

    public Guid StationId { get; set; }

    public string[] Tags { get; set; } = [];

    public List<Reading> Readings { get; set; } = [];

    public Dictionary<string, int> Counters { get; set; } = [];

    public Location Location { get; set; } = new();
}

public sealed class Reading
{
    public int Minute { get; set; }

    public double Value { get; set; }
}

public sealed class Location
{
    public string City { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}

[JsonSerializable(typeof(Forecast))]
[JsonSerializable(typeof(Forecast[]))]
public sealed partial class ForecastContext : JsonSerializerContext;

public static class ForecastData
{
    public static Forecast Create(int seed) => new()
    {
        Date = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(seed),
        TemperatureCelsius = seed % 40,
        Summary = $"summary-{seed % 17}",
        Humidity = seed % 100 / 100.0,
        IsEstimate = seed % 2 == 0,
        StationId = new Guid(seed, 7, 9, 1, 2, 3, 4, 5, 6, 7, 8),
        Tags = ["alpha", "beta", $"t{seed % 5}"],
        Readings = Enumerable.Range(0, 8).Select(i => new Reading { Minute = i, Value = seed * 0.1 + i }).ToList(),
        Counters = new Dictionary<string, int> { ["a"] = seed, ["b"] = seed * 2, ["c"] = 3 },
        Location = new Location { City = "Reykjavik", Latitude = 64.1466, Longitude = -21.9426 },
    };

    public static byte[] CreateMegabyteArray()
    {
        var items = new List<Forecast>();
        var size = 0;
        var probe = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Create(0), ForecastContext.Default.Forecast).Length;
        while (size < 1024 * 1024)
        {
            items.Add(Create(items.Count));
            size += probe + 1;
        }

        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(items.ToArray(), ForecastContext.Default.ForecastArray);
    }
}
