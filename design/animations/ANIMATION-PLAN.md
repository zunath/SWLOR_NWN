# Active ability animations

The library covers 282 animation entries and 506 active ability-rank bindings. It includes the 214 perk requirements, 67 active Mimicry techniques, and Call Beast separately from Tame. Passive Mimicry traits, NPC-only abilities, and Beast-model abilities are excluded. Player Beast Mastery remains included. Stealth has a tester clip while gameplay uses its native action mode.

The seven original Vibroblade clips are preserved. New clips use native motion families and three reusable procedural bases (pistol recoil, rifle recoil, and leap). Related abilities may share a base, but each has its own stable internal name and editable source. These are animation drafts requiring in-game visual review with equipment; generation does not reproduce image VFX or alter gameplay effects.

The Design Bible **Animations** tab documents every entry, preserving all 119 image links. **Internal Name** is the animator's authoritative name. Names are at most 12 characters so `_in` and `_out` fit within the 16-character engine limit. **Base Motion** identifies the source; **Status** distinguishes installation from visual approval; **Source Project** points to the editable file.

- [Active ability contract](active-abilities.json): categories, descriptions, exact feat ranks, image references, and internal names.
- [Reserved names](animation-names.json): stable identifier-to-internal-name mapping.
- [Generation provenance](active-manifest.json): source motion, duration, and project hash.
- [Perk summary](animation-plan.csv): the original 214 perk requirements, now linked to their installed names.
- [Workflow and tester](README.md): generation, compilation, and `/animations`.

Generated gameplay animation is restricted to player creatures. Native equipment remaps, projectile release, channels, instant movement choreography, space, and stealth contracts remain authoritative where required. Every generated clip is independently available in the tester even when gameplay retains native choreography.

| Category | Animation entries |
|---|---:|
| Beast Mastery | 7 |
| Devices | 27 |
| Espionage | 6 |
| First Aid | 12 |
| Force | 25 |
| General | 1 |
| Heavy Vibroblade | 12 |
| Katar | 11 |
| Leadership | 12 |
| Lightsaber | 11 |
| Mimicry | 68 |
| Pistol | 10 |
| Rifle | 10 |
| Saberstaff | 10 |
| Spear | 10 |
| Staff | 11 |
| Throwing | 10 |
| Twin Blade | 10 |
| Vibroblade | 9 |
| Vibroknife | 10 |
