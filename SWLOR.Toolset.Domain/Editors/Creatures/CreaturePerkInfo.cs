namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Name, maximum rank, and granted feats for an ability's effective-level perk.</summary>
    public sealed record CreaturePerkInfo(
        int Id,
        string Name,
        int MaximumLevel,
        IReadOnlySet<int>? GrantedFeatIds = null,
        IReadOnlyDictionary<int, string>? GrantedFeatDescriptions = null);
}
