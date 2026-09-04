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

## Compile against the engine's conditional declarations

The base shader disables normal mapping but still samples `texUnit1` for
optional legacy cutout alpha. NWN's `inc_common` declares that sampler only
when `NORMAL_MAP == 1`, so the base shader must supply its own declaration
under the opposite guard. The mapped variants use the engine declaration.
Missing this declaration causes a shader compile failure when the module loads
the generated materials. Fixing compilation alone does not validate texture
loading or prevent the DDS overread described below.

Compile and link the production vertex/fragment pairs with the installed
engine's complete include tree for each supported shader quality. A material
test adapter that declares extra uniforms cannot validate this contract; keep
its numerical lighting checks separate from the production compilation checks.

## Declare and enforce single-level packed textures

Each generated ATI2/BC5 DDS stores only its base level. Set
`DDSD_MIPMAPCOUNT` and `dwMipMapCount = 1`, and ship a same-resref TXI containing
`mipmap 0` alongside every DDS in `sw_tint0`, `sw_tint1`, and `sw_tint2`.
The TXI is required: the installed 89.8193.37-17 client's compressed upload
loop derives further levels from the texture dimensions when mipmaps are
enabled, regardless of the DDS mip count. Merely fixing the header is
insufficient. The mask's green channel contains categorical layer IDs, so
filtered mipmap generation is inappropriate for this texture.

The NPC-spawn crash dump confirmed a `glCompressedTexImage2D` call requesting
the 512 x 512 first mip (262,144 bytes) from a pointer exactly 1,048,576 bytes
after a 1024 x 1024 BC5 base level. That DDS contained no mip payload, and the
driver faulted reading beyond the buffer. Inspection of the installed client
confirmed that TXI `mipmap 0` controls the condition that skips this upload loop.

Use `python SWLOR_Haks/tools/GenerateTintMapAssets.py --refresh-packed-metadata`
to repair existing DDS headers and create the TXIs without recompressing
pixels. Generation, relocation, deduplication, and removal must preserve each
DDS/TXI pair. Both the asset audit and C# corpus test require the pair; the GPU
test rejects missing or enabled-mipmap TXIs before issuing compressed uploads.
Rebuild and deploy all three `sw_tint*.hak` archives after this repair.

## Regression scene and checks

The placed Bounty Hunter and Force Sensitive Civilian in `ooc_area` exercise
this failure. Their materials include `pme0_head056` (Rodian face and hat),
`pmh0_pelvis102`, `pmh0_legl104`, `pfh0_head121`, and `pfh0_robe187`.
These are already registered tint materials; no NPC appearance changes are
needed.

Run `python SWLOR_Haks/tools/GenerateTintMapAssets.py --check` and the relevant
`TintMapReviewTests` after changes. On Windows, run
`python SWLOR_Haks/tools/TestTintShaderMaterials.py --game-data "<NWN>/data"`
to compile the production shader pairs and exercise the installed engine's
material function on the GPU, including actual compressed NPC texture draws
and negative controls for unsafe DDS/TXI metadata, the missing sampler, and
metallic lighting failures. Rebuild `sw_shader.hak` and load it in a
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
