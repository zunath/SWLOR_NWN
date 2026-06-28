# Capstone Quest Line Plan

This plan tracks quest-line rules for locking level 50 capstone perks behind quest completion.

## Global Rules

- Each capstone quest line must require the matching skill at rank 50 before the player can accept any quest in the line.
- The perk unlock must require completion of the final quest in its capstone line.
- Each capstone quest line should use new enemies, items, or encounters when it asks for kills, collection, investigation, or other tasks.
- High-level capstone enemies must not be added to low-level or general public spawn tables. Keep capstone enemies in dedicated high-level encounter tables, gated areas, instances, or other content spaces appropriate for level 50 players.
- If no suitable high-level placement exists, do not mix the enemies into a low-level area as a temporary shortcut. Leave the dedicated spawn tables unwired, mark the quest line as not ready for in-game testing, and add area-builder follow-up tasks.
- New fixed-placement spawn tables must include matching `Module/utw` waypoint blueprints in `Module/itp/waypointpalcus.itp.json`. The waypoint blueprint `Tag` should be the spawn table ID, while `TemplateResRef` stays within resref length limits.
- Boss encounters should use the reusable quest encounter activator flow instead of ambient spawn tables.
- Quest encounter activators use `OnUsed = quest_enc`, `QUEST_ID`, `QUEST_STATE`, and `QUEST_ENCOUNTER_*` locals so the same object flow can be reused by non-capstone quest encounters.
- Quest encounter activator objects must be hidden unless the player is on the exact quest and state for that encounter.
- Quest encounter activator visibility should be refreshed from quest state changes, player area entry, and encounter lifecycle events. Do not add per-object heartbeat polling for these activators unless an engine limitation makes it unavoidable, and document the reason if that exception is ever needed.
- Quest encounter activators must enforce one active creature at a time per encounter, a 60-minute cooldown per player who starts the encounter, participant-based quest credit for eligible players fighting the creature, and idle despawn for abandoned fights.
- Progression-critical proof from kill objectives should be granted from quest credit, not corpse loot, when multiple players can receive credit for the same kill. Use quest-state advance key item grants for proof that should go to every credited player.
- Quest journals and dialogue must name the final player-facing location once placement is settled. Avoid promising an existing low-level area if the capstone encounter space still needs to be built.

## Blood Frenzy

Blood Frenzy is the Vibroblade 50 capstone quest line.

### Current Implementation State

- `BloodFrenzyQuestDefinition` defines the five-quest chain and requires `Vibroblade` rank 50 on each quest.
- `BloodFrenzyQuestDefinition.FinalQuestId` is used by the Blood Frenzy perk requirement.
- Sera Vonn exists as the quest giver and is placed in Veles.
- Accepting `blood_frenzy_blade` grants the `ViscaraSewersDepthsKey` key item, displayed to players as `Viscara Sewers Depths Key`. Abandoning that opening quest before completion removes it.
- The Sewers Depths access point uses the existing `tele_obj` teleport placeable pattern. The placed object is named `Enter Sewers Depths` and uses `OnUsed = teleport`, `KEY_ITEM_ID = ViscaraSewersDepthsKey`, `DESTINATION = VISC_SEWER_DEPTHS_INSIDE`, and `TELEPORT_PARTY_MEMBERS = 1`. This follows the existing click-object-to-boss-area pattern used elsewhere instead of introducing a special door flow, while allowing nearby party members within 8 meters of the object to enter with the key holder.
- Blood Frenzy enemies and proof key items exist as new content.
- Blood Frenzy proof items are granted as temporary key items when the relevant kill objective advances the quest. They are removed on quest completion or abandonment and are not required corpse loot.
- Dedicated Viscara Sewers Depths spawn tables exist:
  - `VISCARA_SEWERS_DEPTHS_GENERAL`
- Viscara Sewers Depths loot tables exist for each Blood Frenzy creature and contain ordinary rewards only, not progression-critical proof:
  - `VISCARA_SEWERS_DEPTHS_SCAVENGER`
  - `VISCARA_SEWERS_DEPTHS_PULSE_DROID`
  - `VISCARA_SEWERS_DEPTHS_BUTCHER`
  - `VISCARA_SEWERS_DEPTHS_DUELIST`
  - `VISCARA_SEWERS_DEPTHS_KING`
- The Sewers Depths inside teleport waypoint is intentionally hand-placed with tag `VISC_SEWER_DEPTHS_INSIDE`; it does not need a palette blueprint.
- Sewers Depths waypoint palette templates exist for the ambient spawn table and optional boss spawn points:
  - `bf_sd_general` -> `VISCARA_SEWERS_DEPTHS_GENERAL`
  - `bf_butch_spawn` -> `BF_BUTCHER_SPAWN`
  - `bf_kess_spawn` -> `BF_KESS_SPAWN`
- The normal `VISCARA_VELES_SEWERS` table intentionally remains low-level and must not contain Blood Frenzy enemies.
- `bf_butch_call` is the Blood Frenzy Butcher on-demand quest encounter activator blueprint. It is configured for `blood_frenzy_glass` state 1, uses `OnUsed = quest_enc`, spawns `bf_butcher` at `BF_BUTCHER_SPAWN` when that waypoint exists, has a 60-minute starter cooldown, and hides itself outside the intended quest state.
- `bf_kess_call` is the Kess Draavo on-demand quest encounter activator blueprint. It is configured for `blood_frenzy_mastery` state 1, uses `OnUsed = quest_enc`, spawns `bf_kess` at `BF_KESS_SPAWN` when that waypoint exists, has a 60-minute starter cooldown, and hides itself outside the intended quest state.

### Placement Status

The current module content includes the level 50 Viscara Sewers Depths area and a separate Kess arena area.

- Sera Vonn is hand-placed in Veles Shops.
- `Enter Sewers Depths` is a standard `tele_obj` instance in Veles Sewers and points to the hand-placed `VISC_SEWER_DEPTHS_INSIDE` waypoint.
- The general Sewers Depths spawn table is wired through `VISCARA_SEWERS_DEPTHS_GENERAL`.
- `Blood Frenzy Butcher Marker` is placed in Viscara Sewers Depths and spawns the Butcher at `BF_BUTCHER_SPAWN`.
- `Blood Frenzy Challenge Marker` is placed in the Kess arena and spawns Kess at `BF_KESS_SPAWN`.
- The Sewers Depths area has a teleport to the Kess arena, and the arena has a return teleport to `VISC_SEWERS_ARENA_EXIT`.

Do not consider the Blood Frenzy line fully validated until the in-game progression pass below is complete.

### Area Builder Follow-Ups

- Keep the Sewers Depths access point as a standard `tele_obj` instance instead of a Blood Frenzy-specific access blueprint. Hand-place the inside destination waypoint in the target space with tag `VISC_SEWER_DEPTHS_INSIDE`, or update `DESTINATION` if the inside waypoint tag changes.
- Keep the Sewers Depths spawn waypoint from the custom waypoint palette: `bf_sd_general` for general-purpose enemies. Do not add this directly to the low-level Veles sewer area.
- Ensure normal low-level Veles sewer routes do not path through these encounters.
- Add access gating, transition placement, or encounter boundaries so lower-level players do not wander into capstone enemies by accident.
- Keep `Blood Frenzy Butcher Marker` (`bf_butch_call`) in the final stim lab encounter space and keep `bf_butch_spawn` where the Blood Frenzy Butcher should appear.
- Keep `Blood Frenzy Challenge Marker` (`bf_kess_call`) in the final Blood Frenzy circle encounter space and keep `bf_kess_spawn` where Kess should appear. Use the same `quest_enc`/`QUEST_ENCOUNTER_*` pattern for future on-demand quest encounters.
- Confirm there are enough spawn points for:
  - Red Vein Scavengers
  - Pulse-Frame Training Droids
  - Blood Frenzy Duelists
- Confirm the on-demand spawn positions for:
  - Blood Frenzy Butcher
  - Kess Draavo, the Blood Frenzy King
- Keep Sera Vonn's dialogue, the Blood Frenzy journal text, and the access key item naming aligned on `Viscara Sewers Depths`.
- Run an in-game progression pass from quest 1 through Blood Frenzy purchase after the final area placement is in place.
