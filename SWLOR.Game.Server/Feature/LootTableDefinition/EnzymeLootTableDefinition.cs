using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class EnzymeLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Hydrolase();
            Isomerase();
            Lyase();

            return _builder.Build();
        }

        /// <summary>
        /// Rolls the enzyme's stats from every bracket (<see cref="EnzymeRolls"/>) and puts them on the item. A vat's cultured
        /// enzymes roll the same way from the lowest brackets only.
        /// </summary>
        private void SpawnAction(uint item, EnzymeType type)
        {
            foreach (var roll in EnzymeRolls.Roll(type, EnzymeRolls.HuntedMaxPrimaryBracket, EnzymeRolls.HuntedMaxSecondaryBracket))
            {
                var ip = ItemPropertyCustom(ItemPropertyType.Incubation, (int)roll.Stat, roll.Amount);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }
        }

        private void Hydrolase()
        {
            void HydrolaseSpawn(uint item)
            {
                SpawnAction(item, EnzymeType.Hydrolase);
            }

            _builder.Create("HYDROLASE_TYPE_1")
                .AddItem("hydrolase_blue", 100)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_orange", 90)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_red", 70)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_purple", 20)
                .AddSpawnAction(HydrolaseSpawn)
                .AddItem("hydrolase_white", 5)
                .AddSpawnAction(HydrolaseSpawn);
        }

        private void Isomerase()
        {
            void IsomeraseSpawn(uint item)
            {
                SpawnAction(item, EnzymeType.Isomerase);
            }

            _builder.Create("ISOMERASE_TYPE_1")
                .AddItem("isomerase_orange", 100)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_blue", 90)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_green", 70)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_red", 20)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_yellow", 5)
                .AddSpawnAction(IsomeraseSpawn);

            _builder.Create("ISOMERASE_TYPE_2")
                .AddItem("isomerase_orange", 5)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_blue", 20)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_green", 70)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_red", 90)
                .AddSpawnAction(IsomeraseSpawn)
                .AddItem("isomerase_yellow", 100)
                .AddSpawnAction(IsomeraseSpawn);
        }

        private void Lyase()
        {
            void LyaseSpawn(uint item)
            {
                SpawnAction(item, EnzymeType.Lyase);
            }

            _builder.Create("LYASE_TYPE_1")
                .AddItem("lyase_green", 100)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_blue", 90)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_orange", 70)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_red", 20)
                .AddSpawnAction(LyaseSpawn)
                .AddItem("lyase_black", 5)
                .AddSpawnAction(LyaseSpawn);
        }
    }
}
