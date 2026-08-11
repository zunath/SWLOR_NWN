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
        Stat.MaximumDeflectionChanceCap.Should().Be(100);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestore).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionStaminaRestorePercent).Should().Be(DeflectionSource.None);
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
        source.Should().Contain("Combat.IsHostileAttackSource(defender.m_idSelf, attacker.m_idSelf)");
        source.Should().NotContain("!GetIsReactionTypeHostile(attacker.m_idSelf, defender.m_idSelf) ||");
        source.Should().Contain("attacker.m_ScriptVars.SetInt(new CExoString(DeflectionAttemptedVariable), 0)");
        source.Should().Contain("attacker.m_ScriptVars.GetInt(new CExoString(DeflectionAttemptedVariable))");
        source.Should().Contain("attacker.m_ScriptVars.SetInt(new CExoString(DeflectionAttemptedVariable), 1)");
        source.Should().NotContain("defender.m_ScriptVars.SetInt(new CExoString(DeflectionAttemptedVariable)");
        source.Should().Contain("var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);");
        source.Should().Contain("Stat.GetRangedDeflectionChanceNative(defender)");
        source.Should().Contain("Stat.GetMeleeDeflectionChanceNative(defender)");
        source.Should().Contain("Stat.ApplyDeflectionEffectsNative(defender, source)");
        source.Should().Contain("deflectionSource == DeflectionSource.Ranged");
        source.Should().Contain("Combat.GetDeflectionResultName(source)");
        source.Should().NotContain("private static string GetDeflectionName");

        var combatSource = ReadSource("SWLOR.Game.Server", "Service", "Combat.cs");
        combatSource.Should().Contain("DeflectionSource.Melee => \"melee deflect\"");
        combatSource.Should().Contain("DeflectionSource.Ranged => \"ranged deflect\"");
        combatSource.Should().Contain("DeflectionSource.Shield => \"shield deflect\"");
        combatSource.Should().Contain("internal static bool IsHostileAttackSource(uint defender, uint attacker)");
        combatSource.Should().Contain("GetIsPC(attacker) || GetIsDM(attacker) || GetIsDMPossessed(attacker)");

        source.IndexOf("var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("Stat.GetRangedDeflectionChanceNative(defender)", StringComparison.Ordinal));
    }

    [Test]
    public void DeflectionRiders_PreserveGenericStaminaRestoreAndFilterForceGyreToRanged()
    {
        var statSource = ReadSource("SWLOR.Game.Server", "Service", "Stat.cs");
        var combatSource = ReadSource("SWLOR.Game.Server", "Service", "Combat.cs");
        var abilitySource = ReadSource(
            "SWLOR.Game.Server", "Feature", "AbilityDefinition", "WeaponActiveAbilityDefinitionBase.cs");

        statSource.Should().Contain(
            "GetStatAdjustment(creatureId, StatType.DeflectionStaminaRestorePercent)");
        statSource.Should().NotContain(
            "GetDeflectionStatAdjustment(creatureId, StatType.DeflectionStaminaRestorePercent, source)");
        combatSource.Should().Contain(
            "ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Melee)");
        combatSource.Should().Contain(
            "ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Ranged)");
        combatSource.Should().Contain(
            "Stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestore) != source");
        abilitySource.Should().Contain(
            "Combat.ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Ranged)");
    }

    [Test]
    public void WeaponGenerator_ParsesSplitDeflectionWordingWithoutLegacyStatOutput()
    {
        var generator = ReadSource("tools", "GenerateWeaponArchetypeImplementation.py");

        generator.Should().Contain("DEFLECTION_NAME_PATTERN = r\"(?:Melee|Ranged|Attack) Deflection\"");
        generator.Should().Contain("parse_deflection_count(description)");
        generator.Should().Contain("add_stat(stats, \"MeleeDeflection\"");
        generator.Should().Contain("add_stat(stats, \"RangedDeflection\"");
        generator.Should().NotContain("add_stat(stats, \"AttackDeflection\"");
        generator.Should().NotContain("parse_count(r\"\\+(\\d+) Attack Deflection\", description)");
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
