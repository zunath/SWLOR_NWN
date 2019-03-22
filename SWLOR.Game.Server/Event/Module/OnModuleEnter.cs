using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleEnter : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IPlayerService _player;
        private readonly ISkillService _skill;
        private readonly IQuestService _quest;
        private readonly IActivityLoggingService _activityLogging;
        private readonly IMapPinService _mapPin;
        private readonly IObjectVisibilityService _objectVisibility;
        private readonly ICustomEffectService _customEffect;
        private readonly IChatTextService _chatText;
        private readonly IPlayerValidationService _playerValidation;
        private readonly IDataService _data;
        private readonly IRaceService _race;
        private readonly IPlayerMigrationService _migration;
        private readonly IMarketService _market;
        private readonly IPerkService _perk;

        public OnModuleEnter(
            INWScript script,
            IPlayerService player,
            ISkillService skill,
            IQuestService quest,
            IActivityLoggingService activityLogging,
            IMapPinService mapPin,
            IObjectVisibilityService objectVisibility,
            ICustomEffectService customEffect,
            IChatTextService chatText,
            IPlayerValidationService playerValidation,
            IDataService data,
            IRaceService race,
            IPlayerMigrationService migration,
            IMarketService market,
            IPerkService perk)
        {
            _ = script;
            _player = player;
            _skill = skill;
            _quest = quest;
            _activityLogging = activityLogging;
            _mapPin = mapPin;
            _objectVisibility = objectVisibility;
            _customEffect = customEffect;
            _chatText = chatText;
            _playerValidation = playerValidation;
            _data = data;
            _race = race;
            _migration = migration;
            _market = market;
            _perk = perk;
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
            _data.CachePlayerData(player);
            _skill.OnModuleEnter();
            _perk.OnModuleEnter();
            _player.LoadCharacter(player);
            _migration.OnModuleEnter();
            _player.ShowMOTD(player);
            ApplyGhostwalk();
            _quest.OnClientEnter();
            _activityLogging.OnModuleClientEnter();
            ApplyScriptEvents(player);
            _mapPin.OnModuleClientEnter();
            _objectVisibility.OnClientEnter();
            _customEffect.OnModuleEnter();
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
