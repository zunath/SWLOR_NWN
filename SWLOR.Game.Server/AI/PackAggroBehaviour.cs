using FluentBehaviourTree;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;


namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Generic behaviour for creatures who aggro by sight.
    /// </summary>
    public class PackAggroBehaviour : StandardBehaviour
    {
        public override BehaviourTreeBuilder BuildBehaviour(NWCreature self)
        {
            return base.BuildBehaviour(self)
                .Do<CleanUpEnmity>(self)
                .Do<AttackHighestEnmity>(self)
                .Do<EquipBestMelee>(self)
                .Do<EquipBestRanged>(self)
                .Do<AggroTargetBySight>(self)
                .Do<RandomWalk>(self)
                .Do<AILinking>(self);
        }

    }
}
