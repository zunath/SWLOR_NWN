using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Tests.Feature;

// Mutation outcomes are chosen from every mutation of the parent whose recipe the job meets. A recipe that a
// job can meet together with another of the same parent's recipes can replace that parent's real mutation.
public class BeastMutationRecipeTests
{
    private enum EnzymeKind
    {
        Lyase,
        Isomerase,
        Hydrolase,
    }

    private sealed class Recipe
    {
        public Dictionary<(EnzymeKind Kind, EnzymeColorType Color), int> Enzymes { get; } = new();
        public HashSet<DayOfWeek> Days { get; } = Enum.GetValues<DayOfWeek>().ToHashSet();
    }

    // One lyase, isomerase, and hydrolase can be added per stage, so a job holds at most this many of each.
    private static int GetStagesPerJob()
    {
        var stagesField = typeof(IncubatorViewModel).GetField("NumberOfStages", BindingFlags.NonPublic | BindingFlags.Static);
        stagesField.Should().NotBeNull("the overlap checks need the number of stages a job runs");
        return (int)stagesField!.GetRawConstantValue()!;
    }

    private static Recipe BuildRecipe(MutationDetail mutation)
    {
        var recipe = new Recipe();

        foreach (var requirement in mutation.Requirements)
        {
            switch (requirement)
            {
                case MutationRequirementEnzyme enzyme:
                    AddEnzymes(recipe, EnzymeKind.Lyase, enzyme.LyaseEnzymeColors);
                    AddEnzymes(recipe, EnzymeKind.Isomerase, enzyme.IsomeraseEnzymeColors);
                    AddEnzymes(recipe, EnzymeKind.Hydrolase, enzyme.HydrolaseEnzymeColors);
                    break;
                case MutationRequirementDayOfWeek:
                    var days = (List<DayOfWeek>)typeof(MutationRequirementDayOfWeek)
                        .GetField("_daysOfWeek", BindingFlags.NonPublic | BindingFlags.Instance)!
                        .GetValue(requirement)!;
                    recipe.Days.IntersectWith(days);
                    break;
                default:
                    Assert.Fail($"{requirement.GetType().Name} is not covered by the recipe overlap checks.");
                    break;
            }
        }

        return recipe;
    }

    private static void AddEnzymes(Recipe recipe, EnzymeKind kind, Dictionary<EnzymeColorType, int> colors)
    {
        foreach (var (color, count) in colors)
        {
            recipe.Enzymes[(kind, color)] = Math.Max(recipe.Enzymes.GetValueOrDefault((kind, color)), count);
        }
    }

    // Requirements are minimums, so the smallest job meeting both recipes holds the larger count of each color.
    private static bool CanOneJobMeetBoth(Recipe first, Recipe second, int stagesPerJob)
    {
        if (!first.Days.Overlaps(second.Days))
            return false;

        foreach (var kind in Enum.GetValues<EnzymeKind>())
        {
            var colors = first.Enzymes.Keys.Concat(second.Enzymes.Keys)
                .Where(key => key.Kind == kind)
                .Select(key => key.Color)
                .Distinct();

            var needed = colors.Sum(color => Math.Max(
                first.Enzymes.GetValueOrDefault((kind, color)),
                second.Enzymes.GetValueOrDefault((kind, color))));

            if (needed > stagesPerJob)
                return false;
        }

        return true;
    }

    private static bool CanOneJobMeet(Recipe recipe, int stagesPerJob)
    {
        return CanOneJobMeetBoth(recipe, new Recipe(), stagesPerJob);
    }

    // The failure beasts: dead ends a player picks on purpose with a recipe of their own.
    private static readonly BeastType[] BadOutcomes =
    {
        BeastType.MustardlashSlime,
        BeastType.OchreMaw,
        BeastType.GraymireAmalgam,
        BeastType.VoidmireEcho,
        BeastType.CrystalflowSkimmer,
        BeastType.GreenbulkWallow,
        BeastType.HouseCat,
    };

    private static IEnumerable<(BeastType Parent, MutationDetail Bad)> BadOutcomeMutations()
    {
        foreach (var (parent, detail) in BeastDefinitionCatalog.All)
        {
            foreach (var mutation in detail.PossibleMutations.Where(m => BadOutcomes.Contains(m.Type)))
            {
                yield return (parent, mutation);
            }
        }
    }

    [Test]
    public void BadOutcomes_AreReachableFromTwoOrMoreParents()
    {
        foreach (var bad in BadOutcomes)
        {
            BadOutcomeMutations().Count(pair => pair.Bad.Type == bad).Should().BeGreaterThanOrEqualTo(2, $"{bad} is a dead end players choose on purpose");
            BeastDefinitionCatalog.All[bad].PossibleMutations.Should().BeEmpty($"{bad} is a dead end");
        }
    }

    [Test]
    public void BadOutcomeRecipes_NeverShareAJobWithAnotherMutationOfTheParent()
    {
        var stages = GetStagesPerJob();

        foreach (var (parent, bad) in BadOutcomeMutations())
        {
            var badRecipe = BuildRecipe(bad);

            foreach (var other in BeastDefinitionCatalog.All[parent].PossibleMutations.Where(m => !ReferenceEquals(m, bad)))
            {
                CanOneJobMeetBoth(badRecipe, BuildRecipe(other), stages).Should().BeFalse(
                    $"a job that makes {other.Type} from {parent} must never also be able to make {bad.Type}");
            }
        }
    }

    [Test]
    public void BadOutcomeRecipes_AreDeliberateAndReachable()
    {
        var stages = GetStagesPerJob();

        foreach (var (parent, bad) in BadOutcomeMutations())
        {
            bad.Requirements.Should().NotBeEmpty($"{parent}'s {bad.Type} needs its own recipe");

            var recipe = BuildRecipe(bad);
            recipe.Enzymes.Should().NotBeEmpty($"{parent}'s {bad.Type} recipe needs enzymes, not only a day");
            CanOneJobMeet(recipe, stages).Should().BeTrue($"{parent}'s {bad.Type} recipe must fit in one job");
        }
    }

    [Test]
    public void EveryMutationRecipe_UsesEnzymesThatExist()
    {
        var root = FindRepositoryRoot();

        foreach (var (parent, detail) in BeastDefinitionCatalog.All)
        {
            foreach (var mutation in detail.PossibleMutations)
            {
                foreach (var (kind, color) in BuildRecipe(mutation).Enzymes.Keys)
                {
                    var resref = $"{kind}_{color}".ToLowerInvariant();
                    File.Exists(Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json")).Should().BeTrue(
                        $"{parent} -> {mutation.Type} needs a {color} {kind}, which has no item");
                }
            }
        }
    }

    [Test]
    public void OverlapCheck_FlagsRecipesOneJobCanMeetTogether()
    {
        var blue = new Recipe();
        blue.Enzymes[(EnzymeKind.Lyase, EnzymeColorType.Blue)] = 2;

        var alsoBlue = new Recipe();
        alsoBlue.Enzymes[(EnzymeKind.Lyase, EnzymeColorType.Blue)] = 3;

        var red = new Recipe();
        red.Enzymes[(EnzymeKind.Lyase, EnzymeColorType.Red)] = 2;

        var isomerase = new Recipe();
        isomerase.Enzymes[(EnzymeKind.Isomerase, EnzymeColorType.Red)] = 3;

        var monday = new Recipe();
        monday.Days.IntersectWith(new[] { DayOfWeek.Monday });

        var saturday = new Recipe();
        saturday.Days.IntersectWith(new[] { DayOfWeek.Saturday });

        CanOneJobMeetBoth(blue, alsoBlue, 3).Should().BeTrue("the larger count covers both");
        CanOneJobMeetBoth(blue, red, 3).Should().BeFalse("2 blue and 2 red lyase need 4 lyase slots");
        CanOneJobMeetBoth(blue, isomerase, 3).Should().BeTrue("different enzyme types do not compete");
        CanOneJobMeetBoth(monday, saturday, 3).Should().BeFalse("no day is both Monday and Saturday");
        CanOneJobMeetBoth(monday, blue, 3).Should().BeTrue("a recipe with no day applies on Monday too");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
