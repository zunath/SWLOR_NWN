using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>A logic file of the server that names content: code outside <c>Feature</c> naming types of one <c>Feature</c> unit.</summary>
/// <param name="File">The logic file, relative to the repository root.</param>
/// <param name="From">The unit the file's code belongs to (<c>Domain/Combat</c>, <c>Platform/Native</c>).</param>
/// <param name="To">The content unit (<c>Feature/StatusEffectDefinition</c>, <c>Feature.UsePerkFeat</c>).</param>
/// <param name="FromDomain">The file's domain.</param>
/// <param name="ToDomain">The content unit's domain.</param>
/// <param name="Types">The distinct content types of that unit the file names, sorted.</param>
internal sealed record ContentReference(string File, DependencyUnit From, DependencyUnit To, string FromDomain, string ToDomain, IReadOnlyList<string> Types)
{
    public string Key => $"{File} -> {To}";

    public bool IsSameDomain => FromDomain == ToDomain;
}
