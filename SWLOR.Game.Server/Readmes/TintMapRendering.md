# Tint-map material lighting

The generated tint materials share three fragment shaders in
`SWLOR_Haks/sw_shader`: `fs_plt_tinter`, `fs_plt_tinter_nm`, and
`fs_plt_hair_nm`. A change to palette lighting must cover all three, including
materials shared by multiple models through `tintmap.2da`.

## Initialize lighting from the final palette surface

NWN's `SetupStandardShaderInputs()` initializes more than texture coordinates
and normals. It also calls `SetupSpecularity()` and caches specularity,
roughness, metallicness, and specular color for `ApplyStandardShader()`.

The tint shaders call standard setup before replacing its diffuse input with
the PLT palette. The engine explicitly supports surfaces with
`texture0Bound == 0`; on that path, standard setup assumes full environment
reflection. Merely assigning the correct `fEnvMapLevel` afterward leaves the
cached lighting metallic. Even when the white placeholder is bound, its color
is not the final palette albedo.

After setting both the palette environment coverage and the final
`FragmentColor`, call `SetupSpecularity(FragmentColor.rgb *
materialFrontDiffuse.rgb)` again, before `ApplyStandardShader()`. Use the same
lighting preprocessor guard as the engine so shader quality modes without
fragment specular lighting still compile.

Preserve the engine's authored specular and roughness maps, material overrides,
palette alpha, and hair cutouts. Disabling specularity globally or editing each
NPC would hide the initialization error and remove intentional material detail.

## Regression scene and checks

The placed Bounty Hunter and Force Sensitive Civilian in `ooc_area` exercise
this failure. Their materials include `pme0_head056` (Rodian face and hat),
`pmh0_pelvis102`, `pmh0_legl104`, `pfh0_head121`, and `pfh0_robe187`.
These are already registered tint materials; no NPC appearance changes are
needed.

Run `python SWLOR_Haks/tools/GenerateTintMapAssets.py --check` and the relevant
`TintMapReviewTests` after changes. On Windows, run
`python SWLOR_Haks/tools/TestTintShaderMaterials.py --game-data "<NWN>/data"`
to exercise all three shader bodies with the installed engine's material
function on the GPU, including a negative control that reproduces chrome when
the repair is removed. Rebuild `sw_shader.hak` and load it in a
fresh client session for visual verification. Shader-only changes do not
require changing the module's existing HAK list or regenerating tint maps.

The deployed module must actually list `sw_tint_mtr`, `sw_tint0`, `sw_tint1`,
and `sw_tint2`. Files merely present in the HAK directory are not loaded.
The runtime model registry can still report success because `tintmap.2da`
lives in `sw_2da`, independently of the material and mask HAKs.

After changing the module HAK list, run the CLI with `--pack
".\Star Wars LOR v2.mod" --no-prompt` from `Module` and deploy that rebuilt
module. The normal `DeployBuild` operation copies the existing packed module;
it does not repack changes to `module.ifo.json`. An old module paired with
converted model HAKs can therefore leave custom NPCs without any texture or
material, while stock player parts still render.
