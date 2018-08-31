using FluentBehaviourTree;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI
{
    public class SightAggroBehaviour: StandardBehaviour
    {
        public SightAggroBehaviour(
            BehaviourTreeBuilder builder, 
            INWScript script, 
            IEnmityService enmity, 
            IDialogService dialog,
            INWNXObject nwnxObject) 
            : base(builder, script, enmity, dialog, nwnxObject)
        {
        }

        public override BehaviourTreeBuilder Behaviour => 
            base.Behaviour
            .Do<EquipBestMelee>(Self)
            .Do<EquipBestRanged>(Self)
            .Do<AggroTargetBySight>(Self)
            .Do<RandomWalk>(Self);

    }
}
