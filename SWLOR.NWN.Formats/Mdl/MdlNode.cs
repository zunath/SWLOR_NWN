// SPDX-License-Identifier: MIT

using System.Numerics;

namespace SWLOR.NWN.Formats.Mdl;

public class MdlNode
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether a renderer may replace this node's authored local transform with an animation pose
    /// found by <see cref="Name"/>. This is runtime composition metadata and is not serialized.
    /// </summary>
    /// <remarks>
    /// Segmented body-part models are attached beneath an already-posed skeleton bone. Their mesh
    /// nodes frequently repeat that bone's name, so posing both nodes would apply the same transform
    /// twice and pull equipment away from the body.
    /// </remarks>
    public bool ReceivesNamedAnimationPose { get; set; } = true;

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
