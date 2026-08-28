# Mimicry Balance (Re-tune)

This document captures the ratified balance model for the Mimicry technique pool (the "Design Bible" → `Combat Upgrade` workbook, `Mimicry` tab, technique rows). All 88 techniques and all 10 core Mimicry perk ranks are mandatory Bible-review and combat-upgrade audit scope. They must never be treated as optional, excluded, or intentionally unimplemented.

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
| Damage-dealing combat actives | 27 |
| Non-damage combat actives (control / debuff / support) | 37 |
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

Every non-damage active carries **no effect-magnitude or damage-scaling attribute** (`Primary Stat = None` in the Bible); its power is a fixed-magnitude effect (duration, percent, heal/shield amount), balanced through Stamina cost, cooldown, and effect duration rather than a damage number. An internal accuracy attribute may still be declared to resolve whether a hostile control effect lands; that does not scale the effect's magnitude.

## Damage / Cost Bands

For the **damage-dealing subset**, base damage and Stamina cost use four tuning bands. These bands describe payload strength only; they are not runtime tiers and do not determine when a technique can be learned. Single-target hits use the higher damage figure in a band; area techniques (cone/sphere/line) use the lower figure, reflecting that they hit multiple targets. Non-damage actives use the same Stamina/cooldown bands but replace the damage figure with a fixed-magnitude effect.

| Power band | Single dmg | Area dmg | STM | Cooldown (single/area) |
|---|---|---|---|---|
| Starter | 12 | 10 | 3 | 12s / 15s |
| Standard | 22 | 18 | 5 | 15s / 18s |
| Advanced | 34 | 28 | 7 (+1 for area, i.e. 8) | 18s / 24s |
| Signature | 48 | 40 | 9 (+1 for area, i.e. 10) | 24s / 30s |

Damage bands target roughly **75% of a same-gate weapon active's per-cast damage**: techniques pay
technique slots and learn RNG, carry no weapon requirement, and do not scale with gear, so they sit
deliberately below dedicated weapon actives while remaining a meaningful extra button.

Each technique has an individual Mimicry requirement ordered by its earliest player-accessible source in the Design Bible's `World NPCs` tab. `Additional` and `Training` rows do not establish progression. Practical encounter order breaks nominal-level ties: CZ-220 Mynock techniques begin at rank 0, the more difficult Probe Droid techniques begin at rank 1, and the remaining starting encounters continue from there. Later source bands progress continuously through rank 40. Techniques first found in level-50 encounters are ordered by Tough, Elite, and Boss source difficulty across ranks 41–50, with apex boss techniques remaining at rank 50. Every Mimicry rank from 0 through 50 unlocks at least one technique.

Cooldowns are banded by payload strength and shape (stronger and area techniques recast slower), replacing the ad-hoc creature values inherited during generation.

### Non-damage actives (the majority)

**37 of the 64 combat actives deal no direct damage.** They span control, debuffs, resource disruption, threat, ally support, self-buffs, and reactive defenses. Their power is the fixed effect itself, so it is balanced through Stamina cost, cooldown, area, and duration rather than a damage number. None declares a damage-scaling attribute (`Primary Stat = None`). The three stances are counted separately. No fear effects are used anywhere in the pool.

### Passive trait techniques

Twenty-one techniques are **passive traits** rather than activated abilities. They do not appear on the hotbar, cost no Stamina, and deal no direct damage; equipping one contributes static stats for as long as it occupies a technique slot (see the `IsMimicryTrait` model). A trait's power *is* its passive effect, listed here.

Traits declare their bonuses on the ability builder (`MimicryTraitStat` / `MimicryTraitResistance`) and the stat and resistance pipelines read them straight off the equipped loadout. Equipping a trait deliberately applies **no persistent status effect to the wearer**: the bonus never varies while the trait is slotted, so there is no transient state for the status icon bar to communicate, and no apply/remove lifecycle that could drift out of sync with the equipped set. The elemental-resonance set bonus is derived the same way.

That is a statement about the trait's own lifecycle, not about what it does in combat. An on-hit proc trait still inflicts an ordinary status effect on its *target* when it fires (see below); those are transient, carry their own icons, and are unaffected by this.

Each trait grants a distinct effect profile, and stronger payload bands provide larger bonuses. Two flavours exist:

- **On-hit procs** — a percent chance for a landed hit to inflict a status effect. Elemental DoT procs (Poison/Shock/Freezing) scale roughly from 12% to 15–18%. The debuff families (Bleed, Hemorrhage, Sunder) run at **half those rates** (6%, 9–10%, then 12%): their payloads scale with target max HP or strip defenses, so at equal chances they dwarf every perk-priced passive against elite/boss targets. Halved, their steady-state uptime lands near 25–45% instead of 55–85%.
- **Flat buffs** — a permanent stat bonus, scaling roughly from +4% through +6% to +8%.

Trait proc chances read the shared `DamageDealt*Chance` stats consumed by `Combat.ApplyDamageDealtMimicryTraitProcs`; nothing about the trait system special-cases a perk. Alternative versions of the same loadout role belong to a mutually exclusive trait family: only one of `Chitin Guard` / `Iron Carapace`, and only one of `Force Rend` / `Essence Scar`, may be equipped at once. The two carapace traits retain different resistance/defense profiles so the choice depends on the encounter. This keeps `Apex Collapse`'s defense penalty meaningful: one carapace can soften it, but the pair cannot erase it and leave a net defense bonus.

| Technique | Power band | Slot | Passive trait effect |
|---|---|---|---|
| Rending Bite | Starter | 1 | 6% chance to inflict Bleed |
| Crippling Talons | Starter | 1 | 6% chance to inflict Hemorrhage |
| Target Lock | Starter | 1 | +4% Accuracy |
| Bonecrusher Bite | Standard | 2 | 9% chance to inflict Sunder |
| Chitin Guard | Standard | 2 | +10% Physical Def, +15% Force Def, +20 Fire & Poison Resist |
| Force Rend | Standard | 2 | +6% Force Attack |
| Glacial Slime | Standard | 2 | 18% chance to inflict Poison |
| Hoarfrost Glob | Standard | 2 | 18% chance to inflict Freezing |
| Iron Carapace | Standard | 2 | +15% Physical Def, +10% Force Def, +25 Trauma, +15 Fire & Poison Resist |
| Mauling Bite | Standard | 2 | 9% chance to inflict Bleed |
| Mind Spike | Standard | 2 | +6% Accuracy |
| Overload Shot | Standard | 2 | 18% chance to inflict Shock |
| Precision Shot | Standard | 2 | +6% Critical Rate |
| Rending Carve | Standard | 2 | 9% chance to inflict Hemorrhage |
| Rime Pounce | Standard | 2 | 15% chance to inflict Freezing |
| Serrated Slash | Standard | 2 | 10% chance to inflict Bleed |
| Tactical Mark | Standard | 2 | +6% Attack |
| Essence Scar | Advanced | 2 | +8% Force Attack |
| Force Sunder | Advanced | 2 | 12% chance to inflict Sunder |
| Opening Cut | Advanced | 2 | 12% chance to inflict Bleed |
| Rangefinder Shot | Advanced | 2 | +8% Accuracy |

### Effect durations (match the rest of the Bible)

Durations follow the conventions used across the other skill trees, not ad-hoc creature values:

- **Soft effects default to 30 seconds** — DoTs (Bleed, Burn, Poison, Shock, Sunder…), stat debuffs (Attack/Accuracy/Defense down, suppression, slows, vulnerability marks, Force-suppression, ability lockout), self/ally buffs (Haste, Attack, Defense, shields, momentum), enmity/taunt, and reflect. This is the dominant duration in the Bible; effects should not be shorter than this without a real reason.
- **Dazed = 15 seconds**, matching every other Dazed application.
- **Hard full-lockout control = 6 seconds** — Stun, Knockdown, and solid Freeze. This is the only case with a real reason to run short: the target can take no action at all.
- **Root / ability-lockout = 15 seconds** — Immobilized (still able to act) and Weapon Jam sit between the two.

Magnitudes are likewise capped to Bible norms: Accuracy debuffs at −10%, movement slows at −18%, Attack/Defense at ±20%, Haste/Attack buffs at +15%, critical chance at +25%, damage-taken marks at +10%, reflect at 20%, and taunt as **+25% Enmity toward you for 30 seconds** (the pattern shared with Covering Strike, etc.) rather than a forced-attack charm.

Passive on-hit trait procs are a deliberate exception to the active-technique duration bands because they can trigger repeatedly without spending Stamina: Bleed and Hemorrhage last 12 seconds, Freezing 6 seconds, Shock 10 seconds, Sunder 14 seconds, and Poison 12 seconds. Freezing's Ice damage scales with twice the source's Perception modifier, its status level, Mimicry Potency when present, and the target's damage-taken modifiers. Sustain Burn overrides the shared Poison proc duration to 30 seconds while its capstone stance is active.

### Loadout economy (technique slots)

The slot budget is the primary limiter on simultaneous power, since a player can freely swap techniques out of combat and cheaply learns the entire pool. Combat Analyzer grants **2** slots; Analyzer Memory adds **+2 per rank** (3 ranks = +6) and the Overclocked Analyzer capstone adds **+2**, for a maximum of **10**. With slot costs of 1–3 (stronger payloads generally cost more), a fully-invested analyst runs roughly 4–6 techniques at once — enough to build a real per-encounter kit while still forcing choices, rather than a standing library of every effect.

### Signature mechanics

The signature payload band contains 30 combat actives and 3 stances. Eleven combat actives deal direct damage, while nineteen are control, debuff, support, threat, reactive, or self-buff tools. Their individual Mimicry requirements follow encounter order and source difficulty rather than payload strength alone. Non-damage abilities carry no damage-scaling **Primary Stat** (`None` in the Bible).

| Damaging technique | Distinct role |
|---|---|
| Final Eclipse | Aimed line; +40% damage against Weakened targets; restores FP |
| Final Line | Aimed line finisher scaling with missing HP |
| Inferno Blast | Aimed cone; +50% damage against existing Burn |
| Inner Circle Pounce | Single-target strike with +25% Critical Rate |
| Inner Circle Surge | Shock combo with three-target electrical chain |
| Inner Circle Volley | Dazed/Disoriented combo strike |
| Inner Void | Single-target damage with FP recovery |
| Lockstep Crush | Cone damage with 6-second Knockdown and 30-second Sunder |
| Merciless Angle | Bleed/Hemorrhage setup and detonation |
| Rupturing Quake | Self-centered damage, 6-second Knockdown, and 30-second Sunder |
| Scorching Breath | Cone damage with Burn and Weakened |

| Non-damage technique | Distinct role |
|---|---|
| Crossfire Drill | Cone Suppression |
| Cryo Bile | Cone Freezing/Immobilized control with enmity |
| Final Mandate | Self-centered ally Attack/Accuracy command |
| Final Suppression | Aimed-line Stun |
| Finishing Drive | Stacking Mimicry-potency momentum |
| Holdfast Slam | Single-target Sunder/Exposed setup with enmity |
| Inner Circle Bind | Interrupt, Weapon Jam, and Immobilized |
| Inner Ring Flurry | Bleed setup and STM recovery |
| Last Bastion | Self-centered ally shield and enemy enmity |
| Pressure Lock | Cone Immobilized control |
| Snap Rush | STM recovery and self-Haste |
| Terrifying Bellow | Self-centered Daze and interrupt |
| Warden Clamp | Self-centered Daze with enmity |
| Warden Mark | Self-centered Marked setup with enmity |
| Warden Maul | Self-centered pull and Knockdown with enmity |
| Warden Order | Self-centered party healing |
| Warden Rend | Self-centered Weakened, FP recovery, and enmity |
| Warden Sweep | Physical-damage reflection |
| Will Fracture | Cone Foggy Mind with FP recovery |

The three stances are **Warden Wall** (defensive aura), **Apex Collapse** (offense-for-defense trade), and **Sustain Burn** (30-second Poison on landed hits).

Mechanics reuse shared, stat-driven building blocks such as the chain, detonation, pull, heal, targeting, and status-effect services. Stances use the existing `ConfigureToggle` model; stances and non-damage utility actives are classified separately from damage abilities so their scaling contracts remain accurate.

### Per-technique reference

The authoritative per-technique specifics — name, Mimicry requirement, slot cost, role, scaling stat (or `None`), and full effect text — live in the **Design Bible workbook** (`Mimicry` tab) and its generated manifest `SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv`. The `World NPCs` tab is authoritative for source encounter level. A duplicate damage-centric table previously lived here and repeatedly drifted, so it has been removed; the passive traits are tabulated above, the signature roles are mapped above, and the other active roles are summarised below.

### Non-signature active roles

The 34 combat actives outside the signature payload band contain 16 damage techniques and 18 non-damage techniques.

- **Damage** — Raking Claws, Toxic Spit, Barbed Volley, Brutal Bash, Pouncing Strike, Sonic Shriek, Tail Sweep, Venom Spray, Blood Frenzy Flurry, Dread Wave, Goring Charge, Grenade Burst, Seismic Slam, Shrapnel Burst, Static Burst, and Toxic Cloud.
- **Non-damage control and debuff** — Frost Spit, Capacitor Surge, Concussive Challenge, Ion Burst, Piercing Quills, Savage Roar, Static Web, Suppressing Shot, Arc Pulse, Brace Breaker, Dark Shock, Disorienting Screech, Null Shock, Pack Harrier, Permafrost Rupture, Rally Breaker, and Signal Snare.
- **Non-damage support** — Stim Canister, which buffs the user and nearby allies.

## Force-Flavored Subset Note

Nine techniques carry Force-flavored names inherited from their source creatures — `Force Rend`, `Force Sunder`, `Mind Spike`, `Dark Shock`, `Null Shock`, `Dread Wave`, `Essence Scar`, `Inner Void`, and `Will Fracture`. Despite the naming, these remain ordinary members of the universal Stamina-costed technique pool described above; they are not FP-gated and are not restricted to Force-Sensitive characters.

A cosmetic re-flavor of these nine (renaming and re-theming them toward tech/analyzer language consistent with the rest of Mimicry, e.g. "the analyzer replicates a psionic disruption pulse" rather than an explicitly Force-branded effect) is an acknowledged open follow-up. It is a naming/flavor-text cleanup only and is not required for, and does not affect, the balance model in this document.
