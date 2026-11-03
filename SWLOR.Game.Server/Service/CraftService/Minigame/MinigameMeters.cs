namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The meters every minigame shares. An engine maps its own names onto them: Shape, Assembly or Doneness is
/// <see cref="Progress"/>; Finish, Calibration or Subtlety is <see cref="Quality"/>; Machine Condition, Freshness or
/// Stability is <see cref="Integrity"/>.
/// </summary>
public sealed record MinigameMeters(
    MinigameMeter Progress,
    MinigameMeter Quality,
    MinigameMeter Integrity,
    MinigameMeter Cp)
{
    /// <summary>The quality meter as a whole percentage, for thresholds and display.</summary>
    public int QualityPercent => Quality.Percent;

    /// <summary>A fresh attempt: no progress, no quality, full integrity and CP.</summary>
    public static MinigameMeters Start(int maxProgress, int maxQuality, int maxIntegrity, int maxCp) =>
        new(
            new MinigameMeter(0, maxProgress),
            new MinigameMeter(0, maxQuality),
            new MinigameMeter(maxIntegrity, maxIntegrity),
            new MinigameMeter(maxCp, maxCp));

    /// <summary>The meters after <paramref name="delta"/>, each kept inside its range.</summary>
    public MinigameMeters Apply(MinigameMeterDelta delta) =>
        new(
            Progress.Add(delta.Progress),
            Quality.Add(delta.Quality),
            Integrity.Add(delta.Integrity),
            Cp.Add(delta.Cp));

    /// <summary>The change from these meters to <paramref name="other"/>.</summary>
    public MinigameMeterDelta DeltaTo(MinigameMeters other) =>
        new(
            other.Progress.Value - Progress.Value,
            other.Quality.Value - Quality.Value,
            other.Integrity.Value - Integrity.Value,
            other.Cp.Value - Cp.Value);
}
