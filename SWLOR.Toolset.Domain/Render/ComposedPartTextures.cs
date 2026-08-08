using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Undoes the part composer's texture override on a composed creature, where the part's own
    /// texture really exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>MdlPartComposer</c> overwrites every attached part mesh's <c>Bitmap</c> with the part's resref.
    /// That is a workaround for BioWare's reused part files, which carry stale bitmap fields, and it is
    /// correct for them because a BioWare part's texture is named after the part. Many SWLOR custom parts
    /// are not: <c>pmh0_bicepl249</c>'s meshes reference <c>N_RepSold01</c>, so the override points at a
    /// texture that does not exist and the part renders white.
    /// </para>
    /// <para>
    /// So the authored name is recorded while each part loads and put back afterwards - but only when it
    /// resolves to a real texture, which is what keeps the BioWare stale-bitmap case working.
    /// </para>
    /// <para>
    /// Shared rather than duplicated: the GL model preview and the palette's thumbnail renderer both
    /// compose creatures, and a creature that renders textured in one and white in the other is a bug
    /// waiting to be reported twice.
    /// </para>
    /// </remarks>
    public sealed class ComposedPartTextures
    {
        private readonly Dictionary<(string PartResRef, string MeshName), string> _authored =
            new(PartMeshKeyComparer.Instance);

        /// <summary>Forgets everything recorded. Call before each compose run.</summary>
        public void Clear() => _authored.Clear();

        /// <summary>
        /// Records the authored texture of every mesh in a part model, as it is loaded.
        /// </summary>
        public void Record(string partResRef, MdlModel partModel)
        {
            ArgumentNullException.ThrowIfNull(partModel);

            foreach (var mesh in partModel.GetMeshNodes())
            {
                // NULL is Aurora's sentinel for "no authored bitmap", not a texture override.
                // Standard hand models use it intentionally so the body-part resref stamped by
                // MdlPartComposer resolves their PLT. Recording the sentinel can otherwise restore
                // it over that resref when a literal null texture exists in the resource index,
                // leaving hands flat gray instead of skin-coloured.
                if (!string.IsNullOrWhiteSpace(mesh.Bitmap) &&
                    !mesh.Bitmap.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                {
                    _authored[(partResRef, mesh.Name)] = mesh.Bitmap;
                }
            }
        }

        /// <summary>
        /// Puts authored textures back on a composed model wherever <paramref name="textureExists"/>
        /// confirms them. Meshes the composer renamed to something that does resolve are left alone.
        /// </summary>
        public void Restore(MdlModel composed, Func<string, bool> textureExists)
        {
            ArgumentNullException.ThrowIfNull(composed);
            ArgumentNullException.ThrowIfNull(textureExists);

            foreach (var mesh in composed.GetMeshNodes())
            {
                if (string.IsNullOrWhiteSpace(mesh.Bitmap))
                    continue;

                // The composer set Bitmap to the part resref, so it is also the key into what was
                // recorded for that part.
                if (_authored.TryGetValue((mesh.Bitmap, mesh.Name), out var authored) &&
                    !string.Equals(authored, mesh.Bitmap, StringComparison.OrdinalIgnoreCase) &&
                    textureExists(authored))
                {
                    mesh.Bitmap = authored;
                }
            }
        }

        private sealed class PartMeshKeyComparer : IEqualityComparer<(string PartResRef, string MeshName)>
        {
            public static readonly PartMeshKeyComparer Instance = new();

            public bool Equals((string PartResRef, string MeshName) x, (string PartResRef, string MeshName) y) =>
                string.Equals(x.PartResRef, y.PartResRef, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.MeshName, y.MeshName, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string PartResRef, string MeshName) key) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(key.PartResRef),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(key.MeshName));
        }
    }
}
