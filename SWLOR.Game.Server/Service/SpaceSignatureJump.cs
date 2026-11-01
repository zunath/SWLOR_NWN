using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    /// Jump to Signature and Sensor Sweep (plan section 24). A pilot whose party has located a site micro-jumps to it
    /// after a spool that damage breaks; the Probe Launcher's Sensor Sweep lists every ship in range on the Scanner tab.
    /// </summary>
    public static partial class Space
    {
        private const float AtSiteMeters = 40f;

        private class PendingSignatureJump
        {
            public string SignatureId { get; init; }
            public DateTime StartedAt { get; init; }
        }

        private class SensorSweepResult
        {
            public DateTime SweptAt { get; init; }
            public List<SensorSweepContact> Contacts { get; init; }
        }

        private static readonly Dictionary<uint, PendingSignatureJump> _signatureJumps = new();
        private static readonly Dictionary<uint, DateTime> _lastSensorSweep = new();
        private static readonly Dictionary<uint, SensorSweepResult> _sensorSweeps = new();

        [NWNEventHandler(ScriptName.OnFeatUseBefore)]
        public static void HandleJumpToSignatureFeat()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));
            if (feat != FeatType.ShipJumpToSignature)
                return;

            var player = OBJECT_SELF;
            var signature = FindNearestJumpableSignature(player);
            if (signature == null)
            {
                SendMessageToPC(player, ColorToken.Red("Your party has located no signature to jump to here."));
                return;
            }

            BeginSignatureJump(player, signature.Id);
        }

        /// <summary>
        /// The nearest site the player's party has located, other than one the ship is already at. A located rift counts
        /// at any distance: jumping to it crosses it.
        /// </summary>
        private static ScanSignature FindNearestJumpableSignature(uint player)
        {
            var party = GetScanPartyIds(player);
            var position = GetPosition(player);
            return GetSignatures(GetResRef(GetArea(player)))
                .Where(x => ScanningRules.CanJump(x, party))
                .Select(x => (Signature: x, Distance: Distance(position, x)))
                .Where(x => x.Distance > AtSiteMeters || x.Signature.Type == SignatureType.Rift)
                .OrderBy(x => x.Distance)
                .Select(x => x.Signature)
                .FirstOrDefault();
        }

        private static float Distance(Vector3 position, ScanSignature signature)
        {
            var dx = position.X - signature.X;
            var dy = position.Y - signature.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Puts the Jump to Signature feat in the first empty quickbar slot when the ship has a Probe Launcher.</summary>
        public static void EnsureJumpToSignatureOnHotBar(uint player, ShipStatus status)
        {
            if (GetBestProbeLauncher(status) == null)
                return;

            var firstEmpty = -1;
            for (var index = 0; index < QuickBarSlotCount; index++)
            {
                var slot = PlayerPlugin.GetQuickBarSlot(player, index);
                if (slot.ObjectType == QuickBarSlotType.Feat && slot.INTParam1 == (int)FeatType.ShipJumpToSignature)
                    return;

                if (firstEmpty < 0 && slot.ObjectType == QuickBarSlotType.Empty)
                    firstEmpty = index;
            }

            if (firstEmpty >= 0)
                PlayerPlugin.SetQuickBarSlot(player, firstEmpty, PlayerQuickBarSlot.UseFeat(FeatType.ShipJumpToSignature));
        }

        /// <summary>Whether a pilot's jump drive is spooling.</summary>
        public static bool IsSpoolingSignatureJump(uint player) => _signatureJumps.ContainsKey(player);

        /// <summary>Starts the spool of a jump to a signature, or tells the pilot why it cannot start.</summary>
        public static bool BeginSignatureJump(uint player, string signatureId)
        {
            var cloakBlock = GetCloakAbilityBlock(player);
            if (!string.IsNullOrEmpty(cloakBlock))
            {
                SendMessageToPC(player, ColorToken.Red(cloakBlock));
                return false;
            }

            // An Interdictor's hold keeps the drive from spooling (W11k); a hold that starts mid-spool breaks it off.
            if (IsShipInterdicted(player))
            {
                SendMessageToPC(player, ColorToken.Red(SignatureJumpRules.Message(SignatureJumpRefusal.Interdicted)));
                return false;
            }

            var signature = FindSignature(signatureId);
            var refusal = SignatureJumpRules.GetStartRefusal(
                IsPlayerInSpaceMode(player),
                signature,
                GetResRef(GetArea(player)),
                ScanningRules.CanJump(signature, GetScanPartyIds(player)),
                IsSpoolingSignatureJump(player));
            if (refusal != SignatureJumpRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(SignatureJumpRules.Message(refusal)));
                return false;
            }

            // A rift turns away a hull it cannot carry before the drive spools.
            if (signature.Type == SignatureType.Rift)
            {
                var status = GetShipStatus(player);
                var riftRefusal = status == null
                    ? RiftPassageRefusal.Collapsed
                    : RiftRules.GetPassageRefusal(FindRift(signature.RiftId), status.Size, DateTime.UtcNow);
                if (riftRefusal != RiftPassageRefusal.None)
                {
                    SendMessageToPC(player, ColorToken.Red(RiftRules.Message(riftRefusal)));
                    return false;
                }
            }

            var pending = new PendingSignatureJump { SignatureId = signatureId, StartedAt = DateTime.UtcNow };
            _signatureJumps[player] = pending;
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Glow_Light_Blue), player, (float)ScanningTuning.JumpSpoolSeconds);
            SendMessageToPC(player, ColorToken.Cyan($"Spooling the jump drive: {ScanningTuning.JumpSpoolSeconds:0} seconds. Taking fire breaks it off."));
            AssignCommand(player, () => DelayCommand((float)ScanningTuning.JumpSpoolSeconds, () => CompleteSignatureJump(player, pending)));
            return true;
        }

        /// <summary>Breaks off a spooling jump when the ship takes damage.</summary>
        private static void InterruptSignatureJump(uint ship)
        {
            if (!_signatureJumps.Remove(ship))
                return;

            SendMessageToPC(ship, ColorToken.Red(SignatureJumpRules.Message(SignatureJumpRefusal.Damaged)));
        }

        private static void CompleteSignatureJump(uint player, PendingSignatureJump pending)
        {
            if (!_signatureJumps.TryGetValue(player, out var current) || current != pending)
                return;

            _signatureJumps.Remove(player);
            if (!GetIsObjectValid(player) || !IsPlayerInSpaceMode(player))
                return;

            var signature = FindSignature(pending.SignatureId);
            var refusal = SignatureJumpRules.GetCompletionRefusal(
                false,
                IsShipInterdicted(player),
                ScanningRules.CanJump(signature, GetScanPartyIds(player)),
                signature != null && signature.AreaResref == GetResRef(GetArea(player)));
            if (refusal != SignatureJumpRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(SignatureJumpRules.Message(refusal)));
                return;
            }

            if (signature.Type == SignatureType.Rift)
            {
                CrossRift(player, signature);
                return;
            }

            var from = GetPosition(player);
            var (x, y) = SignatureJumpRules.Arrival(from.X, from.Y, signature.X, signature.Y);
            var facing = (float)(Math.Atan2(signature.Y - y, signature.X - x) * 180.0 / Math.PI);
            var arrival = Location(GetArea(player), Vector3(x, y, signature.Z), facing);

            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), GetLocation(player));
            AssignCommand(player, () =>
            {
                ClearAllActions();
                JumpToLocation(arrival);
            });
            DelayCommand(0.5f, () => ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Magblue), player));
            SendMessageToPC(player, ColorToken.Cyan($"Jumped to the {ScanningRules.TypeName(signature.Type)}."));
            GrantSiteAchievement(player, signature.Type);
        }

        // Sensor Sweep

        /// <summary>Seconds until the ship can sweep again, 0 when ready.</summary>
        public static int GetSensorSweepSecondsLeft(uint player) =>
            SensorSweepRules.SecondsLeft(_lastSensorSweep.TryGetValue(player, out var last) ? last : null, DateTime.UtcNow);

        /// <summary>The ships the player's last sweep found, while its list is still shown.</summary>
        public static List<SensorSweepContact> GetSensorSweepContacts(uint player)
        {
            if (!_sensorSweeps.TryGetValue(player, out var result))
                return new List<SensorSweepContact>();

            if (SensorSweepRules.IsResultFresh(result.SweptAt, DateTime.UtcNow))
                return result.Contacts;

            _sensorSweeps.Remove(player);
            return new List<SensorSweepContact>();
        }

        /// <summary>
        /// Pulses the ship's best Probe Launcher: lists every ship within its sweep range, cloaked ships included, and
        /// starts the sweep's cooldown. Only the pilot sweeps.
        /// </summary>
        public static bool RunSensorSweep(uint player)
        {
            if (!IsPlayerInSpaceMode(player))
            {
                SendMessageToPC(player, ColorToken.Red("Only the pilot can run a Sensor Sweep."));
                return false;
            }

            var status = GetShipStatus(player);
            if (GetBestProbeLauncher(status) == null)
            {
                SendMessageToPC(player, ColorToken.Red("A Sensor Sweep needs a Probe Launcher fitted."));
                return false;
            }

            // A launcher in a station slot sweeps only while someone who meets its rank works that station.
            var launcher = GetBestWorkedProbeLauncher(player, status);
            if (launcher == null)
            {
                SendMessageToPC(player, ColorToken.Red("A Sensor Sweep needs someone qualified working the station with the Probe Launcher."));
                return false;
            }

            var cloakBlock = ShipCloakRules.GetModuleBlock(GetCloakPhase(player), 0, false, false);
            if (!string.IsNullOrEmpty(cloakBlock))
            {
                SendMessageToPC(player, ColorToken.Red(cloakBlock));
                return false;
            }

            var now = DateTime.UtcNow;
            var last = _lastSensorSweep.TryGetValue(player, out var at) ? at : (DateTime?)null;
            if (!SensorSweepRules.IsReady(last, now))
            {
                SendMessageToPC(player, ColorToken.Red($"The Sensor Sweep is recharging: {SensorSweepRules.SecondsLeft(last, now)} seconds."));
                return false;
            }

            if (status.Capacitor < ScanningTuning.SweepCapacitor)
            {
                SendMessageToPC(player, ColorToken.Red("Not enough capacitor for a Sensor Sweep."));
                return false;
            }

            status.Capacitor -= ScanningTuning.SweepCapacitor;
            SaveShipStatus(player, status);
            _lastSensorSweep[player] = now;

            var range = launcher.SweepRange > 0 ? launcher.SweepRange : SensorSweepRules.Range(launcher.Rank);
            var contacts = new List<SensorSweepContact>();
            foreach (var ship in ObjectSnapshot.InShape(Shape.Sphere, range, GetLocation(player), false, ObjectType.Creature))
            {
                if (ship == player || !GetIsObjectValid(ship) || GetIsDead(ship))
                    continue;

                var shipStatus = GetShipStatus(ship);
                if (shipStatus == null)
                    continue;

                var distance = GetDistanceBetween(player, ship);
                var cloaked = IsShipCloaked(ship);
                if (!SensorSweepRules.Includes(distance, range, cloaked))
                    continue;

                // The sweep sees through the cloak: the sweeper's party sees the ship for a few seconds.
                if (cloaked)
                    RevealCloakedShip(ship, player);

                contacts.Add(new SensorSweepContact
                {
                    Name = PlayerName.GetDisplayName(player, ship),
                    Distance = distance,
                    Size = shipStatus.Size,
                    Hostile = IsShipEnemy(player, ship),
                    Cloaked = cloaked,
                });
            }

            var ordered = SensorSweepRules.Order(contacts);
            _sensorSweeps[player] = new SensorSweepResult { SweptAt = now, Contacts = ordered };
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White), player, 1.0f);
            SendMessageToPC(player, ColorToken.Cyan($"Sensor Sweep: {ordered.Count} ship(s) within {range} m."));
            Gui.PublishRefreshEvent(player, new ShipHudRefreshEvent());
            return true;
        }

        /// <summary>Forgets a pilot's jump, sweep and its cooldown when they leave space.</summary>
        private static void ClearScanningState(uint player)
        {
            _signatureJumps.Remove(player);
            _sensorSweeps.Remove(player);
            _lastSensorSweep.Remove(player);
        }
    }
}
