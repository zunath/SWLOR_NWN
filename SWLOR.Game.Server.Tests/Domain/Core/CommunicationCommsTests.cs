using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;
using static SWLOR.NWN.API.NWScript.NWScript;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the Comms channel (the native Party channel, relabeled): who receives a message by the sender's
/// location scope, who overhears it, which DMs read it, the out-of-range warning and the DM party prefix, against the
/// scripted world in <see cref="CommunicationBench"/>. They replace the source-text checks of the old static class and
/// the harness that compiled its DM Comms routing.
/// </summary>
[TestFixture]
public class CommunicationCommsTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint OtherDM = 4;
    private const uint Alice = 10;
    private const uint Bob = 11;
    private const uint Carol = 12;
    private const uint Dave = 13;
    private const uint Npc = 50;
    private const uint Hologram = 60;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;
    private const uint Orbit = 102;
    private const uint FarOrbit = 103;
    private const uint EventHall = 104;
    private const uint EventStage = 105;
    private const uint ShipDeck = 106;
    private const uint ShipBridge = 107;
    private const uint Apartment = 108;
    private const uint Street = 109;
    private const uint CityArea = 110;

    private const string OutOfRange = "Your Comms message could not reach one or more out-of-range receivers.";

    private CommunicationBench _bench = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new CommunicationBench();
        _bench.AddPlayer(Alice, area: Cantina, name: "Alice");
    }

    [TearDown]
    public void TearDown()
    {
        _bench.Dispose();
    }

    private static string Basic(string text) => ColorToken.Custom(text, 255, 255, 255);

    // Comms messages are orange as a whole, including the DM party prefix.
    private static string Comms(string text) => ColorToken.Orange(text);

    private bool Hears(uint receiver)
    {
        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");
        return _bench.Receivers.Contains(receiver);
    }

    private void PartyOfTwoAcrossAreas(uint bobArea)
    {
        _bench.AddPlayer(Bob, area: bobArea, name: "Bob");
        _bench.Group(Alice, Bob);
    }

    // ---- who receives

    [Test]
    public void Comms_ReachesPartyMembersInRangeAndNearbyListenersAndSkipsThoseTooFarToOverhear()
    {
        _bench.AddPlayer(Bob, area: Cantina);
        _bench.AddPlayer(Carol, area: Cantina);
        _bench.AddPlayer(Dave, area: Cantina);
        _bench.Group(Alice, Bob);
        _bench.At(Carol, 20f);
        _bench.At(Dave, 20.5f);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Skipped.Should().BeTrue();
        _bench.Receivers.Should().Equal(Alice, Bob, Carol);
        _bench.Sent.Should().OnlyContain(sent =>
            sent.Channel == ChatChannel.PlayerParty && sent.Sender == Alice && sent.Message == Comms(Basic("hello")));
    }

    [Test]
    public void Comms_IsSentThroughTheNativePartyChannelWithTheObserversNameOverride()
    {
        // NWNX_Rename patches the name override around the native Party channel, so the speaker's true name never
        // leaks once they leave the receiver's area; DM Talk is not patched and is never used.
        PartyOfTwoAcrossAreas(Cantina);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Service.NameOverrides.Should().Equal((Alice, Alice, 1), (Bob, Alice, 1));
        _bench.Sent.Should().NotContain(sent => sent.Channel == ChatChannel.DMTalk);
    }

    [Test]
    public void ATalkMessageIsNotRelayedToThePartyElsewhere()
    {
        PartyOfTwoAcrossAreas(Spaceport);

        _bench.Says(ChatChannel.PlayerTalk, Alice, "hello");

        _bench.Receivers.Should().Equal(Alice);
    }

    [Test]
    public void Comms_ReachesAPartyMemberOnTheSamePlanetInAnotherArea()
    {
        PartyOfTwoAcrossAreas(Spaceport);
        _bench.AreaPlanets[Cantina] = PlanetType.Viscara;
        _bench.AreaPlanets[Spaceport] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue();
        _bench.Notices.Should().BeEmpty();
    }

    [Test]
    public void Comms_DoesNotReachAPartyMemberOnAnotherPlanetAndWarnsTheSenderOnce()
    {
        PartyOfTwoAcrossAreas(Spaceport);
        _bench.AddPlayer(Carol, area: Spaceport);
        _bench.Group(Alice, Bob, Carol);
        _bench.AreaPlanets[Cantina] = PlanetType.Viscara;
        _bench.AreaPlanets[Spaceport] = PlanetType.Tatooine;

        Hears(Bob).Should().BeFalse();

        _bench.Receivers.Should().Equal(Alice);
        // One warning, however many members are out of range.
        _bench.Notices.Should().Equal(ColorToken.Red(OutOfRange));
    }

    [Test]
    public void Comms_WithoutAPlanetReachesOnlyThePartyMembersInTheSameArea()
    {
        PartyOfTwoAcrossAreas(Spaceport);

        Hears(Bob).Should().BeFalse();

        _bench.Notices.Should().Equal(ColorToken.Red(OutOfRange));
    }

    [Test]
    public void Comms_DoesNotReachAPartyMemberWhoseAreaIsNotValid()
    {
        // A member with no valid area cannot be placed anywhere, so they are out of range.
        _bench.AddPlayer(Bob, area: Invalid);
        _bench.Group(Alice, Bob);

        Hears(Bob).Should().BeFalse();
        _bench.Notices.Should().Equal(ColorToken.Red(OutOfRange));
    }

    [Test]
    public void Comms_ReachesADMInThePartyWhereverTheyAre()
    {
        _bench.AddPlayer(DM, isDM: true, area: Spaceport);
        _bench.Group(Alice, DM);

        Hears(DM).Should().BeTrue();
        _bench.Receivers.Should().Equal(new[] { Alice, DM }, "a DM who is a party member and a DM reader is sent the message once");
        _bench.Notices.Should().BeEmpty();
    }

    [Test]
    public void Comms_ReachesTheMembersInRangeWhenAnotherMemberHasNoArea()
    {
        _bench.AddPlayer(Bob, area: Cantina);
        _bench.AddPlayer(Carol, area: Cantina);
        _bench.World.Place(Carol, Invalid);
        _bench.Group(Alice, Bob, Carol);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Receivers.Should().Equal(Alice, Bob);
        _bench.Notices.Should().Equal(ColorToken.Red(OutOfRange));
    }

    // ---- the warning

    [Test]
    public void TheOutOfRangeWarningCanBeTurnedOffInSettings()
    {
        PartyOfTwoAcrossAreas(Spaceport);
        _bench.Record(Alice).Settings.DisplayCommsOutOfRangeWarnings = false;

        Hears(Bob).Should().BeFalse();

        _bench.Notices.Should().BeEmpty();
    }

    [Test]
    public void TheOutOfRangeWarningDefaultsToOnWhenTheSettingIsUnset()
    {
        PartyOfTwoAcrossAreas(Spaceport);
        _bench.Record(Alice).Settings.DisplayCommsOutOfRangeWarnings = null;

        Hears(Bob).Should().BeFalse();

        _bench.Notices.Should().Equal(ColorToken.Red(OutOfRange));
    }

    [Test]
    public void TheOutOfRangeWarningIsShownToASenderWithNoSavedCharacter()
    {
        _bench.World.AddPlayer(Dave, area: Cantina);
        _bench.AddPlayer(Bob, area: Spaceport);
        _bench.Group(Dave, Bob);

        _bench.Says(ChatChannel.PlayerParty, Dave, "hello");

        _bench.Notices.Should().Equal(ColorToken.Red(OutOfRange));
    }

    [Test]
    public void OnlyPlayersCountAsOutOfRangeReceivers()
    {
        _bench.AddCreature(Npc, Spaceport);
        _bench.Group(Alice, Npc);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Notices.Should().BeEmpty("a companion that is not a player cannot be warned about");
        _bench.Receivers.Should().Equal(Alice);
    }

    // ---- the sender's location scope

    [Test]
    public void ASenderInSpaceReachesOnlyTheirOwnSpaceArea()
    {
        PartyOfTwoAcrossAreas(Orbit);
        _bench.AddPlayer(Carol, area: FarOrbit);
        _bench.Group(Alice, Bob, Carol);
        _bench.World.Place(Alice, Orbit);
        SetLocalBool(Orbit, "SPACE", true);
        SetLocalBool(FarOrbit, "SPACE", true);
        _bench.AreaPlanets[Orbit] = PlanetType.Viscara;
        _bench.AreaPlanets[FarOrbit] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue("they share the area");
        Hears(Carol).Should().BeFalse("another space area is out of range even on the same planet");
    }

    [Test]
    public void AnAreaNamedLikeASpaceAreaCountsAsSpace()
    {
        PartyOfTwoAcrossAreas(Cantina);
        _bench.World.Name(Cantina, "Space - Viscara Orbit");
        _bench.World.Place(Bob, FarOrbit);
        _bench.AreaPlanets[Cantina] = PlanetType.Viscara;
        _bench.AreaPlanets[FarOrbit] = PlanetType.Viscara;

        Hears(Bob).Should().BeFalse();
    }

    [Test]
    public void AnEventAreaReachesEveryOtherEventAreaAndNothingElse()
    {
        _bench.AddPlayer(Bob, area: EventStage);
        _bench.AddPlayer(Carol, area: Spaceport);
        _bench.Group(Alice, Bob, Carol);
        _bench.World.Place(Alice, EventHall);
        SetLocalBool(EventHall, Communication.EventCommsAreaVariable, true);
        SetLocalBool(EventStage, Communication.EventCommsAreaVariable, true);
        _bench.AreaPlanets[EventHall] = PlanetType.Viscara;
        _bench.AreaPlanets[Spaceport] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue();
        Hears(Carol).Should().BeFalse("an event area ignores the planet");
    }

    [Test]
    public void ASenderOutsideAnEventAreaDoesNotReachIntoOne()
    {
        PartyOfTwoAcrossAreas(EventStage);
        SetLocalBool(EventStage, Communication.EventCommsAreaVariable, true);

        Hears(Bob).Should().BeFalse();
    }

    [Test]
    public void APlayerAboardTheSameStarshipIsReachedWhateverArea()
    {
        PartyOfTwoAcrossAreas(ShipBridge);
        _bench.AddProperty("ship-1", PropertyType.Starship);
        _bench.PropertyIds[Cantina] = "ship-1";
        _bench.PropertyIds[ShipBridge] = "ship-1";
        _bench.AreaPlanets[Cantina] = PlanetType.Viscara;
        _bench.AreaPlanets[ShipBridge] = PlanetType.Tatooine;

        Hears(Bob).Should().BeTrue("the same starship outranks the planet rule");
    }

    [Test]
    public void AShipFlownInSpaceBelongsToItsStarshipProperty()
    {
        PartyOfTwoAcrossAreas(ShipBridge);
        _bench.AddProperty("ship-1", PropertyType.Starship);
        _bench.Db.With(new PlayerShip { Id = "pilot-ship", PropertyId = "ship-1" });
        _bench.Record(Alice).ActiveShipId = "pilot-ship";
        _bench.Service.InSpace.Add(Alice);
        _bench.World.Place(Alice, Orbit);
        _bench.PropertyIds[ShipBridge] = "ship-1";

        Hears(Bob).Should().BeTrue("the pilot in space and the crew inside are on one starship");
    }

    [Test]
    public void ACharacterWhoseSavedShipIsMissingFallsBackToTheAreasProperty()
    {
        PartyOfTwoAcrossAreas(ShipBridge);
        _bench.AddProperty("ship-1", PropertyType.Starship);
        _bench.Record(Alice).ActiveShipId = "gone";
        _bench.Service.InSpace.Add(Alice);
        _bench.PropertyIds[Cantina] = "ship-1";
        _bench.PropertyIds[ShipBridge] = "ship-1";

        Hears(Bob).Should().BeTrue();
    }

    [Test]
    public void APropertyThatIsNotAStarshipDoesNotMakeAShipmate()
    {
        PartyOfTwoAcrossAreas(Spaceport);
        _bench.AddProperty("apartment-1", PropertyType.Apartment);
        _bench.PropertyIds[Cantina] = "apartment-1";
        _bench.PropertyIds[Spaceport] = "apartment-1";

        Hears(Bob).Should().BeFalse("only a starship counts; an apartment is resolved to its planet or its area");
    }

    // ---- the planet of a property

    [Test]
    public void AnApartmentInACityResolvesToTheCitysPlanetThroughItsLoadedArea()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("city-1", PropertyType.City, "city-resref");
        _bench.AddProperty("apt-1", PropertyType.Apartment, "city-1");
        _bench.PropertyIds[Apartment] = "apt-1";
        _bench.AreasByResref["city-resref"] = Street;
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue();
    }

    [Test]
    public void AnApartmentResolvesToThePlanetOfItsCityAreaThatIsNotLoaded()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("city-1", PropertyType.City, "city-resref");
        _bench.AddProperty("apt-1", PropertyType.Apartment, "city-1");
        _bench.PropertyIds[Apartment] = "apt-1";
        _bench.ResrefPlanets["city-resref"] = PlanetType.Viscara;
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue();
    }

    [Test]
    public void ALoadedCityAreaWithoutAPlanetFallsBackToThePlanetOfItsResref()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("city-1", PropertyType.City, "city-resref");
        _bench.AddProperty("apt-1", PropertyType.Apartment, "city-1");
        _bench.PropertyIds[Apartment] = "apt-1";
        _bench.AreasByResref["city-resref"] = CityArea;
        _bench.ResrefPlanets["city-resref"] = PlanetType.Viscara;
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue();
    }

    [Test]
    public void ABuildingWhoseParentIsAnAreaResrefResolvesToThatAreasPlanet()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("apt-1", PropertyType.Apartment, "street-resref");
        _bench.PropertyIds[Apartment] = "apt-1";
        _bench.ResrefPlanets["street-resref"] = PlanetType.Viscara;
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue();
    }

    [TestCase(PropertyLocationType.CurrentPosition)]
    [TestCase(PropertyLocationType.DockPosition)]
    public void AStarshipResolvesToThePlanetWhereItIsOrWhereItIsDocked(PropertyLocationType position)
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("ship-1", PropertyType.Starship).Positions[position] =
            new PropertyLocation { AreaResref = "dock-resref" };
        _bench.PropertyIds[Apartment] = "ship-1";
        _bench.ResrefPlanets["dock-resref"] = PlanetType.Viscara;
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        // Bob is in the street, not aboard, so only the planet rule can reach him.
        Hears(Bob).Should().BeTrue();
    }

    [Test]
    public void AStarshipWithNoPositionResolvesToNothing()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("ship-1", PropertyType.Starship);
        _bench.PropertyIds[Apartment] = "ship-1";
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AStarshipWhosePositionNamesNeitherAnAreaNorAnInstanceResolvesToNothing(bool positionIsMissing)
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("ship-1", PropertyType.Starship).Positions[PropertyLocationType.CurrentPosition] =
            positionIsMissing ? null : new PropertyLocation();
        _bench.PropertyIds[Apartment] = "ship-1";
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeFalse();
    }

    [Test]
    public void AParkedStarshipResolvesToTheInstancePropertyItIsParkedIn()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("city-1", PropertyType.City, "city-resref");
        _bench.AddProperty("sub-1", PropertyType.Apartment, "city-1");
        _bench.AddProperty("ship-1", PropertyType.Starship).Positions[PropertyLocationType.CurrentPosition] =
            new PropertyLocation { InstancePropertyId = "sub-1" };
        _bench.PropertyIds[Apartment] = "ship-1";
        _bench.ResrefPlanets["city-resref"] = PlanetType.Viscara;
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeTrue("the ship's instance resolves through its city to the planet of Bob's street");
    }

    [Test]
    public void APropertyWithNoParentOrKnownPlanetFallsBackToTheSameAreaRule()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("apt-1", PropertyType.Apartment);
        _bench.PropertyIds[Apartment] = "apt-1";
        _bench.AreaPlanets[Street] = PlanetType.Viscara;

        Hears(Bob).Should().BeFalse();
    }

    [Test]
    public void APropertyThatIsNotSavedResolvesToNothing()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.PropertyIds[Apartment] = "missing";

        Hears(Bob).Should().BeFalse();
    }

    [Test]
    public void ACycleOfParentPropertiesEndsInsteadOfLooping()
    {
        PartyOfTwoAcrossAreas(Street);
        _bench.World.Place(Alice, Apartment);
        _bench.AddProperty("p1", PropertyType.Apartment, "p2");
        _bench.AddProperty("p2", PropertyType.Apartment, "p1");
        _bench.PropertyIds[Apartment] = "p1";

        Hears(Bob).Should().BeFalse();
    }

    // ---- the DMs who read Comms

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void Comms_ReachEveryDMIncludingOneWhoIsPossessing(int possessionType)
    {
        _bench.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        PossessingDM(possessionType);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Receivers.Should().Equal(Alice, OtherDM, Creature);
        _bench.Receivers.Select(_bench.World.ReceivingPlayer).Should().Equal(Alice, OtherDM, DM);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void OwnPartyFilter_HidesOtherPartiesCommsFromEveryWayTheDMCouldHearThem(int possessionType)
    {
        _bench.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        PossessingDM(possessionType);
        SetCommsFilter(Creature, DMCommsFilter.OwnParty);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Receivers.Should().Equal(Alice, OtherDM);
        _bench.Receivers.Should().NotContain(Creature, "a muted DM does not overhear it either");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void OwnPartyFilter_KeepsTheDMsOwnPartyWhilePossessing(int possessionType)
    {
        PossessingDM(possessionType);
        _bench.PartyPairs.Add((Alice, DM));
        SetCommsFilter(Creature, DMCommsFilter.OwnParty);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Receivers.Should().Equal(Alice, Creature);
    }

    [Test]
    public void OwnPartyFilter_ChecksThePartyOfThePossessingDMWhoSentIt()
    {
        _bench.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        PossessingDM(DMPossess);
        _bench.PartyPairs.Add((DM, OtherDM));
        SetCommsFilter(OtherDM, DMCommsFilter.OwnParty);

        _bench.Says(ChatChannel.PlayerParty, Creature, "hello");

        _bench.Receivers.Should().Contain(OtherDM);
    }

    [Test]
    public void CommsFilterSetOnTheAvatarFollowsTheDMIntoPossession()
    {
        _bench.AddPlayer(DM, isDM: true, area: Cantina);
        SetCommsFilter(DM, DMCommsFilter.OwnParty);
        _bench.World.Possess(DM, Creature, DMPossess, Cantina);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Receivers.Should().Equal(Alice);
    }

    [Test]
    public void ADMReadsAnotherPartysCommsWithTheirOwnersAsAPrefix()
    {
        _bench.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        _bench.AddPlayer(Bob, area: Cantina, name: "Bob");
        _bench.Group(Alice, Bob);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Sent.Single(sent => sent.Target == OtherDM).Message
            .Should().Be(Comms("{ Alice,Bob } " + Basic("hello")));
        _bench.Sent.Single(sent => sent.Target == Bob).Message.Should().Be(Comms(Basic("hello")));
    }

    [Test]
    public void TheDMPrefixNamesAtMostThreeMembersEachCutToTenLetters()
    {
        _bench.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        _bench.AddPlayer(Bob, area: Cantina, name: "Bartholomew Longname");
        _bench.AddPlayer(Carol, area: Cantina, name: "Carol");
        _bench.AddPlayer(Dave, area: Cantina, name: "Dave");
        _bench.Group(Alice, Bob, Carol, Dave);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Sent.Single(sent => sent.Target == OtherDM).Message
            .Should().Be(Comms("{ Alice,Bartholome,Carol, ... } " + Basic("hello")));
    }

    [Test]
    public void TheDMPrefixOfALoneSenderIsJustTheirName()
    {
        _bench.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        _bench.Group(Alice);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Sent.Single(sent => sent.Target == OtherDM).Message
            .Should().Be(Comms("{ Alice } " + Basic("hello")));
    }

    // ---- relays and languages

    [Test]
    public void AHologramRelaysCommsOnThePartyChannelNamingItsOwner()
    {
        _bench.AddPlayer(Bob, area: Spaceport);
        _bench.AddCreature(Hologram, Cantina);
        _bench.HologramOwners[Hologram] = Alice;
        _bench.Group(Hologram, Bob);
        _bench.AreaPlanets[Cantina] = PlanetType.Viscara;
        _bench.AreaPlanets[Spaceport] = PlanetType.Viscara;

        _bench.Says(ChatChannel.PlayerParty, Hologram, "hello");

        _bench.Receivers.Should().Equal(Hologram, Bob, Alice);
        _bench.Sent.Should().OnlyContain(sent => sent.Channel == ChatChannel.PlayerParty && sent.Sender == Hologram);
        _bench.Sent.Single(sent => sent.Target == Bob).Message
            .Should().Be(Comms(TestCommunicationService.ChatName(Bob, Alice) + ": " + Basic("hello")));
        _bench.Service.NameOverrides.Should().BeEmpty();
    }

    [Test]
    public void CommsAreTranslatedAndLabeledLikeTalk()
    {
        _bench.AddPlayer(Bob, area: Cantina);
        _bench.Group(Alice, Bob);
        _bench.Speaks(Alice, SkillType.Huttese);
        var (r, g, b) = Language.GetColor(SkillType.Huttese);

        _bench.Says(ChatChannel.PlayerParty, Alice, "hello");

        _bench.Sent.Single(sent => sent.Target == Bob).Message.Should().Be(Comms(
            ColorToken.Custom("[Huttese] ", r, g, b) +
            ColorToken.Custom(TestCommunicationService.Translated(Bob, "hello"), r, g, b)));
    }

    private void PossessingDM(int possessionType)
    {
        _bench.AddPlayer(DM, isDM: true, area: Spaceport);
        _bench.World.Possess(DM, Creature, possessionType, Cantina);
    }

    private void SetCommsFilter(uint actor, DMCommsFilter filter) =>
        _bench.DMPreference.Update(actor, preferences => preferences.CommsFilter = filter).Should().BeTrue();
}
