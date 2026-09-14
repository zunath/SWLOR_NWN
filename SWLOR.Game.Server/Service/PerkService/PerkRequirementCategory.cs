using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerkService
{
    public enum PerkRequirementCategory
    {
        Other = 0,
        Skill = 1,
        Quest = 2,
        CharacterType = 3,
        MustHavePerk = 4,
        CannotHavePerk = 5,
        BeastLevel = 6,
        BeastRole = 7,
        Unlock = 8
    }

    /// <summary>
    /// Maps a perk requirement category to its player-facing label and icon in the
    /// Perks window. A met requirement (or a purchasable perk) shows the shared green
    /// check; an unmet requirement shows that category's red, type-specific icon.
    /// (Icon resrefs live in the sw_ability hak.)
    /// </summary>
    public static class PerkRequirementCategoryResolver
    {
        // Shared icons that are not type-specific.
        public const string MetIcon = "req_met";     // green check: requirement met / perk purchasable
        public const string MaxedIcon = "req_other"; // neutral check: perk fully upgraded

        public class Detail
        {
            public string Name { get; set; }

            // Red, type-specific icon shown when the requirement is not met.
            public string IconResrefLocked { get; set; }

            // Green check when met, otherwise the red type-specific icon.
            public string GetIcon(bool met) => met ? MetIcon : IconResrefLocked;
        }

        private static readonly Dictionary<PerkRequirementCategory, Detail> Details = new()
        {
            { PerkRequirementCategory.Skill, new Detail { Name = "Skill", IconResrefLocked = "req_skill_r" } },
            { PerkRequirementCategory.Quest, new Detail { Name = "Quest", IconResrefLocked = "req_quest_r" } },
            { PerkRequirementCategory.CharacterType, new Detail { Name = "Character Type", IconResrefLocked = "req_chartype_r" } },
            { PerkRequirementCategory.MustHavePerk, new Detail { Name = "Required Perk", IconResrefLocked = "req_needperk_r" } },
            { PerkRequirementCategory.CannotHavePerk, new Detail { Name = "Excluded Perk", IconResrefLocked = "req_noperk_r" } },
            { PerkRequirementCategory.BeastLevel, new Detail { Name = "Beast Level", IconResrefLocked = "req_beastlvl_r" } },
            { PerkRequirementCategory.BeastRole, new Detail { Name = "Beast Role", IconResrefLocked = "req_beastrole_r" } },
            { PerkRequirementCategory.Unlock, new Detail { Name = "Unlock", IconResrefLocked = "req_unlock_r" } },
            { PerkRequirementCategory.Other, new Detail { Name = "Requirement", IconResrefLocked = "req_other_r" } },
        };

        public static Detail GetDetail(PerkRequirementCategory category)
        {
            return Details.TryGetValue(category, out var detail)
                ? detail
                : Details[PerkRequirementCategory.Other];
        }
    }
}
