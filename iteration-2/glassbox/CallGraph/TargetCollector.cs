using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Glassbox.CallGraph;

public static class TargetCollector
{
    public static IEnumerable<IMethodSymbol> Collect(SyntaxNode body, SemanticModel model)
    {
        foreach (var node in body.DescendantNodesAndSelf())
        {
            foreach (var target in TargetsOf(node, model))
            {
                yield return target;
            }
        }
    }

    private static IEnumerable<IMethodSymbol> TargetsOf(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax or ConstructorInitializerSyntax or BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax or CastExpressionSyntax:
                if (Resolve(model.GetSymbolInfo(node)) is IMethodSymbol method)
                {
                    yield return method;
                }

                break;
            case ElementAccessExpressionSyntax element:
                foreach (var accessor in PropertyAccessors(Resolve(model.GetSymbolInfo(element)), element))
                {
                    yield return accessor;
                }

                break;
            case IdentifierNameSyntax or GenericNameSyntax:
                foreach (var target in NameTargets((SimpleNameSyntax)node, model))
                {
                    yield return target;
                }

                break;
            case ForEachStatementSyntax loop:
                foreach (var target in ForEachTargets(model.GetForEachStatementInfo(loop)))
                {
                    yield return target;
                }

                break;
        }
    }

    private static IEnumerable<IMethodSymbol> NameTargets(SimpleNameSyntax name, SemanticModel model)
    {
        var expression = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? (ExpressionSyntax)access : name;
        if (expression.Parent is InvocationExpressionSyntax invocation && invocation.Expression == expression)
        {
            yield break;
        }

        var symbol = Resolve(model.GetSymbolInfo(name));
        if (symbol is IMethodSymbol methodGroup)
        {
            yield return methodGroup;
        }
        else
        {
            foreach (var accessor in PropertyAccessors(symbol, expression))
            {
                yield return accessor;
            }
        }
    }

    private static IEnumerable<IMethodSymbol> PropertyAccessors(ISymbol? symbol, ExpressionSyntax expression)
    {
        if (symbol is not IPropertySymbol property)
        {
            yield break;
        }

        var access = AccessOf(expression);
        if (access.HasFlag(Access.Read) && property.GetMethod is { } getter)
        {
            yield return getter;
        }

        if (access.HasFlag(Access.Write) && property.SetMethod is { } setter)
        {
            yield return setter;
        }
    }

    private static IEnumerable<IMethodSymbol> ForEachTargets(ForEachStatementInfo info)
    {
        foreach (var method in new[] { info.GetEnumeratorMethod, info.MoveNextMethod, info.DisposeMethod })
        {
            if (method is not null)
            {
                yield return method;
            }
        }

        if (info.CurrentProperty?.GetMethod is { } current)
        {
            yield return current;
        }
    }

    private static ISymbol? Resolve(SymbolInfo info) => info.Symbol ?? (info.CandidateSymbols.Length == 1 ? info.CandidateSymbols[0] : null);

    private static Access AccessOf(ExpressionSyntax expression)
    {
        var parent = expression.Parent;
        return parent switch
        {
            AssignmentExpressionSyntax assignment when assignment.Left == expression => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) ? Access.Write : Access.Read | Access.Write,
            ArgumentSyntax { RefKindKeyword.RawKind: (int)SyntaxKind.RefKeyword } => Access.Read | Access.Write,
            ArgumentSyntax { RefKindKeyword.RawKind: (int)SyntaxKind.OutKeyword } => Access.Write,
            PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.PreIncrementExpression) || unary.IsKind(SyntaxKind.PreDecrementExpression) => Access.Read | Access.Write,
            PostfixUnaryExpressionSyntax => Access.Read | Access.Write,
            _ => Access.Read,
        };
    }

    [Flags]
    private enum Access
    {
        Read = 1,
        Write = 2,
    }
}
