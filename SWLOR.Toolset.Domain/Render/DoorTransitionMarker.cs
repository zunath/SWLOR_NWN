using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Fallback editor shape for an invisible area-transition door whose MDL cannot be resolved.
    /// Door models span local +X along the wall, with their stored position at the centre of the
    /// doorway; the standard Aurora transition plane is two metres wide and three metres high.
    /// </summary>
    public static class DoorTransitionMarker
    {
        public const float HalfWidth = 1f;
        public const float HalfDepth = 0.05f;
        public const float HalfHeight = 1.5f;

        public static readonly Vector3 LocalMinimum =
            new(-HalfWidth, -HalfDepth, -HalfHeight);

        public static readonly Vector3 LocalMaximum =
            new(HalfWidth, HalfDepth, HalfHeight);

        /// <summary>
        /// Creates the fixed doorway box used when a transition MDL has no usable authored
        /// geometry. The software thumbnail renderer consumes the same dimensions as viewport
        /// drawing and picking, so every editor surface shows the same fallback silhouette.
        /// </summary>
        public static RenderModel CreateFallbackModel()
        {
            var min = LocalMinimum;
            var max = LocalMaximum;
            return new RenderModel
            {
                Name = "door_transition_fallback",
                IsDoorTransitionGeometry = true,
                Meshes =
                [
                    new RenderMesh
                    {
                        NodeName = "door_transition_fallback",
                        TextureName = string.Empty,
                        DiffuseColor = new Vector3(0.52f, 0.52f, 0.82f),
                        Positions =
                        [
                            min.X, min.Y, min.Z,
                            max.X, min.Y, min.Z,
                            max.X, max.Y, min.Z,
                            min.X, max.Y, min.Z,
                            min.X, min.Y, max.Z,
                            max.X, min.Y, max.Z,
                            max.X, max.Y, max.Z,
                            min.X, max.Y, max.Z
                        ],
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices =
                        [
                            3, 2, 1, 3, 1, 0,
                            4, 5, 6, 4, 6, 7,
                            0, 1, 5, 0, 5, 4,
                            2, 3, 7, 2, 7, 6,
                            3, 0, 4, 3, 4, 7,
                            1, 2, 6, 1, 6, 5
                        ],
                        Transform = Matrix4x4.Identity
                    }
                ]
            };
        }
    }
}
