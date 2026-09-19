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

    [TearDown]
    public void TearDown()
    {
        _cache.Clear();
        foreach (var entry in _original)
            _cache.Add(entry.Key, entry.Value);
    }

    [TestCase(0, CharacterType.Standard)]
    [TestCase(1, CharacterType.ForceSensitive)]
    public void ListIncludesExactlyTheSkillsAllowedByMetadata(int selection, CharacterType characterType)
    {
        var model = CreateModel(selection);

        AssertAvailableSkills(model, characterType);
        model.RemainingSkillPoints.Should().Be("Skills - 400 Points Remaining");
    }

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

    private void AssertAvailableSkills(CharacterFullRebuildViewModel model, CharacterType characterType)
    {
        var expected = _cache.Where(x => x.Value.CharacterTypeRestriction == CharacterType.Invalid ||
                                         x.Value.CharacterTypeRestriction == characterType).ToList();
        Skills(model).Should().Equal(expected.Select(x => x.Key));
        model.SkillNames.Should().Equal(expected.Select((x, index) => $"{x.Value.Name} [{Points(model)[index]}]"));
        model.SkillTooltips.Should().Equal(expected.Select(x => x.Value.Description));
    }

    private static CharacterFullRebuildViewModel CreateModel(int selection)
    {
        var model = new CharacterFullRebuildViewModel();
        typeof(CharacterFullRebuildViewModel).GetProperty("CanSelectStandard", PrivateInstance)!.SetValue(model, true);
        typeof(CharacterFullRebuildViewModel).GetProperty("CanSelectForceSensitive", PrivateInstance)!.SetValue(model, true);
        typeof(CharacterFullRebuildViewModel).GetField("_totalSkillPoints", PrivateInstance)!.SetValue(model, 400);
        model.CharacterType = selection;
        typeof(CharacterFullRebuildViewModel).GetMethod("LoadSkills", PrivateInstance)!.Invoke(model, null);
        Recalculate(model);
        return model;
    }

    private static void Recalculate(CharacterFullRebuildViewModel model) =>
        typeof(CharacterFullRebuildViewModel).GetMethod("RecalculateAvailableSkillPoints", PrivateInstance)!.Invoke(model, null);

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

    private static List<SkillType> Skills(CharacterFullRebuildViewModel model) =>
        (List<SkillType>)typeof(CharacterFullRebuildViewModel).GetField("_skills", PrivateInstance)!.GetValue(model)!;

    private static List<int> Points(CharacterFullRebuildViewModel model) =>
        (List<int>)typeof(CharacterFullRebuildViewModel).GetField("_skillDistributionPoints", PrivateInstance)!.GetValue(model)!;
}
