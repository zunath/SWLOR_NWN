# Importing generated starship models

`tools/ImportShipModel.py` turns a generated GLB (Meshy or TRELLIS.2) into a native ship
appearance, such as a Hull Design for an existing ship. Commit only the finished MDL, DDS
(plus MTR and normal-map DDS when used) in `SWLOR_Haks/sw_cr_vehicle/` and the `appearance.2da` row. Keep the GLB, manifest,
exports and captures in ignored staging outside the repository, and delete them once the
resources are committed (`AgentEnvironment.md`, "Disk hygiene").

## What a ship model is

Player and NPC ships are creature appearances (`SetCreatureAppearanceType`). Each is a
static simple model with `classification CHARACTER` and no animations:

- a root dummy named after the model;
- `headconjure` and `impact` dummies (effect and hit attach points);
- one or more engine `emitter` nodes (the stock glow is `fxpa_flare`);
- textured `trimesh` hull geometry with a plain `bitmap` texture and no MTR.

Ships are not scaled in code, so the model's size is its in-game size. The stock Aurek is
about 0.9 m long and 1,236 faces. A Hull Design copies every non-mesh node from the ship it
belongs to (the reference) and is fitted to the reference's footprint and center, so its
effects, selection and camera match the ship it skins.

## Workflow

Tools: Blender 4.0, CleanModels (`SWLOR_Haks/tools/SetupModelCompiler.ps1`), NWN Crunch EE
(see `BlasterModelImport.md`), and a full `SWLOR_Haks` checkout for `--haks` (a sparse
worktree lacks `tools/`). Every output directory must be new, empty and outside the repository.

1. **Decompile the reference ship** from the HAK sources:

   ```powershell
   python -B tools/ImportShipModel.py --mode reference --haks <SWLOR_Haks> `
     --mdl <SWLOR_Haks>/sw_cr_vehicle/c_rep_aurek.mdl --output $env:TEMP/ship/ref
   ```

2. **Write a fit manifest** next to the GLB:

   ```json
   {
     "schema_version": 1,
     "source": "aurek_h1.glb", "sha256": "<sha256 of the GLB>",
     "model": "c_rep_aurek_h1", "texture": "c_rep_aurek_h1",
     "reference": "../ref/c_rep_aurek.mdl",
     "orientation": [[1, 0, 0], [0, 1, 0], [0, 0, 1]],
     "max_faces": 4000, "texture_size": 1024,
     "node_positions": {"reactor": [-0.1, -0.32, 0.53]},
     "node_copies": {"reactor2": {"from": "reactor", "position": [0.1, -0.32, 0.53]}},
     "material": {"ambient": [0.7, 0.7, 0.7], "diffuse": [0.85, 0.85, 0.85]},
     "texture_adjust": {"exposure": 0.9, "contrast": 1.1},
     "normal_map": "opengl"
   }
   ```

   - `model` and `texture` are lowercase resrefs of at most 16 characters (14 for `texture`
     with a normal map, which adds `_n`). Hull Designs use the reference name plus `_h<n>`,
     shortened when that is too long (`c_chs_trir_h1`, `c_mando_jhv_h1`).
   - `orientation` is a proper rotation applied after Blender's glTF import. The result must
     have its nose on +Y and its top on +Z, the frame stock ships use. Inspect the import in
     Blender first; a nose on -Y, for example, needs `[[-1,0,0],[0,-1,0],[0,0,1]]`.
   - `max_faces` is the hull budget. The tool collapses denser meshes to it. Small ships stay
     near the stock 1,000-2,500 faces; larger hulls may use more, up to 20,000.
   - `node_positions` moves copied nodes (usually the engine emitters) onto the new hull.
     Positions are in the fitted model frame; export once without them and read the engine
     nozzles off the exported MDL or in Blender.
   - `node_copies` duplicates a reference node under a new name, such as one emitter per
     engine when the design has more engines than the reference.
   - `emitters` (a list of `[x, y, z]`) replaces every reference emitter with copies of the
     first one, named `reactor1`, `reactor2` and so on. Use it when the design's engines don't
     match the reference's; it can't be combined with `node_positions` or `node_copies`. Place
     each glow on its own engine's rear face, not the hull's rearmost point: render the fitted
     hull from behind on a coordinate grid, read off each nozzle's x and z, then take the
     rearmost vertex near that x and z within the rear half of the hull (hollow exhausts have
     no geometry at their center).
   - References may nest, rotate and scale their nodes (stock capitals often do). The tool
     works out each node's world transform, measures the hull in world space and copies every
     non-mesh node flattened onto the root. Repeated names get a numeric suffix.
   - `texture_size` is 1024 for small ships: on the Aurek design a 2048 export looked the same
     in the client at 3.5 times the size. A normal map doubles the texture cost.
   - `material` sets the hull's `ambient` and `diffuse` (default 1). Generated textures are
     brighter than stock art, and at 1 their whites blow out; 0.7 and 0.85 brought the Aurek
     design in line with the stock ship.
   - `texture_adjust` applies `exposure`, then `contrast` about mid grey, then `saturation` to
     the base color. Darken near-white generated hulls slightly (exposure 0.85-0.9).
   - `normal_map` (`none`, `opengl`, `flip_green`) exports the GLB's normal texture as
     `<texture>_n` (DXN), writes `<texture>.mtr` and gives the hull `materialname` and
     `renderhint NormalAndSpecMapped`. Meshy maps use `opengl`. The map is not reprojected
     into the compiled tangent basis (unlike the weapon importer), so check seams and
     highlights in the client.

3. **Export** (Blender) and **package** (Python):

   ```powershell
   $blender = 'C:/Program Files/Blender Foundation/Blender 4.0/blender.exe'
   & $blender -b --factory-startup --python-exit-code 1 --python tools/ImportShipModel.py -- `
     --mode export --manifest $env:TEMP/ship/fit.json --output $env:TEMP/ship/export
   python -B tools/ImportShipModel.py --mode package --haks <SWLOR_Haks> `
     --export $env:TEMP/ship/export --output $env:TEMP/ship/package --crunch <nwn_crunch.exe>
   ```

   Export joins the meshes, applies the orientation, decimates to the budget, fits the hull to
   the reference, resamples the base color texture to a square TGA, and writes the ASCII MDL
   with an `export_manifest.json` of hashes. Package checks those hashes, compiles the MDL with
   CleanModels, decompiles it to confirm every node and face survived, and compresses the
   texture to DXT1 DDS with mipmaps (and the normal map to DXN). `package/runtime_resources`
   holds the HAK files: the MDL and DDS, plus the MTR and `_n` DDS with a normal map.

4. **Register the appearance.** Copy the reference ship's `appearance.2da` row to the next free
   row, set `LABEL` to `"[SWLOR] Ship - <ship> Hull Design <n>"` and `NAME` and `RACE` to the new
   model. Keep every other column (size category, footsteps, sounds) from the reference.
   Released designs:

   | Row | Model | Ship (stock row) |
   |---|---|---|
   | 10269 | `c_rep_aurek_h1` | Aurek Strikefighter (10074) |
   | 10270 | `c_sith_eclp_h1` | Eclipse Interceptor (2006) |
   | 10271 | `c_fl_smtr_h1` | Hutt Hauler (10186) |
   | 10272 | `c_hutt_corv_h1` | Hutt Corvette (10108) |
   | 10273 | `c_mando_jhv_h1` | Jehavey'ir (10154) |
   | 10274 | `c_chs_trir_h1` | Chiss Trireme (10162) |
   | 10275 | `c_yz2500_h1` | Corellian Gunboat (10145) |
   | 10276 | `c_fl_artr_h1` | Czerka Armored Transport (10218) |

5. **Verify in the client** with the offline launcher (`InEngineVisualVerification.md`): the
   ship's space area, the new design and the stock reference side by side, from the front,
   rear (engine glow) and above. Check orientation, size, texture alignment and the emitter
   position before committing. The camera aims above the creature, so ships sit low in the
   frame: a pitch near 28 degrees centers most hulls, and tiny ones (the 0.13 m Eclipse) need
   about 8. Show the owner the in-game shots before adding a batch to a pull request.

## Generating sources with Meshy

Meshy costs credits, so confirm the model and cost with the owner before each task, and pay
Meshy only for 3D: make the concept image locally (FLUX.1 [schnell] in ComfyUI) and have the
owner pick one. Then run `meshy_image_to_3d` with `ai_model: "meshy-7.1"`, `enable_pbr: true`
(for the normal map), `should_remesh: true` and `target_polycount` at the face budget, with
`target_formats: ["glb"]` (30 credits). Remesh targets used so far: 3,000 faces for Small and
Medium ships, 4,000-5,000 for Large and Capital ones. Meshy has put every ship nose on -X and top
on +Z, so `orientation` has been `[[0,1,0],[-1,0,0],[0,0,1]]`; check each one anyway. On the Aurek design this beat Smart Topology
(`meshy-t2.5`, 15 credits), whose mesh was clean but whose texture was flat and soft. Unremeshed
7.1 output is far too dense for the engine. Download the GLB into staging, never into the
repository.

## Tests

```powershell
python -B tools/tests/test_import_ship_model.py
& $blender -b --factory-startup --python-exit-code 1 --python tools/tests/test_import_ship_model.py
```

The Blender run adds an end-to-end GLB export that checks the face budget, the fit and the
normal-map material.
