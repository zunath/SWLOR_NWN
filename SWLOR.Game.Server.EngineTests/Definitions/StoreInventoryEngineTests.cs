using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class StoreInventoryEngineTests
{
    [EngineTest("Legacy Hunter purchase requirements repair without losing custom properties", Category = "StoreInventory")]
    public static async Task LegacyHunterPurchaseRequirements(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        uint rifle = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            rifle = CreateItemOnObject("h_rifle_1", owner);
            ctx.Track(rifle);
            var oldRequirements = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(rifle); GetIsItemPropertyValid(property); property = GetNextItemProperty(rifle))
                if (GetItemPropertyType(property) == ItemPropertyType.RequiresSkill) oldRequirements.Add(property);
            var removeProperty = typeof(EquipmentRequirementCompatibility).Assembly
                .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.MigrationObject")!
                .GetMethod("RemoveProperty", BindingFlags.Static | BindingFlags.Public)!;
            foreach (var property in oldRequirements) removeProperty.Invoke(null, new object[] { rifle, property });
            // Legacy weapon requirement 6 now names Beacon Targeting. This is
            // the reported unrelated-perk gate, and its row still exists so
            // the engine retains it through item-property updates.
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, 6, 1), rifle);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 13), rifle);
            SetName(rifle, "Custom Hunter Rifle");
            SetLocalString(rifle, "STORE_REQUIREMENT_FIXTURE", "preserve");
        });
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.AssertEqual(1, ReadPropertyValue(rifle, ItemPropertyType.UseLimitationPerk), "The fixture retains the old store requirement");
            ctx.Assert(GetHasInventory(owner), "The login fixture exposes its carried inventory");
            var uuid = GetObjectUUID(rifle);
            var damage = ReadPropertyValue(rifle, ItemPropertyType.DMG);
            var accuracy = ReadPropertyTotal(rifle, ItemPropertyType.Accuracy);
            ctx.AssertEqual(15, accuracy, "The fixture contains both its base accuracy and installed bonus");
            ctx.Assert(EquipmentRequirementCompatibility.Normalize(owner), "Login repair finds the previously purchased legacy rifle");
            AssertRequirements(ctx, rifle, 0);
            ctx.AssertEqual(accuracy, ReadPropertyTotal(rifle, ItemPropertyType.Accuracy), "The base accuracy and installed bonus survive repair");
            ctx.AssertEqual(damage, ReadPropertyValue(rifle, ItemPropertyType.DMG), "Existing weapon damage survives repair");
            ctx.AssertEqual("Custom Hunter Rifle", GetName(rifle), "The custom name survives repair");
            ctx.AssertEqual("preserve", GetLocalString(rifle, "STORE_REQUIREMENT_FIXTURE"), "Unrelated local data survives repair");
            ctx.AssertEqual(uuid, GetObjectUUID(rifle), "The item keeps its identity");
            ctx.Assert(!EquipmentRequirementCompatibility.Normalize(rifle), "Repeated acquisition repair is idempotent");
        });
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(owner, () => AssertRequirements(ctx, rifle, 0));
    }

    [EngineTest("Placed Hunter Guild rifles and acquired copies use Rifle skill", Category = "StoreInventory")]
    public static async Task HunterStoreRequirements(EngineTestContext ctx)
    {
        var buyer = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var storesByTier = new Dictionary<int, int>();
        var copies = new List<(uint Item, int Rank)>();

        await ctx.ExecuteInCreatureContextAsync(buyer, () =>
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            for (var store = GetFirstObjectInArea(area); GetIsObjectValid(store); store = GetNextObjectInArea(area))
            {
                if (GetObjectType(store) != ObjectType.Store)
                    continue;
                var resref = GetResRef(store);
                if (!resref.StartsWith("gp_hunt_") || !int.TryParse(resref[8..], out var tier) || tier < 1 || tier > 5)
                    continue;

                uint rifle = OBJECT_INVALID;
                for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
                    if (GetResRef(item) == $"h_rifle_{tier}") rifle = item;

                ctx.Assert(GetIsObjectValid(rifle), $"{resref} contains its Hunter Rifle");
                var requiredRank = (tier - 1) * 10;
                AssertRequirements(ctx, rifle, requiredRank);
                var copy = CopyItem(rifle, buyer, true);
                ctx.Track(copy);
                ctx.Assert(GetIsObjectValid(copy), "The store rifle can be acquired as an item copy");
                copies.Add((copy, requiredRank));
                storesByTier[tier] = storesByTier.GetValueOrDefault(tier) + 1;
            }
        });

        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(buyer, () =>
        {
            for (var tier = 1; tier <= 5; tier++)
                ctx.Assert(storesByTier.GetValueOrDefault(tier) > 0, $"Placed rank {tier} Hunter stores were checked");
            foreach (var (item, rank) in copies)
            {
                ctx.AssertEqual(buyer, GetItemPossessor(item), "The acquired rifle is in the buyer inventory");
                AssertRequirements(ctx, item, rank);
            }
            ctx.Log($"Verified {copies.Count} placed Hunter stores and their acquired rifle copies across all five ranks.");
        });
    }

    private static void AssertRequirements(EngineTestContext ctx, uint item, int expectedRank)
    {
        var requirements = 0;
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
        {
            var type = GetItemPropertyType(property);
            ctx.Assert(type != ItemPropertyType.UseLimitationPerk, "The rifle does not require a retired or unrelated perk");
            if (type != ItemPropertyType.RequiresSkill)
                continue;
            requirements++;
            ctx.AssertEqual((int)SkillType.Rifle, GetItemPropertySubType(property), "The rifle requires the current Rifle skill");
            ctx.AssertEqual(expectedRank, GetItemPropertyCostTableValue(property), "The rifle requires the correct skill rank");
        }
        ctx.AssertEqual(1, requirements, "The rifle has exactly one skill requirement");
    }

    private static int ReadPropertyValue(uint item, ItemPropertyType type)
    {
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            if (GetItemPropertyType(property) == type) return GetItemPropertyCostTableValue(property);
        return -1;
    }

    private static int ReadPropertyTotal(uint item, ItemPropertyType type)
    {
        var total = 0;
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            if (GetItemPropertyType(property) == type) total += GetItemPropertyCostTableValue(property);
        return total;
    }
}
