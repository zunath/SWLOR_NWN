using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Feat;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.ValueObject;
using static NWN._;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;

namespace SWLOR.Game.Server.Service
{
    public static class CraftService
    {
        private const float BaseCraftDelay = 18.0f;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnAreaEnter>(message => OnAreaEnter());
            MessageHub.Instance.Subscribe<OnUseCraftingFeat>(messsage =>
            {
                NWPlayer player = NWGameObject.OBJECT_SELF;
                DialogService.StartConversation(player, player, "ModifyItemAppearance");
            });
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
            MessageHub.Instance.Subscribe<OnModuleUseFeat>(message => OnModuleUseFeat());
        }

        private static List<CraftBlueprint> GetCraftBlueprintsAvailableToPlayer(Guid playerID)
        {
            var pcPerks = DataService.PCPerk.GetAllByPlayerID(playerID).ToList();
            var pcSkills = DataService.PCSkill.GetAllByPlayerID(playerID).ToList();

            // This likely needs to be improved with additional indexes in the CraftBlueprint cache.
            // Will revisit this at some point in the future but I don't want to risk breaking existing functionality.
            return DataService.CraftBlueprint.GetAll().Where(x =>
            {
                // ReSharper disable once ReplaceWithSingleAssignment.True
                bool found = true;

                // Exclude blueprints which the player doesn't meet the required perk level for.
                var pcPerk = pcPerks.SingleOrDefault(p => p.PerkID == x.PerkID);
                int perkLevel = pcPerk == null ? 0 : pcPerk.PerkLevel;
                if (x.PerkID != null && perkLevel < x.RequiredPerkLevel)
                    found = false;

                // Exclude blueprints which the player doesn't meet the skill requirements for
                var pcSkill = pcSkills.Single(s => s.SkillID == x.SkillID);
                if (x.BaseLevel > pcSkill.Rank + 5)
                    found = false;

                return found;
            }).ToList();
        }

        public static List<CraftBlueprintCategory> GetCategoriesAvailableToPCByDeviceID(Guid playerID, int deviceID)
        {
            var blueprints = GetCraftBlueprintsAvailableToPlayer(playerID).Where(x => x.CraftDeviceID == deviceID);
            var categoryIDs = blueprints.Select(x => x.CraftCategoryID).Distinct();

            var categories = DataService.CraftBlueprintCategory.GetAllActiveByIDs(categoryIDs);
            return categories.ToList();
        }

        public static List<CraftBlueprint> GetPCBlueprintsByDeviceAndCategoryID(Guid playerID, int deviceID, int categoryID)
        {
            return GetCraftBlueprintsAvailableToPlayer(playerID).Where(x => x.CraftDeviceID == deviceID &&
                                                                                      x.CraftCategoryID == categoryID)
                .ToList();
        }

        public static string BuildBlueprintHeader(NWPlayer player, bool showAddedComponentList)
        {
            var model = GetPlayerCraftingData(player);
            var bp = model.Blueprint;
            int playerEL = CalculatePCEffectiveLevel(player, model.PlayerSkillRank, (SkillType)bp.SkillID);
            var baseStructure = bp.BaseStructureID == null ? null : DataService.BaseStructure.GetByID(Convert.ToInt32(bp.BaseStructureID));
            var mainComponent = DataService.ComponentType.GetByID(bp.MainComponentTypeID);
            var secondaryComponent = DataService.ComponentType.GetByID(bp.SecondaryComponentTypeID);
            var tertiaryComponent = DataService.ComponentType.GetByID(bp.TertiaryComponentTypeID);

            string header = ColorTokenService.Green("Blueprint: ") + bp.Quantity + "x " + bp.ItemName + "\n";
            header += ColorTokenService.Green("Level: ") + (model.AdjustedLevel < 0 ? 0 : model.AdjustedLevel) + " (Base: " + (bp.BaseLevel < 0 ? 0 : bp.BaseLevel) + ")\n";
            header += ColorTokenService.Green("Difficulty: ") + CalculateDifficultyDescription(playerEL, model.AdjustedLevel) + "\n";

            if (baseStructure != null)
            {
                header += ColorTokenService.Green("Raises Atmosphere: ");
                if (baseStructure.HasAtmosphere)
                {
                    header += ColorTokenService.Green("Yes");
                }
                else
                {
                    header += ColorTokenService.Red("No");
                }

                header += "\n";
            }

            header += ColorTokenService.Green("Required Components (Required/Maximum): ") + "\n\n";

            string mainCounts = " (" + (model.MainMinimum > 0 ? Convert.ToString(model.MainMinimum) : "Optional") + "/" + model.MainMaximum + ")";
            header += ColorTokenService.Green("Main: ") + mainComponent.Name + mainCounts + "\n";

            if (bp.SecondaryMinimum > 0 && bp.SecondaryComponentTypeID > 0)
            {
                string secondaryCounts = " (" + (model.SecondaryMinimum > 0 ? Convert.ToString(model.SecondaryMinimum) : "Optional") + "/" + model.SecondaryMaximum + ")";
                header += ColorTokenService.Green("Secondary: ") + secondaryComponent.Name + secondaryCounts + "\n";
            }
            if (bp.TertiaryMinimum > 0 && bp.TertiaryComponentTypeID > 0)
            {
                string tertiaryCounts = " (" + (model.TertiaryMinimum > 0 ? Convert.ToString(model.TertiaryMinimum) : "Optional") + "/" + model.TertiaryMaximum + ")";
                header += ColorTokenService.Green("Tertiary: ") + tertiaryComponent.Name + tertiaryCounts + "\n";
            }
            if (bp.EnhancementSlots > 0)
            {
                int nSlots = bp.EnhancementSlots;
                if (model.IsInitialized)
                {
                    // We have the player's stats, so tell them how many they can actually add.
                    if (model.PlayerPerkLevel / 2 < nSlots)
                    {
                        nSlots = model.PlayerPerkLevel / 2;
                    }
                }

                string enhancementSlots = " (0/" + Convert.ToString(nSlots) + ")";
                header += ColorTokenService.Green("Enhancement slots: ") + enhancementSlots + "\n";
            }

            if (showAddedComponentList)
            {
                header += "\n" + ColorTokenService.Green("Your components:") + "\n\n";
                if (!model.HasPlayerComponents) header += "No components selected yet!";
                else
                {
                    foreach (var item in model.MainComponents)
                    {
                        header += item.Name + "\n";
                    }
                    foreach (var item in model.SecondaryComponents)
                    {
                        header += item.Name + "\n";
                    }
                    foreach (var item in model.TertiaryComponents)
                    {
                        header += item.Name + "\n";
                    }
                    foreach (var item in model.EnhancementComponents)
                    {
                        header += item.Name + "\n";
                    }
                }

            }

            return header;
        }

        public static CraftBlueprint GetBlueprintByID(int craftBlueprintID)
        {
            return DataService.CraftBlueprint.GetByIDOrDefault(craftBlueprintID);
        }

        public static List<CraftBlueprintCategory> GetCategoriesAvailableToPC(Guid playerID)
        {
            var blueprintIDs = GetCraftBlueprintsAvailableToPlayer(playerID).Select(x => x.CraftCategoryID).Distinct();
            return DataService.CraftBlueprintCategory.GetAllActiveByIDs(blueprintIDs).ToList();
        }

        public static List<CraftBlueprint> GetPCBlueprintsByCategoryID(Guid playerID, int categoryID)
        {
            return GetCraftBlueprintsAvailableToPlayer(playerID).Where(x => x.CraftCategoryID == categoryID).ToList();
        }


        public static void CraftItem(NWPlayer oPC, NWPlaceable device)
        {
            var model = GetPlayerCraftingData(oPC);
            CraftBlueprint blueprint = DataService.CraftBlueprint.GetByID(model.BlueprintID);
            if (blueprint == null) return;

            if (oPC.IsBusy)
            {
                oPC.SendMessage("You are too busy right now.");
                return;
            }

            if (!model.CanBuildItem)
            {
                oPC.SendMessage("You are missing one or more components...");
                return;
            }

            oPC.IsBusy = true;

            float modifiedCraftDelay = CalculateCraftingDelay(oPC, blueprint.SkillID);
            oPC.AssignCommand(() =>
            {
                _.ClearAllActions();
                _.ActionPlayAnimation(ANIMATION_LOOPING_GET_MID, 1.0f, modifiedCraftDelay);
            });
            _.DelayCommand(1.0f * (modifiedCraftDelay / 2.0f), () =>
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_COM_BLOOD_SPARK_MEDIUM), device.Object);
            });
            Effect immobilize = _.EffectCutsceneImmobilize();
            immobilize = _.TagEffect(immobilize, "CRAFTING_IMMOBILIZATION");
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, immobilize, oPC.Object);

            NWNXPlayer.StartGuiTimingBar(oPC, modifiedCraftDelay, "");

            var @event = new OnCreateCraftedItem(oPC);
            oPC.DelayEvent(modifiedCraftDelay, @event);
        }


        public static float CalculateCraftingDelay(NWPlayer oPC, int skillID)
        {
            int atmosphere = CalculateAreaAtmosphereBonus(oPC.Area);
            PerkType perkType;
            float adjustedSpeed = 1.0f;
            SkillType skillType = (SkillType)skillID;

            // Identify which perk to use for this skill.
            if (skillType == SkillType.Weaponsmith) perkType = PerkType.SpeedyWeaponsmith;
            else if (skillType == SkillType.Armorsmith) perkType = PerkType.SpeedyArmorsmith;
            else if (skillType == SkillType.Cooking) perkType = PerkType.SpeedyCooking;
            else if (skillType == SkillType.Engineering) perkType = PerkType.SpeedyEngineering;
            else if (skillType == SkillType.Fabrication) perkType = PerkType.SpeedyFabrication;
            else if (skillType == SkillType.Medicine) perkType = PerkType.SpeedyMedicine;
            else if (skillType == SkillType.Harvesting) perkType = PerkType.SpeedyReassembly;
            else return BaseCraftDelay;

            int perkLevel = PerkService.GetCreaturePerkLevel(oPC, perkType);

            // Each perk level reduces crafting speed by 10%.
            switch (perkLevel)
            {
                case 1: adjustedSpeed = 0.9f; break;
                case 2: adjustedSpeed = 0.8f; break;
                case 3: adjustedSpeed = 0.7f; break;
                case 4: adjustedSpeed = 0.6f; break;
                case 5: adjustedSpeed = 0.5f; break;
                case 6: adjustedSpeed = 0.4f; break;
                case 7: adjustedSpeed = 0.3f; break;
                case 8: adjustedSpeed = 0.2f; break;
                case 9: adjustedSpeed = 0.1f; break;
                case 10: adjustedSpeed = 0.01f; break;
            }

            // Workshops with an atmosphere bonus decrease crafting time.
            if (atmosphere >= 45)
            {
                adjustedSpeed -= 0.2f;
            }
            else if (atmosphere >= 5)
            {
                adjustedSpeed -= 0.1f;
            }

            // Never fall below 1% of overall crafting time.
            if (adjustedSpeed <= 0.01f)
            {
                adjustedSpeed = 0.01f;
            }

            return BaseCraftDelay * adjustedSpeed;
        }

        public static string CalculateDifficultyDescription(int pcLevel, int blueprintLevel)
        {
            int delta = pcLevel - blueprintLevel;
            string difficulty = "";

            if (delta <= -5)
            {
                difficulty = ColorTokenService.Custom("Impossible", 255, 62, 150);
            }
            else if (delta >= 4)
            {
                difficulty = ColorTokenService.Custom("Trivial", 102, 255, 102);
            }
            else
            {
                switch (delta)
                {
                    case -4:
                        difficulty = ColorTokenService.Custom("Extremely Difficult", 220, 20, 60);
                        break;
                    case -3:
                        difficulty = ColorTokenService.Custom("Very Difficult", 255, 69, 0);
                        break;
                    case -2:
                        difficulty = ColorTokenService.Custom("Difficult", 255, 165, 0);
                        break;
                    case -1:
                        difficulty = ColorTokenService.Custom("Challenging", 238, 238, 0);
                        break;
                    case 0:
                        difficulty = ColorTokenService.Custom("Moderate", 255, 255, 255);
                        break;
                    case 1:
                        difficulty = ColorTokenService.Custom("Easy", 65, 105, 225);
                        break;
                    case 2:
                        difficulty = ColorTokenService.Custom("Very Easy", 113, 113, 198);
                        break;
                    case 3:
                        difficulty = ColorTokenService.Custom("Extremely Easy", 153, 255, 255);
                        break;
                }
            }


            return difficulty;
        }


        public static int CalculatePCEffectiveLevel(NWPlayer player, int skillRank, SkillType skill)
        {
            int effectiveLevel = skillRank;
            BackgroundType background = (BackgroundType)player.Class1;

            switch (skill)
            {
                case SkillType.Armorsmith:
                    if (background == BackgroundType.Armorsmith)
                        effectiveLevel++;
                    break;
                case SkillType.Cooking:
                    if (background == BackgroundType.Chef)
                        effectiveLevel++;
                    break;
                case SkillType.Weaponsmith:
                    if (background == BackgroundType.Weaponsmith)
                        effectiveLevel++;
                    break;
                case SkillType.Engineering:
                    if (background == BackgroundType.Engineer)
                        effectiveLevel++;
                    break;
                case SkillType.Fabrication:
                    if (background == BackgroundType.Fabricator)
                        effectiveLevel++;
                    break;
            }

            return effectiveLevel;
        }


        public static string GetIngotResref(string oreResref)
        {
            string ingotResref;
            switch (oreResref)
            {
                case "raw_veldite":
                    ingotResref = "ref_veldite";
                    break;
                case "raw_scordspar":
                    ingotResref = "ref_scordspar";
                    break;
                case "raw_plagionite":
                    ingotResref = "ref_plagionite";
                    break;
                case "raw_keromber":
                    ingotResref = "ref_keromber";
                    break;
                case "raw_jasioclase":
                    ingotResref = "ref_jasioclase";
                    break;
                case "raw_hemorgite":
                    ingotResref = "ref_hemorgite";
                    break;
                case "raw_ochne":
                    ingotResref = "ref_ochne";
                    break;
                case "raw_croknor":
                    ingotResref = "ref_croknor";
                    break;
                case "raw_arkoxit":
                    ingotResref = "ref_arkoxit";
                    break;
                case "raw_bisteiss":
                    ingotResref = "ref_bisteiss";
                    break;
                default:
                    return "";
            }

            return ingotResref;
        }

        public static int GetIngotLevel(string oreResref)
        {
            int level;
            switch (oreResref)
            {
                case "raw_veldite":
                    level = 3;
                    break;
                case "raw_scordspar":
                    level = 8;
                    break;
                case "raw_plagionite":
                    level = 13;
                    break;
                case "raw_keromber":
                    level = 18;
                    break;
                case "raw_jasioclase":
                    level = 23;
                    break;
                case "raw_hemorgite":
                    level = 28;
                    break;
                case "raw_ochne":
                    level = 33;
                    break;
                case "raw_croknor":
                    level = 38;
                    break;
                case "raw_arkoxit":
                    level = 43;
                    break;
                case "raw_bisteiss":
                    level = 48;
                    break;
                default:
                    return -1;
            }

            return level;
        }

        public static int GetIngotPerkLevel(string oreResref)
        {
            int level;
            switch (oreResref)
            {
                case "raw_veldite":
                case "power_core":
                    level = 1;
                    break;
                case "raw_scordspar":
                    level = 2;
                    break;
                case "raw_plagionite":
                    level = 3;
                    break;
                case "raw_keromber":
                    level = 4;
                    break;
                case "raw_jasioclase":
                    level = 5;
                    break;
                case "raw_hemorgite":
                    level = 6;
                    break;
                case "raw_ochne":
                    level = 7;
                    break;
                case "raw_croknor":
                    level = 8;
                    break;
                case "raw_arkoxit":
                    level = 9;
                    break;
                case "raw_bisteiss":
                    level = 10;
                    break;
                default:
                    return -1;
            }

            return level;
        }


        public static PCCraftingData GetPlayerCraftingData(NWPlayer player)
        {
            // Need to store the data outside of the conversation because of the constant
            // context switching between conversation and accessing placeable containers.
            // Conversation data is wiped when it closes.
            if (player.Data.ContainsKey("CRAFTING_MODEL"))
            {
                return player.Data["CRAFTING_MODEL"];
            }

            var model = new PCCraftingData();
            player.Data["CRAFTING_MODEL"] = model;
            return model;
        }

        public static void ClearPlayerCraftingData(NWPlayer player, bool destroyComponents = false)
        {
            var model = GetPlayerCraftingData(player);

            foreach (var item in model.MainComponents)
            {
                if (!destroyComponents)
                    _.CopyItem(item.Object, player.Object, TRUE);
                item.Destroy();
            }
            foreach (var item in model.SecondaryComponents)
            {
                if (!destroyComponents)
                    _.CopyItem(item.Object, player.Object, TRUE);
                item.Destroy();
            }
            foreach (var item in model.TertiaryComponents)
            {
                if (!destroyComponents)
                    _.CopyItem(item.Object, player.Object, TRUE);
                item.Destroy();
            }
            foreach (var item in model.EnhancementComponents)
            {
                if (!destroyComponents)
                    _.CopyItem(item.Object, player.Object, TRUE);
                item.Destroy();
            }

            if (!string.IsNullOrWhiteSpace(model.SerializedSalvageItem))
            {
                SerializationService.DeserializeItem(model.SerializedSalvageItem, player);
            }

            player.Data.Remove("CRAFTING_MODEL");
            player.DeleteLocalInt("CRAFT_BLUEPRINT_ID");

        }

        public static bool CanHandleChat(NWObject sender)
        {
            return sender.GetLocalInt("CRAFT_RENAMING_ITEM") == TRUE;
        }

        private static void OnModuleNWNXChat()
        {
            NWPlayer pc = NWNXChat.GetSender().Object;
            string newName = NWNXChat.GetMessage();

            if (!CanHandleChat(pc))
            {
                return;
            }

            NWNXChat.SkipMessage();
            NWItem renameItem = pc.GetLocalObject("CRAFT_RENAMING_ITEM_OBJECT");

            pc.DeleteLocalInt("CRAFT_RENAMING_ITEM");
            pc.DeleteLocalObject("CRAFT_RENAMING_ITEM_OBJECT");

            if (!renameItem.IsValid)
            {
                pc.SendMessage("Cannot find the item you were renaming.");
                return;
            }

            if (newName.Length < 3 || newName.Length > 64)
            {
                pc.SendMessage("Item names must be between 3 and 64 characters long.");
                return;
            }

            renameItem.Name = newName;

            pc.FloatingText("New name set!");
        }

        private static void OnModuleUseFeat()
        {
            NWPlayer pc = NWGameObject.OBJECT_SELF;
            int featID = NWNXEvents.OnFeatUsed_GetFeatID();

            if (featID != (int)CustomFeatType.RenameCraftedItem) return;
            pc.ClearAllActions();

            bool isSetting = pc.GetLocalInt("CRAFT_RENAMING_ITEM") == TRUE;
            NWItem renameItem = NWNXEvents.OnFeatUsed_GetTarget().Object;

            if (isSetting)
            {
                pc.SendMessage("You are no longer naming an item.");
                pc.DeleteLocalInt("CRAFT_RENAMING_ITEM");
                pc.DeleteLocalObject("CRAFT_RENAMING_ITEM_OBJECT");
                return;
            }

            string crafterPlayerID = renameItem.GetLocalString("CRAFTER_PLAYER_ID");
            if (string.IsNullOrWhiteSpace(crafterPlayerID) || new Guid(crafterPlayerID) != pc.GlobalID)
            {
                pc.SendMessage("You may only rename items which you have personally crafted.");
                return;
            }

            pc.SetLocalInt("CRAFT_RENAMING_ITEM", TRUE);
            pc.SetLocalObject("CRAFT_RENAMING_ITEM_OBJECT", renameItem);
            pc.SendMessage("Please enter in a name for this item. Length should be between 3 and 64 characters. Use this feat again to cancel this procedure.");
        }

        public static int CalculateAreaAtmosphereBonus(NWArea area)
        {
            // Building IDs are stored on the instanced area's local variables.
            string pcStructureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            if (string.IsNullOrWhiteSpace(pcStructureID)) return 0;

            // Pull the building structure from the database.
            Guid buildingID = new Guid(pcStructureID);
            var building = DataService.PCBaseStructure.GetByID(buildingID);

            // Building must be in "Workshop" mode in order for the atmosphere bonuses to take effect.
            if (building.StructureModeID != (int)StructureModeType.Workshop) return 0;

            // Get all child structures contained by this building which improve atmosphere.
            var structures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID(buildingID)
                    .Where(x =>
                    {
                        var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);
                        return baseStructure.HasAtmosphere;
                    });

            // Add up the total atmosphere rating, being careful not to go over the cap.
            int bonus = structures.Sum(x => 1 + x.StructureBonus);
            if (bonus > 75) bonus = 75;

            return bonus;
        }

        public static string GetAreaAtmosphereBonusText(NWArea area)
        {
            int bonus = CalculateAreaAtmosphereBonus(area);

            string craftingSpeedBonus = string.Empty;
            string propertyTransferBonus = string.Empty;
            string equipmentBonus = string.Empty;

            if (bonus >= 5)
            {
                craftingSpeedBonus = "Crafting speed increased by 10%\n";
            }
            if (bonus >= 15)
            {
                propertyTransferBonus = "Property transfer chance increased by 2%\n";
            }
            if (bonus >= 25)
            {
                equipmentBonus = "Equipment with +Crafting bonuses grant an additional +0.1% per stat.\n";
            }
            if (bonus >= 45)
            {
                craftingSpeedBonus = "Crafting speed increased by 20%\n";
            }
            if (bonus >= 60)
            {
                propertyTransferBonus = "Property transfer chance increased by 4%\n";
            }

            if (bonus >= 75)
            {
                equipmentBonus = "Equipment with +Crafting bonuses grant an additional +0.1% per stat.\n";
            }

            var text = string.Empty;

            if (!string.IsNullOrWhiteSpace(craftingSpeedBonus) &&
                !string.IsNullOrWhiteSpace(propertyTransferBonus) &&
                !string.IsNullOrWhiteSpace(equipmentBonus))
            {
                text = "Workshop Crafting Bonuses:\n\n";
                text += craftingSpeedBonus;
                text += propertyTransferBonus;
                text += equipmentBonus;
            }

            return text;
        }

        private static void OnAreaEnter()
        {
            NWArea area = NWGameObject.OBJECT_SELF;
            string bonuses = GetAreaAtmosphereBonusText(area);

            if (string.IsNullOrWhiteSpace(bonuses)) return;
            NWCreature entering = _.GetEnteringObject();

            entering.SendMessage(bonuses);
        }

        public static int CalculateReassemblyChance(NWPlayer player, int penalty)
        {
            const int BaseChance = 70;
            int harvesting = SkillService.GetPCSkillRank(player, SkillType.Harvesting);
            var itemBonuses = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.MolecularReassemblyProficiency);

            // Calculate the base chance after factoring in skills, perks, and items.
            int categoryChance = (int)(BaseChance + (harvesting / 2.5f) + perkLevel * 10 + itemBonuses.Harvesting / 3f);

            // Reduce the chance by the penalty. This penalty is generally determined by how many properties were already
            // applied during this batch.
            categoryChance -= penalty;

            // Keep bounds between 0 and 100
            if (categoryChance < 0) return 0;
            else if (categoryChance > 100) return 100;
            else return categoryChance;
        }

    }
}
