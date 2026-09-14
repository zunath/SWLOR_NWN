using System.Text.Json;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Tools;

namespace SWLOR.Game.Server.Tests.Service;

public class PlayerMessageAuditTests
{
    private static readonly HashSet<string> MessageMethods = new(StringComparer.Ordinal)
    {
        "SendMessageToPC", "FloatingTextStringOnCreature", "FloatingTextStrRefOnCreature",
        "SendMessageToPCByStrRef", "SendMessageToAllPCs", "SendMessageNearbyToPlayers",
        "SendFeedbackString", "SendFeedbackMessage", "SendMessage", "PostString",
        "SpeakString", "ActionSpeakString", "SendDiagnosticToPlayer", "ShowDiagnosticFloatingText",
        "SendResourceRestored", "SendWarningToPlayer", "SendWarningNearby"
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

    [TestCase("Service/Ability.cs", "SendCombatImpactNoTargetsMessage")]
    [TestCase("Feature/AbilityDefinition/Force/ThrowLightsaberAbilityDefinition.cs", "ApplyThrowLightsaber")]
    public void NoTargetOutcomes_ReachTheActorAndNearbyObservers(string relative, string member)
    {
        var file = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", relative);
        var messages = ReadCalls(file).Where(call => MethodName(call) == "SendMessageNearbyToPlayers" &&
            call.Ancestors().OfType<MethodDeclarationSyntax>().First().Identifier.ValueText == member).ToArray();
        messages.Should().ContainSingle();
        var message = messages.Single();
        message.ArgumentList.Arguments[0].ToString().Should().Be("activator");
        message.ArgumentList.Arguments[1].ToString().Should().Contain("Combat.BuildAbilityNoTargetCombatLogMessage");
        message.ArgumentList.Arguments[2].ToString().Should().Be("60f");
        UsesDiagnostics(message).Should().BeFalse();
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
            "SendWarningToPlayer" or "SendWarningNearby" => "Rate limited in Production",
            _ => PlayerMessagePolicy.IsDiagnosticOnly(call)
                ? "Testing only"
                : "Retained"
        };
        if ((file is "SWLOR.Game.Server/Service/PlayerFeedback.cs" or "SWLOR.Game.Server/Service/Messaging.cs" or
            "SWLOR.Game.Server/Service/Communication.cs" or "SWLOR.Game.Server/Service/Gui.cs") &&
            !name.Contains("Diagnostic", StringComparison.OrdinalIgnoreCase))
            delivery = "Shared transport; policy at caller";

        return new MessageAuditEntry(file, member, delivery, call.ToString().Replace("\r\n", "\n"));
    }

    [TestCase("if ((PlayerFeedback.DiagnosticsEnabled)) SendMessageToPC();", true)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled && ready) SendMessageToPC();", true)]
    [TestCase("if (ready && (PlayerFeedback.DiagnosticsEnabled)) SendMessageToPC();", true)]
    [TestCase("if ((PlayerFeedback.DiagnosticsEnabled && ready) || (PlayerFeedback.DiagnosticsEnabled && other)) SendMessageToPC();", true)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled || ready) SendMessageToPC();", false)]
    [TestCase("if (!PlayerFeedback.DiagnosticsEnabled) SendMessageToPC();", false)]
    [TestCase("if (!PlayerFeedback.DiagnosticsEnabled) {} else SendMessageToPC();", true)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled && ready) {} else SendMessageToPC();", false)]
    [TestCase("if (!PlayerFeedback.DiagnosticsEnabled || ready) {} else SendMessageToPC();", true)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled == true) SendMessageToPC();", true)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled != false) SendMessageToPC();", true)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled == false) SendMessageToPC();", false)]
    [TestCase("var x = PlayerFeedback.DiagnosticsEnabled ? SendMessageToPC() : 0;", true)]
    [TestCase("var x = PlayerFeedback.DiagnosticsEnabled ? 0 : SendMessageToPC();", false)]
    [TestCase("var x = !PlayerFeedback.DiagnosticsEnabled ? 0 : SendMessageToPC();", true)]
    [TestCase("SendMessageToPC(PlayerFeedback.DiagnosticsEnabled ? detail : concise);", false)]
    [TestCase("if (SendMessageToPC() && PlayerFeedback.DiagnosticsEnabled) {}", false)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled && SendMessageToPC()) {}", true)]
    [TestCase("if (!PlayerFeedback.DiagnosticsEnabled || SendMessageToPC()) {}", true)]
    [TestCase("if (PlayerFeedback.DiagnosticsEnabled) Register(() => SendMessageToPC());", false)]
    [TestCase("Register(() => { if (PlayerFeedback.DiagnosticsEnabled) SendMessageToPC(); });", true)]
    [TestCase("if (global::SWLOR.Game.Server.Service.PlayerFeedback.DiagnosticsEnabled) SendMessageToPC();", true)]
    [TestCase("if (unrelated.DiagnosticsEnabled) SendMessageToPC();", false)]
    public void MessageInventory_ClassifiesOnlyBranchesThatRequireDiagnostics(string body, bool diagnosticOnly)
    {
        var call = CSharpSyntaxTree.ParseText("class Example { void ExampleMethod() { " + body + " } }")
            .GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(node => MethodName(node) == "SendMessageToPC");
        BuildAuditEntry("Example.cs", call).Delivery.Should().Be(diagnosticOnly ? "Testing only" : "Retained");
        UsesDiagnostics(call).Should().Be(diagnosticOnly);
    }

    [TestCase("SendDiagnosticToPlayer")]
    [TestCase("ShowDiagnosticFloatingText")]
    [TestCase("SendResourceRestored")]
    public void DiagnosticWrappers_AreClassifiedConsistentlyWithoutCallerGuards(string method)
    {
        var call = CSharpSyntaxTree.ParseText("class Example { void ExampleMethod() { PlayerFeedback." + method + "(); } }")
            .GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        BuildAuditEntry("Example.cs", call).Delivery.Should().Be("Testing only");
        UsesDiagnostics(call).Should().BeTrue();
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

    [Test]
    public void ParalysisWarnings_ReachNearbyObserversForPcAndNpcAttackers()
    {
        var root = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Service");
        var warning = ReadCalls(Path.Combine(root, "Combat.cs"))
            .Single(call => MethodName(call) == "SendWarningNearby" &&
                call.Ancestors().OfType<MethodDeclarationSyntax>().First().Identifier.ValueText == "HandleParalyze");
        warning.ArgumentList.Arguments[0].ToString().Should().Be("attacker");
        warning.ArgumentList.Arguments[2].ToString().Should().Contain("PlayerName.GetDisplayName(receiver, attacker)");
        warning.ArgumentList.Arguments[3].ToString().Should().Be("5");

        var delivery = ReadCalls(Path.Combine(root, "PlayerFeedback.cs"))
            .Where(call => call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText == "SendWarningNearby")
            .ToArray();
        delivery.Should().Contain(call => MethodName(call) == "SendMessageNearbyToPlayers");
        delivery.Should().Contain(call => MethodName(call) == "TryBeginWarning");
        delivery.Should().NotContain(call => MethodName(call) == "GetIsPC", "NPC state is also important to nearby players");
    }

    private static IEnumerable<InvocationExpressionSyntax> ReadCalls(string file) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

    private static bool UsesDiagnostics(InvocationExpressionSyntax call) =>
        MethodName(call).Contains("Diagnostic", StringComparison.Ordinal) ||
        MethodName(call) == "SendResourceRestored" || PlayerMessagePolicy.IsDiagnosticOnly(call);

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
