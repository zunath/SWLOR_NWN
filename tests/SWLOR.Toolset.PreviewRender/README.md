# Shared area editor native acceptance

The existing SWLOR MainWindow consumes Formats26, Authoring36, Preview37 and
Avalonia45 through normal package references. Incremental placements carry their
actual index in the resource-kind GIT list, preserving scene selection and the
matching Area Contents row without rebuilding unrelated terrain or instances.

Run `Run-SharedAreaNativeAcceptance.ps1` with explicit licensed installation,
read-only source/HAK roots and the pinned seed module. The recipe creates an owned
workspace, uses the ordinary New Area template, paints tile 75, places an unlocked
door through the shared Palette and production pointer handlers, applies Specific:1
through the appearance gallery, saves, packs with SWLOR.CLI, then reopens in a fresh
MainWindow process. It compares complete decoded trees and exact production
converter output bytes for ARE/GIT/GIC, preserving the original seed creature list
and its absent GIC. Different GFF writers can encode equivalent trees differently;
no claim of byte equality between independent writers is made.

The qualified run is `artifacts/qualification/sw-area-editor-native-effb96b4c9fb4fefbd009049ed1ec1d4`.
Its packed module SHA-256 is d32e30a35f7bab1f770176ece18981b999d0248799942117a2cd003b6d9e7d6b.
`qualified-provenance.json` records package DLLs, source/PDB bindings and the earlier
mixed CLI graph. Root separately verifies those bindings and freezes 509 runtime
files plus 16 pending source files in `root-frozen-family-r2`; its manifest SHA-256
is 2044511ff182df3b5c481b600bd4027ab9bf4ee8d832234d53e4a91e5627ad48.
The focused AreaPlacementIncremental suite passes 3/3 with zero skips.

The Xenomech feature worktree's existing official-client fixture takes that exact
SWLOR-packed module by explicit source path and hash. Its action wrapper changes
only the module entry script and adds the compiled check script, preserving all
other packed resource bytes. The official client passes normal closed/open/closed
door actions in the authored `sweditarea01` area on explicit embedded UDP 56317,
with inspected 1280 by 720 frames and no client process remaining afterward.
The actual test passes 1/1 with zero skips at
`artifacts/qualification/sw-official-client-root-20261004T042811Z` in that worktree.
Root's verification SHA-256 is 141659f0a5ffddcbefa60df75a6bd21564c9f9f68e8431fe7ccc11d7c5ef437c.

This qualifies the small shared area/door edit-save-reopen-pack-client workflow.
It does not qualify broader appearance classes, continuous animation, collision,
DEV resource refresh or desktop performance budgets. Primary checkouts and running
services are not modified by this recipe. Failed runs remain retained.
