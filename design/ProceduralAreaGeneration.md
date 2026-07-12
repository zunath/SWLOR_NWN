# Procedural Area Generation — Implementation Plan

Branch: `feature/procedural-areas` (based on `feature/combat-upgrade`). Worktree: `C:\Projects\SWLOR_procedural-areas`.

## Decisions (locked)

| Decision | Choice |
|---|---|
| Generation approach | True tile-level synthesis (constraint solver over tileset `.set` data) |
| Runtime model | Generic library — consumers include on-demand instances, boot-time generation, future uses |
| Persistence | Pluggable strategies: Ephemeral, Seed-Persisted (deterministic regen), Full Area Export |
| Failure posture | Bounded retries with validation; abort cleanly (no fallback content pipeline) |
| Difficulty | Tiered spawn/loot tables (no runtime stat mutation — preserves Bible NPC balance parity) |
| Engine integration | NWNX_Tileset plugin (primary), base-game `SetTileJson` retained as documented contingency |
| First theme | Mine/Cave — `tdt01` (`sw_t_minecave`) |

## Engine facts this plan is built on

- **No from-scratch area creation exists.** `CreateArea(resref)` only clones a toolset-authored module template ([AreaFunctions.cs:388](../SWLOR.NWN.API/NWScript/AreaFunctions.cs)). Runtime instances get auto-generated resrefs (`nw_5`), and transitions are never auto-relinked.
- **`SetTile` / `SetTileJson` / `GetTileID` / `ReloadAreaBorder` / `ReloadAreaGrass` are already wrapped** in `SWLOR.NWN.API/NWScript/AreaFunctions.cs:851-935`. `SetTileJson` bulk-writes `{index, tileid, orientation, height, animloop1-3}` objects and can even re-tileset an area.
- **Live tile edits in occupied areas are walkmesh-risky.** NWNX docs state tiles/walkmesh reliably update "on area load" (see `NWNX_Area_RotateArea` caveat). Rule: **all geometry writes complete before any player enters.** Single-tile `SetTile` is documented as walkmesh-safe live, but the generator never needs it.
- **NWNX_Tileset** (present in `zunath/nwn-dotnet:8193.37.15-1`, currently `NWNX_TILESET_SKIP=y` in `debugserver/swlor.env:70`; SWLOR has **no wrapper yet**) provides:
  - `CreateTileOverride(name, tileset, w, h)` (1–32 per axis) + `SetOverrideTileData(name, index, data)` — define an entire tile grid.
  - `SetAreaTileOverride(areaResref, overrideName)` — bind the override to a placeholder area resref; instances of that resref load with the override grid.
  - Read APIs: `GetTilesetData`, `GetTilesetTerrain`, `GetTileEdgesAndCorners`, `GetTileDoorData`, `GetTileModel` — live tileset adjacency introspection, exactly what the solver needs.
- **`NWNX_Area_GetPathExists(area, start, end, depth)`** validates traversability via tile path nodes ([AreaPlugin.cs:660](../SWLOR.NWN.API/NWNX/AreaPlugin.cs)) — the post-generation gate.
- **Three boot-time caches ignore runtime areas** and must be handled explicitly:
  - `Service/Area.cs` `AreasByResref` — instances are deliberately excluded (property pattern).
  - `Service/Spawn.cs` — registers spawns only for boot-time areas; property system works around this with `OnSpawnAction` callbacks.
  - `Service/Walkmesh.cs` — keyed by resref; runtime areas miss and `GetRandomLocation` silently returns the area origin.
- **Existing lifecycle patterns to reuse:** `Property.cs` job-queue area creation (budgeted across scheduler ticks, `PropertyLoadState` machine) and `Shuttle.cs` teardown (evacuate players → `DestroyArea` → retry on `-2` after 30s).
- **Community state of the art** is template pools / geomorph chunk assembly; no one has shipped a true tile-level generator. This plan is first-of-its-kind territory — hence the spike-first milestone order.

## Architecture

Layered library under `Service/AreaGenerationService/`, exposed by a `Service/AreaGeneration.cs` facade. No consumer-specific logic in the core layers.

### 1. Tileset data layer
`TilesetDataCache` builds an adjacency model per tileset, cached at module load:
- Per tile ID: 4 corner terrain labels (+ corner heights), 4 edge crosser labels, path node, door slots, group membership.
- Orientations 0–3 derived by rotating the corner/edge arrays — one canonical record per tile, four views.
- Primary source: NWNX_Tileset read APIs. Secondary/offline source: parse the `.set` INI directly (`ResManGetFileContents` at runtime; direct file read in unit tests against `SWLOR_Haks/sw_t_minecave/tdt01.set`). Both feed the same model, so solver unit tests run without a game server.

### 2. Layout solver (pure C#, deterministic)
Two stages — deliberately *not* free-form WFC, so global connectivity is guaranteed by construction rather than by luck:
1. **Macro layout**: generate a room-and-corridor graph on a corner-granularity grid (cellular automata / drunkard's walk for caves; BSP rooms for facility themes later). Output: a terrain label per grid corner (e.g. `Rock` / `Floor`), room metadata (centers, bounds, role tags: entrance, boss, treasure), and the connectivity graph.
2. **Tile resolution**: for each cell, pick a (tileID, orientation) whose four corners match the corner-terrain map — the same corner-matching model the toolset terrain brush uses. Weighted random among candidates for cosmetic variety. Contradiction ⇒ local backtrack; exhausted ⇒ reseed retry.

All randomness flows from one seeded RNG in the `GenerationRequest` — required for the Seed-Persisted strategy. Solving is pure C# and can run on a worker thread (`Task.Run`); only engine calls return to the main thread.

Scope discipline for v1 on `tdt01`: single height level, no crossers, no groups. Height transitions, water/lava terrain, and group prefabs are explicit later increments.

### 3. Area realization
`AreaSynthesizer` turns a solved layout into a live area:
1. Build override: `CreateTileOverride` → `SetOverrideTileData` per index → `SetAreaTileOverride(placeholderResref, override)`.
2. `CreateArea(placeholderResref)` → capture instance handle.
3. Unbind/rotate the override binding.
4. Create doors for any tile door slots (`UtilPlugin.CreateDoor` + `.set` door data) and entrance/exit transitions (`AreaPlugin.CreateTransition`, `SetTransitionTarget`).

**Concurrency rule:** override binding is keyed by *resref*, so creation is serialized through a generation job queue (mirror the property job pattern). Module ships a small pool of blank placeholder areas (one per size class) on the target tileset.

**Contingency path** (kept documented, shares the same layout model): `CreateArea` a blank max-size template + one `SetTileJson` call before entry. If the M0 spike falsifies NWNX_Tileset, only this layer changes.

### 4. Validation gate
After realization, before any player entry:
- `NWNX_Area_GetPathExists` from entrance to every room center (depth ≈ width×height).
- Tile-ID readback spot checks (`GetTileID`) against the solved layout.
- Failure ⇒ destroy instance, retry with next seed (bounded, e.g. 3 attempts) ⇒ abort cleanly: consumer gets a failure result, entrance reports unavailable, loud Serilog error. No silent degradation.

### 5. Runtime wiring
`RuntimeAreaRegistry` — parallel registry for generated instances (id → handle, layout metadata, consumer, persistence strategy). Explicitly *not* added to `Area.AreasByResref` (matches property-instance convention).
- **Walkable points**: computed from the solved layout (floor tiles are known) + `GetGroundHeight` sampling after realization — a runtime-area provider alongside `Walkmesh`, so random-location spawning works. Never rely on the boot-time bake.
- **Spawns/loot**: `DungeonContentBuilder` consumes room metadata and the requested tier, resolving through existing `SpawnTable`/`LootTable` systems (property `OnSpawnAction` pattern — no dependence on `Spawn.cs` boot registration).
- **Teardown**: evacuate players to the bound exit location → `DestroyArea` → retry on `-2` (shuttle pattern) → `DeleteTileOverride` → unregister. Idle-timeout destruction for abandoned instances.

### 6. Persistence strategies
`IAreaPersistenceStrategy` chosen per `GenerationRequest`:
- **Ephemeral**: nothing persisted. Verified complete by existing infrastructure: `PersistentLocation.SaveLocation` never saves positions inside areas absent from `Area.AreasByResref` (all runtime instances), so players inside a generated dungeon at restart load at their last saved outside location automatically.
- **SeedPersisted**: persist `{seed, theme, tier, size, consumerId}` as a Redis entity; regenerate deterministically on boot or first access; players resume inside. Depends on solver determinism (guard with a golden-seed regression test).
- **FullExport**: `NWNX_Area_ExportARE/ExportGIT` to the NWNX alias, reload as a real resource. Heaviest; documented caveats (local-variable object refs don't survive restarts). Implemented last; API shape reserved from day one.

### 7. Consumer API
```
AreaGeneration.Request(new GenerationRequest {
    Theme, Tier, SizeClass, Seed?, Persistence, ConsumerId
}) → queued job → OnGenerated(instanceHandle) / OnFailed(reason)
```
- **On-demand consumer**: dungeon entrance device/dialog → tier selection → generate → open transition (first shipped use).
- **Boot-time consumer**: generate while area is empty during module load (rotating world dungeon — proves the "generic" requirement with a second caller).
- **Theme definitions** as `Feature/DungeonDefinition/` builder classes (project convention): tileset, size ranges, macro-layout algorithm + parameters, tier → spawn/loot table mappings, objective templates.

## Gameplay loop (first shipped dungeon)
Mine/Cave (`tdt01`) entrance on a suitable planet → party selects tier → instance generates (rooms: entrance, 3–6 combat/treasure rooms, boss room) → tiered spawn tables populate enemies from existing Bible-balanced NPCs (no new NPC balance work in v1) → boss/objective room → loot → exit portal → instance destroyed when empty. DM chat command to generate a dungeon by seed/theme/tier for testing.

## Risks, ordered (each has a milestone that retires it)
1. **NWNX_Tileset is unproven here** — plugin has never been enabled on SWLOR; behavior at 8193.37 (walkmesh, minimap, lighting, doors on override tiles) unverified. → M0 spike before anything else.
2. **Solver output quality** on real tileset data (corner-matching completeness of `tdt01`'s tile inventory). → M2 solve-rate tests over many seeds, offline.
3. **Override-binding concurrency** under simultaneous requests. → serialize via job queue; M3 load test with concurrent requests.
4. **Walkmesh/pathing correctness** for players in generated geometry. → hard `GetPathExists` gate + M0/M3 in-game walkthroughs.
5. **Memory/instance growth** (leak history fixed in 8193.31, but monitor). → Profiler plugin metrics + idle teardown; M6 soak test.
6. **Determinism drift** breaking SeedPersisted. → golden-seed regression tests in CI.

## Offline review workflow

`dotnet run --project tools/ProcgenReview` builds a standalone `Module/SWLOR Procgen Review.mod`
containing offline-generated areas for every registered dungeon theme (default: seeds 4242 and 777,
16x16; override with `--seeds a,b,c --size N --out <path>`). It uses the production solver and each
theme's real tileset/lighting/placeholder settings via linked sources, so the review module always
matches runtime behavior. Paths derive from the repository root, so it runs on any machine/drive;
point nwn.ini's MODULES directory at `<repo>/Module` (the dev convention) and the toolset sees the
output directly. Note: offline generation skips engine path validation — the review module is for
visual inspection, not traversal QA.

## Milestones
- **M0 — Spike (riskiest first)**: flip `NWNX_TILESET_SKIP=n` (debugserver + note for prod env), write `SWLOR.NWN.API/NWNX/TilesetPlugin.cs` wrapper, hand-author a 4×4 override on `tdt01`, bind + `CreateArea`, walk it in-game. Verify: geometry, walkmesh, minimap, lighting, `GetPathExists`, door slots, teardown. Go/no-go on the NWNX path (contingency: `SetTileJson`).
- **M1 — Tileset data model**: `.set` parser + NWNX-backed provider + adjacency cache for `tdt01`; unit tests against the real `.set` file.
- **M2 — Layout solver**: macro cave generator + tile resolver, seeded/deterministic; offline tests for connectivity, solve rate, golden seeds.
- **M3 — Realization + wiring**: synthesizer, job queue, registry, transitions, walkable-point provider, validation gate, teardown. Deliverable: walk a freshly generated cave in-game.
- **M4 — Content loop**: `DungeonDefinition` builder, tiered spawn/loot tables, boss/objective room, exit flow. Deliverable: full playable run.
- **M5 — Persistence + consumers**: Ephemeral + SeedPersisted strategies, entrance device (on-demand) + boot-time rotating dungeon. FullExport if time permits, else stubbed behind the interface.
- **M6 — Hardening**: concurrency/soak tests, metrics, DM tooling, failure-path polish, docs.

## File layout
```
SWLOR.NWN.API/NWNX/TilesetPlugin.cs
SWLOR.Game.Server/Service/AreaGeneration.cs                  (facade)
SWLOR.Game.Server/Service/AreaGenerationService/
    TilesetDataCache.cs  TilesetSetParser.cs  TileAdjacency.cs
    LayoutSolver.cs  MacroLayout.cs  TileResolver.cs
    AreaSynthesizer.cs  GenerationJob.cs  RuntimeAreaRegistry.cs
    RuntimeWalkableProvider.cs  DungeonContentBuilder.cs
    Persistence/ (IAreaPersistenceStrategy + Ephemeral/SeedPersisted/FullExport)
SWLOR.Game.Server/Feature/DungeonDefinition/MineCaveDungeonDefinition.cs
SWLOR.Game.Server.Tests/AreaGeneration/                       (parser, solver, determinism)
Module/are|git: placeholder pool areas (blank tdt01, per size class)
debugserver/swlor.env: NWNX_TILESET_SKIP=n
```
Naming stays domain-based (AreaGeneration, DungeonDefinition) per project rules — no initiative labels.
