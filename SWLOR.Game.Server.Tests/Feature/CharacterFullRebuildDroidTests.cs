using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class CharacterFullRebuildDroidTests
{
    [Test]
    public void FullRebuild_UsesRaceClassPrerequisiteTablesForCharacterTypeOptions()
    {
        var root = FindRepositoryRoot();
        var raceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Race.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterFullRebuildViewModel.cs"));
        var definitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "CharacterFullRebuildDefinition.cs"));

        raceSource.Should().Contain("public static bool IsClassAvailableToRace(ClassType classType, RacialType race)");
        raceSource.Should().Contain("private static readonly Dictionary<RacialType, int> _forceSensitiveRacePrerequisites");
        raceSource.Should().Contain("private const string ForceSensitivePrerequisites2DA = \"cls_pres_force\";");
        raceSource.Should().Contain("private const string RequirementParam1Column = \"ReqParam1\";");
        raceSource.Should().Contain("return GetForceSensitiveRacePrerequisites().ContainsKey(race);");
        raceSource.Should().Contain("Get2DAString(ForceSensitivePrerequisites2DA, RequirementParam1Column, row)");
        raceSource.Should().Contain("_forceSensitiveRacePrerequisites[(RacialType)requiredRaceId] = row;");
        raceSource.Should().NotContain("_classRacePrerequisites");
        raceSource.Should().NotContain("Disabled2DAValue");

        viewModelSource.Should().Contain("private int NormalizeCharacterType(int value)");
        viewModelSource.Should().Contain("CanSelectStandard = Race.IsClassAvailableToRace(ClassType.Standard, race);");
        viewModelSource.Should().Contain("CanSelectForceSensitive = Race.IsClassAvailableToRace(ClassType.ForceSensitive, race);");
        viewModelSource.Should().Contain("ShowCharacterTypeOptions = CanSelectStandard && CanSelectForceSensitive;");
        viewModelSource.Should().Contain("ShowReadOnlyCharacterType = !ShowCharacterTypeOptions;");
        viewModelSource.Should().Contain("var selectedCharacterType = NormalizeCharacterType(CharacterType);");
        viewModelSource.Should().Contain("var selectedClassType = GetCharacterClassType(selectedCharacterType);");
        viewModelSource.Should().Contain("if (!Race.IsClassAvailableToRace(selectedClassType, race))");
        viewModelSource.Should().NotContain("RacialType.Droid");

        definitionSource.Should().Contain("row.BindIsVisible(model => model.ShowCharacterTypeOptions);");
        definitionSource.Should().Contain("row.BindIsVisible(model => model.ShowReadOnlyCharacterType);");
        definitionSource.Should().Contain(".BindText(model => model.SelectedCharacterTypeName)");
    }

    [Test]
    public void ClassPrerequisite2DAs_DisableForceSensitiveDroids()
    {
        var root = FindRepositoryRoot();
        var twoDARoot = Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da");

        var classes = Read2DA(Path.Combine(twoDARoot, "classes.2da"));
        classes[(int)ClassType.Standard]["PreReqTable"].Should().Be("cls_pres_stand");
        classes[(int)ClassType.ForceSensitive]["PreReqTable"].Should().Be("cls_pres_force");

        var standardPrereqs = Read2DA(Path.Combine(twoDARoot, "cls_pres_stand.2da"));
        var forcePrereqs = Read2DA(Path.Combine(twoDARoot, "cls_pres_force.2da"));

        var standardDroidRow = standardPrereqs.Single(row =>
            row.Value["ReqType"] == "RACE" &&
            row.Value["ReqParam1"] == ((int)RacialType.Droid).ToString()).Key;

        standardPrereqs.Values.Should().Contain(row =>
            row["ReqType"] == "RACE" &&
            row["ReqParam1"] == ((int)RacialType.Droid).ToString());
        forcePrereqs.Values.Should().NotContain(row =>
            row["ReqType"] == "RACE" &&
            row["ReqParam1"] == ((int)RacialType.Droid).ToString());
        forcePrereqs[standardDroidRow]["LABEL"].Should().Be("DroidDisabled");
        forcePrereqs[standardDroidRow]["ReqType"].Should().Be("****");
        forcePrereqs[standardDroidRow]["ReqParam1"].Should().Be("****");
    }

    private static Dictionary<int, Dictionary<string, string>> Read2DA(string path)
    {
        var rows = File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Skip(1)
            .ToList();
        var columns = rows[0]
            .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        return rows
            .Skip(1)
            .Select(line => line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= columns.Count + 1)
            .ToDictionary(
                parts => int.Parse(parts[0]),
                parts => columns
                    .Select((column, index) => (column, value: parts[index + 1]))
                    .ToDictionary(x => x.column, x => x.value));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
