using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanNPCGroupTests
{
    [Test]
    public void NPCGroups_HaveUniqueIds()
    {
        Enum.GetValues<NPCGroupType>().Select(group => (int)group)
            .Should().OnlyHaveUniqueItems("each enemy group must track quest progress independently");
    }

    [TestCase(NPCGroupType.Hutlar_FoundryDroid, 297)]
    [TestCase(NPCGroupType.AbandonedStation_Horror, 298)]
    public void ExistingGroups_KeepPersistedIds(NPCGroupType group, int expected)
    {
        ((int)group).Should().Be(expected, "existing quest progress must retain its enemy group");
    }

    [TestCase("esh_direwolf", NPCGroupType.Eshan_DireWolf)]
    [TestCase("esh_frostwolf", NPCGroupType.Eshan_FrostWolf)]
    [TestCase("esh_wolfalpha", NPCGroupType.Eshan_DireWolfAlpha)]
    [TestCase("esh_nc_scout", NPCGroupType.Eshan_NeoCrusaderScout)]
    [TestCase("esh_nc_hunter", NPCGroupType.Eshan_NeoCrusaderHunter)]
    [TestCase("esh_nc_vanguard", NPCGroupType.Eshan_NeoCrusaderVanguard)]
    [TestCase("esh_nc_heavy", NPCGroupType.Eshan_NeoCrusaderHeavy)]
    [TestCase("esh_nc_medic", NPCGroupType.Eshan_NeoCrusaderMedic)]
    [TestCase("esh_nc_captain", NPCGroupType.Eshan_NeoCrusaderCaptain)]
    [TestCase("esh_sunguard", NPCGroupType.Eshan_SunGuard)]
    [TestCase("esh_scrap_smug", NPCGroupType.Eshan_ScrapyardSmuggler)]
    [TestCase("esh_gorakvesh", NPCGroupType.Eshan_GorakVesh)]
    public void CreatureQuestGroups_MatchTheirEshanGroup(string resref, NPCGroupType group)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        if (root == null)
            throw new DirectoryNotFoundException("Could not locate repository root.");

        using var creature = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName, "Module", "utc", $"{resref}.utc.json")));
        var groupId = creature.RootElement.GetProperty("VarTable").GetProperty("value")
            .EnumerateArray()
            .Single(variable => variable.GetProperty("Name").GetProperty("value").GetString()
                == "QUEST_NPC_GROUP_ID")
            .GetProperty("Value").GetProperty("value").GetInt32();

        groupId.Should().Be((int)group);
    }
}
