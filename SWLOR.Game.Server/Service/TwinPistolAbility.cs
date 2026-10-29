using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using NumericsVector3 = System.Numerics.Vector3;
using Activation = SWLOR.Game.Server.Service.CombatService.TwinPistolAbilityActivation;

namespace SWLOR.Game.Server.Service
{
    /// <summary>One paid activation and result across captured weapon impacts.</summary>
    public static class TwinPistolAbility
    {
        private static readonly Dictionary<uint, Activation> Activations = new();
        private static readonly Dictionary<uint, TwinPistolChannel> Channels = new();

        public static bool HasActiveActivation(uint actor) =>
            Activations.TryGetValue(actor, out var activation) &&
            UsePerkFeat.GetActiveAbilityActivationId(actor) == activation.ActivationId ||
            Channels.TryGetValue(actor, out var channel) &&
            UsePerkFeat.GetActiveAbilityActivationId(actor) == channel.Activation.ActivationId;

        public static uint[] GetEligibleWeapons(uint actor) => new[]
            {
                GetItemInSlot(InventorySlot.RightHand, actor),
                GetItemInSlot(InventorySlot.LeftHand, actor)
            }
            .Where(weapon => Combat.CanItemTriggerWeaponAbility(weapon, SkillType.TwinPistol))
            .Distinct().Take(2).ToArray();

        public static string Validate(uint actor, uint target, TwinPistolAbilitySpec spec)
        {
            if (GetEligibleWeapons(actor).Length == 0) return "Equip an eligible attack weapon.";
            return IsLegalTarget(actor, target, spec.MaxRange) ? string.Empty : "Choose a hostile target within range and line of fire.";
        }

        public static bool Begin(uint actor, uint target, TwinPistolAbilitySpec spec)
        {
            if (!string.IsNullOrEmpty(Validate(actor, target, spec))) return false;
            var activationId = UsePerkFeat.GetActiveAbilityActivationId(actor);
            if (string.IsNullOrWhiteSpace(activationId)) return false;
            Activations[actor] = new Activation(activationId, GetArea(actor), GetPosition(actor),
                GetItemInSlot(InventorySlot.RightHand, actor), GetItemInSlot(InventorySlot.LeftHand, actor),
                GetEligibleWeapons(actor).Select(weapon =>
                    WeaponShotProfile.Capture(actor, weapon, SkillType.TwinPistol, AbilityType.Perception)).ToArray(),
                target, spec, UsePerkFeat.GetActiveWeaponTraits(actor));
            return true;
        }

        public static void Complete(uint actor, TwinPistolAbilitySpec spec)
        {
            if (!Activations.Remove(actor, out var activation) || activation.Spec != spec) return;
            var ability = Ability.GetActiveAbilityImpactSummary(actor)?.Ability;
            var paidStamina = Combat.GetAbilityStaminaPaid(actor, ability);
            if (!IsCurrent(actor, activation)) return;
            if (spec.VolleyCount > 1)
            {
                BeginChannel(actor, activation, paidStamina);
                return;
            }
            Resolve(actor, activation, ability, paidStamina);
        }

        public static void Cancel(uint actor, CombatRecordInterruptCause cause = CombatRecordInterruptCause.Other)
        {
            // Death can dispatch equipment or area cleanup before its dedicated lifecycle handler.
            if (cause == CombatRecordInterruptCause.Other && GetIsObjectValid(actor) &&
                (GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0))
                cause = CombatRecordInterruptCause.Died;
            if (Channels.TryGetValue(actor, out var channel))
            {
                FinishChannel(actor, channel);
                UsePerkFeat.InterruptAbilityActivation(actor, channel.Activation.ActivationId, cause);
            }
            if (Activations.Remove(actor, out var activation) && !string.IsNullOrWhiteSpace(activation.ActivationId))
                UsePerkFeat.InterruptAbilityActivation(actor, activation.ActivationId, cause);
        }

        /// <summary>
        /// A destroyed owner (a dismissed droid) never reaches its delayed completion, and its object ID may be
        /// reused; drop its cached activation and channel, and release the channel's deferred stamina-cost
        /// context, without touching the destroyed object.
        /// </summary>
        public static void ForgetDestroyed(uint actor)
        {
            if (Channels.Remove(actor, out var channel))
                Combat.CompleteDeferredAbilityStaminaCostContext(actor, channel.Ability);
            Activations.Remove(actor);
        }

        public static void InterruptChannel(uint actor)
        {
            if (Channels.TryGetValue(actor, out var channel)) FinishChannel(actor, channel);
        }

        private static void BeginChannel(uint actor, Activation activation, int paidStamina)
        {
            var ability = Ability.GetActiveAbilityImpactSummary(actor)?.Ability;
            var channel = new TwinPistolChannel(activation, ability, paidStamina);
            Channels[actor] = channel;
            Combat.DeferAbilityStaminaCostContext(actor, ability);
            PairedRangedAttackCycle.Cancel(actor);
            WeaponAttackCycle.CancelPendingHand(actor);
            var resolve = Ability.CaptureRepeatedAbilityImpact<int>(actor, index =>
            {
                channel.AnyHit |= ResolveVolley(actor, activation, channel.Limits, index);
            }, activation.Spec.FlatPower + activation.Weapons.Max(weapon => weapon.Damage), retainCapturedBonuses: true);

            void Fire(int index)
            {
                if (!Channels.TryGetValue(actor, out var current) || !ReferenceEquals(current, channel)) return;
                if (!IsCurrent(actor, activation) ||
                    !CanContinueChannel(actor) ||
                    UsePerkFeat.GetActiveAbilityActivationId(actor) != activation.ActivationId ||
                    NumericsVector3.Distance(GetPosition(actor), activation.Origin) > .01f ||
                    !IsLegalTarget(actor, activation.Target, activation.Spec.MaxRange))
                {
                    Cancel(actor);
                    return;
                }
                try
                {
                    if (index == 0)
                        channel.AnyHit |= ResolveVolley(actor, activation, channel.Limits, index);
                    else
                        resolve(index);
                    if (index == activation.Spec.VolleyCount - 1) FinishChannel(actor, channel);
                }
                catch
                {
                    Cancel(actor);
                    throw;
                }
            }

            Fire(0);
            for (var index = 1; index < activation.Spec.VolleyCount; index++)
            {
                var volley = index;
                DelayCommand(index * activation.Spec.VolleyIntervalSeconds, () => Fire(volley));
            }
        }

        private static bool CanContinueChannel(uint actor)
        {
            if (!GetCommandable(actor) || StatusEffect.HasStatusEffectCategory(actor, StatusEffectCategory.Incapacitating))
                return false;
            for (var effect = GetFirstEffect(actor); GetIsEffectValid(effect); effect = GetNextEffect(actor))
                if (GetEffectType(effect) is EffectTypeScript.Paralyze or EffectTypeScript.CutsceneParalyze or
                    EffectTypeScript.Stunned or EffectTypeScript.Sleep or EffectTypeScript.Petrify or EffectTypeScript.Dazed)
                    return false;
            return true;
        }

        private static void FinishChannel(uint actor, TwinPistolChannel channel)
        {
            if (!Channels.TryGetValue(actor, out var current) || !ReferenceEquals(current, channel)) return;
            Channels.Remove(actor);
            try
            {
                if (GetIsObjectValid(actor) && !GetIsDead(actor))
                    CompleteIntrinsic(actor, channel.Activation, channel.Ability, channel.AnyHit, channel.PaidStamina);
            }
            finally { Combat.CompleteDeferredAbilityStaminaCostContext(actor, channel.Ability); }
        }

        private static void Resolve(uint actor, Activation activation, AbilityService.AbilityDetail ability, int paidStamina)
        {
            var anyHit = ResolveVolley(actor, activation, new WeaponImpactActivation(), 0);
            if (!GetIsObjectValid(actor) || GetIsDead(actor)) return;
            CompleteIntrinsic(actor, activation, ability, anyHit, paidStamina);
            Ability.PlayAbilityImpactAnimation(actor);
        }

        private static void CompleteIntrinsic(uint actor, Activation activation, AbilityService.AbilityDetail ability,
            bool anyHit, int paidStamina)
        {
            var spec = activation.Spec;
            PairedRangedTraits.CompleteIntrinsicAbility(actor, activation.Traits, anyHit, paidStamina,
                spec.RestoreStaminaOnHit, spec.OrdinaryBonusAttempts, spec.OrdinaryBonusDamagePercent,
                spec.OrdinaryBonusSeconds);
            TwinPistolMastery.RecordAbility(actor, anyHit, ability?.EffectiveLevelPerkType ?? PerkType.Invalid,
                activation.Traits?.Snapshot.CadenceSpent ?? 0);
        }

        private static bool ResolveVolley(uint actor, Activation activation, WeaponImpactActivation limits, int volleyIndex)
        {
            var spec = activation.Spec;
            var target = activation.Target;
            var anyHit = false;
            var fired = false;
            if (!IsCurrent(actor, activation) || !IsLegalTarget(actor, target, spec.MaxRange)) return false;
            foreach (var weapon in activation.Weapons)
            {
                if (!IsCurrent(actor, activation)) return anyHit;
                if (!GetIsObjectValid(target) || GetIsDead(target)) break;
                var scaledWeapon = weapon with { Damage = weapon.Damage * spec.WeaponPowerPercent / 100 };
                using var source = new WeaponImpactProfile(actor, scaledWeapon, limits,
                    volleyIndex: volleyIndex, volleyCount: spec.VolleyCount);
                var handHit = false;
                fired = true;
                Ability.ApplyCombatImpact(actor, target, GetLocation(target), SkillType.TwinPistol,
                    spec.FlatPower, 0, null, false,
                    damageType: weapon.DamageType, afterSuccessfulHit: _ => { anyHit = true; handHit = true; },
                    playImpactAnimation: false, combatImpactDamageAbility: weapon.DamageAbility);
                PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, weapon.Weapon, handHit);
            }
            // Paired equipment already owns the uninterrupted channel clip. Other grips
            // need one native gesture per volley, inside this wave's captured ability context.
            var ability = Ability.GetActiveAbilityImpactSummary(actor)?.Ability;
            if (spec.VolleyCount > 1 && fired && GetIsObjectValid(actor) && !GetIsDead(actor) &&
                ability != null && !AbilityAnimationEquipment.IsCompatible(ability.AnimationEquipmentRequirement, actor))
                Ability.PlayAbilityImpactAnimation(actor);
            return anyHit;
        }

        private static bool IsCurrent(uint actor, Activation activation) =>
            GetIsObjectValid(actor) && !GetIsDead(actor) && GetCurrentHitPoints(actor) > 0 &&
            GetArea(actor) == activation.Area &&
            GetItemInSlot(InventorySlot.RightHand, actor) == activation.MainHand &&
            GetItemInSlot(InventorySlot.LeftHand, actor) == activation.OffHand &&
            activation.Weapons.Length > 0 && activation.Weapons.All(weapon => weapon.IsUnchanged());

        private static bool IsLegalTarget(uint actor, uint target, float range) =>
            GetIsObjectValid(target) && target != actor && !GetIsDead(target) && GetCurrentHitPoints(target) > 0 &&
            GetArea(actor) == GetArea(target) && GetIsReactionTypeHostile(target, actor) &&
            GetDistanceBetween(actor, target) <= range && LineOfSightObject(actor, target);
    }
}
