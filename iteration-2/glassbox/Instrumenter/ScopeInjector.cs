using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Glassbox.Instrumenter;

public sealed class ScopeInjector(
    IReadOnlySet<string> targets,
    IReadOnlyDictionary<(int Line, int Ordinal), string> lineTargets,
    Dictionary<string, int> hits,
    List<string> warnings,
    string tag) : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        var owner = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
        var key = KeyFor(node, $"{owner}.{node.Identifier.Text}");
        if (key is null || (visited.Body is null && visited.ExpressionBody is null))
        {
            return visited;
        }

        var isAsync = visited.Modifiers.Any(SyntaxKind.AsyncKeyword);
        var body = Scoped(visited, visited.Body, visited.ExpressionBody?.Expression, visited.SemicolonToken, isAsync, ReturnsNothing(visited.ReturnType, isAsync), key);
        return body is null ? visited : visited.WithExpressionBody(null).WithSemicolonToken(default).WithBody(body);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var visited = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
        var key = KeyFor(node, null);
        if (key is null)
        {
            return visited;
        }

        var body = Scoped(visited, visited.Body, visited.ExpressionBody?.Expression, visited.SemicolonToken, false, true, key);
        return body is null ? visited : visited.WithExpressionBody(null).WithSemicolonToken(default).WithBody(body);
    }

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        var visited = (OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node)!;
        var key = KeyFor(node, null);
        if (key is null)
        {
            return visited;
        }

        var body = Scoped(visited, visited.Body, visited.ExpressionBody?.Expression, visited.SemicolonToken, false, false, key);
        return body is null ? visited : visited.WithExpressionBody(null).WithSemicolonToken(default).WithBody(body);
    }

    public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
        var visited = (AccessorDeclarationSyntax)base.VisitAccessorDeclaration(node)!;
        var key = KeyFor(node, null);
        if (key is null || (visited.Body is null && visited.ExpressionBody is null))
        {
            return visited;
        }

        var returnsNothing = !visited.Keyword.IsKind(SyntaxKind.GetKeyword);
        var body = Scoped(visited, visited.Body, visited.ExpressionBody?.Expression, visited.SemicolonToken, false, returnsNothing, key);
        return body is null ? visited : visited.WithExpressionBody(null).WithSemicolonToken(default).WithBody(body);
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var visited = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
        if (node.ExpressionBody is null)
        {
            return visited;
        }

        var key = KeyFor(node.ExpressionBody, null);
        var body = key is null ? null : Scoped(visited, null, visited.ExpressionBody!.Expression, visited.SemicolonToken, false, false, key);
        return body is null ? visited : visited.WithExpressionBody(null).WithSemicolonToken(default).WithAccessorList(GetterOnly(body));
    }

    public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        var visited = (IndexerDeclarationSyntax)base.VisitIndexerDeclaration(node)!;
        if (node.ExpressionBody is null)
        {
            return visited;
        }

        var key = KeyFor(node.ExpressionBody, null);
        var body = key is null ? null : Scoped(visited, null, visited.ExpressionBody!.Expression, visited.SemicolonToken, false, false, key);
        return body is null ? visited : visited.WithExpressionBody(null).WithSemicolonToken(default).WithAccessorList(GetterOnly(body));
    }

    private static AccessorListSyntax GetterOnly(BlockSyntax body) =>
        AccessorList(
            Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(Space).WithTrailingTrivia(Space),
            SingletonList(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithKeyword(Token(SyntaxKind.GetKeyword).WithTrailingTrivia(Space)).WithBody(body)),
            Token(SyntaxKind.CloseBraceToken).WithTrailingTrivia(ElasticCarriageReturnLineFeed));

    private string? KeyFor(SyntaxNode node, string? legacyKey)
    {
        var (line, ordinal) = DeclarationSites.Locate(node);
        if (lineTargets.TryGetValue((line, ordinal), out var identity))
        {
            return identity;
        }

        return legacyKey is not null && targets.Contains(legacyKey) ? legacyKey : null;
    }

    private BlockSyntax? Scoped(SyntaxNode visited, BlockSyntax? existing, ExpressionSyntax? expression, SyntaxToken semicolon, bool isAsync, bool returnsNothing, string key)
    {
        if (visited.DescendantNodes().OfType<YieldStatementSyntax>().Any())
        {
            warnings.Add($"warning: {key} in {tag} is an iterator; a scope would span the whole enumeration, so it is not instrumented");
            return null;
        }

        var escaped = key.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        var statement = ParseStatement(isAsync
                ? $"await using var __et = global::EmberTrace.Tracer.ScopeAsync(global::EmberTrace.Tracer.Id(\"{escaped}\"));"
                : $"using var __et = global::EmberTrace.Tracer.Scope(global::EmberTrace.Tracer.Id(\"{escaped}\"));")
            .WithTrailingTrivia(ElasticCarriageReturnLineFeed);
        var body = existing ?? ToBlock(expression!, returnsNothing, semicolon);
        hits[key] = hits.GetValueOrDefault(key) + 1;
        return body.WithStatements(body.Statements.Insert(0, statement));
    }

    private static BlockSyntax ToBlock(ExpressionSyntax expression, bool returnsNothing, SyntaxToken semicolon)
    {
        var leading = expression.GetLeadingTrivia();
        StatementSyntax statement = expression switch
        {
            ThrowExpressionSyntax thrown => ThrowStatement(
                Token(SyntaxKind.ThrowKeyword).WithLeadingTrivia(leading).WithTrailingTrivia(Space),
                thrown.Expression.WithLeadingTrivia(TriviaList()),
                Token(SyntaxKind.SemicolonToken)),
            _ when returnsNothing => ExpressionStatement(expression),
            _ => ReturnStatement(
                Token(SyntaxKind.ReturnKeyword).WithLeadingTrivia(leading).WithTrailingTrivia(Space),
                expression.WithLeadingTrivia(TriviaList()),
                Token(SyntaxKind.SemicolonToken)),
        };
        var closing = Token(SyntaxKind.CloseBraceToken).WithTrailingTrivia(semicolon.TrailingTrivia);
        return Block(Token(SyntaxKind.OpenBraceToken), SingletonList(statement.WithTrailingTrivia(ElasticCarriageReturnLineFeed)), closing);
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
