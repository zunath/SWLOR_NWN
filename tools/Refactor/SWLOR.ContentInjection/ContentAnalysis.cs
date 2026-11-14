namespace SWLOR.ContentInjection;

/// <summary>What the analysis of the converted files found: every class, by name, and the names table with the singletons added.</summary>
internal sealed class ContentAnalysis
{
    public ContentAnalysis(IReadOnlyList<ContentClass> classes, NameTable names, IReadOnlyList<Refusal> refusals)
    {
        Classes = classes;
        Names = names;
        Refusals = refusals;
        ByName = classes.GroupBy(type => type.Name, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
    }

    public IReadOnlyList<ContentClass> Classes { get; }

    /// <summary>The converted classes by simple name (a name two classes share is in neither).</summary>
    public IReadOnlyDictionary<string, ContentClass> ByName { get; }

    public NameTable Names { get; }

    /// <summary>Refusals that belong to no single class (a name two classes share).</summary>
    public IReadOnlyList<Refusal> Refusals { get; }

    /// <summary>The converted singletons that become instance classes, by name.</summary>
    public IEnumerable<ContentClass> Singletons => Classes.Where(type => type.IsSingleton);

    /// <summary>The made types that take services, by name: their creation sites change.</summary>
    public IReadOnlyDictionary<string, ContentClass> MadeWithServices =>
        ByName.Values.Where(type => type.IsMadeWithServices).ToDictionary(type => type.Name, StringComparer.Ordinal);
}
