using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class EquipmentUnequipTests
{
    [Test]
    public void IndependentOwnersCanHandleTheirItemsWithoutBlockingUnrelatedEquipment()
    {
        const string first = "test:first-equipment-owner";
        const string second = "test:second-equipment-owner";
        const uint actor = uint.MaxValue;
        try
        {
            EquipmentUnequip.RegisterGuard(first, (creature, item) => creature == actor && item == 1);
            EquipmentUnequip.RegisterGuard(second, (creature, item) => creature == actor && item == 2);
            EquipmentUnequip.TryHandle(actor, 1).Should().BeTrue();
            EquipmentUnequip.TryHandle(actor, 2).Should().BeTrue();
            EquipmentUnequip.TryHandle(actor, 3).Should().BeFalse();
            EquipmentUnequip.TryHandle(actor - 1, 1).Should().BeFalse();

            EquipmentUnequip.RegisterGuard(first, (_, _) => false);
            EquipmentUnequip.TryHandle(actor, 1).Should().BeFalse("cache registration replaces the same owner's previous guard");
            EquipmentUnequip.TryHandle(actor, 2).Should().BeTrue("replacing one owner cannot remove another owner's guard");
        }
        finally
        {
            EquipmentUnequip.RegisterGuard(first, (_, _) => false);
            EquipmentUnequip.RegisterGuard(second, (_, _) => false);
        }
    }
}
