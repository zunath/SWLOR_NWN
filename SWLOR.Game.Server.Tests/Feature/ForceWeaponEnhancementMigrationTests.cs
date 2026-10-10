using System.Reflection;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Game.Server.Tests.Feature;

public class ForceWeaponEnhancementMigrationTests
{
    [Test]
    public void ReleasedServersAndPlayersReceiveNewMigrationVersions()
    {
        var server = new _23_RetireForceWeaponEnhancements();
        server.Version.Should().Be(23);
        server.ExecutionType.Should().Be(MigrationExecutionType.PostCacheLoad);
        new _16_RetireForceWeaponEnhancements().Version.Should().Be(16);
    }

    [TestCase(CombatDamageType.Force, CombatDamageType.Physical)]
    [TestCase(CombatDamageType.Fire, CombatDamageType.Fire)]
    [TestCase(CombatDamageType.Ice, CombatDamageType.Ice)]
    [TestCase(CombatDamageType.Poison, CombatDamageType.Poison)]
    [TestCase(CombatDamageType.Electrical, CombatDamageType.Electrical)]
    public void CraftingRetiresOnlyForceConversion(CombatDamageType input, CombatDamageType expected)
    {
        Craft.TryGetWeaponDamageTypeForEnhancement(EnhancementSubType.DMG, input, out var actual).Should().BeTrue();
        actual.Should().Be(expected);
    }

    [Test]
    public void RetiredRecipesAreRemovedAndKnowledgeMovesToExistingDmgRecipesOnce()
    {
        var recipes = new EnhancementRecipes().BuildRecipes();
        var player = new Player("force-enhancement-test");
        var date = new DateTime(2026, 10, 1);
        var existing = date.AddDays(-1);
        foreach (var recipe in new[] { RecipeType.WeaponEnhancementDMGForce1,
                     RecipeType.WeaponEnhancementDMGForce2, RecipeType.WeaponEnhancementDMGForce3 })
        {
            recipes.Should().NotContainKey(recipe);
            recipes[ForceWeaponEnhancementMigration.GetReplacementRecipe(recipe)].IsActive.Should().BeTrue();
            player.UnlockedRecipes[recipe] = date;
            player.CraftedRecipes[recipe] = date;
        }
        player.UnlockedRecipes[RecipeType.WeaponEnhancementDMGPhysical1] = existing;
        ForceWeaponEnhancementMigration.MigrateRecipeKnowledge(player).Should().BeTrue();
        player.UnlockedRecipes.Should().HaveCount(3);
        player.CraftedRecipes.Should().HaveCount(3);
        player.UnlockedRecipes[RecipeType.WeaponEnhancementDMGPhysical1].Should().Be(existing);
        ForceWeaponEnhancementMigration.MigrateRecipeKnowledge(player).Should().BeFalse();
        var list = $"{(int)RecipeType.WeaponEnhancementDMGForce1},unknown,17";
        ForceWeaponEnhancementMigration.MigrateRecipeList(list)
            .Should().Be($"{(int)RecipeType.WeaponEnhancementDMGPhysical1},unknown,17");
    }

    [Test]
    public void SavedCreatureAndNestedInventoriesKeepTheirStateAndDamageWhileLosingForceConversion()
    {
        var weapon = Weapon();
        var bag = Node(7, Scalar(5, "BaseItem", (int)BaseItem.MiscSmall), Text("UUID", "bag-id"),
            List("ItemList", weapon), Scalar(4, "UnknownField", 9876));
        var root = Node(0, Scalar(2, "Appearance_Type", 65000), Text("UUID", "creature-id"),
            Scalar(5, "HitPoints", 123), List("ItemList", bag), List("Equip_ItemList", Weapon(16)));
        var data = Encode(root, "BIC ");
        var migrated = ForceWeaponEnhancementMigration.MigrateSerializedObject(data);
        migrated.Should().NotBe(data);
        var saved = Read(migrated);
        Value(saved, "Appearance_Type").Should().Be((ushort)65000);
        Value(saved, "HitPoints").Should().Be(123);
        Value(saved, "UUID").Should().Be("creature-id");
        var savedBag = Children(saved, "ItemList").Single();
        Value(savedBag, "UUID").Should().Be("bag-id");
        Value(savedBag, "UnknownField").Should().Be(9876u);
        foreach (var item in Children(savedBag, "ItemList").Concat(Children(saved, "Equip_ItemList")))
        {
            Value(item, "UUID").Should().Be("weapon-id");
            Value(item, "StackSize").Should().Be((ushort)9);
            Children(item, "PropertiesList").Select(x => Value(x, "PropertyName"))
                .Should().NotContain((ushort)ItemPropertyType.WeaponDamageType);
            Value(Children(item, "PropertiesList").Single(), "CostValue").Should().Be((ushort)37);
        }
        Children(saved, "Equip_ItemList").Single().Type.Should().Be(16);
        ForceWeaponEnhancementMigration.MigrateSerializedObject(migrated).Should().Be(migrated);
    }

    [TestCase((int)BaseItem.CreaturePierceWeapon, "Creature weapon", false)]
    [TestCase((int)BaseItem.Longsword, "[NPC] Force weapon", false)]
    [TestCase((int)BaseItem.Longsword, "NPC custom weapon", true)]
    public void IntentionalNpcForceWeaponsRemainByteForByteUnchanged(int baseItem, string name, bool restricted)
    {
        var weapon = Weapon();
        SetFields(weapon, Scalar(5, "BaseItem", baseItem), LocalizedName(name));
        if (restricted) AddField(weapon, List("VarTable", Variable("NO_ECONOMY", 1)));
        var data = Encode(weapon);
        ForceWeaponEnhancementMigration.MigrateSerializedObject(data).Should().Be(data);
    }

    [Test]
    public void BlueprintPairsAndLegacyEnhancementsKeepAmountsAndOtherDamageTypes()
    {
        var blueprint = Node(0, Scalar(5, "BaseItem", (int)BaseItem.Book),
            LocalizedName("Blueprint: Weapon Enhancement - Force Damage II"), Text("UUID", "blueprint-id"),
            List("PropertiesList", Property(ItemPropertyType.WeaponEnhancement, 19, 3),
                Property(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Force),
                Property(ItemPropertyType.WeaponEnhancement, 18, 4),
                Property(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire)),
            List("VarTable", Variable("BLUEPRINT_RECIPE_ID", (int)RecipeType.WeaponEnhancementDMGForce2)));
        var migrated = ForceWeaponEnhancementMigration.MigrateSerializedObject(Encode(blueprint));
        var saved = Read(migrated);
        ((CExoLocString)Value(saved, "LocalizedName")).LocalizedStrings[0]
            .Should().Be("Blueprint: Weapon Enhancement - DMG II");
        var properties = Children(saved, "PropertiesList").ToArray();
        properties.Should().HaveCount(3);
        Value(properties[0], "Subtype").Should().Be((ushort)18);
        Value(properties[0], "CostValue").Should().Be((ushort)3);
        Value(properties[1], "CostValue").Should().Be((ushort)4);
        Value(properties[2], "Subtype").Should().Be((ushort)CombatDamageType.Fire);
        Value(Children(saved, "VarTable").Single(), "Value").Should().Be((int)RecipeType.WeaponEnhancementDMGPhysical2);
        ForceWeaponEnhancementMigration.MigrateSerializedObject(migrated).Should().Be(migrated);
    }

    [Test]
    public void DroidArchivesKeepUnknownFieldsAndStableKeysAndRetryWithoutRewriting()
    {
        var weapon = Encode(Weapon());
        var droid = new JObject
        {
            ["SerializedCPU"] = weapon,
            ["EquippedItems"] = new JObject { ["16"] = weapon },
            ["Inventory"] = new JObject { ["stable-key"] = weapon },
            ["UnknownData"] = new JObject { ["preserve"] = 42 },
        };
        var controller = Node(0, Scalar(5, "BaseItem", (int)BaseItem.MiscSmall), LocalizedName("Controller"),
            List("VarTable", Node(0, Text("Name", "CONSTRUCTED_DROID"), Scalar(4, "Type", 3), Text("Value", droid.ToString()))));
        var migrated = ForceWeaponEnhancementMigration.MigrateSerializedObject(Encode(controller));
        var result = JObject.Parse((string)Value(Children(Read(migrated), "VarTable").Single(), "Value"));
        result["UnknownData"].Should().BeEquivalentTo(droid["UnknownData"]);
        foreach (var data in new[] { result["SerializedCPU"], result["EquippedItems"]["16"], result["Inventory"]["stable-key"] })
            Children(Read(data.Value<string>()), "PropertiesList").Should().HaveCount(1);
        ForceWeaponEnhancementMigration.MigrateSerializedObject(migrated).Should().Be(migrated);
    }

    [Test]
    public void AuthoredPlayerItemsAndResearchRollsCannotProvideForceConversion()
    {
        var root = FindRoot();
        var checkedItems = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "Module", "uti"), "*.uti.json"))
        {
            var item = JObject.Parse(File.ReadAllText(path).TrimStart('\uFEFF'));
            var properties = item["PropertiesList"]?["value"] as JArray;
            if (properties == null || !properties.Any(x => x["PropertyName"]?["value"]?.Value<int>() == 134 &&
                x["Subtype"]?["value"]?.Value<int>() == 2)) continue;
            var noEconomy = (item["VarTable"]?["value"] as JArray)?.Any(x =>
                x["Name"]?["value"]?.Value<string>() == "NO_ECONOMY" && x["Value"]?["value"]?.Value<int>() == 1) == true;
            Item.IsEconomyRestricted((BaseItem)item["BaseItem"]["value"].Value<int>(),
                item["LocalizedName"]?["value"]?["0"]?.Value<string>(), noEconomy, true)
                .Should().BeTrue(path);
            checkedItems++;
        }
        checkedItems.Should().BeGreaterThan(0, "NPC Force weapons remain authored");
        File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "CraftService", "BlueprintBonuses.cs"))
            .Should().NotContain("CombatDamageType.Force");
        File.ReadAllLines(Path.Combine(root, "SWLOR.CLI", "InputFiles", "enhancement_list.tsv"))
            .Should().NotContain(line => line.Split('\t').Last().Trim() == "Force");
    }

    // Fixtures use the existing raw GFF writer; assertions read with the independent formats library.
    private static readonly Type Document = typeof(ForceWeaponEnhancementMigration).Assembly
        .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData")!;
    private static readonly Type NodeType = Document.GetNestedType("Node", BindingFlags.NonPublic)!;
    private static readonly Type FieldType = Document.GetNestedType("Field", BindingFlags.NonPublic)!;
    private static object Node(uint type, params object[] fields)
    {
        var node = Activator.CreateInstance(NodeType, true)!;
        NodeType.GetField("Type")!.SetValue(node, type);
        foreach (var field in fields) AddField(node, field);
        return node;
    }
    private static void AddField(object node, object field) =>
        ((System.Collections.IList)NodeType.GetField("Fields")!.GetValue(node)!).Add(field);
    private static object Field(uint type, string name, byte[] data = null, params object[] children)
    {
        object list = null;
        if (type == 15)
        {
            list = Activator.CreateInstance(typeof(List<>).MakeGenericType(NodeType))!;
            foreach (var child in children) ((System.Collections.IList)list).Add(child);
        }
        return Document.GetMethod("MakeField", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new[] { (object)type, name, data, list })!;
    }
    private static object Scalar(uint type, string name, int value) => Field(type, name, BitConverter.GetBytes(value));
    private static object Text(string name, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Field(10, name, BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray());
    }
    private static object LocalizedName(string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        return Field(12, "LocalizedName", BitConverter.GetBytes(16 + bytes.Length)
            .Concat(BitConverter.GetBytes(uint.MaxValue)).Concat(BitConverter.GetBytes(1))
            .Concat(BitConverter.GetBytes(0)).Concat(BitConverter.GetBytes(bytes.Length)).Concat(bytes).ToArray());
    }
    private static object List(string name, params object[] items) => Field(15, name, null, items);
    private static object Property(ItemPropertyType type, int subtype, int amount = 0) =>
        Node(0, Scalar(2, "PropertyName", (int)type), Scalar(2, "Subtype", subtype), Scalar(2, "CostValue", amount));
    private static object Variable(string name, int value) => Node(0, Text("Name", name), Scalar(4, "Type", 1), Scalar(5, "Value", value));
    private static object Weapon(uint slot = 0) => Node(slot, Scalar(5, "BaseItem", (int)BaseItem.Longsword),
        LocalizedName("Crafted sword"), Text("UUID", "weapon-id"), Scalar(2, "StackSize", 9),
        List("PropertiesList", Property(ItemPropertyType.DMG, 0, 37), Property(ItemPropertyType.WeaponDamageType, 2)));
    private static void SetFields(object node, params object[] fields)
    {
        var list = (System.Collections.IList)NodeType.GetField("Fields")!.GetValue(node)!;
        foreach (var field in fields)
        {
            var name = FieldType.GetProperty("Name")!.GetValue(field);
            for (var index = list.Count - 1; index >= 0; index--)
                if (Equals(FieldType.GetProperty("Name")!.GetValue(list[index]), name)) list.RemoveAt(index);
            list.Add(field);
        }
    }
    private static string Encode(object root, string fileType = "UTI ")
    {
        NodeType.GetField("Type")!.SetValue(root, uint.MaxValue);
        var document = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Document);
        Document.GetField("_header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, Encoding.ASCII.GetBytes(fileType + "V3.2"));
        Document.GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, root);
        return (string)Document.GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(document, null)!;
    }
    private static GffStruct Read(string data) => GffReader.Read(Convert.FromBase64String(data)).RootStruct;
    private static object Value(GffStruct node, string field) => node.Fields.Single(x => x.Label == field).Value!;
    private static IEnumerable<GffStruct> Children(GffStruct node, string field) => ((GffList)Value(node, field)).Elements;
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
