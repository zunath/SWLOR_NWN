namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>A change to the shared meters. Negative values spend or lose, positive values gain or restore.</summary>
public readonly record struct MinigameMeterDelta(int Progress = 0, int Quality = 0, int Integrity = 0, int Cp = 0)
{
    public bool IsZero => Progress == 0 && Quality == 0 && Integrity == 0 && Cp == 0;

    public static MinigameMeterDelta operator +(MinigameMeterDelta left, MinigameMeterDelta right) =>
        new(left.Progress + right.Progress, left.Quality + right.Quality, left.Integrity + right.Integrity, left.Cp + right.Cp);
}
