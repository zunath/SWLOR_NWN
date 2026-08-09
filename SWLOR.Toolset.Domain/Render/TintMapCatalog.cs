using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.Formats.TwoDA;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Toolset-side view of tintmap.2da. The table remains the authority for which shader material
    /// channels a model exposes; the editor projects it by material because composed preview models
    /// already carry those material names on their meshes.
    /// </summary>
    public sealed class TintMapCatalog
    {
        private readonly IReadOnlyDictionary<string, TintMapMaterialDefinition> _materials;

        private TintMapCatalog(IReadOnlyDictionary<string, TintMapMaterialDefinition> materials)
        {
            _materials = materials;
        }

        public static TintMapCatalog? Load(ResourceIndex resourceIndex)
        {
            ArgumentNullException.ThrowIfNull(resourceIndex);

            var identity = new ResourceIdentity(
                "tintmap",
                ResourceIdentity.TypeFromExtension("2da"));
            if (!resourceIndex.TryLookup(identity, out var handle))
                return null;

            try
            {
                var table = TwoDAReader.Read(handle.GetBytes());
                var layersByMaterial = new Dictionary<string, HashSet<TintMapLayerType>>(
                    StringComparer.OrdinalIgnoreCase);
                for (var row = 0; row < table.RowCount; row++)
                {
                    var material = table.GetValue(row, "MATERIAL");
                    var layerList = table.GetValue(row, "LAYERS");
                    if (string.IsNullOrWhiteSpace(material) || string.IsNullOrWhiteSpace(layerList))
                        continue;

                    if (!layersByMaterial.TryGetValue(material, out var layers))
                    {
                        layers = new HashSet<TintMapLayerType>();
                        layersByMaterial[material] = layers;
                    }

                    foreach (var rawLayer in layerList.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(rawLayer, out var value) &&
                            Enum.IsDefined(typeof(TintMapLayerType), value))
                        {
                            layers.Add((TintMapLayerType)value);
                        }
                    }
                }

                var materials = layersByMaterial
                    .Where(pair => pair.Value.Count > 0)
                    .ToDictionary(
                        pair => pair.Key,
                        pair => new TintMapMaterialDefinition(
                            pair.Key,
                            pair.Key,
                            pair.Value.OrderBy(layer => layer).ToArray()),
                        StringComparer.OrdinalIgnoreCase);
                return new TintMapCatalog(materials);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IReadOnlyList<TintMapMaterialDefinition> FindMaterials(
            RenderModel? model,
            bool includeItemOwnedMaterials = true,
            bool includeNonItemOwnedMaterials = true,
            bool includeCreatureLayersFromItemOwnedMaterials = false)
        {
            if (model == null)
                return Array.Empty<TintMapMaterialDefinition>();

            var layersByMaterial = new Dictionary<string, HashSet<TintMapLayerType>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var mesh in model.Meshes)
            {
                TintMapMaterialDefinition? material = null;
                if (!string.IsNullOrWhiteSpace(mesh.MaterialName))
                    _materials.TryGetValue(mesh.MaterialName, out material);
                if (material == null && !string.IsNullOrWhiteSpace(mesh.TextureName))
                    _materials.TryGetValue(mesh.TextureName, out material);
                if (material == null)
                {
                    continue;
                }

                IReadOnlyList<TintMapLayerType> layers;
                if (mesh.UsesItemTintOverrides)
                {
                    layers = includeItemOwnedMaterials
                        ? material.Layers.Where(layer => !TintMapVariable.IsCreatureColorLayer(layer)).ToArray()
                        : includeCreatureLayersFromItemOwnedMaterials
                            ? material.Layers.Where(TintMapVariable.IsCreatureColorLayer).ToArray()
                            : Array.Empty<TintMapLayerType>();
                }
                else
                {
                    layers = includeNonItemOwnedMaterials
                        ? material.Layers
                        : Array.Empty<TintMapLayerType>();
                }

                if (layers.Count == 0)
                    continue;

                if (!layersByMaterial.TryGetValue(material.Resref, out var selectedLayers))
                {
                    selectedLayers = new HashSet<TintMapLayerType>();
                    layersByMaterial[material.Resref] = selectedLayers;
                }

                selectedLayers.UnionWith(layers);
            }

            return layersByMaterial
                .Select(pair => new TintMapMaterialDefinition(
                    pair.Key,
                    pair.Key,
                    pair.Value.OrderBy(layer => layer).ToArray()))
                .OrderBy(material => material.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
