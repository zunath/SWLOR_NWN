using System;
using System.Collections.Generic;
using System.Globalization;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Parses NWN tileset .set files (INI format) into a <see cref="TilesetModel"/>.
    /// Pure C# / no engine dependencies so it can run both at boot time (fed by
    /// ResManGetFileContents) and inside unit tests (fed by File.ReadAllText).
    /// Parsing is tolerant: unknown keys/sections are ignored, missing values fall
    /// back to the documented defaults (empty string / 0), and section/key
    /// lookups are case-insensitive since real .set files are inconsistent about casing.
    /// </summary>
    public static class TilesetSetParser
    {
        // Community .set files contain corrupt counts (e.g. udp2.set declares Doors=1848138868 on
        // some tiles — garbage left by third-party editors). Counts drive loops, so unclamped
        // garbage turns parsing into a multi-minute stall. Bounds are far above any legitimate set.
        private const int MaxTiles = 16384;
        private const int MaxTerrains = 64;
        private const int MaxCrossers = 64;
        private const int MaxGroups = 4096;
        private const int MaxGroupDimension = 32;
        private const int MaxDoorsPerTile = 8;

        public static TilesetModel Parse(string resref, string setFileContents)
        {
            var sections = ParseSections(setFileContents);

            var model = new TilesetModel
            {
                Resref = resref
            };

            if (sections.TryGetValue("GENERAL", out var general))
            {
                model.Name = GetString(general, "Name");
                model.IsInterior = GetInt(general, "Interior") != 0;
                model.HasHeightTransition = GetInt(general, "HasHeightTransition") != 0;
                model.HeightTransition = GetFloat(general, "Transition");
                model.BorderTerrain = GetString(general, "Border");
                model.DefaultTerrain = GetString(general, "Default");
                model.FloorTerrain = GetString(general, "Floor");
            }

            model.Terrains = ParseNamedList(sections, "TERRAIN TYPES", "TERRAIN");
            model.Crossers = ParseNamedList(sections, "CROSSER TYPES", "CROSSER");
            model.Tiles = ParseTiles(sections);
            model.Groups = ParseGroups(sections, model.Tiles);

            return model;
        }

        /// <summary>
        /// Splits the raw file into an ordered map of section name -&gt; (key -&gt; value).
        /// Comment lines (starting with ';') and blank lines are ignored. Both section
        /// names and keys are treated case-insensitively.
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> ParseSections(string setFileContents)
        {
            var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> current = null;

            foreach (var rawLine in setFileContents.Split('\n'))
            {
                var line = rawLine.Trim('\r', ' ', '\t');
                if (line.Length == 0)
                    continue;

                if (line[0] == ';')
                    continue;

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var sectionName = line.Substring(1, line.Length - 2).Trim();
                    current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    sections[sectionName] = current;
                    continue;
                }

                if (current == null)
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0)
                    continue;

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1).Trim();
                current[key] = value;
            }

            return sections;
        }

        private static List<string> ParseNamedList(
            Dictionary<string, Dictionary<string, string>> sections,
            string countSectionName,
            string itemSectionPrefix)
        {
            var result = new List<string>();

            if (!sections.TryGetValue(countSectionName, out var countSection))
                return result;

            var count = Math.Clamp(GetInt(countSection, "Count"), 0, MaxTerrains);
            for (var i = 0; i < count; i++)
            {
                var name = sections.TryGetValue(itemSectionPrefix + i, out var itemSection)
                    ? GetString(itemSection, "Name")
                    : string.Empty;

                result.Add(name);
            }

            return result;
        }

        private static List<TileRecord> ParseTiles(Dictionary<string, Dictionary<string, string>> sections)
        {
            var tiles = new List<TileRecord>();

            if (!sections.TryGetValue("TILES", out var tilesSection))
                return tiles;

            var count = Math.Clamp(GetInt(tilesSection, "Count"), 0, MaxTiles);
            for (var i = 0; i < count; i++)
            {
                var tile = new TileRecord
                {
                    TileId = i
                };

                if (sections.TryGetValue("TILE" + i, out var tileSection))
                {
                    tile.Model = GetString(tileSection, "Model");
                    tile.WalkMesh = GetString(tileSection, "WalkMesh");
                    tile.PathNode = GetString(tileSection, "PathNode");
                    tile.ImageMap2D = GetString(tileSection, "ImageMap2D");

                    // [TL, TR, BR, BL]
                    tile.Corners = new[]
                    {
                        GetString(tileSection, "TopLeft"),
                        GetString(tileSection, "TopRight"),
                        GetString(tileSection, "BottomRight"),
                        GetString(tileSection, "BottomLeft")
                    };

                    tile.CornerHeights = new[]
                    {
                        GetInt(tileSection, "TopLeftHeight"),
                        GetInt(tileSection, "TopRightHeight"),
                        GetInt(tileSection, "BottomRightHeight"),
                        GetInt(tileSection, "BottomLeftHeight")
                    };

                    // [Top, Right, Bottom, Left]
                    tile.Edges = new[]
                    {
                        GetString(tileSection, "Top"),
                        GetString(tileSection, "Right"),
                        GetString(tileSection, "Bottom"),
                        GetString(tileSection, "Left")
                    };

                    var doorCount = Math.Clamp(GetInt(tileSection, "Doors"), 0, MaxDoorsPerTile);
                    for (var d = 0; d < doorCount; d++)
                    {
                        if (!sections.TryGetValue("TILE" + i + "DOOR" + d, out var doorSection))
                            continue;

                        tile.Doors.Add(new TileDoorRecord
                        {
                            Type = GetInt(doorSection, "Type"),
                            X = GetFloat(doorSection, "X"),
                            Y = GetFloat(doorSection, "Y"),
                            Z = GetFloat(doorSection, "Z"),
                            Orientation = GetFloat(doorSection, "Orientation")
                        });
                    }
                }

                tiles.Add(tile);
            }

            return tiles;
        }

        private static List<TileGroupRecord> ParseGroups(
            Dictionary<string, Dictionary<string, string>> sections,
            List<TileRecord> tiles)
        {
            var groups = new List<TileGroupRecord>();

            if (!sections.TryGetValue("GROUPS", out var groupsSection))
                return groups;

            var count = Math.Clamp(GetInt(groupsSection, "Count"), 0, MaxGroups);
            for (var groupIndex = 0; groupIndex < count; groupIndex++)
            {
                var group = new TileGroupRecord();

                if (sections.TryGetValue("GROUP" + groupIndex, out var groupSection))
                {
                    group.Name = GetString(groupSection, "Name");
                    group.Rows = Math.Clamp(GetInt(groupSection, "Rows"), 0, MaxGroupDimension);
                    group.Columns = Math.Clamp(GetInt(groupSection, "Columns"), 0, MaxGroupDimension);

                    var slotCount = group.Rows * group.Columns;
                    for (var slot = 0; slot < slotCount; slot++)
                    {
                        var tileId = GetInt(groupSection, "Tile" + slot, -1);
                        group.TileIds.Add(tileId);

                        // -1 marks an empty slot in the group footprint; there is no tile to tag.
                        // First group wins if a tile appears in more than one group.
                        if (tileId >= 0 && tileId < tiles.Count && tiles[tileId].GroupIndex < 0)
                            tiles[tileId].GroupIndex = groupIndex;
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        private static string GetString(Dictionary<string, string> section, string key)
        {
            return section != null && section.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static int GetInt(Dictionary<string, string> section, string key, int defaultValue = 0)
        {
            var raw = GetString(section, key);
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }

        private static float GetFloat(Dictionary<string, string> section, string key, float defaultValue = 0f)
        {
            var raw = GetString(section, key);
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }
    }
}
