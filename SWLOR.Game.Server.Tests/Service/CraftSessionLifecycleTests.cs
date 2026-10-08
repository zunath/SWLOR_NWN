using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftSessionLifecycleTests
{
    private static CraftSession Session() => CraftSession.CreateLegacy(50, 50, new RecipeLevelDetail(186, 2641, 80, 0), 29, 30, 37);

    [Test]
    public void ReplayedAndWrongSessionRequests_AreRejectedWithoutSpendingOrRolling()
    {
        var state = Session();
        var request = new CraftActionRequest(state.Id, 0, CraftActionType.BasicTouch);
        state = CraftActionEvaluator.Resolve(state, request, () => 1).Session;
        foreach (var stale in new[] { request, request with { SessionId = Guid.NewGuid(), ExpectedActionCount = 1 } })
        {
            var result = CraftActionEvaluator.Resolve(state, stale,
                () => throw new AssertionException("Stale requests must not roll."));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Session, Is.SameAs(state));
        }
    }

    [TestCase(CraftSessionStatus.Succeeded)]
    [TestCase(CraftSessionStatus.Failed)]
    [TestCase(CraftSessionStatus.Aborted)]
    public void TerminalSessions_RejectEveryActionAndCanOnlyBeSettledOnce(CraftSessionStatus status)
    {
        var state = Session() with { Status = status };
        foreach (var action in CraftActionDetail.LegacyActions)
        {
            var result = CraftActionEvaluator.Resolve(state, new CraftActionRequest(state.Id, 0, action.Type),
                () => throw new AssertionException("Terminal sessions must not roll."));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Session, Is.SameAs(state));
        }
        var settlement = new CraftSessionSettlement();
        Assert.That(settlement.TryClaim(state), Is.True);
        Assert.That(settlement.TryClaim(state), Is.False);
        Assert.That(settlement.TryClaim(Session() with { Status = status }), Is.False);
        Assert.That(new CraftSessionSettlement().TryClaim(Session() with { Status = status }), Is.True);
    }

    [Test]
    public void ActiveSession_CannotGrantRewardsOrSettleFailure()
    {
        var settlement = new CraftSessionSettlement();
        Assert.That(settlement.TryClaim(Session()), Is.False);
        Assert.That(settlement.IsClaimed, Is.False);
    }

    [Test]
    public void MissingMaterials_ReserveNothingAndDoNotMutateInventoryOrRecipe()
    {
        var recipe = new Dictionary<string, int> { ["metal"] = 3, ["wood"] = 2 };
        var inventory = new[] { new CraftComponentStack(1, "metal", 10), new CraftComponentStack(2, "wood", 1) };
        Assert.That(CraftComponentBudget.Plan(recipe, inventory), Is.Empty);
        Assert.That(inventory[0].Quantity, Is.EqualTo(10));
        Assert.That(inventory[1].Quantity, Is.EqualTo(1));
        Assert.That(recipe["metal"], Is.EqualTo(3));
        Assert.That(recipe["wood"], Is.EqualTo(2));
    }

    [Test]
    public void CompleteMaterialBudget_ReservesExactQuantitiesAcrossStacks()
    {
        var recipe = new Dictionary<string, int> { ["metal"] = 7, ["wood"] = 2 };
        var inventory = new[]
        {
            new CraftComponentStack(1, "metal", 10), new CraftComponentStack(2, "metal", 3),
            new CraftComponentStack(3, "wood", 2), new CraftComponentStack(4, "unrelated", 99)
        };
        var plan = CraftComponentBudget.Plan(recipe, inventory);
        Assert.That(plan, Is.EqualTo(new[]
        {
            new CraftComponentReservation(3, 2), new CraftComponentReservation(2, 3), new CraftComponentReservation(1, 4)
        }));
        Assert.That(inventory[0].Quantity, Is.EqualTo(10));
    }

    [Test]
    public void CraftWindow_BuildsWithNoLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        Assert.That(new CraftDefinition().BuildWindow().LayoutFindings, Is.Empty);
    }
}
