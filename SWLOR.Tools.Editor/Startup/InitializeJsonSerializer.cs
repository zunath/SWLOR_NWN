using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SWLOR.Game.Server.Data;

namespace SWLOR.Tools.Editor.Startup
{
    public class InitializeJsonSerializer: IStartable
    {
        public void Start()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new EntityContractResolver()
            };
        }
        
        private class EntityContractResolver : DefaultContractResolver
        {
            private Type _type;
            private MemberSerialization _memberSerialization;

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                _type = type;
                _memberSerialization = memberSerialization;

                switch (type.BaseType == null ? type.Name : type.BaseType.Name)
                {
                    case nameof(ApartmentBuilding):
                        return Create("ApartmentBuildingID", "Name");
                    case nameof(BaseStructure):
                        return Create("BaseStructureID", "BaseStructureTypeID", "Name", "PlaceableResref", "ItemResref", "IsActive", "Power", "CPU", "Durability", "Storage", "HasAtmosphere", "ReinforcedStorage", "RequiresBasePower", "ResourceStorage", "RetrievalRating");
                    case nameof(BaseStructureType):
                        return Create("BaseStructureTypeID", "Name", "IsActive", "CanPlaceInside", "CanPlaceOutside");
                    case nameof(BuildingStyle):
                        return Create("BuildingStyleID", "Name", "Resref", "BaseStructureID", "IsDefault", "DoorRule", "IsActive", "BuildingTypeID", "PurchasePrice", "DailyUpkeep", "FurnitureLimit");
                    case nameof(ComponentType):
                        return Create("ComponentTypeID", "Name");
                    case nameof(CooldownCategory):
                        return Create("CooldownCategoryID", "Name", "BaseCooldownTime");
                    case nameof(CraftBlueprint):
                        return Create("CraftBlueprintID", "CraftCategoryID", "BaseLevel", "ItemName", "ItemResref", "Quantity", "SkillID", "CraftDeviceID", "PerkID", "RequiredPerkLevel", "IsActive", "MainComponentTypeID", "MainMinimum", "SecondaryComponentTypeID", "SecondaryMinimum", "TertiaryComponentTypeID", "TertiaryMinimum", "EnhancementSlots", "MainMaximum", "SecondaryMaximum", "TertiaryMaximum", "BaseStructureID");
                    case nameof(CraftBlueprintCategory):
                        return Create("CraftBlueprintCategoryID", "Name", "IsActive");
                    case nameof(CraftDevice):
                        return Create("CraftDeviceID", "Name");
                    case nameof(CustomEffect):
                        return Create("CustomEffectID", "Name", "IconID", "ScriptHandler", "StartMessage", "ContinueMessage", "WornOffMessage", "IsStance");
                    case nameof(Download):
                        return Create("DownloadID", "Name", "Description", "Url", "IsActive");
                    case nameof(FameRegion):
                        return Create("FameRegionID", "Name");
                    case nameof(GameTopic):
                        return Create("GameTopicID", "Name", "Text", "GameTopicCategoryID", "IsActive", "Sequence", "Icon");
                    case nameof(GameTopicCategory):
                        return Create("GameTopicCategoryID", "Name");
                    case nameof(GrowingPlant):
                        return Create("GrowingPlantID", "PlantID", "RemainingTicks", "LocationAreaTag", "LocationX", "LocationY", "LocationZ", "LocationOrientation", "DateCreated", "IsActive", "TotalTicks", "WaterStatus", "LongevityBonus");
                    case nameof(ItemType):
                        return Create("ItemTypeID", "Name");
                    case nameof(KeyItem):
                        return Create("KeyItemID", "KeyItemCategoryID", "Name", "Description");
                    case nameof(KeyItemCategory):
                        return Create("KeyItemCategoryID", "Name", "IsActive");
                    case nameof(LootTable):
                        return Create("LootTableID", "Name", "LootTableItems");
                    case nameof(LootTableItem):
                        return Create("LootTableItemID", "LootTableID", "Resref", "MaxQuantity", "Weight", "IsActive", "SpawnRule");
                    case nameof(Mod):
                        return Create("ModID", "Name", "Script", "IsActive");
                    case nameof(NPCGroup):
                        return Create("NPCGroupID", "Name");
                    case nameof(Perk):
                        return Create("PerkID", "Name", "FeatID", "IsActive", "ScriptName", "BaseFPCost", "BaseCastingTime", "Description", "PerkCategoryID", "CooldownCategoryID", "ExecutionTypeID", "ItemResref", "IsTargetSelfOnly", "Enmity", "EnmityAdjustmentRuleID", "CastAnimationID");
                    case nameof(PerkCategory):
                        return Create("PerkCategoryID", "Name", "IsActive", "Sequence");
                    case nameof(PerkLevel):
                        return Create("PerkLevelID", "PerkID", "Level", "Price", "Description");
                    case nameof(PerkLevelQuestRequirement):
                        return Create("PerkLevelQuestRequirementID", "PerkLevelID", "RequiredQuestID");
                    case nameof(PerkLevelSkillRequirement):
                        return Create("PerkLevelSkillRequirementID", "PerkLevelID", "SkillID", "RequiredRank");
                    case nameof(Plant):
                        return Create("PlantID", "Name", "BaseTicks", "Resref", "WaterTicks", "Level", "SeedResref");
                    case nameof(Quest):
                        return Create("QuestID", "Name", "JournalTag", "FameRegionID", "RequiredFameAmount", "AllowRewardSelection", "RewardGold", "RewardKeyItemID", "RewardFame", "IsRepeatable", "MapNoteTag", "StartKeyItemID", "RemoveStartKeyItemAfterCompletion", "OnAcceptRule", "OnAdvanceRule", "OnCompleteRule", "OnKillTargetRule", "OnAcceptArgs", "OnAdvanceArgs", "OnCompleteArgs", "OnKillTargetArgs");
                    case nameof(QuestKillTargetList):
                        return Create("QuestKillTargetListID", "QuestID", "NPCGroupID", "Quantity", "QuestStateID");
                    case nameof(QuestPrerequisite):
                        return Create("QuestPrerequisiteID", "QuestID", "RequiredQuestID");
                    case nameof(QuestRequiredItemList):
                        return Create("QuestRequiredItemListID", "QuestID", "Resref", "Quantity", "QuestStateID", "MustBeCraftedByPlayer");
                    case nameof(QuestRequiredKeyItemList):
                        return Create("QuestRequiredKeyItemID", "QuestID", "KeyItemID", "QuestStateID");
                    case nameof(QuestRewardItem):
                        return Create("QuestRewardItemID", "QuestID", "Resref", "Quantity");
                    case nameof(QuestState):
                        return Create("QuestStateID", "QuestID", "Sequence", "QuestTypeID", "JournalStateID");
                    case nameof(Skill):
                        return Create("SkillID", "SkillCategoryID", "Name", "MaxRank", "IsActive", "Description", "Primary", "Secondary", "Tertiary", "ContributesToSkillCap");
                    case nameof(SkillCategory):
                        return Create("SkillCategoryID", "Name", "IsActive", "Sequence");
                    case nameof(SkillXPRequirement):
                        return Create("SkillXPRequirementID", "SkillID", "Rank", "XP");
                    case nameof(Spawn):
                        return Create("SpawnID", "Name", "SpawnObjectTypeID");
                    case nameof(SpawnObject):
                        return Create("SpawnObjectID", "SpawnID", "Resref", "Weight", "SpawnRule", "NPCGroupID", "BehaviourScript", "DeathVFXID");
                    default:
                        return base.CreateProperties(_type, _memberSerialization);
                }
            }

            private IList<JsonProperty> Create(params string[] props)
            {
                IList<JsonProperty> retval = base.CreateProperties(_type, _memberSerialization);
                retval = retval.Where(p => props.Contains(p.PropertyName)).ToList();
                return retval;
            }

        }

    }
}
