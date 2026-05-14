# Perk Redesign Import Notes

Source pulled from the public Google Sheet on 2026-05-10. I reviewed all 50 downloaded tabs, with the weapon skill tabs used as the main balance baseline.

## Files

- `Force_Redesign.tsv`
- `Devices_Redesign.tsv`
- `Beast_Perks_Redesign.tsv`
- `First_Aid_Redesign.tsv`
- `Espionage_Redesign.tsv`

Each file is tab-separated. Import into Google Sheets with separator set to Tab.

## Balance Assumptions

- Standard character perk lines follow the weapon baseline: about 55 SP per line.
- Force has four 55 SP lines: two Light-side and two Dark-side.
- Devices has four 55 SP lines.
- First Aid has two 55 SP lines.
- Espionage has two 55 SP lines and is no longer a small side utility tree.
- Beast perks use beast-specific SP. The old shared beast General bucket is removed from the purchasable beast pool; role tools are folded into each role line so a beast only buys perks from its own role.
- No new perk grants raw attributes such as MGT, PER, VIT, WIL, AGI, or SOC.
- Sustained Force upkeep mechanics are removed.
- Devices avoids stealth and trap gameplay. Espionage owns stealth, slicing, traps, and poison.
- First Aid is intended to be the best direct healing, cleanse, and recovery skill. Force, Devices, and Beast perks can provide alternatives, but with lower throughput, narrower targeting, or longer cooldowns.

## Reviewer Pass

- Grenadier no longer spends SP on grenade consumption savings; grenades are cheap enough that this was a bad tax. The line now has blast radius scaling.
- Bruiser beast AoE is explicitly hostile-only. Cone breath perks include an implementation fallback to hostile-only target-centered bursts if cone AI still stalls beast behavior.
- Evasion beasts now generate or hold attention through Evasive Challenge, Distracting Feint, and Untouchable Instinct so evade-trigger perks can reliably matter.

## Reviewer Pass 2

- Field Engineer is now turret-centered. Combat Emitters and Shock Pylons became Blaster Turrets and Shock Turrets so the line is not dependent on enemies standing in a tiny ground zone.
- Droidbreaker was removed as a Devices line because droid-only value is too niche. Its useful concepts were redistributed: turret targeting moved to Field Engineer and Weapon Jam moved to Field Support.
- The fourth Devices line is now Assault Gadgets: close and mid-range personal device attacks such as flamethrowers, wrist rockets, sonic bursts, darts, and cryo sprayers.
- Field Support no longer includes Kolto Mist, Emergency Sealant, or Medical Injector Rig. It now focuses on temporary HP, mitigation, power support, hardlight cover, and weapon jamming.
- Deflector Shield effects now grant a flat temporary HP value plus a target max-HP percentage, so low-HP characters still receive meaningful protection.
- Hardlight Screen now uses percentage ranged physical damage reduction instead of a small Defense stat increase.
- Trauma Medic now owns Kolto Mist, Emergency Sealant, Medical Injector Rig, and Infusion-style regeneration.
- Combat Pharmacology Shielding was converted to percentage mitigation, and Emergency Cocktail now applies full-strength effects with a 5 minute cooldown.

## Scaling Guideline

- Buffs, debuffs, mitigation, hit chance, evasion chance, healing-over-time, resource restoration, and temporary HP should prefer percentage-based scaling.
- Raw damage riders and save DC base values may remain numeric where they mirror weapon perk conventions.
- Flat temporary HP minimums on Devices shields are an intentional exception so low-HP targets still receive meaningful protection; the main shield value still scales from target maximum HP.
- First Aid healing now uses target maximum HP plus WIL scaling instead of fixed HP values.
- First Aid and Devices raw Accuracy, Defense, Force Defense, Force Attack, and similar stat bumps were converted to percentage hit chance, mitigation, or effect-strength modifiers.

## Force Affinity Rule

Force Affinity range is -10 to 10.

- Buying a Light power increases Force Affinity by 1, clamped to 10.
- Buying a Dark power decreases Force Affinity by 1, clamped to -10.
- Buying a Universal power does not change Force Affinity.

Recommended calculation:

- Light SideAffinity = Force Affinity.
- Dark SideAffinity = Force Affinity * -1.
- Universal SideAffinity = 0.
- Magnitude multiplier = clamp(1 + 0.05 * SideAffinity, 0.50, 1.50).
- Save DC = Base DC + floor((WIL - 10) / 4) + floor(SideAffinity / 2).
- Apply magnitude multiplier to damage, healing, shields, regeneration, and drain values.
- Apply the DC adjustment to detrimental Light or Dark powers.
- Universal powers use WIL scaling but do not receive the affinity multiplier or DC modifier.
