using System;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

namespace SWLOR.Game.Server.Tests.Feature;

public class TintMapShaderColorTests
{
    [Test]
    public void EveryRgbValueSurvivesTheScalarFloatTransport()
    {
        // Exhaustive byte coverage, including the precision change at R=128.
        for (var packed = 0; packed <= 0xFFFFFF; packed++)
        {
            var color = new TintMapColor((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed);
            var value = TintMapShaderColor.Encode(color);
            var decoded = value < 2f ? (value - 1f) * 8388608f
                : (value - 2f) * 4194304f + 8388608f;
            if (value < 1f || value >= 4f || decoded != packed)
                Assert.Fail($"RGB {packed:X6} became {decoded} through {value:R}.");
        }
    }

    [Test]
    public void NativePaletteCoordinatesCannotBeMistakenForCustomRgb()
    {
        foreach (var layer in Enum.GetValues<TintMapLayerType>())
        for (var color = 0; color < TintMapMaterialRegistry.PaletteColorCount; color++)
            Assert.That(TintMapMaterialRegistry.GetPaletteCoordinate(layer, color), Is.InRange(0f, 0.999f));
    }
}
