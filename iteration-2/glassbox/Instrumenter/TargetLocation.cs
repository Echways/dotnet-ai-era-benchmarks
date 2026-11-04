namespace Glassbox.Instrumenter;

public sealed record TargetLocation(string Id, string Library, string File, int Line, int Ordinal)
{
    public static IReadOnlyList<TargetLocation> Read(FileInfo file) =>
        System.IO.File.ReadAllLines(file.FullName)
            .Skip(1)
            .Where(line => line.Length > 0)
            .Select(line => line.Split('\t'))
            .Select(parts => new TargetLocation(parts[0], parts[1], parts[2].Replace('\\', '/'), int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture), int.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture)))
            .ToArray();
}
