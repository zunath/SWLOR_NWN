using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.RoleplayXPService;

namespace SWLOR.Game.Server.Tests.Feature;

public class RoleplayXPOOCPenaltyTests
{
    private static readonly DateTime Morning = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

    [TestCase("//brb, getting food")]
    [TestCase("((gotta run, night all))")]
    [TestCase("[[back]]")]
    [TestCase("{{one sec}}")]
    [TestCase(@"\\sure let me finish this first")]
    [TestCase("OOC: is the event still on?")]
    [TestCase("[OOC] heading out")]
    [TestCase("(OOC) heading out")]
    [TestCase("   //leading whitespace")]
    public void Classify_ExplicitOOCPrefix_IsOutOfCharacter(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
    }

    [TestCase("\"I'll catch you all later.\" //gotta bounce, work in the morning")]
    [TestCase("She nods slowly. ((sorry, didn't see you there))")]
    [TestCase("stranger*))")]
    [TestCase("Make him sit in it))")]
    [TestCase("pretend we're normal//")]
    public void Classify_OOCMarkerAfterInCharacterText_IsOutOfCharacter(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
    }

    [TestCase("brb")]
    [TestCase("afk rq, food is ready")]
    [TestCase("gonna relog to fix my combat log")]
    [TestCase("idk what im giving you")]
    [TestCase("does it now lmao")]
    [TestCase("LMAOOO")]
    [TestCase("lolol")]
    [TestCase("Tryin to find where they are xD")]
    [TestCase("can my pet get the xp?")]
    [TestCase("Also yes, I can see DM chat")]
    [TestCase("Ohp, nvm! Misread the chart")]
    [TestCase("\"Sounds good.\" ty for the rp!")]
    [TestCase("\"Fair enough.\" She grins. tbh I think that was the best roll all night")]
    public void Classify_ChatSpeakTypedAsNarration_IsOutOfCharacter(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
    }

    [TestCase("no problem buddy :)")]
    [TestCase("*:D*")]
    [TestCase("\"Ogay.\" :P")]
    [TestCase("You all take care! <3")]
    [TestCase("o7")]
    [TestCase("not the evil >:3c")]
    public void Classify_EmoticonInNarration_IsOutOfCharacter(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
    }

    [TestCase("\"Hello there.\" He waves.")]
    [TestCase("She nods, his own definition aligning well.")]
    [TestCase("*waves* Good to see you.")]
    [TestCase("[waves] Good to see you.")]
    [TestCase("\"Lol, you think the council will buy that?\" She snorts.")]
    [TestCase("\"brb, they said. Whatever that means.\"")]
    [TestCase("\"Thir-ty...\" she counts. \"Twen-ty...\"")]
    [TestCase("She counts it out under her breath: thir-ty, for-ty.")]
    [TestCase("He wanders over to the RP(o)G rack.")]
    [TestCase("Check https://example.com for the schedule.")]
    [TestCase("The ship lurched as the hyperdrive cut out.")]
    [TestCase("Plan D: we run.")]
    [TestCase("\"Ugh.\"")]
    [TestCase("")]
    public void Classify_InCharacterText_IsInCharacter(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().Be(RoleplayMessageType.InCharacter);
    }

    [TestCase("/emote")]
    [TestCase("/emotes")]
    [TestCase("/emote sit")]
    [TestCase("/ brb, afk")]
    public void Classify_ChatCommand_IsNotTreatedAsOOCChatter(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().Be(RoleplayMessageType.ChatCommand);
    }

    [TestCase("their*")]
    [TestCase("on his face*")]
    [TestCase("Sergeant* ")]
    public void Classify_TypoCorrection_IsTypoCorrection(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().Be(RoleplayMessageType.TypoCorrection);
    }

    [TestCase("*waves*")]
    [TestCase("She glances over at the others and smiles*")]
    [TestCase("\"their\"*")]
    public void Classify_EmoteOrLongerTextEndingInAsterisk_IsNotATypoCorrection(string message)
    {
        RoleplayMessageClassifier.Classify(message).Should().NotBe(RoleplayMessageType.TypoCorrection);
    }

    [Test]
    public void RecordOOCMessage_WithinDailyAllowance_AddsNoPenalty()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DailyAllowance, Morning, OOCMessageContext.Standard);

        progress.DailyOOCMessageCount.Should().Be(OOCAllowance.DailyAllowance);
        progress.PendingOOCPenaltySteps.Should().Be(0);
        progress.OOCMessageCount.Should().Be((ulong)OOCAllowance.DailyAllowance);
    }

    [Test]
    public void RecordOOCMessage_PastDailyAllowance_AddsOnePenaltyStepPerMessage()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DailyAllowance + 3, Morning, OOCMessageContext.Standard);

        progress.PendingOOCPenaltySteps.Should().Be(3);
    }

    [Test]
    public void RecordOOCMessage_PenaltySteps_AreCappedAtFullAward()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DailyAllowance + 50, Morning, OOCMessageContext.Standard);

        progress.PendingOOCPenaltySteps.Should().Be(OOCAllowance.MaxPenaltySteps);
        OOCAllowance.ApplyPenalty(1500, progress.PendingOOCPenaltySteps).Should().Be(0);
    }

    [Test]
    public void RecordOOCMessage_WithDMPresent_UsesDMAllowanceBeforeDailyAllowance()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DMPresentAllowance, Morning, OOCMessageContext.DMPresent);

        progress.DailyDMPresentOOCMessageCount.Should().Be(OOCAllowance.DMPresentAllowance);
        progress.DailyOOCMessageCount.Should().Be(0);
        progress.PendingOOCPenaltySteps.Should().Be(0);

        RecordMessages(progress, OOCAllowance.DailyAllowance + 1, Morning, OOCMessageContext.DMPresent);

        progress.DailyOOCMessageCount.Should().Be(OOCAllowance.DailyAllowance + 1);
        progress.PendingOOCPenaltySteps.Should().Be(1);
    }

    [Test]
    public void RecordOOCMessage_InEventArea_IsUnlimited()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, 500, Morning, OOCMessageContext.EventArea);

        progress.DailyOOCMessageCount.Should().Be(0);
        progress.DailyDMPresentOOCMessageCount.Should().Be(0);
        progress.PendingOOCPenaltySteps.Should().Be(0);
        progress.OOCMessageCount.Should().Be(500);
    }

    [Test]
    public void RecordOOCMessage_ResetsDailyCountsAtTheScheduledRestartTime()
    {
        var progress = new RoleplayProgress();
        var beforeRestart = new DateTime(2026, 10, 13, 9, 59, 0, DateTimeKind.Utc);
        var afterRestart = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc);

        RecordMessages(progress, OOCAllowance.DailyAllowance, Morning, OOCMessageContext.Standard);
        RecordMessages(progress, 5, Morning, OOCMessageContext.DMPresent);
        OOCAllowance.RecordOOCMessage(progress, beforeRestart, OOCMessageContext.Standard);

        progress.PendingOOCPenaltySteps.Should().Be(1);

        OOCAllowance.RecordOOCMessage(progress, afterRestart, OOCMessageContext.Standard);

        progress.DailyOOCMessageCount.Should().Be(1);
        progress.DailyDMPresentOOCMessageCount.Should().Be(0);
        progress.PendingOOCPenaltySteps.Should().Be(1, "unspent penalties apply to the next award, not the next day");
    }

    [Test]
    public void RecordOOCMessage_DoesNotResetAcrossTimesWithinTheSameDay()
    {
        var progress = new RoleplayProgress();
        var times = new[]
        {
            new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 12, 15, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 12, 23, 59, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 13, 9, 59, 59, DateTimeKind.Utc)
        };

        foreach (var time in times)
        {
            OOCAllowance.RecordOOCMessage(progress, time, OOCMessageContext.Standard);
        }

        progress.DailyOOCMessageCount.Should().Be(times.Length, "extra restarts and midnight do not start a new allowance day");
    }

    [TestCase(0, 1500)]
    [TestCase(1, 1350)]
    [TestCase(5, 750)]
    [TestCase(10, 0)]
    [TestCase(25, 0)]
    [TestCase(-3, 1500)]
    public void ApplyPenalty_ReducesAwardByTenPercentPerStep(int steps, int expectedXP)
    {
        OOCAllowance.ApplyPenalty(1500, steps).Should().Be(expectedXP);
    }

    [Test]
    public void RoleplayXP_ClearsPendingPenaltyWhenAwardIsGranted()
    {
        var roleplayXpSource = ReadServerSource("Feature", "RoleplayXP.cs");
        var awardMethod = ExtractMethod(roleplayXpSource, "private static void ProcessPlayerRoleplayXP(uint player)");

        awardMethod.Should().Contain("xp = OOCAllowance.ApplyPenalty(xp, dbPlayer.RoleplayProgress.PendingOOCPenaltySteps);");
        awardMethod.Should().Contain("dbPlayer.RoleplayProgress.PendingOOCPenaltySteps = 0;");
        roleplayXpSource.Should().NotContain("SetLocalInt(player, RPTickVariable, -");
    }

    [Test]
    public void OOCTracking_RemainsInvisibleToPlayersAndAdminViews()
    {
        var root = FindRepositoryRoot();
        var roleplayXpSource = ReadServerSource("Feature", "RoleplayXP.cs");
        var allowanceSource = ReadServerSource("Service", "RoleplayXPService", "OOCAllowance.cs");
        var adminPlayerAdvanced = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Admin",
            "Shared",
            "Components",
            "PlayerAdvanced.razor"));
        var adminPlayerOverview = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Admin",
            "Shared",
            "Components",
            "PlayerOverview.razor"));

        var playerGuideSource = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "PlayerGuideViewModel.cs");

        roleplayXpSource.Should().NotContain("OOC message detected");
        playerGuideSource.Should().NotContainAny(new[] { "allowance", "OOC messages each day", "reduces your next RP XP" },
            "OOC limits and penalties are not documented to players");
        allowanceSource.Should().NotContain("SendMessageToPC(");
        foreach (var adminView in new[] { adminPlayerAdvanced, adminPlayerOverview })
        {
            adminView.Should().NotContain("OOCMessageCount");
            adminView.Should().NotContain("PendingOOCPenaltySteps");
        }
    }

    private static void RecordMessages(RoleplayProgress progress, int count, DateTime utcNow, OOCMessageContext context)
    {
        for (var index = 0; index < count; index++)
        {
            OOCAllowance.RecordOOCMessage(progress, utcNow, context);
        }
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
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
