# SWLOR Toolset — Work Log

Single source of truth for work-package progress. Plan:
`C:\Users\benco\.claude\plans\i-love-it-see-recursive-owl.md` (controller keeps a copy of the
approved plan; phases/packages are summarized there). One entry per work package; update the
status line in place and append details as work happens. Statuses: `pending | in-progress |
done | blocked`.

## WP0.1 — done — 2026-07-19
- Tier: Mid (controller-executed).
- Files: `External/Radoub` submodule (pinned `radoub-v0.11.0`, commit `8dd65638`),
  `SWLOR.Toolset{,.Domain,.Tests}` projects, sln entries, `LICENSE-NOTICE.md`, this file,
  placeholder test, minimal Avalonia scaffold (`Program.cs`, `App.axaml`, `Shell\MainWindow`).
- **Decisions recorded:**
  - **.NET version: `net10.0`** for all three toolset projects. Deviation from the expected
    `net9.0`, with rationale: the reference floor is Radoub.Formats (`net9.0`), which rules
    out `net8.0`; but .NET 9 is an STS release that left Microsoft support in May 2026 AND
    no 9.0 runtime is installed on the dev machine (tests failed to launch). `net10.0` is
    therefore the lowest *supported* version meeting all cross-compatibilities (LTS, runtime
    10.0.10 + SDK 10.0.302 installed; net10 projects reference net9 Radoub and net8
    SWLOR.Game.Server downward without issue — verified by full-solution Debug and Release
    builds plus a passing test run).
  - **Radoub.UI: referenced** — cleanly consumable csproj; contains `ModelPreviewGLControl`
    (OpenGL model preview, Silk.NET.OpenGL) which WP4.x will reuse. Chains
    `Radoub.Dictionary` transitively.
  - Radoub uses **Central Package Management** via `External\Radoub\Directory.Packages.props`
    (scoped to its subtree; repo root unaffected). Our packages pin matching versions:
    Avalonia 11.3.17, CommunityToolkit.Mvvm 8.4.2.
  - Radoub uses Nerdbank.GitVersioning — works because the submodule is its own git repo.

## WP0.2 — pending — CLI `--no-prompt` flag
## WP1.1 — pending — Generic JSON-GFF model + reader/writer
## WP1.2 — pending — Corpus conformance utilities
## WP1.3 — pending — Round-trip gate
## WP1.4 — pending — Typed documents
## WP1.5 — pending — Transactions/undo/dirty
## WP1.6 — pending — Radoub bridge
## WP2.1 — pending — 2DA + TLK services
## WP2.2 — pending — SET parser
## WP2.3 — pending — Resource index
## WP2.4 — pending — Lookup services
## WP2.5 — pending — Game-code index
## WP2.6 — pending — Shell + read-only browser
## WP3.1 — pending — Editor schema infrastructure + UTC editor
## WP3.2 — pending — Remaining blueprint schemas
## WP3.3 — pending — Instance editing
## WP3.4 — pending — Validation rules
## WP3.5 — pending — Save + pack services
## WP3.6 — pending — End-to-end daily-driver gate (human verify)
## WP4.1 — pending — GL spike
## WP4.2 — pending — Mesh/texture pipeline
## WP4.3 — pending — Model preview panes
## WP4.4 — pending — Area scene assembly
## WP4.5 — pending — Area view
## WP5.1 — pending — Picking + selection sync
## WP5.2 — pending — Gizmos + placement
## WP6.1 — pending — Walkmesh (WOK)
## WP6.2 — pending — Perf + fidelity pass
## WP7.1 — pending — Tile adjacency corpus
## WP7.2 — pending — Tile rule matcher
## WP7.3 — pending — Paint tools + new-area wizard
