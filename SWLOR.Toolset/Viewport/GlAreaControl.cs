using System.Numerics;
using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Silk.NET.OpenGL;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// 3D viewport for one area's <see cref="AreaScene"/>: renders tile-grid
    /// placements (batched per distinct <see cref="RenderModel"/> via <see cref="AreaDrawBatcher"/>)
    /// plus placed-instance markers, with an orbit/pan/zoom camera framed on the area's tile-grid
    /// bounds (<see cref="AreaCameraMath"/>). Follows the <see cref="OpenGlControlBase"/> +
    /// Silk.NET.OpenGL lifecycle documented in Viewport/README.md and is tailored to a scene of
    /// many placements sharing a handful of distinct meshes.
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
        private const float OrbitYawDegreesPerSecond = 247f;

        /// <summary>
        /// The same idea for pitch, which Aurora turns far more slowly - about 0.72 degrees per motion
        /// sample against yaw's 3.3, measured as 17 degrees of elevation over a 150px vertical drag.
        /// </summary>
        private const float OrbitPitchDegreesPerSecond = 48f;

        /// <summary>Timestamp of the last orbit step, so the rate above is per second and not per event.</summary>
        private long _lastOrbitTicks;
        private const float FallbackCubeHeight = 1.5f;

        private const float MarkerHalfWidth = 0.4f;
        private const float MarkerHeight = 1.2f;
        private const float MarkerGroundOffset = 0.05f;

        // Aurora keeps runtime-invisible area-transition doors editable as a translucent
        // lavender plane in the doorway. The model's authored hidden geometry supplies the shape;
        // DoorTransitionMarker supplies the fixed 2m x 3m fallback when that MDL is unavailable.
        private static readonly Vector3 DoorTransitionColor = new(0.52f, 0.52f, 0.82f);
        private const float DoorTransitionAlpha = 0.42f;

        /// <summary>Net press-to-release pointer movement (logical px) below which a left button press+release is treated as a pick click rather than a (degenerate/aborted) orbit drag.</summary>
        private const float ClickDragThresholdPixels = 4f;

        private static readonly Vector3 LightDir = Vector3.Normalize(new Vector3(0.35f, -0.5f, 0.8f));

        // Editor lighting: the area's authored ambient/diffuse colors drive hue and mood,
        // but authored night colors are near-black - too dark to edit in - so each channel is
        // lifted from a floor toward its true value (raw 0 -> floor, raw 1 -> unchanged). Tunable;
        // the human gate calibrates the feel.
        /// <summary>
        /// The light used when the area's own is switched off: a flat white key plus generous fill, so
        /// every surface reads at close to its texture's own colour. This is what makes the viewport
        /// match Aurora's, which lights its preview the same way rather than through the area.
        /// </summary>
        private static readonly Vector3 NeutralAmbient = new(0.62f, 0.62f, 0.62f);

        private static readonly Vector3 NeutralDiffuse = new(0.55f, 0.55f, 0.55f);

        private const float AmbientLightFloor = 0.25f;
        private const float DiffuseLightFloor = 0.20f;
        /// <summary>The empty-space colour behind the scene.</summary>
        private static readonly Vector3 ViewportBackground = new(0.12f, 0.14f, 0.18f);
        /// <summary>Aurora's neutral grey background for isolated model and item previews.</summary>
        private static readonly Vector3 AuroraPreviewBackground = new(0.4f, 0.4f, 0.4f);
        private static readonly Vector3 UntexturedTileColor = new(0.6f, 0.6f, 0.6f);
        private static readonly Vector3 FallbackTileColor = new(0.95f, 0.15f, 0.55f);
        // Aurora draws trigger outlines unlit at #6666CC (sampled off the reference toolset in
        // both a lit interior and full daylight - the colour does not vary with area lighting).
        private static readonly Vector3 PolygonOverlayColor = new(0.4f, 0.4f, 0.8f);

        // Aurora's sound-range blue (#66B1FD, sampled the same way), shared by the dotted
        // MinDistance sphere, its solid equator ring, and the flat MaxDistance circle.
        private static readonly Vector3 SoundRangeColor = new(0.40f, 0.69f, 0.99f);

        // The reference toolset marks a sound with a small upright musical note: red head,
        // black stem and flag.
        private static readonly Vector3 SoundNoteHeadColor = new(0.80f, 0.06f, 0.06f);
        private static readonly Vector3 SoundNoteStemColor = new(0.05f, 0.05f, 0.05f);
        private const float SoundNoteHeightMeters = 1.6f;
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

        // ----- GLSL source (kept inline because this renderer owns its alpha-cutoff and unlit
        // uniforms and their viewport-specific behavior). -----
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
uniform vec2 uvScale;
uniform vec2 uvOffset;

void main()
{
    Normal = mat3(model) * aNormal;
    TexCoord = aTexCoord * uvScale + uvOffset;
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
uniform sampler2D normalTexture;
uniform sampler2D specularTexture;
uniform sampler2D roughnessTexture;
uniform sampler2D environmentTexture;
uniform sampler2D tintMapTexture;
uniform sampler2D tintPaletteTexture;
uniform sampler2D tintAlphaTexture;
uniform bool hasTexture;
uniform bool hasTintMap;
uniform bool hasTintAlpha;
uniform bool tintAlphaUsesRedChannel;
uniform bool hasNormalMap;
uniform bool hasSpecularMap;
uniform bool hasRoughnessMap;
uniform bool hasEnvironmentMap;
uniform vec3 flatColor;
uniform bool unlit;
uniform float alphaCutoff;
uniform float flatAlpha;
uniform bool useTextureAlpha;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambientColor;
uniform vec3 fogColor;
uniform float fogDensity;
uniform vec3 cameraPos;
uniform mat4 view;
uniform vec4 tintColor0;
uniform vec4 tintColor1;
uniform vec4 tintColor2;
uniform vec4 tintColor3;
uniform vec4 tintColor4;
uniform vec4 tintColor5;
uniform vec4 tintColor6;
uniform vec4 tintColor7;
uniform vec4 tintColor8;
uniform vec4 tintColor9;
uniform float tintPaletteRow0;
uniform float tintPaletteRow1;
uniform float tintPaletteRow2;
uniform float tintPaletteRow3;
uniform float tintPaletteRow4;
uniform float tintPaletteRow5;
uniform float tintPaletteRow6;
uniform float tintPaletteRow7;
uniform float tintPaletteRow8;
uniform float tintPaletteRow9;

// Blinn-Phong exponent for a specular-mapped highlight when the material carries no
// roughness map. One shared value rather than a material parameter: MTR files carry no
// shininess figure of their own.
const float DefaultSpecularShininess = 32.0;

// Apply the tangent-space normal map to the interpolated geometric normal. The tangent basis
// is derived per-fragment from screen-space position/UV derivatives (Schueler's cotangent
// frame) because the shared 8-float vertex layout carries no tangent attribute - this keeps
// every mesh-building and batching path untouched.
vec3 PerturbNormal(vec3 geomNormal, vec2 uv)
{
    vec3 dp1 = dFdx(WorldPos);
    vec3 dp2 = dFdy(WorldPos);
    vec2 duv1 = dFdx(uv);
    vec2 duv2 = dFdy(uv);

    vec3 dp2perp = cross(dp2, geomNormal);
    vec3 dp1perp = cross(geomNormal, dp1);
    vec3 tangent = dp2perp * duv1.x + dp1perp * duv2.x;
    vec3 bitangent = dp2perp * duv1.y + dp1perp * duv2.y;

    // Degenerate UVs (a mesh face with no UV area) would make inversesqrt blow up to NaN;
    // fall back to the unperturbed normal instead.
    float maxLenSq = max(dot(tangent, tangent), dot(bitangent, bitangent));
    if (maxLenSq <= 0.0)
        return geomNormal;

    float invMax = inversesqrt(maxLenSq);
    mat3 tbn = mat3(tangent * invMax, bitangent * invMax, geomNormal);
    // The content's normal maps are OpenGL-convention (green = +V), which is exactly what this
    // frame expects: the bitangent axis above is the +V direction, so the sample is used
    // unflipped. Only a DirectX-authored map (green = -V) would need mapNormal.y negated here.
    vec3 mapNormal = texture(normalTexture, uv).xyz * 2.0 - 1.0;
    return normalize(tbn * mapNormal);
}

// Aurora's legacy environment maps are mirrored-sphere images rather than cubemaps. Reproduce
// fixed-function GL_SPHERE_MAP coordinates in eye space so chrome1 moves across the surface as
// either the camera or the model turns.
vec3 SampleEnvironmentMap(vec3 worldNormal)
{
    vec3 incident = normalize(WorldPos - cameraPos);
    vec3 reflected = normalize(mat3(view) * reflect(incident, worldNormal));
    float denominator = 2.0 * sqrt(max(
        reflected.x * reflected.x +
        reflected.y * reflected.y +
        (reflected.z + 1.0) * (reflected.z + 1.0),
        0.000001));
    vec2 sphereUv = reflected.xy / denominator + vec2(0.5);
    return texture(environmentTexture, sphereUv).rgb;
}

vec4 ResolveTintMapColor()
{
    vec2 size = vec2(textureSize(tintMapTexture, 0));
    vec2 wrappedUv = fract(TexCoord);
    vec2 nearestUv = (floor(wrappedUv * size) + vec2(0.5)) / max(size, vec2(1.0));
    float shade = textureLod(tintMapTexture, wrappedUv, 0.0).r;
    float encodedLayer = textureLod(tintMapTexture, nearestUv, 0.0).g;
    float layer = floor(clamp(encodedLayer, 0.0, 0.9999) * 10.0);

    vec4 custom = tintColor0;
    float paletteRow = tintPaletteRow0;
    float referenceRow = 0.000244;
    if      (layer > 8.5) { custom = tintColor9; paletteRow = tintPaletteRow9; referenceRow = 0.515869; }
    else if (layer > 7.5) { custom = tintColor8; paletteRow = tintPaletteRow8; referenceRow = 0.515869; }
    else if (layer > 6.5) { custom = tintColor7; paletteRow = tintPaletteRow7; referenceRow = 0.429932; }
    else if (layer > 5.5) { custom = tintColor6; paletteRow = tintPaletteRow6; referenceRow = 0.429932; }
    else if (layer > 4.5) { custom = tintColor5; paletteRow = tintPaletteRow5; referenceRow = 0.343994; }
    else if (layer > 3.5) { custom = tintColor4; paletteRow = tintPaletteRow4; referenceRow = 0.343994; }
    else if (layer > 2.5) { custom = tintColor3; paletteRow = tintPaletteRow3; referenceRow = 0.258057; }
    else if (layer > 1.5) { custom = tintColor2; paletteRow = tintPaletteRow2; referenceRow = 0.172119; }
    else if (layer > 0.5) { custom = tintColor1; paletteRow = tintPaletteRow1; referenceRow = 0.086182; }

    float paletteU = (shade * 255.0 + 0.5) / 256.0;
    vec4 paletteColor = textureLod(
        tintPaletteTexture,
        vec2(paletteU, paletteRow),
        0.0);
    if (custom.a > 0.5)
    {
        vec3 referenceShade = textureLod(
            tintPaletteTexture,
            vec2(paletteU, referenceRow),
            0.0).rgb;
        vec3 referenceMidpoint = textureLod(
            tintPaletteTexture,
            vec2(128.5 / 256.0, referenceRow),
            0.0).rgb;
        const vec3 luminanceWeights = vec3(0.2126, 0.7152, 0.0722);
        float shadeScale = max(
            dot(referenceShade, luminanceWeights) /
                max(dot(referenceMidpoint, luminanceWeights), 1.0 / 255.0),
            0.0);
        paletteColor.rgb = clamp(custom.rgb * shadeScale, 0.0, 1.0);
        // A direct RGB choice must not inherit the hidden preset row's reflection mask. Without
        // this, the same custom color can turn chrome/grey depending on the preset selected before
        // it. Presets retain their authored PLT environment coverage through paletteColor.a.
        paletteColor.a = 1.0;
    }
    return paletteColor;
}

void main()
{
    vec4 texColor = hasTintMap
        ? ResolveTintMapColor()
        : hasTexture ? texture(diffuseTexture, TexCoord) : vec4(flatColor, 1.0);
    float environmentDiffuseCoverage = texColor.a;

    if (hasTintAlpha)
    {
        vec4 alphaSample = texture(tintAlphaTexture, TexCoord);
        texColor.a = tintAlphaUsesRedChannel ? alphaSample.r : alphaSample.a;
    }

    if (alphaCutoff > 0.0 && texColor.a < alphaCutoff)
        discard;

    if (unlit)
    {
        // flatAlpha defaults to 1.0 for every opaque unlit draw (markers, outlines, selection box);
        // the translucent walkmesh overlay is the only pass that lowers it.
        FragColor = vec4(texColor.rgb, flatAlpha * (useTextureAlpha ? texColor.a : 1.0));
        return;
    }

    vec3 norm = normalize(Normal);
    // Gated on hasTexture as well as the map flags: draw paths that render flat-colored lit
    // geometry (fallback cubes) never touch the map uniforms, so only the textured mesh path -
    // which sets them on every bind - can turn these on.
    if (hasTexture && hasNormalMap)
        norm = PerturbNormal(norm, TexCoord);

    // Two-sided lighting (abs, not max) - NWN tile/prop meshes have inconsistent winding.
    float diff = abs(dot(norm, lightDir));
    vec3 result = (ambientColor + diff * lightColor) * texColor.rgb;
    if (hasTexture && hasEnvironmentMap)
    {
        // Aurora renders this as two passes: an unlit environment map first, then the normally
        // lit diffuse texture source-alpha blended over it. PLT alpha therefore means diffuse
        // coverage (zero = full reflection), not transparency. Applying diffuse lighting after
        // this mix incorrectly dims the reflective metal regions.
        result = mix(SampleEnvironmentMap(norm), result, environmentDiffuseCoverage);
    }

    if (hasTexture && hasSpecularMap)
    {
        // Blinn-Phong half-vector highlight, tinted by the specular map. abs on the half-dot
        // for the same two-sided reason as the diffuse term. A roughness map reshapes the
        // exponent per-fragment: rough (1.0) spreads the highlight wide and dull, smooth (0.0)
        // tightens it toward a sharp gleam.
        float shininess = DefaultSpecularShininess;
        if (hasRoughnessMap)
        {
            float roughness = clamp(texture(roughnessTexture, TexCoord).r, 0.0, 1.0);
            shininess = exp2(mix(8.0, 1.0, roughness)); // 256 at mirror-smooth down to 2 at fully rough
        }

        vec3 viewDir = normalize(cameraPos - WorldPos);
        vec3 halfDir = normalize(lightDir + viewDir);
        float spec = pow(abs(dot(norm, halfDir)), shininess);
        result += spec * texture(specularTexture, TexCoord).rgb * lightColor;
    }

    if (fogDensity > 0.0)
    {
        float depth = length(WorldPos - cameraPos);
        result = mix(result, fogColor, clamp(1.0 - exp(-fogDensity * depth), 0.0, 1.0));
    }

    // flatAlpha is 1.0 for every ordinary draw; the placement ghosts lower it so the scene reads
    // through the object about to be placed.
    FragColor = vec4(result, flatAlpha * (hasTintAlpha ? texColor.a : 1.0));
}
";

        private sealed class MeshRange
        {
            public required int IndexOffset { get; init; }
            public required int IndexCount { get; init; }
            public required Matrix4x4 MeshTransform { get; init; }

            /// <summary>
            /// This mesh's transform at each frame of its model's idle, empty when it has none.
            /// <see cref="MeshTransform"/> is the resting pose - the frame the idle ends on.
            /// </summary>
            public IReadOnlyList<Matrix4x4> PoseFrames { get; init; } = Array.Empty<Matrix4x4>();

            /// <summary>
            /// Byte offsets into the shared index buffer for the matching skinned idle frames.
            /// Empty for rigid meshes, whose geometry never changes between poses.
            /// </summary>
            public IReadOnlyList<int> PoseIndexOffsets { get; init; } = Array.Empty<int>();

            public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
                new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Byte offsets into the shared index buffer for named skinned-animation frames.</summary>
            public IReadOnlyDictionary<string, IReadOnlyList<int>> AnimationIndexOffsets { get; init; } =
                new Dictionary<string, IReadOnlyList<int>>(StringComparer.OrdinalIgnoreCase);

            public string? TextureName { get; init; }

            public string? MaterialName { get; init; }

            /// <summary>Equipment-specific PLT dyes; empty means use the instance/model palette.</summary>
            public IReadOnlyDictionary<int, int> LayerColorIndices { get; init; } =
                new Dictionary<int, int>();

            public bool UsesItemTintOverrides { get; init; }

            public IReadOnlyDictionary<string, int> TintMapOverrides { get; init; } =
                new Dictionary<string, int>(StringComparer.Ordinal);

            /// <summary>
            /// The source node's MDL <c>tilefade</c> flag - see <see cref="RenderMesh.TileFade"/>.
            /// Non-zero marks the tileset's own overhead geometry, which the tile pass drops unless
            /// <see cref="ShowCeilings"/> is on.
            /// </summary>
            public int TileFade { get; init; }
        }

        private sealed class ModelBuffer
        {
            public required uint Vao { get; init; }
            public required uint Vbo { get; init; }
            public required uint Ebo { get; init; }
            public required IReadOnlyList<MeshRange> MeshRanges { get; init; }
            public IReadOnlyList<RenderAnimation> Animations { get; init; } = Array.Empty<RenderAnimation>();
            public IReadOnlyList<RenderEmitter> Emitters { get; init; } = Array.Empty<RenderEmitter>();

            /// <summary>Frame stamp of the last draw that used this buffer, for stale eviction.</summary>
            public long LastUsedFrame;
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

        /// <summary>
        /// The GL-side textures one mesh material binds: the diffuse (with its alpha cutoff) plus
        /// optional normal, specular, roughness and environment maps, each 0 when absent or
        /// unloadable.
        /// </summary>
        private readonly record struct MeshMaterial(
            uint TexId,
            float AlphaCutoff,
            TxiBlendMode Blending,
            uint NormalTexId,
            uint SpecularTexId,
            uint RoughnessTexId,
            uint EnvironmentTexId,
            uint TintMapTexId,
            uint TintPaletteTexId,
            uint TintAlphaTexId,
            bool TintAlphaUsesRedChannel,
            float TintAlphaCutoff);

        private readonly record struct UploadedDiffuse(
            uint TexId,
            float AlphaCutoff,
            string? EnvironmentMapTexture,
            TxiBlendMode Blending);

        private GL? _gl;
        private uint _shaderProgram;

        private readonly Dictionary<RenderModel, ModelBuffer> _modelBuffers = new();
        private readonly Dictionary<string, UploadedDiffuse> _textureCache =
            new(StringComparer.OrdinalIgnoreCase);

        // The dye indices of the model currently being drawn, and a stable string form of them for
        // cache keys. A PLT is only a picture once its layers are coloured, so these decide what the
        // texture actually looks like; empty for everything that carries no dyed surfaces.
        private IReadOnlyDictionary<int, int>? _layerColors;
        private string _layerColorKey = string.Empty;

        // Normal/specular map textures by their own resref, separate from _textureCache because a
        // map resref is a different resource than a diffuse and needs no alpha-cutoff resolution.
        // 0 memoizes a failed load. Cleared alongside the other caches on GL teardown.
        private readonly Dictionary<string, uint> _mapTextureCache =
            new(StringComparer.OrdinalIgnoreCase);

        // Memoize the raw-mesh-texture-name -> resolved result so the per-draw path
        // (thousands of BindMeshTexture calls per frame) skips MaterialResolver's string resolution.
        // Points at the same GL texture ids as _textureCache/_mapTextureCache; cleared alongside
        // them on GL teardown.
        private readonly Dictionary<string, MeshMaterial> _rawTextureCache =
            new(StringComparer.OrdinalIgnoreCase);

        // MTR parsing reads and decodes the resource. Keep both successful parses and misses out of
        // the per-mesh draw path; invalidating game resources clears this together with the GPU
        // material cache so a changed HAK can be reparsed.
        private readonly Dictionary<string, MtrMaterial?> _parsedMaterialCache =
            new(StringComparer.OrdinalIgnoreCase);

        private int _gameResourceInvalidationRequested;

        // GPU caches only ever grew before eviction was added: an item-preview document composes a
        // brand-new RenderModel for every part/dye edit, and each one stayed VAO/VBO/EBO-resident
        // until the tab closed. The caps are scan triggers, not hard limits - a live working set
        // larger than a cap is never evicted, only entries no frame has touched recently.
        private const int ModelBufferEvictionScanThreshold = 512;
        private const long StaleModelBufferFrames = 600;
        private const int TextureCacheResetThreshold = 2048;
        private const long TextureCacheResetCooldownFrames = 600;
        private long _frameStamp;
        private long _lastTextureCacheResetFrame;

        private StaticMeshBuffer? _fallbackCubeBuffer;
        private StaticMeshBuffer? _markerMeshBuffer;
        private StaticMeshBuffer? _doorTransitionBuffer;
        private StaticMeshBuffer? _particleQuadBuffer;

        // Sound marker geometry: a billboarded musical note (indices split into a red head range
        // followed by a black stem/flag range) plus unit-radius range graphics scaled per sound.
        private StaticMeshBuffer? _soundNoteBuffer;
        private int _soundNoteHeadIndexCount;
        private uint _soundCircleVao, _soundCircleVbo;
        private uint _soundSphereVao, _soundSphereVbo;
        private int _soundCircleVertexCount, _soundSphereVertexCount;
        private bool _hasSoundRangeBuffers;

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

        /// <summary>
        /// The tile list <see cref="_tileBatches"/> and the walkmesh buffer were built from, so a
        /// scene that carries the same list forward keeps both instead of re-uploading them. Cleared
        /// whenever that GPU state is torn down, so a rebuilt context never matches on a stale list.
        /// </summary>
        private IReadOnlyList<TilePlacement>? _batchedTiles;

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
        private Vector3 _cameraEye;

        /// <summary>
        /// A Go To request can arrive while an area is still being built. Applying it immediately
        /// would be lost when the first scene performs its initial framing, so retain the newest
        /// request until that scene has been attached.
        /// </summary>
        private Vector3? _pendingFocus;

        /// <summary>Whether this control has ever framed a scene - see the <c>Scene</c> setter.</summary>
        private bool _cameraFramed;

        /// <summary>
        /// A restored camera arrived before its first scene. That scene still supplies the baseline
        /// used to compare later rebuilds, but must not replace the restored orbit itself.
        /// </summary>
        private bool _restoredCameraAwaitingScene;

        /// <summary>Captures the current orbit camera once a scene has supplied its framing scale.</summary>
        public AreaViewportState? CaptureViewportState()
        {
            if (!_cameraFramed)
                return null;

            return new AreaViewportState(
                _target, _distance, _initialDistance, _azimuth, _elevation);
        }

        /// <summary>Restores a camera saved by the owning area document.</summary>
        public void RestoreViewportState(AreaViewportState state)
        {
            if (!IsFinite(state.Target) ||
                !float.IsFinite(state.Distance) || state.Distance <= 0f ||
                !float.IsFinite(state.InitialDistance) || state.InitialDistance <= 0f ||
                !float.IsFinite(state.Azimuth) || !float.IsFinite(state.Elevation))
                return;

            _target = state.Target;
            _initialDistance = state.InitialDistance;
            _distance = AreaCameraMath.ClampDistance(state.Distance, _initialDistance);
            _azimuth = state.Azimuth;
            _elevation = AreaCameraMath.ClampElevation(state.Elevation);
            _cameraFramed = true;
            if (Scene == null)
            {
                _framedTarget = _target;
                _framedDistance = _distance;
                _restoredCameraAwaitingScene = true;
            }
            else
            {
                // The current scene has already supplied the correct comparison baseline. Restoring
                // a panned/zoomed camera must not replace it with the user's current orbit.
                _restoredCameraAwaitingScene = false;
            }
            RequestNextFrameRendering();
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

        /// <summary>What the camera was last fitted to, so <see cref="NeedsRefit"/> can tell a
        /// same-size rebuild from a genuinely different model.</summary>
        private Vector3 _framedTarget;
        private float? _framedDistance;

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
        public event Action<PlacementPick>? PlacementPointPicked;

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

        /// <summary>
        /// The doorway the ghost has snapped itself to, for a door placement; null for everything else,
        /// and for a door in an area whose tiles declare no doorways at all.
        /// </summary>
        private TileDoorAnchor? _snappedDoorAnchor;

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
                _tileHoverEdge = null;
                RequestNextFrameRendering();
            }
        }

        private bool _tilePlacementTargetsVertex;

        /// <summary>
        /// Whether the armed palette entry paints a terrain VERTEX rather than stamping whole cells.
        /// This is how the reference toolset paints terrain: the cursor snaps to the nearest 10m grid
        /// vertex, the highlight is a red wireframe square centred on that vertex (straddling the up
        /// to four cells the paint will re-solve), and the picked coordinates reported through
        /// <see cref="TileCellPicked"/> are the VERTEX column/row (0..Width, 0..Height inclusive),
        /// not a cell.
        /// </summary>
        public bool TilePlacementTargetsVertex
        {
            get => _tilePlacementTargetsVertex;
            set
            {
                if (_tilePlacementTargetsVertex == value)
                    return;

                _tilePlacementTargetsVertex = value;
                _tileHoverCell = null; // cell coords and vertex coords must never mix in one hover
                _tileHoverEdge = null;
                RequestNextFrameRendering();
            }
        }

        private bool _tilePlacementTargetsEdge;

        /// <summary>The grid edge the pointer is over while a crosser brush is armed, or null.</summary>
        private (int Column, int Row, bool Vertical)? _tileHoverEdge;

        /// <summary>
        /// Whether the armed palette entry paints a crosser onto a grid EDGE (road, bridge, wall -
        /// the reference toolset's model, verified live): the cursor snaps to the nearest edge, the
        /// highlight is the red paint square centred on that edge's midpoint (straddling the two
        /// cells the paint will re-solve), and picks are reported through
        /// <see cref="TileEdgePicked"/>. Takes precedence over
        /// <see cref="TilePlacementTargetsVertex"/> if both are somehow set.
        /// </summary>
        public bool TilePlacementTargetsEdge
        {
            get => _tilePlacementTargetsEdge;
            set
            {
                if (_tilePlacementTargetsEdge == value)
                    return;

                _tilePlacementTargetsEdge = value;
                _tileHoverCell = null;
                _tileHoverEdge = null;
                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// Raised when a crosser brush is armed and a click lands on a grid edge: the edge's
        /// column/row and whether it is a vertical edge (see <c>TilePainter.PaintCrosserEdge</c>
        /// for the coordinate convention). The brush stays armed, like the terrain brush.
        /// </summary>
        public event Action<int, int, bool>? TileEdgePicked;

        /// <summary>
        /// Whether the armed crosser would paint at an edge - the paint cursor's green/red verdict,
        /// answered by the host's solver dry-run the same way <see cref="TilePlacementValidator"/>
        /// answers for cells and vertices.
        /// </summary>
        public Func<int, int, bool, bool>? TilePlacementEdgeValidator { get; set; }

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
        /// <see cref="TilePlacementFootprint"/>), or the VERTEX column/row when
        /// <see cref="TilePlacementTargetsVertex"/> is on. A stamp pick clears
        /// <see cref="IsTilePlacementActive"/> before raising; a vertex paint leaves it armed, so
        /// the brush keeps dabbing the way the reference toolset's does. Not raised when a stamp
        /// footprint would not fit inside the area grid (the host has no way to tell a rejected
        /// stamp from a legal one), nor for a vertex outside the grid's vertex range.
        /// </summary>
        public event Action<int, int>? TileCellPicked;

        private (int Column, int Row)? _selectedTileCell;

        /// <summary>
        /// The grid cell the builder has selected, or null when none is. Set by the host from its own
        /// selection state (so a rebuilt scene keeps the highlight), and reported by
        /// <see cref="TileSelected"/> when a click resolves one here.
        /// </summary>
        /// <remarks>
        /// A selected tile is what the raise/lower commands act on. Aurora works the same way: you
        /// click the tile you mean and then raise it, rather than arming a mode and clicking a cell -
        /// which gave no way to see which cell you were about to change, and cost a click per level.
        /// </remarks>
        public (int Column, int Row)? SelectedTileCell
        {
            get => _selectedTileCell;
            set
            {
                if (_selectedTileCell == value)
                    return;

                _selectedTileCell = value;
                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// Raised when a plain click lands on the area's ground rather than on an instance: the
        /// clicked cell's column and row, or null when the click missed the grid entirely.
        /// </summary>
        public event Action<(int Column, int Row)?>? TileSelected;

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
        /// Draws an interior tileset's ceilings instead of looking into its rooms from above. Off by
        /// default, which is what Aurora's area view does: a room you cannot see into is not editable.
        /// </summary>
        /// <remarks>
        /// What counts as a ceiling is the tileset's own answer, not a guess about height or facing.
        /// Every mesh node carries an MDL <c>tilefade</c> flag, and a non-zero value is exactly the
        /// geometry the engine fades out when the camera would otherwise be looking through it - in
        /// zsf01 that is every <c>ceilling*</c> node plus the wall bands above 3m, and nothing at floor
        /// or wall height.
        /// <para>
        /// Only interior tilesets are cut (see <see cref="AreaScene.IsInteriorTileset"/>). An exterior
        /// set flags overhead geometry too - ttw01's <c>treefol_01</c> canopy hangs 10-20m over the
        /// forest floor - but Aurora draws that, and removing it turns a wood into a field of bare
        /// poles.
        /// </para>
        /// <para>
        /// An earlier attempt cut fragments that faced downward above a height threshold instead, and
        /// that took more than the ceiling with it: an interior wall is full of downward-facing
        /// surfaces that are not ceilings - window ledges, sills, the trim band round the room - so
        /// walls appeared to come and go as the camera orbited. Reading the flag has no such
        /// ambiguity, and it costs nothing per fragment because whole mesh ranges are skipped.
        /// </para>
        /// </remarks>
        private bool _showCeilings;

        public bool ShowCeilings
        {
            get => _showCeilings;
            set
            {
                if (_showCeilings == value)
                    return;

                _showCeilings = value;
                RequestNextFrameRendering();
            }
        }

        /// <summary>Whether <paramref name="scene"/>'s ceilings are cut out of the tile pass.</summary>
        private bool HidesCeilings(AreaScene scene) => !_showCeilings && scene.IsInteriorTileset;

        /// <summary>
        /// Whether textured meshes render with their normal/specular/roughness maps (from an .mtr
        /// or NWN:EE's <c>_n</c>/<c>_s</c>/<c>_r</c> companion-texture convention). On by default -
        /// it is what the game itself renders - with a quick-access-bar switch to drop back to
        /// plain diffuse when the extra relief and glint get in the way of judging base artwork.
        /// </summary>
        private bool _showMaterialMaps = true;

        public bool ShowMaterialMaps
        {
            get => _showMaterialMaps;
            set
            {
                if (_showMaterialMaps == value)
                    return;

                _showMaterialMaps = value;
                RequestNextFrameRendering();
            }
        }

        private bool _showAreaLighting;

        /// <summary>
        /// Whether to light the scene with the area's own sun/moon colours.
        /// </summary>
        /// <remarks>
        /// Off by default, which is what Aurora does and why its viewport looks so much brighter than
        /// ours did. A night area carries genuinely dark authored light - cz220shipbreakin is
        /// ambient RGB(45,45,45) over diffuse RGB(135,138,98) - and rendering through that buries the
        /// textures under an olive cast and makes colours impossible to judge. Turning it on is for
        /// checking what the area will actually look like; editing wants neutral light.
        /// </remarks>
        public bool ShowAreaLighting
        {
            get => _showAreaLighting;
            set
            {
                if (_showAreaLighting == value)
                    return;

                _showAreaLighting = value;
                RequestNextFrameRendering();
            }
        }

        private bool _showFog;

        /// <summary>
        /// Whether to apply the area's distance fog. Off by default, for the same reason as the
        /// lighting: fog hides exactly the far geometry a builder is trying to place.
        /// </summary>
        public bool ShowFog
        {
            get => _showFog;
            set
            {
                if (_showFog == value)
                    return;

                _showFog = value;
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
        private static bool DrawsAsModel(InstanceMarker instance) =>
            !instance.IsDoorTransition && instance.Model is { Meshes.Count: > 0 };

        /// <summary>
        /// Whether the placed model can use NWN's ordinary back-face culling pass.
        /// </summary>
        /// <remarks>
        /// Dynamic creature bodies are assembled from independently-authored body, armor, helmet,
        /// cloak, and weapon parts. Their triangle winding is not consistent across those resources;
        /// applying the tile/placeable culling rule to the combined model discards visible equipment
        /// even though the same geometry renders correctly in the two-sided blueprint preview.
        /// Creature counts are small compared with area placeables, so drawing them two-sided fixes
        /// the authored equipment without giving up culling on the thousands of props in dense areas.
        /// </remarks>
        private static bool CullInstanceModelFaces(InstanceMarkerKind kind) =>
            kind != InstanceMarkerKind.Creature;

        /// <summary>Layered resource index used to resolve tile/mesh textures and MTR materials. Null degrades every mesh to a flat gray fallback.</summary>
        public ResourceIndex? ResourceIndex { get; set; }

        private readonly object _previewAnimationGate = new();
        private string? _previewAnimationName;
        private bool _previewAnimationPlaying;
        private bool _previewAnimationActive;
        private float _previewAnimationElapsed;
        private long _previewAnimationStartedTicks;

        /// <summary>
        /// The model-declared state continuously previewed by the one-model host. Null leaves the
        /// area viewport on its existing one-shot creature idle path.
        /// </summary>
        public string? PreviewAnimationName
        {
            get
            {
                lock (_previewAnimationGate)
                    return _previewAnimationName;
            }
            set
            {
                lock (_previewAnimationGate)
                {
                    if (string.Equals(_previewAnimationName, value, StringComparison.OrdinalIgnoreCase))
                        return;

                    UpdatePreviewClockLocked();
                    _previewAnimationName = value;
                    _previewAnimationElapsed = 0f;
                    StartPreviewClockIfNeededLocked();
                }

                RequestNextFrameRendering();
            }
        }

        public bool PreviewAnimationPlaying
        {
            get
            {
                lock (_previewAnimationGate)
                    return _previewAnimationPlaying;
            }
            set
            {
                lock (_previewAnimationGate)
                {
                    if (_previewAnimationPlaying == value)
                        return;

                    UpdatePreviewClockLocked();
                    _previewAnimationPlaying = value;
                    StartPreviewClockIfNeededLocked();
                }

                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// False while the Appearance tab is hidden or the retained preview control is detached.
        /// This is the lifecycle gate that prevents any continuous frame requests off-screen.
        /// </summary>
        public bool PreviewAnimationActive
        {
            get
            {
                lock (_previewAnimationGate)
                    return _previewAnimationActive;
            }
            set
            {
                lock (_previewAnimationGate)
                {
                    if (_previewAnimationActive == value)
                        return;

                    UpdatePreviewClockLocked();
                    _previewAnimationActive = value;
                    StartPreviewClockIfNeededLocked();
                }

                if (value)
                    RequestNextFrameRendering();
            }
        }

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
                var previous = Volatile.Read(ref _sceneState).Scene;
                var preservesPreviewGeometry = HasSamePreviewGeometry(previous, value);
                var version = Interlocked.Increment(ref _nextSceneVersion);
                Volatile.Write(ref _sceneState, new SceneState(value, version));

                // A retained preview control commonly swaps between models whose selected state has
                // the same name ("default"). Restart on the scene boundary rather than carrying the
                // previous model's effect phase into the replacement.
                lock (_previewAnimationGate)
                {
                    _previewAnimationElapsed = 0f;
                    _previewAnimationStartedTicks = 0;
                    StartPreviewClockIfNeededLocked();
                }

                // Framed on the first scene, and again only when the new one needs a materially
                // different camera. The host clears the scene while it rebuilds, so re-framing on
                // every non-null scene threw away the orbit and zoom the builder had set; but
                // never re-framing left a preview control that had fitted a 0.9m sword holding
                // that distance when the base type changed to a 1.9m mannequin (and the reverse
                // showing an item as a speck). An appearance edit rebuilds geometry of about the
                // same size, so it compares equal here and keeps the builder's view.
                if (value != null && _restoredCameraAwaitingScene)
                {
                    _restoredCameraAwaitingScene = false;
                    RecordSceneFramingBaseline(value);
                }
                else if (value != null && preservesPreviewGeometry)
                {
                    // Palette and RGB edits replace the immutable scene only to publish new tint
                    // dictionaries. The model, transform and placement are unchanged, so retain
                    // the builder's exact orbit while refreshing the layout-aware baseline.
                    RecordSceneFramingBaseline(value);
                }
                else if (value != null && (!_cameraFramed || NeedsRefit(value)))
                {
                    _cameraFramed = true;
                    ResetCameraForScene(value);
                }

                if (value != null && _pendingFocus is { } pendingFocus)
                {
                    _pendingFocus = null;
                    ApplyFocus(pendingFocus);
                }

                RequestNextFrameRendering();
            }
        }

        private static bool HasSamePreviewGeometry(AreaScene? previous, AreaScene? next)
        {
            if (!IsSingleModelPreview(previous) || !IsSingleModelPreview(next))
                return false;

            var before = previous!.Instances[0];
            var after = next!.Instances[0];
            return before.Model != null &&
                   ReferenceEquals(before.Model, after.Model) &&
                   before.Kind == after.Kind &&
                   before.Position == after.Position &&
                   before.Orientation == after.Orientation &&
                   before.VisualTransform.Equals(after.VisualTransform);
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
        /// the GL render thread because OnOpenGlInit/OnOpenGlRender do not run on the UI thread.
        /// </summary>
        private void RaiseStatus(string message)
        {
            if (Dispatcher.UIThread.CheckAccess())
                RenderStatusChanged?.Invoke(this, message);
            else
                Dispatcher.UIThread.Post(() => RenderStatusChanged?.Invoke(this, message));
        }

        /// <summary>
        /// Whether a newly assigned scene wants a different camera than the one currently framed.
        /// Compares the framing the scene ASKS for rather than model identity: an appearance edit
        /// produces a brand-new RenderModel instance of practically the same size (identity says
        /// "different", the builder's view should survive), while a base-type change produces one
        /// of a different scale entirely.
        /// </summary>
        private bool NeedsRefit(AreaScene scene)
        {
            if (_framedDistance is not { } framedDistance)
                return true;

            var aspect = _viewportWidth > 0 && _viewportHeight > 0
                ? (float)_viewportWidth / _viewportHeight
                : 1.5f;
            var (target, distance) = AreaCameraMath.ComputeSceneFraming(
                scene, AreaSceneBuilder.TileSize, VerticalFovRadians, aspect);

            var ratio = distance / MathF.Max(framedDistance, 0.0001f);
            return ratio is < 0.8f or > 1.25f ||
                   Vector3.Distance(target, _framedTarget) > distance * 0.25f;
        }

        /// <summary>
        /// Barely above level, for a single-model preview: enough to read the top of a shoulder
        /// without the foreshortening a survey angle puts on a standing figure.
        /// </summary>
        private const float PreviewElevationRadians = 0.09f;

        private void ResetCameraForScene(AreaScene scene)
        {
            // A single-model preview scene frames the MODEL where it stands, not the grid it
            // nominally sits on - otherwise the camera sits back far enough to hold a whole 10m
            // tile and an item renders as a speck.
            var (target, distance) = SceneFraming(scene);

            _target = target;
            _distance = distance;
            _initialDistance = distance;
            _framedTarget = target;
            _framedDistance = distance;
            // A single-model preview is looked at, not surveyed. Aurora shows an item's wearer
            // straight on and near level; the area editor's raised three-quarter view is for reading
            // a floor plan and puts a mannequin at a diagonal seen from above. The eye sits at
            // (cos, sin) from the target, so 270 degrees is directly in front of a model facing -Y.
            var isSingleModelPreview = IsSingleModelPreview(scene);
            _azimuth = isSingleModelPreview ? MathF.PI * 1.5f : MathF.PI * 1.25f;
            _elevation = isSingleModelPreview
                ? PreviewElevationRadians
                : AreaCameraMath.DefaultElevationRadians;
        }

        private void RecordSceneFramingBaseline(AreaScene scene)
        {
            (_framedTarget, _framedDistance) = SceneFraming(scene);
        }

        private (Vector3 Target, float Distance) SceneFraming(AreaScene scene)
        {
            var aspect = _viewportWidth > 0 && _viewportHeight > 0
                ? (float)_viewportWidth / _viewportHeight
                : 1.5f;
            return AreaCameraMath.ComputeSceneFraming(
                scene, AreaSceneBuilder.TileSize, VerticalFovRadians, aspect);
        }

        // ----- Pointer input: middle orbits, middle+left pans, wheel zooms -----
        //
        // OpenGlControlBase has no Background brush, so pointer events never hit-test to this
        // control directly.
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

            // Outside placement, a right press selects whatever is under the cursor so that the
            // context menu the host opens from the same gesture describes the thing that was
            // clicked, not whatever happened to be selected before. Deliberately NOT marked
            // handled - the menu still has to open - and safe to fall out of, because the right
            // button drives no camera drag.
            if (props.IsRightButtonPressed && _dragMode == DragMode.None)
            {
                RaiseInstancePicked(pos);
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

            // A click that hit nothing placed is a click on the map itself, and that selects the tile
            // under it. Raised after InstancePicked so the host has already cleared its instance
            // selection by the time it takes a tile one - the two are mutually exclusive.
            if (hit == null)
                TileSelected?.Invoke(ResolveTileCell(ray, scene));
            else
                TileSelected?.Invoke(null);
        }

        /// <summary>
        /// The grid cell <paramref name="ray"/> lands on, or null when it misses the ground or falls
        /// outside the area's grid.
        /// </summary>
        private static (int Column, int Row)? ResolveTileCell(PickRay ray, AreaScene scene)
        {
            var point = AreaWalkmesh.RaycastGround(ray, scene)
                        ?? AreaManipulation.IntersectRayWithHorizontalPlane(ray, 0f);
            if (point is not { } hit)
                return null;

            var (column, row) = WorldPointToCell(hit);
            return column < 0 || row < 0 || column >= scene.Width || row >= scene.Height
                ? null
                : (column, row);
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
            LayerColorIndices = source.LayerColorIndices,
            Geometry = source.Geometry,
            Model = source.Model,
            IsDoorTransition = source.IsDoorTransition,
            TintMapOverrides = source.TintMapOverrides,
            SoundMinDistance = source.SoundMinDistance,
            SoundMaxDistance = source.SoundMaxDistance,
            IsPositionalSound = source.IsPositionalSound
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

        /// <summary>
        /// How far one press of a rotate button turns the selection before the repeat takes over. A
        /// sixteenth of a turn: coarse enough to square something up in a couple of taps, fine enough
        /// to angle a chair.
        /// </summary>
        private const float RotateTapRadians = MathF.PI / 8f;

        /// <summary>
        /// How fast a held rotate button turns the selection - a full turn in about three seconds,
        /// slow enough to stop on a heading by eye and fast enough not to wait for the far side.
        /// Driven off the same <see cref="PadStepSeconds"/> clock as the camera pad, so the speed is
        /// a speed rather than a function of how fast the repeat happens to fire.
        /// </summary>
        private const float RotateHeldRadiansPerSecond = MathF.Tau / 3f;

        /// <summary>
        /// Turns the selected instance as a live preview, opening a rotation if one is not already
        /// open. <paramref name="isFirstStep"/> turns the fixed tap step; every step after it glides at
        /// <see cref="RotateHeldRadiansPerSecond"/>. Nothing is written until
        /// <see cref="CommitSelectedRotation"/>.
        /// </summary>
        /// <remarks>
        /// This is what makes the rotate buttons feel like Aurora's rather than like a series of
        /// separate decisions. Each press used to be a document edit and an async scene rebuild, so a
        /// held button could not turn the object faster than the scene could be reassembled, and a
        /// quarter turn cost eight undo entries. Rotating the preview instead costs a redraw, and the
        /// single edit lands on release.
        /// <para>
        /// It borrows the drag machinery deliberately: the preview, the readout and the commit all
        /// already exist for the rotate gizmo, and a button-driven rotation is the same operation
        /// started a different way. The pointer is over the button while this runs, so it cannot also
        /// be starting a drag in the viewport.
        /// </para>
        /// </remarks>
        public void NudgeSelectedRotation(float direction, bool isFirstStep)
        {
            if (SelectedInstance is not { } selected)
                return;

            if (_dragMode != DragMode.Rotate || _manipulationOriginal == null)
                BeginManipulation(selected, DragMode.Rotate);

            var step = isFirstStep
                ? RotateTapRadians
                : RotateHeldRadiansPerSecond * PadStepSeconds();

            _manipulationHeadingRadians += direction * step;
            var orientation = AreaManipulation.HeadingToOrientation(_manipulationHeadingRadians);
            _manipulationPreview = ClonePreview(_manipulationOriginal!, _manipulationOriginal!.Position, orientation);
            ManipulationPreviewChanged?.Invoke(_manipulationOriginal, _manipulationPreview);
            RequestNextFrameRendering();
        }

        /// <summary>
        /// Ends a rotation started by <see cref="NudgeSelectedRotation"/>, raising
        /// <see cref="InstanceRotated"/> once for the whole turn. Safe to call when none is open.
        /// </summary>
        public void CommitSelectedRotation()
        {
            if (_dragMode == DragMode.Rotate && _manipulationOriginal != null)
                CommitManipulation();
        }

        /// <summary>The instance actually rendered/highlighted for <paramref name="instance"/> right now - its live manipulation preview while a drag is in progress on it, otherwise itself.</summary>
        private InstanceMarker Displayed(InstanceMarker instance) =>
            _manipulationPreview != null && ReferenceEquals(instance, _manipulationOriginal) ? _manipulationPreview : instance;

        // ----- Place-from-palette -----

        private void CancelPlacement()
        {
            _snappedDoorAnchor = null;
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

            // A door still rides the cursor when it is nowhere near a doorway - it just drops into one
            // as soon as it reaches one, and cannot be put down until it has. Showing it at the pointer
            // is what makes "this one, not that one" readable while choosing; snapping from any distance
            // put the preview off where the builder was not looking.
            _snappedDoorAnchor = SnapsToDoorAnchors ? NearestDoorAnchor(hit) : null;
            _ghostPosition = _snappedDoorAnchor?.Position ?? hit;

            RequestNextFrameRendering();
        }

        /// <summary>
        /// True while the armed placement is a door, which may only be hung in a doorway a tile
        /// declares - see <see cref="TileDoorAnchor"/>.
        /// </summary>
        private bool SnapsToDoorAnchors => _placementGhost?.Kind == InstanceMarkerKind.Door;

        /// <summary>
        /// The empty doorway the cursor is close enough to hang a door in, or null when it is near none
        /// or when the ones in reach are already filled. See
        /// <see cref="AreaScene.NearestEmptyDoorway"/> for both rules.
        /// </summary>
        private TileDoorAnchor? NearestDoorAnchor(Vector3 groundPoint) =>
            Volatile.Read(ref _sceneState).Scene?.NearestEmptyDoorway(groundPoint);

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

            if (SnapsToDoorAnchors)
            {
                // A door that is not standing in an empty doorway has nowhere to go, so the click is
                // ignored and the placement stays armed rather than dropping the door on open floor -
                // or hanging a second leaf in a doorway that already has one.
                if (NearestDoorAnchor(hit) is not { } anchor)
                    return;

                _isPlacementActive = false;
                _ghostPosition = null;
                _snappedDoorAnchor = null;
                PlacementPointPicked?.Invoke(new PlacementPick(anchor.Position, anchor.Orientation));
                return;
            }

            _isPlacementActive = false;
            _ghostPosition = null;
            PlacementPointPicked?.Invoke(new PlacementPick(hit, null));
        }

        // ----- Paint-tiles-from-palette -----

        private void CancelTilePlacement()
        {
            if (!_isTilePlacementActive)
                return;

            _isTilePlacementActive = false;
            _tileHoverCell = null;
            _tileHoverEdge = null;
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

            if (_tilePlacementTargetsEdge)
            {
                var edge = WorldPointToEdge(hit);
                if (_tileHoverEdge == edge)
                    return;

                _tileHoverEdge = edge;
                RequestNextFrameRendering();
                return;
            }

            var cell = _tilePlacementTargetsVertex ? WorldPointToVertex(hit) : WorldPointToCell(hit);
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

            if (_tilePlacementTargetsEdge)
            {
                var (edgeColumn, edgeRow, vertical) = WorldPointToEdge(hit);
                // The two bounds mirror TilePainter.PaintCrosserEdge's convention; a border edge is
                // legal (a road may run off the map). The brush stays armed, like terrain.
                var inRange = vertical
                    ? edgeColumn >= 0 && edgeColumn <= scene.Width && edgeRow >= 0 && edgeRow < scene.Height
                    : edgeColumn >= 0 && edgeColumn < scene.Width && edgeRow >= 0 && edgeRow <= scene.Height;
                if (!inRange)
                    return;

                TileEdgePicked?.Invoke(edgeColumn, edgeRow, vertical);
                return;
            }

            if (_tilePlacementTargetsVertex)
            {
                var (vertexColumn, vertexRow) = WorldPointToVertex(hit);
                // Any vertex inside 0..Width / 0..Height touches at least one real cell; outside it
                // touches none and the paint could only refuse. The reference keeps painting armed
                // between clicks, and so does this - terrain is dabbed repeatedly.
                if (vertexColumn < 0 || vertexRow < 0 || vertexColumn > scene.Width || vertexRow > scene.Height)
                    return;

                TileCellPicked?.Invoke(vertexColumn, vertexRow);
                return;
            }

            var (column, row) = WorldPointToCell(hit);

            // A stamp that would run off the grid is refused here rather than reported: the host only
            // learns the anchor cell, so it could not tell a clipped write from a clean one. Placement
            // stays armed so the next click can land somewhere it fits, and the refusal answers on
            // the map rather than passing as a dead click.
            if (!FootprintFitsGrid(scene, column, row))
            {
                FlashStampRejection((column, row));
                return;
            }

            _isTilePlacementActive = false;
            _tileHoverCell = null;
            RequestNextFrameRendering();
            TileCellPicked?.Invoke(column, row);
        }

        /// <summary>The grid cell containing a world point. Floor, not truncate - a point west or south of the grid origin belongs to a negative cell, and truncation would fold two cells onto index 0.</summary>
        private static (int Column, int Row) WorldPointToCell(Vector3 world) => (
            (int)MathF.Floor(world.X / AreaSceneBuilder.TileSize),
            (int)MathF.Floor(world.Y / AreaSceneBuilder.TileSize));

        /// <summary>The 10m grid vertex nearest a world point - the terrain paint target, exactly as the reference toolset snaps it.</summary>
        private static (int Column, int Row) WorldPointToVertex(Vector3 world) => (
            (int)MathF.Round(world.X / AreaSceneBuilder.TileSize),
            (int)MathF.Round(world.Y / AreaSceneBuilder.TileSize));

        /// <summary>
        /// The grid edge nearest a world point - the crosser paint target. Whichever grid line
        /// (vertical at a column boundary, horizontal at a row boundary) lies closer wins;
        /// coordinates follow <c>TilePainter.PaintCrosserEdge</c>'s convention.
        /// </summary>
        private static (int Column, int Row, bool Vertical) WorldPointToEdge(Vector3 world)
        {
            var tileX = world.X / AreaSceneBuilder.TileSize;
            var tileY = world.Y / AreaSceneBuilder.TileSize;
            var nearestColumnLine = MathF.Round(tileX);
            var nearestRowLine = MathF.Round(tileY);

            return MathF.Abs(tileX - nearestColumnLine) <= MathF.Abs(tileY - nearestRowLine)
                ? ((int)nearestColumnLine, (int)MathF.Floor(tileY), true)
                : ((int)MathF.Floor(tileX), (int)nearestRowLine, false);
        }

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
        /// Rates, not per-press amounts, and integrated against the clock rather than counted per
        /// repeat: a RepeatButton set to a 16ms interval actually delivers nearer 43 a second, which
        /// left the pad panning at 1440 px/s when it was meant to do 2025. Driving it from elapsed
        /// time makes the figure below what the camera really does. All three measured off Aurora's
        /// own buttons - 2025 px/s over a 150ms press, 196 deg/s over 300ms, 2.7x per second of zoom.
        /// Pan is sized in screen terms rather than world units so it feels the same zoomed into a
        /// doorway or looking at the whole area.
        /// </remarks>
        private const float PanPixelsPerSecond = 2025f;

        private const float OrbitDegreesPerSecondPad = 196f;

        /// <summary>
        /// Model-preview turn sensitivity. Preview drags are distance based, unlike Aurora's
        /// time-based area-editor orbit gesture, so a given pointer motion always produces the same
        /// turn regardless of event rate.
        /// </summary>
        private const float PreviewOrbitDegreesPerPixel = 1.2f;

        /// <summary>
        /// Aurora's zoom button is not a constant rate: a 150ms press gives 1.415x and a 300ms press
        /// 1.640x, which fits an initial 1.22x on press followed by about 2.7x per second held.
        /// </summary>
        private const float ZoomFactorPerSecond = 2.7f;

        private const float ZoomPressFactor = 1.22f;

        private const float DegreesToRadians = MathF.PI / 180f;

        /// <summary>Clock for the pad, so a held button moves by time rather than by repeat count.</summary>
        private long _lastPadTicks;

        /// <summary>
        /// The offscreen target the scene is actually drawn into, for its 24-bit depth buffer.
        /// See <see cref="DepthPrecisionFramebuffer"/> for why Avalonia's own is not good enough.
        /// </summary>
        private readonly DepthPrecisionFramebuffer _depthPrecisionTarget = new();

        /// <summary>
        /// How long a creature's idle runs for before it settles, in seconds.
        /// </summary>
        /// <remarks>
        /// Matches Aurora, which plays a creature's idle briefly when the area opens and then leaves it
        /// standing in the pose it finished on. Long enough to see what the thing is; short enough that
        /// the viewport is not permanently animating behind someone trying to place objects.
        /// </remarks>
        private const float IdlePlaybackSeconds = 2.5f;

        private readonly record struct PreviewAnimationSnapshot(string? Name, float Seconds, bool Running);

        private void UpdatePreviewClockLocked()
        {
            if (_previewAnimationStartedTicks == 0)
                return;

            var now = System.Diagnostics.Stopwatch.GetTimestamp();
            _previewAnimationElapsed += (float)((now - _previewAnimationStartedTicks) /
                                                (double)System.Diagnostics.Stopwatch.Frequency);
            _previewAnimationStartedTicks = 0;
        }

        private void StartPreviewClockIfNeededLocked()
        {
            if (_previewAnimationActive &&
                _previewAnimationPlaying &&
                !string.IsNullOrWhiteSpace(_previewAnimationName) &&
                _previewAnimationStartedTicks == 0)
            {
                _previewAnimationStartedTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }

        private PreviewAnimationSnapshot PreviewAnimation()
        {
            lock (_previewAnimationGate)
            {
                var elapsed = _previewAnimationElapsed;
                if (_previewAnimationStartedTicks != 0)
                {
                    elapsed += (float)((System.Diagnostics.Stopwatch.GetTimestamp() -
                                        _previewAnimationStartedTicks) /
                                       (double)System.Diagnostics.Stopwatch.Frequency);
                }

                return new PreviewAnimationSnapshot(
                    _previewAnimationName,
                    elapsed,
                    _previewAnimationActive && _previewAnimationPlaying);
            }
        }

        /// <summary>When the current scene appeared, or null once its idle has finished playing.</summary>
        private long? _idlePlaybackStartedTicks;

        /// <summary>
        /// A single-model preview keeps its idle alive like Aurora's item/creature property window.
        /// Area scenes still play once and settle so a busy area does not animate forever.
        /// </summary>
        private bool _idlePlaybackLoops;

        /// <summary>
        /// How far through the idle the scene is, or null when it has settled. Drives which pose frame
        /// each animated mesh draws with, and keeps asking for another frame until it is done.
        /// </summary>
        private float? IdlePlaybackSeconds_Elapsed()
        {
            if (_idlePlaybackStartedTicks is not { } started)
                return null;

            var elapsed = (float)((System.Diagnostics.Stopwatch.GetTimestamp() - started)
                / (double)System.Diagnostics.Stopwatch.Frequency);

            if (_idlePlaybackLoops)
                return elapsed % IdlePlaybackSeconds;

            if (elapsed >= IdlePlaybackSeconds)
            {
                _idlePlaybackStartedTicks = null;
                return null;
            }

            return elapsed;
        }

        /// <summary>
        /// The transform an animated mesh draws with right now: its frame partway through the idle
        /// while that is playing, and its resting transform once it has settled.
        /// </summary>
        private static Matrix4x4 PosedMeshTransform(MeshRange mesh, float? elapsed)
        {
            var frame = IdleFrameIndex(mesh.PoseFrames.Count, elapsed);
            if (frame < 0)
                return mesh.MeshTransform;

            return mesh.PoseFrames[frame];
        }

        private static int IdleFrameIndex(int frameCount, float? elapsed)
        {
            if (elapsed is not { } seconds || frameCount == 0)
                return -1;

            var through = Math.Clamp(seconds / IdlePlaybackSeconds, 0f, 1f);
            return Math.Clamp((int)(through * (frameCount - 1)), 0, frameCount - 1);
        }

        private static int PreviewMeshIndexOffset(
            MeshRange mesh,
            ModelBuffer model,
            PreviewAnimationSnapshot preview,
            float? idleElapsed)
        {
            if (preview.Name is { } animatedName &&
                mesh.AnimationIndexOffsets.TryGetValue(animatedName, out var offsets) &&
                offsets.Count > 0)
            {
                return offsets[PreviewAnimationFrameIndex(model, animatedName, offsets.Count, preview.Seconds)];
            }

            // Rigid named animations carry transform frames only. Their static vertex range remains
            // correct; idle skin frames apply only when no named state has taken over the mesh.
            if (preview.Name is { } name &&
                mesh.AnimationFrames.TryGetValue(name, out var animationFrames) &&
                animationFrames.Count > 0)
            {
                return mesh.IndexOffset;
            }

            var frame = IdleFrameIndex(mesh.PoseIndexOffsets.Count, idleElapsed);
            return frame < 0 ? mesh.IndexOffset : mesh.PoseIndexOffsets[frame];
        }

        private static Matrix4x4 PreviewMeshTransform(
            MeshRange mesh,
            ModelBuffer model,
            PreviewAnimationSnapshot preview,
            float? idleElapsed)
        {
            if (preview.Name is not { } name ||
                !mesh.AnimationFrames.TryGetValue(name, out var frames) ||
                frames.Count == 0)
            {
                return PosedMeshTransform(mesh, idleElapsed);
            }

            var length = model.Animations.FirstOrDefault(
                animation => string.Equals(animation.Name, name, StringComparison.OrdinalIgnoreCase))?.Length ?? 0f;
            if (length <= 0f || frames.Count == 1)
                return frames[0];

            return frames[PreviewAnimationFrameIndex(model, name, frames.Count, preview.Seconds)];
        }

        private static int PreviewAnimationFrameIndex(
            ModelBuffer model,
            string name,
            int frameCount,
            float seconds)
        {
            if (frameCount <= 1)
                return 0;
            var length = model.Animations.FirstOrDefault(animation =>
                string.Equals(animation.Name, name, StringComparison.OrdinalIgnoreCase))?.Length ?? 0f;
            if (length <= 0f)
                return 0;
            var through = (seconds % length) / length;
            return Math.Clamp((int)(through * frameCount), 0, frameCount - 1);
        }

        private static Matrix4x4 PreviewEmitterTransform(
            RenderEmitter emitter,
            ModelBuffer model,
            PreviewAnimationSnapshot preview)
        {
            if (preview.Name is not { } name ||
                !emitter.AnimationFrames.TryGetValue(name, out var frames) ||
                frames.Count == 0)
            {
                return emitter.Transform;
            }

            var length = model.Animations.FirstOrDefault(
                animation => string.Equals(animation.Name, name, StringComparison.OrdinalIgnoreCase))?.Length ?? 0f;
            if (length <= 0f || frames.Count == 1)
                return frames[0];

            var through = (preview.Seconds % length) / length;
            var frame = Math.Clamp((int)(through * frames.Count), 0, frames.Count - 1);
            return frames[frame];
        }

        /// <summary>
        /// Seconds since the previous pad step. A first step, or one after a gap, counts as a single
        /// 60Hz frame so a lone click still does something; a stall cannot bank up into a jump.
        /// </summary>
        private float PadStepSeconds()
        {
            var now = System.Diagnostics.Stopwatch.GetTimestamp();
            var seconds = _lastPadTicks == 0
                ? 1.0 / 60.0
                : (now - _lastPadTicks) / (double)System.Diagnostics.Stopwatch.Frequency;
            _lastPadTicks = now;

            return (float)Math.Clamp(seconds, 1.0 / 240.0, 0.1);
        }

        /// <summary>
        /// Slides the view across the ground plane at <see cref="PanPixelsPerSecond"/>.
        /// </summary>
        /// <remarks>
        /// The arguments move the CAMERA, which is the opposite of how the scene appears to move -
        /// Aurora's left arrow carries the camera left, so the scene travels right. Measured: its left
        /// arrow moves the scene right by 372px, its up arrow moves the scene down by 372px.
        /// <paramref name="cameraForward"/> travels along the ground, not up into the air.
        /// </remarks>
        public void NudgePan(float cameraRight, float cameraForward)
        {
            var pixels = PanPixelsPerSecond * PadStepSeconds();
            var worldPerPixel = AreaCameraMath.WorldUnitsPerPixel(_distance, VerticalFovRadians, _viewportHeight);
            _target += AreaCameraMath.PanDelta(
                _azimuth, -cameraRight * pixels, cameraForward * pixels, worldPerPixel);

            RequestNextFrameRendering();
        }

        /// <summary>Turns the view around the point it is looking at, at <see cref="OrbitDegreesPerSecondPad"/>.</summary>
        public void NudgeOrbit(float azimuthDirection, float elevationDirection)
        {
            var radians = OrbitDegreesPerSecondPad * DegreesToRadians * PadStepSeconds();
            _azimuth += azimuthDirection * radians;
            _elevation = AreaCameraMath.ClampElevation(_elevation + elevationDirection * radians);

            RequestNextFrameRendering();
        }

        /// <summary>
        /// Pans a single-model preview in the camera's screen plane, keeping the model under the
        /// pointer horizontally and vertically.
        /// </summary>
        public void PanPreviewByPixels(float dxPixels, float dyPixels)
        {
            var worldPerPixel = AreaCameraMath.WorldUnitsPerPixel(
                _distance,
                VerticalFovRadians,
                _viewportHeight);
            _target += AreaCameraMath.ScreenPanDelta(
                _azimuth,
                _elevation,
                dxPixels,
                dyPixels,
                worldPerPixel);

            RequestNextFrameRendering();
        }

        /// <summary>
        /// Turns a single-model preview by pointer distance so event timing cannot change the result.
        /// The camera orbits the fixed model; the signs make the displayed model follow the drag.
        /// </summary>
        public void OrbitPreviewByPixels(float dxPixels, float dyPixels)
        {
            var radiansPerPixel = PreviewOrbitDegreesPerPixel * DegreesToRadians;
            _azimuth -= dxPixels * radiansPerPixel;
            _elevation = AreaCameraMath.ClampElevation(_elevation - dyPixels * radiansPerPixel);

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

            var factor = gap > 0.15
                ? ZoomPressFactor
                : MathF.Pow(ZoomFactorPerSecond, (float)Math.Clamp(gap, 1.0 / 240.0, 0.1));
            _distance = AreaCameraMath.ClampDistance(
                _distance * MathF.Pow(factor, -steps), _initialDistance);

            RequestNextFrameRendering();
        }

        /// <summary>
        /// Zoom per wheel notch. Aurora is aggressive here - measured 3.31x over two notches, so 1.82
        /// each; four notches take a whole area to a close-up.
        /// </summary>
        private const float WheelZoomPerNotch = 1.82f;

        /// <summary>
        /// Puts the camera back to the framing the area opened with.
        /// </summary>
        /// <remarks>
        /// Aurora keeps this on its own pad, and it is the way out of a view that has been orbited
        /// into the floor or panned off the edge of the world - which is easy to do and otherwise
        /// takes a lot of careful dragging to undo.
        /// </remarks>
        public void ReorientCamera()
        {
            if (Volatile.Read(ref _sceneState).Scene is not { } scene)
                return;

            ResetCameraForScene(scene);
            RequestNextFrameRendering();
        }

        /// <summary>
        /// How close the camera comes when it is sent to a single object. Chosen to frame the object
        /// and the ground around it rather than to fill the viewport with it: an object this tool can
        /// place ranges from a coin on the floor to a building, and a distance that suits the coin
        /// puts the camera inside the building.
        /// </summary>
        private const float FocusDistance = 15f;

        /// <summary>
        /// Sends the camera to one object: the orbit target moves onto it and the view closes to
        /// <see cref="FocusDistance"/>, while azimuth and elevation are left where the builder had
        /// them - arriving at a different angle than you were working at is disorienting, and the
        /// pad is right there if the new spot needs turning.
        /// </summary>
        /// <remarks>
        /// Never zooms out. Someone already in close on a neighbouring object is looking at this
        /// scale deliberately, and pushing the camera back out to a fixed distance would throw that
        /// away every time they stepped through a list.
        /// </remarks>
        public void FocusOn(Vector3 position)
        {
            if (Scene == null)
            {
                _pendingFocus = position;
                return;
            }

            _pendingFocus = null;
            ApplyFocus(position);
            RequestNextFrameRendering();
        }

        private void ApplyFocus(Vector3 position)
        {
            _target = position;
            _distance = AreaCameraMath.ClampDistance(
                MathF.Min(_distance, FocusDistance), _initialDistance);
        }

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
                _batchedTiles = null;

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
                    foreach (var diffuse in _textureCache.Values)
                        if (diffuse.TexId != 0)
                            _gl.DeleteTexture(diffuse.TexId);
                    _textureCache.Clear();
                    foreach (var texId in _mapTextureCache.Values)
                        if (texId != 0)
                            _gl.DeleteTexture(texId);
                    _mapTextureCache.Clear();
                    _rawTextureCache.Clear();
                    _parsedMaterialCache.Clear();

                    foreach (var buffer in _modelBuffers.Values)
                        DeleteBuffer(buffer.Vao, buffer.Vbo, buffer.Ebo);
                    _modelBuffers.Clear();

                    if (_fallbackCubeBuffer is { } cube)
                        DeleteBuffer(cube.Vao, cube.Vbo, cube.Ebo);
                    if (_markerMeshBuffer is { } marker)
                        DeleteBuffer(marker.Vao, marker.Vbo, marker.Ebo);
                    if (_doorTransitionBuffer is { } transition)
                        DeleteBuffer(transition.Vao, transition.Vbo, transition.Ebo);
                    _doorTransitionBuffer = null;
                    if (_particleQuadBuffer is { } particle)
                        DeleteBuffer(particle.Vao, particle.Vbo, particle.Ebo);
                    _particleQuadBuffer = null;
                    if (_soundNoteBuffer is { } note)
                        DeleteBuffer(note.Vao, note.Vbo, note.Ebo);
                    _soundNoteBuffer = null;

                    if (_hasSoundRangeBuffers)
                    {
                        _gl.DeleteVertexArray(_soundCircleVao);
                        _gl.DeleteBuffer(_soundCircleVbo);
                        _gl.DeleteVertexArray(_soundSphereVao);
                        _gl.DeleteBuffer(_soundSphereVbo);
                        _hasSoundRangeBuffers = false;
                    }

                    DeletePolygonBuffer();
                    DeleteWalkmeshBuffer();
                    _tileBatches = null;
                    _batchedTiles = null;

                    if (_hasHighlightBuffer)
                    {
                        _gl.DeleteVertexArray(_highlightVao);
                        _gl.DeleteBuffer(_highlightVbo);
                        _hasHighlightBuffer = false;
                    }

                    _depthPrecisionTarget.Dispose(_gl);

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

            _frameStamp++;

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

            // Everything below draws into our own framebuffer rather than Avalonia's, purely to get a
            // 24-bit depth buffer instead of its 16-bit one, and is blitted back in the finally. When
            // that target is unavailable this is a no-op and the scene renders straight to Avalonia's
            // framebuffer, flicker and all.
            _depthPrecisionTarget.BeginFrame(_gl, pixelWidth, pixelHeight);
            var sceneState = Volatile.Read(ref _sceneState);
            var scene = sceneState.Scene;
            var background = BackgroundForScene(scene);

            try
            {
                if (Interlocked.Exchange(ref _gameResourceInvalidationRequested, 0) != 0)
                    ClearGameResourceGpuCaches();

                _gl.ClearColor(background.X, background.Y, background.Z, 1f);
                _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
                _gl.Enable(EnableCap.DepthTest);
                _gl.DepthFunc(DepthFunction.Less);
                // Off by default: this is the state the overlay passes (markers, gizmo, walkmesh,
                // outlines, particle quads) want. The MDL passes turn it on for themselves -
                // see BeginModelFaceCulling.
                _gl.Disable(EnableCap.CullFace);
                _gl.Viewport(0, 0, pixelWidth, pixelHeight);

                if (scene == null)
                    return;

                if (sceneState.Version != _renderedSceneVersion)
                {
                    var isSingleModelPreview = IsSingleModelPreview(scene);
                    if (isSingleModelPreview)
                    {
                        // Preview controls reuse Array.Empty<TilePlacement>() for every model, so a
                        // tile-list identity check cannot detect replacements. Restart on every
                        // preview scene assignment and loop it for as long as the view is visible.
                        _idlePlaybackStartedTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                        _idlePlaybackLoops = true;
                    }
                    else
                    {
                        _idlePlaybackLoops = false;
                    }

                    // Trigger volumes live on the instances, so this follows any scene change.
                    RebuildPolygonBuffer(scene);

                    // The tile-derived GPU state does not. An edit that only moved or turned an
                    // instance hands back the very same tile list, and re-uploading every tile's
                    // walkmesh and regrouping every draw batch for that was the bulk of what made an
                    // edit stutter on a large area.
                    if (!ReferenceEquals(scene.Tiles, _batchedTiles))
                    {
                        RebuildWalkmeshBuffer(scene);
                        _tileBatches = AreaDrawBatcher.GroupByModel(scene.Tiles);

                        // A new area starts its idle again and then settles. Single-model previews
                        // already started above and keep looping instead.
                        if (!isSingleModelPreview)
                            _idlePlaybackStartedTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                        _batchedTiles = scene.Tiles;
                    }

                    _renderedSceneVersion = sceneState.Version;
                }

                DrawScene(scene, width, height);
                EvictStaleGpuResources();
            }
            catch (Exception ex)
            {
                RaiseStatus($"Area render error: {ex.Message}");
            }
            finally
            {
                // Composite onto Avalonia's framebuffer whatever happened above - including the
                // early return for a null scene, which still owes it the cleared background.
                _depthPrecisionTarget.EndFrame(_gl, fb, background);
            }
        }

        /// <summary>
        /// Schedules GL-owned model and texture caches for disposal on the render thread. This is
        /// called when Module Properties changes the HAK list; clearing only the CPU resource index
        /// would otherwise leave an open area drawing the old GPU uploads under the same resrefs.
        /// </summary>
        public void InvalidateGameResources()
        {
            Interlocked.Exchange(ref _gameResourceInvalidationRequested, 1);
            RequestNextFrameRendering();
        }

        private void ClearGameResourceGpuCaches()
        {
            if (_gl == null)
                return;

            foreach (var diffuse in _textureCache.Values)
            {
                if (diffuse.TexId != 0)
                    _gl.DeleteTexture(diffuse.TexId);
            }
            _textureCache.Clear();

            foreach (var texId in _mapTextureCache.Values)
            {
                if (texId != 0)
                    _gl.DeleteTexture(texId);
            }
            _mapTextureCache.Clear();
            _rawTextureCache.Clear();
            _parsedMaterialCache.Clear();

            foreach (var buffer in _modelBuffers.Values)
                DeleteBuffer(buffer.Vao, buffer.Vbo, buffer.Ebo);
            _modelBuffers.Clear();
            _tileBatches = null;
            _batchedTiles = null;
            _renderedSceneVersion = -1;
        }

        private static bool IsSingleModelPreview(AreaScene? scene) =>
            scene != null &&
            scene.Tiles.Count == 0 &&
            scene.Instances.Count == 1;

        private static Vector3 BackgroundForScene(AreaScene? scene) =>
            IsSingleModelPreview(scene)
                ? AuroraPreviewBackground
                : ViewportBackground;

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

            var gl = _gl;
            if (gl == null)
                return -1;

            location = gl.GetUniformLocation(_shaderProgram, name);
            _uniformLocations[name] = location;
            return location;
        }

        private void SetUniformMatrix4(string name, Matrix4x4 matrix)
        {
            var location = GetUniformLocationCached(name);
            var gl = _gl;
            if (location < 0 || gl == null)
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
            gl.UniformMatrix4(location, 1, false, values);
        }

        private void SetUniformVec3(string name, Vector3 value)
        {
            var location = GetUniformLocationCached(name);
            var gl = _gl;
            if (location >= 0 && gl != null)
                gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        private void SetUniformVec4(string name, Vector4 value)
        {
            var location = GetUniformLocationCached(name);
            var gl = _gl;
            if (location >= 0 && gl != null)
                gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }

        private void SetUniformVec2(string name, Vector2 value)
        {
            var location = GetUniformLocationCached(name);
            var gl = _gl;
            if (location >= 0 && gl != null)
                gl.Uniform2(location, value.X, value.Y);
        }

        private void SetUniformBool(string name, bool value)
        {
            // Every path that disables the diffuse texture is switching to flat-color rendering.
            // Tint state otherwise survives from the last model mesh and takes precedence over the
            // flat color in the fragment shader, corrupting later markers and overlays.
            if (name == "hasTexture" && !value)
            {
                SetUniformBoolCore("hasTintMap", false);
                SetUniformBoolCore("hasTintAlpha", false);
            }

            SetUniformBoolCore(name, value);
        }

        private void SetUniformBoolCore(string name, bool value)
        {
            var location = GetUniformLocationCached(name);
            var gl = _gl;
            if (location >= 0 && gl != null)
                gl.Uniform1(location, value ? 1 : 0);
        }

        private void SetUniformFloat(string name, float value)
        {
            var location = GetUniformLocationCached(name);
            var gl = _gl;
            if (location >= 0 && gl != null)
                gl.Uniform1(location, value);
        }

        private void SetUniformInt(string name, int value)
        {
            var location = GetUniformLocationCached(name);
            var gl = _gl;
            if (location >= 0 && gl != null)
                gl.Uniform1(location, value);
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
            // The far plane only has to reach past the area; 25x the framing distance covers the
            // largest in the corpus with room to spare. The near plane scales with the distance -
            // see AreaCameraMath.NearPlaneFor - because the ratio between the two is what decides
            // whether a painting can be told from the wall it hangs on.
            var farPlane = MathF.Max(_distance, _initialDistance) * 25f + 100f;
            var aspect = (float)width / height;
            var projection = AreaCameraMath.CreateProjection(
                IsSingleModelPreview(scene),
                _distance,
                VerticalFovRadians,
                aspect,
                AreaCameraMath.NearPlaneFor(_distance),
                farPlane);

            var eye = _target + AreaCameraMath.OrbitEyeOffset(_azimuth, _elevation, _distance);
            _cameraEye = eye;
            var view = Matrix4x4.CreateLookAt(eye, _target, Vector3.UnitZ);

            // Kept for picking (RaiseInstancePicked runs on a click, not every frame, so it needs
            // the matrices from whatever frame last actually rendered).
            _lastView = view;
            _lastProjection = projection;
            _viewProjection = view * projection;

            _gl!.UseProgram(_shaderProgram);
            SetUniformMatrix4("view", view);
            SetUniformMatrix4("projection", projection);
            // Neutral unless the builder asks for the area's own light - see ShowAreaLighting.
            var (ambient, diffuse) = _showAreaLighting
                ? EditorSceneLighting(scene)
                : (NeutralAmbient, NeutralDiffuse);
            SetUniformVec3("cameraPos", eye);
            SetUniformVec3("lightDir", LightDir);
            SetUniformVec3("lightColor", diffuse);
            SetUniformVec3("ambientColor", ambient);
            SetUniformVec3("fogColor", scene.Lighting.FogColor);
            SetUniformFloat("fogDensity", _showFog ? scene.Lighting.FogDensity : 0f);
            SetUniformInt("diffuseTexture", 0);
            SetUniformInt("normalTexture", 1);
            SetUniformInt("specularTexture", 2);
            SetUniformInt("roughnessTexture", 3);
            SetUniformInt("environmentTexture", 4);
            SetUniformInt("tintMapTexture", 5);
            SetUniformInt("tintPaletteTexture", 6);
            SetUniformInt("tintAlphaTexture", 7);
            SetUniformBool("hasTintMap", false);
            SetUniformBool("hasTintAlpha", false);
            SetUniformBool("tintAlphaUsesRedChannel", false);
            SetUniformBool("hasNormalMap", false);
            SetUniformBool("hasSpecularMap", false);
            SetUniformBool("hasRoughnessMap", false);
            SetUniformBool("hasEnvironmentMap", false);
            SetUniformVec2("uvScale", Vector2.One);
            SetUniformVec2("uvOffset", Vector2.Zero);
            SetUniformBool("useTextureAlpha", false);

            DrawTileBatches(HidesCeilings(scene));

            DrawWalkmeshOverlay();
            DrawInstanceMarkers(scene);
            DrawPolygonOverlays();
            DrawSoundOverlays(scene);
            DrawSelectionHighlight();
            DrawTransformGizmo();
            DrawDoorAnchors();
            DrawPlacementGhost();
            DrawSelectedTileCell(scene);
            DrawTileCellHighlight(scene);

            _gl.BindVertexArray(0);
        }

        /// <summary>
        /// Turns on back-face culling for a pass that draws MDL geometry, the way NWN itself renders.
        /// </summary>
        /// <remarks>
        /// This is what lets an interior area be edited from above. A tile's ceiling faces downward
        /// into the room, so from an overhead camera only its back faces are visible; NWN culls them
        /// and you see the floor and everything standing on it. Drawn two-sided instead, the ceiling
        /// becomes an opaque lid: Mon Cala - Coral Isles - Facility rendered as flat slabs of bare
        /// tile texture with all 789 of its placeables sealed underneath, where Aurora shows the
        /// kelp, sand and machinery. Only the tallest props poked out, which is what made the area
        /// look empty rather than covered.
        /// <para>
        /// The winding is reliable enough to cull on: measured across virtunet, tatooine, wildwood,
        /// starfighter-interior tiles and base-game placeables, 96.6% of faces wind counter-clockwise
        /// about their own vertex normals, and the exceptions are not errors - foliage like
        /// <c>plc_kelp13</c> emits each leaf quad twice, once per facing (its per-mesh agree/disagree
        /// counts are exactly equal), which is how an NWN model asks to be seen from both sides under
        /// a culling renderer. Cutting the doubled halves is the intended result, not a loss.
        /// </para>
        /// <para>
        /// Scoped to the MDL passes rather than set once for the frame: the overlay geometry this
        /// control builds itself (markers, gizmo arms, walkmesh triangles, trigger outlines, ghost
        /// boxes, billboarded particle quads) is meant to be visible from either side.
        /// </para>
        /// </remarks>
        private void BeginModelFaceCulling()
        {
            _gl!.Enable(EnableCap.CullFace);
            _gl.CullFace(TriangleFace.Back);
            _gl.FrontFace(FrontFaceDirection.Ccw);
        }

        private void EndModelFaceCulling() => _gl!.Disable(EnableCap.CullFace);

        private void DrawTileBatches(bool hideCeilings)
        {
            if (_tileBatches == null)
                return;

            BeginModelFaceCulling();
            try
            {
                foreach (var batch in _tileBatches)
                {
                    if (batch.Model == null)
                    {
                        // The placeholder cube is this control's own geometry, not an MDL, so it
                        // draws under the same two-sided rule as the other overlays.
                        EndModelFaceCulling();
                        DrawFallbackBatch(batch.Placements);
                        BeginModelFaceCulling();
                        continue;
                    }

                    var buffer = GetOrBuildModelBuffer(batch.Model);
                    _gl!.BindVertexArray(buffer.Vao);

                    foreach (var placement in batch.Placements)
                    {
                        if (!IsPlacementVisible(placement))
                            continue;

                        foreach (var meshRange in buffer.MeshRanges)
                        {
                            if (hideCeilings && meshRange.TileFade != 0)
                                continue;

                            var worldMatrix = meshRange.MeshTransform * placement.Transform;
                            SetUniformMatrix4("model", worldMatrix);
                            var blending = BindMeshTexture(meshRange.TextureName, meshRange.MaterialName);

                            unsafe
                            {
                                _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                                    DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                            }
                            RestoreMeshBlending(blending);
                        }
                    }
                }
            }
            finally
            {
                EndModelFaceCulling();
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
            // One clock for the whole scene, so everything plays its idle together when the area opens
            // and settles together - which is what Aurora does, and what makes it read as the area
            // waking up rather than as objects twitching independently.
            var idleElapsed = IdlePlaybackSeconds_Elapsed();
            if (idleElapsed != null)
                RequestNextFrameRendering();
            var preview = PreviewAnimation();
            var previewNeedsFrames = false;

            var faceCullingEnabled = false;
            try
            {
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

                    var shouldCullFaces = CullInstanceModelFaces(raw.Kind);
                    if (shouldCullFaces != faceCullingEnabled)
                    {
                        if (shouldCullFaces)
                            BeginModelFaceCulling();
                        else
                            EndModelFaceCulling();
                        faceCullingEnabled = shouldCullFaces;
                    }

                    var instanceTransform = AreaPicking.ComputeInstanceTransform(instance);

                    var buffer = GetOrBuildModelBuffer(raw.Model!);
                    previewNeedsFrames |= preview.Running &&
                                          buffer.Animations.Any(animation =>
                                              string.Equals(
                                                  animation.Name,
                                                  preview.Name,
                                                  StringComparison.OrdinalIgnoreCase) &&
                                              animation.IsPlayable);
                    _gl!.BindVertexArray(buffer.Vao);
                    SetUniformBool("unlit", false);
                    foreach (var meshRange in buffer.MeshRanges)
                    {
                        SetUniformMatrix4(
                            "model",
                            PreviewMeshTransform(meshRange, buffer, preview, idleElapsed) * instanceTransform);
                        UseLayerColors(
                            meshRange.LayerColorIndices, instance.LayerColorIndices, raw.Model);
                        var itemOwnedCreatureMesh = instance.Kind == InstanceMarkerKind.Creature &&
                                                    meshRange.UsesItemTintOverrides;
                        var blending = BindMeshTexture(
                            meshRange.TextureName,
                            meshRange.MaterialName,
                            meshRange.LayerColorIndices,
                            itemOwnedCreatureMesh
                                ? meshRange.TintMapOverrides
                                : instance.TintMapOverrides,
                            itemOwnedCreatureMesh ? instance.TintMapOverrides : null);

                        unsafe
                        {
                            _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                                DrawElementsType.UnsignedInt,
                                (void*)PreviewMeshIndexOffset(meshRange, buffer, preview, idleElapsed));
                        }
                        RestoreMeshBlending(blending);
                    }
                }
            }
            finally
            {
                if (faceCullingEnabled)
                    EndModelFaceCulling();
            }

            DrawDoorTransitions(scene);

            DrawPreviewEmitters(scene, preview);
            if (previewNeedsFrames)
                RequestNextFrameRendering();

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
                if (DrawsAsModel(raw) || raw.IsDoorTransition)
                    continue;

                var instance = Displayed(raw);

                // Sounds draw as Aurora's musical-note billboard in DrawSoundOverlays instead of
                // the generic kind pyramid.
                if (instance.Kind == InstanceMarkerKind.Sound)
                    continue;

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
        /// Draws runtime-invisible area-transition doors from their authored hidden MDL surfaces,
        /// falling back to Aurora's standard two-by-three-metre doorway plane. Depth testing stays
        /// on so walls can occlude the plane naturally; depth writes are disabled so its transparent
        /// surface does not punch holes in geometry drawn later in the frame.
        /// </summary>
        private void DrawDoorTransitions(AreaScene scene)
        {
            if (_gl == null || !scene.Instances.Any(instance => instance.IsDoorTransition))
                return;

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);

            SetUniformBool("hasTexture", false);
            SetUniformBool("useTextureAlpha", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", DoorTransitionAlpha);
            SetUniformVec3("flatColor", DoorTransitionColor);

            foreach (var raw in scene.Instances)
            {
                if (!raw.IsDoorTransition)
                    continue;

                var instance = Displayed(raw);
                if (!IsInstanceVisible(instance))
                    continue;

                DrawDoorTransitionGeometry(instance);
            }

            _gl.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
            SetUniformFloat("flatAlpha", 1f);
        }

        /// <summary>
        /// Submits one transition door using flat-colour state already configured by the caller.
        /// </summary>
        private void DrawDoorTransitionGeometry(InstanceMarker instance)
        {
            var instanceTransform = AreaPicking.ComputeInstanceTransform(instance);
            if (instance.Model is { Meshes.Count: > 0 } model)
            {
                var buffer = GetOrBuildModelBuffer(model);
                _gl!.BindVertexArray(buffer.Vao);
                foreach (var meshRange in buffer.MeshRanges)
                {
                    SetUniformMatrix4("model", meshRange.MeshTransform * instanceTransform);
                    unsafe
                    {
                        _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                            DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                    }
                }

                return;
            }

            if (_doorTransitionBuffer is not { } fallback)
                return;

            _gl!.BindVertexArray(fallback.Vao);
            SetUniformMatrix4("model", instanceTransform);
            unsafe
            {
                _gl.DrawElements(PrimitiveType.Triangles, (uint)fallback.IndexCount,
                    DrawElementsType.UnsignedInt, (void*)0);
            }
        }

        /// <summary>
        /// Draws a deliberately bounded particle cue for emitter-based placeables. The model reader
        /// exposes emitter placement, texture, sprite grid, and blend mode rather than NWN's full
        /// particle controller set, so this is not an engine simulation. It is enough
        /// to make authored portal/fire effects visibly alive in the one-model design preview, and
        /// it is never entered by the area viewport because that host has no preview animation name.
        /// </summary>
        private void DrawPreviewEmitters(AreaScene scene, PreviewAnimationSnapshot preview)
        {
            if (preview.Name == null || _particleQuadBuffer is not { } quad || _gl == null)
                return;

            _gl.Enable(EnableCap.Blend);
            _gl.DepthMask(false);
            try
            {
                _gl.BindVertexArray(quad.Vao);
                SetUniformBool("unlit", true);
                SetUniformBool("useTextureAlpha", true);
                SetUniformFloat("alphaCutoff", 0.01f);

                var cameraDirection = _target - _cameraEye;
                var cameraForward = cameraDirection.LengthSquared() > 0.000001f
                    ? Vector3.Normalize(cameraDirection)
                    : Vector3.UnitY;

                foreach (var raw in scene.Instances)
                {
                    if (!DrawsAsModel(raw) || !IsInstanceVisible(Displayed(raw)))
                        continue;

                    var buffer = GetOrBuildModelBuffer(raw.Model!);
                    var animation = buffer.Animations.FirstOrDefault(
                        candidate => string.Equals(
                            candidate.Name,
                            preview.Name,
                            StringComparison.OrdinalIgnoreCase));
                    if (animation?.ShowsEmitters != true)
                        continue;

                    var instanceTransform = AreaPicking.ComputeInstanceTransform(Displayed(raw));
                    foreach (var emitter in buffer.Emitters)
                    {
                        if (!BindParticleTexture(emitter.TextureName))
                            continue;

                        var additive =
                            emitter.Blend.Contains("light", StringComparison.OrdinalIgnoreCase) ||
                            emitter.Blend.Contains("add", StringComparison.OrdinalIgnoreCase);
                        _gl.BlendFunc(
                            BlendingFactor.SrcAlpha,
                            additive ? BlendingFactor.One : BlendingFactor.OneMinusSrcAlpha);

                        var emitterTransform = PreviewEmitterTransform(emitter, buffer, preview) * instanceTransform;
                        var seed = StableParticleSeed(emitter.NodeName);
                        var spriteCount = Math.Max(1, emitter.XGrid * emitter.YGrid);

                        const int particles = 8;
                        for (var index = 0; index < particles; index++)
                        {
                            var phase = Fraction(
                                preview.Seconds * (0.45f + seed * 0.2f) +
                                index / (float)particles +
                                seed);
                            var angle = preview.Seconds * (0.8f + seed) +
                                        index * MathF.Tau / particles +
                                        seed * MathF.Tau;
                            var radius = 0.08f + phase * 0.55f;
                            var offset = new Vector3(
                                MathF.Cos(angle) * radius,
                                MathF.Sin(angle) * radius,
                                (phase - 0.35f) * 0.7f);
                            var worldPosition = Vector3.Transform(offset, emitterTransform);
                            var billboard = Matrix4x4.CreateBillboard(
                                worldPosition,
                                _cameraEye,
                                Vector3.UnitZ,
                                cameraForward);
                            var size = 0.18f + (1f - phase) * 0.34f;

                            SetUniformMatrix4("model", Matrix4x4.CreateScale(size) * billboard);
                            SetUniformFloat("flatAlpha", 0.2f + (1f - phase) * 0.8f);

                            var sprite = ((int)(preview.Seconds * 15f) + index) % spriteCount;
                            var spriteX = sprite % emitter.XGrid;
                            var spriteY = sprite / emitter.XGrid;
                            SetUniformVec2(
                                "uvScale",
                                new Vector2(1f / emitter.XGrid, 1f / emitter.YGrid));
                            SetUniformVec2(
                                "uvOffset",
                                new Vector2(
                                    spriteX / (float)emitter.XGrid,
                                    spriteY / (float)emitter.YGrid));

                            unsafe
                            {
                                _gl.DrawElements(
                                    PrimitiveType.Triangles,
                                    (uint)quad.IndexCount,
                                    DrawElementsType.UnsignedInt,
                                    (void*)0);
                            }
                        }
                    }
                }
            }
            finally
            {
                _gl.DepthMask(true);
                _gl.Disable(EnableCap.Blend);
                SetUniformBool("useTextureAlpha", false);
                SetUniformVec2("uvScale", Vector2.One);
                SetUniformVec2("uvOffset", Vector2.Zero);
                SetUniformFloat("flatAlpha", 1f);
            }
        }

        private bool BindParticleTexture(string textureName)
        {
            if (string.IsNullOrWhiteSpace(textureName))
                return false;

            // Particles draw unlit, so only the diffuse matters here.
            var texId = ResolveTexture(textureName, materialName: null).TexId;
            if (texId == 0)
                return false;

            SetUniformBool("hasTexture", true);
            _gl!.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, texId);
            return true;
        }

        private static float Fraction(float value) => value - MathF.Floor(value);

        private static float StableParticleSeed(string value)
        {
            uint hash = 2166136261;
            foreach (var character in value)
                hash = (hash ^ char.ToLowerInvariant(character)) * 16777619;

            return (hash & 0xffff) / 65535f;
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
        /// Draws every sound instance the way the reference toolset does: a billboarded musical
        /// note at the position (red head, black stem - depth-tested like the other kind markers),
        /// and for positional sounds its range graphics in <see cref="SoundRangeColor"/> - a dotted
        /// sphere plus solid equator ring at MinDistance and a flat circle at MaxDistance, drawn
        /// with depth testing off so a range is visible through terrain exactly as Aurora shows it.
        /// </summary>
        private void DrawSoundOverlays(AreaScene scene)
        {
            if (_gl == null)
                return;

            var cameraDirection = _target - _cameraEye;
            var cameraForward = cameraDirection.LengthSquared() > 0.000001f
                ? Vector3.Normalize(cameraDirection)
                : Vector3.UnitY;

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", 1f);

            if (_soundNoteBuffer is { } note)
            {
                _gl.BindVertexArray(note.Vao);

                foreach (var raw in scene.Instances)
                {
                    if (raw.Kind != InstanceMarkerKind.Sound)
                        continue;

                    var instance = Displayed(raw);
                    var billboard = Matrix4x4.CreateBillboard(
                        instance.Position, _cameraEye, Vector3.UnitZ, cameraForward);
                    SetUniformMatrix4("model", Matrix4x4.CreateScale(SoundNoteHeightMeters) * billboard);

                    unsafe
                    {
                        SetUniformVec3("flatColor", SoundNoteHeadColor);
                        _gl.DrawElements(PrimitiveType.Triangles, (uint)_soundNoteHeadIndexCount,
                            DrawElementsType.UnsignedInt, (void*)0);

                        SetUniformVec3("flatColor", SoundNoteStemColor);
                        _gl.DrawElements(PrimitiveType.Triangles,
                            (uint)(note.IndexCount - _soundNoteHeadIndexCount),
                            DrawElementsType.UnsignedInt, (void*)(_soundNoteHeadIndexCount * sizeof(uint)));
                    }
                }
            }

            if (!_hasSoundRangeBuffers)
                return;

            // Range graphics ignore the depth buffer: Aurora keeps a sound's rings visible across
            // (and through) terrain, which is what makes a 50m audible radius readable at a glance.
            _gl.Disable(EnableCap.DepthTest);
            _gl.DepthMask(false);
            SetUniformVec3("flatColor", SoundRangeColor);
            _gl.PointSize(2f);

            foreach (var raw in scene.Instances)
            {
                if (raw.Kind != InstanceMarkerKind.Sound || !raw.IsPositionalSound)
                    continue;

                var instance = Displayed(raw);
                var translation = Matrix4x4.CreateTranslation(instance.Position);

                if (instance.SoundMinDistance is { } min && min > 0.05f)
                {
                    SetUniformMatrix4("model", Matrix4x4.CreateScale(min) * translation);
                    _gl.BindVertexArray(_soundSphereVao);
                    _gl.DrawArrays(PrimitiveType.Points, 0, (uint)_soundSphereVertexCount);
                    _gl.BindVertexArray(_soundCircleVao);
                    _gl.DrawArrays(PrimitiveType.LineLoop, 0, (uint)_soundCircleVertexCount);
                }

                if (instance.SoundMaxDistance is { } max && max > 0.05f)
                {
                    SetUniformMatrix4("model", Matrix4x4.CreateScale(max) * translation);
                    _gl.BindVertexArray(_soundCircleVao);
                    _gl.DrawArrays(PrimitiveType.LineLoop, 0, (uint)_soundCircleVertexCount);
                }
            }

            _gl.PointSize(1f);
            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthMask(true);
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
            SetUniformVec3("flatColor", SelectionHighlightColor);
            SetUniformMatrix4("model", Matrix4x4.Identity); // bounds are already world-space

            _gl.DrawArrays(PrimitiveType.Lines, 0, 24);
        }

        /// <summary>
        /// How solid the placement ghost draws. High enough that the model's own textures read as
        /// themselves, low enough that the floor beneath still shows through and says "not yet placed".
        /// </summary>
        private const float PlacementGhostAlpha = 0.78f;

        /// <summary>Tint for a ghost with no model, and for a tile stamp the grid will refuse.</summary>
        private static readonly Vector3 PlacementGhostColor = new(0.36f, 0.61f, 0.96f);

        /// <summary>
        /// Tint for a ghost the next click will refuse: a door away from any empty doorway.
        /// </summary>
        /// <remarks>
        /// Same red as the tile-stamp highlight uses for a footprint that will not fit, and for the same
        /// reason - a preview that follows the cursor everywhere has to say where it can actually land,
        /// or a click that does nothing reads as the editor having stopped responding.
        /// </remarks>
        private static readonly Vector3 PlacementRefusedColor = new(0.90f, 0.30f, 0.30f);

        /// <summary>
        /// Fainter than the object ghost. A tile fills a whole 10m cell, so at the object ghost's
        /// opacity it blots out the area underneath it rather than previewing against it.
        /// </summary>
        private const float TileGhostAlpha = 0.6f;

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
        /// Drawn with its own textures and lighting, because the one question a ghost exists to answer
        /// is "is this the thing I meant to place?" - and a flat silhouette cannot answer it when the
        /// palette offers forty variations on the same crate. Translucency, not colour, is what marks
        /// it provisional. A blueprint whose model would not resolve still ghosts, as the kind's marker.
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
                Orientation = _snappedDoorAnchor?.Orientation ?? ghost.Orientation,
                VisualTransform = ghost.VisualTransform,
                LayerColorIndices = ghost.LayerColorIndices,
                Model = ghost.Model,
                IsDoorTransition = ghost.IsDoorTransition,
                TintMapOverrides = ghost.TintMapOverrides
            };

            var transform = AreaPicking.ComputeInstanceTransform(placed);

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);
            _gl.Disable(EnableCap.DepthTest);

            SetUniformFloat("flatAlpha", PlacementGhostAlpha);

            // A door away from any empty doorway draws as a red silhouette of itself: still the object
            // being placed, visibly not placeable here.
            var refused = SnapsToDoorAnchors && _snappedDoorAnchor == null;

            if (placed.IsDoorTransition)
            {
                SetUniformBool("hasTexture", false);
                SetUniformBool("useTextureAlpha", false);
                SetUniformBool("unlit", true);
                SetUniformFloat("alphaCutoff", 0f);
                SetUniformVec3("flatColor", refused ? PlacementRefusedColor : DoorTransitionColor);
                DrawDoorTransitionGeometry(placed);
            }
            else if (DrawsAsModel(placed))
            {
                var buffer = GetOrBuildModelBuffer(placed.Model!);
                _gl.BindVertexArray(buffer.Vao);
                if (refused)
                {
                    SetUniformBool("hasTexture", false);
                    SetUniformBool("unlit", true);
                    SetUniformFloat("alphaCutoff", 0f);
                    SetUniformVec3("flatColor", PlacementRefusedColor);
                }

                var cullFaces = CullInstanceModelFaces(placed.Kind);
                if (cullFaces)
                    BeginModelFaceCulling();
                try
                {
                    foreach (var meshRange in buffer.MeshRanges)
                    {
                        SetUniformMatrix4("model", meshRange.MeshTransform * transform);
                        var blending = TxiBlendMode.None;
                        if (!refused)
                        {
                            UseLayerColors(
                                meshRange.LayerColorIndices, placed.LayerColorIndices, placed.Model);
                            var tintMapOverrides = placed.TintMapOverrides;
                            IReadOnlyDictionary<string, int>? creatureTintMapOverrides = null;
                            if (meshRange.UsesItemTintOverrides)
                            {
                                tintMapOverrides = meshRange.TintMapOverrides.Count > 0 ||
                                                   placed.Kind != InstanceMarkerKind.Item
                                    ? meshRange.TintMapOverrides
                                    : placed.TintMapOverrides;
                                if (placed.Kind == InstanceMarkerKind.Creature)
                                    creatureTintMapOverrides = placed.TintMapOverrides;
                            }

                            blending = BindMeshTexture(
                                meshRange.TextureName,
                                meshRange.MaterialName,
                                null,
                                tintMapOverrides,
                                creatureTintMapOverrides);
                        }

                        unsafe
                        {
                            _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                                DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                        }
                        RestoreMeshBlending(blending, restoreStandardTransparency: true);
                    }
                }
                finally
                {
                    if (cullFaces)
                        EndModelFaceCulling();
                }
            }
            else if (_markerMeshBuffer is { } marker)
            {
                SetUniformBool("hasTexture", false);
                SetUniformBool("unlit", true);
                SetUniformFloat("alphaCutoff", 0f);
                SetUniformVec3("flatColor", refused ? PlacementRefusedColor : PlacementGhostColor);
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

        /// <summary>Size of a doorway marker relative to the kind markers, and how far it floats off the floor.</summary>
        private const float DoorAnchorMarkerScale = 0.9f;

        private const float DoorAnchorMarkerLift = 0.4f;

        /// <summary>The doorway the ghost is not currently in - available, but not the one about to be filled.</summary>
        private static readonly Vector3 DoorAnchorColor = new(0.94f, 0.78f, 0.22f);

        private const float DoorAnchorAlpha = 0.65f;

        /// <summary>
        /// While a door is armed, marks every doorway that is still empty.
        /// </summary>
        /// <remarks>
        /// A door cannot be placed anywhere else, so the builder needs to see where "anywhere else" is
        /// not - otherwise a ghost that refuses to go down looks like a bug rather than the rule it is.
        /// Only drawn during a door placement: the rest of the time these are noise, and there can be
        /// dozens of them in a corridor tileset. A doorway that already holds a door is left out, since
        /// it is not somewhere the next click can land either; so is the snapped one, which the ghost is
        /// already standing in.
        /// </remarks>
        private void DrawDoorAnchors()
        {
            if (!_isPlacementActive || !SnapsToDoorAnchors || _gl == null ||
                _markerMeshBuffer is not { } marker ||
                Volatile.Read(ref _sceneState).Scene is not { } scene ||
                scene.DoorAnchors.Count == 0)
            {
                return;
            }

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);
            _gl.Disable(EnableCap.DepthTest);

            _gl.BindVertexArray(marker.Vao);
            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", DoorAnchorAlpha);
            SetUniformVec3("flatColor", DoorAnchorColor);

            foreach (var anchor in scene.DoorAnchors)
            {
                if (_snappedDoorAnchor is { } snapped &&
                    snapped.TileIndex == anchor.TileIndex && snapped.DoorIndex == anchor.DoorIndex)
                {
                    continue;
                }

                if (scene.IsDoorwayFilled(anchor))
                    continue;

                SetUniformMatrix4("model",
                    Matrix4x4.CreateScale(DoorAnchorMarkerScale) *
                    Matrix4x4.CreateTranslation(
                        anchor.Position.X, anchor.Position.Y, anchor.Position.Z + DoorAnchorMarkerLift));

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

        /// <summary>
        /// Cell-highlight tint: green where the click will land, red where it will be refused.
        /// </summary>
        /// <remarks>
        /// Green and red rather than accent-and-red because this is a yes/no about the very next
        /// click, and an accent colour reads as "selected" rather than "allowed" - the builder has to
        /// learn which of the two blues means what. The pairing is the one Aurora uses.
        /// </remarks>
        private static readonly Vector3 TileCellHighlightColor = new(0.30f, 0.82f, 0.36f);

        private static readonly Vector3 TileCellRejectedColor = new(0.92f, 0.28f, 0.22f);

        /// <summary>
        /// The selected cell's tint - the same yellow the instance selection box uses, because it
        /// answers the same question ("this is the thing the commands will act on") rather than the
        /// hover highlight's "the next click lands here".
        /// </summary>
        private static readonly Vector3 SelectedTileCellColor = SelectionHighlightColor;

        private const float SelectedTileCellAlpha = 0.28f;

        /// <summary>
        /// Whether the armed tile would actually go down at this (column, row). Supplied by the area
        /// editor, which is the only thing that can answer it: in Auto mode the answer comes from the
        /// tileset's own rules, and "inside the grid" is not the same question.
        /// </summary>
        public Func<int, int, bool>? TilePlacementValidator { get; set; }

        /// <summary>Translucent enough to read the tile underneath - the builder is choosing between tiles, not covering one up.</summary>
        private const float TileCellHighlightAlpha = 0.45f;

        /// <summary>Lifts the highlight above the tile floor (and above the walkmesh overlay, which may be on at the same time) so it reads as painted on the ground rather than buried in it.</summary>
        private const float TileCellHighlightHeightOffset = 0.08f;

        /// <summary>Tints the selected grid cell, so the tile the raise/lower commands act on is visible.</summary>
        private void DrawSelectedTileCell(AreaScene scene)
        {
            // While a stamp is armed the hovered-cell highlight is the cursor, and a second tinted
            // cell beside it would only be read as a second cursor.
            if (_isPlacementActive || _isTilePlacementActive ||
                _selectedTileCell is not { } cell || _gl == null)
            {
                return;
            }

            var vertices = BuildFootprintQuadVertices(scene, cell.Column, cell.Row, (1, 1));
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
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertices), BufferUsageARB.DynamicDraw);
            SetVertexAttribPointers();

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", SelectedTileCellAlpha);
            SetUniformVec3("flatColor", SelectedTileCellColor);
            SetUniformMatrix4("model", Matrix4x4.Identity); // cell corners are already world-space

            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(vertices.Length / FloatsPerVertex));

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
            SetUniformFloat("flatAlpha", 1f);
        }

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
            if (_isPlacementActive || !_isTilePlacementActive || _gl == null)
                return;

            // The refusal wash goes down first so the cursor outline stays legible on top of it, and
            // unconditionally - a refusal must finish fading even if the pointer has since left the
            // map or the hover has not resolved a target this frame.
            DrawPaintRejectionFlash(scene);

            if (PaintTargetCenter() is { } target)
            {
                DrawPaintCursorSquare(scene, target.X, target.Y, IsPaintTargetValid());
                return;
            }

            if (_tileHoverCell is not { } anchor)
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
            SetUniformVec3("flatColor", TilePlacementAllowed(scene, anchor.Column, anchor.Row)
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
        /// The paint cursor's two colours, both sampled off the reference toolset at full purity:
        /// green when the dab under the cursor would be accepted, red when the solver would refuse
        /// it. The colour is the standing warning; <see cref="DrawPaintRejectionFlash"/> is what
        /// answers a click that lands on a red one.
        /// </summary>
        private static readonly Vector3 PaintCursorValidColor = new(0f, 1f, 0f);

        private static readonly Vector3 PaintCursorInvalidColor = new(1f, 0f, 0f);

        /// <summary>
        /// Where the armed brush would paint, as the world-space centre of its cursor square: a grid
        /// vertex for a terrain brush, an edge midpoint for a crosser. Null when no brush is armed
        /// or the pointer has not been over the map yet.
        /// </summary>
        private (float X, float Y)? PaintTargetCenter()
        {
            const float half = AreaSceneBuilder.TileSize / 2f;

            if (_tilePlacementTargetsEdge)
            {
                return _tileHoverEdge is { } edge
                    ? (edge.Column * AreaSceneBuilder.TileSize + (edge.Vertical ? 0f : half),
                       edge.Row * AreaSceneBuilder.TileSize + (edge.Vertical ? half : 0f))
                    : null;
            }

            if (_tilePlacementTargetsVertex)
            {
                return _tileHoverCell is { } vertex
                    ? (vertex.Column * AreaSceneBuilder.TileSize, vertex.Row * AreaSceneBuilder.TileSize)
                    : null;
            }

            return null;
        }

        /// <summary>Whether the armed brush's current target would be accepted, per the host's solver dry-run.</summary>
        private bool IsPaintTargetValid() =>
            _tilePlacementTargetsEdge
                ? _tileHoverEdge is { } edge &&
                  (TilePlacementEdgeValidator?.Invoke(edge.Column, edge.Row, edge.Vertical) ?? true)
                : _tileHoverCell is { } vertex &&
                  (TilePlacementValidator?.Invoke(vertex.Column, vertex.Row) ?? true);

        /// <summary>How long a refused paint stays lit under the cursor.</summary>
        /// <remarks>
        /// Long enough to register as an answer to the click even while the pointer keeps moving,
        /// short enough that a builder dabbing along a boundary is not left reading stale warnings.
        /// </remarks>
        private static readonly TimeSpan PaintRejectionFlashDuration = TimeSpan.FromMilliseconds(500);

        private long _paintRejectionTicks;
        private (float X, float Y)? _paintRejectionCenter;

        /// <summary>The refused stamp's anchor cell, when the flash is answering a stamp rather than a brush.</summary>
        private (int Column, int Row)? _paintRejectionAnchor;

        /// <summary>Lights the refusal flash over a stamp footprint that would not fit the grid.</summary>
        private void FlashStampRejection((int Column, int Row) anchor)
        {
            _paintRejectionCenter = null;
            _paintRejectionAnchor = anchor;
            _paintRejectionTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            RequestNextFrameRendering();
        }

        /// <summary>
        /// Marks a refused paint for the flash: the click landed but the solver declined it.
        /// </summary>
        /// <remarks>
        /// A refusal has to answer visibly or it reads as a dead click - the cursor is already red
        /// under the pointer, but a builder watching the map rather than the cursor sees nothing
        /// happen at all. The flash fills the same square the cursor outlines, so the warning is
        /// where the click was, and fades out on its own.
        /// </remarks>
        public void FlashPaintRejection()
        {
            _paintRejectionCenter = PaintTargetCenter();
            if (_paintRejectionCenter == null)
                return;

            _paintRejectionAnchor = null;
            _paintRejectionTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            RequestNextFrameRendering();
        }

        /// <summary>
        /// Fills the refused square with fading red over the cursor outline, and keeps frames coming
        /// until it has faded. A no-op once the flash has expired or none is pending.
        /// </summary>
        private void DrawPaintRejectionFlash(AreaScene scene)
        {
            if (_gl == null || (_paintRejectionCenter == null && _paintRejectionAnchor == null))
                return;

            var elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - _paintRejectionTicks)
                          / (double)System.Diagnostics.Stopwatch.Frequency;
            if (elapsed >= PaintRejectionFlashDuration.TotalSeconds)
            {
                _paintRejectionCenter = null;
                _paintRejectionAnchor = null;
                return;
            }

            var fade = (float)(1.0 - elapsed / PaintRejectionFlashDuration.TotalSeconds);
            const float half = AreaSceneBuilder.TileSize / 2f;

            float CornerZ(float x, float y)
            {
                var column = Math.Clamp((int)MathF.Floor(x / AreaSceneBuilder.TileSize), 0, scene.Width - 1);
                var row = Math.Clamp((int)MathF.Floor(y / AreaSceneBuilder.TileSize), 0, scene.Height - 1);
                return CellFloorHeight(scene, column, row) + TileCellHighlightHeightOffset;
            }

            float[] vertices;
            if (_paintRejectionAnchor is { } anchor)
            {
                // A stamp is refused as a whole footprint, so the flash covers what the cursor
                // outlined. Cells outside the grid contribute nothing, exactly as the hover does.
                vertices = BuildFootprintQuadVertices(scene, anchor.Column, anchor.Row);
                if (vertices.Length == 0)
                {
                    _paintRejectionAnchor = null;
                    return;
                }
            }
            else
            {
                var centre = _paintRejectionCenter!.Value;
                var minX = centre.X - half;
                var maxX = centre.X + half;
                var minY = centre.Y - half;
                var maxY = centre.Y + half;

                var data = new List<float>(6 * FloatsPerVertex);
                AppendCellQuadVertex(data, minX, minY, CornerZ(minX, minY));
                AppendCellQuadVertex(data, maxX, minY, CornerZ(maxX, minY));
                AppendCellQuadVertex(data, maxX, maxY, CornerZ(maxX, maxY));
                AppendCellQuadVertex(data, minX, minY, CornerZ(minX, minY));
                AppendCellQuadVertex(data, maxX, maxY, CornerZ(maxX, maxY));
                AppendCellQuadVertex(data, minX, maxY, CornerZ(minX, maxY));
                vertices = data.ToArray();
            }

            EnsureHighlightBuffer();
            if (!_hasHighlightBuffer)
                return;

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.Disable(EnableCap.DepthTest);
            _gl.DepthMask(false);

            _gl.BindVertexArray(_highlightVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _highlightVbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertices), BufferUsageARB.DynamicDraw);
            SetVertexAttribPointers();

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", PaintRejectionFlashAlpha * fade);
            SetUniformVec3("flatColor", PaintCursorInvalidColor);
            SetUniformMatrix4("model", Matrix4x4.Identity);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(vertices.Length / FloatsPerVertex));

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
            SetUniformFloat("flatAlpha", 1f);

            RequestNextFrameRendering(); // keep the fade animating
        }

        /// <summary>Peak opacity of the refusal flash - a clear wash, still translucent enough to read the ground through.</summary>
        private const float PaintRejectionFlashAlpha = 0.55f;

        /// <summary>
        /// Draws the paint cursor the way the reference toolset does: a wireframe square, one tile
        /// wide, centred on the paint target - a grid VERTEX for terrain (straddling the up to four
        /// cells that re-solve) or an EDGE midpoint for a crosser (straddling its two) - green when
        /// the paint would land, red when it would refuse. Each corner sits just above the floor of
        /// the cell it lies in, so the square reads as draped over a height seam rather than buried
        /// in it. Depth test off, like every other cursor here.
        /// </summary>
        private void DrawPaintCursorSquare(AreaScene scene, float cx, float cy, bool valid)
        {
            if (_gl == null)
                return;

            const float half = AreaSceneBuilder.TileSize / 2f;

            float CornerZ(float x, float y)
            {
                var column = Math.Clamp((int)MathF.Floor(x / AreaSceneBuilder.TileSize), 0, scene.Width - 1);
                var row = Math.Clamp((int)MathF.Floor(y / AreaSceneBuilder.TileSize), 0, scene.Height - 1);
                return CellFloorHeight(scene, column, row) + TileCellHighlightHeightOffset;
            }

            var vertices = new List<float>(4 * FloatsPerVertex);
            AppendCellQuadVertex(vertices, cx - half, cy - half, CornerZ(cx - half, cy - half));
            AppendCellQuadVertex(vertices, cx + half, cy - half, CornerZ(cx + half, cy - half));
            AppendCellQuadVertex(vertices, cx + half, cy + half, CornerZ(cx + half, cy + half));
            AppendCellQuadVertex(vertices, cx - half, cy + half, CornerZ(cx - half, cy + half));
            var data = vertices.ToArray();

            EnsureHighlightBuffer();
            if (!_hasHighlightBuffer)
                return;

            _gl.Disable(EnableCap.DepthTest);
            _gl.DepthMask(false);

            _gl.BindVertexArray(_highlightVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _highlightVbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(data.Length * sizeof(float)),
                new ReadOnlySpan<float>(data), BufferUsageARB.DynamicDraw);
            SetVertexAttribPointers();

            SetUniformBool("hasTexture", false);
            SetUniformBool("unlit", true);
            SetUniformFloat("alphaCutoff", 0f);
            SetUniformFloat("flatAlpha", 1f);
            SetUniformVec3("flatColor", valid ? PaintCursorValidColor : PaintCursorInvalidColor);
            SetUniformMatrix4("model", Matrix4x4.Identity);

            _gl.DrawArrays(PrimitiveType.LineLoop, 0, (uint)(data.Length / FloatsPerVertex));

            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthMask(true);
        }

        /// <summary>
        /// Draws the armed stamp's own tile models over the highlighted cells.
        /// </summary>
        /// <remarks>
        /// A tile is chosen for its shape - a doorway, a stair, a corner, a bridge - and an outlined
        /// cell shows none of it, so a builder had to stamp one to find out what it was. Drawn with its
        /// own textures, translucently, so the choice can be made on the artwork rather than on a
        /// silhouette; a stamp the grid will refuse drops to a flat red instead, the one case where the
        /// warning matters more than the picture. Called from inside
        /// <see cref="DrawTileCellHighlight"/>, which has already set up blending and turned the depth
        /// test off, and which leaves the outline underneath as the fits/does-not-fit signal.
        /// </remarks>
        /// <summary>
        /// Whether the armed tile may be placed at this cell: the grid's own bounds, and then whatever
        /// the editor says. Without a validator this is the bounds check alone, which is what it was
        /// before the rules had a say.
        /// </summary>
        private bool TilePlacementAllowed(AreaScene scene, int anchorColumn, int anchorRow) =>
            FootprintFitsGrid(scene, anchorColumn, anchorRow) &&
            (TilePlacementValidator == null || TilePlacementValidator(anchorColumn, anchorRow));

        private void DrawTileGhostModels(AreaScene scene, int anchorColumn, int anchorRow)
        {
            if (_gl == null || _tilePlacementModels.Count == 0)
                return;

            var (columns, rows) = _tilePlacementFootprint;
            var fits = TilePlacementAllowed(scene, anchorColumn, anchorRow);
            // The ghost has to show what the tile will look like once it is down, and in an interior
            // that is the room without its ceiling - otherwise the stamp a builder is aiming reads as
            // a translucent slab with its doorways and stairs hidden underneath.
            var hideCeilings = HidesCeilings(scene);

            SetUniformFloat("flatAlpha", TileGhostAlpha);
            if (!fits)
            {
                SetUniformBool("hasTexture", false);
                SetUniformBool("unlit", true);
                SetUniformFloat("alphaCutoff", 0f);
                SetUniformVec3("flatColor", TileCellRejectedColor);
            }

            BeginModelFaceCulling();
            try
            {
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
                        if (hideCeilings && meshRange.TileFade != 0)
                            continue;

                        SetUniformMatrix4("model", meshRange.MeshTransform * transform);
                        var blending = fits
                            ? BindMeshTexture(meshRange.TextureName, meshRange.MaterialName)
                            : TxiBlendMode.None;

                        unsafe
                        {
                            _gl.DrawElements(PrimitiveType.Triangles, (uint)meshRange.IndexCount,
                                DrawElementsType.UnsignedInt, (void*)meshRange.IndexOffset);
                        }
                        RestoreMeshBlending(blending, restoreStandardTransparency: true);
                    }
                }
            }
            finally
            {
                EndModelFaceCulling();
            }
        }

        /// <summary>
        /// Two world-space triangles per in-bounds cell of the footprint anchored bottom-left at
        /// (<paramref name="anchorColumn"/>, <paramref name="anchorRow"/>), each sitting just above
        /// that cell's own tile floor so the highlight follows elevation changes across the footprint.
        /// </summary>
        private float[] BuildFootprintQuadVertices(AreaScene scene, int anchorColumn, int anchorRow) =>
            BuildFootprintQuadVertices(scene, anchorColumn, anchorRow, _tilePlacementFootprint);

        private static float[] BuildFootprintQuadVertices(
            AreaScene scene, int anchorColumn, int anchorRow, (int Columns, int Rows) footprint)
        {
            var (columns, rows) = footprint;
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

        private static readonly Vector3 AuroraWaypointMarkerColor = new(0.98f, 0.80f, 0.10f);

        private static Vector3 MarkerColor(InstanceMarkerKind kind) => kind switch
        {
            InstanceMarkerKind.Creature => new Vector3(0.85f, 0.15f, 0.15f),
            InstanceMarkerKind.Door => new Vector3(0.55f, 0.35f, 0.15f),
            InstanceMarkerKind.Item => new Vector3(0.9f, 0.85f, 0.2f),
            InstanceMarkerKind.Placeable => new Vector3(0.2f, 0.45f, 0.9f),
            InstanceMarkerKind.Sound => new Vector3(0.2f, 0.8f, 0.8f),
            // Aurora represents merchants with the same yellow marker it uses for waypoints.
            InstanceMarkerKind.Store => AuroraWaypointMarkerColor,
            InstanceMarkerKind.Trigger => new Vector3(0.95f, 0.55f, 0.15f),
            // Aurora's waypoint yellow, for a waypoint whose appearance row names no model.
            InstanceMarkerKind.Waypoint => AuroraWaypointMarkerColor,
            _ => new Vector3(0.7f, 0.7f, 0.7f)
        };

        // ----- Per-RenderModel GPU buffer (uploaded once per distinct model per GL context) -----

        private ModelBuffer GetOrBuildModelBuffer(RenderModel model)
        {
            if (_modelBuffers.TryGetValue(model, out var existing))
            {
                existing.LastUsedFrame = _frameStamp;
                return existing;
            }

            var built = BuildModelBuffer(model);
            built.LastUsedFrame = _frameStamp;
            _modelBuffers[model] = built;
            return built;
        }

        /// <summary>
        /// Drops GPU resources no recent frame has drawn. Runs on the render thread with the GL
        /// context current. Model buffers evict individually by last-use stamp; the texture caches
        /// reset wholesale (they are shared by name across models, so per-entry lifetimes are not
        /// tracked) and live textures re-upload lazily on the next frame.
        /// </summary>
        private void EvictStaleGpuResources()
        {
            if (_gl == null)
                return;

            if (_modelBuffers.Count > ModelBufferEvictionScanThreshold)
            {
                List<RenderModel>? stale = null;
                foreach (var (model, buffer) in _modelBuffers)
                {
                    if (_frameStamp - buffer.LastUsedFrame > StaleModelBufferFrames)
                        (stale ??= new List<RenderModel>()).Add(model);
                }

                if (stale != null)
                {
                    foreach (var model in stale)
                    {
                        var buffer = _modelBuffers[model];
                        DeleteBuffer(buffer.Vao, buffer.Vbo, buffer.Ebo);
                        _modelBuffers.Remove(model);
                    }
                }
            }

            if (_textureCache.Count + _mapTextureCache.Count > TextureCacheResetThreshold &&
                _frameStamp - _lastTextureCacheResetFrame > TextureCacheResetCooldownFrames)
            {
                _lastTextureCacheResetFrame = _frameStamp;

                foreach (var diffuse in _textureCache.Values)
                {
                    if (diffuse.TexId != 0)
                        _gl.DeleteTexture(diffuse.TexId);
                }
                _textureCache.Clear();

                foreach (var texId in _mapTextureCache.Values)
                {
                    if (texId != 0)
                        _gl.DeleteTexture(texId);
                }
                _mapTextureCache.Clear();
                _rawTextureCache.Clear();
                _parsedMaterialCache.Clear();
            }
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

                var hasUvs = mesh.TexCoords.Length == vertexCount * 2;

                void AppendVertices(float[] positions, float[] normals)
                {
                    var hasNormals = normals.Length == vertexCount * 3;
                    for (var i = 0; i < vertexCount; i++)
                    {
                        vertices.Add(positions[i * 3]);
                        vertices.Add(positions[i * 3 + 1]);
                        vertices.Add(positions[i * 3 + 2]);

                        vertices.Add(hasNormals ? normals[i * 3] : 0f);
                        vertices.Add(hasNormals ? normals[i * 3 + 1] : 0f);
                        vertices.Add(hasNormals ? normals[i * 3 + 2] : 1f);

                        vertices.Add(hasUvs ? mesh.TexCoords[i * 2] : 0f);
                        vertices.Add(hasUvs ? mesh.TexCoords[i * 2 + 1] : 0f);
                    }
                }

                AppendVertices(mesh.Positions, mesh.Normals);
                var indexOffset = indices.Count * sizeof(uint);
                foreach (var index in mesh.Indices)
                    indices.Add(baseVertex + (uint)index);
                baseVertex += (uint)vertexCount;

                var poseIndexOffsets = new List<int>();
                var hasPoseVertices =
                    mesh.PosePositions.Count == mesh.PoseFrames.Count &&
                    mesh.PosePositions.All(frame => frame.Length == vertexCount * 3);
                if (hasPoseVertices)
                {
                    for (var frame = 0; frame < mesh.PosePositions.Count; frame++)
                    {
                        var frameNormals = frame < mesh.PoseNormals.Count
                            ? mesh.PoseNormals[frame]
                            : Array.Empty<float>();
                        AppendVertices(mesh.PosePositions[frame], frameNormals);

                        poseIndexOffsets.Add(indices.Count * sizeof(uint));
                        foreach (var index in mesh.Indices)
                            indices.Add(baseVertex + (uint)index);
                        baseVertex += (uint)vertexCount;
                    }
                }

                var animationIndexOffsets =
                    new Dictionary<string, IReadOnlyList<int>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (name, positionFrames) in mesh.AnimationPositions)
                {
                    if (!mesh.AnimationFrames.TryGetValue(name, out var transformFrames) ||
                        positionFrames.Count != transformFrames.Count ||
                        positionFrames.Any(frame => frame.Length != vertexCount * 3))
                    {
                        continue;
                    }

                    var offsets = new List<int>(positionFrames.Count);
                    mesh.AnimationNormals.TryGetValue(name, out var normalFrames);
                    for (var frame = 0; frame < positionFrames.Count; frame++)
                    {
                        var frameNormals = normalFrames != null && frame < normalFrames.Count
                            ? normalFrames[frame]
                            : Array.Empty<float>();
                        AppendVertices(positionFrames[frame], frameNormals);

                        offsets.Add(indices.Count * sizeof(uint));
                        foreach (var index in mesh.Indices)
                            indices.Add(baseVertex + (uint)index);
                        baseVertex += (uint)vertexCount;
                    }

                    animationIndexOffsets[name] = offsets;
                }

                meshRanges.Add(new MeshRange
                {
                    IndexOffset = indexOffset,
                    IndexCount = mesh.Indices.Length,
                    MeshTransform = mesh.Transform,
                    PoseFrames = mesh.PoseFrames,
                    PoseIndexOffsets = poseIndexOffsets,
                    AnimationFrames = mesh.AnimationFrames,
                    AnimationIndexOffsets = animationIndexOffsets,
                    TextureName = string.IsNullOrEmpty(mesh.TextureName) ? null : mesh.TextureName,
                    MaterialName = string.IsNullOrEmpty(mesh.MaterialName) ? null : mesh.MaterialName,
                    LayerColorIndices = mesh.LayerColorIndices,
                    UsesItemTintOverrides = mesh.UsesItemTintOverrides,
                    TintMapOverrides = mesh.TintMapOverrides,
                    TileFade = mesh.TileFade
                });
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

            return new ModelBuffer
            {
                Vao = vao,
                Vbo = vbo,
                Ebo = ebo,
                MeshRanges = meshRanges,
                Animations = model.Animations,
                Emitters = model.Emitters
            };
        }

        // ----- Textures -----

        /// <summary>
        /// Makes a model's dye choices current for the texture loads its meshes are about to
        /// trigger. PLT surfaces are only coloured at load, so this has to be set before the first
        /// BindMeshTexture of the model and stays set for the rest of its draw.
        /// </summary>
        private void UseLayerColors(
            IReadOnlyDictionary<int, int>? meshColors,
            IReadOnlyDictionary<int, int>? instanceColors,
            RenderModel? model)
        {
            // A garment's own item dyes win over the creature instance's chest-armor palette. The
            // instance remains next because it can change its body/armor dye without rebuilding.
            var colors = meshColors is { Count: > 0 }
                ? meshColors
                : instanceColors is { Count: > 0 }
                    ? instanceColors
                    : model?.LayerColorIndices;
            if (colors == null || colors.Count == 0)
            {
                _layerColors = null;
                _layerColorKey = string.Empty;
                return;
            }

            _layerColors = colors;
            _layerColorKey = "|" + string.Join(
                ",", colors.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"));
        }

        private TxiBlendMode BindMeshTexture(
            string? textureName,
            string? materialName = null,
            IReadOnlyDictionary<int, int>? layerColorIndices = null,
            IReadOnlyDictionary<string, int>? tintMapOverrides = null,
            IReadOnlyDictionary<string, int>? creatureTintMapOverrides = null)
        {
            var surfaceName = !string.IsNullOrWhiteSpace(materialName)
                ? materialName
                : textureName;
            var material = string.IsNullOrWhiteSpace(surfaceName)
                ? default
                : ResolveTexture(textureName!, materialName);

            SetUniformBool("unlit", false);

            if (material.TexId != 0)
            {
                SetUniformBool("hasTexture", true);
                var activeLayerColors = layerColorIndices is { Count: > 0 }
                    ? layerColorIndices
                    : _layerColors;
                BindTintMapState(
                    surfaceName!,
                    material,
                    activeLayerColors,
                    tintMapOverrides,
                    creatureTintMapOverrides);
                SetUniformFloat(
                    "alphaCutoff",
                    material.TintAlphaTexId != 0
                        ? MathF.Max(material.AlphaCutoff, material.TintAlphaCutoff)
                        : material.AlphaCutoff);

                // The display toggle gates the flags rather than the caches, so flipping it is
                // instant - the map textures stay resident and just stop being sampled.
                var useMaps = _showMaterialMaps;
                SetUniformBool("hasNormalMap", useMaps && material.NormalTexId != 0);
                SetUniformBool("hasSpecularMap", useMaps && material.SpecularTexId != 0);
                SetUniformBool("hasRoughnessMap", useMaps && material.RoughnessTexId != 0);
                SetUniformBool("hasEnvironmentMap", material.EnvironmentTexId != 0);
                _gl!.ActiveTexture(TextureUnit.Texture1);
                _gl.BindTexture(TextureTarget.Texture2D, material.NormalTexId);
                _gl.ActiveTexture(TextureUnit.Texture2);
                _gl.BindTexture(TextureTarget.Texture2D, material.SpecularTexId);
                _gl.ActiveTexture(TextureUnit.Texture3);
                _gl.BindTexture(TextureTarget.Texture2D, material.RoughnessTexId);
                _gl.ActiveTexture(TextureUnit.Texture4);
                _gl.BindTexture(TextureTarget.Texture2D, material.EnvironmentTexId);

                // Unit 0 last, so every other draw path (particles, markers) that assumes the
                // active unit is Texture0 keeps working untouched.
                _gl.ActiveTexture(TextureUnit.Texture0);
                _gl.BindTexture(TextureTarget.Texture2D, material.TexId);
            }
            else
            {
                SetUniformBool("hasTexture", false);
                SetUniformBool("hasTintMap", false);
                SetUniformBool("hasTintAlpha", false);
                SetUniformBool("tintAlphaUsesRedChannel", false);
                SetUniformBool("hasNormalMap", false);
                SetUniformBool("hasSpecularMap", false);
                SetUniformBool("hasRoughnessMap", false);
                SetUniformBool("hasEnvironmentMap", false);
                SetUniformFloat("alphaCutoff", 0f);
                SetUniformVec3("flatColor", UntexturedTileColor);
            }

            ConfigureMeshBlending(material.Blending);
            return material.Blending;
        }

        private void ConfigureMeshBlending(TxiBlendMode blending)
        {
            if (blending != TxiBlendMode.Additive)
                return;

            _gl!.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            _gl.DepthMask(false);
        }

        private void RestoreMeshBlending(
            TxiBlendMode blending,
            bool restoreStandardTransparency = false)
        {
            if (blending != TxiBlendMode.Additive)
                return;

            if (restoreStandardTransparency)
            {
                _gl!.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                _gl.DepthMask(false);
                return;
            }

            _gl!.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
        }

        private MeshMaterial ResolveTexture(string rawTextureName, string? materialName)
        {
            if (ResourceIndex == null)
                return default;

            var hasMaterial = !string.IsNullOrWhiteSpace(materialName);
            var surfaceName = hasMaterial
                ? materialName!
                : rawTextureName;
            MtrMaterial? parsedMaterial = null;
            if (hasMaterial)
            {
                parsedMaterial = TryParseMaterial(surfaceName);
            }
            else
            {
                // Same-resref fallback is only valid for the generated tint shaders. A plain mesh
                // whose bitmap happens to share a resref with an unrelated MTR must keep the bitmap.
                var candidate = TryParseMaterial(surfaceName);
                if (IsTintMapMaterial(candidate))
                    parsedMaterial = candidate;
            }

            var resolveMaterial = hasMaterial || parsedMaterial != null;
            var rawKey = (resolveMaterial ? "m|" : "t|") + surfaceName + _layerColorKey;
            if (_rawTextureCache.TryGetValue(rawKey, out var memo))
                return memo;

            MaterialMaps maps;
            try
            {
                maps = MaterialResolver.ResolveMaterialMaps(
                    ResourceIndex,
                    surfaceName,
                    resolveMaterial);
            }
            catch (Exception)
            {
                parsedMaterial = null;
                maps = new MaterialMaps { Diffuse = surfaceName };
            }

            // Keyed by the dye set as well as the resref: one PLT dyed two ways is two different
            // pictures, and caching on the name alone handed the first item's colours to the second.
            var diffuseKey = maps.Diffuse + _layerColorKey;
            if (!_textureCache.TryGetValue(diffuseKey, out var cached))
            {
                cached = LoadAndUploadTexture(maps.Diffuse);
                _textureCache[diffuseKey] = cached;
            }

            // A mesh whose diffuse failed to resolve draws flat-colored; loading its maps
            // anyway would waste GPU memory on textures the shader never samples.
            var isTintMap = IsTintMapMaterial(parsedMaterial);
            var alphaSource = isTintMap ? parsedMaterial?.GetAlphaSource() : null;
            var material = cached.TexId == 0
                ? new MeshMaterial(0, 0f, TxiBlendMode.None, 0, 0, 0, 0, 0, 0, 0, false, 0f)
                : new MeshMaterial(
                    cached.TexId,
                    cached.AlphaCutoff,
                    cached.Blending,
                    ResolveMapTexture(maps.Normal),
                    ResolveMapTexture(maps.Specular),
                    ResolveMapTexture(maps.Roughness),
                    ResolveMapTexture(
                        isTintMap
                            ? cached.EnvironmentMapTexture ??
                              TextureRenderPolicy.StandaloneEnvironmentMap
                            : cached.EnvironmentMapTexture),
                    isTintMap
                        ? ResolveMapTexture(parsedMaterial!.GetTexture(7))
                        : 0,
                    isTintMap
                        ? ResolveMapTexture(parsedMaterial!.GetTexture(10))
                        : 0,
                    ResolveTintAlphaTexture(parsedMaterial),
                    alphaSource?.UsesRedChannel == true,
                    alphaSource?.Cutoff ?? 0f);

            _rawTextureCache[rawKey] = material;
            return material;
        }

        private MtrMaterial? TryParseMaterial(string surfaceName)
        {
            if (_parsedMaterialCache.TryGetValue(surfaceName, out var cached))
                return cached;

            MtrMaterial? material;
            try
            {
                material = MaterialResolver.TryParseMaterial(ResourceIndex!, surfaceName);
            }
            catch (Exception)
            {
                material = null;
            }

            _parsedMaterialCache[surfaceName] = material;
            return material;
        }

        private static bool IsTintMapMaterial(MtrMaterial? material)
        {
            return TintMapTextureRenderer.IsTintMapMaterial(material);
        }

        private uint ResolveTintAlphaTexture(MtrMaterial? material)
        {
            if (!IsTintMapMaterial(material))
                return 0;

            return ResolveMapTexture(material!.GetAlphaSource()?.TextureName);
        }

        private void BindTintMapState(
            string materialName,
            MeshMaterial material,
            IReadOnlyDictionary<int, int>? layerColorIndices,
            IReadOnlyDictionary<string, int>? tintMapOverrides,
            IReadOnlyDictionary<string, int>? creatureTintMapOverrides)
        {
            var hasTintMap = material.TintMapTexId != 0 && material.TintPaletteTexId != 0;
            var hasTintAlpha = hasTintMap && material.TintAlphaTexId != 0;
            SetUniformBool("hasTintMap", hasTintMap);
            SetUniformBool("hasTintAlpha", hasTintAlpha);
            SetUniformBool(
                "tintAlphaUsesRedChannel",
                hasTintAlpha && material.TintAlphaUsesRedChannel);
            if (!hasTintMap)
                return;

            _gl!.ActiveTexture(TextureUnit.Texture5);
            _gl.BindTexture(TextureTarget.Texture2D, material.TintMapTexId);
            _gl.ActiveTexture(TextureUnit.Texture6);
            _gl.BindTexture(TextureTarget.Texture2D, material.TintPaletteTexId);
            _gl.ActiveTexture(TextureUnit.Texture7);
            _gl.BindTexture(TextureTarget.Texture2D, material.TintAlphaTexId);
            _gl.ActiveTexture(TextureUnit.Texture0);

            for (var layerValue = 0; layerValue < 10; layerValue++)
            {
                var layer = (TintMapLayerType)layerValue;
                var activeOverrides = TintMapVariable.IsCreatureColorLayer(layer) &&
                                      creatureTintMapOverrides != null
                    ? creatureTintMapOverrides
                    : tintMapOverrides;
                var savedValue = TintMapOverrides.GetMaterialColor(
                    activeOverrides,
                    materialName,
                    layer);

                if (TintMapColor.TryFromStoredValue(savedValue, out var custom))
                {
                    SetUniformVec4(
                        $"tintColor{layerValue}",
                        new Vector4(
                            custom.Red / 255f,
                            custom.Green / 255f,
                            custom.Blue / 255f,
                            1f));
                }
                else
                {
                    SetUniformVec4($"tintColor{layerValue}", Vector4.Zero);
                }

                var paletteColor = savedValue > 0 &&
                                   savedValue <= TintMapMaterialRegistry.PaletteColorCount
                    ? savedValue - 1
                    : layerColorIndices != null &&
                      layerColorIndices.TryGetValue(layerValue, out var standardColor)
                        ? standardColor
                        : 0;
                var paletteCoordinate = TintMapMaterialRegistry.GetPaletteCoordinate(
                    layer,
                    Math.Clamp(
                        paletteColor,
                        0,
                        TintMapMaterialRegistry.PaletteColorCount - 1));
                SetUniformFloat($"tintPaletteRow{layerValue}", paletteCoordinate);
            }
        }

        private uint ResolveMapTexture(string? mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                return 0;

            if (_mapTextureCache.TryGetValue(mapName, out var texId))
                return texId;

            texId = LoadAndUploadMapTexture(mapName);
            _mapTextureCache[mapName] = texId;
            return texId;
        }

        private uint LoadAndUploadMapTexture(string mapName)
        {
            try
            {
                var image = TextureLoader.Load(ResourceIndex!, mapName);
                return image == null
                    ? 0u
                    : UploadTexture(image.Width, image.Height, image.Pixels, mapName);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private UploadedDiffuse LoadAndUploadTexture(string resolvedName)
        {
            try
            {
                var image = TextureLoader.Load(ResourceIndex!, resolvedName, _layerColors);
                if (image == null)
                    return default;

                var texId = UploadTexture(image.Width, image.Height, image.Pixels, resolvedName);
                var hints = TextureRenderPolicy.Resolve(ResourceIndex!, resolvedName, image);
                return new UploadedDiffuse(
                    texId,
                    hints.AlphaCutoff,
                    hints.EnvironmentMapTexture,
                    hints.Blending);
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <summary>
        /// Uploads top-first decoded RGBA pixels as an OpenGL texture. Every image, including the
        /// row-addressed tint palette, must be flipped so v = 0 lands on its bottom decoded row.
        /// </summary>
        private uint UploadTexture(int width, int height, byte[] rgba, string resourceName)
        {
            var texId = _gl!.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, texId);

            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte,
                new ReadOnlySpan<byte>(PrepareTextureUploadPixels(resourceName, width, height, rgba)));

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            _gl.GenerateMipmap(TextureTarget.Texture2D);

            _gl.BindTexture(TextureTarget.Texture2D, 0);
            return texId;
        }

        private static byte[] PrepareTextureUploadPixels(
            string resourceName,
            int width,
            int height,
            byte[] rgba)
        {
            return TextureOrientation.FlipRows(width, height, rgba);
        }

        // ----- Static placeholder/marker geometry (scene-independent; built once at GL init) -----

        private void BuildStaticMeshes()
        {
            var (cubeVertices, cubeIndices) = BuildFallbackCubeMesh();
            _fallbackCubeBuffer = UploadStaticMesh(cubeVertices, cubeIndices);

            var (markerVertices, markerIndices) = BuildMarkerPyramidMesh();
            _markerMeshBuffer = UploadStaticMesh(markerVertices, markerIndices);

            var (transitionVertices, transitionIndices) = BuildDoorTransitionMesh();
            _doorTransitionBuffer = UploadStaticMesh(transitionVertices, transitionIndices);

            var (particleVertices, particleIndices) = BuildParticleQuadMesh();
            _particleQuadBuffer = UploadStaticMesh(particleVertices, particleIndices);

            var (noteVertices, noteIndices, noteHeadIndexCount) = BuildSoundNoteMesh();
            _soundNoteBuffer = UploadStaticMesh(noteVertices, noteIndices);
            _soundNoteHeadIndexCount = noteHeadIndexCount;

            BuildSoundRangeBuffers();
        }

        /// <summary>
        /// Aurora's sound marker: an upright musical note billboard, unit height in local Y with
        /// its head bottom at the local origin. Indices are ordered head first so the draw can
        /// tint the leading <see cref="_soundNoteHeadIndexCount"/> indices red and the rest black.
        /// </summary>
        private static (float[] Vertices, uint[] Indices, int HeadIndexCount) BuildSoundNoteMesh()
        {
            var vertices = new List<float>();
            var indices = new List<uint>();

            uint AddVertex(float x, float y)
            {
                var index = (uint)(vertices.Count / FloatsPerVertex);
                vertices.Add(x);
                vertices.Add(y);
                vertices.Add(0f);
                vertices.Add(0f);
                vertices.Add(0f);
                vertices.Add(1f);
                vertices.Add(0f);
                vertices.Add(0f);
                return index;
            }

            // Head: a slightly tilted ellipse fanned about its centre.
            const int headSegments = 14;
            const float headCenterX = 0f, headCenterY = 0.12f;
            const float headRadiusX = 0.17f, headRadiusY = 0.12f;
            var headCenter = AddVertex(headCenterX, headCenterY);
            var rim = new uint[headSegments];
            for (var i = 0; i < headSegments; i++)
            {
                var angle = MathF.Tau * i / headSegments;
                rim[i] = AddVertex(
                    headCenterX + MathF.Cos(angle) * headRadiusX,
                    headCenterY + MathF.Sin(angle) * headRadiusY);
            }

            for (var i = 0; i < headSegments; i++)
            {
                indices.Add(headCenter);
                indices.Add(rim[i]);
                indices.Add(rim[(i + 1) % headSegments]);
            }

            var headIndexCount = indices.Count;

            // Stem: thin rectangle rising from the head's right edge.
            var s0 = AddVertex(0.13f, 0.16f);
            var s1 = AddVertex(0.20f, 0.16f);
            var s2 = AddVertex(0.20f, 0.97f);
            var s3 = AddVertex(0.13f, 0.97f);
            indices.AddRange(new[] { s0, s1, s2, s0, s2, s3 });

            // Flag: a wedge sweeping down-right from the stem top.
            var f0 = AddVertex(0.20f, 1.00f);
            var f1 = AddVertex(0.46f, 0.66f);
            var f2 = AddVertex(0.36f, 0.58f);
            var f3 = AddVertex(0.20f, 0.78f);
            indices.AddRange(new[] { f0, f1, f2, f0, f2, f3 });

            return (vertices.ToArray(), indices.ToArray(), headIndexCount);
        }

        /// <summary>
        /// Unit-radius sound-range geometry, scaled per sound at draw time: a 64-segment line-loop
        /// circle (the MaxDistance ring, and the sphere's solid equator), and a dotted sphere -
        /// points at every latitude/longitude crossing, matching the dot grid the reference
        /// toolset draws for MinDistance.
        /// </summary>
        private void BuildSoundRangeBuffers()
        {
            const int circleSegments = 64;
            var circle = new List<float>(circleSegments * FloatsPerVertex);
            for (var i = 0; i < circleSegments; i++)
            {
                var angle = MathF.Tau * i / circleSegments;
                AppendOverlayVertex(circle, new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f));
            }

            const int longitudes = 24;
            const int latitudes = 17; // every ~10 degrees, poles excluded
            var sphere = new List<float>(longitudes * latitudes * FloatsPerVertex);
            for (var lat = 1; lat <= latitudes; lat++)
            {
                var pitch = MathF.PI * lat / (latitudes + 1) - MathF.PI / 2f;
                var ringRadius = MathF.Cos(pitch);
                var z = MathF.Sin(pitch);
                for (var lon = 0; lon < longitudes; lon++)
                {
                    var angle = MathF.Tau * lon / longitudes;
                    sphere.Add(MathF.Cos(angle) * ringRadius);
                    sphere.Add(MathF.Sin(angle) * ringRadius);
                    sphere.Add(z);
                    sphere.Add(0f);
                    sphere.Add(0f);
                    sphere.Add(1f);
                    sphere.Add(0f);
                    sphere.Add(0f);
                }
            }

            _soundCircleVertexCount = circle.Count / FloatsPerVertex;
            _soundSphereVertexCount = sphere.Count / FloatsPerVertex;

            _soundCircleVao = _gl!.GenVertexArray();
            _soundCircleVbo = _gl.GenBuffer();
            _gl.BindVertexArray(_soundCircleVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _soundCircleVbo);
            var circleData = circle.ToArray();
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(circleData.Length * sizeof(float)),
                new ReadOnlySpan<float>(circleData), BufferUsageARB.StaticDraw);
            SetVertexAttribPointers();

            _soundSphereVao = _gl.GenVertexArray();
            _soundSphereVbo = _gl.GenBuffer();
            _gl.BindVertexArray(_soundSphereVao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _soundSphereVbo);
            var sphereData = sphere.ToArray();
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sphereData.Length * sizeof(float)),
                new ReadOnlySpan<float>(sphereData), BufferUsageARB.StaticDraw);
            SetVertexAttribPointers();
            _gl.BindVertexArray(0);

            _hasSoundRangeBuffers = true;
        }

        /// <summary>Appends one position-only vertex in the shared 8-float layout (up normal, zero UV).</summary>
        private static void AppendOverlayVertex(List<float> target, Vector3 position)
        {
            target.Add(position.X);
            target.Add(position.Y);
            target.Add(position.Z);
            target.Add(0f);
            target.Add(0f);
            target.Add(1f);
            target.Add(0f);
            target.Add(0f);
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

        private static (float[] Vertices, uint[] Indices) BuildParticleQuadMesh()
        {
            // Unit square in local XY. Matrix4x4.CreateBillboard supplies the camera-facing world
            // transform; the shared vertex layout still carries a normal for the common shader.
            var vertices = new[]
            {
                -0.5f, -0.5f, 0f, 0f, 0f, 1f, 0f, 0f,
                 0.5f, -0.5f, 0f, 0f, 0f, 1f, 1f, 0f,
                 0.5f,  0.5f, 0f, 0f, 0f, 1f, 1f, 1f,
                -0.5f,  0.5f, 0f, 0f, 0f, 1f, 0f, 1f
            };
            return (vertices, new uint[] { 0, 1, 2, 0, 2, 3 });
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

        /// <summary>
        /// Thin two-sided box matching <see cref="DoorTransitionMarker"/>'s fallback bounds. A
        /// small depth keeps the transition visible from an edge-on editing angle and gives picking
        /// the same stable volume the renderer shows.
        /// </summary>
        private static (float[] Vertices, uint[] Indices) BuildDoorTransitionMesh()
        {
            var min = DoorTransitionMarker.LocalMinimum;
            var max = DoorTransitionMarker.LocalMaximum;
            var c = new[]
            {
                new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z),
                new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z),
                new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z),
                new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z)
            };

            var builder = new BoxMeshBuilder();
            builder.AddQuad(c[3], c[2], c[1], c[0], new Vector3(0, 0, -1));
            builder.AddQuad(c[4], c[5], c[6], c[7], new Vector3(0, 0, 1));
            builder.AddQuad(c[0], c[1], c[5], c[4], new Vector3(0, -1, 0));
            builder.AddQuad(c[2], c[3], c[7], c[6], new Vector3(0, 1, 0));
            builder.AddQuad(c[3], c[0], c[4], c[7], new Vector3(-1, 0, 0));
            builder.AddQuad(c[1], c[2], c[6], c[5], new Vector3(1, 0, 0));
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

        /// <summary>
        /// How far apart the draped samples along a trigger outline's edges sit. Fine enough that
        /// an edge crossing a terrain rise visibly follows the ground the way Aurora's outlines
        /// do, coarse enough that a whole area's triggers stay a trivial vertex count.
        /// </summary>
        private const float PolygonDrapeStepMeters = 1f;

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

                // Each edge (including the loop-closing one) is subdivided and every sample draped
                // onto the walkmesh under it, so the outline hugs the floor across slopes and
                // height seams the way the reference toolset draws it. Off the walkmesh the sample
                // keeps the edge's own interpolated height.
                var count = 0;
                for (var i = 0; i < marker.Geometry.Count; i++)
                {
                    var from = marker.Geometry[i];
                    var to = marker.Geometry[(i + 1) % marker.Geometry.Count];
                    var steps = Math.Max(1, (int)MathF.Ceiling(
                        Vector2.Distance(new Vector2(from.X, from.Y), new Vector2(to.X, to.Y)) /
                        PolygonDrapeStepMeters));

                    // The end point is skipped: it is the next edge's start sample, and LineLoop
                    // closes the final edge back to the first sample.
                    for (var step = 0; step < steps; step++)
                    {
                        var t = step / (float)steps;
                        var sample = Vector3.Lerp(from, to, t);
                        var z = AreaWalkmesh.GroundHeightAt(scene.Tiles, sample.X, sample.Y) ?? sample.Z;

                        vertexFloats.Add(sample.X);
                        vertexFloats.Add(sample.Y);
                        vertexFloats.Add(z + PolygonHeightOffset);
                        vertexFloats.Add(0f);
                        vertexFloats.Add(0f);
                        vertexFloats.Add(1f);
                        vertexFloats.Add(0f);
                        vertexFloats.Add(0f);
                        count++;
                    }
                }

                ranges.Add((start, count));
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
