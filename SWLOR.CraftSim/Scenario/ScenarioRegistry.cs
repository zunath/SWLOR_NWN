namespace SWLOR.CraftSim.Scenario
{
    /// <summary>The scenarios the <c>gates</c> command can run, found by name.</summary>
    public sealed class ScenarioRegistry
    {
        private readonly List<ICraftScenario> _scenarios;

        public ScenarioRegistry(IEnumerable<ICraftScenario> scenarios)
        {
            _scenarios = scenarios.OrderBy(scenario => scenario.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var duplicate = _scenarios.GroupBy(scenario => scenario.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new ArgumentException($"Two scenarios are named '{duplicate.Key}'.");
        }

        /// <summary>
        /// Every public, concrete <see cref="ICraftScenario"/> with a public parameterless constructor in the simulator's assembly: a
        /// minigame's scenario is picked up by being added to <c>SWLOR.CraftSim</c>.
        /// </summary>
        public static ScenarioRegistry Discover() => new(
            typeof(ICraftScenario).Assembly.GetTypes()
                .Where(type => type.IsPublic && !type.IsAbstract && !type.IsInterface && typeof(ICraftScenario).IsAssignableFrom(type))
                .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => (ICraftScenario)Activator.CreateInstance(type)));

        public IReadOnlyList<ICraftScenario> Scenarios => _scenarios;

        public ICraftScenario Find(string name)
        {
            var found = _scenarios.FirstOrDefault(scenario => string.Equals(scenario.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
                return found;

            throw new ArgumentException(_scenarios.Count == 0
                ? $"No scenario named '{name}': no minigame has added one to SWLOR.CraftSim yet."
                : $"No scenario named '{name}'. Available: {string.Join(", ", _scenarios.Select(scenario => scenario.Name))}.");
        }
    }
}
