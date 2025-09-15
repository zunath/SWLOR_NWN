using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class EventRegistration
    {
        /// <summary>
        /// Fires on the module PreLoad event. This event should be specified in the environment variables.
        /// This will hook all module/global events.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModulePreload)]
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

                ExecuteScript(ScriptName.OnModuleContentChange, GetModule());
            }

            // Fire off the mod_cache event which is used for caching data, before mod_load runs.
            ExecuteScript(ScriptName.OnModuleCacheBefore, GetModule());
            ExecuteScript(ScriptName.OnModuleCacheAfter, GetModule());
        }

        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void ExecuteHeartbeatEvent()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                ExecuteScript(ScriptName.OnIntervalPC6Seconds, player);
            }
        }

        /// <summary>
        /// When a player enters the server, hook their event scripts.
        /// Also add them to a UI processor list.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void EnterServer()
        {
            HookPlayerEvents();
        }

        private static void HookPlayerEvents()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) 
                return;

            SetEventScript(player, EventScript.Creature_OnHeartbeat, ScriptName.OnPlayerHeartbeat);
            SetEventScript(player, EventScript.Creature_OnNotice, ScriptName.OnPlayerPerception);
            SetEventScript(player, EventScript.Creature_OnSpellCastAt, ScriptName.OnPlayerSpellCastAt);
            SetEventScript(player, EventScript.Creature_OnMeleeAttacked, ScriptName.OnPlayerAttacked);
            SetEventScript(player, EventScript.Creature_OnDamaged, ScriptName.OnPlayerDamaged);
            SetEventScript(player, EventScript.Creature_OnDisturbed, ScriptName.OnPlayerDisturbed);
            SetEventScript(player, EventScript.Creature_OnEndCombatRound, ScriptName.OnPlayerRoundEnd);
            SetEventScript(player, EventScript.Creature_OnSpawnIn, ScriptName.OnPlayerSpawn);
            SetEventScript(player, EventScript.Creature_OnRested, ScriptName.OnPlayerRested);
            SetEventScript(player, EventScript.Creature_OnDeath, ScriptName.OnPlayerDeath);
            SetEventScript(player, EventScript.Creature_OnUserDefined, ScriptName.OnPlayerUserDefined);
            SetEventScript(player, EventScript.Creature_OnBlockedByDoor, ScriptName.OnPlayerBlocked);
        }


        /// <summary>
        /// Hooks module-wide scripts.
        /// </summary>
        private static void HookModuleEvents()
        {
            var module = GetModule();

            SetEventScript(module, EventScript.Module_OnAcquireItem, ScriptName.OnModuleAcquire);
            SetEventScript(module, EventScript.Module_OnActivateItem, ScriptName.OnModuleActivate);
            SetEventScript(module, EventScript.Module_OnClientEnter, ScriptName.OnModuleEnter);
            SetEventScript(module, EventScript.Module_OnClientExit, ScriptName.OnModuleExit);
            SetEventScript(module, EventScript.Module_OnPlayerCancelCutscene, ScriptName.OnModulePlayerCancelCutscene);
            SetEventScript(module, EventScript.Module_OnHeartbeat, ScriptName.OnModuleHeartbeat);
            SetEventScript(module, EventScript.Module_OnModuleLoad, ScriptName.OnModuleLoad);
            SetEventScript(module, EventScript.Module_OnPlayerChat, ScriptName.OnModuleChat);
            SetEventScript(module, EventScript.Module_OnPlayerDying, ScriptName.OnModuleDying);
            SetEventScript(module, EventScript.Module_OnPlayerDeath, ScriptName.OnModuleDeath);
            SetEventScript(module, EventScript.Module_OnEquipItem, ScriptName.OnModuleEquip);
            SetEventScript(module, EventScript.Module_OnPlayerLevelUp, ScriptName.OnModuleLevelUp);
            SetEventScript(module, EventScript.Module_OnRespawnButtonPressed, ScriptName.OnModuleRespawn);
            SetEventScript(module, EventScript.Module_OnPlayerRest, ScriptName.OnModuleRest);
            SetEventScript(module, EventScript.Module_OnUnequipItem, ScriptName.OnModuleUnequip);
            SetEventScript(module, EventScript.Module_OnLoseItem, ScriptName.OnModuleUnacquire);
            SetEventScript(module, EventScript.Module_OnUserDefined, ScriptName.OnModuleUserDefined);
            SetEventScript(module, EventScript.Module_OnPlayerTarget, ScriptName.OnModulePlayerTarget);
            SetEventScript(module, EventScript.Module_OnPlayerGuiEvent, ScriptName.OnModuleGuiEvent);
            SetEventScript(module, EventScript.Module_OnPlayerTileEvent, ScriptName.OnModuleTileEvent);
            SetEventScript(module, EventScript.Module_OnNuiEvent, ScriptName.OnModuleNuiEvent);
        }

        /// <summary>
        /// Hooks area-wide scripts.
        /// </summary>
        private static void HookAreaEvents()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                SetEventScript(area, EventScript.Area_OnEnter, ScriptName.OnAreaEnter);
                SetEventScript(area, EventScript.Area_OnExit, ScriptName.OnAreaExit);
                SetEventScript(area, EventScript.Area_OnHeartbeat, string.Empty); // Disabled for performance reasons
                SetEventScript(area, EventScript.Area_OnUserDefined, ScriptName.OnAreaUserDefined);
            }
        }

        /// <summary>
        /// Hooks NWNX scripts.
        /// </summary>
        private static void HookNWNXEvents()
        {
            // Chat Plugin Events start here.
            ChatPlugin.RegisterChatScript(ScriptName.OnNWNXChat);

            // Events Plugin Events start here.

            // Associate events
            EventsPlugin.SubscribeEvent("NWNX_ON_ADD_ASSOCIATE_BEFORE", ScriptName.OnAssociateAddBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ADD_ASSOCIATE_AFTER", ScriptName.OnAssociateAddAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_REMOVE_ASSOCIATE_BEFORE", ScriptName.OnAssociateRemoveBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_REMOVE_ASSOCIATE_AFTER", ScriptName.OnAssociateRemoveAfter);

            // Stealth events
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_ENTER_BEFORE", ScriptName.OnStealthEnterBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_ENTER_AFTER", ScriptName.OnStealthEnterAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_EXIT_BEFORE", ScriptName.OnStealthExitBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_EXIT_AFTER", ScriptName.OnStealthExitAfter);

            // Examine events
            EventsPlugin.SubscribeEvent("NWNX_ON_EXAMINE_OBJECT_BEFORE", ScriptName.OnExamineObjectBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_EXAMINE_OBJECT_AFTER", ScriptName.OnExamineObjectAfter);

            // Validate Use Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_USE_ITEM_BEFORE", ScriptName.OnValidateUseItemBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_USE_ITEM_AFTER", ScriptName.OnValidateUseItemAfter);

            // Use Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_ITEM_BEFORE", ScriptName.OnUseItemBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_ITEM_AFTER", ScriptName.OnUseItemAfter);

            // Item Container events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_OPEN_BEFORE", ScriptName.OnItemInventoryOpenBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_OPEN_AFTER", ScriptName.OnItemInventoryOpenAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_CLOSE_BEFORE", ScriptName.OnItemInventoryCloseBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_CLOSE_AFTER", ScriptName.OnItemInventoryCloseAfter);

            // Ammunition Reload events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_AMMO_RELOAD_BEFORE", ScriptName.OnItemAmmoReloadBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_AMMO_RELOAD_AFTER", ScriptName.OnItemAmmoReloadAfter);

            // Scroll Learn events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SCROLL_LEARN_BEFORE", ScriptName.OnItemScrollLearnBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SCROLL_LEARN_AFTER", ScriptName.OnItemScrollLearnAfter);

            // Validate Item Equip events
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_ITEM_EQUIP_BEFORE", ScriptName.OnValidateItemEquipBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_ITEM_EQUIP_AFTER", ScriptName.OnValidateItemEquipAfter);

            // Item Equip events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_EQUIP_BEFORE", ScriptName.OnItemEquipBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_EQUIP_AFTER", ScriptName.OnItemEquipAfter);

            // Item Unequip events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_UNEQUIP_BEFORE", ScriptName.OnItemUnequipBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_UNEQUIP_AFTER", ScriptName.OnItemUnequipAfter);

            // Item Destroy events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DESTROY_OBJECT_BEFORE", ScriptName.OnItemDestroyObjectBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DESTROY_OBJECT_AFTER", ScriptName.OnItemDestroyObjectAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DECREMENT_STACKSIZE_BEFORE", ScriptName.OnItemDecrementStackSizeBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DECREMENT_STACKSIZE_AFTER", ScriptName.OnItemDecrementStackSizeAfter);

            // Item Use Lore to Identify events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_USE_LORE_BEFORE", ScriptName.OnItemUseLoreBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_USE_LORE_AFTER", ScriptName.OnItemUseLoreAfter);

            // Item Pay to Identify events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_PAY_TO_IDENTIFY_BEFORE", ScriptName.OnItemPayToIdentifyBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_PAY_TO_IDENTIFY_AFTER", ScriptName.OnItemPayToIdentifyAfter);

            // Item Split events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SPLIT_BEFORE", ScriptName.OnItemSplitBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SPLIT_AFTER", ScriptName.OnItemSplitAfter);

            // Item Merge events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_MERGE_BEFORE", ScriptName.OnItemMergeBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_MERGE_AFTER", ScriptName.OnItemMergeAfter);

            // Acquire Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_ACQUIRE_BEFORE", ScriptName.OnItemAcquireBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ITEM_ACQUIRE_AFTER", ScriptName.OnItemAcquireAfter);

            // Feat Use events
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_FEAT_BEFORE", ScriptName.OnUseFeatBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_FEAT_AFTER", ScriptName.OnUseFeatAfter);

            // DM Give events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_GOLD_BEFORE", ScriptName.OnDMGiveGoldBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_GOLD_AFTER", ScriptName.OnDMGiveGoldAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_XP_BEFORE", ScriptName.OnDMGiveXPBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_XP_AFTER", ScriptName.OnDMGiveXPAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_LEVEL_BEFORE", ScriptName.OnDMGiveLevelBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_LEVEL_AFTER", ScriptName.OnDMGiveLevelAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ALIGNMENT_BEFORE", ScriptName.OnDMGiveAlignmentBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ALIGNMENT_AFTER", ScriptName.OnDMGiveAlignmentAfter);

            // DM Spawn events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_OBJECT_BEFORE", ScriptName.OnDMSpawnObjectBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_OBJECT_AFTER", ScriptName.OnDMSpawnObjectAfter);

            // DM Give Item events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ITEM_BEFORE", ScriptName.OnDMGiveItemBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ITEM_AFTER", ScriptName.OnDMGiveItemAfter);

            // DM Multiple Object Action events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_HEAL_BEFORE", ScriptName.OnDMHealBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_HEAL_AFTER", ScriptName.OnDMHealAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_KILL_BEFORE", ScriptName.OnDMKillBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_KILL_AFTER", ScriptName.OnDMKillAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_INVULNERABLE_BEFORE", ScriptName.OnDMToggleInvulnerableBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_INVULNERABLE_AFTER", ScriptName.OnDMToggleInvulnerableAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_FORCE_REST_BEFORE", ScriptName.OnDMForceRestBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_FORCE_REST_AFTER", ScriptName.OnDMForceRestAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_LIMBO_BEFORE", ScriptName.OnDMLimboBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_LIMBO_AFTER", ScriptName.OnDMLimboAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_AI_BEFORE", ScriptName.OnDMToggleAIBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_AI_AFTER", ScriptName.OnDMToggleAIAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_IMMORTAL_BEFORE", ScriptName.OnDMToggleImmortalBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_IMMORTAL_AFTER", ScriptName.OnDMToggleImmortalAfter);

            // DM Single Object Action events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GOTO_BEFORE", ScriptName.OnDMGotoBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GOTO_AFTER", ScriptName.OnDMGotoAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_BEFORE", ScriptName.OnDMPossessBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_AFTER", ScriptName.OnDMPossessAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_FULL_POWER_BEFORE", ScriptName.OnDMPossessFullPowerBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_FULL_POWER_AFTER", ScriptName.OnDMPossessFullPowerAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_LOCK_BEFORE", ScriptName.OnDMToggleLockBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_LOCK_AFTER", ScriptName.OnDMToggleLockAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISABLE_TRAP_BEFORE", ScriptName.OnDMDisableTrapBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISABLE_TRAP_AFTER", ScriptName.OnDMDisableTrapAfter);

            // DM Jump events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TO_POINT_BEFORE", ScriptName.OnDMJumpToPointBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TO_POINT_AFTER", ScriptName.OnDMJumpToPointAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TARGET_TO_POINT_BEFORE", ScriptName.OnDMJumpTargetToPointBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TARGET_TO_POINT_AFTER", ScriptName.OnDMJumpTargetToPointAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_BEFORE", ScriptName.OnDMJumpAllPlayersToPointBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_AFTER", ScriptName.OnDMJumpAllPlayersToPointAfter);

            // DM Change Difficulty events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_CHANGE_DIFFICULTY_BEFORE", ScriptName.OnDMChangeDifficultyBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_CHANGE_DIFFICULTY_AFTER", ScriptName.OnDMChangeDifficultyAfter);

            // DM View Inventory events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_VIEW_INVENTORY_BEFORE", ScriptName.OnDMViewInventoryBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_VIEW_INVENTORY_AFTER", ScriptName.OnDMViewInventoryAfter);

            // DM Spawn Trap events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_BEFORE", ScriptName.OnDMSpawnTrapOnObjectBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_AFTER", ScriptName.OnDMSpawnTrapOnObjectAfter);

            // DM Dump Locals events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DUMP_LOCALS_BEFORE", ScriptName.OnDMDumpLocalsBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DUMP_LOCALS_AFTER", ScriptName.OnDMDumpLocalsAfter);

            // DM Other events
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_APPEAR_BEFORE", ScriptName.OnDMAppearBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_APPEAR_AFTER", ScriptName.OnDMAppearAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISAPPEAR_BEFORE", ScriptName.OnDMDisappearBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_DISAPPEAR_AFTER", ScriptName.OnDMDisappearAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_BEFORE", ScriptName.OnDMSetFactionBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_AFTER", ScriptName.OnDMSetFactionAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TAKE_ITEM_BEFORE", ScriptName.OnDMTakeItemBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_TAKE_ITEM_AFTER", ScriptName.OnDMTakeItemAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_STAT_BEFORE", ScriptName.OnDMSetStatBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_STAT_AFTER", ScriptName.OnDMSetStatAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_VARIABLE_BEFORE", ScriptName.OnDMGetVariableBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_VARIABLE_AFTER", ScriptName.OnDMGetVariableAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_VARIABLE_BEFORE", ScriptName.OnDMSetVariableBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_VARIABLE_AFTER", ScriptName.OnDMSetVariableAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_TIME_BEFORE", ScriptName.OnDMSetTimeBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_TIME_AFTER", ScriptName.OnDMSetTimeAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_DATE_BEFORE", ScriptName.OnDMSetDateBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_DATE_AFTER", ScriptName.OnDMSetDateAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_REPUTATION_BEFORE", ScriptName.OnDMSetFactionReputationBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_REPUTATION_AFTER", ScriptName.OnDMSetFactionReputationAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_FACTION_REPUTATION_BEFORE", ScriptName.OnDMGetFactionReputationBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_FACTION_REPUTATION_AFTER", ScriptName.OnDMGetFactionReputationAfter);

            // Client Disconnect events
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_DISCONNECT_BEFORE", ScriptName.OnClientDisconnectBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_DISCONNECT_AFTER", ScriptName.OnClientDisconnectAfter);

            // Client Connect events
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_CONNECT_BEFORE", ScriptName.OnClientConnectBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_CONNECT_AFTER", ScriptName.OnClientConnectAfter);

            // Combat Round Start events
            EventsPlugin.SubscribeEvent("NWNX_ON_START_COMBAT_ROUND_BEFORE", ScriptName.OnStartCombatRoundBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_START_COMBAT_ROUND_AFTER", ScriptName.OnStartCombatRoundAfter);

            // Cast Spell events
            EventsPlugin.SubscribeEvent("NWNX_ON_CAST_SPELL_BEFORE", ScriptName.OnCastSpellBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_CAST_SPELL_AFTER", ScriptName.OnCastSpellAfter);

            // Set Memorized Spell Slot events
            EventsPlugin.SubscribeEvent("NWNX_SET_MEMORIZED_SPELL_SLOT_BEFORE", ScriptName.OnSetMemorizedSpellSlotBefore);
            EventsPlugin.SubscribeEvent("NWNX_SET_MEMORIZED_SPELL_SLOT_AFTER", ScriptName.OnSetMemorizedSpellSlotAfter);

            // Clear Memorized Spell Slot events
            EventsPlugin.SubscribeEvent("NWNX_CLEAR_MEMORIZED_SPELL_SLOT_BEFORE", ScriptName.OnClearMemorizedSpellSlotBefore);
            EventsPlugin.SubscribeEvent("NWNX_CLEAR_MEMORIZED_SPELL_SLOT_AFTER", ScriptName.OnClearMemorizedSpellSlotAfter);

            // Healer Kit Use events
            EventsPlugin.SubscribeEvent("NWNX_ON_HEALER_KIT_BEFORE", ScriptName.OnHealerKitBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_HEALER_KIT_AFTER", ScriptName.OnHealerKitAfter);

            // Healing events
            EventsPlugin.SubscribeEvent("NWNX_ON_HEAL_BEFORE", ScriptName.OnHealBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_HEAL_AFTER", ScriptName.OnHealAfter);

            // Party Action events
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_LEAVE_BEFORE", ScriptName.OnPartyLeaveBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_LEAVE_AFTER", ScriptName.OnPartyLeaveAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_BEFORE", ScriptName.OnPartyKickBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_AFTER", ScriptName.OnPartyKickAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_TRANSFER_LEADERSHIP_BEFORE", ScriptName.OnPartyTransferLeadershipBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_TRANSFER_LEADERSHIP_AFTER", ScriptName.OnPartyTransferLeadershipAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_INVITE_BEFORE", ScriptName.OnPartyInviteBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_INVITE_AFTER", ScriptName.OnPartyInviteAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_IGNORE_INVITATION_BEFORE", ScriptName.OnPartyIgnoreInvitationBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_IGNORE_INVITATION_AFTER", ScriptName.OnPartyIgnoreInvitationAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_ACCEPT_INVITATION_BEFORE", ScriptName.OnPartyAcceptInvitationBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_ACCEPT_INVITATION_AFTER", ScriptName.OnPartyAcceptInvitationAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_REJECT_INVITATION_BEFORE", ScriptName.OnPartyRejectInvitationBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_REJECT_INVITATION_AFTER", ScriptName.OnPartyRejectInvitationAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_HENCHMAN_BEFORE", ScriptName.OnPartyKickHenchmanBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_HENCHMAN_AFTER", ScriptName.OnPartyKickHenchmanAfter);

            // Combat Mode Toggle events
            EventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_MODE_ON", ScriptName.OnCombatModeOn);
            EventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_MODE_OFF", ScriptName.OnCombatModeOff);

            // Use Skill events
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_SKILL_BEFORE", ScriptName.OnUseSkillBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_USE_SKILL_AFTER", ScriptName.OnUseSkillAfter);

            // Map Pin events
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_ADD_PIN_BEFORE", ScriptName.OnMapPinAddPinBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_ADD_PIN_AFTER", ScriptName.OnMapPinAddPinAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_CHANGE_PIN_BEFORE", ScriptName.OnMapPinChangePinBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_CHANGE_PIN_AFTER", ScriptName.OnMapPinChangePinAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_DESTROY_PIN_BEFORE", ScriptName.OnMapPinDestroyPinBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_DESTROY_PIN_AFTER", ScriptName.OnMapPinDestroyPinAfter);

            // Spot/Listen Detection events
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_LISTEN_DETECTION_BEFORE", ScriptName.OnDoListenDetectionBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_LISTEN_DETECTION_AFTER", ScriptName.OnDoListenDetectionAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_SPOT_DETECTION_BEFORE", ScriptName.OnDoSpotDetectionBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DO_SPOT_DETECTION_AFTER", ScriptName.OnDoSpotDetectionAfter);

            // Polymorph events
            EventsPlugin.SubscribeEvent("NWNX_ON_POLYMORPH_BEFORE", ScriptName.OnPolymorphBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_POLYMORPH_AFTER", ScriptName.OnPolymorphAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_UNPOLYMORPH_BEFORE", ScriptName.OnUnpolymorphBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_UNPOLYMORPH_AFTER", ScriptName.OnUnpolymorphAfter);

            // Effect Applied/Removed events
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_APPLIED_BEFORE", ScriptName.OnEffectAppliedBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_APPLIED_AFTER", ScriptName.OnEffectAppliedAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_REMOVED_BEFORE", ScriptName.OnEffectRemovedBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_REMOVED_AFTER", ScriptName.OnEffectRemovedAfter);

            // Quickchat events
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKCHAT_BEFORE", ScriptName.OnQuickchatBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKCHAT_AFTER", ScriptName.OnQuickchatAfter);

            // Inventory Open events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_OPEN_BEFORE", ScriptName.OnInventoryOpenBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_OPEN_AFTER", ScriptName.OnInventoryOpenAfter);

            // Inventory Select Panel events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_SELECT_PANEL_BEFORE", ScriptName.OnInventorySelectPanelBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_SELECT_PANEL_AFTER", ScriptName.OnInventorySelectPanelAfter);

            // Barter Start events
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_START_BEFORE", ScriptName.OnBarterStartBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_START_AFTER", ScriptName.OnBarterStartAfter);

            // Barter End events
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_END_BEFORE", ScriptName.OnBarterEndBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_BARTER_END_AFTER", ScriptName.OnBarterEndAfter);

            // Trap events
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_DISARM_BEFORE", ScriptName.OnTrapDisarmBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_DISARM_AFTER", ScriptName.OnTrapDisarmAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_ENTER_BEFORE", ScriptName.OnTrapEnterBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_ENTER_AFTER", ScriptName.OnTrapEnterAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_EXAMINE_BEFORE", ScriptName.OnTrapExamineBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_EXAMINE_AFTER", ScriptName.OnTrapExamineAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_FLAG_BEFORE", ScriptName.OnTrapFlagBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_FLAG_AFTER", ScriptName.OnTrapFlagAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_RECOVER_BEFORE", ScriptName.OnTrapRecoverBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_RECOVER_AFTER", ScriptName.OnTrapRecoverAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_BEFORE", ScriptName.OnTrapSetBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_AFTER", ScriptName.OnTrapSetAfter);

            // Timing Bar events
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_START_BEFORE", ScriptName.OnTimingBarStartBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_START_AFTER", ScriptName.OnTimingBarStartAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_STOP_BEFORE", ScriptName.OnTimingBarStopBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_STOP_AFTER", ScriptName.OnTimingBarStopAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_CANCEL_BEFORE", ScriptName.OnTimingBarCancelBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_CANCEL_AFTER", ScriptName.OnTimingBarCancelAfter);

            // Webhook events
            EventsPlugin.SubscribeEvent("NWNX_ON_WEBHOOK_SUCCESS", ScriptName.OnWebhookSuccess);
            EventsPlugin.SubscribeEvent("NWNX_ON_WEBHOOK_FAILURE", ScriptName.OnWebhookFailure);

            // Servervault events
            EventsPlugin.SubscribeEvent("NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_BEFORE", ScriptName.OnCheckStickyPlayerNameReservedBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_AFTER", ScriptName.OnCheckStickyPlayerNameReservedAfter);

            // Levelling events
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_BEFORE", ScriptName.OnLevelUpBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AFTER", ScriptName.OnLevelUpAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AUTOMATIC_BEFORE", ScriptName.OnLevelUpAutomaticBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AUTOMATIC_AFTER", ScriptName.OnLevelUpAutomaticAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_DOWN_BEFORE", ScriptName.OnLevelDownBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_DOWN_AFTER", ScriptName.OnLevelDownAfter);

            // Container Change events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_ITEM_BEFORE", ScriptName.OnInventoryAddItemBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_ITEM_AFTER", ScriptName.OnInventoryAddItemAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_ITEM_BEFORE", ScriptName.OnInventoryRemoveItemBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_ITEM_AFTER", ScriptName.OnInventoryRemoveItemAfter);

            // Gold events
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_GOLD_BEFORE", ScriptName.OnInventoryAddGoldBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_GOLD_AFTER", ScriptName.OnInventoryAddGoldAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_GOLD_BEFORE", ScriptName.OnInventoryRemoveGoldBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_GOLD_AFTER", ScriptName.OnInventoryRemoveGoldAfter);

            // PVP Attitude Change events
            EventsPlugin.SubscribeEvent("NWNX_ON_PVP_ATTITUDE_CHANGE_BEFORE", ScriptName.OnPVPAttitudeChangeBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_PVP_ATTITUDE_CHANGE_AFTER", ScriptName.OnPVPAttitudeChangeAfter);

            // Input Walk To events
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_WALK_TO_WAYPOINT_BEFORE", ScriptName.OnInputWalkToWaypointBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_WALK_TO_WAYPOINT_AFTER", ScriptName.OnInputWalkToWaypointAfter);

            // Material Change events
            EventsPlugin.SubscribeEvent("NWNX_ON_MATERIALCHANGE_BEFORE", ScriptName.OnMaterialChangeBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_MATERIALCHANGE_AFTER", ScriptName.OnMaterialChangeAfter);

            // Input Attack events
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_ATTACK_OBJECT_BEFORE", ScriptName.OnInputAttackObjectBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_ATTACK_OBJECT_AFTER", ScriptName.OnInputAttackObjectAfter);

            // Input Force Move To events
            // NOTE: These events are disabled because they cause NWServer to crash when a player clicks to move anywhere.
            //Events.SubscribeEvent("NWNX_ON_INPUT_FORCE_MOVE_TO_OBJECT_BEFORE", "force_move_bef");
            //Events.SubscribeEvent("NWNX_ON_INPUT_FORCE_MOVE_TO_OBJECT_AFTER", "force_move_aft");

            // Object Lock events
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_LOCK_BEFORE", ScriptName.OnObjectLockBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_LOCK_AFTER", ScriptName.OnObjectLockAfter);

            // Object Unlock events
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_UNLOCK_BEFORE", ScriptName.OnObjectUnlockBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_UNLOCK_AFTER", ScriptName.OnObjectUnlockAfter);

            // UUID Collision events
            EventsPlugin.SubscribeEvent("NWNX_ON_UUID_COLLISION_BEFORE", ScriptName.OnUUIDCollisionBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_UUID_COLLISION_AFTER", ScriptName.OnUUIDCollisionAfter);

            // Resource events
            // NOTE: These events are disabled because they cause NWServer to crash when CTRL+C is pressed on a Docker server.
            //Events.SubscribeEvent("NWNX_ON_RESOURCE_ADDED", "resource_added");
            //Events.SubscribeEvent("NWNX_ON_RESOURCE_REMOVED", "resource_removed");
            //Events.SubscribeEvent("NWNX_ON_RESOURCE_MODIFIED", "resource_modified");

            // ELC Events
            EventsPlugin.SubscribeEvent("NWNX_ON_ELC_VALIDATE_CHARACTER_BEFORE", ScriptName.OnELCValidateCharacterBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_ELC_VALIDATE_CHARACTER_AFTER", ScriptName.OnELCValidateCharacterAfter);

            // Quickbar Events
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKBAR_SET_BUTTON_BEFORE", ScriptName.OnQuickbarSetButtonBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_QUICKBAR_SET_BUTTON_AFTER", ScriptName.OnQuickbarSetButtonAfter);

            // Calendar Events
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_HOUR", ScriptName.OnCalendarHour);
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DAY", ScriptName.OnCalendarDay);
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_MONTH", ScriptName.OnCalendarMonth);
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_YEAR", ScriptName.OnCalendarYear);
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DAWN", ScriptName.OnCalendarDawn);
            EventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DUSK", ScriptName.OnCalendarDusk);

            // Broadcast Spell Cast Events
            EventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_CAST_SPELL_BEFORE", ScriptName.OnBroadcastCastSpellBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_CAST_SPELL_AFTER", ScriptName.OnBroadcastCastSpellAfter);

            // RunScript Debug Events
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_BEFORE", ScriptName.OnDebugRunScriptBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_AFTER", ScriptName.OnDebugRunScriptAfter);

            // RunScriptChunk Debug Events
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_CHUNK_BEFORE", ScriptName.OnDebugRunScriptChunkBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_CHUNK_AFTER", ScriptName.OnDebugRunScriptChunkAfter);

            // Buy/Sell Store Events
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_BUY_BEFORE", ScriptName.OnStoreRequestBuyBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_BUY_AFTER", ScriptName.OnStoreRequestBuyAfter);
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_SELL_BEFORE", ScriptName.OnStoreRequestSellBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_SELL_AFTER", ScriptName.OnStoreRequestSellAfter);

            // Input Drop Item Events
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_DROP_ITEM_BEFORE", ScriptName.OnInputDropItemBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_INPUT_DROP_ITEM_AFTER", ScriptName.OnInputDropItemAfter);

            // Broadcast Attack of Opportunity Events
            EventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_ATTACK_OF_OPPORTUNITY_BEFORE", ScriptName.OnBroadcastAttackOfOpportunityBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_ATTACK_OF_OPPORTUNITY_AFTER", ScriptName.OnBroadcastAttackOfOpportunityAfter);

            // Combat Attack of Opportunity Events
            EventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_ATTACK_OF_OPPORTUNITY_BEFORE", ScriptName.OnCombatAttackOfOpportunityBefore);
            EventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_ATTACK_OF_OPPORTUNITY_AFTER", ScriptName.OnCombatAttackOfOpportunityAfter);
        }

        /// <summary>
        /// Hooks all application-specific scripts.
        /// </summary>
        private static void HookApplicationEvents()
        {
            // Application Shutdown events
            EventsPlugin.SubscribeEvent("APPLICATION_SHUTDOWN", ScriptName.OnApplicationShutdown);
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                EventsPlugin.SignalEvent("APPLICATION_SHUTDOWN", GetModule());
            };

            EventsPlugin.SubscribeEvent("SWLOR_ITEM_EQUIP_VALID_BEFORE", ScriptName.OnItemEquipValidBefore);
            EventsPlugin.SubscribeEvent("SWLOR_BUY_PERK", ScriptName.OnBuyPerk);
            EventsPlugin.SubscribeEvent("SWLOR_GAIN_SKILL_POINT", ScriptName.OnGainSkillPoint);
            EventsPlugin.SubscribeEvent("SWLOR_COMPLETE_QUEST", ScriptName.OnCompleteQuest);
            EventsPlugin.SubscribeEvent("SWLOR_CACHE_SKILLS_LOADED", ScriptName.OnCacheSkillsLoaded);
            EventsPlugin.SubscribeEvent("SWLOR_COMBAT_POINT_DISTRIBUTED", ScriptName.OnCombatPointDistributed);
            EventsPlugin.SubscribeEvent("SWLOR_SKILL_LOST_BY_DECAY", ScriptName.OnSkillLostByDecay);
            EventsPlugin.SubscribeEvent("SWLOR_DELETE_PROPERTY", ScriptName.OnDeleteProperty);

            Scheduler.ScheduleRepeating(() =>
            {
                ExecuteScript(ScriptName.OnSwlorHeartbeat, GetModule());
            }, TimeSpan.FromSeconds(6));
        }

        /// <summary>
        /// A handful of NWNX functions require special calls to load persistence.
        /// When the module loads, run those methods here.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void TriggerNWNXPersistence()
        {
            var firstObject = GetFirstObjectInArea(GetFirstArea());
            CreaturePlugin.SetCriticalRangeModifier(firstObject, 0, 0, true);
        }
    }
}
