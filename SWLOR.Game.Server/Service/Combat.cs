using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private const float DamageStatDeltaMultiplier = 0.35f;
        public const int BaseGuardDamageReductionPercent = 20;
        public const int MaximumGuardDamageReductionPercent = 55;
        public const int MaximumNormalDamageReductionPercent = 95;
        public const int MaximumDamageBonusPercent = 100;
        private const int MaximumCrossResourceRestorePercent = 95;
        public const int MaximumCombinedDamageReductionPercent = 85;
        public const int MaximumAttackDelayAdjustmentPercent = 50;
        public const int BaseHitRate = 75;
        public const int MinimumHitRate = 20;
        /// <summary>A boss's floor, so stacking Evasion can't make a boss harmless.</summary>
        public const int BossMinimumHitRate = 35;
        public const int MaximumHitRate = 95;
        public const int MinimumCriticalRate = 5;
        public const int MaximumCriticalRate = 50;
        public const int MaximumDamageDerivedHealingPercentPerHit = 50;
        public const int MaximumActivatedDamageHealingMaxHPPercent = 15;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerHit = 6;
        public const int MaximumCooldownDamageHealingMaxHPPercentPerHit = 12;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerWindow = 24;
        public const int MaximumDefeatedEnemyHealingMaxHPPercentPerWindow = 12;
        public const int CombatHealingWindowSeconds = 6;
        public const int MaximumCriticalDamagePercentAdjustment = 200;
        public const int StandardCriticalRating = 2;
        public const int BaseAttackDelayMilliseconds = 1750;
        public const float MeleeWeaponEngagementRange = 1.5f;
        public const float RangedWeaponEngagementRange = 10f;
        // The engine/client cannot play swing animations faster than the base attack delay.
        // Delays below it are honored by resolving multiple attack rolls within a single swing,
        // mirroring the stock engine's flurry behavior for high attacks-per-round builds.
        public const int MaxAttacksPerSwing = 3;
        public const int MinimumAttackDelayMilliseconds =
            (BaseAttackDelayMilliseconds + MaxAttacksPerSwing - 1) / MaxAttacksPerSwing;
        private const int AttackDelayUnitsPerSecond = 60;
        private const int MillisecondsPerSecond = 1000;
        private const int BaseAttackDelayUnits = BaseAttackDelayMilliseconds * AttackDelayUnitsPerSecond / MillisecondsPerSecond;
        private static readonly IdleSkillAbilityStatChannel[] _idleSkillAbilityStatChannels =
        {
            new(
                StatType.IdleSkillAbilitySkillType,
                StatType.IdleSkillAbilityRequiredIdleSeconds,
                StatType.IdleSkillAbilityDamageBonus,
                StatType.IdleSkillAbilityHitChancePercentAdjustment,
                StatType.IdleSkillAbilityCriticalDamagePercentAdjustment),
            new(
                StatType.AlternateIdleSkillAbilitySkillType,
                StatType.AlternateIdleSkillAbilityRequiredIdleSeconds,
                StatType.AlternateIdleSkillAbilityDamageBonus,
                StatType.AlternateIdleSkillAbilityHitChancePercentAdjustment,
                StatType.AlternateIdleSkillAbilityCriticalDamagePercentAdjustment),
        };

        private readonly record struct IdleSkillAbilityStatChannel(
            StatType SkillType,
            StatType RequiredIdleSeconds,
            StatType DamageBonus,
            StatType HitChancePercentAdjustment,
            StatType CriticalDamagePercentAdjustment);

        private sealed class TargetHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
            public DateTime? RechargeAvailableAt { get; init; }
        }
    }
}
