using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class RecipeOutputFactoryTests
{
    [Test]
    public void SharedHotpot_CreatesFourDistinctPortionsDespiteNativeStackLimitOfOne()
    {
        var recipe = new EchaniCookingRecipes().BuildRecipes()[RecipeType.EchaniSharedHotpot];
        var requestedStackSizes = new List<int>();
        var nativeStacks = new Dictionary<uint, int>();

        var outputs = RecipeOutputFactory.Create(recipe, stackSize =>
        {
            requestedStackSizes.Add(stackSize);
            var item = (uint)nativeStacks.Count + 1;
            nativeStacks.Add(item, Math.Clamp(stackSize, 1, 1));
            return item;
        });

        Assert.That(recipe.Quantity, Is.EqualTo(4));
        Assert.That(outputs, Has.Count.EqualTo(4));
        Assert.That(outputs.Distinct().Count(), Is.EqualTo(4));
        Assert.That(requestedStackSizes, Is.EqualTo(new[] { 1, 1, 1, 1 }));
        Assert.That(outputs.Sum(item => nativeStacks[item]), Is.EqualTo(4));
    }

    [Test]
    public void SinglePortionFood_KeepsOneNativeCreation()
    {
        var recipe = new EchaniCookingRecipes().BuildRecipes()[RecipeType.EchaniSteamedDumplings];
        var requestedStackSizes = new List<int>();

        var outputs = RecipeOutputFactory.Create(recipe, stackSize =>
        {
            requestedStackSizes.Add(stackSize);
            return 1;
        });

        Assert.That(recipe.Quantity, Is.EqualTo(1));
        Assert.That(outputs, Is.EqualTo(new uint[] { 1 }));
        Assert.That(requestedStackSizes, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void SharedHotpot_WhenOnlyThreePortionsFit_RollsBackTheEntireBatchAndCanRetry()
    {
        var recipe = new EchaniCookingRecipes().BuildRecipes()[RecipeType.EchaniSharedHotpot];
        var nativeStacks = new Dictionary<uint, int>();
        var destroyedItems = new List<uint>();
        var availableSlots = 3;
        uint nextItem = 1;

        uint CreateNativeItem(int stackSize)
        {
            if (nativeStacks.Count >= availableSlots)
                return 0;

            var item = nextItem++;
            nativeStacks.Add(item, Math.Clamp(stackSize, 1, 1));
            return item;
        }

        void DestroyNativeItem(uint item)
        {
            destroyedItems.Add(item);
            nativeStacks.Remove(item);
        }

        var succeeded = RecipeOutputFactory.TryCreate(recipe, CreateNativeItem,
            nativeStacks.ContainsKey, DestroyNativeItem, out var outputs);

        Assert.That(succeeded, Is.False);
        Assert.That(outputs, Is.Empty);
        Assert.That(destroyedItems, Is.EqualTo(new uint[] { 1, 2, 3 }));
        Assert.That(nativeStacks, Is.Empty);

        availableSlots = 4;
        succeeded = RecipeOutputFactory.TryCreate(recipe, CreateNativeItem,
            nativeStacks.ContainsKey, DestroyNativeItem, out outputs);

        Assert.That(succeeded, Is.True);
        Assert.That(outputs, Has.Count.EqualTo(4));
        Assert.That(outputs.Distinct().Count(), Is.EqualTo(4));
        Assert.That(outputs.Sum(item => nativeStacks[item]), Is.EqualTo(4));
        Assert.That(destroyedItems, Has.Count.EqualTo(3));
    }

    [Test]
    public void StarshipMissiles_KeepOneNativeStackWithTheEntireRecipeQuantity()
    {
        var recipe = new StarshipAmmoRecipes().BuildRecipes()[RecipeType.Missile15];
        var requestedStackSizes = new List<int>();
        var nativeStacks = new Dictionary<uint, int>();

        var outputs = RecipeOutputFactory.Create(recipe, stackSize =>
        {
            requestedStackSizes.Add(stackSize);
            var item = (uint)nativeStacks.Count + 1;
            nativeStacks.Add(item, Math.Clamp(stackSize, 1, 99));
            return item;
        });

        Assert.That(recipe.Quantity, Is.EqualTo(15));
        Assert.That(outputs, Has.Count.EqualTo(1));
        Assert.That(requestedStackSizes, Is.EqualTo(new[] { recipe.Quantity }));
        Assert.That(outputs.Sum(item => nativeStacks[item]), Is.EqualTo(recipe.Quantity));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void RecipeBuilder_NormalizesNonPositiveFoodQuantitiesToOnePortion(int quantity)
    {
        var recipe = new RecipeBuilder()
            .Create(RecipeType.EchaniSharedHotpot, SkillType.Agriculture)
            .Category(RecipeCategoryType.Food)
            .Quantity(quantity)
            .Build()[RecipeType.EchaniSharedHotpot];
        var requestedStackSizes = new List<int>();

        var outputs = RecipeOutputFactory.Create(recipe, stackSize =>
        {
            requestedStackSizes.Add(stackSize);
            return 1;
        });

        Assert.That(recipe.Quantity, Is.EqualTo(1));
        Assert.That(outputs, Is.EqualTo(new uint[] { 1 }));
        Assert.That(requestedStackSizes, Is.EqualTo(new[] { 1 }));
    }
}
