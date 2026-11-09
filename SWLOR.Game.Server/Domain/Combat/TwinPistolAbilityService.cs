using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
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

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// One paid activation and result across captured weapon impacts. The logic that used to be static in
    /// <c>Service.TwinPistolAbility</c> is moved here unchanged; the per-actor activations and channels are this
    /// instance's own. The paired ranged traits and mastery services are injected; calls into the unconverted
    /// <c>Combat</c>, <c>Ability</c>, <c>StatusEffect</c> and <c>UsePerkFeat</c> code and the native attack cycles sit
    /// behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class TwinPistolAbilityService : ITwinPistolAbilityService
    {
        private readonly Dictionary<uint, Activation> _activations = new();
        private readonly Dictionary<uint, TwinPistolChannel> _channels = new();

        private readonly IPairedRangedTraitsService _traits;
        private readonly ITwinPistolMasteryService _mastery;
        private readonly IAbilityService _ability;
        private readonly IStatusEffectService _statusEffect;
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;

        public TwinPistolAbilityService(
            IPairedRangedTraitsService traits,
            ITwinPistolMasteryService mastery,
            IAbilityService ability,
            IStatusEffectService statusEffect,
            ICombatQueriesService queries,
            ICombatResourcesService resources)
        {
            _traits = traits;
            _mastery = mastery;
            _ability = ability;
            _statusEffect = statusEffect;
            _queries = queries;
            _resources = resources;
        }

        public bool HasActiveActivation(uint actor) =>
            _activations.TryGetValue(actor, out var activation) &&
            GetActiveAbilityActivationId(actor) == activation.ActivationId ||
            _channels.TryGetValue(actor, out var channel) &&
            GetActiveAbilityActivationId(actor) == channel.Activation.ActivationId;

        public uint[] GetEligibleWeapons(uint actor) => new[]
            {
                GetItemInSlot(InventorySlot.RightHand, actor),
                GetItemInSlot(InventorySlot.LeftHand, actor)
            }
            .Where(weapon => CanItemTriggerWeaponAbility(weapon))
            .Distinct().Take(2).ToArray();

        public string Validate(uint actor, uint target, TwinPistolAbilitySpec spec)
        {
            if (GetEligibleWeapons(actor).Length == 0) return "Equip an eligible attack weapon.";
            return IsLegalTarget(actor, target, spec.MaxRange) ? string.Empty : "Choose a hostile target within range and line of fire.";
        }

        public bool Begin(uint actor, uint target, TwinPistolAbilitySpec spec)
        {
            if (!string.IsNullOrEmpty(Validate(actor, target, spec))) return false;
            var activationId = GetActiveAbilityActivationId(actor);
            if (string.IsNullOrWhiteSpace(activationId)) return false;
            _activations[actor] = new Activation(activationId, GetArea(actor), GetPosition(actor),
                GetItemInSlot(InventorySlot.RightHand, actor), GetItemInSlot(InventorySlot.LeftHand, actor),
                GetEligibleWeapons(actor).Select(weapon =>
                    CaptureShotProfile(actor, weapon, AbilityType.Perception)).ToArray(),
                target, spec, GetActiveWeaponTraits(actor));
            return true;
        }

        public void Complete(uint actor, TwinPistolAbilitySpec spec)
        {
            if (!_activations.Remove(actor, out var activation) || activation.Spec != spec) return;
            var ability = GetActiveAbility(actor);
            var paidStamina = GetAbilityStaminaPaid(actor, ability);
            if (!IsCurrent(actor, activation)) return;
            if (spec.VolleyCount > 1)
            {
                BeginChannel(actor, activation, paidStamina);
                return;
            }
            Resolve(actor, activation, ability, paidStamina);
        }

        public void Cancel(uint actor, CombatRecordInterruptCause cause = CombatRecordInterruptCause.Other)
        {
            // Death can dispatch equipment or area cleanup before its dedicated lifecycle handler.
            if (cause == CombatRecordInterruptCause.Other && GetIsObjectValid(actor) &&
                (GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0))
                cause = CombatRecordInterruptCause.Died;
            if (_channels.TryGetValue(actor, out var channel))
            {
                FinishChannel(actor, channel);
                InterruptAbilityActivation(actor, channel.Activation.ActivationId, cause);
            }
            if (_activations.Remove(actor, out var activation) && !string.IsNullOrWhiteSpace(activation.ActivationId))
                InterruptAbilityActivation(actor, activation.ActivationId, cause);
        }

        /// <summary>
        /// A destroyed owner (a dismissed droid) never reaches its delayed completion, and its object ID may be
        /// reused; drop its cached activation and channel, and release the channel's deferred stamina-cost
        /// context, without touching the destroyed object.
        /// </summary>
        public void ForgetDestroyed(uint actor)
        {
            if (_channels.Remove(actor, out var channel))
                CompleteDeferredAbilityStaminaCostContext(actor, channel.Ability);
            _activations.Remove(actor);
        }

        public void InterruptChannel(uint actor)
        {
            if (_channels.TryGetValue(actor, out var channel)) FinishChannel(actor, channel);
        }

        private void BeginChannel(uint actor, Activation activation, int paidStamina)
        {
            var ability = GetActiveAbility(actor);
            var channel = new TwinPistolChannel(activation, ability, paidStamina);
            _channels[actor] = channel;
            DeferAbilityStaminaCostContext(actor, ability);
            CancelPairedRangedAttackCycle(actor);
            CancelPendingHand(actor);
            var resolve = CaptureRepeatedAbilityImpact(actor, index =>
            {
                channel.AnyHit |= ResolveVolley(actor, activation, channel.Limits, index);
            }, activation.Spec.FlatPower + activation.Weapons.Max(weapon => weapon.Damage));

            void Fire(int index)
            {
                if (!_channels.TryGetValue(actor, out var current) || !ReferenceEquals(current, channel)) return;
                if (!IsCurrent(actor, activation) ||
                    !CanContinueChannel(actor) ||
                    GetActiveAbilityActivationId(actor) != activation.ActivationId ||
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

        private bool CanContinueChannel(uint actor)
        {
            if (!GetCommandable(actor) || HasStatusEffectCategory(actor, StatusEffectCategory.Incapacitating))
                return false;
            for (var effect = GetFirstEffect(actor); GetIsEffectValid(effect); effect = GetNextEffect(actor))
                if (GetEffectType(effect) is EffectTypeScript.Paralyze or EffectTypeScript.CutsceneParalyze or
                    EffectTypeScript.Stunned or EffectTypeScript.Sleep or EffectTypeScript.Petrify or EffectTypeScript.Dazed)
                    return false;
            return true;
        }

        private void FinishChannel(uint actor, TwinPistolChannel channel)
        {
            if (!_channels.TryGetValue(actor, out var current) || !ReferenceEquals(current, channel)) return;
            _channels.Remove(actor);
            try
            {
                if (GetIsObjectValid(actor) && !GetIsDead(actor))
                    CompleteIntrinsic(actor, channel.Activation, channel.Ability, channel.AnyHit, channel.PaidStamina);
            }
            finally { CompleteDeferredAbilityStaminaCostContext(actor, channel.Ability); }
        }

        private void Resolve(uint actor, Activation activation, AbilityDetail ability, int paidStamina)
        {
            var anyHit = ResolveVolley(actor, activation, new WeaponImpactActivation(), 0);
            if (!GetIsObjectValid(actor) || GetIsDead(actor)) return;
            CompleteIntrinsic(actor, activation, ability, anyHit, paidStamina);
            PlayAbilityImpactAnimation(actor);
        }

        private void CompleteIntrinsic(uint actor, Activation activation, AbilityDetail ability,
            bool anyHit, int paidStamina)
        {
            var spec = activation.Spec;
            _traits.CompleteIntrinsicAbility(actor, activation.Traits, anyHit, paidStamina,
                spec.RestoreStaminaOnHit, spec.OrdinaryBonusAttempts, spec.OrdinaryBonusDamagePercent,
                spec.OrdinaryBonusSeconds);
            _mastery.RecordAbility(actor, anyHit, ability?.EffectiveLevelPerkType ?? PerkType.Invalid,
                activation.Traits?.Snapshot.CadenceSpent ?? 0);
        }

        private bool ResolveVolley(uint actor, Activation activation, WeaponImpactActivation limits, int volleyIndex)
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
                ApplyCombatImpact(actor, target, GetLocation(target),
                    spec.FlatPower, weapon.DamageType, _ => { anyHit = true; handHit = true; }, weapon.DamageAbility);
                OnAbilityWeaponLaunch(actor, target, weapon.Weapon, handHit);
            }
            // Paired equipment already owns the uninterrupted channel clip. Other grips
            // need one native gesture per volley, inside this wave's captured ability context.
            var ability = GetActiveAbility(actor);
            if (spec.VolleyCount > 1 && fired && GetIsObjectValid(actor) && !GetIsDead(actor) &&
                ability != null && !IsAnimationEquipmentCompatible(ability.AnimationEquipmentRequirement, actor))
                PlayAbilityImpactAnimation(actor);
            return anyHit;
        }

        private bool IsCurrent(uint actor, Activation activation) =>
            GetIsObjectValid(actor) && !GetIsDead(actor) && GetCurrentHitPoints(actor) > 0 &&
            GetArea(actor) == activation.Area &&
            GetItemInSlot(InventorySlot.RightHand, actor) == activation.MainHand &&
            GetItemInSlot(InventorySlot.LeftHand, actor) == activation.OffHand &&
            activation.Weapons.Length > 0 && activation.Weapons.All(weapon => weapon.IsUnchanged());

        private bool IsLegalTarget(uint actor, uint target, float range) =>
            GetIsObjectValid(target) && target != actor && !GetIsDead(target) && GetCurrentHitPoints(target) > 0 &&
            GetArea(actor) == GetArea(target) && GetIsReactionTypeHostile(target, actor) &&
            GetDistanceBetween(actor, target) <= range && LineOfSightObject(actor, target);

        /// <summary>The unconverted <c>UsePerkFeat</c> activation registry.</summary>
        protected virtual string GetActiveAbilityActivationId(uint actor) => UsePerkFeat.GetActiveAbilityActivationId(actor);

        /// <summary>The unconverted <c>UsePerkFeat</c> activation registry.</summary>
        protected virtual WeaponAbilityTraitActivation GetActiveWeaponTraits(uint actor) => UsePerkFeat.GetActiveWeaponTraits(actor);

        /// <summary>The unconverted <c>UsePerkFeat</c> activation registry.</summary>
        protected virtual bool InterruptAbilityActivation(uint actor, string activationId, CombatRecordInterruptCause cause) =>
            UsePerkFeat.InterruptAbilityActivation(actor, activationId, cause);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual bool CanItemTriggerWeaponAbility(uint item) =>
            _queries.CanItemTriggerWeaponAbility(item, SkillType.TwinPistol);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int GetAbilityStaminaPaid(uint actor, AbilityDetail ability) =>
            _resources.GetAbilityStaminaPaid(actor, ability);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual void DeferAbilityStaminaCostContext(uint actor, AbilityDetail ability) =>
            _resources.DeferAbilityStaminaCostContext(actor, ability);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual void CompleteDeferredAbilityStaminaCostContext(uint actor, AbilityDetail ability) =>
            _resources.CompleteDeferredAbilityStaminaCostContext(actor, ability);

        /// <summary>The ability of the actor's active ability impact, from the unconverted <c>Ability</c> service.</summary>
        protected virtual AbilityDetail GetActiveAbility(uint actor) =>
            _ability.GetActiveAbilityImpactSummary(actor)?.Ability;

        /// <summary>The unconverted <c>Ability</c> service.</summary>
        protected virtual Action<int> CaptureRepeatedAbilityImpact(uint actor, Action<int> impactAction, int baseDamage) =>
            _ability.CaptureRepeatedAbilityImpact<int>(actor, impactAction, baseDamage, retainCapturedBonuses: true);

        /// <summary>The unconverted <c>Ability</c> service.</summary>
        protected virtual void PlayAbilityImpactAnimation(uint actor) => _ability.PlayAbilityImpactAnimation(actor);

        /// <summary>The unconverted <c>Ability</c> service.</summary>
        protected virtual int ApplyCombatImpact(uint actor, uint target, Location targetLocation,
            int baseDamage, CombatDamageType damageType, Action<uint> afterSuccessfulHit, AbilityType damageAbility) =>
            _ability.ApplyCombatImpact(actor, target, targetLocation, SkillType.TwinPistol,
                baseDamage, 0, null, false,
                damageType: damageType, afterSuccessfulHit: afterSuccessfulHit,
                playImpactAnimation: false, combatImpactDamageAbility: damageAbility);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual bool HasStatusEffectCategory(uint actor, StatusEffectCategory category) =>
            _statusEffect.HasStatusEffectCategory(actor, category);

        /// <summary>The native paired ranged attack cycle.</summary>
        protected virtual void CancelPairedRangedAttackCycle(uint actor) => PairedRangedAttackCycle.Cancel(actor);

        /// <summary>The native weapon attack cycle.</summary>
        protected virtual void CancelPendingHand(uint actor) => WeaponAttackCycle.CancelPendingHand(actor);

        /// <summary>The native paired ranged presentation.</summary>
        protected virtual void OnAbilityWeaponLaunch(uint actor, uint target, uint weapon, bool hit) =>
            PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, weapon, hit);

        /// <summary>The unconverted animation equipment rules.</summary>
        protected virtual bool IsAnimationEquipmentCompatible(AnimationEquipmentRequirement requirement, uint actor) =>
            AbilityAnimationEquipment.IsCompatible(requirement, actor);

        /// <summary>The unconverted weapon shot profile capture.</summary>
        protected virtual WeaponShotProfile CaptureShotProfile(uint actor, uint weapon, AbilityType damageAbilityOverride) =>
            WeaponShotProfile.Capture(actor, weapon, SkillType.TwinPistol, damageAbilityOverride);
    }
}
