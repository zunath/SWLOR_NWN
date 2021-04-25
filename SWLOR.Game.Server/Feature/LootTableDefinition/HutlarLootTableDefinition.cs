using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class HutlarLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Byysk();
            QionSlugs();
            QionTigers();

            return _builder.Build();
        }

        private void Byysk()
        {
            _builder.Create("HUTLAR_BYYSK");
        }

        private void QionSlugs()
        {
            _builder.Create("HUTLAR_QION_SLUGS");
        }

        private void QionTigers()
        {
            _builder.Create("HUTLAR_QION_TIGERS");
        }
    }
}
