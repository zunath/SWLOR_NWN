using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Feature.LootTableDefinition;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>Where a recipe's component comes from, as far as the model can supply it.</summary>
    public enum ComponentSource
    {
        /// <summary>A refined ore: gather the raw ore, then refine it.</summary>
        Refined,

        /// <summary>An item a gathering loot table drops (ore, wood, gems, fiber, herbs, vegetables, supply crates).</summary>
        Harvested,

        /// <summary>An item another active recipe makes.</summary>
        Crafted,

        /// <summary>Everything else: enemy drops no gathering table holds, vendor goods and quest items.</summary>
        Other
    }

    /// <summary>How a gathering loot table is worked.</summary>
    public enum GatheringMechanism
    {
        /// <summary>A harvester used on an ore vein or tree: each use rolls the table once (<c>HarvesterItemDefinition</c>).</summary>
        Harvest,

        /// <summary>A scavenge site (bushes, herbs, vegetable patches, supply crates): one visit makes one or two search rolls, each success one item (<c>ScavengePoint</c>).</summary>
        Scavenge,

        /// <summary>Asteroid mining from a ship, priced like a harvester node because space mining is not modelled.</summary>
        Asteroid
    }

    /// <summary>What gathering the components of one craft costs, priced together.</summary>
    /// <param name="Seconds">Seconds of gathering and refining for one craft attempt that commits every stack.</param>
    /// <param name="ModelledUnits">Component units a table supplies.</param>
    /// <param name="UnmodelledUnits">Component units no table supplies.</param>
    /// <param name="SecondsByFamily">Seconds by the kind of node.</param>
    /// <param name="SecondsByComponent">Seconds by component; a table's visits are shared out by each item's own need.</param>
    /// <param name="UnitsBySource">Component units by where they come from.</param>
    public sealed record RecipeGathering(
        double Seconds,
        double ModelledUnits,
        double UnmodelledUnits,
        IReadOnlyDictionary<string, double> SecondsByFamily,
        IReadOnlyDictionary<string, double> SecondsByComponent,
        IReadOnlyDictionary<ComponentSource, double> UnitsBySource);

    /// <summary>One loot table that can drop an item.</summary>
    /// <param name="Table">The loot table's name.</param>
    /// <param name="Share">The chance one roll of the table gives the item.</param>
    /// <param name="MaxQuantity">The most one successful roll gives; a roll gives 1 to this many.</param>
    /// <param name="SiteLevel">A scavenge site's level, from its table name; 0 for the other mechanisms.</param>
    public sealed record GatheringSource(string Table, GatheringMechanism Mechanism, double Share, int MaxQuantity, int SiteLevel);

    /// <summary>
    /// Prices a recipe component in seconds of gathering at the cheapest loot table that drops it. A harvester use takes
    /// <see cref="ProgressionAssumptions.SecondsPerHarvest"/>, a node holds 2.5 harvests on average and costs
    /// <see cref="ProgressionAssumptions.NodeTravelSeconds"/> to reach, and each use rolls the table once (plus the Might bonus roll). A
    /// scavenge site costs the walk plus two seconds per search; it is searched once, and each successful search gives one roll of the table
    /// (<see cref="ScavengeModel.ExpectedSuccessesPerSite"/>) of 1 to the item's quantity. A refined ore adds a refining operation per batch.
    /// </summary>
    public sealed class ComponentSupply
    {
        private readonly Dictionary<string, List<GatheringSource>> _sources = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, OreTier> _oreByRefined = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _craftedItems = new(StringComparer.OrdinalIgnoreCase);

        private ComponentSupply()
        {
        }

        public static ComponentSupply Build(RecipeCatalog recipes)
        {
            var supply = new ComponentSupply();
            foreach (var (name, table) in new HarvestingLootTableDefinition().BuildLootTables())
            {
                var total = table.Sum(item => item.Weight);
                if (total <= 0)
                    continue;

                var mechanism = MechanismOf(name);
                var level = mechanism == GatheringMechanism.Scavenge ? SiteLevelOf(name) : 0;
                foreach (var item in table)
                {
                    if (!supply._sources.TryGetValue(item.Resref, out var list))
                        supply._sources[item.Resref] = list = new List<GatheringSource>();

                    list.Add(new GatheringSource(name, mechanism, item.Weight / (double)total, Math.Max(1, item.MaxQuantity), level));
                }
            }

            foreach (var ore in RefiningModel.Ores())
                supply._oreByRefined[ore.RefinedItem] = ore;

            foreach (var recipe in recipes.Recipes)
            {
                if (!string.IsNullOrEmpty(recipe.Resref))
                    supply._craftedItems.Add(recipe.Resref);
            }

            return supply;
        }

        /// <summary>
        /// How a table in <c>HarvestingLootTableDefinition</c> is worked, from the placeable blueprints that name it: ore veins and trees are
        /// harvested (<c>HARVESTING_LOOT_TABLE</c>); bushes, herbs, vegetable patches and supply crates are scavenge sites
        /// (<c>SCAVENGE_POINT_LOOT_TABLE_NAME</c>); asteroid tables are mined from a ship.
        /// </summary>
        public static GatheringMechanism MechanismOf(string table)
        {
            if (table.StartsWith("ASTEROID_", StringComparison.Ordinal))
                return GatheringMechanism.Asteroid;

            return table.StartsWith("HARVESTING_", StringComparison.Ordinal) ? GatheringMechanism.Harvest : GatheringMechanism.Scavenge;
        }

        /// <summary>A scavenge site's level: the number its table name ends with (<c>SUPPLY_CRATE_3</c> is level 3).</summary>
        public static int SiteLevelOf(string table) =>
            int.TryParse(table[(table.LastIndexOf('_') + 1)..], out var level) ? level : 1;

        public ComponentSource Classify(string resref)
        {
            if (_oreByRefined.ContainsKey(resref) && _sources.ContainsKey(_oreByRefined[resref].Ore))
                return ComponentSource.Refined;

            if (_sources.ContainsKey(resref))
                return ComponentSource.Harvested;

            return _craftedItems.Contains(resref) ? ComponentSource.Crafted : ComponentSource.Other;
        }

        /// <summary>The loot tables that can drop an item; empty when none does.</summary>
        public IReadOnlyList<GatheringSource> SourcesOf(string resref) =>
            _sources.TryGetValue(resref, out var list) ? list : Array.Empty<GatheringSource>();

        /// <summary>The harvest table with the best chance of dropping an item, and that chance per roll; null when no node holds it.</summary>
        public (string Table, double Share)? NodeFor(string resref)
        {
            var best = SourcesOf(resref).OrderByDescending(s => s.Share).FirstOrDefault();
            return best == null ? null : (best.Table, best.Share);
        }

        /// <summary>The ore a refined item is made from; null for anything else.</summary>
        public OreTier OreFor(string refinedResref) =>
            _oreByRefined.TryGetValue(refinedResref, out var ore) ? ore : null;

        /// <summary>Seconds for one harvester use, counting its share of the walk to the node.</summary>
        public static double SecondsPerHarvestUse(ProgressionAssumptions assumptions) =>
            assumptions.SecondsPerHarvest + assumptions.NodeTravelSeconds / HarvestModel.ExpectedHarvestsPerNode;

        /// <summary>
        /// The Hard Look level a scavenger has at a site: the server derives it from the player's Gathering rank (<c>CraftRankMilestones.GetHardLookLevel</c>),
        /// which is at least the site's level (the tier that can search it). <see cref="ProgressionAssumptions.GatheringTier"/> is the gatherer's tier.
        /// </summary>
        public static int HardLookAt(int siteLevel, ProgressionAssumptions assumptions) =>
            Math.Min(ScavengeModel.MaximumHardLook, Math.Max(siteLevel, assumptions.GatheringTier ?? 0));

        /// <summary>Seconds for one scavenge site: the walk plus the search animation of each attempt.</summary>
        public static double SecondsPerScavengeVisit(int siteLevel, ProgressionAssumptions assumptions) =>
            assumptions.NodeTravelSeconds +
            ScavengeModel.ExpectedAttemptsPerSite(HardLookAt(siteLevel, assumptions), assumptions.PerceptionModifier) * assumptions.SecondsPerScavengeAttempt;

        /// <summary>Units of the item one visit of a scavenge site, or one use of a harvest node, gives on average.</summary>
        public static double UnitsPerVisit(GatheringSource source, ProgressionAssumptions assumptions)
        {
            if (source.Mechanism == GatheringMechanism.Scavenge)
            {
                var successes = ScavengeModel.ExpectedSuccessesPerSite(HardLookAt(source.SiteLevel, assumptions), assumptions.PerceptionModifier);
                return successes * source.Share * (source.MaxQuantity + 1) / 2.0;
            }

            return source.Share * HarvestModel.ExpectedItemsPerHarvest(assumptions.MightModifier);
        }

        /// <summary>Seconds one visit of a scavenge site, or one use of a harvest node, takes.</summary>
        public static double SecondsPerVisit(GatheringSource source, ProgressionAssumptions assumptions) =>
            source.Mechanism == GatheringMechanism.Scavenge
                ? SecondsPerScavengeVisit(source.SiteLevel, assumptions)
                : SecondsPerHarvestUse(assumptions);

        /// <summary>Seconds to gather one unit from a source on its own.</summary>
        public static double SecondsPerUnit(GatheringSource source, ProgressionAssumptions assumptions) =>
            SecondsPerVisit(source, assumptions) / UnitsPerVisit(source, assumptions);

        /// <summary>The cheapest source of a gatherable item under the assumptions; null when no table drops it.</summary>
        public GatheringSource CheapestSource(string resref, ProgressionAssumptions assumptions) =>
            SourcesOf(resref)
                .OrderBy(source => SecondsPerUnit(source, assumptions))
                .ThenBy(source => source.Table, StringComparer.Ordinal)
                .FirstOrDefault();

        /// <summary>The kind of node a component is gathered from, for the report: ore veins, trees, bushes, herbs, vegetable patches, supply crates or asteroids.</summary>
        public string Family(string resref, ProgressionAssumptions assumptions)
        {
            var source = Classify(resref);
            if (source is ComponentSource.Crafted or ComponentSource.Other)
                return NoNodeFamily;

            var gathered = source == ComponentSource.Refined ? _oreByRefined[resref].Ore : resref;
            var table = CheapestSource(gathered, assumptions).Table;
            if (table.StartsWith("HARVESTING_", StringComparison.Ordinal))
                return table.EndsWith("_TREE", StringComparison.Ordinal) ? "trees" : "ore veins";
            if (table.StartsWith("SCAVENGING_BUSH", StringComparison.Ordinal))
                return "bushes";
            if (table.StartsWith("HERBS", StringComparison.Ordinal))
                return "herbs";
            if (table.StartsWith("VEGETABLES", StringComparison.Ordinal))
                return "vegetable patches";
            if (table.StartsWith("SUPPLY_CRATE", StringComparison.Ordinal))
                return "supply crates";
            if (table.StartsWith("ASTEROID", StringComparison.Ordinal))
                return "asteroids";

            return table;
        }

        public const string NoNodeFamily = "no node supplies it";

        /// <summary>Seconds to gather one unit of a component, or <see cref="ProgressionAssumptions.UnmodelledUnitSeconds"/> when no table supplies it.</summary>
        public double SecondsPerUnit(string resref, ProgressionAssumptions assumptions)
        {
            switch (Classify(resref))
            {
                case ComponentSource.Refined:
                    return SecondsPerUnit(CheapestSource(_oreByRefined[resref].Ore, assumptions), assumptions) +
                           assumptions.RefineSecondsPerBatch / assumptions.RefineBatchUnits;
                case ComponentSource.Harvested:
                    return SecondsPerUnit(CheapestSource(resref, assumptions), assumptions);
                default:
                    return assumptions.UnmodelledUnitSeconds;
            }
        }

        /// <summary>
        /// Prices a recipe's components together. A craft needs all of them at once, and one visit to a table gives every item it holds, so the
        /// items drawn from one table cost the visits the scarcest of them needs (the rest come free as by-products), not the sum of each priced
        /// alone. Refined ores add their refining time per unit; components no table supplies cost the flat estimate.
        /// </summary>
        public RecipeGathering PriceRecipe(RecipeEntry recipe, ProgressionAssumptions assumptions)
        {
            var secondsByComponent = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var secondsByFamily = new Dictionary<string, double>();
            var unitsBySource = Enum.GetValues<ComponentSource>().ToDictionary(source => source, _ => 0.0);
            double modelled = 0, unmodelled = 0;
            var byTable = new Dictionary<string, List<(string Component, double Units, GatheringSource Source)>>();

            void Add(string component, double seconds)
            {
                secondsByComponent[component] = secondsByComponent.GetValueOrDefault(component) + seconds;
                var family = Family(component, assumptions);
                secondsByFamily[family] = secondsByFamily.GetValueOrDefault(family) + seconds;
            }

            foreach (var (resref, quantity) in recipe.Components)
            {
                var kind = Classify(resref);
                unitsBySource[kind] += quantity;
                if (kind is ComponentSource.Crafted or ComponentSource.Other)
                {
                    unmodelled += quantity;
                    Add(resref, quantity * assumptions.UnmodelledUnitSeconds);
                    continue;
                }

                modelled += quantity;
                var gathered = kind == ComponentSource.Refined ? _oreByRefined[resref].Ore : resref;
                if (kind == ComponentSource.Refined)
                    Add(resref, quantity * assumptions.RefineSecondsPerBatch / assumptions.RefineBatchUnits);

                var source = CheapestSource(gathered, assumptions);
                if (!byTable.TryGetValue(source.Table, out var lines))
                    byTable[source.Table] = lines = new List<(string, double, GatheringSource)>();

                lines.Add((resref, quantity, source));
            }

            foreach (var lines in byTable.Values)
            {
                var source = lines[0].Source;
                var visitSeconds = SecondsPerVisit(source, assumptions);
                var alone = lines.Select(l => l.Units / PerVisitOf(l, assumptions)).ToList();
                var visits = alone.Max();
                var total = alone.Sum();
                for (var i = 0; i < lines.Count; i++)
                    Add(lines[i].Component, visits * visitSeconds * (total > 0 ? alone[i] / total : 0));
            }

            return new RecipeGathering(secondsByComponent.Values.Sum(), modelled, unmodelled, secondsByFamily, secondsByComponent, unitsBySource);
        }

        private static double PerVisitOf((string Component, double Units, GatheringSource Source) line, ProgressionAssumptions assumptions) =>
            UnitsPerVisit(line.Source, assumptions);

        /// <summary>Whether the model prices the component from a node, rather than leaving it out or using the flat estimate.</summary>
        public bool IsModelled(string resref) => Classify(resref) is ComponentSource.Refined or ComponentSource.Harvested;
    }
}
