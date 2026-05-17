using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using NativeDamageType = NWN.Native.API.DamageType;
using NWNScriptDamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatDamageTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Environment.SetEnvironmentVariable(
            "SWLOR_APP_LOG_DIRECTORY",
            Path.Combine(TestContext.CurrentContext.WorkDirectory, "logs") + Path.DirectorySeparatorChar);
        Log.Register();
    }

    [Test]
    public void CalculateDamageRange_FloorsPositiveDmgHitsAtOne()
    {
        var (minDamage, maxDamage) = Combat.CalculateDamageRange(
            attackerAttack: 1,
            attackerDMG: 18,
            attackerStat: 10,
            defenderDefense: 100000,
            defenderStat: 10,
            critical: 0);

        minDamage.Should().Be(1);
        maxDamage.Should().Be(1);
    }

    [Test]
    public void CalculateDamageRange_PreservesZeroDmgImpacts()
    {
        var (minDamage, maxDamage) = Combat.CalculateDamageRange(
            attackerAttack: 1,
            attackerDMG: 0,
            attackerStat: 10,
            defenderDefense: 100000,
            defenderStat: 10,
            critical: 0);

        minDamage.Should().Be(0);
        maxDamage.Should().Be(0);
    }

    [Test]
    public void ForceDamage_UsesForceCombatMetadataAndMagicEnginePayload()
    {
        var forceDamage = CombatDamageType.Force.GetDetails();

        forceDamage.Category.Should().Be(CombatDamageCategoryType.Force);
        forceDamage.DefenseDamageType.Should().Be(CombatDamageType.Force);
        forceDamage.SourceResistanceType.Should().Be(ResistanceType.Disruption);
        forceDamage.NWScriptDamageType.Should().Be(NWNScriptDamageType.Force);
        forceDamage.NativeDamageType.Should().Be(NativeDamageType.Magical);
    }

    [Test]
    public void PhysicalDamage_UsesPhysicalCombatMetadataAndSlashingEnginePayload()
    {
        var physicalDamage = CombatDamageType.Physical.GetDetails();

        physicalDamage.Category.Should().Be(CombatDamageCategoryType.Physical);
        physicalDamage.DefenseDamageType.Should().Be(CombatDamageType.Physical);
        physicalDamage.SourceResistanceType.Should().Be(ResistanceType.Trauma);
        physicalDamage.NWScriptDamageType.Should().Be(NWNScriptDamageType.Slashing);
        physicalDamage.NativeDamageType.Should().Be(NativeDamageType.Slashing);
    }

    [Test]
    public void AbilityCombatImpact_IncludesWeaponDamageAndKeepsPhysicalEffectDamagePhysical()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));
        var damageTypeSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "CombatService", "CombatDamageType.cs"));

        abilitySource.Should().Contain("Combat.GetCombatImpactWeaponDamage(activator, skillType)");
        abilitySource.Should().Contain("effectDamageType ?? damageType.GetNWScriptDamageType()");
        abilitySource.Should().NotContain("GetNWScriptDamagePower");
        abilitySource.Should().NotContain("GetCombatImpactEffectDamagePower");
        abilitySource.Should().NotContain("private static int GetCombatImpactWeaponDamage");
        damageTypeSource.Should().NotContain("GetNWScriptDamagePower");
        damageTypeSource.Should().NotContain("DamagePower");
    }

    [Test]
    public void GuardedHitModifiers_OnlyRunForPhysicalDamageFromDamageRoll()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var damageRollSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));

        combatSource.Should().Contain("ApplyGuardedHitModifiers(uint defender, uint attacker, int damage, CombatDamageType damageType)");
        combatSource.Should().Contain("!damageType.IsPhysicalDamageType()");
        damageRollSource.Should().Contain("Combat.ApplyGuardedHitModifiers(target.m_idSelf, attacker.m_idSelf, damage, damageType);");
        damageRollSource.Should().NotContain("Combat.ApplyGuardedHitModifiers(target.m_idSelf, attacker.m_idSelf, damage);");
    }

    [Test]
    public void NormalDamageMitigation_IsCappedSeparatelyFromExplicitImmunity()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var invincibleSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "InvincibleStatusEffect.cs"));

        combatSource.Should().Contain("MaximumNormalDamageReductionPercent = 95");
        combatSource.Should().Contain("Math.Max(adjustment, -MaximumNormalDamageReductionPercent)");
        combatSource.Should().Contain("HasDamageImmunity(defender, damageType)");
        invincibleSource.Should().Contain("StatType.PhysicalDamageImmunity");
        invincibleSource.Should().NotContain("PhysicalDamageTakenPercentAdjustment] = -100");
    }

    [Test]
    public void IsWeaponSkillType_UsesSkillCombatPointMetadata()
    {
        Combat.IsWeaponSkillType(SkillType.Lightsaber).Should().BeTrue();
        Combat.IsWeaponSkillType(SkillType.Rifle).Should().BeTrue();
        Combat.IsWeaponSkillType(SkillType.Force).Should().BeFalse();
        Combat.IsWeaponSkillType(SkillType.Devices).Should().BeFalse();
        Combat.IsWeaponSkillType(SkillType.Invalid).Should().BeFalse();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
