using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Native;

public class PlayerSlotRotationTests
{
    private static bool[] Slots(int count, params int[] inUse)
    {
        var slots = new bool[count];
        foreach (var slot in inUse)
            slots[slot] = true;
        return slots;
    }

    // Logs in a connection the way PlayerSlotAssignment does: observe, choose, then record the engine's grant.
    private static int Login(PlayerSlotRotation rotation, bool[] engine)
    {
        rotation.Observe(engine);
        var slot = rotation.ChooseSlot();
        engine[slot] = true;
        rotation.MarkInUse(slot);
        return slot;
    }

    [Test]
    public void SlotsThatWereNeverUsed_AreTakenInOrder()
    {
        var rotation = new PlayerSlotRotation(5);
        var engine = Slots(5);

        Login(rotation, engine).Should().Be(0);
        Login(rotation, engine).Should().Be(1);
        Login(rotation, engine).Should().Be(2);
    }

    [Test]
    public void AFreedSlot_WaitsBehindSlotsThatWereNeverUsed()
    {
        var rotation = new PlayerSlotRotation(5);
        var engine = Slots(5);
        Login(rotation, engine);
        Login(rotation, engine);

        engine[0] = false;

        Login(rotation, engine).Should().Be(2);
        Login(rotation, engine).Should().Be(3);
        Login(rotation, engine).Should().Be(4);
        Login(rotation, engine).Should().Be(0);
    }

    [Test]
    public void OnceEverySlotHasBeenUsed_TheSlotFreedLongestAgoComesFirst()
    {
        var rotation = new PlayerSlotRotation(3);
        var engine = Slots(3);
        Login(rotation, engine);
        Login(rotation, engine);
        Login(rotation, engine);

        engine[2] = false;
        Login(rotation, engine).Should().Be(2);
        engine[2] = false;
        engine[0] = false;
        rotation.Observe(engine);
        engine[1] = false;

        // Slots 0 and 2 were seen free before slot 1, so slot 1 waits; 0 and 2 tie and fall back to order.
        Login(rotation, engine).Should().Be(0);
        Login(rotation, engine).Should().Be(2);
        Login(rotation, engine).Should().Be(1);
    }

    [Test]
    public void ALogoutAndLoginAtOnce_DoesNotHandTheNewcomerTheLeaversSlot()
    {
        var rotation = new PlayerSlotRotation(255);
        var engine = Slots(255);
        for (var player = 0; player < 40; player++)
            Login(rotation, engine);

        engine[7] = false;

        Login(rotation, engine).Should().Be(40);
    }

    [Test]
    public void EverySlotInUse_ChoosesNone()
    {
        var rotation = new PlayerSlotRotation(2);
        rotation.Observe(Slots(2, 0, 1));

        rotation.ChooseSlot().Should().Be(-1);
    }

    [Test]
    public void AGrantedSlot_CountsAsInUseUntilSeenFree()
    {
        var rotation = new PlayerSlotRotation(3);
        rotation.Observe(Slots(3));
        rotation.MarkInUse(0);

        rotation.ChooseSlot().Should().Be(1);
    }

    [Test]
    public void AnObservationOfTheWrongSize_IsRejected()
    {
        var rotation = new PlayerSlotRotation(3);

        rotation.Invoking(r => r.Observe(Slots(4))).Should().Throw<ArgumentException>();
    }
}
