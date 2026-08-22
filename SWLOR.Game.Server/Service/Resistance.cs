using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    public static class Resistance
    {
        public const int MinimumResistance = -100;
        public const int MaximumResistance = 100;
        public const int VulnerabilityCostTableOffset = MaximumResistance;
        private const int MaximumNonTemporaryPlayerResistance = MaximumResistance - 1;

        private static readonly List<ResistanceType> _allResistanceTypes = new();
        private static readonly HashSet<ResistanceType> _validResistanceTypes = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadResistanceTypes()
        {
            _allResistanceTypes.Clear();
            _validResistanceTypes.Clear();

            foreach (var type in Enum.GetValues(typeof(ResistanceType)).Cast<ResistanceType>())
            {
                if (type == ResistanceType.Invalid)
                    continue;

                _allResistanceTypes.Add(type);
                _validResistanceTypes.Add(type);
            }

            Console.WriteLine($"Loaded {_allResistanceTypes.Count} resistance types.");
        }

        public static void AdjustResistance(Player entity, ResistanceType type, int adjustBy)
        {
            if (!IsValidResistanceType(type))
                return;

            if (!entity.Resistances.ContainsKey(type))
                entity.Resistances[type] = 0;

            entity.Resistances[type] += adjustBy;
        }

        public static Dictionary<ResistanceType, int> CreateDefaultResistanceValues(int defaultValue = 0)
        {
            return GetAllResistanceTypes()
                .ToDictionary(type => type, _ => defaultValue);
        }

        public static int GetResistance(uint creature, ResistanceType type)
        {
            if (!IsValidResistanceType(type))
                return 0;

            return CalculateTotalResistance(creature, type, GetStoredResistance(creature, type));
        }

        public static int GetResistanceNative(CNWSCreature creature, ResistanceType type)
        {
            if (!IsValidResistanceType(type))
                return 0;

            return CalculateTotalResistance(creature.m_idSelf, type, GetStoredResistanceNative(creature, type));
        }

        public static float CalculateResistanceDamageMultiplier(uint creature, ResistanceType type)
        {
            return CalculateResistanceDamageMultiplier(GetResistance(creature, type));
        }

        public static float CalculateResistanceDamageMultiplierNative(CNWSCreature creature, ResistanceType type)
        {
            return CalculateResistanceDamageMultiplier(GetResistanceNative(creature, type));
        }

        public static int ApplyResistanceToDamage(uint creature, CombatDamageType type, int damage)
        {
            if (damage <= 0)
                return 0;

            if (!type.TryGetElementalResistanceType(out var resistanceType) &&
                !type.TryGetSourceResistanceType(out resistanceType))
                return damage;

            return ApplyResistanceToDamage(creature, resistanceType, damage);
        }

        public static int ApplyResistanceToDamage(uint creature, ResistanceType type, int damage)
        {
            if (damage <= 0)
                return 0;

            return ApplyResistanceMultiplier(damage, CalculateResistanceDamageMultiplier(creature, type));
        }

        public static int ApplyResistanceToDamageNative(CNWSCreature creature, CombatDamageType type, int damage)
        {
            if (damage <= 0)
                return 0;

            if (!type.TryGetElementalResistanceType(out var resistanceType) &&
                !type.TryGetSourceResistanceType(out resistanceType))
                return damage;

            return ApplyResistanceToDamageNative(creature, resistanceType, damage);
        }

        public static int ApplyResistanceToDamageNative(CNWSCreature creature, ResistanceType type, int damage)
        {
            if (damage <= 0)
                return 0;

            return ApplyResistanceMultiplier(damage, CalculateResistanceDamageMultiplierNative(creature, type));
        }

        public static int CalculateResistedTicks(uint creature, ResistanceType type, int baseTicks)
        {
            if (baseTicks <= 0 || !IsValidResistanceType(type))
                return baseTicks;

            var resistance = GetResistance(creature, type);
            if (resistance == 0)
                return baseTicks;

            var multiplier = CalculateResistanceDamageMultiplier(resistance);
            if (multiplier <= 0f)
                return 0;

            return Math.Max((int)Math.Round(baseTicks * multiplier), 1);
        }

        public static IReadOnlyList<ResistanceType> GetAllResistanceTypes()
        {
            EnsureResistanceTypesLoaded();
            return _allResistanceTypes;
        }

        public static bool IsValidResistanceType(ResistanceType type)
        {
            EnsureResistanceTypesLoaded();
            return _validResistanceTypes.Contains(type);
        }

        public static bool HasImmunity(uint creature, ResistanceType type)
        {
            return IsValidResistanceType(type) &&
                   GetResistance(creature, type) >= MaximumResistance;
        }

        public static int EncodeItemPropertyCostTableValue(int resistance)
        {
            resistance = ClampResistance(resistance);
            return resistance < 0
                ? VulnerabilityCostTableOffset + Math.Abs(resistance)
                : resistance;
        }

        public static int DecodeItemPropertyCostTableValue(int costTableValue)
        {
            if (costTableValue < 0)
                return ClampResistance(costTableValue);

            if (costTableValue > VulnerabilityCostTableOffset)
                return ClampResistance(-(costTableValue - VulnerabilityCostTableOffset));

            return ClampResistance(costTableValue);
        }

        private static void EnsureResistanceTypesLoaded()
        {
            if (_allResistanceTypes.Count <= 0)
                LoadResistanceTypes();
        }

        public static float CalculateResistanceDamageMultiplier(int resistance)
        {
            resistance = ClampResistance(resistance);

            return Math.Max(0f, 1f - (resistance / 100f));
        }

        private static int GetStoredResistance(uint creature, ResistanceType type)
        {
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);
                return dbPlayer?.Resistances != null &&
                       dbPlayer.Resistances.TryGetValue(type, out var playerResistance)
                    ? playerResistance
                    : 0;
            }

            var npcStats = Stat.GetNPCStats(creature);
            return npcStats.Resistances.TryGetValue(type, out var npcResistance)
                ? npcResistance
                : 0;
        }

        private static int GetStoredResistanceNative(CNWSCreature creature, ResistanceType type)
        {
            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);
                return dbPlayer?.Resistances != null &&
                       dbPlayer.Resistances.TryGetValue(type, out var playerResistance)
                    ? playerResistance
                    : 0;
            }

            var npcStats = Stat.GetNPCStatsNative(creature);
            return npcStats.Resistances.TryGetValue(type, out var npcResistance)
                ? npcResistance
                : 0;
        }

        private static int CalculateTotalResistance(uint creature, ResistanceType type, int storedResistance)
        {
            var resistance =
                storedResistance +
                GetStatusEffectResistance(creature, type) +
                Mimicry.GetResistanceBonus(creature, type) +
                GetResistanceAdjustment(creature, type);

            if (GetIsPC(creature) &&
                !GetIsDM(creature) &&
                resistance >= MaximumResistance &&
                !HasTemporaryResistanceImmunity(creature, type))
            {
                return MaximumNonTemporaryPlayerResistance;
            }

            return ClampResistance(resistance);
        }

        private static int ApplyResistanceMultiplier(int damage, float multiplier)
        {
            if (multiplier <= 0f)
                return 0;

            return Math.Max(1, (int)Math.Round(damage * multiplier));
        }

        private static int ClampResistance(int resistance)
        {
            return Math.Clamp(resistance, MinimumResistance, MaximumResistance);
        }

        private static int GetStatusEffectResistance(uint creature, ResistanceType type)
        {
            return StatusEffect.GetCreatureStatusEffects(creature).StatGroup.Resists.TryGetValue(type, out var value)
                ? value
                : 0;
        }

        private static int GetResistanceAdjustment(uint creature, ResistanceType type)
        {
            var statType = GetResistanceStatType(type);
            return statType == StatType.Invalid
                ? 0
                : Stat.GetStatAdjustment(creature, statType);
        }

        private static StatType GetResistanceStatType(ResistanceType type)
        {
            return type switch
            {
                ResistanceType.Fire => StatType.FireDefense,
                ResistanceType.Poison => StatType.PoisonDefense,
                ResistanceType.Electrical => StatType.ElectricalDefense,
                ResistanceType.Ice => StatType.IceDefense,
                ResistanceType.Mind => StatType.MindResistance,
                ResistanceType.Mobility => StatType.MobilityResistance,
                ResistanceType.Trauma => StatType.TraumaResistance,
                ResistanceType.Disruption => StatType.DisruptionResistance,
                _ => StatType.Invalid
            };
        }

        private static bool HasTemporaryResistanceImmunity(uint creature, ResistanceType type)
        {
            var statType = GetResistanceStatType(type);

            return StatusEffect.GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Any(effect =>
                    effect.DurationTicks > 0 &&
                    (
                        (effect.StatGroup.Resists.TryGetValue(type, out var resistanceValue) &&
                         resistanceValue >= MaximumResistance) ||
                        (statType != StatType.Invalid &&
                         effect.StatGroup.Stats.TryGetValue(statType, out var statValue) &&
                         statValue >= MaximumResistance)
                    ));
        }
    }
}
