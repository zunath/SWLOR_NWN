#nullable enable
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Game.Server.Tests.Feature;

public partial class SavedDataItemSweepTests
{
    // AppearanceArmor order, as saved.
    private static readonly string[] SavedArmorParts =
    {
        "RFoot", "LFoot", "RShin", "LShin", "LThigh", "RThigh", "Pelvis", "Torso", "Belt", "Neck",
        "RFArm", "LFArm", "RBicep", "LBicep", "RShoul", "LShoul", "RHand", "LHand", "Robe",
    };

    private static readonly string[] KeptArmorParts = { "Belt", "RShoul", "LShoul", "Robe" };

    private static string ExtendedPart(string part) => ("xArmorPart_" + part).Length > 16 ? ("xArmorPart_" + part)[..16] : "xArmorPart_" + part;

    private static object[] ArmorParts(Func<string, int> style, bool extended = true) => SavedArmorParts
        .SelectMany(part => extended
            ? new[] { Scalar(0, "ArmorPart_" + part, style(part)), Scalar(2, ExtendedPart(part), style(part)) }
            : new[] { Scalar(0, "ArmorPart_" + part, style(part)) })
        .ToArray();

    private static object Armor(string id, Func<string, int> style, params object[] fields) =>
        Item("armor", BaseItem.Armor, null, ArmorParts(style).Concat(new[] { Text("UUID", id) }).Concat(fields).ToArray());

    private static void ShouldBeMovedOffStyleTwo(GffStruct armor, string id)
    {
        Value(armor, "UUID").Should().Be(id);
        foreach (var part in SavedArmorParts)
        {
            var expected = KeptArmorParts.Contains(part) ? 2 : 1;
            Int(armor, "ArmorPart_" + part).Should().Be(expected, part);
            Int(armor, ExtendedPart(part)).Should().Be(expected, part);
        }
    }

    [Test]
    public void RetiredArmorStyle_ReplacementIsListedOnEverySlotItMoves()
    {
        RetiredArmorStyleMigration.Parts.Select(part => SavedArmorParts[(int)part])
            .Should().BeEquivalentTo(SavedArmorParts.Except(KeptArmorParts));
        foreach (var (type, definition) in SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.ArmorAppearanceCatalog.Definitions)
        foreach (var part in RetiredArmorStyleMigration.Parts)
        {
            var styles = SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.ArmorAppearanceCatalog.GetStyles(definition, part);
            styles.Should().Contain(RetiredArmorStyleMigration.Replacement, $"{type} {part}");
            styles.Should().NotContain(RetiredArmorStyleMigration.Removed, $"{type} {part}");
        }
    }

    [Test]
    public void RetiredArmorStyle_MovesStyleTwoToOneAndKeepsEverythingElse()
    {
        var original = Encode(Armor("worn-armor", _ => 2,
            Scalar(0, "Metal1Color", 63), Scalar(0, "Cloth1Color", 12),
            List("VarTable", Node(0, Text("Name", "TINT_STATE"), Scalar(4, "Type", 1), Scalar(5, "Value", 42)))));

        RetiredArmorStyleMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        ShouldBeMovedOffStyleTwo(root, "worn-armor");
        (Int(root, "Metal1Color"), Int(root, "Cloth1Color")).Should().Be((63, 12), "dyes are kept");
        Locals(root).Should().Equal(("TINT_STATE", 42));

        RetiredArmorStyleMigration.MigrateSavedObject(migrated, out var rerun).Should().BeFalse("nothing is left on style 2");
        rerun.Should().BeSameAs(migrated);
    }

    [Test]
    public void RetiredArmorStyle_ReadsTheByteFieldWhenNoExtendedFieldIsSaved()
    {
        var original = Encode(Item("armor", BaseItem.Armor, null, ArmorParts(part => part == "Torso" ? 2 : 4, extended: false)));

        RetiredArmorStyleMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        (Int(root, "ArmorPart_Torso"), Int(root, "xArmorPart_Torso")).Should().Be((1, 1));
        Int(root, "ArmorPart_Neck").Should().Be(4);
    }

    [Test]
    public void RetiredArmorStyle_LeavesOtherStylesNonArmorItemsAndBodyPartsUntouched()
    {
        foreach (var style in new[] { 0, 1, 3, 200 })
        {
            var armor = Encode(Armor("kept", _ => style));
            RetiredArmorStyleMigration.MigrateSavedObject(armor, out var migrated).Should().BeFalse(style.ToString());
            migrated.Should().BeSameAs(armor);
        }
        var cloak = Encode(Item("cloak", BaseItem.Cloak, 2, ArmorParts(_ => 2)));
        RetiredArmorStyleMigration.MigrateSavedObject(cloak, out _).Should().BeFalse("only armor has armor parts");

        // Body part 2 is a real body (tattooed arms), not an armor style.
        var creature = Encode(Node(0, Text("Tag", "saved-npc"), Scalar(0, "BodyPart_RBicep", 2), Scalar(0, "BodyPart_Neck", 2),
            List("Equip_ItemList", Armor("npc-armor", _ => 2))), "UTC ");
        RetiredArmorStyleMigration.MigrateSavedObject(creature, out var npc).Should().BeTrue();
        var root = Read(npc);
        (Int(root, "BodyPart_RBicep"), Int(root, "BodyPart_Neck")).Should().Be((2, 2));
        ShouldBeMovedOffStyleTwo(Children(root, "Equip_ItemList").Single(), "npc-armor");
    }

    [Test]
    public void RetiredArmorStyle_TraversesBagsAndDroids()
    {
        var droid = new JObject
        {
            ["SerializedBody"] = Encode(Node(0, Text("Tag", "droid-body"), Scalar(0, "BodyPart_Torso", 2)), "UTC "),
            ["EquippedItems"] = new JObject { ["Chest"] = Encode(Armor("droid-armor", _ => 2)) },
            ["Inventory"] = new JObject { ["owned-id"] = Encode(Armor("droid-spare", _ => 2)) },
            ["Name"] = "Retained droid",
        };
        var controller = Item("droid_ctrl", (BaseItem)24, 1, List("VarTable", Node(0,
            Text("Name", "CONSTRUCTED_DROID"), Scalar(4, "Type", 3), Text("Value", droid.ToString(Formatting.None)))));
        var original = Encode(Item("bag", BaseItem.LargeBox, 1, List("ItemList", Armor("bagged", _ => 2), controller)));

        RetiredArmorStyleMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var items = Children(Read(migrated), "ItemList").ToList();
        ShouldBeMovedOffStyleTwo(items[0], "bagged");
        var converted = JObject.Parse((string)Value(Children(items[1], "VarTable").Single(), "Value"));
        converted["Name"]!.Value<string>().Should().Be("Retained droid");
        Int(Read((string)converted["SerializedBody"]!), "BodyPart_Torso").Should().Be(2);
        ShouldBeMovedOffStyleTwo(Read((string)converted["EquippedItems"]!["Chest"]!), "droid-armor");
        ShouldBeMovedOffStyleTwo(Read((string)converted["Inventory"]!["owned-id"]!), "droid-spare");
        RetiredArmorStyleMigration.MigrateSavedObject(migrated, out _).Should().BeFalse();
    }

    [Test]
    public void ServerMigration31_MovesEnzymesAndArmorStylesInOneDecode()
    {
        var converterType = ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.EnzymeAndArmorStyleConverter")!;
        var converter = converterType.GetField("Instance")!.GetValue(null)!;
        var original = Encode(Item("bag", BaseItem.LargeBox, 1, List("ItemList",
            Item("hydrolase_blue", BaseItem.Electronics5, 3, Text("UUID", "enzyme")), Armor("armor", _ => 2))));

        bool Convert(string data, out string result)
        {
            var arguments = new object?[] { data, null, true, null };
            var changed = (bool)converterType.GetMethod("MigrateStoredObjectData")!.Invoke(converter, arguments)!;
            result = (string)arguments[1]!;
            return changed;
        }

        Convert(original, out var migrated).Should().BeTrue();
        var items = Children(Read(migrated), "ItemList").ToList();
        (Int(items[0], "BaseItem"), Int(items[0], "ModelPart1")).Should().Be((EnzymeBaseItem, 1));
        ShouldBeMovedOffStyleTwo(items[1], "armor");
        Convert(migrated, out var rerun).Should().BeFalse();
        rerun.Should().BeSameAs(migrated);

        var armorOnly = Encode(Armor("only-armor", _ => 2));
        Convert(armorOnly, out var armorMigrated).Should().BeTrue("a record with no enzyme still moves its armor");
        ShouldBeMovedOffStyleTwo(Read(armorMigrated), "only-armor");
    }

    [Test]
    public void PlayerMigration26_MovesWornAndCarriedArmorAndKeepsBodyParts()
    {
        const BindingFlags members = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        var characterType = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.SavedCharacter")!;
        var migrationType = ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration._26_ConvertCraftingPerksToRanks")!;
        var migration = Activator.CreateInstance(migrationType, true)!;
        var record = new Player { Version = 26, CharacterFileVersion = 26, HP = 17, FP = 13, Stamina = 11 };
        var worn = Item("armor", BaseItem.Armor, null, ArmorParts(_ => 2).Append(Text("UUID", "worn")).ToArray());
        NodeType.GetField("Type")!.SetValue(worn, 2u); // chest slot
        var original = Encode(Node(0, Scalar(0, "BodyPart_RBicep", 2), Scalar(0, "BodyPart_Neck", 2),
            List("Equip_ItemList", worn),
            List("ItemList", Armor("carried", _ => 2, Scalar(0, "Repos_PosX", 2), Scalar(0, "Repos_Posy", 4)),
                Item("bag", BaseItem.LargeBox, 1, Text("UUID", "player-bag"), List("ItemList", Armor("bagged", _ => 2))))), "BIC ");
        var current = original;
        for (var pass = 0; pass < 3; pass++)
        {
            var character = Activator.CreateInstance(characterType, members, null,
                new object[] { System.Convert.FromBase64String(current), record }, null)!;
            characterType.GetProperty("AppliesRecordEffects", members)!.SetValue(character, false);
            migrationType.GetMethod("Migrate")!.Invoke(migration, new[] { character });
            var migrated = System.Convert.ToBase64String((byte[])characterType.GetMethod("Serialize")!.Invoke(character, null)!);
            if (pass > 0) migrated.Should().Be(current);
            current = migrated;
            var root = Read(current);
            (Int(root, "BodyPart_RBicep"), Int(root, "BodyPart_Neck")).Should().Be((2, 2), "body parts are not armor styles");
            ShouldBeMovedOffStyleTwo(Children(root, "Equip_ItemList").Single(), "worn");
            var carried = Children(root, "ItemList").ToList();
            ShouldBeMovedOffStyleTwo(carried[0], "carried");
            (Int(carried[0], "Repos_PosX"), Int(carried[0], "Repos_Posy")).Should().Be((2, 4), "the armor keeps its inventory cell");
            ShouldBeMovedOffStyleTwo(Children(carried[1], "ItemList").Single(), "bagged");
        }
    }
}
