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

        method.Should().Contain("GetIsDead(player)");
        method.Should().Contain("GetCurrentHitPoints(player) <= 0");
        method.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(method.IndexOf("var modifiedSkills", StringComparison.Ordinal));
    }

    [Test]
    public void CombatPointDistribution_SkipsDeadPlayersBeforeSkillAndBeastXPHooks()
    {
        var source = ReadServerSource("Service", "CombatPoint.cs");
        var method = ExtractMethod(source, "static void DistributeSkillXP()");

        method.Should().Contain("GetIsDead(player)");
        method.Should().Contain("GetCurrentHitPoints(player) <= 0");
        method.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(method.IndexOf("Skill.GiveSkillXP", StringComparison.Ordinal));
        method.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(method.IndexOf("SWLOR_COMBAT_POINT_DISTRIBUTED", StringComparison.Ordinal));
    }

    [Test]
    public void RoleplayXP_DoesNotAccrueOrPayOutWhileDead()
    {
        var source = ReadServerSource("Feature", "RoleplayXP.cs");
        var heartbeat = ExtractMethod(source, "public static void DistributeRoleplayXP()");
        var payout = ExtractMethod(source, "private static void ProcessPlayerRoleplayXP(uint player)");
        var message = ExtractMethod(source, "public static void ProcessRPMessage()");

        heartbeat.Should().Contain("GetIsDead(player)");
        heartbeat.Should().Contain("GetCurrentHitPoints(player) <= 0");
        heartbeat.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(heartbeat.IndexOf("var ticks = GetLocalInt(player, RPTickVariable) + 1;", StringComparison.Ordinal));

        payout.Should().Contain("GetIsDead(player)");
        payout.Should().Contain("GetCurrentHitPoints(player) <= 0");
        payout.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(payout.IndexOf("dbPlayer.UnallocatedXP += xp;", StringComparison.Ordinal));

        message.Should().Contain("GetIsDead(player)");
        message.Should().Contain("GetCurrentHitPoints(player) <= 0");
        message.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(message.IndexOf("dbPlayer.RoleplayProgress.RPPoints++;", StringComparison.Ordinal));
    }

    [Test]
    public void RPXPDistribution_DoesNotSpendAvailableXPWhileDead()
    {
        var skillsSource = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "SkillsViewModel.cs");
        var distributeSource = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "DistributeRPXPViewModel.cs");
        var openWindow = ExtractMethod(skillsSource, "public Action OnClickDistributeRPXP() => () =>");
        var confirm = ExtractMethod(distributeSource, "public Action OnClickConfirm() => () =>");

        openWindow.Should().Contain("GetIsDead(Player)");
        openWindow.Should().Contain("GetCurrentHitPoints(Player) <= 0");

        confirm.Should().Contain("GetIsDead(Player)");
        confirm.Should().Contain("GetCurrentHitPoints(Player) <= 0");
        confirm.IndexOf("GetIsDead(Player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(confirm.IndexOf("dbPlayer.UnallocatedXP -= amount;", StringComparison.Ordinal));
    }

    [Test]
    public void QuestKillCreditAndCompletion_SkipDeadPlayersBeforeRewards()
    {
        var questSource = ReadServerSource("Service", "Quest.cs");
        var questDetailSource = ReadServerSource("Service", "QuestService", "QuestDetail.cs");
        var killProgression = ExtractMethod(questSource, "public static void ProgressKillTargetObjectives()");
        var canComplete = ExtractMethod(questDetailSource, "public bool CanComplete(uint player)");
        var advance = ExtractMethod(questDetailSource, "public void Advance(uint player, uint questSource)");
        var complete = ExtractMethod(questDetailSource, "public void Complete(uint player, uint questSource, IQuestReward selectedReward)");

        killProgression.Should().Contain("GetIsDead(member)");
        killProgression.Should().Contain("GetCurrentHitPoints(member) <= 0");
        killProgression.IndexOf("GetIsDead(member)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(killProgression.IndexOf("killTargetObjective.Advance(member, questId);", StringComparison.Ordinal));

        canComplete.Should().Contain("GetIsDead(player)");
        canComplete.Should().Contain("GetCurrentHitPoints(player) <= 0");
        canComplete.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(canComplete.IndexOf("var playerId = GetObjectUUID(player);", StringComparison.Ordinal));

        advance.Should().Contain("GetIsDead(player)");
        advance.Should().Contain("GetCurrentHitPoints(player) <= 0");

        complete.Should().Contain("GetIsDead(player)");
        complete.Should().Contain("GetCurrentHitPoints(player) <= 0");
        complete.IndexOf("GetIsDead(player)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(complete.IndexOf("if (!CanComplete(player)) return;", StringComparison.Ordinal));
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
