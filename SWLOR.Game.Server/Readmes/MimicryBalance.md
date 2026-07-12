# Mimicry Balance (Re-tune)

This document captures the ratified balance model for the Mimicry technique pool (the "Design Bible" → `Combat Upgrade` workbook, `Mimicry` tab, technique rows). It supersedes the previous ad-hoc NPC-derived numbers with a single, consistent resource and scaling model shared with native player abilities.

## Resource Model

**Every Mimicry technique costs Stamina, for every character type — Standard and Force-Sensitive alike. There is no FP cost and no character-type gating. One identical pool for everyone.**

Rationale:
- The Mimicry analyzer is a piece of technology that studies and reproduces observed creature attacks; it replicates physical/technological combat techniques, not the Force. Charging it against Force Points would misrepresent what the system fictionally is.
- A single resource avoids per-character-type divergence in tuning, testing, and player expectations. Every technique behaves identically regardless of who casts it — no separate FP/STM variants to design, balance, or maintain.
- It gives Force-Sensitive builds a reason to invest in Stamina instead of leaving the resource entirely to Standard characters. Mimicry becomes a shared, build-agnostic toolkit layered on top of a character's primary resource kit rather than a secondary Force-Point tax.

The handful of techniques with Force-flavored names (see below) are not exempt from this — they draw from the same universal Stamina pool as every other technique.

## Scaling Stats

Every technique's damage now scales off exactly one of the six core attributes, matching how native abilities scale via `CombatImpactDamageAbility`. Previously, technique damage was a flat value copied from the source NPC/creature with no player-stat scaling at all; this re-tune brings Mimicry in line with the rest of the ability system.

Assignment rule — the stat is chosen by the technique's combat fantasy, not arbitrarily:

| Combat fantasy | Stat |
|---|---|
| Mental / Force / fear / sonic effects | Willpower (WIL) |
| Command / coordination / pack-tactics effects | Social (SOC) |
| Defensive hardening / self-buff effects | Vitality (VIT) |
| Aimed / ranged / thrown / chemical / energy attacks | Perception (PER) |
| Finesse / speed / flurry attacks | Agility (AGI) |
| Brute melee attacks | Might (MGT) |

Resulting distribution across all 88 techniques (all six attributes represented):

| Stat | Count |
|---|---|
| MGT | 27 |
| PER | 19 |
| SOC | 13 |
| WIL | 12 |
| AGI | 11 |
| VIT | 6 |
| **Total** | **88** |

## Damage / Cost Tiers

Base damage and Stamina cost are banded by tier. Single-target hits use the higher damage figure in a band; area techniques (cone/sphere/line) use the lower figure, reflecting that they hit multiple targets:

| Tier | Single dmg | Area dmg | STM | Cooldown (single/area) |
|---|---|---|---|---|
| T1 | 10 | 8 | 3 | 12s / 15s |
| T2 | 16 | 13 | 5 | 15s / 18s |
| T3 | 24 | 20 | 7 (+1 for area, i.e. 8) | 18s / 24s |
| T4 | 32 | 28 | 9 (+1 for area, i.e. 10) | 24s / 30s |

Learn gates (Mimicry skill requirement) scale with tier: **T1 = Mimicry 0, T2 = Mimicry 15, T3 = Mimicry 30, T4 = Mimicry 45.**

Cooldowns are banded by tier and shape (higher tiers and area techniques recast slower), replacing the ad-hoc creature values inherited during generation.

### Utility / crowd-control techniques

Two active techniques deal no direct damage — `Savage Roar` (debuff) and `Terrifying Bellow` (AoE fear). Their power is the effect itself, whose magnitude is a fixed, shared status-effect class also used by the source creatures. Per the stat-driven-gameplay rule, those shared magnitudes are **not** re-scaled per-caster (doing so would special-case shared infrastructure and skew NPC balance), so a damage-scaling attribute is meaningless for them. They are instead balanced through the same levers as any utility ability — Stamina cost, cooldown, and effect duration, all normalized to their tier band. Their attribute assignment is retained as thematic metadata only.

### Passive trait techniques

Twenty-one techniques are **passive traits** rather than activated abilities. They do not appear on the hotbar, cost no Stamina, and deal no direct damage; equipping one applies a permanent status effect for as long as it occupies a technique slot (see the `IsMimicryTrait` model). Because they are passive, the damage/Stat/STM columns of the per-technique table below do not apply to trait rows — a trait's power *is* its passive effect, listed here.

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

### Hard crowd-control durations

Effects copied from creatures carried creature-length durations that are oppressive in player hands (a 14-second knockdown, for instance). Hard full-lockout control is capped to player-appropriate lengths: **Knockdown and Stun to 3 seconds** (matching native player knockdowns) and **Dazed to 6 seconds**. Soft debuffs that still let the target act — Terrified (−8% Attack/Defense), Immobilized (root), Hamstring, Disoriented, Weakened, Vulnerable — keep their longer durations, since they are not lockouts.

### Loadout economy (technique slots)

The slot budget is the primary limiter on simultaneous power, since a player can freely swap techniques out of combat and cheaply learns the entire pool. Combat Analyzer grants **2** slots; Analyzer Memory adds **+1 per rank** (3 ranks) for a maximum of **5**. With slot costs of 1–3 (rising with tier), a fully-invested analyst runs roughly 2–3 techniques at once — a deliberate jack-of-all-trades kit that must be tailored per encounter rather than a standing library of every effect.

### Full per-technique table

> Note: the 21 passive traits listed in *Passive trait techniques* above are included below for tier/stat provenance, but their **Base Dmg** and **STM** figures are historical (from when they were active abilities) and no longer apply — traits are passive, cost no Stamina, and deal no direct damage. Use the trait table for their live values.

| Technique | Tier | Stat | Base Dmg | STM |
|---|---|---|---|---|
| Crippling Talons | T1 | MGT | 10 | 3 |
| Frost Spit | T1 | PER | 10 | 3 |
| Raking Claws | T1 | AGI | 10 | 3 |
| Rending Bite | T1 | MGT | 10 | 3 |
| Target Lock | T1 | SOC | 10 | 3 |
| Toxic Spit | T1 | PER | 10 | 3 |
| Barbed Volley | T2 | PER | 13 | 5 |
| Bonecrusher Bite | T2 | MGT | 16 | 5 |
| Brutal Bash | T2 | MGT | 16 | 5 |
| Capacitor Surge | T2 | PER | 13 | 5 |
| Chitin Guard | T2 | VIT | 0 | 5 |
| Concussive Challenge | T2 | SOC | 13 | 5 |
| Force Rend | T2 | WIL | 16 | 5 |
| Glacial Slime | T2 | MGT | 16 | 5 |
| Hoarfrost Glob | T2 | PER | 16 | 5 |
| Ion Burst | T2 | PER | 13 | 5 |
| Iron Carapace | T2 | VIT | 0 | 5 |
| Mauling Bite | T2 | MGT | 16 | 5 |
| Mind Spike | T2 | WIL | 16 | 5 |
| Overload Shot | T2 | PER | 16 | 5 |
| Piercing Quills | T2 | PER | 13 | 5 |
| Pouncing Strike | T2 | MGT | 16 | 5 |
| Precision Shot | T2 | PER | 16 | 5 |
| Rending Carve | T2 | AGI | 16 | 5 |
| Rime Pounce | T2 | AGI | 16 | 5 |
| Savage Roar | T2 | MGT | 0 | 5 |
| Serrated Slash | T2 | AGI | 16 | 5 |
| Sonic Shriek | T2 | WIL | 13 | 5 |
| Static Web | T2 | PER | 13 | 5 |
| Suppressing Shot | T2 | PER | 13 | 5 |
| Tactical Mark | T2 | SOC | 16 | 5 |
| Tail Sweep | T2 | MGT | 13 | 5 |
| Venom Spray | T2 | PER | 13 | 5 |
| Arc Pulse | T3 | PER | 20 | 8 |
| Blood Frenzy Flurry | T3 | AGI | 20 | 8 |
| Brace Breaker | T3 | VIT | 24 | 7 |
| Dark Shock | T3 | WIL | 20 | 8 |
| Disorienting Screech | T3 | WIL | 20 | 8 |
| Dread Wave | T3 | WIL | 20 | 8 |
| Essence Scar | T3 | WIL | 24 | 7 |
| Force Sunder | T3 | WIL | 24 | 7 |
| Goring Charge | T3 | MGT | 20 | 8 |
| Grenade Burst | T3 | PER | 20 | 8 |
| Null Shock | T3 | WIL | 20 | 8 |
| Opening Cut | T3 | AGI | 24 | 7 |
| Pack Harrier | T3 | SOC | 24 | 7 |
| Permafrost Rupture | T3 | MGT | 20 | 8 |
| Rally Breaker | T3 | SOC | 24 | 7 |
| Rangefinder Shot | T3 | PER | 24 | 7 |
| Seismic Slam | T3 | MGT | 20 | 8 |
| Shrapnel Burst | T3 | PER | 20 | 8 |
| Signal Snare | T3 | SOC | 24 | 7 |
| Static Burst | T3 | PER | 20 | 8 |
| Stim Canister | T3 | MGT | 20 | 8 |
| Toxic Cloud | T3 | PER | 20 | 8 |
| Apex Collapse | T4 | MGT | 28 | 10 |
| Crossfire Drill | T4 | AGI | 28 | 10 |
| Cryo Bile | T4 | PER | 28 | 10 |
| Final Eclipse | T4 | MGT | 28 | 10 |
| Final Line | T4 | MGT | 28 | 10 |
| Final Mandate | T4 | MGT | 28 | 10 |
| Final Suppression | T4 | MGT | 28 | 10 |
| Finishing Drive | T4 | AGI | 28 | 10 |
| Holdfast Slam | T4 | VIT | 32 | 9 |
| Inferno Blast | T4 | MGT | 28 | 10 |
| Inner Circle Bind | T4 | SOC | 32 | 9 |
| Inner Circle Pounce | T4 | SOC | 32 | 9 |
| Inner Circle Surge | T4 | SOC | 32 | 9 |
| Inner Circle Volley | T4 | SOC | 32 | 9 |
| Inner Ring Flurry | T4 | AGI | 32 | 9 |
| Inner Void | T4 | WIL | 32 | 9 |
| Last Bastion | T4 | VIT | 28 | 10 |
| Lockstep Crush | T4 | AGI | 28 | 10 |
| Merciless Angle | T4 | SOC | 28 | 10 |
| Pressure Lock | T4 | MGT | 28 | 10 |
| Rupturing Quake | T4 | MGT | 28 | 10 |
| Scorching Breath | T4 | MGT | 28 | 10 |
| Snap Rush | T4 | AGI | 28 | 10 |
| Sustain Burn | T4 | MGT | 28 | 10 |
| Terrifying Bellow | T4 | WIL | 0 | 10 |
| Warden Clamp | T4 | MGT | 28 | 10 |
| Warden Mark | T4 | SOC | 28 | 10 |
| Warden Maul | T4 | MGT | 28 | 10 |
| Warden Order | T4 | SOC | 28 | 10 |
| Warden Rend | T4 | MGT | 28 | 10 |
| Warden Sweep | T4 | MGT | 28 | 10 |
| Warden Wall | T4 | VIT | 28 | 10 |
| Will Fracture | T4 | WIL | 28 | 10 |

## Force-Flavored Subset Note

Nine techniques carry Force-flavored names inherited from their source creatures — `Force Rend`, `Force Sunder`, `Mind Spike`, `Dark Shock`, `Null Shock`, `Dread Wave`, `Essence Scar`, `Inner Void`, and `Will Fracture`. Despite the naming, these remain ordinary members of the universal Stamina-costed technique pool described above; they are not FP-gated and are not restricted to Force-Sensitive characters.

A cosmetic re-flavor of these nine (renaming and re-theming them toward tech/analyzer language consistent with the rest of Mimicry, e.g. "the analyzer replicates a psionic disruption pulse" rather than an explicitly Force-branded effect) is an acknowledged open follow-up. It is a naming/flavor-text cleanup only and is not required for, and does not affect, the balance model in this document.
