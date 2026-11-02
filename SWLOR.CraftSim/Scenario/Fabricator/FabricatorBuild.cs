using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>
    /// A Builder: the set of Builder perks a Fabricator crafter owns. The numbers come from the game's own perk definitions
    /// (<see cref="FabricationBuilderPerkDefinition"/>), at the rank the recipe is played at, so a rank-30 Builder owns only the
    /// purchases rank 30 allows and a perk with two ranks gives the higher rank the crafter can own.
    /// </summary>
    public sealed class FabricatorBuild
    {
        private static readonly Lazy<IReadOnlyDictionary<PerkType, PerkDetail>> Definitions =
            new(() => PerkLines.Build(new FabricationBuilderPerkDefinition()));

        private static readonly PerkType[] Styles = { PerkType.ProductionRun, PerkType.FineTolerance };

        private readonly HashSet<PerkType> _perks;

        public FabricatorBuild(string name, IEnumerable<PerkType> perks)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _perks = new HashSet<PerkType>(perks ?? throw new ArgumentNullException(nameof(perks)));
        }

        public string Name { get; }

        /// <summary>The perks this build owns (at the highest rank the crafter's rank allows).</summary>
        public IReadOnlyCollection<PerkType> Perks => _perks;

        /// <summary>Every Builder perk, so a study can build each one alone.</summary>
        public static IReadOnlyCollection<PerkType> AllPerks => Definitions.Value.Keys.OrderBy(type => (int)type).ToList();

        public static FabricatorBuild ProductionRunAlone { get; } = Alone(PerkType.ProductionRun);

        public static FabricatorBuild FineToleranceAlone { get; } = Alone(PerkType.FineTolerance);

        public static FabricatorBuild ProductionRunLine { get; } = FullLine(PerkType.ProductionRun);

        public static FabricatorBuild FineToleranceLine { get; } = FullLine(PerkType.FineTolerance);

        /// <summary>A Builder who owns one perk only.</summary>
        public static FabricatorBuild Alone(PerkType perk) => new(Definitions.Value[perk].Name, new[] { perk });

        /// <summary>A Builder with the given style and every other perk of the line, named "(full line)".</summary>
        public static FabricatorBuild FullLine(PerkType style) =>
            new($"{Definitions.Value[style].Name} (full line)", Definitions.Value.Keys.Where(type => type == style || !Styles.Contains(type)));

        public CraftStyle AsStyle() => new(Name, this);

        /// <summary>The numbers the Fabricator reads for a Builder of this rank. Below Fabrication 20 there is no style yet.</summary>
        public FabricatorPerkStats StatsAt(int rank)
        {
            var totals = PerkLines.TotalsAt(Definitions.Value, SkillType.Fabrication, rank, _perks.Contains);
            return FabricatorPerkStats.FromStatTotals(PerkLines.Lookup(totals));
        }
    }
}
