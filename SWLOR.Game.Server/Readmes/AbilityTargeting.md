# Ability Targeting Rules

Required reading before adding or changing an active ability, its `feat.2da`/`spells.2da` rows, or its targeting metadata. `AreaAbilityTargetingTests` and `QuickCastTests` enforce the area and optional-targeting rules respectively.

Declare targeting metadata on the ability definition through the builder, never in separate per-ability lists.

- A hostile active ability presents a manual target cursor only when it is a single-target *cast* or an **aimed** area. Targeted support casts retain their normal ally/self selection unless the player enables optional self-casting below. Queued weapon abilities (fire on the wearer's next landed auto-attack) and **self-centered** area abilities must NOT prompt for a target: in `feat.2da` they use `TARGETSELF=1` with `HostileFeat` cleared, and in C# they must not call `RequiresTarget()` (`ConfigureWeaponAbility` already skips it for `IsQueuedWeaponAbility`).
- **Aimed vs self-centered is decided by the area's shape, and the shape must match the Design Bible wording.** An ability whose Bible description says "in a line" or "in a cone" is *aimed*: the player chooses the direction, so it needs a cursor. An ability that damages "enemies within Nm" (naming only a radius) is *self-centered*: it always originates on the caster and needs no cursor.
- `RequiresTarget()` means a real target object is mandatory; it is not a cursor flag. Ground- or direction-aimed areas must declare targeting metadata and use `AbilityDetail.RequiresLocationTarget`, while leaving `RequiresTarget` false so empty-ground casts work. An area whose Bible explicitly requires a selected creature may call `RequiresTarget()`, but the builder must never infer that requirement from shape alone. `CanUseAbility` validates location targets separately and applies `MaxRange` only when the definition explicitly calls `HasMaxRange`—the default 5m object range must never become an implicit area-placement limit. A free-destination dash that should go its full reach toward a farther click (Pistol Rush) calls `ClampsLocationTargetToMaxRange()`; its own handlers shorten the destination instead of the range check refusing the cast.

  | Bible wording | `AbilityTargetingShapeType` | Cursor | `feat.2da` | C# targeting spell |
  |---|---|---|---|---|
  | "in a line" | `Rect` (sizeX = length, sizeY = width) | yes | `TARGETSELF` blank, `HostileFeat=1` | real `Spell`, **per rank** |
  | "in a cone" | `Cone` (sizeX = length, sizeY = width) | yes | `TARGETSELF` blank, `HostileFeat=1` | real `Spell`, **per rank** |
  | "to enemies within Nm" | `Sphere` (sizeX = radius, sizeY = `0`) | no | `TARGETSELF=1`, `HostileFeat` blank | real `Spell`, **per rank** |

  **State the size in the description.** The generator reads the numbers out of the Bible line — "in an 8m x 2.5m line" and "enemies within 3m" produce those exact sizes — and only falls back to an archetype default when the line names none. A description that omits its size silently inherits a default that may not be what you intended.

  Two traps in that parsing, both of which shipped bugs before:
  - A radius area is recognised by the **noun**, not by "within Nm" alone: "enemies within 5m", "all targets within 6m" and "hostile targets within 5m" are areas. A bare "within Nm" is not, because the Bible also uses it for an ally buff ("allies within 5m"), a leash range ("while within 20m") and a placement range ("a field within 15m"). The singular form is also excluded — "one enemy within 5m" is a reach check, not a shape.
  - A `line`/`cone` is only recognised in the "in a line" form or immediately after a stated size ("8m x 2.5m line"). A bare mention of the word does not count, because "anchors a defensive line" is a radius buff. Matching the literal phrase alone used to miss the sized form entirely and infer a self-centered Sphere for an aimed line — losing the cursor.

  `Earthshatter I/II` is the reference implementation for an aimed line.
- **Every rank of an area ability needs its own `spells.2da` row and its own `Spell` enum value.** Never leave `Spell.Invalid` on a rank that declares a real `AbilityTargetingShapeType`. The two failure modes differ, and only one of them is loud:
  - Targeting metadata that *exists* but carries `Spell.Invalid` is rejected at load — `AbilityTargeting.ValidateTargeting` throws `InvalidOperationException`.
  - Passing `Spell.Invalid` to `ConfigureWeaponAbility` never reaches that check, because `ApplyTargetingMetadata` skips building targeting metadata at all. The ability ends up with no `Targeting`, so it silently loses both its cursor and its ground area marker with nothing thrown or logged. This is how several ranks shipped broken, and it is what `AreaAbilityTargetingTests` guards.
- `tools/GenerateWeaponArchetypeImplementation.py` encodes the table above, and `SWLOR.Game.Server.Tests/Perks/AreaAbilityTargetingTests.cs` enforces it by reflecting over every `IAbilityListDefinition` and cross-checking `feat.2da`. Keep that test green rather than adding explicit per-ability lists. Deploy C# and rebuilt HAKs together; repack the module when `Module/` content also changes.

## Radial and quickbar class menu rows

The client builds the radial's Special Abilities > Standard list and the quickbar's class ability menu only from known feats that have a row in `SWLOR_Haks/sw_2da/CLS_FEAT_FIGHT.2da`. A player ability feat without one still works from an existing hotkey but never appears in either menu, so players cannot find or bind it.

- Give every rank of a player ability exactly one `CLS_FEAT_FIGHT` row: `FeatLabel` equal to its `feat.2da` `LABEL`, `List=1`, `GrantedOnLevel=99` and `OnMenu=1`.
- A castable ability's feat must also link to a `spells.2da` row whose `Label` matches and whose `FeatID` points back to it. Passive Mimicry traits are the exception: they keep `SPELLID=****` (`MimicryTests` validates them).
- A rank that presents a manual hostile target cursor (hostile, not a queued weapon ability, and `RequiresTarget` or `RequiresLocationTarget`) must use a row below 1024; the client's hotbar scan stops there. Ghost Protocol must also stay below 1024, because automatic quickbar placement can leave it without a primary-bar slot. Reuse a free row below 1024 before appending, and never move these rows above it.
- Hide a retired generated feat's row (`List` and `OnMenu` not `1`) instead of leaving it visible.
- The `AutoCast<Feat>` alias rows for optional targeting are generated separately; see [Generate and validate the native variants](#generate-and-validate-the-native-variants).

`PlayerAbilityRadialMenuTests` enforces these rows only for player ability feats with IDs of 2000 and above; older active abilities below 2000 (such as `Provoke1`, `Provoke2` and `Tame`) follow the same rules but are not covered, so check their rows by hand when you change them. `QuickCastTests` enforces the alias rows. In the client, with both Settings > Targeting options off and then with each one on, check that the ability appears under the radial's Special Abilities > Standard and that it can be bound from the quickbar class menu (right-click an empty slot, Special Abilities > Standard) and fired from that slot.

## Optional self-cast and current-enemy targeting

Players configure **Self-Cast Support Abilities** and **Cast on Current Enemy** in Settings > Targeting. Both settings default off, save per character, and persist through reconnects and server resets. They change the presentation of existing feat buttons, quickbar hotkeys and radial choices; they are not additional abilities or chat commands. Current-enemy targeting requires a living hostile creature that the player is actively attacking, not merely a selected portrait or a stale combat target. The normal ability pipeline still enforces ownership, target validation, costs, cooldowns and incapacitation.

### Authoring a new ability or rank

1. Define the ordinary ability and canonical native feat first. For a creature-targeted cast, keep its normal manual targeting and `RequiresTarget()` requirement so the off setting preserves ally/enemy selection. Do not set `TARGETSELF=1` on the canonical targeted cast to implement an optional preference, or use `.UsesActiveAttackTarget()` as a shortcut: that changes the default behavior and excludes it from optional targeting.
2. Check every rank against `Service/AbilityService/QuickCastPolicy.cs`. Ordinary eligibility requires `IsCastedAbility`, `IsSingleTargetAbility` and `RequiresTarget`, with no area flag, active-attack-target behavior or non-`None` targeting shape. Self-casting also accepts explicitly opted-in, non-hostile creature-targeted area casts with a `Sphere` shape and exactly `HelpsAllies` flags, such as Power Cell III. These retain their ally-centered area when targeting manually and center on the caster when self-casting.

   | Ability kind | Definition requirement | Optional mode |
   |---|---|---|
   | Support cast that legally accepts its own caster | Non-hostile, eligible metadata, `.AllowsSelfQuickCast()` on each applicable rank | Self-cast |
   | Ally-targeted sphere that legally accepts its own caster | Casted, `IsAreaAbility`, `RequiresTarget`, exactly `HelpsAllies` flags, `.AllowsSelfQuickCast()`, no active-attack-target behavior | Self-cast |
   | Hostile cast targeting one creature | Eligible metadata and `.IsHostileAbility()` | Current enemy; no separate opt-in flag |
   | Ally-only or revival cast | Leave `.AllowsSelfQuickCast()` unset | Normal manual targeting |
   | Queued weapon, self-centered area, aimed line/cone or location-targeted cast | Keep its existing targeting metadata | Existing targeting behavior |

   `.AllowsSelfQuickCast()` does not bypass custom validation. Use it only when casting on oneself is legal. Follow `Feature/AbilityDefinition/Leadership/RousingShoutAbilityDefinition.cs` or `FirstAid/MedKitAbilityDefinition.cs` for support casts, and `Force/ForceDrainAbilityDefinition.cs` for hostile casts. Declare eligibility on the definition; never maintain a parallel list of ability names or IDs.
3. Generate the native presentation variants from freshly compiled definitions using the commands below. Every eligible rank needs its own `AutoCast<CanonicalFeatEnumName>` row and `FeatType` member. The shared map discovers these names automatically; no new ability-specific runtime branch is needed.
4. Keep canonical IDs in perk grants, saved builds and level history. The shared `QuickCast` service selects native presentation and normalizes quickbar slots when preferences, ownership or login state change. Follow the existing grant/restore/load helpers when adding a new ownership path; do not grant presentation aliases as a separate perk.

### Generate and validate the native variants

Run from the repository root with `SWLOR_Haks` initialized at the intended revision. The generator reads the compiled definitions, so a stale DLL can silently omit a newly authored ability.

```powershell
dotnet build SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never
powershell -ExecutionPolicy Bypass -File tools/GenerateAutomaticTargetingFeats.ps1
```

The generator appends missing entries to `SWLOR_Haks/sw_2da/feat.2da` and `SWLOR.NWN.API/NWScript/Enum/FeatType.cs`. It preserves every canonical row, copies all presentation cells including names, descriptions, icons and **the same `SPELLID`**, then changes only the alias label, `TARGETSELF=1` and cleared `HostileFeat`. It also appends a `SWLOR_Haks/sw_2da/CLS_FEAT_FIGHT.2da` row for each alias that copies its canonical row's `List`, `GrantedOnLevel` and `OnMenu`: the client builds the radial and quickbar class ability menus from that table, and a player with a preference on knows only the alias, so an alias without a row hides the ability from both menus. Optional variants do not need separate spell rows or artwork; the ordinary ability still follows the area, spell and [icon rules](IconStandards.md).

If generation added enum members, rebuild the test project once so the new IDs are compiled before validating. Otherwise reuse the fresh build above. Then run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/GenerateAutomaticTargetingFeats.ps1 -CheckOnly
dotnet test SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj --no-build --filter "FullyQualifiedName~QuickCastTests|FullyQualifiedName~AreaAbilityTargetingTests|FullyQualifiedName~AbilityTargetingMetadataTests"
```

For a Release build or a custom artifacts directory, pass `-AssemblyDirectory` pointing to the directory containing the matching `SWLOR.Game.Server.dll` and `SWLOR.NWN.API.dll` on both generator commands. `-EligibilityJson` is only for output captured from the same compiled-definition exporter; a hand-authored file would bypass discovery.

`-CheckOnly` must report no required additions. The generator fails on mismatched existing alias cells or IDs and never rewrites existing alias rows. Follow the repository's replace-by-adding rule when a native definition needs replacement; do not recycle IDs or force the check to pass by rewriting old rows. Removing eligibility leaves old native rows intact; runtime metadata gates their use. `QuickCastTests` checks completeness for all eligible ranks, canonical-cell/`SPELLID` parity for every alias including retained rows, and class-feat-row parity.

Run the relevant ability behavior cases and the `QuickCast` engine filter using [EngineTesting.md](EngineTesting.md). Include `PlayerPerkFeats` or `MimicryLoadout` coverage when changing grants, ranks, refunds, loadouts or quickbar restoration.

### Deploy and verify normal client input

Commit the generated `FeatType` entries in the server repository and the generated `feat.2da` and `CLS_FEAT_FIGHT.2da` rows in a companion HAK PR. Push both branches, link both PRs, and update the parent submodule pointer. Merge and deploy them together: rebuild `sw_2da.hak` using `SWLOR_Haks/BuildHaks.cmd` and deploy the rebuilt C# assemblies. Repack with `Module/PackModule.cmd` only when module content also changed. Missing deployed native rows are a HAK deployment problem; changing C# target resolution cannot remove a client cursor that appears before the server receives the ability.

Use an isolated test server and the background, muted client rules in [InEngineVisualVerification.md](InEngineVisualVerification.md). A client feature requires a real connected-client check; headless engine activation or an offline rendered window alone cannot prove hotkey behavior. Wait for the module and client area to finish loading before opening test windows or placing an enemy. If using instrumentation, verify saved preferences and native aliases before setup and never repair them during the reconnect/reset checks.

- **Off:** confirm both preferences default off on a new character. With the relevant option off, the new targeted cast retains its normal manual target selection, including legal ally selection for support abilities.
- **On:** enable the relevant preference through Settings and Save. Activate the new rank through an existing normal hotkey/button, without an extra target click. For current-enemy casts, start attacking a living enemy first; also verify rejection of missing, dead or stale targets. Confirm the actual effect or damage on the intended creature, resource cost and cooldown, not just a feat-use event. Check each preference independently when the change affects both modes.
- **Reconnect:** save/logout normally, stop the owned client, then reconnect the same saved character with a fresh client to the same server process. Without toggling or resaving Settings, verify preferences, owned native feats and quickbar bindings, then repeat the normal hotkey cast.
- **Server reset:** log out normally, restart the actual test server process while keeping its saved character and database, then reconnect with a fresh client. Repeat the saved-state and no-extra-click hotkey checks without repairing or resaving preferences.

Preserve native screenshots and input timestamps showing no target click between the hotkey and its impact, plus character identity and server/client process identities for the lifecycle checks. Correlate hostile damage to the caster and exact enemy so ordinary auto-attacks cannot count as success. Check the enemy chosen at activation; a cast may clear or replace its attack queue afterward. Keep failed fixture attempts separate from the final passing cases, and report any unverified cases plainly. Stop all owned clients/servers afterward, remove temporary profiles, and record validation and deployment requirements in the PR description.
