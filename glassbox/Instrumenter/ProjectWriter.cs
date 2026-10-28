using System.Text;

namespace Glassbox.Instrumenter;

public static class ProjectWriter
{
    private static readonly Dictionary<string, string[]> LibraryReferences = new(StringComparer.Ordinal)
    {
        ["Microsoft.Extensions.DependencyInjection"] = ["Microsoft.Extensions.DependencyInjection.Abstractions"],
        ["Microsoft.Extensions.Caching.Memory"] = ["Microsoft.Extensions.Caching.Abstractions"],
    };

    private static readonly Dictionary<string, string[]> PackageReferences = new(StringComparer.Ordinal)
    {
        ["Microsoft.Extensions.Caching.Abstractions"] = ["Microsoft.Extensions.Primitives"],
        ["Microsoft.Extensions.Caching.Memory"] = ["Microsoft.Extensions.Primitives", "Microsoft.Extensions.Options", "Microsoft.Extensions.Logging.Abstractions"],
    };

    public static void Write(DirectoryInfo directory, GlassboxNaming naming, string library, IReadOnlyList<string> libraries, bool hasResources, bool allowUnsafe)
    {
        var assembly = naming.AssemblyName(library);
        var text = new StringBuilder();
        text.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        text.AppendLine("  <PropertyGroup>");
        text.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        text.AppendLine($"    <AssemblyName>{assembly}</AssemblyName>");
        text.AppendLine($"    <RootNamespace>{assembly}</RootNamespace>");
        text.AppendLine("    <EnableDefaultItems>false</EnableDefaultItems>");
        text.AppendLine("    <ImplicitUsings>disable</ImplicitUsings>");
        text.AppendLine("    <Nullable>annotations</Nullable>");
        text.AppendLine("    <LangVersion>preview</LangVersion>");
        text.AppendLine($"    <AllowUnsafeBlocks>{(allowUnsafe ? "true" : "false")}</AllowUnsafeBlocks>");
        text.AppendLine("    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>");
        text.AppendLine("    <WarningLevel>0</WarningLevel>");
        text.AppendLine("    <RunAnalyzers>false</RunAnalyzers>");
        text.AppendLine("    <NoWarn>$(NoWarn);CS0436;CS1591;CS0618;CS8981;CS9113</NoWarn>");
        text.AppendLine("    <DefineConstants>$(DefineConstants);IL_EMIT</DefineConstants>");
        text.AppendLine("  </PropertyGroup>");
        text.AppendLine("  <ItemGroup>");
        text.AppendLine("    <Compile Include=\"src/**/*.cs\" />");
        if (hasResources)
        {
            text.AppendLine($"    <EmbeddedResource Include=\"Strings.resx\" LogicalName=\"{assembly}.Strings.resources\" />");
        }

        text.AppendLine("    <PackageReference Include=\"EmberTrace\" />");
        foreach (var package in PackageReferences.GetValueOrDefault(library, []))
        {
            text.AppendLine($"    <PackageReference Include=\"{package}\" />");
        }

        foreach (var reference in LibraryReferences.GetValueOrDefault(library, []).Where(libraries.Contains))
        {
            var shortName = naming.ShortName(reference);
            text.AppendLine($"    <ProjectReference Include=\"../{shortName}/{naming.AssemblyName(reference)}.csproj\" />");
        }

        text.AppendLine("  </ItemGroup>");
        text.AppendLine("</Project>");
        File.WriteAllText(Path.Combine(directory.FullName, assembly + ".csproj"), text.ToString());
    }
}
