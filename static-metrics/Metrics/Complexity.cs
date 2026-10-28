using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticMetrics;

public static class Complexity
{
    public static int Cyclomatic(SyntaxNode body)
    {
        var decisions = 0;
        foreach (var node in body.DescendantNodesAndSelf(descendIntoChildren: n => n == body || !IsNestedFunction(n)))
        {
            decisions += node switch
            {
                IfStatementSyntax or WhileStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax
                    or DoStatementSyntax or CatchClauseSyntax or ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax => 1,
                CaseSwitchLabelSyntax or CasePatternSwitchLabelSyntax => 1,
                SwitchExpressionArmSyntax arm when arm.Pattern is not DiscardPatternSyntax => 1,
                BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression)
                    || binary.IsKind(SyntaxKind.CoalesceExpression) => 1,
                AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression) => 1,
                BinaryPatternSyntax => 1,
                _ => 0,
            };
        }

        return decisions + 1;
    }

    public static int MaxNesting(SyntaxNode body) => Depth(body, 0);

    private static int Depth(SyntaxNode node, int current)
    {
        var max = current;
        foreach (var child in node.ChildNodes())
        {
            if (IsNestedFunction(child))
            {
                continue;
            }

            var next = IsNestingStatement(child) ? current + 1 : current;
            max = Math.Max(max, Depth(child, next));
        }

        return max;
    }

    private static bool IsNestingStatement(SyntaxNode node) => node is IfStatementSyntax or ElseClauseSyntax { Statement: not IfStatementSyntax }
        or WhileStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or DoStatementSyntax
        or SwitchStatementSyntax or TryStatementSyntax or LockStatementSyntax or UsingStatementSyntax or FixedStatementSyntax
        or CheckedStatementSyntax or UnsafeStatementSyntax;

    private static bool IsNestedFunction(SyntaxNode node) => node is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;
}
