# Espionage Implementation Plan

Implements the Espionage skill from the Design Bible Espionage tab (Infiltrator + Saboteur trees, 50 max rank, 110 SP, AGI primary / PER secondary, Utility category), plus the supporting systems: custom stealth/detection, back attack, slicing/lockboxes, weapon poisons, traps, and six new item stats. Espionage is currently excluded from the combat-upgrade audit scope (`CombatUpgradeImplementationStatus.md` item 5); bringing it into scope is an explicit step in Phase 0.

## Design Inputs

- Bible Espionage tab: Infiltrator tree (Stealth I–IV, Back Attack I–III, Slicing I–V, Tactical Escape I–II, Shadow Step I–II, Ghost Protocol) and Saboteur tree (Poisoncraft I–V, Trapcraft I–IV, Venom Expertise I–II, Razor Trap I–II, Shock Trap, Trap Management I–II, Master Saboteur).
- Design doc: Detection attribute replaces Listen/Spot; Stealth attribute replaces Hide/MS; detection checks every 30 seconds; stamina drains while stealthed; new item stats (Detection, Stealth, Trap Bonus, Disarm, Poison Bonus, Lockpicking).
- Contributor feedback (7/2024 thread): vanilla stealth is all-or-nothing and rolls far too often — full custom replacement; single opposed check (not separate spot+listen); poisons craftable-by-Espionage but usable/sellable by anyone, and not usable on lightsabers; traps must not stack and must have a per-player active cap; Detection counter lives in the General perk category but must not be a cheap hard-counter to an 8+ SP stealth investment; Vibroknife's Backstab active coexists with the Back Attack trait; no changes to Stealth Field Generator (invisibility path is untouched).

## Core Architecture Decisions

### 1. New skill: Espionage

- Add `SkillType.Espionage` (next free enum value) in `Service/SkillService/SkillType.cs` with `[Skill(...)]` metadata: Utility category, max rank 50, contributes to skill cap, description from the Bible tab, and `CharacterType.Standard` restriction — Espionage is Standard-only, mirroring Devices (Bible Char. Type column is `Standard` on every Espionage row).
- Add the matching row to `SWLOR_Haks/sw_2da/iprp_skill.2da` (required per the SkillType file header).
- XP sources: crafting poisons/traps (recipe path grants this automatically), successful lockbox/lock/terminal slicing, trap triggers on hostile NPCs, and time-in-stealth near hostiles (guardrail: only ticks XP when a hostile NPC is within detection range, to prevent AFK farming).

### 2. New stats (stat-driven rule compliant)

Six new `StatType` entries with `[StatType(StatTypeCategory.BeneficialWhenPositive)]`:

| StatType | Item stat name | Purpose |
|---|---|---|
| `Stealth` | Stealth | Opposed-check bonus while stealthed |
| `Detection` | Detection | Opposed-check bonus to detect stealthed creatures |
| `TrapBonus` | Trap Bonus | Increases trap effect strength |
| `TrapDisarm` | Disarm | Improves trap disarm success |
| `PoisonBonus` | Poison Bonus | Increases poison potency/duration |
| `Lockpicking` | Lockpicking | Improves slicing/lockbox success |

Item side mirrors the Evasion chain end to end: new `ItemPropertyType` entries in `SWLOR.NWN.API/NWScript/Enum/Item/ItemPropertyType.cs`, rows in `SWLOR_Haks/sw_2da/itempropdef.2da` (+ cost table), registration in `Feature/EquipmentStats.cs` (`RegisterStatActions` → `Apply*`/`Remove*` → `Stat.Adjust*` on the `Player` entity), and NPC support through the skin-item-property path (`ReapplyNPCStat`). Single accessors on `Stat` (e.g. `Stat.GetDetection(creature)`) combine: attribute scaling (PER for Detection, AGI for Stealth) + persistent equipment stat + `GetStatAdjustment(StatType.X)` (perks/status effects/temporary modifiers).

Perks grant bonuses only through `PerkBuilder.IncreasesStat(...)` — no perk-type checks inside shared combat/stealth systems (AGENTS.md stat-driven rule).

### 3. Stealth/detection replacement (NWNX)

All required NWNX events are **already subscribed** in `Feature/EventRegistration.cs` with `ScriptName` constants in place: `NWNX_ON_STEALTH_ENTER/EXIT_BEFORE/AFTER` and `NWNX_ON_DO_SPOT_DETECTION_BEFORE/AFTER`, `NWNX_ON_DO_LISTEN_DETECTION_BEFORE/AFTER`. Verified against the NWNX Events plugin source (`Plugins/Events/Events/StealthEvents.cpp`):

- `DoSpotDetection`/`DoListenDetection` are hooked per observer→target pair. The BEFORE event fires **only when the target is actually stealthed** (invisibility short-circuits before the event, so Stealth Field Generator behavior is untouched). Skipping the BEFORE event bypasses the entire vanilla opposed skill roll; the event result string decides the outcome: `"1"` = detected, anything else = not detected.
- `NWNX_ON_STEALTH_ENTER_BEFORE` is skippable to deny entering stealth (existing precedent: `Space.PreventSpaceStealth`). Result `"0"` forces entry without HIPS, `"1"` forces HIPS-style entry, other = cancel.

New service `Service/Stealth.cs`:

- **Entry gating** (`OnStealthEnterBefore`): players without any Stealth perk rank are denied with a message, and entry is denied while in combat — baseline stealth is out-of-combat only; Ghost Protocol's capstone stealth window is the only in-combat entry (NPCs pass through — spawn tables may still use stealthy NPCs). Stealth actives (Stealth I–IV feats) toggle stealth mode via `SetActionMode`; the native stealth toggle button routes through the same gate, so both paths are consistent.
- **Detection verdict** (`OnDoSpotDetectionBefore`): always skip the vanilla roll. Look up a cached verdict for (observer, target); if missing or older than 30 seconds, roll once — `d20 + Stat.GetDetection(observer)` vs `d20 + Stat.GetStealth(target)` — cache it with a 30-second expiry, and answer from cache until expiry. The engine re-queries this up to 5x/second on modifier changes, so the handler must be dictionary-lookup cheap; the roll itself happens at most once per pair per 30 seconds. This single check **is** the detection model — no environmental modifiers, no day/night, no separate listen channel.
- **Listen suppression** (`OnDoListenDetectionBefore`): always skip with result `"0"`. One stat pair, one check, per the design discussion.
- **Cache hygiene**: evict entries on stealth exit, death, area transition, and logout. Keyed dictionary `(observerObjectId, targetObjectId)` → `(detected, expiryTime)`.
- **Detect mode** (optional, cheap): while a creature has Detect mode active, grant a flat Detection bonus in `Stat.GetDetection`, making the vanilla toggle meaningful at its native movement-speed cost.

**Stealth effectiveness from perks**: Stealth I–IV grant +15/25/35/45% stealth effectiveness while active. Modeled as a status effect applied on stealth entry whose magnitude feeds `StatType.Stealth` (percentage applied inside `Stat.GetStealth`), not as a perk check in the stealth service.

### 4. Stamina drain while stealthed

`EspionageStealthStatusEffect : StatusEffectBase` applied on `OnStealthEnterAfter`, removed on `OnStealthExitAfter`:

- `Frequency => 6f`; each tick calls `Stat.ReduceStamina(creature, drainPerTick)`. Starting number: 2 STM per 6 seconds (must outpace the 30-second natural STM regen tick in `Feature/NaturalRegeneration.cs` for net drain; tune in playtest).
- When stamina reaches 0, force `SetActionMode(creature, ActionMode.Stealth, false)` — the exit event removes the effect.
- Carries the Stealth-perk effectiveness bonus (decision 3) so one status effect owns the whole "while stealthed" package.
- **Breaks on hostile action**: on ability activation against a hostile and on damage dealt, exit stealth mode. Hooked in the existing ability-activation and damage pipelines, not per-ability.

NPCs in stealth are exempt from the drain (no meaningful NPC stamina economy).

### 5. Detection counter for everyone else

New General-category perk line **Alertness I–III** (2/3/4 SP, Armor 5/25/40 requirements, `PerkCategoryType.General`) granting flat `StatType.Detection` (+5/+10/+15). Balance stance from the thread: the counter must exist (8 SP stealth investment needs an answer) but must not be a cheap hard-counter — at equal investment the dedicated stealther beats the casual detector; the detector closes the gap with Detection gear (the item stat) and Detect mode. The Bible **General** tab rows 13–15 now carry these perks with Dev Status `Design`.

### 6. Back Attack

- Trait perks grant `StatType.BackAttackDamagePercent` (+3/5/8%) and `StatType.BackAttackCriticalChance` (+3/+5% at ranks II/III) via `IncreasesStat`.
- Consumed in the shared damage pipeline (native `GetDamageRoll` hook / `Combat.cs`), melee weapons only, when the attacker is behind the defender. Add `Combat.IsAttackerBehindTarget` beside the existing `IsAttackerBesideTarget` facing math (`Combat.cs:1876`) using angle > 135°.
- Coexists with Vibroknife's Backstab active: Backstab is an active ability with its own positional rider; Back Attack is a passive percentage on every melee hit from behind. They stack naturally because they occupy different layers (active bonus damage vs passive percent modifier); no rework of Backstab needed.
- Ghost Protocol's "next back attack crits and inflicts Exposed" rides the same positional check plus a consume-on-trigger status effect; Exposed already exists (`ExposedStatusEffect`).

### 7. Poisons

- **Crafting**: standard recipe system — `RecipeBuilder.Create(RecipeType.X, SkillType.Espionage)`, tiers 1–5 gated by Poisoncraft I–V via recipe level + perk requirement. Mark `SkillType.Espionage` as shown in the craft menu. *Open decision: which crafting device surfaces these (new "chemistry station" vs an existing bench).*
- **Items**: poison vials, stackable (identical stats per tier — no per-item variance, unlike enzymes), sellable, **not** economy-restricted (player-tradable is the point — crafter niche per contributor feedback). New `uti` blueprints wired to a real player source (the recipes) so `EconomyObtainabilityCoverageTests` passes without flags.
- **Application**: using a vial targets a melee/thrown weapon; **lightsabers and force-weapon types are rejected**. Stores locals on the weapon: poison type, tier, charge count, and a potency snapshot from the **applier** (Bible: Venom Expertise buffs "poisons applied by you") — so bought poison is fully effective for the buyer, and Saboteur ranks make *your own application* stronger. Anyone may apply/use.
- **On-hit**: in the damage-dealt pipeline, an attack with a poisoned weapon consumes a charge and applies the tier's status effect (reuse `StatusEffect` system — Venom DoT, accuracy down, slow, etc. per poison type), scaled by the stored potency + target's `PoisonDefense`/Poison resistance. Internal cooldown per target (e.g. one application per 6 seconds) to keep dual-wield/fast weapons from multiplying output.

### 8. Traps

Custom implementation (placeable + scripted trigger), **not** native NWN traps — native traps can't consume our stats or cap logic.

- **Crafted trap kits** (Trapcraft I–IV tiers, recipes as with poisons): using a kit places a trap at a target location — arming delay 3 seconds, then an invisible trigger placeable with a subtle VFX. Hidden from creatures whose `Detection` fails a passive check vs the placer's trap tier; Trapcraft ranks also unlock **detect and disarm** interactions (disarm = `TrapDisarm` stat check vs trap DC; success yields the kit back at lower tiers).
- **Perk actives** (Razor Trap I–II, Shock Trap): `AbilityDefinition` per ability file rule, place a *visible* trap per Bible text — no detection game, pure zoning tool. Damage/status per Bible (Bleed / Shock), scaled by `TrapBonus` and PER.
- **Trigger**: on-enter, hostile creatures only; applies damage via `AssignCommand(placer, () => ApplyEffectToObject(...))` (combat-log attribution rule) and the tier's status effect. Trap despawns after triggering or after a lifetime (e.g. 5 minutes).
- **Anti-stacking**: per-player active-trap registry — cap 1 (base), 2 (Trap Management I), 3 (Trap Management II); placing over cap despawns the oldest. Minimum placement distance (e.g. 3m) from any existing trap, regardless of owner, kills the trap-pile exploit.
- **NPC traps in areas** (design doc "in the cards"): later phase — spawn-table-driven trap spawns in dungeon areas so detect/disarm has PvE value; reuses the same trap objects with an NPC owner.

### 9. Slicing and lockboxes

- Slicing I–V unlock lock/terminal tiers 1–5; ranks III+ also speed up the interaction (progress-bar style delay reduced 20–40%).
- **Lockboxes**: rare loot drops (existing Loot service/spawn table wiring) in five tiers. Opening: requires the tier's Slicing perk, then success roll — `d100 vs DC(tier) - (Lockpicking stat + AGI/PER scaling)`; success opens a loot table roll (mundane→exceptional by box tier), failure consumes the attempt with a short lockout (box is never destroyed — a specialist can always eventually open it, preserving the "take it to a slicer" economy loop).
- **World surfaces**: existing locked doors/containers/terminals migrate to tiered locks opportunistically (per-area content work, not a blocker for the perk line).

## Phases

**Phase 0 — Bible + scope.** Alertness rows are in the General tab (done); resolve remaining open decisions below; flip Espionage rows from `Design Only` as they land; extend `tools/UpdateCombatUpgradeAudit.ps1` scope + `CombatUpgradeBibleSyncTests` to include Espionage (workbook edits per `DesignBibleWorkbookRules.md` — zip/XML surgical edits only, then `-RefreshLocalBible`).

**Phase 1 — Foundation.** `SkillType.Espionage` + `iprp_skill.2da`; six StatTypes; six ItemPropertyTypes + `itempropdef.2da`/cost tables; `EquipmentStats` + `Stat` accessors + NPC skin path; `EspionagePerkDefinition` skeleton (both trees, trait ranks with `IncreasesStat` wiring); TLK entries (reuse empty slots); icons per `IconStandards.md` + cooldown-icon regen; recast groups (≤14-char short names).

**Phase 2 — Stealth core.** `Service/Stealth.cs` (entry gate, spot verdict cache, listen suppression, cache eviction); `EspionageStealthStatusEffect` (drain + effectiveness + zero-STM exit + hostile-action break); Stealth I–IV actives (self-targeted toggles — `TARGETSELF=1`, no `RequiresTarget()`, no `HostileFeat`); Alertness perks; Detect-mode bonus. **This phase is independently shippable and playtestable.**

**Phase 3 — Back Attack.** `IsAttackerBehindTarget`; damage-pipeline consumption of the two stats; Back Attack trait ranks; verify Vibroknife Backstab interaction.

**Phase 4 — Infiltrator actives.** Tactical Escape I–II (enmity reduction — existing Enmity service — + Evasion status effect), Shadow Step I–II (position warp behind target + Evasion buff; no invisibility), Ghost Protocol (enmity drop + timed stealth + primed back-attack crit/Exposed).

**Phase 5 — Poisons.** Recipes + vial blueprints; application targeting/validation (no lightsabers); weapon locals + potency snapshot; on-hit consumption in the damage pipeline; per-poison status effects; Venom Expertise/Master Saboteur `PoisonBonus` wiring.

**Phase 6 — Traps.** Trap registry service (cap, spacing, ownership, lifetime); crafted kits + hidden-trap detect/disarm; Razor/Shock Trap actives; `TrapBonus`/`TrapDisarm` consumption.

**Phase 7 — Slicing + lockboxes.** Lockbox blueprints + loot wiring; slicing interaction + success formula; loot tables per tier; Slicing perk gates.

**Phase 8 — Ship.** NPC trap spawns in select dungeons; enemy Detection/Stealth presets for spawn tables (Bible Enemy Stat Presets); balance pass on drain/check numbers; fold any persistent-data needs into the in-flight combat-upgrade migrations (no new numbered migrations; character-build data relies on the full rebuild); hak rebuild + module repack; full test suite.

## Decided (2026-07-16 review)

- Baseline Stealth I–IV is **out-of-combat entry only**; Ghost Protocol is the sole in-combat stealth entry.
- Back Attack damage progression is **+3/+5/+8%** (crit +3/+5% at ranks II/III).
- Poisons **cannot be applied to lightsabers or saberstaffs**; only crafting is perk-gated — anyone may apply and use crafted poisons (noted on the Poisoncraft I Bible row).
- Espionage actives are **player-only** — no droid instruction AI slots.
- Both capstones follow the standard capstone convention: Type `Capstone`, 6 SP, and for Ghost Protocol the shared Capstone recast timer at 90 seconds / 15 STM.
- Each tree totals the standard 60 SP / 18 rows. The missing Espionage 32 step is filled by two new 4 SP traits: **Silent Stride** (Infiltrator — no stealth movement penalty, 20% slower stealth STM drain) and **Lasting Coatings** (Saboteur — applied weapon poisons last 50% longer before wearing off).
- The six new stats are documented on the Bible **Character Stats** tab (rows 60–65), including the opposed-check formula and the applier-snapshot rule for Poison Bonus.
- Espionage is **Standard-only** (Devices precedent; Force-sensitives keep Force/Lightsaber/Saberstaff exclusives). Alertness stays `All` so every character can counter stealth, and poison *usage* stays universal — only the skill and its perks are restricted.

## Open Decisions (resolve in Phase 0)

1. **Poison/trap crafting surface** — new chemistry/saboteur bench placeable vs existing crafting device. Recommendation: new bench placed in seedy-district locations; flavor fits and avoids crowding existing menus.
2. **Detection check symmetry for NPCs** — NPC Detection derived from enemy stat presets (recommended: level-scaled Detection so dungeon sneaking is viable at-level, hard above level) vs flat per-family values.
3. **Stealth drain vs regen numbers** — proposed 2 STM/6s drain against the 30s regen tick; needs a target uptime (recommendation: ~2–3 minutes of continuous stealth at full STM for a dedicated build).
4. **Trait vs stat naming for the crit rider** — whether Back Attack crit chance folds into the existing crit-stat plumbing or is a distinct positional stat (recommended: distinct `BackAttackCriticalChance`, read only in the behind-target branch).
5. **Equipment stat budgets** — Alertness rows are on the General tab (2/3/4 SP, +5/+10/+15 Detection); still open whether the six new item stats need budget rows on the Equipment tabs.

## Key References

- NWNX event semantics: `C:\Projects\unified\Plugins\Events\Events\StealthEvents.cpp` (read-only reference)
- Event subscriptions: `Feature/EventRegistration.cs:152-155, 386-389`; script names: `Core/ScriptName.cs:381-385, 556-560`
- Stealth-deny precedent: `Service/Space.cs:2155` (`PreventSpaceStealth`)
- Stamina API: `Service/Stat.cs` (`ReduceStamina`:414, `RestoreStamina`:370); regen tick: `Feature/NaturalRegeneration.cs`
- Status-effect tick pattern: `Service/StatusEffectService/StatusEffectBase.cs` (`Frequency`), example `Feature/StatusEffectDefinition/VenomStatusEffect.cs`
- Item-stat chain example (Evasion): `SWLOR.NWN.API/NWScript/Enum/Item/ItemPropertyType.cs`, `SWLOR_Haks/sw_2da/itempropdef.2da`, `Feature/EquipmentStats.cs`
- Facing math: `Service/Combat.cs:1876` (`IsAttackerBesideTarget`)
- Recipe-to-skill linkage: `Service/CraftService/RecipeBuilder.cs` (`Create(RecipeType, SkillType)`)

## Implementation Status

- **2026-07-16 — Phases 1-2 foundation slice** (branch `feature/espionage-skill`): `SkillType.Espionage` (49, Standard-only, Utility), nine StatTypes (919-927), six item-stat ItemPropertyTypes (136-141) with the full EquipmentStats/Player-entity/`Stat.Adjust*` chain, `Stat.GetDetection`/`Stat.GetStealth` accessors, `Service/Stealth.cs` (perk + out-of-combat entry gate, cached 30s opposed spot verdicts, listen suppression), `StealthStatusEffect` (6s STM drain, zero-STM stealth break, drain slowed by `StealthStaminaDrainReductionPercent`), and `EspionagePerkDefinition` with all 16 perks. Stealth effectiveness and Silent Stride/Venom Expertise/Lasting Coatings/Master Saboteur bonuses flow through `IncreasesStat` — no perk checks in shared systems.
- **2026-07-16 — Phase 3**: Back Attack flows through the shared pipeline (`BackAttackDamagePercentAdjustment`/`BackAttackCriticalRatePercentAdjustment`, `IsAttackerBehindTarget` rear-arc check, native damage/attack-roll consumption).
- **2026-07-16 — Feat/TLK/icon slice + Phase 4**: all 22 feats exist (traits in blank rows 1476-1500, actives 2889-2898 plus reclaimed 2216/2217), TLK entries and `iprp_skill.2da` row 49 added, 22 hand-illustrated icons with semantic frames and cooldown variants pass the icon audit, and all 15 Espionage perks are purchasable. Ability definitions live for Stealth I-IV (native stealth toggle), Tactical Escape I-II (enmity dump + Evasion window + rank-2 slow cleanse), Shadow Step I-II (behind-target jump + Evasion window), and Ghost Protocol (enmity dump, in-combat 30s stealth window via the entry-gate exception, primed 100% back-attack crit window).
- **2026-07-16 — Phases 5-7**: Traps service (stat-driven capacity via `AdditionalTrapCapacity`, 3m spacing, 3s arming, proximity trigger, 5-minute lifetime, `TrapBonus` snapshot, Espionage XP on trigger) with Razor Trap I/II and Shock Trap actives; venom coatings (five craftable vial tiers on `SkillType.Espionage` recipes gated by the new `RecipePerkRequirement`, apply-to-melee/thrown-weapon item behavior that rejects energy blades, applier `PoisonBonus` potency snapshot, `PoisonCoatingDurationPercent` charge scaling, and on-hit consumption via the damage event with a 6s internal cooldown); lockboxes t1-t5 dropping rarely across five planets, opened through Slicing perk tiers with a `Lockpicking`-stat success roll, tier-scaled loot tables, delta-XP grants, and a 30s retry lockout; an Espionage Workbench (skill 49) placed in the Dantooine crafter base with its own blueprint; VenomCoating recipes documented in the Bible Cooking Recipes tab (worktree copy).
- **2026-07-17 — Integration**: merged the latest `feature/combat-upgrade` (Techniques window, capstone rare elites, Bible equipment-tab rebuild); the workbook conflict was resolved by taking the target branch's workbook and re-applying the VenomCoating recipe rows, and the Alertness perk is now registered (PerkType 800, `AlertnessTrait` feat) with its three General-tab Bible rows flipped to Implemented.
- **2026-07-17 — Lockbox uniques**: itempropdef.2da rows 136-141 register the six Espionage stats as item properties (flat NPCSTM cost table), and fifteen lockbox-exclusive accessories (ring/necklace/belt per tier, `espn_*` resrefs) carry them at +2/+4/+6/+8/+10 primary with smaller secondaries, weighted at 3 in the `ESPIONAGE_LOCKBOX_1-5` reward tables. Lockbox drop planets re-tiered to CZ-220/Viscara/Nar Shaddaa/Hutlar/Korriban. The Bible Equipment - Armor catalog now lists all fifteen accessories, with their Espionage stats in the Other Stats column and `Loot: <planet> (Espionage Lockbox)` as the source.
- **2026-07-18 — XP + rider completion**: stealth now grants Espionage XP (once per 30-second detection window while a hostile NPC is within 15m, scaled by a level-vs-rank delta), which also fixes the progression bootstrap - lockbox XP needed Espionage 8 and trap XP needed Espionage 12, so poison crafting was previously the only rank-0 source. Ghost Protocol's primed back attack now inflicts Exposed (20% for 30s) through new `BackAttackExposedPercent`/`BackAttackExposedDurationSeconds` stats consumed on the landed hit, and landing any damaging hit now breaks stealth (auto-attacks included), completing "breaks on hostile action".
- **2026-07-18 — Trap kits, detection, and disarm**: five craftable Snare Kits (Espionage recipes on the Engineering tab, gated by Trapcraft I-IV and Master Saboteur for tier 5) deploy *concealed* traps through a new `espn_trap` usable placeable. Concealed traps are globally hidden via the Visibility plugin and revealed per-observer to players whose Trapcraft rank meets the trap's tier within detection range (`TrapDetectionRangeBonus`, granted by Trap Management II); the owner always sees their own. Using a revealed trap runs a disarm check that finally consumes `TrapDisarm` (plus Perception, penalized by tier, clamped 5-95) - success clears the trap and grants delta-scaled Espionage XP, failure sets it off on the disarmer. Trapcraft III/IV shorten the arming delay via `TrapPlacementSpeedPercent`.
- **2026-07-18 — Workbench blueprint + palette**: `espionage_bench.utp.json` had `CRAFTING_SKILL_TYPE_ID = 32` (Engineering) copied from `engineering_term`, corrected to 49; the placed `dan_crafterbase` instance had been masking this with its own override (every workbench instance in the module carries an explicit override, so that override is convention and stays). The workbench is now in the toolset placeable palette under Crafting. The palette's "non-UTF8 byte" was not corruption — `placeablepalcus.itp.json` contains NWN `<cRGB>` color tokens with raw 0xff bytes (the "Color Tag Generator" entry), so the file must be read as latin-1/binary rather than UTF-8. `espn_trap` is deliberately *not* in the palette: it is created only at runtime by `Traps.cs`, and a hand-placed one would be an inert marker with no backing service record.
- **Still deferred**: Espionage-tab Dev Status flips (rows stay Design until playtest numbers settle), NPC trap spawns, and module repack + hak rebuild on deploy.
