# Codex execution brief — Radoub removal

Use this brief with `RADOUB-REPLACEMENT-PLAN.md`. Give a Codex session the standing brief and one
task only.

## Standing brief

```text
You are working in the SWLOR_NWN repository.

Read AGENTS.md and SWLOR.Toolset/RADOUB-REPLACEMENT-PLAN.md completely before acting. State your
assigned task and role: legacy/oracle, clean implementation, or integration/review.

SCOPE

Replace only the Radoub behavior named by your task. Do not port adjacent APIs, refactor unrelated
toolset code, change game-server behavior, edit Module JSON, or edit hak content.

CLEAN-ROOM SOURCE RULE

Never read, open, grep, list, search, diff, or inspect any content under External/Radoub. Do not
inspect its git objects or history. Scope searches to named first-party directories; do not run a
repository-root recursive search.

If your role is clean implementation, also do not read or inspect the current contents or history
of:

  SWLOR.Toolset.Domain/Render/MdlPartComposer.cs
  SWLOR.Toolset.Domain/Render/MdlPartBoneMap.cs
  SWLOR.Toolset.Domain/Render/TextureLoader.cs
  SWLOR.Toolset.Domain/Render/MdlMeshBuilder.cs
  SWLOR.Toolset.Domain/Render/MdlGeometryFlattener.cs

"Only reading the public API" from an old derived file is still forbidden. Use the caller-derived
contract or interface stubs supplied in the sanitized worktree. If a required contract is missing,
stop and report it.

A legacy/oracle task may build the existing dependency in a designated legacy checkout but may
not inspect its source. A clean task must never initialize External/Radoub.

PROVENANCE

Write each parser from the exact public sources listed in
SWLOR.NWN.Formats/FORMAT-PROVENANCE.md. Record source title, URL, revision/date, fixture identities,
ambiguities, exposure declaration, and verification commands there. Do not rely only on a commit
message.

Specifications outrank old-parser goldens. Treat compatibility manifests as regression signals.
If the specification and corpus prove a golden wrong, stop verification, document the difference,
and request review instead of copying the old behavior or silently refreshing the golden.

PROPRIETARY CORPUS

Do not commit decoded base-game strings, model arrays, textures, or other game assets. Commit
identifiers, hashes, counts, and minimal numeric diagnostics. Write full diagnostics to an ignored
temporary directory.

WORKTREE SETUP

Use the worktree assigned to the task. For tasks that need hak content, initialize only:

  git -c protocol.file.allow=always -c http.sslBackend=schannel submodule update --init SWLOR_Haks

Confirm SWLOR_Haks/sw_2da is non-empty. If it is not, stop. Never initialize or update
External/Radoub in a clean task.

BUILD AND TEST

For legacy/integration tasks, build once and test the toolset without rebuilding:

  dotnet build SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
  dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build --filter "FullyQualifiedName~<Class>"

Do not set OS=Unix on Windows. Run the portable full toolset suite before handoff for integration
changes that touch the toolset. For a clean library-only task, build and test SWLOR.NWN.Formats and
its test project without referencing the toolset. Run the separately documented licensed-corpus
command in the designated environment and report exact requested/found/executed/failed/skipped
counts.

Replacement-specific portable verification may not skip. The existing broader toolset suite has
environment-gated tests, so compare its skip count to the recorded baseline and reject increases.
A licensed corpus prerequisite must fail loudly when the explicit corpus command is requested.

BINARY READER RULES

Validate signature, version, offsets, counts, integer arithmetic, depth, allocation sizes, and file
bounds before use. Reject malformed/truncated input with the library's documented catchable format
exception. Add negative tests for truncated data, bad offsets, oversized counts, and wrong
versions. Archive paths must remain under the configured install root.

CODE CONVENTIONS

Target net10.0 with nullable references enabled. Use one public top-level type per file and a
folder per format. Do not use initiative/phase/Radoub labels in production identifiers. Match the
surrounding first-party style. Comment non-obvious format decisions and their evidence, not obvious
syntax.

HANDOFF

Report:

  - task and role;
  - files changed;
  - exact specifications used;
  - exposure declaration;
  - tests and corpus counts;
  - golden differences;
  - anything skipped or unavailable;
  - remaining risks.

Do not start the next task.
```

## Task 0 — Governance and dead adapter

Role: legacy/oracle.

```text
Implement Task 0 from RADOUB-REPLACEMENT-PLAN.md.

Add the durable provenance/exposure template and corpus verification convention. Record the
baseline. Then remove only the dead IGameDataService dependency:

  - remove MdlPartComposer's unused service field and constructor parameter;
  - delete SWLOR.Toolset/Viewport/SwlorGameDataService.cs;
  - update BlueprintPreviewRenderer's one construction site;
  - remove the now-unused Itp, Ssf, Resolver, and Services dependencies;
  - remove or replace the old logger calls without changing composition logic.

MdlPartComposer remains GPL-derived in this task and keeps its GPL header. Record that you are
ineligible to author its clean replacement.

Confirm GFF binary writing remains deferred: do not implement it in this task.
```

## Task 1 — Compatibility manifests

Role: legacy/oracle.

```text
Implement Task 1 from RADOUB-REPLACEMENT-PLAN.md.

Build a deterministic capture/verify tool around the existing first-party wrappers. Run it only in
the designated legacy environment. Do not inspect External/Radoub.

Use a checked-in manifest. Commit semantic hashes, counts, and small diagnostics—not duplicate
Module JSON and not decoded proprietary base-game assets. Separate portable fixtures from the
licensed local corpus. --verify must never rewrite expected output and must fail when its explicitly
requested corpus is absent.

Report requested/found/executed/failed/skipped counts per format and prove two captures against the
same inputs are byte-identical.
```

## Task 2.0 — Formats project bootstrap

Role: clean implementation.

```text
Implement Task 2.0 from RADOUB-REPLACEMENT-PLAN.md in a sanitized worktree.

Create SWLOR.NWN.Formats and its tests once, add them to the solution, add
FORMAT-PROVENANCE.md, and establish the catchable format exception and guarded binary-reading
conventions. Do not implement a file format or reference the toolset/game server.

Prove the project builds and tests without Radoub.
```

## Task 2a — Resource types

Role: clean implementation.

```text
Implement Task 2a only.

Add bidirectional extension/type lookup, invalid handling, case-insensitive extensions, and NWN:EE
mtr type 2072. You may compare against the first-party SWLOR.NWN.API ResType enum in tests, but the
formats library must not reference that project. After independent review, an integration commit
swaps ResourceIdentity and HakDirectoryCatalog.
```

## Task 2b — 2DA

Role: clean implementation.

```text
Implement Task 2b only.

Text is "2DA V2.0"; binary is "2DA V2.b". Support the required corpus surface, including row
labels, optional DEFAULT, quoted text cells, **** nulls, encoding/BOM policy, and verified binary
padding. Add hand-authored fixtures and malformed-input tests. After independent review, an
integration commit swaps TwoDaService/TwoDaTable without changing their public API.
```

## Task 2c — TLK

Role: clean implementation.

```text
Implement read-only TLK V3.0 for the consumed surface. Cover flags, language/encoding behavior,
string offset/length validation, missing text, non-ASCII text, and sound resrefs. Do not duplicate
custom-strref arithmetic from TlkService. Integrate only after independent review.
```

## Task 2d — KEY/BIF

Role: clean implementation.

```text
Implement read-only KEY/BIF metadata plus on-demand extraction.

Important correction: KEY entries resolve the BIF variable-resource table; that is the normal path
and is required. Fixed-resource entries may be rejected explicitly only after the corpus confirms
they are unused. Preserve the current multi-KEY precedence and lazy-load behavior. Validate every
index/offset/size and keep BIF paths under the configured install root.

Add synthetic multi-archive, duplicate, traversal, corrupt, and truncated tests before integration
with KeyBifCatalog.
```

## Task 2e — TGA

Role: clean implementation.

```text
Implement the TGA forms proven necessary by the corpus. Cover 24/32-bit true color, RLE packet
bounds, BGR(A) disk order, and both origin bits. Scan before deciding whether color-mapped or
grayscale input is out of scope. Compare numeric pixels, never screenshots by eye.

Do not open the old TextureLoader. Deliver the low-level reader and tests only; Task 5 performs the
clean texture integration.
```

## Task 2f — PLT

Role: clean implementation.

```text
Implement PLT dimensions and per-pixel layer/intensity data with strict length/count validation.
Keep color palette and alpha policy outside the low-level reader. Do not open the old
TextureLoader. Task 5 performs integration.
```

## Task 3 — GFF reader

Role: clean implementation.

```text
Implement Task 3 from RADOUB-REPLACEMENT-PLAN.md: GFF V3.2 read support only.

Cover every required field type, all indexed/data blocks, locstrings, nested structs/lists,
encoding policy, and comprehensive bounds/depth validation. Drive expected values from hand-built
spec fixtures, approved nwn_gff samples, and corpus semantics.

Do not implement a GFF writer. During integration, delete GffJsonBridge.ToGffFile and writer-only
tests; retain JSON fidelity and read-bridge coverage. RoundTripCorpusTests is a JSON test and is not
evidence for a binary writer.
```

## Task 4a — MDL corpus spike

Role: clean implementation.

```text
Perform only the Task 4 ASCII/binary corpus spike after Task 2d is available.

Scan loose override resources, loose hak-source directories from Build/hakbuilder.json, and
base-game KEY/BIF resources. A binary MDL begins with a four-byte zero field. Report counts by
source and sample resource identities without committing model data.

If any required ASCII model exists, stop and propose revised scope. If none exists, record that
ASCII input will receive a specific unsupported-format exception. Do not begin the reader in this
session. The 2026-07-26 spike found 16,078 required ASCII resources, and the owner approved
expanding Task 4 to implement them.
```

## Task 4b — MDL binary and ASCII reader

Role: clean implementation.

```text
Implement both binary and ASCII MDL readers after the spike determines the required surface.

Work in the increments and verify the complete caller-derived contract described in the plan.
Include model SuperModel and animation Name/Length/GeometryRoot, which the original draft's surface
summary omitted. Cover topology, transforms/controllers, trimeshes, normals, UVs, skin data,
emitters, animations, bounds, overflow, malformed offsets/counts, depth, and cycles.

For ASCII, use a bounded line/token parser and cover legacy concatenated directives, independent
face UV indices, named skin weights, static and keyed axis-angle transforms, emitters, animation
trees, and the full required licensed corpus.

Deliver the reader and tests without opening the old derived render files. Integration and clean
render replacements happen later.
```

## Task 5 — One derived render replacement

Role: clean implementation.

```text
Implement exactly one Task 5 replacement named in the assignment, in a sanitized worktree where all
five old derived implementations and their history are unavailable.

Use only the approved public specifications, interface contract, synthetic tests, corpus bytes, and
semantic hashes. Do not request the old file "for reference." If the contract is insufficient, stop
and report the missing contract.

Add the repository MIT SPDX identifier only when you can truthfully make the independence
declaration. Report every corpus deviation. Do not begin another Task 5 file.
```

## Task 6 — Cutover

Role: integration/review.

```text
Implement Task 6 only after Tasks 0-5 are complete and approved.

Replace project/type references, remove Radoub solution entries, replace ScaffoldingTests, remove
the tracked gitlink and .gitmodules entry, update current documentation, and perform the scoped
source/project and built-binary dependency audits.

Do not delete local .git/modules data as a repository step. Do not create a self-matching test that
rejects the words Radoub or GPL from historical attribution documents. Preserve historical worklogs
and use narrowly scoped executable-dependency assertions.

Do not claim MIT-only status or remove the old derived-header assertion until the provenance and
owner/legal signoff gates are satisfied. Verify a clean checkout that has only SWLOR_Haks
initialized and report all portable and licensed-corpus counts.
```

## Review checklist

- [ ] The task and role match the plan.
- [ ] No denied source or history was inspected.
- [ ] Clean tasks used a sanitized worktree.
- [ ] `FORMAT-PROVENANCE.md` names exact sources and evidence.
- [ ] No proprietary decoded asset content was committed.
- [ ] Replacement-specific portable tests executed with zero skips.
- [ ] Licensed corpus counts prove every required format actually ran.
- [ ] Every changed golden has an explicit, evidence-backed disposition.
- [ ] Binary readers include malformed/count/offset/overflow tests.
- [ ] One public top-level type per file.
- [ ] No initiative labels appear in production identifiers.
- [ ] The PR contains only its assigned task.
- [ ] Task 5 independence and final license changes received the required approval.
