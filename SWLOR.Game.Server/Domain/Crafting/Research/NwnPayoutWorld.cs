using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Domain.Crafting.Research;

/// <summary>Pays Research Experiment XP through the live game: the player's state, saves and pending XP grant.</summary>
internal sealed class NwnPayoutWorld : IResearchPayoutWorld
{
    private readonly uint _player;

    public NwnPayoutWorld(uint player)
    {
        _player = player;
    }

    public bool CanReceiveXp() => !GetIsDead(_player) && GetCurrentHitPoints(_player) > 0;

    public int BaseXp(ResearchJob job) => CraftResearchExperiment.BaseXp(_player, job);

    public void SavePlayer(Player dbPlayer) => DB.Set(dbPlayer);

    public void SaveJob(ResearchJob job) => DB.Set(job);

    public void GrantPendingXp() => CraftSessions.GrantPendingXp(_player);
}
