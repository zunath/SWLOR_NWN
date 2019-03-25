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
        public override bool IgnoreNWNEvents => true;

        private void DoForceAttack()
        {
            // Trigger ForceAttackHighestEmnity if not doing anything. 
            if (_.GetCurrentAction() == _.ACTION_ATTACKOBJECT)
            {
                _.ClearAllActions();

                NWObject self = Object.OBJECT_SELF;
                ForceAttackHighestEnmity component = new ForceAttackHighestEnmity();
                component.Run(new object[] {self});
            }
        }

        public override BehaviourTreeBuilder BuildBehaviour(NWCreature self)
        {
            if (!self.IsValid) return null;

            return AIService.BehaviourTree
                .Parallel("StandardBehaviour", 5, 1)
                .Do<CleanUpEnmity>(self)
                .Do<ForceAttackHighestEnmity>(self);
        } 

        public override void OnPhysicalAttacked(NWCreature self)
        {
            base.OnPhysicalAttacked(self);
            EnmityService.OnNPCPhysicallyAttacked();

            DoForceAttack();
        }

        public override void OnDeath(NWCreature self)
        {
            base.OnDeath(self);

            int vfx = self.GetLocalInt("DEATH_VFX");
            if (vfx > 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(vfx), self);
            }
        }

        public override void OnDamaged(NWCreature self)
        {
            base.OnDamaged(self);
            EnmityService.OnNPCDamaged();

            DoForceAttack();
        }

        public override void OnConversation(NWCreature self)
        {
            base.OnConversation(self);
            string convo = self.GetLocalString("CONVERSATION");
            
            if (!string.IsNullOrWhiteSpace(convo))
            {
                NWPlayer player = (_.GetLastSpeaker());
                DialogService.StartConversation(player, self, convo);
            }
            else if (!string.IsNullOrWhiteSpace(NWNXObject.GetDialogResref(self)))
            {
                _.BeginConversation(NWNXObject.GetDialogResref(self));
            }
        }

        public override void OnBlocked(NWCreature self)
        {
            base.OnBlocked(self);

            NWObject door = (_.GetBlockingDoor());
            if (!door.IsValid) return;

            if (_.GetIsDoorActionPossible(door.Object, DOOR_ACTION_OPEN) == TRUE)
            {
                _.DoDoorAction(door.Object, DOOR_ACTION_OPEN);
            }
        }

    }
}
