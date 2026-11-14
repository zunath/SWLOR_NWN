using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Ship-to-ship docking (plan section 10, "Ship-to-ship docking"): a Small or Medium ship docks aboard a
    /// player's Large or Capital ship in space. Every number names its source.
    /// </summary>
    public static class ShipDockingRules
    {
        /// <summary>The visitor must be this close to the host to dock: the command range (30 m), well inside weapon range (40 m).</summary>
        public const float DockRange = SpaceRadii.CommandRange;

        /// <summary>
        /// A ship that dealt or took ship damage this recently is in combat, as is one an enemy holds threat on
        /// (the rule the starport dock and <c>/exit</c> use). 10 s covers PvP, where no enemy holds threat.
        /// </summary>
        public const double CombatWindowSeconds = 10;

        /// <summary>An undocking ship comes out this far beside the host, clear of its hull.</summary>
        public const float UndockDistance = 10f;

        /// <summary>The docked ships a Large host carries.</summary>
        public const int LargeCapacity = 1;

        /// <summary>The docked ships a Capital host carries.</summary>
        public const int CapitalCapacity = 2;

        /// <summary>A hull's size, legacy hulls with no size counted Small, or Capital when flagged a capital ship.</summary>
        public static ShipSize ResolveSize(ShipSize size, bool capitalShip)
        {
            if (size != ShipSize.Invalid)
                return size;

            return capitalShip ? ShipSize.Capital : ShipSize.Small;
        }

        /// <summary>Whether a ship of this size can carry docked ships.</summary>
        public static bool CanHost(ShipSize size) => size is ShipSize.Large or ShipSize.Capital;

        /// <summary>Whether a ship of this size can dock aboard another.</summary>
        public static bool CanVisit(ShipSize size) => size is ShipSize.Small or ShipSize.Medium;

        /// <summary>How many ships a host of this size carries: Large 1, Capital 2, others none.</summary>
        public static int GetCapacity(ShipSize hostSize) => hostSize switch
        {
            ShipSize.Large => LargeCapacity,
            ShipSize.Capital => CapitalCapacity,
            _ => 0,
        };

        /// <summary>Whether a ship is in combat: an enemy holds threat on it, or it dealt or took damage within <see cref="CombatWindowSeconds"/>.</summary>
        public static bool IsInCombat(bool hasEnmity, DateTime lastCombatAt, DateTime now) =>
            hasEnmity || (now - lastCombatAt).TotalSeconds < CombatWindowSeconds;

        /// <summary>Why the pilot cannot dock with the host, or <see cref="ShipDockRefusal.None"/>.</summary>
        public static ShipDockRefusal GetDockRefusal(ShipDockRequest request)
        {
            if (request.SameShip)
                return ShipDockRefusal.SameShip;
            if (!CanHost(request.HostSize))
                return ShipDockRefusal.HostTooSmall;
            if (!CanVisit(request.VisitorSize))
                return ShipDockRefusal.VisitorTooLarge;
            if (!request.HasDockPermission)
                return ShipDockRefusal.NoPermission;
            if (request.Distance > DockRange)
                return ShipDockRefusal.OutOfRange;
            if (request.HostInCombat)
                return ShipDockRefusal.HostInCombat;
            if (request.VisitorInCombat)
                return ShipDockRefusal.VisitorInCombat;
            if (request.HostDockedCount >= GetCapacity(request.HostSize))
                return ShipDockRefusal.HostFull;

            return ShipDockRefusal.None;
        }

        /// <summary>The message a pilot sees for a refusal.</summary>
        public static string DescribeRefusal(ShipDockRefusal refusal, ShipSize hostSize) => refusal switch
        {
            ShipDockRefusal.SameShip => "You cannot dock with your own ship.",
            ShipDockRefusal.HostTooSmall => "Only Large and Capital ships can take a docked ship.",
            ShipDockRefusal.VisitorTooLarge => "Only Small and Medium ships can dock with another ship.",
            ShipDockRefusal.NoPermission => "You do not have permission to dock with this ship.",
            ShipDockRefusal.OutOfRange => $"You must be within {DockRange:0} meters of the ship to dock.",
            ShipDockRefusal.HostInCombat => "You cannot dock with a ship that is in combat.",
            ShipDockRefusal.VisitorInCombat => "You cannot dock while in combat.",
            ShipDockRefusal.HostFull => GetCapacity(hostSize) == 1
                ? "This ship already has a ship docked aboard."
                : $"This ship already has {GetCapacity(hostSize)} ships docked aboard.",
            _ => string.Empty,
        };

        /// <summary>
        /// Where an undocking ship goes: beside the host when it is in space, otherwise into the starport where the
        /// host is docked when that dock takes the visitor (<see cref="ShipAccessRules.GetStarportRefusal"/>, judged
        /// by the caller once the host has a dock; pass <see cref="StarportDockRefusal.None"/> otherwise).
        /// </summary>
        public static ShipUndockPlacement ChooseUndockPlacement(bool hostInSpace, bool hostHasDock, StarportDockRefusal dockRefusal)
        {
            if (hostInSpace)
                return ShipUndockPlacement.Space;
            if (!hostHasDock)
                return ShipUndockPlacement.NoDock;

            return dockRefusal == StarportDockRefusal.None ? ShipUndockPlacement.Starport : ShipUndockPlacement.StarportTooSmall;
        }

        /// <summary>
        /// The offset from the host's position of the docked ship at <paramref name="index"/> when it comes out:
        /// <see cref="UndockDistance"/> to the host's right for the first, to its left for the second, and further
        /// out on alternate sides after that. NWN facing is in degrees, 0 = east, counterclockwise.
        /// </summary>
        public static (float X, float Y) GetUndockOffset(float hostFacing, int index)
        {
            var side = index % 2 == 0 ? -90.0 : 90.0;
            var distance = UndockDistance * (1 + Math.Max(0, index) / 2);
            var radians = (hostFacing + side) * Math.PI / 180.0;
            return ((float)(Math.Cos(radians) * distance), (float)(Math.Sin(radians) * distance));
        }

        /// <summary>Whether a player belongs to a docked ship: its owner, or someone who came aboard with it.</summary>
        public static bool BelongsToVisitor(string playerId, ShipDockVisitor visitor) =>
            !string.IsNullOrWhiteSpace(playerId) &&
            (playerId == visitor.OwnerPlayerId || visitor.CrewPlayerIds.Contains(playerId));

        /// <summary>
        /// When a host is destroyed, which docked ship each player aboard returns to: the first ship they came
        /// aboard with, otherwise the first they own. Players who belong to no docked ship are left out and share
        /// the host's fate.
        /// </summary>
        public static Dictionary<string, string> AssignReturningPlayers(IEnumerable<string> playersAboard, IReadOnlyList<ShipDockVisitor> visitors)
        {
            var result = new Dictionary<string, string>();
            foreach (var playerId in playersAboard.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                var visitor = visitors.FirstOrDefault(x => x.CrewPlayerIds.Contains(playerId)) ??
                              visitors.FirstOrDefault(x => x.OwnerPlayerId == playerId);
                if (visitor != null)
                    result[playerId] = visitor.ShipId;
            }

            return result;
        }

        /// <summary>
        /// Who flies a detached ship out: the pilot who docked it, then the rest of its crew in boarding order, then
        /// its owner, whoever is first able to. Null leaves it adrift beside the wreck for its crew to fly from the interior.
        /// </summary>
        public static string ChooseDetachPilot(ShipDockVisitor visitor, Func<string, bool> canPilot)
        {
            foreach (var playerId in visitor.CrewPlayerIds.Append(visitor.OwnerPlayerId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                if (canPilot(playerId))
                    return playerId;
            }

            return null;
        }
    }
}
