# Bible and perk combination review

Review date: September 12, 2026.

The review found and corrected shared damage-budget bypasses, merged perk proc/discount sources, healing riders triggered without healing, an incorrectly broad duration bonus, uncapped control areas and a control-conversion timer loophole. The local Bible, definitions, generation rules and player-facing TLK descriptions have been brought into agreement.

The [tracker checklist](PerkRetestChecklist.md) identifies **309 perk/rank rows for verification: 30 direct changes and 279 combination regressions**. The [CSV](PerkRetestChecklist.csv) includes the current tracker status, reason, full scenario and a direct link for every row. These are change-specific testing requirements. At the user's request, the 26 directly changed rows previously marked Pass have been changed to Retest in the live tracker, with every other cell preserved.

## Scope and evidence

- Cross-checked the local Bible manifest's 1,004 perk/rank entries across 25 tabs against the tracker; every skill/perk/rank name matched.
- The generated implementation review reports 999 PASS rows and five SKIP rows. The skipped rows are Crop Management I-V in Agriculture, explicitly marked Not Started in the Bible and lacking current perk definitions; they are planned noncombat content, not verified implementations.
- Reviewed shared attack, ability, periodic damage, triggered damage, healing, resource discount, control, targeting and stat-source aggregation paths. Audited weapon, Force, Devices, Leadership, First Aid, Beast Mastery and Mimicry interactions through their definitions and existing corpus tests.
- Added behavioral checks for independent proc probability, actual perk stat sources, discount consumption, flat/percentage damage ordering, combined mitigation, healing eligibility, control duration and conversion. Added a corpus check for finite player/companion area-control target caps and Mimicry control cooldowns.
- Used the existing Bible implementation, legal-build balance, targeting, status, perk, progression and economy test coverage as regression guards. This is source/data review plus automated testing; it is not an exhaustive simulation of every legal build or a substitute for NWN playtests.

## Corrected findings

| Finding | Before | Result |
|---|---|---|
| Avoidance discount sources collided | Flowing Defense, Evasive Reload and Opportunist's Flow shared aggregated selectors and windows. Combining them could overwrite scope, consume the wrong discount or combine their durations. | Each source retains its own scope, value, expiry and consumption. Evasive Reload covers ranged weapon abilities; Opportunist's Flow covers hostile combat abilities; Flowing Defense stays Staff-specific. |
| Percentage bonuses escaped the outgoing budget | Positional/shape bonuses could precede the cap baseline. Authored finisher and Dark Force low-HP bonuses could run after the capped pipeline. | Those percentage stages are included in the +100% outgoing budget. Flat damage remains outside that cap. Incoming target vulnerability/reduction is a separate stage. |
| Incoming reduction floor undid other mechanics | An absolute minimum based on damage before target reduction could raise already-mitigated hits and interfere with Guard or physical-to-Force splitting. | The target/generic stages share an 85% reduction budget through their multipliers. Guard, resistance and typed Leadership retain their separate stages. Tiny hits are covered by regression tests. |
| Split and periodic damage used inconsistent paths | Converted Force damage could lose the earlier target reduction's budget information. Some DoT paths omitted it, and Bleed skipped the common final mitigation stage. | Physical and converted Force portions carry the same budget information. Converted damage does not apply outgoing type modifiers twice. Bleed, Fragmentation and Force DoTs pass through the appropriate common mitigation stages. |
| Payload Pouch did not implement its description | It added single-target auto-attack damage without the thrown-weapon restriction. Its proc fields merged with Savage Reflexes: two 15% effects could become a 30% proc of their combined damage. | Independent rolls. Pouch is a 15% thrown auto-attack proc for 8 Physical DMG on the original target and up to four other hostile targets within 3m. Splash cannot trigger on-hit effects. |
| Healing riders did not require healing | Defensive Harmony and Soul Amplification could trigger at full HP or before damage-derived healing was reduced to zero by its aggregate cap. | Riders require actual positive HP restoration. Damage-derived healing checks them after its 50%-of-damage budget is enforced. |
| Disruption Expert extended unrelated debuffs | Its implementation used the all-debuff duration stat despite promising ability disruption. | A category-driven stat extends Foggy Mind, Force Disruption and ability-cost debuffs. Iron Grip retains its general debuff role. Stunned and Immobilized now consistently carry the Debuff category. |
| Duration bonuses exceeded the hard-control budget | Bonuses could extend an authored 30-second control beyond 30 seconds. | The final duration is capped at 30 seconds after outgoing bonuses and resistance. Soft debuffs retain their duration bonuses. |
| Control conversion could postpone immunity | A late Daze-to-Knockdown conversion could start a fresh control timer. Refresh/extension APIs could also lengthen hard control. | Conversion cannot outlast the replaced effect's remaining duration. Hard control cannot be refreshed or extended. Existing shared immunity lasts 20 seconds after control ends. |
| Player/companion control areas lacked finite caps | Twenty-two weapon, Mimicry and beast perk/rank areas could affect an unlimited number of enemies. | Those areas now affect at most five enemies. Existing explicit caps on other abilities, including Rupturing Quake's 12-target cap and Force Push's rank caps, remain intact. |
| Four Mimicry control cooldowns missed the existing budget | Brace Breaker, Concussive Challenge, Suppressing Shot and Tail Sweep had 15-second Daze on an 18-second cooldown. | Their cooldowns are 24 seconds, meeting the authored 1.5-times-duration minimum. |

The independent-proc example changes from an erroneous expected 5.4 bonus damage per attack (30% of 18) to 2.7 before mitigation (15% of 10 plus 15% of 8) when both procs are eligible on the original target. Pouch's additional area targets are separately bounded.

The combined Stamina-discount example is Staff -7 STM, Pistol/Rifle/Throwing -7 STM and another hostile combat ability -4 STM while all three discounts are active. A ranged ability consumes the ranged/global sources and leaves the Staff-only -3 STM source available until its own expiry.

## Control timeline

Immunity starts **after control ends**, not when control begins. At the maximum duration, the earliest repeat is 30 seconds of control followed by 20 seconds of immunity: at most 60% control uptime under this shared gate, even with several attackers. Shorter control produces a shorter controlled window followed by the same immunity window.

Conversion preserves the original upper bound. For example, converting Daze with 1.5 seconds left into a one-second-frequency Knockdown allows at most one tick, not a new six-second effect. Conversion with less than one tick remaining is rejected while the original effect finishes. Refresh and extension cannot postpone the immunity window.

This bounds hard control. It does not prove that every combination of soft debuffs, movement restrictions, resource pressure and incoming damage feels fair. The checklist includes coordinated attacker tests for that reason.

## Bible and asset integrity

- Updated 23 perk descriptions: Payload Pouch and 22 capped area-control ranks. Updated the four Mimicry cooldowns and the global damage/healing/control budget notes. Corrected the stale Guard budget note to the existing 55% runtime cap.
- Edited workbook cells at the zip/XML level and ran the required local Bible formatter and manifest/audit refresh. All **28,290 formula cells and their cached results** match the pre-edit snapshot.
- Verified the weapon generator reproduces the new Pouch stats, discount scopes, disruption duration stat and seven changed weapon area-rank caps.
- Updated existing TLK entries, regenerated the binary TLK and round-tripped it: all **22,304 entry texts** match the JSON source. No new TLK IDs or targeting/icon resources were required.
- Refreshed the player-message audit snapshot. Its only semantic delta is the already-existing animation preview message using the formatted duration field; other changes are shifted source line numbers.

## Validation

**Final full suite: 2,714 passed, zero failed, zero skipped**, in 7 minutes 56 seconds. The final incremental build succeeded with zero warnings and zero errors. Builds skipped the Windows post-build deploy as required.

```powershell
dotnet build SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never --no-restore
dotnet test SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj --no-build --no-restore --results-directory TestResults/BibleReview --logger 'trx;LogFileName=bible-review-verified-full.trx'
```

The completed TRX is `TestResults/BibleReview/bible-review-verified-full.trx` in this workspace. Parent and TLK submodule whitespace checks also passed.

The focused damage/control checks passed 199 tests. After making the duration test helper compatible with status-effect discovery, the combined status-loader and perk-combination regression run passed all 40 tests. The behavioral coverage includes 10,000 pairs of independent proc rolls and 9,216 mitigation pairs, plus exact boundary cases for tiny hits, flat damage and late control conversion.

## Required gameplay verification

Run the linked tracker scenarios with legal builds, against representative enemies and in PvP where supported. Prioritize the 30 direct rows, then combine the affected damage, mitigation, healing and control sources rather than merely repeating isolated perk activations. For damage-path changes, smoke-test ordinary attacks, queued weapon abilities, direct casts, periodic damage, triggered damage, NPC attacks and companion attacks.

Release confidence still requires real NWN checks of damage attribution, queued-ability resource consumption, target selection, native effect removal and shared immunity across cleansing/logout. Check high damage plus sustain, high avoidance plus mitigation, coordinated control and crowded area effects. Automated checks establish the listed invariants; they cannot establish that every possible build is balanced in play.

The code, workbook and TLK changes have not been deployed, and the engine playtests in this checklist have not been claimed as completed. Deployment must include the regenerated TLK with the server changes.
