# Item appearance rules

Read this before creating or changing an item blueprint, appearance catalog, generator,
or migration that assigns item models.

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
Breastplate revision. Production has run both; server migration 28 is retired, and characters
not yet upgraded receive migration 23 at the next boot. Saved DM creatures receive the model
correction within their existing artwork visit, retaining authored stats and equipment slots.
Valid custom models and the reserved NPC prop remain untouched. Keep the appearance, type and
stat corrections in one combined visit per release rather than adding another pass; a later
stored-item correction goes into the next release's stored-item sweep (`Migrations.md`).
