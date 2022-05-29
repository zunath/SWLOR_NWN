using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class KorribanResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            WastelandsResources();
            ValleyResources();
            RavineResources();
            CavernsResources();
            DunesResources();
            SithCryptResources();

            return _builder.Build();
        }

        private void WastelandsResources()
        {
            _builder.Create("KORRIBAN_WASTELANDS_RESOURCES", "Wastelands")
                .AddSpawn(ObjectType.Placeable, "")
                .WithFrequency(50);

        }

        private void ValleyResources()
        {
            _builder.Create("KORRIBAN_VALLEY_RESOURCES", "Valley")
                .AddSpawn(ObjectType.Placeable, "")
                .WithFrequency(50);
        }

        private void RavineResources()
        {
            _builder.Create("KORRIBAN_RAVINE_RESOURCES", "Ravine")
                .AddSpawn(ObjectType.Placeable, "")
                .WithFrequency(50);
        }

        private void CavernsResources()
        {
            _builder.Create("KORRIBAN_CAVERNS_RESOURCES", "Caverns")
                .AddSpawn(ObjectType.Placeable, "")
                .WithFrequency(50);
        }

        private void DunesResources()
        {
            _builder.Create("KORRIBAN_DUNES_RESOURCES", "Dunes")
                .AddSpawn(ObjectType.Placeable, "")
                .WithFrequency(50);
        }

        private void SithCryptResources()
        {
            _builder.Create("KORRIBAN_SITH_CRYPT_RESOURCES", "Sith Crypt")
                .AddSpawn(ObjectType.Placeable, "")
                .WithFrequency(50);
        }
    }
}
