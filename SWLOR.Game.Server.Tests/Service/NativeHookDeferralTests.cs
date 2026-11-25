using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Native combat hooks (GetDamageRoll, ResolveAttackRoll, AIActionAttackObject) run managed code in a script context
/// whose OBJECT_SELF is invalid. A bare DelayCommand there is refused by NWNX and silently dropped, so deferrals these
/// hooks reach must be owned by the module.
/// </summary>
public class NativeHookDeferralTests
{
    [TestCase("Service/Combat.cs", "ApplyAutoAttackSplashEffects",
        TestName = "AutoAttackSplash_LandsOnARealAutoAttack")]
    [TestCase("Service/CombatService/TemporaryStatModifier.cs", "PublishRefresh",
        TestName = "StatWindow_RefreshesAfterOnHitModifiers")]
    [TestCase("Feature/StatusEffectDefinition/InspiredStatusEffect.cs", "TryStandingOvation",
        TestName = "StandingOvation_HealsAfterANativeWeaponHit")]
    [TestCase("Service/AI.cs", "StartLeashEvade",
        TestName = "LeashEvade_ReassertsItsMovementRateWhenStartedFromAnAttack")]
    [TestCase("Service/StatusEffect.cs", "RemoveStatusEffectInstance",
        TestName = "StatusRemoval_RecomputesMovementRateInsideAnAttack")]
    [TestCase("Service/Stealth.cs", "ExitDetectedPlayerStealth",
        TestName = "StealthExit_ResyncsAfterDetectionDuringAnAttack")]
    [TestCase("Service/EspionageInfiltration.cs", "StartMovementSampler",
        TestName = "InfiltrationSampler_StartsWhenBegunDuringAnAttack")]
    public void Deferral_IsOwnedByTheModule(string relativePath, string methodName)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var file = Path.Combine(directory!.FullName, "SWLOR.Game.Server", relativePath);
        var method = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.Text == methodName);
        var delays = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString() == "DelayCommand").ToArray();
        delays.Should().NotBeEmpty();

        foreach (var delay in delays)
            delay.Ancestors().OfType<InvocationExpressionSyntax>()
                .Should().Contain(call => call.Expression.ToString() == "AssignCommand" &&
                                          call.ArgumentList.Arguments.First().ToString() == "GetModule()",
                    $"{methodName} is reached from native combat hooks, where a bare DelayCommand is dropped");
    }
}
