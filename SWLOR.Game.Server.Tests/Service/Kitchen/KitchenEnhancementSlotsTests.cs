using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>Covers the enhancement picks made before cooking: empty slots must read as empty, and the request keeps slot numbers.</summary>
public class KitchenEnhancementSlotsTests
{
    [Test]
    public void NewSlotsAreAllEmpty_AndSendTheEngineTheInvalidObjectNotZero()
    {
        var slots = new KitchenEnhancementSlots(3);

        slots.Items.Should().Equal(OBJECT_INVALID, OBJECT_INVALID, OBJECT_INVALID);
        Enumerable.Range(0, 3).Should().OnlyContain(slot => !slots.IsFilled(slot));
        slots.FilledSlotNumbers.Should().BeEmpty();
        slots.TotalProgressPenalty.Should().Be(0);
        slots.Contains(OBJECT_INVALID).Should().BeFalse();
    }

    [Test]
    public void FillingASlot_KeepsItsNumberEvenWhenAnEarlierSlotIsEmpty()
    {
        var slots = new KitchenEnhancementSlots(2);

        slots.Fill(1, 42u, "Cooking Enhancement - HP I", 20);

        slots.IsFilled(0).Should().BeFalse();
        slots.IsFilled(1).Should().BeTrue();
        slots.NameAt(1).Should().Be("Cooking Enhancement - HP I");
        slots.Items.Should().Equal(OBJECT_INVALID, 42u);
        slots.FilledSlotNumbers.Should().Equal(2);
        slots.TotalProgressPenalty.Should().Be(20);
        slots.Contains(42u).Should().BeTrue();
    }

    [Test]
    public void ClearingASlot_TakesBackItsItemNameAndPenalty()
    {
        var slots = new KitchenEnhancementSlots(2);
        slots.Fill(0, 7u, "A", 10);
        slots.Fill(1, 8u, "B", 30);

        slots.Clear(0);

        slots.FilledSlotNumbers.Should().Equal(2);
        slots.TotalProgressPenalty.Should().Be(30);
        slots.Contains(7u).Should().BeFalse();

        slots.ClearAll();

        slots.Items.Should().OnlyContain(item => item == OBJECT_INVALID);
        slots.TotalProgressPenalty.Should().Be(0);
    }

    [Test]
    public void ARequestBuiltFromTheSlots_KeepsGapsAsInvalidObjects()
    {
        var slots = new KitchenEnhancementSlots(3);
        slots.Fill(2, 99u, "C", 0);

        var request = new CraftSessionStartRequest(SWLOR.Game.Server.Service.CraftService.RecipeType.MynockMeatBall, OBJECT_INVALID, slots.Items);

        request.EnhancementItems.Should().Equal(OBJECT_INVALID, OBJECT_INVALID, 99u);
    }
}
