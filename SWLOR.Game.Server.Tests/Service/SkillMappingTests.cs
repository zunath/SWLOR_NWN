using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class SkillMappingTests
{
    // The item groups are the item service's, which the Item facade resolves from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _facades = new FacadeContainer();
        Skill.LoadMappings();
    }

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [Test]
    public void GetSkillTypeByBaseItem_MapsPistolProjectileBaseItemsToPistol()
    {
        Skill.GetSkillTypeByBaseItem(BaseItem.Pistol).Should().Be(SkillType.Pistol);
        Skill.GetSkillTypeByBaseItem(BaseItem.LegacyPistol).Should().Be(SkillType.Pistol);
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

    [Test]
    public void ForceSensitiveWeapons_RequireForceSensitiveCharacterTypeToEquip()
    {
        Item.ForceSensitiveWeaponBaseItemTypes.Should().BeEquivalentTo(
            Item.LightsaberBaseItemTypes.Concat(Item.SaberstaffBaseItemTypes));

        SkillType.Lightsaber
            .GetAttribute<SkillType, SkillAttribute>()
            .CharacterTypeRestriction
            .Should()
            .Be(CharacterType.ForceSensitive);

        SkillType.Saberstaff
            .GetAttribute<SkillType, SkillAttribute>()
            .CharacterTypeRestriction
            .Should()
            .Be(CharacterType.ForceSensitive);
    }
}
