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
}
