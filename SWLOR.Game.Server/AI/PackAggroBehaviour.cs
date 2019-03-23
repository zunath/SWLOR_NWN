using FluentBehaviourTree;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;




namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Generic behaviour for creatures who aggro by sight.
    /// </summary>
    public class PackAggroBehaviour : StandardBehaviour
    {
        public PackAggroBehaviour(
            BehaviourTreeBuilder builder)
            : base(builder)
        {
        }

        public override BehaviourTreeBuilder Behaviour =>
            base.Behaviour
                .Do<CleanUpEnmity>(Self)
                .Do<AttackHighestEnmity>(Self)
                .Do<EquipBestMelee>(Self)
                .Do<EquipBestRanged>(Self)
                .Do<AggroTargetBySight>(Self)
                .Do<RandomWalk>(Self)
                .Do<AILinking>(Self);
    }
}
