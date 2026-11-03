#nullable enable
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public partial class SavedDataItemSweepTests
{
    [Test]
    public void PelvisConsolidation_InheritsDyesOnThePelvisAloneAndDoesNotRerunShippedRules()
    {
        var original = Encode(Item("precision_optic", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Pelvis", 35), Scalar(0, "ArmorPart_Torso", 186),
            Scalar(0, "Cloth1Color", 23), Scalar(0, "Cloth2Color", 57), Scalar(0, "Leather1Color", 9),
            Scalar(2, "StackSize", 4), Text("UUID", "retained-item"),
            List("VarTable", PelvisLocal("TM_pfh0_chest186_4", 0x01010203))));

        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        Int(root, "ArmorPart_Pelvis").Should().Be(34);
        Int(root, "xArmorPart_Pelvi").Should().Be(34);
        Int(root, "ArmorPart_Torso").Should().Be(186);
        Int(root, "BaseItem").Should().Be((int)BaseItem.Armor, "the new release must not rerun the old headgear conversion");
        Int(root, "StackSize").Should().Be(4);
        Value(root, "UUID").Should().Be("retained-item");
        (Int(root, "Cloth1Color"), Int(root, "Cloth2Color")).Should().Be((23, 57));
        (Int(root, "APart_6_Col_2"), Int(root, "APart_6_Col_3")).Should().Be((57, 23));
        var locals = Locals(root).ToDictionary(pair => pair.Item1, pair => pair.Item2);
        locals["TM_pfh0_pelvis034_4"].Should().Be(58);
        locals["TM_pfh0_pelvis034_5"].Should().Be(24);
        locals["TM_pmh0_pelvis034_4"].Should().Be(58);
        locals["TM_pmh0_pelvis034_5"].Should().Be(24);
        locals["TM_pfh0_pelvis034_6"].Should().Be(10);
        locals["TM_pfh0_chest186_4"].Should().Be(0x01010203);
        locals["APC_6_2"].Should().Be(1);
        locals["APC_6_3"].Should().Be(1);
        root.Fields.Should().NotContain(field => field.Label == "PropertiesList");
        AssertPelvisStable(migrated);
    }

    [Test]
    public void PelvisConsolidation_PreservesRgbAndPaletteOverridesAndDoesNotSwapGlobalTint()
    {
        var original = Encode(Item("custom_armor", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Pelvis", 35), Scalar(0, "Cloth1Color", 23), Scalar(0, "Cloth2Color", 57),
            List("VarTable",
                PelvisLocal("TM_pfh0_pelvis035_4", 0x01112233), PelvisLocal("TM_pfh0_pelvis035_5", 1),
                PelvisLocal("TM_pfh0_pelvis035_6", 0x01445566), PelvisLocal("TM_pfh0_pelvis034_4", 0x01ABCDEF),
                PelvisLocal("TM_pmh0_pelvis035_4", 0x01778899),
                PelvisLocal("TMG_4", 0x01FEDCBA), PelvisLocal("TMG_5", 0x01987654), PelvisLocal("TMI_4", 1),
                PelvisLocal("TM_pfh0_chest186_4", 0x01010203))));

        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var locals = Locals(Read(migrated)).ToDictionary(pair => pair.Item1, pair => pair.Item2);
        locals["TM_pfh0_pelvis034_5"].Should().Be(0x01112233);
        locals["TM_pfh0_pelvis034_4"].Should().Be(1, "active style 35 wins over stale style 34 overrides");
        locals["TM_pfh0_pelvis034_6"].Should().Be(0x01445566);
        locals["TM_pmh0_pelvis034_5"].Should().Be(0x01778899);
        locals["TM_pmh0_pelvis034_4"].Should().Be(0x01987654, "the inherited global RGB becomes a pelvis-only override");
        locals["TMG_4"].Should().Be(0x01FEDCBA);
        locals["TMG_5"].Should().Be(0x01987654);
        locals["TMI_4"].Should().Be(1);
        locals["TM_pfh0_chest186_4"].Should().Be(0x01010203);
        locals.Keys.Should().NotContain(key => key.Contains("pelvis035"));
        AssertPelvisStable(migrated);
    }

    [Test]
    public void PelvisConsolidation_TraversesEquippedNestedAndDroidItemsWithoutChangingOwnership()
    {
        string Armor(string id) => Encode(Item("custom_armor", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Pelvis", 35), Text("UUID", id), Scalar(2, "StackSize", 3)));
        var droid = new JObject
        {
            ["SerializedBody"] = Armor("body"),
            ["EquippedItems"] = new JObject { ["Chest"] = Armor("worn") },
            ["Inventory"] = new JObject { ["owned-id"] = Armor("carried") },
            ["Name"] = "Retained droid",
        };
        var controller = Item("droid_ctrl", (BaseItem)24, 1, List("VarTable", Node(0,
            Text("Name", "CONSTRUCTED_DROID"), Scalar(4, "Type", 3), Text("Value", droid.ToString(Formatting.None)))));
        var bag = Item("bag", BaseItem.LargeBox, 1,
            List("ItemList", Item("custom_armor", BaseItem.Armor, null,
                Scalar(0, "ArmorPart_Pelvis", 35), Text("UUID", "nested")), controller));
        var original = Encode(Node(0, Scalar(0, "BodyPart_Pelvis", 35), Text("Tag", "saved-npc"),
            List("Equip_ItemList", Item("custom_armor", BaseItem.Armor, null, Scalar(0, "ArmorPart_Pelvis", 35))),
            List("ItemList", bag)), "UTC ");

        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        Int(root, "BodyPart_Pelvis").Should().Be(34);
        Value(root, "Tag").Should().Be("saved-npc");
        Int(Children(root, "Equip_ItemList").Single(), "ArmorPart_Pelvis").Should().Be(34);
        var nested = Children(Children(root, "ItemList").Single(), "ItemList").ToList();
        Int(nested[0], "ArmorPart_Pelvis").Should().Be(34);
        Value(nested[0], "UUID").Should().Be("nested");
        var converted = JObject.Parse((string)Value(Children(nested[1], "VarTable").Single(), "Value"));
        converted["Name"]!.Value<string>().Should().Be("Retained droid");
        foreach (var (value, id) in new[] { ((string)converted["SerializedBody"]!, "body"),
                     ((string)converted["EquippedItems"]!["Chest"]!, "worn"), ((string)converted["Inventory"]!["owned-id"]!, "carried") })
        {
            var armor = Read(value);
            Int(armor, "ArmorPart_Pelvis").Should().Be(34);
            Value(armor, "UUID").Should().Be(id);
            Int(armor, "StackSize").Should().Be(3);
        }
        AssertPelvisStable(migrated);
    }

    [Test]
    public void PelvisConsolidation_CharacterRetryChangesOnlyTheOldFile()
    {
        const BindingFlags members = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        var characterType = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.SavedCharacter")!;
        var migrationType = ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration._25_ConsolidatePelvisAppearance")!;
        var migration = Activator.CreateInstance(migrationType, true)!;
        var record = new Player { Version = 25, CharacterFileVersion = 25, HP = 17, FP = 13, Stamina = 11,
            CreatureTintOverrides = new Dictionary<string, int> { ["TMC_0"] = 0x01010203 } };
        var beforeRecord = JsonConvert.SerializeObject(record);
        var original = Encode(Node(0, Scalar(0, "BodyPart_Pelvis", 35),
            List("ItemList", Item("custom_armor", BaseItem.Armor, null,
                Scalar(0, "ArmorPart_Pelvis", 35), Scalar(2, "StackSize", 2), Text("UUID", "player-owned")))), "BIC ");
        var current = original;
        for (var pass = 0; pass < 3; pass++)
        {
            var character = Activator.CreateInstance(characterType, members, null,
                new object[] { Convert.FromBase64String(current), record }, null)!;
            characterType.GetProperty("AppliesRecordEffects", members)!.SetValue(character, false);
            migrationType.GetMethod("Migrate")!.Invoke(migration, new[] { character });
            var bytes = (byte[])characterType.GetMethod("Serialize")!.Invoke(character, null)!;
            var migrated = Convert.ToBase64String(bytes);
            if (pass > 0) migrated.Should().Be(current);
            current = migrated;
            var root = Read(current);
            Int(root, "BodyPart_Pelvis").Should().Be(34);
            var armor = Children(root, "ItemList").Single();
            Int(armor, "ArmorPart_Pelvis").Should().Be(34);
            Int(armor, "StackSize").Should().Be(2);
            Value(armor, "UUID").Should().Be("player-owned");
            JsonConvert.SerializeObject(record).Should().Be(beforeRecord);
        }
    }

    [Test]
    public void PelvisConsolidation_LeavesRetainedStyleByteForByteAndHidesOnlyTheDuplicate()
    {
        var original = Encode(Item("custom_armor", BaseItem.Armor, null, Scalar(0, "ArmorPart_Pelvis", 34),
            List("VarTable", PelvisLocal("TM_pfh0_pelvis035_4", 0x01112233))));
        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeFalse();
        migrated.Should().BeSameAs(original);
        var catalog = new GeneralArmorAppearanceDefinition();
        catalog.Pelvis.Should().Contain(34).And.NotContain(35);
    }

    [Test]
    public void PelvisConsolidation_OutfitListingIsIdempotentAndKeepsOtherParts()
    {
        var outfit = new PlayerOutfit { PelvisId = 35, TorsoId = 35, Name = "Saved colors" };
        var migrate = typeof(PelvisAppearanceMigration).GetMethod("MigrateOutfitListing", BindingFlags.NonPublic | BindingFlags.Static)!;
        migrate.Invoke(null, new object[] { outfit }).Should().Be(true);
        outfit.PelvisId.Should().Be(34);
        outfit.TorsoId.Should().Be(35);
        outfit.Name.Should().Be("Saved colors");
        migrate.Invoke(null, new object[] { outfit }).Should().Be(false);
        migrate.Invoke(null, new object[] { outfit }).Should().Be(false);
    }

    [Test]
    public void PelvisConsolidation_ExtendedStyleWinsAndUnrelatedItemsStayUntouched()
    {
        var alreadyCurrent = Encode(Item("custom_armor", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Pelvis", 35), Scalar(2, "xArmorPart_Pelvi", 34)));
        PelvisAppearanceMigration.MigrateSavedObject(alreadyCurrent, out var unchanged).Should().BeFalse();
        unchanged.Should().BeSameAs(alreadyCurrent);
        var legacy = Encode(Item("custom_armor", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Pelvis", 34), Scalar(2, "xArmorPart_Pelvi", 35)));
        PelvisAppearanceMigration.MigrateSavedObject(legacy, out var migrated).Should().BeTrue();
        Int(Read(migrated), "ArmorPart_Pelvis").Should().Be(34);
        Int(Read(migrated), "xArmorPart_Pelvi").Should().Be(34);
        var weapon = Encode(Item("custom_weapon", BaseItem.Longsword, 35, Scalar(0, "ArmorPart_Pelvis", 35)));
        PelvisAppearanceMigration.MigrateSavedObject(weapon, out unchanged).Should().BeFalse();
        unchanged.Should().BeSameAs(weapon);
    }

    [TestCase(0, false, 23)]
    [TestCase(0, true, 0)]
    [TestCase(255, false, 23)]
    [TestCase(255, true, 23)]
    [TestCase(42, false, 42)]
    public void PelvisConsolidation_PreservesExplicitZeroAndInheritedNativePalettes(int partColor, bool explicitColor, int expected)
    {
        var original = Encode(Item("custom_armor", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Pelvis", 35), Scalar(0, "Cloth1Color", 23), Scalar(0, "Cloth2Color", 57),
            Scalar(0, "APart_6_Col_2", partColor), Scalar(0, "APart_6_Col_3", 59), Scalar(0, "APart_7_Col_2", 61),
            List("VarTable", PelvisLocal("APC_6_2", explicitColor ? 1 : 0))));

        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        Int(root, "APart_6_Col_2").Should().Be(59);
        Int(root, "APart_6_Col_3").Should().Be(expected);
        Int(root, "APart_7_Col_2").Should().Be(61, "a torso's independent palette remains unchanged");
        (Int(root, "Cloth1Color"), Int(root, "Cloth2Color")).Should().Be((23, 57));
        var locals = Locals(root).ToDictionary(pair => pair.Item1, pair => pair.Item2);
        locals["TM_pfh0_pelvis034_4"].Should().Be(60);
        locals["TM_pfh0_pelvis034_5"].Should().Be(expected + 1);
        AssertPelvisStable(migrated);
    }

    [Test]
    public void PelvisConsolidation_ExplicitPaletteOptOutStillWinsOverGlobalRgb()
    {
        var original = Encode(Item("custom_armor", BaseItem.Armor, null,
            Scalar(0, "ArmorPart_Pelvis", 35), Scalar(0, "Cloth1Color", 23), Scalar(0, "Cloth2Color", 57),
            Scalar(0, "APart_6_Col_2", 0),
            List("VarTable", PelvisLocal("APC_6_2", 1), PelvisLocal("TMG_4", 0x01010203), PelvisLocal("TMG_5", 0x01040506))));
        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var locals = Locals(Read(migrated)).ToDictionary(pair => pair.Item1, pair => pair.Item2);
        locals["TM_pfh0_pelvis034_5"].Should().Be(1);
        locals["TM_pfh0_pelvis034_4"].Should().Be(0x01040506);
        locals["TMG_4"].Should().Be(0x01010203);
        AssertPelvisStable(migrated);
    }

    [TestCase(false)]
    [TestCase(true)]
    [NonParallelizable]
    public void PelvisConsolidation_NormalizesLegacyRgbInheritanceWithoutChangingExplicitSiblingColors(bool alreadyExplicit)
    {
        var indexField = typeof(TintMapMaterialRegistry).GetField("_equipmentMaterialIndex", BindingFlags.NonPublic | BindingFlags.Static)!;
        var originalIndex = indexField.GetValue(null);
        var layers = new[] { TintMapLayerType.Cloth1, TintMapLayerType.Cloth2 };
        indexField.SetValue(null, new TintMapEquipmentMaterialIndex(new Dictionary<string, IReadOnlyList<TintMapMaterialDefinition>>
        {
            ["pfh0_chest001"] = new[] { new TintMapMaterialDefinition("Torso", "torso_inherit", layers) },
            ["pfh0_neck001"] = new[] { new TintMapMaterialDefinition("Neck", "neck_explicit", layers) },
        }));
        try
        {
            const int rgb = 0x01112233;
            var original = Encode(Item("custom_armor", BaseItem.Armor, null, Scalar(0, "ArmorPart_Pelvis", 35),
                List("VarTable", PelvisLocal("TMG_4", rgb), PelvisLocal("TMG_5", rgb),
                    PelvisLocal("TMI_4", alreadyExplicit ? 1 : 0), PelvisLocal("TMI_5", alreadyExplicit ? 1 : 0),
                    PelvisLocal("TM_torso_inherit_4", rgb), PelvisLocal("TM_neck_explicit_4", rgb),
                    PelvisLocal("APC_9_2", 1), PelvisLocal("TM_unknown_4", rgb))));
            PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
            var locals = Locals(Read(migrated)).ToDictionary(pair => pair.Item1, pair => pair.Item2);
            locals["TMI_4"].Should().Be(1);
            locals["TMI_5"].Should().Be(1);
            locals["TMG_4"].Should().Be(rgb);
            locals["TMG_5"].Should().Be(rgb);
            locals["TM_pfh0_pelvis034_4"].Should().Be(rgb);
            locals["TM_pfh0_pelvis034_5"].Should().Be(rgb);
            locals["TM_neck_explicit_4"].Should().Be(rgb, "an explicit sibling must not fall back to its native palette");
            locals["TM_unknown_4"].Should().Be(rgb, "an unresolved material cannot safely be classified as inherited");
            if (alreadyExplicit)
                locals["TM_torso_inherit_4"].Should().Be(rgb, "the explicit inheritance contract already protects independent colors");
            else
                locals.Should().NotContainKey("TM_torso_inherit_4", "the sibling continues inheriting TMG when the global color changes");
            AssertPelvisStable(migrated);
        }
        finally
        {
            indexField.SetValue(null, originalIndex);
        }
    }

    private static object PelvisLocal(string name, int value) =>
        Node(0, Text("Name", name), Scalar(4, "Type", 1), Scalar(5, "Value", value));

    private static void AssertPelvisStable(string migrated)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            PelvisAppearanceMigration.MigrateSavedObject(migrated, out var retry).Should().BeFalse();
            retry.Should().BeSameAs(migrated);
            migrated = retry;
        }
    }
}
