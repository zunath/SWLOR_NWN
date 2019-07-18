using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Event.Creature;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public static class AIService
    {
        private static readonly Dictionary<string, IAIBehaviour> _aiBehaviours;
        private static readonly Dictionary<NWArea, HashSet<NWCreature>> _areaAICreatures;

        static AIService()
        {
            _aiBehaviours = new Dictionary<string, IAIBehaviour>();
            _areaAICreatures = new Dictionary<NWArea, HashSet<NWCreature>>();
        }

        public static void SubscribeEvents()
        {
            // Module Events
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());

            // SWLOR Events
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => ProcessAreaAI());
            MessageHub.Instance.Subscribe<OnAreaInstanceCreated>(message => OnAreaInstanceCreated(message.Instance));
            MessageHub.Instance.Subscribe<OnAreaInstanceDestroyed>(message => OnAreaInstanceDestroyed(message.Instance));

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
            RegisterAreaAICreatures();
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

        private static void RegisterAreaAICreatures()
        {
            foreach (var area in NWModule.Get().Areas)
            {
                _areaAICreatures.Add(area, new HashSet<NWCreature>());
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
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnBlocked(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureConversation()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnConversation(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureDamaged()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnDamaged(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureDeath()
        {
            NWCreature self = NWGameObject.OBJECT_SELF;

            // Remove any custom object data from the cache.
            if (AppCache.CustomObjectData.ContainsKey(self.GlobalID))
            {
                AppCache.CustomObjectData.Remove(self.GlobalID);
            }

            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnDeath(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureDisturbed()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnDisturbed(NWGameObject.OBJECT_SELF);
        }


        private static void OnCreatureHeartbeat()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnHeartbeat(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreaturePerception()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnPerception(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreaturePhysicalAttacked()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnPhysicalAttacked(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureRested()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnRested(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureCombatRoundEnd()
        {
            NWCreature self = NWGameObject.OBJECT_SELF;
            WeatherService.OnCombatRoundEnd(self);

            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnCombatRoundEnd(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureSpawn()
        {
            NWCreature self = NWGameObject.OBJECT_SELF;

            // Don't modify AI behaviour for DM-spawned creatures.
            if (self.GetLocalInt("DM_SPAWNED") == _.TRUE) return;

            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
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

            _areaAICreatures[self.Area].Add(self);
            ai.OnSpawn(self);
        }

        private static void OnCreatureSpellCastAt()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnSpellCastAt(NWGameObject.OBJECT_SELF);
        }

        private static void OnCreatureUserDefined()
        {
            string script = GetBehaviourScript(NWGameObject.OBJECT_SELF);
            if (string.IsNullOrWhiteSpace(script)) return;
            IAIBehaviour behaviour = GetAIBehaviour(script);
            behaviour.OnUserDefined(NWGameObject.OBJECT_SELF);
        }

        private static void ProcessAreaAI()
        {
            using (new Profiler(nameof(AIService) + "." + nameof(ProcessAreaAI)))
            {
                foreach (var area in NWModule.Get().Areas)
                {
                    int lastTickPlayerCount = area.GetLocalInt("AI_PLAYER_COUNT");
                    int thisTickPlayerCount = NWNXArea.GetNumberOfPlayersInArea(area);
                    area.SetLocalInt("AI_PLAYER_COUNT", thisTickPlayerCount);

                    // AI gets processed one more time after an area becomes empty.
                    // We do this so that behaviours can clean up properly.
                    if (thisTickPlayerCount <= 0 && lastTickPlayerCount <= 0) continue;

                    // Safety check - If the area isn't in the cache, report it.
                    if (!_areaAICreatures.ContainsKey(area))
                    {
                        Console.WriteLine("Area " + area.Name + " not registered with AI service. Tag: " + area.Tag + ", Resref = " + area.Resref);
                        continue;
                    }

                    var creatures = _areaAICreatures[area];
                    ProcessCreatureAI(area, ref creatures);
                }
            }
        }

        private static void ProcessCreatureAI(NWArea area, ref HashSet<NWCreature> creatures)
        {
            // Iterate backwards so we can remove the creature if it's no longer valid.
            for (int x = creatures.Count - 1; x >= 0; x--)
            {
                NWCreature creature = creatures.ElementAt(x);

                // Limbo check.
                if (!area.IsValid) continue;

                // Is this creature invalid or dead? If so, remove it and move to the next one.
                if (!creature.IsValid ||
                    creature.IsDead)
                {
                    creatures.Remove(creature);
                    continue;
                }

                // Are there no players in the area? Is the creature being possessed? If so, don't execute AI this tick. Move to the next one.
                if (creature.IsPossessedFamiliar || creature.IsDMPossessed)
                    continue;

                string script = GetBehaviourScript(creature);
                if (string.IsNullOrWhiteSpace(script)) continue;
                IAIBehaviour behaviour = GetAIBehaviour(script);
                behaviour.OnProcessObject(creature);
            }
        }

        private static void OnAreaInstanceCreated(NWArea instance)
        {
            if (_areaAICreatures.ContainsKey(instance)) return;

            _areaAICreatures.Add(instance, new HashSet<NWCreature>());
        }

        private static void OnAreaInstanceDestroyed(NWArea instance)
        {
            if (!_areaAICreatures.ContainsKey(instance)) return;

            _areaAICreatures.Remove(instance);
        }
    }
}
