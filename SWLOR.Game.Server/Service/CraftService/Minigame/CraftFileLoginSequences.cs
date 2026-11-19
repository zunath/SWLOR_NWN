using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The craft file sequence each character file carried when a player logged in with it, by character UUID, until the login applies it.
/// <para>
/// The engine keeps a player's creature locals in memory across relogs until the server restarts, and a relog reads the character file
/// into a temporary creature, so a sequence restored onto the creature being loaded never reaches the creature that enters. Without this,
/// every relog compared the journals with the sequence of the first login since the restart, and gave back items the file already held.
/// </para>
/// </summary>
public static class CraftFileLoginSequences
{
    private static readonly Dictionary<string, int> Pending = new();
    private static readonly object Gate = new();

    /// <summary>
    /// Records the sequence a character file carried as a player loads it: 0 when the file has none. A later load of the same character
    /// replaces it.
    /// </summary>
    public static void Record(string characterId, int sequence)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        lock (Gate)
            Pending[characterId] = Math.Max(0, sequence);
    }

    /// <summary>
    /// Takes the sequence recorded for the character's last load, once. Returns false when there is none, including when this login
    /// already took it.
    /// </summary>
    public static bool TryTake(string characterId, out int sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        lock (Gate)
            return Pending.Remove(characterId, out sequence);
    }
}
