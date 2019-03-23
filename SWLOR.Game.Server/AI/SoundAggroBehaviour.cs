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
    public class SoundAggroBehaviour : StandardBehaviour
    {
        public SoundAggroBehaviour(
            BehaviourTreeBuilder builder,
            
            
            IDialogService dialog)
            : base(builder, dialog)
        {
        }

        public override BehaviourTreeBuilder Behaviour =>
            base.Behaviour
                .Do<CleanUpEnmity>(Self)
                .Do<AttackHighestEnmity>(Self)
                .Do<EquipBestMelee>(Self)
                .Do<EquipBestRanged>(Self)
                .Do<AggroTargetBySound>(Self);

    }
}
