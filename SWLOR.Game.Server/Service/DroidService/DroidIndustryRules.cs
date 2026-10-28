using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service.DroidService
{
    /// <summary>
    /// Industrial droid rules (plan section 27): a droid's Industry is the sum of its industrial parts', and Industry pays
    /// off at a ship's stations, in its droid bay, and (through the hooks here) in holdings, station buildings and
    /// derelict salvage teams. The industrial protocols are droid-only AI instructions. Numbers: <see cref="DroidIndustryTuning"/>.
    /// </summary>
    public static class DroidIndustryRules
    {
        /// <summary>An industrial part's Industry: 4 x its tier.</summary>
        public static int PartIndustry(int tier) => DroidIndustryTuning.IndustryPerTier * Math.Clamp(tier, 1, 5);

        /// <summary>A droid's Industry: the sum of its parts' (combat parts add none).</summary>
        public static int SumIndustry(IEnumerable<int> partIndustry) => partIndustry?.Sum(x => Math.Max(0, x)) ?? 0;

        /// <summary>A droid's Industry as a share of the best droid's, 0 to 1.</summary>
        public static double Share(int industry) =>
            Math.Clamp(industry / (double)DroidIndustryTuning.MaximumIndustry, 0.0, 1.0);

        /// <summary>Whether the instruction is an industrial protocol.</summary>
        public static bool IsProtocol(PerkType perk) => DroidIndustryTuning.ProtocolValues.ContainsKey(perk);

        /// <summary>The industrial protocols among a droid's active instructions (perk to rank).</summary>
        public static Dictionary<PerkType, int> SelectProtocols(IReadOnlyDictionary<PerkType, int> activeInstructions) =>
            activeInstructions == null
                ? new Dictionary<PerkType, int>()
                : activeInstructions.Where(x => IsProtocol(x.Key) && x.Value > 0).ToDictionary(x => x.Key, x => x.Value);

        /// <summary>A protocol's value at the rank the droid runs it, or 0 when it does not run it.</summary>
        public static int GetProtocolValue(IReadOnlyDictionary<PerkType, int> protocols, PerkType protocol) =>
            protocols != null && protocols.TryGetValue(protocol, out var rank) &&
            DroidIndustryTuning.ProtocolValues.TryGetValue(protocol, out var values) && values.TryGetValue(rank, out var value)
                ? value
                : 0;

        /// <summary>The Industry an output reads: raised by Efficiency Protocol's percent.</summary>
        public static double OutputIndustry(int industry, int efficiencyPercent) =>
            Math.Max(0, industry) * (1 + Math.Max(0, efficiencyPercent) / 100.0);

        // Droid bays

        /// <summary>Whether a hull of this role has a droid bay: Mining Barge, Industrial and Logistics.</summary>
        public static bool HasDroidBay(ShipRole role) => DroidIndustryTuning.BayRoles.Contains(role);

        /// <summary>Whether the hull's droid bay adds to mining, gas and salvage yield.</summary>
        public static bool BayAddsYield(ShipRole role) => DroidIndustryTuning.BayYieldRoles.Contains(role);

        /// <summary>Whether the hull's droid bay adds to the ship's repairs.</summary>
        public static bool BayAddsRepairs(ShipRole role) => DroidIndustryTuning.BayRepairRoles.Contains(role);

        /// <summary>The percent a bay droid adds to every mining, gas and salvage cycle's yield.</summary>
        public static int BayYieldPercent(int industry, int efficiencyPercent) =>
            Round(DroidIndustryTuning.BayYieldPercentPerIndustry * OutputIndustry(industry, efficiencyPercent));

        /// <summary>The percent a bay droid adds to a Logistics ship's repairs, Damage Control Protocol included.</summary>
        public static int BayRepairPercent(int industry, int damageControlPercent) =>
            Round(DroidIndustryTuning.BayRepairPercentPerIndustry * Math.Max(0, industry)) + Math.Max(0, damageControlPercent);

        /// <summary>A yield after a percent bonus: rounded half up, never below the yield itself.</summary>
        public static int ApplyPercent(int amount, int percent) =>
            percent <= 0 ? amount : Math.Max(amount, (int)Math.Round(amount * (1 + percent / 100.0), MidpointRounding.AwayFromZero));

        // Hooks for later slices: each takes the working droid's Industry and returns a percent.

        /// <summary>Planetary Holdings (W14b): the output a staff droid adds.</summary>
        public static int HoldingOutputPercent(int industry, int efficiencyPercent) =>
            Round(DroidIndustryTuning.HoldingOutputPercentPerIndustry * OutputIndustry(industry, efficiencyPercent));

        /// <summary>A station's Asteroid Drill (W13): the output an assigned droid adds.</summary>
        public static int DrillOutputPercent(int industry, int efficiencyPercent) =>
            Round(DroidIndustryTuning.DrillOutputPercentPerIndustry * OutputIndustry(industry, efficiencyPercent));

        /// <summary>A station's Shipyard (W13): how much faster an assigned droid builds.</summary>
        public static int ShipyardBuildSpeedPercent(int industry) =>
            Round(DroidIndustryTuning.ShipyardBuildSpeedPercentPerIndustry * Math.Max(0, industry));

        /// <summary>A derelict salvage team (W15): how much shorter the droids' timer is.</summary>
        public static int SalvageTeamSpeedPercent(int industry) =>
            Round(DroidIndustryTuning.SalvageTeamSpeedPercentPerIndustry * Math.Max(0, industry));

        /// <summary>A derelict salvage team (W15): the yield the droids add.</summary>
        public static int SalvageTeamYieldPercent(int industry) =>
            Round(DroidIndustryTuning.SalvageTeamYieldPercentPerIndustry * Math.Max(0, industry));

        private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
