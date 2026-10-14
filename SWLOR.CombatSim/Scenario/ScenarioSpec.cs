using Newtonsoft.Json;

namespace SWLOR.CombatSim.Scenario
{
    public sealed class ScenarioSpec
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float DurationSeconds { get; set; } = 600f;
        public float TimeStepSeconds { get; set; } = 0.1f;

        /// <summary>Player-vs-player fight: PvP rules apply between player combatants.</summary>
        public bool IsPvp { get; set; } = true;

        public List<TeamSpec> Teams { get; set; } = new();

        public static ScenarioSpec Load(string path)
        {
            ScenarioSpec scenario;
            try
            {
                // Unknown names fail, so a misspelled setting can't silently keep its default.
                scenario = JsonConvert.DeserializeObject<ScenarioSpec>(File.ReadAllText(path), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Scenario '{path}': {ex.Message}", ex);
            }

            if (scenario?.Teams == null || scenario.Teams.Count < 2)
                throw new InvalidDataException($"Scenario '{path}' needs at least two teams.");

            return scenario;
        }
    }
}
