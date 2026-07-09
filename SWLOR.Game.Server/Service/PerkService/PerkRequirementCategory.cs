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
    /// Maps a perk requirement category to its player-facing label and the icon
    /// shown beside the requirement in the Perks window. Each requirement type has
    /// its own dedicated icon (resrefs below live in the sw_ability hak).
    /// </summary>
    public static class PerkRequirementCategoryResolver
    {
        public class Detail
        {
            public string Name { get; set; }
            public string IconResref { get; set; }

            // Red-framed variant shown when the requirement is not met.
            public string IconResrefLocked { get; set; }

            // Returns the normal icon when the requirement is met, otherwise the
            // red locked icon.
            public string GetIcon(bool met) => met ? IconResref : IconResrefLocked;
        }

        private static readonly Dictionary<PerkRequirementCategory, Detail> Details = new()
        {
            { PerkRequirementCategory.Skill, new Detail { Name = "Skill", IconResref = "req_skill", IconResrefLocked = "req_skill_r" } },
            { PerkRequirementCategory.Quest, new Detail { Name = "Quest", IconResref = "req_quest", IconResrefLocked = "req_quest_r" } },
            { PerkRequirementCategory.CharacterType, new Detail { Name = "Character Type", IconResref = "req_chartype", IconResrefLocked = "req_chartype_r" } },
            { PerkRequirementCategory.MustHavePerk, new Detail { Name = "Required Perk", IconResref = "req_needperk", IconResrefLocked = "req_needperk_r" } },
            { PerkRequirementCategory.CannotHavePerk, new Detail { Name = "Excluded Perk", IconResref = "req_noperk", IconResrefLocked = "req_noperk_r" } },
            { PerkRequirementCategory.BeastLevel, new Detail { Name = "Beast Level", IconResref = "req_beastlvl", IconResrefLocked = "req_beastlvl_r" } },
            { PerkRequirementCategory.BeastRole, new Detail { Name = "Beast Role", IconResref = "req_beastrole", IconResrefLocked = "req_beastrole_r" } },
            { PerkRequirementCategory.Unlock, new Detail { Name = "Unlock", IconResref = "req_unlock", IconResrefLocked = "req_unlock_r" } },
            { PerkRequirementCategory.Other, new Detail { Name = "Requirement", IconResref = "req_other", IconResrefLocked = "req_other_r" } },
        };

        public static Detail GetDetail(PerkRequirementCategory category)
        {
            return Details.TryGetValue(category, out var detail)
                ? detail
                : Details[PerkRequirementCategory.Other];
        }
    }
}
