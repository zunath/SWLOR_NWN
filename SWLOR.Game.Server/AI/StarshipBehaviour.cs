using FluentBehaviourTree;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Standard behaviour which executes for all derived behaviours
    /// </summary>
    public class StarshipBehaviour : BehaviourBase
    {
        
        protected readonly BehaviourTreeBuilder _builder;
        private readonly IEnmityService _enmity;
        private readonly IDialogService _dialog;
        private readonly INWNXObject _nwnxObject;
        private readonly ISpaceService _space;

        public StarshipBehaviour(BehaviourTreeBuilder builder,
            
            IEnmityService enmity,
            IDialogService dialog,
            INWNXObject nwnxObject,
            ISpaceService space)
        {
            
            _builder = builder;
            _enmity = enmity;
            _dialog = dialog;
            _nwnxObject = nwnxObject;
            _space = space;
        }

        public override bool IgnoreNWNEvents => true;

        public override BehaviourTreeBuilder Behaviour
        {
            get
            {
                if (!Self.IsValid) return null;

                return _builder
                    .Parallel("StarshipBehaviour", 5, 1)
                    .Do<CleanUpEnmity>(Self);
            }
        } 

        public override void OnPhysicalAttacked()
        {
            base.OnPhysicalAttacked();
            _enmity.OnNPCPhysicallyAttacked();

            NWCreature self = Object.OBJECT_SELF;
            NWCreature attacker = _.GetLastAttacker();

            _space.OnPhysicalAttacked(self, attacker);
        }

        public override void OnDeath()
        {
            base.OnDeath();

            int vfx = Self.GetLocalInt("DEATH_VFX");
            if (vfx > 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(vfx), Self);
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
            else if (!string.IsNullOrWhiteSpace(_nwnxObject.GetDialogResref(Self)))
            {
                _.BeginConversation(_nwnxObject.GetDialogResref(Self));
            }
        }

        public override void OnPerception()
        {
            base.OnPerception();
            _space.OnPerception(Object.OBJECT_SELF, _.GetLastPerceived());
        }

        public override void OnHeartbeat()
        {
            base.OnHeartbeat();
            _space.OnHeartbeat(Object.OBJECT_SELF);
        }

        public override void OnBlocked()
        {
            base.OnBlocked();

            return;
        }

    }
}
