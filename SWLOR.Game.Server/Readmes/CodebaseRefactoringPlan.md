# Codebase Refactoring & Reorganization Plan

Goal: make the SWLOR codebase more maintainable and readable without changing gameplay behavior.
Scope: code organization, duplication, hygiene, and tooling. Infrastructure modernization
(hot-reload frameworks, persistence engine swaps) was proposed separately and is out of scope here.

Everything below is grounded in a survey of the current branch (`feature/combat-upgrade` lineage,
~744 commits ahead of `master`). Key baseline numbers:

| Area | Size | Headline problem |
|---|---:|---|
| `Feature/` | 1,554 files / 441k lines | 58% of it is one generated folder (`BeastDefinition/`) |
| `Service/` | 423 files / 94k lines | `Combat.cs` alone is 10,062 lines with 18 distinct concerns |
| `SWLOR.Game.Server.Tests` | 143 files / 969 tests | not run by any CI workflow |
| Hygiene | — | no `.editorconfig`, no `Directory.Build.props`, `LangVersion 10` pinned on the main project |

The plan is ordered so each phase makes the next one safer. Phases 0–2 are low-risk and can start
immediately; Phases 3–5 change large amounts of code and should ride on the safety net Phase 0
creates.

---

## Phase 0 — Safety net and hygiene baseline

Nothing else in this plan should land at scale until this exists. Estimated effort: small (days).

1. **Put the unit suite in CI.** The only workflow (`.github/workflows/engine-tests.yml`) runs
   engine tests only, on PRs to one branch, and self-describes as unvalidated. Add a workflow that
   runs `dotnet build SWLOR.Game.Server.sln -p:RunPostBuildEvent=Never` + `dotnet test` on every PR.
   969 existing tests become an enforced ratchet instead of an optional one.
2. **Fix the `.gitignore` landmine.** `.gitignore:9` ignores `SWLOR.Game.Server/SWLOR.Game.Server.csproj`
   itself (currently inert because the file is tracked, but one `git rm --cached` away from silently
   dropping the main project file). One-line fix.
3. **Add `Directory.Build.props` + `.editorconfig` + `Directory.Packages.props`.** Today nine
   projects carry five different combinations of `LangVersion`/`Nullable`/`ImplicitUsings`, there is
   no analyzer enforcement of any kind, and package versions are hand-synchronized across csproj
   files. Centralize: one TFM, one LangVersion (drop the `LangVersion 10` pin in
   `SWLOR.Game.Server.csproj` — it locks the largest project out of C# 11–14), central package
   pinning, and a real `.editorconfig` replacing the ReSharper-only `.DotSettings`.
   Turn `Nullable` on per-project over time (start with new files via `#nullable enable`), not big-bang.
4. **Deduplicate `FindRepositoryRoot()` in tests.** 92 of 143 test files carry a private copy
   (111 call it) — the single largest copy-paste block in the repo. Extract one shared helper
   (e.g. alongside `Test2daHelper.cs`). ~800 lines deleted, and future content-directory moves stop
   requiring 92-file edits.

## Phase 1 — Delete dead weight

Pure deletions and inventory fixes; near-zero risk, immediate readability win. Effort: small.

- **Stale/orphaned files:** `SWLOR.Game.Server/SWLOR.sln` (stale GUIDs, legacy project type),
  root `apply_hak_targeting_and_icons.py` (hardcodes a nonexistent `C:/Projects/SWLOR_combat-upgrade`),
  `SWLOR.CLI/hakbuilder.json` (pre-fork FFO config shadowing `Build/hakbuilder.json`),
  `SWLOR.CLI/efpt.config.json` (EF-era artifact), the dot-less root `dockerignore` duplicate.
- **Zero-reference tool scripts** (10 of the 28 in `tools/` have no references anywhere):
  `ApplyCombatBibleReviewFixes.ps1`, `GenerateSlicingContent.py`, `GenerateSlicingObjectiveArt.py`,
  `ReadDesignBibleSheet.ps1`, `SyncCombatBibleDescriptions.py`, `SyncMimicryTechniqueIcons.ps1`,
  `UpdateNpcAbilityBibleRows.ps1`, `UpdateSlicingDesignBible.ps1`, `reorganize_hak_sources.py`, and
  friends. Verify one last time, then delete; git history keeps them recoverable.
- **Committed runtime state:** `SWLOR.Game.Server/Docker/redis/dump.rdb` and the
  `Docker/influxdb/**` meta/WAL/series files are container runtime data in git. Delete and gitignore.
- **Duplicate committed binaries:** `nwn_erf.exe`/`nwn_gff.exe`/`pcre64.dll` exist byte-identical in
  both `SWLOR.CLI/` and `tools/SWLOR.CLI/`. Keep one location.
- **Dead code:** `Service/Menu.cs` (zero references), `Core/Extensions/CollectionExtensions.AddElement`
  and `.SafeGet` (zero call sites). Collapse the two-file `Core/Extensions/` folder into `Extension/`
  (2 importers vs 41) and normalize the singular/plural file naming while doing so.
- **Solution inventory:** `tools/SWLOR.SlicingBoardGenerator/` is a real project referencing
  `SWLOR.Game.Server` but absent from the solution — add it to the sln (or delete it) so it stops
  silently rotting. Also clean the untracked `SWLOR.Toolset*`/`SWLOR.NWN.Formats` `bin`/`obj`
  leftovers from other branches out of local checkouts (nothing to commit; they're not tracked here).

**Flagged, larger decision:** `tools/SWLOR.CLI/SWLOR.CLI.exe` is a committed 90.5 MB self-contained
binary that the main project's PostBuild depends on, with nothing enforcing parity with `SWLOR.CLI/`
source. Long-term it should be built from source in the deploy pipeline rather than committed.
Deferred to its own task because it touches the deploy machine.

## Phase 2 — Mechanical consistency sweeps

Wide but shallow, compiler- or test-verified, each an independent PR. Effort: small-medium.

1. **`ExecuteScript` literals → `ScriptName` constants.** The attribute side is perfect (472/472
   handlers use constants); the dispatch side is 55 literals vs 1 constant, including six sites that
   duplicate existing constants (`Space.cs:1310`ff, `DB.cs:77`, `Enmity.cs:190/223`). Add constants
   for the ~20 Bioware/community script names that lack them. Consider a unit test that greps for
   `ExecuteScript("` to ratchet this.
2. **`public class` → `static class`** for the 14 Feature event-handler files that contain only
   static members (`Auditing.cs`, `DMActions.cs`, etc.) plus `Service/Messaging.cs`, the lone
   non-static service.
3. **Finish the Core→API namespace extraction.** 18 files under `SWLOR.NWN.API/NWNX/Enum/` still
   declare `namespace SWLOR.Game.Server.Core.NWNX.Enum`, emitted from the API assembly and imported
   by 38 files. Mechanical rename to `SWLOR.NWN.API.NWNX.Enum`.
4. **Group the 46 loose `Feature/*.cs` files** into intent-revealing subfolders that already exist in
   the names: `Persistence/`, `DM/`, `Terminal/`, `Configuration/` (for the sub-50-line toggles).
   Pure file moves.
5. **Local-variable key constants.** 60 distinct magic strings used as bare NWN local-variable keys
   (`Property.cs` 21 sites, `Weather.cs` 17, `"STAMINA"`/`"FP"` duplicated 8× each). Adopt the
   `AI.cs:20-23` const pattern per service, or a shared `LocalVariableName` class for cross-service keys.
6. **Migration naming normalization.** Four unnumbered `ServerMigration` files sit beside `_N_`
   numbered ones, with gaps (`_1_`, `_4_`, `_5_`) and the version duplicated between filename and the
   `Version` property. Rename to the numbered convention, document the gaps, and add a boot-time (or
   unit-test) assertion that filename prefix == `Version`.
7. **Logging consistency.** ~40 `Console.WriteLine` sites (`DB.cs`, `Item.cs`, `Fishing.cs`…) vs 143
   `Log.Write` sites. Convert to Serilog-backed `Log.Write`.
8. **Encapsulation fixes.** `Space.cs:35` exposes a publicly mutable static `Dictionary` cache;
   convert to the private-field + `IReadOnlyDictionary` getter convention used elsewhere
   (`Ability.cs:70`). Relocate the three logic classes living in types-only `*Service/` folders
   (`AIService/NPCAI.cs`, `SlicingService/SlicingSession.cs`, `QuestService/QuestEncounter.cs`).

## Phase 3 — Unified data-driven definitions

**Direction (owner decision):** every definition type shares one model. There is no split between
"builder definitions" and "data definitions" — all definitions are declarative data, and data hooks
into code by name where behavior is genuinely custom. This replaces the earlier idea of converting
only the logic-free folders.

### The model

- **`Data/<Type>/` files declare everything:** identity, stats, costs, components, targeting
  metadata, effect lists. Format: JSON (the module pipeline already speaks JSON) or TSV where purely
  tabular.
- **Behavior comes in two tiers:**
  1. *Parameterized standard routines* — shared runtime code driven by data fields:
     "deal damage of type X scaled by stat Y", "apply status Z for N seconds", "pull aggro in
     radius R". The evidence this covers most existing lambdas: a generator
     (`tools/GenerateWeaponArchetypeImplementation.py`) already writes those lambda bodies *from
     Bible description text*. Logic a script can write from a sentence is parameterizable logic.
  2. *Named hooks* — for the genuinely bespoke remainder, a data field references a key
     (`"onImpact": "dark-force-conversion"`); C# classes register under keys via an attribute
     (`[DefinitionHook("dark-force-conversion")]`) and a boot-built registry resolves them.
- **Validation replaces compile-time checking.** A boot-time pass (and a unit test that loads every
  data file) asserts: every hook key resolves to a registered hook, every referenced stat/status/
  resref exists, every registered hook is referenced by at least one definition (dead-hook warning).
  The existing test culture — 104 of 143 test files already scan repo content — fits this exactly;
  the tests get *easier* because they read data directly instead of parsing C#.

**Trade-offs, stated plainly:** hook references are strings, so a typo is caught at boot/test time
instead of compile time (the validation test in CI is the mitigation, and it must exist before the
first hooked folder converts); debugging goes through one dispatch layer; and stage 3d below is a
large program, not a sweep. In exchange: one mental model for all content, Bible→data becomes a
direct pipeline (deleting the generate-C#-then-hand-copy workflows entirely), content edits stop
requiring recompiles (which is also the precondition for any future content hot-reload), and merge
conflicts on content drop to near zero.

### Staged migration (each stage shippable, parity-tested)

Lambda density measured per folder — this sets the order, easiest first:
Beast **0** · Recipe **0** · LootTable **0** · Perk 18 · Item 50 · Dialog 61 · Quest 166 ·
Ability 286 · StatusEffect 1,130.

**3a. Build the definition runtime once.** Data schema, loader, hook registry + attribute,
validation harness. Small core; everything else rides it.

**3b. Pure-data folders: Beast (~250k lines), Recipe (~32k), LootTable.** Zero hooks needed, so
this proves the loader with no behavior risk and takes the biggest deletion first.
`BeastDefinition/` is 172 files that are 98.7% structurally identical (50 `LevelN()` stat methods
each), generated by `SWLOR.CLI/BeastCodeBuilder.cs` and hand-copied into the tree; `BeastMastery`
loads the data file at boot instead of reflecting over 172 classes. `RecipeCodeBuilder.cs`'s
scratch-`.txt`-and-hand-paste workflow disappears.

**3c. Low-logic folders: Perk, Item, Dialog, Quest.** First use of named hooks. Most of their
lambdas become standard routines (tier 1); the rest become hook classes named for their content.

**3d. Logic-dense folders: Ability and StatusEffect.** The big one. StatusEffect's 1,130 lambdas
across 333 files are mostly generator-templated tick/apply/remove bodies — prime tier-1 material.
Decompose each definition into data fields + standard routines, with named hooks only where behavior
is truly unique (the per-content riders). Retarget `GenerateWeaponArchetypeImplementation.py` to
emit data rows instead of C# files — the Bible-parsing front half survives, the C#-emitting back
half is deleted. The AGENTS.md targeting table (shape/cursor/`TARGETSELF` rules) maps to data
fields enforced by the same validation test that today reflects over builders; update AGENTS.md and
the readmes as each folder converts so the documented workflow tracks reality.

**Guardrail for every stage:** old and new representations built side by side; built output
serialized and compared field-for-field (hooks compared by resolved method identity); the C# folder
is deleted only after the parity test passes. Never eyeball 250k generated lines.

**Related but deferred: giant enums.** `StatType.cs` (5,653 lines / 941 members), `RecipeType.cs`,
`StructureType.cs` are data tables wearing enum costumes. Full data-driving eventually turns
recipe/structure identifiers into data ids, shrinking those enums naturally — let that fall out of
3b–3d rather than attacking the enums directly. `StatType` stays an enum: the `StatTypeAttribute`
metadata pattern is load-bearing (AGENTS.md mandates it) and shared systems switch on it.

## Phase 4 — Service-layer decomposition

Highest-value readability work and the highest-risk phase. Do it after Phase 0 CI exists, and
sequence it around large in-flight content merges (every step here conflicts with anything touching
`Combat.cs`). Effort: large, but incremental — each step is independently shippable.

**4a. Split `Combat.cs` (10,062 lines, 497 members, 27 static caches, 18 concerns, 0 regions).**
Use the in-repo precedent: `Skill` is already a `static partial class` across 4 files. Split
mechanics-first, no behavior change:
- Partial files by concern: roll math; damage-dealt pipeline; damage-taken pipeline
  (mitigation/redirect/reflect); positional combat; crit effects; on-kill effects; low-HP triggers;
  guard/deflection/retaliation; status stacking; resource economy; the "next attack/next ability"
  buff-token ledger (~25 `Grant*/Consume*/Has*` triples); combat-log construction; attack-delay/APR
  math; and the 46 per-content rider methods (Predator's Mark, Ricochet, Saberstaff Tempest, …) in
  their own file(s).
- Then extract genuinely separable subsystems into their own services where a clean seam exists
  (buff-token ledger and low-HP triggers are the best candidates).
- Keep the public `Combat.X()` API stable throughout so the 700+ external call sites don't churn.

**4b. Creature-state cache registry.** `Combat.cs` holds 27 per-creature static dictionaries whose
cleanup is a misnamed 58-line manual method (`RemoveStatTriggerCooldowns`) living 4,400 lines from
the declarations, wired to death/exit but *not* `OnObjectDestroyed` — despawned creatures leak into
all 27 caches, and NWN recycles object ids. Introduce a small registry: caches self-register a
`Clear(uint)` callback at declaration site; one handler set (death, exit, destroyed — matching
`Enmity.cs:69`) sweeps them. Fixes the leak and the two-site-edit trap in one move.

**4c. Extract the two trapped subsystems in `Ability.cs` (3,143 lines):** the aura system (~500
lines + 6 event handlers, fully self-contained) and the AoE geometry math (cone/line/rotation,
~500 lines) into `AbilityService/` files. Neither touches ability registration.

**4d. Split `Property.cs` (3,484 lines, five unrelated subsystems):** async load scheduler, city
politics (elections/upkeep/tax — includes the repo's longest method, 216-line
`ProcessCityElections`), structure spawning/placement, permissions, Discord webhook broadcasting.

**4e. Kill the managed/`Native` twin implementations in `Stat.cs`.** Nine getter pairs
(`GetAttack`/`GetAttackNative`, `GetDefense`/`GetDefenseNative`, …) mean every balance change must
be made twice with nothing guarding parity. Preferred: extract the shared math into a core that both
object models call. Minimum: add parity tests that run both paths over a matrix of stat inputs and
assert equal results, so divergence fails CI instead of shipping.

**4f. Deduplicate cross-service helpers.** Byte-identical private copies of
`GetRecastGroupFromStat`, `GetSkillTypeFromStat`, `GetPerkTypeFromStat`,
`GetHighFPAndStaminaAttackAdjustment`, `ApplyPercentAdjustment` across
`Combat`/`Stat`/`Perk`/`Ability`; the 120-line henchman-event-forwarding clone between
`BeastMastery.cs:450` and `Droid.cs:970`; `Space.cs`'s six clamp-and-notify twins. `GameMath.cs`
already exists as the natural home for the math ones and is currently bypassed.

**4g. Make boot ordering explicit.** `Core/ScriptRegistry.cs` registers handlers in reflection
enumeration order with no ordering guarantee; real dependencies exist inside a single phase
(`EventRegistration`'s first statement requires `DB.Load()` to have already run in the same
`mod_preload` bucket) and are otherwise documented only in prose comments (`Mimicry.cs:71`,
`IncubationFieldNote.cs:47`). Minimum viable fix: an optional priority/order value on
`NWNEventHandler` (or a dedicated init-phase attribute), sort registrations, and convert the known
prose-documented dependencies into declared ones. Also split the two `Combat.cs` test seams
(`SetAbilityHitResolutionOverride` etc.) behind a clearly-named test-only surface.

**Explicit non-goal:** converting static services to DI/instances. The static-service pattern is
pervasive (100+ services, thousands of call sites) and works with the NWN interop model; the
circular Combat↔Stat↔Ability dependencies are real but a DI rewrite is infrastructure-scale work
that belongs with the previously-proposed modernization plans, not this cleanup.

## Phase 5 — Data layer and GUI targeted fixes

Independent of Phase 4; can run in parallel with it. Effort: medium.

**5a. Fix the `DBQuery` 50-row default.** Queries without `AddPaging` are silently capped at 50
(`DBQuery.cs:182-187`); callers who noticed invented the racy two-round-trip
`SearchCount` → `AddPaging(count, 0)` idiom, now copy-pasted 57 times across 27 files. Add an
explicit `.All()`/`.NoLimit()` to `DBQuery`, migrate the 57 sites, and audit the remaining ~86
unpaged query sites for unintentional truncation.

**5b. Batch `DB.Search` reads.** It issues one `JsonGet` per result row (a 500-row load = 500+
round trips). Pipeline the fetches.

**5c. Key the entity cache by `(Type, Id)`.** `DB.cs:37` keys by id string alone; one future entity
type sharing an id space produces `InvalidCastException` in `Get<T>` and false `Exists<T>`. Small
change, removes a latent production bug. While in there: unify the three escaping regimes in
`DBQuery`, and stop dropping/recreating all 27 RediSearch indexes on every boot (skip when the
schema hash is unchanged).

**5d. GUI viewmodel dedupe.** `GuiViewModelBase` infrastructure is good; the bloat is 1,183
hand-written `Get<T>()/Set<T>()` binding properties (~4,700 lines) plus genuine copy-paste in
`AppearanceEditorViewModel` (3,501 lines across two partials): four copies of the same
category×gender switch ladder (lines 1033/1212/1603/1694, 122 `case` labels) collapse to one
`SelectedPartCategoryIndex → (CreaturePart, male[], female[])` lookup table, and the 14 enumerated
body-part properties collapse to an indexed accessor. The binding properties are a good candidate
for a `[GuiBound]` source generator, but that's optional polish; the switch-ladder dedupe is not.

## Phase 6 — Documentation truth

Cheap, high-value for onboarding and for agents. Effort: small.

- **Rewrite `Readmes/ProjectStructure.md`:** it says .NET 7.0, documents a `Core/NWScript` layout
  that moved to `SWLOR.NWN.API` (which it never mentions), and lists Discord.Net (no longer a
  dependency). Update the Core-layer description in `CLAUDE.md` and `CoreSystems.md` samples too.
- **Regenerate `Readmes/README.md`:** the index is missing 16 of 32 files, including docs
  `AGENTS.md` declares mandatory (`EngineTesting.md`, `DesignBibleWorkbookRules.md`).
- **Split `Readmes/` by genre:** reference docs vs one-off project plans vs generated CSV artifacts
  currently share one flat folder. Move plans to `Readmes/plans/`, generated artifacts to
  `Readmes/generated/` (updating the generating tools' paths), and resolve the `AGENTS.md` naming
  rule conflict on the eight `CombatUpgrade*` filenames when moving them.

---

## Target end-state structure

What the repo looks like when all six phases have landed. Only changed areas are annotated;
anything not mentioned keeps its current shape.

```
<repo root>
├── Directory.Build.props            # NEW — shared TFM, LangVersion (latest), analyzers, nullable policy
├── Directory.Packages.props         # NEW — central package version pinning
├── .editorconfig                    # NEW — enforced style, replaces ReSharper-only .DotSettings
├── .github/workflows/
│   ├── build-and-test.yml           # NEW — build + unit suite on every PR
│   └── engine-tests.yml
├── SWLOR.Game.Server.sln            # includes every real project (SlicingBoardGenerator in or deleted)
│
├── SWLOR.NWN.API/                   # NWScript/NWNX wrapper — everything under SWLOR.NWN.API.* namespaces
│                                    #   (no more SWLOR.Game.Server.Core.NWNX.Enum split-brain)
├── SWLOR.Game.Server/
│   ├── Core/                        # bridge/runtime only: bootstrapper, scheduler, script registry
│   │   └── ScriptRegistry supports explicit handler ordering; no Extensions/ subfolder
│   ├── Data/                        # NEW — ALL definitions as declarative data (embedded/content resources)
│   │   ├── Beasts/                  #   stage 3b: replaces Feature/BeastDefinition (~250k lines)
│   │   ├── Recipes/                 #   stage 3b: replaces Feature/RecipeDefinition (~32k lines)
│   │   ├── LootTables/              #   stage 3b
│   │   ├── Perks/  Items/  Dialogs/  Quests/        # stage 3c — data + named hook keys
│   │   └── Abilities/  StatusEffects/               # stage 3d — data + standard routines + hook keys
│   ├── Entity/
│   ├── Enumeration/                 # cross-cutting enums only (documented rule);
│   │                                #   domain enums live in their owning Service/XService/ folder
│   ├── Extension/                   # single extensions home (Core/Extensions merged in, dead methods gone)
│   ├── Feature/                     # end-state: event subscribers + behavior code, no data tables
│   │   ├── Configuration/           # NEW grouping — the sub-50-line toggle handlers
│   │   ├── DM/                      # NEW grouping
│   │   ├── Persistence/             # NEW grouping
│   │   ├── Terminal/                # NEW grouping
│   │   ├── Behavior/                # NEW — [DefinitionHook]-registered classes referenced by Data/
│   │   │   ├── Ability/             #   bespoke impact/activation hooks + shared standard routines
│   │   │   ├── StatusEffect/        #   bespoke tick/apply/remove hooks
│   │   │   └── Quest/  Item/  ...   #   (replaces the *Definition builder folders as stages 3c/3d land)
│   │   ├── GuiDefinition/           # stays code — viewmodels are real logic (see Phase 5d)
│   │   └── MigrationDefinition/     # uniformly _N_-numbered; Version==filename asserted
│   ├── Service/
│   │   ├── Combat.cs                # public API surface only — stable entry points
│   │   ├── Combat.Rolls.cs          # partials by concern (Skill.*.cs precedent):
│   │   ├── Combat.DamageDealt.cs    #   dealt/taken pipelines, positional, crits, on-kill,
│   │   ├── Combat.DamageTaken.cs    #   low-HP triggers, guard/deflection, status stacking,
│   │   ├── Combat.BuffTokens.cs     #   resource economy, combat log, attack delay/APR,
│   │   ├── Combat.ContentRiders.cs  #   per-perk/weapon-line riders
│   │   ├── Combat....cs             #   (~15 files total; per-creature caches in a self-registering
│   │   │                            #    registry swept on death/exit/OnObjectDestroyed)
│   │   ├── Ability.cs               # registration/activation only
│   │   ├── AbilityService/          # + extracted Aura subsystem, AoE geometry (types folders stay types-only;
│   │   │                            #   NPCAI/SlicingSession/QuestEncounter relocated out of theirs)
│   │   ├── Property.cs → Property.*.cs  # split: load scheduler / city politics / structures /
│   │   │                            #   permissions / webhooks
│   │   ├── Stat.cs                  # one implementation per getter; Native/managed share a core
│   │   │                            #   (or parity tests guarding the pair)
│   │   └── GameMath.cs              # actual home of the shared percent/conversion helpers
│   ├── Readmes/
│   │   ├── README.md                # complete index
│   │   ├── *.md                     # reference docs only, accurate to the current layout
│   │   ├── plans/                   # NEW — one-off project plans (renamed off the CombatUpgrade label)
│   │   └── generated/               # NEW — tool-regenerated CSVs
│   └── Docker/                      # no committed runtime state (dump.rdb, influxdb data gone)
│
├── SWLOR.Game.Server.Tests/
│   ├── Support/RepoPaths.cs         # NEW — the one FindRepositoryRoot (92 private copies deleted)
│   └── ... (mirrors source layout)
├── SWLOR.Game.Server.EngineTests/
├── SWLOR.CLI/                       # generators emit directly to final paths (no scratch+hand-copy);
│                                    #   one copy of nwn_erf/nwn_gff/pcre64; stale configs deleted
├── SWLOR.Admin/  SWLOR.Web/  SWLOR.BackgroundServices/  SWLOR.Runner/
├── tools/                           # load-bearing scripts only (~18 of 28 survive);
│                                    #   SWLOR.CLI.exe no longer committed once deploy builds from source
└── scripts/
```

Rules of thumb the end state encodes:

- **`Service/` convention, now written down:** `X.cs` (or `X.*.cs` partials) at top level owns all
  behavior; `XService/` holds only types (enums, DTOs, builders). No logic in types folders, no
  file over ~2,000 lines without partials.
- **One definition model:** every definition is declarative data in `Data/`; behavior is either a
  parameterized standard routine driven by data fields or a named `[DefinitionHook]` class in
  `Feature/Behavior/`. No builder-vs-data split.
- **`Feature/` is thin:** event subscribers and behavior code. Anything holding state or an API
  belongs in `Service/`; anything declarative belongs in `Data/`.
- **Validation is the type system for data:** a boot pass + CI test resolves every hook key and
  cross-reference in every data file; unresolved = red build, not a runtime surprise.
- **Generated files are labeled and reproducible** while any remain: `<auto-generated>` headers,
  generators write to final paths.

## Sequencing and risk notes

- **Branch reality:** the active line is `feature/combat-upgrade` (~744 commits ahead of `master`),
  and it will be merged into `master` when complete — everything here, including the test suite and
  tools that don't exist on `master` yet, lands there via that merge. So do the refactoring on the
  combat-upgrade line and let it ride the merge; don't duplicate any of it against `master`.
  Two timing consequences:
  - Large renames/moves/splits (Phases 3–4) widen the divergence, so they make any interim
    `master`→branch syncs or cross-branch cherry-picks harder until the merge happens. Prefer
    landing them either well before an interim sync or after the combat-upgrade merge ships.
  - Each Phase 4 step is also a merge-conflict generator for other in-flight branches touching the
    same service (`Combat.cs` especially) — coordinate with active content work.
- **One PR per bullet.** Every numbered item above is deliberately shippable alone. Mechanical
  sweeps (Phase 2) must not share a PR with behavioral changes.
- **Verification pattern for data conversions (Phase 3):** build old and new representations side by
  side, assert serialized equality, then delete the old — never eyeball 250k generated lines.
- **Verification pattern for splits (Phase 4):** partial-class splits and file moves are
  compiler-verified; subsystem extractions ride the unit suite plus the in-engine `[EngineTest]`
  harness for combat behavior.
- **Phase 3 staging matters more than any other phase's.** 3a+3b are self-contained and can land
  early (no hooks, pure deletion). 3c/3d change how content behavior executes and should be
  sequenced around the combat-upgrade merge — 3d in particular touches everything the engine-test
  harness exercises, so it leans on that suite and likely lands as a post-merge program.
- **What we deliberately did not touch:** event-handler attribute wiring (`ScriptName` adoption is
  472/472 — already finished), static-service architecture (see Phase 4 non-goal), the giant enums
  (deferred — see end of Phase 3), and hot-reload/persistence modernization (separate proposals —
  though Phase 3's recompile-free content is the natural precondition for content hot-reload).

## Impact summary

| Phase | Risk | Approx. effect |
|---|---|---|
| 0 Safety net | none | CI enforcement; ~800 test lines deleted; hygiene baseline |
| 1 Deletions | minimal | dead files/scripts/binaries/runtime-state removed |
| 2 Consistency | low | 55 literal dispatch sites, 15 class decls, namespace split-brain, 46-file folder, magic strings |
| 3 Unified data-driven definitions | medium (3a–3b) → large (3c–3d) | **one definition model for all content**; ~280k lines deleted in 3b alone, more in 3c/3d; Bible→data direct; content edits without recompile |
| 4 Service decomposition | highest | `Combat.cs` 10k→~15 navigable files; cache-leak fix; Native/managed parity; explicit boot order |
| 5 Data/GUI | medium | 50-row bug class removed; N+1 reads fixed; ~1,500 lines of GUI copy-paste collapsed |
| 6 Docs | none | structure docs match reality; index complete |
