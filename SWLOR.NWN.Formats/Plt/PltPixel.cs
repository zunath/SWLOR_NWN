// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Plt;

/// <summary>
/// One PLT pixel's palette intensity and material layer.
/// </summary>
public readonly record struct PltPixel(byte Intensity, byte Layer);
