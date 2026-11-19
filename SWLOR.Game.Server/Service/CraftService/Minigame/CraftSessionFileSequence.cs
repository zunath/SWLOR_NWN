namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Names of the creature locals behind the craft file sequence. The sequence counts the item transfers between the player record and
/// the character file, and checkpoints restoring a Performance hot bar. <c>PlayerFileCheckpoint</c> writes it into the character file
/// and, at login, sets these locals from the file the player logged in with (<c>ApplyLoginCraftSequence</c>): the engine keeps a
/// player's locals across relogs until a restart, so after a relog they would still describe the last session.
/// </summary>
public static class CraftSessionFileSequence
{
    /// <summary>The sequence of the last transfer made in memory. The next character file written carries this value.</summary>
    public const string Variable = "CRAFT_FILE_SEQUENCE";

    /// <summary>The highest sequence known to be in the character file. It follows <see cref="Variable"/> each time this process writes the file.</summary>
    public const string ExportedVariable = "CRAFT_FILE_EXPORTED";

    /// <summary>The sequence the character file carried when it loaded. It never changes until the next load, so every login check compares
    /// against the same value whatever ran first.</summary>
    public const string LoadedVariable = "CRAFT_FILE_LOADED";
}
