using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ChatPortraitTellService;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class ChatPortraitTellGuardTests
{
    private const uint Observer = 0x7FFFFFF0;
    private const uint SharedSlot = 5;
    private const uint OtherSlot = 6;
    // Two characters who share one public descriptor. The client never compares descriptors: it addresses the
    // tell to whoever holds the portrait's slot, so the guard keys on slots and character IDs only.
    private const string FirstSpecialist = "character-first";
    private const string SecondSpecialist = "character-second";
    private static readonly DateTime Now = new(2026, 10, 22, 17, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    [Test]
    public void PortraitFromALoggedOutPlayer_RefusesTheTellToWhoeverTookTheirSlot()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);

        // The first character logs out and the second logs in to the freed slot.
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.FormerHolder);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now.AddMinutes(1)).Should().BeTrue();
    }

    [Test]
    public void PortraitFromTheCurrentHolder_SendsTheTell()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, SecondSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.CurrentHolder);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void SharedDescriptorsOnlineTogether_NeitherPortraitIsStale()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, OtherSlot, SecondSpecialist);

        guard.Click(Observer, SharedSlot, FirstSpecialist, Now).Should().Be(ChatPortraitClick.CurrentHolder);
        guard.ConsumeRefusedTell(Observer, SharedSlot, FirstSpecialist, Now).Should().BeFalse();
        guard.Click(Observer, OtherSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.CurrentHolder);
        guard.ConsumeRefusedTell(Observer, OtherSlot, SecondSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void CharacterWhoLogsBackInToTheSameSlot_StillReceivesTellsFromOldPortraits()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);

        guard.Click(Observer, SharedSlot, FirstSpecialist, Now).Should().Be(ChatPortraitClick.CurrentHolder);
        guard.ConsumeRefusedTell(Observer, SharedSlot, FirstSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void FormerAndCurrentHolderBothSpoke_RefusesBecauseTheClickedLineIsUnknown()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, SharedSlot, SecondSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Ambiguous);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeTrue();
    }

    [Test]
    public void UnrecordedSlot_SendsTheTell()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Unrecorded);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void RefusedClick_EndsWithTheNextTell()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeTrue();
        // A tell the player then types to the same character was not started from the portrait.
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void RefusedClick_DoesNotBlockATellToAnotherPlayer()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.ConsumeRefusedTell(Observer, OtherSlot, SecondSpecialist, Now).Should().BeFalse();
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void RefusedClick_ExpiresAfterItsLifetime()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now + Lifetime + TimeSpan.FromSeconds(1)).Should().BeFalse();
    }

    [Test]
    public void LaterClickOnACurrentPortrait_ReplacesARefusedClick()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, OtherSlot, SecondSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.Click(Observer, OtherSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.CurrentHolder);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void ForgottenObserver_StartsWithAnEmptyChatLog()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.ForgetObserver(Observer);

        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeFalse();
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Unrecorded);
    }

    [Test]
    public void HistoryIsKeptPerObserver()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);

        guard.Click(Observer + 1, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Unrecorded);
    }

    [Test]
    public void SlotChangesHandsAfterACurrentClick_RefusesTheTell()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, FirstSpecialist, Now).Should().Be(ChatPortraitClick.CurrentHolder);

        // The first character logs out and the second takes the slot while the tell is being written.
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeTrue();
    }

    [Test]
    public void FormerHolderLinesThatScrolledOutOfTheChatLog_NoLongerMakeTheCurrentHolderAmbiguous()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        for (var line = 0; line < ChatPortraitTellGuard.ChatLogHorizon; line++)
            guard.RecordLine(Observer, OtherSlot, "character-chatty");
        guard.RecordLine(Observer, SharedSlot, SecondSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.CurrentHolder);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now).Should().BeFalse();
    }

    [Test]
    public void FormerHolderLinesStillInTheChatLog_KeepTheSlotAmbiguous()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        for (var line = 0; line < ChatPortraitTellGuard.ChatLogHorizon - 2; line++)
            guard.RecordLine(Observer, OtherSlot, "character-chatty");
        guard.RecordLine(Observer, SharedSlot, SecondSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Ambiguous);
    }

    [Test]
    public void PortraitThatScrolledOutOfTheChatLog_IsUnrecorded()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        for (var line = 0; line < ChatPortraitTellGuard.ChatLogHorizon; line++)
            guard.RecordLine(Observer, OtherSlot, "character-chatty");

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Unrecorded);
    }

    // A doubtful click that is clicked again, after the player was told who now holds the slot, sends the tell.
    [Test]
    public void AmbiguousPortraitClickedAgain_ConfirmsTheCurrentHolderAndSendsTheTell()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, SharedSlot, SecondSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Ambiguous);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(5)).Should().Be(ChatPortraitClick.Confirmed);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(30)).Should().BeFalse();
    }

    [Test]
    public void FormerHolderPortraitClickedAgain_ConfirmsTheCurrentHolderAndSendsTheTell()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.FormerHolder);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(5)).Should().Be(ChatPortraitClick.Confirmed);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(30)).Should().BeFalse();
    }

    [Test]
    public void DoubleClick_DoesNotConfirmBeforeThePlayerCouldReadWhoTheTellReaches()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, SharedSlot, SecondSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.Ambiguous);
        var tooSoon = Now + ChatPortraitTellGuard.ConfirmationDelay - TimeSpan.FromMilliseconds(1);
        guard.Click(Observer, SharedSlot, SecondSpecialist, tooSoon).Should().Be(ChatPortraitClick.Ambiguous);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, tooSoon).Should().BeTrue();

        // A deliberate click after reading the message still confirms.
        guard.Click(Observer, SharedSlot, SecondSpecialist, tooSoon + ChatPortraitTellGuard.ConfirmationDelay)
            .Should().Be(ChatPortraitClick.Confirmed);
    }

    [Test]
    public void ConfirmingClickAfterTheWindow_IsDoubtfulAgain()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, SharedSlot, SecondSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        var late = Now + ChatPortraitTellGuard.ConfirmationWindow + TimeSpan.FromSeconds(1);
        guard.Click(Observer, SharedSlot, SecondSpecialist, late).Should().Be(ChatPortraitClick.Ambiguous);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, late).Should().BeTrue();
    }

    [Test]
    public void SlotChangesHandsBetweenTheTwoClicks_DoesNotConfirmTheNewHolder()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now).Should().Be(ChatPortraitClick.FormerHolder);

        // The second character logs out and a third takes the slot before the confirming click.
        guard.Click(Observer, SharedSlot, "character-third", Now.AddSeconds(5)).Should().Be(ChatPortraitClick.FormerHolder);
        guard.ConsumeRefusedTell(Observer, SharedSlot, "character-third", Now.AddSeconds(10)).Should().BeTrue();
    }

    [Test]
    public void SlotChangesHandsAfterAConfirmedClick_RefusesTheTell()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(5)).Should().Be(ChatPortraitClick.Confirmed);

        guard.ConsumeRefusedTell(Observer, SharedSlot, "character-third", Now.AddSeconds(10)).Should().BeTrue();
    }

    [Test]
    public void ClickOnAnotherPortraitBetween_CancelsTheConfirmation()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.RecordLine(Observer, OtherSlot, "character-chatty");
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.Click(Observer, OtherSlot, "character-chatty", Now.AddSeconds(3)).Should().Be(ChatPortraitClick.CurrentHolder);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(6)).Should().Be(ChatPortraitClick.FormerHolder);
    }

    [Test]
    public void RefusedTellBetweenTheTwoClicks_StillLetsTheSecondClickConfirm()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(20)).Should().BeTrue();

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(30)).Should().Be(ChatPortraitClick.Confirmed);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(50)).Should().BeFalse();
    }

    [Test]
    public void EachPortraitTell_NeedsItsOwnConfirmation()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(5)).Should().Be(ChatPortraitClick.Confirmed);
        guard.ConsumeRefusedTell(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(10)).Should().BeFalse();

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(15)).Should().Be(ChatPortraitClick.FormerHolder);
    }

    [Test]
    public void Classify_DoesNotCountAsAClick()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.Classify(Observer, SharedSlot, SecondSpecialist).Should().Be(ChatPortraitClick.FormerHolder);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(5)).Should().Be(ChatPortraitClick.Confirmed);
    }

    [Test]
    public void ForgottenObserver_LosesAPendingConfirmation()
    {
        var guard = new ChatPortraitTellGuard(Lifetime);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);
        guard.Click(Observer, SharedSlot, SecondSpecialist, Now);

        guard.ForgetObserver(Observer);
        guard.RecordLine(Observer, SharedSlot, FirstSpecialist);

        guard.Click(Observer, SharedSlot, SecondSpecialist, Now.AddSeconds(5)).Should().Be(ChatPortraitClick.FormerHolder);
    }
}
