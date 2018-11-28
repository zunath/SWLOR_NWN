using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.Delayed;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;
using ComponentType = SWLOR.Game.Server.Data.Entity.ComponentType;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class CraftService : ICraftService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IPerkService _perk;
        private readonly IColorTokenService _color;
        private readonly INWNXPlayer _nwnxPlayer;
        private readonly INWNXEvents _nwnxEvents;
        private readonly INWNXChat _nwnxChat;

        public CraftService(
            INWScript script,
            IDataService data,
            IPerkService perk,
            IColorTokenService color,
            INWNXPlayer nwnxPlayer,
            INWNXEvents nwnxEvents,
            INWNXChat nwnxChat)
        {
            _ = script;
            _data = data;
            _perk = perk;
            _color = color;
            _nwnxPlayer = nwnxPlayer;
            _nwnxEvents = nwnxEvents;
            _nwnxChat = nwnxChat;
        }

        private const float BaseCraftDelay = 18.0f;

        private List<CraftBlueprint> GetCraftBlueprintsAvailableToPlayer(Guid playerID)
        {
            var pcPerks = _data.Where<PCPerk>(x => x.PlayerID == playerID).ToList();
            var pcSkills = _data.Where<PCSkill>(x => x.PlayerID == playerID).ToList();

            return _data.Where<CraftBlueprint>(x =>
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

        public List<CraftBlueprintCategory> GetCategoriesAvailableToPCByDeviceID(Guid playerID, int deviceID)
        {
            var blueprints = GetCraftBlueprintsAvailableToPlayer(playerID).Where(x => x.CraftDeviceID == deviceID);
            var categoryIDs = blueprints.Select(x => x.CraftCategoryID).Distinct();

            var categories = _data.Where<CraftBlueprintCategory>(x => x.IsActive &&
                                                                      categoryIDs.Contains(x.ID));
            return categories.ToList();
        }

        public List<CraftBlueprint> GetPCBlueprintsByDeviceAndCategoryID(Guid playerID, int deviceID, int categoryID)
        {
            return GetCraftBlueprintsAvailableToPlayer(playerID).Where(x => x.CraftDeviceID == deviceID && 
                                                                                      x.CraftCategoryID == categoryID)
                .ToList();
        }

        public string BuildBlueprintHeader(NWPlayer player, int blueprintID, bool showAddedComponentList)
        {
            var model = GetPlayerCraftingData(player);
            var bp = model.Blueprint;
            int playerEL = CalculatePCEffectiveLevel(player, model.PlayerSkillRank, (SkillType)bp.SkillID);
            var baseStructure = bp.BaseStructureID == null ? null : _data.Get<BaseStructure>(bp.BaseStructureID);
            var mainComponent = _data.Get<ComponentType>(bp.MainComponentTypeID);
            var secondaryComponent = _data.Get<ComponentType>(bp.SecondaryComponentTypeID);
            var tertiaryComponent = _data.Get<ComponentType>(bp.TertiaryComponentTypeID);

            string header = _color.Green("Blueprint: ") + bp.Quantity + "x " + bp.ItemName + "\n";
            header += _color.Green("Level: ") + (model.AdjustedLevel < 0 ? 0 : model.AdjustedLevel) + " (Base: " + (bp.BaseLevel < 0 ? 0 : bp.BaseLevel) + ")\n";
            header += _color.Green("Difficulty: ") + CalculateDifficultyDescription(playerEL, model.AdjustedLevel) + "\n";
            
            if (baseStructure != null)
            {
                header += _color.Green("Raises Atmosphere: ");
                if (baseStructure.HasAtmosphere)
                {
                    header += _color.Green("Yes");
                }
                else
                {
                    header += _color.Red("No");
                }

                header += "\n";
            }

            header += _color.Green("Required Components (Required/Maximum): ") + "\n\n";

            string mainCounts = " (" + (model.MainMinimum > 0 ? Convert.ToString(model.MainMinimum) : "Optional") + "/" + model.MainMaximum + ")";
            header += _color.Green("Main: ") + mainComponent.Name + mainCounts + "\n";

            if (bp.SecondaryMinimum > 0 && bp.SecondaryComponentTypeID > 0)
            {
                string secondaryCounts = " (" + (model.SecondaryMinimum > 0 ? Convert.ToString(model.SecondaryMinimum) : "Optional") + "/" + model.SecondaryMaximum + ")";
                header += _color.Green("Secondary: ") + secondaryComponent.Name + secondaryCounts + "\n";
            }
            if (bp.TertiaryMinimum > 0 && bp.TertiaryComponentTypeID > 0)
            {
                string tertiaryCounts = " (" + (model.TertiaryMinimum > 0 ? Convert.ToString(model.TertiaryMinimum) : "Optional") + "/" + model.TertiaryMaximum + ")";
                header += _color.Green("Tertiary: ") + tertiaryComponent.Name + tertiaryCounts + "\n";
            }

            if (showAddedComponentList)
            {
                header += "\n" + _color.Green("Your components:") + "\n\n";
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

        public CraftBlueprint GetBlueprintByID(int craftBlueprintID)
        {
            return _data.SingleOrDefault<CraftBlueprint>(x => x.ID == craftBlueprintID);
        }

        public List<CraftBlueprintCategory> GetCategoriesAvailableToPC(Guid playerID)
        {
            var blueprints = GetCraftBlueprintsAvailableToPlayer(playerID).Select(x => x.CraftCategoryID).Distinct();
            return _data.Where<CraftBlueprintCategory>(x => blueprints.Contains(x.ID)).ToList();
        }

        public List<CraftBlueprint> GetPCBlueprintsByCategoryID(Guid playerID, int categoryID)
        {
            return GetCraftBlueprintsAvailableToPlayer(playerID).Where(x => x.CraftCategoryID == categoryID).ToList();
        }


        public void CraftItem(NWPlayer oPC, NWPlaceable device)
        {
            var model = GetPlayerCraftingData(oPC);
            CraftBlueprint blueprint = _data.Single<CraftBlueprint>(x => x.ID == model.BlueprintID);
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

            _nwnxPlayer.StartGuiTimingBar(oPC, modifiedCraftDelay, "");

            oPC.DelayEvent<CraftCreateItem>(
                modifiedCraftDelay,
                oPC);
        }


        private float CalculateCraftingDelay(NWPlayer oPC, int skillID)
        {
            PerkType perkType;
            float adjustedSpeed = 1.0f;
            SkillType skillType = (SkillType)skillID;

            if (skillType == SkillType.Weaponsmith) perkType = PerkType.SpeedyWeaponsmith;
            else if (skillType == SkillType.Armorsmith) perkType = PerkType.SpeedyArmorsmith;
            else if (skillType == SkillType.Cooking) perkType = PerkType.SpeedyCooking;
            else if (skillType == SkillType.Engineering) perkType = PerkType.SpeedyEngineering;
            else if (skillType == SkillType.Fabrication) perkType = PerkType.SpeedyFabrication;
            else if (skillType == SkillType.Medicine) perkType = PerkType.SpeedyMedicine;
            else return BaseCraftDelay;

            int perkLevel = _perk.GetPCPerkLevel(oPC, perkType);

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

            return BaseCraftDelay * adjustedSpeed;
        }
        
        public string CalculateDifficultyDescription(int pcLevel, int blueprintLevel)
        {
            int delta = pcLevel - blueprintLevel;
            string difficulty = "";

            if (delta <= -5)
            {
                difficulty = _color.Custom("Impossible", 255, 62, 150);
            }
            else if (delta >= 4)
            {
                difficulty = _color.Custom("Trivial", 102, 255, 102);
            }
            else
            {
                switch (delta)
                {
                    case -4:
                        difficulty = _color.Custom("Extremely Difficult", 220, 20, 60);
                        break;
                    case -3:
                        difficulty = _color.Custom("Very Difficult", 255, 69, 0);
                        break;
                    case -2:
                        difficulty = _color.Custom("Difficult", 255, 165, 0);
                        break;
                    case -1:
                        difficulty = _color.Custom("Challenging", 238, 238, 0);
                        break;
                    case 0:
                        difficulty = _color.Custom("Moderate", 255, 255, 255);
                        break;
                    case 1:
                        difficulty = _color.Custom("Easy", 65, 105, 225);
                        break;
                    case 2:
                        difficulty = _color.Custom("Very Easy", 113, 113, 198);
                        break;
                    case 3:
                        difficulty = _color.Custom("Extremely Easy", 153, 255, 255);
                        break;
                }
            }


            return difficulty;
        }


        public int CalculatePCEffectiveLevel(NWPlayer player, int skillRank, SkillType skill)
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
                    if (background ==  BackgroundType.Fabricator)
                        effectiveLevel++;
                    break;
            }

            return effectiveLevel;
        }


        public string GetIngotResref(string oreResref)
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

        public int GetIngotLevel(string oreResref)
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

        public int GetIngotPerkLevel(string oreResref)
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


        public CraftingData GetPlayerCraftingData(NWPlayer player)
        {
            // Need to store the data outside of the conversation because of the constant
            // context switching between conversation and accessing placeable containers.
            // Conversation data is wiped when it closes.
            if (player.Data.ContainsKey("CRAFTING_MODEL"))
            {
                return player.Data["CRAFTING_MODEL"];
            }

            var model = new CraftingData();
            player.Data["CRAFTING_MODEL"] = model;
            return model;
        }

        public void ClearPlayerCraftingData(NWPlayer player, bool destroyComponents = false)
        {
            var model = GetPlayerCraftingData(player);

            foreach (var item in model.MainComponents)
            {
                if(!destroyComponents)
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

            player.Data.Remove("CRAFTING_MODEL");
            player.DeleteLocalInt("CRAFT_BLUEPRINT_ID");

        }

        public static bool CanHandleChat(NWObject sender, string message)
        {
            return sender.GetLocalInt("CRAFT_RENAMING_ITEM") == TRUE;
        }

        public void OnNWNXChat()
        {
            NWPlayer pc = _nwnxChat.GetSender().Object;
            string newName = _nwnxChat.GetMessage();

            if (!CanHandleChat(pc, newName))
            {
                return;
            }

            _nwnxChat.SkipMessage();
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

        public void OnModuleUseFeat()
        {
            NWPlayer pc = Object.OBJECT_SELF;
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();

            if (featID != (int)CustomFeatType.RenameCraftedItem) return;
            pc.ClearAllActions();

            bool isSetting = pc.GetLocalInt("CRAFT_RENAMING_ITEM") == TRUE;
            NWItem renameItem = _nwnxEvents.OnFeatUsed_GetTarget().Object;

            if (isSetting)
            {
                pc.SendMessage("You are no longer naming an item.");
                pc.DeleteLocalInt("CRAFT_RENAMING_ITEM");
                pc.DeleteLocalObject("CRAFT_RENAMING_ITEM_OBJECT");
                return;
            }

            string crafterPlayerID = renameItem.GetLocalString("CRAFTER_PLAYER_ID");
            if(string.IsNullOrWhiteSpace(crafterPlayerID) || new Guid(crafterPlayerID) != pc.GlobalID)
            {
                pc.SendMessage("You may only rename items which you have personally crafted.");
                return;
            }

            pc.SetLocalInt("CRAFT_RENAMING_ITEM", TRUE);
            pc.SetLocalObject("CRAFT_RENAMING_ITEM_OBJECT", renameItem);
            pc.SendMessage("Please enter in a name for this item. Length should be between 3 and 64 characters. Use this feat again to cancel this procedure.");
        }
    }
}
