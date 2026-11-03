using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>Pays Research Experiment XP through the live game: the player's state, saves and pending XP grant.</summary>
internal sealed class NwnPayoutWorld : IResearchPayoutWorld
{
    private readonly uint _player;

    public NwnPayoutWorld(uint player)
    {
        _player = player;
    }

    public bool CanReceiveXp() => !GetIsDead(_player) && GetCurrentHitPoints(_player) > 0;

    public int BaseXp(ResearchJob job) => ResearchExperimentService.BaseXp(_player, job);

    public void SavePlayer(Player dbPlayer) => DB.Set(dbPlayer);

    public void SaveJob(ResearchJob job) => DB.Set(job);

    public void GrantPendingXp() => CraftSessionService.GrantPendingXp(_player);
}
