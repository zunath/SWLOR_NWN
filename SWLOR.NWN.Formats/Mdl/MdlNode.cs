// SPDX-License-Identifier: MIT

using System.Numerics;

namespace SWLOR.NWN.Formats.Mdl;

public class MdlNode
{
    public string Name { get; set; } = string.Empty;

    public MdlNode? Parent { get; set; }

    public List<MdlNode> Children { get; } = new();

    public Vector3 Position { get; set; }

    public Quaternion Orientation { get; set; } = Quaternion.Identity;

    public float Scale { get; set; } = 1f;

    public float[] PositionTimes { get; set; } = Array.Empty<float>();

    public Vector3[] PositionValues { get; set; } = Array.Empty<Vector3>();

    public float[] OrientationTimes { get; set; } = Array.Empty<float>();

    public Quaternion[] OrientationValues { get; set; } = Array.Empty<Quaternion>();

    public float[] ScaleTimes { get; set; } = Array.Empty<float>();

    public float[] ScaleValues { get; set; } = Array.Empty<float>();
}
