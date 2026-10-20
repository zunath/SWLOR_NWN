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

Install steps are in the [root README](../README.md#install-the-toolset). Packages include the .NET runtime and command-line tools.

- **Workspace:** on first run, choose **Create SWLOR workspace** or **Open existing workspace**. A workspace holds editable module resources, conversation graphs, the category catalog, and matching editor metadata. `swlor-workspace.json` records the original revision and file checksums.
- **Assets:** HAKs and TLK download once per revision, separately from workspaces, and every workspace shares them. They live in `%LOCALAPPDATA%/SWLOR.Toolset/Assets/<revision>` on Windows and `~/.local/share/SWLOR.Toolset/Assets/<revision>` on Linux (`XDG_DATA_HOME` applies). NWN:EE supplies optional base-game assets.
- **Switching workspaces:** use **File → Workspaces**, then reopen the application.
- **Contributions:** **File → Export Contribution** writes a ZIP of added, changed, and deleted resources with original formatting. It never includes server code, credentials, or deployment configuration.
- **Updates:** **Help → Check for Updates** covers the Windows installer and Linux AppImage. Prerelease installs also see newer prereleases; stable installs see stable releases only. Workspaces and asset packs are never touched. Newer content arrives as a new workspace and never replaces local edits. Portable archives update by downloading a newer archive.

## Source build requirements

| | |
|---|---|
| .NET SDK | Stable **10.0.100 or newer**; `global.json` rejects previews. Both toolset projects target `net10.0`, x64 |
| Submodules | `SWLOR_Haks` must be initialised |
| NWN:EE install | **Optional.** Needed only for base-game tilesets/models; SWLOR hak content works without one |
| OS | Windows x64 or Linux x64; Ubuntu 24.04 tested, X11 or Wayland with XWayland, OpenGL |
| Linux libraries | `libpcre3`, `libsqlite3-0`, and the desktop libraries in the [root README](../README.md#set-up-the-source-checkout) |

```bash
git submodule update --init --recursive
```

## Build and run

Run `Setup.cmd` then `StartToolset.cmd` on Windows, or `./Setup.sh` then `./StartToolset.sh` on Linux. [Root README](../README.md#set-up-the-source-checkout) explains each step.

To build by hand, run `dotnet` from the **repository root** after setup has prepared the native tools:

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
   broken install degrades to hak-only rather than failing startup. Linux checks Steam (including
   Flatpak and extra libraries), GOG, and Beamdog; use **Locate Neverwinter Nights** for other paths.
   Linux reads `~/.local/share/Neverwinter Nights/nwn.ini`; `NWN_USER_DIRECTORY` overrides the profile folder.

Sound preview is unavailable on Linux: it uses the Windows audio backend.

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

### Publish a release

Toolset releases never use GitHub's **Latest** badge, which belongs to the dated game releases. Each version is published as `toolset-v<version>` (not latest). Stable versions also refresh one fixed, non-latest release, `toolset-latest`, which holds the download links in the [root README](../README.md#install-the-toolset) and the feed (`ToolsetFeed.DefaultUrl`) that source builds read. Feed entries and installed copies use the immutable `toolset-v<version>` assets: installed builds read `releases/download/toolset-v<version>/toolset-feed.json` and update through Velopack's GitHub source, which lists releases and ignores `toolset-latest`. Prerelease versions (`1.1.0-beta`) never touch `toolset-latest`.

Maintainers only. Python 3.11 or newer is required. Windows PowerShell wrappers exist for the lower-level commands: `tools/PublishToolsetAssets.ps1`, `tools/PublishToolset.ps1`, `tools/ImportToolsetContribution.ps1`.

#### From your machine (no GitHub Actions)

Needs Windows, Python 3.11+, the .NET SDK, Docker Desktop (Linux containers), and `gh` logged in as `zunath` with the `repo` scope.

1. Pack the assets for the pinned `SWLOR_Haks` revision once per revision. You need disk space for the source assets, packed HAKs, and download parts.

   ```bash
   python tools/ToolsetDistribution.py assets --output /path/outside/checkout/swlor-assets-new
   ```

2. Upload every file in the output's `release` directory to a GitHub release named `toolset-assets-<full-haks-commit>` and do not mark it latest (`gh release create toolset-assets-<commit> <files> --latest=false`). Files are split into 1 GiB parts with per-part and combined SHA-256 checksums.
3. Merge the toolset change, then check out `master` with no local edits. `ReleaseToolset.cmd` refuses any other checkout, a stale `origin/master`, or a `gh` login that is not `zunath` with `repo` scope.
4. Dry run (the default). It builds `win-x64` natively and `linux-x64` in a pinned Docker container (AppImage, tar.gz, and permissions need Linux), runs the same release tests and install checks as the workflow, and prints every file with its size and checksum plus the `gh` commands it would run. Nothing is created, tagged, or uploaded.

   ```
   ReleaseToolset.cmd --version 1.0.0
   ```

5. Publish with the same command plus `--publish`. It reuses the dry run's checksum-verified build (nothing is rebuilt) and creates `toolset-v1.0.0` (latest disabled) with all assets, then moves the `toolset-latest` tag to the released commit and replaces that release's download assets. Reruns are safe: an identical existing version is skipped, a different one is refused (publish a new version instead).

   ```
   ReleaseToolset.cmd --version 1.0.0 --publish
   ```

Output goes to `%USERPROFILE%\swlor-toolset-releases\<version>-<commit>` (or `--output`); delete it afterwards. Other options: `--skip-tests`, `--asset-descriptor <asset-pack.json>` instead of downloading it from the asset release. For Windows signing, set `SWLOR_SIGN_PFX` (path to the PFX) and `SWLOR_SIGN_PASSWORD` in the environment, then add `--sign-windows`. The password is never written to disk or logs, and the certificate is removed from your store afterwards unless it was already there. Unsigned downloads may show SmartScreen warnings.

#### From GitHub Actions (when Actions is available)

After step 2, run **Package SWLOR Toolset** with a semantic version. Without `publish`, it uploads Windows and Linux preview artifacts. Publishing requires master, the repository owner, and approval in the `toolset-release` environment, and then runs the same publish code as `ReleaseToolset.cmd`. The workflow builds self-contained Windows and Linux applications, tests both hosts, and builds Velopack installers and AppImage updates. Editor releases reuse the asset pack, so they never rebuild the large asset repository.

Keep GitHub's immutable releases setting off for this repository: `toolset-latest` is replaced by design, and the publisher refuses an immutable one.

Windows signing in Actions is off by default. To enable it, set `sign_windows` and add the `TOOLSET_SIGNING_PFX_BASE64` and `TOOLSET_SIGNING_PASSWORD` secrets to the `toolset-release` environment.

### Build a local preview

Download the release's `asset-pack.json`, then run:

```bash
python tools/ToolsetDistribution.py publish --version 1.0.0 --asset-descriptor /path/asset-pack.json --output /path/outside/checkout/swlor-toolset-new
```

Add `--skip-workspace` to build only the application. On Ubuntu 24.04, Linux packaging needs `squashfs-tools`, `file`, `desktop-file-utils`, `patchelf`, and `libfuse2t64`. AppImage users need FUSE 2 or can set `APPIMAGE_EXTRACT_AND_RUN=1`. The tar.gz needs neither.

### Import a contribution

1. Check out a clean feature branch (not master) whose `SWLOR_Haks` pin matches the contribution. The import refuses a dirty tree or a different asset revision.
2. Check the contribution. `--toolset` is the path to the `SWLOR.Toolset` executable (`SWLOR.Toolset.exe` on Windows).

   ```bash
   python tools/ToolsetDistribution.py import-contribution --archive /path/contribution.zip --toolset /path/SWLOR.Toolset
   ```

3. Apply it by adding `--apply` to the same command.
4. Review `git diff`, build the server if conversations changed, run validation, and open a pull request.

The import writes nothing if a changed file no longer matches its original checksum. That means newer work exists, so merge it by hand. Writes keep exact bytes and roll back on failure. After an interrupted import, review `git diff` before retrying.
