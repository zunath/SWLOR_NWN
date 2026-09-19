using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

[NonParallelizable]
public class CharacterRebuildSkillAvailabilityTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private Dictionary<SkillType, SkillAttribute> _cache;
    private Dictionary<SkillType, SkillAttribute> _original;

    /// <summary>Populates the skill cache from production metadata without starting the NWN engine.</summary>
    [SetUp]
    public void SetUp()
    {
        _cache = (Dictionary<SkillType, SkillAttribute>)typeof(Skill)
            .GetField("_activeSkillsContributingToCap", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        _original = new(_cache);
        _cache.Clear();
        foreach (var type in Enum.GetValues<SkillType>())
        {
            var detail = type.GetAttribute<SkillType, SkillAttribute>();
            if (detail.IsActive && detail.ContributesToSkillCap)
                _cache.Add(type, detail);
        }
    }

    /// <summary>Restores the shared cache so this fixture cannot affect other tests.</summary>
    [TearDown]
    public void TearDown()
    {
        _cache.Clear();
        foreach (var entry in _original)
            _cache.Add(entry.Key, entry.Value);
    }

    /// <summary>Checks every initial skill row and tooltip against the selected character type.</summary>
    [TestCase(0, CharacterType.Standard)]
    [TestCase(1, CharacterType.ForceSensitive)]
    public void ListIncludesExactlyTheSkillsAllowedByMetadata(int selection, CharacterType characterType)
    {
        var model = CreateModel(selection);

        AssertAvailableSkills(model, characterType);
        model.RemainingSkillPoints.Should().Be("Skills - 400 Points Remaining");
    }

    /// <summary>Pins the five exclusive skills and verifies that every other skill is shared.</summary>
    [Test]
    public void AllSkillRestrictionsMatchTheCharacterTypeRules()
    {
        var restrictions = new Dictionary<SkillType, CharacterType>
        {
            [SkillType.Espionage] = CharacterType.Standard,
            [SkillType.Devices] = CharacterType.Standard,
            [SkillType.Force] = CharacterType.ForceSensitive,
            [SkillType.Lightsaber] = CharacterType.ForceSensitive,
            [SkillType.Saberstaff] = CharacterType.ForceSensitive
        };

        foreach (var type in Enum.GetValues<SkillType>())
        {
            var detail = type.GetAttribute<SkillType, SkillAttribute>();
            var restriction = restrictions.GetValueOrDefault(type, CharacterType.Invalid);
            detail.CharacterTypeRestriction.Should().Be(restriction, $"{type} has the correct skill restriction");
            foreach (var characterType in new[] { CharacterType.Standard, CharacterType.ForceSensitive })
            {
                detail.IsAvailableToCharacterType(characterType).Should().Be(
                    restriction == CharacterType.Invalid || restriction == characterType,
                    $"{type} availability must match {characterType}");
            }
        }
    }

    /// <summary>Exercises both selection paths and verifies shared allocations and refunds across a round trip.</summary>
    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    public void SwitchingTypesKeepsEverySharedAllocationAndRefundsEveryUnavailableSkill(int initialSelection, bool fromClient)
    {
        var model = CreateModel(initialSelection);
        var initialType = initialSelection == 0 ? CharacterType.Standard : CharacterType.ForceSensitive;
        var otherType = initialSelection == 0 ? CharacterType.ForceSensitive : CharacterType.Standard;
        var sharedSkills = _cache.Where(x => x.Value.CharacterTypeRestriction == CharacterType.Invalid)
            .Select(x => x.Key).ToHashSet();
        for (var index = 0; index < Skills(model).Count; index++)
            Points(model)[index] = sharedSkills.Contains(Skills(model)[index]) ? 5 : 50;
        Recalculate(model);

        SelectType(model, 1 - initialSelection, fromClient);

        AssertAvailableSkills(model, otherType);
        Points(model).Should().Equal(Skills(model).Select(skill => sharedSkills.Contains(skill) ? 5 : 0));
        model.RemainingSkillPoints.Should().Be($"Skills - {400 - sharedSkills.Count * 5} Points Remaining");
        model.SelectedCharacterTypeName.Should().Be(otherType == CharacterType.Standard ? "Standard" : "Force Sensitive");

        // Invest in the other type's exclusive skills before switching back.
        for (var index = 0; index < Skills(model).Count; index++)
            if (!sharedSkills.Contains(Skills(model)[index]))
                Points(model)[index] = 50;
        Recalculate(model);

        SelectType(model, initialSelection, fromClient);

        AssertAvailableSkills(model, initialType);
        Points(model).Should().Equal(Skills(model).Select(skill => sharedSkills.Contains(skill) ? 5 : 0));
        model.RemainingSkillPoints.Should().Be($"Skills - {400 - sharedSkills.Count * 5} Points Remaining");
    }

    /// <summary>Ensures duplicate selection events do not rebuild an already correct list.</summary>
    [TestCase(0)]
    [TestCase(1)]
    public void RepeatingTheSelectedTypeDoesNotReplaceTheSkillList(int selection)
    {
        var model = CreateModel(selection);
        var names = model.SkillNames;
        model.CharacterType = selection;
        SelectType(model, selection, true);
        model.SkillNames.Should().BeSameAs(names);
    }

    /// <summary>Checks that a client cannot bypass a race's Standard-only character restriction.</summary>
    [Test]
    public void StandardOnlyRaceCannotAcquireForceSkillsThroughClientSelection()
    {
        var model = CreateModel(0);
        typeof(CharacterFullRebuildViewModel).GetProperty("CanSelectForceSensitive", PrivateInstance)!.SetValue(model, false);

        SelectType(model, 1, true);

        model.CharacterType.Should().Be(0);
        model.SelectedCharacterTypeName.Should().Be("Standard");
        AssertAvailableSkills(model, CharacterType.Standard);
    }

    /// <summary>Includes points earned after opening the window in recalculation and type-switch balances.</summary>
    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    public void NewlyEarnedPointsAreIncludedInTheNextBalanceRefresh(int selection, bool switchType)
    {
        var model = CreateModel(selection);
        model.TotalSPAcquired = 40;
        Points(model)[Skills(model).IndexOf(SkillType.Armor)] = 40;
        Recalculate(model);
        model.RemainingSkillPoints.Should().Be("Skills - 0 Points Remaining");

        model.TotalSPAcquired = 41;
        if (switchType)
            SelectType(model, 1 - selection, true);
        else
            Recalculate(model);

        model.RemainingSkillPoints.Should().Be("Skills - 1 Points Remaining");
        Points(model)[Skills(model).IndexOf(SkillType.Armor)].Should().Be(40);
    }

    /// <summary>Save validation must catch an XP award even when no allocation or type changed afterward.</summary>
    [TestCase(0)]
    [TestCase(1)]
    public void SaveValidationRequiresNewlyEarnedPointsToBeDistributed(int selection)
    {
        var model = CreateModel(selection);
        model.TotalSPAcquired = 40;
        var armorIndex = Skills(model).IndexOf(SkillType.Armor);
        Points(model)[armorIndex] = 40;
        HasUnallocatedPoints(model).Should().BeFalse();

        model.TotalSPAcquired = 41;

        HasUnallocatedPoints(model).Should().BeTrue();
        model.RemainingSkillPoints.Should().Be("Skills - 1 Points Remaining");

        Points(model)[armorIndex] = 41;
        HasUnallocatedPoints(model).Should().BeFalse();
        model.RemainingSkillPoints.Should().Be("Skills - 0 Points Remaining");
    }

    /// <summary>Verifies the ordered skill identities, labels, allocations, and tooltips as one consistent list.</summary>
    private void AssertAvailableSkills(CharacterFullRebuildViewModel model, CharacterType characterType)
    {
        var expected = _cache.Where(x => x.Value.CharacterTypeRestriction == CharacterType.Invalid ||
                                         x.Value.CharacterTypeRestriction == characterType).ToList();
        Skills(model).Should().Equal(expected.Select(x => x.Key));
        model.SkillNames.Should().Equal(expected.Select((x, index) => $"{x.Value.Name} [{Points(model)[index]}]"));
        model.SkillTooltips.Should().Equal(expected.Select(x => x.Value.Description));
    }

    /// <summary>Initializes a rebuilder with production skill data and a mutable budget source.</summary>
    private static TestRebuildViewModel CreateModel(int selection)
    {
        var model = new TestRebuildViewModel();
        typeof(CharacterFullRebuildViewModel).GetProperty("CanSelectStandard", PrivateInstance)!.SetValue(model, true);
        typeof(CharacterFullRebuildViewModel).GetProperty("CanSelectForceSensitive", PrivateInstance)!.SetValue(model, true);
        model.CharacterType = selection;
        typeof(CharacterFullRebuildViewModel).GetMethod("LoadSkills", PrivateInstance)!.Invoke(model, null);
        Recalculate(model);
        return model;
    }

    /// <summary>Runs the same balance refresh used by allocation buttons and character-type changes.</summary>
    private static void Recalculate(CharacterFullRebuildViewModel model) =>
        typeof(CharacterFullRebuildViewModel).GetMethod("RecalculateAvailableSkillPoints", PrivateInstance)!.Invoke(model, null);

    /// <summary>Runs the budget validation used by the save confirmation callback.</summary>
    private static bool HasUnallocatedPoints(CharacterFullRebuildViewModel model) =>
        (bool)typeof(CharacterFullRebuildViewModel).GetMethod("HasUnallocatedPoints", PrivateInstance)!.Invoke(model, null)!;

    /// <summary>Applies a direct selection or reproduces client cache writes and notification suppression.</summary>
    private static void SelectType(CharacterFullRebuildViewModel model, int selection, bool fromClient)
    {
        if (!fromClient)
        {
            model.CharacterType = selection;
            return;
        }

        // Reproduce UpdatePropertyFromClient after the native NuiGetBind read: it writes
        // the backing value BEFORE invoking the setter, and suppresses the binding echo.
        var baseType = typeof(CharacterFullRebuildViewModel).BaseType!;
        var cache = (IDictionary)baseType.GetField("_propertyValues", PrivateInstance)!.GetValue(model)!;
        var detail = cache[nameof(model.CharacterType)]!;
        var previous = model.CharacterType;
        detail.GetType().GetProperty("Value")!.SetValue(detail, selection);
        var skipNotify = detail.GetType().GetProperty("SkipNotify")!;
        skipNotify.SetValue(detail, true);
        try
        {
            if (previous != selection)
                model.CharacterType = selection;
        }
        finally
        {
            skipNotify.SetValue(detail, false);
        }
        baseType.GetMethod("OnClientPropertyUpdated", PrivateInstance)!.Invoke(model, new object[] { nameof(model.CharacterType) });
    }

    /// <summary>Reads the skill identities used to map client row indices to allocations.</summary>
    private static List<SkillType> Skills(CharacterFullRebuildViewModel model) =>
        (List<SkillType>)typeof(CharacterFullRebuildViewModel).GetField("_skills", PrivateInstance)!.GetValue(model)!;

    /// <summary>Provides allocation access without calling native NUI button-event functions.</summary>
    private static List<int> Points(CharacterFullRebuildViewModel model) =>
        (List<int>)typeof(CharacterFullRebuildViewModel).GetField("_skillDistributionPoints", PrivateInstance)!.GetValue(model)!;

    /// <summary>Replaces only the engine-backed budget lookup so tests can simulate an external XP award.</summary>
    private sealed class TestRebuildViewModel : CharacterFullRebuildViewModel
    {
        public int TotalSPAcquired { get; set; } = 400;

        /// <inheritdoc />
        protected override int GetCurrentSkillPointBudget() => TotalSPAcquired;
    }
}
