using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;

namespace SWLOR.Game.Server.Service
{
    public static class AIService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnCreaturePhysicalAttacked>(message => OnCreaturePhysicalAttacked());
            MessageHub.Instance.Subscribe<OnCreatureBlocked>(message => OnCreatureBlocked());
            MessageHub.Instance.Subscribe<OnCreatureConversation>(message => OnCreatureConversation());
            MessageHub.Instance.Subscribe<OnCreatureDamaged>(message => OnCreatureDamaged());
            MessageHub.Instance.Subscribe<OnCreatureDeath>(message => OnCreatureDeath());
            MessageHub.Instance.Subscribe<OnCreatureDisturbed>(message => OnCreatureDisturbed());
            MessageHub.Instance.Subscribe<OnCreatureHeartbeat>(message => OnCreatureHeartbeat());
            MessageHub.Instance.Subscribe<OnCreaturePerception>(message => OnCreaturePerception());
            MessageHub.Instance.Subscribe<OnCreatureRested>(message => OnCreatureRested());
            MessageHub.Instance.Subscribe<OnCreatureCombatRoundEnd>(message => OnCreatureCombatRoundEnd());
            MessageHub.Instance.Subscribe<OnCreatureSpawn>(message => OnCreatureSpawn());
            MessageHub.Instance.Subscribe<OnCreatureSpellCastAt>(message => OnCreatureSpellCastAt());
            MessageHub.Instance.Subscribe<OnCreatureUserDefined>(message => OnCreatureUserDefined());
        }


        private static void OnCreatureBlocked()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnBlocked();
            });
        }

        private static void OnCreatureConversation()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnConversation();
            });

        }

        private static void OnCreatureDamaged()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnDamaged();
            });

        }

        private static void OnCreatureDeath()
        {
            NWCreature self = Object.OBJECT_SELF;

            if (AppCache.CustomObjectData.ContainsKey(self.GlobalID))
            {
                AppCache.CustomObjectData.Remove(self.GlobalID);
            }

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnDeath();
            });
        }

        private static void OnCreatureDisturbed()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnDisturbed();
            });

        }


        private static void OnCreatureHeartbeat()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnHeartbeat();
            });
        }

        private static void OnCreaturePerception()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnPerception();
            });
        }

        private static void OnCreaturePhysicalAttacked()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnPhysicalAttacked();
            });

        }

        private static void OnCreatureRested()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnRested();
            });
        }

        private static void OnCreatureCombatRoundEnd()
        {
            NWCreature self = Object.OBJECT_SELF;

            WeatherService.OnCombatRoundEnd(self);

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnCombatRoundEnd();
            });

        }

        private static void OnCreatureSpawn()
        {
            NWCreature self = Object.OBJECT_SELF;
            
            // Don't modify AI behaviour for DM-spawned creatures.
            if (self.GetLocalInt("DM_SPAWNED") == _.TRUE) return;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

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
                    BehaviourService.RegisterBehaviour(result, self);
                }

                behaviour.OnSpawn();
            });
        }

        private static void OnCreatureSpellCastAt()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnSpellCastAt();
            });
        }

        private static void OnCreatureUserDefined()
        {
            NWCreature self = Object.OBJECT_SELF;

            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(creatureScript)) return;
            if (!App.IsKeyRegistered<IBehaviour>("AI." + creatureScript)) return;

            App.ResolveByInterface<IBehaviour>("AI." + creatureScript, behaviour =>
            {
                behaviour.OnUserDefined();
            });
        }
    }
}
