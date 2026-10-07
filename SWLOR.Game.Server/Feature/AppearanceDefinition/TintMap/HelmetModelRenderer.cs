using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public sealed class HelmetModelCatalog
    {
        private readonly Dictionary<string, ushort> _heads = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<int> _renderedHeads = new();

        public HelmetModelCatalog(IEnumerable<(string Model, int Head)> definitions)
        {
            foreach (var (model, head) in definitions)
            {
                if (string.IsNullOrWhiteSpace(model) || !model.StartsWith("helm_", StringComparison.OrdinalIgnoreCase) ||
                    head is < 1000 or > ushort.MaxValue || !_renderedHeads.Add(head) || !_heads.TryAdd(model, (ushort)head))
                    throw new ArgumentException("Invalid helmet rendering definition.");
            }
        }

        public ushort ResolveHead(string model, ushort originalHead, bool visible, bool partsAppearance) =>
            visible && partsAppearance && model != null && _heads.TryGetValue(model, out var head)
                ? head : originalHead;

        public bool IsRenderedHead(ushort head) => _renderedHeads.Contains(head);
    }

    public static class HelmetModelRenderer
    {
        private static HelmetModelCatalog _catalog = new(Array.Empty<(string, int)>());

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            var definitions = new List<(string Model, int Head)>();
            for (var row = 0; row < Get2DARowCount("helmrgb"); row++)
            {
                if (!int.TryParse(Get2DAString("helmrgb", "HEAD", row), out var head))
                    throw new InvalidOperationException($"Invalid helmet rendering row {row}.");
                definitions.Add((Get2DAString("helmrgb", "MODEL", row), head));
            }
            _catalog = new HelmetModelCatalog(definitions);
        }

        public static void Apply(uint creature, IReadOnlyList<TintMapMaterialSelection> selections)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeCreature = server.GetCreatureByGameObjectID(creature);
            if (nativeCreature?.m_pStats == null)
                return;

            var helmet = selections.FirstOrDefault(selection => selection.IsWornHelmet);
            var visible = helmet != null && GetHiddenWhenEquipped(helmet.PaletteSource) == 0;
            var parts = Get2DAString("appearance", "MODELTYPE", (int)GetAppearanceType(creature))
                .StartsWith("P", StringComparison.OrdinalIgnoreCase);
            var originalHead = nativeCreature.m_pStats.m_nHeadVariation;
            var head = _catalog.ResolveHead(helmet?.ModelResref, originalHead, visible, parts);
            var appearance = nativeCreature.m_cAppearance;
            if (head == originalHead)
            {
                // Leave unsupported appearances entirely native. Restore only an appearance
                // that we previously projected; this also honors the native hidden-item flag.
                if (!_catalog.IsRenderedHead(appearance.m_nHeadVariation))
                    return;
                nativeCreature.UpdateAppearanceForEquippedItems();
                appearance.m_nHeadVariation = originalHead;
                server.SetForceUpdate();
                return;
            }
            if (appearance.m_nHeadVariation == head && appearance.m_oidHeadItem == OBJECT_INVALID)
                return;

            // The client skips its separate helmet when replaying creature material rows.
            // Render the same compiled geometry as its head instead. Only the replicated
            // appearance changes: stats, the equipped item, dyes, and visibility stay native.
            appearance.m_nHeadVariation = head;
            appearance.m_oidHeadItem = OBJECT_INVALID;
            server.SetForceUpdate();
        }
    }
}
