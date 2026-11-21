using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Service;

public class ClientJsonRepairReportTests
{
    [Test]
    public void ReportAddsUpPayloadsAndKeepsTheFirstDistinctSamples()
    {
        var report = new ClientJsonRepairReport("account", 7);

        report.Add(2, false, new[] { "StatV?f-hs", "StatV?f-hs" }, 3);
        report.Add(0, true, new string[0], 3);
        report.Add(3, false, new[] { "Outfit ?", "Na?me", "dropped" }, 3);

        Assert.That(report.Replaced, Is.EqualTo(5));
        Assert.That(report.Rejected, Is.EqualTo(1));
        Assert.That(report.Samples, Is.EqualTo(new[] { "StatV?f-hs", "Outfit ?", "Na?me" }));
    }

    [Test]
    public void LogLineNamesTheSenderAndQuotesTheRepairedText()
    {
        var report = new ClientJsonRepairReport("account", 7);
        report.Add(2, false, new[] { "Stat\"V?\nf" }, 3);

        Assert.That(report.Describe("Character - CDKEY"), Is.EqualTo(
            "Client NUI data from account (Character - CDKEY): replaced 2 invalid UTF-8 byte(s), " +
            "rejected 0 unreadable payload(s). Repaired text: \"Stat?V??f\""));
    }

    [Test]
    public void LogLineWithoutSamplesKeepsItsFormat()
    {
        var report = new ClientJsonRepairReport("client 4294967295", 0x7F000000);
        report.Add(0, true, new string[0], 3);

        Assert.That(report.Describe("no character"), Is.EqualTo(
            "Client NUI data from client 4294967295 (no character): replaced 0 invalid UTF-8 byte(s), " +
            "rejected 1 unreadable payload(s)."));
    }
}
