# Render replacement provenance

## Scope

This record covers the five render files replaced as part of the external format dependency
removal:

- `MdlPartComposer.cs`
- `MdlPartBoneMap.cs`
- `TextureLoader.cs`
- `MdlMeshBuilder.cs`
- `MdlGeometryFlattener.cs`

## Evidence permitted for clean authors

- BioWare Aurora MDL and ASCII MDL format specifications.
- Microsoft `System.Numerics` matrix and quaternion contracts.
- Microsoft DDS documentation and the S3TC/DXT1/DXT5 block layouts.
- Truevision TGA 2.0 and the Aurora PLT layout recorded in
  `SWLOR.NWN.Formats/FORMAT-PROVENANCE.md`.
- First-party callers and tests outside the five replacement files.
- Lawfully available NWN:EE and `SWLOR_Haks` corpus bytes.
- Semantic results from the portable and licensed-corpus tests.

The denied sources and history are listed in `SWLOR.Toolset/RADOUB-REPLACEMENT-PLAN.md`.

## Exposure declaration

- Integration author: OpenAI Codex.
- `External/Radoub/` exposure: none.
- Historical replacement-file exposure: an incorrectly scoped first-party audit displayed
  isolated matching lines from all five old files. The integration author is therefore not
  eligible to make the plan's clean implementation declaration for these files.
- Independent clean author: OpenAI Codex, completed 2026-07-26; see
  `CLEAN-AUTHOR-ATTESTATION.md`.
- Independent reviewer: OpenAI Codex, final approval completed 2026-07-26 after the earlier
  checkpoint and later full-format remediation; see `CLEAN-REVIEW-ATTESTATION.md` and
  `SWLOR.NWN.Formats/FORMAT-REVIEW-ATTESTATION.md`.

The clean author independently re-derived the five files without candidate or denied-history
exposure, and each replacement now carries the repository's MIT SPDX header. Final independent
review is approved. On 2026-07-26, the repository owner expressly directed completion of the
removal ("I want Radoub gone" and "Of course you need to implement it"), which approves the
dependency cutover, replacement headers, and current third-party notice for this change. This
engineering record does not claim review by legal counsel or offer a legal opinion.

## Consumer contracts and verification

- Model composition loads a skeleton and body-part models through a caller-supplied loader,
  attaches each part to the skeleton bone selected by its part category, and retains authored
  texture identities.
- Geometry flattening composes node-local scale, quaternion rotation, and translation through the
  parent chain, transforms positions and normals, then resets baked transforms. Bounds and exact
  farthest-vertex radius are accumulated inside the same visited, node-limited traversal, including
  on cyclic mutable graphs.
- Mesh building emits render vertices, normals, UVs, faces, texture identities, emitters, and
  animation metadata for model and placeable-preview callers.
- Texture loading returns top-left row-major RGBA for TGA, DDS, and PLT. Standard DDS headers and
  allocation bounds are validated before Pfim decoding. Pfim exposes positive-stride DDS rows in
  file order, but NWN artwork was authored for the engine's bottom-up UV convention. The toolset
  therefore reverses those rows for its consumer-facing orientation; this is an NWN semantic
  choice rather than a generic claim about DDS block order. A nonuniform two-block-row fixture
  guards the consumer contract. Compact BioWare DDS blocks are decoded as
  DXT1/DXT5 directly after validating the 20-byte header:
  width, height, channel count, linear size, and an alpha-mean float. Compressed blocks begin
  after the alpha-mean field; treating that field as the first DXT block was rejected by both
  the documented layout and the licensed solid-surface alpha corpus tests.
- TGA decoding writes directly into one bounded contiguous RGBA surface rather than allocating a
  per-pixel managed array and a second full output surface, and unsupported descriptor
  interleaving modes are rejected.

Verification recorded on 2026-07-26:

- Portable render/animation/scaffolding tests: 23 requested, 23 executed, 0 failed, 0 skipped.
- Licensed geometry, texture, facing, idle-pose, playback, and real skeleton/part composition
  tests: 46 requested, 46 executed, 0 failed, 0 skipped after correcting binary quaternion order
  and DDS vertical orientation.
- Required MDL licensed corpus scope scan (signature classification by four-byte prefix, binary
  vs. ASCII — not a parse): 96,005 requested/executed, 0 failed, 0 skipped. Parse coverage at the
  time was ASCII-complete (16,078 files) plus a 600-file evenly distributed base-game binary
  sample; a full parse sweep of every binary MDL under SWLOR_Haks is now covered separately by
  `HakMdlParseSweepTests` in `SWLOR.NWN.Formats.Corpus.Tests`.

Clean-author verification after independent-review remediation on 2026-07-26:

- Required toolset test-project build: succeeded with 0 errors and 2 unrelated nullable warnings.
- Portable/synthetic render selection: 105 requested/executed, 0 failed, 0 skipped.
- Full standalone portable formats selection: 41 requested/executed, 0 failed, 0 skipped; its
  test-project build succeeded with 0 warnings and 0 errors.
- Focused regressions cover cyclic flattener termination, exact mixed-axis radius for flattening
  and composition, oversized standard DDS rejection before Pfim allocation, contiguous
  uncompressed TGA output, mixed raw/RLE packets, TGA allocation/pixel/interleaving bounds,
  2DA shape and column validation, GFF/KEY alias expansion, conservative 128-byte KEY
  resource-object/ResRef accounting, BIF metadata/resource ceilings, and MDL
  section/flag/signed-value/cumulative-allocation handling.
- Licensed corpus verification was not rerun in the sanitized snapshot because `Module` and
  `SWLOR_Haks` are intentionally absent.

Controlled integration evidence supplied to the clean author resolves the standard-DDS ambiguity:
the clean `MdlMeshBuilder` with positive-stride row reversal passes
`ADdsTexturedModelSamplesTheArtistsSideOfItsTexture`; holding that mesh implementation constant
and removing the reversal fails at dark-area ratio `0.150315`, above the `0.08` limit. The semantic
corpus result therefore governs the NWN/toolset orientation even though Pfim exposes file-order
rows. The clean author did not access the licensed model or texture bytes.

Final independent-review verification after all remediation on 2026-07-26:

- Clean-room integration check: 1 requested/executed, 0 failed.
- Standalone formats build: succeeded with 0 warnings and 0 errors.
- Complete portable formats suite: 41 requested/executed, 0 failed, 0 skipped.
- Required toolset test-project build: succeeded with 0 warnings and 0 errors.
- Portable/synthetic render selection: 105 requested/executed, 0 failed, 0 skipped.
- Standalone format-library MIT SPDX headers: 40/40; public-type/file-layout failures: 0.
- Replacement and portable-fixture MIT SPDX headers: 6/6.
- The reviewer approved the formats and render replacement gates; see the two independent-review
  attestations named above.

Earlier independent-review checkpoint on 2026-07-26:

- Clean-room integration check: 1 requested/executed, 0 failed.
- Required toolset test-project build: succeeded with 0 warnings and 0 errors.
- Portable/synthetic render selection: 104 requested/executed, 0 failed, 0 skipped.
- Standalone MDL/TGA/PLT selection: 12 requested/executed, 0 failed, 0 skipped.
- Toolset scaffolding/license selection: 6 requested/executed, 0 failed, 0 skipped.
- At that checkpoint the reviewer confirmed four prior findings were remediated: bounded cyclic flattening,
  pre-Pfim DDS dimension/allocation validation, exact farthest-vertex radii, and contiguous TGA
  decoding. A later full formats review found the additional issues recorded in the clean-author
  verification above; the earlier checkpoint is therefore not final approval.
- Licensed corpus bytes were intentionally unavailable and were not accessed or rerun; the
  existing recorded licensed counts remain the corpus evidence for the later integration gate.

No proprietary decoded model, texture, or animation payload is committed by this record.
