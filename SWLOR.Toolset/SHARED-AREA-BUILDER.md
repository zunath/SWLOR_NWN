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

Package pins: `Nwn.Formats 0.1.0-dev.20`, `Nwn.Authoring 0.1.0-dev.18`.
Preview and Avalonia pins remain `0.1.0-dev.16` and `0.1.0-dev.11`.
The shared repository's `docs/area-builder-extraction.md` records provenance,
package hashes and the source/package qualification boundary.

Existing tests pass 131 area/tile/corpus and 610 document/editor/corpus checks
in source mode, and 199 focused checks in package mode, all with zero skips.
Package report: `artifacts/test-results/area-extraction-package-session-tiles.trx`.
The corpus is selected explicitly with `SWLOR_TEST_REPOSITORY_ROOT` and
`SWLOR_TEST_HAKS_ROOT`; no HAK or module source is copied to a primary checkout.
The first failed fixture attempts remain preserved alongside the passing reports.

Placement mapping/GIC, scene and viewport moves are still required. The current
checks establish the extracted document/tile responsibilities and the actual
SWLOR package consumer, not a completed cross-game graphical area builder or
official-client acceptance. Builds use `RunPostBuildEvent=Never`.
