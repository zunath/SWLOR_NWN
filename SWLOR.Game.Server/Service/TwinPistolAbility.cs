using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AnimationService;
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
            var destination = GetDestination(actor, target, targetLocation, spec, out var retreatError);
            if (!string.IsNullOrEmpty(retreatError)) return retreatError;
            if (GetAreaFromLocation(destination) != GetArea(actor)) return "Choose a destination in your current area.";
            var distance = NumericsVector3.Distance(GetPosition(actor), GetPositionFromLocation(destination));
            if (distance < .01f && spec.Movement is TwinPistolMovementType.None or TwinPistolMovementType.Approach)
                return string.Empty;
            return WeaponRepositioning.Validate(actor, GetLocation(actor), destination,
                spec.MinimumMovementMeters, spec.MaxMovementMeters, spec.PassesThroughCreatures);
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
                UsePerkFeat.GetActiveWeaponTraits(actor));
            return true;
        }

        public static void Complete(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec)
        {
            if (!Activations.Remove(actor, out var activation) || activation.Spec != spec) return;
            var ability = Ability.GetActiveAbilityImpactSummary(actor)?.Ability;
            var paidStamina = Combat.GetAbilityStaminaPaid(actor, ability);
            var traits = activation.Traits;
            if (!IsCurrent(actor, activation)) return;
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
            }, TravelAnimation(actor, spec), spec.PassesThroughCreatures);
        }

        /// <summary>
        /// The authored travel clips hold both pistols, so other grips keep the native run.
        /// </summary>
        private static string TravelAnimation(uint actor, TwinPistolAbilitySpec spec)
        {
            if (!AbilityAnimationEquipment.IsCompatible(AnimationEquipmentRequirement.TwinPistol, actor)) return null;
            return spec.Movement switch
            {
                TwinPistolMovementType.Retreat => AuthoredAnimation.BlasterSlideTravel.Name,
                TwinPistolMovementType.FreeDestination => AuthoredAnimation.PistolRushTravel.Name,
                TwinPistolMovementType.Approach => AuthoredAnimation.GrappleShotTravel.Name,
                _ => null
            };
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
                    PairedRangedTraits.CompleteIntrinsicAbility(actor, activation.Traits, channel.AnyHit,
                        channel.PaidStamina, spec.RestoreStaminaOnHit,
                        spec.OrdinaryBonusAttempts, spec.OrdinaryBonusDamagePercent, spec.OrdinaryBonusSeconds);
                    TwinPistolMastery.RecordAbility(actor, channel.AnyHit, 0f, spec, activation.Traits?.Snapshot.MomentumSpent ?? 0);
                }
            }
            finally { Combat.CompleteDeferredAbilityStaminaCostContext(actor, channel.Ability); }
        }

        private static void Resolve(uint actor, Activation activation, WeaponAbilityTraitActivation traits,
            int paidStamina, float distanceTravelled)
        {
            var spec = activation.Spec;
            var limits = new WeaponImpactActivation();
            var anyHit = ResolveVolley(actor, activation, limits, 0);
            if (!GetIsObjectValid(actor) || GetIsDead(actor)) return;
            PairedRangedTraits.CompleteIntrinsicAbility(actor, traits, anyHit,
                paidStamina, spec.RestoreStaminaOnHit, spec.OrdinaryBonusAttempts,
                spec.OrdinaryBonusDamagePercent, spec.OrdinaryBonusSeconds);
            TwinPistolMastery.RecordAbility(actor, anyHit, distanceTravelled, spec, traits?.Snapshot.MomentumSpent ?? 0);
            Ability.PlayAbilityImpactAnimation(actor);
        }

        private static bool ResolveVolley(uint actor, Activation activation, WeaponImpactActivation limits, int volleyIndex)
        {
            var spec = activation.Spec;
            var traits = activation.Traits;
            var anyHit = false;
            var fired = false;
            foreach (var target in activation.Targets)
            {
                if (!IsCurrent(actor, activation)) return anyHit;
                if (!IsLegalTarget(actor, target, spec.ArrivalRangeMeters)) continue;
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
                        playImpactAnimation: false, combatImpactDamageAbility: weapon.DamageAbility,
                        isAreaImpact: spec.MaximumTargets > 1);
                    PairedRangedPresentation.OnAbilityWeaponLaunch(actor, target, weapon.Weapon, handHit);
                }
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

        private static Location GetDestination(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec) =>
            GetDestination(actor, target, targetLocation, spec, out _);

        private static Location GetDestination(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec,
            out string retreatError)
        {
            retreatError = string.Empty;
            var origin = GetPosition(actor);
            if (spec.Movement == TwinPistolMovementType.None) return GetLocation(actor);
            if (spec.Movement == TwinPistolMovementType.FreeDestination)
                return GetAiRushDestination(actor, target, targetLocation, spec) ??
                       GetRushDestination(actor, targetLocation, spec);
            var enemy = GetPosition(target);
            var delta = new NumericsVector3(origin.X - enemy.X, origin.Y - enemy.Y, 0);
            var distance = delta.Length();
            if (spec.Movement == TwinPistolMovementType.Approach && distance <= spec.StopDistanceMeters)
                return GetLocation(actor);
            var direction = distance > .001f ? delta / distance : new NumericsVector3(
                (float)Math.Cos(GetFacing(actor) * Math.PI / 180), (float)Math.Sin(GetFacing(actor) * Math.PI / 180), 0);
            if (spec.Movement == TwinPistolMovementType.Retreat)
                return GetRetreatDestination(actor, target, origin, direction, spec, out retreatError);
            var travel = -Math.Min(spec.MaxMovementMeters, Math.Max(0, distance - spec.StopDistanceMeters));
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

        /// <summary>
        /// The AI casts a free-destination dash at a creature, passing the creature's own location, which the
        /// dash rejects as an occupied endpoint. Non-player casters dash past the creature instead, along the
        /// first candidate line that clears it and passes movement validation; players keep their chosen point.
        /// </summary>
        private static Location GetAiRushDestination(uint actor, uint target, Location targetLocation, TwinPistolAbilitySpec spec)
        {
            if (GetIsPC(actor) || !GetIsObjectValid(target) || target == actor ||
                GetObjectType(target) != ObjectType.Creature || GetArea(target) != GetArea(actor) ||
                NumericsVector3.Distance(GetPositionFromLocation(targetLocation), GetPosition(target)) > 0.5f)
                return null;
            var origin = GetPosition(actor);
            var enemy = GetPosition(target);
            foreach (var endpoint in RushEndpointsPast(new(origin.X, origin.Y), new(enemy.X, enemy.Y),
                         spec.MinimumMovementMeters, spec.MaxMovementMeters, spec.LineWidthMeters))
            {
                var position = new NumericsVector3(endpoint.X, endpoint.Y, origin.Z);
                position.Z = GetGroundHeight(Location(GetArea(actor), position, GetFacing(actor)));
                var destination = Location(GetArea(actor), position, GetFacing(actor));
                if (string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), destination,
                        spec.MinimumMovementMeters, spec.MaxMovementMeters, spec.PassesThroughCreatures)))
                    return destination;
            }
            return null;
        }

        /// <summary>
        /// A dash toward the clicked point: a click past the dash's reach rushes the full distance toward it.
        /// When the dash passes through creatures, an end point on a creature's body shifts along the same line
        /// to the nearest clear spot. Everything else, such as a wall on the line, is left to movement validation.
        /// </summary>
        private static Location GetRushDestination(uint actor, Location targetLocation, TwinPistolAbilitySpec spec)
        {
            if (GetAreaFromLocation(targetLocation) != GetArea(actor)) return targetLocation;
            var origin = GetPosition(actor);
            var clicked = GetPositionFromLocation(targetLocation);
            var flat = new System.Numerics.Vector2(clicked.X - origin.X, clicked.Y - origin.Y);
            var distance = flat.Length();
            if (!float.IsFinite(distance) || distance < .001f) return targetLocation;
            var heading = flat / distance;
            Location first = null;
            foreach (var length in RushEndpointLengths(distance, spec.MinimumMovementMeters, spec.MaxMovementMeters))
            {
                var position = new NumericsVector3(origin.X + heading.X * length, origin.Y + heading.Y * length, origin.Z);
                position.Z = GetGroundHeight(Location(GetArea(actor), position, GetFacing(actor)));
                var destination = Location(GetArea(actor), position, GetFacing(actor));
                first ??= destination;
                if (!spec.PassesThroughCreatures || !WeaponRepositioning.IsEndpointOccupied(actor, position))
                    return destination;
            }
            return first ?? targetLocation;
        }

        private const float RushEndpointStepMeters = .25f;
        private const float RushEndpointSearchMeters = 2f;

        /// <summary>
        /// Dash lengths along the clicked heading, in preference order: the click shortened to the maximum reach,
        /// then alternately a quarter meter farther and nearer, within the movement limits and two meters of it.
        /// A click short of the minimum keeps its own length so movement validation refuses it as before.
        /// </summary>
        public static IEnumerable<float> RushEndpointLengths(float requestedMeters, float minimumMeters, float maximumMeters)
        {
            if (!float.IsFinite(requestedMeters) || requestedMeters <= 0 || maximumMeters < minimumMeters) yield break;
            if (requestedMeters + .001f < minimumMeters)
            {
                yield return requestedMeters;
                yield break;
            }
            var length = Math.Min(requestedMeters, maximumMeters);
            yield return length;
            for (var step = RushEndpointStepMeters; step <= RushEndpointSearchMeters + .001f; step += RushEndpointStepMeters)
            {
                if (length + step <= maximumMeters + .001f) yield return Math.Min(length + step, maximumMeters);
                if (length - step >= minimumMeters - .001f) yield return Math.Max(length - step, minimumMeters);
            }
        }

        /// <summary>
        /// A retreat takes the first lane away from the target that passes movement validation and keeps a line
        /// of fire to the target, so a rock or ally directly behind the caster does not refuse the ability while
        /// open ground lies beside it. With no legal lane, <paramref name="error"/> says why before anything is
        /// paid: the straight retreat's movement error, that every lane ends out of range, or that every open lane loses
        /// the line of fire.
        /// </summary>
        private static Location GetRetreatDestination(uint actor, uint target, NumericsVector3 origin,
            NumericsVector3 direction, TwinPistolAbilitySpec spec, out string error)
        {
            error = string.Empty;
            Location straight = null;
            var anyInRange = false;
            var targetPosition = GetPosition(target);
            foreach (var endpoint in RetreatEndpoints(new(origin.X, origin.Y), new(direction.X, direction.Y),
                         spec.MinimumMovementMeters, spec.MaxMovementMeters))
            {
                var position = new NumericsVector3(endpoint.X, endpoint.Y, origin.Z);
                position.Z = GetGroundHeight(Location(GetArea(actor), position, GetFacing(actor)));
                var destination = Location(GetArea(actor), position, GetFacing(actor));
                straight ??= destination;
                if (NumericsVector3.Distance(position, targetPosition) > spec.ArrivalRangeMeters) continue;
                anyInRange = true;
                // The arrival volley checks line of sight from where the actor stops; a lane behind cover would
                // spend the cast without an attack. The move counts as arrived up to the arrival tolerance short
                // of its end point, so both must see the target. LineOfSightVector tests in the caller's area, so any
                // caller there works, such as the owner whose combat round drives companion AI.
                var travel = new NumericsVector3(position.X - origin.X, position.Y - origin.Y, 0);
                var early = position - travel * (WeaponRepositioning.ArrivalTolerance / travel.Length());
                early.Z = GetGroundHeight(Location(GetArea(actor), early, GetFacing(actor)));
                if (GetArea(OBJECT_SELF) == GetArea(actor) && (!HasLineOfFire(position, targetPosition) || !HasLineOfFire(early, targetPosition)))
                    continue;
                if (string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), destination,
                        spec.MinimumMovementMeters, spec.MaxMovementMeters)))
                    return destination;
            }
            if (straight == null)
            {
                error = "Invalid movement destination.";
                return GetLocation(actor);
            }
            error = WeaponRepositioning.Validate(actor, GetLocation(actor), straight,
                spec.MinimumMovementMeters, spec.MaxMovementMeters);
            if (string.IsNullOrEmpty(error))
                error = anyInRange
                    ? "No retreat path keeps a line of fire to the target."
                    : "The target would be out of range after retreating.";
            return straight;
        }

        private static bool HasLineOfFire(NumericsVector3 from, NumericsVector3 target) =>
            LineOfSightVector(from + new NumericsVector3(0, 0, 1f), target + new NumericsVector3(0, 0, 1f));

        /// <summary>
        /// The native move may report arrival up to 0.35m short, and slide rewards need the minimum distance
        /// actually travelled, so the shortest retreat endpoint keeps this much headroom above the minimum.
        /// </summary>
        private const float RetreatArrivalHeadroomMeters = .5f;

        /// <summary>
        /// Retreat endpoints in preference order: straight away from the target, then lanes turned 30 and 60
        /// degrees to either side; each lane tries the full distance first and shortens one meter at a time,
        /// ending half a meter above the minimum.
        /// </summary>
        public static IEnumerable<System.Numerics.Vector2> RetreatEndpoints(System.Numerics.Vector2 origin,
            System.Numerics.Vector2 awayFromTarget, float minimumMeters, float maximumMeters)
        {
            if (awayFromTarget.LengthSquared() < .000001f || maximumMeters <= 0 || minimumMeters > maximumMeters) yield break;
            var away = System.Numerics.Vector2.Normalize(awayFromTarget);
            var shortest = Math.Min(maximumMeters, minimumMeters + RetreatArrivalHeadroomMeters);
            foreach (var degrees in new[] { 0f, 30f, -30f, 60f, -60f })
            {
                var radians = degrees * MathF.PI / 180f;
                var (sin, cos) = MathF.SinCos(radians);
                var heading = new System.Numerics.Vector2(away.X * cos - away.Y * sin, away.X * sin + away.Y * cos);
                for (var length = maximumMeters; length > shortest + .001f; length -= 1f)
                    yield return origin + heading * length;
                yield return origin + heading * shortest;
            }
        }

        /// <summary>
        /// Dash endpoints, in preference order, whose travel line passes beside <paramref name="target"/> (clear of
        /// both bodies, inside half the line width) and ends past it, so the target is struck without blocking the path.
        /// </summary>
        public static IEnumerable<System.Numerics.Vector2> RushEndpointsPast(System.Numerics.Vector2 origin,
            System.Numerics.Vector2 target, float minimumMeters, float maximumMeters, float lineWidthMeters)
        {
            var toTarget = target - origin;
            var distance = toTarget.Length();
            if (distance < .001f) yield break;
            var forward = toTarget / distance;
            foreach (var clearance in new[] { 1.5f, 1.8f, 1.2f })
            {
                if (clearance >= lineWidthMeters / 2 || clearance >= distance) continue;
                var along = MathF.Sqrt(distance * distance - clearance * clearance);
                foreach (var side in new[] { 1f, -1f })
                {
                    // Rotate the heading so the line's closest approach to the target equals the clearance.
                    var sin = side * clearance / distance;
                    var cos = along / distance;
                    var heading = new System.Numerics.Vector2(forward.X * cos - forward.Y * sin, forward.X * sin + forward.Y * cos);
                    foreach (var beyond in new[] { 1f, 2f, .5f })
                    {
                        var length = along + beyond;
                        if (length >= minimumMeters && length <= maximumMeters)
                            yield return origin + heading * length;
                    }
                }
            }
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
