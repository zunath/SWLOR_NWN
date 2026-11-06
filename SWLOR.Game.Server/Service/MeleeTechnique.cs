using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Enemy-bound melee sequences driven by ability metadata and source-scoped stats. The logic lives in <see cref="IMeleeTechniqueService"/>; the event handlers stay here and every member forwards to the container's instance.</summary>
    public static class MeleeTechnique
    {
        private static IMeleeTechniqueService Instance => FacadeAccessor.Resolve<IMeleeTechniqueService>();

        /// <summary>One notification per completed, landed technique which paid stamina.</summary>
        public static event Action<uint, uint, AbilityDetail, MeleeTechniqueStage> TechniqueLanded
        {
            add => Instance.TechniqueLanded += value;
            remove => Instance.TechniqueLanded -= value;
        }

        /// <summary>Replaces this skill's content profile; invalid definitions fail closed.</summary>
        public static void RegisterProfile(SkillType skill, MeleeTechniqueProfile profile) => Instance.RegisterProfile(skill, profile);

        public static void UnregisterProfile(SkillType skill) => Instance.UnregisterProfile(skill);

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void ResetProfiles() => Instance.ResetProfiles();

        public static int GetLineStat(uint creature, MeleeTechniqueLine line, StatType payload) => Instance.GetLineStat(creature, line, payload);

        public static string ValidateActivation(uint creature, AbilityDetail ability, uint target) => Instance.ValidateActivation(creature, ability, target);

        /// <summary>
        /// Called when a cast starts. A technique which requires a setup keeps the one it started with
        /// until that same cast completes, so the setup cannot expire under its cast time.
        /// </summary>
        public static void HoldSetupForCast(uint creature, uint target, AbilityDetail ability, string activationId) => Instance.HoldSetupForCast(creature, target, ability, activationId);

        /// <summary>Called after all paid requirements, before an impact is launched.</summary>
        public static void BeginPaidActivation(uint creature, uint target, AbilityDetail ability) => Instance.BeginPaidActivation(creature, target, ability);

        public static int GetConditionalPower(uint creature, uint target, int impactIndex = 0) => Instance.GetConditionalPower(creature, target, impactIndex);

        public static uint GetImpactTarget(uint creature, uint primaryTarget, int impactIndex) => Instance.GetImpactTarget(creature, primaryTarget, impactIndex);

        public static bool IsSweepActivation(uint creature, AbilityDetail ability) => Instance.IsSweepActivation(creature, ability);

        public static int GetConditionalAccuracy(uint creature, uint target) => Instance.GetConditionalAccuracy(creature, target);

        /// <summary>Enemy-bound accuracy follows the setup, not the current weapon or ability skill.</summary>
        public static int GetAbilityAccuracy(uint creature, uint target, AbilityDetail ability) => Instance.GetAbilityAccuracy(creature, target, ability);

        public static MeleeTechniqueStage GetSetupStage(uint creature, uint target) => Instance.GetSetupStage(creature, target);

        public static int GetPendingWeavePower(uint creature, uint target) => Instance.GetPendingWeavePower(creature, target);

        public static int GetPendingRetaliationPower(uint creature, uint target) => Instance.GetPendingRetaliationPower(creature, target);

        public static int? GetCapturedWeaponDamage(uint creature, AbilityDetail ability) => Instance.GetCapturedWeaponDamage(creature, ability);

        public static CombatDamageType? GetCapturedWeaponDamageType(uint creature, AbilityDetail ability) => Instance.GetCapturedWeaponDamageType(creature, ability);

        public static bool IsImpactValid(uint creature, uint target, AbilityDetail ability) => Instance.IsImpactValid(creature, target, ability);

        public static void CompleteActivation(uint creature, AbilityImpactSummary summary) => Instance.CompleteActivation(creature, summary);

        public static void AbortActivation(uint creature) => Instance.AbortActivation(creature);

        /// <summary>One sample per real native roll, including misses, excluding queued replacements.</summary>
        public static int PrepareOrdinaryAttempt(uint creature, uint target, SkillType skill, bool queuedReplacement, int attackIndex) => Instance.PrepareOrdinaryAttempt(creature, target, skill, queuedReplacement, attackIndex);

        /// <summary>Duplicate/discarded damage queries cannot pay the sampled weave again.</summary>
        public static int TakeOrdinaryPower(uint creature, uint target, SkillType skill, int attackIndex, bool landed) => Instance.TakeOrdinaryPower(creature, target, skill, attackIndex, landed);

        public static int ApplyOrdinaryDamageModifiers(uint creature, int damage, CombatDamageType damageType) => Instance.ApplyOrdinaryDamageModifiers(creature, damage, damageType);

        public static int GetStaminaDiscount(uint creature, AbilityDetail ability, int cost, bool consume) => Instance.GetStaminaDiscount(creature, ability, cost, consume);

        /// <summary>Captures actual HP before a typed direct hit; periodic/triggered/reflected paths never enter.</summary>
        public static void PrepareDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType) => Instance.PrepareDirectDamage(attacker, defender, deliveryType);

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void OnDirectDamageApplied() => Instance.OnDirectDamageApplied();

        public static void ClearSetup(uint creature) => Instance.ClearSetup(creature);

        public static void OnStanceChanged(uint creature) => Instance.OnStanceChanged(creature);

        public static void ClearBenefits(uint creature) => Instance.ClearBenefits(creature);

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip() => Instance.OnEquip();

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnUnequip() => Instance.OnUnequip();

        [NWNEventHandler(ScriptName.OnAreaExit)]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnExit() => Instance.OnExit();

        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnDeath() => Instance.OnDeath();

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath() => Instance.OnPlayerDeath();

        [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
        public static void RefreshAll() => Instance.RefreshAll();
    }
}
