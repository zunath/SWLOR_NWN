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
Palette IDs and blueprint colors remain unchanged.

## Preserve exact custom RGB

RGB edits persist the requested bytes in the existing TMC/TMG/TM variables.
The editor must load these values after reopening, not remember a requested
color in a session cache while rendering a nearest preset. Native preset
clicks clear the corresponding RGB override; unset armor parts inherit TMG,
and explicit part colors remain independent. These edits do not replace or
re-equip items.

`TintMapShaderColor.Encode` carries all 24 RGB bits through the existing scalar
row parameter. Atlas coordinates occupy [0,1). Custom values occupy [1,4),
using the two float exponent ranges' 2^24 distinct values. All three shaders
decode this value before lighting. They multiply RGB by a neutral reference
row's luminance curve, normalized at shade128, preserving shading without
importing a preset's hue or reflection mask. The Toolset uses the same shading
formula. Preset lookup and authored colors are unchanged.

Run `TestTintRgb.py --game-data <NWN data directory>` for GPU RGB checks and
`TestTintShaderMaterials.py` for native engine shader/MTR and preset parity.
`TintMapShaderColorTests` exhaustively checks every RGB byte combination;
the native AppearanceEditor tests cover scalar storage, drafts, reopening,
inheritance and preset reset. Shader changes require rebuilding sw_shader.hak
and fully restarting the client.

The editor's global and per-part swatches draw exact RGB over their native
preset image. NUI polyline points must be a flat array of alternating X/Y floats,
not an array of `NuiVec` objects; the latter silently leaves the preset visible.
Keep the preset image and custom fill in the same draw list: NUI only retains
one `draw_list` on each widget. The native editor tests validate the serialized
coordinates and both drawing layers for all 120 swatches, along with the color
and custom-mode publications after an RGB edit.

## Preserve the robe's native palette transport

The 89.8193.37-17 client does not replay creature material overrides onto the
separate robe attachment. Its ordinary body and head receive those records;
the robe does not, even for an empty material-name wildcard. Repeating the
refresh or writing the records on the equipped item cannot repair that gap.
The civilian reproduced it with all 14 robe materials retaining defaults while
the head had the correct rows. A successful server-side row test therefore
does not establish that a robe received its colors.

NWN has another per-material transport: `PLTscheme[15]`. The native body-part
loader fills it from the wearer and equipment when its selected PLT resource
exists, and the renderer supplies it to custom shaders. Converted robes retain
tiny, one-pixel PLT control resources for that lookup. Their visible shade and
layer data still come from the full BC5 tint mask; the control is not artwork.
Do not reconvert or remove these controls, replace stock PLTs, or treat them as
authored diffuse textures when resolving model materials.

Robe-consuming materials opt into the native fallback and use negative row
defaults. A received nonnegative scripted row still takes priority. Otherwise,
the shader decodes the native color byte from the corresponding scheme entry
and uses that color in the existing 2048-row tint atlas. The native scheme
uses 256-row palette blocks divided by 1792, so it cannot be sampled as an
atlas coordinate directly. Skin, hair, tattoos, cloth, leather, and metal all
need the same conversion.

Native body parts, helmets, tails, and wings select `pal_armor01` for both
metal layers. `pal_armor02` is a different palette and is not the native Metal2
selection for this corpus. Keep the established Metal2 atlas base at row 528,
but fill it from `pal_armor01` and use matching picker colors. Do not modify
the original palette resources themselves. The GPU regression checks every
color and shade against the original palette RGBA, including this distinction.

This transport only supports native palette IDs. Unsupported robe/body
combinations retain the nearest-preset compatibility projection for persisted
RGB, with RGB editing disabled and presets available. Preserve the authored
palette values separately from projected values so reset restores the original
color and per-part inheritance. NPCs without overrides must retain their raw
palette fields.

## Exact RGB on robes

`RobeModelRenderer` uses `roberender.2da` to select a generated body root when
the worn robe has an effective RGB override. The body root contains the robe's
geometry and therefore receives the same material scalars as other body parts.
Robe choices must have an actual MDL for the wearer's gender, race and original
body phenotype. `RobeAppearance` checks the module's resource search space, which
includes native models without RGB materials. The shared style list alone is not
proof of availability: male human styles 18, 19, 24 and 166 have no models, while
female human styles 19 and 24 do. The editor filters these combinations and uses
the displayed model ID directly when stepping across gaps. Opening the editor or
applying an outfit with an unavailable robe restores robe 0 on the equipped armor,
retaining its other parts, dyes, properties and identity. Saved outfit templates
are unchanged. This is a missing-model recovery, not a replacement mesh for a
gender that the source robe does not support.

An empty robe attachment for that phenotype preserves the original robe number
and its native body-part hiding rules without drawing a second copy.

`SWLOR_Haks/tools/GenerateRobeRgbModels.py` keeps two independent joint trees
inside each generated body root. The wearer's complete canonical skeleton stays
unchanged, including hand, foot, weapon, neck, and cloak attachments. The robe's
authored skeleton and meshes sit beneath a private `rg_model` branch. Garment
joint names and skin bone references are renamed together; mesh data, material
references, bind transforms, and native inverse skin binds are preserved.
Duplicate legacy nodes are removed or renamed only after checking actual binary
bone references and child/animation relationships.

`RobeSkeleton.py` generates shared animation parents for every converted model,
including custom coats and robes with different joint hierarchies. Body and
robe tracks have separate native part IDs. Ordinary body clips retain their
original controllers, duration, and events; garment overlays follow the same
phase without replacing the wearer's tracks. Each distinct garment hierarchy
receives its own private animation path. This avoids inheriting a body's missing
hands through an incomplete robe skeleton or replacing body bind transforms
with the robe's transforms. It applies across the catalog, with no robe-number
exceptions or opt-in conversion list.

The compiler matches part IDs against its immediate parent. The generator
checks the compiled IDs, not only ASCII joint names. Native inherited tracks
are mapped by their original IDs: some stock cloak helpers have different IDs
between animation sources and smaller body roots. The generated root retains
the wearer's animation scale; local source translations are compensated when
moved into an inherited clip so dwarf and halfling animations are not scaled
twice. Tiny rotations are recovered directly from binary quaternions because
the legacy decompiler can round them to identity.

`RobePoseAudit.py` compares the full canonical body subtree and evaluates every
resolved body clip at five phases against the original native animation. It
also verifies each garment's world bind pose and actual weighted-bone inverse
bind bytes. Compiler round-trip validation checks mesh/controller data and
vertex attributes before any generated resource is installed. Run
`python -B -m unittest discover -s SWLOR_Haks/tools -p "TestRobe*.py"` for focused
regressions, and run the complete generator for corpus validation.

`animation_bridges` in `RobeRgbModels.json` preserves stable shared parent names.
The manifest records generator/source/output hashes, pose coverage, and any
missing animation-source fallback. Missing authored parents use the body's
available clips and are explicitly recorded; they never retain the old unsafe
robe-as-body inheritance path. Regeneration removes obsolete manifest-owned
animation parents after successful validation.

The catalog currently covers 1,596 normal-body models across 184 robe styles.
Phenotype IDs are a native byte, so generated IDs are reserved in the range
34–255 and are never recycled. Normal body type 0 covers the current authored
NPC corpus; large and mounted body types retain the native fallback. Reserved
rows are internal rendering metadata, not character-creation choices.

Stock KEY/BIF resources must also be included: scanning HAK files alone omitted
robe3 and 97 other selectable stock model variants. Run
`SWLOR_Haks/tools/ImportStockRobeTints.py --game-data "<NWN>/data" --apply`
before regenerating robe roots. The importer preserves HAK overrides, excludes
invalid `parts_robe.2da` rows, compiles affected ASCII inputs, converts missing
stock PLTs, and binds imported variants that use already-converted masks.
`TintMapStockRobes.json` records all 132 selectable stock model names and native
palette choices; the robe audit requires those models in the RGB catalog.
Rebuild the three `sw_tint*` texture HAKs and `sw_tint_mtr.hak` as well when
importing masks. The male and female robe3 assets use Leather1, Cloth1/2, and
Metal1/2; Leather2 has no pixels in those authored masks.

The NWScript `SetPhenoType` command silently rejects IDs above 99 despite the
native byte fields. The renderer uses the typed native API to write the same
two fields as that command (`m_pStats.m_nPhenoType` and
`m_cAppearance.m_nPhenoType`), then requests replication. It does not call an
equipment or stat rebuild. The engine test exercises a catalog ID above 99
and checks that both fields agree.

The model resolver always uses the original body type and robe resource name
for material selection and saved colors. Removing the robe or the last effective
RGB override restores the base phenotype. Saved generated phenotypes normalize
through the reserved table rows, including when a robe is later retired. Mount
phenotypes are not replaced. The renderer changes no equipped item, item property,
model number, ownership, or equip/unequip event; native engine tests verify this
with real event observers and gameplay-state snapshots.

Regenerate after changing a source robe or base skeleton, and rebuild
`sw_pt_root.hak`, `sw_pt_robe.hak`, and `sw_2da.hak` together. The tint generator
keeps derived roots out of material-scope inference and checks their source/output
hash manifest instead. A full client restart is required after installing models
or phenotype tables. The female human robe187 prototype was verified in the
89.8193.37-17 client: all eight converted material instances received exact Cloth1
RGB (205,228,197), and walking/sitting kept skirt, sleeves, hands, and feet intact.
Only surfaces using the selected layer change color; Cloth1 alone does not recolor
the entire dress.

The full catalog was also checked with the male human player switching between
robes 7 and 187. Both outfits were intact in the supplied client views. After
returning to robe187 (generated phenotype 171), a read-only client inspection
confirmed all ten material instances received exact Leather1 and Leather2 RGB
(205,228,197); the cloth channels retained their separate palette values. This
also verifies client rendering with a generated phenotype above 99. Other styles
still require visual spot checks in the client.

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

### Cover native body-part texture fallbacks

The native segmented-body loader selects a PLT using the exact model name,
then the same gender and race with phenotype zero, then the same gender with
human phenotype zero. Female parts finally fall back to male human phenotype
zero. Converting the PLT removes that implicit lookup: every eligible mesh in
each affected model must explicitly bind the selected tint material, and the
exact model name must appear in `tintmap.2da` so runtime colors reach it.
The native loader formats part indices with `%03u`; an unpadded texture name
does not precede the padded human fallback in that lookup.

For example, the placed male bounty hunter uses `pme0_shinl249`,
`pme0_shinr249`, `pme0_footl247`, and `pme0_footr247`. Their legacy bitmap names
are `shin` and `but`; the native loader selects the corresponding `pmh0_`
PLTs. All four masks use Leather2, whose authored item palette ID is 23. A
canonical human MTR alone cannot replace the missing racial model binding.

Audit the complete active modular-model corpus through the native fallback
chain. A small list of known NPC parts cannot establish coverage. Once a PLT
is selected, the native loader replaces every mesh surface in the body-part
subtree, including meshes with existing bitmap or material names. Match that
behavior and verify the resulting binary mesh bindings as well as catalog
rows. The GPU regression resolves the reported feet and shins through
the catalog and checks their actual packed masks select Leather2 row 23 with
zero environment-map coverage.

PLT replacement changes the underlying diffuse texture, not the existing
SharedMaterial configuration. Preserve the original normal, specular,
emission, transparency, and cutout settings; the fallback PLT's same-name MTR
does not become the new configuration. An authored non-null `texture0` takes
priority over the underlying PLT, so those fixed-diffuse meshes retain their
original material and shader. This also applies to an unresolved texture: the
native renderer binds its missing texture instead of falling back to the PLT.
Resolve original resources using the module's `Mod_HakList`, where the first
entry wins. `hakbuilder.json` controls packaging, not runtime resource priority.

`TintMapMaterialSources.json` records the original MTR declarations, Git
provenance, module priority, and lineage of retired bitmap aliases. Reproduce
it with `CaptureTintMaterialSources.py --check`, supplying the recorded
`hakCommit`, `moduleCommit`, and `convertedCommit` as `--baseline`,
`--module-baseline`, and `--converted-baseline`, plus `--game-data`. Profile
aliases preserve these inputs separately from the selected PLT. Fixed materials
whose names collide with generated tint materials have preserved copies in
`sw_item` and do not appear in the tint catalog.

Use `python SWLOR_Haks/tools/GenerateTintMapAssets.py --refresh-model-bindings`
to regenerate these bindings and their catalog without recompressing textures.
Compile any changed ASCII models before packaging. Rebuild every changed model
HAK together with `sw_2da.hak`, `sw_tint_mtr.hak`, and any changed preserved
material HAK; the server must reload the catalog and the client must reload the
model resources. ASCII models with duplicate node names must be compiled before
binding, so each mesh can be addressed by its unique binary material offset.

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

The legacy `pmo0_forel129` source also had an empty dangly `period` value.
Its male human, dwarf, and elf counterparts agree on period 10 with the same
displacement and tightness. Compile it with the explicit, audited repair
`--dangly-period pmo0_forel129:pmo0_forel129g:10`; this fills the missing value
and must never overwrite a supplied value or become a general default.

Keep restored geometry and its palette mask paired. The base male hand models
`pmh0_handl001` and `pmh0_handr001` use master geometry and require master's
256-by-256 masks; retaining the earlier stock 64-by-64 masks changes their
shading. The existing base male feet still use their matched stock models and
masks. The material-profile tests protect both pairings.

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
