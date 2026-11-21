using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The salvage and gas harvesting rules (plan section 12) as pure functions. Numbers marked "draft" are W6a
    /// drafts recorded in plan section 12 ("Added by the salvage update") for tuning in testing.
    /// </summary>
    public static class ShipSalvageRules
    {
        /// <summary>How long a wreck stays in space (plan: "a few minutes"; W6a: 5 minutes).</summary>
        public const int WreckSeconds = 300;

        /// <summary>How long the killing party has the wreck to itself.</summary>
        public const int ClaimSeconds = 60;

        /// <summary>The highest salvager and gas harvester tier (rank 40-45).</summary>
        public const int MaximumModuleTier = 5;

        /// <summary>The Aurodium chance per salvage cycle on a tier 5+ wreck outside the Korriban Debris Field (draft).</summary>
        public const int DefaultAurodiumPercent = 4;

        /// <summary>The area local that sets a different Aurodium chance per cycle (the Korriban Debris Field zone raises it).</summary>
        public const string AurodiumPercentVariable = "SALVAGE_AURODIUM_PERCENT";


        private static readonly Regex _tierTag = new("^t([1-6])", RegexOptions.IgnoreCase);

        /// <summary>A preset's Difficulty text as a wreck difficulty; anything unknown counts as Normal.</summary>
        public static WreckDifficulty ParseDifficulty(string difficulty) =>
            difficulty?.Trim().ToLowerInvariant() switch
            {
                "elite" => WreckDifficulty.Elite,
                "boss" => WreckDifficulty.Boss,
                _ => WreckDifficulty.Normal,
            };

        /// <summary>
        /// The faction tier of an NPC ship: the digit of a "tN" tag (t1fighter, t5capital), otherwise from its level in
        /// steps of 10 (the Sith and Mandalorian capitals at level 65 are tier 6).
        /// </summary>
        public static int WreckTier(string creatureTag, int level)
        {
            var match = _tierTag.Match(creatureTag ?? string.Empty);
            if (match.Success)
                return int.Parse(match.Groups[1].Value);

            return Math.Clamp((level - 5) / 10 + 1, 1, 6);
        }

        /// <summary>The salvager or gas harvester tier a target needs: its own tier, at most the highest module tier.</summary>
        public static int RequiredModuleTier(int targetTier) => Math.Clamp(targetTier, 1, MaximumModuleTier);

        /// <summary>Whether a player may salvage: the killing party during the claim, anyone after it or when nobody claims.</summary>
        public static bool CanSalvage(IReadOnlyCollection<string> claimants, string playerId, DateTime destroyedAt, DateTime now)
        {
            if (claimants == null || claimants.Count == 0)
                return true;

            if ((now - destroyedAt).TotalSeconds >= ClaimSeconds)
                return true;

            return playerId != null && claimants.Contains(playerId);
        }

        /// <summary>Seconds the killing party keeps its claim, from now; 0 when it has ended.</summary>
        public static int ClaimSecondsLeft(DateTime destroyedAt, DateTime now) =>
            Math.Max(0, (int)Math.Ceiling(ClaimSeconds - (now - destroyedAt).TotalSeconds));

        /// <summary>A Capital wreck needs a Large or Capital salvager; any hull can salvage the rest.</summary>
        public static bool HullCanSalvage(ShipSize wreckSize, ShipSize salvagerSize) =>
            wreckSize != ShipSize.Capital || salvagerSize is ShipSize.Large or ShipSize.Capital;

        /// <summary>Salvage cycles in a fresh wreck (draft): by hull size, more for elites and bosses.</summary>
        public static int WreckCycles(ShipSize size, WreckDifficulty difficulty)
        {
            var cycles = size switch
            {
                ShipSize.Medium => 4,
                ShipSize.Large => 6,
                ShipSize.Capital => 10,
                _ => 3,
            };

            return cycles + difficulty switch
            {
                WreckDifficulty.Elite => 2,
                WreckDifficulty.Boss => 4,
                _ => 0,
            };
        }

        /// <summary>Items one salvage cycle yields: the mining laser's base amount plus Salvage Expert.</summary>
        public static int SalvageItemsPerCycle(int industrialBonus, int moduleBonus, int salvageExpertBonus) =>
            SpaceCombatMath.ExtractionBaseAmount(industrialBonus, moduleBonus) + Math.Max(0, salvageExpertBonus);

        /// <summary>Tibanna Gas one harvest cycle yields: the mining laser's base amount plus Gas Siphoning.</summary>
        public static int GasPerCycle(int industrialBonus, int moduleBonus, int gasSiphoningBonus) =>
            SpaceCombatMath.ExtractionBaseAmount(industrialBonus, moduleBonus) + Math.Max(0, gasSiphoningBonus);

        /// <summary>The Ship Scrap of a wreck's tier: the Refinery turns tier N scrap into tier N electronics (T6 uses T5).</summary>
        public static string ShipScrapResref(int wreckTier) => ShipSalvageItems.ShipScrap[Math.Clamp(wreckTier, 1, 5) - 1];

        /// <summary>Every item a salvage cycle's scrap yields.</summary>
        public static List<string> ScrapYield(int wreckTier, int items) =>
            Enumerable.Repeat(ShipScrapResref(wreckTier), Math.Max(0, items)).ToList();

        /// <summary>The chance per salvage cycle of a rare find (draft): elite and boss wrecks only.</summary>
        public static int RareFindPercent(WreckDifficulty difficulty) => difficulty switch
        {
            WreckDifficulty.Elite => 10,
            WreckDifficulty.Boss => 20,
            _ => 0,
        };

        /// <summary>
        /// The loot table a rare find comes from: the wreck tier's space elite or boss table (the tables the same ships
        /// drop from), or null for a normal wreck. In Wild Space, the Rift Raiders' elite or boss table.
        /// </summary>
        public static string RareFindTable(int wreckTier, WreckDifficulty difficulty, bool wildSpace = false)
        {
            if (wildSpace)
            {
                return difficulty switch
                {
                    WreckDifficulty.Elite => "SPACE_ELITE_WS",
                    WreckDifficulty.Boss => "SPACE_BOSS_WS",
                    _ => null,
                };
            }

            var tier = Math.Clamp(wreckTier, 1, 6);
            return difficulty switch
            {
                WreckDifficulty.Elite => $"SPACE_ELITE_T{tier}",
                WreckDifficulty.Boss => $"SPACE_BOSS_T{tier}",
                _ => null,
            };
        }

        /// <summary>Whether a 0-99 roll gives a rare find.</summary>
        public static bool RollsRareFind(WreckDifficulty difficulty, int roll) => roll < RareFindPercent(difficulty);

        /// <summary>The Aurodium chance per cycle: tier 5 and 6 wrecks only, at the area's chance when it sets one.</summary>
        public static int AurodiumPercent(int wreckTier, int areaPercent) =>
            wreckTier < 5 ? 0 : areaPercent > 0 ? Math.Min(100, areaPercent) : DefaultAurodiumPercent;

        /// <summary>Whether a 0-99 roll gives a piece of Aurodium.</summary>
        public static bool RollsAurodium(int wreckTier, int areaPercent, int roll) => roll < AurodiumPercent(wreckTier, areaPercent);
    }
}
