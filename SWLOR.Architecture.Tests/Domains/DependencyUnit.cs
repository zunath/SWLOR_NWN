namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// The granularity of the guard: one service, one Feature folder, one top-level Feature type or one Platform area.
/// Its text form is what the baseline files and failure messages show:
/// <c>Combat</c>, <c>Feature/PerkDefinition</c>, <c>Feature.Auditing</c>, <c>Platform/Native</c>.
/// </summary>
internal readonly record struct DependencyUnit(UnitKind Kind, string Name)
{
    public override string ToString() => Kind switch
    {
        UnitKind.Service => Name,
        UnitKind.FeatureFolder => "Feature/" + Name,
        UnitKind.FeatureType => "Feature." + Name,
        UnitKind.Platform => "Platform/" + Name,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind))
    };

    /// <summary>Reads the text form back; returns false for anything else.</summary>
    public static bool TryParse(string text, out DependencyUnit unit)
    {
        unit = default;
        if (text.StartsWith("Feature/", StringComparison.Ordinal))
            unit = new DependencyUnit(UnitKind.FeatureFolder, text["Feature/".Length..]);
        else if (text.StartsWith("Feature.", StringComparison.Ordinal))
            unit = new DependencyUnit(UnitKind.FeatureType, text["Feature.".Length..]);
        else if (text.StartsWith("Platform/", StringComparison.Ordinal))
            unit = new DependencyUnit(UnitKind.Platform, text["Platform/".Length..]);
        else if (text.Length > 0 && !text.Contains('/') && !text.Contains(' '))
            unit = new DependencyUnit(UnitKind.Service, text);
        else
            return false;

        return unit.Name.Length > 0;
    }
}
