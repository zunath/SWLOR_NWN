using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The officer rules: the limit by city level, appointment, titles, who may handle City Affairs, and a level drop.</summary>
public class OfficerRulesTests
{
    private const string Mayor = "mayor";
    private const string Citizen = "citizen";

    private static readonly DateTime Start = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Dictionary<string, CityOfficer> Officers(params (string Id, string Title, int DaysAfterStart)[] officers)
    {
        return officers.ToDictionary(
            officer => officer.Id,
            officer => new CityOfficer { Title = officer.Title, AppointedUtc = Start.AddDays(officer.DaysAfterStart) });
    }

    #region Limit

    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 4)]
    [TestCase(4, 5)]
    [TestCase(5, 6)]
    public void ACityHasOneOfficerPlusOnePerLevel(int cityLevel, int expectedLimit)
    {
        OfficerRules.GetOfficerLimit(cityLevel).Should().Be(expectedLimit);
    }

    [Test]
    public void TheLimit_StaysWithinTwoToSix()
    {
        OfficerRules.GetOfficerLimit(0).Should().Be(2);
        OfficerRules.GetOfficerLimit(9).Should().Be(6);
    }

    #endregion

    #region Appointing

    [Test]
    public void TheMayorCanAppointACitizen()
    {
        OfficerRules.CheckAppointment(Mayor, Mayor, Citizen, true, new List<string>(), 1).Should().BeEmpty();
    }

    [Test]
    public void OnlyTheMayorCanAppoint()
    {
        OfficerRules.CheckAppointment("officer", Mayor, Citizen, true, new[] { "officer" }, 3)
            .Should().Be("Only the mayor can appoint officers.");
        OfficerRules.CheckAppointment(string.Empty, Mayor, Citizen, true, new List<string>(), 3).Should().NotBeEmpty();
    }

    [Test]
    public void OnlyACitizenOfTheCityCanBeAppointed()
    {
        OfficerRules.CheckAppointment(Mayor, Mayor, Citizen, false, new List<string>(), 3)
            .Should().Be("Only citizens of this city can be officers.");
    }

    [Test]
    public void TheMayorAndExistingOfficersCannotBeAppointed()
    {
        OfficerRules.CheckAppointment(Mayor, Mayor, Mayor, true, new List<string>(), 3).Should().NotBeEmpty();
        OfficerRules.CheckAppointment(Mayor, Mayor, Citizen, true, new[] { Citizen }, 3)
            .Should().Be("That citizen is already an officer.");
        OfficerRules.CheckAppointment(Mayor, Mayor, string.Empty, true, new List<string>(), 3).Should().NotBeEmpty();
    }

    [Test]
    public void TheMayorCannotAppointPastTheLimit()
    {
        var full = new[] { "a", "b" };

        OfficerRules.CheckAppointment(Mayor, Mayor, Citizen, true, full, 1).Should().NotBeEmpty("a level 1 city has 2 officers");
        OfficerRules.CheckAppointment(Mayor, Mayor, Citizen, true, full, 2).Should().BeEmpty("a level 2 city has 3");
    }

    #endregion

    #region Titles

    [TestCase("Minister of Trade", "Minister of Trade")]
    [TestCase("  Marshal  ", "Marshal")]
    [TestCase("High   Chancellor", "High Chancellor")]
    [TestCase("", "Officer")]
    [TestCase("   ", "Officer")]
    [TestCase(null, "Officer")]
    [TestCase("<c255>Red<c>", "c255Redc")]
    [TestCase("Line\nBreak", "LineBreak")]
    public void ATitle_IsCleanedUp(string typed, string expected)
    {
        OfficerRules.NormalizeTitle(typed).Should().Be(expected);
    }

    [Test]
    public void ATitle_IsCutToThirtyTwoCharacters()
    {
        var title = OfficerRules.NormalizeTitle(new string('A', 40));

        title.Should().HaveLength(OfficerRules.TitleMaxLength);
        OfficerRules.NormalizeTitle("Minister of Trade and Foreign Affairs of the Outer Rim").Length.Should().BeLessThanOrEqualTo(32);
    }

    #endregion

    #region Handling City Affairs

    [Test]
    public void TheMayor_AlwaysHandlesCityAffairs()
    {
        OfficerRules.CanHandleCityAffairs(Mayor, Mayor, true, Officers(), false).Should().BeTrue();
    }

    [Test]
    public void AnOfficer_HandlesCityAffairs_OnlyWithTheOfficeThePermissionAndCitizenship()
    {
        var officers = Officers(("officer", "Marshal", 0));

        OfficerRules.CanHandleCityAffairs("officer", Mayor, true, officers, true).Should().BeTrue();
        OfficerRules.CanHandleCityAffairs("officer", Mayor, false, officers, true).Should().BeFalse("they left the city");
        OfficerRules.CanHandleCityAffairs("officer", Mayor, true, officers, false).Should().BeFalse("the permission was taken");
        OfficerRules.CanHandleCityAffairs("officer", Mayor, true, Officers(), true).Should().BeFalse("the permission alone is not an office");
    }

    [Test]
    public void NobodyElse_HandlesCityAffairs()
    {
        OfficerRules.CanHandleCityAffairs(Citizen, Mayor, true, Officers(("officer", "Marshal", 0)), true).Should().BeFalse();
        OfficerRules.CanHandleCityAffairs(string.Empty, Mayor, true, Officers(), true).Should().BeFalse();
    }

    [Test]
    public void AnOfficial_ActsUnderTheirTitle()
    {
        var officers = Officers(("officer", "Minister of Trade", 0), ("blank", "", 1));

        OfficerRules.GetTitle(Mayor, Mayor, officers).Should().Be("Mayor");
        OfficerRules.GetTitle("officer", Mayor, officers).Should().Be("Minister of Trade");
        OfficerRules.GetTitle("blank", Mayor, officers).Should().Be("Officer");
    }

    #endregion

    #region A level drop

    [Test]
    public void ALevelDrop_EndsTheNewestAppointmentsAboveTheLimit()
    {
        var officers = Officers(("first", "A", 0), ("second", "B", 1), ("third", "C", 2), ("fourth", "D", 3), ("fifth", "E", 4));

        OfficerRules.SelectAppointmentsAboveLimit(officers, 5).Should().BeEmpty();
        OfficerRules.SelectAppointmentsAboveLimit(officers, 4).Should().BeEmpty("a level 4 city has 5");
        OfficerRules.SelectAppointmentsAboveLimit(officers, 3).Should().Equal("fifth");
        OfficerRules.SelectAppointmentsAboveLimit(officers, 2).Should().Equal("fifth", "fourth");
        OfficerRules.SelectAppointmentsAboveLimit(officers, 1).Should().Equal("fifth", "fourth", "third");
    }

    [Test]
    public void AnOlderOfficerKeepsTheirOffice_WhateverTheirName()
    {
        var officers = Officers(("zed", "Old hand", 0), ("abe", "Newcomer", 5), ("mia", "Newer", 6));

        OfficerRules.SelectAppointmentsAboveLimit(officers, 1).Should().Equal("mia");
        OfficerRules.SelectAppointmentsAboveLimit(null, 1).Should().BeEmpty();
    }

    [Test]
    public void AppointmentsMadeTogether_EndInNameOrder()
    {
        var officers = Officers(("beta", "B", 0), ("alpha", "A", 0), ("gamma", "C", 0), ("delta", "D", 0));

        OfficerRules.SelectAppointmentsAboveLimit(officers, 1).Should().Equal("alpha", "beta");
    }

    #endregion
}
