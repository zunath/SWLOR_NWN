using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// A capstone kill step: any enemy of the dungeon tier counts, shared by every line in that dungeon, but only once
    /// the player has used their capstone's mechanic on it. Progress stays under the line's own group, so steps already
    /// under way keep their counts.
    /// </summary>
    public sealed class CapstoneProofKillObjective : KillTargetObjective
    {
        private readonly NPCGroupType[] _creditedGroups;

        public string ProofKey { get; }

        public CapstoneProofKillObjective(NPCGroupType group, IEnumerable<NPCGroupType> tierGroups, string proofKey, int amount)
            : base(group, amount)
        {
            _creditedGroups = tierGroups.Append(group).Distinct().ToArray();
            ProofKey = proofKey;
        }

        public override IReadOnlyCollection<NPCGroupType> CreditedGroups => _creditedGroups;

        public override bool CountsKill(uint player, uint creature) => CapstoneProof.HasProof(player, creature, ProofKey);
    }
}
