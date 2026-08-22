// SPDX-License-Identifier: MIT

using System.Numerics;

namespace SWLOR.NWN.Formats.Mdl;

public sealed class MdlModel
{
    public string Name { get; set; } = string.Empty;

    public string SuperModel { get; set; } = string.Empty;

    public byte ModelType { get; set; }

    public Vector3 BoundsMinimum { get; set; }

    public Vector3 BoundsMaximum { get; set; }

    public float Radius { get; set; }

    public float Scale { get; set; } = 1f;

    public MdlNode? GeometryRoot { get; set; }

    public List<MdlAnimation> Animations { get; } = new();

    public IEnumerable<MdlTrimeshNode> GetMeshNodes()
    {
        if (GeometryRoot == null)
            yield break;

        var pending = new Stack<MdlNode>();
        pending.Push(GeometryRoot);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is MdlTrimeshNode mesh)
                yield return mesh;

            for (var index = node.Children.Count - 1; index >= 0; index--)
                pending.Push(node.Children[index]);
        }
    }
}
