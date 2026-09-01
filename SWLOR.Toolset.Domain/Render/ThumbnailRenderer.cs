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
    /// Painter's algorithm: triangles are sorted back to front and filled, lit by a single fixed key
    /// light. No z-buffer, because sorting per triangle is enough at this size and costs less memory per
    /// concurrent render.
    /// </para>
    /// <para>
    /// Meshes are textured when the caller supplies a texture resolver, sampling the diffuse map at each
    /// covered pixel's interpolated UV; a mesh whose texture cannot be resolved falls back to the
    /// palette's flat tone, so a partly-resolvable model still renders whole. Texels the artwork marks
    /// transparent are skipped, which is what keeps foliage and grating meshes from reading as solid
    /// slabs - the one place the missing z-buffer would otherwise show.
    /// </para>
    /// </remarks>
    public static class ThumbnailRenderer
    {
        /// <summary>Bytes per pixel in the BGRA output.</summary>
        public const int BytesPerPixel = 4;

        /// <summary>
        /// Triangles collected before the rest of a model is ignored.
        /// </summary>
        /// <remarks>
        /// A 128px tile cannot show detail past a few thousand triangles, and thumbnails are rendered in
        /// parallel across a whole module - so the cap is really about bounding memory on the handful of
        /// enormous models in the corpus, not about drawing quality. Framing uses whatever was collected,
        /// which for a truncated model is the first meshes in file order: still a recognisable silhouette.
        /// </remarks>
        public const int MaxTriangles = 120_000;

        /// <summary>
        /// Where the camera sits, as a direction from the model toward it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// NOT the direction the camera looks along - the opposite. <see cref="Project"/> takes
        /// <c>Depth = dot(point, Forward)</c> and paints in ascending order, so the smallest value has to
        /// be the farthest point, which only holds if this vector points back at the viewer. Reading it
        /// as a look direction inverts both axes: a positive Z reads as "above" but puts the camera
        /// underneath, and the model is drawn from below.
        /// </para>
        /// <para>
        /// +Y is the model's front. Verified against the corpus: a creature rendered from +Y shows its
        /// face and the fronts of its feet, from -Y its shoulder blades and heels.
        /// </para>
        /// <para>
        /// +Z is above, tilted <see cref="TiltOffVerticalDegrees"/> off straight-down. Steeper and a
        /// creature becomes scalp and shoulders with no face. The height is the tangent of the tilt
        /// against the bearing's own length, so the angle stays what it says it is if the bearing is
        /// ever changed.
        /// </para>
        /// </remarks>
        private const float TiltOffVerticalDegrees = 35f;

        private static readonly Vector3 ToCameraDirection = Vector3.Normalize(new Vector3(
            -0.7f,
            1f,
            MathF.Sqrt(0.7f * 0.7f + 1f) * MathF.Tan((90f - TiltOffVerticalDegrees) * MathF.PI / 180f)));

        /// <summary>
        /// Keyed to the same side as the camera, so the faces being looked at are the faces being lit.
        /// Moving the camera without moving this leaves every model in its own shadow.
        /// </summary>
        private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.4f, 0.6f, 1f));

        /// <summary>
        /// Renders to a BGRA pixel buffer of <paramref name="size"/> squared, or null when the model has
        /// no triangles to draw - callers fall back to their placeholder rather than showing an empty box.
        /// </summary>
        /// <param name="resolveTexture">
        /// Resolves a mesh's texture name to decoded pixels. Optional: without it every mesh renders in
        /// the palette's flat tone, which is how the pipeline behaves when no resource index is loaded.
        /// The callback is invoked at most once per distinct texture per render.
        /// </param>
        /// <param name="resolveLayeredTexture">
        /// Resolves a texture with the palette carried by its individual mesh. When supplied it
        /// takes precedence over <paramref name="resolveTexture"/> and lets equipped garments keep
        /// dyes that differ from the creature's chest armor.
        /// </param>
        /// <param name="resolveMeshTexture">
        /// Resolves the complete mesh, including its explicit material binding. When supplied it
        /// takes precedence over the name-only resolvers so a bitmap is not mistaken for a
        /// same-named material.
        /// </param>
        /// <param name="renderDoorTransitionFallback">
        /// Draws the fixed editor doorway when transition metadata exists but the authored model is
        /// null, has no triangles, or has no triangles that survive projection.
        /// </param>
        public static byte[]? Render(
            RenderModel? model,
            int size,
            ThumbnailPalette? palette = null,
            Func<string, TextureImage?>? resolveTexture = null,
            Func<string, IReadOnlyDictionary<int, int>?, TextureImage?>? resolveLayeredTexture = null,
            Func<RenderMesh, TextureImage?>? resolveMeshTexture = null,
            bool renderDoorTransitionFallback = false)
        {
            if (size <= 0)
                return null;

            palette ??= ThumbnailPalette.Default;

            var isDoorTransition = renderDoorTransitionFallback || model?.IsDoorTransitionGeometry == true;
            var usingDoorTransitionFallback = false;
            var triangles = model == null
                ? new List<SourceTriangle>()
                : CollectTriangles(model, resolveTexture, resolveLayeredTexture, resolveMeshTexture);
            if (triangles.Count == 0 && isDoorTransition)
            {
                triangles = CollectTriangles(
                    DoorTransitionMarker.CreateFallbackModel(),
                    resolveTexture: null,
                    resolveLayeredTexture: null,
                    resolveMeshTexture: null);
                usingDoorTransitionFallback = true;
            }

            if (triangles.Count == 0)
                return null;

            var pixels = new byte[size * size * BytesPerPixel];
            FillBackground(pixels, palette.Background);

            var view = BuildViewBasis();
            var projected = Project(triangles, view, size, out var any);
            if (!any && isDoorTransition && !usingDoorTransitionFallback)
            {
                triangles = CollectTriangles(
                    DoorTransitionMarker.CreateFallbackModel(),
                    resolveTexture: null,
                    resolveLayeredTexture: null,
                    resolveMeshTexture: null);
                projected = Project(triangles, view, size, out any);
            }

            if (!any)
                return null;

            // Back to front: the far side of a model must not paint over its near side.
            projected.Sort(static (left, right) => left.Depth.CompareTo(right.Depth));

            var painted = false;
            foreach (var triangle in projected)
                painted |= FillTriangle(pixels, size, triangle, palette);

            if (!painted && isDoorTransition && !usingDoorTransitionFallback)
            {
                triangles = CollectTriangles(
                    DoorTransitionMarker.CreateFallbackModel(),
                    resolveTexture: null,
                    resolveLayeredTexture: null,
                    resolveMeshTexture: null);
                projected = Project(triangles, view, size, out any);
                if (!any)
                    return null;

                FillBackground(pixels, palette.Background);
                projected.Sort(static (left, right) => left.Depth.CompareTo(right.Depth));
                foreach (var triangle in projected)
                    painted |= FillTriangle(pixels, size, triangle, palette);

                if (!painted)
                    return null;
            }

            return pixels;
        }

        private static List<SourceTriangle> CollectTriangles(
            RenderModel model,
            Func<string, TextureImage?>? resolveTexture,
            Func<string, IReadOnlyDictionary<int, int>?, TextureImage?>? resolveLayeredTexture,
            Func<RenderMesh, TextureImage?>? resolveMeshTexture)
        {
            var triangles = new List<SourceTriangle>();

            // Meshes routinely share a texture, and decoding one is far dearer than sampling it.
            var decoded = new Dictionary<string, TextureImage?>(StringComparer.OrdinalIgnoreCase);

            foreach (var mesh in model.Meshes)
            {
                if (mesh.Indices.Length < 3 || mesh.Positions.Length < 9)
                    continue;

                var texture = ResolveMeshTexture(
                    mesh, resolveTexture, resolveLayeredTexture, resolveMeshTexture, decoded);
                var hasUvs = texture != null && mesh.TexCoords.Length * 3 >= mesh.Positions.Length * 2;

                for (var i = 0; i + 2 < mesh.Indices.Length; i += 3)
                {
                    if (triangles.Count >= MaxTriangles)
                        return triangles;

                    var first = mesh.Indices[i];
                    var second = mesh.Indices[i + 1];
                    var third = mesh.Indices[i + 2];

                    if (!TryVertex(mesh, first, out var a) ||
                        !TryVertex(mesh, second, out var b) ||
                        !TryVertex(mesh, third, out var c))
                        continue;

                    triangles.Add(new SourceTriangle
                    {
                        A = a,
                        B = b,
                        C = c,
                        Texture = hasUvs ? texture : null,
                        Tint = mesh.DiffuseColor,
                        UvA = hasUvs ? Uv(mesh, first) : Vector2.Zero,
                        UvB = hasUvs ? Uv(mesh, second) : Vector2.Zero,
                        UvC = hasUvs ? Uv(mesh, third) : Vector2.Zero
                    });
                }
            }

            return triangles;
        }

        /// <summary>
        /// The mesh's decoded diffuse texture, or null when it has none, it cannot be resolved, or no
        /// resolver was supplied. Failures are remembered so an unresolvable name is only attempted once.
        /// </summary>
        private static TextureImage? ResolveMeshTexture(
            RenderMesh mesh,
            Func<string, TextureImage?>? resolveTexture,
            Func<string, IReadOnlyDictionary<int, int>?, TextureImage?>? resolveLayeredTexture,
            Func<RenderMesh, TextureImage?>? resolveMeshTexture,
            Dictionary<string, TextureImage?> decoded)
        {
            if (resolveTexture == null && resolveLayeredTexture == null && resolveMeshTexture == null)
                return null;

            if (resolveMeshTexture != null)
            {
                if (string.IsNullOrWhiteSpace(mesh.TextureName) &&
                    string.IsNullOrWhiteSpace(mesh.MaterialName))
                    return null;
            }
            else if (string.IsNullOrWhiteSpace(mesh.TextureName))
                return null;

            var usesMeshState = resolveLayeredTexture != null || resolveMeshTexture != null;
            var paletteKey = !usesMeshState || mesh.LayerColorIndices.Count == 0
                ? string.Empty
                : "|" + string.Join(
                    ",",
                    mesh.LayerColorIndices.OrderBy(pair => pair.Key)
                        .Select(pair => $"{pair.Key}:{pair.Value}"));
            var tintKey = !usesMeshState || mesh.TintMapOverrides.Count == 0
                ? string.Empty
                : "|" + string.Join(
                    ",",
                    mesh.TintMapOverrides.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => $"{pair.Key}:{pair.Value}"));
            var ownershipKey = resolveMeshTexture == null
                ? string.Empty
                : mesh.UsesItemTintOverrides ? "|item" : "|owner";
            var armorPartKey = resolveMeshTexture == null
                ? string.Empty
                : $"|part:{(int)mesh.ArmorPart}";
            var cacheKey = mesh.MaterialName + "|" + mesh.TextureName + paletteKey + tintKey +
                           ownershipKey + armorPartKey;
            if (decoded.TryGetValue(cacheKey, out var cached))
                return cached;

            TextureImage? texture = null;
            try
            {
                texture = resolveMeshTexture != null
                    ? resolveMeshTexture(mesh)
                    : resolveLayeredTexture != null
                        ? resolveLayeredTexture(mesh.TextureName, mesh.LayerColorIndices)
                        : resolveTexture!(mesh.TextureName);
            }
            catch (Exception)
            {
                // An unreadable texture just means this mesh renders in the flat tone.
            }

            if (texture != null && texture.Pixels.Length < texture.Width * texture.Height * 4)
                texture = null;

            decoded[cacheKey] = texture;
            return texture;
        }

        private static Vector2 Uv(RenderMesh mesh, int index)
        {
            var offset = index * 2;
            return offset + 1 < mesh.TexCoords.Length
                ? new Vector2(mesh.TexCoords[offset], mesh.TexCoords[offset + 1])
                : Vector2.Zero;
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

        /// <summary>An orthonormal basis about <see cref="ToCameraDirection"/>, Z-up.</summary>
        private static (Vector3 Right, Vector3 Up, Vector3 Forward) BuildViewBasis()
        {
            // Named Forward for the basis it forms, but it points at the camera, not away from it -
            // see the remarks on ToCameraDirection.
            var forward = ToCameraDirection;
            var worldUp = new Vector3(0, 0, 1);
            var right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
            var up = Vector3.Cross(forward, right);
            return (right, up, forward);
        }

        private static List<ProjectedTriangle> Project(
            List<SourceTriangle> triangles,
            (Vector3 Right, Vector3 Up, Vector3 Forward) view,
            int size,
            out bool any)
        {
            var minX = float.MaxValue; var maxX = float.MinValue;
            var minY = float.MaxValue; var maxY = float.MinValue;

            var viewSpace = new List<(Vector2 A, Vector2 B, Vector2 C, float Depth, Vector3 Normal, SourceTriangle Source)>(
                triangles.Count);

            foreach (var triangle in triangles)
            {
                var a = triangle.A; var b = triangle.B; var c = triangle.C;
                var pa = ToView(a, view); var pb = ToView(b, view); var pc = ToView(c, view);
                var normal = Vector3.Cross(b - a, c - a);
                if (normal.LengthSquared() > 0)
                    normal = Vector3.Normalize(normal);

                var depth = (pa.Z + pb.Z + pc.Z) / 3f;
                viewSpace.Add((
                    new Vector2(pa.X, pa.Y), new Vector2(pb.X, pb.Y), new Vector2(pc.X, pc.Y),
                    depth, normal, triangle));

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
            foreach (var (a, b, c, depth, normal, source) in viewSpace)
            {
                projected.Add(new ProjectedTriangle
                {
                    // Y is flipped: model space is Z-up/Y-forward, bitmaps run top-down.
                    A = new Vector2(a.X * scale + offsetX, size - (a.Y * scale + offsetY)),
                    B = new Vector2(b.X * scale + offsetX, size - (b.Y * scale + offsetY)),
                    C = new Vector2(c.X * scale + offsetX, size - (c.Y * scale + offsetY)),
                    Depth = depth,
                    Shade = Math.Clamp(Vector3.Dot(normal, LightDirection), 0f, 1f),
                    Texture = source.Texture,
                    Tint = source.Tint,
                    UvA = source.UvA,
                    UvB = source.UvB,
                    UvC = source.UvC
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

        private static bool FillTriangle(
            byte[] pixels,
            int size,
            ProjectedTriangle triangle,
            ThumbnailPalette palette)
        {
            var minX = Math.Max(0, (int)MathF.Floor(MathF.Min(triangle.A.X, MathF.Min(triangle.B.X, triangle.C.X))));
            var maxX = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(triangle.A.X, MathF.Max(triangle.B.X, triangle.C.X))));
            var minY = Math.Max(0, (int)MathF.Floor(MathF.Min(triangle.A.Y, MathF.Min(triangle.B.Y, triangle.C.Y))));
            var maxY = Math.Min(size - 1, (int)MathF.Ceiling(MathF.Max(triangle.A.Y, MathF.Max(triangle.B.Y, triangle.C.Y))));

            var area = EdgeFunction(triangle.A, triangle.B, triangle.C);
            if (MathF.Abs(area) < 1e-6f)
                return false;

            var shade = palette.Ambient + (1f - palette.Ambient) * triangle.Shade;

            // The MDL diffuse multiplies the texture rather than replacing it, exactly as the shade
            // does, so the two fold into one factor per channel before the span is walked.
            var tintR = shade * triangle.Tint.X;
            var tintG = shade * triangle.Tint.Y;
            var tintB = shade * triangle.Tint.Z;
            var flatB = (byte)Math.Clamp(palette.BaseB * tintB, 0, 255);
            var flatG = (byte)Math.Clamp(palette.BaseG * tintG, 0, 255);
            var flatR = (byte)Math.Clamp(palette.BaseR * tintR, 0, 255);
            var painted = false;

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

                    byte b = flatB, g = flatG, r = flatR;
                    if (triangle.Texture != null)
                    {
                        // Affine UV interpolation: at 128px the perspective error over one triangle is
                        // far below a pixel, so the divide a correct implementation would need buys
                        // nothing here.
                        var uv = triangle.UvA * w0 + triangle.UvB * w1 + triangle.UvC * w2;
                        if (!TrySample(triangle.Texture, uv, tintR, tintG, tintB, out r, out g, out b))
                            continue; // Cut-out texel: leave whatever is behind it showing.
                    }

                    var offset = (y * size + x) * BytesPerPixel;
                    pixels[offset] = b;
                    pixels[offset + 1] = g;
                    pixels[offset + 2] = r;
                    pixels[offset + 3] = 255;
                    painted = true;
                }
            }

            return painted;
        }

        /// <summary>
        /// Point-samples a wrapped UV and applies the key light and the mesh's diffuse colour.
        /// Returns false for a texel the artwork marks transparent, which the caller treats as not
        /// covered at all.
        /// </summary>
        private static bool TrySample(
            TextureImage texture,
            Vector2 uv,
            float tintR,
            float tintG,
            float tintB,
            out byte r,
            out byte g,
            out byte b)
        {
            r = g = b = 0;

            var u = uv.X - MathF.Floor(uv.X);
            var v = uv.Y - MathF.Floor(uv.Y);
            if (!float.IsFinite(u) || !float.IsFinite(v))
                return false;

            var x = Math.Clamp((int)(u * texture.Width), 0, texture.Width - 1);

            // NWN UVs run bottom-up; the decoded pixel rows run top-down.
            var y = Math.Clamp((int)((1f - v) * texture.Height), 0, texture.Height - 1);

            var offset = (y * texture.Width + x) * 4;
            if (texture.Pixels[offset + 3] < texture.AlphaCutoff)
                return false;

            r = (byte)Math.Clamp(texture.Pixels[offset] * tintR, 0, 255);
            g = (byte)Math.Clamp(texture.Pixels[offset + 1] * tintG, 0, 255);
            b = (byte)Math.Clamp(texture.Pixels[offset + 2] * tintB, 0, 255);
            return true;
        }

        private static float EdgeFunction(Vector2 a, Vector2 b, Vector2 c) =>
            (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);

        /// <summary>A triangle in model space, with the texture and UVs it should be filled from.</summary>
        private struct SourceTriangle
        {
            public Vector3 A;
            public Vector3 B;
            public Vector3 C;

            /// <summary>Null when this triangle's mesh has no resolvable texture and fills flat.</summary>
            public TextureImage? Texture;

            /// <summary>The mesh's diffuse colour, multiplied into whatever this triangle fills with.</summary>
            public Vector3 Tint;

            public Vector2 UvA;
            public Vector2 UvB;
            public Vector2 UvC;
        }

        private struct ProjectedTriangle
        {
            public Vector2 A;
            public Vector2 B;
            public Vector2 C;
            public float Depth;
            public float Shade;
            public TextureImage? Texture;

            /// <summary>The mesh's diffuse colour, multiplied into the fill.</summary>
            public Vector3 Tint;

            public Vector2 UvA;
            public Vector2 UvB;
            public Vector2 UvC;
        }
    }
}
