using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Decision rules for toggled ship modules (plan section 8). Pure logic with no engine calls.
    /// </summary>
    public static class ShipToggleRules
    {
        /// <summary>How often the auto-fire loop looks for due cycles.</summary>
        public const double LoopSeconds = 0.25;

        private const string NormalPrefix = "iit_sm";
        private const string LitPrefix = "iit_sl";

        /// <summary>
        /// Picks how a module of the given type chooses its target.
        /// </summary>
        public static ShipToggleTargetMode GetTargetMode(ShipModuleType type)
        {
            switch (type)
            {
                case ShipModuleType.ShieldRepairer:
                case ShipModuleType.HullRepairer:
                    return ShipToggleTargetMode.Locked;
                case ShipModuleType.BulwarkShieldGenerator:
                case ShipModuleType.RepairFieldGenerator:
                case ShipModuleType.CapitalEwar:
                case ShipModuleType.ShieldHardener:
                case ShipModuleType.ProbeLauncher:
                case ShipModuleType.CloakingDevice:
                case ShipModuleType.Afterburner:
                    return ShipToggleTargetMode.SelfCentered;
                default:
                    return ShipToggleTargetMode.Follow;
            }
        }

        /// <summary>Whether a module of this type is a weapon an NPC ship turns on against its target.</summary>
        public static bool IsOffensive(ShipModuleType type)
        {
            return type == ShipModuleType.LaserBattery ||
                   type == ShipModuleType.TurboLaser ||
                   type == ShipModuleType.AssaultConcussionMissile ||
                   type == ShipModuleType.StormCannon ||
                   type == ShipModuleType.QuadLaser ||
                   type == ShipModuleType.ProtonBomb ||
                   type == ShipModuleType.Missile ||
                   type == ShipModuleType.IonCannon ||
                   type == ShipModuleType.BeamLaser ||
                   type == ShipModuleType.CombatLaser;
        }

        /// <summary>
        /// Whether a module of this type works against an enemy ship it targets: a weapon, or an Ion Web, Interdictor,
        /// Jammer or Target Painter. PvP rules gate both, and droid crew run both at the pilot's target.
        /// </summary>
        public static bool TargetsEnemy(ShipModuleType type) =>
            IsOffensive(type) || ShipElectronicWarfareRules.IsHostile(type);

        /// <summary>
        /// Whether a module of this type restores shield (an NPC ship runs it while shield is missing). The Repair Field
        /// restores allies' shields.
        /// </summary>
        public static bool RepairsShield(ShipModuleType type)
        {
            return type == ShipModuleType.ShieldRepairer || type == ShipModuleType.BulwarkShieldGenerator ||
                   type == ShipModuleType.RepairFieldGenerator;
        }

        /// <summary>Whether a module of this type restores hull (an NPC ship runs it while hull is missing).</summary>
        public static bool RepairsHull(ShipModuleType type)
        {
            return type == ShipModuleType.HullRepairer;
        }

        /// <summary>An NPC ship turns a repairer on once this share of its maximum is missing.</summary>
        public static bool ShouldRepairerTurnOn(int missing, int maximum, float threshold)
        {
            return maximum > 0 && missing >= maximum * threshold;
        }

        /// <summary>An NPC ship turns a repairer off once nothing is missing.</summary>
        public static bool ShouldRepairerTurnOff(int missing)
        {
            return missing <= 0;
        }

        /// <summary>
        /// Whether a toggled module's cycle has ended.
        /// </summary>
        public static bool IsDue(DateTime now, DateTime nextCycle)
        {
            return now >= nextCycle;
        }

        /// <summary>
        /// Computes the end of the cycle that starts at <paramref name="scheduled"/>. The cycle stays anchored to
        /// the schedule, so a loop that runs a little late does not push later cycles back. A ship that has fallen
        /// a whole cycle behind restarts from now instead of firing a burst to catch up.
        /// </summary>
        public static DateTime NextCycleTime(DateTime scheduled, DateTime now, double cycleSeconds)
        {
            if (cycleSeconds <= 0)
                cycleSeconds = LoopSeconds;

            var next = scheduled.AddSeconds(cycleSeconds);
            return next <= now ? now.AddSeconds(cycleSeconds) : next;
        }

        /// <summary>
        /// The earliest next-cycle time of a set of toggles, or null when none are on.
        /// </summary>
        public static DateTime? EarliestCycle(IEnumerable<DateTime> nextCycles)
        {
            DateTime? earliest = null;
            foreach (var next in nextCycles)
            {
                if (earliest == null || next < earliest)
                    earliest = next;
            }

            return earliest;
        }

        /// <summary>
        /// Picks the target a module fires at this cycle.
        /// </summary>
        public static uint ChooseTarget(ShipToggleTargetMode mode, uint currentTarget, uint lockedTarget, uint self)
        {
            return mode switch
            {
                ShipToggleTargetMode.Locked => lockedTarget,
                ShipToggleTargetMode.SelfCentered => self,
                _ => currentTarget
            };
        }

        /// <summary>
        /// Picks the target a support module locks onto when it is turned on: the selected target when it is a
        /// usable ally, otherwise the ship itself.
        /// </summary>
        public static uint ChooseLockTarget(bool targetUsable, uint selectedTarget, uint self)
        {
            return targetUsable ? selectedTarget : self;
        }

        /// <summary>
        /// Decides whether a toggled module must turn itself off before its next cycle.
        /// </summary>
        public static ShipToggleAutoOffReason Evaluate(ShipToggleCheck check)
        {
            if (!check.ShipInSpace)
                return ShipToggleAutoOffReason.ShipLeftSpace;

            if (check.ShipCloaked)
                return ShipToggleAutoOffReason.Cloaked;

            if (check.NeedsTarget)
            {
                if (!check.TargetValid || check.TargetDead)
                    return ShipToggleAutoOffReason.TargetLost;

                if (!check.TargetTypeAllowed)
                    return ShipToggleAutoOffReason.InvalidTarget;

                if (check.PvpForbidden)
                    return ShipToggleAutoOffReason.PvpForbidden;

                if (check.CheckRange && check.Distance > check.MaxDistance)
                    return ShipToggleAutoOffReason.OutOfRange;
            }

            if (!string.IsNullOrWhiteSpace(check.ValidationError))
                return ShipToggleAutoOffReason.ValidationFailed;

            if (check.Capacitor < check.RequiredCapacitor)
                return ShipToggleAutoOffReason.NoCapacitor;

            return ShipToggleAutoOffReason.None;
        }

        /// <summary>
        /// The one short message a player sees when a module turns itself off. Empty when no message is shown.
        /// </summary>
        public static string AutoOffMessage(ShipToggleAutoOffReason reason, string moduleName, string validationError)
        {
            switch (reason)
            {
                case ShipToggleAutoOffReason.TargetLost:
                    return $"{moduleName} switched off: its target is gone.";
                case ShipToggleAutoOffReason.InvalidTarget:
                    return $"{moduleName} switched off: invalid target.";
                case ShipToggleAutoOffReason.OutOfRange:
                    return $"{moduleName} switched off: its target is out of range.";
                case ShipToggleAutoOffReason.PvpForbidden:
                    return $"{moduleName} switched off: you can no longer attack that player.";
                case ShipToggleAutoOffReason.ValidationFailed:
                    return $"{moduleName} switched off: {validationError}";
                case ShipToggleAutoOffReason.NoCapacitor:
                    return $"{moduleName} switched off: not enough capacitor.";
                case ShipToggleAutoOffReason.Cloaked:
                    return $"{moduleName} switched off: the ship is cloaked.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// The lit icon name for a module texture: the "iit_sm" prefix becomes "iit_sl". Null when the texture
        /// does not follow that naming.
        /// </summary>
        public static string LitTextureName(string texture)
        {
            if (string.IsNullOrWhiteSpace(texture) ||
                !texture.StartsWith(NormalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return LitPrefix + texture.Substring(NormalPrefix.Length);
        }
    }
}
