using SWLOR.Game.Server.Tests.Support;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponDeflectionTests
{
    // The code under test reaches the Stat facade, which resolves its instance from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [Test]
    public void WeaponDeflectionStats_HaveIndependentCapsAndSourceSpecificRiders()
    {
        StatService.DefaultMeleeDeflectionChanceCap.Should().Be(50);
        StatService.DefaultRangedDeflectionChanceCap.Should().Be(50);
        StatService.MaximumDeflectionChanceCap.Should().Be(100);
        Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflection).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflectionChanceCap).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.RangedDeflection).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.RangedDeflectionChanceCap).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.ShieldDeflection).Should().Be(DeflectionSource.Shield);
        Stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAttackDeflection).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.LightGuardianPowerAttackDeflection).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.StatusAppliedSelfAttackDeflection).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.SingleTargetAbilityAttackDeflection).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedAttackDeflection).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedAttackDeflectionFPRestore).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedPerkCategoryAttackDeflection).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestore).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionStaminaRestorePercent).Should().Be(DeflectionSource.None);
        Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflectionFPRestore).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflectionFPRestoreCooldownSeconds).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionFPRestore).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionFPRestoreCooldownSeconds).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionEnmityPercentAdjustment).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionRecastReductionSeconds).Should().Be(DeflectionSource.Shield);
        Stat.GetStatTypeDeflectionSource(StatType.DeflectionNearbyAllyGuard).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflectionStaminaRestore).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflectionStaminaRestoreCooldownSeconds).Should().Be(DeflectionSource.Melee);
        Stat.GetStatTypeDeflectionSource(StatType.ShieldDeflectionStaminaRestore).Should().Be(DeflectionSource.Shield);
        Stat.GetStatTypeDeflectionSource(StatType.ShieldDeflectionStaminaRestoreCooldownSeconds).Should().Be(DeflectionSource.Shield);
        Stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment).Should().Be(DeflectionSource.Ranged);
        Stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAfterDeflectionWindowSeconds).Should().Be(DeflectionSource.Ranged);
        Stat.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection).Should().Be(StatType.MeleeDeflection);
        Stat.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection).Should().Be(StatType.RangedDeflection);
        Stat.GetGrantedDeflectionStatType(StatType.ShieldDeflection).Should().Be(StatType.ShieldDeflection);
        Stat.GetGrantedDeflectionStatType(StatType.Attack).Should().Be(StatType.Invalid);
    }

    [Test]
    public void AttackRoll_RoutesOneAutoAttackDeflectionBySourceAndShieldPriority()
    {
        var source = ReadSource("SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs");

        source.Should().Contain("UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, attacker.m_pcCombatRound.m_nCurrentAttack)");
        source.Should().Contain("weaponSkillType == SkillType.Invalid");
        source.Should().Contain("GameServices.CombatDamageTaken.IsHostileAttackSource(defender.m_idSelf, attacker.m_idSelf)");
        source.Should().NotContain("!GetIsReactionTypeHostile(attacker.m_idSelf, defender.m_idSelf) ||");
        source.Should().Contain("var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);");
        source.Should().Contain("Stat.GetRangedDeflectionChanceNative(defender)");
        source.Should().Contain("Stat.GetMeleeDeflectionChanceNative(defender)");
        source.Should().Contain("Stat.ApplyDeflectionEffectsNative(defender, source)");
        source.Should().Contain("deflectionSource == DeflectionSource.Ranged");
        source.Should().Contain("CombatRules.GetDeflectionResultName(source)");
        source.Should().NotContain("private static string GetDeflectionName");

        var combatSource = CombatSource.Read();
        combatSource.Should().Contain("DeflectionSource.Melee => \"melee deflect\"");
        combatSource.Should().Contain("DeflectionSource.Ranged => \"ranged deflect\"");
        combatSource.Should().Contain("DeflectionSource.Shield => \"shield deflect\"");
        // The body lives in the damage taken part; the native attack roll reads it through GameServices.
        combatSource.Should().Contain("public static bool IsHostileAttackSource(uint defender, uint attacker)");
        typeof(SWLOR.Game.Server.Domain.Combat.ICombatDamageTakenService)
            .GetMethod("IsHostileAttackSource", new[] { typeof(uint), typeof(uint) })
            .Should().NotBeNull("Native/ResolveAttackRoll.cs calls GameServices.CombatDamageTaken.IsHostileAttackSource");
        combatSource.Should().Contain("GetIsPC(attacker) || GetIsDM(attacker) || GetIsDMPossessed(attacker)");
        combatSource.Should().Contain("var requiredSource = _stat.GetStatTypeDeflectionSource(");
        combatSource.Should().Contain("StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);");
        combatSource.Should().Contain("!HasRecentDeflection(attacker, requiredSource, window)");
        combatSource.Should().NotContain("HasRecentDeflection(attacker, DeflectionSource.Ranged, window)");

        source.IndexOf("var shieldDeflection = Stat.GetShieldDeflectionChanceNative(defender);", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("Stat.GetRangedDeflectionChanceNative(defender)", StringComparison.Ordinal));
    }

    [Test]
    public void DeflectionRiders_PreserveGenericStaminaRestoreAndFilterForceGyreToRanged()
    {
        var statSource = ReadSource("SWLOR.Game.Server", "Domain", "Character", "StatService.cs");
        var statRulesSource = ReadSource("SWLOR.Game.Server", "Domain", "Character", "StatRules.cs");
        var combatSource = CombatSource.Read();
        var abilitySource = ReadSource(
            "SWLOR.Game.Server", "Feature", "AbilityDefinition", "WeaponActiveAbilityDefinitionBase.cs");
        var forceSupportSource = ReadSource(
            "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Force", "LightGuardianPowerSupport.cs");
        var sentinelGuardSource = ReadSource(
            "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "SentinelGuardStatusEffect.cs");

        statSource.Should().Contain(
            "GetStatAdjustment(creatureId, StatType.DeflectionStaminaRestorePercent)");
        statSource.Should().NotContain(
            "GetDeflectionStatAdjustment(creatureId, StatType.DeflectionStaminaRestorePercent, source)");
        statSource.Should().Contain("GetDeflectionStatTypeForSource(");
        statSource.Should().NotContain("source == DeflectionSource.Shield");
        statSource.Should().Contain("StatType.MeleeDeflectionFPRestore");
        statSource.Should().Contain("StatType.MeleeDeflectionFPRestoreCooldownSeconds");
        statSource.Should().Contain("var fpRestoreCooldown = fpRestoreCooldownStat != StatType.Invalid");
        statSource.Should().NotContain(
            "var fpRestoreCooldown = GetStatAdjustment(creatureId, StatType.DeflectionFPRestoreCooldownSeconds)");
        statSource.Should().Contain("TryUseStatTrigger(creatureId, fpRestoreStat, fpRestoreCooldown)");
        combatSource.Should().NotContain(
            "ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Melee)");
        combatSource.Should().Contain(
            "ApplyAbilityGrantedAttackDeflectionEffects(activator, source)");
        combatSource.Should().Contain(
            "_stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestore) != source");
        combatSource.Should().Contain("StatType.MeleeDeflectionFPRestore");
        combatSource.Should().Contain("var source = _stat.GetStatTypeDeflectionSource(attackDeflectionStatType);");
        statRulesSource.Should().Contain("public static StatType GetGrantedDeflectionStatType(StatType sourceStatType)");
        statRulesSource.Should().Contain("DeflectionSource.Melee => StatType.MeleeDeflection");
        statRulesSource.Should().Contain("DeflectionSource.Ranged => StatType.RangedDeflection");
        statRulesSource.Should().Contain("DeflectionSource.Shield => StatType.ShieldDeflection");
        combatSource.Should().Contain("_stat.GetGrantedDeflectionStatType(attackDeflectionStatType)");
        combatSource.Should().MatchRegex(
            @"_temporaryStatModifier\.Replace\(\s*activator,\s*targetStatType,\s*attackDeflection,\s*duration,\s*attackDeflectionStatType\);");
        combatSource.Should().Contain("_stat.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection)");
        combatSource.Should().Contain("_stat.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection)");
        combatSource.Should().Contain("_stat.GetGrantedDeflectionStatType(sourceStatType)");
        combatSource.Should().Contain("var fpRestoreSource = _stat.GetStatTypeDeflectionSource(deflectionFPRestoreStatType);");
        combatSource.Should().Contain("fpRestore > 0 && fpRestoreSource == source");
        combatSource.Should().Contain("ApplyAbilityGrantedAttackDeflectionEffects(activator, source)");
        abilitySource.Should().Contain(
            "GameServices.CombatDamageTaken.ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Melee)");
        abilitySource.Should().Contain(
            "GameServices.CombatDamageTaken.ApplyAbilityGrantedAttackDeflectionEffects(activator, DeflectionSource.Ranged)");
        forceSupportSource.Should().Contain("Stat.GetGrantedDeflectionStatType(sourceStatType)");
        sentinelGuardSource.Should().Contain("Stat.GetGrantedDeflectionStatType(");
        sentinelGuardSource.Should().Contain("StatGroup.Stats[_deflectionStatType]");
        sentinelGuardSource.Should().Contain("private readonly int _deflection;");
        sentinelGuardSource.Should().NotContain("_attackDeflection");
    }

    [Test]
    public void WeaponGenerator_ParsesSplitDeflectionWordingWithoutLegacyStatOutput()
    {
        var generator = ReadSource("tools", "GenerateWeaponArchetypeImplementation.py");

        generator.Should().Contain("DEFLECTION_NAME_PATTERN = r\"(?:Melee|Ranged|Attack) Deflection\"");
        generator.Should().Contain("parse_deflection_count(description)");
        generator.Should().Contain("add_stat(stats, \"MeleeDeflection\"");
        generator.Should().Contain("add_stat(stats, \"RangedDeflection\"");
        generator.Should().Contain("(\"SelfMeleeDeflection\"");
        generator.Should().Contain("(\"SelfRangedDeflection\"");
        generator.Should().Contain("(?:Ranged|Attack) Deflection");
        generator.Should().Contain(
            "add_stat(stats, \"RangedDeflection\", parse_count(r\"\\+(\\d+) (?:Ranged|Attack) Deflection\", description))");
        generator.Should().NotContain("(\"SelfAttackDeflection\"");
        generator.Should().NotContain("add_stat(stats, \"AttackDeflection\"");
        generator.Should().NotContain("parse_count(r\"\\+(\\d+) Attack Deflection\", description)");
    }

    [Test]
    public void UnmovingCenter_UsesItsStatusForOneMeleeDeflectionGrant()
    {
        var ability = ReadSource(
            "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Staff", "UnmovingCenterAbilityDefinition.cs");
        var status = ReadSource(
            "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "UnmovingCenterStatusEffect.cs");

        ability.Should().NotContain("SelfMeleeDeflection");
        status.Should().Contain("StatGroup.Stats[StatType.MeleeDeflection] = 20;");
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
        guide.Should().Contain("Each incoming weapon auto-attack gets its own deflection attempt");
    }

    [Test]
    public void DeflectingReturn_ReportsNamedReflectionDamage()
    {
        var source = CombatSource.Read();

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
