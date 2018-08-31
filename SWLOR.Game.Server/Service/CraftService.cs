using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

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
        private readonly IItemService _item;
        private readonly IBiowareXP2 _biowareXP2;

        public CraftService(
            INWScript script,
            IDataContext db,
            IPerkService perk,
            ISkillService skill,
            IColorTokenService color,
            INWNXPlayer nwnxPlayer,
            IRandomService random,
            IErrorService error,
            IItemService item,
            IBiowareXP2 biowareXP2)
        {
            _ = script;
            _db = db;
            _perk = perk;
            _skill = skill;
            _color = color;
            _nwnxPlayer = nwnxPlayer;
            _random = random;
            _error = error;
            _item = item;
            _biowareXP2 = biowareXP2;
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

        public string BuildBlueprintHeader(NWPlayer player, int blueprintID)
        {
            CraftBlueprint blueprint = _db.CraftBlueprints.Single(x => x.CraftBlueprintID == blueprintID);
            PCSkill pcSkill = _db.PCSkills.Single(x => x.PlayerID == player.GlobalID && x.SkillID == blueprint.SkillID);

            string header = _color.Green("Blueprint: ") + _color.White(blueprint.ItemName) + "\n\n";
            header += _color.Green("Skill: ") + _color.White(pcSkill.Skill.Name) + "\n";

            header += _color.Green("Base Difficulty: ") + CalculateDifficulty(pcSkill.Rank, blueprint.BaseLevel) + "\n";
            header += _color.Green("Max Enhancement Slots: ") + blueprint.EnhancementSlots + "\n\n";

            header += _color.Green("Components: ") + "\n\n";

            header += _color.White(blueprint.MainMinimum + "x " + blueprint.MainComponentType.Name) + "\n";

            if (blueprint.SecondaryComponentTypeID > 0)
                header += _color.White(blueprint.SecondaryMinimum + "x " + blueprint.SecondaryComponentType.Name) + "\n";

            if (blueprint.TertiaryComponentTypeID > 0)
                header += _color.White(blueprint.TertiaryMinimum + "x " + blueprint.TertiaryComponentType.Name) + "\n";

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

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectCutsceneImmobilize(), oPC.Object, modifiedCraftDelay + 0.1f);
            oPC.AssignCommand(() =>
            {
                _.ClearAllActions();
                _.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_GET_MID, 1.0f, modifiedCraftDelay);
            });
            device.DelayCommand(() =>
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectVisualEffect(NWScript.VFX_COM_BLOOD_SPARK_MEDIUM), device.Object);
            }, 1.0f * (modifiedCraftDelay / 2.0f));

            _nwnxPlayer.StartGuiTimingBar(oPC, modifiedCraftDelay, "");

            oPC.DelayCommand(() =>
            {
                try
                {
                    RunCreateItem(oPC, device);
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


        private void RunCreateItem(NWPlayer oPC, NWPlaceable device)
        {
            var model = GetPlayerCraftingData(oPC);

            CraftBlueprint blueprint = _db.CraftBlueprints.Single(x => x.CraftBlueprintID == model.BlueprintID);
            PCSkill pcSkill = _db.PCSkills.Single(x => x.PlayerID == oPC.GlobalID && x.SkillID == blueprint.SkillID);

            int pcEffectiveLevel = CalculatePCEffectiveLevel(oPC, device, pcSkill.Rank);
            float chance = CalculateBaseChanceToAddProperty(pcEffectiveLevel, model.AdjustedLevel);
            float equipmentBonus = CalculateEquipmentBonus(oPC, (SkillType)blueprint.SkillID);
            
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

            foreach (var component in model.MainComponents)
            {
                ComponentBonusType bonus = ComponentBonusType.Unknown;
                foreach (var ip in component.ItemProperties)
                {
                    if (_.GetItemPropertyType(ip) == (int)CustomItemPropertyType.ComponentBonus)
                    {
                        bonus = (ComponentBonusType) _.GetItemPropertyCostTableValue(ip);
                    }
                }

                if (bonus == ComponentBonusType.Unknown) continue;

                if (_random.RandomFloat() * 100.0f + equipmentBonus <= chance)
                {
                    foreach (var item in craftedItems)
                    {
                        ApplyComponentBonus(item, bonus);
                    }
                }
            }
            
            oPC.SendMessage("You created " + blueprint.Quantity + "x " + blueprint.ItemName + "!");
            float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(250, model.AdjustedLevel, pcSkill.Rank);
            _skill.GiveSkillXP(oPC, blueprint.SkillID, (int)xp);
            ClearPlayerCraftingData(oPC);
        }

        private void ApplyComponentBonus(NWItem product, ComponentBonusType bonus)
        {
            ItemProperty prop = null;
            string sourceTag = string.Empty;

            switch (bonus)
            {
                case ComponentBonusType.RunicSocketRed: sourceTag = "rslot_red"; break;
                case ComponentBonusType.RunicSocketBlue: sourceTag = "rslot_blue"; break;
                case ComponentBonusType.RunicSocketGreen: sourceTag = "rslot_green"; break;
                case ComponentBonusType.RunicSocketYellow: sourceTag = "rslot_yellow"; break;
                case ComponentBonusType.RunicSocketPrismatic: sourceTag = "rslot_prismatic"; break;
            }

            if (!string.IsNullOrWhiteSpace(sourceTag))
            {
                prop = _item.GetCustomItemPropertyByItemTag(sourceTag);
            }

            if (prop == null) return;

            _biowareXP2.IPSafeAddItemProperty(product, prop, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
        }

        private string CalculateDifficulty(int pcLevel, int blueprintLevel)
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
                percentage = 95.0f;
            }
            else
            {
                switch (delta)
                {
                    case -4:
                        percentage = 15.0f;
                        break;
                    case -3:
                        percentage = 25.0f;
                        break;
                    case -2:
                        percentage = 40.0f;
                        break;
                    case -1:
                        percentage = 60.0f;
                        break;
                    case 0:
                        percentage = 75.0f;
                        break;
                    case 1:
                        percentage = 82.5f;
                        break;
                    case 2:
                        percentage = 87.0f;
                        break;
                    case 3:
                        percentage = 90.0f;
                        break;
                }
            }


            return percentage;
        }

        private int CalculatePCEffectiveLevel(NWPlayer pcGO, NWPlaceable device, int skillRank)
        {
            int deviceID = device.GetLocalInt("CRAFT_DEVICE_ID");
            int effectiveLevel = skillRank;
            PlayerCharacter player = _db.PlayerCharacters.Single(x => x.PlayerID == pcGO.GlobalID);

            switch ((CraftDeviceType)deviceID)
            {
                case CraftDeviceType.ArmorsmithBench:
                    if (player.BackgroundID == (int)BackgroundType.Armorsmith)
                        effectiveLevel++;
                    break;
                case CraftDeviceType.Cookpot:
                    if (player.BackgroundID == (int)BackgroundType.Chef)
                        effectiveLevel++;
                    break;
                case CraftDeviceType.WeaponsmithBench:
                    if (player.BackgroundID == (int)BackgroundType.Weaponsmith)
                        effectiveLevel++;
                    break;
                case CraftDeviceType.EngineeringBench:
                    if (player.BackgroundID == (int)BackgroundType.Engineer)
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
                case "power_unit":
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

        public void ClearPlayerCraftingData(NWPlayer player)
        {
            var model = GetPlayerCraftingData(player);

            foreach (var item in model.MainComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }
            foreach (var item in model.SecondaryComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }
            foreach (var item in model.TertiaryComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }
            foreach (var item in model.EnhancementComponents)
            {
                _.CopyItem(item.Object, player.Object, NWScript.TRUE);
                item.Destroy();
            }

            player.Data.Remove("CRAFTING_MODEL");
            player.DeleteLocalInt("CRAFT_BLUEPRINT_ID");

        }
    }
}
