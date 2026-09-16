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
        var model = new CharacterFullRebuildViewModel();
        LoadForType(model, selection);

        var expected = _cache.Where(x => x.Value.CharacterTypeRestriction == CharacterType.Invalid ||
                                         x.Value.CharacterTypeRestriction == characterType).ToList();
        Skills(model).Should().Equal(expected.Select(x => x.Key));
        model.SkillNames.Should().Equal(expected.Select(x => $"{x.Value.Name} [0]"));
        model.SkillTooltips.Should().Equal(expected.Select(x => x.Value.Description));
    }

    [Test]
    public void SwitchingTypesKeepsSharedAllocationsAndReleasesUnavailableAllocations()
    {
        var model = new CharacterFullRebuildViewModel();
        LoadForType(model, 0);
        Points(model)[Skills(model).IndexOf(SkillType.Armor)] = 20;
        Points(model)[Skills(model).IndexOf(SkillType.Espionage)] = 30;

        LoadForType(model, 1);
        Skills(model).Should().NotContain(SkillType.Espionage).And.NotContain(SkillType.Devices);
        Skills(model).Should().Contain(SkillType.Force);
        Points(model).Sum().Should().Be(20);
        model.SkillNames[Skills(model).IndexOf(SkillType.Armor)].Should().Be("Armor [20]");

        LoadForType(model, 0);
        Skills(model).Should().NotContain(SkillType.Force);
        Points(model)[Skills(model).IndexOf(SkillType.Espionage)].Should().Be(0);
        Points(model).Sum().Should().Be(20);
    }

    private static void LoadForType(CharacterFullRebuildViewModel model, int selection)
    {
        // Exercise list rebuilding without the live player's DB-backed point pool.
        typeof(CharacterFullRebuildViewModel).GetField("_skillsLoaded", PrivateInstance)!.SetValue(model, false);
        typeof(CharacterFullRebuildViewModel).GetProperty("CanSelectStandard", PrivateInstance)!.SetValue(model, true);
        typeof(CharacterFullRebuildViewModel).GetProperty("CanSelectForceSensitive", PrivateInstance)!.SetValue(model, true);
        model.CharacterType = selection;
        typeof(CharacterFullRebuildViewModel).GetMethod("LoadSkills", PrivateInstance)!.Invoke(model, null);
    }

    private static List<SkillType> Skills(CharacterFullRebuildViewModel model) =>
        (List<SkillType>)typeof(CharacterFullRebuildViewModel).GetField("_skills", PrivateInstance)!.GetValue(model)!;

    private static List<int> Points(CharacterFullRebuildViewModel model) =>
        (List<int>)typeof(CharacterFullRebuildViewModel).GetField("_skillDistributionPoints", PrivateInstance)!.GetValue(model)!;
}
