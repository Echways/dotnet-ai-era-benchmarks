using System.Xml.Linq;

namespace Glassbox.CallGraph;

public sealed record PackageSource(string Id, string Repo, string SourceDirectory);

public sealed record OperationRow(string Package, EntryOperation Operation);

public static class PackageSet
{
    public static IReadOnlyList<PackageSource> ReadPackages(FileInfo file) =>
        File.ReadAllLines(file.FullName)
            .Skip(1)
            .Where(line => line.Length > 0)
            .Select(line => line.Split('\t'))
            .Select(parts => new PackageSource(parts[0], parts[1], parts[2]))
            .ToArray();

    public static IReadOnlyList<OperationRow> ReadOperations(FileInfo file) =>
        File.ReadAllLines(file.FullName)
            .Skip(1)
            .Where(line => line.Length > 0)
            .Select(line => line.Split('\t'))
            .Select(parts => new OperationRow(
                parts[0],
                new EntryOperation(parts[1], parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))))
            .ToArray();

    public static IReadOnlyList<PackageSource> Closure(DirectoryInfo checkout, IReadOnlyList<PackageSource> all, PackageSource root)
    {
        var byId = all.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var ordered = new List<PackageSource>();
        var queue = new Queue<PackageSource>([root]);
        var seen = new HashSet<string>(StringComparer.Ordinal) { root.Id };
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            ordered.Add(current);
            var project = Path.Combine(checkout.FullName, current.SourceDirectory, current.Id + ".csproj");
            if (!File.Exists(project))
            {
                continue;
            }

            var references = XDocument.Load(project).Descendants()
                .Where(e => e.Name.LocalName == "ProjectReference")
                .Select(e => Path.GetFileNameWithoutExtension(((string?)e.Attribute("Include") ?? string.Empty).Replace('\\', '/')));
            foreach (var name in references)
            {
                if (byId.TryGetValue(name, out var dependency) && seen.Add(name))
                {
                    queue.Enqueue(dependency);
                }
            }
        }

        return ordered;
    }
}
