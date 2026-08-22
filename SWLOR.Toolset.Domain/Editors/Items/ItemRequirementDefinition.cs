using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// One equip-requirement itemprop row: RequiresSkill (131), RequiresStat (132),
    /// UseLimitationPerk (100), or UseLimitationRacial (64). Shaped like
    /// <see cref="ItemStatDefinition"/> rather than reusing it, since a requirement gates equipping
    /// instead of contributing a stat and is never shown alongside <see cref="ItemStatGroup"/>.
    /// </summary>
    /// <param name="SkillCategory">
    /// Set only when <paramref name="Category"/> is <see cref="ItemRequirementCategory.Skill"/> -
    /// the referenced <see cref="SkillType"/>'s own <see cref="SkillCategoryType"/>, read from its
    /// <see cref="SkillAttribute"/> so this catalog never re-declares the game's grouping.
    /// </param>
    public sealed record ItemRequirementDefinition(
        ItemRequirementCategory Category,
        string Label,
        int PropertyId,
        int SubtypeId,
        int CostTableId,
        int DisplayOrder,
        SkillCategoryType? SkillCategory = null);
}
