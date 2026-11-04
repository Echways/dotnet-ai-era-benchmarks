using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Glassbox.Instrumenter;

namespace Glassbox.CallGraph;

public sealed record Declaration(string Id, IMethodSymbol Symbol, SyntaxNode? Node, SemanticModel? Model, SourceUnit? Unit, string Kind)
{
    public static string IdOf(IMethodSymbol symbol) => symbol.OriginalDefinition.GetDocumentationCommentId() ?? symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static IMethodSymbol Normalize(IMethodSymbol symbol) => (symbol.ReducedFrom ?? symbol).OriginalDefinition;

    public static string KindOf(IMethodSymbol symbol) => symbol.MethodKind switch
    {
        MethodKind.Constructor or MethodKind.StaticConstructor => "ctor",
        MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove => "accessor",
        MethodKind.UserDefinedOperator or MethodKind.Conversion => "operator",
        MethodKind.Destructor => "finalizer",
        _ => "method",
    };

    public int EndLine => Node is null ? 0 : Node.SyntaxTree.GetLineSpan(Node.Span).EndLinePosition.Line + 1;

    public (int Line, int Ordinal) Site => Node is null ? (0, 0) : DeclarationSites.Locate(Node);

    public bool Instrumentable => Node is not null && !Node.DescendantNodes().OfType<YieldStatementSyntax>().Any() && Node switch
    {
        BaseMethodDeclarationSyntax method => method.Body is not null || method.ExpressionBody is not null,
        AccessorDeclarationSyntax accessor => accessor.Body is not null || accessor.ExpressionBody is not null,
        null => false,
        _ => true,
    };

    public int Size => Node?.DescendantNodes().Count() ?? 0;
}
