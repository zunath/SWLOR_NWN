# Mimicry Balance (Re-tune)

This document captures the ratified balance model for the Mimicry technique pool (the "Design Bible" → `Combat Upgrade` workbook, `Mimicry` tab, technique rows). It supersedes the previous ad-hoc NPC-derived numbers with a single, consistent resource and scaling model shared with native player abilities.

## Resource Model

**Every Mimicry technique costs Stamina, for every character type — Standard and Force-Sensitive alike. There is no FP cost and no character-type gating. One identical pool for everyone.**

Rationale:
- The Mimicry analyzer is a piece of technology that studies and reproduces observed creature attacks; it replicates physical/technological combat techniques, not the Force. Charging it against Force Points would misrepresent what the system fictionally is.
- A single resource avoids per-character-type divergence in tuning, testing, and player expectations. Every technique behaves identically regardless of who casts it — no separate FP/STM variants to design, balance, or maintain.
- It gives Force-Sensitive builds a reason to invest in Stamina instead of leaving the resource entirely to Standard characters. Mimicry becomes a shared, build-agnostic toolkit layered on top of a character's primary resource kit rather than a secondary Force-Point tax.

The handful of techniques with Force-flavored names (see below) are not exempt from this — they draw from the same universal Stamina pool as every other technique.

## Composition & scaling

The pool is deliberately **not** a set of interchangeable damage nukes. Most active techniques deal no direct damage at all — they are control, debuffs, support, persistent zones, or stances — so that each technique is a distinct *kind* of ability rather than "damage in a shape with a different status." Composition of the 88-technique pool:

| Group | Count |
|---|---|
| Passive traits | 21 |
| Stances | 3 |
| Damage-dealing actives | 21 |
| Non-damage actives (control / debuff / support / zone) | 43 |
| **Total** | **88** |

Only the **damage-dealing subset** scales off a core attribute (via `CombatImpactDamageAbility`), chosen by combat fantasy:

| Combat fantasy | Stat |
|---|---|
| Mental / Force / sonic effects | Willpower (WIL) |
| Command / coordination / pack-tactics effects | Social (SOC) |
| Defensive / self-buff effects | Vitality (VIT) |
| Aimed / ranged / thrown / chemical / energy attacks | Perception (PER) |
| Finesse / speed / flurry attacks | Agility (AGI) |
| Brute melee attacks | Might (MGT) |

Every non-damage active carries **no scaling attribute** (`Primary Stat = None` in the Bible); its power is a fixed-magnitude effect (duration, percent, heal/shield amount), balanced through Stamina cost, cooldown, and effect duration rather than a damage number.

## Damage / Cost Tiers

For the **damage-dealing subset**, base damage and Stamina cost are banded by tier. Single-target hits use the higher damage figure in a band; area techniques (cone/sphere/line) use the lower figure, reflecting that they hit multiple targets. Non-damage actives use the same Stamina/cooldown bands but replace the damage figure with a fixed-magnitude effect.

| Tier | Single dmg | Area dmg | STM | Cooldown (single/area) |
|---|---|---|---|---|
| T1 | 10 | 8 | 3 | 12s / 15s |
| T2 | 16 | 13 | 5 | 15s / 18s |
| T3 | 24 | 20 | 7 (+1 for area, i.e. 8) | 18s / 24s |
| T4 | 32 | 28 | 9 (+1 for area, i.e. 10) | 24s / 30s |

Learn gates (Mimicry skill requirement) scale with tier: **T1 = Mimicry 0, T2 = Mimicry 15, T3 = Mimicry 30, T4 = Mimicry 45.**

Cooldowns are banded by tier and shape (higher tiers and area techniques recast slower), replacing the ad-hoc creature values inherited during generation.

### Non-damage actives (the majority)

**43 of the 64 active techniques deal no direct damage.** They span control (roots, stuns, freezes, knockdowns), debuffs (armor-strip, accuracy/attack-down, caster-accuracy debuff, vulnerability marks, ability lockout, resource drain), support (ally buffs/heals, taunts, riposte), and persistent zones (root/stun/burning fields). Their power is the effect itself at a fixed magnitude — never re-scaled per-caster — so it is balanced through Stamina cost, cooldown, and effect duration rather than a damage number. None declares a scaling attribute (`Primary Stat = None`). No fear effects are used anywhere in the pool.

### Passive trait techniques

Twenty-one techniques are **passive traits** rather than activated abilities. They do not appear on the hotbar, cost no Stamina, and deal no direct damage; equipping one applies a permanent status effect for as long as it occupies a technique slot (see the `IsMimicryTrait` model). A trait's power *is* its passive effect, listed here.

Each trait grants a **unique** effect, and effect strength scales with tier so a higher-tier trait is always worth more than a lower-tier one (no two traits are interchangeable). Two flavours exist:

- **On-hit procs** — a percent chance for a landed hit to inflict a status effect, scaling roughly T1 = 12%, T2 = 18%, T3 = 24% (with a couple of intentional off-band values to keep same-tier procs of one element distinct).
- **Flat buffs** — a permanent stat bonus, scaling roughly T1 = +4%, T2 = +6%, T3 = +8%.

Trait proc chances read the shared `DamageDealt*Chance` stats consumed by `Combat.ApplyDamageDealtMimicryTraitProcs`; nothing about the trait system special-cases a perk. The two defensive traits (`Chitin Guard`, `Iron Carapace`) are deliberately given **different** resistance/defense profiles so they are complementary rather than identical.

| Technique | Tier | Slot | Passive trait effect |
|---|---|---|---|
| Rending Bite | T1 | 1 | 12% chance to inflict Bleed |
| Crippling Talons | T1 | 1 | 12% chance to inflict Hemorrhage |
| Target Lock | T1 | 1 | +4% Accuracy |
| Bonecrusher Bite | T2 | 2 | 18% chance to inflict Sunder |
| Chitin Guard | T2 | 2 | +10% Physical Def, +15% Force Def, +20 Fire & Poison Resist |
| Force Rend | T2 | 2 | +6% Force Attack |
| Glacial Slime | T2 | 2 | 18% chance to inflict Poison |
| Hoarfrost Glob | T2 | 2 | 18% chance to inflict Freezing |
| Iron Carapace | T2 | 2 | +15% Physical Def, +10% Force Def, +25 Trauma, +15 Fire & Poison Resist |
| Mauling Bite | T2 | 2 | 18% chance to inflict Bleed |
| Mind Spike | T2 | 2 | +6% Accuracy |
| Overload Shot | T2 | 2 | 18% chance to inflict Shock |
| Precision Shot | T2 | 2 | +6% Critical Rate |
| Rending Carve | T2 | 2 | 18% chance to inflict Hemorrhage |
| Rime Pounce | T2 | 2 | 15% chance to inflict Freezing |
| Serrated Slash | T2 | 2 | 20% chance to inflict Bleed |
| Tactical Mark | T2 | 2 | +6% Attack |
| Essence Scar | T3 | 2 | +8% Force Attack |
| Force Sunder | T3 | 2 | 24% chance to inflict Sunder |
| Opening Cut | T3 | 2 | 24% chance to inflict Bleed |
| Rangefinder Shot | T3 | 2 | +8% Accuracy |

### Effect durations (match the rest of the Bible)

Durations follow the conventions used across the other skill trees, not ad-hoc creature values:

- **Soft effects default to 30 seconds** — DoTs (Bleed, Burn, Poison, Shock, Sunder…), stat debuffs (Attack/Accuracy/Defense down, suppression, slows, vulnerability marks, Force-suppression, ability lockout), self/ally buffs (Haste, Attack, Defense, shields, momentum), enmity/taunt, and reflect. This is the dominant duration in the Bible; effects should not be shorter than this without a real reason.
- **Dazed = 15 seconds**, matching every other Dazed application.
- **Hard full-lockout control = 6 seconds** — Stun, Knockdown, and solid Freeze. This is the only case with a real reason to run short: the target can take no action at all.
- **Root / ability-lockout = 15 seconds** — Immobilized (still able to act) and Weapon Jam sit between the two.

Magnitudes are likewise capped to Bible norms: Accuracy debuffs at −10%, movement slows at −18%, Attack/Defense at ±20%, Haste/Attack buffs at +15%, critical chance at +25%, damage-taken marks at +10%, reflect at 20%, and taunt as **+25% Enmity toward you for 30 seconds** (the pattern shared with Covering Strike, etc.) rather than a forced-attack charm.

### Loadout economy (technique slots)

The slot budget is the primary limiter on simultaneous power, since a player can freely swap techniques out of combat and cheaply learns the entire pool. Combat Analyzer grants **2** slots; Analyzer Memory adds **+1 per rank** (3 ranks) for a maximum of **5**. With slot costs of 1–3 (rising with tier), a fully-invested analyst runs roughly 2–3 techniques at once — a deliberate jack-of-all-trades kit that must be tailored per encounter rather than a standing library of every effect.

### Tier-4 signature mechanics

Tier-4 (Mimicry 45–50) techniques are the capstone of the pool. The design goal is that **each is a distinct *kind* of ability, not a damage nuke with a different garnish** — so only ~10 deal meaningful direct damage; the other ~23 are built around a non-damage core (control, debuff, support, threat, reactive, self-buff, or a stance). Non-damage abilities carry no damage-scaling **Primary Stat** (`None` in the Bible). Grouped by core identity:

**Direct damage (9)** — each a distinct damage archetype
| Technique | Core |
|---|---|
| Inner Circle Pounce | Precision strike — greatly increased critical chance |
| Inner Circle Surge | Chain lightning — arcs to up to 3 nearby enemies |
| Inner Circle Volley | Rapid 3-shot volley on a single target |
| Lockstep Crush | Crowd-scaling — more damage per additional enemy struck |
| Merciless Angle | Detonator — consumes Bleed/Hemorrhage; little vs unafflicted |
| Final Line | Missing-HP finisher (≤ +35%) |
| Final Eclipse | Delayed nova |
| Inferno Blast | Persistent burning damage field |
| Inner Void | Lifesteal + FP siphon |

**Damage-over-time (1)** · Scorching Breath — contagious Burn that spreads across a group

**Hard control, no direct damage (4)** · Holdfast Slam (single stun) · Cryo Bile (AoE freeze) · Warden Maul (pull/gather + knockdown) · Rupturing Quake (delayed knockdown)

**Caster disruption (1)** · Will Fracture (−20% Ability Accuracy debuff; the earlier AoE charm was dropped as over-strong and without Bible precedent)

**Persistent zones (2)** · Warden Clamp (root field) · Final Suppression (shock/stun field)

**Debuff / setup, no damage (5)** · Inner Circle Bind (interrupt + disable-lock) · Inner Ring Flurry (armor-strip / guard-break) · Warden Mark (vulnerability mark) · Pressure Lock (control-amplifier — deepens existing CC) · Crossfire Drill (suppression — cuts enemy Attack/Accuracy)

**Threat / tank (2)** · Warden Rend (pure taunt) · Last Bastion (ally-guard + taunt)

**Support / party (2)** · Final Mandate (buffs allies) · Warden Order (heals allies)

**Reactive (1)** · Warden Sweep (riposte / reflect melee)

**Self-buff & economy (2 + 3 stances ⬥)** · Finishing Drive (stacking momentum) · Snap Rush (self-haste + Stamina) · **Warden Wall** ⬥ (defensive stance) · **Apex Collapse** ⬥ (offensive stance) · **Sustain Burn** ⬥ (caustic stance)

**Setup nova (1)** · Terrifying Bellow — mass interrupt + Daze (no fear, no damage)

Mechanics reuse shared, stat-driven building blocks (chain/detonate/hazard-pulse/pull/heal helpers on `InnateAbility` and the `CombatAreaPulses`/`AbilityTargeting` services) so nothing special-cases a perk. Stances use the existing `ConfigureToggle` model; stances and the non-damage utility actives are classified so the contract tests exempt them (like passive traits) from the hostile/damage-element/scaling assertions.

### Per-technique reference

The authoritative per-technique specifics — name, tier, slot cost, role, scaling stat (or `None`), and full effect text — live in the **Design Bible workbook** (`Mimicry` tab) and its generated manifest `SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv`. That is the single source of truth. A duplicate damage-centric table previously lived here and repeatedly drifted, so it has been removed; the passive traits are tabulated above, the Tier-4 roles are mapped above, and the pre-45 active roles are summarised below.

### Pre-45 active roles

The 34 pre-45 active techniques get the same treatment as Tier-4 — spread across kinds (~15 deal damage, ~19 non-damage), all with specific numbers. By role:

- **Damage** — Raking Claws (basic strike), Pouncing Strike (opener), Brutal Bash (hit + stun), Sonic Shriek (cone), Tail Sweep (AoE + knockdown), Blood Frenzy Flurry (multi-hit), Goring Charge (line), Grenade Burst (AoE fire), Seismic Slam (AoE + knockdown), Shrapnel Burst (cone + Sunder), Static Burst (chain)
- **Damage-over-time** — Toxic Spit, Venom Spray & Barbed Volley (Bleed), Toxic Cloud (poison zone)
- **Control** — Frost Spit (chill), Static Web (root), Capacitor Surge (ability lockout), Permafrost Rupture (freeze), Brace Breaker (guard-break stun), Signal Snare (root), Arc Pulse (stun)
- **Debuff** — Piercing Quills (Sunder / −Def), Ion Burst (−Accuracy), Suppressing Shot (−Attack), Savage Roar (−damage), Disorienting Screech (Disorient), Dark Shock (Force lockout), Null Shock (resource drain), Rally Breaker (vulnerability mark), Pack Harrier (slow), Dread Wave (demoralize — no fear)
- **Threat / support** — Concussive Challenge (taunt), Stim Canister (ally buff)

## Force-Flavored Subset Note

Nine techniques carry Force-flavored names inherited from their source creatures — `Force Rend`, `Force Sunder`, `Mind Spike`, `Dark Shock`, `Null Shock`, `Dread Wave`, `Essence Scar`, `Inner Void`, and `Will Fracture`. Despite the naming, these remain ordinary members of the universal Stamina-costed technique pool described above; they are not FP-gated and are not restricted to Force-Sensitive characters.

A cosmetic re-flavor of these nine (renaming and re-theming them toward tech/analyzer language consistent with the rest of Mimicry, e.g. "the analyzer replicates a psionic disruption pulse" rather than an explicitly Force-branded effect) is an acknowledged open follow-up. It is a naming/flavor-text cleanup only and is not required for, and does not affect, the balance model in this document.
