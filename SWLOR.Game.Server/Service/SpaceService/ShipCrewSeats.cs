using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Which seats a hull has, which hotbar slots each seat works and which skill each seat checks (plan section 10).
    /// Slots come from the hull's slot tags (<see cref="ShipStatCalculator.BuildSlotTags"/>): Helm slots belong to the
    /// Pilot, Gunner slots to the Gunner (a Capital hull splits them between two Gunners, the first half to the
    /// first Gunner), Engineer slots to the Engineer and the Sensors slot to Sensors.
    /// </summary>
    public static class ShipCrewSeats
    {
        private static readonly ShipCrewSeat[] _soloSeats = { ShipCrewSeat.Pilot };
        private static readonly ShipCrewSeat[] _largeSeats = { ShipCrewSeat.Pilot, ShipCrewSeat.Gunner, ShipCrewSeat.Engineer };
        private static readonly ShipCrewSeat[] _capitalSeats =
        {
            ShipCrewSeat.Pilot, ShipCrewSeat.Gunner, ShipCrewSeat.SecondGunner, ShipCrewSeat.Engineer, ShipCrewSeat.Sensors,
        };

        /// <summary>Every seat of a hull size, the Pilot first.</summary>
        public static IReadOnlyList<ShipCrewSeat> GetSeats(ShipSize size) => size switch
        {
            ShipSize.Large => _largeSeats,
            ShipSize.Capital => _capitalSeats,
            _ => _soloSeats,
        };

        /// <summary>The crew stations of a hull size: every seat except the Pilot's. Empty for Small and Medium hulls.</summary>
        public static IReadOnlyList<ShipCrewSeat> GetStationSeats(ShipSize size) =>
            GetSeats(size).Where(x => x != ShipCrewSeat.Pilot).ToList();

        /// <summary>Whether a hull of this size has the seat.</summary>
        public static bool HasSeat(ShipSize size, ShipCrewSeat seat) => GetSeats(size).Contains(seat);

        /// <summary>
        /// Whether a hull carries an astromech socket: the Small and Medium combat hulls (Fighter, Interceptor, Bomber
        /// and Gunship). Industry hulls have the droid bay instead, and haulers and explorers have neither.
        /// </summary>
        public static bool HasAstromechSocket(ShipSize size, ShipRole role) =>
            (size == ShipSize.Small || size == ShipSize.Medium) &&
            (role == ShipRole.Fighter || role == ShipRole.Interceptor || role == ShipRole.Bomber || role == ShipRole.Gunship);

        /// <summary>
        /// The crew stations a player can take aboard a hull: those of its size, then the astromech socket when the
        /// hull carries one. Droids are posted from <see cref="GetStationSeats(ShipSize)"/>, so never at the socket.
        /// </summary>
        public static IReadOnlyList<ShipCrewSeat> GetStationSeats(ShipSize size, bool astromechSocket)
        {
            var seats = GetStationSeats(size).ToList();
            if (astromechSocket)
                seats.Add(ShipCrewSeat.AstromechSocket);
            return seats;
        }

        /// <summary>Whether a hull of this size, with or without an astromech socket, has the seat.</summary>
        public static bool HasSeat(ShipSize size, ShipCrewSeat seat, bool astromechSocket) =>
            seat == ShipCrewSeat.AstromechSocket ? astromechSocket : HasSeat(size, seat);

        /// <summary>
        /// Whether the astromech socket works a module type: the repairers and the capacitor transfer, never a weapon.
        /// </summary>
        public static bool IsSocketModule(ShipModuleType type) =>
            type is ShipModuleType.HullRepairer or ShipModuleType.ShieldRepairer or ShipModuleType.RepairFieldGenerator
                or ShipModuleType.HypermatterInjector;

        /// <summary>
        /// Whether a crew member at a seat works a slot: a station seat works the slots it owns; the astromech
        /// socket owns none and works the Pilot's socket modules (<see cref="IsSocketModule"/>), which the Pilot
        /// keeps working too, so a socket hull flies the same with the socket empty.
        /// </summary>
        public static bool SeatWorksSlot(ShipCrewSeat seat, ShipCrewSeat slotSeat, ShipModuleType moduleType) =>
            seat == ShipCrewSeat.AstromechSocket
                ? slotSeat == ShipCrewSeat.Pilot && IsSocketModule(moduleType)
                : seat == slotSeat;

        /// <summary>
        /// The seat that runs a cycle of a module that is switched on: the astromech socket for the Pilot's socket
        /// modules while an astromech who meets the module's rank is plugged in, otherwise the slot's own seat. An
        /// astromech below the module's rank cannot switch it on, so it does not run or take credit for its cycles
        /// either; they stay the Pilot's.
        /// </summary>
        public static ShipCrewSeat GetCycleSeat(ShipCrewSeat slotSeat, ShipModuleType moduleType, bool socketCrewed, bool socketMeetsRank) =>
            socketCrewed && socketMeetsRank && SeatWorksSlot(ShipCrewSeat.AstromechSocket, slotSeat, moduleType)
                ? ShipCrewSeat.AstromechSocket
                : slotSeat;

        /// <summary>Whether a seat routes the ship's power: the Engineer station and the astromech socket.</summary>
        public static bool RoutesPower(ShipCrewSeat seat) =>
            seat is ShipCrewSeat.Engineer or ShipCrewSeat.AstromechSocket;

        /// <summary>
        /// The seat that works a hotbar slot (1-30). A slot with no tag, or a Helm tag, is the Pilot's.
        /// </summary>
        public static ShipCrewSeat GetSeatForSlot(ShipSize size, IReadOnlyDictionary<int, ShipSlotTag> tags, int slot)
        {
            if (tags == null || !tags.TryGetValue(slot, out var tag) || !ShipStatCalculator.IsStation(tag))
                return ShipCrewSeat.Pilot;

            switch (tag)
            {
                case ShipSlotTag.Gunner:
                    if (size != ShipSize.Capital)
                        return ShipCrewSeat.Gunner;

                    var gunnerSlots = tags.Where(x => x.Value == ShipSlotTag.Gunner).Select(x => x.Key).OrderBy(x => x).ToList();
                    var firstGunnerCount = (gunnerSlots.Count + 1) / 2;
                    return gunnerSlots.IndexOf(slot) < firstGunnerCount ? ShipCrewSeat.Gunner : ShipCrewSeat.SecondGunner;
                case ShipSlotTag.Sensors:
                    return ShipCrewSeat.Sensors;
                default:
                    return ShipCrewSeat.Engineer;
            }
        }

        /// <summary>The hotbar slots (1-30) a seat works, lowest first.</summary>
        public static List<int> GetSlotsForSeat(ShipSize size, IReadOnlyDictionary<int, ShipSlotTag> tags, ShipCrewSeat seat)
        {
            if (tags == null)
                return new List<int>();

            return tags.Keys
                .Where(slot => GetSeatForSlot(size, tags, slot) == seat)
                .OrderBy(slot => slot)
                .ToList();
        }

        /// <summary>
        /// The skill a seat checks to switch a module on: Piloting for the Pilot and Gunners, Engineering for the
        /// Engineer, Espionage for Sensors (plan section 4). Crew also earn their XP in it.
        /// </summary>
        public static SkillType GetSeatSkill(ShipCrewSeat seat) => seat switch
        {
            ShipCrewSeat.Engineer or ShipCrewSeat.AstromechSocket => SkillType.Engineering,
            ShipCrewSeat.Sensors => SkillType.Espionage,
            _ => SkillType.Piloting,
        };

        /// <summary>The seat name shown to players.</summary>
        public static string GetSeatName(ShipCrewSeat seat) => seat switch
        {
            ShipCrewSeat.Gunner => "Gunner",
            ShipCrewSeat.SecondGunner => "Second Gunner",
            ShipCrewSeat.Engineer => "Engineer",
            ShipCrewSeat.Sensors => "Sensors",
            ShipCrewSeat.DroidBay => "Droid Bay",
            ShipCrewSeat.AstromechSocket => "Astromech Socket",
            _ => "Pilot",
        };

        /// <summary>A seat as a place to work, for messages: "the Gunner station", or "the droid bay".</summary>
        public static string GetPostName(ShipCrewSeat seat) => seat switch
        {
            ShipCrewSeat.DroidBay => "the droid bay",
            ShipCrewSeat.AstromechSocket => "the astromech socket",
            _ => $"the {GetSeatName(seat)} station",
        };

        /// <summary>The Czerka Armored Transport's tag: its signature makes crew stations 10% and droid crew 20% more effective.</summary>
        public const string CzerkaArmoredTransportTag = "sd_czerkaarmored";

        /// <summary>The crew effectiveness percent a hull's signature adds for player crew, or for droid crew when <paramref name="droid"/>.</summary>
        public static int GetHullCrewBonusPercent(string hullTag, bool droid) =>
            hullTag == CzerkaArmoredTransportTag ? (droid ? 20 : 10) : 0;

        /// <summary>The rank a module needs: the highest of its RequiresSkill ranks, or 0 when it has none.</summary>
        public static int GetRequiredRank(IReadOnlyDictionary<SkillType, int> requiredSkills) =>
            requiredSkills == null || requiredSkills.Count == 0 ? 0 : requiredSkills.Values.Max();

        /// <summary>
        /// Whether the seat's skill stands in for a module's required skill at a crew station: the ship skills
        /// (Piloting, Engineering, Espionage) do, so an Engineer works a Piloting-ranked module on Engineering. Any
        /// other skill a module names, such as Gathering on mining gear, is checked against the crew member's own rank.
        /// </summary>
        public static bool SeatSkillStandsIn(SkillType moduleSkill) =>
            moduleSkill is SkillType.Piloting or SkillType.Engineering or SkillType.Espionage;

        /// <summary>
        /// The first requirement a crew member at a seat does not meet, or null when they meet them all. A ship-skill
        /// requirement is checked against the seat's skill (<see cref="SeatSkillStandsIn"/>), any other against that
        /// skill itself.
        /// </summary>
        public static (SkillType Skill, int Rank)? GetUnmetRequirement(
            IReadOnlyDictionary<SkillType, int> requiredSkills,
            SkillType seatSkill,
            Func<SkillType, int> crewRank)
        {
            if (requiredSkills == null)
                return null;

            foreach (var (skill, rank) in requiredSkills)
            {
                var checkedSkill = SeatSkillStandsIn(skill) ? seatSkill : skill;
                if (crewRank(checkedSkill) < rank)
                    return (checkedSkill, rank);
            }

            return null;
        }

        /// <summary>Whether a crew member at a seat meets every rank the module needs (<see cref="GetUnmetRequirement"/>).</summary>
        public static bool MeetsRank(IReadOnlyDictionary<SkillType, int> requiredSkills, SkillType seatSkill, Func<SkillType, int> crewRank) =>
            GetUnmetRequirement(requiredSkills, seatSkill, crewRank) == null;

        /// <summary>
        /// The most any seat works at, after Crew Morale and Leadership. A ship crewed by N players must not beat N
        /// separate ships (plan section 10): a crewed Cruiser at 105% is level with five Fighters against a T5 Boss
        /// capital (90 s against 91 s). The cap binds on player crews, who start at 100%; Crew Morale and Leadership
        /// pay off most for droid crew, who start below it.
        /// </summary>
        public const double MaximumSeatEffectiveness = 1.05;

        /// <summary>An effectiveness held between 0 and <see cref="MaximumSeatEffectiveness"/>.</summary>
        public static double CapEffectiveness(double effectiveness) =>
            Math.Clamp(effectiveness, 0.0, MaximumSeatEffectiveness);

        /// <summary>
        /// A crew station slot's effectiveness: the crew multiplier, capped at <see cref="MaximumSeatEffectiveness"/>,
        /// while a crew member who meets the module's rank works the seat, otherwise 0 (the slot does nothing, its
        /// passives included). Starships (Revamp): "a player at the module's rank 100%".
        /// </summary>
        public static double SeatEffectiveness(bool crewed, bool meetsRank, double crewMultiplier) =>
            crewed && meetsRank ? CapEffectiveness(crewMultiplier) : 0.0;

        /// <summary>
        /// An amount worked at an effectiveness, rounded. A positive amount stays at least 1; an effectiveness of
        /// 1 leaves it alone.
        /// </summary>
        public static int ScaleByEffectiveness(int amount, double effectiveness)
        {
            if (Math.Abs(effectiveness - 1.0) < 1e-9 || amount == 0)
                return amount;

            var scaled = (int)Math.Round(amount * Math.Max(0.0, effectiveness), MidpointRounding.AwayFromZero);
            return amount > 0 && effectiveness > 0 ? Math.Max(1, scaled) : scaled;
        }
    }
}
