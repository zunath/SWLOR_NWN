using FluentAssertions;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

namespace SWLOR.Game.Server.Tests.Service.Reaction;

/// <summary>
/// Finds a way through a Reaction attempt for the tests: a beam search over copies of the engine. The attempt is deterministic once
/// its starting position is set, so a path found from a fresh engine can be played back on the real one.
/// </summary>
internal static class ReactionPlanner
{
    private static readonly string[] Actions =
    {
        ReactionEngine.Measure, ReactionEngine.Charge, ReactionEngine.Refine, ReactionEngine.Dampen, ReactionEngine.Stabilize
    };

    /// <summary>The shortest action sequence the search finds that finishes the item at or above the quality, or null when it finds none.</summary>
    public static IReadOnlyList<string> FindPath(ICraftMinigameEngine start, int minimumQuality, int width = 60, int maxDepth = 40)
    {
        var beam = new List<(ICraftMinigameEngine Engine, List<string> Path)> { (start, new List<string>()) };
        for (var depth = 1; depth <= maxDepth && beam.Count > 0; depth++)
        {
            var next = new Dictionary<string, (double Score, ICraftMinigameEngine Engine, List<string> Path)>();
            foreach (var (engine, path) in beam)
            {
                foreach (var action in Actions)
                {
                    if (!engine.Preview(action).Enabled)
                        continue;

                    var copy = engine.Fork(new SeededMinigameRandom(1));
                    var result = copy.Perform(action);
                    var extended = new List<string>(path) { action };
                    if (result.Status == MinigameStatus.Succeeded && copy.Meters.Quality.Percent >= minimumQuality)
                        return extended;

                    if (result.Status != MinigameStatus.InProgress)
                        continue;

                    var key = Key(copy);
                    var score = Score(copy, minimumQuality);
                    if (!next.TryGetValue(key, out var existing) || existing.Score < score)
                        next[key] = (score, copy, extended);
                }
            }

            beam = next.Values.OrderByDescending(entry => entry.Score).Take(width).Select(entry => (entry.Engine, entry.Path)).ToList();
        }

        return null;
    }

    /// <summary>Plays a path found by <see cref="FindPath"/> on an engine and returns it.</summary>
    public static T Play<T>(T engine, IEnumerable<string> path)
        where T : ICraftMinigameEngine
    {
        foreach (var action in path)
            engine.Perform(action).Performed.Should().BeTrue(action);

        return engine;
    }

    private static string Key(ICraftMinigameEngine engine) =>
        string.Join("|", engine.DescribeMeters().Select(meter => meter.Value));

    private static double Score(ICraftMinigameEngine engine, int minimumQuality) =>
        Math.Min(engine.Meters.Quality.Value, minimumQuality) * 1.5 +
        engine.Meters.Progress.Value +
        0.3 * engine.Meters.Integrity.Value +
        0.3 * Math.Min(engine.Meters.Cp.Value, 20);
}
