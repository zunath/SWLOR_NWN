namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Builder-facing metadata for one registered gameplay ability.</summary>
    public sealed record CreatureAbilityInfo(
        int FeatId,
        string Name,
        string Description,
        int EffectivePerkId,
        string EffectivePerkName,
        int SkillId = 0,
        string SkillName = "",
        bool IsNpcIntended = false)
    {
        public string IntendedFor => IsNpcIntended ? "NPC-intended" : "Player-intended";

        public string Classification => string.IsNullOrWhiteSpace(SkillName)
            ? $"{IntendedFor} \u00b7 No skill"
            : $"{IntendedFor} \u00b7 {SkillName}";
    }
}
