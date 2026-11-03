using System.Text.Json.Nodes;

namespace Compare;

public sealed record PointInfo(string Label, string Epoch, string Track, int Order);

public sealed class MatrixPoints
{
    public const string TrackBBaseline = "b-net10";

    private readonly Dictionary<string, PointInfo> _points;

    private MatrixPoints(Dictionary<string, PointInfo> points, string trackABaseline)
    {
        _points = points;
        TrackABaseline = trackABaseline;
    }

    public string TrackABaseline { get; }

    public static MatrixPoints Load(FileInfo file)
    {
        var document = JsonNode.Parse(File.ReadAllText(file.FullName))!;
        var points = new Dictionary<string, PointInfo>(StringComparer.Ordinal);
        var order = 0;
        foreach (var point in document["points"]!.AsArray())
        {
            var label = point!["label"]!.GetValue<string>();
            points[label] = new PointInfo(label, point["epoch"]!.GetValue<string>(), "A", order++);
        }

        points["b-net10"] = new PointInfo("b-net10", "E2", "B", order++);
        points["b-net11"] = new PointInfo("b-net11", "E2", "B", order);
        return new MatrixPoints(points, document["baseline"]!.GetValue<string>());
    }

    public PointInfo Resolve(string label) =>
        _points.TryGetValue(label, out var point) ? point : new PointInfo(label, "unknown", label.StartsWith("b-", StringComparison.Ordinal) ? "B" : "A", int.MaxValue);

    public string BaselineFor(string track) => track == "B" ? TrackBBaseline : TrackABaseline;
}
