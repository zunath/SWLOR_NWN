using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Embattled (Surrounded, Not Outmatched): each distinct hostile attacker currently pressuring the
    /// wearer grants one stack (up to a stat-defined maximum). Each stack grants flat Defense and Force Defense.
    /// Center of the Storm grants additional Mobility Resistance once the high-stack threshold is reached.
    /// </summary>
    public sealed class EmbattledStatusEffect : StatusEffectBase
    {
        private const float PressureWindowSeconds = 6f;

        // defender -> (attacker -> last pressuring attack time)
        private static readonly Dictionary<uint, Dictionary<uint, DateTime>> Attackers = new();

        private readonly int _stacks;
        private readonly int _defensePercent;
        private readonly int _forceDefensePercent;
        private readonly int _mobilityResistance;

        public int Stacks => _stacks;

        public override string Name => "Embattled";
        public override EffectIconType Icon => EffectIconType.EmbattledStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public EmbattledStatusEffect()
            : this(1, 2, 2, 0)
        {
        }

        public EmbattledStatusEffect(int stacks, int defensePercent, int forceDefensePercent, int mobilityResistance)
        {
            _stacks = stacks;
            _defensePercent = defensePercent;
            _forceDefensePercent = forceDefensePercent;
            _mobilityResistance = mobilityResistance;

            if (defensePercent != 0)
                StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = defensePercent;
            if (forceDefensePercent != 0)
                StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = forceDefensePercent;
            if (mobilityResistance != 0)
                StatGroup.Stats[StatType.MobilityResistance] = mobilityResistance;
        }

        public override IStatusEffect Clone()
        {
            return new EmbattledStatusEffect(_stacks, _defensePercent, _forceDefensePercent, _mobilityResistance);
        }

        /// <summary>
        /// Records a pressuring attacker against the defender and (re)applies Embattled with the
        /// updated stack count. Does nothing unless the defender owns the Surrounded, Not Outmatched trait,
        /// as declared by the per-stack Defense stat.
        /// </summary>
        public static void Refresh(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender))
            {
                Attackers.Remove(defender);
                return;
            }

            var perStackDefense = Stat.GetStatAdjustment(defender, StatType.EmbattledStackDefensePercent);
            if (perStackDefense <= 0)
                return;

            var perStackForceDefense = Stat.GetStatAdjustment(defender, StatType.EmbattledStackForceDefensePercent);
            var maxStacks = Math.Max(1, Stat.GetStatAdjustment(defender, StatType.EmbattledMaxStacks));
            var highStackThreshold = Stat.GetStatAdjustment(defender, StatType.EmbattledHighStackThreshold);
            var highStackMobility = Stat.GetStatAdjustment(defender, StatType.EmbattledHighStackMobilityResistance);

            var stackCount = TrackAndCount(defender, attacker, maxStacks);
            var mobility = highStackThreshold > 0 && stackCount >= highStackThreshold ? highStackMobility : 0;

            StatusEffect.RemoveStatusEffect(defender, typeof(EmbattledStatusEffect), false);
            StatusEffect.ApplyStatusEffect(
                defender,
                defender,
                new EmbattledStatusEffect(
                    stackCount,
                    stackCount * perStackDefense,
                    stackCount * perStackForceDefense,
                    mobility),
                PressureWindowSeconds);
        }

        /// <summary>
        /// Current Embattled stack count on the defender, treating Perfect Aegis as the maximum.
        /// </summary>
        public static int GetStackCount(uint defender)
        {
            if (StatusEffect.HasStatusEffect(defender, typeof(PerfectAegisStatusEffect)))
                return PerfectAegisStatusEffect.TreatedAsEmbattledStacks;

            return StatusEffect.GetStatusEffect(defender, typeof(EmbattledStatusEffect)) is EmbattledStatusEffect pressure
                ? pressure.Stacks
                : 0;
        }

        private static int TrackAndCount(uint defender, uint attacker, int maxStacks)
        {
            if (!Attackers.TryGetValue(defender, out var attackerTimes))
            {
                attackerTimes = new Dictionary<uint, DateTime>();
                Attackers[defender] = attackerTimes;
            }

            var now = DateTime.UtcNow;
            if (GetIsObjectValid(attacker))
                attackerTimes[attacker] = now;

            foreach (var stale in attackerTimes
                         .Where(x => (now - x.Value).TotalSeconds > PressureWindowSeconds)
                         .Select(x => x.Key)
                         .ToList())
            {
                attackerTimes.Remove(stale);
            }

            if (attackerTimes.Count == 0)
                Attackers.Remove(defender);

            return Math.Clamp(attackerTimes.Count, 1, maxStacks);
        }
    }
}
