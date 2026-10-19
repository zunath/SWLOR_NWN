using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
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

        public static string Validate(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec)
        {
            if (GetEligibleWeapons(actor).Length == 0) return "Equip an eligible attack weapon.";
            if (WeaponRepositioning.IsMoving(actor)) return "Finish your current movement first.";
            if (spec.Movement != TwinPistolMovementType.FreeDestination &&
                (!IsLegalTarget(actor, target, spec.MaxRange))) return "Choose a hostile target within range and line of fire.";
            var destination = GetDestination(actor, target, targetLocation, spec);
            if (GetAreaFromLocation(destination) != GetArea(actor)) return "Choose a destination in your current area.";
            var distance = NumericsVector3.Distance(GetPosition(actor), GetPositionFromLocation(destination));
            if (distance < .01f && spec.Movement is TwinPistolMovementType.None or TwinPistolMovementType.Approach)
                return string.Empty;
            return WeaponRepositioning.Validate(actor, GetLocation(actor), destination,
                spec.MinimumMovementMeters, spec.MaxMovementMeters);
        }

        public static bool Begin(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec)
        {
            if (!string.IsNullOrEmpty(Validate(actor, target, targetLocation, spec))) return false;
            var activationId = UsePerkFeat.GetActiveAbilityActivationId(actor);
            if (string.IsNullOrWhiteSpace(activationId)) return false;
            var origin = GetPosition(actor);
            var destination = GetDestination(actor, target, targetLocation, spec);
            var targets = spec.Movement == TwinPistolMovementType.FreeDestination
                ? CaptureLineTargets(actor, origin, GetPositionFromLocation(destination), spec)
                : new[] { target };
            Activations[actor] = new Activation(activationId, GetArea(actor), origin, destination,
                GetItemInSlot(InventorySlot.RightHand, actor), GetItemInSlot(InventorySlot.LeftHand, actor),
                GetEligibleWeapons(actor).Select(weapon =>
                    WeaponShotProfile.Capture(actor, weapon, SkillType.TwinPistol, AbilityType.Perception)).ToArray(),
                targets, spec.Movement == TwinPistolMovementType.FreeDestination ? GetAttackTarget(actor) : target, spec,
                PairedRangedTraits.CaptureAbility(actor, spec.IsCrossfire, spec.SpendsMomentum));
            return true;
        }

        public static void Complete(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec)
        {
            if (!Activations.Remove(actor, out var activation) || activation.Spec != spec) return;
            var ability = Ability.GetActiveAbilityImpactSummary(actor)?.Ability;
            var paidStamina = Combat.GetAbilityStaminaPaid(actor, ability);
            var traits = activation.Traits;
            if (!IsCurrent(actor, activation) || !PairedRangedTraits.CommitAbility(actor, traits)) return;
            if (spec.VolleyCount > 1)
            {
                BeginChannel(actor, activation, paidStamina);
                return;
            }
            var movement = NumericsVector3.Distance(activation.Origin, GetPositionFromLocation(activation.Destination));
            if (movement < .01f)
            {
                Resolve(actor, activation, traits, paidStamina, 0f);
                return;
            }

            // The native move action enforces collision throughout travel. Preserve the paid
            // cast context until arrival rather than teleporting through a prechecked obstacle.
            Combat.DeferAbilityStaminaCostContext(actor, ability);
            var distanceTravelled = 0f;
            var resolve = Ability.CaptureRepeatedAbilityImpact(actor,
                () => Resolve(actor, activation, traits, paidStamina, distanceTravelled),
                spec.FlatPower + activation.Weapons.Max(weapon => weapon.Damage));
            PairedRangedAttackCycle.Cancel(actor);
            WeaponRepositioning.Begin(actor, activation.Destination, (arrived, distance) =>
            {
                try
                {
                    if (!arrived || !IsCurrent(actor, activation)) return;
                    distanceTravelled = distance;
                    resolve();
                    UsePerkFeat.ResumeAttackAfterReposition(actor, activation.ResumeTarget);
                }
                finally { Combat.CompleteDeferredAbilityStaminaCostContext(actor, ability); }
            });
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
            WeaponRepositioning.Cancel(actor);
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
                    !IsLegalTarget(actor, activation.Targets[0], activation.Spec.MaxRange))
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
                {
                    var activation = channel.Activation;
                    var spec = activation.Spec;
                    PairedRangedTraits.CompleteAbility(actor, activation.Traits, channel.AnyHit, 0f,
                        spec.GeneratesMomentum, channel.PaidStamina, spec.RestoreStaminaOnHit,
                        spec.OrdinaryBonusAttempts, spec.OrdinaryBonusDamagePercent, spec.OrdinaryBonusSeconds);
                    TwinPistolMastery.RecordAbility(actor, channel.AnyHit, 0f, spec, activation.Traits.MomentumSpent);
                }
            }
            finally { Combat.CompleteDeferredAbilityStaminaCostContext(actor, channel.Ability); }
        }

        private static void Resolve(uint actor, Activation activation, PairedRangedAbilityTraitsSnapshot traits,
            int paidStamina, float distanceTravelled)
        {
            var spec = activation.Spec;
            var limits = new WeaponImpactActivation();
            var anyHit = ResolveVolley(actor, activation, limits, 0);
            if (!GetIsObjectValid(actor) || GetIsDead(actor)) return;
            PairedRangedTraits.CompleteAbility(actor, traits, anyHit, distanceTravelled, spec.GeneratesMomentum,
                paidStamina, spec.RestoreStaminaOnHit, spec.OrdinaryBonusAttempts,
                spec.OrdinaryBonusDamagePercent, spec.OrdinaryBonusSeconds);
            TwinPistolMastery.RecordAbility(actor, anyHit, distanceTravelled, spec, traits.MomentumSpent);
            Ability.PlayAbilityImpactAnimation(actor);
        }

        private static bool ResolveVolley(uint actor, Activation activation, WeaponImpactActivation limits, int volleyIndex)
        {
            var spec = activation.Spec;
            var traits = activation.Traits;
            var anyHit = false;
            foreach (var target in activation.Targets)
            {
                if (!IsCurrent(actor, activation)) return anyHit;
                if (!IsLegalTarget(actor, target, spec.ArrivalRangeMeters)) continue;
                foreach (var weapon in activation.Weapons)
                {
                    if (!IsCurrent(actor, activation)) return anyHit;
                    if (!GetIsObjectValid(target) || GetIsDead(target)) break;
                    var scaledWeapon = weapon with { Damage = weapon.Damage * spec.WeaponPowerPercent / 100 };
                    var offhandAccuracy = weapon.Weapon == activation.OffHand
                        ? Stat.GetStatAdjustment(actor, StatType.PairedRangedCompanionAccuracy) : 0;
                    using var source = new WeaponImpactProfile(actor, scaledWeapon, limits,
                        traits.AccuracyAdjustment + offhandAccuracy, volleyIndex, spec.VolleyCount);
                    var handHit = false;
                    Ability.ApplyCombatImpact(actor, target, GetLocation(target), SkillType.TwinPistol,
                        spec.FlatPower + traits.MomentumSpent * spec.MomentumPowerPerCharge, 0, null, false,
                        damageType: weapon.DamageType, afterSuccessfulHit: _ => { anyHit = true; handHit = true; },
                        playImpactAnimation: false, combatImpactDamageAbility: weapon.DamageAbility,
                        isAreaImpact: spec.MaximumTargets > 1);
                    PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, weapon.Weapon, handHit);
                }
            }
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

        private static Location GetDestination(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec)
        {
            var origin = GetPosition(actor);
            if (spec.Movement == TwinPistolMovementType.None) return GetLocation(actor);
            if (spec.Movement == TwinPistolMovementType.FreeDestination) return targetLocation;
            var enemy = GetPosition(target);
            var delta = new NumericsVector3(origin.X - enemy.X, origin.Y - enemy.Y, 0);
            var distance = delta.Length();
            if (spec.Movement == TwinPistolMovementType.Approach && distance <= spec.StopDistanceMeters)
                return GetLocation(actor);
            var direction = distance > .001f ? delta / distance : new NumericsVector3(
                (float)Math.Cos(GetFacing(actor) * Math.PI / 180), (float)Math.Sin(GetFacing(actor) * Math.PI / 180), 0);
            var travel = spec.Movement == TwinPistolMovementType.Retreat ? spec.MaxMovementMeters
                : -Math.Min(spec.MaxMovementMeters, Math.Max(0, distance - spec.StopDistanceMeters));
            var position = origin + direction * travel;
            var destination = Location(GetArea(actor), position, GetFacing(actor));
            position.Z = GetGroundHeight(destination);
            return Location(GetArea(actor), position, GetFacing(actor));
        }

        private static uint[] CaptureLineTargets(uint actor, NumericsVector3 origin, NumericsVector3 destination,
            TwinPistolAbilitySpec spec)
        {
            var targets = new List<uint>();
            for (var target = GetFirstObjectInArea(GetArea(actor), ObjectType.Creature); GetIsObjectValid(target);
                 target = GetNextObjectInArea(GetArea(actor), ObjectType.Creature))
                if (IsLegalTarget(actor, target, 25f) && IsInTravelLine(GetPosition(target), origin, destination, spec.LineWidthMeters))
                    targets.Add(target);
            return targets.OrderBy(target => NumericsVector3.DistanceSquared(origin, GetPosition(target)))
                .Take(spec.MaximumTargets).ToArray();
        }

        public static bool IsInTravelLine(NumericsVector3 point, NumericsVector3 origin, NumericsVector3 destination, float width)
        {
            var segment = new NumericsVector3(destination.X - origin.X, destination.Y - origin.Y, 0);
            var relative = new NumericsVector3(point.X - origin.X, point.Y - origin.Y, 0);
            var length = segment.Length();
            if (length < .001f || width <= 0) return false;
            var along = NumericsVector3.Dot(relative, segment / length);
            var across = Math.Abs(relative.X * segment.Y - relative.Y * segment.X) / length;
            return along >= 0 && along <= length && across <= width / 2 && Math.Abs(point.Z - origin.Z) <= 2;
        }
    }
}
