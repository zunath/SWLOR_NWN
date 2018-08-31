using System;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.Event.Creature
{
    public class OnCreatureEvent : IRegisteredEvent
    {
        private NWCreature Self { get; }
        private readonly ISkillService _skill;
        private readonly ILootService _loot;
        private readonly IBehaviourService _behaviour;
        private readonly AppState _state;

        public OnCreatureEvent(ISkillService skill,
            ILootService loot,
            IBehaviourService behaviour,
            AppState state)
        {
            Self = NWCreature.Wrap(Object.OBJECT_SELF);
            _skill = skill;
            _loot = loot;
            _behaviour = behaviour;
            _state = state;
        }

        public bool Run(params object[] args)
        {
            string creatureScript = Self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = Self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = Self.GetLocalString("SCRIPT");

            IBehaviour behaviour = null;

            if (!string.IsNullOrWhiteSpace(creatureScript) && 
                App.IsKeyRegistered<IBehaviour>("AI." + creatureScript))
            {
                behaviour = App.ResolveByInterface<IBehaviour>("AI." + creatureScript);
                if (behaviour.IgnoreNWNEvents) Self.SetLocalInt("IGNORE_NWN_EVENTS", 1);
                if (behaviour.IgnoreOnBlocked) Self.SetLocalInt("IGNORE_NWN_ON_BLOCKED_EVENT", 1);
                if (behaviour.IgnoreOnCombatRoundEnd) Self.SetLocalInt("IGNORE_NWN_ON_COMBAT_ROUND_END_EVENT", 1);
                if (behaviour.IgnoreOnConversation) Self.SetLocalInt("IGNORE_NWN_ON_CONVERSATION_EVENT", 1);
                if (behaviour.IgnoreOnDamaged) Self.SetLocalInt("IGNORE_NWN_ON_DAMAGED_EVENT", 1);
                if (behaviour.IgnoreOnDeath) Self.SetLocalInt("IGNORE_NWN_ON_DEATH_EVENT", 1);
                if (behaviour.IgnoreOnDisturbed) Self.SetLocalInt("IGNORE_NWN_ON_DISTURBED_EVENT", 1);
                if (behaviour.IgnoreOnHeartbeat) Self.SetLocalInt("IGNORE_NWN_ON_HEARTBEAT_EVENT", 1);
                if (behaviour.IgnoreOnPerception) Self.SetLocalInt("IGNORE_NWN_ON_PERCEPTION_EVENT", 1);
                if (behaviour.IgnoreOnPhysicalAttacked) Self.SetLocalInt("IGNORE_NWN_ON_PHYSICAL_ATTACKED_EVENT", 1);
                if (behaviour.IgnoreOnRested) Self.SetLocalInt("IGNORE_NWN_ON_RESTED_EVENT", 1);
                if (behaviour.IgnoreOnSpawn) Self.SetLocalInt("IGNORE_NWN_ON_SPAWN_EVENT", 1);
                if (behaviour.IgnoreOnSpellCastAt) Self.SetLocalInt("IGNORE_NWN_ON_SPELL_CAST_AT_EVENT", 1);
                if (behaviour.IgnoreOnUserDefined) Self.SetLocalInt("IGNORE_NWN_ON_USER_DEFINED_EVENT", 1);
            }

            CreatureEventType type = (CreatureEventType)args[0];
            switch (type)
            {
                case CreatureEventType.OnPhysicalAttacked:
                    behaviour?.OnPhysicalAttacked();
                    break;
                case CreatureEventType.OnBlocked:
                    behaviour?.OnBlocked();
                    break;
                case CreatureEventType.OnConversation:
                    behaviour?.OnConversation();
                    break;
                case CreatureEventType.OnDamaged:
                    behaviour?.OnDamaged();
                    break;
                case CreatureEventType.OnDeath:
                    _skill.OnCreatureDeath(Self);
                    _loot.OnCreatureDeath(Self);
                    
                    if (_state.CustomObjectData.ContainsKey(Self.GlobalID))
                    {
                        _state.CustomObjectData.Remove(Self.GlobalID);
                    }

                    behaviour?.OnDeath();
                    break;
                case CreatureEventType.OnDisturbed:
                    behaviour?.OnDisturbed();
                    break;
                case CreatureEventType.OnHeartbeat:
                    behaviour?.OnHeartbeat();
                    break;
                case CreatureEventType.OnPerception:
                    behaviour?.OnPerception();
                    break;
                case CreatureEventType.OnRested:
                    behaviour?.OnRested();
                    break;
                case CreatureEventType.OnCombatRoundEnd:
                    behaviour?.OnCombatRoundEnd();
                    break;
                case CreatureEventType.OnSpawn:
                    if (behaviour?.Behaviour != null)
                    {
                        var result = behaviour.Behaviour
                            .End()
                            .Build();
                        _behaviour.RegisterBehaviour(result, Self);
                    }
                    behaviour?.OnSpawn();
                    break;
                case CreatureEventType.OnSpellCastAt:
                    behaviour?.OnSpellCastAt();
                    break;
                case CreatureEventType.OnUserDefined:
                    behaviour?.OnUserDefined();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }
    }
}
