# Dual-wield combat

Two equipped melee weapons share one attack-delay gate. Each ordinary cycle resolves a
main-hand roll and an off-hand roll. Each roll uses its own weapon's accuracy, damage type,
damage amount, skill, critical roll, and on-hit effects. The existing combined weapon delay
and off-hand delay reduction still determine the cycle interval.

Weapon ACC and enhancement properties contribute once to the weapon carrying them. Their
native equipped attack-bonus effects must not also enter the creature-wide accuracy sum.
Ordinary accuracy buffs still apply to both hands.

The server's attack-cycle floor remains 1,750 ms. Faster cadence batches cycles, up to three
cycles/six weapon rolls per batch. Limited-attack charges count actual weapon rolls,
including misses. A final limited no-delay charge can produce an odd extra main-hand roll.
Skill-scoped effects count only the hands they apply to when limiting a batch or expiring
its fractional progress. An off-hand no-delay effect must grant an extra off-hand roll.
Temporary one-shot no-delay procs capture their matching hands before consumption. Their
bonus uses a matching hand even at the minimum delay, without imposing a one-charge cap
on the rest of the timed batch.
Combat and the character sheet use the same timing-skill selection for mixed weapons.
Each eligible weapon roll gets its own deflection attempt, including both hands and
accelerated rolls in the same native combat round. Shield Deflection still replaces
weapon deflection, and queued abilities still use their separate impact rules.
Damage-triggered effects remain per hit and retain their own cooldowns; poison coatings
retain their shared six-second application cooldown.

Queued weapon abilities reserve the first eligible landed roll. Only that roll's ordinary
damage is replaced. Other rolls in the batch remain normal attacks even while the engine
is still waiting to deliver the reserved weapon's on-hit script. The reservation is cleared
on consumption, cancellation, or replacement of the queued ability. NWN can query damage
twice for the same roll, so its replacement remains active until the next attack roll starts.
Placeable hits reserve their first eligible roll too. Ranged reservations accept the
launcher's captured ammunition stack as the originating hit and retain the launcher's
damage profile when applying the ability.

## Native engine contract

`CNWSCombatRound.GetCurrentAttackWeapon` takes a weapon attack type, **not** a boolean:
`0` infers the current weapon, `1` selects main-hand, and `2` selects off-hand. The separate
`GetDamageRoll` hook receives a boolean off-hand parameter and must translate it.

`ResolveAttack(target, count, animationTime)` can resolve both hands in one call. NWN
chooses off-hand rolls once `CurrentAttack` reaches `OnHandAttacks + AdditionalAttacks +
BonusEffectAttacks`. `WeaponAttackCycle` sets these boundaries before resolving the batch.
Keep the attack data alive until the native damage/animation phase finishes; do not call
`RecomputeRound` or clear attacks between the two hands.

Single weapons, shields, ranged attacks, natural weapons, and double weapons without a
separate left-hand weapon keep their existing scheduling paths.

## Animation playback

`WeaponAttackAnimation` captures the completed melee batch and presents its rolls as
consecutive native attack animations, alternating main hand and off hand. The sequence fits
inside the existing native animation duration, capped at 1,650 ms so it ends before the next
1,750 ms cycle. Faster batches divide that same window among their rolls. Late client updates
use the remaining time in a slot; they do not build an animation backlog.

This is presentation only: rolls, damage, queued-ability reservations, on-hit processing and
charge consumption retain their existing timing. Combat-log entries can therefore still
arrive together. No delayed damage callbacks or additional attack actions are introduced.

In engine 8193.37.17, `ComputeUpdateRequired` does not flag a new attack burst when the
animation, speed and target are unchanged. Its companion serializer only includes matching
attack-group entries among the last three attack slots. Two narrowly scoped hooks supply a
fresh update for each visual swing and serialize one captured roll at a time. The client can
select a fresh native swing variant instead of looping the original pose. Variant availability
still depends on the creature's model and combat animation set.

The serializer projection restores all touched fields in `finally`, including on failure;
the real combat cursor and attack budgets never change. Playback stops applying when the
native action, target, combat cursor or group no longer matches the captured batch. Ranged
attacks keep their original visual path. Inactive playback records expire automatically.

## Verification

`DualWieldEngineTests` exercises real commanded attacks, cold/poison weapon separation,
six-roll native batches, weapon ACC isolation, missed-attack charges, and queued abilities.
It also reads real serialized animation packets and verifies restoration of native combat
data, including interrupted playback and simulated serialization failure.
`CombatAttackDelayTests` covers shared-cycle cadence and individual-roll charge limits.
`WeaponAttackAnimationTests` covers non-overlapping slots and alternating hand presentation.

The server tests do not render a client. Check paired hit feedback and animation appearance
in a connected client, including high haste and switching between one and two weapons.
