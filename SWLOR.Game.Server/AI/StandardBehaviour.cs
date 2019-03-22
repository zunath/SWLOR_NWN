using FluentBehaviourTree;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Standard behaviour which executes for all derived behaviours
    /// </summary>
    public class StandardBehaviour : BehaviourBase
    {
        protected readonly BehaviourTreeBuilder _builder;
        private readonly IEnmityService _enmity;
        private readonly IDialogService _dialog;
        

        public StandardBehaviour(BehaviourTreeBuilder builder,
            
            IEnmityService enmity,
            IDialogService dialog)
        {
            _builder = builder;
            _enmity = enmity;
            _dialog = dialog;
        }

        public override bool IgnoreNWNEvents => true;

        public override BehaviourTreeBuilder Behaviour
        {
            get
            {
                if (!Self.IsValid) return null;

                return _builder
                    .Parallel("StandardBehaviour", 5, 1)
                    .Do<CleanUpEnmity>(Self)
                    .Do<AttackHighestEnmity>(Self)
                    .Do<WarpToTargetIfStuck>(Self);
            }
        } 

        public override void OnPhysicalAttacked()
        {
            base.OnPhysicalAttacked();
            _enmity.OnNPCPhysicallyAttacked();
        }

        public override void OnDeath()
        {
            base.OnDeath();

            int vfx = Self.GetLocalInt("DEATH_VFX");
            if (vfx > 0)
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(vfx), Self);
            }
        }

        public override void OnDamaged()
        {
            base.OnDamaged();
            _enmity.OnNPCDamaged();
        }

        public override void OnConversation()
        {
            base.OnConversation();
            string convo = Self.GetLocalString("CONVERSATION");
            
            if (!string.IsNullOrWhiteSpace(convo))
            {
                NWPlayer player = (_.GetLastSpeaker());
                _dialog.StartConversation(player, Self, convo);
            }
            else if (!string.IsNullOrWhiteSpace(NWNXObject.GetDialogResref(Self)))
            {
                _.BeginConversation(NWNXObject.GetDialogResref(Self));
            }
        }

        public override void OnBlocked()
        {
            base.OnBlocked();

            NWObject door = (_.GetBlockingDoor());
            if (!door.IsValid) return;

            if (_.GetIsDoorActionPossible(door.Object, _.DOOR_ACTION_OPEN) == _.TRUE)
            {
                _.DoDoorAction(door.Object, _.DOOR_ACTION_OPEN);
            }
        }

    }
}
