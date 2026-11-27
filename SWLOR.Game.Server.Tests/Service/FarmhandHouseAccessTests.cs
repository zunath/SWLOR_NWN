using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Who may post a farmhand at a house in a city, and who holds its garden permission. A house in a city is owned by the mayor at the
/// time whoever placed it, so both follow the house's keeper (the player whose record holds Edit Structures with the right to grant
/// it), not the owner field.
/// </summary>
public class FarmhandHouseAccessTests
{
    private const string Mayor = "mayor";
    private const string Citizen = "citizen";

    private static WorldProperty CityHouse() => new()
    {
        Id = "house",
        PropertyType = PropertyType.House,
        OwnerPlayerId = Mayor
    };

    private static readonly PropertyPermissionType[] HousePermissions =
    {
        PropertyPermissionType.EditStructures,
        PropertyPermissionType.RetrieveStructures,
        PropertyPermissionType.RenameProperty,
        PropertyPermissionType.ChangeDescription,
        PropertyPermissionType.EnterProperty,
        PropertyPermissionType.EditCategories
    };

    /// <summary>What <c>Property.CreateProperty</c> writes for a player who places a house, or for its owner of record.</summary>
    private static WorldPropertyPermission FullRecord(string playerId)
    {
        var record = new WorldPropertyPermission { PropertyId = "house", PlayerId = playerId };
        foreach (var permission in HousePermissions)
        {
            record.Permissions[permission] = true;
            record.GrantPermissions[permission] = true;
        }

        return record;
    }

    private static WorldPropertyPermission GuestRecord()
    {
        var record = new WorldPropertyPermission { PropertyId = "house", PlayerId = "guest" };
        foreach (var permission in HousePermissions)
        {
            record.Permissions[permission] = true;
            record.GrantPermissions[permission] = false;
        }

        return record;
    }

    private static FarmhandRefusal Assign(WorldPropertyPermission record) =>
        FarmhandRules.CheckAssign(true, true, FarmhandRules.HasHouseAccess(record), false, true);

    #region Farmhands

    [Test]
    public void TheCitizenWhoPlacedACityHouse_MayPostAFarmhandThere_ThoughTheMayorOwnsIt()
    {
        CityHouse().OwnerPlayerId.Should().Be(Mayor);

        Assign(FullRecord(Citizen)).Should().Be(FarmhandRefusal.None);
    }

    [Test]
    public void TheMayorOfRecord_StillMay_BecauseTheyHoldTheSameRecord()
    {
        Assign(FullRecord(Mayor)).Should().Be(FarmhandRefusal.None);
    }

    [Test]
    public void AGuestWithEveryPermissionButNoRightToGrant_MayNot()
    {
        Assign(GuestRecord()).Should().Be(FarmhandRefusal.NoAccess);
    }

    [Test]
    public void ANewMayorWithNoRecordOnTheHouse_MayNot_NorAnyoneWithNoRecord()
    {
        Assign(null).Should().Be(FarmhandRefusal.NoAccess);
        FarmhandRules.HasHouseAccess(new WorldPropertyPermission()).Should().BeFalse();
    }

    [Test]
    public void AHouseGivenToANewKeeper_IsPostedByThemAndNotByThePreviousOne()
    {
        var previous = (WorldPropertyPermission)null; // the old keeper's record was removed when the house changed hands
        var current = FullRecord("next-citizen");

        Assign(previous).Should().Be(FarmhandRefusal.NoAccess);
        Assign(current).Should().Be(FarmhandRefusal.None);
    }

    [Test]
    public void TheFarmhandAccessCheck_ReadsThePermissionRecord_NotTheOwnerField()
    {
        var farmhands = ReadServerSource("Service", "Farmhands.cs");
        var access = ExtractMethod(farmhands, "public static bool HasAccess(uint player, WorldProperty target)");

        access.Should().Contain("FarmhandRules.HasHouseAccess(HouseSpecializations.GetPermissionRecord(target.Id, GetObjectUUID(player)))");
        access.Should().NotContain("target.OwnerPlayerId");
    }

    #endregion

    #region Garden permission

    [Test]
    public void TheMigration_GivesTendGardensToAHousesKeeper_NotOnlyToItsOwnerOfRecord()
    {
        var placer = FullRecord(Citizen);

        GardenPermissionMigration.Migrate(placer, Mayor, keepersHoldIt: true).Should().BeTrue();

        placer.Permissions[PropertyPermissionType.TendGardens].Should().BeTrue("the player who placed the house gardens in it");
        placer.GrantPermissions[PropertyPermissionType.TendGardens].Should().BeTrue("and can let others");
    }

    [Test]
    public void TheMigration_StillGivesTheOwnerOfRecordTheKey_AndAGuestNothing()
    {
        var owner = FullRecord(Mayor);
        var guest = GuestRecord();

        GardenPermissionMigration.Migrate(owner, Mayor, keepersHoldIt: true);
        GardenPermissionMigration.Migrate(guest, Mayor, keepersHoldIt: true);

        owner.Permissions[PropertyPermissionType.TendGardens].Should().BeTrue();
        guest.Permissions[PropertyPermissionType.TendGardens].Should().BeFalse();
        guest.GrantPermissions[PropertyPermissionType.TendGardens].Should().BeFalse();
    }

    [Test]
    public void ACityRecordIsNotJudgedByTheKeeperRule_OnlyTheMayorHoldsIt()
    {
        // A delegate who can edit and grant structures in the city is not the mayor.
        var delegateRecord = FullRecord("delegate");

        GardenPermissionMigration.Migrate(delegateRecord, Mayor).Should().BeTrue();

        delegateRecord.Permissions[PropertyPermissionType.TendGardens].Should().BeFalse();
    }

    [Test]
    public void TheMigrationRun_JudgesHousesByTheirKeeper_AndCitiesByTheirMayor()
    {
        var run = ExtractMethod(
            ReadServerSource("Feature", "MigrationDefinition", "ServerMigration", "GardenPermissionMigration.cs"),
            "public static void Run(int version)");

        run.Should().Contain("Migrate(permission, property.OwnerPlayerId, property.PropertyType == PropertyType.House)");
    }

    [Test]
    public void TheGardenAndVatChecks_ReadTheTendGardensRecord_NotTheOwnerField()
    {
        var farming = ReadServerSource("Service", "Farming.cs");
        var permission = ExtractMethod(farming, "public static bool HasGardenPermission(uint player, WorldProperty plot)");

        permission.Should().Contain("PropertyPermissionType.TendGardens").And.NotContain("OwnerPlayerId");
        ReadServerSource("Service", "FermentationVats.cs").Should().Contain("public static bool HasAccess(uint player, WorldProperty vat) => Farming.HasGardenPermission(player, vat);");
    }

    #endregion

    private static string ReadServerSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()));
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"{signature} has no closing brace.");
    }
}
