using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Ion Web, Interdictor, Jammer, Target Painter and Afterburner (W11k, plan Appendix B). Each hostile module
    /// renews its effect on the ship's target every cycle while it is on, so the effect lapses a moment after the module
    /// switches off. A web, jam or paint works at the effectiveness of whoever runs it, as the E-War Array does. An
    /// Interdictor's hold stops its target jumping a starlane or to a signature, docking, boarding, crossing a rift,
    /// opening a Rift Run, cloaking and running an Afterburner. A web or hold also breaks a cloak on its target, as
    /// damage does.
    /// </summary>
    public static partial class Space
    {
        private const string IonWebEffectTag = "SPACE_ION_WEB";
        public const string HostileTargetMessage = "Target an enemy ship.";

        /// <summary>Whether an Interdictor holds the ship right now.</summary>
        public static bool IsShipInterdicted(uint ship) => GetShipStatus(ship)?.IsInterdicted(DateTime.UtcNow) == true;

        /// <summary>Why an Interdictor stops the ship jumping, docking or leaving space, or an empty string.</summary>
        public static string GetInterdictionRefusal(uint ship) =>
            IsShipInterdicted(ship) ? ShipElectronicWarfareRules.InterdictedMessage : string.Empty;

        /// <summary>Tells the pilot when an Interdictor stops them jumping, docking or leaving space, and returns true then.</summary>
        public static bool RefuseWhileInterdicted(uint ship)
        {
            var refusal = GetInterdictionRefusal(ship);
            if (string.IsNullOrEmpty(refusal))
                return false;

            SendMessageToPC(ship, ColorToken.Red(refusal));
            return true;
        }

        /// <summary>An error for a hostile E-War module aimed at no ship or at its own ship, or an empty string.</summary>
        public static string HostileElectronicWarfareError(uint activator, uint target, ShipStatus targetStatus) =>
            target == activator || targetStatus == null || !GetIsObjectValid(target) ? HostileTargetMessage : string.Empty;

        /// <summary>An Ion Web's error: the target must be another ship whose hull does not shrug webs off.</summary>
        public static string IonWebError(uint activator, uint target, ShipStatus targetStatus)
        {
            var error = HostileElectronicWarfareError(activator, target, targetStatus);
            if (!string.IsNullOrEmpty(error))
                return error;

            return ShipElectronicWarfareRules.IsWebImmune(targetStatus.ItemTag) ? ShipElectronicWarfareRules.WebImmuneMessage : string.Empty;
        }

        /// <summary>An Ion Web's range for the firing hull (the Interceptor and Republic Scout bonuses).</summary>
        public static float GetIonWebRange(ShipStatus status, int baseRange)
        {
            var hull = FindShipDetail(status?.ItemTag)?.Hull;
            return hull == null ? baseRange : ShipElectronicWarfareRules.WebRange(baseRange, hull.Role, hull.Tag);
        }

        /// <summary>An Interdictor's range for the firing hull (the Interceptor bonus).</summary>
        public static float GetInterdictorRange(ShipStatus status, int baseRange)
        {
            var hull = FindShipDetail(status?.ItemTag)?.Hull;
            return hull == null ? baseRange : ShipElectronicWarfareRules.InterdictorRange(baseRange, hull.Role);
        }

        /// <summary>One Ion Web cycle: the target is slowed until a moment after the next cycle is due.</summary>
        public static void ApplyIonWeb(uint activator, ShipStatus status, uint target, ShipModuleStep step)
        {
            var hull = FindShipDetail(status?.ItemTag)?.Hull;
            var percent = ShipElectronicWarfareRules.WebPercent(step.WebPercent, hull?.Role ?? ShipRole.Invalid);
            percent = ShipCrewSeats.ScaleByEffectiveness(percent, SpaceCrew.GetFireEffectiveness(activator));
            var seconds = (float)ShipElectronicWarfareRules.HoldSeconds(step.CycleSeconds);

            RemoveEffectByTag(target, IonWebEffectTag);
            if (percent > 0)
                ApplyEffectToObject(DurationType.Temporary, TagEffect(EffectMovementSpeedDecrease(percent), IonWebEffectTag), target, seconds);

            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Chest), target, 1.0f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Slow), target);
            BreakCloakByElectronicWarfare(target, "An Ion Web broke your cloak.");
        }

        /// <summary>
        /// One Interdictor cycle: the target is held until a moment after the next cycle is due. Its Afterburner shuts
        /// down, and the first cycle of a hold breaks off a spooling signature jump and tells its pilot.
        /// </summary>
        public static void ApplyInterdiction(uint activator, uint target, ShipStatus targetStatus, ShipModuleStep step)
        {
            var now = DateTime.UtcNow;
            var wasHeld = targetStatus.IsInterdicted(now);
            ShipElectronicWarfareRules.Interdict(targetStatus, ShipElectronicWarfareRules.HoldSeconds(step.CycleSeconds), now);

            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest), target, 1.0f);
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_Black), target, 1.0f);

            ShutDownAfterburners(target, targetStatus);
            BreakCloakByElectronicWarfare(target, "An Interdictor broke your cloak.");
            if (wasHeld)
                return;

            if (_signatureJumps.Remove(target))
                SendMessageToPC(target, ColorToken.Red(SignatureJumpRules.Message(SignatureJumpRefusal.Interdicted)));

            if (GetIsPC(target))
            {
                SendMessageToPC(target, ColorToken.Red($"{ShipElectronicWarfareRules.InterdictedMessage} You cannot jump, dock or cloak while it holds."));
                Gui.PublishRefreshEvent(target, new ShipHudRefreshEvent());
            }
        }

        /// <summary>One Jammer cycle: the target's Accuracy drops until a moment after the next cycle is due.</summary>
        public static void ApplyJam(uint activator, uint target, ShipStatus targetStatus, ShipModuleStep step)
        {
            var penalty = ShipCrewSeats.ScaleByEffectiveness(step.TargetAccuracy, SpaceCrew.GetFireEffectiveness(activator));
            ShipElectronicWarfareRules.Jam(targetStatus, penalty, ShipElectronicWarfareRules.HoldSeconds(step.CycleSeconds), DateTime.UtcNow);

            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest), target, 1.0f);
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), target, 1.0f);
        }

        /// <summary>One Target Painter cycle: the target takes more damage until a moment after the next cycle is due.</summary>
        public static void ApplyPaint(uint activator, uint target, ShipStatus targetStatus, ShipModuleStep step)
        {
            var percent = ShipCrewSeats.ScaleByEffectiveness(step.PaintPercent, SpaceCrew.GetFireEffectiveness(activator));
            ShipElectronicWarfareRules.Paint(targetStatus, percent, ShipElectronicWarfareRules.HoldSeconds(step.CycleSeconds), DateTime.UtcNow);

            ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Fire, activator, BodyNode.Chest), target, 1.0f);
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_Orange), target, 1.0f);
        }

        /// <summary>Why the ship's Afterburner cannot run (an Interdictor holds it), or an empty string.</summary>
        public static string AfterburnerError(ShipStatus status) =>
            ShipAfterburnerRules.GetError(status != null && status.IsInterdicted(DateTime.UtcNow));

        /// <summary>One Afterburner cycle: the toggle pays its capacitor; the ship's speed follows the running toggle.</summary>
        public static void RunAfterburnerCycle(uint ship, ShipStatus status)
        {
            ApplyShipSpeed(ship, status);
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Orange_White), ship, 1.0f);
        }

        /// <summary>The strongest running Afterburner's speed percent, or 0 when none runs.</summary>
        public static int GetAfterburnerPercent(ShipStatus status)
        {
            var percent = 0;
            foreach (var slot in GetRunningAfterburnerSlots(status))
            {
                if (TryGetFittedModule(status, slot, out var module) && _shipModules.TryGetValue(module.ItemTag, out var detail))
                    percent = Math.Max(percent, detail.Step?.AfterburnerPercent ?? 0);
            }

            return percent;
        }

        /// <summary>Whether the slot holds an Afterburner.</summary>
        private static bool IsAfterburnerSlot(ShipStatus status, int slot) =>
            TryGetFittedModule(status, slot, out var module) &&
            _shipModules.TryGetValue(module.ItemTag, out var detail) &&
            detail.Type == ShipModuleType.Afterburner;

        private static List<int> GetRunningAfterburnerSlots(ShipStatus status) =>
            status?.ActiveModules == null
                ? new List<int>()
                : status.ActiveModules.Select(x => x.Slot).Where(slot => IsAfterburnerSlot(status, slot)).ToList();

        /// <summary>Switches off every running Afterburner on a held ship, with the usual auto-off message.</summary>
        private static void ShutDownAfterburners(uint ship, ShipStatus status)
        {
            foreach (var slot in GetRunningAfterburnerSlots(status))
            {
                var name = TryGetFittedModule(status, slot, out var module) && _shipModules.TryGetValue(module.ItemTag, out var detail)
                    ? detail.Name
                    : "Afterburner";
                TurnOffToggle(ship, status, slot,
                    ShipToggleRules.AutoOffMessage(ShipToggleAutoOffReason.ValidationFailed, name, ShipElectronicWarfareRules.InterdictedMessage));
            }
        }

        /// <summary>A web or hold breaks the target's cloak, as damage does (plan section 32, W11k).</summary>
        private static void BreakCloakByElectronicWarfare(uint ship, string message)
        {
            if (!_cloaks.TryGet(ship, out _))
                return;

            DelayCommand(0f, () =>
            {
                if (_cloaks.TryGet(ship, out _))
                    EndCloak(ship, message);
            });
        }
    }
}
