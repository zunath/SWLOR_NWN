using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Wild Space (plan section 23): the tier 5.5 systems, their environments, the Rift Raiders' stolen cargo and the
    /// half step a Wild Space area adds to tier 5 content. Pure rules; numbers come from <see cref="WildSpaceTuning"/>.
    /// </summary>
    public static class WildSpaceRules
    {
        /// <summary>The Wild Space system of an area resref, or null.</summary>
        public static WildSpaceSystem GetSystem(string areaResref) =>
            WildSpaceTuning.Systems.FirstOrDefault(x => x.AreaResref == areaResref);

        public static bool IsWildSpaceSystem(string areaResref) => GetSystem(areaResref) != null;

        /// <summary>An area's environment from its local int, or Invalid.</summary>
        public static WildSpaceEnvironment ParseEnvironment(int value) =>
            Enum.IsDefined(typeof(WildSpaceEnvironment), value) ? (WildSpaceEnvironment)value : WildSpaceEnvironment.Invalid;

        public static string EnvironmentName(WildSpaceEnvironment environment) =>
            WildSpaceTuning.EnvironmentNames.TryGetValue(environment, out var name) ? name : string.Empty;

        /// <summary>
        /// The stat changes an environment puts on a ship. A ship that ignores environment penalties (the Raider
        /// Prowler) takes only the helpful ones.
        /// </summary>
        public static IReadOnlyList<WildSpaceEffect> Effects(WildSpaceEnvironment environment, bool ignorePenalties)
        {
            if (!WildSpaceTuning.Effects.TryGetValue(environment, out var effects))
                return Array.Empty<WildSpaceEffect>();

            return ignorePenalties ? effects.Where(x => !x.Penalty).ToList() : effects;
        }

        /// <summary>Whether a hull ignores environment penalties and scans Wild Space faster (the Raider Prowler).</summary>
        public static bool IsProwler(string hullTag) => hullTag == WildSpaceTuning.ProwlerTag;

        /// <summary>The extra scan strength percent a hull has in an area: the Raider Prowler's in Wild Space.</summary>
        public static int HullScanBonusPercent(string hullTag, bool wildSpace) =>
            wildSpace && IsProwler(hullTag) ? WildSpaceTuning.ProwlerScanPercent : 0;

        /// <summary>One effect as text: "-12% shield damage taken".</summary>
        public static string EffectText(WildSpaceEffect effect)
        {
            var sign = effect.Amount > 0 ? "+" : string.Empty;
            return effect.Stat switch
            {
                StatType.SpaceSalvageYieldBonus => $"{sign}{effect.Amount} salvage item per cycle",
                StatType.SpaceRareTraitChance => $"{sign}{effect.Amount} rare-trait chance on salvaged components",
                StatType.SpaceShieldDamageTakenPercentAdjustment => $"{sign}{effect.Amount}% shield damage taken",
                StatType.SpaceCapacitorRegenPercentAdjustment => $"{sign}{effect.Amount}% capacitor regen",
                StatType.SpaceEmDamagePercentAdjustment => $"{sign}{effect.Amount}% EM damage",
                StatType.SpaceAbilityDamagePercentAdjustment => $"{sign}{effect.Amount}% ability damage",
                StatType.SpaceCapacitorCostPercentAdjustment => $"{sign}{effect.Amount}% module capacitor use",
                StatType.MovementSpeedPercentAdjustment => $"{sign}{effect.Amount}% speed",
                StatType.SpaceAccuracyPercentAdjustment => $"{sign}{effect.Amount}% Accuracy",
                StatType.DamageDealtPercentAdjustment => $"{sign}{effect.Amount}% damage",
                StatType.EvasionPercentAdjustment => $"{sign}{effect.Amount}% Evasion",
                StatType.SpaceScanStrengthPercent => $"{sign}{effect.Amount}% scan strength",
                _ => $"{sign}{effect.Amount} {effect.Stat}",
            };
        }

        /// <summary>What a pilot is told on entering a Wild Space system.</summary>
        public static string EntryMessage(string systemName, WildSpaceEnvironment environment, bool ignorePenalties)
        {
            var effects = Effects(environment, ignorePenalties);
            var text = effects.Count == 0 ? "no effect on your ship" : string.Join(", ", effects.Select(EffectText));
            var prowler = ignorePenalties ? " Your hull shrugs off its penalties." : string.Empty;
            return $"Wild Space (Tier {WildSpaceTuning.TierLabel}): {systemName}. {EnvironmentName(environment)}: {text}.{prowler} " +
                   "Rifts are the only way out, and a ship destroyed by another pilot here can lose its modules.";
        }

        /// <summary>The tier shown for content of a tier: 5.5 in Wild Space.</summary>
        public static string TierLabel(int tier, bool wildSpace) =>
            wildSpace ? WildSpaceTuning.TierLabel : tier.ToString();

        /// <summary>The tier a signature's signal scales with: the full 5.5 in Wild Space.</summary>
        public static double SignalTier(int tier, bool wildSpace) =>
            wildSpace ? WildSpaceTuning.TierTenths / 10.0 : Math.Clamp(tier, 1, 6);

        /// <summary>The tier of an area's tier-indexed content from its tier in tenths (55 is tier 5.5: tier 5 content).</summary>
        public static int ContentTier(int tierTenths) => Math.Clamp(tierTenths / 10, 1, 6);

        /// <summary>Capacitor regen changed by the ship's capacitor regen percent; never negative.</summary>
        public static double AdjustCapacitorRegen(double regenPerSecond, int percent) =>
            Math.Max(0.0, regenPerSecond * (1.0 + percent / 100.0));

        /// <summary>
        /// The shield points lost per point of absorbed damage after the target's shield damage-taken percent. Never
        /// below a tenth of a point.
        /// </summary>
        public static double AdjustShieldDamageMultiplier(double multiplier, int percent) =>
            Math.Max(0.1, (multiplier <= 0 ? 1.0 : multiplier) * (1.0 + percent / 100.0));

        /// <summary>The damage percent a shot gains: the ship's EM percent on an EM shot, its ability percent on an ability's shot.</summary>
        public static int EnvironmentDamagePercent(CombatDamageType damageType, bool isAbility, int emPercent, int abilityPercent) =>
            (damageType == CombatDamageType.EM ? emPercent : 0) + (isAbility ? abilityPercent : 0);

        /// <summary>A module's capacitor use after the ship's capacitor use percent: at least 1 when it costs any.</summary>
        public static int AdjustCapacitorCost(int cost, int percent)
        {
            if (cost <= 0 || percent == 0)
                return cost;

            return Math.Max(1, (int)Math.Round(cost * (1.0 + percent / 100.0), MidpointRounding.AwayFromZero));
        }

        /// <summary>Whether a creature tag is one of the Rift Raiders.</summary>
        public static bool IsRiftRaider(string creatureTag) =>
            !string.IsNullOrEmpty(creatureTag) && WildSpaceTuning.RiftRaiderTags.Contains(creatureTag);

        /// <summary>
        /// The stolen cargo a destroyed Rift Raider spills, or nothing: <see cref="WildSpaceTuning.StolenCargoPercent"/>
        /// of the time one good, weighted, with its units rolled. <paramref name="next"/> returns 0 to its argument minus 1.
        /// </summary>
        public static Dictionary<CommodityType, int> RollStolenCargo(Func<int, int> next)
        {
            var cargo = new Dictionary<CommodityType, int>();
            if (next(100) >= WildSpaceTuning.StolenCargoPercent)
                return cargo;

            var entries = WildSpaceTuning.StolenCargo;
            var pick = next(entries.Sum(x => x.Weight));
            var entry = entries[^1];
            foreach (var candidate in entries)
            {
                if (pick < candidate.Weight)
                {
                    entry = candidate;
                    break;
                }

                pick -= candidate.Weight;
            }

            cargo[entry.Commodity] = entry.MinimumUnits + next(entry.MaximumUnits - entry.MinimumUnits + 1);
            return cargo;
        }
    }
}
