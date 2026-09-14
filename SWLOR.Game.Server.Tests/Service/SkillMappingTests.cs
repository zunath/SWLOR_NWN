using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
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

        var root = FindRepositoryRoot();
        var itemSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Item.cs"));
        var normalizedItemSource = itemSource.Replace("\r\n", "\n");

        itemSource.Should().Contain("ForceSensitiveWeaponBaseItemTypes.Contains(itemType)");
        itemSource.Should().Contain("GetIsDM(creature)");
        itemSource.Should().Contain("GetIsDMPossessed(creature)");
        normalizedItemSource.Should().Contain("if (GetIsDM(creature) || GetIsDMPossessed(creature))\n                return true;");
        itemSource.Should().Contain("Droid.IsDroid(creature)");
        itemSource.Should().Contain("BeastMastery.GetBeastType(creature) != BeastType.Invalid");
        normalizedItemSource.Should().Contain("if (!GetIsPC(creature))\n                return true;");
        itemSource.Should().Contain("DB.Get<Player>(playerId)");
        itemSource.Should().Contain("dbPlayer?.CharacterType == CharacterType.ForceSensitive");
        itemSource.Should().NotContain("GetClassByPosition(1, creature)");

        var forceSensitiveGateIndex = itemSource.IndexOf("ForceSensitiveWeaponBaseItemTypes.Contains(itemType)", StringComparison.Ordinal);
        var nonPlayerBypassIndex = itemSource.IndexOf("if ((!isPlayer && !isDroid)", StringComparison.Ordinal);
        forceSensitiveGateIndex.Should().BeLessThan(nonPlayerBypassIndex);

        var dmBypassIndex = normalizedItemSource.IndexOf("if (GetIsDM(creature) || GetIsDMPossessed(creature))\n                return true;", StringComparison.Ordinal);
        var playerEntityLookupIndex = normalizedItemSource.IndexOf("DB.Get<Player>(playerId)", StringComparison.Ordinal);
        dmBypassIndex.Should().BeLessThan(playerEntityLookupIndex);
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
