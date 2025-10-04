using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Server;
using SWLOR.Shared.Events.Contracts;

namespace SWLOR.Shared.Events.Service
{
    public class EventRegistrationService : IEventRegistrationService
    {
        private readonly IScheduler _scheduler;
        private readonly IEventAggregator _eventAggregator;
        private readonly IEventHandlerDiscoveryService _eventHandlerDiscovery;
        private readonly IChatPluginService _chatPlugin;
        private readonly IEventsPluginService _eventsPlugin;

        public EventRegistrationService(
            IScheduler scheduler,
            IEventAggregator eventAggregator,
            IEventHandlerDiscoveryService eventHandlerDiscovery,
            IChatPluginService chatPlugin,
            IEventsPluginService eventsPlugin)
        {
            _scheduler = scheduler;
            _eventAggregator = eventAggregator;
            _eventHandlerDiscovery = eventHandlerDiscovery;
            _chatPlugin = chatPlugin;
            _eventsPlugin = eventsPlugin;
        }

        public void RegisterEvents()
        {

            Console.WriteLine("Discovering and registering event handlers.");
            _eventHandlerDiscovery.DiscoverAndRegisterHandlers();

            Console.WriteLine($"Hooking native overrides.");
            _eventAggregator.Publish(new OnHookNativeOverrides(), GetModule());

            Console.WriteLine("Hooking all module events.");
            HookModuleEvents();

            Console.WriteLine("Hooking all area events.");
            HookAreaEvents();

            Console.WriteLine("Hooking all NWNX events");
            HookNWNXEvents();

            Console.WriteLine("Hooking all application-specific events");
            HookApplicationEvents();

            _eventAggregator.Publish(new OnHookEvents(), GetModule());
        }

        /// <summary>
        /// Hooks module-wide scripts.
        /// </summary>
        private void HookModuleEvents()
        {
            var module = GetModule();

            SetEventScript(module, EventScriptType.Module_OnAcquireItem, ScriptName.OnEventingModuleAcquire);
            SetEventScript(module, EventScriptType.Module_OnActivateItem, ScriptName.OnEventingModuleActivate);
            SetEventScript(module, EventScriptType.Module_OnClientEnter, ScriptName.OnEventingModuleEnter);
            SetEventScript(module, EventScriptType.Module_OnClientExit, ScriptName.OnEventingModuleExit);
            SetEventScript(module, EventScriptType.Module_OnPlayerCancelCutscene, ScriptName.OnEventingModulePlayerCancelCutscene);
            SetEventScript(module, EventScriptType.Module_OnHeartbeat, ScriptName.OnEventingModuleHeartbeat);
            SetEventScript(module, EventScriptType.Module_OnModuleLoad, ScriptName.OnEventingModuleLoad);
            SetEventScript(module, EventScriptType.Module_OnPlayerChat, ScriptName.OnEventingModuleChat);
            SetEventScript(module, EventScriptType.Module_OnPlayerDying, ScriptName.OnEventingModuleDying);
            SetEventScript(module, EventScriptType.Module_OnPlayerDeath, ScriptName.OnEventingModuleDeath);
            SetEventScript(module, EventScriptType.Module_OnEquipItem, ScriptName.OnEventingModuleEquip);
            SetEventScript(module, EventScriptType.Module_OnPlayerLevelUp, ScriptName.OnEventingModuleLevelUp);
            SetEventScript(module, EventScriptType.Module_OnRespawnButtonPressed, ScriptName.OnEventingModuleRespawn);
            SetEventScript(module, EventScriptType.Module_OnPlayerRest, ScriptName.OnEventingModuleRest);
            SetEventScript(module, EventScriptType.Module_OnUnequipItem, ScriptName.OnEventingModuleUnequip);
            SetEventScript(module, EventScriptType.Module_OnLoseItem, ScriptName.OnEventingModuleUnacquire);
            SetEventScript(module, EventScriptType.Module_OnUserDefined, ScriptName.OnEventingModuleUserDefined);
            SetEventScript(module, EventScriptType.Module_OnPlayerTarget, ScriptName.OnEventingModulePlayerTarget);
            SetEventScript(module, EventScriptType.Module_OnPlayerGuiEvent, ScriptName.OnEventingModuleGuiEvent);
            SetEventScript(module, EventScriptType.Module_OnPlayerTileEvent, ScriptName.OnEventingModuleTileEvent);
            SetEventScript(module, EventScriptType.Module_OnNuiEvent, ScriptName.OnEventingModuleNuiEvent);
        }

        /// <summary>
        /// Hooks area-wide scripts.
        /// </summary>
        private void HookAreaEvents()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                SetEventScript(area, EventScriptType.Area_OnEnter, ScriptName.OnAreaEnter);
                SetEventScript(area, EventScriptType.Area_OnExit, ScriptName.OnAreaExit);
                SetEventScript(area, EventScriptType.Area_OnHeartbeat, string.Empty); // Disabled for performance reasons
                SetEventScript(area, EventScriptType.Area_OnUserDefined, ScriptName.OnAreaUserDefined);
            }
        }

        /// <summary>
        /// Hooks NWNX scripts.
        /// </summary>
        private void HookNWNXEvents()
        {
            // Chat Plugin Events start here.
            _chatPlugin.RegisterChatScript(ScriptName.OnNWNXChat);

            // Events Plugin Events start here.

            // Associate events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ADD_ASSOCIATE_BEFORE", ScriptName.OnAssociateAddBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ADD_ASSOCIATE_AFTER", ScriptName.OnAssociateAddAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_REMOVE_ASSOCIATE_BEFORE", ScriptName.OnAssociateRemoveBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_REMOVE_ASSOCIATE_AFTER", ScriptName.OnAssociateRemoveAfter);

            // Stealth events
            _eventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_ENTER_BEFORE", ScriptName.OnStealthEnterBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_ENTER_AFTER", ScriptName.OnStealthEnterAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_EXIT_BEFORE", ScriptName.OnStealthExitBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_STEALTH_EXIT_AFTER", ScriptName.OnStealthExitAfter);

            // Examine events
            _eventsPlugin.SubscribeEvent("NWNX_ON_EXAMINE_OBJECT_BEFORE", ScriptName.OnExamineObjectBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_EXAMINE_OBJECT_AFTER", ScriptName.OnExamineObjectAfter);

            // Validate Use Item events
            _eventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_USE_ITEM_BEFORE", ScriptName.OnValidateUseItemBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_USE_ITEM_AFTER", ScriptName.OnValidateUseItemAfter);

            // Use Item events
            _eventsPlugin.SubscribeEvent("NWNX_ON_USE_ITEM_BEFORE", ScriptName.OnUseItemBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_USE_ITEM_AFTER", ScriptName.OnUseItemAfter);

            // Item Container events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_OPEN_BEFORE", ScriptName.OnInventoryOpenBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_OPEN_AFTER", ScriptName.OnInventoryOpenAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_CLOSE_BEFORE", ScriptName.OnItemInventoryCloseBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_INVENTORY_CLOSE_AFTER", ScriptName.OnItemInventoryCloseAfter);

            // Ammunition Reload events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_AMMO_RELOAD_BEFORE", ScriptName.OnItemAmmoReloadBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_AMMO_RELOAD_AFTER", ScriptName.OnItemAmmoReloadAfter);

            // Scroll Learn events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SCROLL_LEARN_BEFORE", ScriptName.OnItemScrollLearnBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SCROLL_LEARN_AFTER", ScriptName.OnItemScrollLearnAfter);

            // Validate Item Equip events
            _eventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_ITEM_EQUIP_BEFORE", ScriptName.OnValidateItemEquipBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_VALIDATE_ITEM_EQUIP_AFTER", ScriptName.OnValidateItemEquipAfter);

            // Item Equip events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_EQUIP_BEFORE", ScriptName.OnItemEquipValidateBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_EQUIP_AFTER", ScriptName.OnItemEquipValidateAfter);

            // Item Unequip events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_UNEQUIP_BEFORE", ScriptName.OnItemUnequipBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_UNEQUIP_AFTER", ScriptName.OnItemUnequipAfter);

            // Item Destroy events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DESTROY_OBJECT_BEFORE", ScriptName.OnItemDestroyObjectBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DESTROY_OBJECT_AFTER", ScriptName.OnItemDestroyObjectAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DECREMENT_STACKSIZE_BEFORE", ScriptName.OnItemDecrementStackSizeBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_DECREMENT_STACKSIZE_AFTER", ScriptName.OnItemDecrementStackSizeAfter);

            // Item Use Lore to Identify events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_USE_LORE_BEFORE", ScriptName.OnItemUseLoreBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_USE_LORE_AFTER", ScriptName.OnItemUseLoreAfter);

            // Item Pay to Identify events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_PAY_TO_IDENTIFY_BEFORE", ScriptName.OnItemPayToIdentifyBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_PAY_TO_IDENTIFY_AFTER", ScriptName.OnItemPayToIdentifyAfter);

            // Item Split events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SPLIT_BEFORE", ScriptName.OnItemSplitBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_SPLIT_AFTER", ScriptName.OnItemSplitAfter);

            // Item Merge events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_MERGE_BEFORE", ScriptName.OnItemMergeBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_MERGE_AFTER", ScriptName.OnItemMergeAfter);

            // Acquire Item events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_ACQUIRE_BEFORE", ScriptName.OnItemAcquireBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ITEM_ACQUIRE_AFTER", ScriptName.OnItemAcquireAfter);

            // Feat Use events
            _eventsPlugin.SubscribeEvent("NWNX_ON_USE_FEAT_BEFORE", ScriptName.OnUseFeatBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_USE_FEAT_AFTER", ScriptName.OnUseFeatAfter);

            // DM Give events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_GOLD_BEFORE", ScriptName.OnDMGiveGoldBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_GOLD_AFTER", ScriptName.OnDMGiveGoldAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_XP_BEFORE", ScriptName.OnDMGiveXPBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_XP_AFTER", ScriptName.OnDMGiveXPAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_LEVEL_BEFORE", ScriptName.OnDMGiveLevelBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_LEVEL_AFTER", ScriptName.OnDMGiveLevelAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ALIGNMENT_BEFORE", ScriptName.OnDMGiveAlignmentBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ALIGNMENT_AFTER", ScriptName.OnDMGiveAlignmentAfter);

            // DM Spawn events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_OBJECT_BEFORE", ScriptName.OnDMSpawnObjectBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_OBJECT_AFTER", ScriptName.OnDMSpawnObjectAfter);

            // DM Give Item events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ITEM_BEFORE", ScriptName.OnDMGiveItemBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GIVE_ITEM_AFTER", ScriptName.OnDMGiveItemAfter);

            // DM Multiple Object Action events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_HEAL_BEFORE", ScriptName.OnDMHealBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_HEAL_AFTER", ScriptName.OnDMHealAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_KILL_BEFORE", ScriptName.OnDMKillBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_KILL_AFTER", ScriptName.OnDMKillAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_INVULNERABLE_BEFORE", ScriptName.OnDMToggleInvulnerableBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_INVULNERABLE_AFTER", ScriptName.OnDMToggleInvulnerableAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_FORCE_REST_BEFORE", ScriptName.OnDMForceRestBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_FORCE_REST_AFTER", ScriptName.OnDMForceRestAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_LIMBO_BEFORE", ScriptName.OnDMLimboBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_LIMBO_AFTER", ScriptName.OnDMLimboAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_AI_BEFORE", ScriptName.OnDMToggleAIBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_AI_AFTER", ScriptName.OnDMToggleAIAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_IMMORTAL_BEFORE", ScriptName.OnDMToggleImmortalBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_IMMORTAL_AFTER", ScriptName.OnDMToggleImmortalAfter);

            // DM Single Object Action events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GOTO_BEFORE", ScriptName.OnDMGotoBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GOTO_AFTER", ScriptName.OnDMGotoAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_BEFORE", ScriptName.OnDMPossessBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_AFTER", ScriptName.OnDMPossessAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_FULL_POWER_BEFORE", ScriptName.OnDMPossessFullPowerBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_POSSESS_FULL_POWER_AFTER", ScriptName.OnDMPossessFullPowerAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_LOCK_BEFORE", ScriptName.OnDMToggleLockBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TOGGLE_LOCK_AFTER", ScriptName.OnDMToggleLockAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_DISABLE_TRAP_BEFORE", ScriptName.OnDMDisableTrapBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_DISABLE_TRAP_AFTER", ScriptName.OnDMDisableTrapAfter);

            // DM Jump events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TO_POINT_BEFORE", ScriptName.OnDMJumpToPointBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TO_POINT_AFTER", ScriptName.OnDMJumpToPointAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TARGET_TO_POINT_BEFORE", ScriptName.OnDMJumpTargetToPointBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_TARGET_TO_POINT_AFTER", ScriptName.OnDMJumpTargetToPointAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_BEFORE", ScriptName.OnDMJumpAllPlayersToPointBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_JUMP_ALL_PLAYERS_TO_POINT_AFTER", ScriptName.OnDMJumpAllPlayersToPointAfter);

            // DM Change Difficulty events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_CHANGE_DIFFICULTY_BEFORE", ScriptName.OnDMChangeDifficultyBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_CHANGE_DIFFICULTY_AFTER", ScriptName.OnDMChangeDifficultyAfter);

            // DM View Inventory events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_VIEW_INVENTORY_BEFORE", ScriptName.OnDMViewInventoryBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_VIEW_INVENTORY_AFTER", ScriptName.OnDMViewInventoryAfter);

            // DM Spawn Trap events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_BEFORE", ScriptName.OnDMSpawnTrapOnObjectBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SPAWN_TRAP_ON_OBJECT_AFTER", ScriptName.OnDMSpawnTrapOnObjectAfter);

            // DM Dump Locals events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_DUMP_LOCALS_BEFORE", ScriptName.OnDMDumpLocalsBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_DUMP_LOCALS_AFTER", ScriptName.OnDMDumpLocalsAfter);

            // DM Other events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_APPEAR_BEFORE", ScriptName.OnDMAppearBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_APPEAR_AFTER", ScriptName.OnDMAppearAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_DISAPPEAR_BEFORE", ScriptName.OnDMDisappearBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_DISAPPEAR_AFTER", ScriptName.OnDMDisappearAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_BEFORE", ScriptName.OnDMSetFactionBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_AFTER", ScriptName.OnDMSetFactionAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TAKE_ITEM_BEFORE", ScriptName.OnDMTakeItemBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_TAKE_ITEM_AFTER", ScriptName.OnDMTakeItemAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_STAT_BEFORE", ScriptName.OnDMSetStatBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_STAT_AFTER", ScriptName.OnDMSetStatAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_VARIABLE_BEFORE", ScriptName.OnDMGetVariableBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_VARIABLE_AFTER", ScriptName.OnDMGetVariableAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_VARIABLE_BEFORE", ScriptName.OnDMSetVariableBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_VARIABLE_AFTER", ScriptName.OnDMSetVariableAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_TIME_BEFORE", ScriptName.OnDMSetTimeBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_TIME_AFTER", ScriptName.OnDMSetTimeAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_DATE_BEFORE", ScriptName.OnDMSetDateBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_DATE_AFTER", ScriptName.OnDMSetDateAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_REPUTATION_BEFORE", ScriptName.OnDMSetFactionReputationBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_SET_FACTION_REPUTATION_AFTER", ScriptName.OnDMSetFactionReputationAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_FACTION_REPUTATION_BEFORE", ScriptName.OnDMGetFactionReputationBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DM_GET_FACTION_REPUTATION_AFTER", ScriptName.OnDMGetFactionReputationAfter);

            // Client Disconnect events
            _eventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_DISCONNECT_BEFORE", ScriptName.OnClientDisconnectBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_DISCONNECT_AFTER", ScriptName.OnClientDisconnectAfter);

            // Client Connect events
            _eventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_CONNECT_BEFORE", ScriptName.OnClientConnectBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CLIENT_CONNECT_AFTER", ScriptName.OnClientConnectAfter);

            // Combat Round Start events
            _eventsPlugin.SubscribeEvent("NWNX_ON_START_COMBAT_ROUND_BEFORE", ScriptName.OnStartCombatRoundBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_START_COMBAT_ROUND_AFTER", ScriptName.OnStartCombatRoundAfter);

            // Cast Spell events
            _eventsPlugin.SubscribeEvent("NWNX_ON_CAST_SPELL_BEFORE", ScriptName.OnCastSpellBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CAST_SPELL_AFTER", ScriptName.OnCastSpellAfter);

            // Set Memorized Spell Slot events
            _eventsPlugin.SubscribeEvent("NWNX_SET_MEMORIZED_SPELL_SLOT_BEFORE", ScriptName.OnSetMemorizedSpellSlotBefore);
            _eventsPlugin.SubscribeEvent("NWNX_SET_MEMORIZED_SPELL_SLOT_AFTER", ScriptName.OnSetMemorizedSpellSlotAfter);

            // Clear Memorized Spell Slot events
            _eventsPlugin.SubscribeEvent("NWNX_CLEAR_MEMORIZED_SPELL_SLOT_BEFORE", ScriptName.OnClearMemorizedSpellSlotBefore);
            _eventsPlugin.SubscribeEvent("NWNX_CLEAR_MEMORIZED_SPELL_SLOT_AFTER", ScriptName.OnClearMemorizedSpellSlotAfter);

            // Healer Kit Use events
            _eventsPlugin.SubscribeEvent("NWNX_ON_HEALER_KIT_BEFORE", ScriptName.OnHealerKitBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_HEALER_KIT_AFTER", ScriptName.OnHealerKitAfter);

            // Healing events
            _eventsPlugin.SubscribeEvent("NWNX_ON_HEAL_BEFORE", ScriptName.OnHealBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_HEAL_AFTER", ScriptName.OnHealAfter);

            // Party Action events
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_LEAVE_BEFORE", ScriptName.OnPartyLeaveBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_LEAVE_AFTER", ScriptName.OnPartyLeaveAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_BEFORE", ScriptName.OnPartyKickBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_AFTER", ScriptName.OnPartyKickAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_TRANSFER_LEADERSHIP_BEFORE", ScriptName.OnPartyTransferLeadershipBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_TRANSFER_LEADERSHIP_AFTER", ScriptName.OnPartyTransferLeadershipAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_INVITE_BEFORE", ScriptName.OnPartyInviteBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_INVITE_AFTER", ScriptName.OnPartyInviteAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_IGNORE_INVITATION_BEFORE", ScriptName.OnPartyIgnoreInvitationBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_IGNORE_INVITATION_AFTER", ScriptName.OnPartyIgnoreInvitationAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_ACCEPT_INVITATION_BEFORE", ScriptName.OnPartyAcceptInvitationBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_ACCEPT_INVITATION_AFTER", ScriptName.OnPartyAcceptInvitationAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_REJECT_INVITATION_BEFORE", ScriptName.OnPartyRejectInvitationBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_REJECT_INVITATION_AFTER", ScriptName.OnPartyRejectInvitationAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_HENCHMAN_BEFORE", ScriptName.OnPartyKickHenchmanBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PARTY_KICK_HENCHMAN_AFTER", ScriptName.OnPartyKickHenchmanAfter);

            // Combat Mode Toggle events
            _eventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_MODE_ON", ScriptName.OnCombatModeOn);
            _eventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_MODE_OFF", ScriptName.OnCombatModeOff);

            // Use Skill events
            _eventsPlugin.SubscribeEvent("NWNX_ON_USE_SKILL_BEFORE", ScriptName.OnUseSkillBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_USE_SKILL_AFTER", ScriptName.OnUseSkillAfter);

            // Map Pin events
            _eventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_ADD_PIN_BEFORE", ScriptName.OnMapPinAddPinBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_ADD_PIN_AFTER", ScriptName.OnMapPinAddPinAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_CHANGE_PIN_BEFORE", ScriptName.OnMapPinChangePinBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_CHANGE_PIN_AFTER", ScriptName.OnMapPinChangePinAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_DESTROY_PIN_BEFORE", ScriptName.OnMapPinDestroyPinBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_MAP_PIN_DESTROY_PIN_AFTER", ScriptName.OnMapPinDestroyPinAfter);

            // Spot/Listen Detection events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DO_LISTEN_DETECTION_BEFORE", ScriptName.OnDoListenDetectionBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DO_LISTEN_DETECTION_AFTER", ScriptName.OnDoListenDetectionAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DO_SPOT_DETECTION_BEFORE", ScriptName.OnDoSpotDetectionBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DO_SPOT_DETECTION_AFTER", ScriptName.OnDoSpotDetectionAfter);

            // Polymorph events
            _eventsPlugin.SubscribeEvent("NWNX_ON_POLYMORPH_BEFORE", ScriptName.OnPolymorphBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_POLYMORPH_AFTER", ScriptName.OnPolymorphAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_UNPOLYMORPH_BEFORE", ScriptName.OnUnpolymorphBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_UNPOLYMORPH_AFTER", ScriptName.OnUnpolymorphAfter);

            // Effect Applied/Removed events
            _eventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_APPLIED_BEFORE", ScriptName.OnEffectAppliedBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_APPLIED_AFTER", ScriptName.OnEffectAppliedAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_REMOVED_BEFORE", ScriptName.OnEffectRemovedBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_EFFECT_REMOVED_AFTER", ScriptName.OnEffectRemovedAfter);

            // Quickchat events
            _eventsPlugin.SubscribeEvent("NWNX_ON_QUICKCHAT_BEFORE", ScriptName.OnQuickchatBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_QUICKCHAT_AFTER", ScriptName.OnQuickchatAfter);

            // Inventory Open events
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_OPEN_BEFORE", ScriptName.OnInventoryOpenBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_OPEN_AFTER", ScriptName.OnInventoryOpenAfter);

            // Inventory Select Panel events
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_SELECT_PANEL_BEFORE", ScriptName.OnInventorySelectPanelBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_SELECT_PANEL_AFTER", ScriptName.OnInventorySelectPanelAfter);

            // Barter Start events
            _eventsPlugin.SubscribeEvent("NWNX_ON_BARTER_START_BEFORE", ScriptName.OnBarterStartBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_BARTER_START_AFTER", ScriptName.OnBarterStartAfter);

            // Barter End events
            _eventsPlugin.SubscribeEvent("NWNX_ON_BARTER_END_BEFORE", ScriptName.OnBarterEndBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_BARTER_END_AFTER", ScriptName.OnBarterEndAfter);

            // Trap events
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_DISARM_BEFORE", ScriptName.OnTrapDisarmBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_DISARM_AFTER", ScriptName.OnTrapDisarmAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_ENTER_BEFORE", ScriptName.OnTrapEnterBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_ENTER_AFTER", ScriptName.OnTrapEnterAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_EXAMINE_BEFORE", ScriptName.OnTrapExamineBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_EXAMINE_AFTER", ScriptName.OnTrapExamineAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_FLAG_BEFORE", ScriptName.OnTrapFlagBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_FLAG_AFTER", ScriptName.OnTrapFlagAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_RECOVER_BEFORE", ScriptName.OnTrapRecoverBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_RECOVER_AFTER", ScriptName.OnTrapRecoverAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_BEFORE", ScriptName.OnTrapSetBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TRAP_SET_AFTER", ScriptName.OnTrapSetAfter);

            // Timing Bar events
            _eventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_START_BEFORE", ScriptName.OnTimingBarStartBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_START_AFTER", ScriptName.OnTimingBarStartAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_STOP_BEFORE", ScriptName.OnTimingBarStopBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_STOP_AFTER", ScriptName.OnTimingBarStopAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_CANCEL_BEFORE", ScriptName.OnTimingBarCancelBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_TIMING_BAR_CANCEL_AFTER", ScriptName.OnTimingBarCancelAfter);

            // Webhook events
            _eventsPlugin.SubscribeEvent("NWNX_ON_WEBHOOK_SUCCESS", ScriptName.OnWebhookSuccess);
            _eventsPlugin.SubscribeEvent("NWNX_ON_WEBHOOK_FAILURE", ScriptName.OnWebhookFailure);

            // Servervault events
            _eventsPlugin.SubscribeEvent("NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_BEFORE", ScriptName.OnCheckStickyPlayerNameReservedBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CHECK_STICKY_PLAYER_NAME_RESERVED_AFTER", ScriptName.OnCheckStickyPlayerNameReservedAfter);

            // Levelling events
            _eventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_BEFORE", ScriptName.OnLevelUpBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AFTER", ScriptName.OnLevelUpAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AUTOMATIC_BEFORE", ScriptName.OnLevelUpAutomaticBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_UP_AUTOMATIC_AFTER", ScriptName.OnLevelUpAutomaticAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_DOWN_BEFORE", ScriptName.OnLevelDownBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_LEVEL_DOWN_AFTER", ScriptName.OnLevelDownAfter);

            // Container Change events
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_ITEM_BEFORE", ScriptName.OnInventoryAddItemBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_ITEM_AFTER", ScriptName.OnInventoryAddItemAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_ITEM_BEFORE", ScriptName.OnInventoryRemoveItemBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_ITEM_AFTER", ScriptName.OnInventoryRemoveItemAfter);

            // Gold events
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_GOLD_BEFORE", ScriptName.OnInventoryAddGoldBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_ADD_GOLD_AFTER", ScriptName.OnInventoryAddGoldAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_GOLD_BEFORE", ScriptName.OnInventoryRemoveGoldBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INVENTORY_REMOVE_GOLD_AFTER", ScriptName.OnInventoryRemoveGoldAfter);

            // PVP Attitude Change events
            _eventsPlugin.SubscribeEvent("NWNX_ON_PVP_ATTITUDE_CHANGE_BEFORE", ScriptName.OnPVPAttitudeChangeBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_PVP_ATTITUDE_CHANGE_AFTER", ScriptName.OnPVPAttitudeChangeAfter);

            // Input Walk To events
            _eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_WALK_TO_WAYPOINT_BEFORE", ScriptName.OnInputWalkToWaypointBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_WALK_TO_WAYPOINT_AFTER", ScriptName.OnInputWalkToWaypointAfter);

            // Material Change events
            _eventsPlugin.SubscribeEvent("NWNX_ON_MATERIALCHANGE_BEFORE", ScriptName.OnMaterialChangeBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_MATERIALCHANGE_AFTER", ScriptName.OnMaterialChangeAfter);

            // Input Attack events
            _eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_ATTACK_OBJECT_BEFORE", ScriptName.OnInputAttackObjectBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_ATTACK_OBJECT_AFTER", ScriptName.OnInputAttackObjectAfter);

            // Input Force Move To events
            // NOTE: These events are disabled because they cause NWServer to crash when a player clicks to move anywhere.
            //_eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_FORCE_MOVE_TO_OBJECT_BEFORE", ScriptName.OnInputForceMoveToObjectBefore);
            //_eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_FORCE_MOVE_TO_OBJECT_AFTER", ScriptName.OnInputForceMoveToObjectAfter);

            // Object Lock events
            _eventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_LOCK_BEFORE", ScriptName.OnObjectLockBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_LOCK_AFTER", ScriptName.OnObjectLockAfter);

            // Object Unlock events
            _eventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_UNLOCK_BEFORE", ScriptName.OnObjectUnlockBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_OBJECT_UNLOCK_AFTER", ScriptName.OnObjectUnlockAfter);

            // UUID Collision events
            _eventsPlugin.SubscribeEvent("NWNX_ON_UUID_COLLISION_BEFORE", ScriptName.OnUUIDCollisionBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_UUID_COLLISION_AFTER", ScriptName.OnUUIDCollisionAfter);

            // Resource events
            // NOTE: These events are disabled because they cause NWServer to crash when CTRL+C is pressed on a Docker server.
            //_eventsPlugin.SubscribeEvent("NWNX_ON_RESOURCE_ADDED", ScriptName.OnResourceAdded);
            //_eventsPlugin.SubscribeEvent("NWNX_ON_RESOURCE_REMOVED", ScriptName.OnResourceRemoved);
            //_eventsPlugin.SubscribeEvent("NWNX_ON_RESOURCE_MODIFIED", ScriptName.OnResourceModified);

            // ELC Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_ELC_VALIDATE_CHARACTER_BEFORE", ScriptName.OnELCValidateCharacterBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_ELC_VALIDATE_CHARACTER_AFTER", ScriptName.OnELCValidateCharacterAfter);

            // Quickbar Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_QUICKBAR_SET_BUTTON_BEFORE", ScriptName.OnQuickbarSetButtonBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_QUICKBAR_SET_BUTTON_AFTER", ScriptName.OnQuickbarSetButtonAfter);

            // Calendar Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_HOUR", ScriptName.OnCalendarHour);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DAY", ScriptName.OnCalendarDay);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_MONTH", ScriptName.OnCalendarMonth);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_YEAR", ScriptName.OnCalendarYear);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DAWN", ScriptName.OnCalendarDawn);
            _eventsPlugin.SubscribeEvent("NWNX_ON_CALENDAR_DUSK", ScriptName.OnCalendarDusk);

            // Broadcast Spell Cast Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_CAST_SPELL_BEFORE", ScriptName.OnBroadcastCastSpellBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_CAST_SPELL_AFTER", ScriptName.OnBroadcastCastSpellAfter);

            // RunScript Debug Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_BEFORE", ScriptName.OnDebugRunScriptBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_AFTER", ScriptName.OnDebugRunScriptAfter);

            // RunScriptChunk Debug Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_CHUNK_BEFORE", ScriptName.OnDebugRunScriptChunkBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_DEBUG_RUN_SCRIPT_CHUNK_AFTER", ScriptName.OnDebugRunScriptChunkAfter);

            // Buy/Sell Store Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_BUY_BEFORE", ScriptName.OnStoreRequestBuyBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_BUY_AFTER", ScriptName.OnStoreRequestBuyAfter);
            _eventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_SELL_BEFORE", ScriptName.OnStoreRequestSellBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_STORE_REQUEST_SELL_AFTER", ScriptName.OnStoreRequestSellAfter);

            // Input Drop Item Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_DROP_ITEM_BEFORE", ScriptName.OnInputDropItemBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_INPUT_DROP_ITEM_AFTER", ScriptName.OnInputDropItemAfter);

            // Broadcast Attack of Opportunity Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_ATTACK_OF_OPPORTUNITY_BEFORE", ScriptName.OnBroadcastAttackOfOpportunityBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_BROADCAST_ATTACK_OF_OPPORTUNITY_AFTER", ScriptName.OnBroadcastAttackOfOpportunityAfter);

            // Combat Attack of Opportunity Events
            _eventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_ATTACK_OF_OPPORTUNITY_BEFORE", ScriptName.OnCombatAttackOfOpportunityBefore);
            _eventsPlugin.SubscribeEvent("NWNX_ON_COMBAT_ATTACK_OF_OPPORTUNITY_AFTER", ScriptName.OnCombatAttackOfOpportunityAfter);
        }

        /// <summary>
        /// Hooks all application-specific scripts.
        /// </summary>
        private void HookApplicationEvents()
        {
            // Application Shutdown events
            _eventsPlugin.SubscribeEvent("APPLICATION_SHUTDOWN", ScriptName.OnSWLORApplicationShutdown);
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                _eventsPlugin.SignalEvent("APPLICATION_SHUTDOWN", GetModule());
            };

            _eventsPlugin.SubscribeEvent("SWLOR_ITEM_EQUIP_VALID_BEFORE", ScriptName.OnSWLORItemEquipValidBefore);
            _eventsPlugin.SubscribeEvent("SWLOR_BUY_PERK", ScriptName.OnSWLORBuyPerk);
            _eventsPlugin.SubscribeEvent("SWLOR_GAIN_SKILL_POINT", ScriptName.OnSWLORGainSkillPoint);
            _eventsPlugin.SubscribeEvent("SWLOR_COMPLETE_QUEST", ScriptName.OnSWLORCompleteQuest);
            _eventsPlugin.SubscribeEvent("SWLOR_CACHE_SKILLS_LOADED", ScriptName.OnSWLORCacheSkillsLoaded);
            _eventsPlugin.SubscribeEvent("SWLOR_COMBAT_POINT_DISTRIBUTED", ScriptName.OnSWLORCombatPointDistributed);
            _eventsPlugin.SubscribeEvent("SWLOR_SKILL_LOST_BY_DECAY", ScriptName.OnSWLORSkillLostByDecay);
            _eventsPlugin.SubscribeEvent("SWLOR_DELETE_PROPERTY", ScriptName.OnSWLORDeleteProperty);

            _scheduler.ScheduleRepeating(() =>
            {
                _eventAggregator.Publish(new OnServerHeartbeat(), GetModule());
            }, TimeSpan.FromSeconds(6));
        }

    }
}
