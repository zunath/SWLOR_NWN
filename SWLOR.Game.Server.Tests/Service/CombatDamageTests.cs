using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
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
}
