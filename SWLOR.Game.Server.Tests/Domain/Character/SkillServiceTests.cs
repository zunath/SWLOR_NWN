using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the skill instance: the catalog caches, the rank rules and the weapon skill mappings. The XP award path is
/// in <see cref="SkillXPServiceTests"/>.
/// </summary>
[TestFixture]
public class SkillServiceTests : SkillTestBase
{
    // ---- the catalog caches

    [Test]
    public void CacheData_ListsEverySkillAndCategory_AndSignalsTheLoad()
    {
        _engine.ClearCalls();
        _service.Catalog.CacheData();

        _service.Catalog.GetAllSkills().Keys.Should().BeEquivalentTo(Enum.GetValues<SkillType>());
        _service.Catalog.GetAllSkillCategories().Keys.Should().BeEquivalentTo(Enum.GetValues<SkillCategoryType>());
        _log.Should().Contain("signal:SWLOR_CACHE_SKILLS_LOADED");
    }

    [Test]
    public void CacheData_SplitsTheCatalogByActivityAndCapContribution()
    {
        var all = _service.Catalog.GetAllSkills();
        var active = _service.Catalog.GetAllActiveSkills();

        active.Keys.Should().BeEquivalentTo(all.Where(x => x.Value.IsActive).Select(x => x.Key));
        _service.Catalog.GetAllContributingSkills().Keys.Should()
            .BeEquivalentTo(all.Where(x => x.Value.ContributesToSkillCap).Select(x => x.Key));
        _service.Catalog.GetActiveContributingSkills().Keys.Should()
            .BeEquivalentTo(all.Where(x => x.Value.ContributesToSkillCap && x.Value.IsActive).Select(x => x.Key));
        _service.Catalog.GetActiveCraftingSkills().Keys.Should()
            .BeEquivalentTo(active.Where(x => x.Value.IsShownInCraftMenu).Select(x => x.Key));
        _service.Catalog.GetActiveResearchableCraftingSkills().Keys.Should()
            .BeEquivalentTo(active.Where(x => x.Value.IsShownInResearchMenu).Select(x => x.Key));
    }

    [Test]
    public void CacheData_ReadsHandOutCopies()
    {
        _service.Catalog.GetAllSkills().Clear();

        _service.Catalog.GetAllSkills().Should().NotBeEmpty();
    }

    [Test]
    public void Categories_AreGroupedAndFiltered()
    {
        var categories = _service.Catalog.GetAllSkillCategories();

        _service.Catalog.GetAllActiveSkillCategories().Keys.Should().Contain(SkillCategoryType.Combat);
        _service.Catalog.GetAllContributingSkillCategories().Keys.Should().Contain(SkillCategoryType.Combat);
        _service.Catalog.GetActiveContributingSkillCategories().Keys.Should().Contain(SkillCategoryType.Combat);
        _service.Catalog.GetSkillCategoryDetails(SkillCategoryType.Combat).Should().BeSameAs(categories[SkillCategoryType.Combat]);
    }

    [Test]
    public void SkillsByCategory_ListEachSkillUnderItsCategory()
    {
        foreach (var category in _service.Catalog.GetAllSkillCategories().Keys)
            _service.Catalog.GetAllSkillsByCategory(category).Values.Should().OnlyContain(x => x.Category == category);

        _service.Catalog.GetActiveSkillsByCategory(SkillCategoryType.Combat).Keys.Should().Contain(SkillType.Armor);
        _service.Catalog.GetAllSkillsByCategory(SkillCategoryType.Combat).Keys.Should().Contain(SkillType.Armor);
    }

    [Test]
    public void DisplayOrder_SortsByNameAndPutsLanguagesLast()
    {
        var display = _service.Catalog.GetAllActiveSkillsForDisplay().Values.ToList();
        var firstLanguage = display.FindIndex(x => x.Category == SkillCategoryType.Languages);

        display.Skip(firstLanguage).Should().OnlyContain(x => x.Category == SkillCategoryType.Languages);
        display.Take(firstLanguage).Select(x => x.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        display.Skip(firstLanguage).Select(x => x.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);

        foreach (var category in _service.Catalog.GetAllActiveSkillCategories().Keys)
        {
            _service.Catalog.GetActiveSkillsByCategoryForDisplay(category).Values.Select(x => x.Name).Should()
                .BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        }
    }

    [Test]
    public void GetSkillDetails_ReadsTheSkillAttribute()
    {
        var details = _service.Catalog.GetSkillDetails(SkillType.Armor);

        details.Name.Should().Be("Armor");
        details.MaxRank.Should().Be(50);
        details.ContributesToSkillCap.Should().BeTrue();
    }

    // ---- skill points

    [TestCase(-5, 10)]
    [TestCase(0, 10)]
    [TestCase(75, 85)]
    [TestCase(400, 410)]
    [TestCase(900, 410)]
    public void GetTotalSkillPoints_AddsTheStartingPoolToTheClampedEarnedPoints(int earned, int expected)
    {
        _service.Catalog.GetTotalSkillPoints(new Player { TotalSPAcquired = earned }).Should().Be(expected);
    }

    [Test]
    public void ApCap_IsATenthOfTheSkillCap()
    {
        _service.Catalog.APCap.Should().Be(40);
    }

    [Test]
    public void TryIsContributingSkill_IsFalseBeforeTheCacheLoads_AndReadsTheCatalogTableAfterwards()
    {
        var uncached = NewService();
        uncached.Catalog.TryIsContributingSkill(SkillType.Armor, out var contributes).Should().BeFalse();
        contributes.Should().BeFalse();

        _service.Catalog.TryIsContributingSkill(SkillType.Armor, out contributes).Should().BeTrue();
        contributes.Should().BeTrue();
        _service.Catalog.TryIsContributingSkill(SkillType.Mirialan, out contributes).Should().BeTrue();
        contributes.Should().BeFalse("languages do not count toward the skill cap");
    }

    // ---- AddMissingSkills

    [Test]
    public void AddMissingSkills_CreatesARecordForEverySkillTheCharacterLacks()
    {
        _engine.SetReturn("GetEnteringObject", Creature);
        var kept = Give(SkillType.Armor, 12, 34);

        _service.Catalog.AddMissingSkills();

        _record.Skills.Keys.Should().BeEquivalentTo(Enum.GetValues<SkillType>());
        _record.Skills[SkillType.Armor].Should().BeSameAs(kept);
        _record.Skills[SkillType.Piloting].Rank.Should().Be(0);
        _log.Should().Equal("db.get", "db.set");
    }

    [Test]
    public void AddMissingSkills_IgnoresDMsAndNonPlayers()
    {
        _engine.SetReturn("GetEnteringObject", Creature);
        _engine.SetReturn("GetIsDM", true);
        _service.Catalog.AddMissingSkills();

        _engine.SetReturn("GetIsDM", false);
        _engine.SetReturn("GetIsPC", false);
        _service.Catalog.AddMissingSkills();

        _log.Should().BeEmpty();
    }

    // ---- GetCreatureSkillRank

    [Test]
    public void GetCreatureSkillRank_ReadsAPlayersStoredRank()
    {
        Give(SkillType.Armor, 17);

        _service.Catalog.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(17);
        _service.Catalog.GetCreatureSkillRank(Creature, SkillType.Piloting).Should().Be(0);
    }

    [Test]
    public void GetCreatureSkillRank_IsZeroForDMsInvalidSkillsAndOtherCreatures()
    {
        Give(SkillType.Armor, 17);
        _service.Catalog.GetCreatureSkillRank(Creature, SkillType.Invalid).Should().Be(0);

        _engine.SetReturn("GetIsPC", false);
        _service.Catalog.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(0);

        _engine.SetReturn("GetIsDM", true);
        _service.Catalog.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(0);
    }

    [Test]
    public void GetCreatureSkillRank_ADroidAnswersFromItsControllerItem()
    {
        _service.DroidCreatures.Add(Creature);
        _service.DroidRank = 22;
        Give(SkillType.Armor, 17);

        _service.Catalog.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(22);
        _log.Should().BeEmpty("a droid's ranks are not stored on a player record");
    }

    // ---- weapon skills

    [Test]
    public void GetSkillTypeByBaseItem_MapsWeaponsAndAmmunition_AndNothingElse()
    {
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Longsword).Should().Be(SkillType.Vibroblade);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Dagger).Should().Be(SkillType.Vibroknife);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Lightsaber).Should().Be(SkillType.Lightsaber);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Rifle).Should().Be(SkillType.Rifle);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Pistol).Should().Be(SkillType.Pistol);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.CompanionPistol).Should().Be(SkillType.TwinPistol);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.PowerGlove).Should().Be(SkillType.Unarmed);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Shuriken).Should().Be(SkillType.Throwing);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Amulet).Should().Be(SkillType.Invalid);
    }

    [Test]
    public void GetSkillTypeByBaseItem_IsInvalidBeforeTheMappingsLoad()
    {
        NewService().Catalog.GetSkillTypeByBaseItem(BaseItem.Longsword).Should().Be(SkillType.Invalid);
    }

    [Test]
    public void GetAttackSkillTypeByBaseItem_TreatsGlovesAndBracersAsBareHands()
    {
        _service.Catalog.GetAttackSkillTypeByBaseItem(BaseItem.Gloves).Should().Be(SkillType.Unarmed);
        _service.Catalog.GetAttackSkillTypeByBaseItem(BaseItem.Bracer).Should().Be(SkillType.Unarmed);
        _service.Catalog.GetSkillTypeByBaseItem(BaseItem.Gloves).Should().Be(SkillType.Katar);
        _service.Catalog.GetAttackSkillTypeByBaseItem(BaseItem.Katar).Should().Be(SkillType.Katar);
    }

    [Test]
    public void GetWeaponSkill_PrefersAnExplicitOverride()
    {
        _service.Catalog.GetWeaponSkill(Creature, Weapon, SkillType.Force).Should().Be(SkillType.Force);
        _service.Catalog.GetWeaponSkill(Creature, OBJECT_INVALID, SkillType.Force).Should().Be(SkillType.Force);
    }

    [Test]
    public void GetWeaponSkill_IgnoresAnInvalidOverride()
    {
        _engine.SetReturn("GetIsObjectValid", false);

        _service.Catalog.GetWeaponSkill(Creature, Weapon, SkillType.Invalid).Should().Be(SkillType.Invalid);
        _service.Catalog.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Invalid);
    }

    [Test]
    public void GetWeaponSkill_ReadsTheBaseItemOfAValidWeapon()
    {
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetBaseItemType", BaseItem.Rifle);

        _service.Catalog.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Rifle);
        _twinProxy.Calls.Should().BeEmpty("the pair is only asked about when the weapon is held");
    }

    [Test]
    public void GetWeaponSkill_AHeldPistolOfAnActivePairTrainsTwinPistol()
    {
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetBaseItemType", BaseItem.Pistol);
        _engine.SetReturnHandler("GetItemInSlot", args => (InventorySlot)args[0]! == InventorySlot.RightHand ? Weapon : OBJECT_INVALID);

        _twinPair = true;
        _service.Catalog.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.TwinPistol);

        _twinPair = false;
        _service.Catalog.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Pistol);
    }

    [Test]
    public void GetWeaponSkill_ACompanionPistolOutsideAPairTrainsNothing()
    {
        _engine.SetReturn("GetIsObjectValid", true);
        _engine.SetReturn("GetBaseItemType", BaseItem.CompanionPistol);

        _service.Catalog.GetWeaponSkill(Creature, Weapon).Should().Be(SkillType.Invalid);
    }

    // ---- the seams

    [Test]
    public void TheDefaultSeams_RunWithoutAServer()
    {
        var plain = TestSkillService.CreateWithDefaultSeams(new LoggingDB(_store, Mark));

        // A DM answers 0 before any seam is reached, and a creature that is not a droid falls through to the player lookup.
        _engine.SetReturn("GetIsDM", true);
        plain.GetCreatureSkillRank(Creature, SkillType.Armor).Should().Be(0);
    }
}
