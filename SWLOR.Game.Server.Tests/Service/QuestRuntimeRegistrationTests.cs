using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestRuntimeRegistrationTests
{
    private readonly List<string> _registeredQuestIds = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var questId in _registeredQuestIds)
        {
            Quest.UnregisterRuntimeQuest(questId);
        }

        _registeredQuestIds.Clear();
    }

    [Test]
    public void RegisterRuntimeQuest_MakesQuestRetrievableByGetQuestByIdOrDefault()
    {
        var quest = CreateAndRegisterQuest(out var questId);

        Quest.GetQuestByIdOrDefault(questId).Should().BeSameAs(quest);
    }

    [Test]
    public void RegisterRuntimeQuest_ReplacesAnExistingQuestWithTheSameId()
    {
        var questId = "qcontract_" + Guid.NewGuid();
        _registeredQuestIds.Add(questId);

        var original = new QuestDetail { QuestId = questId, Name = "Original" };
        var replacement = new QuestDetail { QuestId = questId, Name = "Replacement" };

        Quest.RegisterRuntimeQuest(original);
        Quest.RegisterRuntimeQuest(replacement);

        Quest.GetQuestByIdOrDefault(questId).Should().BeSameAs(replacement);
    }

    [Test]
    public void UnregisterRuntimeQuest_RemovesQuestFromCache()
    {
        CreateAndRegisterQuest(out var questId);

        Quest.UnregisterRuntimeQuest(questId);

        Quest.GetQuestByIdOrDefault(questId).Should().BeNull();
    }

    [Test]
    public void UnregisterRuntimeQuest_UnknownQuestIdIsANoOp()
    {
        var action = () => Quest.UnregisterRuntimeQuest("qcontract_" + Guid.NewGuid());

        action.Should().NotThrow();
    }

    [Test]
    public void GetQuestByIdOrDefault_ReturnsNullForUnknownQuestId()
    {
        Quest.GetQuestByIdOrDefault("qcontract_" + Guid.NewGuid()).Should().BeNull();
    }

    [Test]
    public void LoadPlayerQuests_TreatsUnregisteredQuestsAsOrphansInsteadOfThrowing()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "Quest.cs");
        var method = ExtractMethod(source, "public static void LoadPlayerQuests()");

        method.Should().Contain("var quest = GetQuestByIdOrDefault(questId);");
        method.Should().NotContain("var quest = GetQuestById(questId);");
        method.Should().Contain("if (quest == null)");
        method.Should().Contain("staleQuestIds.Add(questId);");
        method.Should().Contain("dbPlayer.Quests.Remove(staleQuestId);");
        method.Should().Contain("if (staleQuestIds.Count > 0)");
    }

    private static QuestDetail CreateAndRegisterQuest(out string questId)
    {
        questId = "qcontract_" + Guid.NewGuid();
        var quest = new QuestDetail { QuestId = questId, Name = "Test Quest" };
        Quest.RegisterRuntimeQuest(quest);
        return quest;
    }

    private static string ReadSource(params string[] pathParts)
    {
        var fullPath = Path.Combine(new[] { FindRepositoryRoot().FullName }.Concat(pathParts).ToArray());
        return File.ReadAllText(fullPath);
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should exist");

        var openBrace = source.IndexOf('{', start);
        openBrace.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should have an opening brace");

        var depth = 0;
        for (var i = openBrace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"Method '{signature}' was not closed.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests should run inside the repository checkout");
        return directory!;
    }
}
