using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The rift network (plan section 23): each Wild Space system keeps about two rifts open, to a random known orbit or
    /// another Wild Space system. Each end is a hidden Rift signature; a pilot whose party located it crosses with Jump
    /// to Signature. Rifts collapse at the end of their lifetime or when their mass is spent, after a warning; a ship
    /// left in a Wild Space system with no exit its hull fits is thrown to a random known orbit with hull damage.
    /// Rifts live in memory: a restart seeds a fresh network, and every ship's space position is cleared at boot, so
    /// no ship is left in Wild Space.
    /// </summary>
    public static partial class Space
    {
        private static readonly Dictionary<string, RiftConnection> _rifts = new();
        private static readonly List<(string Area, DateTime At)> _pendingRifts = new();
        private static bool _riftNetworkSeeded;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleRiftNetwork()
        {
            Scheduler.ScheduleRepeating(ProcessRiftNetwork, TimeSpan.FromSeconds(WildSpaceTuning.NetworkCheckSeconds), TimeSpan.FromSeconds(35));
        }

        /// <summary>The open rifts.</summary>
        public static IReadOnlyCollection<RiftConnection> GetRifts() => _rifts.Values;

        /// <summary>A rift by its ID, or null when it has collapsed.</summary>
        public static RiftConnection FindRift(string riftId) =>
            !string.IsNullOrEmpty(riftId) && _rifts.TryGetValue(riftId, out var rift) ? rift : null;

        /// <summary>Warns and collapses rifts that are due, and opens rifts where a Wild Space system is short of them.</summary>
        private static void ProcessRiftNetwork()
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var rift in _rifts.Values.ToList())
                {
                    if (RiftRules.IsWarningDue(rift, now))
                    {
                        rift.WarningSent = true;
                        WarnRiftCollapse(rift, now);
                    }

                    if (RiftRules.IsCollapseDue(rift, now))
                        CollapseRift(rift, now);
                }

                var pending = _pendingRifts.GroupBy(x => x.Area).ToDictionary(x => x.Key, x => x.Count());
                foreach (var system in RiftRules.SystemsShort(_rifts.Values, pending, now))
                    _pendingRifts.Add((system, RiftRules.ReplacementTime(_riftNetworkSeeded, now)));
                _riftNetworkSeeded = true;

                foreach (var due in _pendingRifts.Where(x => now >= x.At).ToList())
                {
                    _pendingRifts.Remove(due);
                    OpenRift(due.Area, now);
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Rift network failed: {ex}");
            }
        }

        /// <summary>The name a rift shows for an area: the system's name without the "Space - " prefix.</summary>
        private static string RiftAreaName(uint area)
        {
            var name = GetName(area);
            return name.StartsWith("Space - ") ? name["Space - ".Length..] : name;
        }

        /// <summary>Opens a rift from a Wild Space system and places a hidden signature at each end.</summary>
        private static RiftConnection OpenRift(string systemResref, DateTime now)
        {
            var system = Area.GetAreaByResref(systemResref);
            if (!GetIsObjectValid(system))
                return null;

            var open = WildSpaceTuning.Systems.ToDictionary(x => x.AreaResref, x => RiftRules.OpenConnections(x.AreaResref, _rifts.Values, now));
            var destinationResref = RiftRules.PickDestination(systemResref, open, max => Random.Next(max));
            var destination = Area.GetAreaByResref(destinationResref);
            if (!GetIsObjectValid(destination))
                return null;

            var rift = RiftRules.Create(Guid.NewGuid().ToString(), systemResref, destinationResref,
                RiftRules.RollSize(max => Random.Next(max)), now, RiftRules.RollLifetimeSeconds(max => Random.Next(max)));
            var endA = PlaceRiftEnd(rift, system, RiftRules.Describe(rift, RiftAreaName(destination)), now);
            var endB = PlaceRiftEnd(rift, destination, RiftRules.Describe(rift, RiftAreaName(system)), now);
            if (endA == null || endB == null)
            {
                if (endA != null) RemoveSignature(endA);
                if (endB != null) RemoveSignature(endB);
                return null;
            }

            rift.SignatureA = endA.Id;
            rift.SignatureB = endB.Id;
            _rifts[rift.Id] = rift;
            Log.Write(LogGroup.Space, $"RIFT OPENED - {rift.Size} {systemResref} <-> {destinationResref}, collapses {rift.CollapsesAt:u}");
            return rift;
        }

        private static ScanSignature PlaceRiftEnd(RiftConnection rift, uint area, string detail, DateTime now)
        {
            var position = GetPositionFromLocation(Walkmesh.GetRandomLocation(area));
            if (position.X == 0f && position.Y == 0f)
                return null;

            var signature = SignatureSiteRules.CreateRiftEnd(Guid.NewGuid().ToString(), GetResRef(area), rift.Id,
                position.X, position.Y, position.Z, now, rift.CollapsesAt, detail);
            PlaceSignature(signature);
            return signature;
        }

        /// <summary>The pilots flying in an area.</summary>
        private static List<uint> GetPilotsInArea(string areaResref)
        {
            var pilots = new List<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (IsPlayerInSpaceMode(player) && GetResRef(GetArea(player)) == areaResref)
                    pilots.Add(player);
            }

            return pilots;
        }

        /// <summary>Tells the pilots in the Wild Space system(s) a rift serves that it is about to collapse.</summary>
        private static void WarnRiftCollapse(RiftConnection rift, DateTime now)
        {
            var seconds = Math.Max(0, (int)Math.Ceiling((rift.CollapsesAt - now).TotalSeconds));
            foreach (var end in new[] { rift.AreaA, rift.AreaB }.Where(WildSpaceRules.IsWildSpaceSystem).Distinct())
            {
                var other = Area.GetAreaByResref(rift.OtherEnd(end));
                foreach (var pilot in GetPilotsInArea(end))
                {
                    SendMessageToPC(pilot, ColorToken.Red(
                        $"Sensors warn that the rift to {RiftAreaName(other)} is collapsing: it closes in {seconds} seconds. " +
                        "A ship left with no way out when it closes is thrown clear and damaged."));
                }
            }
        }

        /// <summary>Closes a rift, removes its ends and throws out the ships it left in Wild Space with no exit.</summary>
        private static void CollapseRift(RiftConnection rift, DateTime now)
        {
            rift.Collapsed = true;
            _rifts.Remove(rift.Id);
            foreach (var signatureId in new[] { rift.SignatureA, rift.SignatureB })
            {
                var signature = FindSignature(signatureId);
                if (signature != null)
                    RemoveSignature(signature);
            }

            Log.Write(LogGroup.Space, $"RIFT COLLAPSED - {rift.Size} {rift.AreaA} <-> {rift.AreaB}, {rift.MassRemaining} mass left");
            foreach (var end in new[] { rift.AreaA, rift.AreaB }.Where(WildSpaceRules.IsWildSpaceSystem).Distinct())
                EjectStrandedShips(end, now);
        }

        /// <summary>Throws every ship in a Wild Space system that has no exit its hull fits to a random known orbit.</summary>
        private static void EjectStrandedShips(string systemResref, DateTime now)
        {
            foreach (var pilot in GetPilotsInArea(systemResref))
            {
                var status = GetShipStatus(pilot);
                if (status == null || RiftRules.HasExit(systemResref, status.Size, _rifts.Values, now))
                    continue;

                EjectPilotFromWildSpace(pilot, status, systemResref);
            }

            // Ships whose pilots logged out in space stay as clones; they are thrown out the same way.
            foreach (var (shipId, clone) in _shipClones.ToList())
            {
                if (!GetIsObjectValid(clone) || GetResRef(GetArea(clone)) != systemResref)
                    continue;

                var dbShip = DB.Get<PlayerShip>(shipId);
                if (dbShip == null || RiftRules.HasExit(systemResref, dbShip.Status.Size, _rifts.Values, now))
                    continue;

                EjectShipCloneFromWildSpace(dbShip, clone, systemResref);
            }
        }

        /// <summary>A random point in a random known orbit, or null when none can be found.</summary>
        private static Location PickEjectionPoint(out string orbitResref)
        {
            orbitResref = WildSpaceTuning.KnownOrbits[Random.Next(WildSpaceTuning.KnownOrbits.Length)];
            var orbit = Area.GetAreaByResref(orbitResref);
            if (!GetIsObjectValid(orbit))
                return null;

            var location = Walkmesh.GetRandomLocation(orbit);
            var position = GetPositionFromLocation(location);
            return position.X == 0f && position.Y == 0f ? null : location;
        }

        private static void EjectPilotFromWildSpace(uint pilot, ShipStatus status, string systemResref)
        {
            var location = PickEjectionPoint(out var orbitResref);
            if (location == null)
                return;

            var hullBefore = status.Hull;
            status.Shield = 0;
            status.Hull = RiftRules.HullAfterEjection(status.Hull, status.MaxHull);
            SaveShipStatus(pilot, status);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Explosion), pilot);
            AssignCommand(pilot, () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });
            SendMessageToPC(pilot, ColorToken.Red(
                $"The last rift out collapses. Its shockwave throws your ship clear to {RiftAreaName(GetAreaFromLocation(location))}: " +
                $"shields down and {hullBefore - status.Hull} hull lost."));
            Log.Write(LogGroup.Space, $"RIFT EJECTION - {PlayerName.GetAuditName(pilot)} from {systemResref} to {orbitResref}, hull {hullBefore} -> {status.Hull}");
        }

        private static void EjectShipCloneFromWildSpace(PlayerShip dbShip, uint clone, string systemResref)
        {
            var location = PickEjectionPoint(out var orbitResref);
            if (location == null)
                return;

            var hullBefore = dbShip.Status.Hull;
            dbShip.Status.Shield = 0;
            dbShip.Status.Hull = RiftRules.HullAfterEjection(dbShip.Status.Hull, dbShip.Status.MaxHull);
            DB.Set(dbShip);

            var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
            if (dbProperty != null)
            {
                var position = GetPositionFromLocation(location);
                dbProperty.Positions[PropertyLocationType.CurrentPosition] = new PropertyLocation
                {
                    AreaResref = orbitResref,
                    X = position.X,
                    Y = position.Y,
                    Z = position.Z,
                    Orientation = GetFacingFromLocation(location),
                };
                DB.Set(dbProperty);
            }

            AssignCommand(clone, () => JumpToLocation(location));
            Log.Write(LogGroup.Space, $"RIFT EJECTION - unpiloted ship {dbShip.Id} from {systemResref} to {orbitResref}, hull {hullBefore} -> {dbShip.Status.Hull}");
        }

        /// <summary>
        /// Crosses a located rift: the ship spends the rift's mass for its hull and drops out beside the far end, which it
        /// then knows, so the party can come back the same way.
        /// </summary>
        private static void CrossRift(uint pilot, ScanSignature signature)
        {
            var now = DateTime.UtcNow;
            var rift = FindRift(signature.RiftId);
            var status = GetShipStatus(pilot);
            var refusal = rift == null || status == null
                ? RiftPassageRefusal.Collapsed
                : RiftRules.GetPassageRefusal(rift, status.Size, now);
            if (refusal != RiftPassageRefusal.None)
            {
                SendMessageToPC(pilot, ColorToken.Red(RiftRules.Message(refusal)));
                return;
            }

            var destinationResref = rift.OtherEnd(signature.AreaResref);
            var destination = Area.GetAreaByResref(destinationResref);
            var farEnd = FindSignature(rift.SignatureIn(destinationResref));
            if (!GetIsObjectValid(destination) || farEnd == null)
            {
                SendMessageToPC(pilot, ColorToken.Red(RiftRules.Message(RiftPassageRefusal.Collapsed)));
                return;
            }

            if (!CanFlownShipEnterArea(pilot, destination))
                return;

            RiftRules.SpendPassage(rift, status.Size, now);
            farEnd.Signal[GetObjectUUID(pilot)] = farEnd.SignalRequired;
            farEnd.RevealedAt ??= now;

            var (x, y) = SignatureJumpRules.Arrival(farEnd.X + 1f, farEnd.Y, farEnd.X, farEnd.Y);
            var arrival = Location(destination, Vector3(x, y, farEnd.Z), Random.Next(360));
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), GetLocation(pilot));
            AssignCommand(pilot, () =>
            {
                ClearAllActions();
                JumpToLocation(arrival);
            });
            DelayCommand(0.5f, () => ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Magblue), pilot));
            SendMessageToPC(pilot, ColorToken.Cyan($"You cross the rift into {RiftAreaName(destination)}. Its far end stays on your scanner."));
            Log.Write(LogGroup.Space, $"RIFT CROSSED - {PlayerName.GetAuditName(pilot)} {signature.AreaResref} -> {destinationResref} ({status.Size}, {rift.MassRemaining} mass left)");
        }
    }
}
