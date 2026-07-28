using SWLOR.NWN.Formats.Gff;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// Reads the base game's standard palettes (<c>*palstd.itp</c>) out of the layered resource index and
    /// imports them into a <see cref="StandardPalette"/>.
    /// </summary>
    /// <remarks>
    /// Every failure returns <see cref="StandardPalette.Empty"/>. Missing base game, missing palette,
    /// unreadable GFF - none of them are worth failing a palette panel over, because the module's own
    /// content is still fully usable without the standard half.
    /// </remarks>
    public static class StandardPaletteLoader
    {
        /// <summary>
        /// The base game's standard palette resref for a blueprint type, or null for the types that have
        /// none (areas, dialogs, scripts).
        /// </summary>
        /// <remarks>
        /// These are the <c>*palstd</c> names, not the bare <c>*pal</c> ones, and the distinction is not
        /// cosmetic: SWLOR's own haks ship <c>creaturepal</c>, <c>doorpal</c> and friends, so the bare
        /// names resolve to SWLOR content through hak-over-base precedence and the "Standard" group would
        /// silently show custom data. Nothing overrides the <c>*palstd</c> names.
        /// </remarks>
        public static string? PaletteResRefFor(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utc => "creaturepalstd",
                ResourceType.Utd => "doorpalstd",
                ResourceType.Uti => "itempalstd",
                ResourceType.Utp => "placeablepalstd",
                ResourceType.Uts => "soundpalstd",
                ResourceType.Utm => "storepalstd",
                ResourceType.Utt => "triggerpalstd",
                ResourceType.Utw => "waypointpalstd",
                ResourceType.Area => null,
                ResourceType.Dlg => null,
                ResourceType.Nss => null,
                _ => null
            };
        }

        /// <summary>
        /// Imports the standard palette for a type. <paramref name="resolveStrRef"/> supplies the category
        /// names, which the base-game palettes carry as base <c>dialog.tlk</c> strrefs rather than as text;
        /// without it every folder reads as a "Category 12345" placeholder.
        /// <paramref name="reportProblem"/> receives a one-line reason whenever the result is empty for a
        /// type that should have had one.
        /// </summary>
        public static StandardPalette Load(
            ResourceIndex? index,
            ResourceType type,
            Func<uint, string?>? resolveStrRef = null,
            Action<string>? reportProblem = null)
        {
            var paletteResRef = PaletteResRefFor(type);
            if (index == null || paletteResRef == null)
                return StandardPalette.Empty;

            try
            {
                var itpIdentity = new ResourceIdentity(paletteResRef, ResourceIdentity.TypeFromExtension("itp"));
                if (!index.TryLookup(itpIdentity, out var handle))
                {
                    reportProblem?.Invoke(
                        $"No standard {type.DisplayName().ToLowerInvariant()} palette: '{paletteResRef}.itp' is not in the base game.");
                    return StandardPalette.Empty;
                }

                var document = new ItpDocument(GffJsonBridge.ToJsonDocument(GffReader.Read(handle.GetBytes())));
                var section = ItpCategoryImporter.Import(document, out var names, resolveStrRef);

                return new StandardPalette(section, ResolvableMembers(index, type, section), names);
            }
            catch (Exception ex)
            {
                reportProblem?.Invoke($"Could not read the standard palette '{paletteResRef}.itp': {ex.Message}");
                return StandardPalette.Empty;
            }
        }

        /// <summary>
        /// Narrows the palette's membership to the resrefs that really resolve. The palette file is a
        /// manifest of what BioWare shipped across every expansion, so it names blueprints a given install
        /// does not have; a tile for one of those could never open or place.
        /// </summary>
        private static IReadOnlySet<string> ResolvableMembers(
            ResourceIndex index, ResourceType type, CategorySection section)
        {
            var blueprintType = ResourceIdentity.TypeFromExtension(type.Extension());
            var resolvable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var resRef in section.AssignedResRefs())
            {
                if (index.TryLookup(new ResourceIdentity(resRef, blueprintType), out _))
                    resolvable.Add(resRef);
            }

            return resolvable;
        }
    }
}
