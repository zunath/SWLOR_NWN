using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class DeadPlayerXPTests
{
    [Test]
    public void SkillXP_GuardsDeadPlayersAtAwardBoundary()
    {
        var source = ReadServerSource("Service", "Skill.cs");
        var method = ExtractMethod(source, "public static void GiveSkillXP(");

        AssertDeadPlayerGuardExitsBefore(method, "player", "return;", "var modifiedSkills");
    }

    [Test]
    public void CombatPointDistribution_SkipsDeadPlayersBeforeSkillAndBeastXPHooks()
    {
        var source = ReadServerSource("Service", "CombatPoint.cs");
        var method = ExtractMethod(source, "static void DistributeSkillXP()");

        AssertDeadPlayerGuardExitsBefore(method, "player", "continue;", "Skill.GiveSkillXP");
        AssertDeadPlayerGuardExitsBefore(method, "player", "continue;", "SWLOR_COMBAT_POINT_DISTRIBUTED");
    }

    [Test]
    public void RoleplayXP_DoesNotAccrueOrPayOutWhileDead()
    {
        var source = ReadServerSource("Feature", "RoleplayXP.cs");
        var heartbeat = ExtractMethod(source, "public static void DistributeRoleplayXP()");
        var payout = ExtractMethod(source, "private static void ProcessPlayerRoleplayXP(uint player)");
        var message = ExtractMethod(source, "public static void ProcessRPMessage()");

        AssertDeadPlayerGuardExitsBefore(heartbeat, "player", "return;", "var ticks = GetLocalInt(player, RPTickVariable) + 1;");
        AssertDeadPlayerGuardExitsBefore(payout, "player", "return;", "dbPlayer.UnallocatedXP += xp;");
        AssertDeadPlayerGuardExitsBefore(message, "player", "return;", "dbPlayer.RoleplayProgress.RPPoints++;");
    }

    [Test]
    public void RPXPDistribution_DoesNotSpendAvailableXPWhileDead()
    {
        var skillsSource = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "SkillsViewModel.cs");
        var distributeSource = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "DistributeRPXPViewModel.cs");
        var openWindow = ExtractMethod(skillsSource, "public Action OnClickDistributeRPXP() => () =>");
        var confirm = ExtractMethod(distributeSource, "public Action OnClickConfirm() => () =>");

        AssertDeadPlayerGuardExitsBefore(openWindow, "Player", "return;", "var playerId = GetObjectUUID(Player);");
        AssertDeadPlayerGuardExitsBefore(confirm, "Player", "return;", "dbPlayer.UnallocatedXP -= amount;");
    }

    [Test]
    public void QuestKillCreditAndCompletion_SkipDeadPlayersBeforeRewards()
    {
        var questSource = ReadServerSource("Service", "Quest.cs");
        var questEncounterSource = ReadServerSource("Service", "QuestService", "QuestEncounter.cs");
        var questDetailSource = ReadServerSource("Service", "QuestService", "QuestDetail.cs");
        var killProgression = ExtractMethod(questSource, "public static void ProgressKillTargetObjectives()");
        var encounterKillCredit = ExtractMethod(questEncounterSource, "private static void ProgressKillCredit(");
        var canComplete = ExtractMethod(questDetailSource, "public bool CanComplete(uint player)");
        var advance = ExtractMethod(questDetailSource, "public bool Advance(uint player, uint questSource)");
        var complete = ExtractMethod(questDetailSource, "public void Complete(uint player, uint questSource, IQuestReward selectedReward)");

        killProgression.Should().Contain("QuestEncounter.ProgressKillCredit(creature, npcGroupType, possibleQuests);");
        AssertDeadPlayerGuardExitsBefore(encounterKillCredit, "player", "return;", "killTargetObjective.Advance(player, questId);");
        AssertDeadPlayerGuardExitsBefore(encounterKillCredit, "player", "return;", "questDetail.Advance(player, encounterCreature);");
        AssertDeadPlayerGuardExitsBefore(canComplete, "player", "return false;", "var playerId = GetObjectUUID(player);");
        AssertDeadPlayerGuardExitsBefore(advance, "player", "return false;", "var playerId = GetObjectUUID(player);");
        AssertDeadPlayerGuardExitsBefore(complete, "player", "return;", "if (!CanComplete(player)) return;");
        AssertDeadPlayerGuardExitsBefore(complete, "player", "return;", "reward.GiveReward(player);");
    }

    private static string ReadServerSource(params string[] pathSegments)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            new[] { root.FullName, "SWLOR.Game.Server" }.Concat(pathSegments).ToArray()));
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        openBraceIndex.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static void AssertDeadPlayerGuardExitsBefore(
        string method,
        string playerExpression,
        string expectedExitStatement,
        string protectedToken)
    {
        var protectedIndex = method.IndexOf(protectedToken, StringComparison.Ordinal);
        protectedIndex.Should().BeGreaterThanOrEqualTo(0);

        var guard = FindDeadPlayerGuard(method, playerExpression, expectedExitStatement);
        guard.Should().NotBeNull(
            $"dead-player guard for '{playerExpression}' should exit with '{expectedExitStatement}'");
        guard!.Value.IfIndex.Should().BeLessThan(protectedIndex);
    }

    private static (int IfIndex, string Condition)? FindDeadPlayerGuard(
        string method,
        string playerExpression,
        string expectedExitStatement)
    {
        const string IfToken = "if";
        var searchIndex = 0;
        while (searchIndex < method.Length)
        {
            var ifIndex = method.IndexOf(IfToken, searchIndex, StringComparison.Ordinal);
            if (ifIndex < 0)
                return null;

            searchIndex = ifIndex + IfToken.Length;
            if ((ifIndex > 0 && IsIdentifierCharacter(method[ifIndex - 1])) ||
                (searchIndex < method.Length && IsIdentifierCharacter(method[searchIndex])))
            {
                continue;
            }

            var openParenIndex = SkipWhitespace(method, searchIndex);
            if (openParenIndex >= method.Length || method[openParenIndex] != '(')
                continue;

            var closeParenIndex = FindMatchingDelimiter(method, openParenIndex, '(', ')');
            var condition = method.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
            if (!condition.Contains($"GetIsDead({playerExpression})", StringComparison.Ordinal) ||
                !condition.Contains($"GetCurrentHitPoints({playerExpression}) <= 0", StringComparison.Ordinal))
                continue;

            if (StatementContainsExit(method, closeParenIndex + 1, expectedExitStatement))
                return (ifIndex, condition);
        }

        return null;
    }

    private static bool StatementContainsExit(string source, int statementIndex, string expectedExitStatement)
    {
        statementIndex = SkipWhitespace(source, statementIndex);
        if (statementIndex >= source.Length)
            return false;

        if (source[statementIndex] != '{')
        {
            var semicolonIndex = source.IndexOf(';', statementIndex);
            return semicolonIndex >= 0 &&
                   source.Substring(statementIndex, semicolonIndex - statementIndex + 1)
                       .Trim()
                       .Equals(expectedExitStatement, StringComparison.Ordinal);
        }

        var closeBraceIndex = FindMatchingDelimiter(source, statementIndex, '{', '}');
        var body = source.Substring(statementIndex + 1, closeBraceIndex - statementIndex - 1);
        var depth = 0;
        for (var index = 0; index < body.Length; index++)
        {
            if (body[index] == '{')
            {
                depth++;
                continue;
            }

            if (body[index] == '}')
            {
                depth--;
                continue;
            }

            if (depth == 0 &&
                body.AsSpan(index).TrimStart().StartsWith(expectedExitStatement.AsSpan(), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static int FindMatchingDelimiter(string source, int openIndex, char openDelimiter, char closeDelimiter)
    {
        var depth = 0;
        for (var index = openIndex; index < source.Length; index++)
        {
            if (source[index] == openDelimiter)
            {
                depth++;
            }
            else if (source[index] == closeDelimiter)
            {
                depth--;
                if (depth == 0)
                    return index;
            }
        }

        throw new InvalidOperationException($"Could not find matching '{closeDelimiter}'.");
    }

    private static int SkipWhitespace(string source, int index)
    {
        while (index < source.Length && char.IsWhiteSpace(source[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsIdentifierCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
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
