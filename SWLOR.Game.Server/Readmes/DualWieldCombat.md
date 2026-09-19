# Dual-wield combat

Two equipped melee weapons share one attack-delay gate. Each ordinary cycle resolves a
main-hand roll and an off-hand roll. Each roll uses its own weapon's accuracy, damage type,
damage amount, skill, critical roll, and on-hit effects. The existing combined weapon delay
and off-hand delay reduction still determine the cycle interval.
Main-hand rolls resolve first. Off-hand rolls resolve after the main-hand animation and
its ready transition, so native damage and hit effects follow the two separate swings.

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
Commanded dual-wield cycles call `ResolveAttack` separately for each hand, keeping that
same prepared budget and starting the shared delay timer only once. Haste rolls remain
grouped by hand. Limited timing effects granted during the cycle are released after the
off hand finishes or is cancelled, so they cannot be spent by an already-budgeted roll.
Keep the attack data alive until the native damage/animation phase finishes; do not call
`RecomputeRound` or clear attacks between the two hands.

Single weapons, shields, ranged attacks, natural weapons, and double weapons without a
separate left-hand weapon keep their existing scheduling paths.

## Animation playback

`WeaponAttackAnimation` captures each resolved hand and presents consecutive native
attack animations. The installed `a_ba` sword clips are 1,000 ms long; the 1,750 ms
gameplay cycle floor is not an individual clip length. Every dual-wield cycle presents
main hand, a 100 ms ready transition, off hand, then ready again. Both hands are always
included. Extra haste rolls share their hand's presentation without dropping the other hand.
At the fastest 1,750 ms cadence, each hand gets 775 ms plus its 100 ms transition; ordinary
weapon delays retain the clips' natural 1,000 ms duration. The next hand waits for the
observer's preceding swing and ready transition. Late updates never skip a transition or
compress its next swing to catch up.

The off-hand callback runs after the first swing plus its transition, using the same
duration as playback (1,100 ms normally; 875 ms at the fastest cadence). It resolves the
real native attack, including its roll, feedback, damage and on-hit processing, then
publishes the second animation without restarting the first. It does not create another
attack action or restart the cycle timer. Client serialization never applies damage.
The callback revalidates the action, target, equipped weapons, combat cursor, range and
line of attack. Cancellation, death, or invalidated combat state discards the pending hand.

In engine 8193.37.17, `ComputeUpdateRequired` does not flag a new attack burst when the
animation, speed and target are unchanged. Its companion serializer only includes matching
attack-group entries among the last three attack slots. Two narrowly scoped hooks supply a
fresh update for each visual swing and serialize one captured roll at a time. A fresh burst
alone does not randomize NWN's clip selection. The same packet now carries explicit
animation replacements (update mask `0x1000000`, serialized before the attack), choosing
left slash, right slash or stab without repeating the preceding cycle's selection.
Off-hand slashes remain distinct. Custom ability clips are preserved, and equipment remaps
such as katar-to-unarmed retain their destination family. Variant availability still depends
on the creature's model and combat animation set.

NWN returns the native actor to ready after its damage phase, before the second visual hand.
That is not an interruption while the same attack action and target remain active. The
playback survives this engine transition, and duplicate native animation updates are
suppressed so they cannot restart or cut short a swing already playing on the client.
Each hand receives an explicit attack-to-ready transition rather than two consecutive
updates with the same attack pose.

The serializer projection restores all touched fields in `finally`, including on failure;
the real combat cursor and attack budgets never change. Playback stops applying when the
native action, target, combat cursor or group no longer matches the captured batch. Temporary
clip mappings exist only during serialization and are never saved to the creature. Observers
receive the actual equipment/ability mappings again at completion or interruption. Ranged
attacks keep their original visual path. Inactive playback records expire automatically.

## Legacy basic vibroblades

The retired `longsword_b` template can survive in saved inventory without a DMG property,
causing the generic damage fallback of 1. Its current equivalent, `b_longsword`, has DMG 5,
Delay 230 and a rank-zero Vibroblade requirement. `BasicVibrobladeCompatibility` restores
only missing properties on those two basic templates during login, acquisition and the
existing stored-item migration. Existing damage, enhancements, identity, appearance and
custom names remain intact. The default name is **Basic Vibroblade LS** in both cases.

## Verification

`DualWieldEngineTests` exercises real commanded attacks, cold/poison weapon separation,
six-roll native batches, weapon ACC isolation, missed-attack charges, and queued abilities.
It also reads real serialized animation packets and verifies restoration of native combat
data, including interrupted playback and simulated serialization failure. It checks explicit
variant projection/restoration, late-observer animation durations, legacy item repair and
matching noncritical damage for identical equipped basic vibroblades.
Commanded-animation tests follow the full attack action across engine updates at ordinary
and fastest cadence. They require main/ready/off/ready packets, completed swing durations,
exactly the original two damage rolls, and separate native HP reductions for each hand.
Interruption tests cover stopping, removing the weapon, either combatant dying, leaving
melee reach, switching targets, and paralysis before the off hand. A commanded queued-ability
test verifies one ability consumption and a separate ordinary off-hand hit.
The ordinary-cadence test reproduced the
previous regression: the native ready pose canceled playback before the off hand was sent.
`CombatAttackDelayTests` covers shared-cycle cadence and individual-roll charge limits.
`WeaponAttackAnimationTests` covers both-hand retention, stage timing and variant selection.

The server tests do not render a client. Check paired hit feedback and animation appearance
in a connected client, including high haste and switching between one and two weapons.
