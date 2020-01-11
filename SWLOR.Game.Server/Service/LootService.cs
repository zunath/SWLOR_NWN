using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class LootService
    {
        private static readonly Dictionary<LootTable, LootTableAttribute> _allLootTables = new Dictionary<LootTable, LootTableAttribute>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad> (Message => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnCreatureDeath>(message => OnCreatureDeath());
        }

        private static void OnModuleLoad()
        {
            var lootTables = Enum.GetValues(typeof(LootTable)).Cast<LootTable>();

            foreach (var lt in lootTables)
            {
                var ltAttr = lt.GetAttribute<LootTable, LootTableAttribute>();
                var lootItemAttrs = lt.GetAttributes<LootTable, LootTableItemAttribute>();

                foreach (var lti in lootItemAttrs)
                {
                    ltAttr.LootTableItems.Add(lti);
                }

                _allLootTables[lt] = ltAttr;
            }
        }

        public static LootTableAttribute GetLootTable(LootTable lootTable)
        {
            return _allLootTables[lootTable];
        }

        public static ItemVO PickRandomItemFromLootTable(LootTable lootTableID)
        {
            if (lootTableID == LootTable.Invalid) return null;
            var lootTableItems = _allLootTables[lootTableID].LootTableItems;

            if (lootTableItems.Count <= 0) return null;
            int[] weights = new int[lootTableItems.Count];
            for (int x = 0; x < lootTableItems.Count; x++)
            {
                weights[x] = lootTableItems.ElementAt(x).Weight;
            }

            int randomIndex = RandomService.GetRandomWeightedIndex(weights);
            var itemEntity = lootTableItems.ElementAt(randomIndex);
            int quantity = RandomService.Random(itemEntity.MaxQuantity) + 1;
            ItemVO result = new ItemVO
            {
                Quantity = quantity,
                Resref = itemEntity.Resref,
                SpawnRule = itemEntity.SpawnRule
            };
            
            return result;
        }

        private static void OnCreatureDeath()
        {
            ProcessLoot();
            ProcessCorpse();
        }

        private static void ProcessLoot()
        {
            NWCreature creature = NWGameObject.OBJECT_SELF;
            
            // Single loot table (without an index)
            var singleLootTableID = (LootTable)creature.GetLocalInt("LOOT_TABLE_ID");
            if (singleLootTableID > 0)
            {
                int chance = creature.GetLocalInt("LOOT_TABLE_CHANCE");
                int attempts = creature.GetLocalInt("LOOT_TABLE_ATTEMPTS");

                RunLootAttempt(creature, singleLootTableID, chance, attempts);
            }

            // Multiple loot tables (with an index)
            int lootTableNumber = 1;
            var lootTableID = (LootTable)creature.GetLocalInt("LOOT_TABLE_ID_" + lootTableNumber);
            while (lootTableID > 0)
            {
                int chance = creature.GetLocalInt("LOOT_TABLE_CHANCE_" + lootTableNumber);
                int attempts = creature.GetLocalInt("LOOT_TABLE_ATTEMPTS_" + lootTableNumber);

                RunLootAttempt(creature, lootTableID, chance, attempts);

                lootTableNumber++;
                lootTableID = (LootTable)creature.GetLocalInt("LOOT_TABLE_ID_" + lootTableNumber);
            }
        }

        private static void RunLootAttempt(NWCreature target, LootTable lootTableID, int chance, int attempts)
        {
            if (chance <= 0)
                chance = 75;
            else if (chance > 100) chance = 100;

            if (attempts <= 0)
                attempts = 1;

            for (int a = 1; a <= attempts; a++)
            {
                if (RandomService.Random(100) + 1 <= chance)
                {
                    ItemVO model = PickRandomItemFromLootTable(lootTableID);
                    if (model == null) continue;

                    int spawnQuantity = model.Quantity > 1 ? RandomService.Random(1, model.Quantity) : 1;

                    for (int x = 1; x <= spawnQuantity; x++)
                    {
                        var item = CreateItemOnObject(model.Resref, target);
                        if (!string.IsNullOrWhiteSpace(model.SpawnRule))
                        {
                            var rule = SpawnService.GetSpawnRule(model.SpawnRule);
                            rule.Run(item);
                        }
                    }
                }
            }
        }


        private static void ProcessCorpse()
        {
            SetIsDestroyable(false);

            NWObject self = NWGameObject.OBJECT_SELF;
            if (self.Tag == "spaceship_copy") return;

            Vector lootPosition = Vector(self.Position.X, self.Position.Y, self.Position.Z - 0.11f);
            Location spawnLocation = Location(self.Area, lootPosition, self.Facing);

            NWPlaceable container = CreateObject(ObjectType.Placeable, "corpse", spawnLocation);
            container.SetLocalObject("CORPSE_BODY", self);
            container.Name = self.Name + "'s Corpse";

            container.AssignCommand(() =>
            {
                TakeGoldFromCreature(self.Gold, self);
            });

            // Dump equipped items in container
            for (int slot = 0; slot < NWNConstants.NumberOfInventorySlots; slot++)
            {
                if (slot == (int)InventorySlot.CreatureSkin ||
                    slot == (int)InventorySlot.CreatureWeaponBite ||
                    slot == (int)InventorySlot.CreatureWeaponLeft ||
                    slot == (int)InventorySlot.CreatureWeaponRight)
                    continue;

                NWItem item = GetItemInSlot((InventorySlot)slot, self);
                if (item.IsValid && !item.IsCursed && item.IsDroppable)
                {
                    NWItem copy = CopyItem(item, container, true);

                    if (slot == (int)InventorySlot.Head ||
                        slot == (int)InventorySlot.Chest)
                    {
                        copy.SetLocalObject("CORPSE_ITEM_COPY", item);
                    }
                    else
                    {
                        item.Destroy();
                    }
                }
            }

            foreach (var item in self.InventoryItems)
            {
                if (item.IsValid && !item.IsCursed && item.IsDroppable)
                {
                    CopyItem(item, container, true);
                    item.Destroy();
                }
            }

            DelayCommand(360.0f, () =>
            {
                if (!container.IsValid) return;

                NWObject body = container.GetLocalObject("CORPSE_BODY");
                body.AssignCommand(() => SetIsDestroyable(true));
                body.DestroyAllInventoryItems();
                body.Destroy();

                container.DestroyAllInventoryItems();
                container.Destroy();
            });

        }

    }
}
