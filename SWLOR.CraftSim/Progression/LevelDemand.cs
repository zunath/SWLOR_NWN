using SWLOR.CraftSim.Data;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// The components one craft of a skill at a recipe level commits, averaged over the level's recipes (each recipe counts equally,
    /// as in the gates' cohort), and what gathering them costs. Each recipe's components are priced together
    /// (<see cref="ComponentSupply.PriceRecipe"/>).
    /// </summary>
    public sealed class LevelDemand
    {
        private readonly Dictionary<ComponentSource, double> _unitsBySource =
            Enum.GetValues<ComponentSource>().ToDictionary(source => source, _ => 0.0);

        private readonly Dictionary<string, double> _secondsByFamily = new();
        private readonly Dictionary<string, double> _secondsByComponent = new(StringComparer.OrdinalIgnoreCase);

        private LevelDemand()
        {
        }

        /// <summary>Recipes the average is taken over.</summary>
        public int Recipes { get; private set; }

        /// <summary>Component units a node supplies, per craft attempt that commits every stack.</summary>
        public double ModelledUnits { get; private set; }

        /// <summary>Component units no node supplies, per craft attempt that commits every stack.</summary>
        public double UnmodelledUnits { get; private set; }

        /// <summary>Seconds of gathering and refining for one craft attempt that commits every stack.</summary>
        public double SecondsPerAttempt { get; private set; }

        /// <summary>Component units per craft attempt that commits every stack, by where the model says they come from.</summary>
        public IReadOnlyDictionary<ComponentSource, double> UnitsBySource => _unitsBySource;

        /// <summary>Seconds per craft attempt that commits every stack, by the kind of node the components come from.</summary>
        public IReadOnlyDictionary<string, double> SecondsByFamily => _secondsByFamily;

        /// <summary>Seconds per craft attempt that commits every stack, by component.</summary>
        public IReadOnlyDictionary<string, double> SecondsByComponent => _secondsByComponent;

        public static LevelDemand Build(IReadOnlyList<RecipeEntry> atLevel, ComponentSupply supply, ProgressionAssumptions assumptions)
        {
            if (atLevel.Count == 0)
                throw new ArgumentException("A level needs at least one recipe.", nameof(atLevel));

            var chosen = atLevel;
            if (assumptions.CheapestQuartileRecipes)
            {
                // Ties break on the recipe's name so the choice does not depend on the order the recipes were loaded in.
                chosen = atLevel
                    .OrderBy(recipe => SecondsOf(recipe, supply, assumptions))
                    .ThenBy(recipe => recipe.Type.ToString(), StringComparer.Ordinal)
                    .Take((atLevel.Count + 3) / 4)
                    .ToList();
            }

            var demand = new LevelDemand { Recipes = chosen.Count };
            foreach (var recipe in chosen)
            {
                var priced = supply.PriceRecipe(recipe, assumptions);
                demand.ModelledUnits += priced.ModelledUnits;
                demand.UnmodelledUnits += priced.UnmodelledUnits;
                demand.SecondsPerAttempt += priced.Seconds;
                foreach (var (source, units) in priced.UnitsBySource)
                    demand._unitsBySource[source] += units;
                foreach (var (family, seconds) in priced.SecondsByFamily)
                    demand._secondsByFamily[family] = demand._secondsByFamily.GetValueOrDefault(family) + seconds;
                foreach (var (component, seconds) in priced.SecondsByComponent)
                    demand._secondsByComponent[component] = demand._secondsByComponent.GetValueOrDefault(component) + seconds;
            }

            demand.ModelledUnits /= chosen.Count;
            demand.UnmodelledUnits /= chosen.Count;
            demand.SecondsPerAttempt /= chosen.Count;
            foreach (var source in demand._unitsBySource.Keys.ToList())
                demand._unitsBySource[source] /= chosen.Count;
            foreach (var family in demand._secondsByFamily.Keys.ToList())
                demand._secondsByFamily[family] /= chosen.Count;
            foreach (var component in demand._secondsByComponent.Keys.ToList())
                demand._secondsByComponent[component] /= chosen.Count;

            return demand;
        }

        /// <summary>Seconds of gathering and refining for one craft of a recipe that commits every stack.</summary>
        public static double SecondsOf(RecipeEntry recipe, ComponentSupply supply, ProgressionAssumptions assumptions) =>
            supply.PriceRecipe(recipe, assumptions).Seconds;
    }
}
