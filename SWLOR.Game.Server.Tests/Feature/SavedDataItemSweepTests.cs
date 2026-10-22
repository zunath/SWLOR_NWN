#nullable enable
using System.Reflection;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.Formats.Gff;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The saved-data stored-item sweep edits saved GFF the way the engine pass edits live items. Parity with
/// the engine on real data is checked on a production copy; these cases pin each rule and its saved form.
/// </summary>
public class SavedDataItemSweepTests
{
    private static readonly Assembly ServerAssembly = typeof(HeadgearSlotMigration).Assembly;
    private static readonly Type Document = ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData")!;
    private static readonly Type NodeType = Document.GetNestedType("Node", BindingFlags.NonPublic)!;
    private static readonly Type ListingType = ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.RootListing")!;

    [Test]
    public void LootRevision_ShiftsEachCatalogStatAndSavesPropertiesTheWayTheEngineDoes()
    {
        // absdef_l1: loot DMG 23 -> 42 and Attack 12 -> 3, then rare Attack 3 -> 12.
        var original = Encode(Item("absdef_l1", BaseItem.Gloves, 59,
            Property(ItemPropertyType.DMG, 0, 34, 23, param1Value: 0),
            Property((ItemPropertyType)82, 125, 26, 39),
            Property(ItemPropertyType.Attack, 0, 45, 12, param1Value: 0)));

        Migrate(original, out var migrated, true, out var converted).Should().BeTrue();

        converted.Should().BeEmpty("absdef_l1 keeps its equipment type");
        var properties = Children(Read(migrated), "PropertiesList").ToList();
        properties.Select(p => (Int(p, "PropertyName"), Int(p, "CostValue")))
            .Should().Equal((82, 39), ((int)ItemPropertyType.DMG, 42), ((int)ItemPropertyType.Attack, 12));
        properties.Select(p => p.Type).Should().Equal(0u, 1u, 2u);
        var added = properties[2];
        added.Fields.Select(f => f.Label).Should().Equal("PropertyName", "Subtype", "CostTable", "CostValue",
            "Param1", "Param1Value", "ChanceAppear", "UsesPerDay", "Useable", "CustomTag");
        (Int(added, "Subtype"), Int(added, "CostTable"), Int(added, "Param1"), Int(added, "Param1Value"),
            Int(added, "ChanceAppear"), Int(added, "UsesPerDay"), Int(added, "Useable"))
            .Should().Be((ushort.MaxValue, 45, 255, 255, 100, 255, 1));
        Locals(Read(migrated)).Should().Equal(
            (LootStatRevisionMigration.RevisionVariable, LootStatRevisionMigration.CurrentRevision),
            (RareLootStatRevisionCatalog.RevisionVariable, RareLootStatRevisionCatalog.CurrentRevision));
        Children(Read(migrated), "VarTable").Select(variable => variable.Type).Should().OnlyContain(type => type == 0,
            "the engine saves every local with struct type 0");

        Migrate(migrated, out var rerun, true, out _).Should().BeFalse("revision locals make the sweep idempotent");
        rerun.Should().BeSameAs(migrated);
    }

    [Test]
    public void LegacyHeadgear_BecomesAHelmetWithItsFallbackModelAndListingMetadata()
    {
        var original = Encode(Item("precision_optic", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Torso", 186), Text("UUID", "optic-uuid")));

        Migrate(original, out var migrated, true, out var converted).Should().BeTrue();

        var root = Read(migrated);
        (Int(root, "BaseItem"), Int(root, "ModelPart1"), Int(root, "xModelPart1")).Should().Be(((int)BaseItem.Helmet, 110, 110));
        Value(root, "UUID").Should().Be("optic-uuid");
        Children(root, "PropertiesList").Should().Contain(p => Int(p, "PropertyName") == (int)ItemPropertyType.HPBonus && Int(p, "CostValue") == 50,
            "the loot revision adds stats the blueprint gained");
        converted.Should().ContainSingle().Which.Should().Be(("ihelm", MarketCategoryType.Helmet));
    }

    [Test]
    public void EquipmentTypeCorrection_SetsTheCorrectedTypeAndModel()
    {
        var original = Encode(Item("fang_gaunt", BaseItem.Bracer, 7));

        Migrate(original, out var migrated, true, out var converted).Should().BeTrue();

        var root = Read(migrated);
        (Int(root, "BaseItem"), Int(root, "ModelPart1"), Int(root, "xModelPart1")).Should().Be(((int)BaseItem.Gloves, 176, 176));
        converted.Should().ContainSingle().Which.Should().Be(("iit_glove_176", MarketCategoryType.Glove));
    }

    [Test]
    public void OutfitTemplates_KeepTheirEquipmentType()
    {
        var original = Encode(Item("precision_optic", BaseItem.Armor, null));

        Migrate(original, out var migrated, false, out var converted).Should().BeTrue("the stat revision still applies");

        Int(Read(migrated), "BaseItem").Should().Be((int)BaseItem.Armor);
        converted.Should().BeEmpty();
    }

    [Test]
    public void Artwork_UpdatesRecognizedModels()
    {
        Migrate(Encode(Item("abdamaryllia", (BaseItem)516, 5)), out var migrated, true, out _).Should().BeTrue();
        (Int(Read(migrated), "ModelPart1"), Int(Read(migrated), "xModelPart1")).Should().Be((19, 19));
    }

    [Test]
    public void UnrecognizedItems_AreLeftUntouched()
    {
        var original = Encode(Item("distilled_water", (BaseItem)24, 3, Property((ItemPropertyType)82, 125, 26, 39)));
        Migrate(original, out var migrated, true, out _).Should().BeFalse();
        migrated.Should().BeSameAs(original);
    }

    [Test]
    public void ContainerContents_AreMigratedInPlace()
    {
        var original = Encode(Item("bag", (BaseItem)66, 1,
            List("ItemList", Item("absdef_l1", BaseItem.Gloves, 59, Property(ItemPropertyType.DMG, 0, 34, 23)))));

        Migrate(original, out var migrated, true, out _).Should().BeTrue();

        var nested = Children(Read(migrated), "ItemList").Single();
        Children(nested, "PropertiesList").Should().Contain(p => Int(p, "PropertyName") == (int)ItemPropertyType.DMG && Int(p, "CostValue") == 42);
    }

    [Test]
    public void DroidControllers_MigrateTheirSavedParts()
    {
        var part = Encode(Item("absdef_l1", BaseItem.Gloves, 59, Property(ItemPropertyType.DMG, 0, 34, 23)));
        var droid = JsonConvert.SerializeObject(new ConstructedDroid { SerializedHead = part });
        var original = Encode(Item("droid_ctrl", (BaseItem)24, 1,
            List("VarTable", Node(0, Text("Name", "CONSTRUCTED_DROID"), Scalar(4, "Type", 3), Text("Value", droid)))));

        Migrate(original, out var migrated, true, out _).Should().BeTrue();

        var saved = (string)Value(Children(Read(migrated), "VarTable").Single(), "Value");
        var head = JsonConvert.DeserializeObject<ConstructedDroid>(saved)!.SerializedHead;
        Children(Read(head), "PropertiesList").Should().Contain(p => Int(p, "PropertyName") == (int)ItemPropertyType.DMG && Int(p, "CostValue") == 42);
    }

    [Test]
    public void SavedCreatures_OnlyChangeTheirItemsArtwork()
    {
        var original = Encode(Node(0,
            Text("Tag", "dm_creature"),
            List("Equip_ItemList",
                Item("absdef_l1", BaseItem.Gloves, 59, Property(ItemPropertyType.DMG, 0, 34, 23)),
                Item("abdamaryllia", (BaseItem)516, 5))), "UTC ");

        Migrate(original, out var migrated, true, out _).Should().BeTrue();

        var equipped = Children(Read(migrated), "Equip_ItemList").ToList();
        Int(Children(equipped[0], "PropertiesList").Single(), "CostValue").Should().Be(23, "saved creatures keep authored stats");
        Int(equipped[1], "xModelPart1").Should().Be(19);
    }

    [Test]
    public void RevisedPropertyCostTables_MatchItemPropDefAndCoverEveryCatalog()
    {
        var table = TwoDAReader.Read(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "itempropdef.2da"));
        var costTables = (IReadOnlyDictionary<ItemPropertyType, byte>)Document
            .GetProperty("RevisedPropertyCostTables", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        foreach (var (type, costTable) in costTables)
            table.GetValue((int)type, "CostTableResRef").Should().Be(costTable.ToString(), $"{type} saves its itempropdef.2da cost table");

        var catalogTypes = new[] { LootStatRevisionCatalog.Changes, RareLootStatRevisionCatalog.Changes,
                EquipmentTypeStatRevisionCatalog.Changes, FoundryLootStatRevisionCatalog.Changes, ArkanianArmorStatRevisionCatalog.Changes,
                CarapaceBracerStatRevisionCatalog.Changes }
            .SelectMany(catalog => catalog.Values).SelectMany(changes => changes).Select(change => change.Type).Distinct();
        costTables.Keys.Should().Contain(catalogTypes);
        costTables.Keys.Should().Contain(ItemPropertyType.Resistance, "the Arkanian revision rebuilds Ice Resistance");
    }

    [Test]
    public void RootIcons_FollowBaseItemIconRulesAndCoverEveryConversion()
    {
        var table = TwoDAReader.Read(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "baseitems.2da"));
        var rootIcons = (IReadOnlyDictionary<BaseItem, string>)Document
            .GetProperty("RootIcons", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var simpleIcons = (IReadOnlyDictionary<(BaseItem BaseItem, int Model), string>)Document
            .GetProperty("SimpleModelRootIcons", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        // Item.GetIconResref: cloaks use iit_cloak, other types that are not simple models use DefaultIcon,
        // and simple models are named i<ItemClass>_<model>.
        foreach (var (baseItem, icon) in rootIcons)
        {
            table.GetValue((int)baseItem, "ModelType").Should().NotBe("0", $"{baseItem}'s icon does not depend on its model");
            if (baseItem != BaseItem.Cloak)
                table.GetValue((int)baseItem, "DefaultIcon").Should().Be(icon);
        }
        foreach (var ((baseItem, model), icon) in simpleIcons)
        {
            table.GetValue((int)baseItem, "ModelType").Should().Be("0");
            icon.Should().Be($"i{table.GetValue((int)baseItem, "ItemClass")}_{model:000}".ToLowerInvariant());
        }

        bool Covered(BaseItem baseItem, int model) => rootIcons.ContainsKey(baseItem) || simpleIcons.ContainsKey((baseItem, model));
        foreach (var correction in EquipmentTypeMigration.Corrections.Values)
            Covered(correction.Type, correction.Model).Should().BeTrue($"{correction.Type} {correction.Model} has a saved-data icon");
        foreach (var (resref, revision) in ItemAppearanceCatalogMigration.Revisions)
            Covered(revision.BaseItem, revision.Current[0]).Should().BeTrue($"{resref} has a saved-data icon");
        foreach (var baseItem in rootIcons.Keys.Concat(simpleIcons.Keys.Select(key => key.BaseItem)))
            PlayerMarket.GetBaseItemMarketCategory(baseItem).Should().NotBeNull($"{baseItem} has a base-item market category");
    }

    [Test]
    public void AppearanceCatalog_MovesUneditedWeaponPartsAndRefreshesTheListing()
    {
        // proto_knife: middle part 14 -> 21; bottom and top parts are unchanged.
        var original = Encode(Item("proto_knife", BaseItem.Dagger, 11, Scalar(0, "ModelPart2", 14), Scalar(0, "ModelPart3", 11)));

        Migrate(original, out var migrated, true, out var converted).Should().BeTrue();

        var root = Read(migrated);
        (Int(root, "ModelPart1"), Int(root, "ModelPart2"), Int(root, "xModelPart2"), Int(root, "ModelPart3")).Should().Be((11, 21, 21, 11));
        converted.Should().ContainSingle().Which.Should().Be(("iwswdg", MarketCategoryType.Vibroknife));

        var customized = Encode(Item("proto_knife", BaseItem.Dagger, 11, Scalar(0, "ModelPart2", 15), Scalar(0, "ModelPart3", 11)));
        Migrate(customized, out var unchanged, true, out _).Should().BeFalse("a part changed from the shipped appearance marks a customized copy");
        unchanged.Should().BeSameAs(customized);
    }

    [Test]
    public void AppearanceCatalog_MovesArmorAndSimpleModelParts()
    {
        var revision = ItemAppearanceCatalogMigration.Revisions["asc_chn_tunic"];
        string[] labels = { "RFoot", "LFoot", "RShin", "LShin", "LThigh", "RThigh", "Pelvis", "Torso", "Belt", "Neck",
            "RFArm", "LFArm", "RBicep", "LBicep", "RShoul", "LShoul", "RHand", "LHand", "Robe" };
        var armor = Encode(Item("asc_chn_tunic", BaseItem.Armor, null,
            labels.Select((label, index) => Scalar(0, "ArmorPart_" + label, revision.Previous[index])).ToArray()));

        Migrate(armor, out var migratedArmor, true, out var armorListing).Should().BeTrue();

        var tunic = Read(migratedArmor);
        labels.Select(label => Int(tunic, "ArmorPart_" + label)).Should().Equal(revision.Current);
        Int(tunic, "xArmorPart_Torso").Should().Be(171);
        armorListing.Should().ContainSingle().Which.Should().Be(("iit_chest", MarketCategoryType.Armor));

        Migrate(Encode(Item("frontier_helmet", BaseItem.Helmet, 96)), out var helmet, true, out _).Should().BeTrue();
        (Int(Read(helmet), "ModelPart1"), Int(Read(helmet), "xModelPart1")).Should().Be((0, 268),
            "the engine saves a part above 255 only in the extended field");

        Migrate(Encode(Item("redcircle_star", BaseItem.Shuriken, 62)), out var star, true, out var starListing).Should().BeTrue();
        (Int(Read(star), "ModelPart1"), Int(Read(star), "xModelPart1")).Should().Be((42, 42));
        starListing.Should().ContainSingle().Which.Should().Be(("iwthsh_042", MarketCategoryType.Throwing));
    }

    [Test]
    public void AppearanceCatalog_MovesShippedHelmetsOffRevansMask()
    {
        // Sentinel's Visor shipped on helmet 223 (Darth Revan's mask and hood); its blueprint now uses 187.
        Migrate(Encode(Item("guardmst_l7", BaseItem.Helmet, 223)), out var visor, true, out _).Should().BeTrue();
        (Int(Read(visor), "ModelPart1"), Int(Read(visor), "xModelPart1")).Should().Be((187, 187));

        // The same pass also revises the visor's stats, so check only the restyled model.
        Migrate(Encode(Item("guardmst_l7", BaseItem.Helmet, 110)), out var customized, true, out _);
        Int(Read(customized), "ModelPart1").Should().Be(110, "a copy restyled away from 223 keeps its choice");
    }

    [TestCase(30, 5, 35)]
    [TestCase(0, 5, 35)]
    [TestCase(0, 30, 30)]
    [TestCase(0, 0, 30)]
    public void ArkanianArmor_ReplacesRetiredIceDefenseWithIceResistance(int iceDefense, int iceResistance, int expectedResistance)
    {
        var shipped = ArkanianArmorStatRevisionCatalog.Changes["ark_armor"];
        var properties = shipped.Where(change => change.Previous > 0)
            .Select(change => Property(change.Type, change.Subtype, 0, change.Previous)).ToList();
        if (iceDefense > 0) properties.Add(Property(ItemPropertyType.Defense, (int)CombatDamageType.Ice, 35, iceDefense));
        if (iceResistance > 0) properties.Add(Property(ItemPropertyType.Resistance, (int)ResistanceType.Ice, 54, iceResistance));
        var original = Encode(Item("ark_armor", BaseItem.Armor, null, properties.ToArray<object>()));

        Migrate(original, out var migrated, true, out _).Should().BeTrue();

        var saved = Children(Read(migrated), "PropertiesList").ToList();
        int Total(ItemPropertyType type, int subtype) => saved
            .Where(p => Int(p, "PropertyName") == (int)type && Int(p, "Subtype") == subtype)
            .Sum(p => Int(p, "CostValue"));
        foreach (var change in shipped)
            Total(change.Type, change.Subtype == 0 && change.Type is not (ItemPropertyType.Defense or ItemPropertyType.RequiresSkill)
                ? ushort.MaxValue : change.Subtype).Should().Be(change.Current, $"{change.Type} {change.Subtype} is revised");
        Total(ItemPropertyType.Defense, (int)CombatDamageType.Ice).Should().Be(0, "the retired Ice Defense is removed");
        Total(ItemPropertyType.Resistance, (int)ResistanceType.Ice).Should().Be(expectedResistance);
        saved.Single(p => Int(p, "PropertyName") == (int)ItemPropertyType.RequiresSkill)
            .Should().Match<GffStruct>(p => Int(p, "Subtype") == (int)SkillType.Armor && Int(p, "CostTable") == 48);
        Locals(Read(migrated)).Should().Contain((ArkanianArmorStatRevisionCatalog.RevisionVariable, ArkanianArmorStatRevisionCatalog.CurrentRevision));
        Migrate(migrated, out _, true, out _).Should().BeFalse();
    }

    // Shipped bracers and wraps: retired Poison Defense alone or beside crafted Poison Resistance, Poison Defense the
    // combat replacement already converted (with and without crafting), and a copy with neither.
    [TestCase(30, 0, 0)]
    [TestCase(30, 5, 5)]
    [TestCase(0, 35, 5)]
    [TestCase(0, 30, 0)]
    [TestCase(0, 0, 0)]
    [TestCase(30, 0, 0, "cara_wraps")]
    [TestCase(30, 5, 5, "cara_wraps")]
    [TestCase(0, 35, 5, "cara_wraps")]
    public void CarapaceBracer_RemovesRetiredPoisonProtection(int poisonDefense, int poisonResistance, int expectedResistance,
        string resref = "cara_bracer")
    {
        var properties = new List<object> { Property(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 35, 23) };
        if (poisonDefense > 0) properties.Add(Property(ItemPropertyType.Defense, (int)CombatDamageType.Poison, 35, poisonDefense));
        if (poisonResistance > 0) properties.Add(Property(ItemPropertyType.Resistance, (int)ResistanceType.Poison, 54, poisonResistance));
        var original = Encode(Item(resref, resref == "cara_wraps" ? BaseItem.Gloves : BaseItem.Bracer, null, properties.ToArray()));

        Migrate(original, out var migrated, true, out _).Should().BeTrue();

        var saved = Children(Read(migrated), "PropertiesList").ToList();
        int Total(ItemPropertyType type, int subtype) => saved
            .Where(p => Int(p, "PropertyName") == (int)type && Int(p, "Subtype") == subtype)
            .Sum(p => Int(p, "CostValue"));
        Total(ItemPropertyType.Defense, (int)CombatDamageType.Physical).Should().Be(23, "the enhanced Physical Defense is kept");
        Total(ItemPropertyType.Defense, (int)CombatDamageType.Poison).Should().Be(0, "the retired Poison Defense is removed");
        Total(ItemPropertyType.Resistance, (int)ResistanceType.Poison).Should().Be(expectedResistance, "only crafted Poison Resistance is kept");
        Locals(Read(migrated)).Should().Contain((CarapaceBracerStatRevisionCatalog.RevisionVariable, CarapaceBracerStatRevisionCatalog.CurrentRevision));
        Migrate(migrated, out _, true, out _).Should().BeFalse();
    }

    [Test]
    public void DroidWeaponSkills_AreAddedAtTheGroupMinimumTheWayTheEngineSavesThem()
    {
        // A controller assembled before the new skills: boosted Katar, and the ranged group at zero.
        var original = Encode(Item("droid_ctrl", (BaseItem)24, 1,
            DroidStat(DroidStatSubType.MGT, 20), DroidStat(DroidStatSubType.Katar, 30), DroidStat(DroidStatSubType.Staff, 25),
            DroidStat(DroidStatSubType.Pistol, 0), DroidStat(DroidStatSubType.Rifle, 0), DroidStat(DroidStatSubType.Throwing, 0)));

        Migrate(original, out var migrated, true, out var converted).Should().BeTrue();

        converted.Should().BeEmpty();
        var properties = Children(Read(migrated), "PropertiesList").ToList();
        properties.Select(p => p.Type).Should().Equal(Enumerable.Range(0, 8).Select(index => (uint)index));
        properties.Skip(6).Select(p => (Int(p, "PropertyName"), Int(p, "Subtype"), Int(p, "CostValue"))).Should().Equal(
            ((int)ItemPropertyType.DroidStat, (int)DroidStatSubType.Unarmed, 25),
            ((int)ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinPistol, 0));
        foreach (var added in properties.Skip(6))
        {
            added.Fields.Select(f => f.Label).Should().Equal("PropertyName", "Subtype", "CostTable", "CostValue",
                "Param1", "Param1Value", "ChanceAppear", "UsesPerDay", "Useable", "CustomTag");
            (Int(added, "CostTable"), Int(added, "Param1"), Int(added, "Param1Value"), Int(added, "ChanceAppear"),
                Int(added, "UsesPerDay"), Int(added, "Useable")).Should().Be((45, 255, 255, 100, 255, 1));
        }
        Read(migrated).Fields.Should().NotContain(f => f.Label == "VarTable", "the rule needs no revision local");

        Migrate(migrated, out var rerun, true, out _).Should().BeFalse("an item with the new skills is current");
        rerun.Should().BeSameAs(migrated);
    }

    [Test]
    public void DroidWeaponSkills_LeaveIncompleteGroupsAndCurrentItemsAlone()
    {
        var boost = Encode(Item("droid_part", (BaseItem)24, 1, DroidStat(DroidStatSubType.Katar, 5)));
        Migrate(boost, out var unchangedBoost, true, out _).Should().BeFalse("a single Katar boost is not the martial group");
        unchangedBoost.Should().BeSameAs(boost);

        var current = Encode(Item("droid_cpu", (BaseItem)24, 1, DroidStat(DroidStatSubType.Katar, 25), DroidStat(DroidStatSubType.Staff, 25),
            DroidStat(DroidStatSubType.Unarmed, 25), DroidStat(DroidStatSubType.Pistol, 25), DroidStat(DroidStatSubType.Rifle, 25),
            DroidStat(DroidStatSubType.Throwing, 25), DroidStat(DroidStatSubType.TwinPistol, 25)));
        Migrate(current, out var unchangedCurrent, true, out _).Should().BeFalse();
        unchangedCurrent.Should().BeSameAs(current);
    }

    [Test]
    public void DroidWeaponSkills_RunOnlyInReleaseMigrations()
    {
        // The release migrations update artwork; earlier passes that do not are left as they shipped.
        var original = Encode(Item("droid_cpu", (BaseItem)24, 1, DroidStat(DroidStatSubType.Katar, 25), DroidStat(DroidStatSubType.Staff, 25)));
        var arguments = new object?[] { original, null, true, null, false };
        ((bool)Document.GetMethod("MigrateStoredItemData", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments)!)
            .Should().BeFalse();
    }

    [Test]
    public void DroidWeaponSkills_ReachTheControllersSavedParts()
    {
        var cpu = Encode(Item("droid_cpu", (BaseItem)24, 1, DroidStat(DroidStatSubType.Katar, 25), DroidStat(DroidStatSubType.Staff, 25),
            DroidStat(DroidStatSubType.Pistol, 25), DroidStat(DroidStatSubType.Rifle, 25), DroidStat(DroidStatSubType.Throwing, 25)));
        var droid = JsonConvert.SerializeObject(new ConstructedDroid { SerializedCPU = cpu });
        var original = Encode(Item("droid_ctrl", (BaseItem)24, 1,
            DroidStat(DroidStatSubType.Katar, 25), DroidStat(DroidStatSubType.Staff, 25),
            List("VarTable", Node(0, Text("Name", "CONSTRUCTED_DROID"), Scalar(4, "Type", 3), Text("Value", droid)))));

        Migrate(original, out var migrated, true, out _).Should().BeTrue();

        var root = Read(migrated);
        Children(root, "PropertiesList").Should().ContainSingle(p => Int(p, "Subtype") == (int)DroidStatSubType.Unarmed && Int(p, "CostValue") == 25);
        var saved = (string)Value(Children(root, "VarTable").Single(), "Value");
        var part = Children(Read(JsonConvert.DeserializeObject<ConstructedDroid>(saved)!.SerializedCPU), "PropertiesList").ToList();
        part.Where(p => Int(p, "PropertyName") == (int)ItemPropertyType.DroidStat)
            .Select(p => (Int(p, "Subtype"), Int(p, "CostValue"))).Should().Contain(new[]
            {
                ((int)DroidStatSubType.Unarmed, 25), ((int)DroidStatSubType.TwinPistol, 25),
            });
        Migrate(migrated, out _, true, out _).Should().BeFalse();
    }

    // Blueprint-made droid parts save Param1Value 0; the engine-saved properties a migration adds save 255.
    private static PropertyFixture DroidStat(DroidStatSubType subtype, int value) =>
        Property(ItemPropertyType.DroidStat, (int)subtype, 45, value, param1Value: 0);

    private static bool Migrate(string original, out string migrated, bool repairHeadgear, out List<(string Icon, MarketCategoryType Category)> converted)
    {
        var sink = new List<object>();
        var capture = typeof(SavedDataItemSweepTests).GetMethod(nameof(Capture), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(ListingType).Invoke(null, new object[] { sink });
        var arguments = new[] { original, null, repairHeadgear, capture, true };
        var changed = (bool)Document.GetMethod("MigrateStoredItemData", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments)!;
        migrated = (string)arguments[1]!;
        converted = sink.Select(value => ((string)ListingType.GetProperty("IconResref")!.GetValue(value)!,
            (MarketCategoryType)ListingType.GetProperty("Category")!.GetValue(value)!)).ToList();
        return changed;
    }

    private static Action<T> Capture<T>(List<object> sink) => value => sink.Add(value!);

    private static object Item(string resref, BaseItem baseItem, int? model, params object[] fields)
    {
        var all = new List<object> { Resref(resref), Scalar(5, "BaseItem", (int)baseItem) };
        if (model != null)
        {
            all.Add(Scalar(0, "ModelPart1", model.Value));
            all.Add(Scalar(2, "xModelPart1", model.Value));
        }
        var properties = fields.OfType<PropertyFixture>().Select((property, index) => Node((uint)index, property.Fields)).ToArray();
        all.AddRange(fields.Where(field => field is not PropertyFixture));
        if (properties.Length > 0) all.Add(List("PropertiesList", properties));
        return Node(0, all.ToArray());
    }

    private sealed record PropertyFixture(object[] Fields);

    private static PropertyFixture Property(ItemPropertyType type, int subtype, int costTable, int value, int param1Value = 255) =>
        new(new[] { Scalar(2, "PropertyName", (int)type), Scalar(2, "Subtype", subtype), Scalar(0, "CostTable", costTable),
            Scalar(2, "CostValue", value), Scalar(0, "Param1", 255), Scalar(0, "Param1Value", param1Value),
            Scalar(0, "ChanceAppear", 100), Scalar(0, "UsesPerDay", 255), Scalar(0, "Useable", 1), Text("CustomTag", "") });

    private static object Node(uint type, params object[] fields)
    {
        var node = Activator.CreateInstance(NodeType, true)!;
        NodeType.GetField("Type")!.SetValue(node, type);
        var list = (System.Collections.IList)NodeType.GetField("Fields")!.GetValue(node)!;
        foreach (var field in fields) list.Add(field);
        return node;
    }

    private static object Field(uint type, string name, byte[]? data = null, params object[] children)
    {
        object? list = null;
        if (type == 15)
        {
            list = Activator.CreateInstance(typeof(List<>).MakeGenericType(NodeType))!;
            foreach (var child in children) ((System.Collections.IList)list).Add(child);
        }
        return Document.GetMethod("MakeField", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { (object)type, name, data, list })!;
    }

    private static object Scalar(uint type, string name, int value) => Field(type, name, BitConverter.GetBytes(value));
    private static object Text(string name, string text) { var bytes = Encoding.UTF8.GetBytes(text); return Field(10, name, BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray()); }
    private static object Resref(string resref) { var bytes = Encoding.ASCII.GetBytes(resref); return Field(11, "TemplateResRef", new[] { (byte)bytes.Length }.Concat(bytes).ToArray()); }
    private static object List(string name, params object[] children) => Field(15, name, null, children);

    private static string Encode(object root, string fileType = "UTI ")
    {
        NodeType.GetField("Type")!.SetValue(root, uint.MaxValue);
        var document = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Document);
        Document.GetField("_header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, Encoding.ASCII.GetBytes(fileType + "V3.2"));
        Document.GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, root);
        return (string)Document.GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(document, null)!;
    }

    private static GffStruct Read(string data) => GffReader.Read(Convert.FromBase64String(data)).RootStruct;
    private static object Value(GffStruct node, string name) => node.Fields.Single(x => x.Label == name).Value!;
    private static int Int(GffStruct node, string name) => Convert.ToInt32(Value(node, name));
    private static IEnumerable<GffStruct> Children(GffStruct node, string name) => ((GffList)Value(node, name)).Elements;
    private static IEnumerable<(string, int)> Locals(GffStruct node) =>
        Children(node, "VarTable").Select(variable => ((string)Value(variable, "Name"), Int(variable, "Value")));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR_Haks", "sw_2da", "itempropdef.2da")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_Haks/sw_2da.");
    }
}
