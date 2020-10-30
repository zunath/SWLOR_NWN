using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Scripts;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.Threading;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class mod_on_load
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            var nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            Console.WriteLine(nowString + ": Module OnLoad executing...");

            using (new Profiler(nameof(mod_on_load) + ":DatabaseMigrator"))
            {
                DatabaseMigrationRunner.Start();
            }

            using (new Profiler(nameof(mod_on_load) + ":DBBackgroundThread"))
            {
                Console.WriteLine("Starting background thread manager...");
                BackgroundThreadManager.Start();
            }

            using (new Profiler(nameof(mod_on_load) + ":SetEventScripts"))
            {
                Chat.RegisterChatScript("mod_on_nwnxchat");
                SetModuleEventScripts();
                SetAreaEventScripts();
                SetWeaponSettings();

            }
            // Bioware default
            NWScript.ExecuteScript("x2_mod_def_load", NWScript.OBJECT_SELF);

            using (new Profiler(nameof(mod_on_load) + ":RegisterSubscribeEvents"))
            {
                RegisterServiceSubscribeEvents();
            }

            ScriptService.Initialize();
            MessageHub.Instance.Publish(new OnModuleLoad());

            nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            Console.WriteLine(nowString + ": Module OnLoad finished!");
        }


        private static void RegisterServiceSubscribeEvents()
        {
            // Use reflection to get all of the SubscribeEvents() methods in the SWLOR namespace.
            var typesInNamespace = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(x => x.Namespace != null && 
                            x.Namespace.StartsWith("SWLOR.Game.Server") && // The entire SWLOR namespace
                            !typeof(IScript).IsAssignableFrom(x) && // Exclude scripts
                            x.IsClass) // Classes only.
                .ToArray();
            foreach (var type in typesInNamespace)
            {
                var method = type.GetMethod("SubscribeEvents");
                if (method != null)
                {
                    method.Invoke(null, null);
                }
            }
        }

        private static void SetAreaEventScripts()
        {
            var area = NWScript.GetFirstArea();
            while (NWScript.GetIsObjectValid(area) == true)
            {
                NWScript.SetEventScript(area, EventScript.Area_OnEnter, "area_on_enter");
                NWScript.SetEventScript(area, EventScript.Area_OnExit, "area_on_exit");
                // Heartbeat events will be set when players enter the area.
                // There's no reason to have them firing if no players are in the area.
                NWScript.SetEventScript(area, EventScript.Area_OnHeartbeat, string.Empty);
                NWScript.SetEventScript(area, EventScript.Area_OnUserDefined, "area_on_user");

                area = NWScript.GetNextArea();
            }
        }


        private static void SetModuleEventScripts()
        {
            // Vanilla NWN Event Hooks
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnAcquireItem, "mod_on_acquire");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnActivateItem, "mod_on_activate");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnClientEnter, "mod_on_enter");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnClientExit, "mod_on_leave");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnPlayerCancelCutscene, "mod_on_csabort");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnHeartbeat, "mod_on_heartbeat");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnPlayerChat , "mod_on_chat");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnPlayerDeath, "mod_on_death");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnPlayerDying, "mod_on_dying");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnEquipItem, "mod_on_equip");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnPlayerLevelUp, "mod_on_levelup");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnRespawnButtonPressed, "mod_on_respawn");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnPlayerRest, "mod_on_rest");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnUnequipItem, "mod_on_unequip");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnLoseItem, "mod_on_unacquire");
            NWScript.SetEventScript(NWScript.GetModule(), EventScript.Module_OnUserDefined , "mod_on_user");

            // NWNX Hooks
            Events.SubscribeEvent(EventType.StartCombatRoundBefore, "mod_on_attack");
            Events.SubscribeEvent(EventType.ExamineObjectBefore, "mod_on_examine");
            Events.SubscribeEvent(EventType.UseFeatBefore, "mod_on_usefeat");
            Events.SubscribeEvent(EventType.EnterStealthAfter, "mod_on_entstlth");
            Events.SubscribeEvent(EventType.DecrementItemStackSizeBefore, "item_dec_stack");
            Events.SubscribeEvent(EventType.UseItemBefore, "item_use_before");
            Events.SubscribeEvent(EventType.UseItemAfter, "item_use_after");
            Damage.SetDamageEventScript("mod_on_applydmg");

            // DM Hooks
            Events.SubscribeEvent(EventType.DMAppearBefore, "dm_appear");
            Events.SubscribeEvent(EventType.DMChangeDifficultyBefore, "dm_change_diff");
            Events.SubscribeEvent(EventType.DMDisableTrapBefore, "dm_disab_trap");
            Events.SubscribeEvent(EventType.DMDisappearBefore, "dm_disappear");
            Events.SubscribeEvent(EventType.DMForceRestBefore, "dm_force_rest");
            Events.SubscribeEvent(EventType.DMGetVariableBefore, "dm_get_var");
            Events.SubscribeEvent(EventType.DMGiveGoldBefore, "dm_give_gold");
            Events.SubscribeEvent(EventType.DMGiveItemBefore, "dm_give_item");
            Events.SubscribeEvent(EventType.DMGiveLevelBefore, "dm_give_level");
            Events.SubscribeEvent(EventType.DMGiveXPBefore, "dm_give_xp");
            Events.SubscribeEvent(EventType.DMHealBefore, "dm_heal");
            Events.SubscribeEvent(EventType.DMJumpBefore, "dm_jump");
            Events.SubscribeEvent(EventType.DMJumpAllPlayersToPointBefore, "dm_jump_all");
            Events.SubscribeEvent(EventType.DMJumpTargetToPointBefore, "dm_jump_target");
            Events.SubscribeEvent(EventType.DMKillBefore, "dm_kill");
            Events.SubscribeEvent(EventType.DMLimboBefore, "dm_limbo");
            Events.SubscribeEvent(EventType.DMPossessBefore, "dm_possess");
            Events.SubscribeEvent(EventType.DMSetDateBefore, "dm_set_date");
            Events.SubscribeEvent(EventType.DMSetStatBefore, "dm_set_stat");
            Events.SubscribeEvent(EventType.DMSetTimeBefore, "dm_set_time");
            Events.SubscribeEvent(EventType.DMSetVariableBefore, "dm_set_var");
            Events.SubscribeEvent(EventType.DMSpawnCreatureAfter, "dm_spawn_crea");
            Events.SubscribeEvent(EventType.DMSpawnEncounterAfter, "dm_spawn_enco");
            Events.SubscribeEvent(EventType.DMSpawnItemAfter, "dm_spawn_item");
            Events.SubscribeEvent(EventType.DMSpawnPlaceableAfter, "dm_spawn_plac");
            Events.SubscribeEvent(EventType.DMSpawnPortalAfter, "dm_spawn_port");
            Events.SubscribeEvent(EventType.DMSpawnTrapOnObjectAfter, "dm_spawn_trap");
            Events.SubscribeEvent(EventType.DMSpawnTriggerAfter, "dm_spawn_trigg");
            Events.SubscribeEvent(EventType.DMSpawnWaypointAfter, "dm_spawn_wayp");
            Events.SubscribeEvent(EventType.DMTakeItemBefore, "dm_take_item");
            Events.SubscribeEvent(EventType.DMToggleImmortalBefore, "dm_togg_immo");
            Events.SubscribeEvent(EventType.DMToggleAIBefore, "dm_toggle_ai");
            Events.SubscribeEvent(EventType.DMToggleLockBefore, "dm_toggle_lock");
        }

        private static void SetWeaponSettings()
        {
            Weapon.SetWeaponFocusFeat(BaseItem.Lightsaber, Feat.EpicWeaponFocus_Longsword);
            Weapon.SetWeaponFocusFeat(BaseItem.Saberstaff, Feat.WeaponFocus_TwoBladedSword);

            Weapon.SetWeaponImprovedCriticalFeat(BaseItem.Lightsaber, Feat.ImprovedCritical_LongSword);
            Weapon.SetWeaponImprovedCriticalFeat(BaseItem.Saberstaff, Feat.ImprovedCritical_TwoBladedSword);

            Weapon.SetWeaponSpecializationFeat(BaseItem.Lightsaber, Feat.EpicWeaponSpecialization_Longsword);
            Weapon.SetWeaponSpecializationFeat(BaseItem.Saberstaff, Feat.EpicWeaponSpecialization_Twobladedsword);

            Weapon.SetWeaponFinesseSize(BaseItem.Lightsaber, CreatureSize.Medium);
            Weapon.SetWeaponFinesseSize(BaseItem.Saberstaff, CreatureSize.Medium);
            Weapon.SetWeaponFinesseSize(BaseItem.Longsword, CreatureSize.Medium);

            Weapon.SetWeaponUnarmed(BaseItem.QuarterStaff);
            Weapon.SetWeaponUnarmed(BaseItem.Club);
        }


    }
}
