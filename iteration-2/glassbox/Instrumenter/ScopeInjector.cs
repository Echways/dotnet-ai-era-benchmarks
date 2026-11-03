using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Glassbox.Instrumenter;

public sealed class ScopeInjector(IReadOnlySet<string> targets, Dictionary<string, int> hits, List<string> warnings, string tag) : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        var owner = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
        var key = $"{owner}.{node.Identifier.Text}";
        if (owner is null || !targets.Contains(key) || (visited.Body is null && visited.ExpressionBody is null))
        {
            return visited;
        }

        if (visited.DescendantNodes().OfType<YieldStatementSyntax>().Any())
        {
            warnings.Add($"warning: {key} in {tag} is an iterator; a scope would span the whole enumeration, so it is not instrumented");
            return visited;
        }

        var isAsync = visited.Modifiers.Any(SyntaxKind.AsyncKeyword);
        var statement = ParseStatement(isAsync
                ? $"await using var __et = global::EmberTrace.Tracer.ScopeAsync(global::EmberTrace.Tracer.Id(\"{key}\"));"
                : $"using var __et = global::EmberTrace.Tracer.Scope(global::EmberTrace.Tracer.Id(\"{key}\"));")
            .WithTrailingTrivia(ElasticCarriageReturnLineFeed);
        var body = visited.Body ?? ToBlock(visited, isAsync);
        hits[key]++;
        return visited
            .WithExpressionBody(null)
            .WithSemicolonToken(default)
            .WithBody(body.WithStatements(body.Statements.Insert(0, statement)));
    }

    private static BlockSyntax ToBlock(MethodDeclarationSyntax method, bool isAsync)
    {
        var expression = method.ExpressionBody!.Expression;
        StatementSyntax statement = expression switch
        {
            ThrowExpressionSyntax thrown => ThrowStatement(thrown.Expression),
            _ when ReturnsNothing(method.ReturnType, isAsync) => ExpressionStatement(expression),
            _ => ReturnStatement(expression.WithLeadingTrivia(Space)),
        };
        return Block(statement.WithTrailingTrivia(ElasticCarriageReturnLineFeed));
    }

    private static bool ReturnsNothing(TypeSyntax returnType, bool isAsync)
    {
        if (returnType is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            return true;
        }

        var name = returnType switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null,
        };
        return isAsync && name is "Task" or "ValueTask" && returnType is not GenericNameSyntax;
    }
}
