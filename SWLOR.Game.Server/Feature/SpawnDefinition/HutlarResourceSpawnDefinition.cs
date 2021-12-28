using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class HutlarResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new SpawnTableBuilder();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            QionTundra();
            Valley();

            return _builder.Build();
        }

        private void QionTundra()
        {
            _builder.Create("RESOURCES_HUTLAR_QIONTUNDRA")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }
        private void Valley()
        {
            _builder.Create("RESOURCES_HUTLAR_VALLEY")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }
    }
}
