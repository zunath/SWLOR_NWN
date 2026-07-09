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
        }

        private static readonly Dictionary<PerkRequirementCategory, Detail> Details = new()
        {
            { PerkRequirementCategory.Skill, new Detail { Name = "Skill", IconResref = "req_skill" } },
            { PerkRequirementCategory.Quest, new Detail { Name = "Quest", IconResref = "req_quest" } },
            { PerkRequirementCategory.CharacterType, new Detail { Name = "Character Type", IconResref = "req_chartype" } },
            { PerkRequirementCategory.MustHavePerk, new Detail { Name = "Required Perk", IconResref = "req_needperk" } },
            { PerkRequirementCategory.CannotHavePerk, new Detail { Name = "Excluded Perk", IconResref = "req_noperk" } },
            { PerkRequirementCategory.BeastLevel, new Detail { Name = "Beast Level", IconResref = "req_beastlvl" } },
            { PerkRequirementCategory.BeastRole, new Detail { Name = "Beast Role", IconResref = "req_beastrole" } },
            { PerkRequirementCategory.Unlock, new Detail { Name = "Unlock", IconResref = "req_unlock" } },
            { PerkRequirementCategory.Other, new Detail { Name = "Requirement", IconResref = "req_other" } },
        };

        public static Detail GetDetail(PerkRequirementCategory category)
        {
            return Details.TryGetValue(category, out var detail)
                ? detail
                : Details[PerkRequirementCategory.Other];
        }
    }
}
