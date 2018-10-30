using System;
using Autofac;
using AutoMapper;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.Startup
{
    public class InitializeAutomapper: IStartable
    {
        public void Start()
        {
            Mapper.Initialize(cfg =>
            {
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
                cfg.CreateMap<ModViewModel, Mod>();
                cfg.CreateMap<NPCGroupViewModel, NPCGroup>();
                cfg.CreateMap<PerkCategoryViewModel, PerkCategory>();
                cfg.CreateMap<PerkViewModel, Perk>();
                cfg.CreateMap<PlantViewModel, Plant>();
                cfg.CreateMap<QuestViewModel, Quest>();
                cfg.CreateMap<SkillCategoryViewModel, SkillCategory>();
                cfg.CreateMap<SkillViewModel, Skill>();
                cfg.CreateMap<SpawnViewModel, Spawn>();
            });
        }
    }
}
