namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Constants for NWN script names used in NWNEventHandler attributes.
    /// This centralizes all script names to prevent typos and make refactoring easier.
    /// All values must be 16 characters or less due to NWN limitations.
    /// </summary>
    public static class ScriptName
    {
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

        // Database events
        public const string OnDatabaseLoaded = "db_loaded";

        // Character events
        public const string OnCharacterInitAfter = "char_init_after";
        public const string OnCharacterRebuild = "char_rebuild";
        public const string OnExitRebuild = "exit_rebuild";
        public const string OnExitSpending = "exit_spending";
        public const string OnBuyStatRebuild = "buy_stat_rebuild";

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

        // Item events
        public const string OnItemHit = "item_on_hit";
        public const string OnItemUseBefore = "item_use_bef";
        public const string OnItemEquipBefore = "item_eqp_bef";
        public const string OnItemUnequipBefore = "item_uneqp_bef";
        public const string OnItemDecrementBefore = "item_dec_bef";
        public const string OnItemDropBefore = "item_drop_bef";
        public const string OnItemEquipValueBefore = "item_eqpval_bef";

        // Dialog events
        public const string OnDialogStart = "dialog_start";
        public const string OnDialogAction0 = "dialog_action_0";
        public const string OnDialogAction1 = "dialog_action_1";
        public const string OnDialogAction2 = "dialog_action_2";
        public const string OnDialogAction3 = "dialog_action_3";
        public const string OnDialogAction4 = "dialog_action_4";
        public const string OnDialogAction5 = "dialog_action_5";
        public const string OnDialogAction6 = "dialog_action_6";
        public const string OnDialogAction7 = "dialog_action_7";
        public const string OnDialogAction8 = "dialog_action_8";
        public const string OnDialogAction9 = "dialog_action_9";
        public const string OnDialogAction10 = "dialog_action_10";
        public const string OnDialogAction11 = "dialog_action_11";
        public const string OnDialogAppears0 = "dialog_appears_0";
        public const string OnDialogAppears1 = "dialog_appears_1";
        public const string OnDialogAppears2 = "dialog_appears_2";
        public const string OnDialogAppears3 = "dialog_appears_3";
        public const string OnDialogAppears4 = "dialog_appears_4";
        public const string OnDialogAppears5 = "dialog_appears_5";
        public const string OnDialogAppears6 = "dialog_appears_6";
        public const string OnDialogAppears7 = "dialog_appears_7";
        public const string OnDialogAppears8 = "dialog_appears_8";
        public const string OnDialogAppears9 = "dialog_appears_9";
        public const string OnDialogAppears10 = "dialog_appears10";
        public const string OnDialogAppears11 = "dialog_appears11";
        public const string OnDialogAppearsH = "dialog_appears_h";
        public const string OnDialogAppearsN = "dialog_appears_n";
        public const string OnDialogActionN = "dialog_action_n";
        public const string OnDialogAppearsP = "dialog_appears_p";
        public const string OnDialogActionP = "dialog_action_p";
        public const string OnDialogAppearsB = "dialog_appears_b";
        public const string OnDialogActionB = "dialog_action_b";
        public const string OnDialogEnd = "dialog_end";
        public const string OnDialogStartConversation = "start_convo";
        public const string OnDialogAppear = "appear";
        public const string OnDialogAppears = "appears";
        public const string OnDialogCondition = "condition";
        public const string OnDialogConditions = "conditions";
        public const string OnDialogAction = "action";
        public const string OnDialogActions = "actions";

        // DMFI events
        public const string OnDMFIClientEnter = "dmfi_onclienter";

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

        // Party events
        public const string OnPartyAcceptBefore = "pty_accept_bef";
        public const string OnPartyLeaveBefore = "pty_leave_bef";
        public const string OnPartyChangeLeaderBefore = "pty_chgldr_bef";
        public const string OnAssociateAddBefore = "asso_add_bef";
        public const string OnAssociateRemoveBefore = "asso_rem_bef";
        public const string OnAssociateStateEffect = "assoc_stateffect";

        // Combat events
        public const string OnHealAfter = "heal_aft";
        public const string OnFeatUseBefore = "feat_use_bef";
        public const string OnRestStarted = "rest_started";
        public const string OnInputAttackBefore = "input_atk_bef";
        public const string OnInputAttackAfter = "input_atk_aft";
        public const string OnBroadcastAttackOfOpportunityBefore = "brdcast_aoo_bef";

        // Space events
        public const string OnSpaceEnter = "space_enter";
        public const string OnShipComputer = "ship_computer";
        public const string OnSpaceTarget = "spc_target";
        public const string OnStealthAddBefore = "stlent_add_bef";

        // Aura events
        public const string OnAuraEnter = "aura_enter";
        public const string OnAuraExit = "aura_exit";

        // Beast events
        public const string OnBeastBlocked = "beast_blocked";
        public const string OnBeastRoundEnd = "beast_roundend";
        public const string OnBeastConversation = "beast_convers";
        public const string OnBeastDamaged = "beast_damaged";
        public const string OnBeastDeath = "beast_death";
        public const string OnBeastDisturbed = "beast_disturbed";
        public const string OnBeastHeartbeat = "beast_hb";
        public const string OnBeastPerception = "beast_perception";
        public const string OnBeastAttacked = "beast_attacked";
        public const string OnBeastRest = "beast_rest";
        public const string OnBeastSpawn = "beast_spawn";
        public const string OnBeastSpellCast = "beast_spellcast";
        public const string OnBeastUserDefined = "beast_userdef";
        public const string OnBeastTerminate = "beast_term";

        // Droid events
        public const string OnDroidAssociateUsed = "droid_ass_used";
        public const string OnDroidBlocked = "droid_blocked";
        public const string OnDroidRoundEnd = "droid_roundend";
        public const string OnDroidConversation = "droid_convers";
        public const string OnDroidDamaged = "droid_damaged";
        public const string OnDroidDeath = "droid_death";
        public const string OnDroidDisturbed = "droid_disturbed";
        public const string OnDroidHeartbeat = "droid_hb";
        public const string OnDroidPerception = "droid_perception";
        public const string OnDroidAttacked = "droid_attacked";
        public const string OnDroidRest = "droid_rest";
        public const string OnDroidSpawn = "droid_spawn";
        public const string OnDroidSpellCast = "droid_spellcast";
        public const string OnDroidUserDefined = "droid_userdef";

        // Craft events
        public const string OnCraftUsed = "craft_on_used";
        public const string OnCraftSuccess = "craft_success";
        public const string OnRefineryUsed = "refinery_used";
        public const string OnResearchTerminal = "research_term";

        // Fishing events
        public const string OnFishPoint = "fish_point";
        public const string OnFinishFishing = "finish_fishing";

        // Guild events
        public const string OnQuestsRegistered = "qsts_registered";

        // Key item events
        public const string OnGetKeyItem = "get_key_item";

        // Loot events
        public const string OnCorpseClosed = "corpse_closed";
        public const string OnCorpseDisturbed = "corpse_disturbed";

        // Spawn events
        public const string OnPlaceableDeath = "plc_death";
        public const string OnSpawnDespawn = "spawn_despawn";

        // Client events
        public const string OnClientConnectBefore = "client_conn_bef";

        // Rest events
        public const string OnRestTriggerEnter = "rest_trg_enter";
        public const string OnRestTriggerExit = "rest_trg_exit";

        // Map pin events
        public const string OnMapPinAddBefore = "mappin_add_bef";
        public const string OnMapPinRemoveBefore = "mappin_rem_bef";
        public const string OnMapPinChangeBefore = "mappin_chg_bef";

        // Placeable events
        public const string OnPlaceableTeleport = "teleport";
        public const string OnPlaceablePermanentVfx = "permanent_vfx";
        public const string OnPlaceableGenericConversation = "generic_convo";
        public const string OnPlaceableSit = "sit";
        public const string OnPlaceableBuyRebuild = "buy_rebuild";
        public const string OnExamineBefore = "examine_bef";

        // Property events
        public const string OnPropertyStarportTerminal = "prop_star_term";
        public const string OnApartmentTerminal = "apartment_term";
        public const string OnEnterProperty = "enter_property";
        public const string OnOpenCitizenship = "open_citizenship";
        public const string OnOpenCityManage = "open_city_manage";
        public const string OnOpenPropertyBank = "open_prop_bank";

        // Quest events
        public const string OnQuestForceCrystal = "qst_force_crys";
        public const string OnQuestCollectOpen = "qst_collect_open";
        public const string OnQuestCollectClosed = "qst_collect_clsd";
        public const string OnQuestCollectDisturbed = "qst_collect_dist";
        public const string OnQuestPlaceable = "quest_placeable";
        public const string OnQuestTrigger = "quest_trigger";

        // Trash events
        public const string OnTrashOpened = "trash_opened";
        public const string OnTrashClosed = "trash_closed";
        public const string OnTrashDisturbed = "trash_disturbed";

        // Trap events
        public const string OnPitfallTrap = "pitfalltrap";

        // Combat point events
        public const string OnCombatPointXPDistribute = "cp_xp_distribute";

        // Incubator events
        public const string OnIncubatorTerminal = "incubator_term";

        // DNA events
        public const string OnDNAExtractUsed = "dna_extract_used";

        // Appearance events
        public const string OnAppearanceEdit = "appearance_edit";

        // SWLOR specific events
        public const string OnSwlorSkillCache = "swlor_skl_cache";
        public const string OnSwlorDeleteProperty = "swlor_del_prop";
        public const string OnSwlorHeartbeat = "swlor_heartbeat";
        public const string OnSwlorLoseSkill = "swlor_lose_skill";
        public const string OnSwlorBuyPerk = "swlor_buy_perk";
        public const string OnSwlorGainSkill = "swlor_gain_skill";
        public const string OnSwlorCompleteQuest = "swlor_comp_qst";

        // Interval events
        public const string OnIntervalPC6Seconds = "interval_pc_6s";

        // Object events
        public const string OnObjectDestroyed = "object_destroyed";

        // Application events
        public const string OnApplicationShutdown = "app_shutdown";
        public const string OnItemEquipValidBefore = "item_eqp_bef";
        public const string OnBuyPerk = "swlor_buy_perk";
        public const string OnGainSkillPoint = "swlor_gain_skill";
        public const string OnCompleteQuest = "swlor_comp_qst";
        public const string OnCacheSkillsLoaded = "swlor_skl_cache";
        public const string OnCombatPointDistributed = "cp_xp_distribute";
        public const string OnSkillLostByDecay = "swlor_lose_skill";
        public const string OnDeleteProperty = "swlor_del_prop";

        // Communication events
        public const string OnNWNXChat = "on_nwnx_chat";

        // GUI events
        public const string OnOpenHoloNet = "open_holonet";
        public const string OnOpenBank = "open_bank";
        public const string OnOpenTrainingStore = "open_train_store";
        public const string OnEnmityChanged = "enmity_changed";

        // Resource events
        public const string OnResourceUsed = "res_used";
        public const string OnResourceHeartbeat = "res_heartbeat";

        // Speeder events
        public const string OnEnmityAcquired = "enmity_acquired";
        public const string OnSpeederHook = "speeder_hook";

        // World events
        public const string OnEnterWorld = "enter_world";
        public const string OnExploreTrigger = "explore_trigger";

        // Harvester events
        public const string OnHarvesterUsed = "harvester_used";

        // Grenade events
        public const string OnGrenadeSmokeEnable = "grenade_smoke_en";
        public const string OnGrenadeSmokeHeartbeat = "grenade_smoke_hb";
        public const string OnGrenadeKolto1Enable = "grenade_kolt1_en";
        public const string OnGrenadeKolto1Heartbeat = "grenade_kolt1_hb";
        public const string OnGrenadeKolto2Enable = "grenade_kolt2_en";
        public const string OnGrenadeKolto2Heartbeat = "grenade_kolt2_hb";
        public const string OnGrenadeKolto3Enable = "grenade_kolt3_en";
        public const string OnGrenadeKolto3Heartbeat = "grenade_kolt3_hb";
        public const string OnGrenadeIncendiary1Enable = "grenade_inc1_en";
        public const string OnGrenadeIncendiary1Heartbeat = "grenade_inc1_hb";
        public const string OnGrenadeIncendiary2Enable = "grenade_inc2_en";
        public const string OnGrenadeIncendiary2Heartbeat = "grenade_inc2_hb";
        public const string OnGrenadeIncendiary3Enable = "grenade_inc3_en";
        public const string OnGrenadeIncendiary3Heartbeat = "grenade_inc3_hb";
        public const string OnGrenadeGas1Enable = "grenade_gas1_en";
        public const string OnGrenadeGas1Heartbeat = "grenade_gas1_hb";
        public const string OnGrenadeGas2Enable = "grenade_gas2_en";
        public const string OnGrenadeGas2Heartbeat = "grenade_gas2_hb";
        public const string OnGrenadeGas3Enable = "grenade_gas3_en";
        public const string OnGrenadeGas3Heartbeat = "grenade_gas3_hb";

        // Burst of Speed events
        public const string OnBurstOfSpeedApply = "bspeed_apply";
        public const string OnBurstOfSpeedRemoved = "bspeed_removed";

        // Store events
        public const string OnStoreSellAfter = "store_sell_aft";
        public const string OnStoreSellBefore = "store_sell_bef";

        // Bartender events
        public const string OnBartenderStartBefore = "bart_start_bef";
        public const string OnBartenderEndBefore = "bart_end_bef";

        // Scavenge events
        public const string OnScavengeOpened = "scav_opened";
        public const string OnScavengeDisturbed = "scav_disturbed";
        public const string OnScavengeClosed = "scav_closed";

        // NWNX Events - Associate events
        public const string OnAssociateAddAfter = "asso_add_aft";
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
        public const string OnItemInventoryOpenBefore = "inv_open_bef";
        public const string OnItemInventoryOpenAfter = "inv_open_aft";
        public const string OnItemInventoryCloseBefore = "inv_close_bef";
        public const string OnItemInventoryCloseAfter = "inv_close_aft";

        // NWNX Events - Item equip/unequip events
        public const string OnItemEquipAfter = "item_eqpval_aft";
        public const string OnItemUnequipAfter = "item_uneqp_aft";

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
        public const string OnDMPossessFullBefore = "dm_possfull_bef";

        // NWNX Events - Client events
        public const string OnClientDisconnectBefore = "client_disc_bef";
        public const string OnClientDisconnectAfter = "client_disc_aft";
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
        public const string OnCombatModeOn = "combat_mode_on";
        public const string OnCombatModeOff = "combat_mode_off";

        // NWNX Events - Party events
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
        public const string OnInventoryRemoveGoldBefore = "add_gold_bef";
        public const string OnInventoryRemoveGoldAfter = "add_gold_aft";

        // NWNX Events - Barter events
        public const string OnBarterStartBefore = "bart_start_bef";
        public const string OnBarterStartAfter = "bart_start_aft";
        public const string OnBarterEndBefore = "bart_end_bef";
        public const string OnBarterEndAfter = "bart_end_aft";

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

        // NWNX Events - Store events
        public const string OnStoreRequestBuyBefore = "store_buy_bef";
        public const string OnStoreRequestBuyAfter = "store_buy_aft";
        public const string OnStoreRequestSellBefore = "store_sell_bef";
        public const string OnStoreRequestSellAfter = "store_sell_aft";

        // NWNX Events - Attack of opportunity events
        public const string OnBroadcastAttackOfOpportunityAfter = "brdcast_aoo_aft";
        public const string OnCombatAttackOfOpportunityBefore = "combat_aoo_bef";
        public const string OnCombatAttackOfOpportunityAfter = "combat_aoo_aft";

        // NWNX Events - Ammunition events
        public const string OnItemAmmoReloadBefore = "ammo_reload_bef";
        public const string OnItemAmmoReloadAfter = "ammo_reload_aft";

        // NWNX Events - Scroll learn events
        public const string OnItemScrollLearnBefore = "scroll_lrn_bef";
        public const string OnItemScrollLearnAfter = "scroll_lrn_aft";

        // Telegraph events
        public const string TelegraphEffect = "telegraph_effect";
        public const string TelegraphApplied = "telegraph_applied";
        public const string TelegraphTicked = "telegraph_ticked";
        public const string TelegraphRemoved = "telegraph_removed";
    }
} 