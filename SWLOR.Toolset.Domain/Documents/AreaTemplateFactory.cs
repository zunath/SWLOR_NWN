using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Builds the contents of a brand-new area from a template triplet (the new-area wizard's
    /// core). Only the .are needs reshaping - its ResRef/Tag/Name/Tileset/dimensions are rewritten
    /// and its Tile_List is regenerated as a solid width×height fill; the paired .git/.gic are
    /// generic empty instance lists that just get saved under the new resref unchanged. Also owns
    /// registering the new area in module.ifo's Mod_Area_list. Pure document work - file I/O and
    /// template selection are the app layer's job.
    /// </summary>
    public static class AreaTemplateFactory
    {
        /// <summary>The corpus "__struct_id" for a Tile_List entry.</summary>
        public const uint TileStructId = 1;

        /// <summary>The corpus "__struct_id" for a Mod_Area_list entry.</summary>
        public const uint AreaListStructId = 6;

        /// <summary>
        /// Rewrites <paramref name="are"/> into a fresh area: identity fields (ResRef/Tag/Name),
        /// tileset, dimensions, and a regenerated Tile_List of <paramref name="width"/>×
        /// <paramref name="height"/> cells all set to (<paramref name="fillTileId"/>,
        /// <paramref name="fillOrientation"/>). Every other field the template carries (lighting,
        /// flags, scripts, weather, …) flows through untouched. Intended for a freshly loaded,
        /// un-sessioned document; if the document is attached to a session, call inside a transaction.
        /// </summary>
        public static void PopulateNewArea(
            AreDocument are,
            string resRef, string displayName, string tilesetResRef,
            int width, int height,
            int fillTileId, int fillOrientation = 0)
        {
            ArgumentNullException.ThrowIfNull(are);
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Area dimensions must be positive.");

            are.Fields.SetString("ResRef", GffFieldType.ResRef, resRef);
            are.Tag = resRef;
            are.Name.Text = string.IsNullOrWhiteSpace(displayName) ? resRef : displayName;
            are.Tileset = tilesetResRef;
            are.Width = width;
            are.Height = height;

            var tileList = are.Fields.GetOrAddList("Tile_List");
            tileList.Clear();
            for (var i = 0; i < width * height; i++)
                tileList.Add(CreateTileStruct(fillTileId, fillOrientation));
        }

        /// <summary>
        /// A new Tile_List entry struct with the corpus field shape: id/orientation/height plus the
        /// toolset-default lighting and animation slots (AnimLoop1-3 = 1, all light slots = 0).
        /// </summary>
        public static JsonGffStruct CreateTileStruct(int tileId, int orientation, int heightLevel = 0)
        {
            var tile = JsonGffField.CreateStruct(TileStructId).Struct!;
            tile.SetInt("Tile_ID", GffFieldType.Int, tileId);
            tile.SetInt("Tile_Orientation", GffFieldType.Int, orientation);
            tile.SetInt("Tile_Height", GffFieldType.Int, heightLevel);
            tile.SetInt("Tile_MainLight1", GffFieldType.Byte, 0);
            tile.SetInt("Tile_MainLight2", GffFieldType.Byte, 0);
            tile.SetInt("Tile_SrcLight1", GffFieldType.Byte, 0);
            tile.SetInt("Tile_SrcLight2", GffFieldType.Byte, 0);
            tile.SetInt("Tile_AnimLoop1", GffFieldType.Byte, 1);
            tile.SetInt("Tile_AnimLoop2", GffFieldType.Byte, 1);
            tile.SetInt("Tile_AnimLoop3", GffFieldType.Byte, 1);
            return tile;
        }

        /// <summary>
        /// Registers <paramref name="resRef"/> in the module's Mod_Area_list (idempotent - a resref
        /// already listed is left as the single entry). Returns true when an entry was added.
        /// </summary>
        public static bool AddAreaToModule(IfoDocument ifo, string resRef)
        {
            ArgumentNullException.ThrowIfNull(ifo);
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            var list = ifo.Fields.GetOrAddList("Mod_Area_list");
            foreach (var entry in list)
            {
                if (string.Equals(entry.GetStringOrNull("Area_Name"), resRef, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var areaStruct = JsonGffField.CreateStruct(AreaListStructId).Struct!;
            areaStruct.SetString("Area_Name", GffFieldType.ResRef, resRef);
            list.Add(areaStruct);
            return true;
        }
    }
}
