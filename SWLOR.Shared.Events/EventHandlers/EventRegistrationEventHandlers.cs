using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Events.Events.Server;

namespace SWLOR.Shared.Events.EventHandlers
{
    internal class EventRegistrationEventHandlers
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ICreaturePluginService _creaturePlugin;

        public EventRegistrationEventHandlers(
            IEventAggregator eventAggregator,
            ICreaturePluginService creaturePlugin)
        {
            _eventAggregator = eventAggregator;
            _creaturePlugin = creaturePlugin;
        }

        [ScriptHandler<OnServerHeartbeat>]
        public void ExecuteHeartbeatEvent()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                _eventAggregator.Publish(new OnPlayerHeartbeat(), player);
            }
        }

        /// <summary>
        /// When a player enters the server, hook their event scripts.
        /// Also add them to a UI processor list.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void EnterServer()
        {
            HookPlayerEvents();
        }

        private void HookPlayerEvents()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) 
                return;

            SetEventScript(player, EventScriptType.Creature_OnHeartbeat, ScriptName.OnPlayerHeartbeat);
            SetEventScript(player, EventScriptType.Creature_OnNotice, ScriptName.OnPlayerPerception);
            SetEventScript(player, EventScriptType.Creature_OnSpellCastAt, ScriptName.OnPlayerSpellCastAt);
            SetEventScript(player, EventScriptType.Creature_OnMeleeAttacked, ScriptName.OnPlayerAttacked);
            SetEventScript(player, EventScriptType.Creature_OnDamaged, ScriptName.OnPlayerDamaged);
            SetEventScript(player, EventScriptType.Creature_OnDisturbed, ScriptName.OnPlayerDisturbed);
            SetEventScript(player, EventScriptType.Creature_OnEndCombatRound, ScriptName.OnPlayerRoundEnd);
            SetEventScript(player, EventScriptType.Creature_OnSpawnIn, ScriptName.OnPlayerSpawn);
            SetEventScript(player, EventScriptType.Creature_OnRested, ScriptName.OnPlayerRested);
            SetEventScript(player, EventScriptType.Creature_OnDeath, ScriptName.OnPlayerDeath);
            SetEventScript(player, EventScriptType.Creature_OnUserDefined, ScriptName.OnPlayerUserDefined);
            SetEventScript(player, EventScriptType.Creature_OnBlockedByDoor, ScriptName.OnPlayerBlocked);
        }

        /// <summary>
        /// A handful of NWNX functions require special calls to load persistence.
        /// When the module loads, run those methods here.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void TriggerNWNXPersistence()
        {
            var firstObject = GetFirstObjectInArea(GetFirstArea());
            _creaturePlugin.SetCriticalRangeModifier(firstObject, 0, 0, true);
        }
    }
}
