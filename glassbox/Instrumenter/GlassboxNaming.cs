namespace Glassbox.Instrumenter;

public sealed class GlassboxNaming
{
    private const string Prefix = "Microsoft.Extensions.";
    private static readonly string[] RenamedRoots = ["Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.Caching"];

    public GlassboxNaming(string tag)
    {
        Major = int.Parse(tag.TrimStart('v').Split('.')[0], System.Globalization.CultureInfo.InvariantCulture);
        Root = $"Glassbox.V{Major}";
    }

    public int Major { get; }

    public string Root { get; }

    public string ShortName(string library) => library.StartsWith(Prefix, StringComparison.Ordinal) ? library[Prefix.Length..] : library;

    public string AssemblyName(string library) => $"{Root}.{ShortName(library)}";

    public string Rename(string text)
    {
        foreach (var root in RenamedRoots)
        {
            text = text.Replace(root, $"{Root}.{root[Prefix.Length..]}", StringComparison.Ordinal);
        }

        return text;
    }
}
