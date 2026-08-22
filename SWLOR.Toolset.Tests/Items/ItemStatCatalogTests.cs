using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>Group membership, the crafting/droid matrices, and family-scoped visibility.</summary>
    [TestFixture]
    public class ItemStatCatalogTests
    {
        [Test]
        public void Defense_HasBothIprpDefensetypeSubtypes()
        {
            var defense = ItemStatCatalog.ByGroup(ItemStatGroup.Defense);

            defense.Should().HaveCount(2);
            defense.Should().OnlyContain(stat => stat.PropertyId == 94);
            defense.Select(stat => stat.SubtypeId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Test]
        public void Resistance_HasAllEightIprpResistanceSubtypes()
        {
            var resistance = ItemStatCatalog.ByGroup(ItemStatGroup.Resistance);

            resistance.Should().OnlyContain(stat => stat.PropertyId == 133);
            resistance.Select(stat => stat.SubtypeId).Should().BeEquivalentTo(
                new[] { 1, 2, 3, 4, 100, 101, 102, 103 });
        }

        [Test]
        public void Combat_DamageStatAndWeaponDamageTypeAreNotInTheFlatCatalogAnymore()
        {
            var combat = ItemStatCatalog.ByGroup(ItemStatGroup.Combat);

            // DamageStat (103) was removed from the editor by owner decision; WeaponDamageType (134)
            // is a single exclusive choice modeled through ItemMultiEntryCatalog instead of six rows.
            combat.Should().NotContain(stat => stat.PropertyId == 103);
            combat.Should().NotContain(stat => stat.PropertyId == 134);

            var weaponDamageType = ItemMultiEntryCatalog.ByPropertyId(134);
            weaponDamageType.Should().NotBeNull();
            weaponDamageType!.IsExclusive.Should().BeTrue();
            weaponDamageType.Context.Should().Be(ItemStatGroup.Combat);
        }

        [Test]
        public void MultiSubtypeProperties_AreRemovedFromTheFlatCatalog()
        {
            var all = ItemStatCatalog.All;

            foreach (var propertyId in new[] { 106, 122, 123, 125, 128, 129 })
                all.Should().NotContain(stat => stat.PropertyId == propertyId,
                    $"property {propertyId} is multi-subtype and lives only in ItemMultiEntryCatalog");
        }

        [Test]
        public void EnhancementsGroupCarriesNoFlatStatsOfItsOwn()
        {
            ItemStatCatalog.ByGroup(ItemStatGroup.Enhancements).Should().BeEmpty(
                "the *Enhancement properties are catalogued only in ItemMultiEntryCatalog");
        }

        [Test]
        public void ItemMultiEntryCatalog_CoversEveryDeclaredMultiSubtypeProperty()
        {
            var expected = new[] { 106, 101, 102, 107, 108, 109, 110, 116, 122, 123, 128, 129, 125, 100, 64 };

            foreach (var propertyId in expected)
                ItemMultiEntryCatalog.Contains(propertyId).Should().BeTrue($"property {propertyId} should be covered");

            ItemMultiEntryCatalog.ByPropertyId(100)!.IsRequirement.Should().BeTrue();
            ItemMultiEntryCatalog.ByPropertyId(64)!.IsRequirement.Should().BeTrue();
            ItemMultiEntryCatalog.ByPropertyId(106)!.Context.Should().Be(ItemStatGroup.Bonuses);
            ItemMultiEntryCatalog.ByPropertyId(101)!.Context.Should().Be(ItemStatGroup.Enhancements);
            ItemMultiEntryCatalog.ByPropertyId(122)!.Context.Should().Be(ItemStatGroup.Droid);
            ItemMultiEntryCatalog.ByPropertyId(128)!.Context.Should().Be(ItemStatGroup.Incubation);
            ItemMultiEntryCatalog.ByPropertyId(125)!.Context.Should().Be(ItemStatGroup.Npc);
        }

        [Test]
        public void ItemEngineLegacyCatalog_ContainsEveryDeclaredEngineProperty()
        {
            // DamageImmunity (20), Immunity (37), Regeneration (51), and UnlimitedAmmo (61) were
            // removed from this catalog by owner decision - the Engine card never shows them, though
            // the corpus's entries for all four are preserved (ItemCombinationAuditTests' allowlist).
            // Unlimited ammunition is assumed for every ranged weapon, so 61 decides nothing.
            var expected = new[]
            {
                0, 1, 6, 10, 11, 12, 16, 18, 21, 22, 23, 24, 26, 32, 35, 40, 43, 44, 45, 47, 48,
                56, 58, 67, 72, 74, 75, 77, 79, 81, 82, 83, 87
            };

            expected.Should().HaveCount(33);
            foreach (var propertyId in expected)
                ItemEngineLegacyCatalog.Contains(propertyId).Should().BeTrue($"property {propertyId} should be covered");

            foreach (var removedPropertyId in new[] { 20, 37, 51, 61 })
                ItemEngineLegacyCatalog.Contains(removedPropertyId).Should().BeFalse(
                    $"property {removedPropertyId} was removed from the Engine card by owner decision");

            ItemEngineLegacyCatalog.Contains(9999).Should().BeFalse();
        }

        [Test]
        public void Crafting_HasTheThreeStatByFourCraftTypeMatrixPlusTwoFlatStats()
        {
            var crafting = ItemStatCatalog.ByGroup(ItemStatGroup.Crafting);

            crafting.Where(stat => stat.PropertyId is 88 or 89 or 115).Should().HaveCount(12,
                "Control, Craftsmanship, and CP Bonus each cross the four craft types");
            crafting.Should().Contain(stat => stat.PropertyId == 95, "Progress Penalty has no subtype");
            crafting.Should().Contain(stat => stat.PropertyId == 130, "Blueprint Level has no subtype");
            crafting.Should().HaveCount(14);
        }

        [Test]
        public void Droid_HasAllThirtyOneDroidStatSubtypeRowsPlusTheOneFlatDroidPersonalityProperty()
        {
            var droid = ItemStatCatalog.ByGroup(ItemStatGroup.Droid);

            droid.Where(stat => stat.PropertyId == 121).Should().HaveCount(31,
                "iprp_droidstat.2da has 31 labeled rows: 2-20 and 115-126");
            droid.Should().NotContain(stat => stat.PropertyId == 122,
                "DroidPartType is multi-subtype and lives only in ItemMultiEntryCatalog now");
            droid.Should().NotContain(stat => stat.PropertyId == 123,
                "DroidInstruction is multi-subtype and lives only in ItemMultiEntryCatalog now");
            droid.Should().Contain(stat => stat.PropertyId == 124);
            droid.Should().HaveCount(32);
        }

        [Test]
        public void Visibility_WeaponCombatIncludesDamageAndArmorCombatExcludesIt()
        {
            var weaponCombat = ItemStatVisibility.CombatStatsFor(ItemFamily.MeleeWeapon);
            weaponCombat.Should().Contain(stat => stat.PropertyId == 93, "DMG belongs on a weapon");
            weaponCombat.Should().Contain(stat => stat.PropertyId == 98, "Delay belongs on a weapon");

            var armorCombat = ItemStatVisibility.CombatStatsFor(ItemFamily.Armor);
            armorCombat.Should().NotContain(stat => new[] { 93, 98 }.Contains(stat.PropertyId));
            armorCombat.Should().Contain(stat => stat.PropertyId == 117, "Evasion still applies off a weapon");
        }

        [Test]
        public void Visibility_MiscellaneousHasNoPrimaryGroups()
        {
            ItemStatVisibility.PrimaryGroups(ItemFamily.Miscellaneous).Should().BeEmpty();
        }

        [Test]
        public void Visibility_SecondaryGroupsAreEverythingNotPrimary()
        {
            var primary = ItemStatVisibility.PrimaryGroups(ItemFamily.MeleeWeapon);
            var secondary = ItemStatVisibility.SecondaryGroups(ItemFamily.MeleeWeapon);

            secondary.Should().NotContain(group => primary.Contains(group));
            primary.Concat(secondary).Distinct()
                .Should().BeEquivalentTo(Enum.GetValues<ItemStatGroup>());
        }

        [Test]
        public void GroupsUnlockedBy_DroidPartUnlocksTheDroidGroup()
        {
            ItemRoleCatalog.GroupsUnlockedBy(ItemRoleCatalog.DroidPartId)
                .Should().BeEquivalentTo(new[] { ItemStatGroup.Droid });
        }

        [Test]
        public void Requirements_CoverSkillsStatsPerkAndRaceAndExcludeLanguages()
        {
            var requirements = ItemRequirementCatalog.All;

            requirements.Count(req => req.Category == ItemRequirementCategory.Stat).Should().Be(6);
            requirements.Count(req => req.Category == ItemRequirementCategory.Perk).Should().Be(1);
            requirements.Count(req => req.Category == ItemRequirementCategory.Race).Should().Be(1);

            var skillRequirements = requirements.Where(req => req.Category == ItemRequirementCategory.Skill).ToList();
            skillRequirements.Should().NotBeEmpty();
            skillRequirements.Should().OnlyContain(req => req.SkillCategory != SkillCategoryType.Languages);
            skillRequirements.Should().Contain(req => req.SubtypeId == (int)SkillType.Armor);
        }
    }
}
