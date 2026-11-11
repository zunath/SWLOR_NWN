using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

public class PowerGloveEquipmentTests
{
    // The item groups and the weapon ability mappings are the item service's, which the Item facade resolves from a
    // container; the service is the real one with its search for item definitions switched off.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void LoadPureWeaponMappings()
    {
        _facades = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<IItemService, MappingOnlyItemService>()));
        using (_facades.Install())
        using (NWScriptRuntime.Override(new FakeNWScript()))
        {
            Skill.LoadMappings();
            Item.CacheData();
        }
    }

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [Test]
    public void PairNoteIsAppendedOnceAndKeepsTheDescription()
    {
        const string existing = "A customized glove with a worn grip.";

        var noted = PowerGloveEquipment.GetPairDescription(existing);

        noted.Should().Be($"{existing}\n{PowerGloveEquipmentService.PairDescriptionNote}");
        PowerGloveEquipment.GetPairDescription(noted).Should().Be(noted, "examining again adds nothing");
        PowerGloveEquipment.GetPairDescription(string.Empty).Should().Be(PowerGloveEquipmentService.PairDescriptionNote);
        PowerGloveEquipmentService.PairDescriptionNote.Should().MatchRegex("^[\\x20-\\x7E]+$", "game text is plain ASCII");
    }

    [Test]
    public void PowerGloveUsesAnIndependentEquippedMeleeProfile()
    {
        Item.PowerGloveBaseItemTypes.Should().Equal(BaseItem.PowerGlove);
        Item.WeaponBaseItemTypes.Should().Contain(BaseItem.PowerGlove);
        Item.OneHandedMeleeItemTypes.Should().Contain(BaseItem.PowerGlove);
        Item.IsAttackWeaponType(BaseItem.PowerGlove).Should().BeTrue();
        Item.IsDoubleWeaponType(BaseItem.PowerGlove).Should().BeFalse();
        Item.ArmorBaseItemTypes.Should().NotContain(BaseItem.PowerGlove);
        Item.KatarBaseItemTypes.Should().NotContain(BaseItem.PowerGlove);
        Item.ForceSensitiveWeaponBaseItemTypes.Should().NotContain(BaseItem.PowerGlove);
        Skill.GetSkillTypeByBaseItem(BaseItem.PowerGlove).Should().Be(SkillType.Unarmed);
        Item.GetWeaponDamageAbilityType(BaseItem.PowerGlove).Should().Be(AbilityType.Might);
        Item.GetWeaponAccuracyAbilityType(BaseItem.PowerGlove).Should().Be(AbilityType.Perception);
        WeaponDelay.GetWeaponDelay(BaseItem.PowerGlove).Should().Be(ItemPropertyAttackDelay.Delay220);
    }

    [Test]
    public void ArmorGlovesAndLegacyKatarsKeepTheirOriginalSkill()
    {
        foreach (var type in new[] { BaseItem.Gloves, BaseItem.Bracer, BaseItem.Katar })
            Skill.GetSkillTypeByBaseItem(type).Should().Be(SkillType.Katar);
        Item.ArmorBaseItemTypes.Should().Contain(BaseItem.Gloves).And.Contain(BaseItem.Bracer);
    }

    [Test]
    public void CosmeticCompanionHasNoIndependentWeaponOrShieldProfile()
    {
        Item.WeaponBaseItemTypes.Should().NotContain(BaseItem.CompanionPowerGlove);
        Item.PowerGloveBaseItemTypes.Should().NotContain(BaseItem.CompanionPowerGlove);
        Item.OneHandedMeleeItemTypes.Should().NotContain(BaseItem.CompanionPowerGlove);
        Item.ShieldBaseItemTypes.Should().NotContain(BaseItem.CompanionPowerGlove);
        Item.IsAttackWeaponType(BaseItem.CompanionPowerGlove).Should().BeFalse();
        Item.IsDoubleWeaponType(BaseItem.CompanionPowerGlove).Should().BeFalse();
        Skill.GetSkillTypeByBaseItem(BaseItem.CompanionPowerGlove).Should().Be(SkillType.Invalid);
    }

    [TestCase(BaseItem.PowerGlove, InventorySlot.RightHand, null, null, false, true)]
    [TestCase(BaseItem.PowerGlove, InventorySlot.LeftHand, null, null, false, false)]
    [TestCase(BaseItem.PowerGlove, InventorySlot.RightHand, null, BaseItem.Dagger, false, false)]
    [TestCase(BaseItem.PowerGlove, InventorySlot.RightHand, null, BaseItem.PowerGlove, false, false)]
    [TestCase(BaseItem.CompanionPowerGlove, InventorySlot.LeftHand, BaseItem.PowerGlove, null, true, true)]
    [TestCase(BaseItem.CompanionPowerGlove, InventorySlot.LeftHand, BaseItem.PowerGlove, null, false, false)]
    [TestCase(BaseItem.CompanionPowerGlove, InventorySlot.RightHand, null, null, true, false)]
    [TestCase(BaseItem.Dagger, InventorySlot.LeftHand, BaseItem.PowerGlove, null, false, false)]
    [TestCase(BaseItem.SmallShield, InventorySlot.LeftHand, BaseItem.PowerGlove, null, false, false)]
    [TestCase(BaseItem.Dagger, InventorySlot.RightHand, null, null, false, true)]
    public void PairEquipPolicyReservesOneOwnedMainAndAnAuthorizedCosmeticHand(
        BaseItem item, InventorySlot slot, BaseItem? main, BaseItem? off, bool authorized, bool allowed) =>
        string.IsNullOrEmpty(EquipmentRestrictions.GetPowerGloveEquipmentError(item, slot, main, off, authorized))
            .Should().Be(allowed);

    [Test]
    public void PairEquipPolicyPreservesExistingRestrictionErrors() =>
        EquipmentRestrictions.GetPowerGloveEquipmentError(BaseItem.PowerGlove, InventorySlot.RightHand,
            null, null, false, "Existing restriction").Should().Be("Existing restriction");

    [TestCase(true, false, true, true, true, true)]
    [TestCase(false, false, true, true, true, false)]
    [TestCase(true, true, true, true, true, false)]
    [TestCase(true, false, false, true, true, false)]
    [TestCase(true, false, true, false, true, false)]
    [TestCase(true, false, true, true, false, false)]
    public void SavedPairRecoveryRequiresPreAreaPlayerSlotsAndUnchangedOwnership(
        bool isPlayer, bool hasArea, bool equippedPair, bool matchingOwners, bool cachedPairMatches, bool expected)
    {
        PowerGloveEquipment.CanRestoreSavedPair(isPlayer, hasArea, equippedPair, matchingOwners, cachedPairMatches)
            .Should().Be(expected);
    }

    [Test]
    public void CompanionNativeDataHasNoDamageArmorValueWeightOrProperties()
    {
        var root = RepositoryRoot();
        var lines = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks/sw_2da/baseitems.2da"));
        var header = lines.First(line => line.TrimStart().StartsWith("Name ", StringComparison.Ordinal))
            .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var fields = lines.Select(line => line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            .Single(row => row.Length > 0 && row[0] == ((int)BaseItem.CompanionPowerGlove).ToString());
        var data = header.Select((column, index) => (column, value: fields[index + 1]))
            .ToDictionary(pair => pair.column, pair => pair.value);
        foreach (var column in new[] { "WeaponType", "BaseAC", "BaseCost", "TenthLBS" })
            data[column].Should().Be("0", $"the cosmetic hand must have no native {column} contribution");
        foreach (var column in new[] { "NumDice", "DieToRoll", "CritThreat", "CritHitMult" })
            data[column].Should().Be("****", $"the cosmetic hand must use the native nonweapon default for {column}");
        Convert.ToInt32(data["EquipableSlots"], 16).Should().Be(0x20, "only the cosmetic left hand is equipable");
        data["ModelType"].Should().Be("2");
        data["ItemClass"].Should().BeEquivalentTo("WPL", "the left hand renders the mirrored glove models");

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module/uti/pg_companion.uti.json")));
        var item = document.RootElement;
        item.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be((int)BaseItem.CompanionPowerGlove);
        item.GetProperty("PropertiesList").GetProperty("value").GetArrayLength().Should().Be(0);
        foreach (var field in new[] { "Cost", "AddCost" })
            item.GetProperty(field).GetProperty("value").GetInt32().Should().Be(0);
        var noEconomy = item.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Single(variable => variable.GetProperty("Name").GetProperty("value").GetString() == ItemService.NoEconomyVariable);
        noEconomy.GetProperty("Value").GetProperty("value").GetInt32().Should().Be(1);
    }

    [Test]
    public void UnarmedIsAWeaponSkillForStandardAndForceSensitiveCharacters()
    {
        var skill = SkillType.Unarmed.GetAttribute<SkillType, SkillAttribute>();
        skill.Category.Should().Be(SkillCategoryType.Combat);
        skill.CombatPointCategory.Should().Be(CombatPointCategoryType.Weapon);
        skill.MaxRank.Should().Be(50);
        skill.ContributesToSkillCap.Should().BeTrue();
        skill.IsAvailableToCharacterType(CharacterType.Standard).Should().BeTrue();
        skill.IsAvailableToCharacterType(CharacterType.ForceSensitive).Should().BeTrue();
    }

    [TestCase(BaseItem.PowerGlove, null, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.CompanionPowerGlove, true)]
    [TestCase(BaseItem.PowerGlove, BaseItem.PowerGlove, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.Katar, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.Dagger, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.SmallShield, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.Invalid, false)]
    [TestCase(BaseItem.Katar, BaseItem.PowerGlove, false)]
    [TestCase(BaseItem.CompanionPowerGlove, BaseItem.PowerGlove, false)]
    [TestCase(BaseItem.CompanionPowerGlove, null, false)]
    [TestCase(null, BaseItem.CompanionPowerGlove, false)]
    [TestCase(BaseItem.Gloves, null, false)]
    [TestCase(BaseItem.Bracer, null, false)]
    [TestCase(BaseItem.Invalid, null, false)]
    [TestCase(null, BaseItem.PowerGlove, false)]
    [TestCase(null, null, false)]
    public void UnarmedGripUsesOneRealGloveAndItsCosmeticHand(BaseItem? mainHand, BaseItem? offHand, bool expected) =>
        EquipmentPredicates.HasUnarmedGrip(mainHand, offHand).Should().Be(expected);

    [Test, NonParallelizable]
    public void DestroyedOwnerStateCannotSurviveIntoAReusedObjectId()
    {
        var service = (PowerGloveEquipmentService)_facades.Get<IPowerGloveEquipmentService>();
        var pairs = (Dictionary<uint, (uint Main, uint Companion)>)typeof(PowerGloveEquipmentService)
            .GetField("_pairs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;
        var changing = (HashSet<uint>)typeof(PowerGloveEquipmentService)
            .GetField("_changing", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;
        var refreshing = (HashSet<uint>)typeof(PowerGloveEquipmentService)
            .GetField("_refreshingAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;
        var originalPairs = pairs.ToArray();
        var originalChanging = changing.ToArray();
        var originalRefreshing = refreshing.ToArray();
        const uint owner = 41, other = 42, uncachedOwner = 43;
        try
        {
            pairs.Clear();
            changing.Clear();
            refreshing.Clear();
            pairs[owner] = (101, 102);
            pairs[other] = (201, 202);
            foreach (var actor in new[] { owner, other, uncachedOwner })
            {
                changing.Add(actor);
                refreshing.Add(actor);
            }

            var removed = service.RemoveOwnerState(owner);
            removed.Should().Be((101u, 102u));
            pairs.Should().NotContainKey(owner);
            changing.Should().NotContain(owner);
            refreshing.Should().NotContain(owner);
            service.RemoveOwnerState(owner).Should().BeNull();

            // An owner destroyed before a pair is cached must also release its reentrancy guards.
            service.RemoveOwnerState(uncachedOwner).Should().BeNull();
            changing.Should().NotContain(uncachedOwner);
            refreshing.Should().NotContain(uncachedOwner);
            pairs[other].Should().Be((201u, 202u));
            changing.Should().Contain(other);
            refreshing.Should().Contain(other);

            pairs[owner] = (301, 302);
            service.RemoveOwnerState(owner).Should().Be((301u, 302u));
            pairs[other].Should().Be((201u, 202u));
        }
        finally
        {
            pairs.Clear();
            foreach (var (actor, pair) in originalPairs) pairs.Add(actor, pair);
            changing.Clear();
            changing.UnionWith(originalChanging);
            refreshing.Clear();
            refreshing.UnionWith(originalRefreshing);
        }
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the SWLOR repository.");
    }
}
