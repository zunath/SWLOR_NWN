using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class HelmetModelRenderer
    {
        /// <summary>
        /// Each tintable helmet helm_NNN is also published as head (HeadBase + NNN).
        /// The range is reserved: authored heads stay below it.
        /// </summary>
        public const ushort HeadBase = 1000;
        private const string HelmetPrefix = "helm_";

        /// <summary>
        /// Returns the render head for a worn, visible helmet on a parts-based body, or the
        /// canonical head otherwise. Only tintable helmets reach this as worn-helmet selections,
        /// so every helmet that resolves here has generated head geometry.
        /// </summary>
        public static ushort ResolveHead(string model, ushort originalHead, bool visible, bool partsAppearance)
        {
            if (!visible || !partsAppearance || model == null ||
                model.Length != HelmetPrefix.Length + 3 ||
                !model.StartsWith(HelmetPrefix, StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(model.AsSpan(HelmetPrefix.Length), out var id) || id < 1)
                return originalHead;

            return (ushort)(HeadBase + id);
        }

        public static bool IsRenderedHead(ushort head) => head > HeadBase && head < HeadBase + 1000;

        /// <summary>
        /// The phenotype-0 head resource the client loads for a render head. Missing phenotypes
        /// fall back to 0 (phenotype.2da DefaultPhenoType), but head models never fall back across
        /// race or gender, so a missing resource would render the wearer headless.
        /// </summary>
        public static string GetHeadModel(char gender, string race, ushort head) =>
            $"p{gender}{race.ToLowerInvariant()}0_head{head}";

        /// <summary>
        /// Exact RGB on a worn helmet needs its generated head for the wearer's race and gender.
        /// Other wearers keep the native helmet attachment, which supports preset colors only.
        /// </summary>
        public static bool SupportsRgb(TintMapMaterialSelection selection) =>
            !selection.IsWornHelmet || HasRenderHead(selection.CreaturePaletteSource, selection.ModelResref);

        private static bool HasRenderHead(uint creature, string helmetModel)
        {
            var head = ResolveHead(helmetModel, 0, true, true);
            if (head == 0)
                return false;

            var appearance = (int)GetAppearanceType(creature);
            if (!Get2DAString("appearance", "MODELTYPE", appearance).StartsWith("P", StringComparison.OrdinalIgnoreCase))
                return false;

            var race = Get2DAString("appearance", "RACE", appearance);
            if (string.IsNullOrWhiteSpace(race) || race.Length != 1)
                return false;

            var gender = GetGender(creature) == Gender.Female ? 'f' : 'm';
            return !string.IsNullOrEmpty(ResManGetAliasFor(GetHeadModel(gender, race, head), ResType.MDL));
        }

        public static void Apply(uint creature, IReadOnlyList<TintMapMaterialSelection> selections)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeCreature = server.GetCreatureByGameObjectID(creature);
            if (nativeCreature?.m_pStats == null)
                return;

            var helmet = selections.FirstOrDefault(selection => selection.IsWornHelmet);
            var visible = helmet != null && GetHiddenWhenEquipped(helmet.PaletteSource) == 0 &&
                          HasRenderHead(creature, helmet.ModelResref);
            var parts = Get2DAString("appearance", "MODELTYPE", (int)GetAppearanceType(creature))
                .StartsWith("P", StringComparison.OrdinalIgnoreCase);
            var originalHead = nativeCreature.m_pStats.m_nHeadVariation;
            var head = ResolveHead(helmet?.ModelResref, originalHead, visible, parts);
            var appearance = nativeCreature.m_cAppearance;
            if (head == originalHead)
            {
                // Leave unsupported appearances entirely native. Restore only an appearance
                // that we previously projected; this also honors the native hidden-item flag.
                if (!IsRenderedHead(appearance.m_nHeadVariation))
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
