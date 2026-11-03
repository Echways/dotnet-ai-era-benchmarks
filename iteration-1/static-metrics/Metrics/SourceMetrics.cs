using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StaticMetrics;

public sealed record MethodMetrics(string File, string Type, string Name, int Line, int Cyclomatic, int CodeLines, int MaxNesting);

public sealed class SourceMetrics
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, preprocessorSymbols: ["NET", "NETCOREAPP", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET7_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER", "NETCOREAPP3_1_OR_GREATER", "IL_EMIT"]);

    public int Files { get; private set; }

    public int CodeLines { get; private set; }

    public int CommentLines { get; private set; }

    public int BlankLines { get; private set; }

    public List<MethodMetrics> Methods { get; } = [];

    public static SourceMetrics Measure(DirectoryInfo directory)
    {
        var metrics = new SourceMetrics();
        foreach (var path in Directory.EnumerateFiles(directory.FullName, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), ParseOptions, path);
            var root = tree.GetRoot();
            var lines = LineKinds.Classify(tree);
            metrics.Files++;
            metrics.CodeLines += lines.Count(k => k == LineKind.Code);
            metrics.CommentLines += lines.Count(k => k == LineKind.Comment);
            metrics.BlankLines += lines.Count(k => k == LineKind.Blank);
            var relative = Path.GetRelativePath(directory.FullName, path);
            foreach (var member in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Cast<SyntaxNode>()
                         .Concat(root.DescendantNodes().OfType<AccessorDeclarationSyntax>())
                         .Concat(root.DescendantNodes().OfType<LocalFunctionStatementSyntax>()))
            {
                var body = BodyOf(member);
                if (body is null)
                {
                    continue;
                }

                var span = tree.GetLineSpan(member.Span);
                var codeLines = Enumerable.Range(span.StartLinePosition.Line, span.EndLinePosition.Line - span.StartLinePosition.Line + 1)
                    .Count(line => line < lines.Count && lines[line] == LineKind.Code);
                metrics.Methods.Add(new MethodMetrics(
                    relative,
                    member.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "",
                    NameOf(member),
                    span.StartLinePosition.Line + 1,
                    Complexity.Cyclomatic(body),
                    codeLines,
                    Complexity.MaxNesting(body)));
            }
        }

        return metrics;
    }

    public JsonObject ToJson(bool includeMethods)
    {
        var complexities = Methods.Select(m => (double)m.Cyclomatic).Order().ToArray();
        var lengths = Methods.Select(m => (double)m.CodeLines).Order().ToArray();
        var nesting = Methods.Select(m => (double)m.MaxNesting).Order().ToArray();
        var node = new JsonObject
        {
            ["files"] = Files,
            ["code_lines"] = CodeLines,
            ["comment_lines"] = CommentLines,
            ["blank_lines"] = BlankLines,
            ["methods"] = Methods.Count,
            ["cc_mean"] = Mean(complexities),
            ["cc_median"] = Percentile(complexities, 0.5),
            ["cc_p90"] = Percentile(complexities, 0.9),
            ["cc_max"] = complexities.LastOrDefault(),
            ["cc_over_10"] = complexities.Count(c => c > 10),
            ["loc_per_method_mean"] = Mean(lengths),
            ["loc_per_method_median"] = Percentile(lengths, 0.5),
            ["loc_per_method_p90"] = Percentile(lengths, 0.9),
            ["nesting_mean"] = Mean(nesting),
            ["nesting_max"] = nesting.LastOrDefault(),
        };
        if (includeMethods)
        {
            node["method_list"] = new JsonArray(Methods.Select(m => (JsonNode)new JsonObject
            {
                ["file"] = m.File,
                ["type"] = m.Type,
                ["name"] = m.Name,
                ["line"] = m.Line,
                ["cc"] = m.Cyclomatic,
                ["loc"] = m.CodeLines,
                ["nesting"] = m.MaxNesting,
            }).ToArray());
        }

        return node;
    }

    private static SyntaxNode? BodyOf(SyntaxNode member) => member switch
    {
        BaseMethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody,
        AccessorDeclarationSyntax accessor => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody,
        LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody,
        _ => null,
    };

    private static string NameOf(SyntaxNode member) => member switch
    {
        MethodDeclarationSyntax method => method.Identifier.Text,
        ConstructorDeclarationSyntax constructor => ".ctor",
        DestructorDeclarationSyntax => "Finalize",
        OperatorDeclarationSyntax op => "op_" + op.OperatorToken.Text,
        ConversionOperatorDeclarationSyntax conversion => "op_" + conversion.ImplicitOrExplicitKeyword.Text,
        AccessorDeclarationSyntax accessor => $"{(accessor.Parent?.Parent as BasePropertyDeclarationSyntax) switch { PropertyDeclarationSyntax p => p.Identifier.Text, EventDeclarationSyntax e => e.Identifier.Text, IndexerDeclarationSyntax => "this[]", _ => "" }}.{accessor.Keyword.Text}",
        LocalFunctionStatementSyntax local => local.Identifier.Text,
        _ => member.Kind().ToString(),
    };

    private static double Mean(double[] values) => values.Length == 0 ? 0 : Math.Round(values.Average(), 3);

    private static double Percentile(double[] sorted, double quantile) =>
        sorted.Length == 0 ? 0 : sorted[(int)Math.Min(sorted.Length - 1, Math.Floor(quantile * (sorted.Length - 1) + 0.5))];
}

public enum LineKind
{
    Blank,
    Comment,
    Code,
}

public static class LineKinds
{
    public static List<LineKind> Classify(SyntaxTree tree)
    {
        var text = tree.GetText();
        var kinds = Enumerable.Repeat(LineKind.Blank, text.Lines.Count).ToList();
        var root = tree.GetRoot();
        foreach (var token in root.DescendantTokens(descendIntoTrivia: false))
        {
            if (token.Span.Length > 0)
            {
                Mark(kinds, text, token.Span, LineKind.Code);
            }

            foreach (var trivia in token.LeadingTrivia.Concat(token.TrailingTrivia))
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    Mark(kinds, text, trivia.Span, LineKind.Comment);
                }
                else if (trivia.IsDirective || trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                {
                    Mark(kinds, text, trivia.Span, LineKind.Code);
                }
            }
        }

        return kinds;
    }

    private static void Mark(List<LineKind> kinds, SourceText text, TextSpan span, LineKind kind)
    {
        var first = text.Lines.GetLineFromPosition(span.Start).LineNumber;
        var last = text.Lines.GetLineFromPosition(Math.Max(span.Start, span.End - 1)).LineNumber;
        for (var line = first; line <= last; line++)
        {
            if (kinds[line] == LineKind.Blank || (kinds[line] == LineKind.Comment && kind == LineKind.Code))
            {
                kinds[line] = kind;
            }
        }
    }
}
