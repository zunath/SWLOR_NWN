<!-- SPDX-License-Identifier: MIT -->

# Render clean-author attestation

Date: 2026-07-26

Role: independent clean implementation author for Task 5 of
`SWLOR.Toolset/RADOUB-REPLACEMENT-PLAN.md`.

## Clean-room boundary and exposure declaration

All work was performed in the sanitized snapshot
`C:\tmp\SWLOR_NWN-radoub-clean-author`.

- Before reading implementation material, I ran
  `powershell -ExecutionPolicy Bypass -File tools/VerifyRadoubCleanRoom.ps1 -Role Render`.
  It reported 6 checks passed and 0 failures.
- I did not open, read, search, list, diff, or otherwise access filesystem content outside that
  sanitized snapshot during this task. A build-lock diagnostic displayed the executable paths of
  running .NET/MSBuild processes; no file at those toolchain paths was opened or read as source
  material.
- I did not access any `External/Radoub` path, Radoub source, third-party implementation source,
  `.git` directory, git object, branch, commit, diff, or history.
- I did not access a historical or candidate copy of any of the five replacement files in another
  workspace. The five paths were absent from the snapshot when the clean-room verifier ran.
- I was not exposed to the historical contents of the five files during this task.
- An initial recursive filename inventory was run inside the sanitized snapshot after verification.
  It exposed only paths in the already-sanitized snapshot and no denied file content. Subsequent
  searches were scoped to named first-party directories. This is disclosed because the execution
  plan prefers every clean-task repository search to be directory-scoped.
- No proprietary decoded model, texture, animation, Module, or hak payload was added.

The repository-wide `grill-me` skill source was outside the allowed snapshot and was not accessed.
The assigned clean-room brief supplied a complete implementation contract, so no external
interview material was used.

## Allowed sources used

Governing and provenance material:

- `AGENTS.md`
- `SWLOR.Toolset/RADOUB-REPLACEMENT-PLAN.md`
- `SWLOR.Toolset/RADOUB-REPLACEMENT-CODEX-BRIEF.md`
- `SWLOR.NWN.Formats/FORMAT-PROVENANCE.md`
- `SWLOR.Toolset.Domain/Render/REPLACEMENT-PROVENANCE.md`
- `tools/VerifyRadoubCleanRoom.ps1`

Specification facts used were those approved and identified by those provenance records:

- BioWare Aurora binary/ASCII MDL transform and node-tree contracts
- Microsoft `System.Numerics` row-vector matrix and quaternion contracts
- Microsoft DDS header conventions and S3TC/DXT1/DXT5 block layouts
- Truevision TGA 2.0 top-left canonicalization
- Aurora PLT V1 layer/intensity layout and ten-layer palette semantics

First-party contract material used:

- `SWLOR.NWN.Formats/Mdl/*`, `SWLOR.NWN.Formats/Tga/*`, and
  `SWLOR.NWN.Formats/Plt/*`
- Render callers and model consumers in `SWLOR.Toolset.Domain` and `SWLOR.Toolset`
- Render, texture, animation, geometry, composition, picking, and thumbnail tests in
  `SWLOR.Toolset.Tests`
- Portable reader fixtures in `SWLOR.NWN.Formats.Tests`
- The Pfim package declaration in `SWLOR.Toolset.Domain/SWLOR.Toolset.Domain.csproj`
- Consumer/corpus outcomes already summarized in the two provenance records; no licensed corpus
  bytes were present or inspected in this snapshot

## Files independently authored

- `SWLOR.Toolset.Domain/Render/MdlPartComposer.cs`
- `SWLOR.Toolset.Domain/Render/MdlPartBoneMap.cs`
- `SWLOR.Toolset.Domain/Render/TextureLoader.cs`
- `SWLOR.Toolset.Domain/Render/MdlMeshBuilder.cs`
- `SWLOR.Toolset.Domain/Render/MdlGeometryFlattener.cs`

Each authored implementation file begins with `// SPDX-License-Identifier: MIT`.

A focused portable contract suite was also added:

- `SWLOR.Toolset.Tests/RenderReplacementPortableTests.cs`

Independent review identified additional format-validation, allocation, and standard-DDS
orientation issues. The clean remediation also modified:

- `SWLOR.NWN.Formats/Internal/AllocationBudget.cs`
- `SWLOR.NWN.Formats/TwoDA/TwoDAReader.cs`
- `SWLOR.NWN.Formats/Gff/GffReader.cs`
- `SWLOR.NWN.Formats/Key/KeyReader.cs`
- `SWLOR.NWN.Formats/Bif/BifReader.cs`
- `SWLOR.NWN.Formats/Bif/BifFile.cs`
- `SWLOR.NWN.Formats/Mdl/MdlReader.cs`
- `SWLOR.NWN.Formats/Tga/TgaReader.cs`
- `SWLOR.NWN.Formats.Tests/TwoDAReaderTests.cs`
- `SWLOR.NWN.Formats.Tests/GffReaderTests.cs`
- `SWLOR.NWN.Formats.Tests/KeyBifReaderTests.cs`
- `SWLOR.NWN.Formats.Tests/MdlReaderTests.cs`
- `SWLOR.NWN.Formats.Tests/TgaReaderTests.cs`

## Independently derived design

- Node transforms use `scale * rotation * translation`, accumulated child-to-parent for
  `System.Numerics` row vectors. Flattening bakes that full chain into positions and normals,
  resets visited node transforms, and accumulates bounds and the exact farthest-vertex radius
  inside that same visited, node-limited traversal. It never re-enters the mutable graph through
  an unbounded mesh enumerator.
- Segmented parts are deep-cloned, attached beneath canonical skeleton bones selected by part
  category, have parent links repaired for coherent seam transforms, retain the skeleton's
  animation tree, and use the part resref texture convention. Parts pass through the caller on
  each composition so the existing authored-texture recovery hook can record custom texture names.
- Render meshes preserve valid vertices, normals, UVs, faces, signed `tilefade`, vertex count,
  triangle count, idle pose frames, placeable animation frames, persistent emitter metadata, and a
  bounded default-state policy. Known placeholder nodes are omitted by name without conflating
  every untextured mesh with a placeholder.
- TGA uses the standalone reader's top-left RGBA output. The reader decodes directly into one
  bounded contiguous RGBA surface (plus a bounded value-type palette when needed), avoiding a
  per-pixel managed object and a second full image allocation. PLT applies caller-selected palette
  rows and uses a bounded grayscale fallback when palette resources are unavailable.
- Standard DDS validates its fixed header, dimensions, pixel count, and RGBA allocation cap before
  Pfim is invoked, then normalizes Pfim's blue-first rows to the toolset's consumer-facing RGBA
  convention. Pfim exposes positive-stride rows in file order; NWN artwork was authored for the
  engine's bottom-up UV convention, so those rows are reversed. This is an NWN/toolset semantic
  choice, not a claim that generic DDS block order is bottom-up. A two-block-row portable fixture
  makes an accidental no-flip implementation observable.
  Compact BioWare DDS validates five 4-byte fields (width, height, channels, linear size, and
  alpha mean), begins compressed data at byte 20, and decodes DXT1 for three channels or DXT5 for
  four channels. Dimensions, sizes, blocks, node counts, hierarchy depth, and cycles are bounded.
- Composition bounds use the exact farthest transformed vertex for radius rather than combining
  independently selected per-axis bounds extrema.
- Text 2DA shape validation enforces the 32,000,000-cell ceiling before row materialization, and
  binary 2DA rejects empty or duplicate columns as `NwnFormatException`. Binary MDL requires its
  complete 232-byte header within declared model data, validates incompatible node flags before
  typed casts, preserves high-bit `tilefade` as a signed integer, and applies a cumulative
  allocation budget to nodes and tables.
- KEY, BIF, GFF, and binary MDL use a conservative 64 MiB per-parse allocation budget. KEY
  filename aliases are charged before decoding, and every KEY resource is charged at 128 bytes for
  its list slot, object, and independent UTF-16 ResRef string; GFF repeated field aliases reuse
  decoded values while charging their logical expansion; BIF metadata is bounded and each lazy
  resource extraction is capped at 256 MiB. TGA rejects unsupported interleaving descriptor bits.

## Verification record

Initial clean-room check:

- requested/executed checks: 6/6
- failed: 0

Required toolset test-project build:

```text
dotnet build SWLOR.Toolset.Tests\SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
```

- succeeded: yes
- errors: 0
- warnings: 2 unrelated nullable warnings in `EditorService.cs` and `ScriptBinderTests.cs`

Selected portable/synthetic render suite:

- requested/executed: 105/105
- passed: 105
- failed: 0
- skipped: 0

The selection covered the new replacement contract tests plus portable animation sampling,
geometry flattening, mesh placeholders and metadata, placeable preview animation/emitter
metadata, idle frames, thumbnail rendering, texture orientation/alpha policy, robe transforms,
area picking, and render-model batching. Review-remediation coverage includes cyclic mutable
geometry, mixed-axis radius extrema for flattening and composition, and oversized standard DDS
headers rejected before Pfim surface allocation. It also includes a nonuniform two-row-block
standard DDS fixture that proves positive-stride rows are reversed for the NWN UV contract.

Controlled licensed integration evidence supplied by the independent integration run isolates
the orientation choice: the clean `MdlMeshBuilder` with positive-stride row reversal passes
`ADdsTexturedModelSamplesTheArtistsSideOfItsTexture`; the same clean mesh with no reversal fails
at dark-area ratio `0.150315`, above the test's `0.08` limit. No licensed texture or model bytes
were present in or accessed from this sanitized snapshot.

Standalone format-reader build:

```text
dotnet build SWLOR.NWN.Formats.Tests\SWLOR.NWN.Formats.Tests.csproj -p:RunPostBuildEvent=Never
```

- succeeded: yes
- errors: 0
- warnings: 0

Full standalone portable format-reader suite:

- requested/executed: 41/41
- passed: 41
- failed: 0
- skipped: 0

The format selection includes all synthetic 2DA, GFF, KEY/BIF, MDL, PLT, resource-type, TGA, and
TLK tests. The new cases cover cell-count overflow, invalid column dictionaries, aliased payload
expansion, metadata/resource ceilings, MDL section/flag/signed-value handling, cumulative shared
face-table allocation, TGA interleaving, contiguous output, and surface bounds.

Licensed corpus verification was not run in this clean snapshot because `Module` and
`SWLOR_Haks` were intentionally absent. No portable test selected for this attestation depended
on either directory, and the selected portable sets had zero skips.

## Remaining review gate

An independent reviewer still needs to verify these additional remediations and run the licensed geometry,
texture, facing, idle/playback, robe, and real skeleton/part corpus gates in the designated
environment. No commit, push, dependency cutover, license-notice change, or owner/legal signoff
was performed by this clean-author task.
