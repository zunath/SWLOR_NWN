using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using ModuleResourceType = Nwn.Authoring.Resources.ModuleResourceType;
using SWLOR.NWN.Formats;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.AreaGeneration.Hosting;

/// <summary>Loads generated-object blueprints from the open module and sizes creatures from appearance.2da.</summary>
public sealed class SwlorBlueprintSource : IGeneratedAreaBlueprintSource
{
    private const float DefaultCreatureRadius = 1f;

    private readonly ModuleWorkspace _workspace;
    private TwoDaTable? _appearanceTable;
    private bool _appearanceTableLoaded;

    public SwlorBlueprintSource(ModuleWorkspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public bool TryLoadBlueprint(ModuleResourceType type, string resRef, out JsonGffDocument blueprint)
    {
        if (_workspace.TryLoadBlueprint(type, resRef, out var document))
        {
            blueprint = document.Document;
            return true;
        }

        blueprint = null!;
        return false;
    }

    public float GetCreatureCollisionRadius(string resRef, JsonGffDocument blueprint)
    {
        if (!_appearanceTableLoaded)
        {
            _appearanceTable = LoadCreatureAppearanceTable(_workspace.ResourceIndex);
            _appearanceTableLoaded = true;
        }

        var appearanceTable = _appearanceTable;
        var radius = DefaultCreatureRadius;
        if (appearanceTable != null)
        {
            var appearanceId = blueprint.Root.GetIntOrNull("Appearance_Type") ?? -1;
            var rawRadius = appearanceTable.GetString(appearanceId, "CREPERSPACE");
            if (!float.TryParse(
                    rawRadius,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out radius) ||
                radius <= 0f)
            {
                throw new InvalidOperationException(
                    $"Creature blueprint '{resRef}' has no valid CREPERSPACE in appearance.2da row {appearanceId}.");
            }
        }

        return radius;
    }

    private static TwoDaTable? LoadCreatureAppearanceTable(ResourceIndex? resourceIndex)
    {
        if (resourceIndex == null)
            return null;

        var identity = new ResourceIdentity("appearance", ResourceIdentity.TypeFromExtension("2da"));
        if (!resourceIndex.TryLookup(identity, out var resource))
            throw new InvalidOperationException("appearance.2da is unavailable for creature placement.");

        try
        {
            return TwoDaTable.Parse("appearance", resource.GetBytes());
        }
        catch (NwnFormatException ex)
        {
            throw new InvalidOperationException(
                "appearance.2da could not be read for creature placement.", ex);
        }
    }
}
