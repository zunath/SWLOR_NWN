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
        private const int MaximumResistance = 100;
        private const float ResistanceDamageCurve = 50f;
        private const float MinimumDamageMultiplier = 0.1f;
        private const float StatusDurationCurve = 150f;
        private const float StatusHighScoreThreshold = 90f;
        private const float StatusHighScoreCurve = 200f;
        private const float StatusDurationVariance = 0.03f;

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
            if (resistance <= 0)
                return baseTicks;

            var baseMultiplier = CalculateStatusDurationMultiplier(resistance);
            var variance = Random.NextFloat(-StatusDurationVariance, StatusDurationVariance);
            var finalMultiplier = Math.Clamp(baseMultiplier + variance, MinimumDamageMultiplier, 1f);
            return Math.Max((int)Math.Round(baseTicks * finalMultiplier), 1);
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

        private static void EnsureResistanceTypesLoaded()
        {
            if (_allResistanceTypes.Count <= 0)
                LoadResistanceTypes();
        }

        private static float CalculateResistanceDamageMultiplier(int resistance)
        {
            resistance = ClampResistance(resistance);

            if (resistance <= 0)
                return 1f;

            return Math.Max(
                1f - (resistance / (resistance + ResistanceDamageCurve)),
                MinimumDamageMultiplier);
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
                GetResistanceAdjustment(creature, type);

            return ClampResistance(resistance);
        }

        private static int ApplyResistanceMultiplier(int damage, float multiplier)
        {
            return Math.Max(1, (int)Math.Round(damage * multiplier));
        }

        private static float CalculateStatusDurationMultiplier(int resistance)
        {
            resistance = ClampResistance(resistance);

            if (resistance >= StatusHighScoreThreshold)
            {
                var extremeResistFactor =
                    1f - ((resistance - StatusHighScoreThreshold) / StatusHighScoreCurve);

                return
                    (1f - (StatusHighScoreThreshold / StatusDurationCurve)) *
                    extremeResistFactor;
            }

            return 1f - (resistance / StatusDurationCurve);
        }

        private static int ClampResistance(int resistance)
        {
            return Math.Clamp(resistance, 0, MaximumResistance);
        }

        private static int GetStatusEffectResistance(uint creature, ResistanceType type)
        {
            return StatusEffect.GetCreatureStatusEffects(creature).StatGroup.Resists.TryGetValue(type, out var value)
                ? value
                : 0;
        }

        private static int GetResistanceAdjustment(uint creature, ResistanceType type)
        {
            return type switch
            {
                ResistanceType.Fire => Stat.GetStatAdjustment(creature, StatType.FireDefense),
                ResistanceType.Poison => Stat.GetStatAdjustment(creature, StatType.PoisonDefense),
                ResistanceType.Electrical => Stat.GetStatAdjustment(creature, StatType.ElectricalDefense),
                ResistanceType.Ice => Stat.GetStatAdjustment(creature, StatType.IceDefense),
                ResistanceType.Mind => Stat.GetStatAdjustment(creature, StatType.MindResistance),
                ResistanceType.Mobility => Stat.GetStatAdjustment(creature, StatType.MobilityResistance),
                ResistanceType.Trauma => Stat.GetStatAdjustment(creature, StatType.TraumaResistance),
                _ => 0
            };
        }
    }
}
