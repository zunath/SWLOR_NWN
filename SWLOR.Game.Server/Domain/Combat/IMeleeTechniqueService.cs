using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Enemy-bound melee sequences driven by ability metadata and source-scoped stats.</summary>
    public interface IMeleeTechniqueService
    {
        /// <summary>One notification per completed, landed technique which paid stamina.</summary>
        event Action<uint, uint, AbilityDetail, MeleeTechniqueStage> TechniqueLanded;

        /// <summary>Replaces this skill's content profile; invalid definitions fail closed.</summary>
        void RegisterProfile(SkillType skill, MeleeTechniqueProfile profile);

        void UnregisterProfile(SkillType skill);

        void ResetProfiles();

        int GetLineStat(uint creature, MeleeTechniqueLine line, StatType payload);

        string ValidateActivation(uint creature, AbilityDetail ability, uint target);

        /// <summary>
        /// Called when a cast starts. A technique which requires a setup keeps the one it started with
        /// until that same cast completes, so the setup cannot expire under its cast time.
        /// </summary>
        void HoldSetupForCast(uint creature, uint target, AbilityDetail ability, string activationId);

        /// <summary>Called after all paid requirements, before an impact is launched.</summary>
        void BeginPaidActivation(uint creature, uint target, AbilityDetail ability);

        int GetConditionalPower(uint creature, uint target, int impactIndex);

        uint GetImpactTarget(uint creature, uint primaryTarget, int impactIndex);

        bool IsSweepActivation(uint creature, AbilityDetail ability);

        int GetConditionalAccuracy(uint creature, uint target);

        /// <summary>Enemy-bound accuracy follows the setup, not the current weapon or ability skill.</summary>
        int GetAbilityAccuracy(uint creature, uint target, AbilityDetail ability);

        MeleeTechniqueStage GetSetupStage(uint creature, uint target);

        int GetPendingWeavePower(uint creature, uint target);

        int GetPendingRetaliationPower(uint creature, uint target);

        int? GetCapturedWeaponDamage(uint creature, AbilityDetail ability);

        CombatDamageType? GetCapturedWeaponDamageType(uint creature, AbilityDetail ability);

        bool IsImpactValid(uint creature, uint target, AbilityDetail ability);

        void CompleteActivation(uint creature, AbilityImpactSummary summary);

        void AbortActivation(uint creature);

        /// <summary>One sample per real native roll, including misses, excluding queued replacements.</summary>
        int PrepareOrdinaryAttempt(uint creature, uint target, SkillType skill, bool queuedReplacement, int attackIndex);

        /// <summary>Duplicate/discarded damage queries cannot pay the sampled weave again.</summary>
        int TakeOrdinaryPower(uint creature, uint target, SkillType skill, int attackIndex, bool landed);

        int ApplyOrdinaryDamageModifiers(uint creature, int damage, CombatDamageType damageType);

        int GetStaminaDiscount(uint creature, AbilityDetail ability, int cost, bool consume);

        /// <summary>Captures actual HP before a typed direct hit; periodic/triggered/reflected paths never enter.</summary>
        void PrepareDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType);

        void OnDirectDamageApplied();

        void ClearSetup(uint creature);

        void OnStanceChanged(uint creature);

        void ClearBenefits(uint creature);

        void OnEquip();

        void OnUnequip();

        void OnExit();

        void OnDeath();

        void OnPlayerDeath();

        void RefreshAll();
    }
}
