namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// A marked range or line on a meter, such as a working range, an overload line or a target zone. A line has
/// <paramref name="From"/> equal to <paramref name="To"/>.
/// </summary>
public sealed record MinigameMeterMark(int From, int To, string Label)
{
    public bool IsLine => From == To;

    public static MinigameMeterMark Line(int at, string label) => new(at, at, label);

    public static MinigameMeterMark Range(int from, int to, string label) => new(from, to, label);
}
