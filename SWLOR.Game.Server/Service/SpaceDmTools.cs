using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What the DM Tools Space tab's derelict, signature, Rift Pocket and cloak buttons run. Each reuses the shipped code
    /// (the signature book and its sites, the Rift Run opener and closer, the cloak) and answers with the message the
    /// tab shows. The rules and texts are <see cref="DmSpaceToolRules"/>.
    /// </summary>
    public static partial class Space
    {
        /// <summary>
        /// Places a signature of <paramref name="type"/> at a point in the DM's area. A revealed signature opens its site
        /// at once (Place Derelict); a hidden one waits for a Probe Launcher (Place Signature). A DM-placed derelict is
        /// boardable.
        /// </summary>
        public static (bool Placed, string Message) DmPlaceSignature(uint dm, uint controlled, SignatureType type, Vector3 position, bool reveal)
        {
            var area = GetArea(controlled);
            var resref = GetResRef(area);
            var refusal = DmSpaceToolRules.GetPlaceRefusal(IsSpaceArea(area), resref);
            if (!string.IsNullOrEmpty(refusal))
                return (false, refusal);

            var detail = SignatureSiteRules.GetArea(resref);
            var signature = SignatureSiteRules.Create(Guid.NewGuid().ToString(), resref, detail.Tier, type,
                position.X, position.Y, position.Z, DateTime.UtcNow, locked: false, wildSpace: detail.WildSpace,
                boardable: type == SignatureType.Derelict, contested: false);
            PlaceSignature(signature);
            if (reveal)
                RevealSite(signature, controlled, openToAll: true);

            Log.Write(LogGroup.DM, $"{GetName(dm)} placed a {(reveal ? "revealed" : "hidden")} {type} (tier {signature.Tier}) in {GetName(area)} at {position.X:0},{position.Y:0}.");
            return (true, DmSpaceToolRules.DescribePlaced(signature, reveal));
        }

        /// <summary>Opens a Rift Pocket of the key's tier for a piloting player and the party ships near them, free of a key.</summary>
        public static (bool Opened, string Message) DmOpenRiftPocket(uint dm, uint target, RiftKeyDetail key)
        {
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target))
                return (false, "Pick a player's ship.");

            var refusal = GetRiftOpenRefusal(target, key);
            if (!string.IsNullOrEmpty(refusal))
                return (false, refusal);

            if (!OpenRiftRun(target, key, OBJECT_INVALID))
                return (false, "The pocket could not be created.");

            Log.Write(LogGroup.DM, $"{GetName(dm)} opened a tier {key.TierLabel} Rift Pocket for {PlayerName.GetAuditName(target)}.");
            return (true, $"Opened a tier {key.TierLabel} Rift Pocket for {GetName(target)}. No key was spent.");
        }

        /// <summary>
        /// Closes the Rift Pocket the DM stands in without destroying anything: its ships go back where they opened it
        /// and the pocket is removed once nobody is inside, the DM included.
        /// </summary>
        public static (bool Closed, string Message) DmCloseRiftPocket(uint dm, uint controlled)
        {
            var area = GetArea(controlled);
            var run = _riftRuns.Values.FirstOrDefault(x => x.Area == area && GetIsObjectValid(area));
            if (run == null)
                return (false, "Stand inside a Rift Pocket to close it.");

            MessageRiftRun(run, ColorToken.Orange("A DM closes the rift pocket. The rift returns you."));
            foreach (var pilot in run.Pilots.ToList())
                ReturnFromRiftPocket(run, pilot);

            run.Phase = RiftRunPhase.Closing;
            Log.Write(LogGroup.DM, $"{GetName(dm)} closed the tier {run.Key.TierLabel} Rift Pocket {run.Id}.");
            return (true, "Closed the Rift Pocket. Its ships return; it is removed once everyone, you included, has left.");
        }

        /// <summary>One line per open Rift Pocket, for the DM.</summary>
        public static List<string> DmListRiftPockets()
        {
            var now = DateTime.UtcNow;
            return _riftRuns.Values.Select(x => DmSpaceToolRules.DescribeRiftRun(x, now)).ToList();
        }

        /// <summary>The summary the tab shows for the open Rift Pockets.</summary>
        public static string DmDescribeRiftPockets() => DmSpaceToolRules.DescribeRiftRuns(_riftRuns.Values.ToList());

        /// <summary>Breaks a ship's cloak, as if it had been hit: the ship is visible again and its weapons lock for a moment.</summary>
        public static (bool Decloaked, string Message) DmDecloakShip(uint dm, uint ship)
        {
            if (!GetIsObjectValid(ship) || GetObjectType(ship) != ObjectType.Creature)
                return (false, "Pick a ship.");

            if (GetCloakPhase(ship) == ShipCloakPhase.Off)
                return (false, $"{GetName(ship)} is not cloaked.");

            EndCloak(ship, "A DM breaks your cloak.");
            Log.Write(LogGroup.DM, $"{GetName(dm)} decloaked {GetName(ship)} in {GetName(GetArea(ship))}.");
            return (true, $"Decloaked {GetName(ship)}.");
        }
    }
}
