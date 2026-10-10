using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Force enhancement retirement preserves live and archived weapon damage", Category = "ForceEnhancement")]
        public static async Task RetireForceWeaponEnhancements(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Force), weapon);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var damage = PropertyValue(weapon, ItemPropertyType.DMG);
                var serialized = ObjectPlugin.Serialize(weapon);
                var migrated = ForceWeaponEnhancementMigration.MigrateSerializedObject(serialized);
                ctx.Assert(migrated != serialized, "The archived Force property is removed");
                var loaded = ObjectPlugin.Deserialize(migrated);
                ctx.Track(loaded);
                ctx.AssertEqual(damage, PropertyValue(loaded, ItemPropertyType.DMG), "Archived total DMG is preserved");
                ctx.AssertEqual(-1, PropertyValue(loaded, ItemPropertyType.WeaponDamageType), "Archived damage type reverts to physical");
                ctx.AssertEqual(migrated, ForceWeaponEnhancementMigration.MigrateSerializedObject(migrated), "Archive retry is unchanged");

                ctx.Assert(ForceWeaponEnhancementMigration.MigrateObject(owner), "Live inventory is migrated");
                ctx.AssertEqual(damage, PropertyValue(weapon, ItemPropertyType.DMG), "Live total DMG is preserved");
                ctx.AssertEqual(-1, PropertyValue(weapon, ItemPropertyType.WeaponDamageType), "Live Force property is removed immediately");
                ctx.AssertEqual(false, ForceWeaponEnhancementMigration.MigrateObject(owner), "Live retry is unchanged");
            });
        }

        [EngineTest("Force enhancement retirement preserves intentional NPC Force weapons", Category = "ForceEnhancement")]
        public static async Task PreserveNpcForceWeaponEnhancements(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalInt(weapon, Item.NoEconomyVariable, 1);
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Force), weapon);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var data = ObjectPlugin.Serialize(weapon);
                ctx.AssertEqual(data, ForceWeaponEnhancementMigration.MigrateSerializedObject(data), "NPC archive is unchanged");
                ctx.AssertEqual(false, ForceWeaponEnhancementMigration.MigrateObject(owner), "NPC live weapon is unchanged");
                AssertDamageType(ctx, weapon, CombatDamageType.Force);
            });
        }

        [EngineTest("Force enhancement retirement persists all storage surfaces beyond fifty records and safely retries", Category = "ForceEnhancement")]
        public static async Task PersistForceEnhancementRetirement(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetName(weapon, "Weapon Enhancement - Force Damage I");
                SetLocalInt(weapon, "BLUEPRINT_RECIPE_ID", (int)RecipeType.WeaponEnhancementDMGForce1);
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Force), weapon);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var data = ObjectPlugin.Serialize(weapon);
                var migrated = ForceWeaponEnhancementMigration.MigrateSerializedObject(data);
                var banks = Enumerable.Range(0, 60).Select(_ => new InventoryItem
                {
                    PlayerId = "force-migration-owner", StorageId = "force-migration-bank", Data = data,
                    Name = GetName(weapon), Quantity = 7, Resref = "b_longsword"
                }).ToArray();
                var market = new MarketItem { Data = data, Name = GetName(weapon), Price = 1234, Quantity = 9, IsListed = true };
                var category = new WorldPropertyCategory
                {
                    Items = { ["stable-item-id"] = new WorldPropertyItem { Data = data, Name = GetName(weapon), Quantity = 11 } }
                };
                var property = new WorldProperty { SerializedItem = data };
                var outfit = new PlayerOutfit { Data = data, TorsoId = 42 };
                var creature = new DMCreature("Test creature", "force-migration-creature", ObjectPlugin.Serialize(owner));
                var migratedCreature = ForceWeaponEnhancementMigration.MigrateSerializedObject(creature.Data);
                var ship = new PlayerShip
                {
                    SerializedItem = data, Status = new ShipStatus
                    {
                        HighPowerModules = { [1] = new ShipStatus.ShipStatusModule { SerializedItem = data, ModuleBonus = 7 } },
                        LowPowerModules = { [2] = new ShipStatus.ShipStatusModule { SerializedItem = data, ModuleBonus = 8 } },
                        ConfigurationModules = { [3] = new ShipStatus.ShipStatusModule { SerializedItem = data, ModuleBonus = 9 } }
                    }
                };
                var date = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
                var contracts = new[]
                {
                    new QuestContract
                    {
                        AuthorPlayerId = "force-migration-owner", Title = "Escrow migration fixture",
                        Status = QuestContractStatus.Published, RewardCredits = 1234, CompletionsRemaining = 2,
                        DatePublished = date, DateExpires = date.AddDays(7), RewardItems = EscrowItems()
                    },
                    new QuestContract { RewardItems = null },
                    new QuestContract()
                };
                var deliveries = new[]
                {
                    new QuestContractDelivery
                    {
                        PlayerId = "force-migration-owner", Credits = 4321, SourceContractId = contracts[0].Id,
                        SourceContractTitle = contracts[0].Title, IsRewardPayment = true, ClaimRevision = 7,
                        Items = EscrowItems()
                    },
                    new QuestContractDelivery
                    {
                        PlayerId = "force-migration-owner", SourceContractId = contracts[0].Id,
                        SourceContractTitle = contracts[0].Title, HeldForCompletion = true, Items = EscrowItems()
                    },
                    new QuestContractDelivery { Items = null },
                    new QuestContractDelivery()
                };
                var originalContracts = contracts.Select(JObject.FromObject).ToArray();
                var originalDeliveries = deliveries.Select(JObject.FromObject).ToArray();
                var job = new ResearchJob
                {
                    SerializedItem = data, Recipe = RecipeType.WeaponEnhancementDMGForce1,
                    DateStarted = date, DateCompleted = date.AddDays(2), Level = 3
                };
                var player = new Player("force-migration-owner");
                player.UnlockedRecipes[RecipeType.WeaponEnhancementDMGForce1] = date;
                player.CraftedRecipes[RecipeType.WeaponEnhancementDMGForce1] = date;
                var originalMarket = JObject.FromObject(market);
                var originalJob = JObject.FromObject(job);
                try
                {
                    foreach (var bank in banks) DB.Set(bank);
                    DB.Set(market);
                    foreach (var contract in contracts) DB.Set(contract);
                    foreach (var delivery in deliveries) DB.Set(delivery);
                    DB.Set(category);
                    DB.Set(property);
                    DB.Set(outfit);
                    DB.Set(creature);
                    DB.Set(ship);
                    DB.Set(job);
                    DB.Set(player);
                    for (var run = 0; run < 2; run++)
                    {
                        new _23_RetireForceWeaponEnhancements().Migrate();
                        foreach (var bank in banks)
                        {
                            var saved = ReadPersisted<InventoryItem>(bank.Id);
                            ctx.AssertEqual(migrated, saved.Data, "Every bank item is persisted, including results beyond fifty");
                            ctx.AssertEqual("Weapon Enhancement - DMG I", saved.Name, "Bank display name");
                            ctx.AssertEqual(7, saved.Quantity, "Bank quantity");
                            ctx.AssertEqual("b_longsword", saved.Resref, "Bank resref");
                        }
                        var savedMarket = ReadPersisted<MarketItem>(market.Id);
                        ctx.AssertEqual(migrated, savedMarket.Data, "Market payload");
                        ctx.AssertEqual("Weapon Enhancement - DMG I", savedMarket.Name, "Market display name");
                        AssertPreservedMetadata(ctx, originalMarket, savedMarket, "Name", "Data");
                        for (var index = 0; index < contracts.Length; index++)
                        {
                            var savedContract = ReadPersisted<QuestContract>(contracts[index].Id);
                            AssertPreservedMetadata(ctx, originalContracts[index], savedContract, "RewardItems");
                            AssertEscrowItems(originalContracts[index]["RewardItems"], savedContract.RewardItems);
                        }
                        for (var index = 0; index < deliveries.Length; index++)
                        {
                            var savedDelivery = ReadPersisted<QuestContractDelivery>(deliveries[index].Id);
                            AssertPreservedMetadata(ctx, originalDeliveries[index], savedDelivery, "Items");
                            AssertEscrowItems(originalDeliveries[index]["Items"], savedDelivery.Items);
                        }
                        var savedCategory = ReadPersisted<WorldPropertyCategory>(category.Id);
                        ctx.AssertEqual(1, savedCategory.Items.Count, "Property item identity is retained");
                        ctx.AssertEqual(migrated, savedCategory.Items["stable-item-id"].Data, "Property storage payload");
                        ctx.AssertEqual(11, savedCategory.Items["stable-item-id"].Quantity, "Property quantity");
                        ctx.AssertEqual(migrated, ReadPersisted<WorldProperty>(property.Id).SerializedItem, "Structure payload");
                        ctx.AssertEqual(migrated, ReadPersisted<PlayerOutfit>(outfit.Id).Data, "Outfit payload");
                        ctx.AssertEqual(42, ReadPersisted<PlayerOutfit>(outfit.Id).TorsoId, "Outfit appearance");
                        ctx.AssertEqual(migratedCreature, ReadPersisted<DMCreature>(creature.Id).Data, "Creature archive");
                        var savedShip = ReadPersisted<PlayerShip>(ship.Id);
                        ctx.AssertEqual(migrated, savedShip.SerializedItem, "Ship payload");
                        ctx.AssertEqual(migrated, savedShip.Status.HighPowerModules[1].SerializedItem, "High module payload");
                        ctx.AssertEqual(migrated, savedShip.Status.LowPowerModules[2].SerializedItem, "Low module payload");
                        ctx.AssertEqual(migrated, savedShip.Status.ConfigurationModules[3].SerializedItem, "Configuration payload");
                        ctx.AssertEqual(7, savedShip.Status.HighPowerModules[1].ModuleBonus, "Module state");
                        var savedJob = ReadPersisted<ResearchJob>(job.Id);
                        ctx.AssertEqual(migrated, savedJob.SerializedItem, "Research payload");
                        ctx.AssertEqual(RecipeType.WeaponEnhancementDMGPhysical1, savedJob.Recipe, "Research recipe");
                        AssertPreservedMetadata(ctx, originalJob, savedJob, "Recipe", "SerializedItem");
                        var savedPlayer = ReadPersisted<Player>(player.Id);
                        ctx.AssertEqual(date, savedPlayer.UnlockedRecipes[RecipeType.WeaponEnhancementDMGPhysical1], "Unlocked recipe investment");
                        ctx.AssertEqual(date, savedPlayer.CraftedRecipes[RecipeType.WeaponEnhancementDMGPhysical1], "Crafted recipe investment");
                    }
                    banks[0].Data = "invalid serialized object";
                    DB.Set(banks[0]);
                    var before = DB.GetRawJson<InventoryItem>(banks[0].Id);
                    var failed = false;
                    try { new _23_RetireForceWeaponEnhancements().Migrate(); }
                    catch (InvalidOperationException ex) { failed = ex.Message.Contains(banks[0].Id); }
                    ctx.Assert(failed, "Corrupt data fails with the record ID for repair");
                    ctx.AssertEqual(before, DB.GetRawJson<InventoryItem>(banks[0].Id), "Corrupt record is never overwritten");
                }
                finally
                {
                    foreach (var bank in banks) DB.Delete<InventoryItem>(bank.Id);
                    DB.Delete<MarketItem>(market.Id);
                    foreach (var contract in contracts) DB.Delete<QuestContract>(contract.Id);
                    foreach (var delivery in deliveries) DB.Delete<QuestContractDelivery>(delivery.Id);
                    DB.Delete<WorldPropertyCategory>(category.Id);
                    DB.Delete<WorldProperty>(property.Id);
                    DB.Delete<PlayerOutfit>(outfit.Id);
                    DB.Delete<DMCreature>(creature.Id);
                    DB.Delete<PlayerShip>(ship.Id);
                    DB.Delete<ResearchJob>(job.Id);
                    DB.Delete<Player>(player.Id);
                }

                List<QuestContractItem> EscrowItems() => new()
                {
                    new QuestContractItem { Data = data, Name = GetName(weapon), StackSize = 9, Resref = "b_longsword", IconResref = "test-icon" },
                    new QuestContractItem { Data = data, Name = "Custom reward name", StackSize = 3, Resref = "b_longsword", IconResref = "custom-icon" },
                    new QuestContractItem { Data = migrated, Name = "Already migrated reward", StackSize = 2, Resref = "b_longsword", IconResref = "other-icon" }
                };

                void AssertEscrowItems(JToken original, List<QuestContractItem> saved)
                {
                    if (original.Type == JTokenType.Null)
                    {
                        ctx.Assert(saved == null, "Null escrow lists remain null");
                        return;
                    }
                    ctx.AssertEqual(original.Count(), saved.Count, "Escrow item count and order are retained");
                    for (var index = 0; index < saved.Count; index++)
                    {
                        ctx.AssertEqual(migrated, saved[index].Data, "Escrow payload is migrated before any later claim");
                        ctx.AssertEqual(index == 0 ? "Weapon Enhancement - DMG I" : original[index]["Name"].Value<string>(),
                            saved[index].Name, "Only default escrow display names change");
                        AssertPreservedMetadata(ctx, (JObject)original[index], saved[index], "Name", "Data");
                    }
                }
            });
        }
    }
}
