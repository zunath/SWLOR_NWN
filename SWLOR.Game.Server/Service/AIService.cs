using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Messaging.Messages;
using SWLOR.Game.Server.NWN.Events.Creature;
using SWLOR.Game.Server.NWN.Events.Module;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public static class AIService
    {
        private static readonly Dictionary<string, IAIBehaviour> _aiBehaviours;
        private static readonly HashSet<NWCreature> _aiCreatures;

        static AIService()
        {
            _aiBehaviours = new Dictionary<string, IAIBehaviour>();
            _aiCreatures = new HashSet<NWCreature>();
        }
        
        public static void SubscribeEvents()
        {
            // Module Events
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());

            // SWLOR Events
            MessageHub.Instance.Subscribe<ObjectProcessorMessage>(message => ProcessCreatureAI());

            // Creature Events
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

        private static void OnModuleLoad()
        {
            RegisterAIBehaviours();
        }

        private static void RegisterAIBehaviours()
        {
            // Use reflection to get all of AI behaviour implementations.
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IAIBehaviour).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                IAIBehaviour instance = Activator.CreateInstance(type) as IAIBehaviour;
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _aiBehaviours.Add(type.Name, instance);
            }
        }

        private static IAIBehaviour GetAIBehaviour(string key)
        {
            if (!_aiBehaviours.ContainsKey(key))
            {
                throw new KeyNotFoundException("AI behaviour '" + key + "' is not registered. Did you create a class for it?");
            }

            return _aiBehaviours[key];
        }

        private static string GetBehaviourScript(NWCreature self)
        {
            string creatureScript = self.GetLocalString("BEHAVIOUR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("BEHAVIOR");
            if (string.IsNullOrWhiteSpace(creatureScript)) creatureScript = self.GetLocalString("SCRIPT");

            // Fall back to standard behaviour if a script can't be found.
            if (string.IsNullOrWhiteSpace(creatureScript) ||
                !_aiBehaviours.ContainsKey(creatureScript)) return "StandardBehaviour";

            return creatureScript;
        }

        private static void OnCreatureBlocked()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnBlocked(Object.OBJECT_SELF);
        }

        private static void OnCreatureConversation()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnConversation(Object.OBJECT_SELF);
        }

        private static void OnCreatureDamaged()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnDamaged(Object.OBJECT_SELF);
        }

        private static void OnCreatureDeath()
        {
            NWCreature self = Object.OBJECT_SELF;

            // Remove any custom object data from the cache.
            if (AppCache.CustomObjectData.ContainsKey(self.GlobalID))
            {
                AppCache.CustomObjectData.Remove(self.GlobalID);
            }

            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnDeath(Object.OBJECT_SELF);
        }

        private static void OnCreatureDisturbed()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnDisturbed(Object.OBJECT_SELF);
        }


        private static void OnCreatureHeartbeat()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnHeartbeat(Object.OBJECT_SELF);
        }

        private static void OnCreaturePerception()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnPerception(Object.OBJECT_SELF);
        }

        private static void OnCreaturePhysicalAttacked()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnPhysicalAttacked(Object.OBJECT_SELF);
        }

        private static void OnCreatureRested()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnRested(Object.OBJECT_SELF);
        }

        private static void OnCreatureCombatRoundEnd()
        {
            NWCreature self = Object.OBJECT_SELF;
            WeatherService.OnCombatRoundEnd(self);

            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnCombatRoundEnd(Object.OBJECT_SELF);
        }

        private static void OnCreatureSpawn()
        {
            NWCreature self = Object.OBJECT_SELF;
            
            // Don't modify AI behaviour for DM-spawned creatures.
            if (self.GetLocalInt("DM_SPAWNED") == _.TRUE) return;

            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour ai = GetAIBehaviour(script);
        
            if (ai.IgnoreNWNEvents) self.SetLocalInt("IGNORE_NWN_EVENTS", 1);
            if (ai.IgnoreOnBlocked) self.SetLocalInt("IGNORE_NWN_ON_BLOCKED_EVENT", 1);
            if (ai.IgnoreOnCombatRoundEnd) self.SetLocalInt("IGNORE_NWN_ON_COMBAT_ROUND_END_EVENT", 1);
            if (ai.IgnoreOnConversation) self.SetLocalInt("IGNORE_NWN_ON_CONVERSATION_EVENT", 1);
            if (ai.IgnoreOnDamaged) self.SetLocalInt("IGNORE_NWN_ON_DAMAGED_EVENT", 1);
            if (ai.IgnoreOnDeath) self.SetLocalInt("IGNORE_NWN_ON_DEATH_EVENT", 1);
            if (ai.IgnoreOnDisturbed) self.SetLocalInt("IGNORE_NWN_ON_DISTURBED_EVENT", 1);
            if (ai.IgnoreOnHeartbeat) self.SetLocalInt("IGNORE_NWN_ON_HEARTBEAT_EVENT", 1);
            if (ai.IgnoreOnPerception) self.SetLocalInt("IGNORE_NWN_ON_PERCEPTION_EVENT", 1);
            if (ai.IgnoreOnPhysicalAttacked) self.SetLocalInt("IGNORE_NWN_ON_PHYSICAL_ATTACKED_EVENT", 1);
            if (ai.IgnoreOnRested) self.SetLocalInt("IGNORE_NWN_ON_RESTED_EVENT", 1);
            if (ai.IgnoreOnSpawn) self.SetLocalInt("IGNORE_NWN_ON_SPAWN_EVENT", 1);
            if (ai.IgnoreOnSpellCastAt) self.SetLocalInt("IGNORE_NWN_ON_SPELL_CAST_AT_EVENT", 1);
            if (ai.IgnoreOnUserDefined) self.SetLocalInt("IGNORE_NWN_ON_USER_DEFINED_EVENT", 1);
            
            _aiCreatures.Add(self);
            ai.OnSpawn(self);
        }
        
        private static void OnCreatureSpellCastAt()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnSpellCastAt(Object.OBJECT_SELF);
        }

        private static void OnCreatureUserDefined()
        {
            string script = GetBehaviourScript(Object.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnUserDefined(Object.OBJECT_SELF);
        }

        private static void ProcessCreatureAI()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // Iterate backwards so we can remove the creature if it's no longer valid.
            for (int x = _aiCreatures.Count-1; x >= 0; x--)
            {
                NWCreature creature = _aiCreatures.ElementAt(x);
                NWArea area = creature.Area;
                bool areaHasPCs = NWModule.Get().Players.Count(p => p.Area.Resref == area.Resref) > 0;

                // Is this creature invalid or dead? If so, remove it and move to the next one.
                if (!creature.IsValid ||
                    creature.IsDead)
                {
                    _aiCreatures.Remove(creature);
                    continue;
                }

                // Are there no players in the area? Is the creature being possessed? If so, don't execute AI this frame. Move to the next one.
                if (creature.IsPossessedFamiliar || creature.IsDMPossessed || !areaHasPCs)
                    continue;

                string script = GetBehaviourScript(creature);
                if (string.IsNullOrWhiteSpace(script)) continue;
                IAIBehaviour behaviour = GetAIBehaviour(script);
                behaviour.OnProcessObject(creature);
            }

            sw.Stop();
            var ts = sw.Elapsed;
            Console.WriteLine(TimeService.GetTimeLongIntervals(ts.Days, ts.Hours, ts.Minutes, ts.Seconds, true, ts.Milliseconds));
        }
    }
}
