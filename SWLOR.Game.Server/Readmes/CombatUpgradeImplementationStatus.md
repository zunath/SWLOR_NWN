# Combat Upgrade Implementation Status

Last updated: 2026-05-05

## Current Source of Truth

The combat upgrade uses the Bible spreadsheet as the planning source of truth. Runtime values must be captured directly in the owning perk and ability implementation code, following the existing one-class-per-perk pattern:

- `Feature/PerkDefinition/CombatUpgradeBiblePerkCatalog.cs`
- one class per Bible perk under `Feature/PerkDefinition/`
- per-perk ability definitions under `Feature/AbilityDefinition/<Skill>/`

The generated implementation currently contains:

- 205 `Combat` perk levels
- 190 `Trait` perk levels
- 27 `Stance` perk levels
- 2 `Toggle` perk levels
- 4 crafting `Action` rows

Crafting `Action` rows are tracked for planning but are not surfaced as generated combat feats. They continue to rely on their existing crafting systems.

## Runtime Coverage

All generated combat-active perk levels are wired through per-perk ability definitions and the existing `Combat` service.

The generated active runtime supports:

- active feat validation and recast locals
- FP and STM costs
- activation delays
- hostile single-target and area damage
- generic support/restoration effects
- stances, toggles, auras, and timed self buffs
- parsed DC-based Fortitude, Reflex, and Will save checks
- parsed Poison, Bleed, Burn, Shock, and Disease status effects
- generated combat point and enmity application

Passive behavior should be implemented explicitly in the appropriate existing hook for that mechanic, such as `Stat`, `Combat`, `GetDamageRoll`, `ResolveAttackRoll`, `Enmity`, or a status effect service implementation. Do not add generic runtime parsing of spreadsheet descriptions.

## Generated Feat Surface

There are 241 generated combat feats.

The following files must stay in sync:

- `SWLOR.NWN.API/NWScript/Enum/FeatType.cs`
- `SWLOR_Haks/swlor2_2da/feat.2da`
- `SWLOR_Haks/swlor2_2da/CLS_FEAT_FIGHT.2da`

`BasicSynthesis` and the other crafting `Action` rows should not receive generated combat feat registrations unless they are backed by existing crafting implementations.

## Notes For Future Agents

Do not reintroduce non-Bible combat perks as player-facing implementations. If a perk exists in old code but not in the Bible, remove its perk definition registration.

Do not introduce centralized runtime data files for the Bible sheet. If a perk has an activation delay of 2 seconds, its ability definition should declare that value directly in the builder call, such as `.HasActivationDelay(2f)`.

If a perk requires exact bespoke behavior beyond the shared active ability helpers, add that behavior through the owning ability definition or the existing service hook that already owns the mechanic.

Heavy Armor attack-delay penalties were intentionally removed and should stay ignored.

The droid instruction discs for the combat-upgrade perk set still need to be added after the core perk implementation is stable.
