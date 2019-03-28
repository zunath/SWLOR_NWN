using Autofac;
using AutoMapper;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.Startup
{
    public class InitializeAutomapper: IStartable
    {
        public void Start()
        {
            Mapper.Initialize(cfg =>
            {
                // View Model -> DB Object
                cfg.CreateMap<ApartmentBuildingViewModel, ApartmentBuilding>();
                cfg.CreateMap<BaseStructureViewModel, BaseStructure>();
                cfg.CreateMap<BuildingStyleViewModel, BuildingStyle>();
                cfg.CreateMap<CooldownCategoryViewModel, CooldownCategory>();
                cfg.CreateMap<CraftBlueprintCategoryViewModel, CraftBlueprintCategory>();
                cfg.CreateMap<CraftBlueprintViewModel, CraftBlueprint>();
                cfg.CreateMap<CraftDeviceViewModel, CraftDevice>();
                cfg.CreateMap<CustomEffectViewModel, CustomEffect>();
                cfg.CreateMap<DownloadViewModel, Download>();
                cfg.CreateMap<FameRegionViewModel, FameRegion>();
                cfg.CreateMap<GameTopicCategoryViewModel, GameTopicCategory>();
                cfg.CreateMap<GameTopicViewModel, GameTopic>();
                cfg.CreateMap<KeyItemCategoryViewModel, KeyItemCategory>();
                cfg.CreateMap<KeyItemViewModel, KeyItem>();
                cfg.CreateMap<LootTableItemViewModel, LootTableItem>();
                cfg.CreateMap<LootTableViewModel, LootTable>();
                cfg.CreateMap<NPCGroupViewModel, NPCGroup>();
                cfg.CreateMap<PerkCategoryViewModel, PerkCategory>();
                cfg.CreateMap<PerkViewModel, Perk>();
                cfg.CreateMap<PlantViewModel, Plant>();
                cfg.CreateMap<QuestViewModel, Quest>();
                cfg.CreateMap<SkillCategoryViewModel, SkillCategory>();
                cfg.CreateMap<SkillViewModel, Skill>();
                cfg.CreateMap<SpawnViewModel, Spawn>();

                // DB Object -> View Model
                cfg.CreateMap<ApartmentBuilding, ApartmentBuildingViewModel>();
                cfg.CreateMap<BaseStructure, BaseStructureViewModel>();
                cfg.CreateMap<BuildingStyle, BuildingStyleViewModel>();
                cfg.CreateMap<CooldownCategory, CooldownCategoryViewModel>();
                cfg.CreateMap<CraftBlueprintCategory, CraftBlueprintCategoryViewModel>();
                cfg.CreateMap<CraftBlueprint, CraftBlueprintViewModel>();
                cfg.CreateMap<CraftDevice, CraftDeviceViewModel>();
                cfg.CreateMap<CustomEffect, CustomEffectViewModel>();
                cfg.CreateMap<Download, DownloadViewModel>();
                cfg.CreateMap<FameRegion, FameRegionViewModel>();
                cfg.CreateMap<GameTopicCategory, GameTopicCategoryViewModel>();
                cfg.CreateMap<GameTopic, GameTopicViewModel>();
                cfg.CreateMap<KeyItemCategory, KeyItemCategoryViewModel>();
                cfg.CreateMap<KeyItem, KeyItemViewModel>();
                cfg.CreateMap<LootTableItem, LootTableItemViewModel>();
                cfg.CreateMap<LootTable, LootTableViewModel>();
                cfg.CreateMap<NPCGroup, NPCGroupViewModel>();
                cfg.CreateMap<PerkCategory, PerkCategoryViewModel>();
                cfg.CreateMap<Perk, PerkViewModel>();
                cfg.CreateMap<Plant, PlantViewModel>();
                cfg.CreateMap<Quest, QuestViewModel>();
                cfg.CreateMap<SkillCategory, SkillCategoryViewModel>();
                cfg.CreateMap<Skill, SkillViewModel>();
                cfg.CreateMap<Spawn, SpawnViewModel>();

            });
        }
    }
}
