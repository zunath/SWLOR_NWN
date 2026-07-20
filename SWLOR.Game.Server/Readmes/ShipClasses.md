# Ship Classes

Tiered player ships (I–V) fall into six stat classes. Each class trades within the same per-tier budget (Armor + Capacitor + Shield = 140 / 280 / 420 / 560 / 700; total power nodes 7 / 9 / 11 / 11 / 13) but emphasizes a different role.

Unchanged (unique stats, not classed): Aurek Strikefighter, Sith Fighter, Basilisk War Droid, and all corvettes.

Store-bought Light Freighter and Light Escort (Tier I only) keep their original store spreads; craftable Tier II–V of those hulls use **Support Focus**.

Source of truth for numbers: `Feature/ShipDefinition/PlayerShipDefinition.cs` (comments above each block).

## Class templates (Armor / Cap / Shield · recharge · High/Low nodes)

| Class | Role | T1 | T5 |
|-------|------|----|----|
| **Armor Focus** | Brawler — soaks hull damage | 75 / 35 / 30 · r4 · 3H/4L | 375 / 175 / 150 · r12 · 6H/7L |
| **Shield Focus** | Sustain tank | 30 / 35 / 75 · r4 · 3H/4L | 150 / 175 / 375 · r12 · 6H/7L |
| **Capacitor Focus** | Ability spam — large energy pool | 35 / 70 / 35 · r4 · 4H/3L | 175 / 350 / 175 · r12 · 7H/6L |
| **Recharge Focus** | Skirmisher — smaller pools, ~75% faster shield recharge | 35 / 40 / 45 · r7 · 3H/4L | 175 / 200 / 225 · r21 · 6H/7L |
| **Combat Focus** | Weapons platform (+2 high slots) | 45 / 35 / 45 · r4 · 5H/2L | 225 / 175 / 225 · r12 · 8H/5L |
| **Support Focus** | Utility / logistics (+2 low slots) | 45 / 40 / 45 · r4 · 2H/5L | 225 / 200 / 225 · r12 · 5H/8L |

Intermediate tiers scale linearly (× tier for Armor/Cap/Shield). Standard recharge is 4 / 6 / 8 / 10 / 12; Recharge Focus uses 7 / 11 / 14 / 18 / 21. Node offsets by tier: +0 / +1 / +2 / +2 / +3 on the T1 High/Low bases.

---

## Armor Focus (15 hulls)

Bombers, assault / armored transports, and heavy patrol craft.

- Hutt Bomber
- Military Bomber MK 1 / MK 2 / MK 3
- Advanced Bomber / Advanced Bomber MK 2 / MK 3
- Twin Bomber
- Onderon Ruping Bomber
- ST-07 Assault Ship
- Assault Transport
- Mandalorian Brute Patrol Ship
- Armored Transport
- Invader
- Throne

## Shield Focus (10 hulls)

Elite fighters and defensive / escort-leaning hulls.

- Civilian Elite Fighter
- Corsair Mk2
- Kusari Mk2
- Liberty Mk2
- Rheinland Mk 2
- Star Saber XC-01
- S-250 Chela Starfighter
- Order Fighter
- Jedi Transport
- Phoebos

## Capacitor Focus (10 hulls)

Gunships and gunboats.

- Hutt Gunship
- Military Gunship MK 1 / MK 2 / MK 3
- Military Gunship, Large
- Advanced Gunship MK 1 / MK 2 / MK 3
- Advanced Gunboat
- Teroch-type Gunship

## Recharge Focus (17 hulls)

Scouts, infiltrators, and light skirmishers.

- Advanced Scout MK 1 / MK 2
- Advanced Scout MK 1 / MK 2 / MK 3 Escort
- Infiltrator MK 1 / MK 2 / MK 3
- Sith Infiltrator MK 1 / MK 2 / MK 3
- Twin Infiltrator
- Starflier
- Zoomer Fighter
- S-100 Stinger Starfighter
- Hunter
- Neutral Barracuda

## Combat Focus (24 hulls)

General fighters and strikers (high weapon-slot budget).

- Striker / Neutral Striker
- Hound / Panther / Saber / Falchion
- Cutlass Starfighter
- Civilian Fighter / Civilian BW Fighter
- Hutt Fighter
- Kusari / Liberty / Rheinland
- Davaab-type Starfighter
- Onderon Type81a Fighter
- Pirate Fighter
- Legion Fighter
- Advanced Striker MK 1 / MK 2 / MK 3
- Advanced Striker Mk 1 / Mk 2 / Mk 3 Escort
- Corsair

## Support Focus (18 hulls)

Freighters, haulers, and utility transports.

- Light Freighter *(craft Tiers II–V; store Tier I uses original store stats)*
- Light Escort *(craft Tiers II–V; store Tier I uses original store stats)*
- Civilian Freighter / Bretonia Freighter / Kusari Freighter / Rheinland Freighter
- XS Freighter / KT-400 Light Freighter
- Y8 Miner Ship / YV-929 Hauler
- Pirate Freighter
- Trandoshan Transport
- Neutral Quartermaster Transport
- Mule / Merchant / Consular / Condor / Civilian Condor

---

## Notes

- Class is **per hull**, not per tier — Striker I through V are all Combat Focus with different absolute numbers.
- Faction-locked recipes (Cartel / Mandalorian / Republic / Sith / Legion blueprints) still use these classes; only craftability differs.
- Existing registered ships were refreshed by server migration `_22_ShipClassStatRebalance` via `RecalculateAllShipStats()`.

## Corvette Hangars

Non-capital ships can dock into a capital (corvette) hangar with `/dock` while piloting in space within range of a capital the pilot has **Enter Property** permission on. Up to **4** ships may be hangared per corvette. Use the Hangar Terminal near the corvette entry bay to board docked ships. Launch via the small ship's computer only works while the host capital is in space.
