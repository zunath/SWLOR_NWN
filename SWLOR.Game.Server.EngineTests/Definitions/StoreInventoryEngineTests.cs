using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class StoreInventoryEngineTests
{
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
}
