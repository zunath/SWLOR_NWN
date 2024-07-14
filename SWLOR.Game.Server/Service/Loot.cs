﻿using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service
{
    public class Loot
    {
        private static readonly Dictionary<string, LootTable> _lootTables = new();

        private const float CorpseLifespanSeconds = 360f;
        public const string CorpseBodyVariable = "CORPSE_BODY";
        private const string CorpseCopyItemVariable = "CORPSE_ITEM_COPY";

        [NWNEventHandler("mod_cache_bef")]
        public static void RegisterLootTables()
        {
            // Get all implementations of spawn table definitions.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ILootTableDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (ILootTableDefinition)Activator.CreateInstance(type);
                var builtTables = instance.BuildLootTables();

                foreach (var table in builtTables)
                {
                    if (string.IsNullOrWhiteSpace(table.Key))
                    {
                        Log.Write(LogGroup.Error, $"Loot table {table.Key} has an invalid key. Values must not be null or white space.");
                        continue;
                    }

                    if (_lootTables.ContainsKey(table.Key))
                    {
                        Log.Write(LogGroup.Error, $"Loot table {table.Key} has already been registered. Please make sure all spawn tables use a unique ID.");
                        continue;
                    }

                    _lootTables[table.Key] = table.Value;
                }
            }
        }

        /// <summary>
        /// When a creature spawns, items which can be stolen are spawned and marked as undroppable.
        /// These items are only available with the Thief ability "Steal" and related perks.
        /// </summary>
        [NWNEventHandler("crea_spawn_bef")]
        public static void SpawnStealLoot()
        {
            var creature = OBJECT_SELF;
            var lootTables = GetLootTableDetails(creature, "STEAL_LOOT_TABLE_");

            foreach (var lootTable in lootTables)
            {
                var (tableName, chance, attempts) = ParseLootTableArguments(lootTable);

                foreach (var item in SpawnLoot(creature, creature, tableName, chance, attempts))
                {
                    SetItemCursedFlag(item, true);
                    SetDroppableFlag(item, false);

                    SetLocalBool(item, "STEAL_ITEM", true);
                }
            }
        }

        /// <summary>
        /// Retrieves the name of the loot table, the chance to spawn an item, and the number of attempts
        /// by a given comma delimited loot table string.
        /// </summary>
        /// <param name="delimitedString">The comma delimited string </param>
        /// <returns>The table name, the percent chance, and the number of attempts</returns>
        private static (string, int, int) ParseLootTableArguments(string delimitedString)
        {
            var data = delimitedString.Split(',');
            var tableName = data[0].Trim();
            var chance = 100;
            var attempts = 1;

            // Second argument: Chance to spawn
            if (data.Length > 1)
            {
                data[1] = data[1].Trim();
                if (!int.TryParse(data[1], out chance))
                {
                    Log.Write(LogGroup.Error, $"Loot Table with arguments '{delimitedString}', 'Chance' variable could not be processed. Must be an integer.");
                }
            }

            // Third argument: Attempts to pull from loot table
            if (data.Length > 2)
            {
                data[2] = data[2].Trim();
                if (!int.TryParse(data[2], out attempts))
                {
                    Log.Write(LogGroup.Error, $"Loot Table with arguments '{delimitedString}', 'Attempts' variable could not be processed. Must be an integer.");
                }
            }

            // Guards against bad data from builder.
            if (chance > 100)
                chance = 100;

            if (attempts <= 0)
                attempts = 1;

            return (tableName, chance, attempts);
        }

        private static List<uint> SpawnLoot(uint source, uint receiver, string lootTableName, int chance, int attempts)
        {
            var creditFinderLevel = GetLocalInt(source, "CREDITFINDER_LEVEL");
            var creditPercentIncrease = creditFinderLevel * 0.2f;
            var rareBonusChance = GetLocalInt(source, "RARE_BONUS_CHANCE");

            var lootList = new List<uint>();
            var table = GetLootTableByName(lootTableName);
            if (rareBonusChance > 0 && table.IsRare)
            {
                chance += rareBonusChance;
            }
            for (int x = 1; x <= attempts; x++)
            {
                if (Random.D100(1) > chance) continue;

                var item = table.GetRandomItem(rareBonusChance);
                var quantity = Random.Next(item.MaxQuantity) + 1;

                // CreditFinder perk - Increase the quantity of gold found.
                if (item.Resref == "nw_it_gold001")
                {
                    quantity += (int)(quantity * creditPercentIncrease);
                }

                var loot = CreateItemOnObject(item.Resref, receiver, quantity);
                lootList.Add(loot);

                item.OnSpawn?.Invoke(loot);
            }

            return lootList;
        }

        /// <summary>
        /// Attempts to spawn items found in the associated loot table based on the configured variables.
        /// </summary>
        /// <param name="source">The source of the items (must contain the local variables)</param>
        /// <param name="receiver">The receiver of the items</param>
        /// <param name="lootTablePrefix">The prefix of the loot tables. In the toolset this should be numeric starting at 1.</param>
        public static void SpawnLoot(uint source, uint receiver, string lootTablePrefix)
        {
            var lootTableEntries = GetLootTableDetails(source, lootTablePrefix);
            foreach (var entry in lootTableEntries)
            {
                var delimitedString = GetLocalString(source, entry);
                var (tableName, chance, attempts) = ParseLootTableArguments(delimitedString);

                SpawnLoot(source, receiver, tableName, chance, attempts);
            }
        }

        /// <summary>
        /// When a creature dies, loot tables are spawned based on local variables.
        /// </summary>
        [NWNEventHandler("crea_death_bef")]
        public static void SpawnLootOnCreatureDeath()
        {
            SpawnLoot(OBJECT_SELF, OBJECT_SELF, "LOOT_TABLE_");
        }

        /// <summary>
        /// Retrieves a loot table by its unique name.
        /// If name is not registered, an exception will be raised.
        /// </summary>
        /// <param name="name">The name of the loot table to retrieve.</param>
        /// <returns>A loot table matching the specified name.</returns>
        public static LootTable GetLootTableByName(string name)
        {
            if (!_lootTables.ContainsKey(name))
                throw new Exception($"Loot table '{name}' is not registered. Did you enter the right name?");

            return _lootTables[name];
        }

        /// <summary>
        /// Checks whether a loot table is defined by the specified name.
        /// </summary>
        /// <param name="name">The name to search by.</param>
        /// <returns>true if the loot table exists, false otherwise</returns>
        public static bool LootTableExists(string name)
        {
            return _lootTables.ContainsKey(name);
        }
        
        /// <summary>
        /// Returns all of the loot table details found on a creature's local variables.
        /// </summary>
        /// <param name="creature">The creature to search.</param>
        /// <param name="lootTablePrefix">The prefix of the loot tables to look for.</param>
        /// <returns>A list of loot table details.</returns>
        private static IEnumerable<string> GetLootTableDetails(uint creature, string lootTablePrefix)
        {
            var lootTables = new List<string>();

            int index = 1;
            var localVariableName = lootTablePrefix + index;
            var localVariable = GetLocalString(creature, localVariableName);

            while (!string.IsNullOrWhiteSpace(localVariable))
            {
                localVariable = GetLocalString(creature, localVariableName);
                if (string.IsNullOrWhiteSpace(localVariable)) break;

                lootTables.Add(localVariableName);

                index++;
                localVariableName = lootTablePrefix + index;
            }

            return lootTables;
        }

        /// <summary>
        /// When a creature is hit by another creature with the Creditfinder or Treasure Hunter perk,
        /// a local variable is set on the creature which will be picked up when spawning items.
        /// These will be checked later when the creature dies and loot is spawned.
        /// </summary>
        [NWNEventHandler("item_on_hit")]
        public static void MarkCreditfinderAndTreasureHunterOnTarget()
        {
            var attacker = OBJECT_SELF;
            var target = GetSpellTargetObject();
            if (GetIsPC(target) || GetIsDM(target)) 
                return;

            var currentCreditFinder = GetLocalInt(target, "CREDITFINDER_LEVEL");
            var currentTreasureHunter = GetLocalInt(target, "RARE_BONUS_CHANCE");

            var creditFinderLevel = Perk.GetPerkLevel(attacker, PerkType.CreditFinder);
            var treasureHunterLevel = Perk.GetPerkLevel(attacker, PerkType.TreasureHunter) * 10;
            var sniffLevel = Perk.GetPerkLevel(attacker, PerkType.Sniff);
            switch (sniffLevel)
            {
                case 1:
                    sniffLevel = 8;
                    break;
                case 2:
                    sniffLevel = 15;
                    break;
                case 3:
                    sniffLevel = 25;
                    break;
                default:
                    sniffLevel = 0;
                    break;
            }

            var rareBonusChance = treasureHunterLevel;
            if (sniffLevel > rareBonusChance)
                rareBonusChance = sniffLevel;

            if (creditFinderLevel > currentCreditFinder)
            {
                SetLocalInt(target, "CREDITFINDER_LEVEL", creditFinderLevel);
            }

            if (rareBonusChance > currentTreasureHunter)
            {
                SetLocalInt(target, "RARE_BONUS_CHANCE", rareBonusChance);
            }
        }


        /// <summary>
        /// Handles creating a corpse placeable on a creature's death, copying its inventory to the placeable,
        /// and changing the name of the placeable to match the creature.
        /// </summary>
        [NWNEventHandler("crea_death_bef")]
        public static void ProcessCorpse()
        {
            var self = OBJECT_SELF;
            SetIsDestroyable(false);

            var area = GetArea(self);
            var position = GetPosition(self);
            var facing = GetFacing(self);
            var lootPosition = Vector3(position.X, position.Y, position.Z - 0.11f);
            var spawnLocation = Location(area, lootPosition, facing);
            var npcStats = Stat.GetNPCStats(self);

            var container = CreateObject(ObjectType.Placeable, "corpse", spawnLocation);
            SetLocalObject(container, CorpseBodyVariable, self);
            SetName(container, $"{GetName(self)}'s Corpse");
            SetLocalInt(container, BeastMastery.BeastTypeVariable, GetLocalInt(self, BeastMastery.BeastTypeVariable));
            SetLocalInt(container, BeastMastery.BeastLevelVariable, npcStats.Level);

            AssignCommand(container, () =>
            {
                var gold = GetGold(self);
                TakeGoldFromCreature(gold, self);
            });

            // Dump equipped items in container
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            {
                if (slot == (int)InventorySlot.CreatureArmor ||
                    slot == (int)InventorySlot.CreatureBite ||
                    slot == (int)InventorySlot.CreatureLeft ||
                    slot == (int)InventorySlot.CreatureRight)
                    continue;

                var item = GetItemInSlot((InventorySlot)slot, self);
                if (GetIsObjectValid(item) && !GetItemCursedFlag(item) && GetDroppableFlag(item))
                {
                    var copy = CopyItem(item, container, true);

                    if (slot == (int)InventorySlot.Head ||
                        slot == (int)InventorySlot.Chest)
                    {
                        SetLocalObject(copy, CorpseCopyItemVariable, item);
                    }
                    else
                    {
                        DestroyObject(item);
                    }
                }
            }

            for (var item = GetFirstItemInInventory(self); GetIsObjectValid(item); item = GetNextItemInInventory(self))
            {
                if (GetIsObjectValid(item) && !GetItemCursedFlag(item) && GetDroppableFlag(item))
                {
                    CopyItem(item, container, true);
                    DestroyObject(item);
                }
            }

            ScheduleCorpseCleanup(container);
        }

        private static void ScheduleCorpseCleanup(uint placeable)
        {
            DelayCommand(CorpseLifespanSeconds, () =>
            {
                if (!GetIsObjectValid(placeable))
                    return;

                var body = GetLocalObject(placeable, CorpseBodyVariable);
                AssignCommand(body, () => SetIsDestroyable());

                for (var item = GetFirstItemInInventory(body); GetIsObjectValid(item); item = GetNextItemInInventory(body))
                {
                    DestroyObject(item);
                }
                DestroyObject(body);

                for (var item = GetFirstItemInInventory(placeable); GetIsObjectValid(item); item = GetNextItemInInventory(placeable))
                {
                    DestroyObject(item);
                }
                DestroyObject(placeable);
            });
        }

        /// <summary>
        /// When the loot corpse is closed, either spawn an "Extract" placeable to be used with Beast Mastery DNA extraction
        /// or remove the dead creature from the game.
        /// </summary>
        [NWNEventHandler("corpse_closed")]
        public static void CloseCorpseContainer()
        {
            var container = OBJECT_SELF;
            var firstItem = GetFirstItemInInventory(container);
            var corpseOwner = GetLocalObject(container, CorpseBodyVariable);
            var beastTypeId = GetLocalInt(container, BeastMastery.BeastTypeVariable);
            var level = GetLocalInt(container, BeastMastery.BeastLevelVariable);

            if (!GetIsObjectValid(firstItem))
            {
                DestroyObject(container);

                if (beastTypeId <= 0)
                {
                    AssignCommand(corpseOwner, () =>
                    {
                        SetIsDestroyable();
                    });
                }
                else
                {
                    var beastType = (BeastType)beastTypeId;
                    var beastDetail = BeastMastery.GetBeastDetail(beastType);
                    var extractCorpse = CreateObject(ObjectType.Placeable, BeastMastery.ExtractCorpseObjectResref, GetLocation(container));
                    SetLocalObject(extractCorpse, CorpseBodyVariable, corpseOwner);
                    SetLocalInt(extractCorpse, BeastMastery.BeastTypeVariable, beastTypeId);
                    SetLocalInt(extractCorpse, BeastMastery.BeastLevelVariable, level);
                    
                    AssignCommand(extractCorpse, () =>
                    {
                        ScheduleCorpseCleanup(extractCorpse);
                    });
                    SetName(extractCorpse, $"Extract DNA: {beastDetail.Name}");
                }
            }
        }

        /// <summary>
        /// When a player adds an item to a corpse, return it to them.
        /// When a player removes an item from the corpse, update the connected creature's appearance if needed.
        /// </summary>
        [NWNEventHandler("corpse_disturbed")]
        public static void DisturbCorpseContainer()
        {
            var looter = GetLastDisturbed();
            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();

            AssignCommand(looter, () =>
            {
                ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 1.0f);
            });

            if (type == DisturbType.Added)
            {
                Item.ReturnItem(looter, item);
                SendMessageToPC(looter, "You cannot place items inside of corpses.");
            }
            else if (type == DisturbType.Removed)
            {
                var copy = GetLocalObject(item, CorpseCopyItemVariable);

                if (GetIsObjectValid(copy))
                {
                    DestroyObject(copy);
                }

                DeleteLocalObject(item, CorpseCopyItemVariable);
            }
        }
    }
}
