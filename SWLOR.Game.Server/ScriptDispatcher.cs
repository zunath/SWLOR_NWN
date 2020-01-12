using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NWN.Scripts;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Conversation;
using SWLOR.Game.Server.Event.Conversation.Quest.AcceptQuest;
using SWLOR.Game.Server.Event.Conversation.Quest.AdvanceQuest;
using SWLOR.Game.Server.Event.Conversation.Quest.CanAcceptQuest;
using SWLOR.Game.Server.Event.Conversation.Quest.CollectQuestItem;
using SWLOR.Game.Server.Event.Conversation.Quest.FinishQuest;
using SWLOR.Game.Server.Event.Conversation.Quest.HasQuest;
using SWLOR.Game.Server.Event.Conversation.Quest.OnQuestState;
using SWLOR.Game.Server.Event.Conversation.Quest.QuestIsDone;
using SWLOR.Game.Server.Event.Conversation.RimerCards;
using SWLOR.Game.Server.Event.Creature;
using SWLOR.Game.Server.Event.DM;
using SWLOR.Game.Server.Event.Feat;
using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.Event.Legacy;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.Player;
using SWLOR.Game.Server.Event.Store;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Logging;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using OpenStore = SWLOR.Game.Server.Event.Conversation.OpenStore;

namespace SWLOR.Game.Server
{
    public class ScriptDispatcher
    {
        private readonly Dictionary<string, Func<int>> _registry;
        private const int SCRIPT_HANDLED = 0;
        private const int SCRIPT_NOT_HANDLED = -1;

        public ScriptDispatcher()
        {
            _registry = new Dictionary<string, Func<int>>();
        }

        /// <summary>
        /// Registers and caches scripts.
        /// </summary>
        public void RegisterScripts()
        {
            // Area Events
            _registry["area_on_enter"] = () =>
            {
                BaseService.OnAreaEnter();
                MessageHub.Instance.Publish(new OnAreaEnter());
                return SCRIPT_HANDLED;
            };
            _registry["area_on_exit"] = () =>
            {
                MessageHub.Instance.Publish(new OnAreaExit());
                return SCRIPT_HANDLED;
            };
            _registry["area_on_hb"] = () =>
            {
                MessageHub.Instance.Publish(new OnAreaHeartbeat());
                return SCRIPT_HANDLED;
            };
            _registry["area_on_user"] = () =>
            {
                MessageHub.Instance.Publish(new OnAreaUserDefined());
                return SCRIPT_HANDLED;
            };

            // Creature Events
            _registry["crea_on_attacked"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreaturePhysicalAttacked());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_blocked"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureBlocked());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_convo"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureConversation());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_damaged"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureDamaged());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_death"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureDeath());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_disturb"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureDisturbed());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_hb"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureHeartbeat());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_percept"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreaturePerception());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_rested"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureRested());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_roundend"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureCombatRoundEnd());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_spawn"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureSpawn());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_splcast"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureSpellCastAt());
                return SCRIPT_HANDLED;
            };
            _registry["crea_on_userdef"] = () =>
            {
                MessageHub.Instance.Publish(new OnCreatureUserDefined());
                return SCRIPT_HANDLED;
            };

            // Dialog
            for (int x = 0; x <= 11; x++)
            {
                // This is used inside the closure, so copy it to another variable so we aren't accessing a modified closure.
                var nodeID = x;
                _registry[$"dialog_action_{nodeID}"] = () =>
                {
                    DialogService.OnActionsTaken(nodeID);
                    return SCRIPT_HANDLED;
                };

                var appearsName = "dialog_appears";
                if (x < 10)
                    appearsName += "_";

                _registry[$"{appearsName}{x}"] = () => DialogService.OnAppearsWhen(2, nodeID) ? 1 : 0;
            }
            // Actions: Next, Previous, Back options
            _registry["dialog_action_n"] = () =>
            {
                DialogService.OnActionsTaken(12);
                return SCRIPT_HANDLED;
            };
            _registry["dialog_action_p"] = () =>
            {
                DialogService.OnActionsTaken(13);
                return SCRIPT_HANDLED;
            };
            _registry["dialog_action_b"] = () =>
            {
                DialogService.OnActionsTaken(14);
                return SCRIPT_HANDLED;
            };
            // Appears: Header, Next, Previous, Back options
            _registry["dialog_appears_h"] = () => DialogService.OnAppearsWhen(1, 0) ? 1 : 0;
            _registry["dialog_appears_n"] = () => DialogService.OnAppearsWhen(3, 12) ? 1 : 0;
            _registry["dialog_appears_p"] = () => DialogService.OnAppearsWhen(4, 13) ? 1 : 0;
            _registry["dialog_appears_b"] = () => DialogService.OnAppearsWhen(5, 14) ? 1 : 0;

            _registry["dialog_start"] = () =>
            {
                DialogService.OnDialogStart();
                return SCRIPT_HANDLED;
            };
            _registry["dialog_end"] = () =>
            {
                DialogService.OnDialogEnd();
                return SCRIPT_HANDLED;
            };

            // DM Events
            _registry["dm_appear"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(20));
                return SCRIPT_HANDLED;
            };
            _registry["dm_change_diff"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(8));
                return SCRIPT_HANDLED;
            };
            _registry["dm_disab_trap"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(19));
                return SCRIPT_HANDLED;
            };
            _registry["dm_disappear"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(21));
                return SCRIPT_HANDLED;
            };
            _registry["dm_force_rest"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(15));
                return SCRIPT_HANDLED;
            };
            _registry["dm_get_var"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(30));
                return SCRIPT_HANDLED;
            };
            _registry["dm_give_gold"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(24));
                return SCRIPT_HANDLED;
            };
            _registry["dm_give_item"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(25));
                return SCRIPT_HANDLED;
            };
            _registry["dm_give_level"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(23));
                return SCRIPT_HANDLED;
            };
            _registry["dm_give_xp"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(22));
                return SCRIPT_HANDLED;
            };
            _registry["dm_heal"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(10));
                return SCRIPT_HANDLED;
            };
            _registry["dm_jump"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(12));
                return SCRIPT_HANDLED;
            };
            _registry["dm_jump_all"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(28));
                return SCRIPT_HANDLED;
            };
            _registry["dm_jump_target"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(27));
                return SCRIPT_HANDLED;
            };
            _registry["dm_kill"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(11));
                return SCRIPT_HANDLED;
            };
            _registry["dm_limbo"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(16));
                return SCRIPT_HANDLED;
            };
            _registry["dm_possess"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(13));
                return SCRIPT_HANDLED;
            };
            _registry["dm_set_date"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(33));
                return SCRIPT_HANDLED;
            };
            _registry["dm_set_stat"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(29));
                return SCRIPT_HANDLED;
            };
            _registry["dm_set_time"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(32));
                return SCRIPT_HANDLED;
            };
            _registry["dm_set_var"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(31));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_crea"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(1));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_enco"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(5));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_item"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(2));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_plac"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(7));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_port"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(6));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_trap"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(9));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_trigg"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(3));
                return SCRIPT_HANDLED;
            };
            _registry["dm_spawn_wayp"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(4));
                return SCRIPT_HANDLED;
            };
            _registry["dm_take_item"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(26));
                return SCRIPT_HANDLED;
            };
            _registry["dm_togg_immo"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(14));
                return SCRIPT_HANDLED;
            };
            _registry["dm_toggle_ai"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(17));
                return SCRIPT_HANDLED;
            };
            _registry["dm_toggle_lock"] = () =>
            {
                MessageHub.Instance.Publish(new OnDMAction(18));
                return SCRIPT_HANDLED;
            };

            // Feats
            _registry["onhit_castspell"] = () =>
            {
                MessageHub.Instance.Publish(new OnHitCastSpell());
                return SCRIPT_HANDLED;
            };
            _registry["swlor_craft"] = () =>
            {
                MessageHub.Instance.Publish(new OnUseCraftingFeat());
                return SCRIPT_HANDLED;
            };

            // Items
            _registry["item_dec_stack"] = () =>
            {
                if (NWGameObject.OBJECT_SELF == null) return SCRIPT_HANDLED;

                NWItem item = NWGameObject.OBJECT_SELF;
                if (!item.IsValid) return SCRIPT_HANDLED;

                // We ignore any decrements to shurikens, darts, and throwing axes.
                if (item.BaseItemType == BaseItemType.Shuriken ||
                    item.BaseItemType == BaseItemType.Dart ||
                    item.BaseItemType == BaseItemType.ThrowingAxe)
                {
                    NWNXEvents.SkipEvent();
                }

                MessageHub.Instance.Publish(new OnItemDecrementStack(), false);
                return SCRIPT_HANDLED;
            };
            _registry["item_use_after"] = () =>
            {
                // Already handled in the item_use_before script. No need for anything else to run at this point.
                NWNXEvents.SkipEvent();
                return SCRIPT_HANDLED;
            };
            _registry["item_use_before"] = () =>
            {
                MessageHub.Instance.Publish(new OnItemUsed());
                return SCRIPT_HANDLED;
            };

            // Generic scripts
            for(int x = 1; x <= 9; x++)
            {
                // Copy the variable to avoid modified closure errors.
                var scriptID = x;
                _registry[$"script_{scriptID}"] = () =>
                {
                    ScriptEvent.Run($"SCRIPT_{scriptID}");
                    return SCRIPT_HANDLED;
                };
            }

            // Module 
            _registry["mod_on_acquire"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleAcquireItem());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_activate"] = () =>
            {
                ExecuteScript("x2_mod_def_act", NWGameObject.OBJECT_SELF);
                MessageHub.Instance.Publish(new OnModuleActivateItem());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_applydmg"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleApplyDamage(), false);
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_attack"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleAttack());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_chat"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleChat());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_csabort"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleCutsceneAbort());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_death"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleDeath());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_dying"] = () =>
            {
                ExecuteScript("nw_o0_dying", NWGameObject.OBJECT_SELF);
                MessageHub.Instance.Publish(new OnModuleDying());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_enter"] = () =>
            {
                // The order of the following procedures matters.
                NWPlayer player = GetEnteringObject();
                if (player.IsDM)
                {
                    AppCache.ConnectedDMs.Add(player);
                }
                player.DeleteLocalInt("IS_CUSTOMIZING_ITEM");
                ExecuteScript("dmfi_onclienter ", NWGameObject.OBJECT_SELF); // DMFI also calls "x3_mod_def_enter"
                PlayerValidationService.OnModuleEnter();
                PlayerService.InitializePlayer(player);
                SkillService.OnModuleEnter();
                PerkService.OnModuleEnter();

                MessageHub.Instance.Publish(new OnModuleEnter());
                player.SetLocalBoolean("LOGGED_IN_ONCE", true);
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_entstlth"] = () =>
            {
                NWObject stealther = NWGameObject.OBJECT_SELF;
                SetActionMode(stealther, ActionMode.Stealth, false);
                FloatingTextStringOnCreature("NWN stealth mode is disabled on this server.", stealther, false);
                MessageHub.Instance.Publish(new OnModuleEnterStealthAfter());

                return SCRIPT_HANDLED;
            };
            _registry["mod_on_equip"] = () =>
            {
                ExecuteScript("x2_mod_def_equ", NWGameObject.OBJECT_SELF);
                MessageHub.Instance.Publish(new OnModuleEquipItem());

                return SCRIPT_HANDLED;
            };

            _registry["mod_on_examine"] = () =>
            {
                // We'll likely want to refactor this so that the logic is handled in the services. For now, I'm leaving it here though.
                NWPlayer examiner = NWGameObject.OBJECT_SELF;
                NWObject examinedObject = NWNXEvents.OnExamineObject_GetTarget();
                if (ExaminationService.OnModuleExamine(examiner, examinedObject))
                {
                    MessageHub.Instance.Publish(new OnModuleExamine());
                    return SCRIPT_HANDLED;
                }

                string description;

                if (GetIsPC(examinedObject.Object))
                {
                    // https://github.com/zunath/SWLOR_NWN/issues/853
                    // safest probably to get the modified (non-original) description only for players
                    // may want to always get the modified description for later flexibility?
                    description = GetDescription(examinedObject.Object, false) + "\n\n";
                }
                else
                {
                    description = GetDescription(examinedObject.Object, true) + "\n\n";
                }

                if (examinedObject.IsCreature)
                {
                    int racialID = Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(examinedObject)));
                    string racialtype = GetStringByStrRef(racialID);
                    if (!description.Contains(ColorTokenService.Green("Racial Type: ") + racialtype))
                    {
                        description += ColorTokenService.Green("Racial Type: ") + racialtype;
                    }
                }

                description = ModService.OnModuleExamine(description, examiner, examinedObject);
                description = ItemService.OnModuleExamine(description, examinedObject);
                description = DurabilityService.OnModuleExamine(description, examinedObject);

                if (!string.IsNullOrWhiteSpace(description))
                {
                    SetDescription(examinedObject.Object, description, false);
                    SetDescription(examinedObject.Object, description);
                }
                MessageHub.Instance.Publish(new OnModuleExamine());

                return SCRIPT_HANDLED;
            };

            _registry["mod_on_heartbeat"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleHeartbeat());
                return SCRIPT_HANDLED;
            };

            _registry["mod_on_leave"] = () =>
            {
                NWPlayer pc = GetExitingObject();

                if (pc.IsDM)
                {
                    AppCache.ConnectedDMs.Remove(pc);
                }

                if (pc.IsPlayer)
                {
                    ExportSingleCharacter(pc.Object);
                }

                MessageHub.Instance.Publish(new OnModuleLeave());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_levelup"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleLevelUp());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_load"] = () =>
            {
                RegisterServiceSubscribeEvents();
                AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
                {
                    MessageHub.Instance.Publish(new OnServerStopped());
                };

                MessageHub.Instance.Publish(new OnServerInitalization());
                MessageHub.Instance.Publish(new OnServerCacheData());

                string nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
                Console.WriteLine(nowString + ": Module OnLoad executing...");
                MessageHub.Instance.Publish(new OnModuleLoad());

                nowString = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
                Console.WriteLine(nowString + ": Module OnLoad finished!");
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_nwnxchat"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleNWNXChat());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_respawn"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleRespawn());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_rest"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleRest());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_unacquire"] = () =>
            {
                ExecuteScript("x2_mod_def_unaqu", NWGameObject.OBJECT_SELF);
                MessageHub.Instance.Publish(new OnModuleUnacquireItem());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_unequip"] = () =>
            {
                // Bioware Default
                ExecuteScript("x2_mod_def_unequ", NWGameObject.OBJECT_SELF);
                MessageHub.Instance.Publish(new OnModuleUnequipItem());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_usefeat"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleUseFeat());
                return SCRIPT_HANDLED;
            };
            _registry["mod_on_user"] = () =>
            {
                MessageHub.Instance.Publish(new OnModuleUserDefined());
                return SCRIPT_HANDLED;
            };

            // Player
            _registry["pc_on_damaged"] = () =>
            {
                MessageHub.Instance.Publish(new OnPlayerDamaged());
                return SCRIPT_HANDLED;
            };
            _registry["pc_on_heartbeat"] = () =>
            {
                MessageHub.Instance.Publish(new OnPlayerHeartbeat());
                return SCRIPT_HANDLED;
            };

            // Store
            _registry["on_close_store"] = () =>
            {
                MessageHub.Instance.Publish(new OnCloseStore());
                return SCRIPT_HANDLED;
            };
            _registry["on_open_store"] = () =>
            {
                MessageHub.Instance.Publish(new OnOpenStore());
                return SCRIPT_HANDLED;
            };

            // Grenades

            // Would really prefer these to be located closer to the specific feature rather than in the dispatcher, but that's a refactor for another day.
            _registry["grenade_bbomb_en"] = () =>
            {
                Item.Grenade.GrenadeAoe(GetEnteringObject(), "BACTABOMB");
                return SCRIPT_HANDLED;
            };
            _registry["grenade_bbomb_hb"] = () =>
            {
                NWObject oTarget = GetFirstInPersistentObject(NWGameObject.OBJECT_SELF);
                while (GetIsObjectValid(oTarget))
                {
                    Item.Grenade.GrenadeAoe(oTarget, "BACTABOMB");
                    //Get the next target in the AOE
                    oTarget = GetNextInPersistentObject(NWGameObject.OBJECT_SELF);
                }
                return SCRIPT_HANDLED;
            };
            _registry["grenade_gas_en"] = () =>
            {
                Item.Grenade.GrenadeAoe(GetEnteringObject(), "GAS");
                return SCRIPT_HANDLED;
            };
            _registry["grenade_gas_hb"] = () =>
            {
                NWObject oTarget = GetFirstInPersistentObject(NWGameObject.OBJECT_SELF);
                while (GetIsObjectValid(oTarget))
                {
                    Item.Grenade.GrenadeAoe(oTarget, "GAS");
                    //Get the next target in the AOE
                    oTarget = GetNextInPersistentObject(NWGameObject.OBJECT_SELF);
                }
                return SCRIPT_HANDLED;
            };
            _registry["grenade_incen_en"] = () =>
            {
                Item.Grenade.GrenadeAoe(GetEnteringObject(), "INCENDIARY");
                return SCRIPT_HANDLED;
            };
            _registry["grenade_incen_hb"] = () =>
            {
                NWObject oTarget = GetFirstInPersistentObject(NWGameObject.OBJECT_SELF);
                while (GetIsObjectValid(oTarget))
                {
                    Item.Grenade.GrenadeAoe(oTarget, "INCENDIARY");
                    //Get the next target in the AOE
                    oTarget = GetNextInPersistentObject(NWGameObject.OBJECT_SELF);
                }
                return SCRIPT_HANDLED;
            };
            _registry["grenade_smoke_en"] = () =>
            {
                Item.Grenade.GrenadeAoe(GetEnteringObject(), "SMOKE");
                return SCRIPT_HANDLED;
            };
            _registry["grenade_smoke_hb"] = () =>
            {
                NWObject oTarget = GetFirstInPersistentObject(NWGameObject.OBJECT_SELF);
                while (GetIsObjectValid(oTarget))
                {
                    Item.Grenade.GrenadeAoe(oTarget, "SMOKE");
                    //Get the next target in the AOE
                    oTarget = GetNextInPersistentObject(NWGameObject.OBJECT_SELF);
                }
                return SCRIPT_HANDLED;
            };

            RegisterConversationEvents();
        }

        private static void RegisterServiceSubscribeEvents()
        {
            // Use reflection to get all of the SubscribeEvents() methods in the SWLOR namespace.
            var typesInNamespace = Assembly.GetExecutingAssembly()
                .GetTypes()
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

        private void RegisterConversationEvents()
        {
            _registry["credit_check"] = CheckCredits.Main;
            _registry["open_store"] = () =>
            {
                OpenStore.Main();
                return SCRIPT_HANDLED;
            };
            _registry["tel_aban_station"] = () =>
            {
                TeleportToAbandonedStation.Main();
                return SCRIPT_HANDLED;
            };
            _registry["warp_to_wp"] = () =>
            {
                WarpToWaypoint.Main();
                return SCRIPT_HANDLED;
            };

            // Skills
            for(int x = 1; x <= 9; x++)
            {
                var nodeID = x;
                _registry[$"has_skill_or_{nodeID}"] = () => HasSkillRank.Check(nodeID, "OR") ? 1 : 0;
            }

            // Rimer Cards
            _registry["rimer_cpu_1"] = RimerCardsCPU1.Main;
            _registry["rimer_cpu_2"] = RimerCardsCPU2.Main;
            _registry["rimer_cpu_3"] = RimerCardsCPU3.Main;
            _registry["rimer_cpu_4"] = RimerCardsCPU4.Main;
            _registry["rimer_cpu_5"] = RimerCardsCPU5.Main;
            _registry["rimer_cpu_6"] = RimerCardsCPU6.Main;
            _registry["rimer_cpu_7"] = RimerCardsCPU7.Main;
            _registry["rimer_cpu_8"] = RimerCardsCPU8.Main;
            _registry["rimer_cpu_9"] = RimerCardsCPU9.Main;

            // Key Items
            for (int x = 1; x <= 10; x++)
            {
                var nodeID = x;
                _registry[$"has_keyitems_{x}"] = () => KeyItemCheck.Check(nodeID, 1) ? 1 : 0;
                _registry[$"any_keyitems_{x}"] = () => KeyItemCheck.Check(nodeID, 2) ? 1 : 0;
            }

            // Quests
            for(int x = 1; x <= 10; x++)
            {
                var nodeID = x;
                _registry[$"accept_quest_{x}"] = () => QuestAccept.Check(nodeID) ? 1 : 0;
                _registry[$"next_state_{x}"] = () => QuestAdvance.Check(nodeID) ? 1 : 0;
                _registry[$"can_accept_{x}"] = () => QuestCanAccept.Check(nodeID) ? 1 : 0;
                _registry[$"collect_item_{x}"] = () => QuestCollectItem.Check(nodeID) ? 1 : 0;
                _registry[$"finish_quest_{x}"] = () => QuestComplete.Check(nodeID, 0) ? 1 : 0;
                _registry[$"has_quest_{x}"] = () => QuestCheck.Check(nodeID) ? 1 : 0;
                _registry[$"quest_done_{x}"] = () => QuestIsDone.Check(nodeID) ? 1 : 0;

                // States & Rules
                for(int y = 1; y <= 9; y++)
                {
                    var stateID = y;
                    _registry[$"on_qst{x}_state_{y}"] = () => QuestCheckState.Check(nodeID, stateID) ? 1 : 0;
                    _registry[$"fin_qst{x}_rule{y}"] = () => QuestComplete.Check(nodeID, stateID) ? 1 : 0;
                }
            }
        }

        /// <summary>
        /// Runs a registered script.
        /// </summary>
        /// <param name="scriptName">The NWN script name.</param>
        /// <returns>0 if the script has been handled, -1 if it has not.</returns>
        public int RunScript(string scriptName)
        {
            if (!_registry.ContainsKey(scriptName))
            {
                return SCRIPT_NOT_HANDLED;
            }

            try
            {
                return _registry[scriptName].Invoke();
            }
            catch (Exception ex)
            {
                Audit.Write(AuditGroup.Error, ex.ToMessageAndCompleteStacktrace());
                return SCRIPT_NOT_HANDLED;
            }
        }
    }
}
