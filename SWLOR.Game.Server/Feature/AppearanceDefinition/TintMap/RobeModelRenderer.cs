using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public sealed class RobeModelCatalog
    {
        private readonly Dictionary<string, int> _phenotypes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, int> _basePhenotypes = new();

        public RobeModelCatalog(IEnumerable<(string Model, int Phenotype, int BasePhenotype)> definitions,
            IEnumerable<(int Phenotype, int BasePhenotype)> reserved = null)
        {
            foreach (var (phenotype, basePhenotype) in reserved ?? Array.Empty<(int, int)>())
            {
                if (phenotype is < 34 or > byte.MaxValue || basePhenotype is < 0 or >= 34 ||
                    !_basePhenotypes.TryAdd(phenotype, basePhenotype))
                    throw new ArgumentException("Invalid reserved robe phenotype.");
            }
            foreach (var (model, phenotype, basePhenotype) in definitions)
            {
                if (string.IsNullOrWhiteSpace(model) || phenotype is < 34 or > byte.MaxValue ||
                    basePhenotype is < 0 or >= 34)
                    throw new ArgumentException("Invalid robe rendering definition.");
                if (_phenotypes.ContainsKey(model) ||
                    (_basePhenotypes.TryGetValue(phenotype, out var previous) && previous != basePhenotype))
                    throw new ArgumentException("Conflicting robe rendering definitions.");
                _phenotypes.Add(model, phenotype);
                _basePhenotypes[phenotype] = basePhenotype;
            }
        }

        public int GetBasePhenotype(int phenotype) => _basePhenotypes.GetValueOrDefault(phenotype, phenotype);
        public bool Supports(string model) => !string.IsNullOrWhiteSpace(model) && _phenotypes.ContainsKey(model);

        public int ResolvePhenotype(int currentPhenotype, string robeModel, bool hasRgb)
        {
            var basePhenotype = GetBasePhenotype(currentPhenotype);
            if (!hasRgb || string.IsNullOrWhiteSpace(robeModel) ||
                !_phenotypes.TryGetValue(robeModel, out var rendered) ||
                GetBasePhenotype(rendered) != basePhenotype)
                return basePhenotype;
            return rendered;
        }
    }

    public static class RobeModelRenderer
    {
        private static RobeModelCatalog _catalog = new(Array.Empty<(string, int, int)>());

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
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
            _catalog = new RobeModelCatalog(definitions, reserved);
            Log.WriteStructured(LogGroup.Server, "Loaded {RobeRgbModelCount} robe RGB models.", definitions.Count);
        }

        public static int GetBasePhenotype(uint creature) => _catalog.GetBasePhenotype((int)GetPhenoType(creature));

        public static bool SupportsRgb(TintMapMaterialSelection selection) =>
            selection.ArmorPart != AppearanceArmor.Robe || _catalog.Supports(selection.ModelResref);

        public static bool Apply(uint creature, IReadOnlyList<TintMapMaterialSelection> selections, bool hasRobeRgb)
        {
            var robe = selections.FirstOrDefault(selection => selection.ArmorPart == AppearanceArmor.Robe);
            var current = (int)GetPhenoType(creature);
            var desired = _catalog.ResolvePhenotype(current, robe?.ModelResref, hasRobeRgb);
            if (desired != current)
            {
                var server = NWNXLib.g_pAppManager.m_pServerExoApp;
                var nativeCreature = server.GetCreatureByGameObjectID(creature);
                if (nativeCreature?.m_pStats == null)
                    return false;

                // ExecuteCommandSetPhenoType writes these two bytes, but rejects IDs > 99.
                // Our catalog validates reserved IDs against the actual byte-sized fields.
                // Match that appearance-only write without rebuilding stats or equipment.
                nativeCreature.m_pStats.m_nPhenoType = checked((byte)desired);
                nativeCreature.m_cAppearance.m_nPhenoType = checked((byte)desired);
                server.SetForceUpdate();
            }
            return desired != _catalog.GetBasePhenotype(desired);
        }
    }
}
