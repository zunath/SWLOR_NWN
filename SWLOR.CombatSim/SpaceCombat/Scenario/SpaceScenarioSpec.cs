using Newtonsoft.Json;

namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>
/// A space fight between an attacking side and a defending side. Every number is a scenario input; the shared
/// ground formulas (hit rate, damage range, critical rate) are the only constants.
/// </summary>
public sealed class SpaceScenarioSpec
{
    public const int MaximumShipsPerSide = 100;

    public string Name { get; set; }
    public string Description { get; set; }
    public double DurationSeconds { get; set; } = 120;
    public double TimeStepSeconds { get; set; } = 0.1;

    /// <summary>Percentage points of hit chance lost for each size step the weapon is above its target.</summary>
    public int SizeHitPenaltyPerStep { get; set; }

    /// <summary>Share of damage lost (0-1) for each size step the weapon is below its target, compounding per step.</summary>
    public double SizeDamagePenaltyPerStep { get; set; }

    /// <summary>Replaces Combat.MinimumCriticalRate (5) when given.</summary>
    public int? CriticalRateMinimum { get; set; }

    /// <summary>Replaces Combat.MaximumCriticalRate (50) when given.</summary>
    public int? CriticalRateMaximum { get; set; }

    public SpaceSideSpec Attackers { get; set; }
    public SpaceSideSpec Defenders { get; set; }

    public static SpaceScenarioSpec Load(string path)
    {
        SpaceScenarioSpec scenario;
        try
        {
            // Unknown names fail, so a misspelled setting can't silently keep its default.
            scenario = JsonConvert.DeserializeObject<SpaceScenarioSpec>(
                File.ReadAllText(path), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Scenario '{path}': {ex.Message}", ex);
        }

        if (scenario == null)
            throw new InvalidDataException($"Scenario '{path}' is empty.");

        try
        {
            SpaceScenarioValidator.Validate(scenario);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"Scenario '{path}': {ex.Message}", ex);
        }

        return scenario;
    }
}
