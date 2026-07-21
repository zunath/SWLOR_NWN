using FluentAssertions;
using NUnit.Framework;
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
    [TestCase(BaseItem.Sling, "blast_jawa_d", BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Sling, "dualpistolmain", BaseItem.LegacyPistol)]
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
            if (column == "label")
                continue;

            rows[61][column].Should().Be(
                value,
                $"the native sling form must preserve the pistol's {column} behavior");
        }

        rows[514]["label"].Should().Be("legacy_smallarms");
        rows[514]["EquipableSlots"].Should().Be("0x00030");
        rows[514]["NumDice"].Should().Be("1");
        rows[514]["DieToRoll"].Should().Be("6");
        rows[514]["AmmunitionType"].Should().Be("3");
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
