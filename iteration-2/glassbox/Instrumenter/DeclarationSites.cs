using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Glassbox.Instrumenter;

public static class DeclarationSites
{
    private static readonly ConditionalWeakTable<SyntaxTree, Dictionary<TextSpan, (int Line, int Ordinal)>> Cache = [];

    public static (int Line, int Ordinal) Locate(SyntaxNode node) => Sites(node.SyntaxTree)[node.Span];

    private static bool IsSite(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax or AccessorDeclarationSyntax => true,
        ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax or IndexerDeclarationSyntax } => true,
        _ => false,
    };

    private static Dictionary<TextSpan, (int Line, int Ordinal)> Sites(SyntaxTree tree) =>
        Cache.GetValue(tree, static t =>
        {
            var sites = new Dictionary<TextSpan, (int Line, int Ordinal)>();
            var perLine = new Dictionary<int, int>();
            foreach (var node in t.GetRoot().DescendantNodes().Where(IsSite).OrderBy(n => n.SpanStart))
            {
                var line = t.GetLineSpan(node.Span).StartLinePosition.Line + 1;
                var ordinal = perLine.GetValueOrDefault(line);
                perLine[line] = ordinal + 1;
                sites[node.Span] = (line, ordinal);
            }

            return sites;
        });
}
