using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Toggled ship modules and the auto-fire loop (plan section 8).
    /// </summary>
    public static partial class Space
    {
        /// <summary>Ships with at least one toggle on, with the earliest time any of their toggles is due.</summary>
        private static readonly Dictionary<uint, DateTime> _toggleWake = new();

        /// <summary>Lit icon names by module texture. Null when the lit art (or its recharge frames) is missing.</summary>
        private static readonly Dictionary<string, string> _litTextures = new();

        /// <summary>NPC repairers turn on once this share of shield or hull is missing.</summary>
        private const float NpcRepairThreshold = 0.25f;

        /// <summary>Starts the auto-fire loop. It runs faster than the one second ship tick so cycles stay on time.</summary>
        private static void ScheduleShipToggleLoop()
        {
            Scheduler.ScheduleRepeating(ProcessShipToggles, TimeSpan.FromSeconds(ShipToggleRules.LoopSeconds));
        }

        /// <summary>Finds the fitted module for a hotbar slot number (high 1-10, low 11-20, mid 21-30).</summary>
        private static bool TryGetFittedModule(ShipStatus status, int slotNumber, out ShipStatus.ShipStatusModule module)
        {
            module = null;
            if (slotNumber >= 1 && slotNumber <= 10)
                return status.HighPowerModules.TryGetValue(slotNumber, out module);
            if (slotNumber >= 11 && slotNumber <= 20)
                return status.LowPowerModules.TryGetValue(slotNumber - 10, out module);
            if (slotNumber >= 21 && slotNumber <= 30)
                return status.MidPowerModules.TryGetValue(slotNumber - 20, out module);

            return false;
        }

        private static FeatType SlotNumberToFeat(int slotNumber)
        {
            return (FeatType)((int)FeatType.ShipModule1 - 1 + slotNumber);
        }

        private static List<ShipStatus.ShipActiveModule> Toggles(ShipStatus status)
        {
            return status.ActiveModules ??= new List<ShipStatus.ShipActiveModule>();
        }

        private static ShipStatus.ShipActiveModule GetActiveToggle(ShipStatus status, int slotNumber)
        {
            return Toggles(status).FirstOrDefault(x => x.Slot == slotNumber);
        }

        /// <summary>Whether the module in a hotbar slot is switched on.</summary>
        public static bool IsShipToggleOn(ShipStatus status, int slotNumber)
        {
            return status != null && GetActiveToggle(status, slotNumber) != null;
        }

        /// <summary>Saves a player's ship status. NPC statuses live in the cache and need no save.</summary>
        public static void SaveShipStatus(uint creature, ShipStatus status)
        {
            if (!GetIsPC(creature)) return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(creature));
            if (dbPlayer == null || dbPlayer.ActiveShipId == Guid.Empty.ToString()) return;

            var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
            if (dbShip == null) return;

            dbShip.Status = status;
            DB.Set(dbShip);
        }

        private static bool CanShowShipModuleVisuals(uint player)
        {
            return GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);
        }

        private static bool ResourceExists(string resref)
        {
            return ResManGetAliasFor(resref, ResType.TGA) != string.Empty ||
                   ResManGetAliasFor(resref, ResType.DDS) != string.Empty;
        }

        /// <summary>
        /// The lit icon for a module texture, or null while the art is missing. The recharge sweep draws its
        /// frames from the icon, so the lit icon is used only when its six recharge frames exist as well.
        /// </summary>
        private static string GetLitTexture(string texture)
        {
            if (string.IsNullOrWhiteSpace(texture)) return null;
            if (_litTextures.TryGetValue(texture, out var cached)) return cached;

            var lit = ShipToggleRules.LitTextureName(texture);
            if (lit != null)
            {
                var complete = ResourceExists(lit);
                for (var stage = 0; complete && stage <= AbilityCooldownVisual.MaximumCooldownStage; stage++)
                {
                    var frame = AbilityCooldownVisual.GetCooldownTextureName(lit, stage);
                    complete = frame != null && ResourceExists(frame);
                }

                if (!complete) lit = null;
            }

            _litTextures[texture] = lit;
            return lit;
        }

        /// <summary>The icon a slot shows: the lit variant while its toggle is on and the art exists.</summary>
        private static string GetShipModuleIconTexture(ShipStatus status, int slotNumber, ShipModuleDetail detail)
        {
            return IsShipToggleOn(status, slotNumber)
                ? GetLitTexture(detail.Texture) ?? detail.Texture
                : detail.Texture;
        }

        /// <summary>
        /// Shows a slot's current state: the recharge sweep when its cycle is running, or its plain or lit icon.
        /// </summary>
        private static void RefreshShipModuleVisual(uint player, ShipStatus status, int slotNumber)
        {
            if (!CanShowShipModuleVisuals(player)) return;
            if (!ShipModuleFeats.TryGetValue(SlotNumberToFeat(slotNumber), out var shipFeat)) return;
            if (!TryGetFittedModule(status, slotNumber, out var module) ||
                !_shipModules.TryGetValue(module.ItemTag, out var detail)) return;

            if (shipFeat.RecastGroup != RecastGroup.Invalid && module.RecastTime > DateTime.UtcNow)
                ApplyShipModuleRecast(player, slotNumber, shipFeat, module, status);
            else
                SetTextureOverride(shipFeat.TextureName, GetShipModuleIconTexture(status, slotNumber, detail), player);
        }

        /// <summary>Keeps the registry's wake time at the earliest due toggle, or drops the ship when none are on.</summary>
        private static void UpdateToggleWake(uint creature, ShipStatus status)
        {
            var earliest = ShipToggleRules.EarliestCycle(Toggles(status).Select(x => x.NextCycleTime));
            if (earliest == null)
                _toggleWake.Remove(creature);
            else
                _toggleWake[creature] = earliest.Value;
        }

        /// <summary>
        /// Switches one toggle off, restores its icon and optionally tells the player who works the slot why (the
        /// crew member at its station, or the pilot).
        /// </summary>
        private static void TurnOffToggle(uint creature, ShipStatus status, int slotNumber, string message = null)
        {
            Toggles(status).RemoveAll(x => x.Slot == slotNumber);
            SaveShipStatus(creature, status);
            RefreshSlotVisuals(creature, status, slotNumber);

            var slotOperator = GetSlotOperator(creature, status, slotNumber);
            if (!string.IsNullOrWhiteSpace(message) && GetIsPC(slotOperator))
                SendMessageToPC(slotOperator, message);

            UpdateToggleWake(creature, status);
        }

        /// <summary>Refreshes a slot's icon for the pilot and, for a station slot, the crew member who works it.</summary>
        private static void RefreshSlotVisuals(uint ship, ShipStatus status, int slotNumber)
        {
            RefreshShipModuleVisual(ship, status, slotNumber);

            var slotOperator = GetSlotOperator(ship, status, slotNumber);
            if (slotOperator != ship && slotOperator != OBJECT_INVALID)
                RefreshShipModuleVisual(slotOperator, status, slotNumber);
        }

        /// <summary>
        /// Forgets every toggle on a status that the caller is about to save itself (entering space, docking,
        /// destruction). Slot icons are cleared by the caller's own recast cleanup.
        /// </summary>
        private static void ClearShipToggleState(uint creature, ShipStatus status)
        {
            _toggleWake.Remove(creature);
            if (status != null)
                Toggles(status).Clear();
        }

        /// <summary>Switches off every toggle on a ship without a message (area change, logout).</summary>
        public static void TurnOffShipToggles(uint creature)
        {
            if (!_toggleWake.ContainsKey(creature)) return;
            _toggleWake.Remove(creature);

            var status = GetShipStatus(creature);
            if (status == null || Toggles(status).Count == 0) return;

            var slots = Toggles(status).Select(x => x.Slot).ToList();
            Toggles(status).Clear();
            SaveShipStatus(creature, status);

            foreach (var slot in slots)
            {
                RefreshSlotVisuals(creature, status, slot);
            }
        }

        /// <summary>
        /// Runs a module that has been validated: its action, then the recast, capacitor and saves.
        /// Used by one-press activation, a toggle's first cycle and every auto-fired cycle.
        /// </summary>
        private static void FireShipModule(
            uint activator,
            ShipStatus activatorShipStatus,
            int slotNumber,
            ShipStatus.ShipStatusModule shipModule,
            ShipModuleDetail shipModuleDetails,
            uint target,
            ShipStatus targetShipStatus,
            int requiredCapacitor,
            bool applyRecast,
            DateTime recastStart,
            DateTime recastEnd,
            bool setGlobalRecast)
        {
            shipModuleDetails.ModuleActivatedAction?.Invoke(activator, activatorShipStatus, target, targetShipStatus, shipModule.ModuleBonus);

            if (applyRecast)
            {
                shipModule.RecastStartedAt = recastStart;
                shipModule.RecastTime = recastEnd;

                if (setGlobalRecast)
                    activatorShipStatus.GlobalRecast = recastStart.AddSeconds(2f);
            }

            if (requiredCapacitor > 0)
            {
                activatorShipStatus.Capacitor -= requiredCapacitor;
            }

            // Update changes for players. No need to update NPCs as they are already stored in memory cache.
            if (GetIsPC(activator))
            {
                SaveShipStatus(activator, activatorShipStatus);
                RefreshSlotVisuals(activator, activatorShipStatus, slotNumber);
                ExecuteScript("pc_target_upd", activator);
            }

            // A player target is a separate copy of its status. Self-targeting already saved above.
            if (GetIsPC(target) && target != activator && targetShipStatus != null)
            {
                SaveShipStatus(target, targetShipStatus);
                ExecuteScript("pc_target_upd", target);
            }
        }

        /// <summary>Whether the target passes the module's valid object types and ship-status checks.</summary>
        private static bool IsToggleTargetAllowed(ShipModuleDetail detail, uint target, ShipStatus targetStatus)
        {
            if (detail.ValidTargetTypes.Count > 0 &&
                !detail.ValidTargetTypes.Contains(GetObjectType(target)) &&
                !(detail.CanTargetSelf && !GetIsObjectValid(target)))
            {
                return false;
            }

            return GetObjectType(target) == ObjectType.Placeable || targetStatus != null || detail.CanTargetSelf;
        }

        /// <summary>Fires every toggle whose cycle has ended, for players and NPC ships alike.</summary>
        private static void ProcessShipToggles()
        {
            if (_toggleWake.Count == 0) return;

            var now = DateTime.UtcNow;
            var due = _toggleWake.Where(x => ShipToggleRules.IsDue(now, x.Value)).Select(x => x.Key).ToList();
            foreach (var creature in due)
            {
                ProcessCreatureToggles(creature, now);
            }
        }

        private static void ProcessCreatureToggles(uint creature, DateTime now)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
            {
                _toggleWake.Remove(creature);
                return;
            }

            var status = GetShipStatus(creature);
            if (status == null)
            {
                _toggleWake.Remove(creature);
                return;
            }

            // Leashed ships stop firing as they return home.
            if (!GetIsPC(creature) && AI.IsLeashEvading(creature))
            {
                Toggles(status).Clear();
                _toggleWake.Remove(creature);
                return;
            }

            var dueSlots = Toggles(status)
                .Where(x => ShipToggleRules.IsDue(now, x.NextCycleTime))
                .OrderBy(x => x.NextCycleTime)
                .Select(x => x.Slot)
                .ToList();

            foreach (var slot in dueSlots)
            {
                // A fired module may have changed the saved status, so each cycle works on a fresh copy.
                status = GetShipStatus(creature);
                if (status == null) break;

                var active = GetActiveToggle(status, slot);
                if (active == null) continue;

                var current = status;
                AssignCommand(creature, () => RunToggleCycle(creature, current, active, now));
            }

            status = GetShipStatus(creature);
            if (status == null)
                _toggleWake.Remove(creature);
            else
                UpdateToggleWake(creature, status);
        }

        /// <summary>
        /// Checks one due toggle and either turns it off or pays for and fires its next cycle.
        /// </summary>
        private static void RunToggleCycle(uint creature, ShipStatus status, ShipStatus.ShipActiveModule active, DateTime now)
        {
            var slot = active.Slot;

            try
            {
                if (!TryGetFittedModule(status, slot, out var module) ||
                    !_shipModules.TryGetValue(module.ItemTag, out var detail))
                {
                    TurnOffToggle(creature, status, slot);
                    return;
                }

                // A dazed ship holds its fire. The toggle stays on and no capacitor is spent; its overdue cycle
                // fires when the daze ends, without a burst to catch up (see ShipToggleRules.NextCycleTime).
                if (IsShipIncapacitated(creature))
                    return;

                // A station slot fires only while its seat is crewed, at the crew member's own target.
                var seat = ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, slot);
                var crewMember = seat == ShipCrewSeat.Pilot ? OBJECT_INVALID : SpaceCrew.GetSeatOccupant(creature, seat);
                if (seat != ShipCrewSeat.Pilot && (crewMember == OBJECT_INVALID || !GetIsObjectValid(crewMember)))
                {
                    TurnOffToggle(creature, status, slot);
                    return;
                }

                var mode = ShipToggleRules.GetTargetMode(detail.Type);
                var currentTarget = GetLocalObject(crewMember == OBJECT_INVALID ? creature : crewMember, "SPACE_TARGET");
                var target =ShipToggleRules.ChooseTarget(mode, currentTarget, active.TargetId, creature);
                var targetStatus = target == creature ? status : GetShipStatus(target);
                var targetValid = GetIsObjectValid(target);
                var required = detail.CalculateCapacitorAction?.Invoke(creature, status, module.ModuleBonus) ?? 0;
                var needsTarget = mode != ShipToggleTargetMode.SelfCentered;

                ShipToggleCheck BuildCheck(string validationError, int requiredCapacitor) => new()
                {
                    ShipInSpace = !GetIsDead(creature),
                    NeedsTarget = needsTarget,
                    TargetValid = targetValid,
                    TargetDead = targetValid && GetIsDead(target),
                    TargetTypeAllowed = !needsTarget || IsToggleTargetAllowed(detail, target, targetStatus),
                    CheckRange = needsTarget && targetValid && target != creature,
                    Distance = targetValid ? GetDistanceBetween(creature, target) : 0f,
                    MaxDistance = GetShipModuleMaxDistance(creature, status, target, targetStatus, module),
                    ValidationError = validationError,
                    Capacitor = status.Capacitor,
                    RequiredCapacitor = requiredCapacitor
                };

                // The module's own validation (ammo, asteroid left) only runs once the target itself is sound.
                var validation = string.Empty;
                if (ShipToggleRules.Evaluate(BuildCheck(string.Empty, 0)) == ShipToggleAutoOffReason.None)
                {
                    validation = detail.ModuleValidationAction?.Invoke(creature, status, target, targetStatus, module.ModuleBonus) ?? string.Empty;
                }

                var reason = ShipToggleRules.Evaluate(BuildCheck(validation, required));
                if (reason != ShipToggleAutoOffReason.None)
                {
                    var message = GetIsPC(creature)
                        ? ShipToggleRules.AutoOffMessage(reason, detail.Name, validation)
                        : string.Empty;
                    TurnOffToggle(creature, status, slot, message);
                    return;
                }

                var cycle = detail.CalculateRecastAction?.Invoke(creature, status, module.ModuleBonus) ?? 0f;
                if (cycle <= 0f) cycle = (float)ShipToggleRules.LoopSeconds;

                // "No extra delay" cycles (a limited weapon cycle effect) run back to back.
                var noDelayCycle = SpaceWeaponCycles.HasNoDelayCycle(creature, detail.Type);
                if (noDelayCycle) cycle = (float)ShipToggleRules.LoopSeconds;

                // The next cycle is anchored to the schedule, not to when this loop happened to run.
                var next = ShipToggleRules.NextCycleTime(active.NextCycleTime, now, cycle);
                active.NextCycleTime = next;

                using (SpaceCrew.BeginStationFire(BuildCrewFireContext(creature, crewMember, seat)))
                {
                    FireShipModule(creature, status, slot, module, detail, target, targetStatus, required,
                        true, next.AddSeconds(-cycle), next, false);
                }

                if (noDelayCycle)
                    SpaceWeaponCycles.ConsumeCycle(creature, detail.Type);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Toggle auto-fire failed for slot {slot}: {ex}");

                // A module that keeps failing must not retry every quarter second.
                TurnOffToggle(creature, status, slot);
            }
        }

        /// <summary>
        /// Switches an NPC ship's repairers: off once nothing is missing on the ship they are locked onto (the ship
        /// itself, or the ally a Logistics ship picked), and (in combat) on for the ship itself once a quarter is missing.
        /// </summary>
        private static void UpdateNpcRepairToggles(uint creature, ShipStatus shipStatus, bool allowTurnOn)
        {
            foreach (var (feat, shipModule) in GetNpcModuleSlots(shipStatus))
            {
                var detail = _shipModules[shipModule.ItemTag];
                if (detail.Activation != ShipModuleActivation.Toggle) continue;

                var repairsShield = ShipToggleRules.RepairsShield(detail.Type);
                var repairsHull = ShipToggleRules.RepairsHull(detail.Type);
                if (!repairsShield && !repairsHull) continue;

                var slot = GetFeatSlotNumber(feat);
                var locked = GetActiveToggle(shipStatus, slot)?.TargetId ?? OBJECT_INVALID;
                var repaired = locked != OBJECT_INVALID && locked != creature && GetIsObjectValid(locked)
                    ? GetShipStatus(locked) ?? shipStatus
                    : shipStatus;
                var missing = repairsShield
                    ? repaired.MaxShield - repaired.Shield
                    : repaired.MaxHull - repaired.Hull;
                var max = repairsShield ? shipStatus.MaxShield : shipStatus.MaxHull;
                var isOn = IsShipToggleOn(shipStatus, slot);

                if (isOn && ShipToggleRules.ShouldRepairerTurnOff(missing))
                {
                    TurnOffToggle(creature, shipStatus, slot);
                }
                else if (!isOn && allowTurnOn && ShipToggleRules.ShouldRepairerTurnOn(missing, max, NpcRepairThreshold))
                {
                    AssignCommand(creature, () => ActivateShipModuleCore(creature, feat, true));
                }
            }
        }

        /// <summary>
        /// Switches on an NPC ship's offensive toggles against its current target. Toggles that are already on
        /// follow the ship's target by themselves when the enmity target changes.
        /// </summary>
        private static void TurnOnNpcWeaponToggles(
            uint creature,
            ShipStatus shipStatus,
            uint target,
            ShipStatus targetShipStatus,
            float distance)
        {
            foreach (var (feat, shipModule) in GetNpcModuleSlots(shipStatus))
            {
                var detail = _shipModules[shipModule.ItemTag];
                if (detail.Activation != ShipModuleActivation.Toggle ||
                    ShipToggleRules.GetTargetMode(detail.Type) != ShipToggleTargetMode.Follow ||
                    IsShipToggleOn(shipStatus, GetFeatSlotNumber(feat)))
                {
                    continue;
                }

                var maxDistance = GetShipModuleMaxDistance(creature, shipStatus, target, targetShipStatus, shipModule);
                // Self-centered weapons hit enemies around the firing ship rather than the selected target.
                var canFire = CanNPCFireShipModule(creature, shipStatus, target, targetShipStatus, shipModule) &&
                              (detail.CanTargetSelf
                                  ? HasEnemyShipWithin(creature, maxDistance)
                                  : distance <= maxDistance);

                if (canFire)
                {
                    AssignCommand(creature, () => ActivateShipModuleCore(creature, feat, true));
                }
            }
        }

        /// <summary>An NPC ship's fitted modules with their hotbar feats, high then mid then low.</summary>
        private static IEnumerable<(FeatType Feat, ShipStatus.ShipStatusModule Module)> GetNpcModuleSlots(ShipStatus shipStatus)
        {
            foreach (var (slot, module) in shipStatus.HighPowerModules)
                yield return (HighSlotToFeat(slot), module);
            foreach (var (slot, module) in shipStatus.MidPowerModules)
                yield return (MidSlotToFeat(slot), module);
            foreach (var (slot, module) in shipStatus.LowPowerModules)
                yield return (LowSlotToFeat(slot), module);
        }
    }
}
