# SWLOR Toolset

A SWLOR-only replacement for the parts of the Aurora Toolset the team still uses: **area editing,
instance placement, blueprint editing, tile painting, and NWScript editing**. Everything else —
dialogs, quests, spawns, and merchant inventory authoring — already lives elsewhere and is not in
scope here.

The script editor covers the module's 87 `.nss` sources: syntax highlighting, context-aware
completion over the 1,187 engine functions and 6,201 constants, signature help, go-to-definition,
rename, an Aurora-style function browser, and compilation via the vendored `nwn_script_comp`.
It exists because `ModulePacker` copies `.nss`/`.ncs` verbatim and never compiles, so the committed
bytecode is what the game runs — see `SCRIPT-EDITOR-PLAN.md`.

It is an Avalonia desktop app that reads and writes the module's per-resource JSON directly
(`Module/{are,git,gic,utc,uti,utp,…}/<resref>.<ext>.json`, neverwinter.nim `nwn_gff` format) and
reuses the existing pack pipeline unchanged.

---

## Installed toolset

Download the Windows installer or portable ZIP, or the Linux x64 AppImage or portable tar.gz from [Releases](https://github.com/zunath/SWLOR_NWN/releases/latest). The standalone downloads become available after the first toolset release is published. All packages include the .NET runtime and command-line tools; users do not need Git, Python, Docker, Visual Studio, or an SDK.

On first run, choose **Create SWLOR workspace** or **Open existing workspace**. Packaged applications download content from their matching release, including prereleases. A new workspace contains editable module resources, conversation graphs, the category catalog, and matching editor metadata. Its `swlor-workspace.json` records the original revision and file checksums, with generated editor metadata checksums separate from editable content. Assets are downloaded separately into `%LOCALAPPDATA%/SWLOR.Toolset/Assets/<revision>` on Windows or `~/.local/share/SWLOR.Toolset/Assets/<revision>` on Linux (respecting `XDG_DATA_HOME`) and reused. NWN:EE remains the source of optional base-game assets.

Use **File → Workspaces** to select another workspace, then reopen the application. Save and validate your edits, then use **File → Export Contribution** to create a ZIP for maintainer review. The export includes added, changed, and deleted resources without changing the original formatting. It never includes server code, credentials, or deployment configuration. Conversation changes still require a server build before in-game testing.

**Help → Check for Updates** updates installed Windows applications and Linux AppImages while keeping workspaces and asset packs separate. Prerelease installations also consider newer prereleases; stable installations consider stable releases. Portable archives are updated by downloading a newer archive. Downloading newer content creates a new workspace; it never replaces local edits.

## Source build requirements


| | |
|---|---|
| .NET SDK | **10.0** (both toolset projects target `net10.0`, x64) |
| Submodules | `SWLOR_Haks` must be initialised |
| NWN:EE install | **Optional.** Needed only for base-game tilesets/models; SWLOR hak content works without one |
| OS | Windows x64 or Linux x64; Ubuntu 24.04 tested, X11 or Wayland with XWayland, OpenGL |
| Linux source tools | `libpcre3`, `libsqlite3-0`, plus the desktop libraries listed in the root README |

```bash
git submodule update --init --recursive
```

## Build and run

Use `Setup.cmd` / `StartToolset.cmd` on Windows or `./Setup.sh` / `./StartToolset.sh` on Linux. These run the shared .NET setup command and prepare host-native packing and compiler utilities. Source setup and launch do not require Python. Linux downloads are pinned and verified by SHA-256 before execution.

For manual builds, always run `dotnet` from the **repository root**:

```bash
dotnet build SWLOR.Toolset/SWLOR.Toolset.csproj -p:RunPostBuildEvent=Never
```

```bash
dotnet run --no-build --project SWLOR.Toolset
```

## Tests

```bash
dotnet build SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build
```

The suite has 3,400+ tests, including corpus-wide animation, model and rendering checks. Runtime
depends on the available assets. The complete pinned `SWLOR_Haks` checkout is required: a sparse
checkout containing only tileset definitions is sufficient for some generator tests but causes
missing-file failures elsewhere. Restore a sparse checkout before running the full suite:

```bash
git -C SWLOR_Haks sparse-checkout disable
```

A handful of corpus gates also need a local NWN:EE install for base-game data and call
`Assert.Ignore` without one. Platform-specific tests can likewise be skipped on an unsupported OS.

```bash
dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build --filter "FullyQualifiedName~TilePainterTests"
```

---

## Project layout

```
SWLOR.Toolset/          Avalonia app  — shell, docked panels, editors, OpenGL viewport
SWLOR.Toolset.Domain/   headless lib  — all logic lives here (no UI dependency)
SWLOR.Toolset.Tests/    NUnit         — unit tests + full-corpus gates
SWLOR.NWN.Formats/      headless lib  — standalone read-only Aurora resource formats
tools/SWLOR.ConversationMigrator/     — single-file legacy dialog importer (never overwrites authored graphs)
tools/SWLOR.AnimationDrafts/          — authored-animation generation, preview, and installation CLI
```

References flow strictly one way:

```
{ SWLOR.Toolset, SWLOR.ConversationMigrator, SWLOR.AnimationDrafts }
    → SWLOR.Toolset.Domain → { SWLOR.NWN.Formats, SWLOR.Game.Server }
```

**Shared authoring and data logic belongs in `Domain`.** The two CLI projects are reviewed executable
leaf consumers of that headless library; shared libraries and the game server must not reference them.
The animation CLI works independently of the desktop editor. See [LICENSE-NOTICE.md](LICENSE-NOTICE.md)
for the source and dependency boundary.

The NUnit project references both `Domain` and the app for domain tests, view-model tests, and
headless Avalonia view checks. These test references do not change the production dependency direction.

### Where things live in `Domain`

| Path | What |
|---|---|
| `Gff/` | Byte-level JSON-GFF reader/writer with raw-token preservation |
| `Documents/` | Typed views (`AreDocument`, `GitDocument`, `UtcDocument`, …) |
| `Editing/` | Transactions, undo stack, edit scope |
| `Editors/` | Field schemas driving the blueprint forms, and lookup keys |
| `GameData/` | 2DA, TLK, resource index, game-code index, editor lookups |
| `GameData/Tilesets/` | `.set` parsing, tile adjacency, SET rule matcher, terrain painter |
| `AreaGeneration/` | deterministic layouts, tile resolution, decoration planning, native area projection |
| `Render/` | MDL meshes, textures, walkmesh, area scene assembly |
| `Validation/` | Module validation rules |
| `Workspace/` | Module enumeration/loading, new-area writing |

---

## How it finds data

1. **Module root** — from `%LOCALAPPDATA%/SWLOR.Toolset/settings.json` on Windows or `~/.local/share/SWLOR.Toolset/settings.json` on Linux, or auto-detected from the
   repo layout. A valid root contains `are/` and `utc/` subfolders. If neither works the status bar
   says so; set `moduleRoot` in that settings file and restart.
2. **Haks** — `SWLOR_Haks/`, layered per `Build/hakbuilder.json` (the first matching layer wins).
3. **Base game** — an NWN:EE install, auto-located or overridden in settings. Missing is fine; a
   broken install degrades to hak-only rather than failing startup. Linux probes Steam (including
   Flatpak and additional libraries), GOG, and Beamdog; use Locate Neverwinter Nights for custom paths.
   Linux profiles use `~/.local/share/Neverwinter Nights/nwn.ini`; `NWN_USER_DIRECTORY` overrides the profile root.

Sound preview currently uses the Windows audio backend and is unavailable on Linux.

Every game-data service is optional at the DI level. When 2DAs, the TLK, or the resource index do not
resolve, the affected features degrade (dropdowns fall back to numeric boxes, the 3D view reports
unavailable) instead of crashing.

---

## Invariants — read before changing anything

**Saves must produce zero spurious git diff.** The GFF layer preserves raw tokens, key order, EOL
style, and trailing-newline state; floats reproduce Nim's `%.16g` formatting exactly. Editing one
field must rewrite that field and nothing else. `RoundTripCorpusTests` is the permanent gate and has
to stay green forever.

**Never reference `SWLOR.Toolset.*` from another project.** The desktop toolset is an outer
application layer. Shared libraries and the game server must remain independently reusable. See
[LICENSE-NOTICE.md](LICENSE-NOTICE.md).

**Builder-facing dropdowns and pickers never show 2DA placeholder rows** (`DELETED`, `USER`,
`UNUSED`, `INVALID*`, `Padding`, numbered `NULL` slots, and similar). Route 2DA options through
`TwoDaChoicePolicy`, declare required columns in `TwoDaLookupTables`, fail closed when validity
cannot be proven, and add regression coverage for each new 2DA-backed option source.

**Corpus gates are the real safety net.** Several tests run against all 438+ module areas — GFF
round-trip, scene assembly, tile adjacency, matcher soundness, paint idempotency. They exist because
this domain is full of rules that look right in isolation and are wrong against real data. Do not
weaken one to make a change pass.

---

## Gotchas that have already bitten

**Corner terrain does not describe what is built on a tile.** A tileset can have hundreds of tiles
with identical corner terrains that differ only in the scenery on them — `tcn01` has 244 all-Cobble
tiles, one of which carries a building wall. Tile selection prefers the `.set` `PathNode` code `A`
(open, unobstructed ground) to avoid filling an area with walls.

**Crossers span a boundary.** A dock, bridge, or corridor must be declared by *both* adjacent tiles.
Generation enforces exact symmetry; validation stays blank-tolerant, because a few real corpus
boundaries genuinely are one-sided. Liberal in what we accept, strict in what we emit.

**Tile models are origin-centred**, spanning `-TileSize/2 .. +TileSize/2`. Placement rotates about the
tile centre and translates to the cell centre. Rotating about a corner instead lands rotated tiles a
full cell away — and stays invisible at orientation 0, where it degrades to a uniform half-tile shift
of the whole grid.

**A tileset's `Floor` is its default fill, not "walkable ground".** Exterior tilesets declare walkable
terrain there (`tms01` Grass); interior ones declare solid rock (`tib01` Wall, whose walkable terrain
is `Room`). A new interior area is *meant* to start solid and have its rooms painted out of it, which
is how Aurora behaves too.

**Base-game names depend on an optional `dialog.tlk`.** When an NWN:EE install is available the
toolset loads its localized base TLK as well as SWLOR's custom TLK. An unreadable base TLK now
degrades to custom text and readable 2DA labels instead of preventing startup.

---

## What works today

Phases 0–7 of [PLAN.md](PLAN.md) are complete. In practice:

- **Browse** the module by resource type, with parsed names/tags and full-text search.
- **Edit blueprints** through schema-driven forms — creatures, items, placeables, doors, stores,
  triggers, sounds, waypoints — with undo/redo and local-variable tables. New merchants are not
  offered until their `StoreList` inventory can be authored in the toolset.
- **Edit areas**: properties, placed-instance lists, and a 3D viewport with orbit/pan/zoom, picking,
  selection sync, move/rotate gizmos, place-from-palette, and trigger footprint sizing.
- **Paint terrain** with automatic neighbour blending that respects terrain, crossers, and absolute
  corner elevation, plus rotate and raise/lower brushes.
- **Create areas** from a template, registered in `module.ifo`, or use **Tools → Area Generator**
  to preview a deterministic themed layout and write its tiles, atmosphere, transitions, doors,
  and decorations directly into the open module. Export remains in the existing ERF Manager.
  See the [area generator guide](AreaGeneration/README.md) for prop spacing options, preview overlays,
  placement diagnostics and verification limits.
- **Validate and pack** without leaving the app.

`WORKLOG.md` records what each work package did and, more usefully, *why* — including the bugs found
and the evidence behind each rule.

---

## License

First-party toolset and format-library source is licensed under the repository's MIT license.
Third-party package and replacement provenance details are recorded in
[LICENSE-NOTICE.md](LICENSE-NOTICE.md) and
[FORMAT-PROVENANCE.md](../SWLOR.NWN.Formats/FORMAT-PROVENANCE.md).

## Releases and maintainer imports

Application source stays in this repository. The release workflow builds self-contained Windows and Linux applications and packers, tests distribution boundaries on both hosts, and prepares Velopack installer/AppImage updates. Both platforms share one content snapshot and the asset pack for the exact pinned SWLOR_Haks revision, so routine editor releases do not clone or rebuild the large asset repository.

Release packaging and maintainer import commands require Python 3.11 or newer.

Prepare assets once per SWLOR_Haks revision on either platform with enough disk space for source assets, packed HAKs, and compressed download parts (use `python3` on Linux):

```bash
python tools/ToolsetDistribution.py assets --output /path/outside/checkout/swlor-assets-new
```

Upload every file from its `release` directory to a GitHub release named `toolset-assets-<full-haks-commit>`, with that release **not** marked latest. Files are split into 1 GiB parts with per-part and combined SHA-256 checksums.

Run **Package SWLOR Toolset** in GitHub Actions with a semantic version. Preview runs upload Windows and Linux artifacts. Public publishing requires master, the repository owner, and the `toolset-release` environment. Signing is optional and off by default. To sign Windows packages, enable `sign_windows` and configure that environment's `TOOLSET_SIGNING_PFX_BASE64` / `TOOLSET_SIGNING_PASSWORD` secrets. Unsigned Windows downloads may show SmartScreen warnings.

For a local preview build, download the matching `asset-pack.json`, then run:

```bash
python tools/ToolsetDistribution.py publish --version 1.0.0 --asset-descriptor /path/asset-pack.json --output /path/outside/checkout/swlor-toolset-new
```

Use `--skip-workspace` for an application-only preview. Linux packaging requires `squashfs-tools`, `file`, `desktop-file-utils`, `patchelf`, and `libfuse2t64` on Ubuntu 24.04. AppImage users need FUSE 2 or can run with `APPIMAGE_EXTRACT_AND_RUN=1`; the tar.gz fallback needs neither FUSE nor an SDK. Desktop dependencies remain OS packages. PowerShell wrappers remain available on Windows.

Maintainers import contributions in a clean feature branch. Check first, review the result, then apply:

```bash
python tools/ToolsetDistribution.py import-contribution --archive /path/contribution.zip --toolset /path/SWLOR.Toolset
python tools/ToolsetDistribution.py import-contribution --archive /path/contribution.zip --toolset /path/SWLOR.Toolset --apply
```

Use `SWLOR.Toolset.exe` on Windows. Native utilities and their Linux shared libraries are bundled with the application; source builds resolve them from the setup cache. Linux runner startup uses Docker Compose and skips Visual Studio debugger attachment.

The helper checks the asset revision and refuses a dirty checkout or master for application. Every changed file must still match its original checksum. Import preserves exact bytes and rolls back writes if an operation fails; review the Git diff after any interrupted process before retrying. Build the server for conversation changes, run relevant validation, and submit the branch through a pull request.
