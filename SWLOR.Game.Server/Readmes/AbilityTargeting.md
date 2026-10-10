# Ability Targeting Rules

Required reading before adding or changing an active ability, its `feat.2da`/`spells.2da` rows, or its targeting metadata. `SWLOR.Game.Server.Tests/Perks/AreaAbilityTargetingTests.cs` enforces these rules.

- An active ability presents a manual target cursor only when it is a single-target hostile *cast* or an **aimed** area. Queued weapon abilities (fire on the wearer's next landed auto-attack) and **self-centered** area abilities must NOT prompt for a target: in `feat.2da` they use `TARGETSELF=1` with `HostileFeat` cleared, and in C# they must not call `RequiresTarget()` (`ConfigureWeaponAbility` already skips it for `IsQueuedWeaponAbility`).
- **Aimed vs self-centered is decided by the area's shape, and the shape must match the Design Bible wording.** An ability whose Bible description says "in a line" or "in a cone" is *aimed*: the player chooses the direction, so it needs a cursor. An ability that damages "enemies within Nm" (naming only a radius) is *self-centered*: it always originates on the caster and needs no cursor.
- `RequiresTarget()` means a real target object is mandatory; it is not a cursor flag. Ground- or direction-aimed areas must declare targeting metadata and use `AbilityDetail.RequiresLocationTarget`, while leaving `RequiresTarget` false so empty-ground casts work. An area whose Bible explicitly requires a selected creature may call `RequiresTarget()`, but the builder must never infer that requirement from shape alone. `CanUseAbility` validates location targets separately and applies `MaxRange` only when the definition explicitly calls `HasMaxRange`—the default 5m object range must never become an implicit area-placement limit.

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
- `tools/GenerateWeaponArchetypeImplementation.py` encodes the table above, and `SWLOR.Game.Server.Tests/Perks/AreaAbilityTargetingTests.cs` enforces it by reflecting over every `IAbilityListDefinition` and cross-checking `feat.2da`. Keep that test green rather than adding explicit per-ability lists. After changing any of this, rebuild the haks and repack the module so the change deploys.
