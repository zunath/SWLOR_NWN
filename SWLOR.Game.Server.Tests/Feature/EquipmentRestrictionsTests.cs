using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class EquipmentRestrictionsTests
{
    [TestCase(BaseItem.Pistol)]
    [TestCase(BaseItem.Sling)]
    public void Pistols_CanBeEquippedInTheRightHandWithAnEmptyLeftHand(BaseItem pistolType)
    {
        var error = EquipmentRestrictions.GetPistolEquipmentError(
            pistolType,
            InventorySlot.RightHand,
            null,
            null);

        error.Should().BeEmpty();
    }

    [TestCase(BaseItem.Pistol, BaseItem.SmallShield)]
    [TestCase(BaseItem.Pistol, BaseItem.LargeShield)]
    [TestCase(BaseItem.Pistol, BaseItem.TowerShield)]
    [TestCase(BaseItem.Sling, BaseItem.SmallShield)]
    [TestCase(BaseItem.Sling, BaseItem.LargeShield)]
    [TestCase(BaseItem.Sling, BaseItem.TowerShield)]
    public void Pistols_CanBePairedWithShields(BaseItem pistolType, BaseItem shieldType)
    {
        var pistolError = EquipmentRestrictions.GetPistolEquipmentError(
            pistolType,
            InventorySlot.RightHand,
            null,
            shieldType);
        var shieldError = EquipmentRestrictions.GetPistolEquipmentError(
            shieldType,
            InventorySlot.LeftHand,
            pistolType,
            null);

        pistolError.Should().BeEmpty();
        shieldError.Should().BeEmpty();
    }

    [TestCase(BaseItem.Pistol)]
    [TestCase(BaseItem.Sling)]
    public void Pistols_CannotBeEquippedInTheLeftHand(BaseItem pistolType)
    {
        var error = EquipmentRestrictions.GetPistolEquipmentError(
            pistolType,
            InventorySlot.LeftHand,
            BaseItem.Pistol,
            null);

        error.Should().Be("Pistols may only be equipped in the right hand.");
    }

    [TestCase(BaseItem.Pistol, BaseItem.Longsword)]
    [TestCase(BaseItem.Pistol, BaseItem.Dagger)]
    [TestCase(BaseItem.Pistol, BaseItem.Pistol)]
    [TestCase(BaseItem.Sling, BaseItem.Longsword)]
    [TestCase(BaseItem.Sling, BaseItem.Dagger)]
    [TestCase(BaseItem.Sling, BaseItem.Pistol)]
    public void Pistols_CannotBePairedWithNonShieldItems(BaseItem pistolType, BaseItem leftHandType)
    {
        var error = EquipmentRestrictions.GetPistolEquipmentError(
            pistolType,
            InventorySlot.RightHand,
            null,
            leftHandType);

        error.Should().Be("Pistols may only be paired with a shield in the left hand.");
    }

    [TestCase(BaseItem.Pistol)]
    [TestCase(BaseItem.Sling)]
    public void NonShieldItems_CannotBeEquippedOppositeAPistol(BaseItem pistolType)
    {
        var error = EquipmentRestrictions.GetPistolEquipmentError(
            BaseItem.Longsword,
            InventorySlot.LeftHand,
            pistolType,
            null);

        error.Should().Be("Pistols may only be paired with a shield in the left hand.");
    }

    [Test]
    public void LegacyOffHandPistols_CannotBeEquipped()
    {
        var error = EquipmentRestrictions.GetPistolEquipmentError(
            BaseItem.OffHandPistol,
            InventorySlot.LeftHand,
            BaseItem.Pistol,
            null);

        error.Should().Be("Off-hand pistols cannot be equipped.");
    }

    [Test]
    public void LegacyPistols_RemainPistolsForEquipmentRestrictions()
    {
        SWLOR.Game.Server.Service.Item.PistolBaseItemTypes
            .Should()
            .Contain(BaseItem.LegacyPistol);

        var offHandError = EquipmentRestrictions.GetPistolEquipmentError(
            BaseItem.LegacyPistol,
            InventorySlot.LeftHand,
            null,
            null);
        var pairedWithWeaponError = EquipmentRestrictions.GetPistolEquipmentError(
            BaseItem.LegacyPistol,
            InventorySlot.RightHand,
            null,
            BaseItem.Longsword);

        offHandError.Should().Be("Pistols may only be equipped in the right hand.");
        pairedWithWeaponError.Should().Be("Pistols may only be paired with a shield in the left hand.");
    }

    [TestCase(BaseItem.Pistol, "b_pistol", BaseItem.Sling)]
    [TestCase(BaseItem.LegacyPistol, "b_pistol", BaseItem.Sling)]
    [TestCase(BaseItem.Sling, "b_pistol", BaseItem.Sling)]
    [TestCase(BaseItem.Arrow, "blaster_bullets", BaseItem.Bullet)]
    [TestCase(BaseItem.Bullet, "blaster_bullets", BaseItem.Bullet)]
    [TestCase(BaseItem.Sling, "blast_jawa_d", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Pistol, "blast_jawa_d", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Sling, "dualpistolmain", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Sling, "extjawa004_wp", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Sling, "jawa_wp", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Sling, "jawaaddit_wp", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Sling, "blast_se14_d", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.LegacyPistol, "blast_se14_d", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.LegacyPistol, "blast_jawa_d", BaseItem.LegacyPistol)]
    public void PistolBaseItems_AreCanonicalizedWithoutShieldDependentSwaps(
        BaseItem currentBaseItem,
        string resref,
        BaseItem expectedBaseItem)
    {
        var result = PistolBaseItemCompatibility.GetCanonicalBaseItem(
            currentBaseItem,
            resref);

        result.Should().Be(expectedBaseItem);
    }

    [TestCase(BaseItem.Arrow, InventorySlot.Arrows, InventorySlot.Bullets)]
    [TestCase(BaseItem.Bullet, InventorySlot.Bullets, InventorySlot.Bullets)]
    [TestCase(BaseItem.Pistol, InventorySlot.RightHand, InventorySlot.RightHand)]
    public void PistolAmmunition_UsesTheNativeSlingBulletSlot(
        BaseItem currentBaseItem,
        InventorySlot requestedSlot,
        InventorySlot expectedSlot)
    {
        var result = PistolBaseItemCompatibility.GetCanonicalInventorySlot(
            currentBaseItem,
            requestedSlot);

        result.Should().Be(expectedSlot);
    }

    [TestCase(BaseItem.Bullet, true, true)]
    [TestCase(BaseItem.Bullet, false, false)]
    [TestCase(BaseItem.Arrow, true, false)]
    public void PistolAmmunitionMigration_ClearsAnOccupiedBulletSlotBeforeEquipping(
        BaseItem normalizedLegacyAmmoType,
        bool bulletSlotOccupied,
        bool expected)
    {
        var result = PistolBaseItemCompatibility.ShouldClearBulletSlot(
            normalizedLegacyAmmoType,
            bulletSlotOccupied);

        result.Should().Be(expected);
    }

    [Test]
    public void PistolBaseItems_UseOneHandedWieldingAndRightHandOnlySlots()
    {
        var root = FindRepositoryRoot();
        var rows = Read2daRows(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "baseitems.2da"));

        rows[11]["WeaponWield"].Should().Be("10");
        rows[11]["EquipableSlots"].Should().Be("0x00010");
        rows[61]["WeaponWield"].Should().Be("10");
        rows[61]["EquipableSlots"].Should().Be("0x00010");
        foreach (var (column, value) in rows[11])
        {
            if (column is "label" or "AmmunitionType")
                continue;

            rows[61][column].Should().Be(
                value,
                $"the native sling form must preserve the pistol's {column} behavior");
        }

        rows[11]["AmmunitionType"].Should().Be("1");
        rows[61]["AmmunitionType"].Should().Be(
            "3",
            "native sling attacks only emit projectiles from bullet-slot ammunition");

        var ammunitionRows = Read2daRows(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "ammunitiontypes.2da"));
        ammunitionRows[2]["Model"].Should().Be(
            "wamar_001",
            "pistol bullets use the existing single-emitter arrow blaster model");
        ammunitionRows[2]["ShotSound"].Should().Be(
            PistolAnimationRemap.PistolShotSound,
            "pistol bullets use the existing single-shot blaster sound");

        rows[514]["label"].Should().Be("legacy_smallarms");
        rows[514]["EquipableSlots"].Should().Be("0x00030");
        rows[514]["NumDice"].Should().Be("1");
        rows[514]["DieToRoll"].Should().Be("6");
        rows[514]["AmmunitionType"].Should().Be("3");
    }

    [TestCase("004.uti.json")]
    [TestCase("blaster_bullets.uti.json")]
    public void PistolAmmunitionBlueprints_UseTheBulletBaseItem(string fileName)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, "Module", "uti", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        document.RootElement
            .GetProperty("BaseItem")
            .GetProperty("value")
            .GetInt32()
            .Should()
            .Be((int)BaseItem.Bullet);
    }

    [Test]
    public void CreatureBlueprints_EquipConvertedPistolAmmunitionInTheBulletSlot()
    {
        const int bulletEquipmentStructId = 4096;
        var root = FindRepositoryRoot();
        var ammoResrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "004",
            "blaster_bullets",
        };
        var matches = new List<string>();

        foreach (var path in Directory.EnumerateFiles(
                     Path.Combine(root.FullName, "Module", "utc"),
                     "*.utc.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("Equip_ItemList", out var equipList))
                continue;

            foreach (var entry in equipList.GetProperty("value").EnumerateArray())
            {
                var resref = entry
                    .GetProperty("EquippedRes")
                    .GetProperty("value")
                    .GetString();
                if (resref == null || !ammoResrefs.Contains(resref))
                    continue;

                matches.Add($"{Path.GetFileName(path)}:{resref}");
                entry.GetProperty("__struct_id")
                    .GetInt32()
                    .Should()
                    .Be(
                        bulletEquipmentStructId,
                        $"{Path.GetFileName(path)} equips converted ammunition {resref}");
            }
        }

        matches.Should().NotBeEmpty("converted pistol ammunition is equipped by creature blueprints");
    }

    [Test]
    public void PlacedCreatureItems_UseCanonicalPistolData()
    {
        const int arrowEquipmentStructId = 2048;
        const int bulletEquipmentStructId = 4096;
        const string legacyResref = "blast_se14_d";
        var root = FindRepositoryRoot();
        var ammoMatches = new List<string>();
        var legacyMatches = new List<string>();

        foreach (var path in Directory.EnumerateFiles(
                     Path.Combine(root.FullName, "Module", "git"),
                     "*.git.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var entry in EnumerateJsonObjects(document.RootElement))
            {
                if (entry.TryGetProperty("__struct_id", out var structId) &&
                    structId.GetInt32() is (arrowEquipmentStructId or bulletEquipmentStructId) &&
                    entry.TryGetProperty("Tag", out var tag) &&
                    tag.TryGetProperty("value", out var tagValue) &&
                    tagValue.GetString() == "blaster_bullets")
                {
                    var resref = entry
                        .GetProperty("TemplateResRef")
                        .GetProperty("value")
                        .GetString();
                    ammoMatches.Add($"{Path.GetFileName(path)}:{resref}");
                    structId.GetInt32()
                        .Should()
                        .Be(
                            bulletEquipmentStructId,
                            $"{Path.GetFileName(path)} equips embedded blaster ammunition {resref}");
                    entry.GetProperty("BaseItem")
                        .GetProperty("value")
                        .GetInt32()
                        .Should()
                        .Be(
                            (int)BaseItem.Bullet,
                            $"{Path.GetFileName(path)} embeds blaster ammunition {resref}");
                }

                if (entry.TryGetProperty("TemplateResRef", out var templateResRef) &&
                    templateResRef.TryGetProperty("value", out var resrefValue) &&
                    resrefValue.GetString() == legacyResref &&
                    entry.TryGetProperty("BaseItem", out var baseItem))
                {
                    legacyMatches.Add(Path.GetFileName(path));
                    baseItem.GetProperty("value")
                        .GetInt32()
                        .Should()
                        .Be(
                            (int)BaseItem.LegacyPistol,
                            $"{Path.GetFileName(path)} embeds legacy small arm {legacyResref}");
                }
            }
        }

        ammoMatches.Should().NotBeEmpty("converted pistol ammunition is embedded in placed creatures");
        legacyMatches.Should().NotBeEmpty($"{legacyResref} is embedded in a placed creature");
    }

    [TestCase("blast_jawa_d.uti.json")]
    [TestCase("dualpistolmain.uti.json")]
    [TestCase("extjawa004_wp.uti.json")]
    [TestCase("jawa_wp.uti.json")]
    [TestCase("jawaaddit_wp.uti.json")]
    public void LegacySmallArmsBlueprints_UseTheLegacyBaseItem(string fileName)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, "Module", "uti", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        document.RootElement
            .GetProperty("BaseItem")
            .GetProperty("value")
            .GetInt32()
            .Should()
            .Be((int)BaseItem.LegacyPistol);
    }

    private static Dictionary<int, Dictionary<string, string>> Read2daRows(string path)
    {
        var lines = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var rows = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            rows[row] = values;
        }

        return rows;
    }

    private static IEnumerable<JsonElement> EnumerateJsonObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in EnumerateJsonObjects(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in EnumerateJsonObjects(item))
                {
                    yield return child;
                }
            }
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(directory.FullName, "SWLOR_Haks", "sw_2da", "baseitems.2da")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
