using FluentBehaviourTree;
using SWLOR.Game.Server.AI.AIComponent;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;

using NWN;


using static NWN._;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Standard behaviour which executes for all derived behaviours
    /// </summary>
    public class DarkForceUser : BehaviourBase
    {
        
        protected readonly BehaviourTreeBuilder _builder;
        
        
        

        public DarkForceUser(BehaviourTreeBuilder builder)
        {
            
            _builder = builder;
            
            
        }

        public override bool IgnoreNWNEvents => true;

        private void DoForceAttack()
        {
            // Trigger ForceAttackHighestEmnity if not doing anything. 
            if (_.GetCurrentAction() == _.ACTION_ATTACKOBJECT)
            {
                _.ClearAllActions();

                NWObject self = NWN.Object.OBJECT_SELF;
                App.RunEvent<ForceAttackHighestEnmity>(self);
            }
        }

        public override BehaviourTreeBuilder Behaviour
        {
            get
            {
                if (!Self.IsValid) return null;

                return _builder
                    .Parallel("StandardBehaviour", 5, 1)
                    .Do<CleanUpEnmity>(Self)
                    .Do<ForceAttackHighestEnmity>(Self);
            }
        } 

        public override void OnPhysicalAttacked()
        {
            base.OnPhysicalAttacked();
            EnmityService.OnNPCPhysicallyAttacked();

            DoForceAttack();
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
            EnmityService.OnNPCDamaged();

            DoForceAttack();
        }

        public override void OnConversation()
        {
            base.OnConversation();
            string convo = Self.GetLocalString("CONVERSATION");
            
            if (!string.IsNullOrWhiteSpace(convo))
            {
                NWPlayer player = (_.GetLastSpeaker());
                DialogService.StartConversation(player, Self, convo);
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

            if (_.GetIsDoorActionPossible(door.Object, DOOR_ACTION_OPEN) == TRUE)
            {
                _.DoDoorAction(door.Object, DOOR_ACTION_OPEN);
            }
        }

    }
}
