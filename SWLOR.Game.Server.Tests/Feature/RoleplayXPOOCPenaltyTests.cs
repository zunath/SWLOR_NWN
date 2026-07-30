using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class RoleplayXPOOCPenaltyTests
{
    [Test]
    public void RoleplayProgress_PersistsOOCMessageCount()
    {
        var dbPlayer = new Player();

        dbPlayer.RoleplayProgress.OOCMessageCount.Should().Be(0);

        dbPlayer.RoleplayProgress.OOCMessageCount++;

        dbPlayer.RoleplayProgress.OOCMessageCount.Should().Be(1);
    }

    [Test]
    public void OOCDetection_RunsBeforeShortMessageFilterAndRecognizesWholeMessagePrefixes()
    {
        var roleplayXpSource = ReadServerSource("Feature", "RoleplayXP.cs");
        var processMessage = ExtractMethod(roleplayXpSource, "public static void ProcessRPMessage()");
        var oocDetection = ExtractMethod(roleplayXpSource, "private static bool IsOOCMessage(string message)");
        var normalizedProcessMessage = processMessage.Replace("\r\n", "\n");

        processMessage.IndexOf("if (IsOOCMessage(message))", StringComparison.Ordinal)
            .Should()
            .BeLessThan(processMessage.IndexOf("if (message.Length <= 3)", StringComparison.Ordinal));
        processMessage.IndexOf("if (message.Length <= 3)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(processMessage.IndexOf("var timestampString = RefreshRPMessageTimestamp(player, now);", StringComparison.Ordinal));
        normalizedProcessMessage.Should().Contain(
            "if (IsOOCMessage(message))\n            {\n                RefreshRPMessageTimestamp(player, now);\n                ApplyOOCMessagePenalty(player, dbPlayer);\n                return;\n            }");

        oocDetection.Should().Contain("var trimmedMessage = message.TrimStart();");
        oocDetection.Should().Contain("trimmedMessage.StartsWith(\"//\", StringComparison.Ordinal)");
        oocDetection.Should().Contain("trimmedMessage.StartsWith(\"((\", StringComparison.Ordinal)");
        oocDetection.Should().Contain("trimmedMessage.StartsWith(\"[[\", StringComparison.Ordinal)");
        oocDetection.Should().Contain("trimmedMessage.StartsWith(\"{{\", StringComparison.Ordinal)");
        oocDetection.Should().Contain("trimmedMessage.StartsWith(\"OOC:\", StringComparison.OrdinalIgnoreCase)");
        oocDetection.Should().Contain("trimmedMessage.StartsWith(\"[OOC]\", StringComparison.OrdinalIgnoreCase)");
        oocDetection.Should().Contain("trimmedMessage.StartsWith(\"(OOC)\", StringComparison.OrdinalIgnoreCase)");
        oocDetection.Should().NotContain("Contains(");
    }

    [Test]
    public void OOCPenalty_ResetsSenderOnlyProgressAndAppliesCappedEscalatingDelay()
    {
        var roleplayXpSource = ReadServerSource("Feature", "RoleplayXP.cs");
        var penaltyMethod = ExtractMethod(roleplayXpSource, "private static void ApplyOOCMessagePenalty(uint player, Player dbPlayer)");
        var penaltyTicksMethod = ExtractMethod(roleplayXpSource, "private static int GetOOCMessagePenaltyTicks(ulong oocMessageCount)");

        roleplayXpSource.Should().Contain("private const string RPTickVariable = \"RP_SYSTEM_TICKS\";");
        roleplayXpSource.Should().Contain("private const int OOCPenaltyTicks = 50;");
        roleplayXpSource.Should().Contain("private const int MaxOOCPenaltyTicks = 300;");
        roleplayXpSource.Should().Contain("var ticks = GetLocalInt(player, RPTickVariable) + 1;");

        penaltyMethod.Should().Contain("dbPlayer.RoleplayProgress.OOCMessageCount++;");
        penaltyMethod.Should().Contain("dbPlayer.RoleplayProgress.RPPoints = 0;");
        penaltyMethod.Should().Contain("DB.Set(dbPlayer);");
        penaltyMethod.Should().Contain("SetLocalInt(player, RPTickVariable, -penaltyTicks);");
        penaltyMethod.Should().NotContain("for (var currentPlayer = GetFirstPC()");

        penaltyTicksMethod.Should().Contain("if (oocMessageCount <= 1)");
        penaltyTicksMethod.Should().Contain("return 0;");
        penaltyTicksMethod.Should().Contain("var penaltySteps = oocMessageCount - 1;");
        penaltyTicksMethod.Should().Contain("var penaltyTicks = penaltySteps * OOCPenaltyTicks;");
        penaltyTicksMethod.Should().Contain("Math.Min((ulong)MaxOOCPenaltyTicks, penaltyTicks)");
    }

    [Test]
    public void OOCMessageCount_RemainsInvisibleToPlayersAndAdminViews()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var roleplayXpSource = ReadServerSource("Feature", "RoleplayXP.cs");
        var penaltyMethod = ExtractMethod(roleplayXpSource, "private static void ApplyOOCMessagePenalty(uint player, Player dbPlayer)");
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

        roleplayXpSource.Should().NotContain("OOC message detected");
        roleplayXpSource.Should().NotContain("Your RP XP timer");
        penaltyMethod.Should().NotContain("SendMessageToPC(");
        adminPlayerAdvanced.Should().NotContain("OOCMessageCount");
        adminPlayerOverview.Should().NotContain("OOCMessageCount");
    }

    private static string ReadServerSource(params string[] pathSegments)
    {
        var root = RepoPaths.FindRepositoryRoot();
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

}
