using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Plan's Permissions table, cell by cell: what the mayor, an Acting Mayor or interim holder, an officer, and a holder
/// of a granted permission may do, and the roles that follow from the city's records.
/// </summary>
public class CityPermissionTableTests
{
    private const string Mayor = "mayor";
    private const string Acting = "acting";
    private const string Interim = "interim";
    private const string Officer = "officer";
    private const string Citizen = "citizen";

    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static WorldProperty City() => new() { OwnerPlayerId = Mayor };

    private static CityStepDown AcceptedStepDown() => new()
    {
        InterimHolderPlayerId = Interim,
        Stage = CityStepDownStage.Accepted,
        NamedUtc = Now.AddDays(-1),
        AcceptDeadlineUtc = Now.AddDays(6)
    };

    private static Dictionary<PropertyPermissionType, bool> Granted(params PropertyPermissionType[] types) =>
        types.ToDictionary(type => type, _ => true);

    #region The table

    // Appoint officers or an Acting Mayor
    [TestCase(CityPower.Appoint, CityRole.Mayor, true)]
    [TestCase(CityPower.Appoint, CityRole.ActingMayor, false)]
    [TestCase(CityPower.Appoint, CityRole.Officer, false)]
    // Grant city permissions
    [TestCase(CityPower.GrantPermissions, CityRole.Mayor, true)]
    [TestCase(CityPower.GrantPermissions, CityRole.ActingMayor, false)]
    [TestCase(CityPower.GrantPermissions, CityRole.Officer, false)]
    // Handle City Affairs
    [TestCase(CityPower.HandleCityAffairs, CityRole.Mayor, true)]
    [TestCase(CityPower.HandleCityAffairs, CityRole.ActingMayor, true)]
    [TestCase(CityPower.HandleCityAffairs, CityRole.Officer, true)]
    // Enact Edicts into empty slots
    [TestCase(CityPower.EnactEdicts, CityRole.Mayor, true)]
    [TestCase(CityPower.EnactEdicts, CityRole.ActingMayor, true)]
    [TestCase(CityPower.EnactEdicts, CityRole.Officer, false)]
    // Repeal Edicts
    [TestCase(CityPower.RepealEdicts, CityRole.Mayor, true)]
    [TestCase(CityPower.RepealEdicts, CityRole.ActingMayor, false)]
    [TestCase(CityPower.RepealEdicts, CityRole.Officer, false)]
    // Change taxes
    [TestCase(CityPower.ChangeTaxes, CityRole.Mayor, true)]
    [TestCase(CityPower.ChangeTaxes, CityRole.ActingMayor, true)]
    [TestCase(CityPower.ChangeTaxes, CityRole.Officer, false)]
    // Withdraw from the treasury
    [TestCase(CityPower.WithdrawTreasury, CityRole.Mayor, true)]
    [TestCase(CityPower.WithdrawTreasury, CityRole.ActingMayor, false)]
    [TestCase(CityPower.WithdrawTreasury, CityRole.Officer, false)]
    // Pay upkeep
    [TestCase(CityPower.PayUpkeep, CityRole.Mayor, true)]
    [TestCase(CityPower.PayUpkeep, CityRole.ActingMayor, true)]
    [TestCase(CityPower.PayUpkeep, CityRole.Officer, false)]
    // Start City Projects
    [TestCase(CityPower.StartProjects, CityRole.Mayor, true)]
    [TestCase(CityPower.StartProjects, CityRole.ActingMayor, true)]
    [TestCase(CityPower.StartProjects, CityRole.Officer, false)]
    // Post bounties, tournaments, and story requests
    [TestCase(CityPower.PostRequests, CityRole.Mayor, true)]
    [TestCase(CityPower.PostRequests, CityRole.ActingMayor, true)]
    [TestCase(CityPower.PostRequests, CityRole.Officer, true)]
    public void ARoleHoldsAPowerAsTheTableSays(CityPower power, CityRole role, bool expected)
    {
        CityPowerRules.RoleHoldsPower(role, power).Should().Be(expected);
        CityPowerRules.HasPower(role, power, null).Should().Be(expected, "no permission record adds to a role");
    }

    [TestCase(CityPower.Appoint)]
    [TestCase(CityPower.HandleCityAffairs)]
    [TestCase(CityPower.PostRequests)]
    [TestCase(CityPower.GrantPermissions)]
    public void NoPermissionGivesTheseToACitizen(CityPower power)
    {
        // Only officers get Handle City Affairs, so the officer limit cannot be bypassed; only the mayor appoints.
        var everything = Enum.GetValues<PropertyPermissionType>().ToDictionary(type => type, _ => true);

        CityPowerRules.GetPermissionFor(power).Should().BeNull();
        CityPowerRules.HasPower(CityRole.None, power, everything).Should().BeFalse();
    }

    [TestCase(CityPower.EnactEdicts, PropertyPermissionType.ManageEdicts)]
    [TestCase(CityPower.RepealEdicts, PropertyPermissionType.ManageEdicts)]
    [TestCase(CityPower.ChangeTaxes, PropertyPermissionType.EditTaxes)]
    [TestCase(CityPower.WithdrawTreasury, PropertyPermissionType.AccessTreasury)]
    [TestCase(CityPower.PayUpkeep, PropertyPermissionType.ManageUpkeep)]
    [TestCase(CityPower.StartProjects, PropertyPermissionType.ManageUpgrades)]
    public void AHolderOfTheGrantedPermissionHasThePower(CityPower power, PropertyPermissionType permission)
    {
        CityPowerRules.GetPermissionFor(power).Should().Be(permission);
        CityPowerRules.HasPower(CityRole.None, power, Granted(permission)).Should().BeTrue();
        CityPowerRules.HasPower(CityRole.None, power, Granted()).Should().BeFalse();
        CityPowerRules.HasPower(CityRole.None, power, null).Should().BeFalse();
    }

    [Test]
    public void ARoleAndAPermissionAddUp()
    {
        // An Acting Mayor the mayor also gave Access Treasury to can withdraw: the permission is theirs.
        CityPowerRules.HasPower(CityRole.ActingMayor, CityPower.WithdrawTreasury, null).Should().BeFalse();
        CityPowerRules.HasPower(CityRole.ActingMayor, CityPower.WithdrawTreasury, Granted(PropertyPermissionType.AccessTreasury)).Should().BeTrue();
        CityPowerRules.HasPower(CityRole.Officer, CityPower.RepealEdicts, Granted(PropertyPermissionType.ManageEdicts)).Should().BeTrue();
    }

    [Test]
    public void TheMayorHasEveryPower()
    {
        foreach (var power in Enum.GetValues<CityPower>().Where(power => power != CityPower.Invalid))
        {
            CityPowerRules.HasPower(CityRole.Mayor, power, null).Should().BeTrue(power.ToString());
        }
    }

    [Test]
    public void ADepartedMayorHasNoPowerEvenWithAFullPermissionRecord()
    {
        var everything = Enum.GetValues<PropertyPermissionType>().ToDictionary(type => type, _ => true);

        foreach (var power in Enum.GetValues<CityPower>())
        {
            CityPowerRules.HasPower(CityRole.DepartedMayor, power, everything).Should().BeFalse(power.ToString());
        }
    }

    [Test]
    public void ANoneRoleHoldsNoPowerByRole()
    {
        foreach (var power in Enum.GetValues<CityPower>())
        {
            CityPowerRules.RoleHoldsPower(CityRole.None, power).Should().BeFalse(power.ToString());
        }
    }

    #endregion

    #region Roles

    [Test]
    public void TheMayorIsTheMayor_AnOfficerIsAnOfficer_AndAStrangerIsNothing()
    {
        var city = City();
        city.Officers[Officer] = new CityOfficer { Title = "Marshal" };

        CityPowerRules.GetRole(city, Mayor, Now).Should().Be(CityRole.Mayor);
        CityPowerRules.GetRole(city, Officer, Now).Should().Be(CityRole.Officer);
        CityPowerRules.GetRole(city, Citizen, Now).Should().Be(CityRole.None);
        CityPowerRules.GetRole(city, string.Empty, Now).Should().Be(CityRole.None);
        CityPowerRules.GetRole(null, Mayor, Now).Should().Be(CityRole.None);
    }

    [Test]
    public void ARunningActingMayorHasTheActingMayorRole()
    {
        var city = City();
        city.ActingMayorPlayerId = Acting;
        city.ActingMayorEndsUtc = Now.AddDays(10);

        CityPowerRules.GetRole(city, Acting, Now).Should().Be(CityRole.ActingMayor);
        CityPowerRules.GetRole(city, Mayor, Now).Should().Be(CityRole.Mayor, "the mayor keeps every power while a stand-in acts");
    }

    [Test]
    public void AnActingMayorWhoseTermRanOutIsACitizenAgain_EvenBeforeTheRecordIsCleared()
    {
        var city = City();
        city.ActingMayorPlayerId = Acting;
        city.ActingMayorEndsUtc = Now.AddMinutes(-1);

        CityPowerRules.GetRole(city, Acting, Now).Should().Be(CityRole.None);
    }

    [Test]
    public void AnActingMayorWhoIsAlsoAnOfficerActsAsActingMayor()
    {
        var city = City();
        city.ActingMayorPlayerId = Acting;
        city.ActingMayorEndsUtc = Now.AddDays(1);
        city.Officers[Acting] = new CityOfficer();

        CityPowerRules.GetRole(city, Acting, Now).Should().Be(CityRole.ActingMayor);
    }

    [Test]
    public void APendingStepDownChangesNothing()
    {
        var city = City();
        city.StepDown = new CityStepDown { InterimHolderPlayerId = Interim, Stage = CityStepDownStage.Pending, AcceptDeadlineUtc = Now.AddDays(7) };
        city.Officers[Interim] = new CityOfficer();

        CityPowerRules.GetRole(city, Mayor, Now).Should().Be(CityRole.Mayor, "the mayor keeps every power until the holder accepts");
        CityPowerRules.GetRole(city, Interim, Now).Should().Be(CityRole.Officer);
    }

    [Test]
    public void AfterTheStepDownIsAccepted_TheMayorIsDeparted_AndTheInterimHolderActs()
    {
        var city = City();
        city.StepDown = AcceptedStepDown();

        CityPowerRules.GetRole(city, Mayor, Now).Should().Be(CityRole.DepartedMayor);
        CityPowerRules.GetRole(city, Interim, Now).Should().Be(CityRole.ActingMayor);
        CityPowerRules.GetRole(city, Citizen, Now).Should().Be(CityRole.None);
    }

    [Test]
    public void TheInterimHolderHoldsTheSeatAcrossTheWholeSnapElection_WhateverAnActingTermSays()
    {
        var city = City();
        city.StepDown = AcceptedStepDown();
        city.ActingMayorPlayerId = Acting;
        city.ActingMayorEndsUtc = Now.AddDays(20);

        CityPowerRules.GetRole(city, Interim, Now.AddDays(5)).Should().Be(CityRole.ActingMayor);
        CityPowerRules.GetRole(city, Acting, Now).Should().Be(CityRole.None, "a stale term is not a second stand-in beside the interim holder");
    }

    [Test]
    public void TheInterimHolderCanDoWhatAnActingMayorCanAndNoMore()
    {
        var city = City();
        city.StepDown = AcceptedStepDown();
        var role = CityPowerRules.GetRole(city, Interim, Now);

        CityPowerRules.HasPower(role, CityPower.HandleCityAffairs, null).Should().BeTrue();
        CityPowerRules.HasPower(role, CityPower.EnactEdicts, null).Should().BeTrue();
        CityPowerRules.HasPower(role, CityPower.WithdrawTreasury, null).Should().BeFalse();
        CityPowerRules.HasPower(role, CityPower.RepealEdicts, null).Should().BeFalse();
        CityPowerRules.HasPower(role, CityPower.Appoint, null).Should().BeFalse();
    }

    #endregion

    #region Manage City and titles

    [Test]
    public void TheManageCityWindowOpensForTheMayor_AnActingMayor_AndAPermissionHolder()
    {
        CityPowerRules.CanOpenManageCity(CityRole.Mayor, null, false).Should().BeTrue();
        CityPowerRules.CanOpenManageCity(CityRole.ActingMayor, null, false).Should().BeTrue();
        CityPowerRules.CanOpenManageCity(CityRole.None, Granted(PropertyPermissionType.EditTaxes), false).Should().BeTrue();
        CityPowerRules.CanOpenManageCity(CityRole.None, Granted(PropertyPermissionType.ManageEdicts), false).Should().BeTrue();
    }

    [Test]
    public void TheManageCityWindowStaysClosedToAnOfficerWithNothingElse()
    {
        CityPowerRules.CanOpenManageCity(CityRole.Officer, Granted(PropertyPermissionType.HandleCityAffairs), false).Should().BeFalse();
        CityPowerRules.CanOpenManageCity(CityRole.None, null, false).Should().BeFalse();
        CityPowerRules.CanOpenManageCity(CityRole.None, Granted(PropertyPermissionType.EditStructures), false).Should().BeFalse();
    }

    [Test]
    public void ThePlayerNamedToHoldTheSeatCanOpenManageCityToAnswer()
    {
        CityPowerRules.CanOpenManageCity(CityRole.Officer, null, true).Should().BeTrue();
        CityPowerRules.CanOpenManageCity(CityRole.None, null, true).Should().BeTrue();
    }

    [Test]
    public void ADepartedMayorCannotOpenManageCity_WhateverTheirRecordSays()
    {
        var everything = Enum.GetValues<PropertyPermissionType>().ToDictionary(type => type, _ => true);

        CityPowerRules.CanOpenManageCity(CityRole.DepartedMayor, everything, false).Should().BeFalse();
        CityPowerRules.CanOpenManageCity(CityRole.DepartedMayor, everything, true).Should().BeFalse();
    }

    [Test]
    public void TaxAndEdictEntriesAreSignedMayorActingMayorOrDelegate()
    {
        CityPowerRules.GetDelegateTitle(CityRole.Mayor).Should().Be("Mayor");
        CityPowerRules.GetDelegateTitle(CityRole.ActingMayor).Should().Be("Acting Mayor");
        CityPowerRules.GetDelegateTitle(CityRole.None).Should().Be("Delegate");
        CityPowerRules.GetDelegateTitle(CityRole.Officer).Should().Be("Delegate");
    }

    #endregion
}
