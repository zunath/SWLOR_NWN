// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Mdl;

public sealed class MdlEmitterNode : MdlNode
{
    public float DeadSpace { get; set; }

    public float BlastRadius { get; set; }

    public float BlastLength { get; set; }

    public int XGrid { get; set; } = 1;

    public int YGrid { get; set; } = 1;

    public string Update { get; set; } = string.Empty;

    public string RenderMode { get; set; } = string.Empty;

    public string Blend { get; set; } = string.Empty;

    public string Texture { get; set; } = string.Empty;

    public string Chunk { get; set; } = string.Empty;

    public bool TextureIsTwoSided { get; set; }

    public bool Loop { get; set; }

    public ushort RenderOrder { get; set; }
}
