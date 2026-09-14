using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service.DroidService
{
    public static class DroidInstructions
    {
        public static bool TryGetLevel(DroidPerk instruction, out PerkLevel level)
        {
            level = null;
            return instruction != null && instruction.Level > 0 &&
                   Perk.TryGetPerkDetails(instruction.Perk, out var detail) && detail.IsActive &&
                   detail.PerkLevels.TryGetValue(instruction.Level, out level) &&
                   level.DroidAISlots > 0 && Perk.GetPerkLevelTier(instruction.Perk, instruction.Level) > 0;
        }

        public static bool TryNormalize(DroidPerk instruction, out DroidPerk normalized)
        {
            normalized = null;
            if (instruction == null || instruction.Level <= 0 ||
                !Perk.TryGetPerkDetails(instruction.Perk, out var detail) || !detail.IsActive)
                return false;

            var rank = detail.PerkLevels.Keys.Where(rank => rank <= instruction.Level &&
                    TryGetLevel(new DroidPerk(instruction.Perk, rank), out _))
                .DefaultIfEmpty(0).Max();
            if (rank <= 0)
                return false;

            normalized = new DroidPerk(instruction.Perk, rank);
            return true;
        }

        public static int GetSlots(IEnumerable<DroidPerk> instructions)
        {
            return instructions.Sum(instruction => TryGetLevel(instruction, out var level) ? level.DroidAISlots : 0);
        }

        public static List<DroidPerk> SelectActive(IEnumerable<DroidPerk> instructions, int tier, int slots)
        {
            var selected = new List<DroidPerk>();
            foreach (var group in instructions.Where(instruction => TryGetLevel(instruction, out _) &&
                         Perk.GetPerkLevelTier(instruction.Perk, instruction.Level) <= tier)
                         .GroupBy(instruction => instruction.Perk))
            {
                // Keep one rank per ability. Lower ranks remain learned for reprogramming.
                var instruction = group.OrderByDescending(value => value.Level).First();
                TryGetLevel(instruction, out var level);
                if (level.DroidAISlots > slots)
                    continue;
                slots -= level.DroidAISlots;
                selected.Add(new DroidPerk(instruction.Perk, instruction.Level));
            }
            return selected;
        }

        public static bool Normalize(ConstructedDroid droid, int tier, int slots)
        {
            var learned = NormalizeList(droid.LearnedPerks);
            var active = NormalizeList(droid.ActivePerks);
            foreach (var instruction in active)
                if (!learned.Any(value => value.Perk == instruction.Perk && value.Level == instruction.Level))
                    learned.Add(new DroidPerk(instruction.Perk, instruction.Level));

            active = SelectActive(active, tier, slots);
            var changed = !AreEqual(droid.LearnedPerks, learned) || !AreEqual(droid.ActivePerks, active);
            droid.LearnedPerks = learned;
            droid.ActivePerks = active;
            return changed;
        }

        private static List<DroidPerk> NormalizeList(IEnumerable<DroidPerk> instructions)
        {
            var result = new List<DroidPerk>();
            foreach (var instruction in instructions ?? Enumerable.Empty<DroidPerk>())
                if (TryNormalize(instruction, out var normalized) &&
                    !result.Any(value => value.Perk == normalized.Perk && value.Level == normalized.Level))
                    result.Add(normalized);
            return result;
        }

        private static bool AreEqual(IReadOnlyList<DroidPerk> left, IReadOnlyList<DroidPerk> right)
        {
            return left != null && left.Count == right.Count &&
                   left.Zip(right).All(pair => pair.First != null &&
                       pair.First.Perk == pair.Second.Perk && pair.First.Level == pair.Second.Level);
        }
    }
}
