# Importing generated starship models

`tools/ImportShipModel.py` turns a generated GLB (Meshy or TRELLIS.2) into a native ship
appearance, such as a Hull Design for an existing ship. Commit only the finished MDL and
DDS in `SWLOR_Haks/sw_cr_vehicle/` and the `appearance.2da` row. Keep the GLB, manifest,
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
     "node_positions": {"reactor": [0, -0.42, 0.52]}
   }
   ```

   - `model` and `texture` are lowercase resrefs of at most 16 characters. Hull Designs use
     the reference name plus `_h<n>`.
   - `orientation` is a proper rotation applied after Blender's glTF import. The result must
     have its nose on +Y and its top on +Z, the frame stock ships use. Inspect the import in
     Blender first; a nose on -Y, for example, needs `[[-1,0,0],[0,-1,0],[0,0,1]]`.
   - `max_faces` is the hull budget. The tool collapses denser meshes to it. Small ships stay
     near the stock 1,000-2,500 faces; larger hulls may use more, up to 20,000.
   - `node_positions` moves copied nodes (usually the engine emitters) onto the new hull.
     Positions are in the fitted model frame; read them off the export in Blender.

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
   texture to DXT1 DDS with mipmaps. `package/runtime_resources` holds the two HAK files.

4. **Register the appearance.** Copy the reference ship's `appearance.2da` row to the next free
   row, set `LABEL` to `"[SWLOR] Ship - <ship> Hull Design <n>"` and `NAME` and `RACE` to the new
   model. Keep every other column (size category, footsteps, sounds) from the reference.

5. **Verify in the client** with the offline launcher (`InEngineVisualVerification.md`): the
   ship's space area, the new design and the stock reference side by side, from the front,
   rear (engine glow) and above. Check orientation, size, texture alignment and the emitter
   position before committing.

## Generating sources with Meshy

Meshy costs credits, so confirm the model and cost with the owner before each task. The image
route gives the most control: a design image (`meshy_text_to_image`) the owner approves, then
`meshy_image_to_3d` from that image with `target_formats: ["glb"]`. Smart Topology
(`model_type: "smart-topology"`, `ai_model: "meshy-t2.5"`) builds a clean triangle mesh at the
face budget directly, so the export barely decimates it. Download the GLB into staging, never
into the repository.

## Tests

```powershell
python -B tools/tests/test_import_ship_model.py
& $blender -b --factory-startup --python-exit-code 1 --python tools/tests/test_import_ship_model.py
```

The Blender run adds an end-to-end GLB export that checks the face budget and the fit.
