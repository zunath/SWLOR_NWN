using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NRediSearch;
using NWN.Native.API;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using StackExchange.Redis;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class CraftedFoodDeliveryEngineTests
{
    private const string FoodResref = "esh_hotpot";
    private static readonly MethodInfo ClaimMethod = typeof(CraftedFoodDeliveryService).GetMethod("ClaimPending",
        BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] { typeof(uint), typeof(Action), typeof(Action<CraftedFoodDelivery>) }, null);
    private static readonly MethodInfo CommitMethod = typeof(CraftedFoodDeliveryService).GetMethod("CommitCompletedBatch",
        BindingFlags.NonPublic | BindingFlags.Static, null,
        new[] { typeof(uint), typeof(IReadOnlyList<uint>), typeof(Action), typeof(Action<CraftedFoodDelivery>) }, null);

    [EngineTest("Full player inventory retains four completed foods across native save and reload", Category = "CraftedFoodDelivery")]
    public static Task FullInventoryRetainsCompletedBatch(EngineTestContext ctx) => VerifyAvailableSlots(ctx, 0);

    [EngineTest("One free player inventory slot claims one food and retains three", Category = "CraftedFoodDelivery")]
    public static Task OneFreeSlotClaimsOnePortion(EngineTestContext ctx) => VerifyAvailableSlots(ctx, 1);

    [EngineTest("Three free player inventory slots claim three foods and retain one", Category = "CraftedFoodDelivery")]
    public static Task ThreeFreeSlotsClaimThreePortions(EngineTestContext ctx) => VerifyAvailableSlots(ctx, 3);

    [EngineTest("Four distinct crafted foods retain merged properties and charges through native delivery", Category = "CraftedFoodDelivery")]
    public static Task NativeFourDistinctPropertyParity(EngineTestContext ctx) => VerifyAvailableSlots(ctx, 4);

    [EngineTest("Food character save failure cannot replay portions consumed or traded before retry", Category = "CraftedFoodDelivery")]
    public static Task CharacterSaveFailureUsesReceipt(EngineTestContext ctx) => VerifyInterruptedClaim(ctx, false);

    [EngineTest("Food database save failure cannot replay portions consumed or traded before retry", Category = "CraftedFoodDelivery")]
    public static Task DatabaseSaveFailureUsesReceipt(EngineTestContext ctx) => VerifyInterruptedClaim(ctx, true);

    [EngineTest("Initial food database failure saves a native recovery batch before any Redis write", Category = "CraftedFoodDelivery")]
    public static Task InitialDatabaseFailureBeforeWrite(EngineTestContext ctx) => VerifyInitialCommitFailure(ctx, false);

    [EngineTest("Initial food database acknowledgement failure deduplicates its native recovery batch", Category = "CraftedFoodDelivery")]
    public static Task InitialDatabaseFailureAfterWrite(EngineTestContext ctx) => VerifyInitialCommitFailure(ctx, true);

    [EngineTest("An interrupted food search-index write recovers without treating its missing JSON as a delivery", Category = "CraftedFoodDelivery")]
    public static Task InitialDatabaseFailureAfterIndexWrite(EngineTestContext ctx) => VerifyInitialCommitFailure(ctx, false, true);

    [EngineTest("Completed food exits crafting and survives closing and reopening a full inventory window", Category = "CraftedFoodDelivery")]
    public static async Task CompletedCraftSurvivesWindowLifecycle(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        await ClearCarriedInventory(ctx, player.Creature);
        await InventoryCapacityFixture.CreateAsync(ctx, player.Creature, 0);
        string deliveryId = null;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                player.Update(record => record.Skills[SkillType.Agriculture].Rank = 49);
                var storage = CreateAssemblyStorage(ctx, player.Creature);
                var blueprint = CreateItemOnObject("blueprint", storage);
                ctx.Track(blueprint);
                Craft.SetBlueprintDetails(blueprint, new BlueprintDetail
                {
                    Recipe = RecipeType.EchaniSharedHotpot, Level = 6, LicensedRuns = 1
                });
                var material = CreateItemOnObject("esh_wolf_meat", storage);
                var enhancement = CreateItemOnObject("aen_hp1", storage);
                ctx.Track(material);
                ctx.Track(enhancement);
                var model = BindCraftModel(player.Creature, blueprint);
                ((List<string>)ModelField(model, "_components")).Add(ObjectPlugin.Serialize(material));
                SetModelField(model, "_enhancement1", ObjectPlugin.Serialize(enhancement));
                var properties = (List<SWLOR.NWN.API.Engine.ItemProperty>)ModelField(model, "_itemPropertiesEnhancement1");
                properties.Add(ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CombatReadiness, 3));
                properties.Add(ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CombatReadiness, 5));
                properties.Add(ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Accuracy, 9));
                SetModelField(model, "_activeBlueprint", Craft.GetBlueprintDetails(blueprint));
                SetModelField(model, "_quality", 100);
                SetModelField(model, "_maxQuality", 100);
                model.IsInCraftMode = true;
                model.IsInSetupMode = false;
                model.IsClosable = false;
                CallModel(model, "ProcessSuccess");

                ctx.Assert(!model.IsInCraftMode && model.IsInSetupMode && model.IsClosable,
                    "A completed craft leaves craft mode immediately even when none of its food fits");
                ctx.AssertEqual(4, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "The real completion path persists all four servings");
                var deliveries = DB.Search(new DBQuery<CraftedFoodDelivery>()
                    .AddFieldSearch(nameof(CraftedFoodDelivery.PlayerId), player.Id, false).AddPaging(10, 0)).ToArray();
                ctx.AssertEqual(1, deliveries.Length, "One completion creates one bounded persisted batch");
                deliveryId = deliveries.Single().Id;
                ctx.AssertEqual(0, ((List<string>)ModelField(model, "_components")).Count, "Completed materials cannot be refunded when this window closes");
                for (var slot = 1; slot <= 8; slot++)
                {
                    ctx.AssertEqual(string.Empty, (string)ModelField(model, "_enhancement" + slot), "Completed enhancement serialization is cleared");
                    ctx.AssertEqual(0, ((List<SWLOR.NWN.API.Engine.ItemProperty>)ModelField(model, "_itemPropertiesEnhancement" + slot)).Count,
                        "Completed enhancement properties are cleared");
                }
                var completedPlayer = DB.Get<Player>(player.Id);
                ctx.Assert(completedPlayer.CraftedRecipes.ContainsKey(RecipeType.EchaniSharedHotpot), "Completion records its first-time recipe reward");
                ctx.Assert(completedPlayer.Skills[SkillType.Agriculture].Rank > 49 || completedPlayer.Skills[SkillType.Agriculture].XP > 0,
                    "Completion grants real crafting XP once");
                var completedState = DB.GetRawJson<Player>(player.Id);
                var configuredProperties = new List<string>();
                foreach (var payload in ReadStoredDelivery(deliveryId).Items)
                {
                    var food = ObjectPlugin.Deserialize(payload);
                    ctx.Track(food);
                    ctx.AssertEqual(7, GetItemCharges(food), "The completed blueprint adds its charges to every serving");
                    ctx.AssertEqual(904, ItemPlugin.GetAddGoldPieceValue(food), "Full quality vendor value is finalized before delivery");
                    AssertFoodProperty(ctx, food, FoodItemPropertySubType.CombatReadiness, 23);
                    AssertFoodProperty(ctx, food, FoodItemPropertySubType.Accuracy, 9);
                    AssertFoodProperty(ctx, food, FoodItemPropertySubType.Duration, 1);
                    AssertFoodProperty(ctx, food, FoodItemPropertySubType.XPBonus, 30);
                    ctx.AssertEqual(6, Properties(food).Length, "Completed serialization retains one merged enhancement and one blueprint level");
                    configuredProperties.Add(string.Join("|", Properties(food)));
                    DestroyObject(food);
                }
                ctx.AssertEqual(1, configuredProperties.Distinct().Count(), "All four servings receive the same completed bonuses");
                CallModel(model, "ProcessSuccess");
                model.OnWindowClosed()();
                var reopened = BindCraftModel(player.Creature, blueprint);
                reopened.OnClickManualCraft()();
                ctx.Assert(!reopened.IsInCraftMode, "Reopening cannot start another batch while food remains pending");
                ctx.AssertEqual(4, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "Closing and reopening preserve all finished food");
                ctx.AssertEqual(completedState, DB.GetRawJson<Player>(player.Id), "Duplicate completion and reopening do not grant XP or first-time rewards again");
                ctx.AssertEqual(1, Craft.GetBlueprintDetails(blueprint).LicensedRuns, "Reopening and recovery do not consume another licensed run");
                ctx.AssertEqual(0, OwnedFoods(player.Creature).Length, "Full native inventory never acquires an untracked portion");
                VerifyCharacterExports(ctx, player.Creature);
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(4, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "The persisted completion survives destruction of the window's assembly storage");
        }
        finally
        {
            if (deliveryId != null)
                DB.Delete<CraftedFoodDelivery>(deliveryId);
        }
    }

    private static async Task VerifyAvailableSlots(EngineTestContext ctx, int availableSlots)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        await ClearCarriedInventory(ctx, player.Creature);
        var capacity = await InventoryCapacityFixture.CreateAsync(ctx, player.Creature, availableSlots);
        string deliveryId = null;
        string playerBefore = null;
        int creditsBefore = 0;
        uint blueprint = OBJECT_INVALID;
        uint[] originalFoods = null;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                var storage = CreateAssemblyStorage(ctx, player.Creature);
                blueprint = CreateItemOnObject("blueprint", storage);
                ctx.Track(blueprint);
                Craft.SetBlueprintDetails(blueprint, new BlueprintDetail
                {
                    Recipe = RecipeType.EchaniSharedHotpot, Level = 6, LicensedRuns = 2
                });
                playerBefore = DB.GetRawJson<Player>(player.Id);
                creditsBefore = GetGold(player.Creature);
                originalFoods = CreateConfiguredFoods(ctx, storage);

                // No frame wait here: persistence must not resurrect properties removed while
                // the production merge operation was configuring this same completed batch.
                deliveryId = CraftedFoodDeliveryService.CommitCompletedBatch(player.Creature, originalFoods);
                var committed = ReadStoredDelivery(deliveryId);
                ctx.AssertEqual(4, committed.Items.Count, "All four configured portions reach Redis before acquisition");
                VerifyStoredFoods(ctx, committed.Items);
                ctx.AssertEqual(4, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "The entire batch is pending");
                var blocked = false;
                try { CraftedFoodDeliveryService.CommitCompletedBatch(player.Creature, originalFoods); }
                catch (InvalidOperationException) { blocked = true; }
                ctx.Assert(blocked, "Another multi-food batch cannot accumulate while completed food remains pending");

                Claim(ctx, player.Creature);
                ctx.AssertEqual(4 - availableSlots, CraftedFoodDeliveryService.GetPendingCount(player.Creature),
                    "A production claim removes only food actually acquired by the native player inventory");
                AssertProgressionUnchanged(ctx, player, playerBefore, creditsBefore, blueprint);
            });
            await ctx.WaitFrameAsync();
            ctx.Assert(originalFoods.All(item => !GetIsObjectValid(item)), "Neutral assembly objects are destroyed after the commit");
            AssertOwnedFoods(ctx, player.Creature, availableSlots);

            for (var cycle = 0; cycle < 2; cycle++)
            {
                await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
                {
                    VerifyCharacterExports(ctx, player.Creature);
                    Claim(ctx, player.Creature);
                    ctx.AssertEqual(4 - availableSlots, CraftedFoodDeliveryService.GetPendingCount(player.Creature),
                        "Repeated reopening with the same inventory capacity preserves the remainder");
                    if (availableSlots < 4)
                    {
                        var reloaded = ReadStoredDelivery(deliveryId);
                        ctx.AssertEqual(4 - availableSlots, reloaded.Items.Count, "Reloading Redis retains every undelivered portion");
                        VerifyStoredFoods(ctx, reloaded.Items);
                    }
                    AssertProgressionUnchanged(ctx, player, playerBefore, creditsBefore, blueprint);
                });
                await ctx.WaitFrameAsync();
                AssertOwnedFoods(ctx, player.Creature, availableSlots);
            }

            await capacity.ExpandToAsync(ctx, 4);
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                Claim(ctx, player.Creature);
                Claim(ctx, player.Creature);
                ctx.Assert(!CraftedFoodDeliveryService.HasPending(player.Creature), "Collecting the remainder clears the batch gate");
                ctx.AssertEqual(string.Empty, DB.GetRawJson<CraftedFoodDelivery>(deliveryId), "Fully delivered batches do not accumulate in Redis");
                AssertProgressionUnchanged(ctx, player, playerBefore, creditsBefore, blueprint);
                VerifyCharacterExports(ctx, player.Creature);
            });
            await ctx.WaitFrameAsync();
            AssertOwnedFoods(ctx, player.Creature, 4);
        }
        finally
        {
            if (deliveryId != null)
                DB.Delete<CraftedFoodDelivery>(deliveryId);
        }
    }

    private static async Task VerifyInterruptedClaim(EngineTestContext ctx, bool databaseFailure)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var recipient = ctx.SpawnCreature("civilian", 3f);
        await ClearCarriedInventory(ctx, player.Creature);
        await InventoryCapacityFixture.CreateAsync(ctx, player.Creature, 4);
        string deliveryId = null;
        string playerBefore = null;
        int creditsBefore = 0;
        uint consumed = OBJECT_INVALID;
        uint[] traded = null;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                var storage = CreateAssemblyStorage(ctx, player.Creature);
                deliveryId = CraftedFoodDeliveryService.CommitCompletedBatch(player.Creature, CreateConfiguredFoods(ctx, storage));
                playerBefore = DB.GetRawJson<Player>(player.Id);
                creditsBefore = GetGold(player.Creature);
                Action saveCharacter = () =>
                {
                    VerifyCharacterExports(ctx, player.Creature);
                    if (!databaseFailure)
                        throw new InvalidOperationException("Injected character-save acknowledgement failure");
                };
                Action<CraftedFoodDelivery> saveDelivery = value =>
                {
                    if (databaseFailure)
                        throw new InvalidOperationException("Injected Redis write failure");
                    DB.Set(value);
                };
                Claim(ctx, player.Creature, saveCharacter, saveDelivery);
                ctx.AssertEqual(4, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "The interrupted claim keeps all persisted value");
                var receipt = JsonConvert.DeserializeObject<CraftedFoodClaim>(GetLocalString(player.Creature, CraftedFoodClaim.CheckpointVariable));
                ctx.AssertEqual(deliveryId, receipt.DeliveryId, "The retained receipt identifies this exact batch");
                ctx.AssertEqual(4, receipt.ItemIndexes.Count, "The retained receipt records every native acquisition");
                AssertOwnedFoods(ctx, player.Creature, 4);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                var foods = OwnedFoods(player.Creature);
                consumed = foods[0];
                SetItemCharges(consumed, 0);
                DestroyObject(consumed);
                traded = foods.Skip(1).ToArray();
                foreach (var food in traded)
                {
                    ctx.Assert(ItemPlugin.MoveTo(food, recipient, true), "The other acquired portions can be transferred before retry");
                    ctx.AssertEqual(recipient, GetItemPossessor(food), "Each transferred portion belongs to the recipient");
                }
            });
            await ctx.WaitFrameAsync();
            ctx.Assert(!GetIsObjectValid(consumed), "The consumed portion has left the live inventory");
            ctx.Assert(traded.All(food => GetItemPossessor(food) == recipient), "All three transferred portions belong to the recipient");

            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                // A normal later save after gameplay still carries the recovery receipt, even
                // though the awarded food is no longer in this character's inventory.
                VerifyCharacterExports(ctx, player.Creature);
                Claim(ctx, player.Creature);
                Claim(ctx, player.Creature);
                ctx.AssertEqual(0, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "Retry finishes only the stored reduction");
                ctx.AssertEqual(playerBefore, DB.GetRawJson<Player>(player.Id), "Claims do not repeat crafting XP or recipe rewards");
                ctx.AssertEqual(creditsBefore, GetGold(player.Creature), "Claims do not issue credits");
                AssertOwnedFoods(ctx, player.Creature, 0);
                AssertOwnedFoods(ctx, recipient, 3);
                VerifyCharacterExports(ctx, player.Creature);
            });
            await ctx.WaitFrameAsync();
            AssertOwnedFoods(ctx, player.Creature, 0);
            ctx.Assert(traded.All(food => GetItemPossessor(food) == recipient), "Retry preserves the original traded objects instead of recreating them");
        }
        finally
        {
            if (deliveryId != null)
                DB.Delete<CraftedFoodDelivery>(deliveryId);
        }
    }

    private static async Task VerifyInitialCommitFailure(EngineTestContext ctx, bool afterWrite, bool indexOnly = false)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        await ClearCarriedInventory(ctx, player.Creature);
        var capacity = await InventoryCapacityFixture.CreateAsync(ctx, player.Creature, 0);
        string deliveryId = null;
        string recoveryData = null;
        string[] expectedPayloads = null;
        var nativeRecoverySaves = 0;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                var storage = CreateAssemblyStorage(ctx, player.Creature);
                var foods = CreateConfiguredFoods(ctx, storage);
                ctx.Assert(CommitMethod != null, "Production commits expose character and database persistence dependencies");
                Action saveCharacter = () =>
                {
                    nativeRecoverySaves++;
                    recoveryData = GetLocalString(player.Creature, CraftedFoodDelivery.RecoveryVariable);
                    var recovery = JsonConvert.DeserializeObject<CraftedFoodDelivery>(recoveryData);
                    ctx.AssertEqual(4, recovery.Items.Count, "The native recovery snapshot starts with all four configured portions");
                    expectedPayloads = recovery.Items.ToArray();
                    VerifyStoredFoods(ctx, expectedPayloads);
                    VerifyCharacterExports(ctx, player.Creature);
                };
                Action<CraftedFoodDelivery> saveDelivery = delivery =>
                {
                    if (indexOnly)
                        WriteSearchIndexOnly(delivery);
                    else if (afterWrite)
                        DB.Set(delivery);
                    throw new InvalidOperationException("Injected initial food database write interruption");
                };
                deliveryId = (string)CommitMethod.Invoke(null, new object[] { player.Creature, foods, saveCharacter, saveDelivery });
                ctx.AssertEqual(1, nativeRecoverySaves, "The failed initial database save invokes the native recovery save");
                var recovery = JsonConvert.DeserializeObject<CraftedFoodDelivery>(recoveryData);
                ctx.AssertEqual(deliveryId, recovery.Id, "Recovery retains the original batch identity");
                ctx.AssertEqual(player.Id, recovery.PlayerId, "Recovery retains the original character owner");
                ctx.AssertEqual(afterWrite, !string.IsNullOrWhiteSpace(DB.GetRawJson<CraftedFoodDelivery>(deliveryId)),
                    "The injected failure occurs at the intended side of the actual Redis write");
                if (indexOnly)
                    ctx.AssertEqual(1L, DB.SearchCount(new DBQuery<CraftedFoodDelivery>()
                            .AddFieldSearch(nameof(CraftedFoodDelivery.PlayerId), player.Id, false)),
                        "The actual Redis search index contains the interrupted row while JSON is absent");
                ctx.AssertEqual(4, CraftedFoodDeliveryService.GetPendingCount(player.Creature),
                    "A database record and its matching recovery field represent one batch");
            });
            await ctx.WaitFrameAsync();

            // The commit's recovery callback already exported and loaded two BIC generations.
            // Export again after the neutral originals have been destroyed, so persistence is
            // proven independent of those native objects and the finished craft's window.
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                VerifyCharacterExports(ctx, player.Creature);
                ctx.AssertEqual(recoveryData, GetLocalString(player.Creature, CraftedFoodDelivery.RecoveryVariable),
                    "Native save and reload do not reroll or modify the completed recovery payload");
                var promotionWrites = 0;
                Claim(ctx, player.Creature, null, delivery =>
                {
                    promotionWrites++;
                    ctx.AssertEqual(deliveryId, delivery.Id, "Promotion reuses the original stable batch ID");
                    DB.Set(delivery);
                });
                ctx.AssertEqual(afterWrite ? 0 : 1, promotionWrites,
                    "An existing record is reused, while an absent record is promoted exactly once");
                ctx.AssertEqual(string.Empty, GetLocalString(player.Creature, CraftedFoodDelivery.RecoveryVariable),
                    "A confirmed Redis promotion releases the character recovery field");
                ctx.Assert(ReadStoredDelivery(deliveryId).Items.SequenceEqual(expectedPayloads),
                    "Promotion preserves the exact four serialized completed portions");
            });
            await capacity.ExpandToAsync(ctx, 1);
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                Claim(ctx, player.Creature);
                ctx.AssertEqual(3, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "Only one actual native acquisition reduces the recovered batch");
            });
            await ctx.WaitFrameAsync();
            AssertOwnedFoods(ctx, player.Creature, 1);

            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                var reducedPayloads = ReadStoredDelivery(deliveryId).Items.ToArray();
                // A historical character export may still contain the full fallback after
                // Redis has confirmed a partial delivery. Its older payload must never
                // replace that reduced record or manufacture another copy of the award.
                SetLocalString(player.Creature, CraftedFoodDelivery.RecoveryVariable, recoveryData);
                VerifyCharacterExports(ctx, player.Creature);
                ctx.AssertEqual(3, CraftedFoodDeliveryService.GetPendingCount(player.Creature),
                    "An older full recovery field cannot inflate a partially reduced batch");
                Claim(ctx, player.Creature);
                ctx.Assert(ReadStoredDelivery(deliveryId).Items.SequenceEqual(reducedPayloads),
                    "Recovery never overwrites an existing partially claimed record");
                ctx.AssertEqual(3, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "A full native inventory leaves those same three portions pending");
            });
            await ctx.WaitFrameAsync();
            AssertOwnedFoods(ctx, player.Creature, 1);
            await capacity.ExpandToAsync(ctx, 4);
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                Claim(ctx, player.Creature);
                Claim(ctx, player.Creature);
                ctx.AssertEqual(0, CraftedFoodDeliveryService.GetPendingCount(player.Creature), "The recovered batch can be collected exactly once");
                VerifyCharacterExports(ctx, player.Creature);
            });
            await ctx.WaitFrameAsync();
            AssertOwnedFoods(ctx, player.Creature, 4);
        }
        finally
        {
            if (deliveryId != null)
                DB.Delete<CraftedFoodDelivery>(deliveryId);
        }
    }

    private static async Task ClearCarriedInventory(EngineTestContext ctx, uint owner)
    {
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var carried = new List<uint>();
            for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
                carried.Add(item);
            foreach (var item in carried)
                DestroyObject(item);
        });
        await ctx.WaitFrameAsync();
    }

    private static uint CreateAssemblyStorage(EngineTestContext ctx, uint owner)
    {
        var storage = CreateObject(ObjectType.Placeable, "craft_temp_store", GetLocation(owner));
        ctx.Track(storage);
        ctx.Assert(GetIsObjectValid(storage) && GetHasInventory(storage), "Completed food is assembled in neutral storage");
        return storage;
    }

    private static uint[] CreateConfiguredFoods(EngineTestContext ctx, uint storage)
    {
        var foods = new uint[4];
        for (var index = 0; index < foods.Length; index++)
        {
            var food = CreateItemOnObject(FoodResref, storage, 1);
            ctx.Track(food);
            ctx.Assert(GetIsObjectValid(food) && GetItemPossessor(food) == storage, "Each fresh portion belongs to assembly storage");
            SetLocalBool(food, Item.PlayerProducedItemVariable, true);
            SetLocalInt(food, "FOOD_TEST_PORTION", index);
            SetItemCharges(food, 7);
            ItemPlugin.SetAddGoldPieceValue(food, 231);
            Craft.ApplyCraftedItemProperty(food, ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CombatReadiness, 3));
            Craft.ApplyCraftedItemProperty(food, ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CombatReadiness, 5));
            Craft.ApplyCraftedItemProperty(food, ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Accuracy, 9));
            Craft.ApplyCraftedItemProperty(food, ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Duration, 18));
            foods[index] = food;
        }
        ctx.AssertEqual(4, foods.Distinct().Count(), "The four servings are distinct native items");
        return foods;
    }

    private static void VerifyStoredFoods(EngineTestContext ctx, IEnumerable<string> data)
    {
        foreach (var payload in data)
        {
            var restored = ObjectPlugin.Deserialize(payload);
            ctx.Track(restored);
            ctx.Assert(GetIsObjectValid(restored), "The production serialized food loads through the native engine");
            AssertConfiguredFood(ctx, restored);
            DestroyObject(restored);
        }
    }

    private static void AssertConfiguredFood(EngineTestContext ctx, uint food)
    {
        ctx.AssertEqual(FoodResref, GetResRef(food), "The correct completed recipe is retained");
        ctx.AssertEqual(1, GetItemStackSize(food), "Each serving remains independently consumable");
        ctx.AssertEqual(7, GetItemCharges(food), "Blueprint-enhanced charges are retained");
        ctx.AssertEqual(231, ItemPlugin.GetAddGoldPieceValue(food), "Quality-dependent vendor value is retained");
        ctx.Assert(GetLocalBool(food, Item.PlayerProducedItemVariable), "The crafted production marker survives");
        AssertFoodProperty(ctx, food, FoodItemPropertySubType.CombatReadiness, 23);
        AssertFoodProperty(ctx, food, FoodItemPropertySubType.XPBonus, 30);
        AssertFoodProperty(ctx, food, FoodItemPropertySubType.Accuracy, 9);
        AssertFoodProperty(ctx, food, FoodItemPropertySubType.Duration, 18);
        ctx.AssertEqual(5, Properties(food).Length, "Merged replacements do not resurrect stale native item properties");
    }

    private static void AssertFoodProperty(EngineTestContext ctx, uint food, FoodItemPropertySubType subtype, int expected)
    {
        var values = new List<int>();
        for (var property = GetFirstItemProperty(food); GetIsItemPropertyValid(property); property = GetNextItemProperty(food))
            if (GetItemPropertyType(property) == ItemPropertyType.FoodBonus && GetItemPropertySubType(property) == (int)subtype)
                values.Add(GetItemPropertyCostTableValue(property));
        ctx.AssertEqual(1, values.Count, $"Exactly one final {subtype} property is stored");
        ctx.AssertEqual(expected, values.Single(), $"The final {subtype} value includes its enhancements exactly once");
    }

    private static uint[] OwnedFoods(uint owner)
    {
        var result = new List<uint>();
        for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
            if (GetResRef(item) == FoodResref)
                result.Add(item);
        return result.ToArray();
    }

    private static void AssertOwnedFoods(EngineTestContext ctx, uint owner, int expected)
    {
        var foods = OwnedFoods(owner);
        ctx.AssertEqual(expected, foods.Length, "The native inventory retains exactly the acquired portions");
        foreach (var food in foods)
        {
            ctx.AssertEqual(owner, GetItemPossessor(food), "Each retained portion belongs to the expected character");
            AssertConfiguredFood(ctx, food);
        }
        ctx.AssertEqual(expected, foods.Select(food => GetLocalInt(food, "FOOD_TEST_PORTION")).Distinct().Count(),
            "Retries cannot duplicate a completed portion");
        var descriptions = foods.Select(food => string.Join("|", Properties(food))).Distinct().ToArray();
        ctx.Assert(descriptions.Length <= 1, "Every independently delivered portion retains identical completed properties");
    }

    private static void Claim(EngineTestContext ctx, uint player, Action saveCharacter = null,
        Action<CraftedFoodDelivery> saveDelivery = null)
    {
        ctx.Assert(ClaimMethod != null, "Production claims expose character and delivery persistence dependencies");
        ClaimMethod.Invoke(null, new object[]
        {
            player, saveCharacter ?? (() => VerifyCharacterExports(ctx, player)),
            saveDelivery ?? new Action<CraftedFoodDelivery>(DB.Set)
        });
    }

    private static CraftedFoodDelivery ReadStoredDelivery(string id) =>
        JsonConvert.DeserializeObject<CraftedFoodDelivery>(DB.GetRawJson<CraftedFoodDelivery>(id));

    private sealed class InventoryCapacityFixture
    {
        private readonly uint _owner;
        private readonly List<uint> _fillers = new();
        private int _foodCapacity;

        private InventoryCapacityFixture(uint owner) => _owner = owner;

        public static async Task<InventoryCapacityFixture> CreateAsync(EngineTestContext ctx, uint owner, int foodCapacity)
        {
            var fixture = new InventoryCapacityFixture(owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                // Cookies and hotpot both use nonstacking base item 311 with a 1 by 1
                // footprint. Fill the player's actual default repository, including all
                // native PC inventory pages, until another real item no longer fits.
                var storage = CreateAssemblyStorage(ctx, owner);
                var template = CreateItemOnObject("acorn_cookies", storage, 1);
                ctx.Track(template);
                ctx.Assert(GetIsObjectValid(template), "Create the nonstacking 1 by 1 inventory filler blueprint");
                var templateData = ObjectPlugin.Serialize(template);
                var full = false;
                for (var index = 0; index < 1024; index++)
                {
                    var filler = ObjectPlugin.Deserialize(templateData);
                    if (GetIsObjectValid(filler))
                        ctx.Track(filler);
                    ctx.Assert(GetIsObjectValid(filler), "The filler template loads as a real native item");
                    // MoveTo preflights FindPosition with originatingFromScript=0.
                    // AcquireItem's script flag can force an older occupant onto the ground.
                    if (!ItemPlugin.MoveTo(filler, owner, true) || GetItemPossessor(filler) != owner)
                    {
                        if (GetIsObjectValid(filler))
                            DestroyObject(filler);
                        full = true;
                        break;
                    }
                    ctx.AssertEqual(1, GetItemStackSize(filler), "Each inventory filler occupies its own actual slot");
                    fixture._fillers.Add(filler);
                }
                ctx.Assert(full, "The actual native player inventory reaches its finite capacity within 1024 nonstacking items");
                ctx.Assert(fixture._fillers.Count >= 4, "The occupied native inventory contains at least four releasable food slots");
                ctx.Log($"Filled the native player inventory with {fixture._fillers.Count} separate 1 by 1 items.");
            });
            await ctx.WaitFrameAsync();
            ctx.Assert(fixture._fillers.All(item => GetIsObjectValid(item) && GetItemPossessor(item) == owner),
                "Filling the native inventory preserves every existing occupant without displacement");
            await fixture.ExpandToAsync(ctx, foodCapacity);
            return fixture;
        }

        public async Task ExpandToAsync(EngineTestContext ctx, int foodCapacity)
        {
            ctx.Assert(foodCapacity >= _foodCapacity, "A capacity expansion only releases additional inventory slots");
            var releaseCount = foodCapacity - _foodCapacity;
            if (releaseCount == 0)
                return;
            await ctx.ExecuteInCreatureContextAsync(_owner, () =>
            {
                for (var index = 0; index < releaseCount; index++)
                {
                    var filler = _fillers[^1];
                    _fillers.RemoveAt(_fillers.Count - 1);
                    ctx.AssertEqual(_owner, GetItemPossessor(filler), "The slot being released is occupied by this player's filler item");
                    DestroyObject(filler);
                }
            });
            // Native destruction is deferred. A queued DestroyObject cannot create
            // available space until the engine processes the following frame.
            await ctx.WaitFrameAsync();
            _foodCapacity = foodCapacity;
        }
    }

    private static void AssertProgressionUnchanged(EngineTestContext ctx, PlayerAbilityFixture player,
        string expectedPlayer, int expectedCredits, uint blueprint)
    {
        ctx.AssertEqual(expectedPlayer, DB.GetRawJson<Player>(player.Id), "Delivery claims do not grant crafting XP, unlock recipes, or consume resources");
        ctx.AssertEqual(expectedCredits, GetGold(player.Creature), "Delivery claims do not grant credits");
        ctx.AssertEqual(2, Craft.GetBlueprintDetails(blueprint).LicensedRuns, "Delivery claims do not consume another blueprint license");
    }

    private static string[] Properties(uint item)
    {
        var result = new List<string>();
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            result.Add($"{GetItemPropertyType(property)}:{GetItemPropertySubType(property)}:{GetItemPropertyCostTable(property)}:" +
                $"{GetItemPropertyCostTableValue(property)}:{GetItemPropertyParam1(property)}:{GetItemPropertyParam1Value(property)}");
        return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static void VerifyCharacterExports(EngineTestContext ctx, uint owner)
    {
        // Headless fixtures cannot authenticate a servervault write. This dependency executes
        // its native BIC save/load pipeline, including the food fields and native inventory.
        var source = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner).AsNWSCreature();
        using var first = new CNWSCreature(OBJECT_INVALID, 0, 1);
        ExportAndLoad(ctx, source, first);
        using var second = new CNWSCreature(OBJECT_INVALID, 0, 1);
        ExportAndLoad(ctx, first, second);
    }

    private static void ExportAndLoad(EngineTestContext ctx, CNWSCreature source, CNWSCreature destination)
    {
        var originalFoods = OwnedFoods(source.m_idSelf).Select(FoodState).OrderBy(value => value).ToArray();
        var originalCredits = GetGold(source.m_idSelf);
        using var receiptName = new CExoString(CraftedFoodClaim.CheckpointVariable);
        using var originalReceipt = source.m_ScriptVars.GetString(receiptName);
        using var recoveryName = new CExoString(CraftedFoodDelivery.RecoveryVariable);
        using var originalRecovery = source.m_ScriptVars.GetString(recoveryName);
        using var file = new CResGFF();
        using var root = new CResStruct();
        using var type = new CExoString("BIC ");
        using var format = new CExoString("V2.0");
        ctx.Assert(file.CreateGFFFile(root, type, format) != 0, "Create the native character export");
        // Match CNWSPlayer.SaveServerCharacter: retain item locals and saved object IDs.
        ctx.Assert(source.SaveCreature(file, root, 0, 0, 0, 1) != 0, "The production native character save succeeds");
        ctx.Assert(destination.LoadCreature(file, root, 0, 0, 0, 0) != 0, "The native character export reloads");
        using var restoredReceipt = destination.m_ScriptVars.GetString(receiptName);
        ctx.AssertEqual(originalReceipt.ToString(), restoredReceipt.ToString(), "The food receipt persists with the awarded inventory");
        using var restoredRecovery = destination.m_ScriptVars.GetString(recoveryName);
        ctx.AssertEqual(originalRecovery.ToString(), restoredRecovery.ToString(), "The full configured recovery batch persists in the native character export");
        ctx.AssertEqual(originalCredits, GetGold(destination.m_idSelf), "The native checkpoint retains the character's credits");
        var restoredFoods = OwnedFoods(destination.m_idSelf);
        var restoredStates = restoredFoods.Select(FoodState).OrderBy(value => value).ToArray();
        ctx.Assert(originalFoods.SequenceEqual(restoredStates),
            "Repeated native save/reload retains each portion's identity, properties, charges, and quantity. " +
            $"Source: [{string.Join("; ", originalFoods)}]. Restored: [{string.Join("; ", restoredStates)}]");
        AssertOwnedFoods(ctx, destination.m_idSelf, originalFoods.Length);
    }

    private static string FoodState(uint food) =>
        $"{GetLocalInt(food, "FOOD_TEST_PORTION")}:{GetItemStackSize(food)}:{GetItemCharges(food)}:" +
        $"{ItemPlugin.GetAddGoldPieceValue(food)}:{GetLocalBool(food, Item.PlayerProducedItemVariable)}:{string.Join("|", Properties(food))}";

    private static CraftViewModel BindCraftModel(uint player, uint blueprint)
    {
        var model = new CraftViewModel();
        typeof(GuiViewModelBase<CraftViewModel, CraftPayload>).GetProperty("Player", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(model, player);
        CallModel(model, "Initialize", new CraftPayload(RecipeType.EchaniSharedHotpot, blueprint));
        return model;
    }

    private static object ModelField(CraftViewModel model, string field) =>
        typeof(CraftViewModel).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);

    private static void SetModelField(CraftViewModel model, string field, object value) =>
        typeof(CraftViewModel).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, value);

    private static void CallModel(CraftViewModel model, string method, params object[] arguments) =>
        typeof(CraftViewModel).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(model, arguments);

    private static void WriteSearchIndexOnly(CraftedFoodDelivery delivery)
    {
        // DB.Set writes this real search document before its JSON. Stop at that exact
        // persistence boundary to exercise the orphaned result returned by Redis.
        var clients = (Dictionary<Type, Client>)typeof(DB)
            .GetField("_searchClientsByType", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var fields = (Dictionary<string, RedisValue>)typeof(DB)
            .GetMethod("BuildIndexData", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { typeof(CraftedFoodDelivery), delivery });
        clients[typeof(CraftedFoodDelivery)].ReplaceDocument(
            SearchIndexDefinition.KeyPrefix(nameof(CraftedFoodDelivery)) + delivery.Id, fields);
    }
}
