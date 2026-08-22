# Waypoint editor — two candidate designs

Design study for the SWLOR Toolset's waypoint editor (`.utw`), in the shape the trigger editor
already ships and with none of its conventions changed:

- the header is **behavior name · kind · owner resref** (`HeaderName`/`HeaderKind`/`HeaderOwner`),
  not the object's own name;
- the tabs are Basic / Behavior, and **Variables is not a tab at all** unless the behavior is
  Custom — `ShowsVariablesTab` binds `IsVisible`, so it is absent rather than disabled. The raw
  waypoint fields formerly proposed for Advanced are rows in the Custom behavior itself;
- the Behavior rail is group headings, plain behavior names, and a rule above the ungrouped Custom
  entry — no counts, no annotations. (A `Tagline` exists on the model; no trigger behavior sets one
  and no waypoint behavior needs one.)
- the pane is one card: the behavior's name, a one-line summary, then its rows. Values the behavior
  writes on the builder's behalf are `BehaviorManagedValue`s applied silently and surfaced, where they
  are worth stating, as non-editable Statement rows — the same device `No-Spawn Zone` uses today.

The same editor serves blueprints and placements. See `Editors\Triggers` and
`TriggerEditorDesigns.md` for the pattern this follows.

**Scope.** Position and facing (`XPosition`/`YPosition`/`ZPosition`/`XOrientation`/`YOrientation`)
are placement geometry, set by dragging in the area editor — the waypoint equivalent of a trigger's
`Geometry`, and out of scope here for the same reason.

---

## 1. The field set

GFF names verified against `Module\utw\*.json` and `Module\git\*.git.json`.

| Field | GFF | Notes |
|---|---|---|
| Name | `LocalizedName` | locstring |
| Tag | `Tag` | **the only field the runtime reads** — see §2 |
| ResRef | `TemplateResRef` | file name on a blueprint; a copy on a placement |
| Category | `PaletteID` | opens the category tree |
| Appearance | `Appearance` | row in `waypoint.2da` — 1 blue, 2 red, 3 green, 4 yellow, …79 rows |
| Has Map Note | `HasMapNote` | |
| Map Note | `MapNote` | locstring |
| Map Note Enabled | `MapNoteEnabled` | |
| Variables | `VarTable` | 286 placements, 9 blueprints |
| ~~Description~~ | `Description` | present on all 3,915 placements, **non-empty on 2** |
| ~~Linked To~~ | `LinkedTo` | present on all 3,915, **non-empty on none** |
| ~~Comment~~ | `Comment` | non-empty on 2 blueprints |

`UtwSchema` already covers every one of these, `WaypointAppearanceService` already reads
`waypoint.2da`, and `IGameCodeIndex.SpawnTableIds` already scans the spawn table declarations.
Neither design below needs new format work.

## 2. What the module says about waypoints

| | |
|---|---|
| Blueprints in `Module\utw` | **217** — 76 of them placed nowhere |
| Placements across `Module\git` | **3,915** in **358** areas |
| …naming a blueprint that is not in `Module\utw` | **1,457** — 356 name nothing, 617 name base-game `nw_waypoint001`/`nw_mapnote001`, 484 name custom blueprints that no longer exist |
| Placements carrying a `VarTable` | **286** |
| Distinct trigger `LinkedTo` targets | **292**, of which **16** name a tag no waypoint and no door in the module carries |
| Door `LinkedTo` targets | **348** of **1,545** doors in **253** areas carry one (**343** distinct targets) — 287 resolve to a door tag, 34 to a waypoint tag, and 27 are dangling across 25 distinct target tags |
| Tags used by more than one placement | **174** |

Four of these drive the designs:

- **The Tag is the whole wiring.** `Spawn.cs` matches a waypoint's tag against the declared spawn
  table IDs; `GetWaypointByTag` resolves travel and respawn points by tag; a trigger or door's
  `LinkedTo` names a tag. Nothing else on a waypoint is read at runtime. Where the game declares a
  closed set of tags, a free-text box would leave the entire object unvalidated.
- **The `VarTable` is dead weight.** All 475 locals across those 286 placements belong to the
  pre-C# spawn system — `SPAWN_TABLE_ID` (stored as an **int**, always 0), `SPAWN_RESREF`,
  `IS_SPAWN`, `SPAWN_TYPE`, `SPAWN_BEHAVIOUR`. Nothing in `SWLOR.Game.Server` reads any of them off
  a waypoint; the only spawn locals the runtime reads are `RESOURCE_/CREATURE_/SLICING_TERMINAL_SPAWN_TABLE_ID`
  on the **area**. One placement family (77 of them, in `hutlar_wastes_ca`) has the spawn table ID
  typed into the *variable name* — `HUTLAR_WASTES = 0` — which is the failure mode of a raw
  variables grid, stated in data.
- **Appearance is already a role code, inconsistently.** Fishing points are green on all 431. Map
  notes, stuck points, transition destinations and travel points are blue on 925 of 958. Spawn
  points are red on 1,729 — and green on 221, colliding with fishing. Nobody chose those colours
  per waypoint; they follow from what the waypoint is, so a behavior can own them.
- **A tag can be wrong in a way nothing reports.** **149** placements — 56 distinct tags across 28
  areas — are painted the spawn colour and name a spawn table this branch does not declare, so they
  spawn nothing: `NS_CZDROIDS` ×40 (all in `nashada_czlabf2`), `creature_spawn` ×25, and a long
  tail. In the other direction, 16 trigger links and 27 door links point at 41 distinct broken
  transition targets that neither a waypoint nor a door carries — 43 broken link records in total.
  Both are one index lookup away from being visible, and neither is visible today.

## 3. The behavior catalog

Shared by both designs — nothing invented, each one is a pattern the module already uses, with its
placement count. Classification is precedence-ordered (map note first, then tag matches).

| Behavior | Group | Placements | Builder fills in | Behavior manages |
|---|---|---|---|---|
| Creature Spawn Point | Spawning | 1,952 | Spawn table (declared name plus ID) | `Tag` = table ID · Appearance = red |
| Fishing Point | Spawning | 431 | Fishing location name (31 declared) | `Tag` = `FP_…` · Appearance = green |
| Map Note | World | 376 | Map note text | `HasMapNote` = 1 · `MapNoteEnabled` = 1 · Appearance = blue |
| Stuck Rescue Point | World | 300 | nothing | `Tag` = `STUCK_WAYPOINT` · Appearance = blue |
| Transition Destination | Movement | 227 | Free-text destination tag | Appearance = blue |
| Property Entrance | Travel | 43 | nothing | `Tag` = `PROPERTY_ENTRANCE` · Appearance = blue |
| Starship Dock | Travel | 11 | nothing; planet comes from its area | `Tag` = `STARSHIP_DOCKPOINT` · Appearance = blue |
| Planet Landing | Travel | 10 | Which planet (10 declared) | `Tag` = `<PLANET>_LANDING` · Appearance = blue |
| Orbit Point | Travel | 10 | Which planet (10 declared) | `Tag` = `<Planet>_Orbit` · Appearance = blue |
| Taxi Stop | Travel | 4 | Which stop (14 declared) | `Tag` = `TAXI_…` · Appearance = blue |
| Death Respawn | Travel | 1 | Which death fallback (2 declared) | `Tag` · Appearance = blue |
| Rebuild | Travel | 2 | Enter rebuild or return to spending (2 declared) | `Tag` · Appearance = blue |
| Custom | — | 548 | Tag · Appearance · the map-note trio · Variables | — |

**Each code-declared destination is its own behavior, not one "Game Destination".** They share a
mechanism — most use a tag the C# side already declares, so those fields are pickers rather than
text boxes — but they answer different questions, carry different rules, and fail differently.
Transition Destination is the deliberate exception because builders must be able to author a new
tag before anything links to it. Rolling the declared destinations together would be the same
mistake as folding Rest Zone and No-Spawn Zone into one "script behavior" on the trigger side.

| | Declared in | Rule the behavior knows |
|---|---|---|
| Planet Landing | `PlanetType.LandingWaypointTag` | one per planet, in that planet's own areas; where the shuttle and NPC property landings arrive |
| Orbit Point | `PlanetType.SpaceOrbitWaypointTag` | one per planet, in its orbit area; where a ship arrives in space |
| Taxi Stop | `TaxiDestinationType` | one per stop; the picker shows the destination name |
| Starship Dock | `Space.cs`, `StarportLayoutDefinition` | repeats — one per starport interior; `Planet.GetPlanetType(area)` determines its planet and the runtime stamps `STARSHIP_DOCKPOINT_ID` |
| Property Entrance | `Property.cs`, `Shuttle.cs` | repeats — one per property interior layout |
| Death Respawn | `Death.cs`, `PlayerInitialization.cs` | two death fallbacks; module-wide singletons |
| Rebuild | `PersistentLocation.cs`, `PlaceableScripts.cs`, `CharacterFullRebuildViewModel.cs` | two rebuild destinations; module-wide singletons |

Two things fall out of the split that a merged behavior would have hidden:

- **10 of the 14 declared taxi stops have no waypoint anywhere in `Module\git`** — every Veles stop,
  from `TAXI_VELES_ENTRANCE` to `TAXI_VELES_APARTMENT`. Only the four Dantooine stops are placed. A
  Taxi Stop picker that lists the declared stops shows that at a glance; a free tag box never can.
- **`Death.cs` looks up two different respawn tags** — `DEATH_DEFAULT_RESPAWN_POINT` and
  `DTH_DEFAULT_RESPAWN_POINT` — and only the second is placed. Worth a code fix, and the Death
  Respawn picker is what surfaces it. Rebuild has its own picker and cannot offer either death tag.

Notes the catalog carries into the editor:

- **Uniqueness is per behavior, not global.** `GetWaypointByTag` returns one object, so a second
  `VISCARA_LANDING` is a silent bug — but `STUCK_WAYPOINT` (×300) and `PROPERTY_ENTRANCE` (×43) are
  resolved with `GetNearestObjectByTag` and are *meant* to repeat, one per area. Each behavior states
  its own rule; a global "tag must be unique" check would be wrong 343 times.
- **Custom** is the unclassified remainder, including the 48 placements that carry only legacy spawn
  locals. 127 of them are `WP_*` route markers inherited from imported areas.

---

## Design A — Behavior owns the tag

**Idea.** The trigger editor, one for one. **Tag leaves the Basic tab**: declared behaviors write it
from a picker, while Transition Destination exposes it as required free text so a builder can create
a new destination before any trigger or door refers to it. What a behavior writes is stated back as
a Statement row. Custom owns the raw Tag, Appearance and map-note fields directly.

```text
┌ malfdroids_cz220.utw ───────────────────────────────────── ● unsaved ┐
│ Creature Spawn Point   blueprint · malfdroids_cz220                  │
├──────────────────────────────────────────────────────────────────────┤
│  Basic │ ┌Behavior─┐                                                 │
├────────────────────┴─────────────────────────────────────────────────┤
│ SPAWNING                   │ ┌ Creature Spawn Point ───────────────┐ │
│  ▸ Creature Spawn Point ◂  │ │ Spawns a table's creatures here and │ │
│  ▸ Fishing Point           │ │ respawns them on its own timer.     │ │
│ WORLD                      │ │                                     │ │
│  ▸ Map Note                │ │ Spawn table *                       │ │
│  ▸ Stuck Rescue Point      │ │  [CZ-220 Droids (CZ220_DROIDS) ▾]   │ │
│ MOVEMENT                   │ │   malsecdroid 50 · malspiderdroid 50│ │
│  ▸ Transition Destination  │ │                                     │ │
│ TRAVEL                     │ │ Tag         CZ220_DROIDS            │ │
│  ▸ Planet Landing          │ │   written from the table above      │ │
│  ▸ Orbit Point             │ │ Marker      red                     │ │
│  ▸ Taxi Stop               │ └─────────────────────────────────────┘ │
│  ▸ Starship Dock           │                                         │
│  ▸ Property Entrance       │                                         │
│  ▸ Death Respawn           │                                         │
│  ▸ Rebuild                 │                                         │
│  ──────────────────────    │                                         │
│  ▸ Custom                  │                                         │
├────────────────────────────┴─────────────────────────────────────────┤
│                       [ Revert ]  [      Save      ]                 │
└──────────────────────────────────────────────────────────────────────┘

Leave the picker empty and the footer says so, in the trigger editor's own words:
"Creature Spawn Point still needs Spawn table."

Map Note                               Taxi Stop
┌────────────────────────────────────┐  ┌────────────────────────────────────┐
│ Draws a pin on the area map.       │  │ Where a taxi terminal sets the     │
│                                    │  │ player down when they pay.         │
│ Map note text *                    │  │                                    │
│  [Veles Colony Starport        ]   │  │  [Dantooine Starport          ▾]   │
│ Shown on map               Always  │  │ Tag        TAXI_DANTOOINE_STARPORT │
│ Map note flag   set                │  │   10 of the 14 declared stops      │
│ Marker          blue               │  │   have no waypoint at all          │
└────────────────────────────────────┘  └────────────────────────────────────┘

Basic tab                             Custom behavior
  Name      [CZ-220 - Malfunctio… …]    Appearance  [red          ▾]
  ResRef    [malfdroids_cz220      ]    Tag         [              ]
  Category  [Waypoints ▸ Spawns   …]    Has Map Note [ ]
                                        Map Note     [              ]
                                        Shown on Map [ ]
```

**Wins.** The smallest thing that fixes the real error: the tag becomes a value picked from what the
game code declares, so `NS_CZDROIDS` cannot be typed and the 40 existing ones surface the moment
their waypoint is opened. Appearance stops being a per-waypoint decision, which retires the
red/green spawn split. Identical machinery to the trigger editor — a `WaypointBehaviorCatalog`, a
classifier, and the existing row view models — so it is mostly catalog data.

**Loses.** It answers "what is this waypoint" and not "is anything still using it", which is the
question a waypoint actually raises: 484 placements already name deleted blueprints and 41 distinct
transition targets already dangle (16 trigger targets and 25 door targets, referenced by 43 linking
objects), and this design shows neither. That makes the case for a Used-by card stronger, not weaker.
Taking Tag off Basic is a real departure from both the base toolset and the trigger editor, and it
puts the editor's correctness entirely in the classifier: a waypoint it misreads drops to Custom,
where the fields are raw again.

**Cost.** Small.

---

## Design B — Behavior, plus what points here

**Idea.** Same rail and same card, with a second card under it: **Used by** — the triggers and doors
whose `LinkedTo` names this tag, the C# call sites, what the chosen spawn table actually spawns, and
the other placements sharing the tag. Tag stays on the Basic tab where triggers keep it; the
behavior *resolves and validates* it rather than owning it, and a rename states its blast radius
before it happens.

```text
┌ v_repubbase_ext · waypoint ─────────────────────────────── ● unsaved ┐
│ Transition Destination   instance · v_repubbase_ext                  │
├──────────────────────────────────────────────────────────────────────┤
│  Basic │ ┌Behavior─┐                                                 │
├────────────────────┴─────────────────────────────────────────────────┤
│ SPAWNING                   │ ┌ Transition Destination ─────────────┐ │
│  ▸ Creature Spawn Point    │ │ Where a trigger or door sends the   │ │
│  ▸ Fishing Point           │ │ player during an area transition.   │ │
│ WORLD                      │ │                                     │ │
│  ▸ Map Note                │ │ Tag         WP_V_RepBaseExt_to_WFork│ │
│  ▸ Stuck Rescue Point      │ │   ✓ 2 links resolve here — set it   │ │
│ MOVEMENT                   │ │     on the Basic tab                │ │
│  ▸ Transition Destination ◂│ │ Marker      blue                    │ │
│ TRAVEL                     │ └─────────────────────────────────────┘ │
│  ▸ Planet Landing          │ ┌ Used by ────────────────────────────┐ │
│  ▸ Orbit Point             │ │ Trigger + door links here      2    │ │
│  ▸ Taxi Stop               │ │   viscara_forkwest V_WFork_to…      │ │
│  ▸ Starship Dock           │ │   viscara_mountasc V_WFork_to…      │ │
│  ▸ Property Entrance       │ │ Game code references           0    │ │
│  ▸ Death Respawn           │ │ Other placements, this tag     0    │ │
│  ▸ Rebuild                 │ │                                     │ │
│  ──────────────────────    │ │                                     │ │
│  ▸ Custom                  │ │ Renaming the tag breaks both.       │ │
│                            │ │ [ Rename and update them ]          │ │
│                            │ └─────────────────────────────────────┘ │
├────────────────────────────┴─────────────────────────────────────────┤
│                       [ Revert ]  [      Save      ]                 │
└──────────────────────────────────────────────────────────────────────┘

Creature Spawn Point, tag typed rather than picked
┌───────────────────────────────────────┐ ┌────────────────────────────────┐
│ Spawn table *                         │ │ Declared in                    │
│  [NS_CZDROIDS                      ▾] │ │   — no such spawn table —      │
│   ⚠ No spawn table is declared with   │ │ Other placements, this tag  39 │
│     this ID. This waypoint spawns     │ │   nashada_czlabf2 — all 39,    │
│     nothing.                          │ │   same problem                 │
│   Did you mean CZ220_DROIDS?          │ │ [ Show them ]                  │
└───────────────────────────────────────┘ └────────────────────────────────┘
```

**Wins.** It answers the question the corpus keeps asking. A bad tag is not merely refused, it is
explained and counted — one open of an `NS_CZDROIDS` waypoint tells you the other 39 are broken the
same way. Renaming a transition destination stops being the operation nobody dares do, which matters
here for the same reason a scoped Update Instances mattered on triggers: the 41 already-dangling
transition targets (16 trigger and 25 door targets, referenced by 43 linking objects) are what an
unassisted rename produces. That makes the argument for the Used-by card stronger, not weaker.
Leaving Tag on Basic also means a waypoint the classifier reads wrongly is still fully editable in
place.

**Loses.** The most build for the least layout novelty — the Used-by card needs a module-wide index
of trigger and door `LinkedTo`, waypoint tags and code-declared tags, and rename-and-update has to
write to other areas' `.git` files, which no editor in the toolset does today. On the 2,000-odd spawn
points the card mostly restates the picker above it, so the second card earns its space on a minority
of waypoints. And a tag typed on Basic can still be saved wrong, where Design A's picker cannot.

**Cost.** Medium.

---

## Comparison

| | A · Behavior owns the tag | B · Behavior + what points here |
|---|---|---|
| Behavior rail and managed values | Yes | Yes |
| Tag | Written or edited by the behavior | On Basic, resolved by the behavior |
| Bad spawn table ID | Impossible to enter | Flagged, counted across the module |
| Dangling transition links | Invisible | Listed, and rename can fix them |
| Cross-area writes | None | Needed for rename-and-update |
| Build cost | Small | Medium |

## Recommendation

**A now, B's Used-by card next** — the same order the trigger editor took.

A is the whole win on data entry: declared spawn, fishing and travel tags come from named
`IGameCodeIndex` choices, Transition Destination keeps the free-text field needed to introduce a new
tag, and Appearance stops being a per-waypoint guess. It is catalog data over machinery that already
exists.

B is the whole win on *existing* content, and the module needs it — 484 placements name deleted
blueprints, 40 spawn nothing, and 41 distinct transition targets dangle (16 trigger targets and 25
door targets, referenced by 43 linking objects). That makes the case for B stronger, not weaker. But
it is a module-wide index plus cross-area writes, and rename-and-update should land **before** any
rename affordance does, never after.

Two implementation requirements belong to whichever ships first:

- **Spawn choices need names, not raw IDs.** `SourceIdScanner` resolves the one-level helper shape
  used by all 31 fishing tables, and the waypoint index pairs declared spawn IDs with their
  programmer-facing names. Pickers show the friendly name together with the stored tag.
- **Custom must warn about the legacy locals rather than inherit them.** 286 placements carry
  variables no runtime reads. Editing one under Custom should say so; silently preserving them keeps
  authoring the `HUTLAR_WASTES = 0` mistake forward.
