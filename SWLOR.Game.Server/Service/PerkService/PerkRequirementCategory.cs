using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerkService
{
    public enum PerkRequirementCategory
    {
        Other = 0,
        Skill = 1,
        Quest = 2,
        CharacterType = 3,
        Perk = 4,
        Beast = 5,
        Unlock = 6
    }

    /// <summary>
    /// Maps a perk requirement category to its player-facing label and the icon
    /// shown beside the requirement in the Perks window. The icon resrefs reuse
    /// existing shipped hak icons so no hak rebuild is required.
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
            { PerkRequirementCategory.Skill, new Detail { Name = "Skill", IconResref = "ife_grd_train" } },
            { PerkRequirementCategory.Quest, new Detail { Name = "Quest", IconResref = "ife_key_items" } },
            { PerkRequirementCategory.CharacterType, new Detail { Name = "Character Type", IconResref = "ife_head1" } },
            { PerkRequirementCategory.Perk, new Detail { Name = "Perk", IconResref = "ife_earthly_star" } },
            { PerkRequirementCategory.Beast, new Detail { Name = "Beast", IconResref = "ife_callbeast" } },
            { PerkRequirementCategory.Unlock, new Detail { Name = "Unlock", IconResref = "ife_lockcrsh" } },
            { PerkRequirementCategory.Other, new Detail { Name = "Requirement", IconResref = "ife_ready_check" } },
        };

        public static Detail GetDetail(PerkRequirementCategory category)
        {
            return Details.TryGetValue(category, out var detail)
                ? detail
                : Details[PerkRequirementCategory.Other];
        }
    }
}
