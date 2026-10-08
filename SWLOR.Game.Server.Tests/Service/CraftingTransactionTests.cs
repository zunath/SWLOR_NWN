using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.StatService;
using System.Reflection;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftingTransactionTests
{
    [TestCase(10, true)] [TestCase(7, false)]
    public void PartialStackRecovery_DebitsOnlyAnUntouchedStack(int current, bool expected)
    {
        var debit = new CraftingDebit("item", "material", 10, 3);
        Assert.That(debit.NeedsDebit(current), Is.EqualTo(expected));
    }
    [Test]
    public void FullyConsumedStackRecovery_RecognizesRemovalOrExportedConsumptionMarker()
    {
        var debit = new CraftingDebit("item", "material", 3, 3);
        Assert.That(debit.NeedsDebit(null), Is.False);
        Assert.That(debit.NeedsDebit(3, true), Is.False);
        Assert.That(debit.NeedsDebit(3), Is.True);
    }
    [TestCase(null)] [TestCase(8)] [TestCase(12)]
    public void Recovery_RejectsUnexpectedStackChangesWithoutGuessing(int? quantity)
    {
        var debit = new CraftingDebit("item", "material", 10, 3);
        Assert.Throws<InvalidOperationException>(() => debit.NeedsDebit(quantity));
    }
    [Test]
    public void PersistentReceipt_RoundTripsCommittedDeckStatsBuffsAndRolledDeliveries()
    {
        var state = CraftSession.Create(50, 50, new(10000, 10000, 80, 0), 30, 29, 37, CraftProfile.Delicate,
            CraftTechnique.PoisonMixing, new Dictionary<StatType,int> { [StatType.CraftingForecastLength] = 3 }, _ => 0);
        state = CraftActionEvaluator.Resolve(state, new(state.Id, 0, CraftActionType.MuscleMemory), () => 1).Session;
        var receipt = new CraftingTransaction { Id = state.Id, Recipe = RecipeType.VenomCoating1, Session = state,
            Phase = CraftingTransactionPhase.RewardsReady, XP = 100, Debits = new() { new("item", "herb_v", 2, 2) },
            Deliveries = new() { new() { Id = "output", Data = "already-rolled-item", Delivered = false } } };
        var restored = JsonConvert.DeserializeObject<CraftingTransaction>(JsonConvert.SerializeObject(receipt));
        Assert.Multiple(() =>
        {
            Assert.That(restored.Id, Is.EqualTo(state.Id)); Assert.That(restored.Session.Id, Is.EqualTo(state.Id));
            Assert.That(restored.Session.Conditions, Is.EqualTo(state.Conditions));
            Assert.That(restored.Session.ConditionIndex, Is.EqualTo(1));
            Assert.That(restored.Session.Buff(CraftBuffType.MuscleMemory), Is.EqualTo(state.Buff(CraftBuffType.MuscleMemory)));
            Assert.That(restored.Session.CraftingStat(StatType.CraftingForecastLength), Is.EqualTo(3));
            Assert.That(restored.Deliveries[0].Data, Is.EqualTo("already-rolled-item")); Assert.That(restored.XP, Is.EqualTo(100));
        });
    }
    [Test]
    public void RewardSnapshot_SurvivesDefinitionChangesAndPersistence()
    {
        var recipe = new RecipeDetail { Resref = "original", Quantity = 5, Skill = SkillType.Espionage, Level = 3,
            EnhancementType = RecipeEnhancementType.None, Category = RecipeCategoryType.Poison };
        var snapshot = CraftRecipeRewards.Capture(recipe, 5);
        recipe.Resref = "changed"; recipe.Quantity = 1; recipe.Skill = SkillType.Engineering;
        var restored = JsonConvert.DeserializeObject<CraftRecipeRewards>(JsonConvert.SerializeObject(snapshot));
        Assert.That(restored.ToRecipe().Resref, Is.EqualTo("original"));
        Assert.That(restored.Quantity, Is.EqualTo(5)); Assert.That(restored.Skill, Is.EqualTo(SkillType.Espionage));
        Assert.That(restored.BaseXP, Is.EqualTo(snapshot.BaseXP));
    }

    [Test]
    public void ActualClientEventArguments_RejectAnOldRevisionBeforeEngineCalls()
    {
        var state = CraftSession.CreateLegacy(50, 50, new(10000,10000,80,0), 30,29,37) with { ActionCount = 2 };
        var view = new CraftViewModel();
        typeof(CraftViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, state);
        view.IsInCraftMode = true;
        view.OnCraftAction(state.Id.ToString("N"), 1, (int)CraftActionType.BasicTouch).Invoke();
        Assert.That(typeof(CraftViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view), Is.SameAs(state));
        Assert.That(view.StatusText, Does.Contain("earlier crafting state"));
    }
}
