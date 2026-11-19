using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SWLOR.NWN.Formats.Bif;
using SWLOR.NWN.Formats.Gff;
using SWLOR.NWN.Formats.Key;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Appearance rows of the placeable blueprints (UTP) that ship with the NWN:EE base game, read from an installed client.
/// Structures such as the plain Footstool use these blueprints, which have no file under Module/utp.
/// </summary>
internal static class BaseGamePlaceableBlueprints
{
    private const ushort PlaceableBlueprintType = 2044;

    private static readonly Lazy<IReadOnlyDictionary<string, int>?> Appearances = new(Load);

    /// <summary>Blueprint resref to appearance row, or null when no NWN:EE install is found.</summary>
    public static IReadOnlyDictionary<string, int>? AppearanceByResref => Appearances.Value;

    private static IReadOnlyDictionary<string, int>? Load()
    {
        var data = FindDataDirectory();
        if (data == null)
            return null;

        var key = KeyReader.Read(Path.Combine(data, "nwn_base.key"));
        var appearances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in key.ResourceEntries.Where(entry => entry.ResourceType == PlaceableBlueprintType).GroupBy(entry => entry.BifIndex))
        {
            var bifName = Path.GetFileName(key.BifEntries[group.Key].Filename.Replace('\\', '/'));
            var bif = BifReader.Read(File.ReadAllBytes(Path.Combine(data, bifName)));
            var indexById = bif.VariableResources
                .Select((resource, index) => (Id: (int)(resource.Id & 0x000F_FFFF), Index: index))
                .ToDictionary(item => item.Id, item => item.Index);
            foreach (var entry in group)
            {
                var gff = GffReader.Read(bif.ExtractVariableResource(indexById[entry.VariableTableIndex])!);
                var appearance = gff.RootStruct.Fields.Single(field => field.Label == "Appearance").Value;
                appearances[entry.ResRef] = Convert.ToInt32(appearance);
            }
        }
        return appearances;
    }

    // Same lookup as the licensed-corpus tests: NWN_INSTALL_ROOT, then the usual Steam, GOG and Beamdog folders.
    private static string? FindDataDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("NWN_INSTALL_ROOT");
        var candidates = new[]
        {
            configured,
            @"C:\Program Files (x86)\Steam\steamapps\common\Neverwinter Nights",
            @"C:\Program Files\Steam\steamapps\common\Neverwinter Nights",
            @"C:\GOG Games\Neverwinter Nights Enhanced Edition",
            @"C:\Program Files (x86)\GOG Galaxy\Games\Neverwinter Nights Enhanced Edition",
            @"C:\Program Files\GOG Galaxy\Games\Neverwinter Nights Enhanced Edition",
            @"C:\Program Files (x86)\Beamdog Library\00785",
            @"C:\Program Files\Beamdog Library\00785"
        };
        return candidates
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.Combine(root!, "data"))
            .FirstOrDefault(data => File.Exists(Path.Combine(data, "nwn_base.key")));
    }
}
