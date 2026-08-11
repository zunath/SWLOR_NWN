using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponDeflectionTests
{
    [Test]
    public void WeaponDeflectionStats_HaveIndependentCapsAndSourceSpecificRiders()
    {
        Stat.DefaultMeleeDeflectionChanceCap.Should().Be(50);
        Stat.DefaultRangedDeflectionChanceCap.Should().Be(50);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionFPRestore).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionEnmityPercentAdjustment).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionRecastReductionSeconds).Should().Be(DeflectionSource.Shield);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionNearbyAllyGuard).Should().Be(DeflectionSource.Melee);
    }

    [Test]
    public void AttackRoll_RoutesOneAutoAttackDeflectionBySourceAndShieldPriority()
    {
        var source = ReadSource("SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs");

        source.Should().Contain("UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType)");
        source.Should().Contain("GetIsReactionTypeHostile(attacker.m_idSelf, defender.m_idSelf)");
        source.Should().Contain("var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);");
        source.Should().Contain("Stat.GetRangedDeflectionChanceNative(defender)");
        source.Should().Contain("Stat.GetMeleeDeflectionChanceNative(defender)");
        source.Should().Contain("Stat.ApplyDeflectionEffectsNative(defender, source)");
        source.Should().Contain("deflectionSource == DeflectionSource.Ranged");
        source.Should().Contain("DeflectionSource.Melee => \"melee deflect\"");
        source.Should().Contain("DeflectionSource.Ranged => \"ranged deflect\"");
        source.Should().Contain("DeflectionSource.Shield => \"shield deflect\"");

        source.IndexOf("var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("Stat.GetRangedDeflectionChanceNative(defender)", StringComparison.Ordinal));
    }

    [Test]
    public void CharacterSheetAndGuide_ExplainSplitScopeAndReplacementRule()
    {
        var sheet = ReadSource(
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "CharacterSheetViewModel.cs");
        var guide = ReadSource(
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "PlayerGuideViewModel.cs");

        sheet.Should().Contain("AddStat(\"Melee Deflection\"");
        sheet.Should().Contain("AddStat(\"Ranged Deflection\"");
        sheet.Should().Contain("Shield Deflection replaces weapon deflection");
        guide.Should().Contain("Deflection does not work against activated combat abilities or Force powers");
        guide.Should().Contain("only one deflection attempt can occur in an incoming combat round");
    }

    [Test]
    public void DeflectingReturn_ReportsNamedReflectionDamage()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "Combat.cs");

        source.Should().Contain("BuildDeflectingReturnCombatLogMessage");
        source.Should().Contain("'s Deflecting Return reflects {damage} Force damage to");
    }

    private static string ReadSource(params string[] parts)
    {
        return File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot().FullName }.Concat(parts).ToArray()));
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
