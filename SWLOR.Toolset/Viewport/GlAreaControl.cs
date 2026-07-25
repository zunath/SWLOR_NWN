using System.Numerics;
using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Silk.NET.OpenGL;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// 3D viewport for one area's <see cref="AreaScene"/>: renders tile-grid
    /// placements (batched per distinct <see cref="RenderModel"/> via <see cref="AreaDrawBatcher"/>)
    /// plus placed-instance markers, with an orbit/pan/zoom camera framed on the area's tile-grid
    /// bounds (<see cref="AreaCameraMath"/>). Follows the same <see cref="OpenGlControlBase"/> +
    /// Silk.NET.OpenGL skeleton as Radoub.UI's <c>ModelPreviewGLControl</c> (see
    /// Viewport/README.md) but is a fresh implementation tailored to a scene of many placements
    /// sharing a handful of distinct meshes, rather than one model per control.
    /// Read-only picking uses a plain left-click (press+release with &lt;4px of movement)
    /// raises <see cref="InstancePicked"/> with the hit instance (or null for empty space), using
    /// the same view/projection the last frame rendered with (<see cref="AreaCameraMath.ScreenPointToRay"/>
    /// + <see cref="AreaPicking"/>). <see cref="SelectedInstance"/> draws a wireframe highlight box
    /// around the current selection. This control still never mutates the scene or the underlying
    /// documents; editing happens through the view model's transaction paths.
    /// </summary>
    public sealed class GlAreaControl : OpenGlControlBase
    {
        static GlAreaControl()
        {
            FocusableProperty.OverrideDefaultValue<GlAreaControl>(true);
        }

        // ----- Tunables -----
        private const float VerticalFovRadians = MathF.PI / 4f; // 45 degrees - comfortable for open outdoor areas
        private const float NearPlane = 0.1f;
        private const int FloatsPerVertex = 8; // position(3) + normal(3) + texcoord(2)
        private const float PolygonHeightOffset = 0.05f; // lift trigger outlines slightly above the tile floor
        /// <summary>
        /// How fast a held middle-drag turns the view, in degrees per second.
        /// </summary>
        /// <remarks>
        /// A rate, not a per-pixel sensitivity, because Aurora's is not proportional to how far the
        /// mouse moves. Measured: a 100px drag and a 400px drag delivering the same number of motion
        /// samples rotate by the same amount (-17.7 vs -15.8 degrees), and so do a 1px-per-sample and
        /// a 10px-per-sample drag. What drives it is how long the mouse keeps moving. Aurora turns
        /// about 3.3 degrees per sample, which at its redraw rate lands near this figure - and matches
        /// its own rotate button, measured at 196 deg/s.
        /// </remarks>
        private const float OrbitYawDegreesPerSecond = 196f;

        /// <summary>
        /// The same idea for pitch, which Aurora turns far more slowly - about 0.72 degrees per motion
        /// sample against yaw's 3.3, measured as 17 degrees of elevation over a 150px vertical drag.
        /// </summary>
        private const float OrbitPitchDegreesPerSecond = 43f;

        /// <summary>Timestamp of the last orbit step, so the rate above is per second and not per event.</summary>
        private long _lastOrbitTicks;
        private const float FallbackCubeHeight = 1.5f;

        /// <summary>
        /// With "hide ceilings" on, DOWNWARD-FACING tile fragments higher than this above their own
        /// tile's base height are discarded. Per-tile rather than absolute so it stays correct in
        /// multi-elevation areas.
        /// </summary>
        /// <remarks>
        /// A plain height cut cannot do this job, which is what a 4m cut got wrong: measured over the
        /// corpus, shp02's ceiling planes sit at 3.0-3.5m while its walls top out at 3.5-4.0m, so no
        /// threshold separates the two. Cutting at 4m therefore kept the ceilings, and an area read
        /// as a field of blank grey plates - the ceilings seen from above, with the walls and floors
        /// sealed underneath them.
        ///
        /// Facing is the signal that actually distinguishes them: a ceiling faces down, a wall faces
        /// sideways, a floor or a roof faces up. So only down-facing fragments are cut, which leaves
        /// every wall standing at any height and leaves exterior terrain and building roofs
        /// (up-facing) intact - hiding those would gut an outdoor map. What else goes is the
        /// underside of overhangs, archways and catwalks, which a camera looking down cannot see.
        ///
        /// 2m rather than something taller because walls no longer need the headroom, and shp02's
        /// lowest ceiling geometry starts at 3.0m. Measured cut share: 17% of triangles for shp02,
        /// 6% for tin01, 10% for tno01, 1% for the ttd01 desert exterior.
        /// </remarks>
        private const float CeilingClipHeight = 2.0f;
        private const float CeilingClipDisabled = 1e9f;
        private const float MarkerHalfWidth = 0.4f;
        private const float MarkerHeight = 1.2f;
        private const float MarkerGroundOffset = 0.05f;

        /// <summary>Net press-to-release pointer movement (logical px) below which a left button press+release is treated as a pick click rather than a (degenerate/aborted) orbit drag.</summary>
        private const float ClickDragThresholdPixels = 4f;

        private static readonly Vector3 LightDir = Vector3.Normalize(new Vector3(0.35f, -0.5f, 0.8f));

        // Editor lighting: the area's authored ambient/diffuse colors drive hue and mood,
        // but authored night colors are near-black - too dark to edit in - so each channel is
        // lifted from a floor toward its true value (raw 0 -> floor, raw 1 -> unchanged). Tunable;
        // the human gate calibrates the feel.
        private const float AmbientLightFloor = 0.25f;
        private const float DiffuseLightFloor = 0.20f;
        private static readonly Vector3 UntexturedTileColor = new(0.6f, 0.6f, 0.6f);
        private static readonly Vector3 FallbackTileColor = new(0.95f, 0.15f, 0.55f);
        private static readonly Vector3 PolygonOverlayColor = new(1f, 0.65f, 0.15f);
        private static readonly Vector3 SelectionHighlightColor = new(1f, 0.95f, 0.2f);

        // Transform gizmo. Axis colours follow the convention every 3D tool shares (X red, Y green,
        // Z blue) so they need no legend; the ring takes the interface accent.
        private static readonly Vector3 GizmoAxisXColor = new(0.91f, 0.51f, 0.42f);
        private static readonly Vector3 GizmoAxisYColor = new(0.50f, 0.82f, 0.54f);
        private static readonly Vector3 GizmoAxisZColor = new(0.50f, 0.71f, 0.96f);
        private static readonly Vector3 GizmoRingColor = new(0.36f, 0.61f, 0.96f);
        private const float GizmoArmLength = 2.2f;
        private const float GizmoRingRadius = 1.8f;

        // Walkmesh overlay: walkable faces green, non-walkable red, drawn translucent just
        // above the floor so the tile geometry still shows through.
        private static readonly Vector3 WalkmeshWalkableColor = new(0.25f, 0.9f, 0.35f);
        private static readonly Vector3 WalkmeshBlockedColor = new(0.9f, 0.2f, 0.2f);
        private const float WalkmeshOverlayAlpha = 0.4f;
        private const float WalkmeshHeightOffset = 0.06f; // lift above the floor to avoid z-fighting (just above PolygonHeightOffset)

        // ----- GLSL source (kept inline for this renderer; adapted from, not shared with,
        // Radoub.UI's OpenGLShaderManager - this control needs an alpha-cutoff/unlit uniform that
        // control doesn't expose, and Radoub's sources must never be modified). -----
        private const string VersionEs = "#version 300 es\nprecision highp float;\n";
        private const string VersionDesktop = "#version 330 core\n";

        private const string VertexShaderBody = @"
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;

out vec3 Normal;
out vec2 TexCoord;
out vec3 WorldPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    Normal = mat3(model) * aNormal;
    TexCoord = aTexCoord;
    vec4 world = model * vec4(aPosition, 1.0);
    WorldPos = world.xyz;
    gl_Position = projection * view * world;
}
";

        private const string FragmentShaderBody = @"
out vec4 FragColor;

in vec3 Normal;
in vec2 TexCoord;
in vec3 WorldPos;

uniform sampler2D diffuseTexture;
uniform bool hasTexture;
uniform vec3 flatColor;
uniform bool unlit;
uniform float alphaCutoff;
uniform float flatAlpha;
uniform float ceilingClipZ;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambientColor;

void main()
{
    // Hide-ceilings support: DOWN-FACING fragments above the per-draw clip height are discarded
    // (the height is a huge value when the toggle is off). Walls and floors survive because they do
    // not face down; see CeilingClipHeight for why facing, not height alone, is the test.
    if (WorldPos.z > ceilingClipZ && normalize(Normal).z < -0.5)
        discard;

    vec4 texColor = hasTexture ? texture(diffuseTexture, TexCoord) : vec4(flatColor, 1.0);

    if (alphaCutoff > 0.0 && texColor.a < alphaCutoff)
        discard;

    if (unlit)
    {
        // flatAlpha defaults to 1.0 for every opaque unlit draw (markers, outlines, selection box);
        // the translucent walkmesh overlay is the only pass that lowers it.
        FragColor = vec4(texColor.rgb, flatAlpha);
        return;
    }

    vec3 norm = normalize(Normal);
    // Two-sided lighting (abs, not max) - NWN tile/prop meshes have inconsistent winding.
    float diff = abs(dot(norm, lightDir));
    vec3 result = (ambientColor + diff * lightColor) * texColor.rgb;
    FragColor = vec4(result, 1.0);
}
";

        private sealed class MeshRange
        {
            public required int IndexOffset { get; init; }
            public required int IndexCount { get; init; }
            public required Matrix4x4 MeshTransform { get; init; }
            public string? TextureName { get; init; }
        }

        private sealed class ModelBuffer
        {
            public required uint Vao { get; init; }
            public required uint Vbo { get; init; }
            public required uint Ebo { get; init; }
            public required IReadOnlyList<MeshRange> MeshRanges { get; init; }
        }

        private readonly struct StaticMeshBuffer
        {
            public StaticMeshBuffer(uint vao, uint vbo, uint ebo, int indexCount)
            {
                Vao = vao;
                Vbo = vbo;
                Ebo = ebo;
                IndexCount = indexCount;
            }

            public uint Vao { get; }
            public uint Vbo { get; }
            public uint Ebo { get; }
            public int IndexCount { get; }
        }

        private GL? _gl;
        private uint _shaderProgram;

        private readonly Dictionary<RenderModel, ModelBuffer> _modelBuffers = new();
        private readonly Dictionary<string, (uint TexId, float AlphaCutoff)> _textureCache =
            new(StringComparer.OrdinalIgnoreCase);

        // Memoize the raw-mesh-texture-name -> resolved result so the per-draw path
        // (thousands of BindMeshTexture calls per frame) skips MaterialResolver's string resolution.
        // Points at the same GL texture ids as _textureCache; cleared alongside it on GL teardown.
        private readonly Dictionary<string, (uint TexId, float AlphaCutoff)> _rawTextureCache =
            new(StringComparer.OrdinalIgnoreCase);

        private StaticMeshBuffer? _fallbackCubeBuffer;
        private StaticMeshBuffer? _markerMeshBuffer;

        private uint _polygonVao;
        private uint _polygonVbo;
        private bool _hasPolygonBuffer;
        private List<(int Start, int Count)> _polygonRanges = new();

        // Walkmesh overlay: one VBO of world-space triangles, walkable faces first then
        // blocked faces, drawn as two flat-colored translucent ranges.
        private uint _walkmeshVao;
        private uint _walkmeshVbo;
        private bool _hasWalkmeshBuffer;
        private int _walkmeshWalkableVertexCount;
        private int _walkmeshBlockedVertexCount;

        private uint _highlightVao;
        private uint _highlightVbo;
        private bool _hasHighlightBuffer;

        private sealed record SceneState(AreaScene? Scene, long Version);

        private SceneState _sceneState = new(null, 0);
        private long _nextSceneVersion;
        private long _renderedSceneVersion = -1;
        private IReadOnlyList<AreaDrawBatcher.TileBatch>? _tileBatches;

        private int _viewportWidth;
        private int _viewportHeight;

        /// <summary>View/projection from the most recently rendered frame - kept for picking (<see cref="RaiseInstancePicked"/>), which runs on a click rather than every frame.</summary>
        private Matrix4x4 _lastView = Matrix4x4.Identity;
        private Matrix4x4 _lastProjection = Matrix4x4.Identity;

        /// <summary>Combined view*projection from the current frame, used for per-tile frustum culling.</summary>
        private Matrix4x4 _viewProjection = Matrix4x4.Identity;

        // ----- Orbit camera state -----
        private Vector3 _target;
        private float _azimuth = MathF.PI * 1.25f;
        private float _elevation = AreaCameraMath.DefaultElevationRadians;
        private float _distance = 50f;
        private float _initialDistance = 50f;

        /// <summary>Whether this control has ever framed a scene - see the <c>Scene</c> setter.</summary>
        private bool _cameraFramed;

        private enum DragMode { None, Orbit, Pan, Move, Rotate, Select }
        private DragMode _dragMode = DragMode.None;
        private Point _lastPointerPos;

        // ----- Picking -----
        private Point _pressStartPos;
        private bool _isClickCandidate;
        private InstanceMarker? _selectedInstance;

        // ----- Move/rotate gizmo -----
        private InstanceMarker? _manipulationOriginal;
        private InstanceMarker? _manipulationPreview;
        private float _manipulationHeadingRadians;
        private bool _manipulationCancelled;

        // ----- Place-from-palette -----
        private bool _isPlacementActive;

        /// <summary>
        /// The instance to draw a highlight box around, or null for no highlight. Set by the host
        /// view when list-driven selection changes (or after a 3D-view pick, for symmetry); this
        /// control itself never changes this property - see <see cref="InstancePicked"/>.
        /// </summary>
        public InstanceMarker? SelectedInstance
        {
            get => _selectedInstance;
            set
            {
                if (ReferenceEquals(_selectedInstance, value))
                    return;

                _selectedInstance = value;
                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// Raised when a plain left-click (press+release with under <see cref="ClickDragThresholdPixels"/>
        /// of net movement - a drag stays camera orbit) lands on the viewport: the hit instance, or
        /// null when the click landed on empty space. This control does not select/highlight the hit
        /// itself - the host view is expected to set <see cref="SelectedInstance"/> (usually after
        /// also syncing the corresponding instance-list row) so 3D-view and list-view selection stay
        /// driven from one place.
        /// </summary>
        public event Action<InstanceMarker?>? InstancePicked;

        /// <summary>
        /// Raised once a move-gizmo drag releases: the instance that was selected when the
        /// drag started, and its final world position (Z unchanged - the move gizmo only edits X/Y).
        /// Not raised when the drag ended with no net change (e.g. a press+release with no motion).
        /// The host view is expected to commit this through the matching instance-list section's
        /// InstanceFieldMap.SetPosition path as one transaction, then refresh the scene.
        /// </summary>
        public event Action<InstanceMarker, Vector3>? InstanceMoved;

        /// <summary>Mirrors <see cref="InstanceMoved"/> for the rotate gizmo: the instance and its final (cos,sin) heading.</summary>
        public event Action<InstanceMarker, Vector2>? InstanceRotated;

        /// <summary>
        /// Live during a manipulation drag: the instance as it stands right now, and where it started.
        /// Both null once the drag ends or is cancelled. Drives the readout beside the map, which is
        /// where a coordinate belongs - visible while you are changing it and gone afterwards.
        /// </summary>
        public event Action<InstanceMarker?, InstanceMarker?>? ManipulationPreviewChanged;

        /// <summary>
        /// Whether a viewport click should place a new instance instead of picking/orbiting in the
        /// "Place..." flow. The host view sets this once a palette blueprint has been chosen;
        /// this control clears it itself once a placement point is picked or cancelled.
        /// </summary>
        public bool IsPlacementActive
        {
            get => _isPlacementActive;
            set => _isPlacementActive = value;
        }

        /// <summary>Raised when placement mode is active and a plain click (not a camera drag) lands in the viewport: the world-space ground point (ray intersected with the Z=0 plane). Clears <see cref="IsPlacementActive"/> before raising.</summary>
        public event Action<Vector3>? PlacementPointPicked;

        /// <summary>Raised when an active placement is cancelled (Esc, or a right-click while placement is active). Clears <see cref="IsPlacementActive"/> before raising.</summary>
        public event Action? PlacementCancelled;

        private InstanceMarker? _placementGhost;

        /// <summary>
        /// What to draw under the cursor while <see cref="IsPlacementActive"/>: the object about to
        /// be placed, rendered translucently at the ground point the pointer is over. Its own
        /// Position is ignored - the cursor supplies that - so the host sets this once when arming
        /// placement rather than rebuilding a marker on every mouse move.
        /// </summary>
        public InstanceMarker? PlacementGhost
        {
            get => _placementGhost;
            set
            {
                if (ReferenceEquals(_placementGhost, value))
                    return;

                _placementGhost = value;
                _ghostPosition = null;
                RequestNextFrameRendering();
            }
        }

        /// <summary>Where the ghost sits right now, or null before the pointer has been over the map.</summary>
        private Vector3? _ghostPosition;

        // ----- Paint-tiles-from-palette -----
        //
        // The tile palette arms a tile (or a named multi-tile group) and the next viewport click
        // names the grid cell to stamp it into. Only the cell travels back to the host: the tile
        // grid itself is the area document's business, not this control's.
        //
        // PRECEDENCE: the two palette modes are mutually exclusive by construction (each palette tab
        // disarms the other), but if both flags are somehow on, object placement wins everywhere -
        // input, drawing and cancel - so the two never half-apply. That choice is arbitrary; having
        // one is not.

        private bool _isTilePlacementActive;
        private (int Columns, int Rows) _tilePlacementFootprint = (1, 1);

        /// <summary>The grid cell the pointer is over, or null before the pointer has been over the map. May be outside the grid - the highlight is what says so.</summary>
        private (int Column, int Row)? _tileHoverCell;

        /// <summary>
        /// Whether a viewport click should report a grid cell to stamp a tile into instead of
        /// picking an instance. The host view sets this once a palette tile/group has been chosen;
        /// this control clears it itself once a cell is picked or the placement is cancelled.
        /// </summary>
        public bool IsTilePlacementActive
        {
            get => _isTilePlacementActive;
            set
            {
                if (_isTilePlacementActive == value)
                    return;

                _isTilePlacementActive = value;
                // Drop the stale hover cell and repaint: disarming from the palette has to take the
                // cell highlight off the map, and re-arming must not flash the previous cursor
                // position before the pointer moves again.
                _tileHoverCell = null;
                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// The footprint in cells that the armed palette entry will write - (1,1) for a single tile,
        /// larger for a group. The clicked cell is the footprint's BOTTOM-LEFT corner (its lowest
        /// column and row) and the footprint extends toward increasing column and row; the highlight
        /// and <see cref="TileCellPicked"/> share that convention so what a builder sees painted is
        /// exactly what the host writes.
        /// </summary>
        public (int Columns, int Rows) TilePlacementFootprint
        {
            get => _tilePlacementFootprint;
            // A zero or negative extent would paint nothing while still reporting cells, so a
            // malformed group definition degrades to a single cell instead of an invisible cursor.
            set
            {
                var clamped = (Columns: Math.Max(1, value.Columns), Rows: Math.Max(1, value.Rows));
                if (_tilePlacementFootprint == clamped)
                    return;

                _tilePlacementFootprint = clamped;
                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// Raised when tile placement is active and a plain click (not a camera drag) lands in the
        /// viewport: the anchor cell's column and row (the footprint's bottom-left - see
        /// <see cref="TilePlacementFootprint"/>). Clears <see cref="IsTilePlacementActive"/> before
        /// raising. Not raised when the footprint would not fit inside the area grid, since the host
        /// has no way to tell a rejected stamp from a legal one.
        /// </summary>
        public event Action<int, int>? TileCellPicked;

        private IReadOnlyList<RenderModel?> _tilePlacementModels = Array.Empty<RenderModel?>();

        /// <summary>
        /// The armed stamp's tile models, row-major over <see cref="TilePlacementFootprint"/>. Empty
        /// (or a null slot) falls back to the plain cell outline for that cell.
        /// </summary>
        public IReadOnlyList<RenderModel?> TilePlacementModels
        {
            get => _tilePlacementModels;
            set
            {
                _tilePlacementModels = value ?? Array.Empty<RenderModel?>();
                RequestNextFrameRendering();
            }
        }

        /// <summary>Raised when an active tile placement is cancelled (Esc, or a right-click while it is armed). Clears <see cref="IsTilePlacementActive"/> before raising.</summary>
        public event Action? TilePlacementCancelled;

        /// <summary>Raised when the builder presses R with a tile armed, to turn it a quarter turn.</summary>
        public event Action? TileRotateRequested;

        /// <summary>
        /// Discards tile geometry above each tile's own base height + ~4m (interior ceilings).
        /// </summary>
        /// <remarks>
        /// On by default. An interior tileset's ceiling sits between the camera and everything a builder
        /// came to edit, so the first thing anyone did with the old toggle was turn it on - which makes it
        /// the default, not an option. There is no UI for it now; the value is what the editor wants.
        /// </remarks>
        private bool _hideCeilings = true;

        public bool HideCeilings
        {
            get => _hideCeilings;
            set
            {
                if (_hideCeilings == value)
                    return;

                _hideCeilings = value;
                RequestNextFrameRendering();
            }
        }

        private bool _showWalkmesh;

        /// <summary>
        /// When true, draws each tile's walkmesh as a translucent overlay (green walkable / red
        /// blocked faces) just above the floor - the visual for the walkmesh feature. Off by
        /// default; tiles without a resolved walkmesh simply contribute nothing.
        /// </summary>
        public bool ShowWalkmesh
        {
            get => _showWalkmesh;
            set
            {
                if (_showWalkmesh == value)
                    return;

                _showWalkmesh = value;
                RequestNextFrameRendering();
            }
        }

        /// <summary>True when this instance should draw its resolved model rather than a marker.</summary>
        private static bool DrawsAsModel(InstanceMarker instance) => instance.Model != null;

        /// <summary>Layered resource index used to resolve tile/mesh textures and MTR materials. Null degrades every mesh to a flat gray fallback.</summary>
        public ResourceIndex? ResourceIndex { get; set; }

        /// <summary>
        /// The scene to render, or null to show an empty viewport. The camera is framed to the
        /// area only on the FIRST non-null scene (initial load). Later assignments are edit-driven
        /// rebuilds of the same area (move/rotate/place/undo/redo, or the manual Rebuild) - those
        /// keep the user's current orbit/zoom rather than snapping back to the default framing.
        /// Always marks GPU state for rebuild on the next render.
        /// </summary>
        public AreaScene? Scene
        {
            get => Volatile.Read(ref _sceneState).Scene;
            set
            {
                var version = Interlocked.Increment(ref _nextSceneVersion);
                Volatile.Write(ref _sceneState, new SceneState(value, version));

                // Framed once per control, not once per non-null scene. The host clears the scene
                // while it rebuilds, so keying off "there was no scene a moment ago" re-framed the
                // camera after every rebuild and threw away the orbit and zoom the builder had set.
                if (value != null && !_cameraFramed)
                {
                    _cameraFramed = true;
                    ResetCameraForScene(value);
                }

                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// Raised on GL init (success or failure) and whenever a render-time error occurs. An
        /// empty string means "no issue to report"; anything else is a human-readable status the
        /// host view should surface (e.g. "3D view unavailable: ..."). Never throws past this
        /// control's own boundary - GL problems degrade to a message instead of crashing the app.
        /// </summary>
        public event EventHandler<string>? RenderStatusChanged;

        /// <summary>
        /// Raises <see cref="RenderStatusChanged"/>, marshaling to the UI thread when called from
        /// the GL render thread (OnOpenGlInit/OnOpenGlRender do not run on the UI thread - mirrors
        /// how Radoub.UI's ModelPreviewGLControl posts its own state-changed events).
        /// </summary>
        private void RaiseStatus(string message)
        {
            if (Dispatcher.UIThread.CheckAccess())
                RenderStatusChanged?.Invoke(this, message);
            else
                Dispatcher.UIThread.Post(() => RenderStatusChanged?.Invoke(this, message));
        }

        private void ResetCameraForScene(AreaScene scene)
        {
            var aspect = _viewportWidth > 0 && _viewportHeight > 0
                ? (float)_viewportWidth / _viewportHeight
                : 1.5f;

            var (target, distance) = AreaCameraMath.ComputeInitialFraming(
                scene.Width, scene.Height, AreaSceneBuilder.TileSize, VerticalFovRadians, aspect);

            _target = target;
            _distance = distance;
            _initialDistance = distance;
            _azimuth = MathF.PI * 1.25f;
            _elevation = AreaCameraMath.DefaultElevationRadians;
        }

        // ----- Pointer input: middle orbits, middle+left pans, wheel zooms -----
        //
        // OpenGlControlBase has no Background brush, so pointer events never hit-test to this
        // control directly (same Avalonia limitation Radoub's ModelPreviewGLControl documents).
        // The hosting view overlays a transparent input Border and forwards its events into the
        // public Handle* methods below; the On* overrides remain as a fallback if the control is
        // ever hosted without the overlay.

        public void HandlePointerPressed(PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            var shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
            var alt = (e.KeyModifiers & KeyModifiers.Alt) != 0;
            var pos = e.GetPosition(this);

            // In placement mode, a right-click cancels rather than panning the camera.
            if (_isPlacementActive && props.IsRightButtonPressed)
            {
                CancelPlacement();
                e.Handled = true;
                return;
            }

            // Same bargain for an armed tile: while a stamp is pending the right button is the way
            // out of it. Outside placement the right button does nothing in the viewport, matching
            // Aurora.
            if (_isTilePlacementActive && props.IsRightButtonPressed)
            {
                CancelTilePlacement();
                e.Handled = true;
                return;
            }

            // A camera drag already owns the pointer, so this press is the builder adding or
            // swapping a button mid-drag - middle+left is pan, middle alone is orbit. Resolving it
            // here rather than falling through matters: the left press would otherwise be read as a
            // grab and start dragging whatever the cursor happens to be over.
            if (_dragMode is DragMode.Orbit or DragMode.Pan)
            {
                _dragMode = CameraDragFor(props, shift) ?? _dragMode;
                _lastPointerPos = pos;
                e.Handled = true;
                return;
            }

            // For the move/rotate gizmo, a plain left press landing ON the current selection
            // starts an object-manipulation drag - the left button is the primary "grab", matching
            // modern editors where you left-drag an object to move it (Alt to rotate). Hit-test the
            // press against the selection first; any other press (empty space, shift, or another
            // button) falls through to the camera navigation below. An armed palette entry (object or
            // tile) suspends the grab: while something is waiting to be placed, the left button
            // belongs to placing it, and the gizmo comes back untouched the moment nothing is armed.
            if (!_isPlacementActive && !_isTilePlacementActive && props.IsLeftButtonPressed && !shift
                && _selectedInstance != null && TryHitSelection(pos, alt) is { } grabbed)
            {
                BeginManipulation(_selectedInstance, grabbed);
                _lastPointerPos = pos;
                _pressStartPos = pos;
                _isClickCandidate = false;
                Focus();
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }

            // Aurora's mapping, measured against the toolset itself with each case driven from the
            // same starting view and undone afterwards: middle alone orbits, middle and left together
            // pan, the wheel zooms, and left alone belongs to the objects. Right does nothing.
            if (CameraDragFor(props, shift) is { } cameraDrag)
                _dragMode = cameraDrag;
            else if (props.IsLeftButtonPressed)
                _dragMode = DragMode.Select;
            else
                return;

            _lastPointerPos = pos;
            _pressStartPos = pos;
            // Only a plain left press is eligible to resolve into a pick click on release; a press that
            // started a camera drag never picks.
            _isClickCandidate = _dragMode == DragMode.Select;
            Focus();
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        /// <summary>
        /// Which camera drag the current buttons ask for, or null when they ask for none.
        /// </summary>
        /// <remarks>
        /// Middle and left together pan, middle alone orbits - Aurora's arrangement. Shift+left also
        /// orbits, as the path for a trackpad with no middle button.
        /// </remarks>
        private static DragMode? CameraDragFor(Avalonia.Input.PointerPointProperties props, bool shift)
        {
            if (props.IsMiddleButtonPressed)
                return props.IsLeftButtonPressed ? DragMode.Pan : DragMode.Orbit;

            return props.IsLeftButtonPressed && shift ? DragMode.Orbit : null;
        }

        public void HandlePointerMoved(PointerEventArgs e)
        {
            // The ghost must track the pointer with no button held - the one case the drag-mode
            // guard below rejects - so it is updated first and independently of it.
            if (_isPlacementActive && _placementGhost != null)
                UpdatePlacementGhost(e.GetPosition(this));

            // The cell highlight is a cursor too, and tracks with no button held for the same reason.
            if (_isTilePlacementActive && !_isPlacementActive)
                UpdateTileHoverCell(e.GetPosition(this));

            if (_dragMode == DragMode.None)
                return;

            // Re-read the buttons every move rather than trusting what the drag started as: pressing
            // or releasing the second button part-way through a drag switches between orbit and pan,
            // which is how it behaves in Aurora.
            if (_dragMode is DragMode.Orbit or DragMode.Pan or DragMode.Select &&
                CameraDragFor(e.GetCurrentPoint(this).Properties,
                    (e.KeyModifiers & KeyModifiers.Shift) != 0) is { } liveDrag)
                _dragMode = liveDrag;

            var pos = e.GetPosition(this);
            var dx = (float)(pos.X - _lastPointerPos.X);
            var dy = (float)(pos.Y - _lastPointerPos.Y);
            _lastPointerPos = pos;

            switch (_dragMode)
            {
                case DragMode.Orbit:
                {
                    // Only the direction of the movement is read, never its size - see
                    // OrbitYawDegreesPerSecond. The elapsed time carries the magnitude, so the turn
                    // rate is the same whatever rate the pointer events arrive at.
                    var now = System.Diagnostics.Stopwatch.GetTimestamp();
                    var seconds = _lastOrbitTicks == 0
                        ? 0.0
                        : (now - _lastOrbitTicks) / (double)System.Diagnostics.Stopwatch.Frequency;
                    _lastOrbitTicks = now;

                    // A stalled frame must not bank up into one huge jump.
                    var step = (float)Math.Clamp(seconds, 0.0, 0.05);

                    if (dx != 0f)
                        _azimuth += MathF.Sign(dx) * OrbitYawDegreesPerSecond * DegreesToRadians * step;
                    if (dy != 0f)
                        _elevation = AreaCameraMath.ClampElevation(
                            _elevation - MathF.Sign(dy) * OrbitPitchDegreesPerSecond * DegreesToRadians * step);
                    break;
                }

                case DragMode.Pan:
                    // Grab and drag: the scene follows the cursor one-for-one, so whatever is under the
                    // pointer stays under it. Confirmed both ways in Aurora - by eye, watching a grass
                    // patch travel right with a rightward drag, and by measurement, 200px of cursor
                    // moving the scene 0.93-1.10 screen px per cursor px across several trials.
                    var worldPerPixel = AreaCameraMath.WorldUnitsPerPixel(_distance, VerticalFovRadians, _viewportHeight);
                    _target += AreaCameraMath.PanDelta(_azimuth, dx, dy, worldPerPixel);
                    break;

                case DragMode.Move:
                    UpdateMovePreview(pos, (e.KeyModifiers & KeyModifiers.Control) != 0);
                    break;

                case DragMode.Rotate:
                    UpdateRotatePreview(dx);
                    break;

                case DragMode.Select:
                    // Nothing follows the pointer: the press is only waiting to become a click.
                    break;
            }

            RequestNextFrameRendering();
        }

        public void HandlePointerReleased(PointerReleasedEventArgs e)
        {
            if (_dragMode == DragMode.Move || _dragMode == DragMode.Rotate)
            {
                CommitManipulation();
                e.Pointer.Capture(null);
                e.Handled = true;
                return;
            }

            if (_dragMode == DragMode.None)
                return;

            var releasePos = e.GetPosition(this);
            var wasClickCandidate = _isClickCandidate;

            _dragMode = DragMode.None;
            _lastOrbitTicks = 0;
            _isClickCandidate = false;
            e.Pointer.Capture(null);
            e.Handled = true;

            if (!wasClickCandidate)
                return;

            var dx = releasePos.X - _pressStartPos.X;
            var dy = releasePos.Y - _pressStartPos.Y;
            var dragDistance = Math.Sqrt(dx * dx + dy * dy);

            if (dragDistance >= ClickDragThresholdPixels)
                return;

            if (_isPlacementActive)
                RaisePlacementPointPicked(releasePos);
            else if (_isTilePlacementActive)
                RaiseTileCellPicked(releasePos);
            else
                RaiseInstancePicked(releasePos);
        }

        /// <summary>
        /// Resolves a click's screen position to a hit instance (or null for empty space) using the
        /// view/projection from the most recently rendered frame, and raises
        /// <see cref="InstancePicked"/>. A no-op (raises with null) before the first frame has ever
        /// rendered a scene, since there is nothing to hit yet.
        /// </summary>
        private void RaiseInstancePicked(Point screenPos)
        {
            var scene = Volatile.Read(ref _sceneState).Scene;
            if (scene == null || _viewportWidth <= 0 || _viewportHeight <= 0)
            {
                InstancePicked?.Invoke(null);
                return;
            }

            var ray = AreaCameraMath.ScreenPointToRay(
                new Vector2((float)screenPos.X, (float)screenPos.Y),
                _viewportWidth, _viewportHeight, _lastView, _lastProjection);

            // Always true now that the marker-instead-of-model switch is gone: picking has to agree with
            // drawing, and everything with geometry draws as its model.
            var hit = AreaPicking.PickClosestInstance(ray, scene, showPlaceableModels: true);
            InstancePicked?.Invoke(hit);
        }

        // ----- Move/rotate gizmo -----

        /// <summary>Builds a ray from the last rendered frame's view/projection for the given screen point, or null before anything has ever rendered.</summary>
        private PickRay? TryBuildRay(Point screenPos)
        {
            if (_viewportWidth <= 0 || _viewportHeight <= 0)
                return null;

            return AreaCameraMath.ScreenPointToRay(
                new Vector2((float)screenPos.X, (float)screenPos.Y), _viewportWidth, _viewportHeight, _lastView, _lastProjection);
        }

        /// <summary>
        /// What a press at <paramref name="screenPos"/> grabs on the current selection: an axis arm
        /// moves, the rotation ring turns, and the object's own body does whichever
        /// <paramref name="alt"/> asks for. Null when the press missed all three.
        /// </summary>
        /// <remarks>
        /// The handles are tested first and on their own geometry. They are drawn well outside most
        /// objects' bounds, so testing only the body meant a press on a visible arm or ring fell
        /// through to camera panning - the gizmo could be seen but not grabbed.
        /// </remarks>
        private DragMode? TryHitSelection(Point screenPos, bool alt)
        {
            if (Volatile.Read(ref _sceneState).Scene == null ||
                _selectedInstance is not { } selected)
                return null;

            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return null;

            switch (GizmoPicking.Pick(
                        ray.Value, Displayed(selected).Position,
                        GizmoArmLength, GizmoRingRadius, GizmoGrabTolerance()))
            {
                case GizmoHandle.Axis:
                    return DragMode.Move;
                case GizmoHandle.Ring:
                    return DragMode.Rotate;
            }

            return AreaPicking.PickInstance(ray.Value, selected, DrawsAsModel(selected)) != null
                ? alt ? DragMode.Rotate : DragMode.Move
                : null;
        }

        /// <summary>
        /// How near a press has to pass a handle, in world units.
        /// </summary>
        /// <remarks>
        /// Scaled with camera distance because the handles are a fixed world size: at a wide zoom an
        /// arm covers a couple of pixels, and a fixed world tolerance would make it effectively
        /// impossible to hit. Clamped at both ends so it never shrinks below a usable grab or grows
        /// wide enough to swallow presses meant for the object itself.
        /// </remarks>
        private float GizmoGrabTolerance() => Math.Clamp(_distance * 0.02f, 0.15f, 1.0f);

        private void BeginManipulation(InstanceMarker selected, DragMode mode)
        {
            _dragMode = mode;
            _manipulationOriginal = selected;
            _manipulationPreview = ClonePreview(selected, selected.Position, selected.Orientation);
            _manipulationHeadingRadians = MathF.Atan2(selected.Orientation.Y, selected.Orientation.X);
            _manipulationCancelled = false;
        }

        private static InstanceMarker ClonePreview(InstanceMarker source, Vector3 position, Vector2 orientation) => new()
        {
            Kind = source.Kind,
            TemplateResRef = source.TemplateResRef,
            Tag = source.Tag,
            Position = position,
            Orientation = orientation,
            VisualTransform = source.VisualTransform,
            Geometry = source.Geometry,
            Model = source.Model
        };

        /// <summary>
        /// Live move preview: follows the destination floor, optionally grid-snapping X/Y while Ctrl is
        /// held.
        /// </summary>
        /// <remarks>
        /// The height comes from the walkmesh under the cursor, the same source new placement already
        /// uses. Holding the instance's original Z instead - which is what this did - left anything
        /// dragged onto a slope or a different elevation floating above its new floor or buried under it.
        /// The original Z is still the fallback for a drag that leaves the walkmesh entirely, since
        /// there is nothing better to put it on.
        /// </remarks>
        private void UpdateMovePreview(Point screenPos, bool snap)
        {
            // Esc already cancelled this drag (_manipulationPreview cleared) - a mouse move that
            // arrives before the button is actually released must not revive the preview.
            if (_manipulationCancelled || _manipulationOriginal is not { } original)
                return;

            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return;

            var scene = Scene;
            var ground = scene != null ? AreaWalkmesh.RaycastGround(ray.Value, scene) : null;

            var hit = ground
                ?? AreaManipulation.IntersectRayWithHorizontalPlane(ray.Value, original.Position.Z);

            if (hit is not { } target)
                return; // Ray parallel to the plane this frame - keep the previous preview rather than snapping to a bogus point.

            var position = snap ? AreaManipulation.SnapToGridXy(target, AreaManipulation.DefaultGridSnapMeters) : target;

            // Off the walkmesh there is no floor to sit on, so the instance keeps the height it had.
            var z = ground != null ? position.Z : original.Position.Z;
            _manipulationPreview = ClonePreview(original, new Vector3(position.X, position.Y, z), original.Orientation);
            ManipulationPreviewChanged?.Invoke(original, _manipulationPreview);
        }

        /// <summary>Live rotate preview: accumulates heading from horizontal drag movement, matching the orbit camera's own pixel-to-radians feel.</summary>
        private void UpdateRotatePreview(float dxPixels)
        {
            if (_manipulationCancelled || _manipulationOriginal is not { } original)
                return;

            _manipulationHeadingRadians += dxPixels * AreaManipulation.RotateRadiansPerPixel;
            var orientation = AreaManipulation.HeadingToOrientation(_manipulationHeadingRadians);
            _manipulationPreview = ClonePreview(original, original.Position, orientation);
            ManipulationPreviewChanged?.Invoke(original, _manipulationPreview);
        }

        /// <summary>Ends the active manipulation drag, raising <see cref="InstanceMoved"/>/<see cref="InstanceRotated"/> once for the net change - or nothing if the drag was cancelled (Esc) or ended with no actual change (e.g. a press+release with no motion on an already-selected instance).</summary>
        private void CommitManipulation()
        {
            var mode = _dragMode;
            var original = _manipulationOriginal;
            var preview = _manipulationPreview;
            var cancelled = _manipulationCancelled;

            _dragMode = DragMode.None;
            _manipulationOriginal = null;
            _manipulationPreview = null;
            _manipulationCancelled = false;
            ManipulationPreviewChanged?.Invoke(null, null);
            RequestNextFrameRendering();

            if (cancelled || original == null || preview == null)
                return;

            if (mode == DragMode.Move && Vector3.Distance(preview.Position, original.Position) > 1e-5f)
                InstanceMoved?.Invoke(original, preview.Position);
            else if (mode == DragMode.Rotate && Vector2.Distance(preview.Orientation, original.Orientation) > 1e-5f)
                InstanceRotated?.Invoke(original, preview.Orientation);
        }

        /// <summary>The instance actually rendered/highlighted for <paramref name="instance"/> right now - its live manipulation preview while a drag is in progress on it, otherwise itself.</summary>
        private InstanceMarker Displayed(InstanceMarker instance) =>
            _manipulationPreview != null && ReferenceEquals(instance, _manipulationOriginal) ? _manipulationPreview : instance;

        // ----- Place-from-palette -----

        private void CancelPlacement()
        {
            if (!_isPlacementActive)
                return;

            _isPlacementActive = false;
            _ghostPosition = null;
            RequestNextFrameRendering();
            PlacementCancelled?.Invoke();
        }

        /// <summary>
        /// Moves the ghost to the ground point under the cursor, using the same
        /// walkmesh-then-flat-plane chain the placement click itself uses - so where the ghost
        /// appears is where the object will actually land, elevated tiles included.
        /// </summary>
        private void UpdatePlacementGhost(Point screenPos)
        {
            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return;

            var scene = Volatile.Read(ref _sceneState).Scene;
            var point = (scene != null ? AreaWalkmesh.RaycastGround(ray.Value, scene) : null)
                        ?? AreaManipulation.IntersectRayWithHorizontalPlane(ray.Value, 0f);
            if (point is not { } hit)
                return;

            _ghostPosition = hit;
            RequestNextFrameRendering();
        }

        private void RaisePlacementPointPicked(Point screenPos)
        {
            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return;

            // Snap the new instance onto the real walkmesh floor under the cursor (its Z
            // then matches in-game ground, including on elevated tiles). Areas/tiles with no
            // resolvable .wok fall back to the flat Z=0 ground plane the pre-6.1 flow always used.
            var scene = Volatile.Read(ref _sceneState).Scene;
            var point = (scene != null ? AreaWalkmesh.RaycastGround(ray.Value, scene) : null)
                        ?? AreaManipulation.IntersectRayWithHorizontalPlane(ray.Value, 0f);
            if (point is not { } hit)
                return;

            _isPlacementActive = false;
            _ghostPosition = null;
            PlacementPointPicked?.Invoke(hit);
        }

        // ----- Paint-tiles-from-palette -----

        private void CancelTilePlacement()
        {
            if (!_isTilePlacementActive)
                return;

            _isTilePlacementActive = false;
            _tileHoverCell = null;
            RequestNextFrameRendering();
            TilePlacementCancelled?.Invoke();
        }

        /// <summary>
        /// Moves the cell highlight to the grid cell under the cursor, resolving the ground point the
        /// same walkmesh-then-flat-plane way the object ghost does - on an elevated tile the flat
        /// plane is metres past the floor the builder is actually looking at, which would highlight
        /// the wrong cell.
        /// </summary>
        private void UpdateTileHoverCell(Point screenPos)
        {
            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return;

            var scene = Volatile.Read(ref _sceneState).Scene;
            var point = (scene != null ? AreaWalkmesh.RaycastGround(ray.Value, scene) : null)
                        ?? AreaManipulation.IntersectRayWithHorizontalPlane(ray.Value, 0f);
            if (point is not { } hit)
                return;

            var cell = WorldPointToCell(hit);
            if (_tileHoverCell == cell)
                return; // Most mouse moves stay inside the same 10m cell; only a crossing changes the picture.

            _tileHoverCell = cell;
            RequestNextFrameRendering();
        }

        private void RaiseTileCellPicked(Point screenPos)
        {
            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return;

            var scene = Volatile.Read(ref _sceneState).Scene;
            if (scene == null)
                return;

            var point = AreaWalkmesh.RaycastGround(ray.Value, scene)
                        ?? AreaManipulation.IntersectRayWithHorizontalPlane(ray.Value, 0f);
            if (point is not { } hit)
                return;

            var (column, row) = WorldPointToCell(hit);

            // A stamp that would run off the grid is refused here rather than reported: the host only
            // learns the anchor cell, so it could not tell a clipped write from a clean one. Placement
            // stays armed so the next click can land somewhere it fits.
            if (!FootprintFitsGrid(scene, column, row))
                return;

            _isTilePlacementActive = false;
            _tileHoverCell = null;
            RequestNextFrameRendering();
            TileCellPicked?.Invoke(column, row);
        }

        /// <summary>The grid cell containing a world point. Floor, not truncate - a point west or south of the grid origin belongs to a negative cell, and truncation would fold two cells onto index 0.</summary>
        private static (int Column, int Row) WorldPointToCell(Vector3 world) => (
            (int)MathF.Floor(world.X / AreaSceneBuilder.TileSize),
            (int)MathF.Floor(world.Y / AreaSceneBuilder.TileSize));

        /// <summary>Whether the armed footprint, anchored bottom-left at the given cell, lies entirely inside the area's tile grid.</summary>
        private bool FootprintFitsGrid(AreaScene scene, int column, int row) =>
            column >= 0 && row >= 0 &&
            column + _tilePlacementFootprint.Columns <= scene.Width &&
            row + _tilePlacementFootprint.Rows <= scene.Height;

        // ----- Button-driven camera nudges -----

        /// <summary>
        /// One frame of camera-button motion, fired about sixty times a second while a button is held.
        /// </summary>
        /// <remarks>
        /// Per-frame amounts at the pad's ~60Hz repeat, chosen so the products match what Aurora's own
        /// buttons do: 2025 px/s of pan (measured over a 150ms press), 196 deg/s of rotation (over
        /// 300ms), and 2.7x of zoom per second. Pan is sized in screen terms rather than world units so
        /// it feels the same zoomed into a doorway or looking at the whole area.
        /// </remarks>
        private const float PanStepPixels = 2025f / PadStepsPerSecond;

        private const float OrbitStepRadians = 196f * DegreesToRadians / PadStepsPerSecond;

        /// <summary>
        /// Aurora's zoom button is not a constant rate: a 150ms press gives 1.415x and a 300ms press
        /// 1.640x, which fits an initial 1.22x on press followed by about 2.7x per second held.
        /// </summary>
        private const float ZoomStepFactor = 1.0161f;   // 2.7^(1/60)

        private const float ZoomPressFactor = 1.22f;

        /// <summary>Matches the RepeatButton interval in the theme.</summary>
        private const float PadStepsPerSecond = 60f;

        private const float DegreesToRadians = MathF.PI / 180f;

        /// <summary>Slides the view across the ground plane, in units of <see cref="PanStepPixels"/>.</summary>
        public void NudgePan(float rightSteps, float upSteps)
        {
            var worldPerPixel = AreaCameraMath.WorldUnitsPerPixel(_distance, VerticalFovRadians, _viewportHeight);
            _target += AreaCameraMath.PanDelta(
                _azimuth, rightSteps * PanStepPixels, upSteps * PanStepPixels, worldPerPixel);

            RequestNextFrameRendering();
        }

        /// <summary>Turns the view around the point it is looking at.</summary>
        public void NudgeOrbit(float azimuthSteps, float elevationSteps)
        {
            _azimuth += azimuthSteps * OrbitStepRadians;
            _elevation = AreaCameraMath.ClampElevation(_elevation + elevationSteps * OrbitStepRadians);

            RequestNextFrameRendering();
        }

        private long _lastZoomTicks;

        /// <summary>Moves the view closer (positive) or further away (negative).</summary>
        /// <remarks>
        /// The first step of a press is the larger <see cref="ZoomPressFactor"/>, the rest the held
        /// rate - Aurora's own button behaves that way, and it is what makes a single click do
        /// something visible instead of nothing. A new press is recognised by the gap since the last
        /// step, which needs no cooperation from the button itself.
        /// </remarks>
        public void NudgeZoom(int steps)
        {
            var now = System.Diagnostics.Stopwatch.GetTimestamp();
            var gap = _lastZoomTicks == 0
                ? double.MaxValue
                : (now - _lastZoomTicks) / (double)System.Diagnostics.Stopwatch.Frequency;
            _lastZoomTicks = now;

            var perStep = gap > 0.15 ? ZoomPressFactor : ZoomStepFactor;
            _distance = AreaCameraMath.ClampDistance(
                _distance * MathF.Pow(perStep, -steps), _initialDistance);

            RequestNextFrameRendering();
        }

        /// <summary>
        /// Zoom per wheel notch. Aurora is aggressive here - measured 3.31x over two notches, so 1.82
        /// each; four notches take a whole area to a close-up.
        /// </summary>
        private const float WheelZoomPerNotch = 1.82f;

        public void HandlePointerWheel(PointerWheelEventArgs e)
        {
            // Wheel up (positive delta) zooms IN (shrinks distance) per common convention.
            var factor = (float)Math.Pow(WheelZoomPerNotch, -e.Delta.Y);
            _distance = AreaCameraMath.ClampDistance(_distance * factor, _initialDistance);

            RequestNextFrameRendering();
            e.Handled = true;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            HandlePointerPressed(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            HandlePointerMoved(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            HandlePointerReleased(e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            HandlePointerWheel(e);
        }

        /// <summary>
        /// Esc cancels an in-progress manipulation drag (reverting to the instance's real
        /// position/heading), an active object placement, or an armed tile placement. R turns an
        /// armed tile a quarter turn before it is stamped.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.R && _isTilePlacementActive)
            {
                TileRotateRequested?.Invoke();
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Escape)
                return;

            if (_dragMode == DragMode.Move || _dragMode == DragMode.Rotate)
            {
                _manipulationCancelled = true;
                _manipulationPreview = null;
                RequestNextFrameRendering();
                e.Handled = true;
                return;
            }

            if (_isPlacementActive)
            {
                CancelPlacement();
                e.Handled = true;
                return;
            }

            if (_isTilePlacementActive)
            {
                CancelTilePlacement();
                e.Handled = true;
                return;
            }
        }

        // ----- GL lifecycle -----

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);

            try
            {
                _gl = GL.GetApi(gl.GetProcAddress);

                var versionString = _gl.GetStringS(StringName.Version) ?? string.Empty;
                var isOpenGLES = versionString.Contains("OpenGL ES", StringComparison.OrdinalIgnoreCase);
                var renderer = _gl.GetStringS(StringName.Renderer) ?? "unknown";

                if (!CreateShaderProgram(isOpenGLES))
                {
                    RaiseStatus("3D view unavailable: shader compilation failed.");
                    _gl = null;
                    return;
                }

                _uniformLocations.Clear(); // Locations are per-program; a fresh program invalidates any cached ones.
                BuildStaticMeshes();
                _renderedSceneVersion = -1;

                RaiseStatus(IsLikelySoftwareRenderer(renderer)
                    ? $"3D view running on software rendering ({renderer}); performance may be degraded."
                    : string.Empty);
            }
            catch (Exception ex)
            {
                RaiseStatus($"3D view unavailable: {ex.Message}");
                _gl = null;
            }
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            try
            {
                if (_gl != null)
                {
                    foreach (var (texId, _) in _textureCache.Values)
                        if (texId != 0)
                            _gl.DeleteTexture(texId);
                    _textureCache.Clear();
                    _rawTextureCache.Clear();

                    foreach (var buffer in _modelBuffers.Values)
                        DeleteBuffer(buffer.Vao, buffer.Vbo, buffer.Ebo);
                    _modelBuffers.Clear();

                    if (_fallbackCubeBuffer is { } cube)
                        DeleteBuffer(cube.Vao, cube.Vbo, cube.Ebo);
                    if (_markerMeshBuffer is { } marker)
                        DeleteBuffer(marker.Vao, marker.Vbo, marker.Ebo);

                    DeletePolygonBuffer();
                    DeleteWalkmeshBuffer();

                    if (_hasHighlightBuffer)
                    {
                        _gl.DeleteVertexArray(_highlightVao);
                        _gl.DeleteBuffer(_highlightVbo);
                        _hasHighlightBuffer = false;
                    }

                    if (_shaderProgram != 0)
                        _gl.DeleteProgram(_shaderProgram);
                    _shaderProgram = 0;
                    _uniformLocations.Clear();
                }
            }
            catch (Exception)
            {
                // GL cleanup must never crash the app on teardown - the context may already be
                // partially invalid (e.g. window closing).
            }
            finally
            {
                _gl = null;
            }

            base.OnOpenGlDeinit(gl);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (_gl == null)
                return;

            var bounds = Bounds;
            var width = (int)bounds.Width;
            var height = (int)bounds.Height;
            if (width <= 0 || height <= 0)
                return;

            // Logical units for input/camera math (pointer deltas arrive in logical pixels)...
            _viewportWidth = width;
            _viewportHeight = height;

            // ...but the framebuffer Avalonia hands us is in PHYSICAL pixels (Bounds x
            // RenderScaling). Passing logical bounds to glViewport leaves the render in the
            // lower-left fraction of the panel on any display scale above 100%.
            var scaling = VisualRoot?.RenderScaling ?? 1.0;
            var pixelWidth = (uint)Math.Max(1, (int)Math.Ceiling(width * scaling));
            var pixelHeight = (uint)Math.Max(1, (int)Math.Ceiling(height * scaling));

            _gl.ClearColor(0.12f, 0.14f, 0.18f, 1f);
            _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Less);
            // NWN tile/prop meshes have inconsistent winding - culling would drop real faces.
            _gl.Disable(EnableCap.CullFace);
            _gl.Viewport(0, 0, pixelWidth, pixelHeight);

            var sceneState = Volatile.Read(ref _sceneState);
            var scene = sceneState.Scene;
            if (scene == null)
                return;

            try
            {
                if (sceneState.Version != _renderedSceneVersion)
                {
                    RebuildPolygonBuffer(scene);
                    RebuildWalkmeshBuffer(scene);
                    _tileBatches = AreaDrawBatcher.GroupByModel(scene.Tiles);
                    _renderedSceneVersion = sceneState.Version;
                }

                DrawScene(scene, width, height);
            }
            catch (Exception ex)
            {
                RaiseStatus($"Area render error: {ex.Message}");
            }
        }

        private static bool IsLikelySoftwareRenderer(string renderer) =>
            renderer.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase) ||
            renderer.Contains("software", StringComparison.OrdinalIgnoreCase) ||
            renderer.Contains("swiftshader", StringComparison.OrdinalIgnoreCase) ||
            renderer.Contains("microsoft basic render", StringComparison.OrdinalIgnoreCase);

        private void DeleteBuffer(uint vao, uint vbo, uint ebo)
        {
            _gl!.DeleteVertexArray(vao);
            _gl.DeleteBuffer(vbo);
            _gl.DeleteBuffer(ebo);
        }

        private void DeletePolygonBuffer()
        {
            if (!_hasPolygonBuffer || _gl == null)
                return;

            _gl.DeleteVertexArray(_polygonVao);
            _gl.DeleteBuffer(_polygonVbo);
            _hasPolygonBuffer = false;
        }

        // ----- Shader compilation -----

        private bool CreateShaderProgram(bool isOpenGLES)
        {
            var preamble = isOpenGLES ? VersionEs : VersionDesktop;
            var vertexShader = CompileShader(ShaderType.VertexShader, preamble + VertexShaderBody);
            var fragmentShader = CompileShader(ShaderType.FragmentShader, preamble + FragmentShaderBody);

            if (vertexShader == 0 || fragmentShader == 0)
                return false;

            _shaderProgram = _gl!.CreateProgram();
            _gl.AttachShader(_shaderProgram, vertexShader);
            _gl.AttachShader(_shaderProgram, fragmentShader);
            _gl.LinkProgram(_shaderProgram);

            _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out var status);

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            return status != 0;
        }

        private uint CompileShader(ShaderType type, string source)
        {
            var shader = _gl!.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
            if (status == 0)
            {
                _gl.DeleteShader(shader);
                return 0;
            }

            return shader;
        }

        // ----- Uniform helpers. Locations are cached per shader program: the large
        // area (pw_ar_czarmrange, 256 tiles) issues thousands of uniform sets per frame, and an
        // uncached glGetUniformLocation is a driver string lookup each time. The cache is cleared
        // whenever the program is (re)created (OnOpenGlInit) or torn down (OnOpenGlDeinit). -----

        private readonly Dictionary<string, int> _uniformLocations = new();

        private int GetUniformLocationCached(string name)
        {
            if (_uniformLocations.TryGetValue(name, out var location))
                return location;

            location = _gl!.GetUniformLocation(_shaderProgram, name);
            _uniformLocations[name] = location;
            return location;
        }

        private void SetUniformMatrix4(string name, Matrix4x4 matrix)
        {
            var location = GetUniformLocationCached(name);
            if (location < 0)
                return;

            // System.Numerics uses row-vector convention (v * M); GLSL uses column-vector (M * v).
            // Transposing here lets GLSL's `model/view/projection * vec4` agree with how the
            // matrices were composed on the C# side (see AreaSceneBuilder/MdlMeshBuilder).
            ReadOnlySpan<float> values = stackalloc float[16]
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            };
            _gl.UniformMatrix4(location, 1, false, values);
        }

        private void SetUniformVec3(string name, Vector3 value)
        {
            var location = GetUniformLocationCached(name);
            if (location >= 0)
                _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        private void SetUniformBool(string name, bool value)
        {
            var location = GetUniformLocationCached(name);
            if (location >= 0)
                _gl.Uniform1(location, value ? 1 : 0);
        }

        private void SetUniformFloat(string name, float value)
        {
            var location = GetUniformLocationCached(name);
            if (location >= 0)
                _gl.Uniform1(location, value);
        }

        private void SetUniformInt(string name, int value)
        {
            var location = GetUniformLocationCached(name);
            if (location >= 0)
                _gl.Uniform1(location, value);
        }

        private void SetVertexAttribPointers()
        {
            const uint stride = FloatsPerVertex * sizeof(float);
            unsafe
            {
                _gl!.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);
                _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
                _gl.EnableVertexAttribArray(1);
                _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, (void*)(6 * sizeof(float)));
                _gl.EnableVertexAttribArray(2);
            }
        }

        // ----- Frame drawing -----

        /// <summary>
        /// The area's decoded lighting brightened for editor visibility: each channel of the
        /// ambient and diffuse colors is lifted from a floor toward its authored value, so night
        /// areas keep their cool hue but never go too dark to edit.
        /// </summary>
        private static (Vector3 Ambient, Vector3 Diffuse) EditorSceneLighting(AreaScene scene)
        {
            var lighting = scene.Lighting;
            return (
                LiftFromFloor(lighting.AmbientColor, AmbientLightFloor),
                LiftFromFloor(lighting.DiffuseColor, DiffuseLightFloor));
        }

        private static Vector3 LiftFromFloor(Vector3 color, float floor) =>
            new Vector3(floor) + color * (1f - floor);

        private void DrawScene(AreaScene scene, int width, int height)
        {
            var farPlane = MathF.Max(_distance, _initialDistance) * 25f + 100f;
            var aspect = (float)width / height;
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(VerticalFovRadians, aspect, NearPlane, farPlane);

            var eye = _target + AreaCameraMath.OrbitEyeOffset(_azimuth, _elevation, _distance);
            var view = Matrix4x4.CreateLookAt(eye, _target, Vector3.UnitZ);

            // Kept for picking (RaiseInstancePicked runs on a click, not every frame, so it needs
            // the matrices from whatever frame last actually rendered).
            _lastView = view;
            _lastProjection = projection;
            _viewProjection = view * projection;

            _gl!.UseProgram(_shaderProgram);
            SetUniformMatrix4("view", view);
            SetUniformMatrix4("projection", projection);
            var (ambient, diffuse) = EditorSceneLighting(scene);
            SetUniformVec3("lightDir", LightDir);
            SetUniformVec3("lightColor", diffuse);
            SetUniformVec3("ambientColor", ambient);
            SetUniformInt("diffuseTexture", 0);
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);

            DrawTileBatches();

            // Markers, walkmesh overlay, and trigger outlines are never ceiling-clipped — reset the sticky per-tile value.
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);
            DrawWalkmeshOverlay();
            DrawInstanceMarkers(scene);
            DrawPolygonOverlays();
            DrawSelectionHighlight();
            DrawTransformGizmo();
            DrawPlacementGhost();
            DrawTileCellHighlight(scene);

            _gl.BindVertexArray(0);
        }

        private void DrawTileBatches()
        {
            if (_tileBatches == null)
                return;

            foreach (var batch in _tileBatches)
            {
                if (batch.Model == null)
                {
                    DrawFallbackBatch(batch.Placements);
                    continue;
                }

                var buffer = GetOrBuildModelBuffer(batch.Model);
                _gl!.BindVertexArray(buffer.Vao);

                foreach (var placement in batch.Placements)
                {
                    if (!IsPlacementVisible(placement))
                        continue;

                    SetUniformFloat("ceilingClipZ",
                        _hideCeilings ? placement.HeightOffset + CeilingClipHeight : CeilingClipDisabled);

                    foreach (var meshRange in buffer.MeshRanges)
                    {
                        var worldMatrix = meshRange.MeshTransform * placement.Transform;
                        SetUniformMatrix4("model", worldMatrix);
                        BindMeshTexture(meshRange.TextureName);

                        unsafe
                        {
                            _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                                DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                        }
                    }
                }
            }
        }

        private void DrawFallbackBatch(IReadOnlyList<TilePlacement> placements)
        {
            if (_fallbackCubeBuffer is not { } cube)
                return;

            _gl!.BindVertexArray(cube.Vao);
            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", false);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformVec3("flatColor", FallbackTileColor);

            foreach (var placement in placements)
            {
                if (!IsPlacementVisible(placement))
                    continue;

                SetUniformFloat("ceilingClipZ",
                    _hideCeilings ? placement.HeightOffset + CeilingClipHeight : CeilingClipDisabled);
                SetUniformMatrix4("model", placement.Transform);
                unsafe
                {
                    _gl.DrawElements(PrimitiveType.Triangles, (uint)cube.IndexCount,
                        DrawElementsType.UnsignedInt, (void*)0);
                }
            }
        }

        // Frustum culling: the largest area (pw_ar_czarmrange, 256 tiles) issues
        // thousands of draw calls per frame; skipping tiles fully outside the view frustum cuts
        // that sharply when the camera is zoomed/panned into a region. The per-tile box is a
        // deliberately generous superset of the tile's cell footprint (tile geometry can overhang
        // its 10m cell and rise well above the floor), so a partially-visible tile is never culled.
        private const float TileCullFootprintHalf = AreaSceneBuilder.TileSize / 2f + 2f; // 10m cell half-width + 2m overhang margin
        private const float TileCullFloorMargin = 5f;
        private const float TileCullCeilingMargin = 20f;

        /// <summary>
        /// Whether an instance's model could be on screen, from the same world bounds picking uses.
        /// </summary>
        private bool IsInstanceVisible(InstanceMarker instance)
        {
            // No resolved bounds means nothing to cull against; draw it and let the model pass decide.
            if (AreaPicking.ComputeModelWorldBounds(instance) is not { } bounds)
                return true;

            return IsAabbInFrustum(bounds.Min, bounds.Max, _viewProjection);
        }

        private bool IsPlacementVisible(TilePlacement placement)
        {
            var min = new Vector3(
                placement.CenterX - TileCullFootprintHalf,
                placement.CenterY - TileCullFootprintHalf,
                placement.HeightOffset - TileCullFloorMargin);
            var max = new Vector3(
                placement.CenterX + TileCullFootprintHalf,
                placement.CenterY + TileCullFootprintHalf,
                placement.HeightOffset + TileCullCeilingMargin);

            return IsAabbInFrustum(min, max, _viewProjection);
        }

        /// <summary>
        /// True unless the axis-aligned box is entirely outside one clip plane of <paramref name="vp"/>
        /// (view*projection). Transforms the 8 corners to clip space and culls only when all 8 fall
        /// beyond the same plane - conservative (never culls a box that straddles the frustum), and
        /// matches the row-vector convention the rest of this control uses (see SetUniformMatrix4).
        /// System.Numerics' perspective matrix maps depth to [0, w], so the near test is z &lt; 0.
        /// </summary>
        private static bool IsAabbInFrustum(Vector3 min, Vector3 max, Matrix4x4 vp)
        {
            int outLeft = 0, outRight = 0, outBottom = 0, outTop = 0, outNear = 0, outFar = 0;

            for (var corner = 0; corner < 8; corner++)
            {
                var point = new Vector3(
                    (corner & 1) == 0 ? min.X : max.X,
                    (corner & 2) == 0 ? min.Y : max.Y,
                    (corner & 4) == 0 ? min.Z : max.Z);
                var clip = Vector4.Transform(point, vp);

                if (clip.X < -clip.W) outLeft++;
                if (clip.X > clip.W) outRight++;
                if (clip.Y < -clip.W) outBottom++;
                if (clip.Y > clip.W) outTop++;
                if (clip.Z < 0f) outNear++;
                if (clip.Z > clip.W) outFar++;
            }

            return !(outLeft == 8 || outRight == 8 || outBottom == 8 || outTop == 8 || outNear == 8 || outFar == 8);
        }

        private void DrawInstanceMarkers(AreaScene scene)
        {
            // Pass 1: instances with resolved render geometry (placeables, doors) draw their
            // actual model, textured and lit, at the instance's position/heading (or its live
            // manipulation preview, while a move/rotate drag is in progress on it).
            foreach (var raw in scene.Instances)
            {
                if (!DrawsAsModel(raw))
                    continue;

                var instance = Displayed(raw);

                // Culled like the tile pass already is. esriauncharted holds 5,249 placeables and each
                // visible-model instance issues several draw calls, so submitting every one of them every
                // frame made a dense area unusable however little of it was on screen.
                if (!IsInstanceVisible(instance))
                    continue;

                var instanceTransform = AreaPicking.ComputeInstanceTransform(instance);

                var buffer = GetOrBuildModelBuffer(raw.Model!);
                _gl!.BindVertexArray(buffer.Vao);
                SetUniformBool("unlit", false);

                foreach (var meshRange in buffer.MeshRanges)
                {
                    SetUniformMatrix4("model", meshRange.MeshTransform * instanceTransform);
                    BindMeshTexture(meshRange.TextureName);

                    unsafe
                    {
                        _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                            DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                    }
                }
            }

            // Pass 2: everything without resolved geometry draws its kind-colored pyramid marker.
            if (_markerMeshBuffer is not { } marker)
                return;

            _gl!.BindVertexArray(marker.Vao);
            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", 1f);

            foreach (var raw in scene.Instances)
            {
                if (DrawsAsModel(raw))
                    continue;

                var instance = Displayed(raw);

                SetUniformMatrix4("model", AreaPicking.ComputeInstanceTransform(instance));
                SetUniformVec3("flatColor", MarkerColor(instance.Kind));

                unsafe
                {
                    _gl.DrawElements(PrimitiveType.Triangles, (uint)marker.IndexCount,
                        DrawElementsType.UnsignedInt, (void*)0);
                }
            }
        }

        /// <summary>
        /// Draws the walkmesh overlay: translucent world-space triangles, walkable faces
        /// green and blocked faces red, blended over the tile floor with depth-writes disabled so
        /// it tints the geometry rather than occluding it. A no-op when the toggle is off or the
        /// scene resolved no walkmeshes.
        /// </summary>
        private void DrawWalkmeshOverlay()
        {
            if (!_showWalkmesh || !_hasWalkmeshBuffer || _gl == null)
                return;

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false); // tint the floor, don't occlude geometry behind the overlay

            _gl.BindVertexArray(_walkmeshVao);
            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", WalkmeshOverlayAlpha);
            SetUniformMatrix4("model", Matrix4x4.Identity); // vertices are already world-space

            if (_walkmeshWalkableVertexCount > 0)
            {
                SetUniformVec3("flatColor", WalkmeshWalkableColor);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_walkmeshWalkableVertexCount);
            }

            if (_walkmeshBlockedVertexCount > 0)
            {
                SetUniformVec3("flatColor", WalkmeshBlockedColor);
                _gl.DrawArrays(PrimitiveType.Triangles, _walkmeshWalkableVertexCount, (uint)_walkmeshBlockedVertexCount);
            }

            _gl.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
            SetUniformFloat("flatAlpha", 1f); // restore for the opaque unlit draws that follow this pass
        }

        private void DrawPolygonOverlays()
        {
            if (!_hasPolygonBuffer || _polygonRanges.Count == 0)
                return;

            _gl!.BindVertexArray(_polygonVao);
            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", 1f);
            SetUniformVec3("flatColor", PolygonOverlayColor);
            SetUniformMatrix4("model", Matrix4x4.Identity);

            foreach (var (start, count) in _polygonRanges)
                _gl.DrawArrays(PrimitiveType.LineLoop, start, (uint)count);
        }

        /// <summary>
        /// Draws a bright wireframe box around <see cref="_selectedInstance"/>'s current world
        /// bounds (<see cref="AreaPicking.ComputeInstanceWorldBounds"/> - the same model/marker
        /// bounds picking itself uses, via the same <see cref="DrawsAsModel"/> rule) as a
        /// GL_LINES box rather than a GL_LINE polygon-mode overlay: OpenGL ES has no wireframe
        /// polygon mode, and this control already renders trigger outlines the same way
        /// (see <see cref="DrawPolygonOverlays"/>), so this stays portable to the same GL profiles.
        /// A no-op when nothing is selected or the GL context isn't ready.
        /// </summary>
        private void DrawSelectionHighlight()
        {
            if (_selectedInstance is not { } instance || _gl == null)
                return;

            var (min, max) = AreaPicking.ComputeInstanceWorldBounds(Displayed(instance), DrawsAsModel(instance));
            var vertices = BuildWireframeBoxVertices(min, max);

            EnsureHighlightBuffer();
            if (!_hasHighlightBuffer)
                return;

            _gl.BindVertexArray(_highlightVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _highlightVbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertices), BufferUsageARB.DynamicDraw);
            SetVertexAttribPointers();

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", 1f);
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);
            SetUniformVec3("flatColor", SelectionHighlightColor);
            SetUniformMatrix4("model", Matrix4x4.Identity); // bounds are already world-space

            _gl.DrawArrays(PrimitiveType.Lines, 0, 24);
        }

        /// <summary>Alpha and tint for the placement ghost - present, clearly provisional.</summary>
        private const float PlacementGhostAlpha = 0.55f;

        private static readonly Vector3 PlacementGhostColor = new(0.36f, 0.61f, 0.96f);

        /// <summary>
        /// Fainter than the object ghost. A tile fills a whole 10m cell, so at the object ghost's
        /// opacity it blots out the area underneath it rather than previewing against it.
        /// </summary>
        private const float TileGhostAlpha = 0.35f;

        /// <summary>
        /// How much larger the ghost's fallback marker is than a placed instance's marker. A blueprint
        /// with no resolvable model has only this to show, and at the marker's own size it was lost
        /// against the floor - which is worse than useless when the marker is the whole preview.
        /// </summary>
        private const float PlacementGhostMarkerScale = 2.2f;

        /// <summary>
        /// Draws the object being placed at the ground point under the cursor.
        /// </summary>
        /// <remarks>
        /// Tinted and translucent rather than a faded copy of the real thing: a half-transparent
        /// textured model is easy to mistake for one already placed, whereas a single accent colour
        /// reads immediately as "not yet real". The model's own geometry is used where it resolved, so
        /// the footprint and height are honest even though the surface is not.
        /// <para>
        /// Drawn with the depth test off, like the transform gizmo. This is a cursor, and a cursor that
        /// disappears behind a wall or sinks into the floor has failed at its one job - which is exactly
        /// what happened to the marker fallback, whose base sits on the walkmesh and so ended up buried
        /// inside the floor geometry drawn above it.
        /// </para>
        /// </remarks>
        private void DrawPlacementGhost()
        {
            if (!_isPlacementActive || _placementGhost is not { } ghost || _ghostPosition is not { } position ||
                _gl == null)
                return;

            var placed = new InstanceMarker
            {
                Kind = ghost.Kind,
                TemplateResRef = ghost.TemplateResRef,
                Tag = ghost.Tag,
                Position = position,
                Orientation = ghost.Orientation,
                VisualTransform = ghost.VisualTransform,
                Model = ghost.Model
            };

            var transform = AreaPicking.ComputeInstanceTransform(placed);

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);
            _gl.Disable(EnableCap.DepthTest);

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", PlacementGhostAlpha);
            SetUniformVec3("flatColor", PlacementGhostColor);
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);

            if (DrawsAsModel(placed))
            {
                var buffer = GetOrBuildModelBuffer(placed.Model!);
                _gl.BindVertexArray(buffer.Vao);

                foreach (var meshRange in buffer.MeshRanges)
                {
                    SetUniformMatrix4("model", meshRange.MeshTransform * transform);

                    unsafe
                    {
                        _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                            DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                    }
                }
            }
            else if (_markerMeshBuffer is { } marker)
            {
                _gl.BindVertexArray(marker.Vao);
                SetUniformMatrix4("model",
                    Matrix4x4.CreateScale(PlacementGhostMarkerScale) * transform);

                unsafe
                {
                    _gl.DrawElements(PrimitiveType.Triangles, (uint)marker.IndexCount,
                        DrawElementsType.UnsignedInt, (void*)0);
                }
            }

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
            SetUniformFloat("flatAlpha", 1f);
        }

        /// <summary>Cell-highlight tint: the ghost's accent when the stamp fits, a warning red when it does not.</summary>
        private static readonly Vector3 TileCellHighlightColor = PlacementGhostColor;

        private static readonly Vector3 TileCellRejectedColor = new(0.92f, 0.28f, 0.22f);

        /// <summary>Translucent enough to read the tile underneath - the builder is choosing between tiles, not covering one up.</summary>
        private const float TileCellHighlightAlpha = 0.45f;

        /// <summary>Lifts the highlight above the tile floor (and above the walkmesh overlay, which may be on at the same time) so it reads as painted on the ground rather than buried in it.</summary>
        private const float TileCellHighlightHeightOffset = 0.08f;

        /// <summary>
        /// Draws a translucent quad over each grid cell the armed palette entry would write.
        /// </summary>
        /// <remarks>
        /// The footprint is anchored bottom-left at the hovered cell (see
        /// <see cref="TilePlacementFootprint"/>), and only cells actually inside the grid are painted -
        /// so a stamp hanging off the edge visibly loses part of itself, and the whole highlight turns
        /// red to say the click will be refused rather than clipped. A footprint entirely off the grid
        /// paints nothing at all, which is the same message with nothing left to draw it on.
        /// <para>
        /// Depth test off, like the transform gizmo and the object ghost: this is a cursor, and one that
        /// sinks into the floor it is meant to be lying on - or hides behind the wall of the cell next
        /// door - has failed at its one job.
        /// </para>
        /// </remarks>
        private void DrawTileCellHighlight(AreaScene scene)
        {
            // Object placement wins when both modes are somehow armed - see the precedence note on
            // the tile-placement fields.
            if (_isPlacementActive || !_isTilePlacementActive || _tileHoverCell is not { } anchor || _gl == null)
                return;

            var vertices = BuildFootprintQuadVertices(scene, anchor.Column, anchor.Row);
            if (vertices.Length == 0)
                return;

            EnsureHighlightBuffer();
            if (!_hasHighlightBuffer)
                return;

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);
            _gl.Disable(EnableCap.DepthTest);

            _gl.BindVertexArray(_highlightVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _highlightVbo);
            // Re-uploaded per frame into the shared dynamic highlight buffer (as the selection box and
            // gizmo arms are), so a changed footprint or hovered cell simply replaces the contents
            // instead of accumulating buffers to leak.
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertices), BufferUsageARB.DynamicDraw);
            SetVertexAttribPointers();

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", TileCellHighlightAlpha);
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);
            SetUniformVec3("flatColor", FootprintFitsGrid(scene, anchor.Column, anchor.Row)
                ? TileCellHighlightColor
                : TileCellRejectedColor);
            SetUniformMatrix4("model", Matrix4x4.Identity); // cell corners are already world-space

            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(vertices.Length / FloatsPerVertex));

            DrawTileGhostModels(scene, anchor.Column, anchor.Row);

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
            SetUniformFloat("flatAlpha", 1f);
        }

        /// <summary>
        /// Draws the armed stamp's own tile models over the highlighted cells.
        /// </summary>
        /// <remarks>
        /// A tile is chosen for its shape - a doorway, a stair, a corner, a bridge - and an outlined
        /// cell shows none of it, so a builder had to stamp one to find out what it was. Drawn tinted
        /// and translucent for the same reason the object ghost is: a textured copy of the real thing
        /// would be indistinguishable from a tile already laid. Called from inside
        /// <see cref="DrawTileCellHighlight"/>, which has already set up blending and turned the depth
        /// test off, and which leaves the outline underneath as the fits/does-not-fit signal.
        /// </remarks>
        private void DrawTileGhostModels(AreaScene scene, int anchorColumn, int anchorRow)
        {
            if (_gl == null || _tilePlacementModels.Count == 0)
                return;

            var (columns, rows) = _tilePlacementFootprint;

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("flatAlpha", TileGhostAlpha);
            SetUniformVec3("flatColor", FootprintFitsGrid(scene, anchorColumn, anchorRow)
                ? TileCellHighlightColor
                : TileCellRejectedColor);

            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                var slot = row * columns + column;
                if (slot >= _tilePlacementModels.Count || _tilePlacementModels[slot] is not { } model)
                    continue;

                var targetColumn = anchorColumn + column;
                var targetRow = anchorRow + row;
                if (targetColumn < 0 || targetRow < 0 ||
                    targetColumn >= scene.Width || targetRow >= scene.Height)
                    continue;

                // Tile models are authored about their own centre, which is where the scene's own tile
                // transform puts them; the ghost has to agree or it would sit a half-tile off.
                var transform = Matrix4x4.CreateTranslation(
                    (targetColumn + 0.5f) * AreaSceneBuilder.TileSize,
                    (targetRow + 0.5f) * AreaSceneBuilder.TileSize,
                    CellFloorHeight(scene, targetColumn, targetRow));

                var buffer = GetOrBuildModelBuffer(model);
                _gl.BindVertexArray(buffer.Vao);

                foreach (var meshRange in buffer.MeshRanges)
                {
                    SetUniformMatrix4("model", meshRange.MeshTransform * transform);

                    unsafe
                    {
                        _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                            DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                    }
                }
            }
        }

        /// <summary>
        /// Two world-space triangles per in-bounds cell of the footprint anchored bottom-left at
        /// (<paramref name="anchorColumn"/>, <paramref name="anchorRow"/>), each sitting just above
        /// that cell's own tile floor so the highlight follows elevation changes across the footprint.
        /// </summary>
        private float[] BuildFootprintQuadVertices(AreaScene scene, int anchorColumn, int anchorRow)
        {
            var (columns, rows) = _tilePlacementFootprint;
            var data = new List<float>(columns * rows * 6 * FloatsPerVertex);

            for (var dRow = 0; dRow < rows; dRow++)
            {
                for (var dColumn = 0; dColumn < columns; dColumn++)
                {
                    var column = anchorColumn + dColumn;
                    var row = anchorRow + dRow;
                    if (column < 0 || row < 0 || column >= scene.Width || row >= scene.Height)
                        continue;

                    var z = CellFloorHeight(scene, column, row) + TileCellHighlightHeightOffset;
                    var minX = column * AreaSceneBuilder.TileSize;
                    var minY = row * AreaSceneBuilder.TileSize;
                    var maxX = minX + AreaSceneBuilder.TileSize;
                    var maxY = minY + AreaSceneBuilder.TileSize;

                    AppendCellQuadVertex(data, minX, minY, z);
                    AppendCellQuadVertex(data, maxX, minY, z);
                    AppendCellQuadVertex(data, maxX, maxY, z);
                    AppendCellQuadVertex(data, minX, minY, z);
                    AppendCellQuadVertex(data, maxX, maxY, z);
                    AppendCellQuadVertex(data, minX, maxY, z);
                }
            }

            return data.ToArray();
        }

        /// <summary>Appends one highlight vertex (up-normal, zero UV) in the shared 8-float layout.</summary>
        private static void AppendCellQuadVertex(List<float> data, float x, float y, float z)
        {
            data.Add(x);
            data.Add(y);
            data.Add(z);
            data.Add(0f);
            data.Add(0f);
            data.Add(1f);
            data.Add(0f);
            data.Add(0f);
        }

        /// <summary>
        /// The floor height of one grid cell, from the tile occupying it. The Tile_List is row-major
        /// by the area format's own contract (see <see cref="TilePlacement"/>), so the index is
        /// computable rather than searched - and a scene whose list is short of Width*Height (corrupt
        /// input the assembler tolerates) falls back to the Z=0 floor instead of throwing.
        /// </summary>
        private static float CellFloorHeight(AreaScene scene, int column, int row)
        {
            var index = row * scene.Width + column;
            return index >= 0 && index < scene.Tiles.Count ? scene.Tiles[index].HeightOffset : 0f;
        }

        /// <summary>
        /// Draws the transform gizmo on the selection: one arm per axis in its conventional colour, and
        /// a ring in the ground plane for rotation.
        /// </summary>
        /// <remarks>
        /// The gizmo is what replaced the coordinate boxes in the chrome - Aurora moved things by direct
        /// manipulation and so does this, with the numbers appearing beside the map only while a drag is
        /// in flight. Arms are a fixed world length rather than scaled to the object, so a crate and a
        /// building present the same handle to grab.
        /// </remarks>
        private void DrawTransformGizmo()
        {
            if (_selectedInstance is not { } instance || _gl == null)
                return;

            EnsureHighlightBuffer();
            if (!_hasHighlightBuffer)
                return;

            var origin = Displayed(instance).Position;

            // Drawn without depth testing so the handles stay grabbable when the instance sits in a
            // hollow or behind a rock - a gizmo you cannot see is a gizmo you cannot use, and every 3D
            // tool draws them on top for the same reason.
            _gl.Disable(EnableCap.DepthTest);

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", 1f);
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);
            SetUniformMatrix4("model", Matrix4x4.Identity);

            DrawGizmoLines(BuildAxisVertices(origin, new Vector3(GizmoArmLength, 0, 0)), GizmoAxisXColor);
            DrawGizmoLines(BuildAxisVertices(origin, new Vector3(0, GizmoArmLength, 0)), GizmoAxisYColor);
            DrawGizmoLines(BuildAxisVertices(origin, new Vector3(0, 0, GizmoArmLength)), GizmoAxisZColor);
            DrawGizmoLines(BuildRotationRingVertices(origin, GizmoRingRadius), GizmoRingColor);

            _gl.Enable(EnableCap.DepthTest);
        }

        private void DrawGizmoLines(float[] vertices, Vector3 color)
        {
            if (_gl == null)
                return;

            _gl.BindVertexArray(_highlightVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _highlightVbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertices), BufferUsageARB.DynamicDraw);
            SetVertexAttribPointers();

            SetUniformVec3("flatColor", color);
            _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(vertices.Length / FloatsPerVertex));
        }

        /// <summary>One axis arm plus a small arrowhead, as line segments.</summary>
        private static float[] BuildAxisVertices(Vector3 origin, Vector3 arm)
        {
            var tip = origin + arm;

            // The arrowhead is two short barbs in whichever plane the arm is not aligned with, which
            // keeps it visible from the orbit camera's usual angles without needing a cone mesh.
            var sideways = MathF.Abs(arm.Z) > 0.001f
                ? new Vector3(1, 0, 0)
                : new Vector3(-arm.Y, arm.X, 0);

            if (sideways.LengthSquared() > 0.0001f)
                sideways = Vector3.Normalize(sideways) * (GizmoArmLength * 0.12f);

            var back = tip - Vector3.Normalize(arm) * (GizmoArmLength * 0.2f);

            return BuildLineVertices(new[]
            {
                origin, tip,
                tip, back + sideways,
                tip, back - sideways
            });
        }

        /// <summary>A closed ring in the ground plane, for the rotate handle.</summary>
        private static float[] BuildRotationRingVertices(Vector3 origin, float radius)
        {
            // Segment count and ground offset come from GizmoPicking so the ring a press is tested
            // against is exactly the ring on screen.
            const int segments = GizmoPicking.RingSegments;
            const float groundOffset = GizmoPicking.RingGroundOffset;
            var points = new List<Vector3>(segments * 2);

            for (var i = 0; i < segments; i++)
            {
                var a = i / (float)segments * MathF.Tau;
                var b = (i + 1) / (float)segments * MathF.Tau;
                points.Add(origin + new Vector3(MathF.Cos(a) * radius, MathF.Sin(a) * radius, groundOffset));
                points.Add(origin + new Vector3(MathF.Cos(b) * radius, MathF.Sin(b) * radius, groundOffset));
            }

            return BuildLineVertices(points);
        }

        /// <summary>Packs world-space points into the shared vertex layout (normal/texcoord slots zeroed).</summary>
        private static float[] BuildLineVertices(IReadOnlyList<Vector3> points)
        {
            var vertices = new float[points.Count * FloatsPerVertex];
            for (var i = 0; i < points.Count; i++)
            {
                vertices[i * FloatsPerVertex] = points[i].X;
                vertices[i * FloatsPerVertex + 1] = points[i].Y;
                vertices[i * FloatsPerVertex + 2] = points[i].Z;
            }

            return vertices;
        }

        private void EnsureHighlightBuffer()
        {
            if (_hasHighlightBuffer || _gl == null)
                return;

            _highlightVao = _gl.GenVertexArray();
            _highlightVbo = _gl.GenBuffer();
            _hasHighlightBuffer = true;
        }

        /// <summary>12 edges (24 line vertices, position-only - normal/texcoord slots zeroed to match the shared vertex layout) tracing an axis-aligned box's wireframe.</summary>
        private static float[] BuildWireframeBoxVertices(Vector3 min, Vector3 max)
        {
            var corners = new[]
            {
                new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z),
                new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z),
                new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z),
                new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z)
            };

            Span<int> edges = stackalloc int[]
            {
                0, 1, 1, 2, 2, 3, 3, 0, // bottom face
                4, 5, 5, 6, 6, 7, 7, 4, // top face
                0, 4, 1, 5, 2, 6, 3, 7  // verticals
            };

            var data = new float[edges.Length * FloatsPerVertex];
            var offset = 0;
            foreach (var cornerIndex in edges)
            {
                var p = corners[cornerIndex];
                data[offset++] = p.X;
                data[offset++] = p.Y;
                data[offset++] = p.Z;
                data[offset++] = 0f;
                data[offset++] = 0f;
                data[offset++] = 1f;
                data[offset++] = 0f;
                data[offset++] = 0f;
            }

            return data;
        }

        private static Vector3 MarkerColor(InstanceMarkerKind kind) => kind switch
        {
            InstanceMarkerKind.Creature => new Vector3(0.85f, 0.15f, 0.15f),
            InstanceMarkerKind.Door => new Vector3(0.55f, 0.35f, 0.15f),
            InstanceMarkerKind.Item => new Vector3(0.9f, 0.85f, 0.2f),
            InstanceMarkerKind.Placeable => new Vector3(0.2f, 0.45f, 0.9f),
            InstanceMarkerKind.Sound => new Vector3(0.2f, 0.8f, 0.8f),
            InstanceMarkerKind.Store => new Vector3(0.2f, 0.8f, 0.3f),
            InstanceMarkerKind.Trigger => new Vector3(0.95f, 0.55f, 0.15f),
            // Aurora's waypoint yellow, for a waypoint whose appearance row names no model.
            InstanceMarkerKind.Waypoint => new Vector3(0.98f, 0.80f, 0.10f),
            _ => new Vector3(0.7f, 0.7f, 0.7f)
        };

        // ----- Per-RenderModel GPU buffer (uploaded once per distinct model per GL context) -----

        private ModelBuffer GetOrBuildModelBuffer(RenderModel model)
        {
            if (_modelBuffers.TryGetValue(model, out var existing))
                return existing;

            var built = BuildModelBuffer(model);
            _modelBuffers[model] = built;
            return built;
        }

        private ModelBuffer BuildModelBuffer(RenderModel model)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();
            var meshRanges = new List<MeshRange>();
            uint baseVertex = 0;

            foreach (var mesh in model.Meshes)
            {
                var vertexCount = mesh.VertexCount;
                if (vertexCount == 0 || mesh.Indices.Length == 0)
                    continue;

                var hasNormals = mesh.Normals.Length == vertexCount * 3;
                var hasUvs = mesh.TexCoords.Length == vertexCount * 2;

                for (var i = 0; i < vertexCount; i++)
                {
                    vertices.Add(mesh.Positions[i * 3]);
                    vertices.Add(mesh.Positions[i * 3 + 1]);
                    vertices.Add(mesh.Positions[i * 3 + 2]);

                    vertices.Add(hasNormals ? mesh.Normals[i * 3] : 0f);
                    vertices.Add(hasNormals ? mesh.Normals[i * 3 + 1] : 0f);
                    vertices.Add(hasNormals ? mesh.Normals[i * 3 + 2] : 1f);

                    vertices.Add(hasUvs ? mesh.TexCoords[i * 2] : 0f);
                    vertices.Add(hasUvs ? mesh.TexCoords[i * 2 + 1] : 0f);
                }

                var indexOffset = indices.Count * sizeof(uint);
                foreach (var index in mesh.Indices)
                    indices.Add(baseVertex + (uint)index);

                meshRanges.Add(new MeshRange
                {
                    IndexOffset = indexOffset,
                    IndexCount = mesh.Indices.Length,
                    MeshTransform = mesh.Transform,
                    TextureName = string.IsNullOrEmpty(mesh.TextureName) ? null : mesh.TextureName
                });

                baseVertex += (uint)vertexCount;
            }

            var vao = _gl!.GenVertexArray();
            var vbo = _gl.GenBuffer();
            var ebo = _gl.GenBuffer();

            _gl.BindVertexArray(vao);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            var vertexArray = vertices.ToArray();
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexArray.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertexArray), BufferUsageARB.StaticDraw);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            var indexArray = indices.ToArray();
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexArray.Length * sizeof(uint)),
                new ReadOnlySpan<uint>(indexArray), BufferUsageARB.StaticDraw);

            SetVertexAttribPointers();
            _gl.BindVertexArray(0);

            return new ModelBuffer { Vao = vao, Vbo = vbo, Ebo = ebo, MeshRanges = meshRanges };
        }

        // ----- Textures -----

        private void BindMeshTexture(string? textureName)
        {
            var (texId, alphaCutoff) = string.IsNullOrWhiteSpace(textureName)
                ? (0u, 0f)
                : ResolveTexture(textureName);

            SetUniformBool("unlit", false);

            if (texId != 0)
            {
                SetUniformBool("hasTexture", true);
                SetUniformFloat("alphaCutoff", alphaCutoff);
                _gl!.ActiveTexture(TextureUnit.Texture0);
                _gl.BindTexture(TextureTarget.Texture2D, texId);
            }
            else
            {
                SetUniformBool("hasTexture", false);
                SetUniformFloat("alphaCutoff", 0f);
                SetUniformVec3("flatColor", UntexturedTileColor);
            }
        }

        private (uint TexId, float AlphaCutoff) ResolveTexture(string rawTextureName)
        {
            if (ResourceIndex == null)
                return (0, 0f);

            if (_rawTextureCache.TryGetValue(rawTextureName, out var memo))
                return memo;

            string resolvedName;
            try
            {
                resolvedName = MaterialResolver.ResolveDiffuseTextureName(ResourceIndex, rawTextureName);
            }
            catch (Exception)
            {
                resolvedName = rawTextureName;
            }

            if (!_textureCache.TryGetValue(resolvedName, out var cached))
            {
                cached = LoadAndUploadTexture(resolvedName);
                _textureCache[resolvedName] = cached;
            }

            _rawTextureCache[rawTextureName] = cached;
            return cached;
        }

        private (uint TexId, float AlphaCutoff) LoadAndUploadTexture(string resolvedName)
        {
            try
            {
                var image = TextureLoader.Load(ResourceIndex!, resolvedName);
                if (image == null)
                    return (0, 0f);

                var texId = UploadTexture(image.Width, image.Height, image.Pixels);
                return (texId, ResolveAlphaCutoff(resolvedName));
            }
            catch (Exception)
            {
                return (0, 0f);
            }
        }

        /// <summary>
        /// Cheap subset of TXI transparency honoring: a punch-through
        /// texture gets a hard alpha cutoff in the fragment shader; every other case (additive,
        /// no hint, unparseable/missing TXI) draws fully opaque. Full alpha sorting/blending is
        /// explicitly out of scope.
        /// </summary>
        private float ResolveAlphaCutoff(string resolvedTextureName)
        {
            if (ResourceIndex == null)
                return 0f;

            try
            {
                var identity = new ResourceIdentity(resolvedTextureName, ResourceIdentity.TypeFromExtension("txi"));
                if (!ResourceIndex.TryLookup(identity, out var handle))
                    return 0f;

                var bytes = handle.GetBytes();
                if (bytes.Length == 0)
                    return 0f;

                var txi = TxiInfo.Parse(System.Text.Encoding.ASCII.GetString(bytes));
                return txi.Blending == TxiBlendMode.PunchThrough ? 0.5f : 0f;
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private uint UploadTexture(int width, int height, byte[] rgba)
        {
            var texId = _gl!.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, texId);

            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, new ReadOnlySpan<byte>(rgba));

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            _gl.GenerateMipmap(TextureTarget.Texture2D);

            _gl.BindTexture(TextureTarget.Texture2D, 0);
            return texId;
        }

        // ----- Static placeholder/marker geometry (scene-independent; built once at GL init) -----

        private void BuildStaticMeshes()
        {
            var (cubeVertices, cubeIndices) = BuildFallbackCubeMesh();
            _fallbackCubeBuffer = UploadStaticMesh(cubeVertices, cubeIndices);

            var (markerVertices, markerIndices) = BuildMarkerPyramidMesh();
            _markerMeshBuffer = UploadStaticMesh(markerVertices, markerIndices);
        }

        private StaticMeshBuffer UploadStaticMesh(float[] vertices, uint[] indices)
        {
            var vao = _gl!.GenVertexArray();
            var vbo = _gl.GenBuffer();
            var ebo = _gl.GenBuffer();

            _gl.BindVertexArray(vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertices), BufferUsageARB.StaticDraw);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)),
                new ReadOnlySpan<uint>(indices), BufferUsageARB.StaticDraw);
            SetVertexAttribPointers();
            _gl.BindVertexArray(0);

            return new StaticMeshBuffer(vao, vbo, ebo, indices.Length);
        }

        /// <summary>
        /// A box spanning local [-TileSize/2,+TileSize/2] on X/Y and
        /// [0,FallbackCubeHeight] on Z, matching the origin-centered convention used by real tile
        /// MDLs and <see cref="AreaSceneBuilder"/>'s placement transform.
        /// Applying a fallback placement's Transform to this box fills the same 10m footprint a
        /// real tile model would, making a missing/unresolvable tile obvious at a glance rather
        /// than a near-invisible 1m cube lost inside the grid.
        /// </summary>
        private static (float[] Vertices, uint[] Indices) BuildFallbackCubeMesh()
        {
            const float size = AreaSceneBuilder.TileSize;
            const float half = size / 2f;
            const float h = FallbackCubeHeight;

            var c = new[]
            {
                new Vector3(-half, -half, 0), new Vector3(half, -half, 0),
                new Vector3(half, half, 0), new Vector3(-half, half, 0),
                new Vector3(-half, -half, h), new Vector3(half, -half, h),
                new Vector3(half, half, h), new Vector3(-half, half, h)
            };

            var builder = new BoxMeshBuilder();
            builder.AddQuad(c[3], c[2], c[1], c[0], new Vector3(0, 0, -1)); // bottom
            builder.AddQuad(c[4], c[5], c[6], c[7], new Vector3(0, 0, 1));  // top
            builder.AddQuad(c[0], c[1], c[5], c[4], new Vector3(0, -1, 0)); // front
            builder.AddQuad(c[2], c[3], c[7], c[6], new Vector3(0, 1, 0));  // back
            builder.AddQuad(c[3], c[0], c[4], c[7], new Vector3(-1, 0, 0)); // left
            builder.AddQuad(c[1], c[2], c[6], c[5], new Vector3(1, 0, 0));  // right

            return builder.Build();
        }

        /// <summary>
        /// A small square-based pyramid (base on the ground, apex pointing up) used as the shared
        /// shape for every instance marker; per-instance color (by <see cref="InstanceMarkerKind"/>)
        /// and placement/orientation are applied via uniforms at draw time.
        /// </summary>
        private static (float[] Vertices, uint[] Indices) BuildMarkerPyramidMesh()
        {
            const float half = MarkerHalfWidth;
            const float baseZ = MarkerGroundOffset;
            const float apexZ = MarkerGroundOffset + MarkerHeight;

            var apex = new Vector3(0, 0, apexZ);
            var b0 = new Vector3(-half, -half, baseZ);
            var b1 = new Vector3(half, -half, baseZ);
            var b2 = new Vector3(half, half, baseZ);
            var b3 = new Vector3(-half, half, baseZ);

            var builder = new BoxMeshBuilder();
            builder.AddQuad(b3, b2, b1, b0, new Vector3(0, 0, -1)); // base
            builder.AddTriangle(b0, b1, apex);
            builder.AddTriangle(b1, b2, apex);
            builder.AddTriangle(b2, b3, apex);
            builder.AddTriangle(b3, b0, apex);

            return builder.Build();
        }

        /// <summary>Small flat-shaded quad/triangle accumulator shared by the fallback cube and marker pyramid builders.</summary>
        private sealed class BoxMeshBuilder
        {
            private readonly List<float> _vertices = new();
            private readonly List<uint> _indices = new();

            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                AddTriangle(a, b, c, normal);
                AddTriangle(a, c, d, normal);
            }

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
                AddTriangle(a, b, c, normal);
            }

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                var baseIndex = (uint)(_vertices.Count / FloatsPerVertex);
                Span<Vector3> triangle = stackalloc Vector3[3] { a, b, c };

                foreach (var p in triangle)
                {
                    _vertices.Add(p.X);
                    _vertices.Add(p.Y);
                    _vertices.Add(p.Z);
                    _vertices.Add(normal.X);
                    _vertices.Add(normal.Y);
                    _vertices.Add(normal.Z);
                    _vertices.Add(0f);
                    _vertices.Add(0f);
                }

                _indices.Add(baseIndex);
                _indices.Add(baseIndex + 1);
                _indices.Add(baseIndex + 2);
            }

            public (float[] Vertices, uint[] Indices) Build() => (_vertices.ToArray(), _indices.ToArray());
        }

        // ----- Trigger polygon overlays (scene-specific; rebuilt whenever the scene changes) -----

        /// <summary>
        /// Rebuilds the walkmesh overlay VBO from the scene's per-tile walkmeshes: each face's
        /// tile-local vertices are transformed to world space by that tile's Transform (the same
        /// transform its rendered model uses, so overlay and floor stay aligned) and lifted a hair
        /// above the floor. Walkable faces are emitted first, then blocked faces, so the draw
        /// colors each group with one contiguous range. Built once per scene change, beside
        /// <see cref="RebuildPolygonBuffer"/>.
        /// </summary>
        private void RebuildWalkmeshBuffer(AreaScene scene)
        {
            DeleteWalkmeshBuffer();

            var walkable = new List<float>();
            var blocked = new List<float>();

            foreach (var tile in scene.Tiles)
            {
                var mesh = tile.Walkmesh;
                if (mesh == null)
                    continue;

                var verts = mesh.Vertices;
                foreach (var face in mesh.Faces)
                {
                    if (face.A < 0 || face.B < 0 || face.C < 0 ||
                        face.A >= verts.Count || face.B >= verts.Count || face.C >= verts.Count)
                        continue;

                    var target = face.Walkable ? walkable : blocked;
                    AppendWalkmeshVertex(target, Vector3.Transform(verts[face.A], tile.Transform));
                    AppendWalkmeshVertex(target, Vector3.Transform(verts[face.B], tile.Transform));
                    AppendWalkmeshVertex(target, Vector3.Transform(verts[face.C], tile.Transform));
                }
            }

            _walkmeshWalkableVertexCount = walkable.Count / FloatsPerVertex;
            _walkmeshBlockedVertexCount = blocked.Count / FloatsPerVertex;
            if (_walkmeshWalkableVertexCount == 0 && _walkmeshBlockedVertexCount == 0)
                return;

            var data = new float[walkable.Count + blocked.Count];
            walkable.CopyTo(data, 0);
            blocked.CopyTo(data, walkable.Count);

            _walkmeshVao = _gl!.GenVertexArray();
            _walkmeshVbo = _gl.GenBuffer();
            _gl.BindVertexArray(_walkmeshVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _walkmeshVbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * sizeof(float)),
                new ReadOnlySpan<float>(data), BufferUsageARB.StaticDraw);
            SetVertexAttribPointers();
            _gl.BindVertexArray(0);

            _hasWalkmeshBuffer = true;
        }

        /// <summary>Appends one overlay vertex (position lifted above the floor, an up-normal, zero UV) in the shared 8-float layout.</summary>
        private static void AppendWalkmeshVertex(List<float> target, Vector3 world)
        {
            target.Add(world.X);
            target.Add(world.Y);
            target.Add(world.Z + WalkmeshHeightOffset);
            target.Add(0f);
            target.Add(0f);
            target.Add(1f);
            target.Add(0f);
            target.Add(0f);
        }

        private void DeleteWalkmeshBuffer()
        {
            if (!_hasWalkmeshBuffer || _gl == null)
                return;

            _gl.DeleteVertexArray(_walkmeshVao);
            _gl.DeleteBuffer(_walkmeshVbo);
            _hasWalkmeshBuffer = false;
            _walkmeshWalkableVertexCount = 0;
            _walkmeshBlockedVertexCount = 0;
        }

        private void RebuildPolygonBuffer(AreaScene scene)
        {
            DeletePolygonBuffer();

            var vertexFloats = new List<float>();
            var ranges = new List<(int Start, int Count)>();

            foreach (var marker in scene.Instances)
            {
                if (marker.Geometry == null || marker.Geometry.Count < 2)
                    continue;

                var start = vertexFloats.Count / FloatsPerVertex;
                foreach (var point in marker.Geometry)
                {
                    vertexFloats.Add(point.X);
                    vertexFloats.Add(point.Y);
                    vertexFloats.Add(point.Z + PolygonHeightOffset);
                    vertexFloats.Add(0f);
                    vertexFloats.Add(0f);
                    vertexFloats.Add(1f);
                    vertexFloats.Add(0f);
                    vertexFloats.Add(0f);
                }

                ranges.Add((start, marker.Geometry.Count));
            }

            _polygonRanges = ranges;

            if (vertexFloats.Count == 0)
                return;

            _polygonVao = _gl!.GenVertexArray();
            _polygonVbo = _gl.GenBuffer();

            _gl.BindVertexArray(_polygonVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _polygonVbo);
            var data = vertexFloats.ToArray();
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * sizeof(float)),
                new ReadOnlySpan<float>(data), BufferUsageARB.StaticDraw);
            SetVertexAttribPointers();
            _gl.BindVertexArray(0);

            _hasPolygonBuffer = true;
        }
    }
}
