// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Common;

/// <summary>
/// SWLOR's filename prefix for interrupted new-area markers. The toolset writes it and the module
/// packer refuses to pack while one exists, so both must use the same value; it predates the shared
/// default and stays fixed so markers left by earlier builds are still recognized.
/// </summary>
public static class SwlorAreaCreationMarker
{
    public const string Prefix = ".swlor-toolset-new-area-";
}
