using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Rasterizes a small thumbnail of a <see cref="RenderModel"/> in software, so the palette can
    /// show what a blueprint looks like instead of a letter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately not OpenGL. Thumbnails are wanted for thousands of blueprints, in the background,
    /// while the user is doing something else - and the GL context in this app belongs to a live
    /// viewport control on the UI thread. A CPU rasterizer runs anywhere, on any thread, needs no
    /// context, and is directly testable; at 128px the quality difference against a GL pass is not
    /// what decides whether a builder recognises a crate.
    /// </para>
    /// <para>
    /// Painter's algorithm: triangles are sorted back to front and filled flat, lit by a single fixed
    /// key light. No z-buffer, because sorting per triangle is enough at this size and costs less
    /// memory per concurrent render.
    /// </para>
    /// </remarks>
    public static class ThumbnailRenderer
    {
        /// <summary>Bytes per pixel in the BGRA output.</summary>
        public const int BytesPerPixel = 4;

        /// <summary>
        /// The view direction. A three-quarter view from above reads as an object rather than a
        /// silhouette, and matches how the area viewport presents the same models.
        /// </summary>
        private static readonly Vector3 ViewDirection = Vector3.Normalize(new Vector3(-0.7f, -1f, 0.75f));

        private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.4f, -0.6f, 1f));

        /// <summary>
        /// Renders to a BGRA pixel buffer of <paramref name="size"/> squared, or null when the model has
        /// no triangles to draw - callers fall back to their placeholder rather than showing an empty box.
        /// </summary>
        public static byte[]? Render(RenderModel? model, int size, ThumbnailPalette? palette = null)
        {
            if (model == null || size <= 0)
                return null;

            palette ??= ThumbnailPalette.Default;

            var triangles = CollectTriangles(model);
            if (triangles.Count == 0)
                return null;

            var pixels = new byte[size * size * BytesPerPixel];
            FillBackground(pixels, palette.Background);

            var view = BuildViewBasis();
            var projected = Project(triangles, view, size, out var any);
            if (!any)
                return null;

            // Back to front: the far side of a model must not paint over its near side.
            projected.Sort(static (left, right) => left.Depth.CompareTo(right.Depth));

            foreach (var triangle in projected)
                FillTriangle(pixels, size, triangle, palette);

            return pixels;
        }

        private static List<(Vector3 A, Vector3 B, Vector3 C)> CollectTriangles(RenderModel model)
        {
            var triangles = new List<(Vector3, Vector3, Vector3)>();

            foreach (var mesh in model.Meshes)
            {
                if (mesh.Indices.Length < 3 || mesh.Positions.Length < 9)
                    continue;

                for (var i = 0; i + 2 < mesh.Indices.Length; i += 3)
                {
                    if (!TryVertex(mesh, mesh.Indices[i], out var a) ||
                        !TryVertex(mesh, mesh.Indices[i + 1], out var b) ||
                        !TryVertex(mesh, mesh.Indices[i + 2], out var c))
                        continue;

                    triangles.Add((a, b, c));
                }
            }

            return triangles;
        }

        private static bool TryVertex(RenderMesh mesh, int index, out Vector3 vertex)
        {
            vertex = default;
            var offset = index * 3;
            if (index < 0 || offset + 2 >= mesh.Positions.Length)
                return false;

            var local = new Vector3(mesh.Positions[offset], mesh.Positions[offset + 1], mesh.Positions[offset + 2]);
            vertex = Vector3.Transform(local, mesh.Transform);
            return true;
        }

        /// <summary>An orthonormal basis looking along <see cref="ViewDirection"/>, Z-up.</summary>
        private static (Vector3 Right, Vector3 Up, Vector3 Forward) BuildViewBasis()
        {
            var forward = ViewDirection;
            var worldUp = new Vector3(0, 0, 1);
            var right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
            var up = Vector3.Cross(forward, right);
            return (right, up, forward);
        }

        private static List<ProjectedTriangle> Project(
            List<(Vector3 A, Vector3 B, Vector3 C)> triangles,
            (Vector3 Right, Vector3 Up, Vector3 Forward) view,
            int size,
            out bool any)
        {
            var minX = float.MaxValue; var maxX = float.MinValue;
            var minY = float.MaxValue; var maxY = float.MinValue;

            var viewSpace = new List<(Vector2 A, Vector2 B, Vector2 C, float Depth, Vector3 Normal)>(triangles.Count);

            foreach (var (a, b, c) in triangles)
            {
                var pa = ToView(a, view); var pb = ToView(b, view); var pc = ToView(c, view);
                var normal = Vector3.Cross(b - a, c - a);
                if (normal.LengthSquared() > 0)
                    normal = Vector3.Normalize(normal);

                var depth = (pa.Z + pb.Z + pc.Z) / 3f;
                viewSpace.Add((new Vector2(pa.X, pa.Y), new Vector2(pb.X, pb.Y), new Vector2(pc.X, pc.Y), depth, normal));

                minX = MathF.Min(minX, MathF.Min(pa.X, MathF.Min(pb.X, pc.X)));
                maxX = MathF.Max(maxX, MathF.Max(pa.X, MathF.Max(pb.X, pc.X)));
                minY = MathF.Min(minY, MathF.Min(pa.Y, MathF.Min(pb.Y, pc.Y)));
                maxY = MathF.Max(maxY, MathF.Max(pa.Y, MathF.Max(pb.Y, pc.Y)));
            }

            var width = maxX - minX;
            var height = maxY - minY;
            any = width > 1e-6f || height > 1e-6f;
            if (!any)
                return new List<ProjectedTriangle>();

            // Uniform scale with a margin, so a long corridor and a small crate both sit inside the tile
            // at their true aspect rather than being stretched to fill it.
            const float margin = 0.10f;
            var span = MathF.Max(width, height);
            var scale = size * (1f - margin * 2f) / span;
            var offsetX = size / 2f - (minX + width / 2f) * scale;
            var offsetY = size / 2f - (minY + height / 2f) * scale;

            var projected = new List<ProjectedTriangle>(viewSpace.Count);
            foreach (var (a, b, c, depth, normal) in viewSpace)
            {
                projected.Add(new ProjectedTriangle
                {
                    // Y is flipped: model space is Z-up/Y-forward, bitmaps run top-down.
                    A = new Vector2(a.X * scale + offsetX, size - (a.Y * scale + offsetY)),
                    B = new Vector2(b.X * scale + offsetX, size - (b.Y * scale + offsetY)),
                    C = new Vector2(c.X * scale + offsetX, size - (c.Y * scale + offsetY)),
                    Depth = depth,
                    Shade = Math.Clamp(Vector3.Dot(normal, LightDirection), 0f, 1f)
                });
            }

            return projected;
        }

        private static Vector3 ToView(Vector3 point, (Vector3 Right, Vector3 Up, Vector3 Forward) view) =>
            new(Vector3.Dot(point, view.Right), Vector3.Dot(point, view.Up), Vector3.Dot(point, view.Forward));

        private static void FillBackground(byte[] pixels, uint color)
        {
            for (var i = 0; i < pixels.Length; i += BytesPerPixel)
            {
                pixels[i] = (byte)(color & 0xFF);
                pixels[i + 1] = (byte)((color >> 8) & 0xFF);
                pixels[i + 2] = (byte)((color >> 16) & 0xFF);
                pixels[i + 3] = (byte)((color >> 24) & 0xFF);
            }
        }

        private static void FillTriangle(byte[] pixels, int size, ProjectedTriangle triangle, ThumbnailPalette palette)
        {
            var minX = Math.Max(0, (int)MathF.Floor(MathF.Min(triangle.A.X, MathF.Min(triangle.B.X, triangle.C.X))));
            var maxX = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(triangle.A.X, MathF.Max(triangle.B.X, triangle.C.X))));
            var minY = Math.Max(0, (int)MathF.Floor(MathF.Min(triangle.A.Y, MathF.Min(triangle.B.Y, triangle.C.Y))));
            var maxY = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(triangle.A.Y, MathF.Max(triangle.B.Y, triangle.C.Y))));

            var area = EdgeFunction(triangle.A, triangle.B, triangle.C);
            if (MathF.Abs(area) < 1e-6f)
                return;

            var shade = palette.Ambient + (1f - palette.Ambient) * triangle.Shade;
            var b = (byte)Math.Clamp(palette.BaseB * shade, 0, 255);
            var g = (byte)Math.Clamp(palette.BaseG * shade, 0, 255);
            var r = (byte)Math.Clamp(palette.BaseR * shade, 0, 255);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    var w0 = EdgeFunction(triangle.B, triangle.C, point) / area;
                    var w1 = EdgeFunction(triangle.C, triangle.A, point) / area;
                    var w2 = EdgeFunction(triangle.A, triangle.B, point) / area;

                    if (w0 < 0 || w1 < 0 || w2 < 0)
                        continue;

                    var offset = (y * size + x) * BytesPerPixel;
                    pixels[offset] = b;
                    pixels[offset + 1] = g;
                    pixels[offset + 2] = r;
                    pixels[offset + 3] = 255;
                }
            }
        }

        private static float EdgeFunction(Vector2 a, Vector2 b, Vector2 c) =>
            (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);

        private struct ProjectedTriangle
        {
            public Vector2 A;
            public Vector2 B;
            public Vector2 C;
            public float Depth;
            public float Shade;
        }
    }
}
