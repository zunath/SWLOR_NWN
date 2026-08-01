using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Documents;
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
    public sealed class BlueprintPreviewRenderer : IPreviewImageSource
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
        private readonly TwoDaService? _twoDa;

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
            _twoDa = twoDa;

            if (resourceIndex != null)
            {
                _partComposer = new MdlPartComposer(LoadComposerModel);
                _textures = new PreviewTextureCache(resourceIndex);
                resourceIndex.ResourcesReloaded += OnResourcesReloaded;
            }
        }

        private void OnResourcesReloaded()
        {
            lock (_composerGate)
            {
                _partComposer?.Clear();
                _partTextures.Clear();
            }
            _textures?.Clear();
        }

        /// <summary>True when the game data needed to resolve any artwork at all is present.</summary>
        public bool IsAvailable => _resourceIndex != null;

        /// <summary>Coarse version of every game-data dependency used by rendered previews.</summary>
        public DateTime ContentVersionUtc => _resourceIndex?.ContentVersionUtc ?? DateTime.MinValue;

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
        public IconImage? Render(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint = false)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(resRef))
                return null;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var root = (useIndexedBlueprint
                ? workspace.LoadIndexedBlueprint(type, resRef)
                : workspace.LoadBlueprint(type, resRef)).Fields;

            return type switch
            {
                ResourceType.Uti => RenderItemIcon(root),
                ResourceType.Utc => RenderModel(type, root, useIndexedBlueprint) ?? RenderPortrait(root),
                ResourceType.Utp or ResourceType.Utd or ResourceType.Utw =>
                    RenderModel(type, root, useIndexedBlueprint),
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
        public RenderModel? BuildModel(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint = false)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(resRef))
                return null;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var blueprint = useIndexedBlueprint
                ? workspace.LoadIndexedBlueprint(type, resRef)
                : workspace.LoadBlueprint(type, resRef);
            return BuildModel(type, blueprint.Fields, useIndexedBlueprint);
        }

        /// <summary>
        /// Builds geometry from an embedded blueprint struct, such as a creature instance in a GIT.
        /// Instances carry a complete copy of the creature fields and may intentionally differ from
        /// (or outlive) their source UTC, so resolving them by TemplateResRef is not equivalent.
        /// </summary>
        public RenderModel? BuildModel(
            ResourceType type,
            Domain.Gff.JsonGffStruct root,
            bool useIndexedBlueprint = false,
            bool armorPreviewFemale = false)
        {
            ArgumentNullException.ThrowIfNull(root);
            if (!IsAvailable)
                return null;

            var reference = BlueprintModelResolver.Resolve(
                type, root, _appearances, _placeables, _doors,
                itemResRef => LoadItemBlueprintRoot(itemResRef, useIndexedBlueprint),
                PartModelExists, _waypoints, _baseItems == null ? null : _baseItems.GetOrNull,
                armorPreviewFemale);

            var model = reference.Kind switch
            {
                BlueprintModelKind.Simple when reference.ModelResRef != null =>
                    type == ResourceType.Utc
                        ? BuildCreatureRenderModel(reference.ModelResRef)
                        : BuildRenderModel(reference.ModelResRef),
                BlueprintModelKind.Segmented => ComposeSegmented(reference, type == ResourceType.Utc),
                BlueprintModelKind.ItemComposite => ComposeItemParts(reference),
                _ => null
            };

            return WithLayerColors(model, reference);
        }

        /// <summary>
        /// Hands the blueprint's dye choices to the model so the viewport can colour its PLT layers.
        /// The software thumbnail path passes them to the texture cache directly; the GL viewport
        /// only sees the model, so without this it drew every dyed surface at the palette default.
        /// </summary>
        private static RenderModel? WithLayerColors(RenderModel? model, BlueprintModelReference reference)
        {
            if (model == null || reference.LayerColorIndices.Count == 0)
                return model;

            return new RenderModel
            {
                Name = model.Name,
                Meshes = model.Meshes,
                Animations = model.Animations,
                Emitters = model.Emitters,
                DefaultAnimationName = model.DefaultAnimationName,
                LayerColorIndices = reference.LayerColorIndices,
            };
        }

        /// <summary>
        /// Renders the inventory icon for a live item struct. Public because an open item editor
        /// previews its own unsaved document; the disk-loading <see cref="Render"/> path cannot see
        /// edits that have not been saved yet.
        /// </summary>
        public IconImage? RenderItemIcon(Domain.Gff.JsonGffStruct root)
        {
            ArgumentNullException.ThrowIfNull(root);
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
        private IconImage? RenderModel(
            ResourceType type,
            Domain.Gff.JsonGffStruct root,
            bool useIndexedBlueprint)
        {
            var reference = BlueprintModelResolver.Resolve(
                type, root, _appearances, _placeables, _doors,
                itemResRef => LoadItemBlueprintRoot(itemResRef, useIndexedBlueprint),
                PartModelExists, _waypoints, _baseItems == null ? null : _baseItems.GetOrNull);

            var model = reference.Kind switch
            {
                BlueprintModelKind.Simple when reference.ModelResRef != null => BuildRenderModel(reference.ModelResRef),
                BlueprintModelKind.Segmented => ComposeSegmented(reference, includeCreatureAnimations: false),
                BlueprintModelKind.ItemComposite => ComposeItemParts(reference),
                _ => null
            };

            var tintMapOverrides = TintMapOverrides.Read(new VarTable(root));
            Func<string, TextureImage?>? resolveTexture = _textures == null
                ? null
                : texture => _textures.Get(texture, reference.LayerColorIndices, tintMapOverrides);
            var pixels = ThumbnailRenderer.Render(
                model, ModelRenderSize, palette: null, resolveTexture: resolveTexture);
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
                palette: null,
                resolveTexture: _textures == null ? null : texture => _textures.Get(texture));

            return pixels == null ? null : new IconImage(ModelRenderSize, ModelRenderSize, pixels);
        }

        /// <summary>
        /// Renders a multi-tile palette group as one picture: every slot's model laid out on the
        /// grid, so the thumbnail shows the group's footprint instead of its first tile.
        /// </summary>
        public IconImage? RenderTileGroup(IReadOnlyList<string> slotModelResRefs, int columns, int rows)
        {
            if (!IsAvailable || slotModelResRefs == null || slotModelResRefs.Count == 0)
                return null;

            var slots = new RenderModel?[slotModelResRefs.Count];
            for (var slot = 0; slot < slotModelResRefs.Count; slot++)
            {
                slots[slot] = string.IsNullOrWhiteSpace(slotModelResRefs[slot])
                    ? null
                    : BuildRenderModel(slotModelResRefs[slot]);
            }

            var pixels = ThumbnailRenderer.Render(
                TileGroupPreview.Compose(slots, columns, rows), ModelRenderSize,
                palette: null,
                resolveTexture: _textures == null ? null : texture => _textures.Get(texture));

            return pixels == null ? null : new IconImage(ModelRenderSize, ModelRenderSize, pixels);
        }

        /// <summary>
        /// Renders one <c>appearance.2da</c> row on a neutral generic creature — what the creature
        /// editor's appearance grid shows.
        /// </summary>
        /// <remarks>
        /// Goes through a synthetic creature struct rather than reading the row's model column
        /// directly, and that is the whole point. Roughly half of appearance.2da is
        /// <c>MODELTYPE = P</c>, where the row names a phenotype rather than a model and the real
        /// geometry is composed from head, torso and limb parts. Handing the struct to the same
        /// resolver the thumbnails and the placement ghost use means those rows draw the same
        /// creature the game would, instead of showing nothing.
        /// </remarks>
        public IconImage? RenderCreatureAppearance(int appearanceId)
        {
            if (!IsAvailable || appearanceId < 0)
                return null;

            var root = new Domain.Gff.JsonGffStruct();
            root.SetInt("Appearance_Type", Domain.Gff.GffFieldType.Word, appearanceId);
            CreatureAppearanceDefaults.ApplyGenericSegmentedBody(root);

            return RenderModel(ResourceType.Utc, root, useIndexedBlueprint: false);
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

        private RenderModel? BuildCreatureRenderModel(string modelResRef)
        {
            var model = LoadMdl(modelResRef, withSupermodelAnims: false);
            if (model == null)
                return null;

            try
            {
                var idle = IdleFrames(model);
                var animations = MdlAnimationPose.SampleCreaturePreviewAnimations(
                    model,
                    superModel => LoadMdl(superModel, withSupermodelAnims: true));
                return MdlMeshBuilder.BuildAnimatedPreview(model, idle, animations);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private RenderModel? ComposeSegmented(
            BlueprintModelReference reference,
            bool includeCreatureAnimations)
        {
            if (_partComposer == null || reference.SkeletonResRef == null)
                return null;

            var parts = ApplyRobeCoverage(reference.Parts);
            if (parts.Count == 0)
                return null;

            // Aurora keeps weighted garments as separate visuals. A robe's a_ba_coat supermodel is
            // an overlay: it supplies coat-helper tracks and body rotations, but deliberately omits
            // translations shared with the wearer. Those missing channels must inherit the wearer's
            // bind pose; taking them from the robe's zeroed skin skeleton collapses the legs upward.
            var skinParts = new List<(string PartType, MdlModel Model)>();
            var rigidParts = new List<(string PartType, string ModelResRef)>();
            foreach (var part in parts)
            {
                var model = LoadMdl(part.ModelResRef, withSupermodelAnims: false);
                if (model != null && MdlMeshBuilder.ContainsNamedSkinWeights(model))
                    skinParts.Add((part.PartType, model));
                else
                    rigidParts.Add((part.PartType, part.ModelResRef));
            }

            var renderModels = new List<RenderModel>();
            var skeleton = LoadMdl(reference.SkeletonResRef, withSupermodelAnims: false);
            var weightedRobe = skinParts
                .Where(part => part.PartType.Equals("robe", StringComparison.OrdinalIgnoreCase))
                .Select(part => part.Model)
                .FirstOrDefault();
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? sharedFrames = null;
            IReadOnlyList<MdlAnimationPose.SampledAnimation> sharedAnimations =
                Array.Empty<MdlAnimationPose.SampledAnimation>();
            if (weightedRobe != null)
            {
                var bindPose = LayeredGarmentBindPose(weightedRobe, skeleton);
                sharedFrames = skeleton == null
                    ? IdleFrames(weightedRobe)
                    : LayeredGarmentIdleFrames(weightedRobe, bindPose);
                if (includeCreatureAnimations)
                {
                    sharedAnimations = MdlAnimationPose.SampleCreaturePreviewAnimations(
                        weightedRobe,
                        superModel => LoadMdl(superModel, withSupermodelAnims: true),
                        bindPose);
                }
            }
            else if (skeleton != null)
            {
                sharedFrames = IdleFrames(skeleton);
                if (includeCreatureAnimations)
                {
                    sharedAnimations = MdlAnimationPose.SampleCreaturePreviewAnimations(
                        skeleton,
                        superModel => LoadMdl(superModel, withSupermodelAnims: true));
                }
            }

            if (rigidParts.Count > 0)
            {
                MdlModel? composed;
                lock (_composerGate)
                {
                    // _partTextures is filled by LoadComposerModel as the composer pulls each part in,
                    // so it has to be cleared and read inside the same lock that owns the compose run.
                    _partTextures.Clear();
                    composed = _partComposer.Compose(reference.SkeletonResRef, rigidParts, adjustSeams: true);
                    if (composed != null)
                        _partTextures.Restore(composed, TextureExists);
                }

                if (composed != null)
                {
                    var frames = sharedFrames ?? IdleFrames(composed);
                    renderModels.Add(includeCreatureAnimations
                        ? MdlMeshBuilder.BuildAnimatedPreview(composed, frames, sharedAnimations)
                        : MdlMeshBuilder.Build(composed, frames));
                }
            }

            renderModels.AddRange(skinParts.Select(part =>
            {
                var frames = sharedFrames ?? IdleFrames(part.Model);
                return includeCreatureAnimations
                    ? MdlMeshBuilder.BuildAnimatedPreview(part.Model, frames, sharedAnimations)
                    : MdlMeshBuilder.Build(part.Model, frames);
            }));

            return CombineRenderModels(reference.SkeletonResRef, renderModels);
        }

        /// <summary>
        /// Composes a composite item's three fixed-position part models (a weapon's bottom/middle/top)
        /// with no skeleton, via <c>MdlPartComposer.ComposeFlat</c> - the same merge path composite
        /// weapons already use nowhere else in this file, but a sibling operation to
        /// <see cref="ComposeSegmented"/>'s skeleton-attached compose.
        /// </summary>
        private RenderModel? ComposeItemParts(BlueprintModelReference reference)
        {
            if (_partComposer == null)
                return null;

            var partResRefs = reference.Parts.Select(part => part.ModelResRef).ToList();
            if (partResRefs.Count == 0)
                return null;

            MdlModel? composed;
            lock (_composerGate)
            {
                // _partTextures is filled as LoadComposerModel loads each part (see its remarks), but
                // ComposeFlat never overrides a mesh's authored Bitmap the way TryAddBodyPart does, so
                // there is nothing for ComposedPartTextures.Restore to undo here - clearing keeps the
                // shared field's state honest for whichever compose runs next.
                _partTextures.Clear();
                composed = _partComposer.ComposeFlat(partResRefs, "item");
            }

            return composed == null ? null : MdlMeshBuilder.Build(composed, IdleFrames(composed));
        }

        /// <summary>The model's sampled idle, including the hierarchy from its own supermodel.</summary>
        private IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> IdleFrames(MdlModel model)
        {
            var frames = MdlAnimationPose.SampleIdleFrames(
                model,
                superModel => LoadMdl(superModel, withSupermodelAnims: true));
            return frames.Select(frame => frame.Pose).ToList();
        }

        /// <summary>
        /// Samples a garment overlay with wearer bones providing the bind transform for every
        /// shared name. Garment-only helpers remain sourced from the garment, so coat panels move
        /// without replacing the mannequin's complete skeleton.
        /// </summary>
        private IReadOnlyDictionary<string, MdlNode> LayeredGarmentBindPose(
            MdlModel garment,
            MdlModel? wearer)
        {
            var bindPose = MdlAnimationPose.BindPose(garment).ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
            if (wearer != null)
            {
                foreach (var (name, node) in MdlAnimationPose.BindPose(wearer))
                    bindPose[name] = node;
            }

            return bindPose;
        }

        private IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> LayeredGarmentIdleFrames(
            MdlModel garment,
            IReadOnlyDictionary<string, MdlNode> bindPose)
        {
            var frames = MdlAnimationPose.SampleIdleFrames(
                garment,
                superModel => LoadMdl(superModel, withSupermodelAnims: true),
                bindPose);
            return frames.Select(frame => frame.Pose).ToList();
        }

        private static RenderModel? CombineRenderModels(string name, IReadOnlyList<RenderModel> models)
        {
            if (models.Count == 0)
                return null;

            return new RenderModel
            {
                Name = name,
                Meshes = models.SelectMany(model => model.Meshes).ToList(),
                Animations = models
                    .SelectMany(model => model.Animations)
                    .GroupBy(animation => animation.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList(),
                Emitters = models.SelectMany(model => model.Emitters).ToList(),
                DefaultAnimationName = models
                    .Select(model => model.DefaultAnimationName)
                    .FirstOrDefault(animation => !string.IsNullOrWhiteSpace(animation))
            };
        }

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
        /// Applies NWN's per-appearance robe concealment flags. Geometry classification remains the
        /// fallback for installations where <c>parts_robe.2da</c> is unavailable.
        /// </summary>
        private IReadOnlyList<BlueprintModelPart> ApplyRobeCoverage(IReadOnlyList<BlueprintModelPart> parts)
        {
            var robe = parts.FirstOrDefault(part => part.PartType.Equals("robe", StringComparison.OrdinalIgnoreCase));
            if (robe == default)
                return parts;

            if (RobePartVisibility.TryGetHiddenParts(_twoDa, robe.ModelResRef, out var hiddenParts))
            {
                return hiddenParts.Count == 0
                    ? parts
                    : parts.Where(part => !hiddenParts.Contains(part.PartType)).ToList();
            }

            var robeModel = LoadMdl(robe.ModelResRef, withSupermodelAnims: false);
            if (robeModel == null || !RobeCoverage.IsFullBodyRobe(robeModel))
                return parts;

            // Legacy fallback when the authoritative table is absent: only a classic full robe
            // replaces the broad body-part set. Partial robes remain additive.
            return parts
                .Where(part => !BlueprintModelResolver.RobeCoveredParts.Contains(part.PartType))
                .ToList();
        }

        /// <summary>
        /// Loader for <see cref="MdlPartComposer"/>. Part models are flattened because the composer
        /// attaches their geometry assuming it sits at the part origin, which several SWLOR hak parts
        /// violate with in-file node offsets. The skeleton must never be flattened - its node transforms
        /// *are* the bone positions - which is exactly what the composer's withSupermodelAnims flag
        /// distinguishes. A skinned robe or cloak is the other exception: its bone transforms are the
        /// inverse-bind data needed to deform the garment into the mannequin's idle pose.
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
                if (!MdlMeshBuilder.ContainsNamedSkinWeights(model))
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
        private Domain.Gff.JsonGffStruct? LoadItemBlueprintRoot(
            string resRef,
            bool useIndexedBlueprint)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            if (useIndexedBlueprint)
            {
                return workspace.TryLoadIndexedBlueprint(ResourceType.Uti, resRef, out var indexed)
                    ? indexed.Fields
                    : null;
            }

            return workspace.TryLoadBlueprint(ResourceType.Uti, resRef, out var moduleOrIndexed)
                ? moduleOrIndexed.Fields
                : null;
        }
    }
}
