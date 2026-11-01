using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// A capstone kill step: any enemy of the dungeon tier counts, shared by every line in that dungeon, but only once
    /// the player has used their capstone's mechanic on it.
    /// </summary>
    public sealed class CapstoneProofKillObjective : KillTargetObjective
    {
        public string ProofKey { get; }

        public CapstoneProofKillObjective(NPCGroupType group, string proofKey, int amount)
            : base(group, amount)
        {
            ProofKey = proofKey;
        }

        public override bool CountsKill(uint player, uint creature) => CapstoneProof.HasProof(player, creature, ProofKey);
    }
}
