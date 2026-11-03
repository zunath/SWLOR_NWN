using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class BareHandedAttackSkillTests
{
    [OneTimeSetUp]
    public void LoadMappings() => Skill.LoadMappings();

    // The engine attacks with arms-slot gloves or bracers when no weapon is in hand.
    [TestCase(BaseItem.Gloves)]
    [TestCase(BaseItem.Bracer)]
    public void BareHandedAttacksTrainUnarmed(BaseItem armsItem)
    {
        Skill.GetAttackSkillTypeByBaseItem(armsItem).Should().Be(SkillType.Unarmed);
    }

    [TestCase(BaseItem.Katar, SkillType.Katar)]
    [TestCase(BaseItem.PowerGlove, SkillType.Unarmed)]
    [TestCase(BaseItem.Longsword, SkillType.Vibroblade)]
    public void WeaponsKeepTheirMappedAttackSkill(BaseItem weapon, SkillType expected)
    {
        Skill.GetAttackSkillTypeByBaseItem(weapon).Should().Be(expected);
    }

    [Test]
    public void ArmsItemRequirementSkillIsUnchanged()
    {
        // Stored requirements and legacy requirement migrations still read the base-item mapping.
        Skill.GetSkillTypeByBaseItem(BaseItem.Gloves).Should().Be(SkillType.Katar);
        Skill.GetSkillTypeByBaseItem(BaseItem.Bracer).Should().Be(SkillType.Katar);
    }

    [Test]
    public void ItemlessBareHandedHitsCreditUnarmed()
    {
        // With no weapon, arms item, or natural weapon no item on-hit event fires, so the landed
        // native damage roll must award the Unarmed combat point itself.
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Native", "GetDamageRoll.cs"));
        var landed = source[source.IndexOf("if (isLandedAttack)", System.StringComparison.Ordinal)..];

        source.Should().Contain("weapon = GetFallbackAttackWeapon(attacker);",
            "a null weapon after the fallback means no arms item or natural weapon is equipped");
        landed.Should().Contain("if (weapon == null && pAttackData.m_bRangedAttack == 0 && creatureTarget)");
        landed.Should().Contain(
            "CombatPoint.RecordWeaponAttackPoint(attackerId, defenderId, OBJECT_INVALID, SkillType.Unarmed);");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
