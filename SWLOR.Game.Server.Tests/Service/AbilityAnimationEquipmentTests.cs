using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Grip = SWLOR.Game.Server.Service.AnimationService.AnimationWeaponGrip;
using Requirement = SWLOR.Game.Server.Service.AnimationService.AnimationEquipmentRequirement;

namespace SWLOR.Game.Server.Tests.Service;

public class AbilityAnimationEquipmentTests
{
    // Shipped baseitems rows: WeaponWield, WeaponSize, WeaponType, creature size.
    [TestCase(BaseItem.GreatSword, null, 4, 3, 3, Grip.TwoHanded)]
    [TestCase(BaseItem.HeavyFlail, null, 4, 1, 3, Grip.TwoHanded)]
    [TestCase(BaseItem.WarHammer, null, 3, 1, 3, Grip.OneHanded)]
    [TestCase(BaseItem.LightMace, null, 2, 1, 3, Grip.OneHanded)]
    [TestCase(BaseItem.ShortSpear, 4, 4, 4, 3, Grip.Polearm)]
    [TestCase(BaseItem.QuarterStaff, 4, 4, 1, 3, Grip.Polearm)]
    [TestCase(BaseItem.DireMace, 8, 4, 1, 3, Grip.Polearm)]
    [TestCase(BaseItem.TwoBladedSword, 8, 4, 3, 3, Grip.Polearm)]
    [TestCase(BaseItem.Longsword, null, 3, 3, 3, Grip.OneHanded)]
    [TestCase(BaseItem.Longsword, null, 3, 3, 2, Grip.TwoHanded)]
    [TestCase(BaseItem.GreatSword, null, 4, 3, 2, Grip.Unknown)]
    [TestCase(BaseItem.Pistol, 10, 2, 2, 3, Grip.Pistol)]
    [TestCase(BaseItem.Rifle, 6, 4, 2, 3, Grip.Rifle)]
    [TestCase(BaseItem.Dart, 11, 2, 2, 3, Grip.Throwing)]
    [TestCase(BaseItem.SmallShield, 7, null, null, 3, Grip.Shield)]
    [TestCase(BaseItem.Katar, null, 2, 2, 3, Grip.Unarmed)]
    [TestCase(BaseItem.Invalid, null, null, null, 3, Grip.Empty)]
    [TestCase(BaseItem.Longsword, null, null, null, 3, Grip.Unknown)]
    [TestCase(BaseItem.Longsword, 99, 3, 3, 3, Grip.Unknown)]
    [TestCase(BaseItem.Longsword, null, 3, 3, 0, Grip.Unknown)]
    public void NativeWeaponDataDeterminesGripInsteadOfSkillLists(BaseItem item, int? wield, int? size,
        int? weaponType, int creatureSize, Grip expected) =>
        AbilityAnimationEquipment.ResolveGrip(item, wield, size, weaponType, creatureSize).Should().Be(expected);

    [TestCase(Requirement.OneHanded, Grip.OneHanded, Grip.Empty, true)]
    [TestCase(Requirement.OneHanded, Grip.OneHanded, Grip.OneHanded, false)]
    [TestCase(Requirement.OneHanded, Grip.OneHanded, Grip.Shield, false)]
    [TestCase(Requirement.OneHanded, Grip.TwoHanded, Grip.Empty, false)]
    [TestCase(Requirement.WeaponAndShield, Grip.OneHanded, Grip.Shield, true)]
    [TestCase(Requirement.WeaponAndShield, Grip.OneHanded, Grip.OneHanded, false)]
    [TestCase(Requirement.WeaponAndShield, Grip.OneHanded, Grip.Empty, false)]
    [TestCase(Requirement.TwoHanded, Grip.TwoHanded, Grip.Empty, true)]
    [TestCase(Requirement.TwoHanded, Grip.Polearm, Grip.Empty, false)]
    [TestCase(Requirement.TwoHanded, Grip.OneHanded, Grip.OneHanded, false)]
    [TestCase(Requirement.TwoHanded, Grip.TwoHanded, Grip.Shield, false)]
    [TestCase(Requirement.Polearm, Grip.Polearm, Grip.Empty, true)]
    [TestCase(Requirement.Polearm, Grip.TwoHanded, Grip.Empty, false)]
    [TestCase(Requirement.Polearm, Grip.OneHanded, Grip.Empty, false)]
    [TestCase(Requirement.Polearm, Grip.Polearm, Grip.OneHanded, false)]
    [TestCase(Requirement.DualWield, Grip.OneHanded, Grip.OneHanded, true)]
    [TestCase(Requirement.DualWield, Grip.Polearm, Grip.Empty, false)]
    [TestCase(Requirement.Unarmed, Grip.Unarmed, Grip.Unarmed, true)]
    [TestCase(Requirement.Unarmed, Grip.Unarmed, Grip.Empty, true)]
    [TestCase(Requirement.Unarmed, Grip.Empty, Grip.Empty, true)]
    [TestCase(Requirement.Unarmed, Grip.Unarmed, Grip.OneHanded, false)]
    [TestCase(Requirement.Unarmed, Grip.Unarmed, Grip.Shield, false)]
    [TestCase(Requirement.Pistol, Grip.Pistol, Grip.Empty, true)]
    [TestCase(Requirement.Pistol, Grip.Pistol, Grip.Shield, false)]
    [TestCase(Requirement.Pistol, Grip.Rifle, Grip.Empty, false)]
    [TestCase(Requirement.Rifle, Grip.Rifle, Grip.Empty, true)]
    [TestCase(Requirement.Rifle, Grip.Pistol, Grip.Empty, false)]
    [TestCase(Requirement.Throwing, Grip.Throwing, Grip.Empty, true)]
    [TestCase(Requirement.Throwing, Grip.Throwing, Grip.Shield, false)]
    [TestCase(Requirement.Throwing, Grip.OneHanded, Grip.Empty, false)]
    [TestCase(Requirement.OneHanded, Grip.Unknown, Grip.Empty, false)]
    [TestCase(Requirement.Unrestricted, Grip.Unknown, Grip.Unknown, true)]
    public void AuthoredMotionRequiresTheCorrectGripAndOffHand(Requirement requirement, Grip mainHand, Grip offHand, bool expected) =>
        AbilityAnimationEquipment.IsCompatible(requirement, mainHand, offHand).Should().Be(expected);

    [Test]
    public void ManifestAndCatalogExplicitlyAgreeOnEveryEquipmentRequirement()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "design/animations/active-abilities.json")));
        var manifest = document.RootElement.EnumerateArray().ToDictionary(row => row.GetProperty("Id").GetString()!);
        ActiveAbilityAnimationCatalog.Entries.Select(entry => entry.Id).Should().BeEquivalentTo(manifest.Keys);
        var weaponCategories = new[] { "Heavy Vibroblade", "Vibroblade", "Vibroknife", "Lightsaber", "Spear", "Staff",
            "Saberstaff", "Twin Blade", "Katar", "Pistol", "Rifle", "Throwing" };
        foreach (var entry in ActiveAbilityAnimationCatalog.Entries)
        {
            var declared = manifest[entry.Id].GetProperty("EquipmentRequirement").GetString();
            Enum.GetNames<Requirement>().Should().Contain(declared);
            entry.EquipmentRequirement.ToString().Should().Be(declared, entry.Id);
            if (weaponCategories.Contains(entry.Category))
                entry.EquipmentRequirement.Should().NotBe(Requirement.Unrestricted, entry.Id);
        }
    }

    [Test]
    public void AuthoredWeaponFamiliesAndShieldRecipesHaveTheCorrespondingRequirements()
    {
        var catalog = ActiveAbilityAnimationCatalog.Entries.ToDictionary(entry => entry.Id);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "design/animations"), "choreographies.json", SearchOption.AllDirectories))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var motion in document.RootElement.EnumerateArray())
            {
                var id = motion.GetProperty("Id").GetString()!;
                var sources = motion.GetProperty("Beats").EnumerateArray().Select(beat => beat.GetProperty("SourceAnimation").GetString()).ToArray();
                var expected = sources.Contains("plreadyr") ? Requirement.Polearm :
                    sources.Any(source => source!.StartsWith("2h")) ? Requirement.TwoHanded :
                    sources.Contains("1hreadyr") ? Requirement.OneHanded : (Requirement?)null;
                if (expected.HasValue) catalog[id].EquipmentRequirement.Should().Be(expected.Value, id);
            }
        }
        using var recipe = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "design/animations/vibroblade/recipe.json")));
        foreach (var motion in recipe.RootElement.GetProperty("motions").EnumerateArray())
            catalog[motion.GetProperty("id").GetString()!].EquipmentRequirement.Should().Be(
                motion.GetProperty("shield").GetBoolean() ? Requirement.WeaponAndShield : Requirement.OneHanded);
    }

    [Test]
    public void IncompatibleEquipmentCannotPlayAnyRestrictedAbilityRankAndDoesNotChangeGameplay()
    {
        var allAbilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var entry in ActiveAbilityAnimationCatalog.Entries.Where(entry => entry.EquipmentRequirement != Requirement.Unrestricted))
        {
            var abilities = entry.Feats.ToDictionary(feat => feat, feat => allAbilities[feat]);
            var original = abilities.ToDictionary(pair => pair.Key, pair =>
                (pair.Value.ActivationAction, pair.Value.ImpactAction, pair.Value.ActivationDelay, pair.Value.ActivationType,
                    pair.Value.ImpactDelay, pair.Value.IsChanneled));
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            AbilityAnimationBinding.Apply(abilities, new[] { entry });
            foreach (var (feat, ability) in abilities)
            {
                ability.AnimationEquipmentRequirement.Should().Be(entry.EquipmentRequirement);
                var compatible = AbilityAnimationEquipment.IsCompatible(ability.AnimationEquipmentRequirement, Grip.Unknown, Grip.Unknown);
                compatible.Should().BeFalse();
                AbilityAnimationBinding.ActivationClip(ability, true, 0, compatible).Should().BeNull(entry.Id);
                AbilityAnimationBinding.ImpactClip(ability, true, compatible).Should().BeNull(entry.Id);
                AbilityAnimationBinding.QueuedClip(ability, true, compatible).Should().BeNull(entry.Id);
                if (ability.AuthoredAnimation != null)
                    AbilityAnimationBinding.ActivationType(ability, true, 0, compatible).Should().Be(
                        ability.HasGeneratedAnimationBinding ? ability.NativeAnimationType : Animation.Invalid, entry.Id);
                (ability.ActivationAction, ability.ImpactAction, ability.ActivationDelay, ability.ActivationType,
                    ability.ImpactDelay, ability.IsChanneled).Should().Be(original[feat]);
                ability.PreviewAnimation.Should().BeSameAs(entry.Clip);
                AbilityAnimationBinding.ActivationClip(ability, true, equipmentCompatible: true).Should().BeSameAs(ability.AuthoredAnimation);
                AbilityAnimationBinding.ImpactClip(ability, true, true).Should().BeSameAs(ability.AuthoredImpactAnimation);
                AbilityAnimationBinding.QueuedClip(ability, true, true).Should().BeSameAs(ability.QueuedAttackAnimation);
            }
        }
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "design/animations/active-abilities.json"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the animation manifest.");
    }
}
