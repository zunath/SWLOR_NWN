using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using Math = System.Math;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Resistances and vulnerabilities. The logic that used to be static in <c>Service.Resistance</c> is moved here
    /// unchanged; the resistance type tables are this instance's own fields (read on every damage event, so they are
    /// never rebuilt on a read). The pure rules live in <see cref="ResistanceRules"/>. Calls into the unconverted
    /// <c>Stat</c> and <c>StatusEffect</c> sit behind one-line <c>protected virtual</c> methods whose bodies are the
    /// original calls, as do the reads of native creature data.
    /// </summary>
    public class ResistanceService : IResistanceService
    {
        public const int MinimumResistance = ResistanceRules.MinimumResistance;
        public const int MaximumResistance = ResistanceRules.MaximumResistance;
        public const int VulnerabilityCostTableOffset = ResistanceRules.VulnerabilityCostTableOffset;
        private const int MaximumNonTemporaryPlayerResistance = MaximumResistance - 1;

        private readonly List<ResistanceType> _allResistanceTypes = new();
        private readonly HashSet<ResistanceType> _validResistanceTypes = new();

        private readonly IDBService _db;
        private readonly IMimicryService _mimicry;
        private readonly Lazy<IStatService> _stat;
        private readonly Lazy<IStatusEffectService> _statusEffect;

        public ResistanceService(IDBService db, IMimicryService mimicry, Lazy<IStatService> stat, Lazy<IStatusEffectService> statusEffect)
        {
            _db = db;
            _mimicry = mimicry;
            _stat = stat;
            _statusEffect = statusEffect;
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void LoadResistanceTypes()
        {
            _allResistanceTypes.Clear();
            _validResistanceTypes.Clear();

            foreach (var type in ResistanceRules.EnumerateResistanceTypes())
            {
                _allResistanceTypes.Add(type);
                _validResistanceTypes.Add(type);
            }

            Console.WriteLine($"Loaded {_allResistanceTypes.Count} resistance types.");
        }

        public void AdjustResistance(Player entity, ResistanceType type, int adjustBy)
        {
            if (!IsValidResistanceType(type))
                return;

            if (!entity.Resistances.ContainsKey(type))
                entity.Resistances[type] = 0;

            entity.Resistances[type] += adjustBy;
        }

        public Dictionary<ResistanceType, int> CreateDefaultResistanceValues(int defaultValue = 0)
        {
            return GetAllResistanceTypes()
                .ToDictionary(type => type, _ => defaultValue);
        }

        public int GetResistance(uint creature, ResistanceType type)
        {
            if (!IsValidResistanceType(type))
                return 0;

            return CalculateTotalResistance(creature, type, GetStoredResistance(creature, type));
        }

        public int GetResistanceNative(CNWSCreature creature, ResistanceType type)
        {
            if (!IsValidResistanceType(type))
                return 0;

            return CalculateTotalResistance(GetNativeSelf(creature), type, GetStoredResistanceNative(creature, type));
        }

        public float CalculateResistanceDamageMultiplier(uint creature, ResistanceType type)
        {
            return CalculateResistanceDamageMultiplier(GetResistance(creature, type));
        }

        public float CalculateResistanceDamageMultiplierNative(CNWSCreature creature, ResistanceType type)
        {
            return CalculateResistanceDamageMultiplier(GetResistanceNative(creature, type));
        }

        public int ApplyResistanceToDamage(uint creature, CombatDamageType type, int damage)
        {
            if (damage <= 0)
                return 0;

            if (!type.TryGetElementalResistanceType(out var resistanceType) &&
                !type.TryGetSourceResistanceType(out resistanceType))
                return damage;

            return ApplyResistanceToDamage(creature, resistanceType, damage);
        }

        public int ApplyResistanceToDamage(uint creature, ResistanceType type, int damage)
        {
            if (damage <= 0)
                return 0;

            return ApplyResistanceMultiplier(damage, CalculateResistanceDamageMultiplier(creature, type));
        }

        public int ApplyResistanceToDamageNative(CNWSCreature creature, CombatDamageType type, int damage)
        {
            if (damage <= 0)
                return 0;

            if (!type.TryGetElementalResistanceType(out var resistanceType) &&
                !type.TryGetSourceResistanceType(out resistanceType))
                return damage;

            return ApplyResistanceToDamageNative(creature, resistanceType, damage);
        }

        public int ApplyResistanceToDamageNative(CNWSCreature creature, ResistanceType type, int damage)
        {
            if (damage <= 0)
                return 0;

            return ApplyResistanceMultiplier(damage, CalculateResistanceDamageMultiplierNative(creature, type));
        }

        public int CalculateResistedTicks(uint creature, ResistanceType type, int baseTicks)
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

        public IReadOnlyList<ResistanceType> GetAllResistanceTypes()
        {
            EnsureResistanceTypesLoaded();
            return _allResistanceTypes;
        }

        public bool IsValidResistanceType(ResistanceType type)
        {
            EnsureResistanceTypesLoaded();
            return _validResistanceTypes.Contains(type);
        }

        public bool HasImmunity(uint creature, ResistanceType type)
        {
            return IsValidResistanceType(type) &&
                   GetResistance(creature, type) >= MaximumResistance;
        }

        public int EncodeItemPropertyCostTableValue(int resistance)
        {
            return ResistanceRules.EncodeItemPropertyCostTableValue(resistance);
        }

        public int DecodeItemPropertyCostTableValue(int costTableValue)
        {
            return ResistanceRules.DecodeItemPropertyCostTableValue(costTableValue);
        }

        private void EnsureResistanceTypesLoaded()
        {
            if (_allResistanceTypes.Count <= 0)
                LoadResistanceTypes();
        }

        public float CalculateResistanceDamageMultiplier(int resistance)
        {
            return ResistanceRules.CalculateResistanceDamageMultiplier(resistance);
        }

        private int GetStoredResistance(uint creature, ResistanceType type)
        {
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                return dbPlayer?.Resistances != null &&
                       dbPlayer.Resistances.TryGetValue(type, out var playerResistance)
                    ? playerResistance
                    : 0;
            }

            var npcStats = GetNPCStats(creature);
            return npcStats.Resistances.TryGetValue(type, out var npcResistance)
                ? npcResistance
                : 0;
        }

        private int GetStoredResistanceNative(CNWSCreature creature, ResistanceType type)
        {
            if (IsNativePlayerCharacter(creature) && !GetIsDMPossessed(GetNativeSelf(creature)))
            {
                var playerId = GetNativePlayerId(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                return dbPlayer?.Resistances != null &&
                       dbPlayer.Resistances.TryGetValue(type, out var playerResistance)
                    ? playerResistance
                    : 0;
            }

            var npcStats = GetNPCStatsNative(creature);
            return npcStats.Resistances.TryGetValue(type, out var npcResistance)
                ? npcResistance
                : 0;
        }

        private int CalculateTotalResistance(uint creature, ResistanceType type, int storedResistance)
        {
            var resistance =
                storedResistance +
                GetStatusEffectResistance(creature, type) +
                _mimicry.GetResistanceBonus(creature, type) +
                GetResistanceAdjustment(creature, type);

            if (GetIsPC(creature) &&
                !GetIsDM(creature) &&
                !GetIsDMPossessed(creature) &&
                resistance >= MaximumResistance &&
                !HasTemporaryResistanceImmunity(creature, type))
            {
                return MaximumNonTemporaryPlayerResistance;
            }

            return ResistanceRules.ClampResistance(resistance);
        }

        private static int ApplyResistanceMultiplier(int damage, float multiplier)
        {
            if (multiplier <= 0f)
                return 0;

            return Math.Max(1, (int)Math.Round(damage * multiplier));
        }

        private int GetStatusEffectResistance(uint creature, ResistanceType type)
        {
            return GetCreatureStatusEffects(creature).StatGroup.Resists.TryGetValue(type, out var value)
                ? value
                : 0;
        }

        private int GetResistanceAdjustment(uint creature, ResistanceType type)
        {
            var statType = GetResistanceStatType(type);
            return statType == StatType.Invalid
                ? 0
                : GetStatAdjustment(creature, statType);
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

        private bool HasTemporaryResistanceImmunity(uint creature, ResistanceType type)
        {
            var statType = GetResistanceStatType(type);

            return GetCreatureStatusEffects(creature)
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

        protected virtual NPCStats GetNPCStats(uint npc) => _stat.Value.GetNPCStats(npc);

        protected virtual NPCStats GetNPCStatsNative(CNWSCreature npc) => _stat.Value.GetNPCStatsNative(npc);

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.Value.GetStatAdjustment(creature, stat);

        protected virtual CreatureStatusEffect GetCreatureStatusEffects(uint creature) =>
            _statusEffect.Value.GetCreatureStatusEffects(creature);

        protected virtual uint GetNativeSelf(CNWSCreature creature) => creature.m_idSelf;

        protected virtual bool IsNativePlayerCharacter(CNWSCreature creature) => creature.m_bPlayerCharacter == 1;

        protected virtual string GetNativePlayerId(CNWSCreature creature) => creature.m_pUUID.GetOrAssignRandom().ToString();
    }
}
