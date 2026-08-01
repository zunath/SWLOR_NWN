# Dialog Editor — Design

> **Current NUI direction (2026-08-01):** the graph-native editor is an authoring surface with a
> persistent, side-by-side simulation of the shipped 650×520 NUI conversation window. The preview
> is no longer a separate tab. It follows the NPC line being edited, updates as the writer types,
> and remains interactive so response buttons can walk the graph. Its title is always
> **Conversation**; the speaker name sits above the portrait; NPC text uses its one runtime scroll
> region; responses use the runtime blue button treatment and response-list scroll region; and an
> empty response set previews the runtime-supplied **Goodbye.** choice. Semantic text colors and
> representative dynamic-token values are rendered in place. Explicit portrait resrefs are loaded
> when available; an honest placeholder explains when the live NPC supplies its name or portrait.
>
> The older DLG corpus analysis and Play-it exploration below are retained as historical rationale.
> They do not override the current graph-native NUI editor or reintroduce the base-game Dialog
> service, generated DLG shells, or NWN's conversation-window constraints.
>
> Approved direction: **Play it** — the conversation is edited in the form the player sees it.
> **Built:** D1–D9 are complete; see §12 for what changed on contact with the real module. Read
> `PLAN.md` for the toolset's ground rules and `README.md` for the invariants any new editor
> inherits. Mockups accompany this document.

---

## 1. Why this exists

`PLAN.md` scoped dialogs out of the toolset on the grounds that "dialogs, quests, spawns, scripts
and stores already live in C#". That is half true. The C# `DialogDefinition` classes cover **19**
dynamic menus. The module also carries **609 `.dlg` files**, and after subtracting the machinery,
**352 of them are hand-authored conversations** — every quest offer, turn-in, merchant greeting and
lore branch in the game. Those are edited today by hand-writing nwn_gff JSON, or by opening Aurora,
which knows nothing about SWLOR's snippet system and therefore cannot check a single thing about
them.

### What is actually in `Module/dlg`

| | count | note |
|---|---:|---|
| Total `.dlg.json` files | 609 | |
| `dialog1` … `dialog255` | 255 | generated shells for the C# `Dialog` service — never hand-edited |
| Imported third-party (`dmfi_universal`, `tk_omnidye`) | 2 | 1,544 and 588 nodes; not ours |
| **Hand-authored conversations** | **352** | the actual editing surface |
| Total nodes | 12,297 | 3,361 NPC entries, 8,936 player replies |
| Median nodes per authored conversation | 17 | largest authored: 78 |
| Links | 17,166 | of which **5,733 (33%) are link-backs** |

### What the corpus says about how the team writes dialogue

| Conditions | uses | Actions | uses |
|---|---:|---|---:|
| `condition-on-quest-state` | 303 | `action-advance-quest` | 293 |
| `condition-has-quest` | 271 | `action-accept-quest` | 278 |
| `condition-can-accept-quest` | 195 | `action-request-quest-items` | 44 |
| `condition-completed-quest` | 122 | `action-open-store` | 33 |
| everything else | 15 | everything else | 7 |

Four condition keys and four action keys carry 98% of all logic. **101** conversations have more
than one starting entry — an ordered, guarded priority chain. **Zero** nodes use a TLK strref, a
`Comment`, or the journal fields. Only **3** set `Speaker`. Just **5** links in the module carry two
conditions at once — one ordinary link, plus four of `sera_vonn`'s openings.

---

## 2. The shape: Play it

The editor draws the conversation the way the game draws it — NPC line, numbered player choices —
for one hypothetical player at a time. Editing happens in place: click a line, type. Navigation is
walking: click a choice and the conversation moves on.

There is no tree, no node ids, no entry/reply distinction, no list indices and no snippet keys on
screen. None of it is removed from the file; it is simply not the writer's problem.

### What that buys, and the one thing it costs

It buys a surface with nothing to learn, where what you see is exactly what ships, and where a line
is always read in the context it is heard in.

It costs the overview. A walk shows one path, so **"what have I not written yet?" cannot be
answered by looking at the conversation** — which is the failure mode of every preview-shaped
editor. Two devices answer it instead, and they are not optional extras:

- **the situation list** — every distinct circumstance this conversation responds to, each marked
  written, empty, or unreachable; and
- **the coverage strip** — for each quest the NPC touches, one cell per step, coloured by whether
  a line exists for it.

Both are expressed as things that happen to a player, never as structure. Clicking either one sets
the pretend-player state and jumps the walk there, so they are navigation as much as they are
reporting.

---

## 3. What is on screen

```
┌ Healer Elara · Dantooine Colony ─────────────────── [Play it] [Advanced ▸] ┐
│  PRETEND THE PLAYER  (Harvesting Herbs: finished ▾) (Field Tinctures: doing it ▾) (Key items: none ▾)
├────────────────┬──────────────────────────────────────────────────────────┤
│ SITUATIONS     │  First meeting → "What do you need this time?" → …        │
│ ● Finished FT  │                                                           │
│ ● Ready to…    │  HEALER ELARA                                             │
│ ▸ Doing FT     │  ┌──────────────────────────────────────────────────┐     │
│ ● Offering FT  │  │ The bottles are lined up and empty. I still need │     │
│ ● Finished HH  │  │ three Wild Innards and one sample of Thune Blood.│     │
│ ○ First meeting│  └──────────────────────────────────────────────────┘     │
│ ⊘ (unreachable)│                                                           │
│ + Add          │            1. I've got the stabilizers.  opens hand-in box │
│                │            2. [Come back later.]         ends the talk     │
│ FIELD TINCTURES│            + Add another choice here                       │
│ [1][2][done]   │                                                           │
│ HARVESTING HERB│  2 choices are hidden for this player · show               │
│ [1][2][done]   │                                                           │
└────────────────┴──────────────────────────────────────────────────────────┘
```

- **State pills** — what the pretend player has done. Quests and their steps, key items, faction
  standing, skills, tutorial. Changing one redraws the conversation.
- **Situation rail** — the openings, as sentences, in priority order. Filled dot = written, hollow
  = empty, ⊘ = can never be reached. Selecting one sets the pills to a state that reaches it.
- **Coverage strip** — under the rail, one row per quest the NPC touches, one cell per step.
- **Breadcrumb** — where the walk currently is, clickable to step back.
- **The conversation** — NPC bubble, choice rows, each choice showing its consequence in plain
  English. Inline `+ Add` affordances where new content goes.
- **Hidden-choice footer** — how many choices this player cannot see, one click to reveal them
  greyed with their condition sentence.

---

## 4. How the file's concepts land on screen

| In the `.dlg` | On screen | Never shown |
|---|---|---|
| Starting entry + its link condition | a **situation** in the rail | the condition key, the priority index |
| `EntryList` node | NPC bubble | node index, `Speaker`, `Quest` fields |
| `ReplyList` node | numbered choice row | node index |
| Link `ConditionParams` | the situation's sentence, or a hidden choice | the key, the `!` prefix, the dispatcher resref |
| Node `ActionParams` | the consequence chip on the choice | the key, the `Script` resref |
| `IsChild` link-back | nothing — the walk just continues | that a link-back exists at all… |
| …until you edit that node | "used in 3 places · editing changes all of them" | |
| Orphan node | a situation marked "nothing leads here" | |
| Ordering of starting entries | rail order, with "the first one that fits wins" | first-match-wins as a rule to learn |

---

## 5. The six hard cases

A preview-shaped editor is easy to draw and hard to make complete. These are the places it breaks,
and what each one is answered with.

### 5.1 "What haven't I written?"

The situation list and the coverage strip, above. A quest step with no line for it is a hollow cell;
clicking it jumps there and offers an empty bubble to type into. `QuestBeatCoverage` — the rule that
knows the eight beats `dialogue-and-content-standards.md` requires — feeds this rather than a
validation list.

### 5.2 "Building 78 nodes this way is slow"

Creating a conversation does not start empty. **New conversation → this NPC gives a quest → pick the
quest**: the editor reads its name, step count, journal text, prerequisites and repeatability from
the C# quest definitions and stamps every situation with an empty line, in the correct priority
order, wired correctly. The writer then walks it and fills the blanks. Structure is never something
they build; it is something they find already correct.

This is the fourth direction from the options set, folded in as the creation flow rather than
kept as a rival view.

### 5.3 Reusing a line (5,733 link-backs)

While walking, following a link-back simply continues — which is what the player experiences, and
therefore right. The cost surfaces only on edit: the bubble carries **"used in 3 places · editing
changes all of them"** with **Make a separate copy** beside it. The writer decides in the words they
already think in, and never learns what a child link is.

### 5.4 Consequences

A choice shows what it does: *starts Field Tinctures*, *opens the hand-in box*, *opens the shop*.
Clicking the chip opens a popover — a plain-English menu of consequences, arguments as dropdowns fed
by the game code. The editor writes and clears the `Script` dispatcher resref itself; a builder can
no longer produce params with no script, which is a silent no-op today.

### 5.5 Choices this player cannot see

Hidden by default, counted in the footer, revealed greyed on request with their condition as a
sentence. Adding a condition is the same sentence picker: *"Only show this choice when…"* followed by
a menu, never a key.

### 5.6 Anything the simple view cannot express

The tree editor stays, one **Advanced** click away, unchanged: full node inspector, link-level
conditions, raw fields. It is the only view that can express anything at all, and the two imported
conversations plus the occasional oddity need it. It is also where the 255 generated shells open,
read-only, with a banner naming the C# `Dialog` service that produces them.

---

## 6. Plain English is a layer, not a screen

Every condition and action has a sentence. The sentence belongs with the snippet declaration in the
game server, next to its description and arguments, so the toolset never hardcodes English for
game logic:

```csharp
_builder.Create("condition-on-quest-state")
    .Description("Checks if a player is on one or more states of a quest.")
    .Phrase("the player is on step {state} of {questId}")
    .Argument("questId", SnippetArgumentType.QuestId)
    .Argument("state", SnippetArgumentType.QuestState, repeats: true)
    .AppearsWhenAction((player, args) => { … });
```

| Stored | Shown |
|---|---|
| `condition-can-accept-quest field_tinctures` | the player is allowed to start **Field Tinctures** |
| `condition-has-quest field_tinctures` | the player is doing **Field Tinctures** |
| `condition-on-quest-state field_tinctures 2` | the player is on step 2 of **Field Tinctures** — *"Deliver the Wild Innards and Thune Blood to Healer Elara."* |
| `condition-completed-quest harvest_herbs` | the player has finished **Harvesting Herbs** |
| `!condition-has-quest field_tinctures` | the player is **not** doing Field Tinctures |
| `action-accept-quest field_tinctures` | starts **Field Tinctures** |
| `action-advance-quest field_tinctures` | moves **Field Tinctures** to the next step, and pays out on the last one |
| `action-request-quest-items field_tinctures` | opens the hand-in box for **Field Tinctures** |
| `action-open-store` | opens the shop |

Quest names, step counts and journal text come from the game code, so a writer never types a quest
id and cannot mistype one.

---

## 7. The reachability evaluator

Everything above — which situation wins, which choices are hidden, which cells are covered, where a
clicked situation jumps to — runs on one piece of logic: given a hypothetical player, does this
condition pass? It lives in `Domain`, is unit-tested against the real corpus, and models the quest,
key item, skill, faction and tutorial conditions, which is 98% of usage.

Anything it cannot model is drawn as **not simulated** and never silently guessed. It predicts from
declared snippet semantics; it does not run C#, and the surface says so.

This is the load-bearing component of the whole design. In the earlier draft it was an optional
playtest panel; here it is the navigation model.

---

## 8. Problems, as sentences

Findings appear where the problem is — on the situation, on the bubble, on the choice — and read as
consequences, not rule names.

| Rule | What the writer sees |
|---|---|
| `UnreachableOpening` | "This can never happen. *First meeting* answers everybody, and it comes first. Move it up?" |
| `SnippetWithoutDispatcher` | *(cannot occur — the editor owns the dispatcher resrefs)* |
| `UnknownSnippetKey` | "This conversation uses a rule the game no longer knows about. It will be skipped." |
| `SnippetArgument` | "*Field Tinctures* only has 2 steps, so step 3 will never match." |
| `QuestBeatCoverage` | a hollow cell in the coverage strip |
| `OrphanNode` | a situation marked "nothing leads here" |
| `UnreferencedConversation` | "Nothing in the module has this conversation. Nobody will ever hear it." — **41** today |
| `DanglingConversation` | "This points at a conversation that does not exist." — **6** today |
| `StrayLocalization` | "This line has French text nothing reads." — **72** today |
| `HouseStyle` | "'traveler' is on the banned list" — advisory, never blocks a pack |

Dangling `Conversation` resrefs are checked against the resource index before being reported, since
several resolve from a hak or the base game. Six survive that check: `chair`, `pug_cap_computer`,
`untitled000`, `zep_demi_regen_c`, `_mdrn_conv_chair`, `_mdrn_conv_ship`. (An earlier draft of this
document said four. That count came from a sweep of blueprints only; two of the six are named
exclusively by placed instances, `untitled000` by seventeen of them.)

Implemented findings the sweeps turned up while building this:

- **28 openings across 5 conversations can never be reached.** Twenty-four are a second unguarded
  greeting behind a first one that already answers everybody; the other is `dantherbs` opening 5,
  where no combination of quest states escapes the openings above it.
- **`trooperquest` offers a quest that does not exist** (`suppress_rogues` appears nowhere in the
  game code), and is itself among the 41 conversations nothing references.
- **`rorrska_buvvien` passes a step number to `condition-has-quest`**, which reads only a quest id —
  so the guard matches on any step rather than step 1. It runs, so the runtime leaves it alone and
  the editor reports it.

---

## 9. What is underneath

The simple surface does not get a simple model. The document layer is the same one the tree view
needs, and the toolset's invariants apply unchanged.

- **A tree row is a link, not a node.** Conditions are stored on the link; actions on the node,
  where they fire from every incoming link. The walk view hides this; the model must not.
- **Link `Index` values are array positions**, so inserts always append to `EntryList`/`ReplyList`
  whatever their position in the conversation — a new line touches its own struct and one link, and
  that is the whole diff. Deleting from the middle renumbers, states its cost before running, and is
  one undo step.
- **Saves produce zero spurious git diff.** `RoundTripCorpusTests` covers all 609 dialogs from D1
  onward and stays green forever.
- **Autosave with unlimited undo.** Anything the simple views cannot show is preserved untouched
  rather than dropped.

---

## 10. Two things it fixes on the way past

- The creature editor's `Conversation` field is a bare resref box described as *"Legacy .dlg resref;
  SWLOR dialogs are C# classes"* — wrong for 352 conversations. It becomes a picker over module
  dialogs **and** the 19 C# dialog classes, with a create-one path.
- Module-wide dialogue search — "who says *Veldite*?" — which the current Search panel cannot do,
  because it indexes names and tags rather than spoken text.

---

## 11. Work packages

| WP | Tier | Scope |
|---|---|---|
| D1 | Lead | `DlgDocument` typed view: entries, replies, links, append-only insert, delete-with-renumber, unlink, orphan detection. Round-trip and edit-locality gates over all 609 files. |
| D2 | Mid | `SnippetArgumentType`, `.Argument(…)` and `.Phrase(…)` on `SnippetBuilder` in the game server; central arity enforcement; snippet catalog reflection in Domain. |
| D3 | Low | `IGameCodeIndex` extensions: quest names, step counts, journal text, repeatability, prerequisites; factions; skills; store and waypoint tags. |
| D4 | Lead | Situation model + reachability evaluator in Domain, tested against the corpus. Gates everything visual — it is the navigation model, not a feature. |
| D5 | Mid | The Play-it surface: state pills, situation rail, coverage strip, breadcrumb, walking, in-place text editing. |
| D6 | Mid | In-place structure editing: add/remove choice and follow-up, consequence popover, condition sentence-picker, "used in N places" with make-a-copy. |
| D7 | Low | Problems as sentences, anchored to the situation, bubble or choice that carries them. |
| D8 | Mid | New-conversation-from-a-quest flow. |
| D9 | Mid | Advanced tree view (the full node inspector), generated-dialog grouping and read-only banner, module-wide dialogue search, `Conversation` picker on the creature editor. |

Order: D1 → {D2, D3} → D4 → D5 → D6 → {D7, D8} → D9. D1 and D4 are the two gates; D2 and D3 are
independent of each other and of the UI work.

---

## 12. What shipped, and what changed on contact

Everything in D1–D9 is built, including the editing affordances (§5.3–5.5), the scaffold wizard, the
non-quest player controls, module-wide dialogue search, and the refusal below.

**The refusal is drawn at the guards.** Play-it declines **15 of 354** authored conversations — the
DMFI DM menus, the pazaak card games, and a handful of imported dialogs — because each decides what
to *show* with its own NWScript rather than with snippets, so not one branch of them could be
predicted. A custom *action* script is allowed through: it does not affect what a player can see, so
the walk stays accurate and the choice reads "runs the script X" instead of pretending to be just
talk. Refusing those too was the first version of the rule and it turned away 28, including ordinary
conversations like `train_terminal`.

**No Advanced tree was built.** With the refusal in place nothing is half-shown, so the tree is no
longer load-bearing; it stays available as future work rather than a gap. That resolves open
decision 4.

Five things in this design were wrong and were corrected by the work:

1. **The scaffold originally emitted a guarded "not ready yet" opening.** Its guard is the exact
   complement of the offer's, so between them the two answered every player and whichever came last
   could never fire. The unguarded line at the bottom does that job with nothing dead behind it, so
   the scaffold no longer emits a separate one — and it declines to add a greeting at all when the
   conversation already has one.
2. **Central arity enforcement had to become minimum-only.** Rejecting a surplus argument would have
   broken `rorrska_buvvien`, which works today because every snippet ignores arguments it did not
   expect. The exact-shape check is the editor's, not the runtime's.
3. **The dangling-conversation count was four, and is six** — see above.
4. **Saving was broken outright** and nothing caught it until the view model got tests. The
   word-count recompute ran outside a transaction, which the session guard rejects, so every save
   threw and was swallowed into the log. Recomputing inside a transaction fixes it; the test that
   found it asserts a saved file actually contains the edited line.
5. **The guard and consequence panel showed raw ids.** It rendered its sentences without the
   name resolver the rest of the editor uses, so it said `field_tinctures` where everything else
   said "Field Tinctures" — the one place the raw data leaked back to the writer.
6. **The placeable editor's conversation slot was removed, and then put back.** The placeable
   editor dropped it on the grounds that "SWLOR dialogs are C# classes"; merging that branch, this
   work initially deferred to it because only 5 of 5,461 placeable blueprints set the field. That
   was the wrong test. The premise is false — 352 of the module's 371 conversations are `.dlg`
   files — and the Behavior tab's Conversation behavior writes `CONVERSATION`, which can only name
   one of the 19 C# classes. So the majority route was unreachable, and the **32 placed placeables
   already using a `.dlg`** could not be edited at all. The slot is back on the Advanced tab.
   Rarity of use is a reason to move a field, never a reason to keep a wrong explanation.

The reachability evaluator also caught a bug in its own supporting scan: quest prerequisites
declared through `private const string` fields were being dropped, which made every capstone quest
chain look like a pile of offers that all fired at once, and reported 184 unreachable openings
instead of 28.

---

## 13. Open decisions

1. **Snippet metadata in the game server.** `.Argument(…)` and `.Phrase(…)` on `SnippetBuilder` keep
   one source of truth and let `ProcessConditions` enforce arity centrally, replacing the
   hand-rolled `args.Length` check copy-pasted into all 18 snippets. The alternative is a parallel
   table in the toolset, which drifts the first time someone adds a snippet.
   *Recommend: the builder.* It touches `SWLOR.Game.Server`.
2. **Delete semantics.** Delete-and-renumber matches Aurora and keeps files clean but produces a
   large diff; unlink-and-orphan produces none but accumulates dead nodes that then show up in the
   situation rail as "nothing leads here". *Recommend: delete-and-renumber, with the cost stated
   before it runs.*
3. **`NumWords`.** 570 of 609 files carry it; nothing in the engine reads it, and **21 of those 570
   are already stale** — they disagree with their own text by as much as 69 words, in both
   directions (`DlgCorpusTests.KnownStaleWordCounts`).
   *Recommend: recompute on text change* — a stale count is worse than an absent one, and the diff
   is one line. Implemented that way in D1: `RecomputeWordCount` writes only when the value moves,
   so a save that changed no text leaves the line alone.
4. **Detecting generated dialogs.** Pattern-matching `^dialog\d+$` covers the 255 C# shells; the two
   imports need a prefix rule or a short explicit list. *Recommend: pattern for the shells, explicit
   list for the imports* — two names is not worth a heuristic.
5. **Does the Advanced tree ship with v1, or after it?** *Resolved: after.* The refusal is
   implemented and measured — 15 of 354 conversations, all with custom guard scripts — so nothing is
   ever half-shown and those files are edited outside the toolset as they were before. The tree
   stays available as future work if that 4% turns out to matter in practice.

---

## 14. Not in scope

The screenplay and situation-card directions from the options set. The screenplay view remains the
better surface for a pure writing pass and can be revisited once Play-it is in builders' hands, but
nothing here depends on it. A free-form graph canvas remains out of scope entirely.
