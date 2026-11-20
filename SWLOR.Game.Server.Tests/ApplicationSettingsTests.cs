using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests;

/// <summary>
/// How the server reads its settings from the process environment: recognized spellings, defaults for blank or mistyped
/// values, and the environment the server counts as explicit.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ApplicationSettingsTests
{
    private static readonly string[] Names =
    {
        "SWLOR_APP_LOG_DIRECTORY", "SWLOR_EVENT_WEBHOOK_URL", "SWLOR_ENGINE_TESTS_ENABLED", "SWLOR_ENGINE_TEST_RESULTS_DIRECTORY",
        "SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS", "SWLOR_ENGINE_TEST_SHUTDOWN", "SWLOR_RPXP_OOC_PENALTY_ENABLED",
        "SWLOR_RPXP_OOC_DAILY_ALLOWANCE", "SWLOR_RPXP_OOC_EVENT_ALLOWANCE", "SWLOR_RPXP_OOC_PENALTY_PERCENT",
        "SWLOR_RPXP_OOC_DETECTION_FILE", "SWLOR_FRAME_PROFILER_ENABLED", "SWLOR_FRAME_PROFILER_SLOW_FRAME_MS",
        "SWLOR_FRAME_PROFILER_SUMMARY_SECONDS", "SWLOR_ENVIRONMENT",
    };

    private string?[] _previous = null!;

    [SetUp]
    public void SetUp()
    {
        _previous = Names.Select(Environment.GetEnvironmentVariable).ToArray();
        foreach (var name in Names)
            Environment.SetEnvironmentVariable(name, null);
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = 0; index < Names.Length; index++)
            Environment.SetEnvironmentVariable(Names[index], _previous[index]);
    }

    private static ApplicationSettings Read(params (string Name, string Value)[] variables)
    {
        foreach (var (name, value) in variables)
            Environment.SetEnvironmentVariable(name, value);
        return (ApplicationSettings)Activator.CreateInstance(typeof(ApplicationSettings), nonPublic: true)!;
    }

    [Test]
    public void WithNothingSet_TheDefaultsApply()
    {
        var settings = Read();

        settings.ServerEnvironment.Should().Be(ServerEnvironmentType.Development);
        settings.ServerEnvironmentIsExplicit.Should().BeFalse();
        settings.EventWebhookUrl.Should().BeNull();
        settings.EngineTestsEnabled.Should().BeFalse();
        settings.EngineTestResultsDirectory.Should().BeNull();
        settings.EngineTestStartupDelaySeconds.Should().Be(15f);
        settings.EngineTestShutdownOnCompletion.Should().BeTrue();
        settings.RoleplayXPOOCPenaltyEnabled.Should().BeFalse();
        settings.FrameProfilerEnabled.Should().BeFalse();
        settings.FrameProfilerSlowFrameMilliseconds.Should().Be(50d);
        settings.FrameProfilerSummarySeconds.Should().Be(60d);
        settings.RoleplayXPOOCDetectionFile.Should().NotBeNullOrWhiteSpace();
    }

    [TestCase("prod", ServerEnvironmentType.Production, true)]
    [TestCase(" Production ", ServerEnvironmentType.Production, true)]
    [TestCase("test", ServerEnvironmentType.Test, true)]
    [TestCase("TESTING", ServerEnvironmentType.Test, true)]
    [TestCase("dev", ServerEnvironmentType.Development, true)]
    [TestCase("development", ServerEnvironmentType.Development, true)]
    [TestCase("produciton", ServerEnvironmentType.Development, false)]
    [TestCase("", ServerEnvironmentType.Development, false)]
    public void TheEnvironmentIsReadFromItsRecognizedSpellings(string value, ServerEnvironmentType expected, bool explicitEnvironment)
    {
        var settings = Read(("SWLOR_ENVIRONMENT", value));

        settings.ServerEnvironment.Should().Be(expected);
        settings.ServerEnvironmentIsExplicit.Should().Be(explicitEnvironment);
    }

    [TestCase("true", true)]
    [TestCase(" 1 ", true)]
    [TestCase("YES", true)]
    [TestCase("false", false)]
    [TestCase("0", false)]
    [TestCase("no", false)]
    [TestCase("maybe", true)]
    [TestCase("", true)]
    public void ABooleanSettingKeepsItsDefaultForABlankOrMistypedValue(string value, bool expected)
    {
        // The shutdown setting defaults to true, so a typo must leave it true.
        Read(("SWLOR_ENGINE_TEST_SHUTDOWN", value)).EngineTestShutdownOnCompletion.Should().Be(expected);
    }

    [Test]
    public void AFloatSettingAcceptsOnlyFiniteNonNegativeNumbers()
    {
        Read(("SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS", "2.5")).EngineTestStartupDelaySeconds.Should().Be(2.5f);
        Read(("SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS", "-1")).EngineTestStartupDelaySeconds.Should().Be(15f);
        Read(("SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS", "Infinity")).EngineTestStartupDelaySeconds.Should().Be(15f);
        Read(("SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS", "soon")).EngineTestStartupDelaySeconds.Should().Be(15f);
    }

    [Test]
    public void TheSummaryIntervalIsHeldToAtLeastOneSecond()
    {
        Read(("SWLOR_FRAME_PROFILER_SUMMARY_SECONDS", "0.2")).FrameProfilerSummarySeconds.Should().Be(1d);
        Read(("SWLOR_FRAME_PROFILER_SUMMARY_SECONDS", "30")).FrameProfilerSummarySeconds.Should().Be(30d);
    }

    [Test]
    public void TheRoleplayAllowancesParseNumbersAndUnlimited()
    {
        Read(("SWLOR_RPXP_OOC_DAILY_ALLOWANCE", "7")).RoleplayXPDailyOOCAllowance.Should().Be(7);
        var fallback = Read(("SWLOR_RPXP_OOC_DAILY_ALLOWANCE", "-3")).RoleplayXPDailyOOCAllowance;
        fallback.Should().BeGreaterThanOrEqualTo(0);
        Read(("SWLOR_RPXP_OOC_DAILY_ALLOWANCE", "many")).RoleplayXPDailyOOCAllowance.Should().Be(fallback);

        Read(("SWLOR_RPXP_OOC_EVENT_ALLOWANCE", "12")).RoleplayXPEventAreaOOCAllowance.Should().Be(12);
        Read(("SWLOR_RPXP_OOC_EVENT_ALLOWANCE", "Unlimited")).RoleplayXPEventAreaOOCAllowance.Should().BeNull();
        Read(("SWLOR_RPXP_OOC_EVENT_ALLOWANCE", "-1")).RoleplayXPEventAreaOOCAllowance.Should().BeNull();
        var defaultAllowance = Read().RoleplayXPEventAreaOOCAllowance;
        Read(("SWLOR_RPXP_OOC_EVENT_ALLOWANCE", "-5")).RoleplayXPEventAreaOOCAllowance.Should().Be(defaultAllowance);
        Read(("SWLOR_RPXP_OOC_EVENT_ALLOWANCE", "lots")).RoleplayXPEventAreaOOCAllowance.Should().Be(defaultAllowance);
    }

    [Test]
    public void ThePenaltyPercentIsCappedAtOneHundred_AndTheDetectionFileIsTrimmed()
    {
        Read(("SWLOR_RPXP_OOC_PENALTY_PERCENT", "250")).RoleplayXPOOCPenaltyPercent.Should().Be(100);
        Read(("SWLOR_RPXP_OOC_DETECTION_FILE", "  /data/ooc.json ")).RoleplayXPOOCDetectionFile.Should().Be("/data/ooc.json");
    }

    [Test]
    public void ThePathsAndWebhookAreReadAsGiven_AndTheEngineTestResultsFollowTheLogDirectory()
    {
        var logged = Read(("SWLOR_APP_LOG_DIRECTORY", "/logs/"), ("SWLOR_EVENT_WEBHOOK_URL", "https://example.test/hook"));
        logged.EventWebhookUrl.Should().Be("https://example.test/hook");
        logged.EngineTestResultsDirectory.Should().Be("/logs/engine_tests/");

        Read(("SWLOR_APP_LOG_DIRECTORY", "/logs/"), ("SWLOR_ENGINE_TEST_RESULTS_DIRECTORY", "/results/"))
            .EngineTestResultsDirectory.Should().Be("/results/");
    }

    [Test]
    public void GetCachesOneInstance()
    {
        ApplicationSettings.Get().Should().BeSameAs(ApplicationSettings.Get());
    }
}
