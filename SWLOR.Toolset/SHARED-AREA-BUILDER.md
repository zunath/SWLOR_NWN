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

SWLOR consumes `Nwn.Formats`, `Nwn.Authoring`, `Nwn.Preview` and `Nwn.Toolset.Avalonia` `0.1.0-preview.2` from nuget.org. The single `NwnToolsetPackageVersion` property in `Directory.Build.props` sets the version, and the resolved versions and hashes are recorded in the package lock files. To develop against the shared source instead, clone https://github.com/zunath/NWN.Toolset (for example to `C:/Projects/NWN.Toolset`) and pass `-p:NwnToolsetSourceRoot=C:/Projects/NWN.Toolset`; to test unpublished local builds, pass `-p:NwnToolsetPackageFeed=<folder of .nupkg files>` together with `-p:RestoreLockedMode=false`.
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

At that initial document/tile extraction stage, scene, viewport and atomic
creation moves were still required. Those reports establish the responsibilities
tested at that stage. Subsequent shared scene/viewport adoption is recorded in
the shared provenance document. Official-client acceptance remains open.
Builds use `RunPostBuildEvent=Never`.

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

## Shared Scene/Properties layout qualification (2026-10-03)

The existing area view now uses `AreaEditorLayout` from the same local
Avalonia dev.32 package as Xenomech. It keeps the original scene, properties,
context menus, camera pad, HUD and selected-tab binding. The neutral
Scene/Properties split comes from this view and does not replace its editing
or resource policies.

The package's production source is
`d97cb90f35b4597db42a0a773046ec07e97e773a`; SHA-256 is
`F5AE982E326559DB356E1CBDA587561A44CC3CE8811508902163BE7794E649A5`.
Debug and Release locks record the same package train. The actual area view
and affected lifecycle/instance/variable workflows pass 20/20, zero skips, in
`SWLOR.Toolset.Tests/TestResults/swlor-shared-scene-properties-dev32.trx`.
The view test switches both tabs, retains the original controls and checks the
scene keeps its full tab height. Existing eight nullable warnings remain;
there are no build errors and deployment hooks are disabled.

Xenomech supplies its own shell and database integration. These tests qualify
shared layout consumption, not complete pixel parity or official-client behavior.
The large-area performance gate above remains open.

## Shared camera controls (2026-10-03)

The eleven existing pan/orbit/zoom/reorient controls and handlers are removed
from the area view and supplied by shared `AreaCameraControls`. It operates
the view's original `AreaView.Viewport`; no second camera is created.
Object-rotation and tile-height controls retain their existing host commands.
Directions, glyphs, square button sizing and 16ms repeat cadence are retained;
the shared pad wraps in narrow host panes.

Both applications pin Avalonia dev.33, produced once from shared source
`f475101`. Package SHA-256 is
`C14DF8662019E0FFB2941C0E2EB384FCD38DEE0C4B25BF5646F8A6C13F1201F2`.
Shared controls pass 19/19 and actual SWLOR view/lifecycle/instance/policy checks
pass 20/20, zero skips, in
`SWLOR.Toolset.Tests/TestResults/swlor-shared-camera-layout-qualified-dev33.trx`.
The actual view test confirms the shared pad targets its original viewport and
contains all eleven actions. Xenomech's actual GL/database placement fixture
also exercises pan, orbit, zoom and reset, reporting a 730×683 viewport and
no document edits. This does not replace both-host official-client acceptance.
