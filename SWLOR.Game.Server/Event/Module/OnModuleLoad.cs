using System;
using SWLOR.Game.Server.Enumeration;

using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleLoad: IRegisteredEvent
    {
        private readonly IObjectProcessingService _objectProcessing;
        private readonly IFarmingService _farming;
        private readonly IAreaService _area;
        private readonly IBaseService _base;
        private readonly ISpawnService _spawn;
        private readonly ICustomEffectService _customEffect;
        private readonly IObjectVisibilityService _objectVisibility;
        private readonly IBackgroundThreadManager _backgroundThreadManager;
        private readonly IDataPackageService _dataPackage;

        public OnModuleLoad(
            
            IObjectProcessingService objectProcessing,
            IFarmingService farming,
            IAreaService area,
            IBaseService @base,
            ISpawnService spawn,
            ICustomEffectService customEffect,
            IObjectVisibilityService objectVisibility,
            IBackgroundThreadManager backgroundThreadManager,
            IDataPackageService dataPackage)
        {
            _objectProcessing = objectProcessing;
            _farming = farming;
            _area = area;
            _base = @base;
            _spawn = spawn;
            _customEffect = customEffect;
            _objectVisibility = objectVisibility;
            _backgroundThreadManager = backgroundThreadManager;
            _dataPackage = dataPackage;
        }

        public bool Run(params object[] args)
        {
            string nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            Console.WriteLine(nowString + ": Module OnLoad executing...");
            
            Console.WriteLine("Starting background thread manager...");
            _backgroundThreadManager.Start();
            
            NWNXChat.RegisterChatScript("mod_onNWNXChat");
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
            NWNXEvents.SubscribeEvent(EventType.StartCombatRoundBefore, "mod_on_attack");
            NWNXEvents.SubscribeEvent(EventType.ExamineObjectBefore, "mod_on_examine");
            NWNXEvents.SubscribeEvent(EventType.UseFeatBefore, "mod_on_usefeat");
            NWNXEvents.SubscribeEvent(EventType.EnterStealthAfter, "mod_on_entstlth");
            NWNXDamage.SetDamageEventScript("mod_on_applydmg");

            // DM Hooks
            NWNXEvents.SubscribeEvent(EventType.DMAppearBefore, "dm_appear");
            NWNXEvents.SubscribeEvent(EventType.DMChangeDifficultyBefore, "dm_change_diff");
            NWNXEvents.SubscribeEvent(EventType.DMDisableTrapBefore, "dm_disab_trap");
            NWNXEvents.SubscribeEvent(EventType.DMDisappearBefore, "dm_disappear");
            NWNXEvents.SubscribeEvent(EventType.DMForceRestBefore, "dm_force_rest");
            NWNXEvents.SubscribeEvent(EventType.DMGetVariableBefore, "dm_get_var");
            NWNXEvents.SubscribeEvent(EventType.DMGiveGoldBefore, "dm_give_gold");
            NWNXEvents.SubscribeEvent(EventType.DMGiveItemBefore, "dm_give_item");
            NWNXEvents.SubscribeEvent(EventType.DMGiveLevelBefore, "dm_give_level");
            NWNXEvents.SubscribeEvent(EventType.DMGiveXPBefore, "dm_give_xp");
            NWNXEvents.SubscribeEvent(EventType.DMHealBefore, "dm_heal");
            NWNXEvents.SubscribeEvent(EventType.DMJumpBefore, "dm_jump");
            NWNXEvents.SubscribeEvent(EventType.DMJumpAllPlayersToPointBefore, "dm_jump_all");
            NWNXEvents.SubscribeEvent(EventType.DMJumpTargetToPointBefore, "dm_jump_target");
            NWNXEvents.SubscribeEvent(EventType.DMKillBefore, "dm_kill");
            NWNXEvents.SubscribeEvent(EventType.DMLimboBefore, "dm_limbo");
            NWNXEvents.SubscribeEvent(EventType.DMPossessBefore, "dm_possess");
            NWNXEvents.SubscribeEvent(EventType.DMSetDateBefore, "dm_set_date");
            NWNXEvents.SubscribeEvent(EventType.DMSetStatBefore, "dm_set_stat");
            NWNXEvents.SubscribeEvent(EventType.DMSetTimeBefore, "dm_set_time");
            NWNXEvents.SubscribeEvent(EventType.DMSetVariableBefore, "dm_set_var");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnCreatureAfter, "dm_spawn_crea");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnEncounterAfter, "dm_spawn_enco");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnItemAfter, "dm_spawn_item");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnPlaceableAfter, "dm_spawn_plac");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnPortalAfter, "dm_spawn_port");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnTrapOnObjectAfter, "dm_spawn_trap");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnTriggerAfter, "dm_spawn_trigg");
            NWNXEvents.SubscribeEvent(EventType.DMSpawnWaypointAfter, "dm_spawn_wayp");
            NWNXEvents.SubscribeEvent(EventType.DMTakeItemBefore, "dm_take_item");
            NWNXEvents.SubscribeEvent(EventType.DMToggleImmortalBefore, "dm_togg_immo");
            NWNXEvents.SubscribeEvent(EventType.DMToggleAIBefore, "dm_toggle_ai");
            NWNXEvents.SubscribeEvent(EventType.DMToggleLockBefore, "dm_toggle_lock");
        }

        private void SetWeaponSettings()
        {
            NWNXWeapon.SetWeaponFocusFeat(CustomBaseItemType.Lightsaber, FEAT_WEAPON_FOCUS_LONG_SWORD);
            NWNXWeapon.SetWeaponFocusFeat(CustomBaseItemType.Saberstaff, FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD);

            NWNXWeapon.SetWeaponImprovedCriticalFeat(CustomBaseItemType.Lightsaber, FEAT_IMPROVED_CRITICAL_LONG_SWORD);
            NWNXWeapon.SetWeaponImprovedCriticalFeat(CustomBaseItemType.Saberstaff, FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD);

            NWNXWeapon.SetWeaponSpecializationFeat(CustomBaseItemType.Lightsaber, FEAT_WEAPON_SPECIALIZATION_LONG_SWORD);
            NWNXWeapon.SetWeaponSpecializationFeat(CustomBaseItemType.Saberstaff, FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD);

            NWNXWeapon.SetWeaponFinesseSize(CustomBaseItemType.Lightsaber, CREATURE_SIZE_MEDIUM);
            NWNXWeapon.SetWeaponFinesseSize(CustomBaseItemType.Saberstaff, CREATURE_SIZE_MEDIUM);
        }

    }
}
