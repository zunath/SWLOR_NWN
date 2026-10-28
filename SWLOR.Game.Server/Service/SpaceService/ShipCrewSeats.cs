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
            ShipCrewSeat.Engineer => SkillType.Engineering,
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
            _ => "Pilot",
        };

        /// <summary>A seat as a place to work, for messages: "the Gunner station", or "the droid bay".</summary>
        public static string GetPostName(ShipCrewSeat seat) =>
            seat == ShipCrewSeat.DroidBay ? "the droid bay" : $"the {GetSeatName(seat)} station";

        /// <summary>
        /// The rank a module needs: the highest of its RequiresSkill ranks, or 0 when it has none. At a crew station
        /// this rank is checked against the seat's skill, whichever skill the module itself names.
        /// </summary>
        public static int GetRequiredRank(IReadOnlyDictionary<SkillType, int> requiredSkills) =>
            requiredSkills == null || requiredSkills.Count == 0 ? 0 : requiredSkills.Values.Max();

        /// <summary>Whether a crew member with this rank in the seat's skill can switch the module on.</summary>
        public static bool MeetsRank(IReadOnlyDictionary<SkillType, int> requiredSkills, int seatSkillRank) =>
            GetRequiredRank(requiredSkills) <= seatSkillRank;

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
