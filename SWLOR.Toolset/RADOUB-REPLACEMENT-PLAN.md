# Plan — Removing Radoub from the SWLOR Toolset

## Status and scope

This is the reviewed execution plan for removing the `Radoub.Formats` dependency from the
`pr/2124` toolset branch and replacing the consumed format support with independently authored,
first-party code.

The plan is intentionally narrower than "port Radoub":

- implement only the file-format behavior that first-party callers consume;
- remove dead behavior instead of recreating it;
- preserve toolset behavior with specification tests and corpus verification;
- keep proprietary game data out of the repository; and
- make the independence claim auditable rather than relying on a commit message.

This is an engineering plan, not a legal opinion. The GNU project describes dynamically linked
modules that exchange function calls and data structures as a combined program, and GPLv3 permits
binary distribution when its corresponding-source requirements are met. The project goal is
therefore to remove GPL distribution obligations from new toolset binaries, not to claim that
binary distribution is currently impossible. The owner should approve the final license notice
and header removal, with counsel if legal certainty is required.

References:

- [GNU GPL FAQ](https://www.gnu.org/licenses/gpl-faq.en.html)
- [GNU GPL version 3](https://www.gnu.org/licenses/gpl-3.0.html)
- [BioWare Aurora file-format specification archive](https://neverwintervault.org/project/nwn1/other/bioware-aurora-engine-file-format-specifications)

## Reviewed facts on `pr/2124`

The following findings were checked against commit
`23661dd5c661b3e15fefc654f071ccaa27b099dd` without inspecting `External/Radoub`:

- `SWLOR.Toolset.Domain` directly references `Radoub.Formats`.
- The dead `SwlorGameDataService` adapter has one construction site in
  `BlueprintPreviewRenderer`.
- `GffJsonBridge.ToGffFile` has test callers but no production caller.
- `RoundTripCorpusTests` validates JSON parse/write fidelity only. It does not invoke a binary GFF
  writer and cannot be cited as proof of binary GFF output compatibility.
- The requested Module GFF folders contain 15,360 JSON files totaling 818,867,273 bytes. Committing
  a second full JSON rendering of that corpus would be wasteful and would make reviews
  impractical.
- The existing tests use mixed corpus behavior: some fail when the corpus is absent and many call
  `Assert.Ignore`. A raw pass/fail total is therefore not enough to prove that coverage ran.
- `ScaffoldingTests.RadoubFormats_IsReferencedAndLoadable` is another cutover dependency and must
  be removed or replaced.
- Radoub references occur in production comments, tests, project files, and historical toolset
  documents in addition to the files named by the original cutover checklist. Final scanning must
  distinguish executable dependency references from historical attribution.

Two format corrections are mandatory:

- text 2DA is `2DA V2.0`; binary 2DA is `2DA V2.b`;
- the BIF variable-resource table is the normal KEY-indexed resource path used by the current
  catalog. Fixed-resource entries are the optional/out-of-scope table unless the corpus proves
  otherwise.

## Goal

The work is complete only when:

1. a clean checkout can build and test the toolset without initializing or possessing
   `External/Radoub`;
2. no built first-party assembly or `.deps.json` references a Radoub assembly;
3. no first-party production source imports a `Radoub` namespace;
4. the five currently GPL-derived render files have been independently reimplemented and their
   provenance has been approved;
5. replacement-specific portable verification has zero skipped cases, and the full toolset suite
   has no new skips relative to its recorded baseline;
6. the licensed full-corpus verification reports its exact executed sample counts and is green;
7. the dependency, submodule, and packaging changes are complete; and
8. the owner has approved the new license notice before the toolset is described as MIT-only.

## Non-goals

- Recreating unused Radoub blueprint wrappers, ERF editing, search/rename services, settings,
  tokens, SSF, or ITP-specific services.
- Adding a binary GFF writer solely because the old dependency has one. There is no production
  caller today. Binary writing should be a future feature with its own requirements unless a
  concrete caller is approved before Task 3 starts.
- Implementing ASCII MDL without corpus evidence that the toolset needs it.
- Committing decoded base-game strings, models, textures, or other proprietary game data.
- Refactoring unrelated toolset or game-server behavior.

## Independence protocol

### Roles

Every pull request records one of these roles in its provenance note:

1. **Legacy/oracle role.** May build the current Radoub-linked toolset, edit existing integration
   code, and capture black-box compatibility results. It must not inspect `External/Radoub`.
   Anyone in this role is ineligible to author the clean replacements for behavior they observed
   in implementation-derived first-party files.
2. **Clean implementation role.** Authors `SWLOR.NWN.Formats` or a replacement for one of the five
   derived render files from approved specifications, corpus bytes, and consumer contracts only.
   It must not read `External/Radoub`, the five old derived implementations, their git history, or
   implementation notes that quote them.
3. **Integration/review role.** Swaps wrappers and validates end-to-end behavior. It may inspect
   first-party integration code, but its review does not substitute for the clean author's
   provenance declaration.

Eligibility is based on source exposure, not task number. A clean MDL implementer is not
automatically disqualified from independently implementing transform behavior; an integrator who
read the old transform implementation is.

### Source denylist

No task may read, search, list, or inspect any content below:

```text
External/Radoub/
```

Clean implementation tasks also deny the old contents and git history of:

```text
SWLOR.Toolset.Domain/Render/MdlPartComposer.cs
SWLOR.Toolset.Domain/Render/MdlPartBoneMap.cs
SWLOR.Toolset.Domain/Render/TextureLoader.cs
SWLOR.Toolset.Domain/Render/MdlMeshBuilder.cs
SWLOR.Toolset.Domain/Render/MdlGeometryFlattener.cs
```

Clean work must use a sanitized worktree in which the denied render files are absent or replaced
by interface-only stubs prepared by the legacy role. "Read only the public API from the old file"
is not an acceptable clean-room boundary. Contracts come from first-party callers, tests, and
separately generated public signatures.

Repository searches must be scoped to named first-party directories. Root-recursive searches are
not allowed during clean tasks.

### Provenance record

Before the first replacement parser lands, add
`SWLOR.NWN.Formats/FORMAT-PROVENANCE.md`. Each format entry records:

- exact specification title, URL, revision/date, and local checksum if archived;
- which corpus files or synthetic byte fixtures were used;
- author and reviewer exposure declarations;
- ambiguities and the evidence used to resolve them;
- intentional incompatibilities with the old toolset behavior; and
- commands and executed sample counts from verification.

Commit messages and PR bodies should summarize this record, but they are not the only record:
commits may be squashed and PR systems may not be available forever.

### Approved evidence hierarchy

When sources disagree, use this order:

1. approved public specification;
2. synthetic fixtures whose expected values are derived by hand from that specification;
3. invariants observed in lawfully available corpus bytes;
4. consumer-facing compatibility hashes produced by the legacy/oracle role.

Old parser output is a regression signal, not an authority. A difference from a golden may be the
correct fix when the specification and corpus prove the old behavior wrong; document and review
such differences rather than rewriting the golden silently.

## Verification model

### Portable suite

The replacement-specific portable suite must run without a local NWN installation and without
Radoub. It uses:

- tiny hand-authored binary fixtures;
- repository-owned SWLOR fixtures where redistribution is already permitted;
- deterministic manifests and expected hashes; and
- malformed/truncated inputs for every binary reader.

Replacement-specific portable verification has zero `Assert.Ignore` outcomes. Absence of a
required portable fixture is a failure. The broader toolset suite currently contains unrelated
environment-gated tests; record its baseline skip count and reject any increase.

### Licensed full-corpus suite

Base-game data remains local. A separate explicit command:

- requires a detected NWN:EE installation and initialized `SWLOR_Haks`;
- fails immediately if either prerequisite is absent;
- reports requested, found, executed, failed, and skipped sample counts per format;
- treats an expected sample changing from executed to skipped as a regression; and
- writes diagnostic artifacts to an ignored temporary directory, not the repository.

Do not wire a hard local-install requirement into an otherwise portable NUnit run. Mark the
licensed tests with one consistent category and invoke that category from the documented corpus
command.

### Golden storage

- Do not duplicate the 819 MB Module JSON corpus.
- For all Module GFF JSON files, store a manifest of relative path, input hash, canonical semantic
  hash, field/type counts, and failure status.
- Keep detailed canonical JSON only for a small, reviewed set that covers every GFF field type and
  structural edge case.
- For base-game TLK, MDL, TGA, PLT, KEY, and BIF data, commit identifiers, hashes, dimensions,
  counts, and small numeric diagnostics only. Do not commit decoded strings, model arrays, or pixel
  buffers without an explicit asset-license review.
- Capture float values by their IEEE bit pattern or an explicitly documented canonical format.
- Running capture twice against the same inputs must produce byte-identical manifests.

### Build and test flow

Follow `AGENTS.md`:

```powershell
dotnet build SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build --filter "FullyQualifiedName~<RelevantClass>"
```

`RunPostBuildEvent=Never` already disables the Windows server deploy. Do not override `OS=Unix` on
a Windows build.

Clean library tasks build and test `SWLOR.NWN.Formats` and its portable tests without referencing
the toolset. Integration tasks run the portable full toolset suite before handoff. Run the licensed
full-corpus command in the designated environment and record its counts. Build once, then test
without rebuilding.

## Target dependency graph

```text
SWLOR.Toolset -> SWLOR.Toolset.Domain -> { SWLOR.NWN.Formats, SWLOR.Game.Server }
SWLOR.CLI --------------------------------> SWLOR.NWN.Formats  (future, optional)
```

`SWLOR.NWN.Formats` targets `net10.0`, enables nullable references, contains one public top-level
type per file, and does not reference the toolset or game server.

## Task sequence

```text
Task 0 governance/surface shrink ----┐
Task 1 compatibility manifests -----┴--> Task 2.0 project bootstrap
                                            |
                         +------------------+------------------+
                         |                  |                  |
                    Tasks 2a-2f         Task 3 GFF         Task 2d KEY/BIF
                         |                  |                  |
                         +------------------+------------> Task 4a MDL spike
                                                            |
                                                       Task 4b MDL reader
                                                            |
                         TGA/PLT + MDL --------------------> Task 5 rewrites
                                                            |
                                      all integrations ----> Task 6 cutover
```

Task 0 and the legacy capture portion of Task 1 may run in parallel. The clean parser tasks branch
only after Task 2.0 lands, which prevents six branches from independently adding the same project
and solution entries. Task 4a's archive scan depends on the new KEY/BIF path or an independently
written scanner.

## Task 0 — Governance and shrink the legacy surface

Role: legacy/oracle.

1. Add the provenance template, exposure declaration, corpus-category convention, and a clean-room
   workspace verification script. The script reports pass/fail without printing denied paths'
   contents.
2. Record a baseline portable test result and a licensed-corpus result with exact executed and
   skipped counts.
3. Remove the unused `IGameDataService` parameter and field from `MdlPartComposer`.
4. Delete `SWLOR.Toolset/Viewport/SwlorGameDataService.cs`.
5. Update the single caller in `BlueprintPreviewRenderer`.
6. Remove the resulting `Itp`, `Ssf`, `Resolver`, and `Services` namespace dependencies.
7. Remove or replace the legacy logger calls without otherwise changing the derived file's logic.
8. Record that this task's author is not eligible to clean-room `MdlPartComposer`.
9. Confirm the binary GFF writer remains deferred. Do not port
   `GffJsonBridge.ToGffFile` merely to keep tests for an unused capability.

Exit criteria: the dead namespaces are absent from first-party production code, the toolset behavior
is unchanged, and baseline counts are recorded.

## Task 1 — Compatibility manifests

Role: legacy/oracle.

Build the capture/verify tool against the existing first-party wrappers. It may run in a designated
legacy checkout where Radoub is already initialized, but its author must not inspect Radoub source.
Do not ask a clean implementation author to initialize the submodule.

The tool:

- reads a checked-in sample manifest;
- emits stable, invariant-culture JSON;
- records input identity and hash separately from semantic output hash;
- reports exact requested/found/executed/failed/skipped counts;
- supports `--capture` and `--verify`;
- refuses to rewrite goldens in `--verify`;
- writes licensed diagnostics outside the repository; and
- returns nonzero for a missing required corpus.

Coverage:

- **2DA:** every repository 2DA plus selected base-game text and binary tables; row identifiers,
  columns, defaults, null cells, quoted spaces, and non-ASCII cases.
- **TLK:** entry count, flags, offsets/lengths, selected semantic hashes, and sound-resref metadata.
- **KEY/BIF:** every archive loaded by `KeyBifCatalog`; precedence, identities, sizes, lazy
  extraction hashes, duplicate resolution, and missing/corrupt archive behavior.
- **GFF:** manifest over the full Module JSON corpus plus detailed fixtures for every field type,
  nested structs/lists, duplicate labels, locstrings, empty values, and non-ASCII strings.
- **MDL:** representative tiles, placeables, creature parts, skinmeshes, emitters, and animations;
  node topology, model/supermodel metadata, transforms, normals, vertices, faces, UVs, skin data,
  emitter metadata, animation tracks, and bounds.
- **TGA/PLT:** decoded semantic hashes, dimensions, origin/channel metadata, alpha policy, and small
  numeric sample grids.

The capture tool's source may be MIT, but a binary linked to the current GPL-covered toolset must
not be published as an MIT-only artifact. Rebuild it after cutover before publishing binaries.

Exit criteria: capture is byte-deterministic, portable verify is green, and the licensed corpus
verify is green with nonzero executed counts in every required format.

## Task 2.0 — Bootstrap `SWLOR.NWN.Formats`

Role: clean implementation.

Create the project once, add it to the solution, add the test project and provenance file, and add
shared guarded binary-reading primitives if justified. Do not add format implementations in this
task.

Every reader must:

- validate signatures, versions, offsets, counts, multiplication/addition overflow, and file
  bounds before allocating or slicing;
- impose defensible allocation/depth limits;
- throw a documented catchable format exception for malformed input;
- avoid path traversal when resolving archive-owned filenames; and
- include truncated, oversized-count, bad-offset, and wrong-version tests.

Exit criteria: the project builds and its empty test suite runs without the toolset, game server, or
Radoub.

## Tasks 2a–2f — Simple formats

Each clean implementation task changes the new library and its tests only. A separate integration
commit swaps an existing first-party wrapper after the parser PR is reviewed. This is mandatory for
TGA/PLT because the current `TextureLoader` is one of the derived files on the clean-room denylist.

| Task | Format | Required behavior | Integration surface | Key traps |
|---|---|---|---|---|
| 2a | Resource types | bidirectional extension/type mapping plus invalid sentinel | `ResourceIdentity`, `HakDirectoryCatalog` | case-insensitive extensions; leading dot; include NWN:EE `mtr` type 2072; compare to the first-party `ResType` enum in tests without adding a project dependency |
| 2b | 2DA | `2DA V2.0` text and `2DA V2.b` binary | `TwoDaService`, `TwoDaTable` | the original draft reversed the versions; preserve row labels, optional `DEFAULT:`, quoted cells, `****` nulls, BOM/encoding policy, and observed base-game padding |
| 2c | TLK | V3.0 read-only | `TlkService` | flags, language/encoding policy, offset/length bounds, sound resrefs; keep custom-strref math in `TlkService` |
| 2d | KEY/BIF | read-only metadata and lazy extraction | `KeyBifCatalog` | the variable-resource table is the normal KEY-indexed path; fixed-resource entries may be rejected clearly if the corpus confirms they are unused; preserve archive precedence and constrain referenced paths to the install root |
| 2e | TGA | uncompressed and RLE forms required by the corpus | clean texture integration in Task 5 | BGR(A) disk order, origin bits, RLE packet bounds, color-map/grayscale decision driven by corpus scan rather than assumption |
| 2f | PLT | dimensions and per-pixel layer/intensity data | clean texture integration in Task 5 | validate dimensions/data length; preserve ten-layer semantics and alpha policy outside the low-level reader |

Each task names its public specification in `FORMAT-PROVENANCE.md` and passes its focused portable
tests plus Task 1 verify for the relevant format.

## Task 3 — GFF V3.2 read support

Role: clean implementation.

Implement the reader and object model required by `GffJsonBridge.ToJsonDocument`,
`StandardPaletteLoader`, and `ModuleWorkspace`:

- header and all six indexed/data blocks;
- Byte, Char, Word, Short, Dword, Int, Dword64, Int64, Float, Double, CExoString, ResRef,
  CExoLocString, Void, Struct, and List;
- label deduplication on read;
- locstring strref and localized substring entries;
- explicit string encoding policy; and
- offset, count, depth, index, and allocation validation.

Do not implement binary writing in this removal task. Delete `ToGffFile` and writer-only tests during
integration, while retaining JSON document round-trip tests and reader bridge tests. If binary
writing becomes an approved requirement, give it a separate plan and real independent binary
fixtures; `RoundTripCorpusTests` does not exercise it.

Verification:

- hand-built binary fixtures for every field type and malformed block;
- `nwn_gff`-produced binary samples where redistribution is permitted;
- extracted base-game ITP samples verified locally;
- full Module JSON fidelity remains green; and
- `GffBridgeTests`, `DocumentTests.*`, `StandardPaletteTests`, and
  `ItpCategoryImporterTests` are adapted and green.

## Task 4a — MDL corpus spike

Role: clean implementation. Depends on Task 2d for the base archive scan.

Scan loose override files, the loose hak-source directories named by `Build/hakbuilder.json`, and
base-game KEY/BIF resources. A binary MDL begins with a four-byte zero field. Report counts by
source and a sample of resource identities without committing model data.

If no required ASCII model is found, ASCII is out of scope and the reader throws a specific
unsupported-format exception. If any are found, stop and re-plan before implementing an ASCII
reader.

### Task 4a implementation result (2026-07-26)

The required licensed scan classified 96,005 loose hak-source and KEY/BIF MDL resources by their
four-byte signature (binary vs. ASCII; it does not parse the file) with zero failures or skips. It
found 79,927 binary resources and 16,078 ASCII resources. The installed archive subset contains
25,598 binary and 7,236 ASCII resources (`nwn_base.key`: 25,598 binary / 7,234 ASCII;
`nwn_retail.key`: 0 binary / 2 ASCII). Binary-only scope is therefore rejected. (At the time, actual
parse coverage of that binary population was limited to a 600-file base-game sample; the full
SWLOR_Haks binary MDL population — about 54,000 files — was not parsed until `HakMdlParseSweepTests`
was added in `SWLOR.NWN.Formats.Corpus.Tests`.)
Task 4 is expanded to include an independently implemented ASCII reader before the Radoub
dependency can be removed. The owner approved that material scope expansion on 2026-07-26.

The follow-up inventory executed all 16,078 ASCII resources with zero failures or skips. The
required grammar surface includes model/supermodel headers, geometry blocks, 11,150 animation
blocks, transform controllers, and trimesh, dummy, light, emitter, AABB, danglymesh, skin,
animmesh, and reference nodes. The revised implementation should share the existing semantic
object model and safety limits, add a bounded line/token reader, implement only caller-consumed
node payloads, and verify an evenly distributed sample plus explicit coverage of each observed
node kind.

### Task 4a implementation follow-up (2026-07-26)

The standalone reader now accepts both binary and ASCII MDL. The ASCII implementation uses a
bounded line/token reader, preserves node topology, transform tracks, trimesh geometry, independent
face UV indices, named skin influences, consumed emitter metadata, and animation trees. Portable
fixtures cover the grammar and malformed boundaries. The licensed parser test executes every one
of the 16,078 required ASCII resources with zero failures or skips, including the observed legacy
concatenated directives, two-coordinate vertices, undefined animation scale, and a truncated final
mesh.

## Task 4b — MDL binary and ASCII reader

Role: clean implementation. Depends on Task 4a determining the required format surface.

Implement in verified increments:

1. file/geometry headers; `Name`, `ModelType`, `SuperModel`, bounds, and radius;
2. node topology and controller arrays;
3. trimesh vertices, faces, normals, UVs, bitmap, and render state;
4. every skinmesh field required by composition;
5. consumed emitter metadata; and
6. animations and pose tracks.

The public contract must include every caller-used member, including
`MdlModel.SuperModel` and `MdlAnimation.Name`, `Length`, and `GeometryRoot`; these were missing from
the original surface summary. Generate the final contract from first-party callers and tests, not
from the old implementation.

Malformed models must never cause out-of-bounds reads, integer overflow, unbounded allocation, or
unbounded recursion. Preserve the caller contract in which `TileModelCache` can catch a parse
failure and fall back.

Verification:

- Task 1 MDL semantic hashes;
- focused model, animation, geometry, robe, tile, placeable, and creature tests;
- explicit malformed/cyclic/oversized fixtures; and
- before/after geometry hashes plus thumbnail diagnostics in a fixed render environment. Record
  every changed resource identity; do not accept "it renders" as sufficient.

## Task 5 — Independently replace the five derived render files

Role: clean implementation in a sanitized worktree. The old file contents and history must not be
available.

Work may be split into small PRs, but each PR carries the same provenance and exposure rules:

- `TextureLoader`: derive BioWare DDS conversion from the approved DDS/DXT documentation and
  observed bytes, then integrate the new TGA/PLT readers.
- `MdlMeshBuilder` and `MdlGeometryFlattener`: derive transform order from the MDL specification,
  synthetic transforms, and geometry hashes. Share a newly derived transform helper if that makes
  the contract explicit.
- `MdlPartBoneMap`: derive the mapping from base-game skeleton/part resource relationships.
- `MdlPartComposer`: derive skeleton and body-part composition, seam handling, texture recovery,
  and composite bounds from caller contracts and corpus results.

Do not open the old files "just for signatures." Interface contracts must already exist in the
sanitized workspace.

Done means:

- the new files carry the repository's MIT SPDX identifier;
- an independent reviewer verifies the provenance record;
- every required appearance, robe, and part corpus case executes;
- geometry and texture semantic hashes are green or deviations are individually approved; and
- the old `EveryRadoubDerivedFileSaysItIsGpl` test is removed only after owner/legal signoff on the
  independence claim.

## Task 6 — Integration, cutover, and audit

Role: integration/review. Start only after Tasks 0–5 are green.

1. Replace the `Radoub.Formats` project reference with `SWLOR.NWN.Formats`.
2. Remove the Radoub solution project entries.
3. Remove only the tracked `External/Radoub` gitlink and its `.gitmodules` entry. Local
   `.git/modules` cleanup is workstation housekeeping, not a repository change, and must not be
   scripted as part of the PR.
4. Replace `ScaffoldingTests.RadoubFormats_IsReferencedAndLoadable` with a formats-library
   dependency assertion.
5. Update every production import and test type to the new library.
6. Rework `ToolsetLicenseBoundaryTests`:
   - keep the allowed-dependency guard, adding `SWLOR.NWN.Formats`;
   - retain a UI/server layering guard even though it is no longer a GPL boundary;
   - remove GPL-binary and old-derived-header assertions after signoff;
   - add a narrowly scoped scan of first-party `.cs` and `.csproj` files for `using Radoub.`,
     qualified `Radoub.*` type references, and Radoub project references. Do not reject the bare
     word in comments, exclude attribution/history documents, and avoid a self-matching
     `"no GPL-3.0 anywhere"` test.
7. Update current dependency/setup documentation. Preserve historical worklogs as history; add a
   superseded banner rather than rewriting past entries.
8. Replace the license notice with an accurate third-party/provenance notice. Do not claim "no code
   remains" until the audit is complete. Retain the historical GPL text under third-party notices
   unless the owner/legal review explicitly directs otherwise.
9. Build and publish to an isolated directory, then inspect assemblies and `.deps.json` for Radoub
   dependencies.
10. Verify a clean checkout with only `SWLOR_Haks` initialized. `External/Radoub` must be absent,
    not merely unreferenced.

Final evidence:

- branch/base SHAs;
- replacement-specific portable pass/fail/skip counts (skip must be zero);
- full toolset pass/fail/skip counts compared with the recorded baseline;
- licensed corpus requested/executed/fail/skip counts per format;
- dependency and source-scan output;
- binary/deps audit output;
- provenance approvals; and
- the owner-approved license notice.

## Principal risks and mitigations

| Risk | Mitigation |
|---|---|
| Replacement accidentally follows GPL implementation expression | sanitized worktrees, role/exposure log, old-source denylist, durable provenance file |
| Golden fixtures recreate old implementation choices instead of the specification | evidence hierarchy; consumer-semantic hashes; differences reviewed rather than auto-accepted |
| Proprietary base-game content is committed | manifests and hashes only; local diagnostics ignored; explicit asset-license review for any sample bytes |
| Parser accepts malicious counts/offsets | shared guarded reading rules and malformed-input tests for every format |
| GFF scope grows around an unused writer | reader-only Task 3; writer deferred until a production caller exists |
| MDL output is plausible but wrong | node/geometry hashes, synthetic transform tests, fixed-environment thumbnail diagnostics |
| Corpus tests silently stop running | separate required corpus command with exact counts and hard prerequisite failures |
| Parallel format branches conflict on project scaffolding | land Task 2.0 before Tasks 2a–2f branch |
| Cutover leaves non-obvious Radoub dependencies | code/project scan, scaffolding-test replacement, binary/deps audit, clean checkout |
| License declaration is changed too early | owner/legal signoff gate before header and notice removal |

## Decisions made by this review

- Keep `SWLOR.NWN.Formats` as a standalone reusable project.
- Defer binary GFF writing because it has no production caller.
- Decide ASCII MDL only from the Task 4a corpus spike.
- Treat license-obligation removal as the driver; Phase 5 and the final provenance audit are
  mandatory.
- Treat Tasks 2a–2f—not "4a–4f"—as the parallel format tasks.
- Base clean-room eligibility on exposure, not on whether someone previously completed Task 3 or
  Task 4.
