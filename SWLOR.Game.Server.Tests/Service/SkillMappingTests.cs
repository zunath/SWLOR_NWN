using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class SkillMappingTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Skill.LoadMappings();
    }

    [Test]
    public void GetSkillTypeByBaseItem_MapsPistolProjectileBaseItemsToPistol()
    {
        Skill.GetSkillTypeByBaseItem(BaseItem.Pistol).Should().Be(SkillType.Pistol);
        Skill.GetSkillTypeByBaseItem(BaseItem.Arrow).Should().Be(SkillType.Pistol);
        Skill.GetSkillTypeByBaseItem(BaseItem.Bullet).Should().Be(SkillType.Pistol);
        Skill.GetSkillTypeByBaseItem(BaseItem.Sling).Should().Be(SkillType.Pistol);
    }

    [Test]
    public void GetSkillTypeByBaseItem_MapsRifleProjectileBaseItemsToRifle()
    {
        Skill.GetSkillTypeByBaseItem(BaseItem.Rifle).Should().Be(SkillType.Rifle);
        Skill.GetSkillTypeByBaseItem(BaseItem.Cannon).Should().Be(SkillType.Rifle);
        Skill.GetSkillTypeByBaseItem(BaseItem.Longbow).Should().Be(SkillType.Rifle);
        Skill.GetSkillTypeByBaseItem(BaseItem.Bolt).Should().Be(SkillType.Rifle);
    }
}
