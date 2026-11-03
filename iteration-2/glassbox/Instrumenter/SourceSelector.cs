using System.Xml.Linq;

namespace Glassbox.Instrumenter;

public sealed record SourceFile(string Path, string RelativePath);

public sealed record SourceSelection(IReadOnlyList<SourceFile> Files, int CommonFiles, FileInfo? Resources, bool AllowUnsafe);

public static class SourceSelector
{
    private static readonly string[] ForeignPlatformMarkers = [".netstandard", ".netfx", ".netframework", ".net462", ".netcoreapp3", ".Browser", ".Typeforwards", ".TypeForwards"];
    private static readonly string[] BridgeFiles = ["MemoryCacheServiceCollectionExtensions.cs"];

    public static SourceSelection Select(DirectoryInfo checkout, string library)
    {
        var librariesRoot = Path.Combine(checkout.FullName, "src", "libraries");
        var sourceRoot = Path.Combine(librariesRoot, library, "src");
        var project = XDocument.Load(Path.Combine(sourceRoot, library + ".csproj"));
        var files = new List<SourceFile>();
        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(sourceRoot, path);
            var name = Path.GetFileName(path);
            if (relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || ForeignPlatformMarkers.Any(m => name.Contains(m, StringComparison.OrdinalIgnoreCase) && !name.Contains(".netcoreapp.", StringComparison.OrdinalIgnoreCase))
                || BridgeFiles.Contains(name, StringComparer.Ordinal))
            {
                continue;
            }

            files.Add(new SourceFile(path, relative));
        }

        var common = 0;
        foreach (var include in project.Descendants().Where(e => e.Name.LocalName == "Compile").Select(e => (Element: e, Include: (string?)e.Attribute("Include"))))
        {
            if (include.Include is null || !include.Include.StartsWith("$(CommonPath)", StringComparison.Ordinal) || !AppliesToNet10(include.Element))
            {
                continue;
            }

            var relative = include.Include["$(CommonPath)".Length..].Replace('\\', Path.DirectorySeparatorChar);
            var path = Path.Combine(librariesRoot, "Common", "src", relative);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"{library} needs {relative}; run glassbox/fetch.sh again", path);
            }

            files.Add(new SourceFile(path, Path.Combine("Common", relative)));
            common++;
        }

        var resx = new FileInfo(Path.Combine(sourceRoot, "Resources", "Strings.resx"));
        var allowUnsafe = project.Descendants().Any(e => e.Name.LocalName == "AllowUnsafeBlocks" && e.Value.Trim() == "true");
        return new SourceSelection(files, common, resx.Exists ? resx : null, allowUnsafe);
    }

    public static bool AppliesToNet10(XElement element)
    {
        for (var current = element; current is not null; current = current.Parent)
        {
            var condition = (string?)current.Attribute("Condition");
            if (condition is not null && !EvaluateForNet10(condition))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateForNet10(string condition)
    {
        var normalized = string.Join(' ', condition.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Contains(" or ", StringComparison.Ordinal))
        {
            return normalized.Split(" or ").Any(EvaluateForNet10);
        }

        if (normalized.Contains(" and ", StringComparison.Ordinal))
        {
            return normalized.Split(" and ").All(EvaluateForNet10);
        }

        if (normalized.Contains("'.NETCoreApp'", StringComparison.Ordinal))
        {
            return normalized.Contains("==", StringComparison.Ordinal);
        }

        if (normalized.Contains("'.NETFramework'", StringComparison.Ordinal) || normalized.Contains("'.NETStandard'", StringComparison.Ordinal))
        {
            return normalized.Contains("!=", StringComparison.Ordinal);
        }

        if (normalized.Contains("IsTargetFrameworkCompatible", StringComparison.Ordinal))
        {
            return !normalized.TrimStart().StartsWith('!');
        }

        if (normalized.Contains("VersionLessThan", StringComparison.Ordinal) && normalized.Contains("TargetFrameworkVersion", StringComparison.Ordinal))
        {
            return false;
        }

        if (normalized.Contains("'netstandard2.0'", StringComparison.Ordinal) || normalized.Contains("'netstandard2.1'", StringComparison.Ordinal))
        {
            return normalized.Contains("!=", StringComparison.Ordinal);
        }

        return true;
    }
}
