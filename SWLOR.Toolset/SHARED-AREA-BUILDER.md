# Shared area builder

The owner directs reuse of SWLOR's implemented area builder in both toolsets.
The isolated `codex/toolset-swlor` worktree moves neutral source from `391634c4e`
into the sibling shared repository's `codex/shared-toolset` branch. Primary
checkouts and running SWLOR services remain untouched.

SET data/parser, native document editing, undo operations, ARE/GIT/IFO views,
template construction, tile adjacency, matching, painting and palettes now use
`Nwn.Formats` and `Nwn.Authoring`. Old implementation files are removed. The
first-party MIT license and copyright are retained in the shared packages.
SWLOR's binary representation adapter connects its existing formats model to
the shared document model; workspace conventions, catalogs and release policy
remain here. Xenomech provides its own source representation and policies.

Current SWLOR integration package pins: `Nwn.Formats 0.1.0-dev.26`, `Nwn.Authoring 0.1.0-dev.30`, `Nwn.Preview 0.1.0-dev.35` and `Nwn.Toolset.Avalonia 0.1.0-dev.31`. These versions are recorded in the package lock files and consume the recovered shared packages.
The shared repository's `docs/area-builder-extraction.md` records provenance,
package hashes and the source/package qualification boundary.

Existing tests pass 131 area/tile/corpus and 610 document/editor/corpus checks
in source mode, and 199 focused checks in package mode, all with zero skips.
Package report: `artifacts/test-results/area-extraction-package-session-tiles.trx`.
The corpus is selected explicitly with `SWLOR_TEST_REPOSITORY_ROOT` and
`SWLOR_TEST_HAKS_ROOT`; no HAK or module source is copied to a primary checkout.
The first failed fixture attempts remain preserved alongside the passing reports.

Recovered integration checkout regression: 62/62 focused shared area-builder,
area-generation, walkmesh, door-anchor and area-writer tests passed with zero
skips. TRX: `artifacts/shared-area-builder-recovery/shared-area-builder-recovery.trx`.

Native placement mapping and GIC comment alignment also move into the shared
library in `Nwn.Authoring 0.1.0-dev.19`; SWLOR retains module naming and palette
policy. Their real package integration passes 206/206 placement, scene, workspace
and palette checks with zero skips:
`artifacts/test-results/area-placement-extraction-package-corpus.trx`.
The initial source run skipped a palette check against the empty worktree HAK
folder; correcting its explicit corpus selection passes all 12 palette checks.
That report remains preserved.

Scene, viewport and atomic creation moves are still required. The current
checks establish the extracted document/tile responsibilities and the actual
SWLOR package consumer, not a completed cross-game graphical area builder or
official-client acceptance. Builds use `RunPostBuildEvent=Never`.

## Responsive native fields qualification (2026-10-02)

Both real hosts adopt Avalonia dev.31 from shared source
`807835cd36e5d003d73c264ca6817f4769685e80`. Native variable inputs
use two rows at narrow widths; placed-object coordinates wrap while retaining
their existing controls and bindings. SWLOR keeps its known-key and gameplay
hint policy in its integration. The package SHA-256 is
`C983418C1EF0EB9E40063B36F6449148601E832CFB289A8CAD9FFC94DFD33092`.

The affected SWLOR area lifecycle, instance editing and variable policy checks
pass 19/19 with zero skips in
`SWLOR.Toolset.Tests/TestResults/swlor-dev31-area-lifecycle.trx`.
Post-build deployment is disabled. The build retains eight existing nullable
warnings and has no errors. A broader large-area run timed out in
`AreaContentsTests.BlueprintGrouping_CollapsesTheReusedBlueprintIntoOneRow`;
its aborted `swlor-field-layout-dev31.trx` and hang dump remain retained.
That large-corpus performance gate and native/official-client acceptance
remain open.
