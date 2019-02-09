using System;
using SWLOR.Game.Server.Enumeration;

using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleLoad: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly INWNXChat _nwnxChat;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IObjectProcessingService _objectProcessing;
        private readonly IFarmingService _farming;
        private readonly INWNXDamage _nwnxDamage;
        private readonly IAreaService _area;
        private readonly IBaseService _base;
        private readonly ISpawnService _spawn;
        private readonly ICustomEffectService _customEffect;
        private readonly IObjectVisibilityService _objectVisibility;
        private readonly IBackgroundThreadManager _backgroundThreadManager;
        private readonly IDataPackageService _dataPackage;
        private readonly INWNXWeapon _nwnxWeapon;
        private readonly IErrorService _error;

        public OnModuleLoad(
            INWScript script,
            INWNXChat nwnxChat,
            INWNXEvents nwnxEvents,
            IObjectProcessingService objectProcessing,
            IFarmingService farming,
            INWNXDamage nwnxDamage,
            IAreaService area,
            IBaseService @base,
            ISpawnService spawn,
            ICustomEffectService customEffect,
            IObjectVisibilityService objectVisibility,
            IBackgroundThreadManager backgroundThreadManager,
            IDataPackageService dataPackage,
            INWNXWeapon nwnxWeapon,
            IErrorService error)
        {
            _ = script;
            _nwnxChat = nwnxChat;
            _nwnxEvents = nwnxEvents;
            _objectProcessing = objectProcessing;
            _farming = farming;
            _nwnxDamage = nwnxDamage;
            _area = area;
            _base = @base;
            _spawn = spawn;
            _customEffect = customEffect;
            _objectVisibility = objectVisibility;
            _backgroundThreadManager = backgroundThreadManager;
            _dataPackage = dataPackage;
            _nwnxWeapon = nwnxWeapon;
            _error = error;
        }

        public bool Run(params object[] args)
        {
            string nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            Console.WriteLine(nowString + ": Module OnLoad executing...");
            
            Console.WriteLine("Starting background thread manager...");
            _backgroundThreadManager.Start();
            
            _nwnxChat.RegisterChatScript("mod_on_nwnxchat");
            SetModuleEventScripts();
            SetAreaEventScripts();
            SetWeaponSettings();

            // Bioware default
            _.ExecuteScript("x2_mod_def_load", Object.OBJECT_SELF);
            _objectProcessing.RegisterProcessingEvent<AppStateProcessor>();
            _objectProcessing.RegisterProcessingEvent<ServerRestartProcessor>();
            _objectProcessing.OnModuleLoad();
            _dataPackage.OnModuleLoad();
            _farming.OnModuleLoad();
            _base.OnModuleLoad();
            _area.OnModuleLoad();
            _spawn.OnModuleLoad();
            _customEffect.OnModuleLoad();
            _objectVisibility.OnModuleLoad();

            nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            Console.WriteLine(nowString + ": Module OnLoad finished!");
            
            return true;
        }

        private void SetAreaEventScripts()
        {
            Object area = _.GetFirstArea();
            while (_.GetIsObjectValid(area) == TRUE)
            {
                _.SetEventScript(area, EVENT_SCRIPT_AREA_ON_ENTER, "area_on_enter");
                _.SetEventScript(area, EVENT_SCRIPT_AREA_ON_EXIT, "area_on_exit");
                _.SetEventScript(area, EVENT_SCRIPT_AREA_ON_HEARTBEAT, "area_on_hb");
                _.SetEventScript(area, EVENT_SCRIPT_AREA_ON_USER_DEFINED_EVENT, "area_on_user");

                area = _.GetNextArea();
            }
        }

        private void SetModuleEventScripts()
        {
            // Vanilla NWN Event Hooks
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_ACQUIRE_ITEM, "mod_on_acquire");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_ACTIVATE_ITEM, "mod_on_activate");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_CLIENT_ENTER, "mod_on_enter");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_CLIENT_EXIT, "mod_on_leave");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_PLAYER_CANCEL_CUTSCENE, "mod_on_csabort");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_HEARTBEAT, "mod_on_heartbeat");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_PLAYER_CHAT, "mod_on_chat");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_PLAYER_DEATH, "mod_on_death");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_PLAYER_DYING, "mod_on_dying");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_EQUIP_ITEM, "mod_on_equip");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_PLAYER_LEVEL_UP, "mod_on_levelup");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_RESPAWN_BUTTON_PRESSED, "mod_on_respawn");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_PLAYER_REST, "mod_on_rest");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_UNEQUIP_ITEM, "mod_on_unequip");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_LOSE_ITEM, "mod_on_unacquire");
            _.SetEventScript(_.GetModule(), EVENT_SCRIPT_MODULE_ON_USER_DEFINED_EVENT, "mod_on_user");

            // NWNX Hooks
            _nwnxEvents.SubscribeEvent(EventType.StartCombatRoundBefore, "mod_on_attack");
            _nwnxEvents.SubscribeEvent(EventType.ExamineObjectBefore, "mod_on_examine");
            _nwnxEvents.SubscribeEvent(EventType.UseFeatBefore, "mod_on_usefeat");
            _nwnxDamage.SetDamageEventScript("mod_on_applydmg");

            // DM Hooks
            _nwnxEvents.SubscribeEvent(EventType.DMAppearBefore, "dm_appear");
            _nwnxEvents.SubscribeEvent(EventType.DMChangeDifficultyBefore, "dm_change_diff");
            _nwnxEvents.SubscribeEvent(EventType.DMDisableTrapBefore, "dm_disab_trap");
            _nwnxEvents.SubscribeEvent(EventType.DMDisappearBefore, "dm_disappear");
            _nwnxEvents.SubscribeEvent(EventType.DMForceRestBefore, "dm_force_rest");
            _nwnxEvents.SubscribeEvent(EventType.DMGetVariableBefore, "dm_get_var");
            _nwnxEvents.SubscribeEvent(EventType.DMGiveGoldBefore, "dm_give_gold");
            _nwnxEvents.SubscribeEvent(EventType.DMGiveItemBefore, "dm_give_item");
            _nwnxEvents.SubscribeEvent(EventType.DMGiveLevelBefore, "dm_give_level");
            _nwnxEvents.SubscribeEvent(EventType.DMGiveXPBefore, "dm_give_xp");
            _nwnxEvents.SubscribeEvent(EventType.DMHealBefore, "dm_heal");
            _nwnxEvents.SubscribeEvent(EventType.DMJumpBefore, "dm_jump");
            _nwnxEvents.SubscribeEvent(EventType.DMJumpAllPlayersToPointBefore, "dm_jump_all");
            _nwnxEvents.SubscribeEvent(EventType.DMJumpTargetToPointBefore, "dm_jump_target");
            _nwnxEvents.SubscribeEvent(EventType.DMKillBefore, "dm_kill");
            _nwnxEvents.SubscribeEvent(EventType.DMLimboBefore, "dm_limbo");
            _nwnxEvents.SubscribeEvent(EventType.DMPossessBefore, "dm_possess");
            _nwnxEvents.SubscribeEvent(EventType.DMSetDateBefore, "dm_set_date");
            _nwnxEvents.SubscribeEvent(EventType.DMSetStatBefore, "dm_set_stat");
            _nwnxEvents.SubscribeEvent(EventType.DMSetTimeBefore, "dm_set_time");
            _nwnxEvents.SubscribeEvent(EventType.DMSetVariableBefore, "dm_set_var");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnCreatureBefore, "dm_spawn_crea");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnEncounterBefore, "dm_spawn_enco");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnItemBefore, "dm_spawn_item");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnPlaceableBefore, "dm_spawn_plac");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnPortalBefore, "dm_spawn_port");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnTrapOnObjectBefore, "dm_spawn_trap");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnTriggerBefore, "dm_spawn_trigg");
            _nwnxEvents.SubscribeEvent(EventType.DMSpawnWaypointBefore, "dm_spawn_wayp");
            _nwnxEvents.SubscribeEvent(EventType.DMTakeItemBefore, "dm_take_item");
            _nwnxEvents.SubscribeEvent(EventType.DMToggleImmortalBefore, "dm_togg_immo");
            _nwnxEvents.SubscribeEvent(EventType.DMToggleAIBefore, "dm_toggle_ai");
            _nwnxEvents.SubscribeEvent(EventType.DMToggleLockBefore, "dm_toggle_lock");
        }

        private void SetWeaponSettings()
        {
            _nwnxWeapon.SetWeaponFocusFeat(CustomBaseItemType.Lightsaber, FEAT_WEAPON_FOCUS_LONG_SWORD);
            _nwnxWeapon.SetWeaponFocusFeat(CustomBaseItemType.Saberstaff, FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD);

            _nwnxWeapon.SetWeaponImprovedCriticalFeat(CustomBaseItemType.Lightsaber, FEAT_IMPROVED_CRITICAL_LONG_SWORD);
            _nwnxWeapon.SetWeaponImprovedCriticalFeat(CustomBaseItemType.Saberstaff, FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD);

            _nwnxWeapon.SetWeaponSpecializationFeat(CustomBaseItemType.Lightsaber, FEAT_WEAPON_SPECIALIZATION_LONG_SWORD);
            _nwnxWeapon.SetWeaponSpecializationFeat(CustomBaseItemType.Saberstaff, FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD);

            _nwnxWeapon.SetWeaponFinesseSize(CustomBaseItemType.Lightsaber, CREATURE_SIZE_MEDIUM);
            _nwnxWeapon.SetWeaponFinesseSize(CustomBaseItemType.Saberstaff, CREATURE_SIZE_MEDIUM);
        }

    }
}
