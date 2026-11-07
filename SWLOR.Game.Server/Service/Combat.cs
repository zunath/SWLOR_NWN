using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
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
        public const int BaseGuardDamageReductionPercent = CombatDamageTakenService.BaseGuardDamageReductionPercent;
        public const int MaximumGuardDamageReductionPercent = CombatDamageTakenService.MaximumGuardDamageReductionPercent;
        public const int MaximumNormalDamageReductionPercent = CombatRules.MaximumNormalDamageReductionPercent;
        public const int MaximumDamageBonusPercent = CombatRules.MaximumDamageBonusPercent;
        public const int MaximumCombinedDamageReductionPercent = CombatRules.MaximumCombinedDamageReductionPercent;
        public const int MaximumAttackDelayAdjustmentPercent = CombatRules.MaximumAttackDelayAdjustmentPercent;
        public const int BaseHitRate = CombatRules.BaseHitRate;
        public const int MinimumHitRate = CombatRules.MinimumHitRate;
        /// <summary>A boss's floor, so stacking Evasion can't make a boss harmless.</summary>
        public const int BossMinimumHitRate = CombatQueriesService.BossMinimumHitRate;
        public const int MaximumHitRate = CombatRules.MaximumHitRate;
        public const int MinimumCriticalRate = CombatRules.MinimumCriticalRate;
        public const int MaximumCriticalRate = CombatRules.MaximumCriticalRate;
        public const int MaximumDamageDerivedHealingPercentPerHit = CombatRules.MaximumDamageDerivedHealingPercentPerHit;
        public const int MaximumActivatedDamageHealingMaxHPPercent = CombatResourcesService.MaximumActivatedDamageHealingMaxHPPercent;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerHit = CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerHit;
        public const int MaximumCooldownDamageHealingMaxHPPercentPerHit = CombatResourcesService.MaximumCooldownDamageHealingMaxHPPercentPerHit;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerWindow = CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerWindow;
        public const int MaximumDefeatedEnemyHealingMaxHPPercentPerWindow = CombatResourcesService.MaximumDefeatedEnemyHealingMaxHPPercentPerWindow;
        public const int CombatHealingWindowSeconds = CombatResourcesService.CombatHealingWindowSeconds;
        public const int MaximumCriticalDamagePercentAdjustment = CombatRules.MaximumCriticalDamagePercentAdjustment;
        public const int StandardCriticalRating = CombatRules.StandardCriticalRating;
        public const int BaseAttackDelayMilliseconds = CombatRules.BaseAttackDelayMilliseconds;
        public const float MeleeWeaponEngagementRange = CombatRules.MeleeWeaponEngagementRange;
        public const float RangedWeaponEngagementRange = CombatRules.RangedWeaponEngagementRange;
        public const int MaxAttacksPerSwing = CombatRules.MaxAttacksPerSwing;
        public const int MinimumAttackDelayMilliseconds = CombatRules.MinimumAttackDelayMilliseconds;

        // The area ability hit sequences of AbilityImpact use this; CombatActivityService has its own copy for the First
        // Strike stacks it moved with.
        private sealed class TargetHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
            public DateTime? RechargeAvailableAt { get; init; }
        }
    }
}
