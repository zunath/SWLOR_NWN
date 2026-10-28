using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The Ship HUD's decisions as pure logic with no engine calls: which modules a seat sees, the module and stat
    /// rows, the Retarget choice and the All Weapons state (plan section 8).
    /// </summary>
    public static class ShipHudRules
    {
        public const string NoTargetText = "No target";
        public const string SelfTargetText = "Own ship";
        public const string AreaTargetText = "Around ship";
        public const string RetargetNeedsAllyMessage = "Select an allied ship to lock that module onto.";

        /// <summary>Progress is shown in steps of this size so an unchanged bar is not pushed again.</summary>
        public const float ProgressStep = 0.05f;

        /// <summary>The seat whose modules the HUD lists for the pilot. A crew member passes their own seat.</summary>
        public const ShipCrewSeat PilotSeat = ShipCrewSeat.Pilot;

        /// <summary>The one filter that decides which slots a seat sees and controls.</summary>
        public static bool IsSlotInSeat(ShipStatus status, int slot, ShipCrewSeat seat)
        {
            return ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, slot) == seat;
        }

        /// <summary>The fitted module in a hotbar slot (high 1-10, low 11-20, mid 21-30), or null.</summary>
        public static ShipStatus.ShipStatusModule GetModule(ShipStatus status, int slot)
        {
            Dictionary<int, ShipStatus.ShipStatusModule> row = null;
            var index = slot;
            if (slot >= 1 && slot <= 10)
            {
                row = status.HighPowerModules;
            }
            else if (slot >= 11 && slot <= 20)
            {
                row = status.LowPowerModules;
                index = slot - 10;
            }
            else if (slot >= 21 && slot <= 30)
            {
                row = status.MidPowerModules;
                index = slot - 20;
            }

            return row != null && row.TryGetValue(index, out var module) ? module : null;
        }

        /// <summary>The slot's row and number, such as "High 2".</summary>
        public static string GetSlotLabel(int slot)
        {
            if (slot >= 1 && slot <= 10) return $"High {slot}";
            if (slot >= 11 && slot <= 20) return $"Low {slot - 10}";
            if (slot >= 21 && slot <= 30) return $"Mid {slot - 20}";
            return $"Slot {slot}";
        }

        /// <summary>The list order: High, then Mid, then Low, each by slot number.</summary>
        private static int GetListOrder(int slot)
        {
            if (slot >= 1 && slot <= 10) return slot;
            if (slot >= 21 && slot <= 30) return 100 + slot;
            return 200 + slot;
        }

        /// <summary>
        /// The modules that are on and belong to a seat, in list order. A module whose slot no longer holds a
        /// registered module is left out.
        /// </summary>
        public static List<ShipHudModuleRow> BuildModuleRows(
            ShipStatus status,
            ShipCrewSeat seat,
            DateTime now,
            Func<string, ShipModuleDetail> getDetail,
            uint currentTarget,
            uint self,
            Func<uint, string> getTargetName)
        {
            var rows = new List<ShipHudModuleRow>();
            if (status?.ActiveModules == null)
                return rows;

            foreach (var active in status.ActiveModules.OrderBy(x => GetListOrder(x.Slot)))
            {
                if (!IsSlotInSeat(status, active.Slot, seat))
                    continue;

                var module = GetModule(status, active.Slot);
                var detail = module == null ? null : getDetail(module.ItemTag);
                if (detail == null)
                    continue;

                var mode = ShipToggleRules.GetTargetMode(detail.Type);
                rows.Add(new ShipHudModuleRow
                {
                    Slot = active.Slot,
                    SlotLabel = GetSlotLabel(active.Slot),
                    Name = detail.Name,
                    TargetText = GetTargetText(mode, currentTarget, active.TargetId, self, getTargetName),
                    Progress = GetCycleProgress(module.RecastStartedAt, module.RecastTime, now),
                    CanRetarget = CanRetarget(detail.Type)
                });
            }

            return rows;
        }

        /// <summary>What a module is aimed at: the ship's target, its locked target, or the area around the ship.</summary>
        public static string GetTargetText(
            ShipToggleTargetMode mode,
            uint currentTarget,
            uint lockedTarget,
            uint self,
            Func<uint, string> getTargetName)
        {
            if (mode == ShipToggleTargetMode.SelfCentered)
                return AreaTargetText;

            var target = ShipToggleRules.ChooseTarget(mode, currentTarget, lockedTarget, self);
            if (mode == ShipToggleTargetMode.Locked && (target == self || target == OBJECT_INVALID))
                return SelfTargetText;

            var name = target == OBJECT_INVALID ? null : getTargetName(target);
            return string.IsNullOrWhiteSpace(name) ? NoTargetText : name;
        }

        /// <summary>The share of the current cycle that has passed, in steps of <see cref="ProgressStep"/>.</summary>
        public static float GetCycleProgress(DateTime cycleStart, DateTime cycleEnd, DateTime now)
        {
            var length = (cycleEnd - cycleStart).TotalSeconds;
            if (length <= 0)
                return 0f;

            var share = Math.Clamp((now - cycleStart).TotalSeconds / length, 0.0, 1.0);
            return (float)(Math.Round(share / ProgressStep) * ProgressStep);
        }

        /// <summary>Whether a module of this type keeps a target the pilot can change.</summary>
        public static bool CanRetarget(ShipModuleType type)
        {
            return ShipToggleRules.GetTargetMode(type) == ShipToggleTargetMode.Locked;
        }

        /// <summary>
        /// Picks the target Retarget locks a support module onto: an allied ship, or the ship itself when nothing is
        /// selected. A selected enemy, or an object without a ship status, is turned away with a message.
        /// </summary>
        public static uint ChooseRetarget(bool hasTarget, bool targetUsable, uint selectedTarget, uint self, out string error)
        {
            error = string.Empty;
            if (!hasTarget)
                return self;

            if (targetUsable)
                return selectedTarget;

            error = RetargetNeedsAllyMessage;
            return OBJECT_INVALID;
        }

        /// <summary>The slots of the weapon toggles a seat controls: toggled offensive modules, in slot order.</summary>
        public static List<int> GetWeaponSlots(ShipStatus status, ShipCrewSeat seat, Func<string, ShipModuleDetail> getDetail)
        {
            var slots = new List<int>();
            if (status == null)
                return slots;

            for (var slot = 1; slot <= 30; slot++)
            {
                var module = GetModule(status, slot);
                var detail = module == null ? null : getDetail(module.ItemTag);
                if (detail == null ||
                    detail.Activation != ShipModuleActivation.Toggle ||
                    !ShipToggleRules.IsOffensive(detail.Type) ||
                    !IsSlotInSeat(status, slot, seat))
                {
                    continue;
                }

                slots.Add(slot);
            }

            return slots;
        }

        /// <summary>How many of the given weapon slots are switched on.</summary>
        public static int CountOn(ShipStatus status, IEnumerable<int> slots)
        {
            var on = status?.ActiveModules?.Select(x => x.Slot).ToHashSet() ?? new HashSet<int>();
            return slots.Count(on.Contains);
        }

        /// <summary>The next press switches every weapon on unless they all are, and then switches them off.</summary>
        public static bool ShouldTurnAllWeaponsOn(int on, int total)
        {
            return total > 0 && on < total;
        }

        /// <summary>The All Weapons button's text.</summary>
        public static string GetAllWeaponsText(int on, int total)
        {
            if (total <= 0)
                return "All Weapons: none fitted";

            if (on <= 0)
                return "All Weapons: Off";

            return on >= total ? "All Weapons: On" : $"All Weapons: {on} of {total} on";
        }

        /// <summary>The stat lines of the Stats tab, in the order they are shown.</summary>
        public static List<ShipHudStatRow> BuildStatRows(ShipHudStats stats)
        {
            var rows = new List<ShipHudStatRow>
            {
                new("VITALS"),
                new("Hull", $"{stats.Hull} / {stats.MaxHull}"),
                new("Shield", $"{stats.Shield} / {stats.MaxShield}"),
                new("Shield Regen", $"{stats.ShieldRegenPerSecond:0.##} / s"),
                new("Capacitor", $"{stats.Capacitor} / {stats.MaxCapacitor}"),
                new("Capacitor Regen", $"{stats.CapacitorRegenPerSecond:0.##} / s"),
                new("OFFENSE"),
                new("Attack (energy)", stats.EnergyAttack.ToString()),
                new("Attack (launcher)", stats.LauncherAttack.ToString()),
                new("Accuracy (energy)", stats.EnergyAccuracy.ToString()),
                new("Accuracy (launcher)", stats.LauncherAccuracy.ToString()),
                new("Critical Rate", $"{stats.CriticalRate}%"),
                new("Thermal Damage", FormatPercent(stats.ThermalDamagePercent)),
                new("EM Damage", FormatPercent(stats.EmDamagePercent)),
                new("Explosive Damage", FormatPercent(stats.ExplosiveDamagePercent)),
                new("DEFENSES"),
                new("Defense", stats.Defense.ToString()),
                new("Evasion", stats.Evasion.ToString()),
                new("Thermal Resistance", $"{stats.ThermalResistance}%"),
                new("EM Resistance", $"{stats.EmResistance}%"),
                new("Explosive Resistance", $"{stats.ExplosiveResistance}%"),
                new("SHIP"),
                new("Speed", $"{Math.Round(stats.SpeedMultiplier * 100)}%"),
                new("Power Routing", PowerRoutingRules.GetName(stats.PowerRouting)),
            };

            if (stats.CloakText != null)
                rows.Add(new("Cloak", stats.CloakText));

            rows.AddRange(new ShipHudStatRow[]
            {
                new("High Slots", $"{stats.HighFitted} / {stats.HighSlots}"),
                new("Mid Slots", $"{stats.MidFitted} / {stats.MidSlots}"),
                new("Low Slots", $"{stats.LowFitted} / {stats.LowSlots}"),
                new("CARGO"),
                new("Cargo", stats.Cargo == null ? "-" : $"{stats.CargoUsed} / {stats.Cargo}"),
            });

            rows.AddRange((stats.CargoLines ?? new List<(string Name, int Units)>())
                .Select(line => new ShipHudStatRow(line.Name, line.Units.ToString())));
            return rows;
        }

        private static string FormatPercent(int percent)
        {
            return percent > 0 ? $"+{percent}%" : $"{percent}%";
        }
    }
}
