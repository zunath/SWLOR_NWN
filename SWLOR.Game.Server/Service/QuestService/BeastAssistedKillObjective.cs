using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// A kill step that counts only once a beast owned by the player hit the enemy with a normal attack or damaging
    /// ability. Any beast role or level counts, and the beast need not land the killing blow.
    /// </summary>
    public sealed class BeastAssistedKillObjective : KillTargetObjective
    {
        public BeastAssistedKillObjective(NPCGroupType group, int amount)
            : base(group, amount)
        {
        }

        public override bool CountsKill(uint player, uint creature) => CapstoneProof.HasOwnedBeastHit(player, creature);
    }
}
