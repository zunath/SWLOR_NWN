using System.Numerics;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Tests.Service;

public class TelegraphTests
{
    private const int PackedRotationShift = 21;
    private const int PackedRotationMask = 0x3ff;

    [Test]
    public void PackTelegraphData_LineKeepsGameplayRotation()
    {
        var packed = InvokePackTelegraphData(TelegraphType.Line, 0f);

        ExtractPackedRotation(packed).Should().Be(0);
    }

    [Test]
    public void PackTelegraphData_ConeKeepsGameplayRotation()
    {
        var packed = InvokePackTelegraphData(TelegraphType.Cone, 0f);

        ExtractPackedRotation(packed).Should().Be(0);
    }

    private static int InvokePackTelegraphData(TelegraphType type, float rotation)
    {
        var method = typeof(Telegraph)
            .GetMethod("PackTelegraphData", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (int)method.Invoke(
            null,
            new object[]
            {
                type,
                TelegraphColorType.Self,
                new Vector2(8f, 2.5f),
                rotation
            })!;
    }

    private static int ExtractPackedRotation(int packed)
    {
        return (int)(((uint)packed >> PackedRotationShift) & PackedRotationMask);
    }
}
