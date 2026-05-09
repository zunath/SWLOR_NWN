using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    internal static class WeaponBlueprintPerkMigration
    {
        private static readonly string NewKey = nameof(PerkType.WeaponBlueprints);

        private static readonly string[][] LegacyKeyGroups =
        {
            new[] { "BladeBlueprints", "OneHandedBlueprints", "127" },
            new[] { "HeavyWeaponBlueprints", "TwoHandedBlueprints", "128" },
            new[] { "MartialBlueprints", "129" },
            new[] { "ProjectileBlueprints", "RangedBlueprints", "130" }
        };

        private static readonly string[] LegacyKeys = LegacyKeyGroups
            .SelectMany(keys => keys)
            .ToArray();

        private static readonly int[] LegacyCumulativeCosts =
        {
            0,
            1,
            2,
            4,
            7,
            10
        };

        private static readonly int[] ConsolidatedCumulativeCosts =
        {
            0,
            2,
            5,
            9,
            14,
            20
        };

        private static readonly string[] AllKeys = LegacyKeys
            .Concat(new[] { NewKey })
            .ToArray();

        public static bool CollapsePlayerPerks(JObject player)
        {
            return CollapsePlayerPerks(player, out _);
        }

        public static bool CollapsePlayerPerks(JObject player, out int refundDelta)
        {
            if (player == null)
            {
                refundDelta = 0;
                return false;
            }

            var migrated = CollapseLearnedPerks(player[nameof(Player.Perks)] as JObject, out refundDelta);
            migrated |= CollapseUnlockedPerks(player[nameof(Player.UnlockedPerks)] as JObject);

            return migrated;
        }

        private static bool CollapseLearnedPerks(JObject perks, out int refundDelta)
        {
            refundDelta = 0;
            if (perks == null)
                return false;

            var found = false;
            var foundLegacy = false;
            var highestRank = 0;
            var legacyCost = 0;

            foreach (var keyGroup in LegacyKeyGroups)
            {
                var highestLegacyRank = 0;

                foreach (var key in keyGroup)
                {
                    var token = perks[key];
                    if (token == null)
                        continue;

                    found = true;
                    foundLegacy = true;
                    var rank = token.Value<int>();
                    highestRank = Math.Max(highestRank, rank);
                    highestLegacyRank = Math.Max(highestLegacyRank, rank);
                }

                legacyCost += GetCumulativeCost(LegacyCumulativeCosts, highestLegacyRank);
            }

            if (perks[NewKey] != null)
            {
                found = true;
                highestRank = Math.Max(highestRank, perks[NewKey].Value<int>());
            }

            if (!found || !foundLegacy)
                return false;

            var consolidatedCost = GetCumulativeCost(ConsolidatedCumulativeCosts, highestRank);
            refundDelta = Math.Max(legacyCost - consolidatedCost, 0);

            RemoveKeys(perks);

            if (highestRank > 0)
                perks[NewKey] = highestRank;

            return true;
        }

        private static bool CollapseUnlockedPerks(JObject unlockedPerks)
        {
            if (unlockedPerks == null)
                return false;

            var found = false;
            var foundLegacy = false;
            JToken selectedToken = null;
            DateTime? selectedDate = null;

            foreach (var key in AllKeys)
            {
                var token = unlockedPerks[key];
                if (token == null)
                    continue;

                found = true;

                if (key != NewKey)
                    foundLegacy = true;

                if (!DateTime.TryParse(token.Value<string>(), out var parsedDate))
                {
                    selectedToken ??= token.DeepClone();
                    continue;
                }

                if (selectedDate != null && parsedDate >= selectedDate.Value)
                    continue;

                selectedDate = parsedDate;
                selectedToken = token.DeepClone();
            }

            if (!found || !foundLegacy)
                return false;

            RemoveKeys(unlockedPerks);

            if (selectedToken != null)
                unlockedPerks[NewKey] = selectedToken;

            return true;
        }

        private static int GetCumulativeCost(int[] costs, int rank)
        {
            if (rank <= 0)
                return 0;

            if (rank >= costs.Length)
                return costs[costs.Length - 1];

            return costs[rank];
        }

        private static void RemoveKeys(JObject dictionary)
        {
            foreach (var key in AllKeys)
            {
                dictionary.Remove(key);
            }
        }
    }
}
