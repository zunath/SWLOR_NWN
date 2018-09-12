using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class CraftService : ICraftService
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly IColorTokenService _color;
        private readonly INWNXPlayer _nwnxPlayer;
        private readonly IRandomService _random;
        private readonly IErrorService _error;
        private readonly IComponentBonusService _componentBonus;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly IBaseService _base;

        public CraftService(
            INWScript script,
            IDataContext db,
            IPerkService perk,
            ISkillService skill,
            IColorTokenService color,
            INWNXPlayer nwnxPlayer,
            IRandomService random,
            IErrorService error,
            IComponentBonusService componentBonus,
            IBiowareXP2 biowareXP2,
            IBaseService @base)
        {
            _ = script;
            _db = db;
            _perk = perk;
            _skill = skill;
            _color = color;
            _nwnxPlayer = nwnxPlayer;
            _random = random;
            _error = error;
            _componentBonus = componentBonus;
            _biowareXP2 = biowareXP2;
            _base = @base;
        }

        private const float BaseCraftDelay = 18.0f;

        public List<CraftBlueprintCategory> GetCategoriesAvailableToPCByDeviceID(string playerID, int deviceID)
        {
            return _db.StoredProcedure<CraftBlueprintCategory>("GetCraftCategoriesAvailableToPCByDeviceID",
                new SqlParameter("DeviceID", deviceID),
                new SqlParameter("PlayerID", playerID));
        }

        public List<CraftBlueprint> GetPCBlueprintsByDeviceAndCategoryID(string playerID, int deviceID, int categoryID)
        {
            return _db.StoredProcedure<CraftBlueprint>("GetPCCraftBlueprintsByDeviceAndCategoryID",
                new SqlParameter("DeviceID", deviceID),
                new SqlParameter("CraftCategoryID", categoryID),
                new SqlParameter("PlayerID", playerID));
        }

        public string BuildBlueprintHeader(NWPlayer player, int blueprintID, bool showAddedComponentList)
        {
            var model = GetPlayerCraftingData(player);
            var bp = model.Blueprint;
            int deviceID = bp.CraftDeviceID;
            int playerEL = CalculatePCEffectiveLevel(player, model.PlayerSkillRank, (SkillType)bp.SkillID);

            string header = _color.Green("Blueprint: ") + bp.Quantity + "x " + bp.ItemName + "\n";
            header += _color.Green("Level: ") + (model.AdjustedLevel < 0 ? 0 : model.AdjustedLevel) + " (Base: " + bp.BaseLevel + ")\n";
            header += _color.Green("Difficulty: ") + CalculateDifficultyDescription(playerEL, model.AdjustedLevel) + "\n";
            header += _color.Green("Required Components (Required/Maximum): ") + "\n\n";

            string mainCounts = " (" + (bp.MainMinimum > 0 ? Convert.ToString(bp.MainMinimum) : "Optional") + "/" + bp.MainMaximum + ")";
            header += _color.Green("Main: ") + bp.MainComponentType.Name + mainCounts + "\n";

            if (bp.SecondaryMinimum > 0 && bp.SecondaryComponentTypeID > 0)
            {
                string secondaryCounts = " (" + (bp.SecondaryMinimum > 0 ? Convert.ToString(bp.SecondaryMinimum) : "Optional") + "/" + bp.SecondaryMaximum + ")";
                header += _color.Green("Secondary: ") + bp.SecondaryComponentType.Name + secondaryCounts + "\n";
            }
            if (bp.TertiaryMinimum > 0 && bp.TertiaryComponentTypeID > 0)
            {
                string tertiaryCounts = " (" + (bp.TertiaryMinimum > 0 ? Convert.ToString(bp.TertiaryMinimum) : "Optional") + "/" + bp.TertiaryMaximum + ")";
                header += _color.Green("Tertiary: ") + bp.TertiaryComponentType.Name + tertiaryCounts + "\n";
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
            return _db.CraftBlueprints.SingleOrDefault(x => x.CraftBlueprintID == craftBlueprintID);
        }

        public List<CraftBlueprintCategory> GetCategoriesAvailableToPC(string playerID)
        {
            return _db.StoredProcedure<CraftBlueprintCategory>("GetCategoriesAvailableToPC",
                new SqlParameter("PlayerID", playerID));
        }

        public List<CraftBlueprint> GetPCBlueprintsByCategoryID(string playerID, int categoryID)
        {
            return _db.StoredProcedure<CraftBlueprint>("GetPCBlueprintsByCategoryID",
                new SqlParameter("PlayerID", playerID),
                new SqlParameter("CraftCategoryID", categoryID));
        }


        public void CraftItem(NWPlayer oPC, NWPlaceable device)
        {
            var model = GetPlayerCraftingData(oPC);
            CraftBlueprint blueprint = _db.CraftBlueprints.Single(x => x.CraftBlueprintID == model.BlueprintID);
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

            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectCutsceneImmobilize(), oPC.Object, modifiedCraftDelay + 0.1f);
            oPC.AssignCommand(() =>
            {
                _.ClearAllActions();
                _.ActionPlayAnimation(ANIMATION_LOOPING_GET_MID, 1.0f, modifiedCraftDelay);
            });
            device.DelayCommand(() =>
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_COM_BLOOD_SPARK_MEDIUM), device.Object);
            }, 1.0f * (modifiedCraftDelay / 2.0f));

            _nwnxPlayer.StartGuiTimingBar(oPC, modifiedCraftDelay, "");

            oPC.DelayCommand(() =>
            {
                try
                {
                    RunCreateItem(oPC);
                    oPC.IsBusy = false;
                }
                catch (Exception ex)
                {
                    _error.LogError(ex);
                }
            }, modifiedCraftDelay);

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


        private void RunCreateItem(NWPlayer oPC)
        {
            var model = GetPlayerCraftingData(oPC);

            CraftBlueprint blueprint = _db.CraftBlueprints.Single(x => x.CraftBlueprintID == model.BlueprintID);
            PCSkill pcSkill = _db.PCSkills.Single(x => x.PlayerID == oPC.GlobalID && x.SkillID == blueprint.SkillID);

            int pcEffectiveLevel = CalculatePCEffectiveLevel(oPC, pcSkill.Rank, (SkillType)blueprint.SkillID);
            int itemLevel = model.AdjustedLevel;
            float chance = CalculateBaseChanceToAddProperty(pcEffectiveLevel, itemLevel);
            float equipmentBonus = CalculateEquipmentBonus(oPC, (SkillType)blueprint.SkillID);
            
            if (chance <= 1.0f)
            {
                oPC.FloatingText(_color.Red("Critical failure! You don't have enough skill to create that item. All components were lost."));
                ClearPlayerCraftingData(oPC, true);
                return;
            }

            var craftedItems = new List<NWItem>();
            NWItem craftedItem = NWItem.Wrap(_.CreateItemOnObject(blueprint.ItemResref, oPC.Object, blueprint.Quantity));
            craftedItem.IsIdentified = true;
            craftedItems.Add(craftedItem);

            // If item isn't stackable, loop through and create as many as necessary.
            if (craftedItem.StackSize < blueprint.Quantity)
            {
                for (int x = 2; x <= blueprint.Quantity; x++)
                {
                    craftedItem = NWItem.Wrap(_.CreateItemOnObject(blueprint.ItemResref, oPC.Object));
                    craftedItem.IsIdentified = true;
                    craftedItems.Add(craftedItem);
                }
            }
            
            // Recommended level gets set regardless if all item properties make it on the final product.
            // Also mark who crafted the item. This is later used for display on the item's examination event.
            foreach (var item in craftedItems)
            {
                item.RecommendedLevel = itemLevel;
                item.SetLocalString("CRAFTER_PLAYER_ID", oPC.GlobalID);

                _base.ApplyCraftedItemLocalVariables(item, blueprint.BaseStructure);
            }

            int successAmount = 0;
            foreach (var component in model.MainComponents)
            {
                var result = RunComponentBonusAttempt(oPC, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }
            foreach (var component in model.SecondaryComponents)
            {
                var result = RunComponentBonusAttempt(oPC, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }
            foreach (var component in model.TertiaryComponents)
            {
                var result = RunComponentBonusAttempt(oPC, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }
            foreach (var component in model.EnhancementComponents)
            {
                var result = RunComponentBonusAttempt(oPC, component, equipmentBonus, chance, craftedItems);
                successAmount += result.Item1;
                chance = result.Item2;
            }

            // Structures gain increased durability based on the blueprint
            if (blueprint.BaseStructure != null)
            {
                foreach (var item in craftedItems)
                {
                    item.MaxDurability += (float)blueprint.BaseStructure.Durability;
                    item.Durability = item.MaxDurability;
                }
            }


            oPC.SendMessage("You created " + blueprint.Quantity + "x " + blueprint.ItemName + "!");
            int baseXP = 250 + successAmount * _random.Random(1, 50);
            float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(baseXP, model.AdjustedLevel, pcSkill.Rank);
            _skill.GiveSkillXP(oPC, blueprint.SkillID, (int)xp);
            ClearPlayerCraftingData(oPC, true);
        }

        private Tuple<int, float> RunComponentBonusAttempt(NWPlayer player, NWItem component, float equipmentBonus, float chance, List<NWItem> itemSet)
        {
            int successAmount = 0;
            foreach (var ip in component.ItemProperties)
            {
                int ipType = _.GetItemPropertyType(ip);
                if (ipType != (int) CustomItemPropertyType.ComponentBonus) continue;

                int bonusTypeID = _.GetItemPropertySubType(ip);
                int tlkID = Convert.ToInt32(_.Get2DAString("iprp_compbon", "Name", bonusTypeID));
                int amount = _.GetItemPropertyCostTableValue(ip);
                string bonusName = _.GetStringByStrRef(tlkID) + " " + amount;
                float random = _random.RandomFloat() * 100.0f;
                
                if (random + equipmentBonus <= chance)
                {
                    foreach (var item in itemSet)
                    {
                        // If the target item is a component itself, we want to add the component bonuses instead of the
                        // actual item property bonuses.
                        // In other words, we want the custom item property "Component Bonus: AC UP" instead of the "AC Bonus" item property.
                        var componentIP = item.ItemProperties.FirstOrDefault(x => _.GetItemPropertyType(x) == (int) CustomItemPropertyType.ComponentType);
                        if(componentIP == null)
                            _componentBonus.ApplyComponentBonus(item, ip);
                        else 
                            _biowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);

                    }
                    player.SendMessage(_color.Green("Successfully applied component property: " + bonusName));

                    chance -= _random.Random(1, 5);
                    if (chance < 1) chance = 1;

                    successAmount++;
                }
                else
                {
                    player.SendMessage(_color.Red("Failed to apply component property: " + bonusName));
                }
            }

            return new Tuple<int, float>(successAmount, chance);
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

        private float CalculateEquipmentBonus(NWPlayer player, SkillType skillType)
        {
            int equipmentBonus = 0;

            switch (skillType)
            {
                case SkillType.Armorsmith: equipmentBonus = player.EffectiveArmorsmithBonus; break;
                case SkillType.Weaponsmith: equipmentBonus = player.EffectiveWeaponsmithBonus; break;
                case SkillType.Cooking: equipmentBonus = player.EffectiveCookingBonus; break;
                case SkillType.Engineering: equipmentBonus = player.EffectiveEngineeringBonus; break;
                case SkillType.Fabrication: equipmentBonus = player.EffectiveFabricationBonus; break;
            }

            return equipmentBonus * 0.5f; // +0.5% per equipment bonus
        }

        private float CalculateBaseChanceToAddProperty(int pcLevel, int blueprintLevel)
        {
            int delta = pcLevel - blueprintLevel;
            float percentage = 0.0f;

            if (delta <= -5)
            {
                percentage = 0.0f;
            }
            else if (delta >= 4)
            {
                percentage = 90.0f;
            }
            else
            {
                switch (delta)
                {
                    case -4:
                        percentage = 10.0f;
                        break;
                    case -3:
                        percentage = 15.0f;
                        break;
                    case -2:
                        percentage = 25.0f;
                        break;
                    case -1:
                        percentage = 35.0f;
                        break;
                    case 0:
                        percentage = 50.0f;
                        break;
                    case 1:
                        percentage = 65.0f;
                        break;
                    case 2:
                        percentage = 75.0f;
                        break;
                    case 3:
                        percentage = 85.0f;
                        break;
                }
            }


            return percentage;
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
    }
}
