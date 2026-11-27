using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Manage Edicts permission: it is a city permission the mayor holds and delegates, and its right to grant is the
/// mayor's alone and can never be given.
/// </summary>
public class EdictPermissionTests
{
    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }

    [Test]
    public void ManageEdicts_IsPermission19AndShowsInThePermissionWindow()
    {
        ((int)PropertyPermissionType.ManageEdicts).Should().Be(19);

        var attribute = typeof(PropertyPermissionType).GetField(nameof(PropertyPermissionType.ManageEdicts))!
            .GetCustomAttributes(typeof(PropertyPermissionAttribute), false)
            .Cast<PropertyPermissionAttribute>()
            .Single();
        attribute.IsActive.Should().BeTrue();
        attribute.Name.Should().Be("Manage Edicts");
        attribute.Description.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void OnlyManageEdictsHasAnOwnerOnlyGrantRight()
    {
        foreach (var type in Enum.GetValues(typeof(PropertyPermissionType)).Cast<PropertyPermissionType>())
        {
            PropertyPermissionRules.IsGrantRightOwnerOnly(type)
                .Should().Be(type == PropertyPermissionType.ManageEdicts, type.ToString());
        }
    }

    [Test]
    public void ManageEdicts_IsInTheCityPermissionList()
    {
        var property = ReadServer("Service", "Property.cs");
        var cityList = Between(property, "_permissionsByPropertyType[PropertyType.City]", "_permissionsByPropertyType[PropertyType.Category]");

        cityList.Should().Contain("PropertyPermissionType.ManageEdicts");
    }

    [Test]
    public void ANewMayor_GetsEveryCityPermissionIncludingTheGrantRightOfManageEdicts()
    {
        var property = ReadServer("Service", "Property.cs");
        var transfer = Between(property, "void TransferPermissions(string winnerPlayerId)", "// No reason to process deleted cities");

        transfer.Should().Contain("foreach (var permission in _permissionsByPropertyType[PropertyType.City])");
        transfer.Should().Contain("winnerPermission.GrantPermissions[permission] = true;");
    }

    [Test]
    public void ThePermissionWindow_NeverOffersOrSavesAGrantRightForManageEdicts()
    {
        var window = ReadServer("Feature", "GuiDefinition", "ViewModel", "PropertyPermissionsViewModel.cs");

        var canAdjust = Between(window, "private bool CanAdjustGrantPermission(", "private void LoadPlayerInfo()");
        canAdjust.Should().Contain("!PropertyPermissionRules.IsGrantRightOwnerOnly(type)");

        var save = Between(window, "public Action OnClickSaveChanges()", "public Action OnClickReset()");
        save.Should().Contain("PermissionGrantingStates[index] && !PropertyPermissionRules.IsGrantRightOwnerOnly(permission)");
    }

    [Test]
    public void TheCityTerminal_OpensForAnOfficialWhoOnlyHoldsManageEdicts()
    {
        var property = ReadServer("Service", "Property.cs");
        var open = Between(property, "public static void OpenCityManagementMenu()", "public static string GetCityLevelName(int level)");

        // The terminal asks the city powers, which count Manage Edicts among the permissions that open the window.
        open.Should().Contain("CityPowers.CanOpenManageCity(");
        var onlyManageEdicts = new Dictionary<PropertyPermissionType, bool> { [PropertyPermissionType.ManageEdicts] = true };
        CityPowerRules.CanOpenManageCity(CityRole.None, onlyManageEdicts, false).Should().BeTrue();
    }

    [Test]
    public void TheEdictsTab_ChecksThePermissionBeforeEveryChange()
    {
        var edicts = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.Edicts.cs");
        var main = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");

        // Enacting needs the power to enact, repealing the power to repeal: an Acting Mayor has the first but not the second.
        main.Should().Contain("CanManageEdicts = access.Can(CityPower.EnactEdicts);");
        main.Should().Contain("CanRepealEdicts = access.Can(CityPower.RepealEdicts);");
        Between(edicts, "public Action EnactEdict()", "private void ConfirmEnact(").Should().Contain("!CanManageEdicts");
        Between(edicts, "private void ConfirmEnact(", "public Action RepealEdict()").Should().Contain("!CanManageEdicts");
        Between(edicts, "public Action RepealEdict()", "private void ConfirmRepeal(").Should().Contain("!CanRepealEdicts");
        Between(edicts, "private void ConfirmRepeal(", "private string DescribeRefusal(").Should().Contain("!CanRepealEdicts");
    }

    private static string Between(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        from.Should().BeGreaterThanOrEqualTo(0, start);
        var to = source.IndexOf(end, from, StringComparison.Ordinal);
        to.Should().BeGreaterThan(from, end);
        return source.Substring(from, to - from);
    }
}
