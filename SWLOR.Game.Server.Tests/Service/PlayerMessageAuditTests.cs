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
        "SendResourceRestored", "SendWarningToPlayer"
    };

    [Test]
    public void MessageInventory_CoversEveryRuntimeCallSite()
    {
        var root = FindRepositoryRoot();
        using var audit = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,
            "SWLOR.Game.Server", "Readmes", "PlayerMessageAudit.json")));
        var reviewed = audit.RootElement.EnumerateArray().Select(row => new MessageAuditEntry(
            row.GetProperty("File").GetString(),
            row.GetProperty("Member").GetString(),
            row.GetProperty("Delivery").GetString(),
            row.GetProperty("Call").GetString()?.Replace("\r\n", "\n"))).ToArray();
        var current = new List<MessageAuditEntry>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.Contains("/obj/") || relative.Contains("/bin/"))
                continue;
            foreach (var call in ReadCalls(file).Where(call => MessageMethods.Contains(MethodName(call))))
                current.Add(BuildAuditEntry(relative, call));
        }

        current.Should().BeEquivalentTo(reviewed,
            "message changes need a Production spam review and a refreshed PlayerMessageAudit.json (tools/ExportPlayerMessageAudit.ps1)");
    }

    [Test]
    public void GameplayOutcomes_CannotBeHiddenBehindDiagnostics()
    {
        var root = FindRepositoryRoot();
        var files = new[] { "Service/Space.cs", "Service/Ability.cs", "Service/StatusEffect.cs",
            "Service/Faction.cs", "Service/Guild.cs", "Service/Skill.cs", "Service/BeastMastery.cs",
            "Service/QuestService/QuestObjectives.cs", "Feature/RoleplayXP.cs", "Service/Mimicry.cs",
            "Service/Fishing.cs", "Service/Weather.cs", "Feature/ScavengePoint.cs",
            "Feature/StatusEffectDefinition/GuardedStatusEffect.cs" }
            .Select(relative => Path.Combine(root, "SWLOR.Game.Server", relative)).ToList();
        files.AddRange(Directory.EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server", "Feature", "ShipModuleDefinition"), "*.cs"));

        var hiddenOutcomes = files.SelectMany(file => ReadCalls(file)
            .Where(call => MessageMethods.Contains(MethodName(call)) && UsesDiagnostics(call))
            .Select(call => Path.GetFileName(file) + ": " + call)).ToArray();
        hiddenOutcomes.Should().BeEmpty("combat, ship modules, status changes, and progression are gameplay feedback even when frequent");
    }

    [TestCase("SendGuardedHitFeedback", 2)]
    [TestCase("SendIncomingCriticalHitDowngradeFeedback", 2)]
    [TestCase("ApplyRangedDeflectionReflection", 1)]
    [TestCase("SendAbilityCriticalHitFeedback", 1)]
    [TestCase("SendTemporaryHitPointDamageFeedback", 1)]
    [TestCase("RefreshIdleReadiness", 2)]
    [TestCase("ApplyLowHPGuardEffect", 1)]
    [TestCase("ApplyGuardedHitNextSkillAbilityEffects", 1)]
    [TestCase("ApplyGuardedHitNextAttackEffects", 1)]
    [TestCase("EnsureFirstHostileAbilityHitState", 1)]
    [TestCase("ApplyFirstHostileAbilityHitCount", 1)]
    [TestCase("ReportFirstStrikeCombatEntry", 1)]
    [TestCase("ReadySameTargetPressure", 1)]
    [TestCase("ApplyStatusAppliedTargetStaminaDrain", 1)]
    public void CombatOutcomesAndReadiness_RetainTheirProductionMessages(string member, int expectedMessages)
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Service", "Combat.cs");
        var messages = ReadCalls(file).Where(call => call.Ancestors().OfType<MethodDeclarationSyntax>()
                .First().Identifier.ValueText == member &&
            MethodName(call) is "SendMessageToPC" or "FloatingTextStringOnCreature" or "SendMessageNearbyToPlayers")
            .ToArray();
        messages.Should().HaveCount(expectedMessages, "optional detail must not replace the actual combat outcome or readiness notice");
        messages.Should().NotContain(call => UsesDiagnostics(call));
    }

    private sealed record MessageAuditEntry(string File, string Member, string Delivery, string Call);

    private static MessageAuditEntry BuildAuditEntry(string file, InvocationExpressionSyntax call)
    {
        var name = MethodName(call);
        var member = call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? string.Empty;
        var delivery = name switch
        {
            "SendDiagnosticToPlayer" or "ShowDiagnosticFloatingText" or "SendResourceRestored"
                => "Testing only",
            "SendWarningToPlayer" => "Rate limited in Production",
            _ => call.Ancestors().OfType<IfStatementSyntax>()
                .Any(statement => statement.Condition.ToString() == "PlayerFeedback.DiagnosticsEnabled")
                ? "Testing only"
                : "Retained"
        };
        if ((file is "SWLOR.Game.Server/Service/PlayerFeedback.cs" or "SWLOR.Game.Server/Service/Messaging.cs" or
            "SWLOR.Game.Server/Service/Communication.cs" or "SWLOR.Game.Server/Service/Gui.cs") &&
            !name.Contains("Diagnostic", StringComparison.OrdinalIgnoreCase))
            delivery = "Shared transport; policy at caller";

        return new MessageAuditEntry(file, member, delivery, call.ToString().Replace("\r\n", "\n"));
    }

    [Test]
    public void SkillCapWarnings_ShareOneProductionLimitAcrossSkillsAndOutcomes()
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Service", "Skill.cs");
        var warnings = ReadCalls(file).Where(call => MethodName(call) == "SendWarningToPlayer").ToArray();
        warnings.Should().HaveCount(2);
        warnings.Select(call => call.ArgumentList.Arguments[1].Expression.ToString())
            .Should().OnlyContain(key => key == "\"SKILL_CAP\"",
                "the same global cap condition must not produce a burst of warnings when one kill awards several skills");
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
    public void MimicryFailures_RemainRateLimitedAndObservationsRemainVisible()
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Service", "Mimicry.cs");
        var calls = ReadCalls(file).ToArray();
        var failures = calls.Where(call => MethodName(call) == "SendWarningToPlayer").ToArray();
        failures.Should().HaveCount(2);
        failures.Should().Contain(call => call.ArgumentList.Arguments[1].ToString().Contains("MIMICRY_RANK_"));
        failures.Should().Contain(call => call.ArgumentList.Arguments[1].ToString().Contains("MIMICRY_DECODE_"));
        foreach (var failure in failures)
            failure.ArgumentList.Arguments.Should().Contain(argument => argument.ToString() == "intervalSeconds: 60");
        calls.Should().Contain(call => MethodName(call) == "SendMessageToPC" &&
            call.ToString().Contains("Your combat analyzer records"));
    }

    [Test]
    public void NativeAttackFeedback_RemainsVisibleInProduction()
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs");
        var calls = ReadCalls(file).Where(call => MethodName(call) == "SendFeedbackString").ToArray();
        calls.Should().HaveCount(5);
        foreach (var call in calls)
        {
            UsesDiagnostics(call).Should().BeFalse("hit/miss, immunity, and deflection are combat outcomes");
        }
    }

    [TestCase("TryUseAbility", 3)]
    [TestCase("InterruptAbilityActivation", 1)]
    [TestCase("DequeueWeaponAbility", 1)]
    public void AbilityStateChanges_RemainVisibleToNearbyPlayers(string member, int expectedMessages)
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs");
        var calls = ReadCalls(file).Where(call => call.Ancestors().OfType<MethodDeclarationSyntax>()
            .First().Identifier.ValueText == member).ToArray();
        var messages = calls.Where(call => MethodName(call) == "SendMessageNearbyToPlayers").ToArray();
        messages.Should().HaveCount(expectedMessages, "readiness and interruption are useful gameplay state changes");
        foreach (var message in messages)
        {
            message.ToString().Should().Contain("PlayerName.GetDisplayName(receiver,");
            message.Ancestors().OfType<IfStatementSyntax>().Should()
                .NotContain(statement => statement.Condition.ToString().Contains("DiagnosticsEnabled"));
        }
        calls.Should().NotContain(call => MethodName(call) == "SendMessageToPC",
            "the nearby helper already includes the actor, so a second private message would duplicate the notice");
    }

    private static IEnumerable<InvocationExpressionSyntax> ReadCalls(string file) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

    private static bool UsesDiagnostics(InvocationExpressionSyntax call) =>
        MethodName(call).Contains("Diagnostic", StringComparison.Ordinal) ||
        call.Ancestors().OfType<IfStatementSyntax>().Any(statement => statement.Condition.ToString().Contains("DiagnosticsEnabled")) ||
        call.Ancestors().OfType<ConditionalExpressionSyntax>().Any(expression => expression.Condition.ToString().Contains("DiagnosticsEnabled"));

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
