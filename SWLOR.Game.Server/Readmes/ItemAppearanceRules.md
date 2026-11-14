# Item appearance rules

Read this before creating or changing an item blueprint, appearance catalog, generator,
or migration that assigns item models.

Wearable mesh, skinning and animation edits also follow
[GarmentModelRules.md](GarmentModelRules.md), including baseline preservation,
clipping checks and dependent robe/part-skin regeneration.

- Choose an existing model appropriate to the item's base type and confirm its actual
  appearance in the NWN client. A model number or an item's display name does not prove
  what it looks like. Use the relevant appearance definition under
  `Feature/AppearanceDefinition/ItemAppearance/` as the catalog of player choices.
- **Helmet model 223 (`helm_223`) is Darth Revan's mask and hood. Never use it for
  dungeon loot, crafted gear, quest rewards, store stock, generic NPC gear, or migration
  fallbacks.** The existing `Module/uti/revan_mask.uti.json` is an NPC-only prop marked
  `NO_ECONOMY`; it is the sole reserved use. Do not copy its model onto other items or
  add it to player appearance choices. Standard visor model 110, already used by
  `scav_visor` and `cox_visor`, is the replacement for the affected crafted visors.
- For helmets (`BaseItem` 17), keep `ModelPart1` and `xModelPart1` consistent. The
  extended word field takes precedence when present; changing only the byte field can
  leave the old appearance active. Models above 255 require the extended field and the
  byte fallback used by existing blueprints; never truncate an extended model to a byte.
- Every armor part the catalog offers must have a model for each playable body (`h`, `e`,
  `o`, `d` for both genders; `z` uses the droid catalog). The editor hides a part whose
  model is missing for the wearer, so a gap silently removes the choice for that species.
  `SWLOR_Haks/tools/GenerateRacialPartVariants.py` refits missing variants from the human
  donor the native palette lookup already selects (manifest `RacialPartVariants.json`);
  run it with `--apply`, then the tint, part-skin, neck, robe RGB and robe cloak generators,
  and keep `--check` green. Racial body parts (201-214, 251) stay species-only. A style
  with no model on any body does not belong in the catalog; `ArmorPartCoverageTests`
  requires both the model and the tint row for every other style.
- A body's own part model that borrows another model's texture must share that model's UV
  layout; a mesh laid out for a different texture renders white, black or scrambled patches.
  Replace such a variant by listing it in `RacialPartVariants.json`, which refits the donor.
  Stock bodies draw the human part's texture, so a body left on the stock mesh after the human
  part gets a new design shows this; `SWLOR_Haks/tools/RacialPartCoverage.py --check` (stock
  art required, `TestRacialPartCoverage.py`) fails on any such part not in its known-gaps list.
- Armor styles 0, 1 and 2 all display the wearer's own body part on every slot; the engine never
  draws models 001 or 002 for them, whatever a preview picture shows. Those models appear only as
  a creature's body part (armor 1 on body part 2 shows model 002). Players have no shoulder or belt
  body part, so those slots show nothing at 1 or 2: the catalog omits both styles there, and
  `ArmorPartAppearance` repairs saved armor to 0 when it is worn. Other slots show the bare body.
- Styles 1 and 2 add no armor part on any body (human, elf, half-orc, dwarf and droid models), so per
  Xisifer's rule no list that offers 0 ("None", which every neck list offers) offers 1 or 2, and no
  list offers 2 at all. A list without 0 (general shin; droid chest, pelvis, bicep, forearm, hand,
  thigh, shin, foot) keeps 1 as its bare option. Saved armor and blueprints keep 1 and 2 and are not
  moved: SWLOR binds dyes to the material named by the armor part number, so moving them would recolor
  the pelvis (cloth dye on the underwear), the female human chest and droid parts (metal dye on
  chrome), and body part 2 on human necks, hands and feet. The editor accepts a saved 1 or 2 on these
  slots (`ArmorAppearanceCatalog.AcceptsSavedStyle`) and shows it as the list's bare option ("None", or
  1 where the list has no 0), which it looks like; the item keeps its value until the player picks a
  part. Body-part lists (Appearance tab) still offer 1 and 2, so their names stay.
- `RetiredAppearances` lists the armor parts, heads, helmets and cloaks removed from the haks to shrink
  them (none was used by a player, NPC, blueprint or DM template in the 2026-11-13 production data).
  Their parts 2DA and `cloakmodel.2da` rows are retired in place; never offer them again or reuse
  their numbers for new art. `RetiredAppearanceMigration` moves anything still on one to the bare
  option, the race's first head, helmet 1 or the Plain cloak (`SavedDataItemSweepTests`, filter
  `RetiredAppearances`).
- Every catalog part needs a `tintmap.2da` row so palette and exact RGB colors reach it.
  Stock parts are imported with their stock PLT and converted like hak parts; a part that
  only has fixed artwork gets an authored palette from
  `SWLOR_Haks/tools/GenerateAuthoredPartPalettes.py` before `--generate-preserving`.
- Quarterstaff parts 62 (model 6, color 2; editor part 206) in every role are the Tusken Gaderffii,
  used by the Tusken weapons and the Sandstorm Gaderffii loot. A weapon revamp must keep that look in
  those slots rather than replace them with a copy of a generic part (`GaderffiiAppearanceTests`).
- Check embedded item instances in creature equipment, stores, containers, and area
  JSON as well as standalone `Module/uti` blueprints. Update any generator or migration
  fallback that could recreate a prohibited appearance. Preserve stats and item identity.
- Moving a player blueprint to another appearance changes only new copies. Add the old and
  new parts to `ItemAppearanceCatalogMigration.Revisions` so owned copies follow in the
  release's combined item visit. A copy converts only while every part the blueprint changed
  still holds its old value.
- Run `HelmetAppearanceTests` and, when changing its fallback catalog,
  `HeadgearSlotMigrationTests`. Follow `InEngineVisualVerification.md` for actual client
  screenshots. Migration behavior also requires the engine coverage in `EngineTesting.md`.

Blueprint corrections require a module repack and deployment. Migration changes also require
a C# server deployment. Player migration 23 and server migration 28 repaired model 223 on the
affected visors during their combined artwork, type and loot-stat visit, including the Foundry
correction and saved root icons, the appearance editor catalog moves and the Arkanian Dragon
Breastplate revision. Production has run both; server migrations through 29 are retired, and characters
not yet upgraded receive migration 23 at the next boot. Saved DM creatures receive the model
correction within their existing artwork visit, retaining authored stats and equipment slots.
Valid custom models and the reserved NPC prop remain untouched. Keep the appearance, type and
stat corrections in one combined visit per release rather than adding another pass; a later
stored-item correction goes into the next release's stored-item sweep (`Migrations.md`). The release after
player migration 25 and server migration 30 gives owned Sandstorm Gaderffii staffs their look back in its
enzyme and building-icon visit (`GaderffiiAppearanceMigration`).
