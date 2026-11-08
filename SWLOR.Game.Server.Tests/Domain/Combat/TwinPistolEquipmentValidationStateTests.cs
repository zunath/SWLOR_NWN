using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The revalidation guard and the failed-return memory that <c>Feature.TwinPistolEquipmentValidation</c> keeps in the
/// equipment instance.
/// </summary>
[TestFixture]
public class TwinPistolEquipmentValidationStateTests
{
    private static TwinPistolEquipmentService Create() => new(null!, FacadeForwarder.LazyFor<ISkillService>());

    [Test]
    public void Revalidation_DoesNotNestAndEndsPerCreature()
    {
        var service = Create();
        service.IsRevalidating(1).Should().BeFalse();

        service.TryBeginRevalidation(1).Should().BeTrue();
        service.TryBeginRevalidation(1).Should().BeFalse();
        service.IsRevalidating(1).Should().BeTrue();
        service.IsRevalidating(2).Should().BeFalse();

        service.EndRevalidation(1);

        service.IsRevalidating(1).Should().BeFalse();
        service.TryBeginRevalidation(1).Should().BeTrue();
    }

    [Test]
    public void FailedReturn_IsRememberedPerCreatureUntilCleared()
    {
        var service = Create();
        service.TryGetFailedReturn(1, out _, out _).Should().BeFalse();

        service.SetFailedReturn(1, 50, InventorySlot.RightHand);
        service.SetFailedReturn(2, 60, InventorySlot.LeftHand);

        service.TryGetFailedReturn(1, out var item, out var slot).Should().BeTrue();
        item.Should().Be(50u);
        slot.Should().Be(InventorySlot.RightHand);

        service.ClearFailedReturn(1);
        service.TryGetFailedReturn(1, out _, out _).Should().BeFalse();
        service.TryGetFailedReturn(2, out item, out slot).Should().BeTrue();
        item.Should().Be(60u);
        slot.Should().Be(InventorySlot.LeftHand);
        service.ClearFailedReturn(1);
    }

    [Test]
    public void SetFailedReturn_ReplacesAnEarlierFailure()
    {
        var service = Create();
        service.SetFailedReturn(1, 50, InventorySlot.RightHand);
        service.SetFailedReturn(1, 70, InventorySlot.LeftHand);

        service.TryGetFailedReturn(1, out var item, out var slot).Should().BeTrue();
        item.Should().Be(70u);
        slot.Should().Be(InventorySlot.LeftHand);
    }
}
