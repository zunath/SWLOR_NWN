# Viewport rendering approach (WP4.1 spike findings)

Decision record for how the SWLOR Toolset renders 3D content, based on reading Radoub
v0.11.0's working implementation (Quartermaster/Reliquary previews).

## What Radoub does (and we adopt)

- **GL host:** Avalonia's built-in `OpenGlControlBase` with **Silk.NET.OpenGL** bindings —
  no third-party GL control. See `External\Radoub\Radoub.UI\Radoub.UI\Controls\
  ModelPreviewGLControl.cs` (~request context via `OnOpenGlInit`, render in `OnOpenGlRender`,
  `RequestNextFrameRendering()` for continuous animation).
- **Shaders:** small GLSL pair managed by `OpenGLShaderManager` (same folder): perspective
  projection, per-pixel directional lighting, texture sampling, PLT tinting.
- **Geometry:** one interleaved VBO + EBO built from `Radoub.Formats.Mdl.MdlModel` trimesh
  nodes; per-mesh draw calls carry texture names; textures cached per GL context.
- **Textures:** `Radoub.UI.Services.TextureService` resolves TGA/DDS/PLT by resref through
  `Radoub.Formats.Services.IGameDataService` (it uses only `FindResource`,
  `FindBaseResource`, and `FindResourceWithSource`).
- **Creature part composition:** `Radoub.UI.Services.MdlPartComposer` assembles segmented
  (MODELTYPE=P) player-style models from body-part MDLs.

## What SWLOR reuses directly

- `SwlorGameDataService` (this folder): a minimal `IGameDataService` adapter over our
  layered `ResourceIndex` (hak sources + base-game KEY/BIF). Only the resource-access
  members are implemented; 2DA/TLK members delegate to our own services where cheap, and
  the soundset/palette members intentionally return empty (documented inline) — the
  texture/model path never calls them.

## What SWLOR builds new (WP4.4/4.5)

The AREA renderer is our own `OpenGlControlBase` subclass following the same skeleton
(init/render/deinit + Silk.NET), because Radoub has no tile/scene renderer:

- `AreaSceneBuilder` (Domain): are.json `Tile_List` × `TilesetDefinition` (SET parser) ×
  `ResourceIndex` → per-tile MDL placements on the 10m grid (orientation = 0-3 × 90°,
  height × tileset transition height) + instance markers from git.json.
- Camera: orbit/pan/zoom like `ModelViewController` (Radoub.UI) but with a ground-plane
  focus; reuse its math where the class is public.
- Instanced/batched tile meshes; texture cache keyed per GL context, PLT irrelevant for
  tiles (plain TGA/DDS + TXI hints).

## Current preview surface

The spike's standalone Model Preview dock was removed. Blueprint and tile artwork is rendered into
the Palette by `BlueprintPreviewRenderer`; the area editor uses the same model resolver and composer
for placement ghosts. This keeps previews next to the builder's actual selection and avoids a dock
whose only update path depended on an Explorer selection that no longer lists blueprints.

## Known constraints

- GPU/driver differences: `OpenGlControlBase` uses Avalonia's shared GL context; if a
  machine falls back to software rendering the area view degrades but must not crash.
- Emitters/particles are not rendered (Radoub explicitly reports `PreviewState.Incomplete`).
- Animations exist in the control (play/pause API) but are out of scope until WP4.3+.
