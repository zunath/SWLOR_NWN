# Droid review for feature/combat-upgrade

## Scope

The review follows companion droids from crafting through assembly, instruction upload, activation, combat, equipment/inventory persistence, and migration. Player droid race/rebuild restrictions are covered by the existing character rebuild checks. The source corpus contains 180 CPUs, 330 other parts, 293 instruction discs, and 57 enhancement blueprints.

## Weapon instruction coverage

The instruction review found that the 82 Devices, First Aid, and Provoke discs omitted every active weapon ability. The catalogue now also includes 211 ranks across 103 weapon abilities:

| Weapon | Abilities | Discs |
| --- | ---: | ---: |
| Vibroblade | 9 | 21 |
| Vibroknife | 10 | 22 |
| Heavy Vibroblade | 12 | 17 |
| Spear | 10 | 22 |
| Twin Blade | 10 | 22 |
| Katar | 11 | 19 |
| Staff | 11 | 22 |
| Pistol | 10 | 22 |
| Rifle | 10 | 22 |
| Throwing | 10 | 22 |

This includes every active attack, stance, and active capstone in those weapon trees. Passive traits, Force-restricted Lightsaber/Saberstaff abilities, Leadership, and player-only systems remain excluded. The rifle suppression area ability is named **Suppressive Line** in the current definitions: ranks I and II cost one and two AI slots and require controller tiers II and IV, respectively. Suppressing Shot and Suppression Stance are also available.

Each new rank follows the existing slot cost of one slot per rank. Its Engineering recipe uses the existing tier material/quantity pattern at level `10 * controller tier`. Existing recipe IDs, costs, tiers, and resource names are preserved. New discs have distinct resource names where historical discs are still retired by the obsolete-item migration. The weapon definition generator emits the same slot metadata so regeneration retains eligibility.

The companion profile already registers every ability and selects granted feats using their existing targeting and activation requirements, including aimed lines and queued attacks. Default self-buff scoring checks the ability's declared effect types so it does not recast an active buff. Stance abilities also check the shared status-effect source metadata: any active stance suppresses other stance actions until it ends, preventing droids programmed with multiple stances from repeatedly replacing them. Ordinary self-buffs remain usable while a stance is active, and effects pending removal no longer block activation. This uses shared ability metadata rather than a list of droid-only exceptions.

The item-property subtype table was also incomplete: `iprp_droidperk.2da` now labels all 143 supported abilities. Twelve missing rankless names reuse existing TLK gaps, and the binary TLK contains those strings without changing its table size. The custom item palette includes every disc. All 82 pre-existing discs had stale `NO_ECONOMY` flags despite being craftable; those flags are removed, and both obtainability scanners now recognize `DroidInstructionRecipe` entries.

Validation: after updating to the current master branches, the initial focused droid, migration, AI, targeting, weapon, and economy run passed 175 tests; the weapon generator passed 25 tests. The stance-scoring PR follow-up passes 151 focused AI, droid instruction, stance, and status-effect tests, including competing stances, expiration, and ordinary buffs. The build succeeds; a full build reports eight existing nullable warnings in the NWN API and GuiWidget. Native in-game deployment and combat were not run as part of this review.

## Corrected defects

| Area | Defect | Result |
| --- | --- | --- |
| Migration IDs | New Fire/Poison/Electrical/Ice resistance reused legacy grouped weapon stat IDs 12–15. Running stored-item migration again converted resistance into weapon skills. | Current resistance uses reserved gaps 21–24. Legacy weapon IDs retain their migration meaning. The companion HAK change is required. |
| Instruction migration | A separate maximum-rank list did not enforce controller tier, AI capacity, or one active rank per ability. | The current perk definitions supply eligibility and slot costs. Valid learned ranks survive; the active list is normalized deterministically. |
| Runtime loadout | Reading a Tier property reset previously read instruction properties. Controller properties and saved active ranks could disagree. | Runtime uses the saved active list with shared validation; properties mirror that list for inspection. |
| AI programming | Selecting multiple ranks charged slots more than once, while removing a rank removed all matching properties for that ability. | Selecting a rank replaces the active rank and accounts for its actual slot cost. Other learned ranks remain available. |
| Disc upload | Unsupported perks could bypass droid eligibility; a multi-property disc could partially update UI state before rejection. Pending targeting callbacks could outlive the programming session. | Validate the complete upload before saving/consuming one disc. Recheck ownership, assembly rank, active-droid state, and programming session at callback time. |
| Part selection | Selecting an unrelated item with no Tier attempted to look up tier zero and threw before the normal wrong-part message. | A non-part has level zero and reaches the existing part-type validation. |
| Part data | `d_bd_vs4` granted 5 Vitality despite its tier-IV generator source specifying 4. | The blueprint now matches the source. Previously assembled droids retain their saved stat totals; this one-point blueprint correction does not rebuild existing controllers. |

## Reviewed integration

- CPU tiers map to levels and Armor ranks 5/15/25/35/45. The concrete weapon skill families match the CPU generator input and the current equipment requirement model.
- Assembly adds stats from the CPU, head, body, arms, and legs; crafting enhancement mappings produce supported droid stat properties, including the new resistance IDs.
- Instruction discs have current recipes, perk ranks, positive AI slot costs, names, and palette entries. The generic droid AI profile exposes registered ability actions and filters them by the droid's granted feats. Tier changes do not grant player-only passive perks.
- Spawn finalizes raw Vitality before `Stat.SetNPCMaxHitPoints`, builds the combat skin, grants active ability feats, and installs the companion AI profile. Native tests verify final HP, skills, resistance, and instruction ranks at all five tiers.
- Stored migration covers serialized part fields, equipped items, controller inventories, nested containers, banks, markets, research jobs, property storage, outfits, DM creatures, and ship module collections. Existing blueprint expansion, retry, corruption-preservation, and pistol/saber migration coverage remains applicable.
- Existing companion command/follow modes, deployment combat-level and companion-slot gates, appearance persistence, death/dismissal recasts, player display-name rules, and droid race/full-rebuild restrictions were inspected. This change does not alter their design.

## Validation and release

`DroidInstructionsTests` exercises normalization and runtime selection. `DroidPartsTests` compares the complete generated part corpus to its source and crafting recipes and reserves legacy subtype IDs. The existing Bible instruction test now checks the actual normalization policy against every current disc.

`MigrationDroidEngineTests` exercises native serialized migration retries, learned/active instruction reconciliation, spawn budgets at every tier, and every droid enhancement recipe. `MigrationEngineTests` restores a retired CPU property through serialization because current constructors correctly reject retired table rows.

Rebuild `sw_2da.hak`, deploy the updated `sw_tlk.tlk`, and repack the module with the server changes. The weapon instruction expansion adds no player migration. The migration work remains in server version 22 and the existing login migration stages. A production database-copy staging run remains the release check for the real saved corpus; local tests use disposable records. Droids created on earlier experimental combat-upgrade builds with the ambiguous 12–15 resistance IDs need their original test data restored or recreated; those values cannot be distinguished reliably from pre-upgrade weapon groups.
