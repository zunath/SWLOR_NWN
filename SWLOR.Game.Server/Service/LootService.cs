using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class LootService: ILootService
    {
        private readonly IDataService _data;
        private readonly IRandomService _random;
        

        public LootService(IDataService data,
            IRandomService random)
        {
            _data = data;
            _random = random;
        }

        public ItemVO PickRandomItemFromLootTable(int lootTableID)
        {
            if (lootTableID <= 0) return null;
            var lootTableItems = _data.Where<LootTableItem>(x => x.LootTableID == lootTableID).ToList();

            if (lootTableItems.Count <= 0) return null;
            int[] weights = new int[lootTableItems.Count];
            for (int x = 0; x < lootTableItems.Count; x++)
            {
                weights[x] = lootTableItems.ElementAt(x).Weight;
            }

            int randomIndex = _random.GetRandomWeightedIndex(weights);
            LootTableItem itemEntity = lootTableItems.ElementAt(randomIndex);
            int quantity = _random.Random(itemEntity.MaxQuantity) + 1;
            ItemVO result = new ItemVO
            {
                Quantity = quantity,
                Resref = itemEntity.Resref,
                SpawnRule = itemEntity.SpawnRule
            };
            
            return result;
        }

        public void OnCreatureDeath(NWCreature creature)
        {
            // Single loot table (without an index)
            int singleLootTableID = creature.GetLocalInt("LOOT_TABLE_ID");
            if (singleLootTableID > 0)
            {
                int chance = creature.GetLocalInt("LOOT_TABLE_CHANCE");
                int attempts = creature.GetLocalInt("LOOT_TABLE_ATTEMPTS");

                RunLootAttempt(creature, singleLootTableID, chance, attempts);
            }

            // Multiple loot tables (with an index)
            int lootTableNumber = 1;
            int lootTableID = creature.GetLocalInt("LOOT_TABLE_ID_" + lootTableNumber);
            while (lootTableID > 0)
            {
                int chance = creature.GetLocalInt("LOOT_TABLE_CHANCE_" + lootTableNumber);
                int attempts = creature.GetLocalInt("LOOT_TABLE_ATTEMPTS_" + lootTableNumber);
                
                RunLootAttempt(creature, lootTableID, chance, attempts);

                lootTableNumber++;
                lootTableID = creature.GetLocalInt("LOOT_TABLE_ID_" + lootTableNumber);
            }
        }

        private void RunLootAttempt(NWCreature target, int lootTableID, int chance, int attempts)
        {
            if (chance <= 0)
                chance = 75;
            else if (chance > 100) chance = 100;

            if (attempts <= 0)
                attempts = 1;

            for (int a = 1; a <= attempts; a++)
            {
                if (_random.Random(100) + 1 <= chance)
                {
                    ItemVO model = PickRandomItemFromLootTable(lootTableID);
                    if (model == null) continue;

                    int spawnQuantity = model.Quantity > 1 ? _random.Random(1, model.Quantity) : 1;

                    for (int x = 1; x <= spawnQuantity; x++)
                    {
                        var item = _.CreateItemOnObject(model.Resref, target);
                        if (!string.IsNullOrWhiteSpace(model.SpawnRule))
                        {
                            App.ResolveByInterface<ISpawnRule>("SpawnRule." + model.SpawnRule, action =>
                            {
                                action.Run(item);
                            });
                        }
                    }
                }
            }
        }
    }
}
