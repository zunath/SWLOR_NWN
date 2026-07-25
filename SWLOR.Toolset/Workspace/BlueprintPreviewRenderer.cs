using Radoub.Formats.Mdl;
using Radoub.UI.Services;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Produces the pixels for one blueprint's palette preview, choosing the best source available for
    /// the kind of thing it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order per type is deliberate, and was settled by measuring the whole SWLOR corpus rather than
    /// guessing:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Items</b> use their inventory icon. There is no world model worth previewing, and the icon
    /// is the picture a builder already knows the item by. All 7,651 resolve.</item>
    /// <item><b>Creatures</b> use their portrait first, then their model. 922 of 938 have a portrait and
    /// 282 distinct ones are in use, so they are set deliberately and they are painted artwork - far
    /// easier to tell apart than the same humanoid silhouette flat-shaded 900 times, and about twenty
    /// times cheaper than composing eighteen body-part models. The model path still covers the rest,
    /// including the 209 dynamic-appearance NPCs whose body parts are all zero.</item>
    /// <item><b>Placeables and doors</b> use their model, which is the only thing they have. Their
    /// PortraitId fields are default data, not chosen artwork, so they are deliberately ignored - a
    /// stranger's face on a park bench is worse than no picture at all.</item>
    /// </list>
    /// <para>
    /// Returning null means "this blueprint genuinely has no artwork", which the caller answers with a
    /// type symbol. That happens for merchants, triggers, sound sets and waypoints (NWN gives them no
    /// model at all) and for the placeables and doors whose appearance row in the 2DA is blank.
    /// </para>
    /// </remarks>
    public sealed class BlueprintPreviewRenderer
    {
        /// <summary>Square size for rasterized model previews.</summary>
        public const int ModelRenderSize = 128;

        private readonly WorkspaceContext _workspaceContext;
        private readonly ResourceIndex? _resourceIndex;
        private readonly AppearanceService? _appearances;
        private readonly PlaceableAppearanceService? _placeables;
        private readonly DoorTypeService? _doors;
        private readonly BaseItemIconService? _baseItems;
        private readonly PortraitService? _portraits;

        private readonly MdlPartComposer? _partComposer;

        /// <summary>Shared across renders: one tileset texture can serve hundreds of placeables.</summary>
        private readonly PreviewTextureCache? _textures;

        /// <summary>
        /// The composer keeps an internal cache of parsed parts, so it is neither thread-safe nor worth
        /// duplicating per worker - one instance behind a lock both protects it and lets every creature
        /// share the parts already parsed.
        /// </summary>
        private readonly object _composerGate = new();

        public BlueprintPreviewRenderer(
            WorkspaceContext workspaceContext,
            ResourceIndex? resourceIndex = null,
            AppearanceService? appearances = null,
            PlaceableAppearanceService? placeables = null,
            DoorTypeService? doors = null,
            BaseItemIconService? baseItems = null,
            PortraitService? portraits = null,
            TwoDaService? twoDa = null,
            Domain.GameData.Tlk.TlkService? tlk = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _resourceIndex = resourceIndex;
            _appearances = appearances;
            _placeables = placeables;
            _doors = doors;
            _baseItems = baseItems;
            _portraits = portraits;

            if (resourceIndex != null)
            {
                var gameData = new SwlorGameDataService(resourceIndex, twoDa, tlk);
                _partComposer = new MdlPartComposer(gameData, LoadComposerModel);
                _textures = new PreviewTextureCache(resourceIndex);
            }
        }

        /// <summary>True when the game data needed to resolve any artwork at all is present.</summary>
        public bool IsAvailable => _resourceIndex != null;

        /// <summary>The blueprint types the palette offers previews for.</summary>
        public static bool IsSupported(ResourceType type) =>
            type is ResourceType.Utc or ResourceType.Uti or ResourceType.Utp
                or ResourceType.Utd or ResourceType.Utm or ResourceType.Utt
                or ResourceType.Uts or ResourceType.Utw;

        /// <summary>
        /// Renders <paramref name="resRef"/>'s preview, or returns null when it has no artwork.
        /// </summary>
        /// <remarks>
        /// Null means a decision - "there is nothing to draw for this blueprint" - and callers persist it,
        /// so this deliberately does <b>not</b> swallow unexpected failures into a null. An early version
        /// did, and a run that hit memory pressure recorded 250 perfectly good placeables as having no
        /// artwork forever. Failures propagate instead; the caller counts them and retries next time,
        /// which is the right trade for a cache that is otherwise permanent. Failures that genuinely mean
        /// "no artwork" - an unparseable model, an undecodable texture - are still handled at the point
        /// where that is the honest answer.
        /// </remarks>
        public IconImage? Render(ResourceType type, string resRef)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(resRef))
                return null;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var root = workspace.LoadBlueprint(type, resRef).Fields;

            return type switch
            {
                ResourceType.Uti => RenderItemIcon(root),
                ResourceType.Utc => RenderPortrait(root) ?? RenderModel(type, root),
                ResourceType.Utp or ResourceType.Utd => RenderModel(type, root),
                _ => null
            };
        }

        private IconImage? RenderItemIcon(Domain.Gff.JsonGffStruct root)
        {
            if (_baseItems == null || _resourceIndex == null)
                return null;

            foreach (var stack in ItemIconResolver.Resolve(root, _baseItems.GetOrNull))
            {
                var layers = new List<TextureImage>(stack.Layers.Count);
                foreach (var layer in stack.Layers)
                {
                    var decoded = TextureLoader.Load(_resourceIndex, layer);
                    if (decoded != null)
                        layers.Add(decoded);
                }

                var composed = IconComposer.Compose(layers);
                if (composed != null)
                    return composed;
            }

            return null;
        }

        private IconImage? RenderPortrait(Domain.Gff.JsonGffStruct root)
        {
            if (_portraits == null || _resourceIndex == null)
                return null;

            if (!root.TryGet("PortraitId", out var field))
                return null;

            var portraitId = (int)field.GetInteger();

            // 0 and 65535 are NWN's "no portrait" values; the rest index portraits.2da.
            if (portraitId is <= 0 or >= ushort.MaxValue)
                return null;

            var row = _portraits.GetAll().FirstOrDefault(candidate => candidate.Id == portraitId);
            if (row == null)
                return null;

            var variants = PortraitService.GetTgaVariants(row.BaseResRef);

            // Medium first: it is the variant sized closest to a tile, so nothing is scaled far.
            foreach (var candidate in new[] { variants.Medium, variants.Large, variants.Small, variants.Huge })
            {
                var decoded = TextureLoader.Load(_resourceIndex, candidate);
                if (decoded == null)
                    continue;

                var composed = IconComposer.Compose(new[] { decoded });
                if (composed != null)
                    return composed;
            }

            return null;
        }

        /// <summary>
        /// Rasterizes the blueprint's appearance model.
        /// </summary>
        /// <remarks>
        /// Models are deliberately parsed per call and dropped rather than cached. The obvious
        /// optimisation - reusing the app's shared <c>TileModelCache</c> - is what made an early version
        /// of the cache build reach a 37 GB working set: that cache never evicts (correctly, for one
        /// area's handful of repeated tile models), so walking every blueprint in the module retained
        /// several thousand fully expanded meshes at once. Parsing again costs milliseconds and the
        /// result is written to the disk cache anyway, so nothing is parsed twice across sessions.
        /// </remarks>
        private IconImage? RenderModel(ResourceType type, Domain.Gff.JsonGffStruct root)
        {
            var reference = BlueprintModelResolver.Resolve(
                type, root, _appearances, _placeables, _doors, LoadItemBlueprintRoot, PartModelExists);

            var model = reference.Kind switch
            {
                BlueprintModelKind.Simple when reference.ModelResRef != null => BuildRenderModel(reference.ModelResRef),
                BlueprintModelKind.Segmented => ComposeSegmented(reference),
                _ => null
            };

            var pixels = ThumbnailRenderer.Render(
                model, ModelRenderSize, palette: null, resolveTexture: _textures == null ? null : _textures.Get);
            return pixels == null ? null : new IconImage(ModelRenderSize, ModelRenderSize, pixels);
        }

        private RenderModel? BuildRenderModel(string modelResRef)
        {
            var model = LoadMdl(modelResRef, withSupermodelAnims: false);
            if (model == null)
                return null;

            try
            {
                return MdlMeshBuilder.Build(model);
            }
            catch (Exception)
            {
                // A model that cannot be turned into meshes falls back to the type symbol.
                return null;
            }
        }

        private RenderModel? ComposeSegmented(BlueprintModelReference reference)
        {
            if (_partComposer == null || reference.SkeletonResRef == null)
                return null;

            var parts = ApplyRobeCoverage(reference.Parts)
                .Select(part => (part.PartType, part.ModelResRef))
                .ToList();
            if (parts.Count == 0)
                return null;

            MdlModel? composed;
            lock (_composerGate)
                composed = _partComposer.Compose(reference.SkeletonResRef, parts, adjustSeams: true);

            return composed == null ? null : MdlMeshBuilder.Build(composed);
        }

        /// <summary>
        /// Only a robe whose geometry spans the whole body replaces the parts it covers; SWLOR's partial
        /// robes (loincloths, tabards) render alongside the full body, exactly as the game draws them.
        /// </summary>
        private IReadOnlyList<BlueprintModelPart> ApplyRobeCoverage(IReadOnlyList<BlueprintModelPart> parts)
        {
            var robe = parts.FirstOrDefault(part => part.PartType.Equals("robe", StringComparison.OrdinalIgnoreCase));
            if (robe == default)
                return parts;

            var robeModel = LoadMdl(robe.ModelResRef, withSupermodelAnims: false);
            if (robeModel == null || !RobeCoverage.IsFullBodyRobe(robeModel))
                return parts;

            return parts
                .Where(part => !BlueprintModelResolver.RobeCoveredParts.Contains(part.PartType))
                .ToList();
        }

        /// <summary>
        /// Loader for <see cref="MdlPartComposer"/>. Part models are flattened because the composer
        /// attaches their geometry assuming it sits at the part origin, which several SWLOR hak parts
        /// violate with in-file node offsets. The skeleton must never be flattened - its node transforms
        /// *are* the bone positions - which is exactly what the composer's withSupermodelAnims flag
        /// distinguishes.
        /// </summary>
        /// <remarks>
        /// Unlike the GL model preview, this does not undo the composer's habit of overwriting each part
        /// mesh's texture name with the part's resref. That override is right for BioWare parts and wrong
        /// for the SWLOR custom parts that name their texture differently, so those meshes simply render
        /// in the flat tone. It is worth almost nothing to fix here: creatures preview as their portrait,
        /// so composition only runs for the handful with none.
        /// </remarks>
        private MdlModel? LoadComposerModel(string resRef, bool withSupermodelAnims)
        {
            var model = LoadMdl(resRef, withSupermodelAnims);
            if (model != null && !withSupermodelAnims)
                MdlGeometryFlattener.FlattenNodeTransforms(model);

            return model;
        }

        private MdlModel? LoadMdl(string resRef, bool withSupermodelAnims)
        {
            if (_resourceIndex == null || string.IsNullOrWhiteSpace(resRef))
                return null;

            if (!_resourceIndex.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out var handle))
                return null;

            try
            {
                // A reader per parse: this runs on pooled threads and MdlReader carries parse state.
                return new MdlReader().Parse(handle.GetBytes());
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool PartModelExists(string resRef) =>
            _resourceIndex != null &&
            _resourceIndex.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out _);

        /// <summary>Loads an equipped item's root struct so armor can override a creature's body parts.</summary>
        private Domain.Gff.JsonGffStruct? LoadItemBlueprintRoot(string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            try
            {
                return workspace.LoadBlueprint(ResourceType.Uti, resRef).Fields;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
