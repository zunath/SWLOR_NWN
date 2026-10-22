using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class BaggedSupplyRequirementEngineTests
    {
        [EngineTest("Emergency Triage counts single medical supplies split across bags", Category = "PlayerHealing", TimeoutSeconds = 40f)]
        public static async Task SingleSuppliesSplitAcrossBags(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var requirement = GetEmergencyTriageSupplyRequirement(ctx);

            var first = await CreateBaggedSupplyAsync(ctx, player, 1);
            var second = await CreateBaggedSupplyAsync(ctx, player, 1);

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.AssertEqual(string.Empty, requirement.CheckRequirements(player), "two single supplies in two bags pay the cost of 2");
                requirement.AfterActivationAction(player);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(first) && !GetIsObjectValid(second), 3f,
                "both bagged single supplies to be consumed");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.AssertEqual("Requires 2 Medical Supplies.", requirement.CheckRequirements(player), "no supplies remain after paying the cost"));
        }

        [EngineTest("Emergency Triage counts a single bagged supply alongside a larger bagged stack", Category = "PlayerHealing", TimeoutSeconds = 40f)]
        public static async Task SingleSupplyBesideBaggedStack(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var requirement = GetEmergencyTriageSupplyRequirement(ctx);

            var single = await CreateBaggedSupplyAsync(ctx, player, 1);
            var stack = await CreateBaggedSupplyAsync(ctx, player, 5);

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.AssertEqual(string.Empty, requirement.CheckRequirements(player), "a single bagged supply does not hide the other bag's stack");
                requirement.AfterActivationAction(player);
            });
            await ctx.DelaySecondsAsync(0.5f);
            var remaining = (GetIsObjectValid(single) ? GetItemStackSize(single) : 0) +
                            (GetIsObjectValid(stack) ? GetItemStackSize(stack) : 0);
            ctx.AssertEqual(4, remaining, "exactly two supplies are consumed across the bags");
        }

        private static AbilityRequirementItem GetEmergencyTriageSupplyRequirement(EngineTestContext ctx)
        {
            var requirement = Ability.GetAbilityDetail(FeatType.EmergencyTriage1).Requirements
                .OfType<AbilityRequirementItem>()
                .SingleOrDefault(r => r.ItemResref == "med_supplies");
            ctx.Assert(requirement != null, "Emergency Triage requires medical supplies");
            ctx.AssertEqual(2, requirement!.Quantity, "Emergency Triage supply cost");
            return requirement;
        }

        private static async Task<uint> CreateBaggedSupplyAsync(EngineTestContext ctx, uint player, int stackSize)
        {
            uint bag = OBJECT_INVALID;
            uint supply = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                bag = CreateItemOnObject("bag_sm", player);
                supply = CreateItemOnObject("med_supplies_50", bag);
                SetItemStackSize(supply, stackSize);
            });
            ctx.Assert(GetIsObjectValid(bag), "the bag exists");
            ctx.Assert(GetIsObjectValid(supply), "the bagged supply exists");
            // GetItemPossessor reports the player for bagged items, so look inside the bag instead.
            await ctx.WaitUntilAsync(() => GetItemPossessor(bag) == player && BagContains(bag, supply), 5f,
                "the supply to sit inside a bag in the player's inventory");
            ctx.AssertEqual(stackSize, GetItemStackSize(supply), "bagged supply stack size");
            return supply;
        }

        private static bool BagContains(uint bag, uint item)
        {
            for (var child = GetFirstItemInInventory(bag); GetIsObjectValid(child); child = GetNextItemInInventory(bag))
            {
                if (child == item)
                    return true;
            }

            return false;
        }
    }
}
