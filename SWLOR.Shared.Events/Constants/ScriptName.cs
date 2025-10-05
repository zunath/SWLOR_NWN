namespace SWLOR.Shared.Events.Constants
{
    /// <summary>
    /// Constants for NWN script names used in NWNEventHandler attributes.
    /// This centralizes all script names to prevent typos and make refactoring easier.
    /// All values must be 16 characters or less due to NWN limitations.
    /// </summary>
    public static class ScriptName
    {
        // =============================================================================
        // EVENTS WITH CORRESPONDING EVENT CLASSES (Using new generic mechanism)
        // =============================================================================

        // Module events
        public const string OnModuleCacheBefore = "mod_cache_bef";
        public const string OnModuleCacheAfter = "mod_cache_aft";
        public const string OnModuleLoad = "mod_load";
        public const string OnModuleEnter = "mod_enter";
        public const string OnModuleExit = "mod_exit";
        public const string OnModuleDeath = "mod_death";
        public const string OnModuleDying = "mod_dying";
        public const string OnModuleRespawn = "mod_respawn";
        public const string OnModuleAcquire = "mod_acquire";
        public const string OnModuleUnacquire = "mod_unacquire";
        public const string OnModuleContentChange = "mod_content_chg";
        public const string OnModulePreload = "mod_preload";
        public const string OnModuleGuiEvent = "mod_gui_event";
        public const string OnModuleChat = "mod_chat";
        public const string OnModuleNuiEvent = "mod_nui_event";
        public const string OnModuleEquip = "mod_equip";
        public const string OnModuleUnequip = "mod_unequip";
        public const string OnModuleRest = "mod_rest";
        public const string OnModulePlayerTarget = "mod_p_target";
        public const string OnModuleActivate = "mod_activate";
        public const string OnModulePlayerCancelCutscene = "mod_abort_cs";
        public const string OnModuleHeartbeat = "mod_heartbeat";
        public const string OnModuleLevelUp = "mod_level_up";
        public const string OnModuleUserDefined = "mod_user_def";
        public const string OnModuleTileEvent = "mod_tile_event";

        // Eventing system module events
        internal const string OnEventingModuleLoad = "ev_mod_load";
        internal const string OnEventingModuleEnter = "ev_mod_enter";
        internal const string OnEventingModuleExit = "ev_mod_exit";
        internal const string OnEventingModuleDeath = "ev_mod_death";
        internal const string OnEventingModuleDying = "ev_mod_dying";
        internal const string OnEventingModuleRespawn = "ev_mod_respawn";
        internal const string OnEventingModuleAcquire = "ev_mod_acquire";
        internal const string OnEventingModuleUnacquire = "ev_mod_unacquire";
        internal const string OnEventingModulePreload = "ev_mod_preload";
        internal const string OnEventingModuleGuiEvent = "ev_mod_gui_event";
        internal const string OnEventingModuleChat = "ev_mod_chat";
        internal const string OnEventingModuleNuiEvent = "ev_mod_nui_event";
        internal const string OnEventingModuleEquip = "ev_mod_equip";
        internal const string OnEventingModuleUnequip = "ev_mod_unequip";
        internal const string OnEventingModuleRest = "ev_mod_rest";
        internal const string OnEventingModulePlayerTarget = "ev_mod_p_target";
        internal const string OnEventingModuleActivate = "ev_mod_activate";
        internal const string OnEventingModulePlayerCancelCutscene = "ev_mod_abort_cs";
        internal const string OnEventingModuleHeartbeat = "ev_mod_heartbeat";
        internal const string OnEventingModuleLevelUp = "ev_mod_level_up";
        internal const string OnEventingModuleUserDefined = "ev_mod_user_def";
        internal const string OnEventingModuleTileEvent = "ev_mod_tile_evt";

        // Infrastructure events
        public const string OnServerLoaded = "server_loaded";
        public const string OnHookEvents = "events_hooked";
        public const string OnHookNativeOverrides = "native_hooked";
        public const string OnServerHeartbeat = "swlor_heartbeat";


        // Character events
        public const string OnPlayerInitialized = "char_init_after";
        public const string OnCharacterRebuild = "char_rebuild";
        public const string OnExitRebuild = "exit_rebuild";
        public const string OnExitSpending = "exit_spending";
        public const string OnBuyStatRebuild = "buy_stat_rebuild";

        // Player events
        public const string OnPlayerDamaged = "pc_damaged";
        public const string OnPlayerHeartbeat = "pc_heartbeat";
        public const string OnPlayerPerception = "pc_perception";
        public const string OnPlayerSpellCastAt = "pc_spellcastat";
        public const string OnPlayerAttacked = "pc_attacked";
        public const string OnPlayerDisturbed = "pc_disturb";
        public const string OnPlayerRoundEnd = "pc_roundend";
        public const string OnPlayerSpawn = "pc_spawn";
        public const string OnPlayerRested = "pc_rested";
        public const string OnPlayerDeath = "pc_death";
        public const string OnPlayerUserDefined = "pc_userdef";
        public const string OnPlayerBlocked = "pc_blocked";
        public const string OnPlayerFPAdjusted = "pc_fp_adjusted";
        public const string OnPlayerStaminaAdjusted = "pc_stm_adjusted";
        public const string OnPlayerShieldAdjusted = "pc_shld_adjusted";
        public const string OnPlayerHullAdjusted = "pc_hull_adjusted";
        public const string OnPlayerCapAdjusted = "pc_cap_adjusted";
        public const string OnPlayerTargetUpdated = "pc_target_upd";
        public const string OnPlayerCacheData = "pc_cache_data";

        // Area events
        public const string OnAreaEnter = "area_enter";
        public const string OnAreaExit = "area_exit";
        public const string OnAreaHeartbeat = "area_heartbeat";
        public const string OnAreaUserDefined = "area_user_def";

        // Creature events
        public const string OnCreatureHeartbeatAfter = "crea_hb_aft";
        public const string OnCreaturePerceptionAfter = "crea_perc_aft";
        public const string OnCreatureRoundEndAfter = "crea_rndend_aft";
        public const string OnCreatureConversationAfter = "crea_convo_aft";
        public const string OnCreatureAttackAfter = "crea_attack_aft";
        public const string OnCreatureDamagedAfter = "crea_damaged_aft";
        public const string OnCreatureDeathAfter = "crea_death_aft";
        public const string OnCreatureDisturbedAfter = "crea_disturb_aft";
        public const string OnCreatureSpawnAfter = "crea_spawn_aft";
        public const string OnCreatureRestedAfter = "crea_rested_aft";
        public const string OnCreatureSpellCastAfter = "crea_splcast_aft";
        public const string OnCreatureUserDefinedAfter = "crea_userdef_aft";
        public const string OnCreatureBlockedAfter = "crea_block_aft";
        public const string OnCreatureAggroEnter = "crea_aggro_enter";
        public const string OnCreatureAggroExit = "crea_aggro_exit";
        public const string OnCreatureDamagedBefore = "crea_damaged_bef";
        public const string OnCreatureAttackBefore = "crea_attack_bef";
        public const string OnCreatureSpawnBefore = "crea_spawn_bef";
        public const string OnCreatureDeathBefore = "crea_death_bef";

        // NWNX Events - Associate events
        public const string OnAssociateAddBefore = "asso_add_bef";
        public const string OnAssociateAddAfter = "asso_add_aft";
        public const string OnAssociateRemoveBefore = "asso_rem_bef";
        public const string OnAssociateRemoveAfter = "asso_rem_aft";


        // NWNX Events - Stealth events
        public const string OnStealthEnterBefore = "stlent_add_bef";
        public const string OnStealthEnterAfter = "stlent_add_aft";
        public const string OnStealthExitBefore = "stlex_add_bef";
        public const string OnStealthExitAfter = "stlex_add_aft";

        // NWNX Events - Examine events
        public const string OnExamineObjectBefore = "examine_bef";
        public const string OnExamineObjectAfter = "examine_aft";

        // NWNX Events - Item validation events
        public const string OnValidateUseItemBefore = "item_valid_bef";
        public const string OnValidateUseItemAfter = "item_valid_aft";
        public const string OnValidateItemEquipBefore = "item_val_bef";
        public const string OnValidateItemEquipAfter = "item_val_aft";

        // NWNX Events - Item use events
        public const string OnUseItemBefore = "item_use_bef";
        public const string OnUseItemAfter = "item_use_aft";

        // NWNX Events - Item container events
        public const string OnItemInventoryCloseBefore = "inv_close_bef";
        public const string OnItemInventoryCloseAfter = "inv_close_aft";

        // NWNX Events - Item destroy events
        public const string OnItemDestroyObjectBefore = "item_dest_bef";
        public const string OnItemDestroyObjectAfter = "item_dest_aft";
        public const string OnItemDecrementStackSizeBefore = "item_dec_bef";
        public const string OnItemDecrementStackSizeAfter = "item_dec_aft";

        // NWNX Events - Item identification events
        public const string OnItemUseLoreBefore = "lore_id_bef";
        public const string OnItemUseLoreAfter = "lore_id_aft";
        public const string OnItemPayToIdentifyBefore = "pay_id_bef";
        public const string OnItemPayToIdentifyAfter = "pay_id_aft";

        // NWNX Events - Item split/merge events
        public const string OnItemSplitBefore = "item_splt_bef";
        public const string OnItemSplitAfter = "item_splt_aft";
        public const string OnItemMergeBefore = "item_merge_bef";
        public const string OnItemMergeAfter = "item_merge_aft";

        // NWNX Events - Item acquire events
        public const string OnItemAcquireBefore = "item_acquire_bef";
        public const string OnItemAcquireAfter = "item_acquire_aft";

        // NWNX Events - Feat use events
        public const string OnUseFeatBefore = "feat_use_bef";
        public const string OnUseFeatAfter = "feat_use_aft";

        // NWNX Events - DM events
        public const string OnDMGiveGoldBefore = "dm_givegold_bef";
        public const string OnDMGiveGoldAfter = "dm_givegold_aft";
        public const string OnDMGiveXPBefore = "dm_givexp_bef";
        public const string OnDMGiveXPAfter = "dm_givexp_aft";
        public const string OnDMGiveLevelBefore = "dm_givelvl_bef";
        public const string OnDMGiveLevelAfter = "dm_givelvl_aft";
        public const string OnDMGiveAlignmentBefore = "dm_givealn_bef";
        public const string OnDMGiveAlignmentAfter = "dm_givealn_aft";
        public const string OnDMSpawnObjectBefore = "dm_spwnobj_bef";
        public const string OnDMSpawnObjectAfter = "dm_spwnobj_aft";
        public const string OnDMGiveItemBefore = "dm_giveitem_bef";
        public const string OnDMGiveItemAfter = "dm_giveitem_aft";
        public const string OnDMHealBefore = "dm_heal_bef";
        public const string OnDMHealAfter = "dm_heal_aft";
        public const string OnDMKillBefore = "dm_kill_bef";
        public const string OnDMKillAfter = "dm_kill_aft";
        public const string OnDMToggleInvulnerableBefore = "dm_invuln_bef";
        public const string OnDMToggleInvulnerableAfter = "dm_invuln_aft";
        public const string OnDMForceRestBefore = "dm_forcerest_bef";
        public const string OnDMForceRestAfter = "dm_forcerest_aft";
        public const string OnDMLimboBefore = "dm_limbo_bef";
        public const string OnDMLimboAfter = "dm_limbo_aft";
        public const string OnDMToggleAIBefore = "dm_ai_bef";
        public const string OnDMToggleAIAfter = "dm_ai_aft";
        public const string OnDMToggleImmortalBefore = "dm_immortal_bef";
        public const string OnDMToggleImmortalAfter = "dm_immortal_aft";
        public const string OnDMGotoBefore = "dm_goto_bef";
        public const string OnDMGotoAfter = "dm_goto_aft";
        public const string OnDMPossessBefore = "dm_poss_bef";
        public const string OnDMPossessAfter = "dm_poss_aft";
        public const string OnDMPossessFullPowerBefore = "dm_possfull_bef";
        public const string OnDMPossessFullPowerAfter = "dm_possfull_aft";
        public const string OnDMToggleLockBefore = "dm_lock_bef";
        public const string OnDMToggleLockAfter = "dm_lock_aft";
        public const string OnDMDisableTrapBefore = "dm_distrap_bef";
        public const string OnDMDisableTrapAfter = "dm_distrap_aft";
        public const string OnDMJumpToPointBefore = "dm_jumppt_bef";
        public const string OnDMJumpToPointAfter = "dm_jumppt_aft";
        public const string OnDMJumpTargetToPointBefore = "dm_jumptarg_bef";
        public const string OnDMJumpTargetToPointAfter = "dm_jumptarg_aft";
        public const string OnDMJumpAllPlayersToPointBefore = "dm_jumpall_bef";
        public const string OnDMJumpAllPlayersToPointAfter = "dm_jumpall_aft";
        public const string OnDMChangeDifficultyBefore = "dm_chgdiff_bef";
        public const string OnDMChangeDifficultyAfter = "dm_chgdiff_aft";
        public const string OnDMViewInventoryBefore = "dm_vwinven_bef";
        public const string OnDMViewInventoryAfter = "dm_vwinven_aft";
        public const string OnDMSpawnTrapOnObjectBefore = "dm_spwntrap_bef";
        public const string OnDMSpawnTrapOnObjectAfter = "dm_spwntrap_aft";
        public const string OnDMDumpLocalsBefore = "dm_dumploc_bef";
        public const string OnDMDumpLocalsAfter = "dm_dumploc_aft";
        public const string OnDMAppearBefore = "dm_appear_bef";
        public const string OnDMAppearAfter = "dm_appear_aft";
        public const string OnDMDisappearBefore = "dm_disappear_bef";
        public const string OnDMDisappearAfter = "dm_disappear_aft";
        public const string OnDMSetFactionBefore = "dm_setfac_bef";
        public const string OnDMSetFactionAfter = "dm_setfac_aft";
        public const string OnDMTakeItemBefore = "dm_takeitem_bef";
        public const string OnDMTakeItemAfter = "dm_takeitem_aft";
        public const string OnDMSetStatBefore = "dm_setstat_bef";
        public const string OnDMSetStatAfter = "dm_setstat_aft";
        public const string OnDMGetVariableBefore = "dm_getvar_bef";
        public const string OnDMGetVariableAfter = "dm_getvar_aft";
        public const string OnDMSetVariableBefore = "dm_setvar_bef";
        public const string OnDMSetVariableAfter = "dm_setvar_aft";
        public const string OnDMSetTimeBefore = "dm_settime_bef";
        public const string OnDMSetTimeAfter = "dm_settime_aft";
        public const string OnDMSetDateBefore = "dm_setdate_bef";
        public const string OnDMSetDateAfter = "dm_setdate_aft";
        public const string OnDMSetFactionReputationBefore = "dm_setrep_bef";
        public const string OnDMSetFactionReputationAfter = "dm_setrep_aft";
        public const string OnDMGetFactionReputationBefore = "dm_getrep_bef";
        public const string OnDMGetFactionReputationAfter = "dm_getrep_aft";

        // NWNX Events - Client events
        public const string OnClientDisconnectBefore = "client_disc_bef";
        public const string OnClientDisconnectAfter = "client_disc_aft";
        public const string OnClientConnectBefore = "client_conn_bef";
        public const string OnClientConnectAfter = "client_conn_aft";

        // NWNX Events - Combat events
        public const string OnStartCombatRoundBefore = "comb_round_bef";
        public const string OnStartCombatRoundAfter = "comb_round_aft";
        public const string OnCastSpellBefore = "cast_spell_bef";
        public const string OnCastSpellAfter = "cast_spell_aft";
        public const string OnSetMemorizedSpellSlotBefore = "set_spell_bef";
        public const string OnSetMemorizedSpellSlotAfter = "set_spell_aft";
        public const string OnClearMemorizedSpellSlotBefore = "clr_spell_bef";
        public const string OnClearMemorizedSpellSlotAfter = "clr_spell_aft";
        public const string OnHealerKitBefore = "heal_kit_bef";
        public const string OnHealerKitAfter = "heal_kit_aft";
        public const string OnHealBefore = "heal_bef";
        public const string OnHealAfter = "heal_aft";
        public const string OnCombatModeOn = "combat_mode_on";
        public const string OnCombatModeOff = "combat_mode_off";

        // NWNX Events - Party events
        public const string OnPartyLeaveBefore = "pty_leave_bef";
        public const string OnPartyLeaveAfter = "pty_leave_aft";
        public const string OnPartyKickBefore = "pty_kick_bef";
        public const string OnPartyKickAfter = "pty_kick_aft";
        public const string OnPartyTransferLeadershipBefore = "pty_chgldr_bef";
        public const string OnPartyTransferLeadershipAfter = "pty_chgldr_aft";
        public const string OnPartyInviteBefore = "pty_invite_bef";
        public const string OnPartyInviteAfter = "pty_invite_aft";
        public const string OnPartyIgnoreInvitationBefore = "pty_ignore_bef";
        public const string OnPartyIgnoreInvitationAfter = "pty_ignore_aft";
        public const string OnPartyAcceptInvitationBefore = "pty_accept_bef";
        public const string OnPartyAcceptInvitationAfter = "pty_accept_aft";
        public const string OnPartyRejectInvitationBefore = "pty_reject_bef";
        public const string OnPartyRejectInvitationAfter = "pty_reject_aft";
        public const string OnPartyKickHenchmanBefore = "pty_kickhen_bef";
        public const string OnPartyKickHenchmanAfter = "pty_kickhen_aft";

        // NWNX Events - Skill events
        public const string OnUseSkillBefore = "use_skill_bef";
        public const string OnUseSkillAfter = "use_skill_aft";

        // NWNX Events - Map pin events
        public const string OnMapPinAddPinBefore = "mappin_add_bef";
        public const string OnMapPinAddPinAfter = "mappin_add_aft";
        public const string OnMapPinChangePinBefore = "mappin_chg_bef";
        public const string OnMapPinChangePinAfter = "mappin_chg_aft";
        public const string OnMapPinDestroyPinBefore = "mappin_rem_bef";
        public const string OnMapPinDestroyPinAfter = "mappin_rem_aft";

        // NWNX Events - Detection events
        public const string OnDoListenDetectionBefore = "det_listen_bef";
        public const string OnDoListenDetectionAfter = "det_listen_aft";
        public const string OnDoSpotDetectionBefore = "det_spot_bef";
        public const string OnDoSpotDetectionAfter = "det_spot_aft";

        // NWNX Events - Polymorph events
        public const string OnPolymorphBefore = "polymorph_bef";
        public const string OnPolymorphAfter = "polymorph_aft";
        public const string OnUnpolymorphBefore = "unpolymorph_bef";
        public const string OnUnpolymorphAfter = "unpolymorph_aft";

        // NWNX Events - Effect events
        public const string OnEffectAppliedBefore = "effect_app_bef";
        public const string OnEffectAppliedAfter = "effect_app_aft";
        public const string OnEffectRemovedBefore = "effect_rem_bef";
        public const string OnEffectRemovedAfter = "effect_rem_aft";

        // NWNX Events - Quickchat events
        public const string OnQuickchatBefore = "quickchat_bef";
        public const string OnQuickchatAfter = "quickchat_aft";

        // NWNX Events - Inventory events
        public const string OnInventoryOpenBefore = "inv_open_bef";
        public const string OnInventoryOpenAfter = "inv_open_aft";
        public const string OnInventorySelectPanelBefore = "inv_panel_bef";
        public const string OnInventorySelectPanelAfter = "inv_panel_aft";
        public const string OnInventoryAddItemBefore = "inv_add_bef";
        public const string OnInventoryAddItemAfter = "inv_add_aft";
        public const string OnInventoryRemoveItemBefore = "inv_rem_bef";
        public const string OnInventoryRemoveItemAfter = "inv_rem_aft";
        public const string OnInventoryAddGoldBefore = "add_gold_bef";
        public const string OnInventoryAddGoldAfter = "add_gold_aft";
        public const string OnInventoryRemoveGoldBefore = "rem_gold_bef";
        public const string OnInventoryRemoveGoldAfter = "rem_gold_aft";

        // NWNX Events - Barter events
        public const string OnBarterStartBefore = "bart_start_bef";
        public const string OnBarterStartAfter = "bart_start_aft";
        public const string OnBarterEndBefore = "bart_end_bef";
        public const string OnBarterEndAfter = "bart_end_aft";

        // NWNX Events - Store events
        public const string OnStoreRequestBuyBefore = "store_buy_bef";
        public const string OnStoreRequestBuyAfter = "store_buy_aft";
        public const string OnStoreRequestSellBefore = "store_sell_bef";
        public const string OnStoreRequestSellAfter = "store_sell_aft";
        public const string OnStoreSellBefore = "store_sell_bef";
        public const string OnStoreSellAfter = "store_sell_aft";

        // NWNX Events - Attack of opportunity events
        public const string OnBroadcastAttackOfOpportunityBefore = "brdcast_aoo_bef";
        public const string OnBroadcastAttackOfOpportunityAfter = "brdcast_aoo_aft";
        public const string OnCombatAttackOfOpportunityBefore = "combat_aoo_bef";
        public const string OnCombatAttackOfOpportunityAfter = "combat_aoo_aft";

        // NWNX Events - Trap events
        public const string OnTrapDisarmBefore = "trap_disarm_bef";
        public const string OnTrapDisarmAfter = "trap_disarm_aft";
        public const string OnTrapEnterBefore = "trap_enter_bef";
        public const string OnTrapEnterAfter = "trap_enter_aft";
        public const string OnTrapExamineBefore = "trap_exam_bef";
        public const string OnTrapExamineAfter = "trap_exam_aft";
        public const string OnTrapFlagBefore = "trap_flag_bef";
        public const string OnTrapFlagAfter = "trap_flag_aft";
        public const string OnTrapRecoverBefore = "trap_rec_bef";
        public const string OnTrapRecoverAfter = "trap_rec_aft";
        public const string OnTrapSetBefore = "trap_set_bef";
        public const string OnTrapSetAfter = "trap_set_aft";

        // NWNX Events - Timing bar events
        public const string OnTimingBarStartBefore = "timing_start_bef";
        public const string OnTimingBarStartAfter = "timing_start_aft";
        public const string OnTimingBarStopBefore = "timing_stop_bef";
        public const string OnTimingBarStopAfter = "timing_stop_aft";
        public const string OnTimingBarCancelBefore = "timing_canc_bef";
        public const string OnTimingBarCancelAfter = "timing_canc_aft";

        // NWNX Events - Webhook events
        public const string OnWebhookSuccess = "webhook_success";
        public const string OnWebhookFailure = "webhook_failure";

        // NWNX Events - Servervault events
        public const string OnCheckStickyPlayerNameReservedBefore = "name_reserve_bef";
        public const string OnCheckStickyPlayerNameReservedAfter = "name_reserve_aft";

        // NWNX Events - Leveling events
        public const string OnLevelUpBefore = "lvl_up_bef";
        public const string OnLevelUpAfter = "lvl_up_aft";
        public const string OnLevelUpAutomaticBefore = "lvl_upauto_bef";
        public const string OnLevelUpAutomaticAfter = "lvl_upauto_aft";
        public const string OnLevelDownBefore = "lvl_down_bef";
        public const string OnLevelDownAfter = "lvl_down_aft";

        // NWNX Events - PVP events
        public const string OnPVPAttitudeChangeBefore = "pvp_chgatt_bef";
        public const string OnPVPAttitudeChangeAfter = "pvp_chgatt_aft";

        // NWNX Events - Input events
        public const string OnInputWalkToWaypointBefore = "input_walk_bef";
        public const string OnInputWalkToWaypointAfter = "input_walk_aft";
        public const string OnInputAttackObjectBefore = "input_atk_bef";
        public const string OnInputAttackObjectAfter = "input_atk_aft";
        public const string OnInputDropItemBefore = "item_drop_bef";
        public const string OnInputDropItemAfter = "item_drop_aft";

        // NWNX Events - Material change events
        public const string OnMaterialChangeBefore = "material_chg_bef";
        public const string OnMaterialChangeAfter = "material_chg_aft";

        // NWNX Events - Object events
        public const string OnObjectLockBefore = "obj_lock_bef";
        public const string OnObjectLockAfter = "obj_lock_aft";
        public const string OnObjectUnlockBefore = "obj_unlock_bef";
        public const string OnObjectUnlockAfter = "obj_unlock_aft";

        // NWNX Events - UUID collision events
        public const string OnUUIDCollisionBefore = "uuid_coll_bef";
        public const string OnUUIDCollisionAfter = "uuid_coll_aft";

        // NWNX Events - ELC events
        public const string OnELCValidateCharacterBefore = "elc_validate_bef";
        public const string OnELCValidateCharacterAfter = "elc_validate_aft";

        // NWNX Events - Quickbar events
        public const string OnQuickbarSetButtonBefore = "qb_set_bef";
        public const string OnQuickbarSetButtonAfter = "qb_set_aft";

        // NWNX Events - Calendar events
        public const string OnCalendarHour = "calendar_hour";
        public const string OnCalendarDay = "calendar_day";
        public const string OnCalendarMonth = "calendar_month";
        public const string OnCalendarYear = "calendar_year";
        public const string OnCalendarDawn = "calendar_dawn";
        public const string OnCalendarDusk = "calendar_dusk";

        // NWNX Events - Broadcast spell cast events
        public const string OnBroadcastCastSpellBefore = "cast_spell_bef";
        public const string OnBroadcastCastSpellAfter = "cast_spell_aft";

        // NWNX Events - Debug events
        public const string OnDebugRunScriptBefore = "debug_script_bef";
        public const string OnDebugRunScriptAfter = "debug_script_aft";
        public const string OnDebugRunScriptChunkBefore = "debug_chunk_bef";
        public const string OnDebugRunScriptChunkAfter = "debug_chunk_aft";



        // NWNX Events - Ammunition events
        public const string OnItemAmmoReloadBefore = "ammo_reload_bef";
        public const string OnItemAmmoReloadAfter = "ammo_reload_aft";

        // NWNX Events - Scroll learn events
        public const string OnItemScrollLearnBefore = "scroll_lrn_bef";
        public const string OnItemScrollLearnAfter = "scroll_lrn_aft";

        // NWNX Events - Item events
        public const string OnItemHit = "item_on_hit";
        public const string OnItemUseBefore = "item_use_bef";
        public const string OnItemUnequipBefore = "item_uneqp_bef";
        public const string OnItemUnequipAfter = "item_uneqp_aft";
        public const string OnItemDecrementBefore = "item_dec_bef";
        public const string OnItemEquipValidateBefore = "item_eqpval_bef";
        public const string OnItemEquipValidateAfter = "item_eqpval_aft";

        // NWNX Events - Bartender events
        public const string OnBartenderStartBefore = "bart_start_bef";
        public const string OnBartenderEndBefore = "bart_end_bef";

        // NWNX Events - Communication events
        public const string OnNWNXChat = "on_nwnx_chat";

        // NWNX Events - SWLOR specific events
        public const string OnSWLORItemEquipValidBefore = "item_eqp_bef";

        // NWNX Events - Feat events
        public const string OnFeatUseBefore = "feat_use_bef";

        // =============================================================================
        // EVENTS WITHOUT CORRESPONDING EVENT CLASSES (Using old ScriptName pattern)
        // =============================================================================

        // DMFI events
        public const string OnDMFIClientEnter = "dmfi_onclienter";

        // Appearance events
        public const string OnAppearanceEdit = "appearance_edit";

        // SWLOR specific events
        public const string OnSWLORApplicationShutdown = "app_shutdown";
        public const string OnSWLORBuyPerk = "swlor_buy_perk";
        public const string OnSWLORGainSkillPoint = "swlor_gain_skill";
        public const string OnSWLORCompleteQuest = "swlor_comp_qst";
        public const string OnSWLORCacheSkillsLoaded = "swlor_skl_cache";
        public const string OnSWLORCombatPointDistributed = "cp_xp_distribute";
        public const string OnSWLORSkillLostByDecay = "swlor_lose_skill";
        public const string OnSWLORDeleteProperty = "swlor_del_prop";
    }
}