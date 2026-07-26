using Radoub.Formats.Mdl;
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
    /// <item><b>Creatures</b> use their model, composed from body parts and textured, with their
    /// portrait as the fallback. A palette is for picking the thing you are about to place, and what gets
    /// placed is the model - a portrait shows a face the world never renders. Composition is the
    /// expensive path (eighteen part models against one TGA) but it runs once per blueprint into the disk
    /// cache. The portrait still covers the creatures whose parts do not resolve.</item>
    /// <item><b>Placeables and doors</b> use their model, which is the only thing they have. Their
    /// PortraitId fields are default data, not chosen artwork, so they are deliberately ignored - a
    /// stranger's face on a park bench is worse than no picture at all.</item>
    /// </list>
    /// <para>
    /// Returning null means "this blueprint genuinely has no artwork", which the caller answers with a
    /// type symbol. That happens for merchants, triggers and sound sets (NWN gives them no model at
    /// all) and for the placeables and doors whose appearance row in the 2DA is blank. Waypoints use
    /// the model declared by waypoint.2da.
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
        private readonly WaypointAppearanceService? _waypoints;
        private readonly BaseItemIconService? _baseItems;
        private readonly PortraitService? _portraits;

        /// <summary>Authored part textures for the compose run in flight; guarded by _composerGate.</summary>
        private readonly Domain.Render.ComposedPartTextures _partTextures = new();

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
            WaypointAppearanceService? waypoints = null,
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
            _waypoints = waypoints;
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
                ResourceType.Utc => RenderModel(type, root) ?? RenderPortrait(root),
                ResourceType.Utp or ResourceType.Utd or ResourceType.Utw => RenderModel(type, root),
                _ => null
            };
        }

        /// <summary>
        /// Builds a blueprint's geometry as a <see cref="RenderModel"/> rather than a thumbnail, for
        /// callers that draw the model themselves - the area editor's placement ghost.
        /// </summary>
        /// <remarks>
        /// Shares the resolver and the segmented-creature composer with the thumbnail path, which is
        /// the point: a ghost built any other way drifts from the preview the builder just clicked.
        /// Composition is not cheap, so callers are expected to hold the result for as long as the
        /// blueprint stays armed rather than rebuild it per frame - and, per the note on
        /// <see cref="RenderModel(ResourceType, Domain.Gff.JsonGffStruct)"/>, nothing here is cached:
        /// caching every blueprint's expanded meshes is what once reached a 37 GB working set.
        /// </remarks>
        public RenderModel? BuildModel(ResourceType type, string resRef)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(resRef))
                return null;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var reference = BlueprintModelResolver.Resolve(
                type, workspace.LoadBlueprint(type, resRef).Fields, _appearances, _placeables, _doors,
                LoadItemBlueprintRoot, PartModelExists, _waypoints);

            return reference.Kind switch
            {
                BlueprintModelKind.Simple when reference.ModelResRef != null => BuildRenderModel(reference.ModelResRef),
                BlueprintModelKind.Segmented => ComposeSegmented(reference),
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
                type, root, _appearances, _placeables, _doors, LoadItemBlueprintRoot, PartModelExists,
                _waypoints);

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

        /// <summary>
        /// Renders a model by resref, with no blueprint involved. This is how a tile gets a thumbnail:
        /// a tile is a row in a .set file, not a module resource, so there is nothing to load fields from
        /// - only geometry to draw.
        /// </summary>
        public IconImage? RenderModel(string modelResRef)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(modelResRef))
                return null;

            var pixels = ThumbnailRenderer.Render(
                BuildRenderModel(modelResRef), ModelRenderSize,
                palette: null, resolveTexture: _textures == null ? null : _textures.Get);

            return pixels == null ? null : new IconImage(ModelRenderSize, ModelRenderSize, pixels);
        }

        private RenderModel? BuildRenderModel(string modelResRef)
        {
            var model = LoadMdl(modelResRef, withSupermodelAnims: false);
            if (model == null)
                return null;

            try
            {
                return MdlMeshBuilder.Build(model, IdleFrames(model));
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
            {
                // _partTextures is filled by LoadComposerModel as the composer pulls each part in, so it
                // has to be cleared and read inside the same lock that owns the compose run.
                _partTextures.Clear();
                composed = _partComposer.Compose(reference.SkeletonResRef, parts, adjustSeams: true);
                if (composed != null)
                    _partTextures.Restore(composed, TextureExists);
            }

            if (composed == null)
                return null;

            // The idle comes off the skeleton, which is why the composer loads it with its supermodel
            // animations - a body part carries geometry, never keyframes.
            return MdlMeshBuilder.Build(composed, IdleFrames(composed));
        }

        /// <summary>
        /// The model's standing pose, or null when it has no idle to stand in.
        /// </summary>
        /// <remarks>
        /// Sampled at the first frame rather than played: the pose is what makes a creature read as a
        /// creature instead of the arms-out bind pose its geometry is stored in, and it costs one
        /// evaluation at build time. Animating it would mean re-posing and re-uploading every composed
        /// body each frame, which is a different piece of work - the sampler already takes a time, so
        /// that is a matter of driving it rather than rewriting it.
        /// </remarks>
        private IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> IdleFrames(MdlModel model) =>
            MdlAnimationPose
                .SampleIdleFrames(model, superModel => LoadMdl(superModel, withSupermodelAnims: true))
                .Select(frame => frame.Pose)
                .ToList();

        /// <summary>Whether a texture name resolves to a real resource, in any of NWN's texture formats.</summary>
        private bool TextureExists(string name)
        {
            if (_resourceIndex == null)
                return false;

            foreach (var extension in new[] { ".plt", ".tga", ".dds" })
            {
                if (_resourceIndex.TryLookup(
                        Domain.GameData.Resources.ResourceIdentity.FromFileName(name + extension), out _))
                {
                    return true;
                }
            }

            return false;
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
        /// Each part's authored texture names are recorded on the way in so
        /// <see cref="ComposedPartTextures"/> can undo the composer's resref override afterwards. Without
        /// that, the SWLOR custom parts which name their texture differently from their resref render
        /// white - which did not matter while creatures previewed as portraits, and matters now that they
        /// preview as models.
        /// </remarks>
        private MdlModel? LoadComposerModel(string resRef, bool withSupermodelAnims)
        {
            var model = LoadMdl(resRef, withSupermodelAnims);
            if (model != null && !withSupermodelAnims)
            {
                MdlGeometryFlattener.FlattenNodeTransforms(model);
                _partTextures.Record(resRef, model);
            }

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
