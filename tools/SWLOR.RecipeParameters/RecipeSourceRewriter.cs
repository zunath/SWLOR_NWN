using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SWLOR.RecipeParameters;

internal sealed class RecipeSourceRewriter
{
    private const string HelperParameterName = "parameters";
    private const string RecordParameterName = "Parameters";
    private const string HookMethod = "MinigameParameters";
    private const string ParameterBaseType = "RecipeMinigameParameters";

    private readonly string _path;
    private readonly string _text;
    private readonly Func<string, RecipeParameterPlan> _planFor;
    private readonly SyntaxTree _tree;
    private readonly string _eol;
    private readonly List<TextChange> _edits = new();
    private readonly HashSet<string> _visited = new(StringComparer.Ordinal);
    private readonly Dictionary<SyntaxNode, bool> _ownersHandled = new();
    private int _chainEdits;
    private int _rowEdits;
    private int _plumbingEdits;

    public RecipeSourceRewriter(string path, string text, Func<string, RecipeParameterPlan> planFor)
    {
        _path = path;
        _text = text;
        _planFor = planFor;
        _eol = text.Contains("\r\n") ? "\r\n" : "\n";
        _tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Latest), path);
        ThrowOnSyntaxErrors(_tree, "does not parse");
    }

    public RecipeSourceRewrite Run()
    {
        var creates = _tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsBuilderCreate)
            .ToList();

        foreach (var create in creates)
        {
            ProcessCreate(create);
        }

        var result = ApplyEdits();
        if (result != _text)
            ThrowOnSyntaxErrors(CSharpSyntaxTree.ParseText(result, new CSharpParseOptions(LanguageVersion.Latest), _path), "does not parse after the rewrite");

        return new RecipeSourceRewrite(result, result != _text, _visited, _chainEdits, _rowEdits, _plumbingEdits);
    }

    private static bool IsBuilderCreate(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax
               {
                   Name.Identifier.ValueText: "Create",
                   Expression: IdentifierNameSyntax { Identifier.ValueText: "_builder" or "builder" },
               }
               && invocation.ArgumentList.Arguments.Count >= 1;
    }

    private void ProcessCreate(InvocationExpressionSyntax create)
    {
        // A loop over a generated catalog reads its parameters from RecipeParameterTable; the updater writes the table instead.
        if (IsTableDriven(create))
            return;

        var first = create.ArgumentList.Arguments[0].Expression;
        var recipeName = RecipeNameOf(first);
        if (recipeName != null)
        {
            _visited.Add(recipeName);
            EditChain(create, _planFor(recipeName));
            return;
        }

        switch (first)
        {
            case IdentifierNameSyntax identifier:
                ProcessHelper(create, identifier.Identifier.ValueText);
                return;
            case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax loopVariable } member:
                ProcessRecordLoop(create, loopVariable.Identifier.ValueText, member.Name.Identifier.ValueText);
                return;
            default:
                throw Unsupported(create, $"Create({first}) is not a RecipeType literal, a helper parameter or a record loop");
        }
    }

    /// <summary>True when the recipe's chain sets its parameters from <see cref="RecipeParameterTableFile.ClassName"/>.</summary>
    private static bool IsTableDriven(InvocationExpressionSyntax create)
    {
        return FindCalls(GetChain(create), HookMethod).Any(call =>
            call.ArgumentList.Arguments.Count == 1 &&
            call.ArgumentList.Arguments[0].Expression is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: RecipeParameterTableFile.ClassName },
                    Name.Identifier.ValueText: "For",
                },
            });
    }

    private void ProcessHelper(InvocationExpressionSyntax create, string parameterName)
    {
        var method = create.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var parameter = method?.ParameterList.Parameters.FirstOrDefault(p => p.Identifier.ValueText == parameterName);
        if (method == null || parameter == null || !IsRecipeTypeName(parameter.Type))
            throw Unsupported(create, $"Create({parameterName}) is not a RecipeType parameter of its method");

        if (!_ownersHandled.TryGetValue(method, out var wantsParameters))
        {
            var index = method.ParameterList.Parameters.IndexOf(parameter);
            var methodName = method.Identifier.ValueText;
            var rows = method.Parent.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(row => row.Expression is IdentifierNameSyntax name && name.Identifier.ValueText == methodName)
                .ToList();
            if (rows.Count == 0)
                throw Unsupported(method, $"{methodName} is not called in this file, so its recipes cannot be found");

            var entries = new List<(ArgumentListSyntax Arguments, RecipeParameterPlan Plan)>();
            foreach (var row in rows)
            {
                var argument = FindArgument(row.ArgumentList, index, parameterName);
                var recipeName = argument == null ? null : RecipeNameOf(argument.Expression);
                if (recipeName == null)
                    throw Unsupported(row, $"{methodName} must be called with a RecipeType literal for '{parameterName}'");

                _visited.Add(recipeName);
                entries.Add((row.ArgumentList, _planFor(recipeName)));
            }

            wantsParameters = entries.Any(entry => entry.Plan.Action == RecipeParameterAction.Write);
            if (wantsParameters && !method.ParameterList.Parameters.Any(p => p.Identifier.ValueText == HelperParameterName))
            {
                AddParameter(method.ParameterList, $"{ParameterBaseType} {HelperParameterName} = null");
                _plumbingEdits++;
            }

            foreach (var (arguments, plan) in entries)
            {
                EditRow(arguments, HelperParameterName, plan);
            }

            _ownersHandled[method] = wantsParameters;
        }

        if (wantsParameters)
            PlumbChain(create, HelperParameterName);
    }

    private void ProcessRecordLoop(InvocationExpressionSyntax create, string loopVariable, string memberName)
    {
        var loop = create.Ancestors().OfType<ForEachStatementSyntax>().FirstOrDefault(f => f.Identifier.ValueText == loopVariable);
        var method = create.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (loop == null || method == null || loop.Expression is not IdentifierNameSyntax source)
            throw Unsupported(create, $"Create({loopVariable}.{memberName}) is not a foreach over a method parameter");

        var sourceParameter = method.ParameterList.Parameters.FirstOrDefault(p => p.Identifier.ValueText == source.Identifier.ValueText);
        var elementType = sourceParameter?.Type == null ? null : ElementTypeName(sourceParameter.Type);
        var record = elementType == null
            ? null
            : _tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>()
                .FirstOrDefault(r => r.Identifier.ValueText == elementType && r.ParameterList != null);
        var member = record?.ParameterList.Parameters.FirstOrDefault(p => p.Identifier.ValueText == memberName);
        if (record == null || member == null)
            throw Unsupported(create, $"Create({loopVariable}.{memberName}) needs a positional record whose '{memberName}' member holds the RecipeType");

        if (!_ownersHandled.TryGetValue(record, out var wantsParameters))
        {
            var index = record.ParameterList.Parameters.IndexOf(member);
            var rows = _tree.GetRoot().DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(row => row.Type is IdentifierNameSyntax name && name.Identifier.ValueText == elementType)
                .ToList();
            if (rows.Count == 0)
                throw Unsupported(record, $"{elementType} is never created in this file, so its recipes cannot be found");

            var entries = new List<(ArgumentListSyntax Arguments, RecipeParameterPlan Plan)>();
            foreach (var row in rows)
            {
                var argument = row.ArgumentList == null ? null : FindArgument(row.ArgumentList, index, memberName);
                var recipeName = argument == null ? null : RecipeNameOf(argument.Expression);
                if (recipeName == null)
                    throw Unsupported(row, $"{elementType} must be created with a RecipeType literal for '{memberName}'");

                _visited.Add(recipeName);
                entries.Add((row.ArgumentList, _planFor(recipeName)));
            }

            wantsParameters = entries.Any(entry => entry.Plan.Action == RecipeParameterAction.Write);
            if (wantsParameters && !record.ParameterList.Parameters.Any(p => p.Identifier.ValueText == RecordParameterName))
            {
                AddParameter(record.ParameterList, $"{ParameterBaseType} {RecordParameterName} = null");
                _plumbingEdits++;
            }

            foreach (var (arguments, plan) in entries)
            {
                EditRow(arguments, RecordParameterName, plan);
            }

            _ownersHandled[record] = wantsParameters;
        }

        if (wantsParameters)
            PlumbChain(create, $"{loopVariable}.{RecordParameterName}");
    }

    private void EditChain(InvocationExpressionSyntax create, RecipeParameterPlan plan)
    {
        if (plan.Action == RecipeParameterAction.Leave)
            return;

        var chain = GetChain(create);
        var calls = FindCalls(chain, plan.BuilderMethod);
        if (calls.Count > 1)
            throw Unsupported(create, $"the recipe calls {plan.BuilderMethod} more than once");

        var existing = calls.FirstOrDefault();
        if (plan.Action == RecipeParameterAction.Write)
        {
            var arguments = $"({plan.Arguments})";
            if (existing == null)
            {
                AppendCall(chain, plan.BuilderMethod + arguments);
                _chainEdits++;
            }
            else if (existing.ArgumentList.ToString() != arguments)
            {
                _edits.Add(new TextChange(existing.ArgumentList.Span, arguments));
                _chainEdits++;
            }
        }
        else if (existing != null)
        {
            var member = (MemberAccessExpressionSyntax)existing.Expression;
            _edits.Add(new TextChange(TextSpan.FromBounds(member.Expression.Span.End, existing.Span.End), string.Empty));
            _chainEdits++;
        }
    }

    private void PlumbChain(InvocationExpressionSyntax create, string argument)
    {
        var chain = GetChain(create);
        if (FindCalls(chain, HookMethod).Count > 0)
            return;

        AppendCall(chain, $"{HookMethod}({argument})");
        _plumbingEdits++;
    }

    private void EditRow(ArgumentListSyntax list, string argumentName, RecipeParameterPlan plan)
    {
        if (plan.Action == RecipeParameterAction.Leave)
            return;

        var existing = list.Arguments.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == argumentName);
        if (plan.Action == RecipeParameterAction.Write)
        {
            var text = $"{argumentName}: new {plan.BuilderMethod}({plan.Arguments})";
            if (existing == null)
            {
                var last = list.Arguments[^1];
                _edits.Add(new TextChange(new TextSpan(last.Span.End, 0), ListSeparator(list.Arguments, list.OpenParenToken.Span.End) + text));
                _rowEdits++;
            }
            else if (existing.ToString() != text)
            {
                _edits.Add(new TextChange(existing.Span, text));
                _rowEdits++;
            }

            return;
        }

        if (existing?.Expression is ObjectCreationExpressionSyntax creation && creation.Type.ToString() == plan.BuilderMethod)
        {
            var index = list.Arguments.IndexOf(existing);
            if (index < 1)
                throw Unsupported(existing, "the parameters argument cannot be the first argument");

            _edits.Add(new TextChange(TextSpan.FromBounds(list.Arguments[index - 1].Span.End, existing.Span.End), string.Empty));
            _rowEdits++;
        }
    }

    private void AddParameter(ParameterListSyntax list, string text)
    {
        var last = list.Parameters.Last();
        if (last.Modifiers.Any(SyntaxKind.ParamsKeyword))
            throw Unsupported(list, "the last parameter is a params array, so a parameters argument cannot follow it");

        _edits.Add(new TextChange(new TextSpan(last.Span.End, 0), ListSeparator(list.Parameters, list.OpenParenToken.Span.End) + text));
    }

    private void AppendCall(List<InvocationExpressionSyntax> chain, string call)
    {
        var top = chain[^1];
        _edits.Add(new TextChange(new TextSpan(top.Span.End, 0), ChainSeparator(chain) + "." + call));
    }

    private string ChainSeparator(List<InvocationExpressionSyntax> chain)
    {
        if (chain.Count == 1)
            return _eol + LineIndent(chain[0].Span.Start) + "    ";

        var member = (MemberAccessExpressionSyntax)chain[^1].Expression;
        var gapStart = member.Expression.Span.End;
        var gap = _text.Substring(gapStart, member.OperatorToken.SpanStart - gapStart);
        var newline = gap.LastIndexOf('\n');
        if (newline < 0)
            return string.Empty;

        var eol = newline > 0 && gap[newline - 1] == '\r' ? "\r\n" : "\n";
        return eol + new string(gap.Substring(newline + 1).TakeWhile(char.IsWhiteSpace).ToArray());
    }

    private string ListSeparator<TNode>(SeparatedSyntaxList<TNode> items, int openParenEnd)
        where TNode : SyntaxNode
    {
        var last = items[^1];
        var previousEnd = items.Count >= 2 ? items[^2].Span.End : openParenEnd;
        if (LineOf(previousEnd) != LineOf(last.Span.Start))
            return "," + _eol + LineIndent(last.Span.Start);

        return ", ";
    }

    private int LineOf(int position)
    {
        return _tree.GetLineSpan(new TextSpan(position, 0)).StartLinePosition.Line;
    }

    private string LineIndent(int position)
    {
        var lineStart = _text.LastIndexOf('\n', Math.Max(0, position - 1)) + 1;
        var end = lineStart;
        while (end < _text.Length && (_text[end] == ' ' || _text[end] == '\t'))
        {
            end++;
        }

        return _text.Substring(lineStart, end - lineStart);
    }

    private static List<InvocationExpressionSyntax> GetChain(InvocationExpressionSyntax create)
    {
        var chain = new List<InvocationExpressionSyntax> { create };
        var current = create;
        while (current.Parent is MemberAccessExpressionSyntax member &&
               member.Expression == current &&
               member.Parent is InvocationExpressionSyntax next &&
               next.Expression == member)
        {
            chain.Add(next);
            current = next;
        }

        return chain;
    }

    private static List<InvocationExpressionSyntax> FindCalls(List<InvocationExpressionSyntax> chain, string method)
    {
        return chain.Skip(1)
            .Where(call => ((MemberAccessExpressionSyntax)call.Expression).Name.Identifier.ValueText == method)
            .ToList();
    }

    private static ArgumentSyntax FindArgument(ArgumentListSyntax list, int index, string name)
    {
        var named = list.Arguments.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == name);
        if (named != null)
            return named;

        return index < list.Arguments.Count && list.Arguments[index].NameColon == null ? list.Arguments[index] : null;
    }

    private static string RecipeNameOf(ExpressionSyntax expression)
    {
        return expression is MemberAccessExpressionSyntax
               {
                   Expression: IdentifierNameSyntax { Identifier.ValueText: "RecipeType" },
               } member
            ? member.Name.Identifier.ValueText
            : null;
    }

    private static bool IsRecipeTypeName(TypeSyntax type)
    {
        return type is IdentifierNameSyntax { Identifier.ValueText: "RecipeType" };
    }

    private static string ElementTypeName(TypeSyntax type)
    {
        return type switch
        {
            ArrayTypeSyntax { ElementType: IdentifierNameSyntax element } => element.Identifier.ValueText,
            GenericNameSyntax { TypeArgumentList.Arguments: { Count: 1 } arguments } when arguments[0] is IdentifierNameSyntax element
                => element.Identifier.ValueText,
            _ => null,
        };
    }

    private string ApplyEdits()
    {
        var ordered = _edits.OrderBy(edit => edit.Span.Start).ThenBy(edit => edit.Span.End).ToList();
        var builder = new System.Text.StringBuilder();
        var cursor = 0;
        foreach (var edit in ordered)
        {
            if (edit.Span.Start < cursor)
                throw new RecipeSourceWriterException($"{_path}: two edits overlap near offset {edit.Span.Start}");

            builder.Append(_text, cursor, edit.Span.Start - cursor);
            builder.Append(edit.NewText);
            cursor = edit.Span.End;
        }

        builder.Append(_text, cursor, _text.Length - cursor);
        return builder.ToString();
    }

    private void ThrowOnSyntaxErrors(SyntaxTree tree, string reason)
    {
        var error = tree.GetDiagnostics().FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        if (error != null)
            throw new RecipeSourceWriterException($"{_path}: {reason} ({error.GetMessage()} at line {error.Location.GetLineSpan().StartLinePosition.Line + 1})");
    }

    private RecipeSourceWriterException Unsupported(SyntaxNode node, string message)
    {
        var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return new RecipeSourceWriterException($"{_path}({line}): unsupported recipe shape: {message}");
    }
}
