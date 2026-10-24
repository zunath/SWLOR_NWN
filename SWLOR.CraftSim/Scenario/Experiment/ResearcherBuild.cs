using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// A Researcher: the set of Researcher perks a Fabrication crafter owns. The numbers come from the game's own perk definitions
    /// (<see cref="FabricationResearcherPerkDefinition"/>), at the rank the experiment is played at, so a rank-30 researcher owns only
    /// the purchases rank 30 allows and a perk with two ranks gives the higher rank the researcher can own.
    /// </summary>
    public sealed class ResearcherBuild
    {
        private static readonly Lazy<IReadOnlyDictionary<PerkType, PerkDetail>> Definitions =
            new(() => PerkLines.Build(new FabricationResearcherPerkDefinition()));

        private static readonly PerkType[] Styles = { PerkType.Methodical, PerkType.Intuitive };

        private readonly HashSet<PerkType> _perks;

        public ResearcherBuild(string name, IEnumerable<PerkType> perks)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _perks = new HashSet<PerkType>(perks ?? throw new ArgumentNullException(nameof(perks)));
        }

        public string Name { get; }

        public IReadOnlyCollection<PerkType> Perks => _perks;

        /// <summary>Every Researcher perk, so a study can build each one alone.</summary>
        public static IReadOnlyCollection<PerkType> AllPerks => Definitions.Value.Keys.OrderBy(type => (int)type).ToList();

        public static ResearcherBuild Unperked { get; } = new("Unperked", Array.Empty<PerkType>());

        public static ResearcherBuild MethodicalAlone { get; } = Alone(PerkType.Methodical);

        public static ResearcherBuild IntuitiveAlone { get; } = Alone(PerkType.Intuitive);

        public static ResearcherBuild MethodicalLine { get; } = FullLine(PerkType.Methodical);

        public static ResearcherBuild IntuitiveLine { get; } = FullLine(PerkType.Intuitive);

        public static string NameOf(PerkType perk) => Definitions.Value[perk].Name;

        /// <summary>A researcher who owns one perk only.</summary>
        public static ResearcherBuild Alone(PerkType perk) => new(NameOf(perk), new[] { perk });

        /// <summary>A researcher with the given style and every other perk of the line, named "(full line)".</summary>
        public static ResearcherBuild FullLine(PerkType style) =>
            new($"{NameOf(style)} (full line)", Definitions.Value.Keys.Where(type => type == style || !Styles.Contains(type)));

        /// <summary>The numbers the Experiment reads for a researcher of this rank. Below Fabrication 20 there is no style yet.</summary>
        public ExperimentTuning TuningAt(int rank)
        {
            var totals = PerkLines.TotalsAt(Definitions.Value, SkillType.Fabrication, rank, _perks.Contains);
            return ExperimentTuning.FromStatTotals(PerkLines.Lookup(totals));
        }
    }
}
