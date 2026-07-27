# Render clean-review attestation

Date: 2026-07-26

Role: independent reviewer for Task 5 of
`SWLOR.Toolset/RADOUB-REPLACEMENT-PLAN.md`.

## Clean-room boundary and exposure declaration

All review work was performed in the sanitized snapshot
`C:\tmp\SWLOR_NWN-radoub-clean-author`.

- I did not open, read, search, list, diff, or otherwise access any path outside that snapshot.
- I did not access `External/Radoub`, Radoub source, third-party implementation source, any
  `.git` directory or object, branch, commit, diff, or history.
- I did not access historical or candidate copies of the five replacement files in another
  workspace. I reviewed only the current clean-authored replacements in this snapshot.
- I did not access proprietary decoded model, texture, animation, Module, or hak payloads.
- The repository-wide `grill-me` skill source was outside the permitted snapshot and was not
  accessed. The replacement plan and assigned review brief supplied the review contract.
- `tools/VerifyRadoubCleanRoom.ps1 -Role Integration` executed 1 check with 0 failures. The
  Render-role verifier is the pre-author absence gate and is expected to reject a post-author
  snapshot in which the five replacement paths now exist.

## Review scope

I read and reviewed:

- `AGENTS.md`, the complete replacement plan and execution brief;
- `SWLOR.NWN.Formats/FORMAT-PROVENANCE.md`;
- `REPLACEMENT-PROVENANCE.md` and `CLEAN-AUTHOR-ATTESTATION.md`;
- all five clean-authored render replacements and `RenderReplacementPortableTests.cs`;
- the standalone MDL, TGA, and PLT readers, tests, and relevant model types; and
- the first-party render callers and focused geometry, composition, animation, emitter, texture,
  picking, thumbnail, scaffolding, and license tests.

The review covered transform order and graph bounds; part-to-bone mapping and composition;
vertices, faces, indices, normals, UVs, `tilefade`, and triangle counts; animation and persistent
placeable-emitter metadata; TGA, PLT, standard DDS, and compact 20-byte BioWare DDS behavior;
consumer-facing RGBA orientation; every standalone format family; malformed input; and cumulative
allocation limits.

All five replacement files and the added portable replacement test begin with
`// SPDX-License-Identifier: MIT`.

## Findings disposition

The initial review returned four findings. The clean author remediated each one, and I independently
verified the implementation and focused regression:

1. Geometry flattening now accumulates bounds inside its existing visited, node-limited traversal,
   so cyclic mutable graphs terminate without re-entering an unbounded enumerator.
2. Standard DDS validates its fixed header, dimensions, pixel limit, and output allocation before
   invoking Pfim.
3. Flattening and part composition calculate radius from the exact farthest transformed vertex,
   including mixed-axis extrema.
4. TGA decoding writes directly into one bounded RGBA surface and uses value-type palette entries,
   eliminating the per-pixel managed-object allocation.

A later full standalone-formats review returned additional validation and allocation findings.
Final review verified their remediation: text/binary 2DA shape and column checks; binary MDL
section, node-flag, signed-value, and 64 MiB cumulative-allocation handling; GFF field caching with
logical alias charges; KEY filename caching plus conservative 128-byte resource accounting; BIF
metadata and 256 MiB extraction ceilings; and TGA interleaving rejection.

The standard-DDS orientation question was resolved under the plan's semantic evidence hierarchy.
Pfim exposes positive-stride rows in file order, but the toolset reverses them for the NWN UV
consumer contract. With the clean `MdlMeshBuilder` held constant, the reversing loader passes
`ADdsTexturedModelSamplesTheArtistsSideOfItsTexture`; changing only the loader to no reversal
fails at dark-area ratio `0.150315`, above the test's `0.08` limit. I accepted that controlled
integration result without accessing licensed bytes in this snapshot.

No unresolved correctness, safety, API-completeness, clean-room provenance, malformed-input,
allocation, public-type-layout, or MIT-header finding remained after remediation.

## Independent verification

Required build:

```text
dotnet build SWLOR.Toolset.Tests\SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
```

- succeeded: yes
- warnings: 0
- errors: 0

Portable/synthetic render selection after all remediation:

- requested/executed: 105/105
- passed: 105
- failed: 0
- skipped: 0

Complete standalone portable formats suite:

- requested/executed: 41/41
- passed: 41
- failed: 0
- skipped: 0

Standalone formats build:

- succeeded: yes
- warnings: 0
- errors: 0

Static audits:

- standalone format-library MIT SPDX headers: 40/40
- public-type/file-layout failures: 0
- replacement/portable-fixture MIT SPDX headers: 6/6

Licensed corpus verification was not rerun because `Module` and `SWLOR_Haks` were intentionally
absent from the sanitized snapshot. I did not weaken, skip, or redirect any selected portable
test. The previously recorded licensed geometry/texture/composition results, the 96,005-resource
MDL signature-classification scan (binary vs. ASCII by four-byte signature, not a parse), and the
accompanying ASCII-complete and 600-file binary-sample MDL parse results remain the corpus
evidence for the designated integration environment.

## Approval

I approve the five clean-authored render replacements, the standalone formats integration, and
their recorded clean-room provenance for the final Task 5 independent-review gate. Final
dependency cutover, publication, and owner/legal license-notice direction remain separate
integration gates.

<!-- SPDX-License-Identifier: MIT -->
