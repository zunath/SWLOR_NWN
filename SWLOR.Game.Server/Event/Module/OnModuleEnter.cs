using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;

using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleEnter : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = GetEnteringPlayer();

            if (player.IsDM)
            {
                AppCache.ConnectedDMs.Add(player);
            }

            player.DeleteLocalInt("IS_CUSTOMIZING_ITEM");
            _.ExecuteScript("dmfi_onclienter ", Object.OBJECT_SELF); // DMFI also calls "x3_mod_def_enter"
            PlayerValidationService.OnModuleEnter();
            PlayerService.InitializePlayer(player);
            DataService.CachePlayerData(player);
            SkillService.OnModuleEnter();
            PerkService.OnModuleEnter();
            PlayerService.LoadCharacter(player);
            PlayerMigrationService.OnModuleEnter();
            PlayerService.ShowMOTD(player);
            ApplyGhostwalk();
            QuestService.OnClientEnter();
            ActivityLoggingService.OnModuleClientEnter();
            ApplyScriptEvents(player);
            MapPinService.OnModuleClientEnter();
            ObjectVisibilityService.OnClientEnter();
            CustomEffectService.OnModuleEnter();
            ChatTextService.OnModuleEnter();
            RaceService.OnModuleEnter();
            MarketService.OnModuleEnter();
            
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
