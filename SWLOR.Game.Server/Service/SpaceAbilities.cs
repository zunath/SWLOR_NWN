using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What a space ability (a Piloting style perk, plan section 6) calls to act from its ship.
    ///
    /// An ability is a shared ability definition marked <c>.CanBeUsedInSpace()</c>. It pays STM and recast like
    /// any ability (the pilot's own stamina), has no activation delay, resolves its target from the cursor or the
    /// ship's selected target, and fires through <see cref="FireWeapon"/> or <see cref="FireWeaponArea"/> from its
    /// impact action. Weapon DMG in an ability's text is the DMG of the module that fires.
    /// </summary>
    public static class SpaceAbilities
    {
        /// <summary>How long a pending rider waits for a landed weapon shot, as a queued ground ability does.</summary>
        public const float RiderSeconds = 30f;

        private static readonly Dictionary<string, Action<SpaceRiderContext>> _riderHandlers = new();

        // Targets

        /// <summary>
        /// The ship's selected target, or invalid when none is selected, it is dead, or it left the area.
        /// </summary>
        public static uint GetSelectedTarget(uint ship) => GetSelectedTarget(ship, ship);

        /// <summary>
        /// The target <paramref name="selector"/> selected (the pilot, or a crew member at one of the ship's
        /// stations, who picks their own), seen from the ship: invalid when none is selected, it is dead, it left the
        /// ship's area or it is hidden from the ship.
        /// </summary>
        public static uint GetSelectedTarget(uint ship, uint selector)
        {
            var target = GetLocalObject(selector, "SPACE_TARGET");
            return GetIsObjectValid(target) && !GetIsDead(target) && GetArea(target) == GetArea(ship) &&
                   !Space.IsShipHiddenFrom(ship, target)
                ? target
                : OBJECT_INVALID;
        }

        // Weapon choice

        private sealed class FittedWeapon
        {
            public ShipFittedModule Fitted { get; init; }
            public ShipStatus.ShipStatusModule Module { get; init; }
            public ShipModuleDetail Detail { get; init; }
            public int Slot { get; init; }
        }

        private static ShipStatus.ShipStatusModule FindModule(ShipStatus status, ShipFittedModule fitted)
        {
            var row = fitted.Row switch
            {
                ShipModulePowerType.High => status.HighPowerModules,
                ShipModulePowerType.Mid => status.MidPowerModules,
                _ => status.LowPowerModules,
            };

            return row.TryGetValue(fitted.Slot, out var module) ? module : null;
        }

        /// <summary>The fitted weapon modules of a ship that can fire at ships, with their hotbar slots.</summary>
        private static List<FittedWeapon> GetFittedWeapons(ShipStatus status)
        {
            var weapons = new List<FittedWeapon>();
            foreach (var fitted in Space.GetFittedRevampModules(status))
            {
                var step = fitted.Step;
                if (step.Activation == ShipModuleActivation.Invalid ||
                    step.WeaponFamily == 0 ||
                    !ShipModuleActions.IsShipWeapon(step.Family))
                {
                    continue;
                }

                var module = FindModule(status, fitted);
                var detail = module != null && Space.IsRegisteredShipModule(module.ItemTag)
                    ? Space.GetShipModuleDetailByItemTag(module.ItemTag)
                    : null;
                if (detail == null)
                    continue;

                weapons.Add(new FittedWeapon
                {
                    Fitted = fitted,
                    Module = module,
                    Detail = detail,
                    Slot = ShipStatCalculator.GetHotbarSlot(fitted.Row, fitted.Slot),
                });
            }

            return weapons;
        }

        private static List<SpaceWeaponCandidate> BuildCandidates(
            uint ship,
            ShipStatus status,
            List<FittedWeapon> weapons,
            uint target,
            ShipStatus targetStatus)
        {
            return weapons.Select(weapon =>
            {
                // The pilot's abilities fire Helm slots only; crew stations fire for their crew (plan section 10).
                var usable = SpaceCrew.CanActingSeatFire(ship, status, weapon.Slot);

                // The module's own checks (ammo, a launcher too large for the target) decide whether it can fire at the target.
                if (usable && GetIsObjectValid(target) && targetStatus != null)
                {
                    usable = string.IsNullOrWhiteSpace(weapon.Detail.ModuleValidationAction?.Invoke(
                        ship, status, target, targetStatus, weapon.Module.ModuleBonus));
                }

                var range = weapon.Detail.ModuleMaxDistanceAction == null || !GetIsObjectValid(target) || targetStatus == null
                    ? weapon.Fitted.Step.Range
                    : weapon.Detail.ModuleMaxDistanceAction(ship, status, target, targetStatus, weapon.Module.ModuleBonus);

                return new SpaceWeaponCandidate
                {
                    Slot = weapon.Slot,
                    Rank = weapon.Fitted.Step.Rank,
                    Family = weapon.Fitted.Step.WeaponFamily,
                    Range = range,
                    Usable = usable,
                    Source = weapon,
                };
            }).ToList();
        }

        /// <summary>
        /// The reach of the longest usable weapon of the ship, for the ability range check when an ability has no
        /// range of its own. Zero when the ship has no usable weapon.
        /// </summary>
        public static float GetLongestWeaponRange(uint ship, SpaceWeaponFilter filter = SpaceWeaponFilter.Any)
        {
            var status = Space.GetShipStatus(ship);
            if (status == null)
                return 0f;

            var candidates = BuildCandidates(ship, status, GetFittedWeapons(status), OBJECT_INVALID, null);
            return SpaceWeaponPicker.LongestRange(candidates, filter);
        }

        /// <summary>
        /// Why a ship cannot fire a weapon of this family at the target, or an empty string when it can. Abilities
        /// call it from their custom validation so a ship with no weapon is turned away before the STM is paid.
        /// </summary>
        public static string GetFireError(uint ship, uint target, SpaceWeaponFilter filter)
        {
            var status = Space.GetShipStatus(ship);
            if (status == null)
                return "You must be piloting a ship.";

            var cloakBlock = Space.GetCloakWeaponBlock(ship);
            if (!string.IsNullOrEmpty(cloakBlock))
                return cloakBlock;

            if (!GetIsObjectValid(target) || target == ship)
                return "A target is required.";

            var targetStatus = Space.GetShipStatus(target);
            if (targetStatus == null)
                return "That is not a ship.";

            // Player ships attack each other only where the ground rules allow it (plan section 9, PvP).
            var pvpRefusal = Space.GetPvpRefusal(ship, target);
            if (!string.IsNullOrEmpty(pvpRefusal))
                return pvpRefusal;

            return PickWeapon(ship, status, target, targetStatus, filter, out _);
        }

        private static string PickWeapon(
            uint ship,
            ShipStatus status,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponFilter filter,
            out FittedWeapon picked)
        {
            picked = null;
            var weapons = GetFittedWeapons(status);
            if (!weapons.Any(x => SpaceWeaponPicker.Matches(filter, x.Fitted.Step.WeaponFamily)))
                return FamilyError(filter);

            var candidates = BuildCandidates(ship, status, weapons, target, targetStatus);
            var pick = SpaceWeaponPicker.Pick(candidates, filter, GetDistanceBetween(ship, target));
            if (pick == null)
            {
                return candidates.Any(x => x.Usable && SpaceWeaponPicker.Matches(filter, x.Family))
                    ? "Your target is out of range of your weapons."
                    : "Your weapons cannot fire at that target right now.";
            }

            picked = (FittedWeapon)pick.Source;
            return string.Empty;
        }

        private static string FamilyError(SpaceWeaponFilter filter) => filter switch
        {
            SpaceWeaponFilter.Energy => "Your ship has no energy weapon fitted.",
            SpaceWeaponFilter.Launcher => "Your ship has no launcher fitted.",
            _ => "Your ship has no weapon fitted.",
        };

        // Firing

        /// <summary>
        /// Fires one weapon at a target for an ability: the highest-ranked usable weapon of the filter's family that
        /// reaches the target (Helm or crewed seat), built through <see cref="ShipShotBuilder"/> so hull size
        /// scaling and an NPC ship's own numbers apply, plus <paramref name="bonusDmg"/>. It makes one impact, so
        /// the damage never scales with how many weapons the ship carries. Launcher ammo is spent as the module would.
        /// The result says whether it hit or critically hit and how much damage the roll did.
        /// </summary>
        public static SpaceFireResult FireWeapon(
            uint activator,
            uint target,
            SpaceWeaponFilter filter,
            int bonusDmg = 0,
            SpaceFireOptions options = null)
        {
            options ??= new SpaceFireOptions();

            var status = Space.GetShipStatus(activator);
            var targetStatus = Space.GetShipStatus(target);
            var error = GetFireError(activator, target, filter);
            if (!string.IsNullOrWhiteSpace(error))
                return SpaceFireResult.Failure(error);

            PickWeapon(activator, status, target, targetStatus, filter, out var weapon);
            return FireFrom(activator, status, weapon, target, targetStatus, bonusDmg, options, consumeAmmo: true);
        }

        /// <summary>
        /// Fires one weapon at every enemy ship within <paramref name="radius"/> meters of a point, up to
        /// <paramref name="maxTargets"/> ships, nearest to the point first. The primary target, when given and
        /// hostile, is always one of them. One weapon makes every impact and spends its ammo once. Ships beyond the
        /// weapon's reach are skipped. The area flag is set on the shot so the area damage stat applies.
        /// </summary>
        public static List<SpaceFireResult> FireWeaponArea(
            uint activator,
            Location center,
            float radius,
            int maxTargets,
            SpaceWeaponFilter filter,
            int bonusDmg = 0,
            SpaceFireOptions options = null,
            uint primaryTarget = OBJECT_INVALID)
        {
            var results = new List<SpaceFireResult>();
            var status = Space.GetShipStatus(activator);
            if (status == null)
            {
                results.Add(SpaceFireResult.Failure("You must be piloting a ship."));
                return results;
            }

            options ??= new SpaceFireOptions();
            options.IsArea = true;

            var targets = new List<uint>();
            if (GetIsObjectValid(primaryTarget) && primaryTarget != activator && !GetIsDead(primaryTarget) &&
                Space.IsShipEnemy(activator, primaryTarget) && Space.IsShipInSpace(primaryTarget))
            {
                targets.Add(primaryTarget);
            }

            foreach (var ship in Space.GetEnemyShipsNear(activator, center, radius))
            {
                if (targets.Count >= maxTargets)
                    break;

                if (!targets.Contains(ship))
                    targets.Add(ship);
            }

            if (targets.Count > maxTargets)
                targets.RemoveRange(maxTargets, targets.Count - maxTargets);

            if (targets.Count == 0)
            {
                results.Add(SpaceFireResult.Failure("There are no enemy ships in the area."));
                return results;
            }

            // The weapon is chosen to reach the point the area is centered on; each ship must be within its reach too.
            var aim = GetIsObjectValid(primaryTarget) ? primaryTarget : targets[0];
            var aimStatus = Space.GetShipStatus(aim);
            var error = PickWeapon(activator, status, aim, aimStatus, filter, out var weapon);
            if (!string.IsNullOrWhiteSpace(error))
            {
                results.Add(SpaceFireResult.Failure(error));
                return results;
            }

            var reach = weapon.Detail.ModuleMaxDistanceAction == null
                ? weapon.Fitted.Step.Range
                : weapon.Detail.ModuleMaxDistanceAction(activator, status, aim, aimStatus, weapon.Module.ModuleBonus);

            var spentAmmo = false;
            foreach (var ship in targets)
            {
                if (GetDistanceBetween(activator, ship) > reach)
                    continue;

                var shipStatus = Space.GetShipStatus(ship);
                if (shipStatus == null)
                    continue;

                results.Add(FireFrom(activator, status, weapon, ship, shipStatus, bonusDmg, options, consumeAmmo: !spentAmmo));
                spentAmmo = true;
            }

            if (results.Count == 0)
                results.Add(SpaceFireResult.Failure("Your enemies are out of range of your weapons."));

            return results;
        }

        private static SpaceFireResult FireFrom(
            uint activator,
            ShipStatus status,
            FittedWeapon weapon,
            uint target,
            ShipStatus targetStatus,
            int bonusDmg,
            SpaceFireOptions options,
            bool consumeAmmo)
        {
            var step = weapon.Fitted.Step;
            var shot = ShipShotBuilder.BuildFor(step, status, targetStatus);
            shot.Dmg += bonusDmg;
            shot.IsAbility = options.IsAbility;
            shot.IsArea = options.IsArea;
            shot.ConsumesRiders = options.ConsumesRiders;
            if (options.DamageType.HasValue)
                shot.DamageType = options.DamageType.Value;

            // An ability's launcher shot spends the ammo the launcher would fire next, plain or a variant.
            var plainAmmo = ShipModuleActions.GetAmmoResref(step.Family);
            if (consumeAmmo && plainAmmo != null)
                ShipModuleActions.ConsumeAmmo(activator, ShipModuleActions.PickAmmo(activator, step.Family, plainAmmo) ?? plainAmmo);

            var roll = ShipModuleActions.FireStep(step, activator, status, target, targetStatus, shot, true);
            if (roll == null)
                return SpaceFireResult.Failure("Your ship cannot fire right now.");

            // The roll may have consumed a rider on the status, which a player's saved copy must hear about.
            if (roll.ConsumedRiders.Count > 0)
                Space.SaveShipStatus(activator, status);

            if (roll.IsHit)
                Ability.RecordSpaceShot(activator, target, options.IsArea, roll.IsCritical, roll.Damage);

            return new SpaceFireResult
            {
                Fired = true,
                Target = target,
                Shot = roll,
                Module = step,
                Slot = weapon.Slot,
            };
        }

        // Riders

        /// <summary>
        /// Arms a rider: the ship's next landed weapon shot that matches consumes it. A rider with the same key
        /// replaces the old one. It lapses after <paramref name="seconds"/> unconsumed.
        /// </summary>
        public static void ArmRider(uint ship, SpaceShotRider rider, float seconds = RiderSeconds)
        {
            var status = Space.GetShipStatus(ship);
            if (status == null || rider == null)
                return;

            rider.ExpiresAt = DateTime.UtcNow.AddSeconds(seconds);
            status.PendingRiders ??= new List<SpaceShotRider>();
            status.PendingRiders.RemoveAll(x => x.Key == rider.Key);
            status.PendingRiders.Add(rider);
            Space.SaveShipStatus(ship, status);
        }

        /// <summary>Whether the ship holds an unexpired rider with this key.</summary>
        public static bool HasRider(uint ship, string key)
        {
            var status = Space.GetShipStatus(ship);
            var now = DateTime.UtcNow;
            return status?.PendingRiders != null && status.PendingRiders.Any(x => x.Key == key && now < x.ExpiresAt);
        }

        /// <summary>Removes a rider without consuming it.</summary>
        public static void ClearRider(uint ship, string key)
        {
            var status = Space.GetShipStatus(ship);
            if (status?.PendingRiders == null || status.PendingRiders.RemoveAll(x => x.Key == key) == 0)
                return;

            Space.SaveShipStatus(ship, status);
        }

        /// <summary>
        /// Registers what happens when a rider with this key is consumed, beyond its DMG, extra damage and extra
        /// threat: the handler runs after the consuming shot's damage lands.
        /// </summary>
        public static void RegisterRiderHandler(string key, Action<SpaceRiderContext> handler)
        {
            _riderHandlers[key] = handler;
        }

        internal static void RunRiderHandler(SpaceRiderContext context)
        {
            if (context.Rider?.Key == null || !_riderHandlers.TryGetValue(context.Rider.Key, out var handler))
                return;

            try
            {
                handler(context);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Space rider handler '{context.Rider.Key}' failed: {ex}");
            }
        }
    }
}
