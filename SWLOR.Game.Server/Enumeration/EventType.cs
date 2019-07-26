using System.CodeDom;

namespace SWLOR.Game.Server.Enumeration
{
    public class EventType
    {
        public const string AddAssociateBefore = "NWNX_ON_ADD_ASSOCIATE_BEFORE";
        public const string AddAssociateAfter = "NWNX_ON_ADD_ASSOCIATE_AFTER";

        public const string RemoveAssociateBefore = "NWNX_ON_REMOVE_ASSOCIATE_BEFORE";
        public const string RemoveAssociateAfter = "NWNX_ON_REMOVE_ASSOCIATE_AFTER";

        public const string EnterStealthBefore = "NWNX_ON_ENTER_STEALTH_BEFORE";
        public const string EnterStealthAfter = "NWNX_ON_ENTER_STEALTH_AFTER";

        public const string ExitStealthBefore = "NWNX_ON_EXIT_STEALTH_BEFORE";
        public const string ExitStealthAfter = "NWNX_ON_EXIT_STEALTH_AFTER";

        public const string ExamineObjectBefore = "NWNX_ON_EXAMINE_OBJECT_BEFORE";
        public const string ExamineObjectAfter = "NWNX_ON_EXAMINE_OBJECT_AFTER";

        public const string UseItemBefore = "NWNX_ON_USE_ITEM_BEFORE";
        public const string UseItemAfter = "NWNX_ON_USE_ITEM_AFTER";

        public const string ClientConnectBefore = "NWNX_ON_CLIENT_CONNECT_BEFORE";
        public const string ClientConnectAfter = "NWNX_ON_CLIENT_CONNECT_AFTER";

        public const string ClientDisconnectBefore = "NWNX_ON_CLIENT_DISCONNECT_BEFORE";
        public const string ClientDisconnectAfter = "NWNX_ON_CLIENT_DISCONNECT_AFTER";

        public const string CastSpellBefore = "NWNX_ON_CAST_SPELL_BEFORE";
        public const string CastSpellAfter = "NWNX_ON_CAST_SPELL_AFTER";

        public const string DestroyObjectBefore = "NWNX_ON_ITEM_DESTROY_OBJECT_BEFORE";
        public const string DestroyObjectAfter = "NWNX_ON_ITEM_DESTROY_OBJECT_AFTER";

        public const string ItemInventoryOpenBefore = "NWNX_ON_ITEM_INVENTORY_OPEN_BEFORE";
        public const string ItemInventoryOpenAfter = "NWNX_ON_ITEM_INVENTORY_OPEN_AFTER";

        public const string ItemInventoryCloseBefore = "NWNX_ON_ITEM_INVENTORY_CLOSE_BEFORE";
        public const string ItemInventoryCloseAfter = "NWNX_ON_ITEM_INVENTORY_CLOSE_AFTER";

        public const string ItemInventoryAddItemBefore = "NWNX_ON_ITEM_INVENTORY_ADD_ITEM_BEFORE";
        public const string ItemInventoryAddItemAfter = "NWNX_ON_ITEM_INVENTORY_ADD_ITEM_AFTER";

        public const string ItemInventoryRemoveItemBefore = "NWNX_ON_ITEM_INVENTORY_REMOVE_ITEM_BEFORE";
        public const string ItemInventoryRemoveItemAfter = "NWNX_ON_ITEM_INVENTORY_REMOVE_ITEM_AFTER";

        public const string ItemAmmoReloadBefore = "NWNX_ON_ITEM_AMMO_RELOAD_BEFORE";
        public const string ItemAmmoReloadAfter = "NWNX_ON_ITEM_AMMO_RELOAD_AFTER";

        public const string ItemScrollLearnBefore = "NWNX_ON_ITEM_SCROLL_LEARN_BEFORE";
        public const string ItemScrollLearnAfter = "NWNX_ON_ITEM_SCROLL_LEARN_AFTER";

        public const string ItemEquipBefore = "NWNX_ON_ITEM_EQUIP_BEFORE";
        public const string ItemEquipAfter = "NWNX_ON_ITEM_EQUIP_AFTER";

        public const string ItemUnequipBefore = "NWNX_ON_ITEM_UNEQUIP_BEFORE";
        public const string ItemUnequipAfter = "NWNX_ON_ITEM_UNEQUIP_AFTER";

        public const string DecrementItemStackSizeBefore = "NWNX_ON_ITEM_DECREMENT_STACKSIZE_BEFORE";
        public const string DecrementItemStackSizeAfter = "NWNX_ON_ITEM_DECREMENT_STACKSIZE_AFTER";

        public const string UseLoreBefore = "NWNX_ON_ITEM_USE_LORE_BEFORE";
        public const string UseLoreAfter = "NWNX_ON_ITEM_USE_LORE_AFTER";

        public const string PayToIdentifyBefore = "NWNX_ON_ITEM_PAY_TO_IDENTIFY_BEFORE";
        public const string PayToIdentifyAfter = "NWNX_ON_ITEM_PAY_TO_IDENTIFY_AFTER";

        public const string UseFeatBefore = "NWNX_ON_USE_FEAT_BEFORE";
        public const string UseFeatAfter = "NWNX_ON_USE_FEAT_AFTER";

        public const string StartCombatRoundBefore = "NWNX_ON_START_COMBAT_ROUND_BEFORE";
        public const string StartCombatRoundAfter = "NWNX_ON_START_COMBAT_ROUND_AFTER";

        public const string DMSpawnCreatureBefore = "NWNX_ON_DM_SPAWN_CREATURE_BEFORE";
        public const string DMSpawnCreatureAfter = "NWNX_ON_DM_SPAWN_CREATURE_AFTER";

        public const string DMSpawnItemBefore = "NWNX_ON_DM_SPAWN_ITEM_BEFORE";
        public const string DMSpawnItemAfter = "NWNX_ON_DM_SPAWN_ITEM_AFTER";

        public const string DMSpawnTriggerBefore = "NWNX_ON_DM_SPAWN_TRIGGER_BEFORE";
        public const string DMSpawnTriggerAfter = "NWNX_ON_DM_SPAWN_TRIGGER_AFTER";

        public const string DMSpawnWaypointBefore = "NWNX_ON_DM_SPAWN_WAYPOINT_BEFORE";
        public const string DMSpawnWaypointAfter = "NWNX_ON_DM_SPAWN_WAYPOINT_AFTER";

        public const string DMSpawnEncounterBefore = "NWNX_ON_DM_SPAWN_ENCOUNTER_BEFORE";
        public const string DMSpawnEncounterAfter = "NWNX_ON_DM_SPAWN_ENCOUNTER_AFTER";

        public const string DMSpawnPortalBefore = "NWNX_ON_DM_SPAWN_PORTAL_BEFORE";
        public const string DMSpawnPortalAfter = "NWNX_ON_DM_SPAWN_PORTAL_AFTER";

        public const string DMSpawnPlaceableBefore = "NWNX_ON_DM_SPAWN_PLACEABLE_BEFORE";
        public const string DMSpawnPlaceableAfter = "NWNX_ON_DM_SPAWN_PLACEABLE_AFTER";

        public const string DMChangeDifficultyBefore = "NWNX_ON_DM_CHANGE_DIFFICULTY_BEFORE";
        public const string DMChangeDifficultyAfter = "NWNX_ON_DM_CHANGE_DIFFICULTY_AFTER";

        public const string DMViewInventoryBefore = "NWNX_ON_DM_VIEW_INVENTORY_BEFORE";
        public const string DMViewInventoryAfter = "NWNX_ON_DM_VIEW_INVENTORY_AFTER";

        public const string DMSpawnTrapOnObjectBefore = "NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_BEFORE";
        public const string DMSpawnTrapOnObjectAfter = "NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_AFTER";

        public const string DMHealBefore = "NWNX_ON_DM_HEAL_BEFORE";
        public const string DMHealAfter = "NWNX_ON_DM_HEAL_AFTER";

        public const string DMKillBefore = "NWNX_ON_DM_KILL_BEFORE";
        public const string DMKillAfter = "NWNX_ON_DM_KILL_AFTER";

        public const string DMJumpBefore = "NWNX_ON_DM_JUMP_BEFORE";
        public const string DMJumpAfter = "NWNX_ON_DM_JUMP_AFTER";

        public const string DMPossessBefore = "NWNX_ON_DM_POSSESS_BEFORE";
        public const string DMPossessAfter = "NWNX_ON_DM_POSSESS_AFTER";

        public const string DMToggleImmortalBefore = "NWNX_ON_DM_TOGGLE_IMMORTAL_BEFORE";
        public const string DMToggleImmortalAfter = "NWNX_ON_DM_TOGGLE_IMMORTAL_AFTER";

        public const string DMForceRestBefore = "NWNX_ON_DM_FORCE_REST_BEFORE";
        public const string DMForceRestAfter = "NWNX_ON_DM_FORCE_REST_AFTER";

        public const string DMLimboBefore = "NWNX_ON_DM_LIMBO_BEFORE";
        public const string DMLimboAfter = "NWNX_ON_DM_LIMBO_AFTER";

        public const string DMToggleAIBefore = "NWNX_ON_DM_TOGGLE_AI_BEFORE";
        public const string DMToggleAIAfter = "NWNX_ON_DM_TOGGLE_AI_AFTER";

        public const string DMToggleLockBefore = "NWNX_ON_DM_TOGGLE_LOCK_BEFORE";
        public const string DMToggleLockAfter = "NWNX_ON_DM_TOGGLE_LOCK_AFTER";

        public const string DMDisableTrapBefore = "NWNX_ON_DM_DISABLE_TRAP_BEFORE";
        public const string DMDisableTrapAfter = "NWNX_ON_DM_DISABLE_TRAP_AFTER";

        public const string DMAppearBefore = "NWNX_ON_DM_APPEAR_BEFORE";
        public const string DMAppearAfter = "NWNX_ON_DM_APPEAR_AFTER";
        
        public const string DMDisappearBefore = "NWNX_ON_DM_DISAPPEAR_BEFORE";
        public const string DMDisappearAfter = "NWNX_ON_DM_DISAPPEAR_AFTER";

        public const string DMJumpToPointBefore = "NWNX_ON_DM_JUMP_TO_POINT_BEFORE";
        public const string DMJumpToPointAfter = "NWNX_ON_DM_JUMP_TO_POINT_AFTER";
        
        public const string DMGiveXPBefore = "NWNX_ON_DM_GIVE_XP_BEFORE";
        public const string DMGiveXPAfter = "NWNX_ON_DM_GIVE_XP_AFTER";
        
        public const string DMGiveLevelBefore = "NWNX_ON_DM_GIVE_LEVEL_BEFORE";
        public const string DMGiveLevelAfter = "NWNX_ON_DM_GIVE_LEVEL_AFTER";
        
        public const string DMGiveGoldBefore = "NWNX_ON_DM_GIVE_GOLD_BEFORE";
        public const string DMGiveGoldAfter = "NWNX_ON_DM_GIVE_GOLD_AFTER";
        
        public const string DMSetFactionBefore = "NWNX_ON_DM_SET_FACTION_BEFORE";
        public const string DMSetFactionAfter = "NWNX_ON_DM_SET_FACTION_AFTER";
        
        public const string DMGiveItemBefore = "NWNX_ON_DM_GIVE_ITEM_BEFORE";
        public const string DMGiveItemAfter = "NWNX_ON_DM_GIVE_ITEM_AFTER";
        
        public const string DMTakeItemBefore = "NWNX_ON_DM_TAKE_ITEM_BEFORE";
        public const string DMTakeItemAfter = "NWNX_ON_DM_TAKE_ITEM_AFTER";
        
        public const string DMJumpTargetToPointBefore = "NWNX_ON_DM_JUMP_TARGET_TO_POINT_BEFORE";
        public const string DMJumpTargetToPointAfter = "NWNX_ON_DM_JUMP_TARGET_TO_POINT_AFTER";
        
        public const string DMJumpAllPlayersToPointBefore = "NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_BEFORE";
        public const string DMJumpAllPlayersToPointAfter = "NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_AFTER";
        
        public const string DMSetStatBefore = "NWNX_ON_DM_SET_STAT_BEFORE";
        public const string DMSetStatAfter = "NWNX_ON_DM_SET_STAT_AFTER";
        
        public const string DMGetVariableBefore = "NWNX_ON_DM_GET_VARIABLE_BEFORE";
        public const string DMGetVariableAfter = "NWNX_ON_DM_GET_VARIABLE_AFTER";
        
        public const string DMSetVariableBefore = "NWNX_ON_DM_SET_VARIABLE_BEFORE";
        public const string DMSetVariableAfter = "NWNX_ON_DM_SET_VARIABLE_AFTER";
        
        public const string DMSetTimeBefore = "NWNX_ON_DM_SET_TIME_BEFORE";
        public const string DMSetTimeAfter = "NWNX_ON_DM_SET_TIME_AFTER";
        
        public const string DMSetDateBefore = "NWNX_ON_DM_SET_DATE_BEFORE";
        public const string DMSetDateAfter = "NWNX_ON_DM_SET_DATE_AFTER";

        public const string UseHealerKitBefore = "NWNX_ON_USE_HEALER_KIT_BEFORE";
        public const string UseHealerKitAfter = "NWNX_ON_USE_HEALER_KIT_AFTER";

        public const string PartyLeaveBefore = "NWNX_ON_PARTY_LEAVE_BEFORE";
        public const string PartyLeaveAfter = "NWNX_ON_PARTY_LEAVE_AFTER";

        public const string PartyKickBefore = "NWNX_ON_PARTY_KICK_BEFORE";
        public const string PartyKickAfter = "NWNX_ON_PARTY_KICK_AFTER";

        public const string PartyTransferLeadershipBefore = "NWNX_ON_PARTY_TRANSFER_LEADERSHIP_BEFORE";
        public const string PartyTransferLeadershipAfter =  "NWNX_ON_PARTY_TRANSFER_LEADERSHIP_AFTER";

        public const string PartyInviteBefore = "NWNX_ON_PARTY_INVITE_BEFORE";
        public const string PartyInviteAfter = "NWNX_ON_PARTY_INVITE_AFTER";

        public const string PartyIgnoreInvitationBefore = "NWNX_ON_PARTY_IGNORE_INVITATION_BEFORE";
        public const string PartyIgnoreInvitationAfter =  "NWNX_ON_PARTY_IGNORE_INVITATION_AFTER";

        public const string PartyAcceptInvitationBefore = "NWNX_ON_PARTY_ACCEPT_INVITATION_BEFORE";
        public const string PartyAcceptInvitationAfter = "NWNX_ON_PARTY_ACCEPT_INVITATION_AFTER";

        public const string PartyRejectInvitationBefore = "NWNX_ON_PARTY_REJECT_INVITATION_BEFORE";
        public const string PartyRejectInvitationAfter =  "NWNX_ON_PARTY_REJECT_INVITATION_AFTER";

        public const string PartyKickHenchmanBefore = "NWNX_ON_PARTY_KICK_HENCHMAN_BEFORE";
        public const string PartyKickHenchmanAfter =  "NWNX_ON_PARTY_KICK_HENCHMAN_AFTER";

        // NOTE: Requires the NWNX_CombatModes plugin to work.
        public const string CombatModeOn = "NWNX_ON_COMBAT_MODE_ON";
        public const string CombatModeOff = "NWNX_ON_COMBAT_MODE_OFF";

        public const string UseSkillBefore = "NWNX_ON_USE_SKILL_BEFORE";
        public const string UseSkillAfter = "NWNX_ON_USE_SKILL_AFTER";

        public const string AddMapPinBefore = "NWNX_ON_MAP_PIN_ADD_PIN_BEFORE";
        public const string AddMapPinAfter = "NWNX_ON_MAP_PIN_ADD_PIN_AFTER";

        public const string ChangeMapPinBefore = "NWNX_ON_MAP_PIN_CHANGE_PIN_BEFORE";
        public const string ChangeMapPinAfter = "NWNX_ON_MAP_PIN_CHANGE_PIN_AFTER";

        public const string DestroyMapPinBefore = "NWNX_ON_MAP_PIN_DESTROY_PIN_BEFORE";
        public const string DestroyMapPinAfter = "NWNX_ON_MAP_PIN_DESTROY_PIN_AFTER";

        public const string DoListenDetectionBefore = "NWNX_ON_DO_LISTEN_DETECTION_BEFORE";
        public const string DoListenDetectionAfter = "NWNX_ON_DO_LISTEN_DETECTION_AFTER";

        public const string DoSpotDetectionBefore = "NWNX_ON_DO_SPOT_DETECTION_BEFORE";
        public const string DoSpotDetectionAfter = "NWNX_ON_DO_SPOT_DETECTION_AFTER";

        public const string PolymorphBefore = "NWNX_ON_POLYMORPH_BEFORE";
        public const string PolymorphAfter = "NWNX_ON_POLYMORPH_AFTER";

        public const string UnPolymorphBefore = "NWNX_ON_UNPOLYMORPH_BEFORE";
        public const string UnPolymorphAfter = "NWNX_ON_UNPOLYMORPH_AFTER";

        public const string EffectAppliedBefore = "NWNX_ON_EFFECT_APPLIED_BEFORE";
        public const string EffectAppliedAfter = "NWNX_ON_EFFECT_APPLIED_AFTER";

        public const string EffectRemovedBefore = "NWNX_ON_EFFECT_REMOVED_BEFORE";
        public const string EffectRemovedAfter = "NWNX_ON_EFFECT_REMOVED_AFTER";

        public const string QuickChatBefore = "NWNX_ON_QUICKCHAT_BEFORE";
        public const string QuickChatAfter = "NWNX_ON_QUICKCHAT_AFTER";

        public const string InventoryOpenBefore = "NWNX_ON_INVENTORY_OPEN_BEFORE";
        public const string InventoryOpenAfter = "NWNX_ON_INVENTORY_OPEN_AFTER";

        public const string InventorySelectPanelBefore = "NWNX_ON_INVENTORY_SELECT_PANEL_BEFORE";
        public const string InventorySelectPanelAfter = "NWNX_ON_INVENTORY_SELECT_PANEL_AFTER";

        public const string BarterStartBefore = "NWNX_ON_BARTER_START_BEFORE";
        public const string BarterStartAfter = "NWNX_ON_BARTER_START_AFTER";

        public const string TrapDisarmBefore = "NWNX_ON_TRAP_DISARM_BEFORE";
        public const string TrapDisarmAfter = "NWNX_ON_TRAP_DISARM_AFTER";

        public const string TrapEnterBefore = "NWNX_ON_TRAP_ENTER_BEFORE";
        public const string TrapEnterAfter = "NWNX_ON_TRAP_ENTER_AFTER";

        public const string TrapExamineBefore = "NWNX_ON_TRAP_EXAMINE_BEFORE";
        public const string TrapExamineAfter = "NWNX_ON_TRAP_EXAMINE_AFTER";

        public const string TrapFlagBefore = "NWNX_ON_TRAP_FLAG_BEFORE";
        public const string TrapFlagAfter = "NWNX_ON_TRAP_FLAG_AFTER";

        public const string TrapRecoverBefore = "NWNX_ON_TRAP_RECOVER_BEFORE";
        public const string TrapRecoverAfter = "NWNX_ON_TRAP_RECOVER_AFTER";

        public const string TrapSetBefore = "NWNX_ON_TRAP_SET_BEFORE";
        public const string TrapSetAfter = "NWNX_ON_TRAP_SET_AFTER";

        public const string TimingBarStartBefore = "NWNX_ON_TIMING_BAR_START_BEFORE";
        public const string TimingBarStartAfter = "NWNX_ON_TIMING_BAR_START_AFTER";

        public const string TimingBarStopBefore = "NWNX_ON_TIMING_BAR_STOP_BEFORE";
        public const string TimingBarStopAfter = "NWNX_ON_TIMING_BAR_STOP_AFTER";

        public const string TimingBarCancelBefore = "NWNX_ON_TIMING_BAR_CANCEL_BEFORE";
        public const string TimingBarCancelAfter = "NWNX_ON_TIMING_BAR_CANCEL_AFTER";

        // Note: This requires the NWNX_WebHook plugin to work.
        // We'll likely never need this since we can make web calls directly from C#, but it's included here for completion's sake.
        public const string WebhookSuccess = "NWNX_ON_WEBHOOK_SUCCESS";
        public const string WebhookFailure = "NWNX_ON_WEBHOOK_FAILURE";

        public const string CheckStickyPlayerNameReservedBefore = "NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_BEFORE";
        public const string CheckStickyPlayerNameReservedAfter = "NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_AFTER";

        public const string LevelUpBefore = "NWNX_ON_LEVEL_UP_BEFORE";
        public const string LevelUpAfter = "NWNX_ON_LEVEL_UP_AFTER";

        public const string LevelUpAutomaticBefore = "NWNX_ON_LEVEL_UP_AUTOMATIC_BEFORE";
        public const string LevelUpAutomaticAfter = "NWNX_ON_LEVEL_UP_AUTOMATIC_AFTER";

        public const string LevelDownBefore = "NWNX_ON_LEVEL_DOWN_BEFORE";
        public const string LevelDownAfter = "NWNX_ON_LEVEL_DOWN_AFTER";
    }
}
