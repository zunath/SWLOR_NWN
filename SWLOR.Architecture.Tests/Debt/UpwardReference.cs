using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The members of one file's code in one node that reference one node higher in the domain order, in their own band. A file
/// whose top-level types are in two nodes has a line for each.
/// </summary>
/// <param name="File">The file, relative to the repository root.</param>
/// <param name="From">The node the file's code is in.</param>
/// <param name="To">The higher node it references.</param>
/// <param name="Members">The distinct members of the file that hold such a reference (<see cref="SourceReference.Member"/>).</param>
/// <param name="Targets">The distinct types of <paramref name="To"/> they name, sorted.</param>
/// <param name="UnitEdges">The unit edges the references make (<c>Domain/Character -&gt; Domain/Combat</c>).</param>
internal sealed record UpwardReference(
    string File,
    OrderNode From,
    OrderNode To,
    IReadOnlySet<string> Members,
    IReadOnlyList<string> Targets,
    IReadOnlySet<(DependencyUnit From, DependencyUnit To)> UnitEdges)
{
    public string Key => $"{File}: {From} -> {To}";

    public string Direction => $"{From} -> {To}";
}
