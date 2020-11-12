using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Area;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using Player = SWLOR.Game.Server.Legacy.Data.Entity.Player;
using Skill = SWLOR.Game.Server.Core.NWScript.Enum.Skill;

namespace SWLOR.Game.Server.Legacy.Service
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
            if (!player.IsPlayer) return;

            // Player is initialized but not in the DB. Wipe the tag and rerun them through initialization - something went wrong before.
            if (player.IsInitializedAsPlayer)
            {
                if (!DataService.Player.ExistsByID(player.GlobalID))
                {
                    SetTag(player, string.Empty);
                }
            }

            if (!player.IsInitializedAsPlayer)
            {
                player.DestroyAllInventoryItems();
                player.InitializePlayer();
                AssignCommand(player, () => TakeGoldFromCreature(GetGold(player), player, true));

                DelayCommand(0.5f, () =>
                {
                    GiveGoldToCreature(player, 100);
                });

                // Capture original stats before we level up the player.
                var str = Creature.GetRawAbilityScore(player, AbilityType.Strength);
                var con = Creature.GetRawAbilityScore(player, AbilityType.Constitution);
                var dex = Creature.GetRawAbilityScore(player, AbilityType.Dexterity);
                var @int = Creature.GetRawAbilityScore(player, AbilityType.Intelligence);
                var wis = Creature.GetRawAbilityScore(player, AbilityType.Wisdom);
                var cha = Creature.GetRawAbilityScore(player, AbilityType.Charisma);

                // Take player to level 5 in NWN levels so that we have access to more HP slots
                GiveXPToCreature(player, 10000);

                for (var level = 1; level <= 5; level++)
                {
                    LevelUpHenchman(player, player.Class1);
                }

                // Set stats back to how they were on entry.
                Creature.SetRawAbilityScore(player, AbilityType.Strength, str);
                Creature.SetRawAbilityScore(player, AbilityType.Constitution, con);
                Creature.SetRawAbilityScore(player, AbilityType.Dexterity, dex);
                Creature.SetRawAbilityScore(player, AbilityType.Intelligence, @int);
                Creature.SetRawAbilityScore(player, AbilityType.Wisdom, wis);
                Creature.SetRawAbilityScore(player, AbilityType.Charisma, cha);

                NWItem knife = (CreateItemOnObject("survival_knife", player));
                knife.Name = player.Name + "'s Survival Knife";
                knife.IsCursed = true;
                DurabilityService.SetMaxDurability(knife, 5);
                DurabilityService.SetDurability(knife, 5);
                
                NWItem book = (CreateItemOnObject("player_guide", player));
                book.Name = player.Name + "'s Player Guide";
                book.IsCursed = true;

                NWItem dyeKit = (CreateItemOnObject("tk_omnidye", player));
                dyeKit.IsCursed = true;
                
                var numberOfFeats = Creature.GetFeatCount(player);
                for (var currentFeat = numberOfFeats; currentFeat >= 0; currentFeat--)
                {
                    Creature.RemoveFeat(player, Creature.GetFeatByIndex(player, currentFeat - 1));
                }

                Creature.AddFeatByLevel(player, Feat.ArmorProficiencyLight, 1);
                Creature.AddFeatByLevel(player, Feat.ArmorProficiencyMedium, 1);
                Creature.AddFeatByLevel(player, Feat.ArmorProficiencyHeavy, 1);
                Creature.AddFeatByLevel(player, Feat.ShieldProficiency, 1);
                Creature.AddFeatByLevel(player, Feat.WeaponProficiencyExotic, 1);
                Creature.AddFeatByLevel(player, Feat.WeaponProficiencyMartial, 1);
                Creature.AddFeatByLevel(player, Feat.WeaponProficiencySimple, 1);
                Creature.AddFeatByLevel(player, Feat.UncannyDodge1, 1);
                Creature.AddFeatByLevel(player, Feat.StructureTool, 1);
                Creature.AddFeatByLevel(player, Feat.OpenRestMenu, 1);
                Creature.AddFeatByLevel(player, Feat.RenameCraftedItem, 1);
                Creature.AddFeatByLevel(player, Feat.ChatCommandTargeter, 1);

                foreach (var skillType in Enum.GetValues(typeof(Skill)))
                {
                    var skill = (Skill) skillType;
                    if (skill == Skill.Invalid || skill == Skill.AllSkills) continue;

                    Creature.SetSkillRank(player, skill, 0);
                }
                SetFortitudeSavingThrow(player, 0);
                SetReflexSavingThrow(player, 0);
                SetWillSavingThrow(player, 0);

                var classType = GetClassByPosition(1, player);

                for (var index = 0; index <= 255; index++)
                {
                    Creature.RemoveKnownSpell(player, classType, 0, index);
                }

                var entity = CreateDBPCEntity(player);
                DataService.SubmitDataChange(entity, DatabaseActionType.Insert);

                var skills = DataService.Skill.GetAll();
                foreach (var skill in skills)
                {
                    var pcSkill = new PCSkill
                    {
                        IsLocked = false,
                        SkillID = skill.ID,
                        PlayerID = entity.ID,
                        Rank = 0,
                        XP = 0
                    };
                    
                    DataService.SubmitDataChange(pcSkill, DatabaseActionType.Insert);
                }

                PlayerStatService.ApplyStatChanges(player, null, true);

                DelayCommand(1.0f, () => ApplyEffectToObject(DurationType.Instant, EffectHeal(999), player));

                InitializeHotBar(player);
            }

        }
        
        private static Player CreateDBPCEntity(NWPlayer player)
        {
            var race = (RacialType)player.RacialType;
            AssociationType assType; 
            var goodEvil = GetAlignmentGoodEvil(player);
            var lawChaos = GetAlignmentLawChaos(player);
            
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

            var sp = 5;
            if (race == RacialType.Human)
                sp++;

            var entity = new Player
            {
                ID = player.GlobalID,
                CharacterName = player.Name,
                HitPoints = player.CurrentHP,
                LocationAreaResref = GetResRef(GetAreaFromLocation(player.Location)),
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
                STRBase = Creature.GetRawAbilityScore(player, AbilityType.Strength),
                DEXBase = Creature.GetRawAbilityScore(player, AbilityType.Dexterity),
                CONBase = Creature.GetRawAbilityScore(player, AbilityType.Constitution),
                INTBase = Creature.GetRawAbilityScore(player, AbilityType.Intelligence),
                WISBase = Creature.GetRawAbilityScore(player, AbilityType.Wisdom),
                CHABase = Creature.GetRawAbilityScore(player, AbilityType.Charisma),
                TotalSPAcquired = 0,
                DisplayHelmet = true,
                PrimaryResidencePCBaseStructureID = null,
                PrimaryResidencePCBaseID = null,
                AssociationID = (int)assType,
                DisplayHolonet = true,
                DisplayDiscord = true, 
                XPBonus = 0,
                LeaseRate = 0,
                ModeDualPistol = false
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
            NWPlayer player = (GetEnteringObject());

            SaveLocation(player);
            if(player.IsPlayer)
                ExportSingleCharacter(player);
        }

        private static void LoadCharacter()
        {
            NWPlayer player = GetEnteringObject();
            if (!player.IsPlayer) return;

            var entity = GetPlayerEntity(player.GlobalID);

            if (entity == null) return;

            var hp = player.CurrentHP;
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
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), player);
            }

            // Handle item stats
            for (var itemSlot = 0; itemSlot < NumberOfInventorySlots; itemSlot++)
            {
                NWItem item = GetItemInSlot((InventorySlot)itemSlot, player);
                PlayerStatService.CalculateEffectiveStats(player, item);
            }
            PlayerStatService.ApplyStatChanges(player, null);


            player.IsBusy = false; // Just in case player logged out in the middle of an action.

            // Cleanup code in case people log out as spaceships.
            var appearance = (AppearanceType)player.Chest.GetLocalInt("APPEARANCE");
            if (appearance > 0 && appearance != GetAppearanceType(player))
            {
                SetCreatureAppearanceType(player, appearance);
                SetObjectVisualTransform(player, ObjectVisualTransform.Scale, 1.0f);
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
            NWPlayer player = GetEnteringObject();
            var config = DataService.ServerConfiguration.Get();
            var message = ColorToken.Green("Welcome to " + config.ServerName + "!\n\nMOTD: ") + ColorToken.White(config.MessageOfTheDay);

            DelayCommand(6.5f, () =>
            {
                player.SendMessage(message);
            });
        }

        private static void ApplyGhostwalk()
        {
            NWPlayer oPC = GetEnteringObject();

            if (!oPC.IsPlayer) return;

            var eGhostWalk = EffectCutsceneGhost();
            eGhostWalk = TagEffect(eGhostWalk, "GHOST_WALK");
            ApplyEffectToObject(DurationType.Permanent, eGhostWalk, oPC.Object);

        }

        private static void ApplyScriptEvents()
        {
            NWPlayer player = GetEnteringObject();
            if (!player.IsPlayer) return;

            // As of 2018-03-28 only the OnDialogue, OnHeartbeat, and OnUserDefined events fire for PCs.
            // The rest are included here for completeness sake.

            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_BLOCKED_BY_DOOR, "pc_on_blocked");
            SetEventScript(player.Object, EventScript.Creature_OnDamaged, "pc_on_damaged");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DEATH, "pc_on_death");
            SetEventScript(player.Object, EventScript.Creature_OnDialogue, "default");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_DISTURBED, "pc_on_disturb");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_END_COMBATROUND, "pc_on_endround");
            SetEventScript(player.Object, EventScript.Creature_OnHeartbeat, "pc_on_heartbeat");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_MELEE_ATTACKED, "pc_on_attacked");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_NOTICE, "pc_on_notice");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_RESTED, "pc_on_rested");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_SPAWN_IN, "pc_on_spawn");
            //NWScript.SetEventScript(oPC.Object, EVENT_SCRIPT_CREATURE_ON_SPELLCASTAT, "pc_on_spellcast");
            SetEventScript(player.Object, EventScript.Creature_OnUserDefined, "pc_on_user");
        }

        private static void OnModuleLeave()
        {
            NWPlayer player = GetExitingObject();
            SaveCharacter(player);
            SaveLocation(player);
        }

        public static void SaveCharacter(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            var entity = GetPlayerEntity(player);
            entity.CharacterName = player.Name;
            entity.HitPoints = player.CurrentHP;

            DataService.SubmitDataChange(entity, DatabaseActionType.Update);
        }

        public static void SaveLocation(NWPlayer player)
        {
            if (!player.IsPlayer) return;
            if (player.GetLocalInt("IS_SHIP") == 1) return;
            if (player.GetLocalInt("IS_GUNNER") == 1) return;

            var area = GetArea(player);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var areaIsInstance = AreaService.IsAreaInstance(area);
            if (GetIsObjectValid(area) && areaTag != "ooc_area" && areaTag != "tutorial" && !areaIsInstance)
            {
                LoggingService.Trace(TraceComponent.Space, "Saving location in area " + GetName(area));
                var entity = GetPlayerEntity(player.GlobalID);
                entity.LocationAreaResref = areaResref;
                entity.LocationX = player.Position.X;
                entity.LocationY = player.Position.Y;
                entity.LocationZ = player.Position.Z;
                entity.LocationOrientation = (player.Facing);
                entity.LocationInstanceID = null;

                if (string.IsNullOrWhiteSpace(entity.RespawnAreaResref))
                {
                    NWObject waypoint = GetWaypointByTag("DTH_DEFAULT_RESPAWN_POINT");
                    entity.RespawnAreaResref = GetResRef(waypoint.Area);
                    entity.RespawnLocationOrientation = waypoint.Facing;
                    entity.RespawnLocationX = waypoint.Position.X;
                    entity.RespawnLocationY = waypoint.Position.Y;
                    entity.RespawnLocationZ = waypoint.Position.Z;
                }

                DataService.SubmitDataChange(entity, DatabaseActionType.Update);
            }
            else if (areaIsInstance)
            {
                LoggingService.Trace(TraceComponent.Space, "Saving location in instance area " + GetName(area));
                var instanceID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    instanceID = GetLocalString(area, "PC_BASE_ID");
                }

                LoggingService.Trace(TraceComponent.Space, "Saving character in instance ID: " + instanceID);

                if (!string.IsNullOrWhiteSpace(instanceID))
                {
                    var entity = GetPlayerEntity(player.GlobalID);
                    entity.LocationAreaResref = areaResref;
                    entity.LocationX = player.Position.X;
                    entity.LocationY = player.Position.Y;
                    entity.LocationZ = player.Position.Z;
                    entity.LocationOrientation = (player.Facing);
                    entity.LocationInstanceID = new Guid(instanceID);

                    DataService.SubmitDataChange(entity, DatabaseActionType.Update);
                }
            }
        }
                
        private static void InitializeHotBar(NWPlayer player)
        {
            var openRestMenu = PlayerQuickBarSlot.UseFeat(Feat.OpenRestMenu);
            var structure = PlayerQuickBarSlot.UseFeat(Feat.StructureTool);
            var renameCraftedItem = PlayerQuickBarSlot.UseFeat(Feat.RenameCraftedItem);
            var chatCommandTargeter = PlayerQuickBarSlot.UseFeat(Feat.ChatCommandTargeter);

            Core.NWNX.Player.SetQuickBarSlot(player, 0, openRestMenu);
            Core.NWNX.Player.SetQuickBarSlot(player, 1, structure);
            Core.NWNX.Player.SetQuickBarSlot(player, 2, renameCraftedItem);
            Core.NWNX.Player.SetQuickBarSlot(player, 3, chatCommandTargeter);
        }

        private static void OnModuleUseFeat()
        {
            NWPlayer pc = (OBJECT_SELF);
            var featID = Convert.ToInt32(Events.GetEventData("FEAT_ID"));

            if (featID != (int)Feat.OpenRestMenu) return;
            pc.ClearAllActions();
            DialogService.StartConversation(pc, pc, "RestMenu");
        }

        private static void OnModuleHeartbeat()
        {
            var playerIDs = NWModule.Get().Players.Where(x => x.IsPlayer).Select(x => x.GlobalID).ToArray();
            var entities = DataService.Player.GetAllByIDs(playerIDs).ToList();

            foreach (var player in NWModule.Get().Players)
            {
                var entity = entities.SingleOrDefault(x => x.ID == player.GlobalID);
                if (entity == null) continue;

                HandleRegenerationTick(player, entity);
                HandleFPRegenerationTick(player, entity);

                DataService.SubmitDataChange(entity, DatabaseActionType.Update);
            }

            SaveCharacters();
        }
        
        private static void HandleRegenerationTick(NWPlayer oPC, Player entity)
        {
            entity.RegenerationTick = entity.RegenerationTick - 1;
            var rate = 5;
            var amount = entity.HPRegenerationAmount;
            

            if (entity.RegenerationTick <= 0)
            {
                if (oPC.CurrentHP < oPC.MaxHP)
                {
                    var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(oPC);
                    // CON bonus
                    var con = (oPC.ConstitutionModifier / 2);
                    if (con > 0)
                    {
                        amount += con;
                    }
                    amount += effectiveStats.HPRegen;

                    if (oPC.Chest.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        var sturdinessLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.Sturdiness);
                        if (sturdinessLevel > 0)
                        {
                            amount += sturdinessLevel + 1;
                        }
                    }
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), oPC.Object);
                }

                entity.RegenerationTick = rate;
            }
        }

        private static void HandleFPRegenerationTick(NWPlayer oPC, Player entity)
        {
            entity.CurrentFPTick = entity.CurrentFPTick - 1;
            var rate = 5;
            var amount = 1;

            if (entity.CurrentFPTick <= 0)
            {
                if (entity.CurrentFP < entity.MaxFP)
                {
                    var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(oPC);
                    // CHA bonus
                    var cha = oPC.CharismaModifier;
                    if (cha > 0)
                    {
                        amount += cha;
                    }
                    amount += effectiveStats.FPRegen;

                    if (oPC.Chest.CustomItemType == CustomItemType.ForceArmor)
                    {
                        var clarityLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.Clarity);
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
            var currentTick = NWModule.Get().GetLocalInt("SAVE_CHARACTERS_TICK") + 1;

            if (currentTick >= 10)
            {
                ExportAllCharacters();
                currentTick = 0;
            }

            NWModule.Get().SetLocalInt("SAVE_CHARACTERS_TICK", currentTick);
        }
    }
}
