using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Cloaking Device (plan section 32). Switching it on starts the cloak time, during which the ship is still
    /// seen but uses no module or ability; then the ship is hidden from every player and NPC ship that cannot reveal
    /// it. A ship's own pilot, party and crew, a Sensor Sweep and any ship within the reveal range see it. The
    /// stealth control is the device's shortcut, since native stealth never hides a ship. Cloaks live in memory and end
    /// with the flight.
    /// </summary>
    public static partial class Space
    {
        private const string CloakEffectTag = "SPACE_CLOAK";

        private static readonly ShipCloakBook _cloaks = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleCloaking()
        {
            Scheduler.ScheduleRepeating(ProcessCloaks, TimeSpan.FromSeconds(CloakTuning.TickSeconds));
        }

        // State

        /// <summary>Where a ship is in its cloak.</summary>
        public static ShipCloakPhase GetCloakPhase(uint ship) => _cloaks.GetPhase(ship, DateTime.UtcNow);

        /// <summary>Whether the ship has vanished: it cloaked and the cloak time has run out.</summary>
        public static bool IsShipCloaked(uint ship) => GetCloakPhase(ship) == ShipCloakPhase.Cloaked;

        /// <summary>Whether the ship cannot fire a weapon: it is cloaking, cloaked or decloaked a moment ago.</summary>
        public static bool IsShipFiringLocked(uint ship) => _cloaks.IsFiringLocked(ship, DateTime.UtcNow);

        /// <summary>The share of its speed a cloaking or cloaked ship keeps, 1 when it has no cloak.</summary>
        public static double GetCloakSpeedMultiplier(uint ship) =>
            _cloaks.TryGet(ship, out var state) ? state.Profile.SpeedMultiplier : 1.0;

        /// <summary>The hotbar slot of the first Cloaking Device fitted to a ship, 0 when it has none.</summary>
        private static int FindFittedCloakSlot(ShipStatus status)
        {
            if (status?.MidPowerModules == null)
                return 0;

            foreach (var (slot, module) in status.MidPowerModules)
            {
                if (_shipModules.TryGetValue(module.ItemTag, out var detail) && detail.Type == ShipModuleType.CloakingDevice)
                    return ShipStatCalculator.GetHotbarSlot(ShipModulePowerType.Mid, slot);
            }

            return 0;
        }

        /// <summary>The hotbar slot of a Cloaking Device that is switched on, 0 when none is.</summary>
        private static int FindActiveCloakSlot(ShipStatus status)
        {
            foreach (var active in Toggles(status))
            {
                if (TryGetFittedModule(status, active.Slot, out var module) &&
                    _shipModules.TryGetValue(module.ItemTag, out var detail) &&
                    detail.Type == ShipModuleType.CloakingDevice)
                {
                    return active.Slot;
                }
            }

            return 0;
        }

        // Rules at the module and ability gates

        /// <summary>Why a module cannot be used while the ship cloaks or has just decloaked, or an empty string.</summary>
        private static string GetCloakModuleBlock(uint ship, ShipModuleDetail detail)
        {
            var now = DateTime.UtcNow;
            return ShipCloakRules.GetModuleBlock(
                _cloaks.GetPhase(ship, now),
                _cloaks.GetLockSecondsLeft(ship, now),
                detail.Type == ShipModuleType.CloakingDevice,
                IsOffensiveShipModule(detail.Type));
        }

        /// <summary>Why a weapon cannot fire (cloaking, cloaked, or the decloak lock), or an empty string.</summary>
        public static string GetCloakWeaponBlock(uint ship)
        {
            var now = DateTime.UtcNow;
            return ShipCloakRules.GetModuleBlock(_cloaks.GetPhase(ship, now), _cloaks.GetLockSecondsLeft(ship, now), false, true);
        }

        /// <summary>Why an ability cannot be used while the ship cloaks, or an empty string.</summary>
        public static string GetCloakAbilityBlock(uint ship) => ShipCloakRules.GetAbilityBlock(GetCloakPhase(ship));

        /// <summary>A cloaked ship slips past the pirate ambush roll.</summary>
        public static bool DodgesPirateAmbush(uint ship) => ShipCloakRules.DodgesAmbush(GetCloakPhase(ship));

        // Cloaking Device

        /// <summary>Why the Cloaking Device cannot run on this ship (hull or cargo), or an empty string.</summary>
        public static string CloakError(uint ship, ShipStatus status)
        {
            var hull = FindShipDetail(status.ItemTag)?.Hull;
            if (hull == null)
                return ShipCloakRules.HullMessage;

            // An Interdictor's hold keeps a ship from cloaking, and ends a cloak within a cycle (W11k).
            if (status.IsInterdicted(DateTime.UtcNow))
                return ShipElectronicWarfareRules.InterdictedMessage;

            var hasCargo = GetIsPC(ship) && Trade.GetUsedVolume(Trade.GetActiveShip(ship)) > 0;
            return ShipCloakRules.GetStartError(hull.Size, hull.Role, hasCargo);
        }

        /// <summary>
        /// One cycle of the device. The first starts the cloak; the cycles after it only draw capacitor.
        /// </summary>
        public static void RunCloakCycle(uint ship, ShipStatus status, ShipModuleStep step)
        {
            if (_cloaks.TryGet(ship, out _))
                return;

            BeginCloak(ship, status, step);
        }

        private static void BeginCloak(uint ship, ShipStatus status, ShipModuleStep step)
        {
            var hull = FindShipDetail(status.ItemTag)?.Hull;
            var slot = FindActiveCloakSlot(status);
            if (hull == null || slot == 0)
                return;

            var profile = ShipCloakRules.BuildProfile(hull.Role, hull.Tag, step.RevealRange);
            var state = _cloaks.Begin(ship, DateTime.UtcNow, profile, slot);

            // The ship uses nothing else while it cloaks.
            foreach (var other in Toggles(status).Where(x => x.Slot != slot).Select(x => x.Slot).ToList())
            {
                var name = TryGetFittedModule(status, other, out var module) && _shipModules.TryGetValue(module.ItemTag, out var detail)
                    ? detail.Name
                    : "Module";
                TurnOffToggle(ship, status, other,
                    ShipToggleRules.AutoOffMessage(ShipToggleAutoOffReason.Cloaked, name, string.Empty));
            }

            ApplyShipSpeed(ship, status);
            ApplyEffectToObject(DurationType.Temporary,
                EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), ship, (float)profile.EngageSeconds);
            SendMessageToPC(ship, ColorToken.Cyan($"Cloaking: your ship vanishes in {profile.EngageSeconds:0.#} seconds."));
            Gui.PublishRefreshEvent(ship, new ShipHudRefreshEvent());

            AssignCommand(ship, () => DelayCommand((float)profile.EngageSeconds, () => CompleteCloak(ship, state)));
        }

        private static void CompleteCloak(uint ship, ShipCloakState state)
        {
            if (!_cloaks.TryGet(ship, out var current) || current != state)
                return;

            if (!GetIsObjectValid(ship) || GetIsDead(ship) || !IsShipInSpace(ship))
            {
                EndCloak(ship, null, false);
                return;
            }

            VisibilityPlugin.SetVisibilityOverride(OBJECT_INVALID, ship, VisibilityType.Hidden);
            VisibilityPlugin.SetVisibilityOverride(ship, ship, VisibilityType.Visible);
            RefreshCloakVisibility(ship, state);

            ApplyEffectToObject(DurationType.Permanent,
                TagEffect(SupernaturalEffect(EffectVisualEffect(VisualEffect.Vfx_Dur_Ghost_Transparent)), CloakEffectTag), ship);
            SendMessageToPC(ship, ColorToken.Cyan("Your ship is cloaked."));
            Enmity.ReevaluateEnemyAttackTargets(ship);
            Gui.PublishRefreshEvent(ship, new ShipHudRefreshEvent());
        }

        /// <summary>
        /// Ends a ship's cloak: it is visible again, NPCs find it, and (when it had vanished) its weapons lock.
        /// <paramref name="turnOffDevice"/> also switches the device off when it is still on.
        /// </summary>
        private static void EndCloak(uint ship, string message, bool turnOffDevice = true)
        {
            if (!_cloaks.TryGet(ship, out var state))
                return;

            var end = _cloaks.End(ship, DateTime.UtcNow);
            RestoreCloakVisibility(ship, state);

            if (!GetIsObjectValid(ship))
                return;

            RemoveEffectByTag(ship, CloakEffectTag);
            if (end == ShipCloakEnd.Decloaked)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Magblue), ship);
                Enmity.ReevaluateEnemyAttackTargets(ship);
            }

            var status = GetShipStatus(ship);
            if (status != null)
            {
                ApplyShipSpeed(ship, status);
                if (turnOffDevice && IsShipToggleOn(status, state.Slot))
                    TurnOffToggle(ship, status, state.Slot);
            }

            if (GetIsPC(ship))
            {
                if (!string.IsNullOrWhiteSpace(message))
                    SendMessageToPC(ship, ColorToken.Red(message));

                SendMessageToPC(ship, end == ShipCloakEnd.Decloaked
                    ? ColorToken.Cyan($"Your ship is visible again. Weapons are locked for {CloakTuning.DecloakLockSeconds:0} seconds.")
                    : ColorToken.Cyan("The cloak failed."));
                Gui.PublishRefreshEvent(ship, new ShipHudRefreshEvent());
            }
        }

        /// <summary>Forgets a ship's cloak without messages when the ship leaves space (the flight ends it).</summary>
        private static void ClearCloakState(uint ship)
        {
            if (_cloaks.TryGet(ship, out var state))
            {
                RestoreCloakVisibility(ship, state);
                if (GetIsObjectValid(ship))
                    RemoveEffectByTag(ship, CloakEffectTag);
            }

            _cloaks.Clear(ship);
        }

        /// <summary>
        /// Taking damage breaks a cloak: one still engaging fails with no lock, one that had vanished decloaks with the
        /// lock. Deferred so the damage that caused it finishes saving first.
        /// </summary>
        private static void NotifyCloakDamaged(uint ship)
        {
            if (!_cloaks.TryGet(ship, out _))
                return;

            DelayCommand(0f, () =>
            {
                if (!_cloaks.TryGet(ship, out _))
                    return;

                EndCloak(ship, "Taking fire broke your cloak.");
            });
        }

        // Visibility

        /// <summary>Whether an observer sees a ship's pilot, party or crew: always the ship's own people.</summary>
        private static bool IsCloakAlly(uint ship, uint observer)
        {
            return GetIsPC(observer) && (SpaceCrew.GetCrew(ship).Contains(observer) || Party.IsInParty(ship, observer));
        }

        /// <summary>
        /// Whether a cloaked ship is hidden from an observer (a player or an NPC ship): not cloaked yet, the ship's own
        /// people, a Sensor Sweep reveal and any ship within the reveal range all see it.
        /// </summary>
        public static bool IsShipHiddenFrom(uint observer, uint target)
        {
            if (_cloaks.Count == 0 || !_cloaks.TryGet(target, out _))
                return false;

            if (!GetIsObjectValid(observer))
                return true;

            if (observer == target || GetIsDM(observer))
                return false;

            var distance = GetArea(observer) == GetArea(target) ? GetDistanceBetween(observer, target) : float.MaxValue;
            return _cloaks.IsHiddenFrom(target, observer, IsCloakAlly(target, observer), distance, DateTime.UtcNow);
        }

        /// <summary>Shows or hides the cloaked ship for every player in its area, by who can see through the cloak.</summary>
        private static void RefreshCloakVisibility(uint ship, ShipCloakState state)
        {
            var area = GetArea(ship);
            var now = DateTime.UtcNow;
            var seen = new HashSet<uint>();
            foreach (var player in ObjectSnapshot.Players())
            {
                if (player == ship || GetArea(player) != area)
                    continue;

                var distance = GetDistanceBetween(player, ship);
                if (!_cloaks.IsHiddenFrom(ship, player, IsCloakAlly(ship, player) || GetIsDM(player), distance, now))
                    seen.Add(player);
            }

            foreach (var player in seen.Except(state.VisibleTo))
            {
                VisibilityPlugin.SetVisibilityOverride(player, ship, VisibilityType.Visible);
            }

            foreach (var player in state.VisibleTo.Except(seen))
            {
                if (GetIsObjectValid(player))
                    VisibilityPlugin.SetVisibilityOverride(player, ship, VisibilityType.Default);
            }

            state.VisibleTo.Clear();
            state.VisibleTo.UnionWith(seen);
        }

        private static void RestoreCloakVisibility(uint ship, ShipCloakState state)
        {
            if (!GetIsObjectValid(ship))
                return;

            VisibilityPlugin.SetVisibilityOverride(OBJECT_INVALID, ship, VisibilityType.Default);
            VisibilityPlugin.SetVisibilityOverride(ship, ship, VisibilityType.Default);
            foreach (var player in state.VisibleTo)
            {
                if (GetIsObjectValid(player))
                    VisibilityPlugin.SetVisibilityOverride(player, ship, VisibilityType.Default);
            }

            state.VisibleTo.Clear();
        }

        /// <summary>A Sensor Sweep found the cloaked ship: the sweeper and their party see it for a few seconds.</summary>
        public static void RevealCloakedShip(uint ship, uint sweeper)
        {
            if (!_cloaks.TryGet(ship, out var state))
                return;

            var observers = Party.GetAllPartyMembers(sweeper).Append(sweeper).Distinct();
            _cloaks.Paint(ship, observers, DateTime.UtcNow.AddSeconds(CloakTuning.SweepRevealSeconds));
            RefreshCloakVisibility(ship, state);
        }

        /// <summary>Every second: ends cloaks whose ship is gone or whose device is off, and refreshes who sees each cloaked ship.</summary>
        private static void ProcessCloaks()
        {
            if (_cloaks.Count == 0)
                return;

            foreach (var ship in _cloaks.Ships)
            {
                if (!_cloaks.TryGet(ship, out var state))
                    continue;

                if (!GetIsObjectValid(ship) || GetIsDead(ship) || !IsShipInSpace(ship))
                {
                    ClearCloakState(ship);
                    continue;
                }

                var status = GetShipStatus(ship);
                if (status == null || !IsShipToggleOn(status, state.Slot))
                {
                    EndCloak(ship, null, false);
                    continue;
                }

                if (_cloaks.GetPhase(ship, DateTime.UtcNow) == ShipCloakPhase.Cloaked)
                    RefreshCloakVisibility(ship, state);
            }
        }

        // Ship HUD

        /// <summary>The Ship HUD's Cloak line, or null when the ship has no Cloaking Device.</summary>
        public static string GetCloakStatusText(uint ship, ShipStatus status)
        {
            var now = DateTime.UtcNow;
            var secondsLeft = _cloaks.TryGet(ship, out var state)
                ? ShipCloakRules.GetEngageSecondsLeft(state.StartedAt, state.Profile.EngageSeconds, now)
                : 0;
            return ShipCloakRules.GetStatusText(
                FindFittedCloakSlot(status) > 0, _cloaks.GetPhase(ship, now), secondsLeft, _cloaks.GetLockSecondsLeft(ship, now));
        }

        // Stealth control

        /// <summary>
        /// The stealth control never hides a ship by itself: native stealth is refused in space, and with a Cloaking
        /// Device fitted the control presses it instead.
        /// </summary>
        [NWNEventHandler(ScriptName.OnStealthEnterBefore)]
        public static void RedirectSpaceStealth()
        {
            var creature = OBJECT_SELF;
            if (!IsPlayerInSpaceMode(creature))
                return;

            EventsPlugin.SkipEvent();
            AssignCommand(creature, () =>
            {
                SetActionMode(creature, ActionMode.Stealth, false);
            });

            var slot = FindFittedCloakSlot(GetShipStatus(creature));
            if (slot == 0)
            {
                SendMessageToPC(creature, ColorToken.Red("Only a Cloaking Device hides a ship in space."));
                return;
            }

            ActivateShipModule(creature, SlotNumberToFeat(slot));
        }
    }
}
