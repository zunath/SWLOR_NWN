using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public static class DMPlayerExamineListFilter
    {
        public static IEnumerable<KeyValuePair<SkillType, SkillAttribute>> Skills(
            IEnumerable<KeyValuePair<SkillType, SkillAttribute>> skills, Player target, int category, string search)
        {
            return skills.Where(x => x.Value.IsAvailableToCharacterType(target.CharacterType) &&
                (category == 0 || (int)x.Value.Category == category) && Matches(x.Value.Name, search));
        }

        public static IEnumerable<KeyValuePair<PerkType, PerkDetail>> Perks(
            IEnumerable<KeyValuePair<PerkType, PerkDetail>> perks, IReadOnlyDictionary<PerkType, int> ranks,
            int category, string search, int status, int sortOrder, Func<PerkDetail, int, bool> canBuy)
        {
            int Rank(PerkType type) => ranks.TryGetValue(type, out var rank) ? rank : 0;

            var filtered = perks.Where(x =>
                    (x.Value.IsActive && x.Value.GroupType == PerkGroupType.Player) || Rank(x.Key) > 0)
                .Where(x =>
                (category == 0 || (int)x.Value.Category == category) && Matches(x.Value.Name, search))
                .Where(x => status switch
                {
                    1 => Rank(x.Key) > 0,
                    2 => x.Value.IsActive && x.Value.GroupType == PerkGroupType.Player && canBuy(x.Value, Rank(x.Key)),
                    3 => Rank(x.Key) > 0 && Rank(x.Key) >= x.Value.PerkLevels.Count,
                    _ => true
                });

            return sortOrder switch
            {
                1 => filtered.OrderByDescending(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                2 => filtered.OrderBy(x => RequiredSkillLevel(x.Value, Rank(x.Key)))
                    .ThenBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                3 => filtered.OrderByDescending(x => RequiredSkillLevel(x.Value, Rank(x.Key)))
                    .ThenBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                _ => filtered.OrderBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase)
            };
        }

        private static bool Matches(string name, string search) => string.IsNullOrWhiteSpace(search) ||
            name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);

        private static int RequiredSkillLevel(PerkDetail detail, int rank)
        {
            rank = Math.Clamp(rank, 0, detail.PerkLevels.Count);
            int Required(PerkLevel level) => level.Requirements.OfType<PerkRequirementSkill>()
                .Select(x => x.RequiredRank).DefaultIfEmpty(0).Max();

            return detail.PerkLevels.TryGetValue(rank + 1, out var next)
                ? Required(next)
                : detail.PerkLevels.Where(x => x.Key <= rank).OrderByDescending(x => x.Key)
                    .Select(x => Required(x.Value)).FirstOrDefault(x => x > 0);
        }
    }
}
