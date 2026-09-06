using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Tools
{
    // Shared by the inventory exporter and its regression tests; no runtime dependency.
    public static class PlayerMessagePolicy
    {
        private const int CanBeFalse = 1;
        private const int CanBeTrue = 2;
        private const int UsesDiagnosticFlag = 4;
        private const int Unknown = CanBeFalse | CanBeTrue;

        public static bool IsDiagnosticOnly(InvocationExpressionSyntax call)
        {
            for (SyntaxNode node = call; node.Parent != null; node = node.Parent)
            {
                var parent = node.Parent;
                // A surrounding guard controls registration, not a deferred callback's execution.
                if (parent is AnonymousFunctionExpressionSyntax || parent is LocalFunctionStatementSyntax)
                    break;

                if (parent is IfStatementSyntax statement)
                {
                    if (node == statement.Statement && RequiresDiagnostics(statement.Condition, true))
                        return true;
                    if (node == statement.Else && RequiresDiagnostics(statement.Condition, false))
                        return true;
                }
                if (parent is ConditionalExpressionSyntax conditional)
                {
                    if (node == conditional.WhenTrue && RequiresDiagnostics(conditional.Condition, true))
                        return true;
                    if (node == conditional.WhenFalse && RequiresDiagnostics(conditional.Condition, false))
                        return true;
                }
                if (parent is BinaryExpressionSyntax binary && node == binary.Right)
                {
                    if (binary.IsKind(SyntaxKind.LogicalAndExpression) && RequiresDiagnostics(binary.Left, true))
                        return true;
                    if (binary.IsKind(SyntaxKind.LogicalOrExpression) && RequiresDiagnostics(binary.Left, false))
                        return true;
                }
            }
            return false;
        }

        private static bool RequiresDiagnostics(ExpressionSyntax condition, bool branch)
        {
            var possible = EvaluateWithDiagnosticsDisabled(condition);
            return (possible & UsesDiagnosticFlag) != 0 &&
                (possible & (branch ? CanBeTrue : CanBeFalse)) == 0;
        }

        // Unknown operands can be either boolean value. Only classify a branch as diagnostic
        // when it cannot execute with diagnostics disabled, regardless of those operands.
        private static int EvaluateWithDiagnosticsDisabled(ExpressionSyntax expression)
        {
            if (expression is ParenthesizedExpressionSyntax parentheses)
                return EvaluateWithDiagnosticsDisabled(parentheses.Expression);
            if (expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.ValueText == "DiagnosticsEnabled" &&
                (member.Expression is IdentifierNameSyntax receiver && receiver.Identifier.ValueText == "PlayerFeedback" ||
                 member.Expression is MemberAccessExpressionSyntax qualified && qualified.Name.Identifier.ValueText == "PlayerFeedback" ||
                 member.Expression is AliasQualifiedNameSyntax alias && alias.Name.Identifier.ValueText == "PlayerFeedback"))
                return CanBeFalse | UsesDiagnosticFlag;
            if (expression.IsKind(SyntaxKind.TrueLiteralExpression))
                return CanBeTrue;
            if (expression.IsKind(SyntaxKind.FalseLiteralExpression))
                return CanBeFalse;
            if (expression is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.LogicalNotExpression))
            {
                var operand = EvaluateWithDiagnosticsDisabled(unary.Operand);
                return (operand & UsesDiagnosticFlag) | ((operand & CanBeFalse) << 1) | ((operand & CanBeTrue) >> 1);
            }
            if (expression is BinaryExpressionSyntax binary)
            {
                var left = EvaluateWithDiagnosticsDisabled(binary.Left);
                var right = EvaluateWithDiagnosticsDisabled(binary.Right);
                var result = (left | right) & UsesDiagnosticFlag;
                for (var leftValue = 0; leftValue <= 1; leftValue++)
                for (var rightValue = 0; rightValue <= 1; rightValue++)
                {
                    if ((left & (1 << leftValue)) == 0 || (right & (1 << rightValue)) == 0)
                        continue;
                    bool value;
                    switch (binary.Kind())
                    {
                        case SyntaxKind.LogicalAndExpression: value = leftValue == 1 && rightValue == 1; break;
                        case SyntaxKind.LogicalOrExpression: value = leftValue == 1 || rightValue == 1; break;
                        case SyntaxKind.EqualsExpression: value = leftValue == rightValue; break;
                        case SyntaxKind.NotEqualsExpression: value = leftValue != rightValue; break;
                        default: return Unknown;
                    }
                    result |= value ? CanBeTrue : CanBeFalse;
                }
                return result;
            }
            return Unknown;
        }
    }
}
