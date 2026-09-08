# World texture DDS packaging

The HAK submodule's `tools/WorldTextureDdsConversions.json` records the reviewed
world textures shipped as standard DDS, their original TGA hashes, output hashes,
dimensions, full mip chains, and decoded color/orientation signatures. Its
`WorldTextureDds.md` records the measured size reduction and validation results.
The original lossless TGAs remain recoverable from the source commit named in
that manifest; they are not duplicated in the shipped HAK directories.

## Scope

This is an explicit reviewed batch of world textures at least 128x128, not a blanket TGA conversion rule. Inventory,
gameplay, minimap, loading-screen and UI images, exact-color palettes, emitter
textures, material maps, metadata-sensitive textures, ambiguous resource
collisions, unsuitable dimensions, and outputs that would grow were held back.
Model and installed-game resource references were reviewed in addition to the
loose-file metadata checks.

`SelectWorldTextureDdsCandidates.py` requires an audited allowlist. It can narrow
that list using current HAK metadata but cannot certify arbitrary textures as
safe. In particular, do not replace model-role review with a filename heuristic.

## Validate the shipped batch

The Python tools require Pillow and ImageMagick for encoding. Validation needs
Pillow and the populated HAK submodule, but not the original TGA files.

```powershell
python -m unittest discover -s tools/tests -p "test_world_texture*.py"
python tools/ConvertWorldTexturesToDds.py validate --manifest SWLOR_Haks/tools/WorldTextureDdsConversions.json --asset-root SWLOR_Haks
dotnet build SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build --filter "FullyQualifiedName~WorldTextureDdsCorpusTests|FullyQualifiedName~TextureOrientation|FullyQualifiedName~TextureChannelOrder|FullyQualifiedName~TextureAlphaPolicy"
```

The corpus test resolves every converted texture through the actual toolset
loader, detecting remaining TGA overrides, failed DDS decoding, changed dimensions,
flipped images and swapped color channels. Small BC interpolation rounding
differences between Pillow and Pfim are allowed. Hash and payload checks live in
the Python validator; compression quality still requires reviewing the images.

## Reproduce or extend a reviewed batch

Restore the original source commit in a separate checkout before using the
source manifest. The current shipped checkout intentionally has DDS replacements
instead of those TGAs. Run from the parent repository, using new staging and report
paths:

```powershell
python tools/ConvertWorldTexturesToDds.py stage --manifest SWLOR_Haks/tools/WorldTextureDdsSources.json --source-root C:/path/to/original-haks --output-root artifacts/new-world-dds --report artifacts/new-world-dds.json
python tools/ConvertWorldTexturesToDds.py validate --manifest artifacts/new-world-dds.json --asset-root artifacts/new-world-dds
```

For a newly reviewed allowlist, generate fresh source hashes and eligibility
checks first:

```powershell
python tools/SelectWorldTextureDdsCandidates.py --source-root C:/path/to/source-haks --config Build/hakbuilder.json --allowlist reviewed-paths.json --output artifacts/new-world-sources.json
```

The converter normalizes TGA origin flags, reverses the displayed rows for NWN's
standard DDS convention, selects BC1 for opaque pixels or BC3 for any nonopaque
alpha, enables cluster-fit compression, and writes every mip level down to 1x1. It does not flatten transparency,
resize the base level, change material metadata, or install assets automatically.
Staging requires new paths, verifies pinned source bytes and uses exclusive writes.
A failed stage must not be installed: it has no completed conversion report.

Review the source/output image metrics and representative images, validate the
entire stage, then replace only the pinned TGAs and update the manifests. Preserve
lossless masters in accessible source history. Rebuild every affected HAK and
verify each packed resource against its source. The parent submodule-pointer PR
and its companion HAK PR must be linked and target corresponding base branches.
