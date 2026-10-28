using System.Text;
using System.Xml.Linq;

namespace Glassbox.Instrumenter;

public static class ResourceGenerator
{
    public static string Generate(FileInfo resx, string logicalName)
    {
        var names = XDocument.Load(resx.FullName).Root!.Elements("data")
            .Select(d => (string?)d.Attribute("name"))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        var text = new StringBuilder();
        text.AppendLine("namespace System");
        text.AppendLine("{");
        text.AppendLine("    internal static partial class SR");
        text.AppendLine("    {");
        text.AppendLine($"        private static readonly global::System.Resources.ResourceManager s_resourceManager = new global::System.Resources.ResourceManager(\"{logicalName}\", typeof(SR).Assembly);");
        text.AppendLine("        internal static global::System.Resources.ResourceManager ResourceManager => s_resourceManager;");
        text.AppendLine("        internal static bool UsingResourceKeys() => false;");
        text.AppendLine("        internal static string GetResourceString(string resourceKey) => s_resourceManager.GetString(resourceKey) ?? resourceKey;");
        text.AppendLine("        internal static string GetResourceString(string resourceKey, string defaultString) => s_resourceManager.GetString(resourceKey) ?? defaultString;");
        text.AppendLine("        internal static string Format(string resourceFormat, object? p1) => string.Format(resourceFormat, p1);");
        text.AppendLine("        internal static string Format(string resourceFormat, object? p1, object? p2) => string.Format(resourceFormat, p1, p2);");
        text.AppendLine("        internal static string Format(string resourceFormat, object? p1, object? p2, object? p3) => string.Format(resourceFormat, p1, p2, p3);");
        text.AppendLine("        internal static string Format(string resourceFormat, params object?[]? args) => args is null ? resourceFormat : string.Format(resourceFormat, args);");
        text.AppendLine("        internal static string Format(global::System.IFormatProvider? provider, string resourceFormat, object? p1) => string.Format(provider, resourceFormat, p1);");
        text.AppendLine("        internal static string Format(global::System.IFormatProvider? provider, string resourceFormat, object? p1, object? p2) => string.Format(provider, resourceFormat, p1, p2);");
        text.AppendLine("        internal static string Format(global::System.IFormatProvider? provider, string resourceFormat, object? p1, object? p2, object? p3) => string.Format(provider, resourceFormat, p1, p2, p3);");
        text.AppendLine("        internal static string Format(global::System.IFormatProvider? provider, string resourceFormat, params object?[]? args) => args is null ? resourceFormat : string.Format(provider, resourceFormat, args);");
        foreach (var name in names)
        {
            text.AppendLine($"        internal static string @{name} => GetResourceString(\"{name}\");");
        }

        text.AppendLine("    }");
        text.AppendLine("}");
        return text.ToString();
    }
}
