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
    /// Read-only 3D viewport for one area's <see cref="AreaScene"/> (WP4.5): renders tile-grid
    /// placements (batched per distinct <see cref="RenderModel"/> via <see cref="AreaDrawBatcher"/>)
    /// plus placed-instance markers, with an orbit/pan/zoom camera framed on the area's tile-grid
    /// bounds (<see cref="AreaCameraMath"/>). Follows the same <see cref="OpenGlControlBase"/> +
    /// Silk.NET.OpenGL skeleton as Radoub.UI's <c>ModelPreviewGLControl</c> (see
    /// Viewport/README.md) but is a fresh implementation tailored to a scene of many placements
    /// sharing a handful of distinct meshes, rather than one model per control.
    /// WP5.1 adds read-only picking: a plain left-click (press+release with &lt;4px of movement)
    /// raises <see cref="InstancePicked"/> with the hit instance (or null for empty space), using
    /// the same view/projection the last frame rendered with (<see cref="AreaCameraMath.ScreenPointToRay"/>
    /// + <see cref="AreaPicking"/>). <see cref="SelectedInstance"/> draws a wireframe highlight box
    /// around the current selection. This control still never mutates the scene or the underlying
    /// documents - editing/gizmos are WP5.2.
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
        private const float PolygonHeightOffset = 0.05f; // lift trigger/encounter outlines slightly above the tile floor
        private const float OrbitSensitivity = 0.01f; // radians per pixel
        private const float FallbackCubeHeight = 1.5f;

        /// <summary>
        /// With "hide ceilings" on, tile fragments higher than this above their own tile's base
        /// height are discarded — removes interior ceilings (walls top out around 5m) while
        /// staying correct per height level in multi-elevation areas.
        /// </summary>
        private const float CeilingClipHeight = 4.0f;
        private const float CeilingClipDisabled = 1e9f;
        private const float MarkerHalfWidth = 0.4f;
        private const float MarkerHeight = 1.2f;
        private const float MarkerGroundOffset = 0.05f;

        /// <summary>Net press-to-release pointer movement (logical px) below which a left button press+release is treated as a pick click rather than a (degenerate/aborted) orbit drag.</summary>
        private const float ClickDragThresholdPixels = 4f;

        private static readonly Vector3 LightDir = Vector3.Normalize(new Vector3(0.35f, -0.5f, 0.8f));
        private static readonly Vector3 LightColor = new(0.55f, 0.55f, 0.55f);
        private static readonly Vector3 AmbientColor = new(0.55f, 0.55f, 0.55f);
        private static readonly Vector3 UntexturedTileColor = new(0.6f, 0.6f, 0.6f);
        private static readonly Vector3 FallbackTileColor = new(0.95f, 0.15f, 0.55f);
        private static readonly Vector3 PolygonOverlayColor = new(1f, 0.65f, 0.15f);
        private static readonly Vector3 SelectionHighlightColor = new(1f, 0.95f, 0.2f);

        // ----- GLSL source (kept inline per the WP4.5 brief; adapted from, not shared with,
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
uniform float ceilingClipZ;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambientColor;

void main()
{
    // Hide-ceilings support: fragments above the per-draw clip height are discarded
    // (set to a huge value when the toggle is off).
    if (WorldPos.z > ceilingClipZ)
        discard;

    vec4 texColor = hasTexture ? texture(diffuseTexture, TexCoord) : vec4(flatColor, 1.0);

    if (alphaCutoff > 0.0 && texColor.a < alphaCutoff)
        discard;

    if (unlit)
    {
        FragColor = vec4(texColor.rgb, 1.0);
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

        private StaticMeshBuffer? _fallbackCubeBuffer;
        private StaticMeshBuffer? _markerMeshBuffer;

        private uint _polygonVao;
        private uint _polygonVbo;
        private bool _hasPolygonBuffer;
        private List<(int Start, int Count)> _polygonRanges = new();

        private uint _highlightVao;
        private uint _highlightVbo;
        private bool _hasHighlightBuffer;

        private AreaScene? _scene;
        private IReadOnlyList<AreaDrawBatcher.TileBatch>? _tileBatches;
        private bool _sceneDirty;

        private int _viewportWidth;
        private int _viewportHeight;

        /// <summary>View/projection from the most recently rendered frame - kept for picking (<see cref="RaiseInstancePicked"/>), which runs on a click rather than every frame.</summary>
        private Matrix4x4 _lastView = Matrix4x4.Identity;
        private Matrix4x4 _lastProjection = Matrix4x4.Identity;

        // ----- Orbit camera state -----
        private Vector3 _target;
        private float _azimuth = MathF.PI * 1.25f;
        private float _elevation = AreaCameraMath.DefaultElevationRadians;
        private float _distance = 50f;
        private float _initialDistance = 50f;

        private enum DragMode { None, Orbit, Pan, Move, Rotate }
        private DragMode _dragMode = DragMode.None;
        private Point _lastPointerPos;

        // ----- Picking (WP5.1) -----
        private Point _pressStartPos;
        private bool _isClickCandidate;
        private InstanceMarker? _selectedInstance;

        // ----- Move/rotate gizmo (WP5.2) -----
        private InstanceMarker? _manipulationOriginal;
        private InstanceMarker? _manipulationPreview;
        private float _manipulationHeadingRadians;
        private bool _manipulationCancelled;

        // ----- Place-from-palette (WP5.2) -----
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
        /// Raised once a move-gizmo drag releases (WP5.2): the instance that was selected when the
        /// drag started, and its final world position (Z unchanged - the move gizmo only edits X/Y).
        /// Not raised when the drag ended with no net change (e.g. a press+release with no motion).
        /// The host view is expected to commit this through the matching instance-list section's
        /// InstanceFieldMap.SetPosition path as one transaction, then refresh the scene.
        /// </summary>
        public event Action<InstanceMarker, Vector3>? InstanceMoved;

        /// <summary>Mirrors <see cref="InstanceMoved"/> for the rotate gizmo: the instance and its final (cos,sin) heading.</summary>
        public event Action<InstanceMarker, Vector2>? InstanceRotated;

        /// <summary>
        /// Whether a viewport click should place a new instance instead of picking/orbiting (WP5.2
        /// "Place..." flow). The host view sets this once a palette blueprint has been chosen;
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

        private bool _hideCeilings;

        /// <summary>Discards tile geometry above each tile's own base height + ~4m (interior ceilings).</summary>
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

        private bool _showPlaceableModels = true;

        /// <summary>
        /// When true (default), placeable instances with resolved geometry render their actual 3D
        /// model; when false they fall back to their kind-colored pyramid marker (useful when
        /// models visually bury the editing markers). The same switch is planned for creatures
        /// once creature appearance models render in the area view.
        /// </summary>
        public bool ShowPlaceableModels
        {
            get => _showPlaceableModels;
            set
            {
                if (_showPlaceableModels == value)
                    return;

                _showPlaceableModels = value;
                RequestNextFrameRendering();
            }
        }

        /// <summary>True when this instance should draw its resolved model rather than a marker.</summary>
        private bool DrawsAsModel(InstanceMarker instance) =>
            instance.Model != null &&
            (_showPlaceableModels || instance.Kind != InstanceMarkerKind.Placeable);

        /// <summary>Layered resource index used to resolve tile/mesh textures and MTR materials. Null degrades every mesh to a flat gray fallback.</summary>
        public ResourceIndex? ResourceIndex { get; set; }

        /// <summary>The scene to render, or null to show an empty viewport. Setting this recomputes the initial camera framing and marks GPU state for rebuild on the next render.</summary>
        public AreaScene? Scene
        {
            get => _scene;
            set
            {
                _scene = value;
                _sceneDirty = true;

                if (value != null)
                    ResetCameraForScene(value);

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

        // ----- Pointer input: left-drag orbit, middle/right/shift-left-drag pan, wheel zoom -----
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

            // Placement mode (WP5.2): a right-click cancels rather than panning the camera.
            if (_isPlacementActive && props.IsRightButtonPressed)
            {
                CancelPlacement();
                e.Handled = true;
                return;
            }

            // Move/rotate gizmo (WP5.2): a plain left press landing ON the current selection
            // starts an object-manipulation drag - the left button is the primary "grab", matching
            // modern editors where you left-drag an object to move it (Alt to rotate). Hit-test the
            // press against the selection first; any other press (empty space, shift, or another
            // button) falls through to the camera navigation below.
            if (!_isPlacementActive && props.IsLeftButtonPressed && !shift
                && _selectedInstance != null && TryHitSelectedInstance(pos))
            {
                BeginManipulation(_selectedInstance, alt ? DragMode.Rotate : DragMode.Move);
                _lastPointerPos = pos;
                _pressStartPos = pos;
                _isClickCandidate = false;
                Focus();
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }

            // Modern-app camera convention: left-drag pans (grab-and-drag the view, like dragging a
            // map), while the right/middle buttons orbit. Shift+left also orbits, keeping an orbit
            // path for laptop/trackpad users without a second mouse button. This reverses the legacy
            // Aurora toolset, where the primary button orbited.
            if (props.IsRightButtonPressed || props.IsMiddleButtonPressed || (props.IsLeftButtonPressed && shift))
                _dragMode = DragMode.Orbit;
            else if (props.IsLeftButtonPressed)
                _dragMode = DragMode.Pan;
            else
                return;

            _lastPointerPos = pos;
            _pressStartPos = pos;
            // Only a plain left press (which became a pan drag) is eligible to resolve into a pick
            // click on release; an orbit-triggering press never picks.
            _isClickCandidate = _dragMode == DragMode.Pan;
            Focus();
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        public void HandlePointerMoved(PointerEventArgs e)
        {
            if (_dragMode == DragMode.None)
                return;

            var pos = e.GetPosition(this);
            var dx = (float)(pos.X - _lastPointerPos.X);
            var dy = (float)(pos.Y - _lastPointerPos.Y);
            _lastPointerPos = pos;

            switch (_dragMode)
            {
                case DragMode.Orbit:
                    _azimuth += dx * OrbitSensitivity;
                    _elevation = AreaCameraMath.ClampElevation(_elevation - dy * OrbitSensitivity);
                    break;

                case DragMode.Pan:
                    var worldPerPixel = AreaCameraMath.WorldUnitsPerPixel(_distance, VerticalFovRadians, _viewportHeight);
                    _target += AreaCameraMath.PanDelta(_azimuth, dx, dy, worldPerPixel);
                    break;

                case DragMode.Move:
                    UpdateMovePreview(pos, (e.KeyModifiers & KeyModifiers.Control) != 0);
                    break;

                case DragMode.Rotate:
                    UpdateRotatePreview(dx);
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
            if (_scene == null || _viewportWidth <= 0 || _viewportHeight <= 0)
            {
                InstancePicked?.Invoke(null);
                return;
            }

            var ray = AreaCameraMath.ScreenPointToRay(
                new Vector2((float)screenPos.X, (float)screenPos.Y),
                _viewportWidth, _viewportHeight, _lastView, _lastProjection);

            var hit = AreaPicking.PickClosestInstance(ray, _scene, _showPlaceableModels);
            InstancePicked?.Invoke(hit);
        }

        // ----- Move/rotate gizmo (WP5.2) -----

        /// <summary>Builds a ray from the last rendered frame's view/projection for the given screen point, or null before anything has ever rendered.</summary>
        private PickRay? TryBuildRay(Point screenPos)
        {
            if (_viewportWidth <= 0 || _viewportHeight <= 0)
                return null;

            return AreaCameraMath.ScreenPointToRay(
                new Vector2((float)screenPos.X, (float)screenPos.Y), _viewportWidth, _viewportHeight, _lastView, _lastProjection);
        }

        /// <summary>Whether a press at <paramref name="screenPos"/> lands on <see cref="_selectedInstance"/> specifically (not the whole scene), using the same marker-vs-model rule everything else here uses.</summary>
        private bool TryHitSelectedInstance(Point screenPos)
        {
            if (_scene == null || _selectedInstance is not { } selected)
                return false;

            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return false;

            return AreaPicking.PickInstance(ray.Value, selected, DrawsAsModel(selected)) != null;
        }

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
            Geometry = source.Geometry,
            Model = source.Model
        };

        /// <summary>Live move preview: reprojects the current screen position onto the horizontal plane at the instance's original Z (Z never changes during a move), optionally grid-snapping X/Y while Ctrl is held.</summary>
        private void UpdateMovePreview(Point screenPos, bool snap)
        {
            // Esc already cancelled this drag (_manipulationPreview cleared) - a mouse move that
            // arrives before the button is actually released must not revive the preview.
            if (_manipulationCancelled || _manipulationOriginal is not { } original)
                return;

            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return;

            if (AreaManipulation.IntersectRayWithHorizontalPlane(ray.Value, original.Position.Z) is not { } hit)
                return; // Ray parallel to the plane this frame - keep the previous preview rather than snapping to a bogus point.

            var position = snap ? AreaManipulation.SnapToGridXy(hit, AreaManipulation.DefaultGridSnapMeters) : hit;
            _manipulationPreview = ClonePreview(original, new Vector3(position.X, position.Y, original.Position.Z), original.Orientation);
        }

        /// <summary>Live rotate preview: accumulates heading from horizontal drag movement, matching the orbit camera's own pixel-to-radians feel.</summary>
        private void UpdateRotatePreview(float dxPixels)
        {
            if (_manipulationCancelled || _manipulationOriginal is not { } original)
                return;

            _manipulationHeadingRadians += dxPixels * AreaManipulation.RotateRadiansPerPixel;
            var orientation = AreaManipulation.HeadingToOrientation(_manipulationHeadingRadians);
            _manipulationPreview = ClonePreview(original, original.Position, orientation);
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

        // ----- Place-from-palette (WP5.2) -----

        private void CancelPlacement()
        {
            if (!_isPlacementActive)
                return;

            _isPlacementActive = false;
            PlacementCancelled?.Invoke();
        }

        private void RaisePlacementPointPicked(Point screenPos)
        {
            var ray = TryBuildRay(screenPos);
            if (ray == null)
                return;

            if (AreaManipulation.IntersectRayWithHorizontalPlane(ray.Value, 0f) is not { } point)
                return;

            _isPlacementActive = false;
            PlacementPointPicked?.Invoke(point);
        }

        public void HandlePointerWheel(PointerWheelEventArgs e)
        {
            // Wheel up (positive delta) zooms IN (shrinks distance) per common convention.
            var factor = (float)Math.Pow(1.1, -e.Delta.Y);
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

        /// <summary>Esc cancels an in-progress manipulation drag (reverting to the instance's real position/heading) or an active placement (WP5.2).</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

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

                BuildStaticMeshes();

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

                    foreach (var buffer in _modelBuffers.Values)
                        DeleteBuffer(buffer.Vao, buffer.Vbo, buffer.Ebo);
                    _modelBuffers.Clear();

                    if (_fallbackCubeBuffer is { } cube)
                        DeleteBuffer(cube.Vao, cube.Vbo, cube.Ebo);
                    if (_markerMeshBuffer is { } marker)
                        DeleteBuffer(marker.Vao, marker.Vbo, marker.Ebo);

                    DeletePolygonBuffer();

                    if (_hasHighlightBuffer)
                    {
                        _gl.DeleteVertexArray(_highlightVao);
                        _gl.DeleteBuffer(_highlightVbo);
                        _hasHighlightBuffer = false;
                    }

                    if (_shaderProgram != 0)
                        _gl.DeleteProgram(_shaderProgram);
                    _shaderProgram = 0;
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

            if (_scene == null)
                return;

            try
            {
                if (_sceneDirty)
                {
                    RebuildPolygonBuffer(_scene);
                    _tileBatches = AreaDrawBatcher.GroupByModel(_scene.Tiles);
                    _sceneDirty = false;
                }

                DrawScene(width, height);
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

        // ----- Uniform helpers (locations are looked up per-call, matching Radoub.UI's own
        // ModelPreviewGLControl approach - draw-call counts per area are modest enough that
        // caching uniform locations is not worth the added bookkeeping). -----

        private void SetUniformMatrix4(string name, Matrix4x4 matrix)
        {
            var location = _gl!.GetUniformLocation(_shaderProgram, name);
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
            var location = _gl!.GetUniformLocation(_shaderProgram, name);
            if (location >= 0)
                _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        private void SetUniformBool(string name, bool value)
        {
            var location = _gl!.GetUniformLocation(_shaderProgram, name);
            if (location >= 0)
                _gl.Uniform1(location, value ? 1 : 0);
        }

        private void SetUniformFloat(string name, float value)
        {
            var location = _gl!.GetUniformLocation(_shaderProgram, name);
            if (location >= 0)
                _gl.Uniform1(location, value);
        }

        private void SetUniformInt(string name, int value)
        {
            var location = _gl!.GetUniformLocation(_shaderProgram, name);
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

        private void DrawScene(int width, int height)
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

            _gl!.UseProgram(_shaderProgram);
            SetUniformMatrix4("view", view);
            SetUniformMatrix4("projection", projection);
            SetUniformVec3("lightDir", LightDir);
            SetUniformVec3("lightColor", LightColor);
            SetUniformVec3("ambientColor", AmbientColor);
            SetUniformInt("diffuseTexture", 0);
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);

            DrawTileBatches();

            // Markers and trigger outlines are never ceiling-clipped — reset the sticky per-tile value.
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);
            DrawInstanceMarkers();
            DrawPolygonOverlays();
            DrawSelectionHighlight();

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

        private void DrawInstanceMarkers()
        {
            if (_scene == null)
                return;

            // Pass 1: instances with resolved render geometry (placeables, doors) draw their
            // actual model, textured and lit, at the instance's position/heading (or its live
            // WP5.2 manipulation preview, while a move/rotate drag is in progress on it).
            foreach (var raw in _scene.Instances)
            {
                if (!DrawsAsModel(raw))
                    continue;

                var instance = Displayed(raw);
                var heading = MathF.Atan2(instance.Orientation.Y, instance.Orientation.X);
                var instanceTransform =
                    Matrix4x4.CreateRotationZ(heading) * Matrix4x4.CreateTranslation(instance.Position);

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

            // Pass 2: everything else draws its kind-colored pyramid marker.
            if (_markerMeshBuffer is not { } marker)
                return;

            _gl!.BindVertexArray(marker.Vao);
            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);

            foreach (var raw in _scene.Instances)
            {
                if (DrawsAsModel(raw))
                    continue;

                var instance = Displayed(raw);
                var heading = MathF.Atan2(instance.Orientation.Y, instance.Orientation.X);
                var model = Matrix4x4.CreateRotationZ(heading) * Matrix4x4.CreateTranslation(instance.Position);

                SetUniformMatrix4("model", model);
                SetUniformVec3("flatColor", MarkerColor(instance.Kind));

                unsafe
                {
                    _gl.DrawElements(PrimitiveType.Triangles, (uint)marker.IndexCount,
                        DrawElementsType.UnsignedInt, (void*)0);
                }
            }
        }

        private void DrawPolygonOverlays()
        {
            if (!_hasPolygonBuffer || _polygonRanges.Count == 0)
                return;

            _gl!.BindVertexArray(_polygonVao);
            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
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
        /// polygon mode, and this control already renders trigger/encounter outlines the same way
        /// (see <see cref="DrawPolygonOverlays"/>), so this stays portable to the same GL profiles.
        /// A no-op when nothing is selected or the GL context isn't ready.
        /// </summary>
        private void DrawSelectionHighlight()
        {
            if (_scene == null || _selectedInstance is not { } instance || _gl == null)
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
            SetUniformFloat("ceilingClipZ", CeilingClipDisabled);
            SetUniformVec3("flatColor", SelectionHighlightColor);
            SetUniformMatrix4("model", Matrix4x4.Identity); // bounds are already world-space

            _gl.DrawArrays(PrimitiveType.Lines, 0, 24);
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
            InstanceMarkerKind.Encounter => new Vector3(0.6f, 0.2f, 0.8f),
            InstanceMarkerKind.Item => new Vector3(0.9f, 0.85f, 0.2f),
            InstanceMarkerKind.Placeable => new Vector3(0.2f, 0.45f, 0.9f),
            InstanceMarkerKind.Sound => new Vector3(0.2f, 0.8f, 0.8f),
            InstanceMarkerKind.Store => new Vector3(0.2f, 0.8f, 0.3f),
            InstanceMarkerKind.Trigger => new Vector3(0.95f, 0.55f, 0.15f),
            InstanceMarkerKind.Waypoint => new Vector3(0.9f, 0.9f, 0.9f),
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

            string resolvedName;
            try
            {
                resolvedName = MaterialResolver.ResolveDiffuseTextureName(ResourceIndex, rawTextureName);
            }
            catch (Exception)
            {
                resolvedName = rawTextureName;
            }

            if (_textureCache.TryGetValue(resolvedName, out var cached))
                return cached;

            var result = LoadAndUploadTexture(resolvedName);
            _textureCache[resolvedName] = result;
            return result;
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
        /// Cheap subset of TXI transparency honoring (per the WP4.5 brief): a punch-through
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
        /// A box spanning local [0,TileSize] x [0,TileSize] x [0,FallbackCubeHeight] - matching the
        /// same corner-origin convention real tile MDLs use (see <see cref="AreaSceneBuilder"/>'s
        /// Transform doc: it recenters via translate(-TileSize/2,-TileSize/2,0) before rotating).
        /// Applying a fallback placement's Transform to this box fills the same 10m footprint a
        /// real tile model would, making a missing/unresolvable tile obvious at a glance rather
        /// than a near-invisible 1m cube lost inside the grid.
        /// </summary>
        private static (float[] Vertices, uint[] Indices) BuildFallbackCubeMesh()
        {
            const float size = AreaSceneBuilder.TileSize;
            const float h = FallbackCubeHeight;

            var c = new[]
            {
                new Vector3(0, 0, 0), new Vector3(size, 0, 0), new Vector3(size, size, 0), new Vector3(0, size, 0),
                new Vector3(0, 0, h), new Vector3(size, 0, h), new Vector3(size, size, h), new Vector3(0, size, h)
            };

            var builder = new BoxMeshBuilder();
            builder.AddQuad(c[3], c[2], c[1], c[0], new Vector3(0, 0, -1)); // bottom
            builder.AddQuad(c[4], c[5], c[6], c[7], new Vector3(0, 0, 1));  // top
            builder.AddQuad(c[0], c[1], c[5], c[4], new Vector3(0, -1, 0)); // front (y=0)
            builder.AddQuad(c[2], c[3], c[7], c[6], new Vector3(0, 1, 0));  // back (y=size)
            builder.AddQuad(c[3], c[0], c[4], c[7], new Vector3(-1, 0, 0)); // left (x=0)
            builder.AddQuad(c[1], c[2], c[6], c[5], new Vector3(1, 0, 0));  // right (x=size)

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

        // ----- Trigger/encounter polygon overlays (scene-specific; rebuilt whenever the scene changes) -----

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
