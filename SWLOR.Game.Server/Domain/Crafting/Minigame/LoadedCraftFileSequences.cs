using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame;

/// <summary>
/// The craft file sequence each character file carried when a player logged in with it, kept outside the creature's locals.
/// A player who logs out and back in before a server restart gets the locals they left with copied back over the new creature after
/// the file loads, which replaces the <see cref="CraftSessionFileSequence.LoadedVariable"/> the load just set with the one from that
/// earlier login. Every transfer made since then (a research job's start, a craft's debit) would then look missing from a file that
/// holds it. The load records the true value here, and <see cref="Apply"/> puts it back on the creature once the player is in.
/// </summary>
public static class LoadedCraftFileSequences
{
    /// <summary>A record no login claims within this time (a load that never entered the module) is dropped at the next record.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private static readonly Dictionary<string, (int Sequence, DateTime Recorded)> Loads = new();

    /// <summary>
    /// Records the sequence the file carried (0 when it carried none) under the character's UUID from that file. The UUID, not the
    /// object id, links the load to the login: a player logging back in gets their earlier object id back after the file loads.
    /// </summary>
    public static void Record(string characterId, int sequence, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        var expired = new List<string>();
        foreach (var (key, load) in Loads)
        {
            if (now - load.Recorded > Lifetime)
                expired.Add(key);
        }

        foreach (var key in expired)
            Loads.Remove(key);

        Loads[characterId] = (Math.Max(0, sequence), now);
    }

    /// <summary>Takes the recorded sequence for the character, once. False when there is none (already applied, or no file was loaded).</summary>
    public static bool TryTake(string characterId, out int sequence)
    {
        if (!string.IsNullOrWhiteSpace(characterId) && Loads.Remove(characterId, out var load))
        {
            sequence = load.Sequence;
            return true;
        }

        sequence = 0;
        return false;
    }

    /// <summary>
    /// The craft locals once the file's sequence is applied: the file holds exactly <paramref name="fileSequence"/>, so that is both the
    /// loaded and the exported sequence. The next transfer must still number past any sequence already handed out, so the counter
    /// keeps the higher of the two.
    /// </summary>
    public static (int Sequence, int Exported, int Loaded) Resolve(int fileSequence, int currentSequence) =>
        (Math.Max(fileSequence, currentSequence), fileSequence, fileSequence);

    /// <summary>
    /// Sets the character's craft locals from the sequence its file loaded with, when one is waiting. Call it before anything reads them
    /// at login; later calls do nothing.
    /// </summary>
    public static void Apply(string characterId, Func<string, int> getInt, Action<string, int> setInt)
    {
        if (!TryTake(characterId, out var file))
            return;

        var (sequence, exported, loaded) = Resolve(file, getInt(CraftSessionFileSequence.Variable));
        setInt(CraftSessionFileSequence.Variable, sequence);
        setInt(CraftSessionFileSequence.ExportedVariable, exported);
        setInt(CraftSessionFileSequence.LoadedVariable, loaded);
    }

    /// <summary>Forgets every record. For tests.</summary>
    public static void Clear() => Loads.Clear();
}
