using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class MeleeRepeatedTargetStackTests
{
    private const uint FirstTarget = 0x100;
    private const uint SecondTarget = 0x200;

    // Rundown I-III: +1/+2/+3 DMG per stack, capped at +5/+10/+15 (five stacks).
    [TestCase(1, 5)]
    [TestCase(2, 10)]
    [TestCase(3, 15)]
    public void RundownText_ShowsOnEachBuildingHit_ThenStaysSilentAtCap_AndRestartsOnNewTarget(int bonusPerHit, int maxBonus)
    {
        var maxStacks = (int)Math.Ceiling(maxBonus / (float)bonusPerHit);
        var stack = new MeleeRepeatedTargetStack(FirstTarget);

        HitTexts(stack, FirstTarget, 5, maxStacks, bonusPerHit, maxBonus).Should().Equal(
            $"Rundown 1/5 (+{bonusPerHit} DMG)",
            $"Rundown 2/5 (+{bonusPerHit * 2} DMG)",
            $"Rundown 3/5 (+{bonusPerHit * 3} DMG)",
            $"Rundown 4/5 (+{bonusPerHit * 4} DMG)",
            $"Rundown 5/5 (+{maxBonus} DMG)");

        HitTexts(stack, FirstTarget, 4, maxStacks, bonusPerHit, maxBonus).Should().AllBe(null,
            "hits at the cap keep the bonus but show no text");
        stack.Stacks.Should().Be(5);

        HitTexts(stack, SecondTarget, 2, maxStacks, bonusPerHit, maxBonus).Should().Equal(
            $"Rundown 1/5 (+{bonusPerHit} DMG)",
            $"Rundown 2/5 (+{bonusPerHit * 2} DMG)");

        HitTexts(stack, FirstTarget, 1, maxStacks, bonusPerHit, maxBonus).Should().Equal(
            $"Rundown 1/5 (+{bonusPerHit} DMG)");
    }

    [Test]
    public void SingleStackCap_ShowsTextOnlyOnTheFirstHit()
    {
        var stack = new MeleeRepeatedTargetStack(FirstTarget);

        stack.RegisterHit(FirstTarget, 1).Should().BeTrue();
        stack.RegisterHit(FirstTarget, 1).Should().BeFalse();
        stack.Stacks.Should().Be(1);
    }

    [Test]
    public void StackText_UsesANeutralNameForAnyIconOtherThanRundown()
    {
        MeleeRepeatedTargetStack.GetStackText(EffectIconType.RundownStatusEffect, 3, 5, 3)
            .Should().Be("Rundown 3/5 (+3 DMG)");
        MeleeRepeatedTargetStack.GetStackText(EffectIconType.Invalid, 3, 5, 3)
            .Should().Be("Repeated Target 3/5 (+3 DMG)");
    }

    [Test]
    public void Combat_FloatsRundownTextOnlyForPlayersWhenTheStackRises()
    {
        var root = FindRepositoryRoot();
        var combat = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "Combat.cs")).Replace("\r\n", "\n");
        var start = combat.IndexOf("private static int ApplyMeleeRepeatedTargetDamageModifier(", StringComparison.Ordinal);
        var end = combat.IndexOf("private static int ApplyRangedRepeatedTargetDamageModifier(", start, StringComparison.Ordinal);
        var body = combat.Substring(start, end - start);

        body.Should().Contain("var stackRaised = state.RegisterHit(defender, maxStacks);");
        body.Should().Contain("if (stackRaised && GetIsPC(attacker))");
        body.Should().Contain("MeleeRepeatedTargetStack.GetStackText(statusEffectIcon, state.Stacks, maxStacks, stackBonus)");
    }

    private static List<string> HitTexts(
        MeleeRepeatedTargetStack stack,
        uint target,
        int hits,
        int maxStacks,
        int bonusPerHit,
        int maxBonus)
    {
        var texts = new List<string>();
        for (var hit = 0; hit < hits; hit++)
        {
            var raised = stack.RegisterHit(target, maxStacks);
            var bonus = Math.Min(maxBonus, stack.Stacks * bonusPerHit);
            texts.Add(raised
                ? MeleeRepeatedTargetStack.GetStackText(EffectIconType.RundownStatusEffect, stack.Stacks, maxStacks, bonus)
                : null);
        }

        return texts;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server", "Service", "Combat.cs")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root contains SWLOR.Game.Server");
        return directory!.FullName;
    }
}
