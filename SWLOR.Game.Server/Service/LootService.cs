using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class LootService: ILootService
    {
        private readonly IDataContext _db;
        private readonly IRandomService _random;
        private readonly INWScript _;

        public LootService(IDataContext db,
            IRandomService random,
            INWScript script)
        {
            _db = db;
            _random = random;
            _ = script;
        }

        public ItemVO PickRandomItemFromLootTable(int lootTableID)
        {
            if (lootTableID <= 0) return null;

            LootTable entity = _db.LootTables.Single(x => x.LootTableID == lootTableID);
            if (entity.LootTableItems.Count <= 0) return null;

            int[] weights = new int[entity.LootTableItems.Count];

            for (int x = 0; x < entity.LootTableItems.Count; x++)
            {
                weights[x] = entity.LootTableItems.ElementAt(x).Weight;
            }
            int randomIndex = _random.GetRandomWeightedIndex(weights);

            LootTableItem itemEntity = entity.LootTableItems.ElementAt(randomIndex);
            int quantity = _random.Random(itemEntity.MaxQuantity) + 1;

            ItemVO result = new ItemVO
            {
                Quantity = quantity,
                Resref = itemEntity.Resref
            };


            return result;
        }

        public void OnCreatureDeath(NWCreature creature)
        {
            int lootTableNumber = 1;
            int lootTableID = creature.GetLocalInt("LOOT_TABLE_ID_" + lootTableNumber);
            while (lootTableID > 0)
            {
                int chance = creature.GetLocalInt("LOOT_TABLE_CHANCE_" + lootTableNumber);
                if (chance <= 0 || chance > 100) chance = 100;

                int attempts = creature.GetLocalInt("LOOT_TABLE_ATTEMPTS_" + lootTableNumber);
                if (attempts <= 0) attempts = 1;

                for (int a = 1; a <= attempts; a++)
                {
                    if (_random.Random(100) + 1 <= chance)
                    {
                        ItemVO model = PickRandomItemFromLootTable(lootTableID);
                        if (model == null) continue;

                        int spawnQuantity = model.Quantity > 1 ? _random.Random(1, model.Quantity) : 1;

                        for (int x = 1; x <= spawnQuantity; x++)
                        {
                            _.CreateItemOnObject(model.Resref, creature.Object);
                        }
                    }
                }

                lootTableNumber++;
                lootTableID = creature.GetLocalInt("LOOT_TABLE_ID_" + lootTableNumber);
            }
        }
    }
}
