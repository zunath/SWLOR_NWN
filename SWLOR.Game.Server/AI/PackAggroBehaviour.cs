using FluentBehaviourTree;
using NWN;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;


using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Generic behaviour for creatures who aggro by sight.
    /// </summary>
    public class PackAggroBehaviour : StandardBehaviour
    {
        public PackAggroBehaviour(
            BehaviourTreeBuilder builder,
            
            IEnmityService enmity,
            IDialogService dialog)
            : base(builder, enmity, dialog)
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
