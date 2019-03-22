using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Event.Creature
{
    public class OnSpawn : IRegisteredEvent
    {
        private readonly IBehaviourService _behaviour;
        private readonly ISpaceService _space;

        public OnSpawn(IBehaviourService behaviour,
                       ISpaceService space)
        {
            _behaviour = behaviour;
            _space = space;
        }

        public bool Run(params object[] args)
        {
            NWCreature self = Object.OBJECT_SELF;

            _space.OnCreatureSpawn(self);

            // Don't modify AI behaviour for DM-spawned creatures.
            if (self.GetLocalInt("DM_SPAWNED") == TRUE) return false;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return false;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return false;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                if (behaviour.IgnoreNWNEvents) self.SetLocalInt("IGNORE_NWN_EVENTS", 1);
                if (behaviour.IgnoreOnBlocked) self.SetLocalInt("IGNORE_NWN_ON_BLOCKED_EVENT", 1);
                if (behaviour.IgnoreOnCombatRoundEnd) self.SetLocalInt("IGNORE_NWN_ON_COMBAT_ROUND_END_EVENT", 1);
                if (behaviour.IgnoreOnConversation) self.SetLocalInt("IGNORE_NWN_ON_CONVERSATION_EVENT", 1);
                if (behaviour.IgnoreOnDamaged) self.SetLocalInt("IGNORE_NWN_ON_DAMAGED_EVENT", 1);
                if (behaviour.IgnoreOnDeath) self.SetLocalInt("IGNORE_NWN_ON_DEATH_EVENT", 1);
                if (behaviour.IgnoreOnDisturbed) self.SetLocalInt("IGNORE_NWN_ON_DISTURBED_EVENT", 1);
                if (behaviour.IgnoreOnHeartbeat) self.SetLocalInt("IGNORE_NWN_ON_HEARTBEAT_EVENT", 1);
                if (behaviour.IgnoreOnPerception) self.SetLocalInt("IGNORE_NWN_ON_PERCEPTION_EVENT", 1);
                if (behaviour.IgnoreOnPhysicalAttacked) self.SetLocalInt("IGNORE_NWN_ON_PHYSICAL_ATTACKED_EVENT", 1);
                if (behaviour.IgnoreOnRested) self.SetLocalInt("IGNORE_NWN_ON_RESTED_EVENT", 1);
                if (behaviour.IgnoreOnSpawn) self.SetLocalInt("IGNORE_NWN_ON_SPAWN_EVENT", 1);
                if (behaviour.IgnoreOnSpellCastAt) self.SetLocalInt("IGNORE_NWN_ON_SPELL_CAST_AT_EVENT", 1);
                if (behaviour.IgnoreOnUserDefined) self.SetLocalInt("IGNORE_NWN_ON_USER_DEFINED_EVENT", 1);

                if (behaviour.Behaviour != null)
                {
                    var result = behaviour.Behaviour
                        .End()
                        .Build();
                    _behaviour.RegisterBehaviour(result, self);
                }

                behaviour.OnSpawn();
            });

            return true;
        }
    }
}
