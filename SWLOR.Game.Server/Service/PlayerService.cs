﻿using System;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;


using static NWN._;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnAreaEnter>(message => OnAreaEnter());
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleHeartbeat>(message => OnModuleHeartbeat());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
            MessageHub.Instance.Subscribe<OnModuleUseFeat>(message => OnModuleUseFeat());
        }

        public static void InitializePlayer(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));
            if (!player.IsPlayer) return;

            // Player is initialized but not in the DB. Wipe the tag and rerun them through initialization - something went wrong before.
            if (player.IsInitializedAsPlayer)
            {
                if (!DataService.Player.ExistsByID(player.GlobalID))
                {
                    _.SetTag(player, string.Empty);
                }
            }

            if (!player.IsInitializedAsPlayer)
            {
                player.DestroyAllInventoryItems();
                player.InitializePlayer();
                _.AssignCommand(player, () => _.TakeGoldFromCreature(_.GetGold(player), player, true));

                _.DelayCommand(0.5f, () =>
                {
                    _.GiveGoldToCreature(player, 100);
                });

                // Capture original stats before we level up the player.
                int str = NWNXCreature.GetRawAbilityScore(player, Ability.Strength);
                int con = NWNXCreature.GetRawAbilityScore(player, Ability.Constitution);
                int dex = NWNXCreature.GetRawAbilityScore(player, Ability.Dexterity);
                int @int = NWNXCreature.GetRawAbilityScore(player, Ability.Intelligence);
                int wis = NWNXCreature.GetRawAbilityScore(player, Ability.Wisdom);
                int cha = NWNXCreature.GetRawAbilityScore(player, Ability.Charisma);

                // Take player to level 5 in NWN levels so that we have access to more HP slots
                _.GiveXPToCreature(player, 10000);

                for (int level = 1; level <= 5; level++)
                {
                    _.LevelUpHenchman(player, player.Class1);
                }

                // Set stats back to how they were on entry.
                NWNXCreature.SetRawAbilityScore(player, Ability.Strength, str);
                NWNXCreature.SetRawAbilityScore(player, Ability.Constitution, con);
                NWNXCreature.SetRawAbilityScore(player, Ability.Dexterity, dex);
                NWNXCreature.SetRawAbilityScore(player, Ability.Intelligence, @int);
                NWNXCreature.SetRawAbilityScore(player, Ability.Wisdom, wis);
                NWNXCreature.SetRawAbilityScore(player, Ability.Charisma, cha);

                NWItem knife = (_.CreateItemOnObject("survival_knife", player));
                knife.Name = player.Name + "'s Survival Knife";
                knife.IsCursed = true;
                DurabilityService.SetMaxDurability(knife, 5);
                DurabilityService.SetDurability(knife, 5);
                
                NWItem book = (_.CreateItemOnObject("player_guide", player));
                book.Name = player.Name + "'s Player Guide";
                book.IsCursed = true;

                NWItem dyeKit = (_.CreateItemOnObject("tk_omnidye", player));
                dyeKit.IsCursed = true;
                
                int numberOfFeats = NWNXCreature.GetFeatCount(player);
                for (int currentFeat = numberOfFeats; currentFeat >= 0; currentFeat--)
                {
                    NWNXCreature.RemoveFeat(player, NWNXCreature.GetFeatByIndex(player, currentFeat - 1));
                }

                NWNXCreature.AddFeatByLevel(player, Feat.Armor_Proficiency_Light, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.Armor_Proficiency_Medium, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.Armor_Proficiency_Heavy, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.Shield_Proficiency, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.Weapon_Proficiency_Exotic, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.Weapon_Proficiency_Martial, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.Weapon_Proficiency_Simple, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.Uncanny_Dodge_1, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.StructureManagementTool, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.OpenRestMenu, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.RenameCraftedItem, 1);
                NWNXCreature.AddFeatByLevel(player, Feat.ChatCommandTargeter, 1);

                for (int iCurSkill = 1; iCurSkill <= 27; iCurSkill++)
                {
                    NWNXCreature.SetSkillRank(player, iCurSkill - 1, 0);
                }
                _.SetFortitudeSavingThrow(player, 0);
                _.SetReflexSavingThrow(player, 0);
                _.SetWillSavingThrow(player, 0);

                var classID = _.GetClassByPosition(ClassPosition.First, player);

                for (int index = 0; index <= 255; index++)
                {
                    NWNXCreature.RemoveKnownSpell(player, classID, 0, index);
                }

                Player entity = CreateDBPCEntity(player);

                var skills = SkillService.GetAllSkills();
                foreach (var skill in skills)
                {
                    var pcSkill = new PCSkill
                    {
                        IsLocked = false,
                        Rank = 0,
                        XP = 0
                    };

                    entity.Skills[skill] = pcSkill;
                }

                DataService.Set(entity);
                RaceService.ApplyDefaultAppearance(player);
                NWNXCreature.SetAlignmentLawChaos(player, 50);
                NWNXCreature.SetAlignmentGoodEvil(player, 50);
                BackgroundService.ApplyBackgroundBonuses(player);

                PlayerStatService.ApplyStatChanges(player, null, true);
                LanguageService.InitializePlayerLanguages(player);

                _.DelayCommand(1.0f, () => _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(999), player));

                InitializeHotBar(player);
            }

        }
        
        private static Player CreateDBPCEntity(NWPlayer player)
        {
            RacialType race = (RacialType)player.RacialType;
            AssociationType assType; 
            var goodEvil = _.GetAlignmentGoodEvil(player);
            var lawChaos = _.GetAlignmentLawChaos(player);
            
            // Jedi Order -- Mandalorian -- Sith Empire
            if(goodEvil == Alignment.Good && lawChaos == Alignment.Lawful)
            {
                assType = AssociationType.JediOrder;
            }
            else if(goodEvil == Alignment.Good && lawChaos == Alignment.Neutral)
            {
                assType = AssociationType.Mandalorian;
            }
            else if(goodEvil == Alignment.Good && lawChaos == Alignment.Chaotic)
            {
                assType = AssociationType.SithEmpire;
            }

            // Smugglers -- Unaligned -- Hutt Cartel
            else if(goodEvil == Alignment.Neutral && lawChaos == Alignment.Lawful)
            {
                assType = AssociationType.Smugglers;
            }
            else if(goodEvil == Alignment.Neutral && lawChaos == Alignment.Neutral)
            {
                assType = AssociationType.Unaligned;
            }
            else if(goodEvil == Alignment.Neutral && lawChaos == Alignment.Chaotic)
            {
                assType = AssociationType.HuttCartel;
            }

            // Republic -- Czerka -- Sith Order
            else if(goodEvil == Alignment.Evil && lawChaos == Alignment.Lawful)
            {
                assType = AssociationType.Republic;
            }
            else if(goodEvil == Alignment.Evil && lawChaos == Alignment.Neutral)
            {
                assType = AssociationType.Czerka;
            }
            else if(goodEvil == Alignment.Evil && lawChaos == Alignment.Chaotic)
            {
                assType = AssociationType.SithOrder;
            }
            else
            {
                throw new Exception("Association type not found. GoodEvil = " + goodEvil + ", LawChaos = " + lawChaos);
            }

            int sp = 5;
            if (race == RacialType.Human)
                sp++;

            Player entity = new Player
            {
                ID = player.GlobalID,
                CharacterName = player.Name,
                HitPoints = player.CurrentHP,
                LocationAreaResref = _.GetResRef(_.GetAreaFromLocation(player.Location)),
                LocationX = player.Position.X,
                LocationY = player.Position.Y,
                LocationZ = player.Position.Z,
                LocationOrientation = player.Facing,
                CreateTimestamp = DateTime.UtcNow,
                UnallocatedSP = sp,
                HPRegenerationAmount = 1,
                RegenerationTick = 20,
                RegenerationRate = 0,
                VersionNumber = 1,
                MaxFP = 0,
                CurrentFP = 0,
                CurrentFPTick = 20,
                RespawnAreaResref = string.Empty,
                RespawnLocationX = 0.0f,
                RespawnLocationY = 0.0f,
                RespawnLocationZ = 0.0f,
                RespawnLocationOrientation = 0.0f,
                DateSanctuaryEnds = DateTime.UtcNow + TimeSpan.FromDays(3),
                IsSanctuaryOverrideEnabled = false,
                STRBase = NWNXCreature.GetRawAbilityScore(player, Ability.Strength),
                DEXBase = NWNXCreature.GetRawAbilityScore(player, Ability.Dexterity),
                CONBase = NWNXCreature.GetRawAbilityScore(player, Ability.Constitution),
                INTBase = NWNXCreature.GetRawAbilityScore(player, Ability.Intelligence),
                WISBase = NWNXCreature.GetRawAbilityScore(player, Ability.Wisdom),
                CHABase = NWNXCreature.GetRawAbilityScore(player, Ability.Charisma),
                TotalSPAcquired = 0,
                DisplayHelmet = true,
                PrimaryResidencePCBaseStructureID = null,
                PrimaryResidencePCBaseID = null,
                AssociationID = assType,
                DisplayHolonet = true,
                DisplayDiscord = true, 
                XPBonus = 0,
                LeaseRate = 0
            };

            return entity;
        }

        public static Player GetPlayerEntity(NWPlayer player)
        {
            if(player == null) throw new ArgumentNullException(nameof(player));
            if(!player.IsPlayer) throw new ArgumentException(nameof(player) + " must be a player.", nameof(player));

            return DataService.Player.GetByID(player.GlobalID);
        }

        public static Player GetPlayerEntity(Guid playerID)
        {
            if (playerID == null) throw new ArgumentException("Invalid player ID.", nameof(playerID));
            return DataService.Player.GetByID(playerID);
        }

        private static void OnAreaEnter()
        {
            NWPlayer player = (_.GetEnteringObject());

            SaveLocation(player);
            if(player.IsPlayer)
                _.ExportSingleCharacter(player);
        }

        private static void LoadCharacter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            Player entity = GetPlayerEntity(player.GlobalID);

            if (entity == null) return;

            int hp = player.CurrentHP;
            int damage;
            if (entity.HitPoints < 0)
            {
                damage = hp + Math.Abs(entity.HitPoints);
            }
            else
            {
                damage = hp - entity.HitPoints;
            }

            if (damage != 0)
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage), player);
            }

            player.IsBusy = false; // Just in case player logged out in the middle of an action.

            // Cleanup code in case people log out as spaceships.
            var appearance = (AppearanceType)player.Chest.GetLocalInt("APPEARANCE");
            if (appearance > 0 && appearance != _.GetAppearanceType(player))
            {
                _.SetCreatureAppearanceType(player, appearance);
                _.SetObjectVisualTransform(player, ObjectVisualTransform.Scale, 1.0f);
            }
        }

        private static void OnModuleEnter()
        {
            LoadCharacter();
            ShowMOTD();
            ApplyGhostwalk();
            ApplyScriptEvents();
        }

        private static void ShowMOTD()
        {
            NWPlayer player = _.GetEnteringObject();
            ServerConfiguration config = DataService.ServerConfiguration.Get();
            string message = ColorTokenService.Green("Welcome to " + config.ServerName + "!\n\nMOTD: ") + ColorTokenService.White(config.MessageOfTheDay);

            _.DelayCommand(6.5f, () =>
            {
                player.SendMessage(message);
            });
        }

        private static void ApplyGhostwalk()
        {
            NWPlayer oPC = _.GetEnteringObject();

            if (!oPC.IsPlayer) return;

            Effect eGhostWalk = _.EffectCutsceneGhost();
            eGhostWalk = _.TagEffect(eGhostWalk, "GHOST_WALK");
            _.ApplyEffectToObject(DurationType.Permanent, eGhostWalk, oPC.Object);

        }

        private static void ApplyScriptEvents()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            // As of 2018-03-28 only the OnDialogue, OnHeartbeat, and OnUserDefined events fire for PCs.
            // The rest are included here for completeness sake.

            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR, "pc_on_blocked");
            _.SetEventScript(player.Object, EventScriptCreature.OnDamaged, "pc_on_damaged");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DEATH, "pc_on_death");
            _.SetEventScript(player.Object, EventScriptCreature.OnDialogue, "default");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DISTURBED, "pc_on_disturb");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND, "pc_on_endround");
            _.SetEventScript(player.Object, EventScriptCreature.OnHeartbeat, "pc_on_heartbeat");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED, "pc_on_attacked");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_NOTICE, "pc_on_notice");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_RESTED, "pc_on_rested");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_SPAWN_IN, "pc_on_spawn");
            //_.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT, "pc_on_spellcast");
            _.SetEventScript(player.Object, EventScriptCreature.OnUserDefinedEvent, "pc_on_user");
        }

        private static void OnModuleLeave()
        {
            NWPlayer player = _.GetExitingObject();
            SaveCharacter(player);
            SaveLocation(player);
        }

        public static void SaveCharacter(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            Player entity = GetPlayerEntity(player);
            entity.CharacterName = player.Name;
            entity.HitPoints = player.CurrentHP;

            DataService.Set(entity);
        }

        public static void SaveLocation(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            if (player.GetLocalInt("IS_SHIP") == 1) return;
            if (player.GetLocalInt("IS_GUNNER") == 1) return;
            
            NWArea area = player.Area;
            if (area.IsValid && area.Tag != "ooc_area" && area.Tag != "tutorial" && !area.IsInstance)
            {
                LoggingService.Trace(TraceComponent.Space, "Saving location in area " + _.GetName(area));
                Player entity = GetPlayerEntity(player.GlobalID);
                entity.LocationAreaResref = area.Resref;
                entity.LocationX = player.Position.X;
                entity.LocationY = player.Position.Y;
                entity.LocationZ = player.Position.Z;
                entity.LocationOrientation = (player.Facing);
                entity.LocationInstanceID = null;

                if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
                {
                    NWObject waypoint = _.GetWaypointByTag("DTH_DEFAULT_RESPAWN_POINT");
                    entity.RespawnAreaResref = waypoint.Area.Resref;
                    entity.RespawnLocationOrientation = waypoint.Facing;
                    entity.RespawnLocationX = waypoint.Position.X;
                    entity.RespawnLocationY = waypoint.Position.Y;
                    entity.RespawnLocationZ = waypoint.Position.Z;
                }

                DataService.Set(entity);
            }
            else if (area.IsInstance)
            {
                LoggingService.Trace(TraceComponent.Space, "Saving location in instance area " + _.GetName(area));
                string instanceID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    instanceID = area.GetLocalString("PC_BASE_ID");
                }

                LoggingService.Trace(TraceComponent.Space, "Saving character in instance ID: " + instanceID);

                if (!string.IsNullOrWhiteSpace(instanceID))
                {
                    Player entity = GetPlayerEntity(player.GlobalID);
                    entity.LocationAreaResref = area.Resref;
                    entity.LocationX = player.Position.X;
                    entity.LocationY = player.Position.Y;
                    entity.LocationZ = player.Position.Z;
                    entity.LocationOrientation = (player.Facing);
                    entity.LocationInstanceID = new Guid(instanceID);

                    DataService.Set(entity);
                }
            }
        }
                
        private static void InitializeHotBar(NWPlayer player)
        {
            var openRestMenu = NWNXPlayerQuickBarSlot.UseFeat(Feat.OpenRestMenu);
            var structure = NWNXPlayerQuickBarSlot.UseFeat(Feat.StructureManagementTool);
            var renameCraftedItem = NWNXPlayerQuickBarSlot.UseFeat(Feat.RenameCraftedItem);
            var chatCommandTargeter = NWNXPlayerQuickBarSlot.UseFeat(Feat.ChatCommandTargeter);

            NWNXPlayer.SetQuickBarSlot(player, 0, openRestMenu);
            NWNXPlayer.SetQuickBarSlot(player, 1, structure);
            NWNXPlayer.SetQuickBarSlot(player, 2, renameCraftedItem);
            NWNXPlayer.SetQuickBarSlot(player, 3, chatCommandTargeter);
        }

        private static void OnModuleUseFeat()
        {
            NWPlayer pc = (NWGameObject.OBJECT_SELF);
            var featID = NWNXEvents.OnFeatUsed_GetFeat();

            if (featID != Feat.OpenRestMenu) return;
            pc.ClearAllActions();
            DialogService.StartConversation(pc, pc, "RestMenu");
        }

        private static void OnModuleHeartbeat()
        {
            Guid[] playerIDs = NWModule.Get().Players.Where(x => x.IsPlayer).Select(x => x.GlobalID).ToArray();
            var entities = DataService.Player.GetAllByIDs(playerIDs).ToList();

            foreach (var player in NWModule.Get().Players)
            {
                var entity = entities.SingleOrDefault(x => x.ID == player.GlobalID);
                if (entity == null) continue;

                HandleRegenerationTick(player, entity);
                HandleFPRegenerationTick(player, entity);

                DataService.Set(entity);
            }

            SaveCharacters();
        }
        
        private static void HandleRegenerationTick(NWPlayer oPC, Data.Entity.Player entity)
        {
            entity.RegenerationTick = entity.RegenerationTick - 1;
            int rate = 5;
            int amount = entity.HPRegenerationAmount;

            if (entity.RegenerationTick <= 0)
            {
                if (oPC.CurrentHP < oPC.MaxHP)
                {
                    var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(oPC);
                    // CON bonus
                    int con = oPC.ConstitutionModifier;
                    if (con > 0)
                    {
                        amount += con;
                    }
                    amount += effectiveStats.HPRegen;

                    if (oPC.Chest.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        int sturdinessLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.Sturdiness);
                        if (sturdinessLevel > 0)
                        {
                            amount += sturdinessLevel + 1;
                        }
                    }
                    _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(amount), oPC.Object);
                }

                entity.RegenerationTick = rate;
            }
        }

        private static void HandleFPRegenerationTick(NWPlayer oPC, Data.Entity.Player entity)
        {
            entity.CurrentFPTick = entity.CurrentFPTick - 1;
            int rate = 5;
            int amount = 1;

            if (entity.CurrentFPTick <= 0)
            {
                if (entity.CurrentFP < entity.MaxFP)
                {
                    var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(oPC);
                    // CHA bonus
                    int cha = oPC.CharismaModifier;
                    if (cha > 0)
                    {
                        amount += cha;
                    }
                    amount += effectiveStats.FPRegen;

                    if (oPC.Chest.CustomItemType == CustomItemType.ForceArmor)
                    {
                        int clarityLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.Clarity);
                        if (clarityLevel > 0)
                        {
                            amount += clarityLevel + 1;
                        }
                    }

                    entity = AbilityService.RestorePlayerFP(oPC, amount, entity);
                }

                entity.CurrentFPTick = rate;
            }
        }

        // Export all characters every minute.
        private static void SaveCharacters()
        {
            int currentTick = NWModule.Get().GetLocalInt("SAVE_CHARACTERS_TICK") + 1;

            if (currentTick >= 10)
            {
                _.ExportAllCharacters();
                currentTick = 0;
            }

            NWModule.Get().SetLocalInt("SAVE_CHARACTERS_TICK", currentTick);
        }
    }
}
