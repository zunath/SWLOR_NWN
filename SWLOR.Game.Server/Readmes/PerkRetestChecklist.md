# Perk test and retest checklist

Snapshot: September 12, 2026. Source: [perk tracker](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203). All 1,004 skill/perk/rank names matched the local Bible manifest. Row links reflect this snapshot; use the skill and perk name if the sheet is reordered.

**331 rows need verification for these changes: 51 directly changed rows and 280 combination regressions.** After the authorized tracker update and subsequent review fixes, 249 say Pass, 63 say Retest, and 19 say Not Tested. Of the 51 direct rows, 30 say Retest, 18 still say Pass, and three Deflecting Return ranks say Not Tested. The additional direct rows need verification too; the authorized live-sheet edit was limited to the original 26 rows. Combination regression means testing the listed interaction, not that the perk is independently known to be broken.

On September 12, the user requested that the 26 directly changed rows previously marked Pass be changed to Retest. The live sheet update was verified to change exactly those 26 Test Status cells; this checklist reflects that update. Preserve its other existing work: 37 additional rows already marked Retest fall outside this change-specific list, and their existing retest requirements still apply. Likewise, this list does not replace the tracker's remaining first-time testing backlog.

Use [the CSV checklist](PerkRetestChecklist.csv) to filter by skill, current status or test case. It contains every row below, the reason, full test instructions and a direct tracker link. See [the review](PerkCombinationReview.md) for fixes and automated validation.

## Test scenarios

### C1: Outgoing damage budget

Combine the listed damage bonus with positional, stance, target-status and finisher bonuses. Compare front/side/back and healthy/low-HP targets. Percentage bonuses stop at +100% of the pre-bonus damage; flat bonuses remain afterward, followed by target vulnerability and mitigation. Include ordinary attacks, queued abilities, direct casts and companion attacks where applicable.

### C2: Incoming damage budget

Combine target-status reduction with generic reduction, Guard, Leadership and resistance. The first two have an 85% combined reduction cap. Guard (up to 55%), resistance and Leadership retain their separate effects. Check tiny hits and physical-to-Force splitting: mitigation must never raise a small hit or double-apply outgoing bonuses. Test strongest ordinary Leadership source plus the Bolster Resolve recovery rider.

### C3: Continuous control and immunity

Combine Iron Grip and Scrapper Stance with hard control; verify a final maximum of 30 seconds after duration bonuses and resistance. Attempt same-type refresh, another control type, and late Ground Quake Daze-to-Knockdown conversion. No attempt may postpone the original expiration. After control ends, all hard-control types must fail for 20 seconds. Repeat with two attackers, cleansing and logout/rejoin. Soft debuffs retain their authored refresh/extension behavior. Separately, verify that the self-applied subdual penalty retains its authored 60-second knockdown; the combat cap must not shorten it.

### C4: Periodic damage mitigation

Apply the listed Bleed, Fragmentation or Force damage-over-time effect with damage bonuses, reduction, Guard and relevant resistance. Tick damage uses the common incoming budget and source attribution. Repeat spread/refresh where available; ticks and triggered damage must not recursively trigger auto-attack procs.

### C5: Healing and resource riders

Receive this heal with Defensive Harmony and Soul Amplification at full HP, with one HP missing, while injured and when damage-derived healing is already capped. Only actual positive HP restoration may grant Stamina/Attack. Combine Blood Weapon, Life Siphon, Soul Ascension, Vampiric Fury and Hunger of the Dark in legal builds: aggregate damage-derived healing stays within 50% of the originating damage. Include periodic and companion healing where applicable.

### C6: Independent procs and splash

Use Payload Pouch and Savage Reflexes together. Each keeps its own 15% roll; the bonuses must not merge into a 30% proc. Payload Pouch triggers only on thrown auto-attacks, dealing 8 Physical DMG to the original target and at most four other hostile enemies within 3m. Test six or more enemies, allies, a dead original target and another weapon. Splash cannot trigger further on-hit effects.

### C7: Independent Stamina discounts

Trigger Flowing Defense, Evasive Reload and Opportunist's Flow together. Expected discounts: hostile Staff -7 STM, hostile Pistol/Rifle/Throwing -7 STM, another hostile combat ability -4 STM, friendly ability 0. A ranged cast consumes the ranged/global discounts and leaves Staff -3. Check separate 30-second expiry, repeated avoidance, queued abilities, misses and minimum-zero cost.

### C8: Five-target control area

Use the ability on six or more hostile enemies inside its authored shape. At most five receive the area effect; targets outside the shape and friendly targets remain excluded. Verify ground/direction targeting, companion source attribution and the control/immunity rules in C3. Existing explicit caps on other abilities remain unchanged.

### C9: Disruption duration scope

With Disruption Expert alone, extend Foggy Mind, Force Disruption and ability-cost debuffs by 25%; unrelated Daze, Poison and Bleed receive no bonus. With Iron Grip, supported disruption effects get +45% duration while unrelated debuffs get only Iron Grip's +20%. Check Fractured Focus and Eclipse of Resolve as well as the 30-second hard-control limit.

### C10: Mimicry Daze cooldown

Verify Brace Breaker, Concussive Challenge, Suppressing Shot (Mimicry) and Tail Sweep display and enforce a 24-second cooldown. Base Daze remains 15 seconds. Shared immunity still begins when control ends; switching techniques or attackers cannot bypass it.

### C11: Triggered damage mitigation

Test this splash, pulse, retaliation or reflection against Physical and Force damage-taken reduction or vulnerability, plus generic reduction, typed Leadership and resistance. Typed reduction must apply once and share the 85% target/generic budget. Include Shielding and Dampening Field against Physical hits, Force Warding against Force hits, tiny damage values, and Physical-to-Force conversion with an explicit zero or nonzero original target adjustment. Conversion must not apply the new hit modifier twice. Triggered damage must not chain further procs.

### C12: Thrown ability secondary hits

With Ricochet Toss, a thrown ability hitting a bleeding target gets one 25% roll per cast for 12 damage to one other enemy within 5m. Check another weapon skill, a nonbleeding target, Flurry Bleed alone, multiple bleeding targets and repeated field pulses. Cluster Pouch adds 10 damage to one other enemy within 5m once per area thrown cast. The original target cannot spend the extra-target slot. Combine both perks; their budgets remain independent and a later cast starts fresh.

## Direct changes: test first

| Tracker row | Skill | Perk / rank | Current status | Test cases |
|---|---|---|---|---|
| [101](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A101:O101) | Beast Mastery | Crushing Slam I | Retest | C3, C8 |
| [106](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A106:O106) | Beast Mastery | Crushing Slam II | Retest | C3, C8 |
| [111](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A111:O111) | Beast Mastery | Ice Breath III | Retest | C3, C8 |
| [112](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A112:O112) | Beast Mastery | Crushing Slam III | Retest | C3, C8 |
| [309](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A309:O309) | Force | Creeping Terror I | Pass | C2, C4 |
| [315](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A315:O315) | Force | Creeping Terror II | Pass | C2, C4 |
| [328](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A328:O328) | Force | Creeping Terror III | Pass | C2, C4 |
| [346](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A346:O346) | Force | Reflective Barrier | Pass | C11 |
| [412](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A412:O412) | Heavy Vibroblade | Defensive Harmony | Retest | C5 |
| [434](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A434:O434) | Heavy Vibroblade | Soul Amplification | Retest | C5 |
| [449](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A449:O449) | Katar | Iron Elbows | Pass | C11 |
| [450](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A450:O450) | Katar | Whirling Guard | Pass | C11 |
| [478](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A478:O478) | Katar | Scrapheap Lockdown | Retest | C3, C8 |
| [540](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A540:O540) | Lightsaber | Epicenter | Retest | C3, C8 |
| [543](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A543:O543) | Lightsaber | Deflecting Return I | Not Tested | C11 |
| [550](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A550:O550) | Lightsaber | Deflecting Return II | Not Tested | C11 |
| [557](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A557:O557) | Lightsaber | Deflecting Return III | Not Tested | C11 |
| [580](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A580:O580) | Mimicry | Concussive Challenge | Retest | C3, C8, C10 |
| [598](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A598:O598) | Mimicry | Suppressing Shot | Retest | C3, C8, C10 |
| [600](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A600:O600) | Mimicry | Tail Sweep | Retest | C3, C8, C10 |
| [604](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A604:O604) | Mimicry | Brace Breaker | Retest | C3, C10 |
| [618](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A618:O618) | Mimicry | Seismic Slam | Retest | C3, C8 |
| [626](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A626:O626) | Mimicry | Cryo Bile | Retest | C1, C3, C8 |
| [630](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A630:O630) | Mimicry | Final Suppression | Retest | C3, C8 |
| [641](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A641:O641) | Mimicry | Lockstep Crush | Retest | C3, C8 |
| [643](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A643:O643) | Mimicry | Pressure Lock | Retest | C3, C8 |
| [648](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A648:O648) | Mimicry | Terrifying Bellow | Retest | C3, C8 |
| [649](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A649:O649) | Mimicry | Warden Clamp | Retest | C3, C8 |
| [651](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A651:O651) | Mimicry | Warden Maul | Retest | C3, C8 |
| [654](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A654:O654) | Mimicry | Warden Sweep | Pass | C11 |
| [695](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A695:O695) | Pistol | Evasive Reload | Retest | C7 |
| [792](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A792:O792) | Spear | Force Warding I | Pass | C11 |
| [798](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A798:O798) | Spear | Force Warding II | Pass | C11 |
| [799](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A799:O799) | Spear | Disruption Expert | Retest | C3, C9 |
| [806](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A806:O806) | Spear | Force Warding III | Pass | C11 |
| [821](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A821:O821) | Spear | Opportunist's Flow | Retest | C7 |
| [832](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A832:O832) | Staff | Ground Quake I | Retest | C3, C8 |
| [838](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A838:O838) | Staff | Ground Quake II | Retest | C3, C8 |
| [843](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A843:O843) | Staff | Worldbreaker | Retest | C3, C8 |
| [853](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A853:O853) | Staff | Flowing Defense | Retest | C7 |
| [863](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A863:O863) | Throwing | Payload Pouch | Retest | C6, C11 |
| [867](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A867:O867) | Throwing | Cluster Pouch | Pass | C11, C12 |
| [868](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A868:O868) | Throwing | Concussive Toss I | Retest | C3, C8 |
| [874](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A874:O874) | Throwing | Concussive Toss II | Retest | C3, C8 |
| [885](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A885:O885) | Throwing | Ricochet Toss | Pass | C11, C12 |
| [911](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A911:O911) | Twin Blade | Edge Rhythm | Pass | C11 |
| [915](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A915:O915) | Twin Blade | Tempest Bloom | Pass | C11 |
| [952](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A952:O952) | Vibroblade | Shield Bash I | Pass | C11 |
| [956](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A956:O956) | Vibroblade | Shield Bash II | Pass | C11 |
| [962](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A962:O962) | Vibroblade | Shield Bash III | Pass | C11 |
| [967](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A967:O967) | Vibroblade | Shield Bash IV | Pass | C11 |

## Combination regressions

| Tracker row | Skill | Perk / rank | Current status | Test cases |
|---|---|---|---|---|
| [20](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A20:O20) | Beast Mastery | Reward I | Pass | C5 |
| [21](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A21:O21) | Beast Mastery | Reward II | Pass | C5 |
| [22](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A22:O22) | Beast Mastery | Reward III | Pass | C5 |
| [28](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A28:O28) | Beast Mastery | Guarding Bond | Pass | C2 |
| [29](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A29:O29) | Beast Mastery | Predatory Bond | Pass | C1 |
| [50](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A50:O50) | Beast Mastery | Rending Claw I | Pass | C1, C4 |
| [55](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A55:O55) | Beast Mastery | Rending Claw II | Pass | C1, C4 |
| [60](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A60:O60) | Beast Mastery | Rending Claw III | Pass | C1, C4 |
| [63](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A63:O63) | Beast Mastery | Execute Prey | Pass | C1 |
| [65](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A65:O65) | Beast Mastery | Iron Hide I | Pass | C2 |
| [68](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A68:O68) | Beast Mastery | Guarding Roar I | Pass | C2 |
| [69](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A69:O69) | Beast Mastery | Iron Hide II | Pass | C2 |
| [73](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A73:O73) | Beast Mastery | Bodyguard's Resolve | Pass | C2 |
| [74](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A74:O74) | Beast Mastery | Guarding Roar II | Pass | C2 |
| [75](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A75:O75) | Beast Mastery | Iron Hide III | Pass | C2 |
| [78](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A78:O78) | Beast Mastery | Rampart Hide | Pass | C2 |
| [79](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A79:O79) | Beast Mastery | Guarding Roar III | Pass | C2 |
| [81](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A81:O81) | Beast Mastery | Unbreakable Beast | Pass | C2 |
| [82](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A82:O82) | Beast Mastery | Claw I | Pass | C1, C4 |
| [83](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A83:O83) | Beast Mastery | Bolster Attack I | Retest | C1 |
| [84](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A84:O84) | Beast Mastery | Guarded Bite I | Pass | C2 |
| [86](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A86:O86) | Beast Mastery | Claw II | Pass | C1, C4 |
| [88](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A88:O88) | Beast Mastery | Bolster Attack II | Retest | C1 |
| [89](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A89:O89) | Beast Mastery | Coordinated Strike I | Retest | C1 |
| [90](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A90:O90) | Beast Mastery | Guarded Bite II | Pass | C2 |
| [92](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A92:O92) | Beast Mastery | Claw III | Pass | C1, C4 |
| [94](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A94:O94) | Beast Mastery | Bolster Attack III | Retest | C1 |
| [95](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A95:O95) | Beast Mastery | Coordinated Strike II | Retest | C1 |
| [97](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A97:O97) | Beast Mastery | Guarded Bite III | Pass | C2 |
| [98](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A98:O98) | Beast Mastery | Alpha Rhythm | Pass | C1, C2 |
| [100](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A100:O100) | Beast Mastery | Ice Breath I | Retest | C3 |
| [105](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A105:O105) | Beast Mastery | Ice Breath II | Retest | C3 |
| [115](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A115:O115) | Beast Mastery | Primal Overrun | Retest | C1 |
| [134](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A134:O134) | Beast Mastery | Innervate I | Pass | C5 |
| [135](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A135:O135) | Beast Mastery | Warding Howl I | Pass | C2 |
| [138](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A138:O138) | Beast Mastery | Psychic Cry I | Pass | C2 |
| [139](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A139:O139) | Beast Mastery | Innervate II | Pass | C5 |
| [140](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A140:O140) | Beast Mastery | Warding Howl II | Pass | C2 |
| [143](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A143:O143) | Beast Mastery | Mindful Hide | Not Tested | C2 |
| [144](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A144:O144) | Beast Mastery | Psychic Cry II | Pass | C2 |
| [145](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A145:O145) | Beast Mastery | Innervate III | Pass | C5 |
| [146](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A146:O146) | Beast Mastery | Warding Howl III | Pass | C2 |
| [148](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A148:O148) | Beast Mastery | Psychic Cry III | Pass | C2 |
| [149](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A149:O149) | Beast Mastery | Force-Bonded Beast | Pass | C2 |
| [150](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A150:O150) | Devices | Frag Grenade I | Pass | C1, C4 |
| [152](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A152:O152) | Devices | Concussion Grenade I | Pass | C3 |
| [154](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A154:O154) | Devices | Frag Grenade II | Pass | C1, C4 |
| [155](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A155:O155) | Devices | Ion Grenade I | Pass | C1 |
| [158](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A158:O158) | Devices | Concussion Grenade II | Pass | C3 |
| [161](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A161:O161) | Devices | Ion Grenade II | Pass | C1 |
| [162](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A162:O162) | Devices | Frag Grenade III | Pass | C1, C4 |
| [167](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A167:O167) | Devices | Beacon Targeting I | Pass | C1 |
| [169](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A169:O169) | Devices | Remote Charge I | Pass | C3 |
| [174](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A174:O174) | Devices | Remote Charge II | Pass | C3 |
| [178](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A178:O178) | Devices | Beacon Targeting II | Pass | C1 |
| [187](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A187:O187) | Devices | Dampening Field I | Pass | C2 |
| [190](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A190:O190) | Devices | Overclock Routine | Pass | C5 |
| [192](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A192:O192) | Devices | Dampening Field II | Pass | C2 |
| [195](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A195:O195) | Devices | Emergency Bunker | Pass | C2 |
| [196](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A196:O196) | Devices | Flamethrower I | Pass | C1 |
| [197](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A197:O197) | Devices | Wrist Rocket I | Pass | C1, C3 |
| [198](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A198:O198) | Devices | Sonic Burst I | Pass | C1 |
| [200](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A200:O200) | Devices | Arc Projector I | Pass | C1 |
| [201](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A201:O201) | Devices | Flamethrower II | Pass | C1 |
| [202](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A202:O202) | Devices | Ion Lance I | Pass | C1 |
| [203](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A203:O203) | Devices | Rail Dart I | Pass | C1, C4 |
| [205](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A205:O205) | Devices | Wrist Rocket II | Pass | C1, C3 |
| [206](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A206:O206) | Devices | Sonic Burst II | Pass | C1 |
| [207](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A207:O207) | Devices | Cryo Sprayer | Pass | C1 |
| [208](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A208:O208) | Devices | Arc Projector II | Pass | C1 |
| [209](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A209:O209) | Devices | Ion Lance II | Pass | C1 |
| [210](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A210:O210) | Devices | Flamethrower III | Pass | C1 |
| [211](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A211:O211) | Devices | Rail Dart II | Pass | C1, C4 |
| [212](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A212:O212) | Devices | Wrist Rocket III | Pass | C1, C3 |
| [213](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A213:O213) | Devices | Sonic Burst III | Pass | C1 |
| [214](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A214:O214) | Devices | Rail Dart III | Pass | C1, C4 |
| [215](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A215:O215) | Devices | Arc Projector III | Pass | C1 |
| [216](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A216:O216) | Devices | Ion Lance III | Pass | C1 |
| [217](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A217:O217) | Devices | Overload Barrage | Pass | C1, C3 |
| [229](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A229:O229) | Espionage | Back Attack I | Pass | C1 |
| [233](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A233:O233) | Espionage | Back Attack II | Not Tested | C1 |
| [240](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A240:O240) | Espionage | Back Attack III | Not Tested | C1 |
| [249](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A249:O249) | Espionage | Razor Trap I | Not Tested | C1, C4 |
| [258](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A258:O258) | Espionage | Razor Trap II | Not Tested | C1, C4 |
| [273](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A273:O273) | First Aid | Med Kit I | Pass | C5 |
| [274](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A274:O274) | First Aid | Treatment Kit I | Pass | C1, C4 |
| [276](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A276:O276) | First Aid | Emergency Sealant | Pass | C5 |
| [277](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A277:O277) | First Aid | Kolto Mist I | Pass | C5 |
| [279](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A279:O279) | First Aid | Treatment Kit II | Pass | C1, C4 |
| [280](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A280:O280) | First Aid | Med Kit II | Pass | C5 |
| [281](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A281:O281) | First Aid | Infusion I | Pass | C5 |
| [282](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A282:O282) | First Aid | Kolto Mist II | Pass | C5 |
| [285](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A285:O285) | First Aid | Med Kit III | Pass | C5 |
| [286](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A286:O286) | First Aid | Treatment Kit III | Pass | C1, C4 |
| [287](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A287:O287) | First Aid | Emergency Triage | Pass | C5 |
| [288](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A288:O288) | First Aid | Infusion II | Pass | C5 |
| [289](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A289:O289) | First Aid | Med Kit IV | Pass | C5 |
| [291](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A291:O291) | First Aid | Shielding I | Pass | C2 |
| [292](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A292:O292) | First Aid | Coagulant I | Pass | C2 |
| [294](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A294:O294) | First Aid | Pain Suppressant I | Pass | C2 |
| [297](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A297:O297) | First Aid | Shielding II | Pass | C2 |
| [300](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A300:O300) | First Aid | Pain Suppressant II | Pass | C2 |
| [302](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A302:O302) | First Aid | Coagulant II | Pass | C2 |
| [303](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A303:O303) | First Aid | Shielding III | Pass | C2 |
| [306](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A306:O306) | First Aid | Emergency Cocktail | Pass | C2, C5 |
| [310](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A310:O310) | Force | Force Push I | Pass | C3 |
| [311](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A311:O311) | Force | Force Choke I | Retest | C3, C4 |
| [313](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A313:O313) | Force | Force Lightning I | Pass | C1 |
| [314](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A314:O314) | Force | Force Drain I | Pass | C5 |
| [318](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A318:O318) | Force | Force Choke II | Retest | C3, C4 |
| [319](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A319:O319) | Force | Force Lightning II | Pass | C1 |
| [320](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A320:O320) | Force | Force Drain II | Pass | C5 |
| [321](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A321:O321) | Force | Force Push II | Pass | C3 |
| [322](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A322:O322) | Force | Devouring Strike | Pass | C1 |
| [324](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A324:O324) | Force | Force Choke III | Retest | C3, C4 |
| [329](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A329:O329) | Force | Force Drain III | Pass | C5 |
| [331](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A331:O331) | Force | Force Lightning III | Pass | C1 |
| [333](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A333:O333) | Force | Force Push III | Pass | C3 |
| [334](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A334:O334) | Force | Force Choke IV | Retest | C3, C4 |
| [336](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A336:O336) | Force | Benevolence I | Pass | C5 |
| [339](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A339:O339) | Force | Renewal I | Pass | C5 |
| [341](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A341:O341) | Force | Serene Focus | Not Tested | C5 |
| [342](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A342:O342) | Force | Fury Stance I | Pass | C1, C2 |
| [344](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A344:O344) | Force | Benevolence II | Pass | C5 |
| [345](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A345:O345) | Force | Renewal II | Pass | C5 |
| [347](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A347:O347) | Force | Force Mend | Pass | C5 |
| [350](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A350:O350) | Force | Force Sanctuary | Pass | C2, C5 |
| [352](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A352:O352) | Force | Benevolence III | Pass | C5 |
| [353](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A353:O353) | Force | Renewal III | Pass | C5 |
| [354](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A354:O354) | Force | Fury Stance II | Pass | C1, C2 |
| [356](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A356:O356) | Force | Harmonic Restoration | Not Tested | C5 |
| [358](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A358:O358) | Force | Hunger of the Dark | Not Tested | C5 |
| [359](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A359:O359) | Force | Weaken Resolve I | Not Tested | C2 |
| [360](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A360:O360) | Force | Force Judgment I | Not Tested | C1 |
| [362](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A362:O362) | Force | Mind Trick I | Not Tested | C3 |
| [366](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A366:O366) | Force | Force Judgment II | Not Tested | C1 |
| [367](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A367:O367) | Force | Weaken Resolve II | Pass | C2 |
| [370](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A370:O370) | Force | Mind Trick II | Pass | C3 |
| [372](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A372:O372) | Force | Force Judgment III | Pass | C1 |
| [410](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A410:O410) | Heavy Vibroblade | Flash | Retest | C1 |
| [416](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A416:O416) | Heavy Vibroblade | Guardian's Resolve | Pass | C5 |
| [417](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A417:O417) | Heavy Vibroblade | Rampart | Pass | C2 |
| [422](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A422:O422) | Heavy Vibroblade | Blood Weapon | Pass | C5 |
| [423](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A423:O423) | Heavy Vibroblade | Guardian's Reaping | Pass | C5 |
| [424](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A424:O424) | Heavy Vibroblade | Absolute Defense | Pass | C2 |
| [426](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A426:O426) | Heavy Vibroblade | Essence Tap | Pass | C2 |
| [430](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A430:O430) | Heavy Vibroblade | Life Siphon | Pass | C5 |
| [433](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A433:O433) | Heavy Vibroblade | Vampiric Fury | Pass | C5 |
| [436](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A436:O436) | Heavy Vibroblade | Soul Storm | Pass | C1 |
| [441](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A441:O441) | Heavy Vibroblade | Soul Reaping | Pass | C5 |
| [442](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A442:O442) | Heavy Vibroblade | Soul Ascension | Retest | C5 |
| [443](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A443:O443) | Katar | Guard Counter I | Pass | C3 |
| [444](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A444:O444) | Katar | Iron Guard Training I | Pass | C1, C2 |
| [447](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A447:O447) | Katar | Guard Counter II | Pass | C3 |
| [448](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A448:O448) | Katar | Iron Guard Training II | Pass | C1, C2 |
| [453](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A453:O453) | Katar | Iron Guard Training III | Pass | C1, C2 |
| [457](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A457:O457) | Katar | Guard Counter III | Pass | C3 |
| [460](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A460:O460) | Katar | Adamantine Guard | Retest | C1, C2 |
| [464](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A464:O464) | Katar | Joint Lock I | Pass | C3 |
| [468](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A468:O468) | Katar | Scrapper Stance | Pass | C3 |
| [472](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A472:O472) | Katar | Joint Lock II | Pass | C3 |
| [474](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A474:O474) | Katar | Iron Grip | Retest | C3 |
| [475](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A475:O475) | Katar | Joint Lock III | Pass | C3 |
| [490](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A490:O490) | Leadership | Press the Attack I | Pass | C1 |
| [494](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A494:O494) | Leadership | Press the Attack II | Pass | C1 |
| [501](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A501:O501) | Leadership | Press the Attack III | Pass | C1 |
| [505](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A505:O505) | Leadership | Decisive Command | Pass | C1 |
| [506](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A506:O506) | Leadership | Watchful Presence I | Pass | C2 |
| [507](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A507:O507) | Leadership | Rousing Shout I | Pass | C2 |
| [509](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A509:O509) | Leadership | Bolster Resolve I | Pass | C2, C5 |
| [511](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A511:O511) | Leadership | Rousing Shout II | Pass | C2 |
| [512](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A512:O512) | Leadership | Watchful Presence II | Pass | C2 |
| [516](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A516:O516) | Leadership | Bolster Resolve II | Pass | C2, C5 |
| [518](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A518:O518) | Leadership | Rousing Shout III | Pass | C2 |
| [520](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A520:O520) | Leadership | Watchful Presence III | Pass | C2 |
| [522](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A522:O522) | Leadership | Hold the Line | Pass | C2 |
| [541](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A541:O541) | Lightsaber | Saber Ward I | Pass | C2 |
| [544](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A544:O544) | Lightsaber | Saber Ward II | Pass | C2 |
| [551](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A551:O551) | Lightsaber | Saber Ward III | Pass | C2 |
| [552](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A552:O552) | Lightsaber | Reprisal I | Pass | C3 |
| [554](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A554:O554) | Lightsaber | Center of the Storm | Not Tested | C11 |
| [555](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A555:O555) | Lightsaber | Reprisal II | Pass | C3 |
| [556](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A556:O556) | Lightsaber | Saber Ward IV | Pass | C2 |
| [558](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A558:O558) | Lightsaber | Aegis Eternal | Pass | C2, C11 |
| [559](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A559:O559) | Mimicry | Combat Analyzer I | Pass | C1 |
| [560](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A560:O560) | Mimicry | Combat Analyzer II | Pass | C1 |
| [561](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A561:O561) | Mimicry | Combat Analyzer III | Pass | C1 |
| [562](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A562:O562) | Mimicry | Combat Analyzer IV | Pass | C1 |
| [568](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A568:O568) | Mimicry | Overclocked Analyzer | Retest | C1 |
| [575](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A575:O575) | Mimicry | Barbed Volley | Pass | C1, C4 |
| [577](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A577:O577) | Mimicry | Brutal Bash | Pass | C3 |
| [590](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A590:O590) | Mimicry | Pouncing Strike | Pass | C3 |
| [603](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A603:O603) | Mimicry | Blood Frenzy Flurry | Pass | C1, C4 |
| [610](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A610:O610) | Mimicry | Goring Charge | Pass | C1, C4 |
| [615](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A615:O615) | Mimicry | Permafrost Rupture | Pass | C1 |
| [616](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A616:O616) | Mimicry | Rally Breaker | Pass | C2 |
| [627](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A627:O627) | Mimicry | Final Eclipse | Pass | C1 |
| [628](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A628:O628) | Mimicry | Final Line | Pass | C1 |
| [631](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A631:O631) | Mimicry | Finishing Drive | Retest | C1 |
| [633](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A633:O633) | Mimicry | Inferno Blast | Pass | C1 |
| [634](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A634:O634) | Mimicry | Inner Circle Bind | Retest | C3 |
| [636](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A636:O636) | Mimicry | Inner Circle Surge | Pass | C1 |
| [637](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A637:O637) | Mimicry | Inner Circle Volley | Pass | C1, C3 |
| [638](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A638:O638) | Mimicry | Inner Ring Flurry | Pass | C1, C4 |
| [642](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A642:O642) | Mimicry | Merciless Angle | Pass | C1, C2, C4 |
| [644](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A644:O644) | Mimicry | Rupturing Quake | Pass | C3 |
| [650](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A650:O650) | Mimicry | Warden Mark | Pass | C2 |
| [652](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A652:O652) | Mimicry | Warden Order | Pass | C5 |
| [691](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A691:O691) | Pistol | Duelist's Distance | Pass | C1 |
| [720](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A720:O720) | Rifle | Sustained Fire I | Pass | C1 |
| [726](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A726:O726) | Rifle | Sustained Fire II | Pass | C1 |
| [731](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A731:O731) | Rifle | Containment Net | Retest | C1 |
| [734](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A734:O734) | Rifle | Sustained Fire III | Retest | C1 |
| [767](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A767:O767) | Saberstaff | Balanced Attunement | Not Tested | C1 |
| [777](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A777:O777) | Saberstaff | Tempest Focus | Not Tested | C1 |
| [779](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A779:O779) | Saberstaff | Tempest Stance | Not Tested | C1 |
| [803](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A803:O803) | Spear | Fracture Strike | Pass | C1 |
| [815](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A815:O815) | Spear | Vigor Stance | Pass | C1 |
| [829](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A829:O829) | Staff | Rib Breaker I | Pass | C3 |
| [831](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A831:O831) | Staff | Heavy Hands | Pass | C1 |
| [833](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A833:O833) | Staff | Crusher Stance | Pass | C1 |
| [837](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A837:O837) | Staff | Rib Breaker II | Pass | C3 |
| [840](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A840:O840) | Staff | Rib Breaker III | Pass | C3 |
| [847](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A847:O847) | Staff | Leg Sweep I | Pass | C3 |
| [855](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A855:O855) | Staff | Leg Sweep II | Pass | C3 |
| [858](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A858:O858) | Staff | Leg Sweep III | Pass | C3 |
| [864](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A864:O864) | Throwing | Shrapnel Casing I | Pass | C4 |
| [865](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A865:O865) | Throwing | Flash Toss I | Pass | C3 |
| [869](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A869:O869) | Throwing | Ordnance Stance | Pass | C1 |
| [870](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A870:O870) | Throwing | Shrapnel Casing II | Pass | C4 |
| [873](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A873:O873) | Throwing | Flash Toss II | Pass | C3 |
| [876](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A876:O876) | Throwing | Flash Toss III | Pass | C3 |
| [878](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A878:O878) | Throwing | Shrapnel Casing III | Pass | C4 |
| [880](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A880:O880) | Throwing | Piercing Toss I | Pass | C1, C4 |
| [882](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A882:O882) | Throwing | Flurry Bleed I | Pass | C1 |
| [884](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A884:O884) | Throwing | Piercing Toss II | Pass | C1, C4 |
| [886](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A886:O886) | Throwing | Severing Toss I | Pass | C2 |
| [888](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A888:O888) | Throwing | Flurry Bleed II | Pass | C1 |
| [889](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A889:O889) | Throwing | Deep Wound | Pass | C1 |
| [890](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A890:O890) | Throwing | Piercing Toss III | Pass | C1, C4 |
| [892](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A892:O892) | Throwing | Severing Toss II | Pass | C2 |
| [895](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A895:O895) | Throwing | Piercing Toss IV | Pass | C1, C4 |
| [896](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A896:O896) | Throwing | Flurry Bleed III | Pass | C1 |
| [916](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A916:O916) | Twin Blade | Lacerating Twin Cut I | Pass | C1, C4 |
| [917](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A917:O917) | Twin Blade | Blood Wake | Pass | C4 |
| [918](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A918:O918) | Twin Blade | Bleed Spread I | Pass | C4 |
| [920](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A920:O920) | Twin Blade | Lacerating Twin Cut II | Pass | C1, C4 |
| [922](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A922:O922) | Twin Blade | Twin Rupture I | Pass | C2 |
| [923](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A923:O923) | Twin Blade | Lacerator Stance | Pass | C1 |
| [924](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A924:O924) | Twin Blade | Bleed Spread II | Pass | C4 |
| [926](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A926:O926) | Twin Blade | Lacerating Twin Cut III | Pass | C1, C4 |
| [928](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A928:O928) | Twin Blade | Twin Rupture II | Pass | C2 |
| [931](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A931:O931) | Twin Blade | Lacerating Twin Cut IV | Pass | C1, C4 |
| [932](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A932:O932) | Twin Blade | Bleed Spread III | Pass | C4 |
| [935](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A935:O935) | Vibroblade | Executioner I | Retest | C1 |
| [936](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A936:O936) | Vibroblade | Rundown I | Retest | C1 |
| [939](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A939:O939) | Vibroblade | Savage Reflexes | Pass | C6 |
| [942](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A942:O942) | Vibroblade | Rundown II | Retest | C1 |
| [947](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A947:O947) | Vibroblade | Executioner II | Retest | C1 |
| [950](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A950:O950) | Vibroblade | Rundown III | Retest | C1 |
| [958](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A958:O958) | Vibroblade | Shield Wall I | Pass | C2 |
| [964](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A964:O964) | Vibroblade | Shield Wall II | Pass | C2 |
| [969](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A969:O969) | Vibroblade | Invincible | Pass | C2 |
| [970](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A970:O970) | Vibroknife | Pathogen Strike I | Retest | C5 |
| [971](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A971:O971) | Vibroknife | Hypermetabolize | Retest | C5 |
| [972](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A972:O972) | Vibroknife | Debilitate I | Pass | C1 |
| [973](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A973:O973) | Vibroknife | Virulent Blade I | Pass | C5 |
| [974](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A974:O974) | Vibroknife | Pathogen Strike II | Retest | C5 |
| [977](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A977:O977) | Vibroknife | Assassin's Stance | Pass | C1 |
| [978](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A978:O978) | Vibroknife | Debilitate II | Pass | C1 |
| [980](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A980:O980) | Vibroknife | Pathogen Strike III | Retest | C5 |
| [981](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A981:O981) | Vibroknife | Virulent Blade II | Pass | C5 |
| [984](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A984:O984) | Vibroknife | Virulent Blade III | Pass | C5 |
| [985](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A985:O985) | Vibroknife | Pathogen Strike IV | Retest | C5 |
| [986](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A986:O986) | Vibroknife | Debilitate III | Pass | C1 |
| [987](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A987:O987) | Vibroknife | Viral Cascade | Pass | C5 |
| [994](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A994:O994) | Vibroknife | Backstab I | Retest | C3 |
| [997](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A997:O997) | Vibroknife | Cheap Shot | Pass | C1 |
| [1000](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A1000:O1000) | Vibroknife | Backstab II | Retest | C3 |
| [1005](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203&range=A1005:O1005) | Vibroknife | Escape Artist | Pass | C3 |
