using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleEnter : IRegisteredEvent
    {
        
        private readonly IPlayerService _player;
        
        private readonly IQuestService _quest;
        private readonly IMapPinService _mapPin;
        private readonly IObjectVisibilityService _objectVisibility;
        
        private readonly IChatTextService _chatText;
        private readonly IPlayerValidationService _playerValidation;
        
        private readonly IRaceService _race;
        private readonly IPlayerMigrationService _migration;
        private readonly IMarketService _market;
        

        public OnModuleEnter(
            
            IPlayerService player,
            
            IQuestService quest,
            IMapPinService mapPin,
            IObjectVisibilityService objectVisibility,
            
            IChatTextService chatText,
            IPlayerValidationService playerValidation,
            
            IRaceService race,
            IPlayerMigrationService migration,
            IMarketService market
            )
        {
            
            _player = player;
            
            _quest = quest;
            _mapPin = mapPin;
            _objectVisibility = objectVisibility;
            
            _chatText = chatText;
            _playerValidation = playerValidation;
            
            _race = race;
            _migration = migration;
            _market = market;
            
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = GetEnteringPlayer();

            if (player.IsDM)
            {
                AppCache.ConnectedDMs.Add(player);
            }

            player.DeleteLocalInt("IS_CUSTOMIZING_ITEM");
            _.ExecuteScript("dmfi_onclienter ", Object.OBJECT_SELF); // DMFI also calls "x3_mod_def_enter"
            _playerValidation.OnModuleEnter();
            _player.InitializePlayer(player);
            DataService.CachePlayerData(player);
            SkillService.OnModuleEnter();
            PerkService.OnModuleEnter();
            _player.LoadCharacter(player);
            _migration.OnModuleEnter();
            _player.ShowMOTD(player);
            ApplyGhostwalk();
            _quest.OnClientEnter();
            ActivityLoggingService.OnModuleClientEnter();
            ApplyScriptEvents(player);
            _mapPin.OnModuleClientEnter();
            _objectVisibility.OnClientEnter();
            CustomEffectService.OnModuleEnter();
            _chatText.OnModuleEnter();
            _race.OnModuleEnter();
            _market.OnModuleEnter();
            
            player.SetLocalInt("LOGGED_IN_ONCE", TRUE);
            return true;
        }

        private NWPlayer GetEnteringPlayer()
        {
            return (_.GetEnteringObject());
        }

        private void ApplyGhostwalk()
        {
            NWPlayer oPC = GetEnteringPlayer();

            if (!oPC.IsPlayer) return;

            Effect eGhostWalk = _.EffectCutsceneGhost();
            eGhostWalk = _.TagEffect(eGhostWalk, "GHOST_WALK");
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, eGhostWalk, oPC.Object);

        }
        
        private void ApplyScriptEvents(NWObject oPC)
        {
            if (!oPC.IsPlayer) return;

            // As of 2018-03-28 only the OnDialogue, OnHeartbeat, and OnUserDefined events fire for PCs.
            // The rest are included here for completeness sake.

            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR, "pc_on_blocked");
            _.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DAMAGED, "pc_on_damaged");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DEATH, "pc_on_death");
            _.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DIALOGUE, "default");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DISTURBED, "pc_on_disturb");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND, "pc_on_endround");
            _.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_HEARTBEAT, "pc_on_heartbeat");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED, "pc_on_attacked");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_NOTICE, "pc_on_notice");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_RESTED, "pc_on_rested");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_SPAWN_IN, "pc_on_spawn");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT, "pc_on_spellcast");
            _.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_USER_DEFINED_EVENT, "pc_on_user");
        }
    }
}
