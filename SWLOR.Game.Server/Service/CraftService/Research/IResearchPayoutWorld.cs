using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>What <see cref="ResearchExperimentPayout"/> needs from the database and the game engine.</summary>
public interface IResearchPayoutWorld
{
    /// <summary>True while the player can be given XP: alive, with hit points.</summary>
    bool CanReceiveXp();

    /// <summary>The chart XP for the job's recipe level against the player's Fabrication rank.</summary>
    int BaseXp(ResearchJob job);

    /// <summary>Saves the player's record.</summary>
    void SavePlayer(Player dbPlayer);

    /// <summary>Saves the job's record.</summary>
    void SaveJob(ResearchJob job);

    /// <summary>Gives the XP waiting in <see cref="Player.PendingCraftXp"/>: it is cleared and saved first, then given, and nothing happens while the player is dead.</summary>
    void GrantPendingXp();
}
