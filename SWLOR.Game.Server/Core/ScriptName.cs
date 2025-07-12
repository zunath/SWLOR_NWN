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

        // DM events
        public const string OnDMPossessBefore = "dm_poss_bef";
        public const string OnDMPossessFullBefore = "dm_possfull_bef";
        public const string OnDMLimboBefore = "dm_limbo_bef";
        public const string OnDMSpawnObjectAfter = "dm_spwnobj_aft";

        // Player events
        public const string OnPlayerDamaged = "pc_damaged";
        public const string OnPlayerHeartbeat = "pc_heartbeat";
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
    }
} 