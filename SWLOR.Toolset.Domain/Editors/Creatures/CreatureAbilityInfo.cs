namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Builder-facing metadata for one registered gameplay ability.</summary>
    public sealed record CreatureAbilityInfo(
        int FeatId,
        string Name,
        string Description,
        int EffectivePerkId,
        string EffectivePerkName);
}
