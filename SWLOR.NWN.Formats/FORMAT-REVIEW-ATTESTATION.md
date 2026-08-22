# Standalone formats independent-review attestation

Date: 2026-07-26

Role: independent format-library reviewer for the standalone `SWLOR.NWN.Formats` replacement
described by `SWLOR.Toolset/RADOUB-REPLACEMENT-PLAN.md`.

## Clean-room boundary and exposure declaration

All review work was performed in the sanitized snapshot
`C:\tmp\SWLOR_NWN-radoub-clean-author`.

- I did not open, read, search, list, diff, or otherwise access any path outside that snapshot.
- I did not access `External/Radoub`, Radoub source, third-party implementation source, any
  `.git` directory or object, branch, commit, diff, or history.
- I did not access historical or candidate copies of the five replacement render files in another
  workspace.
- I did not access proprietary decoded model, texture, animation, Module, or hak payloads.
- The repository-wide `grill-me` skill source was outside the permitted snapshot and was not
  accessed. The replacement plan and assigned review brief supplied the review contract.
- `tools/VerifyRadoubCleanRoom.ps1 -Role Formats` passed during the initial full review.
  `tools/VerifyRadoubCleanRoom.ps1 -Role Integration` executed 1 check with 0 failures during the
  final post-remediation review.

## Review scope

I independently reviewed the complete standalone library and portable tests:

- project/reference separation, public API signatures, resource-type mappings, SPDX headers, and
  the one-public-type-per-file rule;
- text `2DA V2.0` and binary `2DA V2.b`;
- `TLK V3.0`;
- `KEY V1` and `BIF V1`;
- TGA and Aurora PLT image resources;
- `GFF V3.2`; and
- binary and ASCII Aurora MDL, including model/MDX sections, nodes, meshes, skins, emitters,
  animation/controller data, transforms, `tilefade`, malformed inputs, graph bounds, and
  allocation limits.

I also reviewed the first-party render integration affected by these readers, including standard
DDS orientation, and the focused portable regression fixtures.

## Findings disposition

The initial full review returned findings for text-2DA cell expansion, binary-2DA columns, aliased
or oversized allocations, binary-MDL section and flag validation, signed `tilefade`, and TGA
interleaving. The clean author remediated them, and final review verified:

1. Text and binary 2DA shapes are overflow-safe and bounded to 32,000,000 logical cells; binary
   columns are non-empty and case-insensitively unique before dictionary construction.
2. Binary MDL contains its fixed header within model data, rejects incompatible node flags as
   `NwnFormatException`, preserves signed `tilefade`, and applies one overflow-safe 64 MiB
   cumulative allocation budget across nodes, faces, MDX arrays, skins, and controllers.
3. GFF caches aliased fields while charging their logical payload expansion against the same
   conservative budget.
4. KEY validates and charges every filename reference before decoding, caches identical filename
   ranges, and charges 128 bytes per resource entry before allocating its list slot, object, or
   independent ResRef. The 524,289-entry regression proves the budget rejects over-limit metadata
   before entry allocations.
5. BIF metadata is cumulatively bounded and every resource is capped at 256 MiB before extraction.
6. TGA rejects unsupported two-way, four-way, and reserved interleaving descriptors.
7. The allocation-budget helper rejects negative sizes and catches multiplication overflow as
   `NwnFormatException`; caller charges are made before the corresponding untrusted allocations.

No unresolved correctness, safety, API-separation, malformed-input, allocation, provenance, SPDX,
or public-type-layout finding remains.

## DDS consumer-contract disposition

Pfim exposes positive-stride standard-DDS rows in file order. The toolset deliberately reverses
those rows for the NWN consumer contract; this is not a claim that generic DDS block order is
bottom-up.

The approved semantic evidence isolates this choice: with the same clean `MdlMeshBuilder`, the
row-reversing loader passes `ADdsTexturedModelSamplesTheArtistsSideOfItsTexture`; changing only the
loader to no reversal fails at dark-area ratio `0.150315`, above the test's `0.08` limit. Under the
replacement plan's evidence hierarchy, that controlled licensed artist-orientation result governs
the NWN/toolset integration. I reviewed the supplied result but did not access its licensed model
or texture bytes.

## Independent verification

Required standalone formats build:

```text
dotnet build SWLOR.NWN.Formats.Tests\SWLOR.NWN.Formats.Tests.csproj -p:RunPostBuildEvent=Never
```

- succeeded: yes
- warnings: 0
- errors: 0

Complete portable formats suite:

- requested/executed: 41/41
- passed: 41
- failed: 0
- skipped: 0

Required toolset test-project build:

```text
dotnet build SWLOR.Toolset.Tests\SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
```

- succeeded: yes
- warnings: 0
- errors: 0

Portable/synthetic renderer selection:

- requested/executed: 105/105
- passed: 105
- failed: 0
- skipped: 0

Static audits:

- standalone format-library source files with MIT SPDX headers: 40/40
- public-type/file-layout failures: 0
- five clean render replacements plus portable replacement fixture with MIT SPDX headers: 6/6

Licensed corpus verification was not rerun because `Module` and `SWLOR_Haks` were intentionally
absent from the sanitized snapshot. The previously recorded licensed counts and hashes remain the
corpus evidence for the designated integration environment.

## Approval

I approve `SWLOR.NWN.Formats`, its portable regression suite, and its recorded clean-room
provenance for the independent format-library review gate. Publication and owner/legal
license-notice direction remain separate gates.

<!-- SPDX-License-Identifier: MIT -->
