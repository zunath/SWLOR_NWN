using FluentBehaviourTree;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;




namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Generic behaviour for creatures who aggro by sight.
    /// </summary>
    public class SoundAggroRandomWalkBehaviour : StandardBehaviour
    {
        public SoundAggroRandomWalkBehaviour(
            BehaviourTreeBuilder builder
            
            
            )
            : base(builder)
        {
        }

        public override BehaviourTreeBuilder Behaviour =>
            base.Behaviour
                .Do<CleanUpEnmity>(Self)
                .Do<AttackHighestEnmity>(Self)
                .Do<EquipBestMelee>(Self)
                .Do<EquipBestRanged>(Self)
                .Do<AggroTargetBySound>(Self)
                .Do<RandomWalk>(Self);

    }
}
