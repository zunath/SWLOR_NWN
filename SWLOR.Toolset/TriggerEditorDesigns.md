# Trigger editor — four candidate designs

> **Superseded.** Design A was built and then replaced: the trigger editor now follows the placeable
> editor — a tabbed editor whose **Behavior** tab picks what the trigger is for, with that
> behavior's own fields and a "what this behavior manages" block. **Local variables are reachable
> only under Custom**; every other behavior owns its locals and exposes them as named fields. The
> same editor serves blueprints and placements. See the `Editors\Triggers` folders and the
> behavior-editor entry in `WORKLOG.md`.
>
> This file is kept as the record of what was weighed, not as a description of what exists. The two
> findings that still drive the code are below: **Update Instances cannot ship before it can be
> scoped** (42 of 188 placements carry their own locals), and script slots are the real error
> surface.

Design study for the SWLOR Toolset's trigger blueprint editor (`.utt`).

**Scope, per direction:** this editor edits a **reusable blueprint**. The trigger's dimensions —
the `Geometry` polygon — are drawn in the area editor when the trigger is placed, not here. The
same goes for the per-placement transition target (`LinkedTo` / `LinkedToFlags`). All four designs
below follow the base NWN toolset's Trigger Properties dialog.

**The Comments tab is dropped.** `Comment` is written as an empty string on every trigger the
editor saves. Consequence worth knowing: exactly one of the seven existing blueprints carries a
comment today — `anti_spawn_trigg` ("Prevents random resource spawns from spawning within the
trigger.") — and it will be blanked the first time that blueprint is edited.

---

## 1. The field set

From the base dialog, minus Comments. GFF names verified against `Module\utt\*.json`.

| Tab | Field | GFF | Notes |
|---|---|---|---|
| Basic | Name | `LocalizedName` | locstring, with the `…` editor |
| Basic | Tag | `Tag` | |
| Basic | Trigger Type | `Type` | 0 Generic · 1 Area Transition · 2 Trap |
| Basic | Category | `PaletteID` | `…` opens the category tree |
| Scripts | OnClick | `OnClick` | |
| Scripts | OnEnter | `ScriptOnEnter` | |
| Scripts | OnExit | `ScriptOnExit` | |
| Scripts | OnHeartbeat | `ScriptHeartbeat` | |
| Scripts | OnUserDefined | `ScriptUserDefine` | |
| Scripts | OnDisarm / OnTrapTriggered | `OnDisarm`, `OnTrapTriggered` | proposed: shown only for Type = Trap |
| Advanced | Auto-Remove Key | `AutoRemoveKey` | |
| Advanced | Key Tag | `KeyName` | |
| Advanced | Faction | `Faction` | from `repute.fac` |
| Advanced | Highlight Height | `HighlightHeight` | |
| Advanced | Blueprint ResRef | `TemplateResRef` | + Update Instances |
| Advanced | Cursor | `Cursor` | |
| Advanced | Portrait | `PortraitId` | |
| Advanced | Variables | `VarTable` | |
| Trap | seven trap fields | `TrapFlag`, `TrapDetectable`, `TrapDetectDC`, `TrapDisarmable`, `DisarmDC`, `TrapOneShot`, `TrapType` | proposed: a tab that exists only for Type = Trap |

The toolset already has `UttSchema` covering every one of these, `EditorKind.ScriptSlot`,
`LookupKeys.Factions` wired to `repute.fac`, and a VarTable section with known-key completion. What
none of the designs need is new format work.

## 2. What the module says about trigger blueprints

| | |
|---|---|
| Blueprints in `Module\utt` | **7** |
| …of which are placed anywhere | **5** (`badge_trigger` and `space_encounter` are unplaced) |
| Placements of those 5 blueprints | **188** |
| Placements whose fields **differ** from their blueprint | **46** |
| …of that drift, in `VarTable` | **42** — all 41 exploration triggers carry a per-placement `DISPLAY_TEXT`, plus one quest trigger |
| …in `LocalizedName` | **4** |
| Registered `[NWNEventHandler]` script names available to pick from | **512** consts, **475** wired |
| Blueprints with a non-empty `Comment` | **1** |

Two of these drive design decisions:

- **A naive "Update Instances" is destructive here.** 42 of 188 placements carry per-placement
  locals that a blanket push would overwrite — including every exploration trigger's message.
  Whatever the button does, it cannot be "copy the blueprint over the instance."
- **Script slots are guessable, not typed.** 512 known handler names exist in `ScriptName.cs`, and
  today the editor renders a script slot as a plain text box. A wrong resref fails silently at
  runtime.

---

## Design A — Faithful modal dialog

**Idea.** Reproduce the base dialog: a modal, three tabs (Basic / Scripts / Advanced), the same
label-column layout, the same field order, OK / Cancel. Double-click a trigger in the palette and
the dialog opens over the shell. Fields that don't apply are greyed rather than hidden, exactly as
base does. Nothing is added and nothing is interpreted.

```text
┌ Trigger Properties ─────────────────────────────────────────── ✕ ┐
│ ┌Basic─┐ Scripts  Advanced                                       │
│ ├──────┴───────────────────────────────────────────────────────┐ │
│ │ Name          │ No Spawn Zone                            [ …]│ │
│ │ Tag           │ anti_spawn_trigg                            │ │
│ │ Trigger Type  │ Generic                                  ▾ ░ │ │
│ │ Category      │ Generic Trigger                          [ …]│ │
│ │               │                                             │ │
│ └───────────────┴─────────────────────────────────────────────┘ │
│                                          [   OK   ]  [ Cancel ] │
└──────────────────────────────────────────────────────────────────┘

Scripts tab                              Advanced tab
  OnClick        [            ▾][…][Edit]  Auto-Remove Key  [ ]
  OnEnter        [            ▾][…][Edit]  Key Tag          [        ]
  OnExit         [            ▾][…][Edit]  Faction          [Hostile ▾]
  OnHeartbeat    [            ▾][…][Edit]  Highlight Height [0.0    ⇅]
  OnUserDefined  [            ▾][…][Edit]  Blueprint ResRef [anti_spawn_trigg]
                                                            [Update Instances]
        [ Load Script Set ]                Cursor           [Unclickable      ▾ ░]
        [ Save Script Set ]                Portrait         [ img ] [Select Portrait]
                                           Variables        […]
```

**Wins.** Zero relearning for anyone who has used Aurora — same tab positions, same field order,
same words. It is the smallest possible spec: the schema already exists, so this is a view over
`UttSchema` with a tab strip. Reviewable against the real dialog field by field.

**Loses.** A modal fights the toolset's own model: every other blueprint opens as a document tab
with Ctrl+S and shell-level undo. OK / Cancel introduces a second, conflicting notion of "commit".
It also faithfully reproduces base's dead weight — Portrait (0 on all 524 triggers), Key Tag and
Auto-Remove Key (default on all 524) — and leaves script slots as blind text boxes, which is where
the real errors come from.

**Cost.** Small.

---

## Design B — Same three tabs, fields that know things

**Idea.** Keep the base layout exactly — three tabs, same order, same labels — but open it as a
document tab and make every field that can resolve something resolve it. The shape is Aurora's;
the content is SWLOR's.

```text
Scripts tab, OnEnter open
┌──────────────┬─────────────────────────────────────────────────┐
│ OnClick      │                                            ▾    │
│ OnEnter      │ explore_trigger                            ▾    │
│              │ ┌─────────────────────────────────────────────┐ │
│              │ │ explore_trigger      ExplorationTrigger.cs  │ │
│              │ │ rest_trg_enter       RestTrigger.cs         │ │
│              │ │ quest_trigger        Quest.cs:568           │ │
│              │ │ cuts_speeder         (module .nss)          │ │
│              │ │ …475 registered handlers · type to filter   │ │
│              │ └─────────────────────────────────────────────┘ │
│ OnExit       │                                            ▾    │
└──────────────┴─────────────────────────────────────────────────┘

Basic tab                             Advanced tab
  Name      [Exploration Trigger  …]    Faction          [Commoner        ▾]
  Tag       [exploration_trigger    ]   Highlight Height [3.0            ⇅]
  Type      [Generic               ▾]   Blueprint ResRef [badge_trigger001]
  Category  [Triggers ▸ Exploration …]                   [Update Instances]
                                        Cursor           [Default         ▾]
  ⚠ DISPLAY_TEXT is set per placement — leave blank here
                                        Variables
                                          DISPLAY_TEXT  string  ""
                                          [name ▾] = [type ▾] [value] [Set]
```

Changes from A, all inside the same layout:

- **Script slots become pick-lists** over the 512 registered handler names, showing where each is
  implemented, filterable, and free-text-able for module `.nss`. An unknown name is flagged, not
  silently accepted.
- **Category is the toolset's category tree**, not a text box.
- **Variables are inline** on the Advanced tab with known-key completion, instead of behind `…`.
- **Portrait, Key Tag and Auto-Remove Key are dropped** — default on all 524 placements and all 7
  blueprints. Cursor and Faction stay, as real dropdowns.
- **Load / Save Script Set is dropped**; SWLOR's script sets are C# handlers, not files a builder
  assembles.

**Wins.** Same muscle memory, far fewer ways to be wrong. The script picker alone removes the most
common silent failure. Fits the toolset's document/undo/save model.

**Loses.** Not literally the base dialog any more — a builder comparing side by side will find
fields missing. The dropped fields are a judgement call that has to be right; if a future trigger
ever needs a portrait or a key, they have to come back.

**Cost.** Small–medium. The script index is a `SourceIdScanner`-shaped pass over `ScriptName.cs`.

---

## Design C — Tabs plus a Placements rail

**Idea.** The blueprint is reusable, so make the reuse visible. Keep B's three tabs and add a
permanent right rail listing every placement of this blueprint across the module, with **Update
Instances** turned into a scoped, previewed operation instead of a blind push.

```text
┌ badge_trigger001 ──────────────────────────┬ Placements ──────────────┐
│ Basic │ Scripts │ Advanced                 │ 41 in 22 areas           │
│                                            │ ┌──────────────────────┐ │
│  Name      [Exploration Trigger        …]  │ │ Area          Tag    │ │
│  Tag       [exploration_trigger          ] │ │ korr_waste…   expl_1 │ │
│  Type      [Generic                    ▾]  │ │ dath_tranj…   expl_2 │ │
│  Category  [Triggers ▸ Exploration     …]  │ │ tat_moseis…   expl_3 │ │
│                                            │ │ …                    │ │
│                                            │ └──────────────────────┘ │
│                                            │ 41 override VarTable     │
│                                            │ [Open placement]         │
└────────────────────────────────────────────┴──────────────────────────┘

Update Instances — scoped, previewed
┌──────────────────────────────────────────────────────────┐
│ Push which fields to 41 placements?                      │
│  ☑ Name              4 placements would change           │
│  ☑ Scripts           0 placements would change           │
│  ☐ Variables        41 placements carry their own —      │
│                     pushing would erase DISPLAY_TEXT     │
│  ☑ Faction, Cursor, Highlight Height                     │
│                                                          │
│ 4 placements in 4 areas change. [Preview diff] [Push]    │
└──────────────────────────────────────────────────────────┘
```

The rail also states the two facts a blueprint editor can uniquely know: **this blueprint is placed
nowhere** (true for `badge_trigger` and `space_encounter`), and **N placements have drifted from
it**.

**Wins.** Makes the thing the direction is built on — reusability — legible. Turns Update Instances
from a button nobody dares press into one with a preview, which matters concretely: an unscoped
push today would erase the message on all 41 exploration triggers. Finds unplaced blueprints for
free.

**Loses.** The most build for the least layout novelty — the rail needs a module-wide placement
index and a field-level diff. Wider window, so the dialog stops being a dialog. The rail is dead
weight on a blueprint with one placement.

**Cost.** Medium. The index is one pass over every `git`, which the workspace already does to build
catalogs.

---

## Design D — One page, no tabs

**Idea.** Same field set and same order as base, but drop the tab strip for the toolset's existing
blueprint-editor pattern: a scrolling column of collapsible groups, identical in shape to the
`.utc` / `.utp` / `.uti` editors. A trigger stops being a special case of the tool.

```text
┌ badge_trigger001.utt ────────────────────────────────────────────┐
│ Exploration Trigger              badge_trigger001    Generic     │  ← sticky
├──────────────────────────────────────────────────────────────────┤
│ ▾ Basic                                                          │
│     Name       [Exploration Trigger                         …]   │
│     Tag        [exploration_trigger                           ]  │
│     Type       [Generic                                     ▾]   │
│     Category   [Triggers ▸ Exploration                      …]   │
│ ▾ Scripts                                                        │
│     OnEnter    [explore_trigger                             ▾]   │
│     OnExit     [                                            ▾]   │
│     OnClick · OnHeartbeat · OnUserDefined                        │
│ ▸ Trap                              hidden — Type is not Trap    │
│ ▾ Advanced                                                       │
│     Faction [Commoner ▾]  Highlight Height [3.0 ⇅]  Cursor [ ▾]  │
│     Blueprint ResRef  badge_trigger001     [Update Instances]    │
│ ▾ Local Variables                                                │
│     DISPLAY_TEXT   string   ""      ← set per placement          │
└──────────────────────────────────────────────────────────────────┘
```

**Wins.** One scroll, no tab hunting, everything greppable at once — and it is the pattern the
toolset already ships, so triggers gain search, group collapse state, and the shared field
templates for free. Least code of the four: the schema exists, so this is `UttSchema` reordered to
match base and rendered by the existing `BlueprintEditorView`.

**Loses.** Furthest from the requested direction — the base dialog's shape is the thing a builder
recognises, and this discards it. A long form makes the rarely-touched Advanced fields as prominent
as the daily ones, which is precisely what tabs were solving.

**Cost.** Smallest of the four.

---

## Comparison

| | A · Faithful modal | B · Tabs, smart fields | C · Tabs + placements | D · One page |
|---|---|---|---|---|
| Matches the base dialog | Exactly | Layout yes, fields trimmed | Layout yes, plus a rail | No |
| Opens as | Modal, OK/Cancel | Document tab | Document tab | Document tab |
| Script slots | Text box | Picker over 475 handlers | Picker | Picker |
| Update Instances | Blind push | Blind push | Field-scoped, previewed | Blind push |
| Shows reuse | No | No | Yes | No |
| Drops dead fields | No | Yes | Yes | Yes |
| Build cost | Small | Small–medium | Medium | Smallest |

## Recommendation

**B, with C's placements rail as the next step.**

A is the most faithful and the least useful: it reproduces base's blind script boxes, which is
where the real errors live, and its modal fights the shell's save/undo model for no gain. D is
cheapest but spends the one thing the direction asked for — the recognisable dialog.

B keeps the shape and fixes the content. The script picker is the single highest-value change in
this whole study: 475 registered handlers exist, a wrong resref fails silently in game, and the
field is a bare text box today.

C's rail is worth adding once B ships, and its scoped Update Instances should land **before** any
Update Instances button does. An unscoped push would erase `DISPLAY_TEXT` on all 41 exploration
triggers — so the safe order is "no button" → "scoped button", never "button, then scope it".

One fix belongs in whichever design lands: `UttSchema`'s `Type` description still reads
"0 = generic, 1 = trap, 2 = area transition", which is reversed in exactly the way
`LookupOptionProvider` already documents and corrected.
