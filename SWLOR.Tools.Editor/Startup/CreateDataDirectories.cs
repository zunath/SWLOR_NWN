using System.IO;
using Autofac;
using SWLOR.Game.Server.Data;

namespace SWLOR.Tools.Editor.Startup
{
    public class CreateDataDirectories: IStartable
    {
        public void Start()
        {
            string[] folderNames =
            {
                nameof(ApartmentBuilding),
                nameof(BaseStructure),
                nameof(BaseStructureType),
                nameof(BuildingStyle),
                nameof(ComponentType),
                nameof(CooldownCategory),
                nameof(CraftBlueprint),
                nameof(CraftBlueprintCategory),
                nameof(CraftDevice),
                nameof(CustomEffect),
                nameof(Download),
                nameof(FameRegion),
                nameof(GameTopic),
                nameof(GameTopicCategory),
                nameof(GrowingPlant),
                nameof(ItemType),
                nameof(KeyItem),
                nameof(KeyItemCategory),
                nameof(LootTable),
                nameof(Mod),
                nameof(NPCGroup),
                nameof(Perk),
                nameof(PerkCategory),
                nameof(Plant),
                nameof(Quest),
                nameof(Skill),
                nameof(SkillCategory),
                nameof(Spawn),
            };

            foreach (var folder in folderNames)
            {
                CreateDirectory(folder);
            }
        }

        private void CreateDirectory(string folder)
        {
            if (!Directory.Exists("./Data/" + folder))
            {
                Directory.CreateDirectory("./Data/" + folder);
            }
        }

    }
}
