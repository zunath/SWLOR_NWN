using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PlayerIntroductionService;

namespace SWLOR.Game.Server.Tests.Service;

public class PlayerIntroductionTests
{
    private static readonly DateTime Now = new(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);

    private static PlayerIntroductionOffer Offer(string name = "Jax", string presenterId = "speaker",
        string identity = "disguise:one") =>
        new(Guid.NewGuid(), 42, presenterId, identity, "Tall Human", name, Now.AddMinutes(10));

    [Test]
    public void Dismissing_OnlyRemovesTheRecipientsOffer()
    {
        var inbox = new PlayerIntroductionInbox();
        var offer = Offer();
        inbox.Add("observer", offer, Now).Should().BeTrue();
        inbox.Add("another-observer", offer, Now).Should().BeTrue();
        inbox.GetPending("observer", Now).Should().ContainSingle().Which.Should().Be(offer);
        inbox.Dismiss("observer", offer.Id);

        inbox.GetPending("observer", Now).Should().BeEmpty();
        inbox.GetPending("another-observer", Now).Should().Equal(offer);
    }

    [TestCase(null)]
    [TestCase("Red Coat")]
    public void Accepting_SavesTheOfferedNameOnlyOnce(string previousName)
    {
        var inbox = new PlayerIntroductionInbox();
        var offer = Offer();
        inbox.Add("observer", offer, Now);
        var savedName = previousName;
        var saves = 0;
        string Validate(PlayerIntroductionOffer entry) => entry.ValidateIdentityAndConsent(
            "speaker", "disguise:one", savedName, previousName);
        void Remember(PlayerIntroductionOffer entry) { savedName = entry.Name; saves++; }

        inbox.Accept("observer", offer.Id, Now, Validate, Remember).Should().BeEmpty();
        inbox.Accept("observer", offer.Id, Now, Validate, Remember).Should().NotBeEmpty();

        savedName.Should().Be("Jax");
        saves.Should().Be(1);
    }

    [TestCase("speaker", "disguise:two", "Red Coat", "Red Coat")]
    [TestCase("speaker", "speaker", "Red Coat", "Red Coat")]
    [TestCase("another-player", "disguise:one", "Red Coat", "Red Coat")]
    [TestCase("speaker", "disguise:one", "New label", "Red Coat")]
    [TestCase("speaker", "disguise:one", "New label", null)]
    [TestCase("speaker", "disguise:one", null, "Red Coat")]
    public void StaleIdentityOrConsent_CannotSave(string presenterId, string identity,
        string currentName, string approvedName)
    {
        var inbox = new PlayerIntroductionInbox();
        var offer = Offer();
        inbox.Add("observer", offer, Now);
        var saved = false;

        var error = inbox.Accept("observer", offer.Id, Now,
            entry => entry.ValidateIdentityAndConsent(presenterId, identity, currentName, approvedName),
            _ => saved = true);

        error.Should().NotBeEmpty();
        saved.Should().BeFalse();
    }

    [Test]
    public void ExpiredOrUnownedOffers_CannotBeAccepted()
    {
        var inbox = new PlayerIntroductionInbox();
        var offer = Offer();
        inbox.Add("observer", offer, Now);
        var saved = false;

        inbox.Accept("other-observer", offer.Id, Now, _ => "", _ => saved = true).Should().NotBeEmpty();
        inbox.Accept("observer", offer.Id, offer.ExpiresAt, _ => "", _ => saved = true).Should().NotBeEmpty();

        saved.Should().BeFalse();
        inbox.GetPending("observer", offer.ExpiresAt).Should().BeEmpty();
    }

    [Test]
    public void RepeatingPendingName_DoesNotReplaceOfferOrExtendExpiry()
    {
        var inbox = new PlayerIntroductionInbox();
        var offer = Offer();
        inbox.Add("observer", offer, Now);
        var repeatedOffer = Offer() with { ExpiresAt = Now.AddMinutes(15) };

        inbox.Add("observer", repeatedOffer, Now.AddMinutes(5)).Should().BeFalse();
        inbox.GetPending("observer", Now.AddMinutes(5)).Should().Equal(offer);
        inbox.GetPending("observer", offer.ExpiresAt).Should().BeEmpty();
    }

    [Test]
    public void ChangedIntroduction_InvalidatesOldAcceptanceWithoutSilentlySwitchingNames()
    {
        var inbox = new PlayerIntroductionInbox();
        var oldOffer = Offer();
        var newOffer = Offer("Talon");
        inbox.Add("observer", oldOffer, Now);
        inbox.Add("observer", newOffer, Now);
        var saved = false;

        inbox.Accept("observer", oldOffer.Id, Now, _ => "", _ => saved = true).Should().NotBeEmpty();

        saved.Should().BeFalse();
        inbox.GetPending("observer", Now).Should().Equal(newOffer);
    }

    [Test]
    public void NameValidationFailure_PreservesOfferForRetryAndDoesNotSave()
    {
        var inbox = new PlayerIntroductionInbox();
        var offer = Offer();
        inbox.Add("observer", offer, Now);
        var saved = false;

        inbox.Accept("observer", offer.Id, Now, _ => "Name already used", _ => saved = true)
            .Should().Be("Name already used");

        saved.Should().BeFalse();
        inbox.GetPending("observer", Now).Should().Equal(offer);
    }

    [Test]
    public void Disconnect_RemovesSentAndReceivedOffersWithoutAffectingOtherPlayers()
    {
        var inbox = new PlayerIntroductionInbox();
        var remaining = Offer(presenterId: "third-player");
        inbox.Add("observer", Offer(), Now);
        inbox.Add("observer", remaining, Now);
        inbox.Add("speaker", remaining, Now);

        inbox.RemovePlayer("speaker");

        inbox.GetPending("observer", Now).Should().Equal(remaining);
        inbox.GetPending("speaker", Now).Should().BeEmpty();
    }

    [Test]
    public void InboxCapacity_DropsOldestOffers()
    {
        var inbox = new PlayerIntroductionInbox();
        var first = Offer();
        inbox.Add("observer", first, Now);
        for (var index = 0; index < PlayerIntroductionInbox.MaximumPendingOffers; index++)
            inbox.Add("observer", Offer(presenterId: $"speaker-{index}"), Now);

        inbox.GetPending("observer", Now).Should().HaveCount(PlayerIntroductionInbox.MaximumPendingOffers)
            .And.NotContain(first);
    }
}
