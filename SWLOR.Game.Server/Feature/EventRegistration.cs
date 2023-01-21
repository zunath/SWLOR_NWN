using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature
{
    public static class EventRegistration
    {
        /// <summary>
        /// Fires on the module PreLoad event. This event should be specified in the environment variables.
        /// This will hook all module/global events.
        /// </summary>
        [NWNEventHandler("mod_preload")]
        public static void OnModulePreload()
        {
            var serverConfig = DB.Get<ModuleCache>("SWLOR_CACHE") ?? new ModuleCache();

            Console.WriteLine("Hooking all module events.");
            HookModuleEvents();

            Console.WriteLine("Hooking all area events.");
            HookAreaEvents();

            Console.WriteLine("Hooking all NWNX events");
            HookNWNXEvents();

            Console.WriteLine("Hooking all application-specific events");
            HookApplicationEvents();

            // Module has changed since last run.
            // Run procedures dependent on the module file changing.
            if (UtilPlugin.GetModuleMTime() != serverConfig.LastModuleMTime)
            {
                Console.WriteLine("Module has changed since last boot. Running module changed event.");

                // DB record must be updated before the event fires, as some
                // events use the server configuration record.
                serverConfig.LastModuleMTime = UtilPlugin.GetModuleMTime();
                DB.Set(serverConfig);

                ExecuteScript("mod_content_chg", GetModule());
            }

            // Fire off the mod_cache event which is used for caching data, before mod_load runs.
            ExecuteScript("mod_cache", GetModule());
        }

        [NWNEventHandler("mod_heartbeat")]
        public static void ExecuteHeartbeatEvent()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                ExecuteScript("interval_pc_6s", player);
            }
        }

        /// <summary>
        /// When a player enters the server, hook their event scripts.
        /// Also add them to a UI processor list.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void EnterServer()
        {
            HookPlayerEvents();
        }

        private static void HookPlayerEvents()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) 
                return;

            SetEventScript(player, EventScript.Creature_OnHeartbeat, "pc_heartbeat");
            SetEventScript(player, EventScript.Creature_OnNotice, "pc_perception");
            SetEventScript(player, EventScript.Creature_OnSpellCastAt, "pc_spellcastat");
            SetEventScript(player, EventScript.Creature_OnMeleeAttacked, "pc_attacked");
            SetEventScript(player, EventScript.Creature_OnDamaged, "pc_damaged");
            SetEventScript(player, EventScript.Creature_OnDisturbed, "pc_disturb");
            SetEventScript(player, EventScript.Creature_OnEndCombatRound, "pc_roundend");
            SetEventScript(player, EventScript.Creature_OnSpawnIn, "pc_spawn");
            SetEventScript(player, EventScript.Creature_OnRested, "pc_rested");
            SetEventScript(player, EventScript.Creature_OnDeath, "pc_death");
            SetEventScript(player, EventScript.Creature_OnUserDefined, "pc_userdef");
            SetEventScript(player, EventScript.Creature_OnBlockedByDoor, "pc_blocked");
        }


        /// <summary>
        /// Hooks module-wide scripts.
        /// </summary>
        private static void HookModuleEvents()
        {
            var module = GetModule();

            SetEventScript(module, EventScript.Module_OnAcquireItem, "mod_acquire");
            SetEventScript(module, EventScript.Module_OnActivateItem, "mod_activate");
            SetEventScript(module, EventScript.Module_OnClientEnter, "mod_enter");
            SetEventScript(module, EventScript.Module_OnClientExit, "mod_exit");
            SetEventScript(module, EventScript.Module_OnPlayerCancelCutscene, "mod_abort_cs");
            SetEventScript(module, EventScript.Module_OnHeartbeat, "mod_heartbeat");
            SetEventScript(module, EventScript.Module_OnModuleLoad, "mod_load");
            SetEventScript(module, EventScript.Module_OnPlayerChat, "mod_chat");
            SetEventScript(module, EventScript.Module_OnPlayerDying, "mod_dying");
            SetEventScript(module, EventScript.Module_OnPlayerDeath, "mod_death");
            SetEventScript(module, EventScript.Module_OnEquipItem, "mod_equip");
            SetEventScript(module, EventScript.Module_OnPlayerLevelUp, "mod_level_up");
            SetEventScript(module, EventScript.Module_OnRespawnButtonPressed, "mod_respawn");
            SetEventScript(module, EventScript.Module_OnPlayerRest, "mod_rest");
            SetEventScript(module, EventScript.Module_OnUnequipItem, "mod_unequip");
            SetEventScript(module, EventScript.Module_OnLoseItem, "mod_unacquire");
            SetEventScript(module, EventScript.Module_OnUserDefined, "mod_user_def");
            SetEventScript(module, EventScript.Module_OnPlayerTarget, "mod_p_target");
            SetEventScript(module, EventScript.Module_OnPlayerGuiEvent, "mod_gui_event");
            SetEventScript(module, EventScript.Module_OnPlayerTileEvent, "mod_tile_event");
            SetEventScript(module, EventScript.Module_OnNuiEvent, "mod_nui_event");
        }

        /// <summary>
        /// Hooks area-wide scripts.
        /// </summary>
        private static void HookAreaEvents()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                SetEventScript(area, EventScript.Area_OnEnter, "area_enter");
                SetEventScript(area, EventScript.Area_OnExit, "area_exit");
                SetEventScript(area, EventScript.Area_OnHeartbeat, "area_heartbeat");
                SetEventScript(area, EventScript.Area_OnUserDefined, "area_user_def");
            }
        }

        /// <summary>
        /// Hooks NWNX scripts.
        /// </summary>
        private static void HookNWNXEvents()
        {
            // Chat Plugin Events start here.
            ChatPlugin.RegisterChatScript("on_nwnx_chat");

            // Damage Plugin Events start here.
            DamagePlugin.SetDamageEventScript("on_nwnx_dmg", OBJECT_INVALID);

            // Events Plugin Events start here.

            // Associate events
            EventsPlugin.SubscribeEvent("NWNX_ON_ADD_ASSOCIATE_BEFORE", "asso_add_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ADD_ASSOCIATE_AFTER", "asso_add_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_REMOVE_ASSOCIATE_BEFORE", "asso_rem_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_REMOVE_ASSOCIATE_AFTER", "asso_rem_aft");

            // Stealth events
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_ENTER_BEFORE", "stlent_add_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_ENTER_AFTER", "stlent_add_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_EXIT_BEFORE", "stlex_add_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_EXIT_AFTER", "stlex_add_aft");

            // Examine events
            EventsPlugin.SubscribeEvent("NWNX_ON_EXAMINE_OBJECT_BEFORE", "examine_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_EXAMINE_OBJECT_AFTER", "examine_aft");

            // Validate Use Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_USE_ITEM_BEFORE", "item_valid_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_USE_ITEM_AFTER", "item_valid_aft");

            // Use Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_ITEM_BEFORE", "item_use_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_ITEM_AFTER", "item_use_aft");

            // Item Container events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_OPEN_BEFORE", "inv_open_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_OPEN_AFTER", "inv_open_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_CLOSE_BEFORE", "inv_close_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_CLOSE_AFTER", "inv_close_aft");

            // Ammunition Reload events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_AMMO_RELOAD_BEFORE", "ammo_reload_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_AMMO_RELOAD_AFTER", "ammo_reload_aft");

            // Scroll Learn events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SCROLL_LEARN_BEFORE", "scroll_lrn_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SCROLL_LEARN_AFTER", "scroll_lrn_aft");

            // Validate Item Equip events
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_ITEM_EQUIP_BEFORE", "item_val_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_ITEM_EQUIP_AFTER", "item_val_aft");

            // Item Equip events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_EQUIP_BEFORE", "item_eqpval_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_EQUIP_AFTER", "item_eqpval_aft");

            // Item Unequip events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_UNEQUIP_BEFORE", "item_uneqp_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_UNEQUIP_AFTER", "item_uneqp_aft");

            // Item Destroy events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DESTROY_OBJECT_BEFORE", "item_dest_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DESTROY_OBJECT_AFTER", "item_dest_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DECREMENT_STACKSIZE_BEFORE", "item_dec_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DECREMENT_STACKSIZE_AFTER", "item_dec_aft");

            // Item Use Lore to Identify events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_USE_LORE_BEFORE", "lore_id_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_USE_LORE_AFTER", "lore_id_aft");

            // Item Pay to Identify events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_PAY_TO_IDENTIFY_BEFORE", "pay_id_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_PAY_TO_IDENTIFY_AFTER", "pay_id_aft");

            // Item Split events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SPLIT_BEFORE", "item_splt_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SPLIT_AFTER", "item_splt_aft");

            // Item Merge events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_MERGE_BEFORE", "item_merge_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_MERGE_AFTER", "item_merge_aft");

            // Acquire Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_ACQUIRE_BEFORE", "item_acquire_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_ACQUIRE_AFTER", "item_acquire_aft");

            // Feat Use events
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_FEAT_BEFORE", "feat_use_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_FEAT_AFTER", "feat_use_aft");

            // DM Give events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_GOLD_BEFORE", "dm_givegold_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_GOLD_AFTER", "dm_givegold_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_XP_BEFORE", "dm_givexp_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_XP_AFTER", "dm_givexp_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_LEVEL_BEFORE", "dm_givelvl_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_LEVEL_AFTER", "dm_givelvl_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ALIGNMENT_BEFORE", "dm_givealn_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ALIGNMENT_AFTER", "dm_givealn_aft");

            // DM Spawn events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_OBJECT_BEFORE", "dm_spwnobj_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_OBJECT_AFTER", "dm_spwnobj_aft");

            // DM Give Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ITEM_BEFORE", "dm_giveitem_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ITEM_AFTER", "dm_giveitem_aft");

            // DM Multiple Object Action events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_HEAL_BEFORE", "dm_heal_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_HEAL_AFTER", "dm_heal_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_KILL_BEFORE", "dm_kill_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_KILL_AFTER", "dm_kill_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_INVULNERABLE_BEFORE", "dm_invuln_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_INVULNERABLE_AFTER", "dm_invuln_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_FORCE_REST_BEFORE", "dm_forcerest_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_FORCE_REST_AFTER", "dm_forcerest_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_LIMBO_BEFORE", "dm_limbo_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_LIMBO_AFTER", "dm_limbo_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_AI_BEFORE", "dm_ai_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_AI_AFTER", "dm_ai_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_IMMORTAL_BEFORE", "dm_immortal_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_IMMORTAL_AFTER", "dm_immortal_aft");

            // DM Single Object Action events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GOTO_BEFORE", "dm_goto_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GOTO_AFTER", "dm_goto_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_BEFORE", "dm_poss_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_AFTER", "dm_poss_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_FULL_POWER_BEFORE", "dm_possfull_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_FULL_POWER_AFTER", "dm_possfull_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_LOCK_BEFORE", "dm_lock_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_LOCK_AFTER", "dm_lock_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISABLE_TRAP_BEFORE", "dm_distrap_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISABLE_TRAP_AFTER", "dm_distrap_aft");

            // DM Jump events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TO_POINT_BEFORE", "dm_jumppt_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TO_POINT_AFTER", "dm_jumppt_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TARGET_TO_POINT_BEFORE", "dm_jumptarg_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TARGET_TO_POINT_AFTER", "dm_jumptarg_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_BEFORE", "dm_jumpall_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_AFTER", "dm_jumpall_aft");

            // DM Change Difficulty events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_CHANGE_DIFFICULTY_BEFORE", "dm_chgdiff_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_CHANGE_DIFFICULTY_AFTER", "dm_chgdiff_aft");

            // DM View Inventory events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_VIEW_INVENTORY_BEFORE", "dm_vwinven_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_VIEW_INVENTORY_AFTER", "dm_vwinven_aft");

            // DM Spawn Trap events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_BEFORE", "dm_spwntrap_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_AFTER", "dm_spwntrap_aft");

            // DM Dump Locals events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DUMP_LOCALS_BEFORE", "dm_dumploc_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DUMP_LOCALS_AFTER", "dm_dumploc_aft");

            // DM Other events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_APPEAR_BEFORE", "dm_appear_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_APPEAR_AFTER", "dm_appear_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISAPPEAR_BEFORE", "dm_disappear_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISAPPEAR_AFTER", "dm_disappear_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_BEFORE", "dm_setfac_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_AFTER", "dm_setfac_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TAKE_ITEM_BEFORE", "dm_takeitem_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TAKE_ITEM_AFTER", "dm_takeitem_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_STAT_BEFORE", "dm_setstat_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_STAT_AFTER", "dm_setstat_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_VARIABLE_BEFORE", "dm_getvar_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_VARIABLE_AFTER", "dm_getvar_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_VARIABLE_BEFORE", "dm_setvar_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_VARIABLE_AFTER", "dm_setvar_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_TIME_BEFORE", "dm_settime_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_TIME_AFTER", "dm_settime_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_DATE_BEFORE", "dm_setdate_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_DATE_AFTER", "dm_setdate_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_REPUTATION_BEFORE", "dm_setrep_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_REPUTATION_AFTER", "dm_setrep_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_FACTION_REPUTATION_BEFORE", "dm_getrep_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_FACTION_REPUTATION_AFTER", "dm_getrep_aft");

            // Client Disconnect events
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_DISCONNECT_BEFORE", "client_disc_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_DISCONNECT_AFTER", "client_disc_aft");

            // Client Connect events
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_CONNECT_BEFORE", "client_conn_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_CONNECT_AFTER", "client_conn_aft");

            // Combat Round Start events
            EventsPlugin.SubscribeEvent("NWNX_ON_START_COMBAT_ROUND_BEFORE", "comb_round_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_START_COMBAT_ROUND_AFTER", "comb_round_aft");

            // Cast Spell events
            EventsPlugin.SubscribeEvent("NWNX_ON_CAST_SPELL_BEFORE", "cast_spell_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_CAST_SPELL_AFTER", "cast_spell_aft");

            // Set Memorized Spell Slot events
            EventsPlugin.SubscribeEvent("NWNX_SET_MEMORIZED_SPELL_SLOT_BEFORE", "set_spell_bef");
            EventsPlugin.SubscribeEvent("NWNX_SET_MEMORIZED_SPELL_SLOT_AFTER", "set_spell_aft");

            // Clear Memorized Spell Slot events
            EventsPlugin.SubscribeEvent("NWNX_CLEAR_MEMORIZED_SPELL_SLOT_BEFORE", "clr_spell_bef");
            EventsPlugin.SubscribeEvent("NWNX_CLEAR_MEMORIZED_SPELL_SLOT_AFTER", "clr_spell_aft");

            // Healer Kit Use events
            EventsPlugin.SubscribeEvent("NWNX_ON_HEALER_KIT_BEFORE", "heal_kit_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_HEALER_KIT_AFTER", "heal_kit_aft");

            // Healing events
            EventsPlugin.SubscribeEvent("NWNX_ON_HEAL_BEFORE", "heal_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_HEAL_AFTER", "heal_aft");

            // Party Action events
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_LEAVE_BEFORE", "pty_leave_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_LEAVE_AFTER", "pty_leave_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_BEFORE", "pty_kick_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_AFTER", "pty_kick_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_TRANSFER_LEADERSHIP_BEFORE", "pty_chgldr_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_TRANSFER_LEADERSHIP_AFTER", "pty_chgldr_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_INVITE_BEFORE", "pty_invite_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_INVITE_AFTER", "pty_invite_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_IGNORE_INVITATION_BEFORE", "pty_ignore_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_IGNORE_INVITATION_AFTER", "pty_ignore_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_ACCEPT_INVITATION_BEFORE", "pty_accept_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_ACCEPT_INVITATION_AFTER", "pty_accept_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_REJECT_INVITATION_BEFORE", "pty_reject_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_REJECT_INVITATION_AFTER", "pty_reject_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_HENCHMAN_BEFORE", "pty_kickhen_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_HENCHMAN_AFTER", "pty_kickhen_aft");

            // Combat Mode Toggle events
            EventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_MODE_ON", "combat_mode_on");
            EventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_MODE_OFF", "combat_mode_off");

            // Use Skill events
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_SKILL_BEFORE", "use_skill_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_SKILL_AFTER", "use_skill_aft");

            // Map Pin events
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_ADD_PIN_BEFORE", "mappin_add_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_ADD_PIN_AFTER", "mappin_add_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_CHANGE_PIN_BEFORE", "mappin_chg_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_CHANGE_PIN_AFTER", "mappin_chg_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_DESTROY_PIN_BEFORE", "mappin_rem_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_DESTROY_PIN_AFTER", "mappin_rem_aft");

            // Spot/Listen Detection events
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_LISTEN_DETECTION_BEFORE", "det_listen_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_LISTEN_DETECTION_AFTER", "det_listen_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_SPOT_DETECTION_BEFORE", "det_spot_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_SPOT_DETECTION_AFTER", "det_spot_aft");

            // Polymorph events
            EventsPlugin.SubscribeEvent("NWNX_ON_POLYMORPH_BEFORE", "polymorph_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_POLYMORPH_AFTER", "polymorph_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_UNPOLYMORPH_BEFORE", "unpolymorph_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_UNPOLYMORPH_AFTER", "unpolymorph_aft");

            // Effect Applied/Removed events
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_APPLIED_BEFORE", "effect_app_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_APPLIED_AFTER", "effect_app_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_REMOVED_BEFORE", "effect_rem_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_REMOVED_AFTER", "effect_rem_aft");

            // Quickchat events
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKCHAT_BEFORE", "quickchat_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKCHAT_AFTER", "quickchat_aft");

            // Inventory Open events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_OPEN_BEFORE", "inv_open_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_OPEN_AFTER", "inv_open_aft");

            // Inventory Select Panel events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_SELECT_PANEL_BEFORE", "inv_panel_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_SELECT_PANEL_AFTER", "inv_panel_aft");

            // Barter Start events
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_START_BEFORE", "bart_start_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_START_AFTER", "bart_start_aft");

            // Barter End events
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_END_BEFORE", "bart_end_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_END_AFTER", "bart_end_aft");

            // Trap events
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_DISARM_BEFORE", "trap_disarm_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_DISARM_AFTER", "trap_disarm_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_ENTER_BEFORE", "trap_enter_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_ENTER_AFTER", "trap_enter_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_EXAMINE_BEFORE", "trap_exam_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_EXAMINE_AFTER", "trap_exam_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_FLAG_BEFORE", "trap_flag_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_FLAG_AFTER", "trap_flag_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_RECOVER_BEFORE", "trap_rec_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_RECOVER_AFTER", "trap_rec_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_BEFORE", "trap_set_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_AFTER", "trap_set_aft");

            // Timing Bar events
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_START_BEFORE", "timing_start_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_START_AFTER", "timing_start_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_STOP_BEFORE", "timing_stop_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_STOP_AFTER", "timing_stop_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_CANCEL_BEFORE", "timing_canc_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_CANCEL_AFTER", "timing_canc_aft");

            // Webhook events
            EventsPlugin.SubscribeEvent("NWNX_ON_WEBHOOK_SUCCESS", "webhook_success");
            EventsPlugin.SubscribeEvent("NWNX_ON_WEBHOOK_FAILURE", "webhook_failure");

            // Servervault events
            EventsPlugin.SubscribeEvent("NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_BEFORE", "name_reserve_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_AFTER", "name_reserve_aft");

            // Levelling events
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_BEFORE", "lvl_up_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AFTER", "lvl_up_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AUTOMATIC_BEFORE", "lvl_upauto_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AUTOMATIC_AFTER", "lvl_upauto_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_DOWN_BEFORE", "lvl_down_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_DOWN_AFTER", "lvl_down_aft");

            // Container Change events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_ITEM_BEFORE", "inv_add_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_ITEM_AFTER", "inv_add_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_ITEM_BEFORE", "inv_rem_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_ITEM_AFTER", "inv_rem_aft");

            // Gold events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_GOLD_BEFORE", "add_gold_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_GOLD_AFTER", "add_gold_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_GOLD_BEFORE", "add_gold_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_GOLD_AFTER", "add_gold_aft");

            // PVP Attitude Change events
            EventsPlugin.SubscribeEvent("NWNX_ON_PVP_ATTITUDE_CHANGE_BEFORE", "pvp_chgatt_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_PVP_ATTITUDE_CHANGE_AFTER", "pvp_chgatt_aft");

            // Input Walk To events
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_WALK_TO_WAYPOINT_BEFORE", "input_walk_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_WALK_TO_WAYPOINT_AFTER", "input_walk_aft");

            // Material Change events
            EventsPlugin.SubscribeEvent("NWNX_ON_MATERIALCHANGE_BEFORE", "material_chg_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_MATERIALCHANGE_AFTER", "material_chg_aft");

            // Input Attack events
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_ATTACK_OBJECT_BEFORE", "input_atk_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_ATTACK_OBJECT_AFTER", "input_atk_aft");

            // Input Force Move To events
            // NOTE: These events are disabled because they cause NWServer to crash when a player clicks to move anywhere.
            //Events.SubscribeEvent("NWNX_ON_INPUT_FORCE_MOVE_TO_OBJECT_BEFORE", "force_move_bef");
            //Events.SubscribeEvent("NWNX_ON_INPUT_FORCE_MOVE_TO_OBJECT_AFTER", "force_move_aft");

            // Object Lock events
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_LOCK_BEFORE", "obj_lock_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_LOCK_AFTER", "obj_lock_aft");

            // Object Unlock events
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_UNLOCK_BEFORE", "obj_unlock_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_UNLOCK_AFTER", "obj_unlock_aft");

            // UUID Collision events
            EventsPlugin.SubscribeEvent("NWNX_ON_UUID_COLLISION_BEFORE", "uuid_coll_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_UUID_COLLISION_AFTER", "uuid_coll_aft");

            // Resource events
            // NOTE: These events are disabled because they cause NWServer to crash when CTRL+C is pressed on a Docker server.
            //Events.SubscribeEvent("NWNX_ON_RESOURCE_ADDED", "resource_added");
            //Events.SubscribeEvent("NWNX_ON_RESOURCE_REMOVED", "resource_removed");
            //Events.SubscribeEvent("NWNX_ON_RESOURCE_MODIFIED", "resource_modified");

            // ELC Events
            EventsPlugin.SubscribeEvent("NWNX_ON_ELC_VALIDATE_CHARACTER_BEFORE", "elc_validate_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_ELC_VALIDATE_CHARACTER_AFTER", "elc_validate_aft");

            // Quickbar Events
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKBAR_SET_BUTTON_BEFORE", "qb_set_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKBAR_SET_BUTTON_AFTER", "qb_set_aft");

            // Calendar Events
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_HOUR", "calendar_hour");
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DAY", "calendar_day");
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_MONTH", "calendar_month");
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_YEAR", "calendar_year");
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DAWN", "calendar_dawn");
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DUSK", "calendar_dusk");

            // Broadcast Spell Cast Events
            EventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_CAST_SPELL_BEFORE", "cast_spell_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_CAST_SPELL_AFTER", "cast_spell_aft");

            // RunScript Debug Events
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_BEFORE", "debug_script_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_AFTER", "debug_script_aft");

            // RunScriptChunk Debug Events
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_CHUNK_BEFORE", "debug_chunk_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_CHUNK_AFTER", "debug_chunk_aft");

            // Buy/Sell Store Events
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_BUY_BEFORE", "store_buy_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_BUY_AFTER", "store_buy_aft");
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_SELL_BEFORE", "store_sell_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_SELL_AFTER", "store_sell_aft");

            // Input Drop Item Events
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_DROP_ITEM_BEFORE", "item_drop_bef");
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_DROP_ITEM_AFTER", "item_drop_aft");
        }

        /// <summary>
        /// Hooks all application-specific scripts.
        /// </summary>
        private static void HookApplicationEvents()
        {
            // Application Shutdown events
            EventsPlugin.SubscribeEvent("APPLICATION_SHUTDOWN", "app_shutdown");
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                EventsPlugin.SignalEvent("APPLICATION_SHUTDOWN", GetModule());
            };

            EventsPlugin.SubscribeEvent("SWLOR_ITEM_EQUIP_VALID_BEFORE", "item_eqp_bef");
            EventsPlugin.SubscribeEvent("SWLOR_BUY_PERK", "swlor_buy_perk");
            EventsPlugin.SubscribeEvent("SWLOR_GAIN_SKILL_POINT", "swlor_gain_skill");
            EventsPlugin.SubscribeEvent("SWLOR_COMPLETE_QUEST", "swlor_comp_qst");
            EventsPlugin.SubscribeEvent("SWLOR_CACHE_SKILLS_LOADED", "swlor_skl_cache");
            EventsPlugin.SubscribeEvent("SWLOR_COMBAT_POINT_DISTRIBUTED", "cp_xp_distribute");
        }

        /// <summary>
        /// A handful of NWNX functions require special calls to load persistence.
        /// When the module loads, run those methods here.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void TriggerNWNXPersistence()
        {
            var firstObject = GetFirstObjectInArea(GetFirstArea());
            CreaturePlugin.SetCriticalRangeModifier(firstObject, 0, 0, true);
        }

        private static readonly Dictionary<int, List<uint>> _intervalPlayers = new();

        /// <summary>
        /// Schedules five player processors which fire off at 0.2 second intervals.
        /// This is done to stagger out the processing overhead of scripts that run on player one-second events.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void ScheduleProcessors()
        {
            const int GroupCount = 5;

            for (var x = 1; x <= GroupCount; x++)
            {
                var interval = x == 1 ? 0f : 0.2f * (x - 1);
                var groupId = x;
                _intervalPlayers[x] = new List<uint>();
                Scheduler.ScheduleRepeating(() => ProcessIntervalGroup(groupId), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(interval));
            }
        }

        /// <summary>
        /// When a player joins the server they are added to the processor queue with the
        /// fewest number of active players.
        /// DMs are excluded from this.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void ScheduleProcessor()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            AddPlayerToIntervalGroup(player);
        }

        private static void AddPlayerToIntervalGroup(uint player)
        {
            var groupId = 1;
            var lowestCount = 999;
            foreach (var (group, players) in _intervalPlayers)
            {
                if (players.Count < lowestCount)
                {
                    lowestCount = players.Count;
                    groupId = group;
                }
            }

            _intervalPlayers[groupId].Add(player);
        }

        private static void ProcessIntervalGroup(int intervalGroup)
        {
            var players = _intervalPlayers[intervalGroup];

            for (var index = players.Count - 1; index >= 0; index--)
            {
                var player = players[index];

                if (GetIsObjectValid(player))
                {
                    // It's imperative a script doesn't cause this processor to exit upon error.
                    try
                    {
                        ExecuteScript("interval_pc_1s", player);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(LogGroup.Error, ex.ToMessageAndCompleteStacktrace());
                    }
                }
                else
                {
                    _intervalPlayers[intervalGroup].Remove(player);
                }
            }
        }
    }
}
