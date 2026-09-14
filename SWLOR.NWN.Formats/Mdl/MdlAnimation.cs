// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Mdl;

public sealed class MdlAnimation
{
    public string Name { get; set; } = string.Empty;

    public float Length { get; set; }

    public float TransitionTime { get; set; }

    public MdlNode? GeometryRoot { get; set; }
}
