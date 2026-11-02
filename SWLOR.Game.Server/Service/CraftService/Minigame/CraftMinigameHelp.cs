using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Tracks which crafting minigames a character has opened, so a minigame's help page opens by itself the first time
/// and only then.
/// </summary>
public static class CraftMinigameHelp
{
    /// <summary>The partial view every minigame window defines for its help page.</summary>
    public const string PartialName = "CRAFT_MINIGAME_HELP";

    /// <summary>True until the character has opened the minigame once.</summary>
    public static bool ShouldOpenOnFirstUse(Player dbPlayer, CraftMinigameType minigame) =>
        dbPlayer.CraftMinigameHelpSeen == null || !dbPlayer.CraftMinigameHelpSeen.ContainsKey(minigame);

    /// <summary>
    /// Records first use. Returns true, and the caller opens the help page, only the first time for each minigame. The
    /// caller saves the player record.
    /// </summary>
    public static bool TryRecordFirstUse(Player dbPlayer, CraftMinigameType minigame, DateTime utcNow)
    {
        if (!ShouldOpenOnFirstUse(dbPlayer, minigame))
            return false;

        dbPlayer.CraftMinigameHelpSeen ??= new Dictionary<CraftMinigameType, DateTime>();
        dbPlayer.CraftMinigameHelpSeen[minigame] = utcNow;
        return true;
    }
}
