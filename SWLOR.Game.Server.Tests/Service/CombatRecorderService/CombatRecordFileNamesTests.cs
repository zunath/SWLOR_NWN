using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatRecorderService;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordFileNamesTests
{
    private static readonly DateTime Start = new(2026, 10, 12, 23, 15, 7, 42, DateTimeKind.Utc);

    [Test]
    public void BaseName_StartsWithTheUtcStartTimeAndEndsWithTheLabel()
    {
        CombatRecordFileNames.BuildBaseName(Start, "pvp_9v9").Should().Be("20261012T231507042Z-pvp_9v9");
    }

    [Test]
    public void BaseName_DiffersForRestartsWithinTheSameSecond()
    {
        CombatRecordFileNames.BuildBaseName(Start.AddMilliseconds(1), "pvp_9v9")
            .Should().NotBe(CombatRecordFileNames.BuildBaseName(Start, "pvp_9v9"));
    }

    [TestCase("9v9 PvP / round 2", "9v9-PvP-round-2")]
    [TestCase("  ../../etc/passwd  ", "etc-passwd")]
    [TestCase("Chirodactyl: hard mode!", "Chirodactyl-hard-mode")]
    [TestCase("", "recording")]
    [TestCase(null, "recording")]
    [TestCase("***", "recording")]
    public void Label_KeepsOnlyFileNameSafeCharacters(string label, string expected)
    {
        CombatRecordFileNames.SanitizeLabel(label).Should().Be(expected);
    }

    [Test]
    public void Label_IsCappedInLength()
    {
        CombatRecordFileNames.SanitizeLabel(new string('a', 200)).Should().HaveLength(48);
    }

    [Test]
    public void Directory_SitsUnderTheAppLogDirectory()
    {
        CombatRecordFileNames.GetDirectory("/nwn/home/app_logs/").Should().Be("/nwn/home/app_logs/CombatRecordings/");
        CombatRecordFileNames.GetDirectory(null).Should().Be("CombatRecordings/");
    }
}
