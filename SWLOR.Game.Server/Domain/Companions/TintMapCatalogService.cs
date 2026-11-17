using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Reads the tint-map material tables and the neck, part-skin, shoulder and robe render tables from the 2DAs
    /// when the module caches its data, and hands each to the state service that holds it.
    /// </summary>
    public class TintMapCatalogService : ITintMapCatalogService
    {
        private const string TintMap2DA = "tintmap";

        private readonly IAppearanceRenderStateService _appearanceRenderState;
        private readonly ITintMapStateService _tintMapState;

        public TintMapCatalogService(
            IAppearanceRenderStateService appearanceRenderState,
            ITintMapStateService tintMapState)
        {
            _appearanceRenderState = appearanceRenderState;
            _tintMapState = tintMapState;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadMaterials()
        {
            var materials = new Dictionary<string, List<TintMapMaterialDefinition>>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in new[] { TintMap2DA, "cloaktint" })
            {
                for (var row = 0; row < Get2DARowCount(table); row++)
                {
                    var model = Get2DAString(table, "MODEL", row);
                    var material = Get2DAString(table, "MATERIAL", row);
                    var layerValues = Get2DAString(table, "LAYERS", row).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(material) || layerValues.Length == 0)
                        continue;

                    var layers = new List<TintMapLayerType>();
                    foreach (var layerValue in layerValues)
                    {
                        if (int.TryParse(layerValue, out var value) && Enum.IsDefined(typeof(TintMapLayerType), value))
                            layers.Add((TintMapLayerType)value);
                    }

                    if (layers.Count == 0)
                        continue;

                    if (!materials.TryGetValue(model, out var modelMaterials))
                    {
                        modelMaterials = new List<TintMapMaterialDefinition>();
                        materials[model] = modelMaterials;
                    }

                    modelMaterials.Add(new TintMapMaterialDefinition(material, material, layers.ToArray()));
                }
            }

            _tintMapState.LoadMaterials(materials);

            Log.WriteStructured(
                LogGroup.Server,
                "Loaded {TintMapModelCount} tint-map models.",
                materials.Count);
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadNeckCatalog()
        {
            var rows = new List<(string, string, string)>();
            for (var row = 0; row < Get2DARowCount("neckrender"); row++)
                rows.Add((Get2DAString("neckrender", "BODY", row), Get2DAString("neckrender", "SOURCE", row),
                    Get2DAString("neckrender", "RENDER", row)));
            _appearanceRenderState.SetNeckCatalog(new NeckModelCatalog(rows));
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadPartSkinCatalog()
        {
            var layouts = new List<(string, string)>();
            for (var row = 0; row < Get2DARowCount("partlayout"); row++)
                layouts.Add((Get2DAString("partlayout", "BODY", row), Get2DAString("partlayout", "LAYOUT", row)));
            var renders = new List<(string, string, string)>();
            for (var row = 0; row < Get2DARowCount("partrender"); row++)
                renders.Add((Get2DAString("partrender", "LAYOUT", row), Get2DAString("partrender", "SOURCE", row),
                    Get2DAString("partrender", "RENDER", row)));
            _appearanceRenderState.SetPartSkinCatalog(new PartSkinModelCatalog(layouts, renders));
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadRobeCatalog()
        {
            var definitions = new List<(string, int, int)>();
            var reserved = new List<(int, int)>();
            // Preserve normalization of saved creatures even if a robe is later retired.
            // Generated IDs stay reserved in phenotype.2da and must never be recycled.
            for (var row = 34; row < Get2DARowCount("phenotype"); row++)
            {
                if (!Get2DAString("phenotype", "Label", row).StartsWith("RobeRgb_", StringComparison.Ordinal))
                    continue;
                if (!int.TryParse(Get2DAString("phenotype", "DefaultPhenoType", row), out var basePhenotype))
                    throw new InvalidOperationException($"Invalid robe phenotype {row}.");
                reserved.Add((row, basePhenotype));
            }
            for (var row = 0; row < Get2DARowCount("roberender"); row++)
            {
                var model = Get2DAString("roberender", "MODEL", row);
                if (!int.TryParse(Get2DAString("roberender", "PHENOTYPE", row), out var phenotype) ||
                    !int.TryParse(Get2DAString("roberender", "BASEPHENOTYPE", row), out var basePhenotype))
                    throw new InvalidOperationException($"Invalid robe rendering row {row}.");
                definitions.Add((model, phenotype, basePhenotype));
            }
            _appearanceRenderState.SetRobeCatalog(new RobeModelCatalog(definitions, reserved));
            Log.WriteStructured(LogGroup.Server, "Loaded {RobeRgbModelCount} robe RGB models.", definitions.Count);
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadShoulderCatalog()
        {
            var rows = new List<(string, string, string)>();
            for (var row = 0; row < Get2DARowCount("shoulderrender"); row++)
                rows.Add((Get2DAString("shoulderrender", "BODY", row), Get2DAString("shoulderrender", "SOURCE", row),
                    Get2DAString("shoulderrender", "RENDER", row)));
            _appearanceRenderState.SetShoulderCatalog(new ShoulderModelCatalog(rows));
        }
    }
}
