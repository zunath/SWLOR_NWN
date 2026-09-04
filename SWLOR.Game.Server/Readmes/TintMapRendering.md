# Tint-map material lighting

The generated tint materials share three fragment shaders in
`SWLOR_Haks/sw_shader`: `fs_plt_tinter`, `fs_plt_tinter_nm`, and
`fs_plt_hair_nm`. A change to palette lighting must cover all three, including
materials shared by multiple models through `tintmap.2da`.

## Preserve the authored palette rows

Converting a PLT material must preserve the creature and equipment color IDs.
The placed Force Sensitive Civilian in `ooc_area` has the same values as
`master`: skin 2, hair 31, cloth 174/3, and leather 3/174. In the palette atlas,
hair row 31 is blue and cloth row 174 is brown. The generated materials' row-zero
defaults instead produce brown hair and pale cloth; seeing those defaults means
the runtime palette overrides were not applied or retained. The blueprint
colors themselves are unchanged.

A complete creature or world-item tint refresh must reset the shader overrides
once, before writing any rows. Every subsequent row write in that refresh must
leave earlier rows intact. The 89.8193.37-17 client replays the native override
list in order. A type-0 reset becomes an empty value, and the client's material
setter restores **all** shared-material defaults without filtering by the
requested parameter name. Thus named resets can erase earlier valid rows even
when the server's native list still contains those row values. This also applies
to the composed models used by creature body parts.

Both complete refreshes and individual row updates must use a write-only row
helper. Only the complete refresh may clear old RGB/custom-mode parameters.
For the civilian, this reduces a full refresh from 309 material calls to 45
(one reset and 44 row writes); the Rodian drops from 57 to nine. Palette IDs,
blueprint colors, and atlas pixels remain unchanged.

## Match material parameter arity to the shader

Declare each palette row with exactly one value, for example
`parameter float rowHair 0.086182`. The shader declares `uniform float rowHair`.
Adding three zeroes to the MTR declaration makes NWN retain a four-component
parameter and upload it with `glUniform4fv`. OpenGL rejects that call for a scalar
uniform with `GL_INVALID_OPERATION`, leaving the shader's row-zero initializer
unchanged. Correct native server rows and successful shader linking cannot detect
this failure.

The script API's `SetMaterialShaderUniformVec4` transport remains valid. With a
one-component MTR declaration, the client reads the first supplied value and uses
`glUniform1f`. The MTR declaration determines the GPU upload size; the script
transport does not require a four-component MTR parameter.

The production GPU test must parse MTR component counts and follow NWN's upload
dispatch. It must reject the former four-component declaration and verify that
authored NPC rows replace shader defaults. A harness that always calls
`glUniform1f` bypasses the defect. Keep atlas pixels and palette IDs unchanged;
the correction belongs in every generated material and in the generator.

After correcting these declarations, rebuild and deploy `sw_tint_mtr.hak`,
then fully restart the client to reload its cached materials. This repair does
not require changing NPC blueprints, repacking the module, or recompiling models.

Generated MTRs declare the active row uniforms, mask dimensions, and applicable
cutout settings. Obsolete RGB and custom-mode parameters are removed when
refreshing materials; declaring uniforms absent from the shader creates repeated
link warnings during model loading.

The `Tint` native engine tests cover automatic civilian spawn, repeated complete
refreshes, and individual color edits. They inspect the native parameter list,
including the absence of reset records. These checks validate the server state;
visual matching and spawn responsiveness still require a fresh client session.

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

### Ship compiled models

`HakBuilder` packages the model bytes already present in each source folder;
the legacy `CompileModels` configuration field does not invoke a compiler.
Leaving converted MDLs in text format makes the client compile them while
loading creatures. The tint asset audit rejects active tint models in this
format.

Use `SWLOR_Haks/tools/CompileModels.py` before packaging changed model folders.
Select individual resources with repeated `--model <resref>` arguments, or a
branch's changes with `--since <HAK-base-commit>`. Supply `--game-data
"<NWN>/data"` for stock supermodels. The default run stages and validates;
`--apply` replaces source MDLs only after the entire selected batch passes.
The script also compiles the required ASCII supermodel dependencies.

Legacy sources may need `--repair-legacy-inputs` for omitted neutral UV data or
an unused final constraint. Each repair is recorded. The explicit
`--allow-missing-supermodels` option retains unresolved supermodel names while
compiling the local hierarchy as NWN does; it rejects any skin bone that would
require the missing model. Compile-only vertex tags prevent the old compiler
from merging different skin weights, and authored normals and vertex colors
are restored in the binary data when the compiler would discard them.

Validation must inspect the binary geometry, UVs, skin weights, vertex colors,
node transforms, and material bindings. A decompile-only comparison is
insufficient: the old decompiler can merge vertices with distinct skin weights
and omit vertex colors. Preserve authored colors and bindings when repairing
legacy exporter omissions; do not substitute default colors to make an audit
pass. Rebuild every affected model HAK and verify its packed MDLs against the
validated source bytes.

The audit also checks exact binary section lengths and bounded node/material
pointers. Ten older robe resources had variable-length renames inside fixed-size
string fields: `pfe22`/`pfo22` robes 027, 172, 174, and 200, plus `pfh22` robes
172 and 174. Restoring their string padding preserves the authored names and
all numeric geometry data. A fallback text scan must never hide structural
corruption in a deployed binary model.

### Exercise the reported NPCs

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
