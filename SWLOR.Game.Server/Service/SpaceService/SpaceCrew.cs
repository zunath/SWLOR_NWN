using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Crew rules the styles and the module code call (plan section 10). A ship is its pilot's creature. Crew
    /// members hold seats on it while it flies (<see cref="Space.BoardCrewStation"/> boards and <see cref="Space.LeaveCrewStation"/> unseats them); a station
    /// slot works only while its seat is crewed, at the crew's effectiveness. While a crew member fires a station
    /// module, a fire context names them, so the shot or repair reads their attributes and stats while the ship
    /// stays the source of range, threat and damage.
    /// </summary>
    public static class SpaceCrew
    {
        private static readonly Dictionary<uint, Dictionary<ShipCrewSeat, uint>> _seatsByShip = new();
        private static readonly Dictionary<uint, (uint Ship, ShipCrewSeat Seat)> _seatByCrew = new();
        private static readonly Dictionary<uint, SpaceCrewFireContext> _fireContexts = new();

        // Effectiveness

        /// <summary>
        /// The effectiveness of the player crew working a ship's stations, as a multiplier (1.0 is a player at the
        /// module's rank). Command's Crew Morale and the captain's Leadership rank
        /// (<see cref="SpaceCommand.GetLeadershipCrewBonusPercent"/>) raise it, up to
        /// <see cref="ShipCrewSeats.MaximumSeatEffectiveness"/>.
        /// </summary>
        public static double GetCrewEffectiveness(uint ship) =>
            ShipCrewSeats.CapEffectiveness(EffectivenessMultiplier(
                Stat.GetStatAdjustment(ship, StatType.SpaceCrewEffectivenessPercentAdjustment),
                SpaceCommand.GetLeadershipCrewBonusPercent(ship)));

        /// <summary>The crew effectiveness multiplier for a Crew Morale percent and a Leadership percent.</summary>
        public static double EffectivenessMultiplier(int crewMoralePercent, int leadershipPercent) =>
            1.0 + (Math.Max(0, crewMoralePercent) + Math.Max(0, leadershipPercent)) / 100.0;

        // Seats

        /// <summary>Whether someone is working the crew-station seat that owns a hotbar slot (1-30). Helm slots are never crewed.</summary>
        public static bool IsSeatCrewed(uint ship, ShipStatus status, int slotNumber)
        {
            if (status == null)
                return false;

            var seat = ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, slotNumber);
            return seat != ShipCrewSeat.Pilot && GetSeatOccupant(ship, seat) != OBJECT_INVALID;
        }

        /// <summary>Seats a crew member. False when the seat is taken or the crew member already holds a seat.</summary>
        public static bool AssignSeat(uint ship, ShipCrewSeat seat, uint crewMember)
        {
            if (seat == ShipCrewSeat.Invalid || seat == ShipCrewSeat.Pilot || crewMember == ship ||
                _seatByCrew.ContainsKey(crewMember) || GetSeatOccupant(ship, seat) != OBJECT_INVALID)
            {
                return false;
            }

            if (!_seatsByShip.TryGetValue(ship, out var seats))
            {
                seats = new Dictionary<ShipCrewSeat, uint>();
                _seatsByShip[ship] = seats;
            }

            seats[seat] = crewMember;
            _seatByCrew[crewMember] = (ship, seat);
            return true;
        }

        /// <summary>Frees a crew member's seat. False when they held none.</summary>
        public static bool ReleaseSeat(uint crewMember)
        {
            if (!_seatByCrew.Remove(crewMember, out var held))
                return false;

            if (_seatsByShip.TryGetValue(held.Ship, out var seats))
            {
                seats.Remove(held.Seat);
                if (seats.Count == 0)
                    _seatsByShip.Remove(held.Ship);
            }

            return true;
        }

        /// <summary>The crew member in a seat, or OBJECT_INVALID.</summary>
        public static uint GetSeatOccupant(uint ship, ShipCrewSeat seat) =>
            _seatsByShip.TryGetValue(ship, out var seats) && seats.TryGetValue(seat, out var crewMember)
                ? crewMember
                : OBJECT_INVALID;

        /// <summary>The ship and seat a crew member holds.</summary>
        public static bool TryGetSeat(uint crewMember, out uint ship, out ShipCrewSeat seat)
        {
            if (_seatByCrew.TryGetValue(crewMember, out var held))
            {
                ship = held.Ship;
                seat = held.Seat;
                return true;
            }

            ship = OBJECT_INVALID;
            seat = ShipCrewSeat.Invalid;
            return false;
        }

        /// <summary>The crew members seated aboard a ship, by seat.</summary>
        public static IReadOnlyDictionary<ShipCrewSeat, uint> GetSeatedCrew(uint ship) =>
            _seatsByShip.TryGetValue(ship, out var seats)
                ? new Dictionary<ShipCrewSeat, uint>(seats)
                : new Dictionary<ShipCrewSeat, uint>();

        /// <summary>The crew members seated aboard a ship.</summary>
        public static List<uint> GetCrew(uint ship) =>
            _seatsByShip.TryGetValue(ship, out var seats) ? seats.Values.ToList() : new List<uint>();

        // Station fire

        /// <summary>
        /// Starts a crew member's station fire on a ship until the returned scope is disposed (use it in a
        /// <c>using</c>). A null context returns a scope that does nothing, so the pilot's own fire passes null.
        /// </summary>
        public static SpaceCrewFireScope BeginStationFire(SpaceCrewFireContext context)
        {
            if (context == null)
                return new SpaceCrewFireScope(OBJECT_INVALID, null);

            _fireContexts.TryGetValue(context.Ship, out var previous);
            _fireContexts[context.Ship] = context;
            return new SpaceCrewFireScope(context.Ship, previous);
        }

        internal static void RestoreFireContext(uint ship, SpaceCrewFireContext previous)
        {
            if (ship == OBJECT_INVALID)
                return;

            if (previous == null)
                _fireContexts.Remove(ship);
            else
                _fireContexts[ship] = previous;
        }

        /// <summary>The station fire running on a ship, or null when the ship fires for its pilot.</summary>
        public static SpaceCrewFireContext GetFireContext(uint ship) =>
            _fireContexts.TryGetValue(ship, out var context) ? context : null;

        /// <summary>
        /// The creature whose attributes and stats a shot or repair from the ship reads: the crew member at work,
        /// otherwise the ship.
        /// </summary>
        public static uint GetStatSource(uint ship) => GetFireContext(ship)?.CrewMember ?? ship;

        /// <summary>The effectiveness of the station at work, or 1.0 when the pilot fires.</summary>
        public static double GetFireEffectiveness(uint ship) => GetFireContext(ship)?.Effectiveness ?? 1.0;

        /// <summary>The seat whose slots the ship may fire right now: the station at work, otherwise the Pilot's.</summary>
        public static ShipCrewSeat GetActingSeat(uint ship) => GetFireContext(ship)?.Seat ?? ShipCrewSeat.Pilot;

        /// <summary>The player who earns the combat point for what the ship just did.</summary>
        public static uint GetCreditedPlayer(uint ship) => GetStatSource(ship);

        /// <summary>The skill the combat point goes to: the seat's skill at a station, otherwise <paramref name="pilotSkill"/>.</summary>
        public static SkillType GetCreditedSkill(uint ship, SkillType pilotSkill) => GetFireContext(ship)?.Skill ?? pilotSkill;

        /// <summary>
        /// Whether the ship can fire a slot for whoever is acting: the Pilot fires Helm slots only (and every slot of
        /// an older ship with no tags), a crew member fires their own seat's slots only.
        /// </summary>
        public static bool CanActingSeatFire(uint ship, ShipStatus status, int slotNumber)
        {
            if (status == null)
                return false;

            var slotSeat = ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, slotNumber);
            return slotSeat == GetActingSeat(ship);
        }
    }
}
