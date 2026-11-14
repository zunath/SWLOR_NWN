namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The settings of a DM battle grid: square size, how many sections it spans, and how far it floats above the
    /// spot it was placed on.
    /// </summary>
    public readonly record struct BattleGridSettings(float SquareSize, int SectionsWide, int SectionsLong, float Height);
}
