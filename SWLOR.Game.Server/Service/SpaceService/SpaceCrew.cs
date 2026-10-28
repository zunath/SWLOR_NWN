using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Crew rules the styles and the module code call (plan section 10). A ship is its pilot's creature. Crew
    /// members hold seats on it while it flies (<see cref="Space.BoardCrewStation"/> boards and <see cref="Space.LeaveCrewStation"/> unseats them); a station
    /// slot works only while its seat is crewed, at the crew's effectiveness. While a crew member fires a station
    /// module, a fire context names them, so the shot or repair reads their attributes and stats while the ship
    /// stays the source of range, threat and damage. A seat holds a player or a posted droid, never both.
    /// </summary>
    public static class SpaceCrew
    {
        private static readonly Dictionary<uint, Dictionary<ShipCrewSeat, uint>> _seatsByShip = new();
        private static readonly Dictionary<uint, (uint Ship, ShipCrewSeat Seat)> _seatByCrew = new();
        private static readonly Dictionary<uint, SpaceCrewFireContext> _fireContexts = new();
        private static readonly Dictionary<uint, Dictionary<ShipCrewSeat, ShipDroidCrew>> _droidsByShip = new();
        private static readonly Dictionary<uint, Dictionary<int, uint>> _droidEngagements = new();
        private static readonly List<SpaceCrewFireContext> _droidFire = new();

        // Effectiveness

        /// <summary>
        /// The effectiveness of the player crew working a ship's stations, as a multiplier (1.0 is a player at the
        /// module's rank). Command's Crew Morale and the captain's Leadership rank
        /// (<see cref="SpaceCommand.GetLeadershipCrewBonusPercent"/>) raise it, up to
        /// <see cref="ShipCrewSeats.MaximumSeatEffectiveness"/>.
        /// </summary>
        public static double GetCrewEffectiveness(uint ship) =>
            ShipCrewSeats.CapEffectiveness(GetCrewMultiplier(ship));

        /// <summary>The ship's crew multiplier from Crew Morale and the captain's Leadership, before the cap.</summary>
        public static double GetCrewMultiplier(uint ship) =>
            EffectivenessMultiplier(
                Stat.GetStatAdjustment(ship, StatType.SpaceCrewEffectivenessPercentAdjustment),
                SpaceCommand.GetLeadershipCrewBonusPercent(ship));

        /// <summary>
        /// A posted droid's effectiveness at its seat: its Industry's base (<see cref="DroidCrewRules.BaseEffectiveness"/>)
        /// times the crew multiplier, capped like every seat.
        /// </summary>
        public static double GetDroidEffectiveness(uint ship, ShipDroidCrew droid) =>
            droid == null
                ? 0.0
                : DroidCrewRules.SeatEffectiveness(droid.Seat, droid.Industry, GetCrewMultiplier(ship));

        /// <summary>
        /// What a droid's Industry rating (plan section 27) adds to its base effectiveness at its seat: up to +50% at
        /// the Engineer and Sensors seats and +25% at Gunner seats for the best industrial droid, none for a combat droid.
        /// </summary>
        public static double GetDroidIndustryBonus(ShipDroidCrew droid) =>
            droid == null ? 0.0 : DroidCrewRules.IndustryBonus(droid.Seat, droid.Industry);

        /// <summary>
        /// The percent a droid's Survey Protocol instruction (plan section 27) adds to a Probe Launcher it runs, or 0
        /// for a droid without it and for no droid at all.
        /// </summary>
        public static int GetDroidSurveyBonusPercent(ShipDroidCrew droid) =>
            DroidIndustryRules.GetProtocolValue(droid?.Protocols, PerkType.SurveyProtocol);

        /// <summary>
        /// The percent a droid's Damage Control Protocol adds to the repairs it runs at the Engineer station, or 0
        /// anywhere else.
        /// </summary>
        public static int GetDroidRepairBonusPercent(ShipDroidCrew droid) =>
            droid?.Seat == ShipCrewSeat.Engineer
                ? DroidIndustryRules.GetProtocolValue(droid.Protocols, PerkType.DamageControlProtocol)
                : 0;

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
            return seat != ShipCrewSeat.Pilot && IsSeatWorked(ship, seat);
        }

        /// <summary>Whether a player or a droid works the seat.</summary>
        public static bool IsSeatWorked(uint ship, ShipCrewSeat seat) =>
            GetSeatOccupant(ship, seat) != OBJECT_INVALID || GetSeatDroid(ship, seat) != null;

        /// <summary>
        /// Seats a crew member. False when the seat is taken (by a player or a droid) or the crew member already
        /// holds a seat.
        /// </summary>
        public static bool AssignSeat(uint ship, ShipCrewSeat seat, uint crewMember)
        {
            if (seat == ShipCrewSeat.Invalid || seat == ShipCrewSeat.Pilot || seat == ShipCrewSeat.DroidBay ||
                crewMember == ship || crewMember == OBJECT_INVALID || _seatByCrew.ContainsKey(crewMember) || IsSeatWorked(ship, seat))
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

        // Droid crew

        /// <summary>
        /// Seats a posted droid while its ship flies. False when the seat is the Pilot's or is taken by a player or
        /// another droid.
        /// </summary>
        public static bool AssignDroidSeat(uint ship, ShipDroidCrew droid)
        {
            if (droid == null || droid.Seat == ShipCrewSeat.Invalid || droid.Seat == ShipCrewSeat.Pilot ||
                IsSeatWorked(ship, droid.Seat))
            {
                return false;
            }

            if (!_droidsByShip.TryGetValue(ship, out var seats))
            {
                seats = new Dictionary<ShipCrewSeat, ShipDroidCrew>();
                _droidsByShip[ship] = seats;
            }

            seats[droid.Seat] = droid;
            return true;
        }

        /// <summary>Takes the droid off a seat. False when no droid held it.</summary>
        public static bool ReleaseDroidSeat(uint ship, ShipCrewSeat seat)
        {
            if (!_droidsByShip.TryGetValue(ship, out var seats) || !seats.Remove(seat))
                return false;

            if (seats.Count == 0)
                _droidsByShip.Remove(ship);

            _droidEngagements.Remove(ship);
            return true;
        }

        /// <summary>The droid at a seat, or null.</summary>
        public static ShipDroidCrew GetSeatDroid(uint ship, ShipCrewSeat seat) =>
            _droidsByShip.TryGetValue(ship, out var seats) && seats.TryGetValue(seat, out var droid) ? droid : null;

        /// <summary>The droids working a ship's stations.</summary>
        public static List<ShipDroidCrew> GetDroidCrew(uint ship) =>
            _droidsByShip.TryGetValue(ship, out var seats) ? seats.Values.ToList() : new List<ShipDroidCrew>();

        /// <summary>The ships with droids at their stations.</summary>
        public static List<uint> GetShipsWithDroidCrew() => _droidsByShip.Keys.ToList();

        /// <summary>Takes every droid off a ship's seats (the ship docked, lost its pilot or was destroyed).</summary>
        public static void ClearDroidCrew(uint ship)
        {
            _droidsByShip.Remove(ship);
            _droidEngagements.Remove(ship);
        }

        /// <summary>The target a droid last switched a slot's weapon on against, or OBJECT_INVALID.</summary>
        public static uint GetDroidEngagement(uint ship, int slot) =>
            _droidEngagements.TryGetValue(ship, out var engagements) && engagements.TryGetValue(slot, out var target)
                ? target
                : OBJECT_INVALID;

        /// <summary>Remembers that a droid switched a slot's weapon on against a target.</summary>
        public static void RecordDroidEngagement(uint ship, int slot, uint target)
        {
            if (!_droidEngagements.TryGetValue(ship, out var engagements))
            {
                engagements = new Dictionary<int, uint>();
                _droidEngagements[ship] = engagements;
            }

            engagements[slot] = target;
        }

        /// <summary>
        /// Forgets what the ship's droids engaged, so their weapons switch on again at the target the pilot picks
        /// next (the same one included).
        /// </summary>
        public static void ForgetDroidEngagements(uint ship)
        {
            _droidEngagements.Remove(ship);
        }

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

            var droidFire = context.Droid != null;
            if (droidFire)
                _droidFire.Add(context);

            return new SpaceCrewFireScope(context.Ship, previous, droidFire);
        }

        internal static void RestoreFireContext(uint ship, SpaceCrewFireContext previous, bool droidFire = false)
        {
            if (droidFire && _droidFire.Count > 0)
                _droidFire.RemoveAt(_droidFire.Count - 1);

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
        /// otherwise the ship. A droid has no creature, so while one fires this is OBJECT_INVALID: no perks or
        /// statuses apply, and <see cref="TryGetDroidAttribute"/> gives its attributes.
        /// </summary>
        public static uint GetStatSource(uint ship)
        {
            var context = GetFireContext(ship);
            if (context == null)
                return ship;

            return context.Droid != null ? OBJECT_INVALID : context.CrewMember;
        }

        /// <summary>
        /// The creature that uses an ability the ship activates: the crew member using their own Piloting ability from
        /// a station (plan section 10), otherwise <paramref name="creature"/> itself. The ship stays the activator and
        /// source (validation, range, self statuses, the orders' reach); the user's perks, STM, recasts, Social and
        /// Leadership stand behind it.
        /// </summary>
        public static uint GetAbilityUser(uint creature)
        {
            var context = GetFireContext(creature);
            return context is { IsAbility: true } && context.CrewMember != OBJECT_INVALID
                ? context.CrewMember
                : creature;
        }

        /// <summary>Whether a stat source is the droid firing right now.</summary>
        public static bool IsDroidStatSource(uint statSource) => statSource == OBJECT_INVALID && _droidFire.Count > 0;

        /// <summary>
        /// A firing droid's attribute score (<see cref="DroidCrewRules.Attribute"/>, the same for every attribute)
        /// when the stat source is the droid at work; false for every other source.
        /// </summary>
        public static bool TryGetDroidAttribute(uint statSource, AbilityType ability, out int score)
        {
            if (!IsDroidStatSource(statSource))
            {
                score = 0;
                return false;
            }

            score = DroidCrewRules.Attribute(_droidFire[^1].Droid.Tier);
            return true;
        }

        /// <summary>The effectiveness of the station at work, or 1.0 when the pilot fires.</summary>
        public static double GetFireEffectiveness(uint ship) => GetFireContext(ship)?.Effectiveness ?? 1.0;

        /// <summary>The seat whose slots the ship may fire right now: the station at work, otherwise the Pilot's.</summary>
        public static ShipCrewSeat GetActingSeat(uint ship) => GetFireContext(ship)?.Seat ?? ShipCrewSeat.Pilot;

        /// <summary>
        /// The player who earns the combat point for what the ship just did: the crew member at work, or the pilot
        /// for the pilot's own modules and a droid's.
        /// </summary>
        public static uint GetCreditedPlayer(uint ship) =>
            GetFireContext(ship)?.Droid != null ? ship : GetStatSource(ship);

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
