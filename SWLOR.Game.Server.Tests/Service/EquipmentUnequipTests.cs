using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public class EquipmentUnequipTests
{
    private Dictionary<string, Func<uint, uint, bool>> _guards;
    private KeyValuePair<string, Func<uint, uint, bool>>[] _original;

    [SetUp]
    public void IsolateRegisteredGuards()
    {
        _guards = (Dictionary<string, Func<uint, uint, bool>>)typeof(EquipmentUnequip)
            .GetField("Guards", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        _original = _guards.ToArray();
        _guards.Clear();
    }

    [TearDown]
    public void RestoreRegisteredGuards()
    {
        _guards.Clear();
        foreach (var (key, guard) in _original) _guards.Add(key, guard);
    }

    [Test]
    public void RegisteringTheSameOwnerReplacesItsGuardWithoutDuplicateDispatch()
    {
        var oldCalls = 0;
        var newCalls = 0;
        EquipmentUnequip.RegisterGuard("pair", (_, _) => { oldCalls++; return true; });
        Func<uint, uint, bool> replacement = (actor, item) =>
        {
            actor.Should().Be(42);
            item.Should().Be(84);
            newCalls++;
            return true;
        };
        EquipmentUnequip.RegisterGuard("pair", replacement);
        EquipmentUnequip.RegisterGuard("pair", replacement);

        EquipmentUnequip.TryHandle(42, 84).Should().BeTrue();
        oldCalls.Should().Be(0);
        newCalls.Should().Be(1);
    }

    [Test]
    public void UnmatchedRequestsFallThroughAndMatchingOwnersStopDispatch()
    {
        var secondCalls = 0;
        EquipmentUnequip.TryHandle(42, 84).Should().BeFalse();
        EquipmentUnequip.RegisterGuard("first", (_, item) => item == 84);
        EquipmentUnequip.RegisterGuard("second", (_, _) => { secondCalls++; return false; });

        EquipmentUnequip.TryHandle(42, 85).Should().BeFalse();
        secondCalls.Should().Be(1);
        EquipmentUnequip.TryHandle(42, 84).Should().BeTrue();
        secondCalls.Should().Be(1);
    }
}
