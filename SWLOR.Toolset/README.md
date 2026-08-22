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

## Requirements

| | |
|---|---|
| .NET SDK | **10.0** (both toolset projects target `net10.0`, x64) |
| Submodules | `SWLOR_Haks` must be initialised |
| NWN:EE install | **Optional.** Needed only for base-game tilesets/models; SWLOR hak content works without one |
| OS | Windows (the app is `WinExe`; the viewport uses OpenGL) |

```bash
git submodule update --init --recursive
```

## Build and run

Always run `dotnet` from the **repository root**:

```bash
dotnet build SWLOR.Game.Server.sln -p:RunPostBuildEvent=Never
```

```bash
dotnet run --no-build --project SWLOR.Toolset
```

## Tests

```bash
dotnet build SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build
```

900+ tests, about three minutes. A handful of corpus gates need a local NWN:EE install for base-game
data and call `Assert.Ignore` without one, so a skip is expected rather than a failure.

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
tools/SWLOR.ConversationMigrator/     — one-shot dialog migration CLI
```

References flow strictly one way:

```
{ SWLOR.Toolset, SWLOR.ConversationMigrator } → SWLOR.Toolset.Domain → { SWLOR.NWN.Formats, SWLOR.Game.Server }
```

**Logic belongs in `Domain`, not in the app project.** The test project references `Domain` only —
deliberately, so tests stay headless — so anything placed app-side is untestable. That boundary has
already cost us once: `NewAreaWriter` started in the app layer and had to be moved before its
file-writing path could be covered.

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

1. **Module root** — from `%LOCALAPPDATA%\SWLOR.Toolset\settings.json`, or auto-detected from the
   repo layout. A valid root contains `are/` and `utc/` subfolders. If neither works the status bar
   says so; set `moduleRoot` in that settings file and restart.
2. **Haks** — `SWLOR_Haks/`, layered per `Build/hakbuilder.json` (the first matching layer wins).
3. **Base game** — an NWN:EE install, auto-located or overridden in settings. Missing is fine; a
   broken install degrades to hak-only rather than failing startup.

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
- **Validate and pack** without leaving the app.

`WORKLOG.md` records what each work package did and, more usefully, *why* — including the bugs found
and the evidence behind each rule.

---

## License

First-party toolset and format-library source is licensed under the repository's MIT license.
Third-party package and replacement provenance details are recorded in
[LICENSE-NOTICE.md](LICENSE-NOTICE.md) and
[FORMAT-PROVENANCE.md](../SWLOR.NWN.Formats/FORMAT-PROVENANCE.md).
