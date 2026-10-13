# Twin Pistol Implementation Plan

## Design

Twin Pistol is a separate combat skill for fighting with a normal main-hand pistol and a simulated companion pistol in the left hand. Its first perk line rewards sustained fire, coordinated hits, and stamina management. Single Pistol keeps its harder individual hits, existing critical-hit specialization, and shield option.

Twin Pistol has ranks 0 through 50, contributes to the existing 400-rank skill cap, and uses the Weapon combat-point category. It does not require Pistol ranks. The first line contains ten perks with eighteen purchasable ranks costing 60 SP in total. Armor's existing Dual Wield ranks remain optional and cost another 9 SP.

The skill name is **Twin Pistol** throughout player-facing text. Akimbo is the name of its stance, not a replacement skill name. Add new identifiers and resource rows; do not reuse retired skill, perk, base-item, or TLK definitions.

This is the implementation design, not a record of shipped functionality. Numerical values are initial tuning values. Release depends on the balance and engine acceptance criteria below.

## Skill and equipment progression

### Training from rank zero

Provide an obtainable rank-zero main-hand pistol and companion pistol so ordinary attacks can train Twin Pistol before its first ability unlocks at rank 2. There is no skill-rank-2 equip gate and no mandatory training perk. Use the existing basic pistol budget for the starter pair, then mirror existing pistol tiers, damage types, delays, upgrade limits, and crafting progression.

Add companion recipes to the appropriate existing crafting progression and expose the starter through an existing starter or training vendor. Inspect current sources before choosing the vendor and recipe requirements. Do not distribute obsolete off-hand pistols or make an otherwise unobtainable test item searchable in the economy.

Each gun remains a separately owned item with its own damage, accuracy, delay, upgrades, and appearance. A companion costs a comparable amount to a pistol of the same tier. The second item and its enhancement capacity are included in balance comparisons.

### Requirements and equip order

A normal pistol retains its Pistol requirement in its stored item properties. A valid Twin Pistol pair interprets only that main-hand pistol's weapon-skill requirement against Twin Pistol at the same rank. Its other restrictions still apply. Companion items explicitly require Twin Pistol at their tier's rank.

Validation must use the prospective equipment arrangement, not just the arrangement before an equip event:

- A companion may be equipped first as an inactive item. It cannot attack, grant equipment bonuses, or qualify for weapon defenses until paired with an eligible pistol. This allows a character with Twin Pistol ranks and no Pistol ranks to equip the main hand afterward.
- Equipping the main hand then validates both weapons against Twin Pistol. An ordinary pistol with a shield or empty left hand continues to require Pistol.
- Removing or replacing the companion cancels pending shots and revalidates the main hand against Pistol. If the character cannot use it, unequip it safely into their inventory. Do not leave a usable high-tier pistol behind.
- Removing the main hand makes the companion inactive. A rifle, melee weapon, shield, or second companion never activates it.
- Login, respawn, item transfer, equipment restoration, and failed equip attempts run the same validation. No temporary reinterpretation is saved into the item's properties or base-item ID.

The inactive companion must not leak native equipped bonuses. Prefer authored properties whose effects are read from a validated weapon profile; suppress or reject unsupported native properties. If the engine cannot safely support this equip sequence, solve the pair-equip operation before content production rather than adding a hidden Pistol prerequisite.

### Skill selection

Add a context-aware skill resolver for ordinary weapon attacks. Both hands of a valid pair use Twin Pistol for attack, accuracy, critical skill contribution, damage rank limits, timing effects, character-sheet estimates, and combat progression. A lone ordinary pistol still resolves to Pistol. Preserve the base-item-only lookup for callers that actually need item classification.

Use the current pistol attributes: Perception for damage and Agility for accuracy, preserving supported item/stat overrides. Add the companion to the appropriate weapon classification and attribute mappings without classifying it as a shield or a native melee attacker.

Ordinary paired attacks contribute to Twin Pistol only. Route their contribution through the existing combat XP accounting, enemy eligibility, and award caps; do not grant a second full XP award for the simulated hand. An explicitly Pistol-scoped cross-trained ability retains its Pistol skill and progression behavior. Capture that identity when the action starts so changing equipment before impact cannot redirect XP.

Initialize the new skill through normal missing-skill handling for existing characters. Preserve SP/AP accounting and decay rules. Do not refund, convert, or reset existing Pistol ranks.

## Attack behavior

### Ordinary attacks

The native main-hand pistol remains the real ranged launcher. SWLOR schedules and resolves the companion shot. The companion's presentation carrier and its gameplay identity are separate: an attachment technique must not confer shield AC, Shield Deflection, native melee attacks, or attacks of opportunity.

One paired cycle budgets one main-hand shot and one companion shot. Both weapon delays contribute to the cycle. Reuse the existing paired-delay calculation, including the single payment of the engine baseline, ordinary Haste/Slow behavior, fractional batching, and bounded attack budgets. Armor's Dual Wield stat reduces only the companion delay by 10/20/30 percent; do not add a second stacking Twin Pistol delay-reduction trait.

Resolve each shot from its own captured weapon profile and independently determine hit, critical, ranged deflection, and damage. The companion still fires if the main hand misses. Attribute damage, threat, kill credit, combat feedback, and on-hit effects to the original attacker and correct weapon.

The attack resolver must use the same rules as other ranged auto-attacks: concealment and other avoidance, valid targets, ranged deflection, Guard at the damage stage, defenses, immunity/resistance/reduction, and damage-triggered effects. Apply each adjustment once. Do not implement the companion as a hostile ability that bypasses auto-attack defenses or as an unqualified damage copy of the main-hand result.

Both shots use the normal pistol auto-attack range and line-of-fire rules. Main-hand and companion may have different damage types and accuracy. A companion does not copy the main hand's enhancements or inherit its successful hit result.

Any equipped companion disables Single Weapon and Doublehand, just as any occupied off hand does today. Existing weapon deflection remains subject to its normal rules; Shield Deflection is unavailable without a real shield. Inactive companions provide no weapon defenses.

### Captured actions and interruption

Keep an action/cycle identity, target ID, weapon IDs, validated skill, weapon profiles, ammunition source, pending hand, and consumed-roll budget. Do not hold native object pointers across delayed callbacks.

Before launching a pending shot, verify that the action is still current, both actors are valid, the weapons are unchanged, the target is in the same area and legal to attack, and range/line of fire remain valid. Stop, death, incapacitation, target changes, unequip, area transitions, and invalid action state cancel unlaunched shots. A missed main-hand shot is not an interruption.

Define the launch boundary explicitly. Once a shot has launched, retain its original target and profile and use normal ranged impact semantics; it cannot become a shot from replacement equipment. Removed targets safely discard the impact. Presentation updates and observers never cause damage or consume charges.

Consume finite ammunition once per launched logical shot. Reuse unlimited-ammunition behavior without duplicating generated stacks. If ammunition runs out between hands, cancel the unlaunched hand. Ensure the native launcher and simulated resolver cannot both debit or refund the same shot.

### Abilities and cross-training

Twin Pistol actives require an active pair and use Twin Pistol for their skill-based calculations. Capture both weapons once per activation. The single-target attacks use the existing Pistol ability range of 25m; the cone uses its explicit dimensions. These are ordinary activated weapon abilities using the established ability action/impact timing, not free auto-attacks added by a hit callback.

Each ability pays STM and starts its cooldown once. Its impacts may critically hit independently, but activation-level bonuses and resource restoration run once according to the ability's result summary. Follow existing activated-ability avoidance rules; auto-attack deflection does not silently become a new rule for existing activated abilities.

Purchased Pistol abilities remain usable with an eligible main-hand pistol in a pair and retain their original Pistol skill, impact count, and main-hand damage source. They receive no Single Weapon bonus while paired. Double Shot stays at two impacts and never receives an automatic companion copy. Pistol-specific passive selectors continue matching Pistol; generic ranged and unrestricted effects may still apply to Twin Pistol according to their existing declarations. Do not broaden all Pistol selectors to include Twin Pistol.

Shared cooldowns limit additional burst from cross-training:

| Twin Pistol ability | Existing shared recast group | Base cooldown |
|---|---|---:|
| Paired Volley | Quick Draw | 18 seconds |
| Crossfire | Double Shot | 24 seconds |
| Sweeping Salvo | Fan the Hammer | 30 seconds |
| Twin Suns | Dead Man's Hand | 90 seconds |

Use the existing group identities and explain sharing in descriptions. Do not rename existing Pistol cooldowns. Akimbo Stance has its own 30-second recast and participates in existing stance exclusivity.

Queued abilities reserve and replace exactly one eligible landed ordinary roll. Other budgeted rolls remain ordinary attacks. Count limited haste/no-delay charges per actual attempted roll, including misses. Grant effects earned during a budgeted cycle only when they can no longer be spent retroactively by that cycle.

## Perk progression

Each rank requires the listed Twin Pistol rank and the preceding rank of that perk. SP prices are per purchase. Ranked values replace earlier ranks. All new passive bonuses require an active pair; all direct damage retains each firing gun's damage type.

`D` means the firing gun's item weapon DMG with normal eligible adjustments, without Single Weapon or Doublehand. Add listed flat power before the normal damage formula. For Sweeping Salvo, calculate `floor(0.75 * D)` separately per gun before adding its flat power. Percentage damage traits apply at the shared outgoing-damage adjustment stage and never rescale the item rating or recursively multiply secondary damage.

| Twin Pistol rank | Perk | SP | Effect | STM | Cooldown |
|---:|---|---:|---|---:|---:|
| 2 | Paired Volley I | 2 | One shot per gun at one target; each uses D + 4. | 4 | 18s |
| 5 | Alternating Fire | 2 | Both ordinary shots landing on the same target restore 1 STM, at most once every 6s. | - | - |
| 8 | Matched Rhythm I | 2 | Ordinary Twin Pistol shots deal +10% damage. | - | - |
| 10 | Crossfire I | 2 | One shot per gun, each D + 3. Both landing reduces target Evasion by 5% for 8s. | 4 | 24s |
| 12 | Paired Volley II | 2 | One shot per gun, each D + 9. | 6 | 18s |
| 15 | Balanced Grip | 4 | +5 ACC to the companion only. | - | - |
| 18 | Sweeping Salvo I | 3 | Up to 3 enemies in an aimed 10m x 10m cone; one shot per gun per enemy, each floor(0.75 x D) + 7. | 6 | 30s |
| 20 | Akimbo Stance | 4 | +10% Haste while paired. Hostile Pistol and Twin Pistol abilities cost 2 additional STM. | 0 | 30s |
| 22 | Matched Rhythm II | 4 | Ordinary Twin Pistol shots deal +20% damage total. | - | - |
| 25 | Converging Fire | 4 | Both ordinary shots landing on one target grants +5% ordinary Twin Pistol damage against that target for 6s, starting after that pair. | - | - |
| 28 | Paired Volley III | 3 | One shot per gun, each D + 14. | 9 | 18s |
| 30 | Crossfire II | 4 | One shot per gun, each D + 7. Both landing reduces Evasion by 8% for 8s. | 8 | 24s |
| 32 | Sweeping Salvo II | 3 | Same cone and target cap; each shot uses floor(0.75 x D) + 13. | 9 | 30s |
| 35 | Cool Under Fire | 2 | Direct hostile damage reduces the next Twin Pistol ability's STM cost by 2. One charge, lasts 10s, trigger cooldown 10s. | - | - |
| 38 | Crossfire III | 4 | One shot per gun, each D + 10. Both landing reduces Evasion by 10% for 8s. | 10 | 24s |
| 40 | Paired Volley IV | 5 | One shot per gun, each D + 19. | 12 | 18s |
| 45 | Matched Rhythm III | 4 | Ordinary Twin Pistol shots deal +30% damage total. | - | - |
| 50 | Twin Suns | 6 | One shot per gun, each D + 15. Afterward, the next 6 ordinary Twin Pistol shots gain +10 percentage points Critical Rate and +20% Haste. Expires after 12s. Requires mastery quest. | 15 | 90s |
| | **Total** | **60** | | | |

### Effect details

- **Alternating Fire:** only a completed ordinary two-hand pair qualifies. A cast, proc, reflected hit, or a second observer cannot trigger it. Haste batches retain individual pair identities. Apply the six-second cooldown per attacker across all targets; maximum recovery is 10 STM per minute.
- **Matched Rhythm:** affects ordinary shot damage only, including limited-haste ordinary rolls. Excludes queued replacements, activated abilities, reflected damage, DoTs, and damage generated by procs.
- **Crossfire:** apply the Evasion reduction once after both impacts qualify as landed on the same surviving target. Use normal hostile debuff resistance/immunity rules. Multiple Crossfire sources keep the strongest live magnitude and do not add penalties together; a weaker application must not extend a stronger instance. Other debuff interactions follow the shared stat system and are included in testing.
- **Balanced Grip:** a flat five ACC, not five percentage points of hit chance. It enters only the companion's weapon profile once.
- **Sweeping Salvo:** at most three distinct targets and six impacts. Determine the eligible target set once with the existing aimed-area selector, capture both weapon profiles, and revalidate before impact. Area targeting requires a separate valid spell row for each rank. It cannot repeatedly select one target to spend unused target slots.
- **Akimbo Stance:** use the existing stance activation timing and exclusivity. Its Haste stacks through the existing cap, not a private multiplier. Its +2 STM applies once per eligible hostile activation, including cross-trained Pistol casts; normal cost reductions and the normal minimum cost apply afterward. Its benefits end as soon as the pair becomes invalid.
- **Converging Fire:** one tracked target per attacker. Switching targets or invalidating the pair clears it. Repeated qualifying pairs refresh, never stack. It adds to Matched Rhythm for a maximum +35% from these two traits, not 30% followed by a separate 5% multiplier.
- **Cool Under Fire:** requires positive direct damage from a hostile source; excludes self-damage, friendly damage, periodic damage, and reflected damage. It does not stack charges. Consume only when an eligible activation successfully pays its cost; failed validation does not spend it. Pair removal clears its pending discount but does not reset its trigger cooldown.
- **Twin Suns:** resolve its initial two impacts before granting the six-shot benefit, so they do not consume it. Grant after a valid completed cast, even if the target avoided its damage. Each ordinary attempted roll consumes one charge, including misses and deflections. Do not count pairs as one attack, refresh charges from criticals, or grant no-delay attacks. Pair removal clears the benefit. Other limited timing effects share the actual-roll accounting.

### Mastery quest

Use the existing weapon mastery quest conventions and conversation system. The quest tests coordinated fire, not a random drop: demonstrate successful two-hand attacks and complete a combat encounter with the pair. Confirm that progression credit follows actual Twin Pistol actions rather than simply checking equipment at enemy death. Its reward is eligibility to purchase Twin Suns at rank 50 for 6 SP, with no free perk rank or extra damage item.

Select the trainer, encounter, objective count, and any new placement after inspecting existing Pistol mastery content. Those content details do not block the combat prototype. Authored dialogue belongs in conversation JSON; any added NPC follows the NPC HP normalization and visual-verification rules.

## Proc and resource accounting

Represent ordinary attacks, activated impacts, queued replacements, and secondary procs explicitly in the action context. This gives shared rules a reliable way to decide whether an event counts toward a trait.

- Ordinary shots independently use their own weapon's on-hit properties. Shared internal cooldowns, healing caps, and resource limits remain per attacker where they are today.
- An activation has one identity across all targets and hands. Once-per-ability triggers and refunds consume that identity once. Per-impact effects retain their declared behavior.
- Secondary damage and triggered attacks never recursively schedule a companion shot.
- Native and simulated damage paths must not notify the same hit twice. Ammo on-hit forwarding retains the launcher's captured identity.
- Skill-scoped effects match the captured skill. Generic ranged effects may apply to both Pistol and Twin Pistol; unrelated skill selectors do not change.
- Weapon skill, accuracy, and enhancement properties are not also added to creature-wide totals. Limit any necessary shared-code changes to the context and calculations needed for the new attack path.

## Balance targets and checks

### Current equipment baseline

The baseline below uses current `Combat` and `WeaponDamage` formulas at equal Attack/Defense 100, equal damage attributes 26, a fixed 10% critical chance, StandardCriticalRating 2, DMG 22 and Delay 250 per item. It excludes misses, mitigation, other item bonuses, and perk-line bonuses. A single weapon rounds its adjusted item DMG up once; the damage formula rounds its final range endpoints down.

| Equipment | Item DMG per shot | Cycle interval | Mean damage per second |
|---|---:|---:|---:|
| Single pistol with natural +20% | 27 | 2.416s | 9.62 |
| Single pistol with Doublehand III, +60% total | 36 | 2.416s | 13.04 |
| Pair without Dual Wield | 22 each | 4.833s for two shots | 7.90 |
| Pair with Dual Wield III | 22 each | 3.583s for two shots | 10.66 |

These are arithmetic baselines, not full-build measurements. They show why granting a free full-power companion hit at single-pistol cadence is inappropriate and why the slower paired cycle needs specialist damage bonuses. They do not establish that the new 60-SP line is balanced.

### Comparable builds

Compare Twin Pistol 50 with the full 60-SP line plus Dual Wield III against Pistol 50 with its full 60-SP critical line plus Doublehand III: 69 SP each, equal Armor investment, attributes, equipment tier, and external buffs. Include the pistol/shield defensive alternative and the existing Skirmisher, Rifle, and melee paired-weapon builds. Test equal-tier gear first, then repeat at equal acquisition/upgrade cost because the pair provides another item and enhancement capacity.

Run hybrids at equal total SP and skill-rank investment. A character buying both Pistol and Twin Pistol lines is not comparable to a 69-SP single-line build. Include generic ranged/critical effects, Armor traits, buffs from allies, weapon enhancements, suppression, healing-on-hit, and limited haste/no-delay effects. Do not assume Pistol-specific traits apply merely because the models are pistols.

### Release criteria

- At rank 50, target 95-105% of the equally invested single-pistol critical build's sustained single-target damage in representative 120-second encounters. Include STM depletion and the existing critical/haste traits on the comparator.
- Compare 6-second and 12-second openings. Twin Pistol's maximum burst should not exceed the matched single-pistol build by more than 10%; its sustained role does not justify a larger opening kill window.
- Test ranks 0, 10, 25, and 50 using weapons actually available at each rank. No inaccessible rank-zero training path or mandatory Pistol investment is acceptable. Investigate damage gaps outside 90-110% against a comparably invested offensive pistol build, accounting for when ranks become available.
- Check one, three, and excess area targets; high defense; high evasion; ranged/shield deflection; immunity; Guard; moving targets; PvP; and unequal weapon tiers/delays. No extra target, proc, or refund may be created by animation batching.
- Sweep ordinary Haste/Slow from -50% to +50%, then limited effects at and near the animation floor. Observe actual roll counts and damage, not just displayed delay.
- Sustain should have a resource cost. Alternating Fire and Cool Under Fire must stay within their explicit caps and cannot create an indefinite refund loop with abilities or cross-trained traits.

Use fixed-seed simulations and enough samples to report uncertainty, then verify representative rotations in engine. Report the actual rotation, gear, hit rates, resource use, DPS, burst, and target assumptions. Tune Matched Rhythm and the stance first if sustained output misses the target; tune flat ability power or shared cooldown behavior for burst. Do not compensate by changing existing Pistol perks or other weapon systems.

## Implementation sequence

### 1 Establish the engine prototype

Create one new companion resource and one repeatable paired attack fixture. Confirm model attachment, native ranged classification, main-hand ammunition, companion damage attribution, independent misses/criticals/deflection, and exactly two scheduled ordinary rolls at normal cadence.

Prototype the left-hand fire pose, muzzle/projectile origin, hit/miss presentation, and sound with an observing client. Current canonical pistols use native sling behavior; the former empty-offhand firing remap uses both hands. Do not assume it works with a companion or that increasing native `ResolveAttack` count produces a valid off-hand ranged attack.

If a custom attachment or projectile presentation is required, implement and verify it here. Treat left-hand presentation as a feasibility dependency: do not generate all tiers or promise full visual support before this fixture passes. Use a new base-item ID determined from the current HAK data; no reserved numeric ID is assigned by this plan.

### 2 Add the skill and validated weapon context

Add Twin Pistol metadata, missing-skill initialization, skill/requirement labels, contextual equip requirements, both-hand profiles, ranged/weapon classification, and character-sheet support. Keep `GetSkillTypeByBaseItem` compatible; pass a resolved context or the existing skill override where the caller needs equipped behavior. Audit native attack hooks as well as managed services so the main hand cannot accidentally use Pistol rank.

Integrate the simulated hand with the scheduler, damage rules, XP accounting, ammo, effects, pending-action cancellation, and limited-roll budgets. Model perk-driven behavior as attributed stats and declarative ability/impact metadata. New helpers named during implementation must be introduced explicitly rather than assumed to exist.

### 3 Author progression and presentation

Add a Twin Pistol sheet to the public Design Bible and register it with the existing workbook layout, audit, and weapon generators. Include all eighteen perk rows, costs, timings, attributes, target shapes, descriptions, and mastery requirements. Preserve cached formulas and unrelated ZIP entries through targeted XML edits, then run the required refresh.

Generate the perk definitions and one class/file per distinct active ability. Add all needed feat, spell, effect-icon, skill/item-property labels, status effects, and stat definitions. Choose available IDs from the current source data. New custom TLK entries use the first gaps; custom references use the required offset. Recast labels must fit the existing 14-character limit.

Create tiered companion blueprints, crafting recipes, starter acquisition, upgrades, icons, and appearance registrations. Retired off-hand pistols remain unchanged and disabled. No conversion of existing Pistol weapons or broad character-build migration is needed. Add the mastery quest and its dialogue after the mechanics work.

Choose VFX from the repository's effect-selection reference. Author paired attack and ability clips compatible with both weapons, interruption, and supported character models. Regenerate cooldown icons and run the gameplay icon audit. Player-facing combat feedback and UI must use PlayerName when displaying characters.

### 4 Validate and prepare delivery

Run the checks below, tune the line within its own budget, and update the Bible and generated outputs together. Present actual NWN evidence of both weapons and shots. The complete feature requires a parent server/module PR and, when the submodule pointer changes, a pushed HAK companion PR linked in both directions. Resolve review threads on both before final handoff.

Deploy the matching C# server build, rebuilt HAK/TLK resources, and repacked module together. Repack/rebuild/deploy requirements are deployment steps, not reasons to alter gameplay code.

## Integration map

| Existing location | Required work |
|---|---|
| `Service/SkillService/SkillType.cs`, `Service/Skill.Mapping.cs`, `Service/Skill.cs` | Skill metadata, context-sensitive attack selection, initialization and progression |
| `Feature/EquipmentRestrictions.cs`, `Feature/PistolBaseItemCompatibility.cs`, `Service/Item.cs` | Prospective pair validation, tier requirements, compatibility, item classification and attributes |
| `Service/PerkService/EquipmentPredicates.cs`, `Service/Stat.cs` | Active-pair predicate, Armor Dual Wield eligibility, per-hand stats and overrides |
| `Native/OnAIActionAttackObject.cs`, `Native/WeaponAttackCycle.cs`, `Native/ResolveAttackRoll.cs`, `Native/GetDamageRoll.cs` | Paired ranged scheduling and explicit attack context; preserve existing melee/native behavior |
| `Service/Combat.cs`, `Service/CombatService/WeaponDamage.cs` | Shared timing, shot calculations, skill/proc accounting and Single Weapon exclusion |
| `Feature/PistolAnimationRemap.cs`, `Native/WeaponAttackAnimation.cs`, `design/animations/` | Paired ranged presentation without regressing empty-offhand or shielded pistols |
| `Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs`, perk/ability/status definitions | Hand-specific ability profiles, traits, activation summaries and shared recasts |
| Character sheet, equipment examination, skills/perks UI, player guide | Twin Pistol rank, both weapons' values, requirements, costs, and shared cooldown descriptions |
| `tools/GenerateWeaponArchetypeImplementation.py`, Bible and audit/layout inputs | New source sheet, reproducible generated output and balance data |
| `Module/uti`, recipes, starter acquisition, quests and `ConversationData` | Obtainable companions and mastery progression |
| `SWLOR_Haks` and `SWLOR.NWN.API` enums | New base item, skill labels, feats/spells/TLK, icons, models and animations |

Paths in this table are relative to `SWLOR.Game.Server` unless they explicitly name another repository directory. Add new types in matching domain folders. This map identifies integration boundaries; it does not authorize unrelated refactoring.

## Verification

### Logic and content

Add focused tests for the new skill, rank-zero access, equip transitions, per-hand profiles, conditional stats, ability metadata, costs, targets, cooldown sharing, XP attribution, and balance comparisons. Extend existing regression coverage where the feature changes shared behavior:

- `SkillMappingTests`, `CharacterRebuildSkillAvailabilityTests`, `EquipmentRestrictionsTests`, `AttackWeaponClassificationTests`.
- `CombatAttackDelayTests`, `PairedWeaponTimingTests`, `SingleWeaponDamageTests`, `WeaponLoadoutBalanceTests`, `WeaponDeflectionTests`.
- `AreaAbilityTargetingTests`, `StaminaSustainTests`, `CrossSkillPerkInteractionSafetyTests`, `SkillSelectorStackingTests`, `CombatReleaseBalanceAuditTests`.
- `PistolAnimationRemapTests` plus the relevant economy, acquisition, generator, and icon audits.

Build once with `-p:RunPostBuildEvent=Never`, then run relevant tests with `--no-build`. The final implementation changes shared combat infrastructure and adds enum/generator output, so a final full server test pass is justified after focused failures are resolved. Check submodule initialization before diagnosing asset tests; review test-generated tracked diffs before committing.

### Engine and persistence

Add a Twin Pistol engine filter for normal and hasted attacks, both independent misses, criticals, distinct weapon properties, deflection, Guard, immunity, ammunition, once-only effects, queued ability consumption, and all interruption boundaries. Include zero/one/multiple finite ammunition cases and unlimited stacks.

Run existing `DualWield`, `PistolPresentation`, and `SingleWeapon` engine coverage. Verify empty-offhand pistols, pistol/shield, and ordinary melee pairs remain correct. Check weapon swaps before the companion launches and after a projectile launches, target changes, area transitions, death, and disconnect cleanup.

Equip, attack, upgrade/change state, save, and reload through normal gameplay at least twice. Assert ownership, stable item identities, rank requirements, equipment state, ammo quantities, and absence of duplicate items. Include a Twin Pistol character with Pistol rank zero. Any later migration must start with legacy fixtures and run the actual conversion first.

Player-controlled combat droids need an explicit companion-equipment compatibility check before release. If the item is supported for droids, include skill/module and AI-slot behavior and run `DroidInventory` lifecycle coverage; if their current equipment system cannot support it, reject the companion clearly until that integration is complete rather than allowing a partially functional item.

### Client evidence

Follow `InEngineVisualVerification.md`: run the owned NWN client on the isolated private desktop, set every volume channel to zero, capture its real rendered output, and stop it afterward. Do not move the user's focus or desktop.

Capture both hands firing, independent hit/miss presentation, the capstone, the cone, maximum haste, equipment transitions, and another client's observation. Check representative male/female and supported body/appearance variants, weapon alignment, muzzle origin, recoil, ready poses, impact timing, and duplicate/missing sounds. Record any unsupported appearance explicitly. Any new trainer or quest placement also needs evidence from a public approach with normal collision.

## Repository references

The starting mechanics and baseline calculations are based on commit `62364f27c`:

- [Dual wield combat](DualWieldCombat.md) and [weapon loadout balance tests](../../SWLOR.Game.Server.Tests/Service/WeaponLoadoutBalanceTests.cs).
- [Pistol perks](../Feature/PerkDefinition/PistolPerkDefinition.cs) and [Armor traits](../Feature/PerkDefinition/ArmorPerkDefinition.cs).
- [Equipment restrictions](../Feature/EquipmentRestrictions.cs), [pistol compatibility](../Feature/PistolBaseItemCompatibility.cs), and [pistol animation remap](../Feature/PistolAnimationRemap.cs).
- [Ability targeting](AbilityTargeting.md), [Design Bible workbook rules](DesignBibleWorkbookRules.md), [economy restrictions](EconomyRestrictedItems.md), [icon standards](IconStandards.md), [VFX selection](VisualEffectSelection.md), and [player identity](PlayerIdentity.md).
- [Engine testing](EngineTesting.md) and [in-engine visual verification](InEngineVisualVerification.md).
