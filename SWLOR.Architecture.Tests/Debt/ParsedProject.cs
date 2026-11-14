using Microsoft.CodeAnalysis;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>A project of the solution with its source files parsed. Each tree's <c>FilePath</c> is relative to the repository root.</summary>
internal sealed record ParsedProject(SolutionProject Project, IReadOnlyList<SyntaxTree> Trees)
{
    public string Name => Project.Name;
}
