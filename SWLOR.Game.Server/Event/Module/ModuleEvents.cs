using System;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripts;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.Threading;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Profiler = SWLOR.Game.Server.ValueObject.Profiler;

namespace SWLOR.Game.Server.Event.Module
{
    public static class ModuleEvents
    {
        [NWNEventHandler("mod_acquire")]
        public static void OnModuleAcquireItem()
        {
            // Bioware Default
            ExecuteScript("x2_mod_def_aqu", OBJECT_SELF);
            MessageHub.Instance.Publish(new OnModuleAcquireItem());
        }

        [NWNEventHandler("mod_activate")]
        public static void OnModuleActivateItem()
        {
            // Bioware default
            ExecuteScript("x2_mod_def_act", OBJECT_SELF);
            MessageHub.Instance.Publish(new OnModuleActivateItem());
        }

        [NWNEventHandler("mod_enter")]
        public static void OnModuleEnter()
        {
            // The order of the following procedures matters.
            NWPlayer player = GetEnteringObject();

            using (new Profiler(nameof(OnModuleEnter) + ":AddDMToCache"))
            {
                if (player.IsDM)
                {
                    AppCache.ConnectedDMs.Add(player);
                }
            }

            using (new Profiler(nameof(OnModuleEnter) + ":BiowareDefault"))
            {
                player.DeleteLocalInt("IS_CUSTOMIZING_ITEM");
                ExecuteScript("dmfi_onclienter ", OBJECT_SELF); // DMFI also calls "x3_mod_def_enter"
            }

            using (new Profiler(nameof(OnModuleEnter) + ":PlayerValidation"))
            {
                PlayerValidationService.OnModuleEnter();
            }

            using (new Profiler(nameof(OnModuleEnter) + ":InitializePlayer"))
            {
                PlayerService.InitializePlayer(player);
            }

            using (new Profiler(nameof(OnModuleEnter) + ":SkillServiceEnter"))
            {
                SkillService.OnModuleEnter();
            }

            using (new Profiler(nameof(OnModuleEnter) + ":PerkServiceEnter"))
            {
                PerkService.OnModuleEnter();
            }

            MessageHub.Instance.Publish(new OnModuleEnter());
            SetLocalBool(player, "LOGGED_IN_ONCE", true);
        }

        [NWNEventHandler("mod_exit")]
        public static void OnModuleExit()
        {
            NWPlayer pc = (GetExitingObject());

            using (new Profiler(nameof(OnModuleExit) + ":RemoveDMFromCache"))
            {
                if (pc.IsDM)
                {
                    AppCache.ConnectedDMs.Remove(pc);
                }
            }

            using (new Profiler(nameof(OnModuleExit) + ":ExportSingleCharacter"))
            {
                if (pc.IsPlayer)
                {
                    ExportSingleCharacter(pc.Object);
                }
            }

            MessageHub.Instance.Publish(new OnModuleLeave());
        }

        [NWNEventHandler("mod_abort_cs")]
        public static void OnModuleCutsceneAbort()
        {

        }

        [NWNEventHandler("mod_heartbeat")]
        public static void OnModuleHeartbeat()
        {
            MessageHub.Instance.Publish(new OnModuleHeartbeat());
        }

        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {

            static void RegisterServiceSubscribeEvents()
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

            static void SetWeaponSettings()
            {
                Weapon.SetWeaponFocusFeat(BaseItem.Lightsaber, Core.NWScript.Enum.Feat.EpicWeaponFocus_Longsword);
                Weapon.SetWeaponFocusFeat(BaseItem.Saberstaff, Core.NWScript.Enum.Feat.WeaponFocus_TwoBladedSword);

                Weapon.SetWeaponImprovedCriticalFeat(BaseItem.Lightsaber, Core.NWScript.Enum.Feat.ImprovedCritical_LongSword);
                Weapon.SetWeaponImprovedCriticalFeat(BaseItem.Saberstaff, Core.NWScript.Enum.Feat.ImprovedCritical_TwoBladedSword);

                Weapon.SetWeaponSpecializationFeat(BaseItem.Lightsaber, Core.NWScript.Enum.Feat.EpicWeaponSpecialization_Longsword);
                Weapon.SetWeaponSpecializationFeat(BaseItem.Saberstaff, Core.NWScript.Enum.Feat.EpicWeaponSpecialization_Twobladedsword);

                Weapon.SetWeaponFinesseSize(BaseItem.Lightsaber, CreatureSize.Medium);
                Weapon.SetWeaponFinesseSize(BaseItem.Saberstaff, CreatureSize.Medium);
                Weapon.SetWeaponFinesseSize(BaseItem.Longsword, CreatureSize.Medium);

                Weapon.SetWeaponUnarmed(BaseItem.QuarterStaff);
                Weapon.SetWeaponUnarmed(BaseItem.Club);
            }

            var nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            Console.WriteLine(nowString + ": Module OnLoad executing...");

            using (new Profiler(nameof(OnModuleLoad) + ":DatabaseMigrator"))
            {
                DatabaseMigrationRunner.Start();
            }

            using (new Profiler(nameof(OnModuleLoad) + ":DBBackgroundThread"))
            {
                Console.WriteLine("Starting background thread manager...");
                BackgroundThreadManager.Start();
            }

            using (new Profiler(nameof(OnModuleLoad) + ":SetEventScripts"))
            {
                Chat.RegisterChatScript("on_nwnx_chat");
                SetWeaponSettings();

            }
            // Bioware default
            ExecuteScript("x2_mod_def_load", OBJECT_SELF);

            using (new Profiler(nameof(OnModuleLoad) + ":RegisterSubscribeEvents"))
            {
                RegisterServiceSubscribeEvents();
            }

            ScriptService.Initialize();
            MessageHub.Instance.Publish(new OnModuleLoad());

            nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            Console.WriteLine(nowString + ": Module OnLoad finished!");
        }

        [NWNEventHandler("mod_chat")]
        public static void OnModuleChat()
        {
            MessageHub.Instance.Publish(new OnModuleChat());
        }

        [NWNEventHandler("mod_dying")]
        public static void OnModuleDying()
        {
            // Bioware Default
            ExecuteScript("nw_o0_dying", OBJECT_SELF);
            MessageHub.Instance.Publish(new OnModuleDying());
        }

        [NWNEventHandler("mod_death")]
        public static void OnModuleDeath()
        {
            MessageHub.Instance.Publish(new OnModuleDeath());
        }

        [NWNEventHandler("mod_equip")]
        public static void OnModuleEquipItem()
        {
            NWObject equipper = OBJECT_SELF;
            // Bioware Default
            ExecuteScript("x2_mod_def_equ", equipper);

            MessageHub.Instance.Publish(new OnModuleEquipItem());
        }

        [NWNEventHandler("mod_level_up")]
        public static void OnModuleLevelUp()
        {
            MessageHub.Instance.Publish(new OnModuleLevelUp());
        }

        [NWNEventHandler("mod_respawn")]
        public static void OnModuleRespawn()
        {
            MessageHub.Instance.Publish(new OnModuleRespawn());
        }

        [NWNEventHandler("mod_rest")]
        public static void OnModuleRest()
        {
            MessageHub.Instance.Publish(new OnModuleRest());
        }

        [NWNEventHandler("mod_unequip")]
        public static void OnModuleUnequipItem()
        {
            var equipper = OBJECT_SELF;
            // Bioware Default
            ExecuteScript("x2_mod_def_unequ", equipper);

            MessageHub.Instance.Publish(new OnModuleUnequipItem());
        }

        [NWNEventHandler("mod_unacquire")]
        public static void OnModuleUnacquireItem()
        {
            // Bioware default
            ExecuteScript("x2_mod_def_unaqu", OBJECT_SELF);
            MessageHub.Instance.Publish(new OnModuleUnacquireItem());
        }

        [NWNEventHandler("mod_user_def")]
        public static void OnModuleUserDefined()
        {
            MessageHub.Instance.Publish(new OnModuleUserDefined());
        }
    }
}
