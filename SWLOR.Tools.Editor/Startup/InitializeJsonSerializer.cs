using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SWLOR.Game.Server.Data.Entity;

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
                        return Create("ID", "Name");
                    case nameof(BaseStructure):
                        return Create("ID", "BaseStructureTypeID", "Name", "PlaceableResref", "ItemResref", "IsActive", "Power", "CPU", "Durability", "Storage", "HasAtmosphere", "ReinforcedStorage", "RequiresBasePower", "ResourceStorage", "RetrievalRating");
                    case nameof(BaseStructureType):
                        return Create("ID", "Name", "IsActive", "CanPlaceInside", "CanPlaceOutside");
                    case nameof(BuildingStyle):
                        return Create("ID", "Name", "Resref", "BaseStructureID", "IsDefault", "DoorRule", "IsActive", "BuildingTypeID", "PurchasePrice", "DailyUpkeep", "FurnitureLimit");
                    case nameof(ComponentType):
                        return Create("ID", "Name");
                    case nameof(CooldownCategory):
                        return Create("ID", "Name", "BaseCooldownTime");
                    case nameof(CraftBlueprint):
                        return Create("ID", "CraftCategoryID", "BaseLevel", "ItemName", "ItemResref", "Quantity", "SkillID", "CraftDeviceID", "PerkID", "RequiredPerkLevel", "IsActive", "MainComponentTypeID", "MainMinimum", "SecondaryComponentTypeID", "SecondaryMinimum", "TertiaryComponentTypeID", "TertiaryMinimum", "EnhancementSlots", "MainMaximum", "SecondaryMaximum", "TertiaryMaximum", "BaseStructureID");
                    case nameof(CraftBlueprintCategory):
                        return Create("ID", "Name", "IsActive");
                    case nameof(CraftDevice):
                        return Create("ID", "Name");
                    case nameof(CustomEffect):
                        return Create("ID", "Name", "IconID", "ScriptHandler", "StartMessage", "ContinueMessage", "WornOffMessage", "CustomEffectCategoryID");
                    case nameof(Download):
                        return Create("ID", "Name", "Description", "Url", "IsActive");
                    case nameof(FameRegion):
                        return Create("ID", "Name");
                    case nameof(GameTopic):
                        return Create("ID", "Name", "Text", "GameTopicCategoryID", "IsActive", "Sequence", "Icon");
                    case nameof(GameTopicCategory):
                        return Create("ID", "Name");
                    case nameof(GrowingPlant):
                        return Create("ID", "PlantID", "RemainingTicks", "LocationAreaTag", "LocationX", "LocationY", "LocationZ", "LocationOrientation", "DateCreated", "IsActive", "TotalTicks", "WaterStatus", "LongevityBonus");
                    case nameof(ItemType):
                        return Create("ID", "Name");
                    case nameof(KeyItem):
                        return Create("ID", "KeyItemCategoryID", "Name", "Description");
                    case nameof(KeyItemCategory):
                        return Create("ID", "Name", "IsActive");
                    case nameof(LootTable):
                        return Create("ID", "Name", "LootTableItems");
                    case nameof(LootTableItem):
                        return Create("ID", "LootTableID", "Resref", "MaxQuantity", "Weight", "IsActive", "SpawnRule");
                    case nameof(NPCGroup):
                        return Create("ID", "Name");
                    case nameof(Perk):
                        return Create("ID", "Name", "FeatID", "IsActive", "ScriptName", "BaseFPCost", "BaseCastingTime", "Description", "PerkCategoryID", "CooldownCategoryID", "ExecutionTypeID", "ItemResref", "IsTargetSelfOnly", "Enmity", "EnmityAdjustmentRuleID", "CastAnimationID", "PerkLevels");
                    case nameof(PerkCategory):
                        return Create("ID", "Name", "IsActive", "Sequence");
                    case nameof(PerkLevel):
                        return Create("ID", "PerkID", "Level", "Price", "Description", "PerkLevelQuestRequirements", "PerkLevelSkillRequirements");
                    case nameof(PerkLevelQuestRequirement):
                        return Create("ID", "PerkLevelID", "RequiredQuestID");
                    case nameof(PerkLevelSkillRequirement):
                        return Create("ID", "PerkLevelID", "SkillID", "RequiredRank");
                    case nameof(Plant):
                        return Create("ID", "Name", "BaseTicks", "Resref", "WaterTicks", "Level", "SeedResref");
                    case nameof(Quest):
                        return Create("ID", "Name", "JournalTag", "FameRegionID", "RequiredFameAmount", "AllowRewardSelection", "RewardGold", "RewardKeyItemID", "RewardFame", "IsRepeatable", "MapNoteTag", "StartKeyItemID", "RemoveStartKeyItemAfterCompletion", "OnAcceptRule", "OnAdvanceRule", "OnCompleteRule", "OnKillTargetRule", "OnAcceptArgs", "OnAdvanceArgs", "OnCompleteArgs", "OnKillTargetArgs", "QuestKillTargetLists", "QuestPrerequisites", "QuestRequiredItemLists", "QuestRequiredKeyItemLists", "QuestRewardItems", "QuestStates");
                    case nameof(QuestKillTarget):
                        return Create("ID", "QuestID", "NPCGroupID", "Quantity", "QuestStateID");
                    case nameof(QuestPrerequisite):
                        return Create("ID", "QuestID", "RequiredQuestID");
                    case nameof(QuestRequiredItem):
                        return Create("ID", "QuestID", "Resref", "Quantity", "QuestStateID", "MustBeCraftedByPlayer");
                    case nameof(QuestRequiredKeyItem):
                        return Create("ID", "QuestID", "KeyItemID", "QuestStateID");
                    case nameof(QuestRewardItem):
                        return Create("ID", "QuestID", "Resref", "Quantity");
                    case nameof(QuestState):
                        return Create("ID", "QuestID", "Sequence", "QuestTypeID", "JournalStateID");
                    case nameof(Skill):
                        return Create("ID", "SkillCategoryID", "Name", "MaxRank", "IsActive", "Description", "Primary", "Secondary", "Tertiary", "ContributesToSkillCap", "SkillXPRequirements");
                    case nameof(SkillCategory):
                        return Create("ID", "Name", "IsActive", "Sequence");
                    case nameof(SkillXPRequirement):
                        return Create("ID", "SkillID", "Rank", "XP");
                    case nameof(Spawn):
                        return Create("ID", "Name", "SpawnObjectTypeID");
                    case nameof(SpawnObject):
                        return Create("ID", "SpawnID", "Resref", "Weight", "SpawnRule", "NPCGroupID", "BehaviourScript", "DeathVFXID");
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
