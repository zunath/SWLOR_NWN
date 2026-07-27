# Viewport rendering approach

The SWLOR Toolset renders 3D content with Avalonia's `OpenGlControlBase` and Silk.NET.OpenGL.
Format decoding and scene preparation stay in `SWLOR.Toolset.Domain`; the app project owns only
the OpenGL control, GPU resources, and interaction layer.

## Data flow

1. `ResourceIndex` resolves hak and optional base-game resources.
2. `SWLOR.NWN.Formats` reads MDL, TGA, PLT, KEY/BIF, 2DA, TLK, and GFF data.
3. Domain render services compose creature parts, flatten model transforms, resolve materials and
   textures, and emit `RenderModel` data.
4. `AreaSceneBuilder` combines area tiles and placed instances into a scene.
5. `GlAreaControl` uploads meshes and textures, batches draw calls, and handles camera, picking,
   overlays, gizmos, animation previews, and bounded emitter cues.

The viewport code does not parse game formats and the formats library has no dependency on the
toolset or game server.

## Current preview surface

Blueprint and tile artwork is rendered into the Palette by `BlueprintPreviewRenderer`; the area
editor uses the same model resolver and composer for placement ghosts. Keeping one render pipeline
for previews and placed objects prevents the two surfaces from drifting.

## Rendering choices

- Interleaved vertex buffers contain position, normal, and texture coordinates.
- Mesh draw calls retain their effective material or texture resource identity.
- Textures are decoded to a top-left, row-major RGBA convention before GPU upload.
- Tile and instance geometry uses the same world-transform convention and animation pose evaluator.
- The area renderer batches repeated tile meshes and keeps texture caches scoped to a GL context.
- Emitters use a deliberately bounded visual cue; the editor does not simulate the engine's full
  particle-controller system.

## Known constraints

- GPU and driver behavior varies. Initialization or render failures must degrade to a visible
  status message rather than crash the editor.
- A missing NWN:EE install removes base-game artwork but does not prevent hak-only startup.
- Particle previews are representative editing cues, not exact engine playback.
