using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server;
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
        Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
    }

    [TestCase("\"I'll catch you all later.\" //gotta bounce, work in the morning")]
    [TestCase("She nods slowly. ((sorry, didn't see you there))")]
    [TestCase("stranger*))")]
    [TestCase("Make him sit in it))")]
    [TestCase("pretend we're normal//")]
    public void Classify_OOCMarkerAfterInCharacterText_IsOutOfCharacter(string message)
    {
        Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
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
        Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
    }

    [TestCase("no problem buddy :)")]
    [TestCase("*:D*")]
    [TestCase("\"Ogay.\" :P")]
    [TestCase("You all take care! <3")]
    [TestCase("o7")]
    [TestCase("not the evil >:3c")]
    public void Classify_EmoticonInNarration_IsOutOfCharacter(string message)
    {
        Classify(message).Should().Be(RoleplayMessageType.OutOfCharacter);
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
        Classify(message).Should().Be(RoleplayMessageType.InCharacter);
    }

    [TestCase("/emote")]
    [TestCase("/emotes")]
    [TestCase("/emote sit")]
    [TestCase("/ brb, afk")]
    public void Classify_ChatCommand_IsNotTreatedAsOOCChatter(string message)
    {
        Classify(message).Should().Be(RoleplayMessageType.ChatCommand);
    }

    [TestCase("their*")]
    [TestCase("on his face*")]
    [TestCase("Sergeant* ")]
    public void Classify_TypoCorrection_IsTypoCorrection(string message)
    {
        Classify(message).Should().Be(RoleplayMessageType.TypoCorrection);
    }

    [TestCase("*waves*")]
    [TestCase("She glances over at the others and smiles*")]
    [TestCase("\"their\"*")]
    public void Classify_EmoteOrLongerTextEndingInAsterisk_IsNotATypoCorrection(string message)
    {
        Classify(message).Should().NotBe(RoleplayMessageType.TypoCorrection);
    }

    [Test]
    public void RecordOOCMessage_WithinDailyAllowance_AddsNoPenalty()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DefaultDailyAllowance, Morning, OOCMessageContext.Standard);

        progress.DailyOOCMessageCount.Should().Be(OOCAllowance.DefaultDailyAllowance);
        progress.PendingOOCPenaltySteps.Should().Be(0);
        progress.OOCMessageCount.Should().Be((ulong)OOCAllowance.DefaultDailyAllowance);
    }

    [Test]
    public void RecordOOCMessage_PastDailyAllowance_AddsOnePenaltyStepPerMessage()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DefaultDailyAllowance + 3, Morning, OOCMessageContext.Standard);

        progress.PendingOOCPenaltySteps.Should().Be(3);
    }

    [Test]
    public void RecordOOCMessage_PenaltySteps_AreCappedAtFullAward()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DefaultDailyAllowance + OOCAllowance.MaxPenaltySteps + 25, Morning, OOCMessageContext.Standard);

        progress.PendingOOCPenaltySteps.Should().Be(OOCAllowance.MaxPenaltySteps);
        OOCAllowance.ApplyPenalty(1500, progress.PendingOOCPenaltySteps, 1).Should().Be(0);
    }

    [Test]
    public void RecordOOCMessage_WithDMPresent_UsesDMAllowanceBeforeDailyAllowance()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, OOCAllowance.DefaultDMPresentAllowance, Morning, OOCMessageContext.DMPresent);

        progress.DailyDMPresentOOCMessageCount.Should().Be(OOCAllowance.DefaultDMPresentAllowance);
        progress.DailyOOCMessageCount.Should().Be(0);
        progress.PendingOOCPenaltySteps.Should().Be(0);

        RecordMessages(progress, OOCAllowance.DefaultDailyAllowance + 1, Morning, OOCMessageContext.DMPresent);

        progress.DailyOOCMessageCount.Should().Be(OOCAllowance.DefaultDailyAllowance + 1);
        progress.PendingOOCPenaltySteps.Should().Be(1);
    }

    [Test]
    public void RecordOOCMessage_InEventArea_IsUnlimited()
    {
        var progress = new RoleplayProgress();

        RecordMessages(progress, 500, Morning, OOCMessageContext.EventArea);

        progress.DailyOOCMessageCount.Should().Be(0);
        progress.DailyDMPresentOOCMessageCount.Should().Be(0);
        progress.DailyEventAreaOOCMessageCount.Should().Be(0);
        progress.PendingOOCPenaltySteps.Should().Be(0);
        progress.OOCMessageCount.Should().Be(500);
    }

    [Test]
    public void RecordOOCMessage_InEventAreaWithLimit_UsesEventThenDMThenDailyAllowance()
    {
        var progress = new RoleplayProgress();
        var context = OOCMessageContext.EventArea | OOCMessageContext.DMPresent;

        for (var index = 0; index < 7; index++)
        {
            OOCAllowance.RecordOOCMessage(
                progress,
                Morning,
                context,
                dailyAllowance: 1,
                dmPresentAllowance: 2,
                eventAreaAllowance: 3);
        }

        progress.DailyEventAreaOOCMessageCount.Should().Be(3);
        progress.DailyDMPresentOOCMessageCount.Should().Be(2);
        progress.DailyOOCMessageCount.Should().Be(2);
        progress.PendingOOCPenaltySteps.Should().Be(1);
    }

    [Test]
    public void RecordOOCMessage_InEventAreaWithZeroAllowance_UsesDailyAllowance()
    {
        var progress = new RoleplayProgress();

        OOCAllowance.RecordOOCMessage(
            progress,
            Morning,
            OOCMessageContext.EventArea,
            dailyAllowance: 0,
            dmPresentAllowance: 50,
            eventAreaAllowance: 0);

        progress.DailyEventAreaOOCMessageCount.Should().Be(0);
        progress.DailyDMPresentOOCMessageCount.Should().Be(0, "no DM is present");
        progress.PendingOOCPenaltySteps.Should().Be(1);
    }

    [Test]
    public void RecordOOCMessage_ResetsDailyCountsAtTheScheduledRestartTime()
    {
        var progress = new RoleplayProgress();
        var beforeRestart = new DateTime(2026, 10, 13, 9, 59, 0, DateTimeKind.Utc);
        var afterRestart = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc);

        RecordMessages(progress, OOCAllowance.DefaultDailyAllowance, Morning, OOCMessageContext.Standard);
        RecordMessages(progress, 5, Morning, OOCMessageContext.DMPresent);
        OOCAllowance.RecordOOCMessage(progress, Morning, OOCMessageContext.EventArea, 20, 50, eventAreaAllowance: 10);
        Record(progress, beforeRestart, OOCMessageContext.Standard);

        progress.PendingOOCPenaltySteps.Should().Be(1);

        Record(progress, afterRestart, OOCMessageContext.Standard);

        progress.DailyOOCMessageCount.Should().Be(1);
        progress.DailyDMPresentOOCMessageCount.Should().Be(0);
        progress.DailyEventAreaOOCMessageCount.Should().Be(0);
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
            Record(progress, time, OOCMessageContext.Standard);
        }

        progress.DailyOOCMessageCount.Should().Be(times.Length, "extra restarts and midnight do not start a new allowance day");
    }

    [TestCase(0, 10, 1500)]
    [TestCase(1, 10, 1350)]
    [TestCase(5, 10, 750)]
    [TestCase(10, 10, 0)]
    [TestCase(25, 10, 0)]
    [TestCase(-3, 10, 1500)]
    [TestCase(3, 25, 375)]
    [TestCase(4, 25, 0)]
    [TestCase(7, 0, 1500)]
    public void ApplyPenalty_ReducesAwardByConfiguredPercentPerStep(int steps, int percentPerStep, int expectedXP)
    {
        OOCAllowance.ApplyPenalty(1500, steps, percentPerStep).Should().Be(expectedXP);
    }

    [Test]
    public void RecordOOCMessage_UsesConfiguredAllowances()
    {
        var progress = new RoleplayProgress();

        for (var index = 0; index < 4; index++)
        {
            OOCAllowance.RecordOOCMessage(
                progress,
                Morning,
                OOCMessageContext.DMPresent,
                dailyAllowance: 1,
                dmPresentAllowance: 2,
                eventAreaAllowance: null);
        }

        progress.DailyDMPresentOOCMessageCount.Should().Be(2);
        progress.DailyOOCMessageCount.Should().Be(2);
        progress.PendingOOCPenaltySteps.Should().Be(1);
    }

    [Test]
    public void Classify_ConfiguredTerms_ReplaceTheDefaultList()
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "holoskip", ":]" };

        RoleplayMessageClassifier.Classify("She glances away. HOLOSKIP", terms).Should().Be(RoleplayMessageType.OutOfCharacter);
        RoleplayMessageClassifier.Classify("Nice one :]", terms).Should().Be(RoleplayMessageType.OutOfCharacter);
        RoleplayMessageClassifier.Classify("brb, food", terms).Should().Be(RoleplayMessageType.InCharacter);
    }

    [Test]
    [NonParallelizable]
    public void ApplicationSettings_ReadsOOCSettingsFromEnvironment()
    {
        var variables = new Dictionary<string, string>
        {
            ["SWLOR_RPXP_OOC_DAILY_ALLOWANCE"] = "35",
            ["SWLOR_RPXP_OOC_DM_ALLOWANCE"] = "0",
            ["SWLOR_RPXP_OOC_EVENT_ALLOWANCE"] = "15",
            ["SWLOR_RPXP_OOC_PENALTY_PERCENT"] = "250",
            ["SWLOR_RPXP_OOC_TERMS"] = " Holoskip , ,:] "
        };

        var settings = CreateSettings(variables);

        settings.RoleplayXPDailyOOCAllowance.Should().Be(35);
        settings.RoleplayXPDMPresentOOCAllowance.Should().Be(0);
        settings.RoleplayXPEventAreaOOCAllowance.Should().Be(15);
        settings.RoleplayXPOOCPenaltyPercent.Should().Be(100);
        settings.RoleplayXPOOCTerms.Should().BeEquivalentTo(new[] { "Holoskip", ":]" });
        settings.RoleplayXPOOCTerms.Should().Contain("HOLOSKIP");
    }

    [Test]
    [NonParallelizable]
    public void ApplicationSettings_BlankOrInvalidOOCSettings_UseDefaults()
    {
        var variables = new Dictionary<string, string>
        {
            ["SWLOR_RPXP_OOC_DAILY_ALLOWANCE"] = "",
            ["SWLOR_RPXP_OOC_DM_ALLOWANCE"] = "-5",
            ["SWLOR_RPXP_OOC_EVENT_ALLOWANCE"] = "lots",
            ["SWLOR_RPXP_OOC_PENALTY_PERCENT"] = "ten",
            ["SWLOR_RPXP_OOC_TERMS"] = " , "
        };

        var settings = CreateSettings(variables);

        settings.RoleplayXPDailyOOCAllowance.Should().Be(OOCAllowance.DefaultDailyAllowance);
        settings.RoleplayXPDMPresentOOCAllowance.Should().Be(OOCAllowance.DefaultDMPresentAllowance);
        settings.RoleplayXPEventAreaOOCAllowance.Should().BeNull("event areas default to unlimited OOC");
        settings.RoleplayXPOOCPenaltyPercent.Should().Be(OOCAllowance.DefaultPenaltyPercentPerMessage);
        settings.RoleplayXPOOCTerms.Should().BeSameAs(RoleplayMessageClassifier.DefaultOOCTerms);
    }

    [Test]
    public void RoleplayXP_ChatCommandsAndTypoCorrections_ExitBeforePointsOrPenalties()
    {
        var roleplayXpSource = ReadServerSource("Feature", "RoleplayXP.cs");
        var processMessage = ExtractMethod(roleplayXpSource, "public static void ProcessRPMessage()").Replace("\r\n", "\n");

        var oocBranch = processMessage.IndexOf("if (messageType == RoleplayMessageType.OutOfCharacter)", StringComparison.Ordinal);
        var nonRoleplayExit = processMessage.IndexOf(
            "if (messageType == RoleplayMessageType.ChatCommand ||\n                messageType == RoleplayMessageType.TypoCorrection)\n                return;",
            StringComparison.Ordinal);
        var timestampRefresh = processMessage.IndexOf("var timestampString = RefreshRPMessageTimestamp(player, now);", StringComparison.Ordinal);
        var pointAward = processMessage.IndexOf("dbPlayer.RoleplayProgress.RPPoints++;", StringComparison.Ordinal);

        oocBranch.Should().BeGreaterThanOrEqualTo(0);
        nonRoleplayExit.Should().BeGreaterThan(oocBranch, "chat commands must never reach the OOC penalty");
        timestampRefresh.Should().BeGreaterThan(nonRoleplayExit);
        pointAward.Should().BeGreaterThan(nonRoleplayExit, "chat commands must never earn RP points");
    }

    [Test]
    public void RoleplayXP_ClearsPendingPenaltyWhenAwardIsGranted()
    {
        var roleplayXpSource = ReadServerSource("Feature", "RoleplayXP.cs");
        var awardMethod = ExtractMethod(roleplayXpSource, "private static void ProcessPlayerRoleplayXP(uint player)");

        awardMethod.Should().Contain("xp = OOCAllowance.ApplyPenalty(");
        awardMethod.Should().Contain("dbPlayer.RoleplayProgress.PendingOOCPenaltySteps,");
        awardMethod.Should().Contain("ApplicationSettings.Get().RoleplayXPOOCPenaltyPercent);");
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

    [TestCase("unlimited")]
    [TestCase(" UNLIMITED ")]
    [TestCase("-1")]
    [TestCase("")]
    [NonParallelizable]
    public void ApplicationSettings_EventAllowance_SupportsUnlimited(string value)
    {
        var settings = CreateSettings(new Dictionary<string, string> { ["SWLOR_RPXP_OOC_EVENT_ALLOWANCE"] = value });

        settings.RoleplayXPEventAreaOOCAllowance.Should().BeNull();
    }

    private static RoleplayMessageType Classify(string message)
    {
        return RoleplayMessageClassifier.Classify(message, RoleplayMessageClassifier.DefaultOOCTerms);
    }

    private static void Record(RoleplayProgress progress, DateTime utcNow, OOCMessageContext context)
    {
        OOCAllowance.RecordOOCMessage(
            progress,
            utcNow,
            context,
            OOCAllowance.DefaultDailyAllowance,
            OOCAllowance.DefaultDMPresentAllowance,
            OOCAllowance.DefaultEventAreaAllowance);
    }

    private static void RecordMessages(RoleplayProgress progress, int count, DateTime utcNow, OOCMessageContext context)
    {
        for (var index = 0; index < count; index++)
        {
            Record(progress, utcNow, context);
        }
    }

    private static ApplicationSettings CreateSettings(IReadOnlyDictionary<string, string> variables)
    {
        var previous = variables.Keys.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var (name, value) in variables)
            {
                Environment.SetEnvironmentVariable(name, value);
            }

            return (ApplicationSettings)Activator.CreateInstance(typeof(ApplicationSettings), nonPublic: true);
        }
        finally
        {
            foreach (var (name, value) in previous)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
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
