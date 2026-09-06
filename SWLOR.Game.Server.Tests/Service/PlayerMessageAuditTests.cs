using System.Text.Json;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class PlayerMessageAuditTests
{
    private static readonly HashSet<string> MessageMethods = new(StringComparer.Ordinal)
    {
        "SendMessageToPC", "FloatingTextStringOnCreature", "FloatingTextStrRefOnCreature",
        "SendMessageToPCByStrRef", "SendMessageToAllPCs", "SendMessageNearbyToPlayers",
        "SendFeedbackString", "SendFeedbackMessage", "SendMessage", "PostString",
        "SpeakString", "ActionSpeakString", "SendDiagnosticToPlayer", "ShowDiagnosticFloatingText",
        "SendDiagnosticNearby", "SendResourceRestored", "SendWarningToPlayer"
    };

    [Test]
    public void MessageInventory_CoversEveryRuntimeCallSite()
    {
        var root = FindRepositoryRoot();
        using var audit = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,
            "SWLOR.Game.Server", "Readmes", "PlayerMessageAudit.json")));
        var reviewed = audit.RootElement.EnumerateArray().Select(row =>
            row.GetProperty("File").GetString() + "|" +
            row.GetProperty("Call").GetString()?.Replace("\r\n", "\n")).ToArray();
        var current = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.Contains("/obj/") || relative.Contains("/bin/"))
                continue;
            foreach (var call in ReadCalls(file).Where(call => MessageMethods.Contains(MethodName(call))))
                current.Add(relative + "|" + call.ToString().Replace("\r\n", "\n"));
        }

        current.Should().BeEquivalentTo(reviewed,
            "message changes need a Production spam review and a refreshed PlayerMessageAudit.json (tools/ExportPlayerMessageAudit.ps1)");
    }

    [Test]
    public void RepetitiveCombatAndStatusPaths_CannotBypassTheDiagnosticPolicy()
    {
        var root = FindRepositoryRoot();
        var files = new[] { "Service/Combat.cs", "Service/StatusEffect.cs", "Service/Faction.cs",
            "Feature/StatusEffectDefinition/GuardedStatusEffect.cs" }
            .Select(relative => Path.Combine(root, "SWLOR.Game.Server", relative)).ToList();
        // Mining depletion is a completion/failure notice, not a routine ship-combat tick.
        files.AddRange(Directory.EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server", "Feature", "ShipModuleDefinition"), "*.cs")
            .Where(file => !file.EndsWith("MiningLaserModuleDefinition.cs") && !file.EndsWith("StripMinerModuleDefinition.cs")));

        var bypasses = files.SelectMany(file => ReadCalls(file)
            .Where(call => MethodName(call) is "SendMessageToPC" or "FloatingTextStringOnCreature" or "SendMessageNearbyToPlayers")
            .Where(call => !(Path.GetFileName(file) == "StatusEffect.cs" &&
                call.Ancestors().OfType<MethodDeclarationSyntax>().First().Identifier.ValueText == "SendStatusEffectFailure"))
            .Select(call => Path.GetFileName(file) + ": " + call)).ToArray();
        bypasses.Should().BeEmpty("automatic ticks/procs must be silent in Production");
    }

    [Test]
    public void StatusValidationFailures_RemainVisibleInProduction()
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Service", "StatusEffect.cs");
        var failureCalls = ReadCalls(file).Where(call => call.Ancestors().OfType<MethodDeclarationSyntax>()
            .First().Identifier.ValueText == "SendStatusEffectFailure").ToArray();
        failureCalls.Should().Contain(call => MethodName(call) == "SendMessageToPC");
        failureCalls.Should().NotContain(call => MethodName(call) == "SendDiagnosticToPlayer");
    }

    [TestCase("Feature/NaturalRegeneration.cs", 4)]
    [TestCase("Feature/StatusEffectDefinition/RestStatusEffect.cs", 2)]
    public void RegenerationAndRest_ExplicitlySuppressResourceFeedback(string relative, int expectedCalls)
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", relative);
        var restores = ReadCalls(file).Where(call => MethodName(call) is "RestoreFP" or "RestoreStamina").ToArray();
        restores.Should().HaveCount(expectedCalls);
        foreach (var restore in restores)
        {
            restore.ArgumentList.Arguments.Should().Contain(argument =>
                argument.NameColon != null && argument.NameColon.Name.Identifier.ValueText == "sendFeedback" &&
                argument.Expression.IsKind(SyntaxKind.FalseLiteralExpression));
        }
    }

    [Test]
    public void MimicryFailures_RemainRateLimitedAndObservationsRemainDiagnostic()
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Service", "Mimicry.cs");
        var calls = ReadCalls(file).ToArray();
        var failures = calls.Where(call => MethodName(call) == "SendWarningToPlayer").ToArray();
        failures.Should().HaveCount(2);
        failures.Should().Contain(call => call.ArgumentList.Arguments[1].ToString().Contains("MIMICRY_RANK_"));
        failures.Should().Contain(call => call.ArgumentList.Arguments[1].ToString().Contains("MIMICRY_DECODE_"));
        foreach (var failure in failures)
            failure.ArgumentList.Arguments.Should().Contain(argument => argument.ToString() == "intervalSeconds: 60");
        calls.Should().Contain(call => MethodName(call) == "SendDiagnosticToPlayer" &&
            call.ToString().Contains("Your combat analyzer records"));
    }

    [Test]
    public void NativeAttackFeedback_IsGuardedForEveryCustomMessage()
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs");
        var calls = ReadCalls(file).Where(call => MethodName(call) == "SendFeedbackString").ToArray();
        calls.Should().HaveCount(5);
        foreach (var call in calls)
        {
            call.Ancestors().OfType<IfStatementSyntax>().Should()
                .Contain(statement => statement.Condition.ToString() == "PlayerFeedback.DiagnosticsEnabled");
        }
    }

    private static IEnumerable<InvocationExpressionSyntax> ReadCalls(string file) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

    private static string MethodName(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => string.Empty
    };

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
