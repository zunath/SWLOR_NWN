# Gameplay Icon Standards

Gameplay icons must communicate what an effect does before the player reads a tooltip. These standards apply to ability, feat, spell, status-effect, and other gameplay-facing icons. Environment textures, portraits, item appearance textures, and non-gameplay art are outside this standard.

## Semantic Color

Icons use semantic color on the frame, glow, optional rank badge, or other outer accent. The center artwork may retain discipline flavor, such as Force, Devices, Beastmaster, weapon, medical, command, or elemental identity.

Do not move semantic color randomly into the center artwork. The same role must appear in the same frame/accent location across generated and hand-authored icons.

Final production imports must stamp the semantic frame color deterministically at 32x32. Do not rely on generated source art to preserve the exact category hue.

Approved semantic categories:

| Category | Color | Hex | Usage |
| --- | --- | --- | --- |
| Beneficial | Green | `#54F67A` | Healing, cleansing, protection, restoration, ally buffs |
| Harmful | Red | `#F05454` | Damage, enemy debuffs, vulnerabilities, damage-over-time |
| Self | Cyan-blue | `#4FC3FF` | Self-only stances, toggles, personal buffs, personal movement |
| Control | Violet | `#B56CFF` | Stun, daze, immobilize, fear, silence, disorient, forced movement |
| Deployable | Amber | `#FFB84D` | Beacons, traps, fields, ground effects, persistent placed objects |
| Passive | Gold | `#F5D76E` | Passive traits, innate mastery, always-on bonuses, character progression perks |
| Utility | White-steel | `#DDE6F0` | Detection, language, travel, noncombat, neutral actions |

When an ability does several things, color it by the primary player-facing intent. Damage with a small debuff rider is Harmful. Enemy control with a damage rider is Control. A placed object or persistent ground effect is Deployable unless its identity is overwhelmingly heal, control, or damage.

Passive trait feats use the Passive gold frame/accent even when the always-on bonus improves healing, damage, control, utility, or another active-effect domain. Passive trait icons are not usable abilities and do not need generated cooldown variants.

Ship module placeholder feat icons `ife_sm1` through `ife_sm30` are dynamic texture override anchors, not final player-facing artwork. Do not stamp semantic borders on them, and do not include them in the gameplay icon manifest. The actual ship module art comes from the equipped module texture override.

Status effects use the same semantic frame location, but their color assignment is stricter:

- Beneficial status effects must use the Beneficial green frame/accent.
- Detrimental status effects must use the Harmful red frame/accent.
- Neutral or system status effects may use Utility only when they are neither beneficial nor detrimental.
- **Stances use the Self (stance) color**, not Beneficial, even though a stance is beneficial to its holder. The stance color is what tells a player at a glance that the effect is a stance they toggled on rather than a buff something granted them, and it matches the Self color already used by the corresponding stance *ability* icons.
- A paired self buff and enemy debuff may share a motif, but they must differ by both semantic color and a visible shape/sigil.

Note: most pre-existing stance status effects still carry the Beneficial green frame and predate this rule. New stances follow the rule; converting the existing ones is a separate sweep.

## Every Applied Status Effect Carries an Icon

If an effect is applied to a creature, it **must** declare a real `EffectIconType` — never `EffectIconType.Invalid`. The apply path in `StatusEffect.BuildNativeStatusEffect` only links an `EffectIcon` when the icon is not `Invalid`, and there is no fallback: an `Invalid` icon means the effect changes the player's stats with nothing shown on the status bar. `Invalid` also collapses icon-keyed lookups (`GetStatusEffectsFromIcon`), so dispel/cleanse/query logic cannot tell those effects apart.

If an effect has nothing worth showing — because its magnitude never varies for as long as it is held, so there is no transient state to communicate — then it should not be a status effect at all. Model it as a static stat contribution read by the stat pipeline instead (as the Mimicry passive traits do via `MimicryTraitStat` / `MimicryTraitResistance`). Status effects are for state that starts, changes, or ends; static bonuses belong to whatever grants them.

`tools/UpdateGameplayIconStandards.ps1` enforces this: status effect discovery does not skip `Invalid` declarations, so any new effect without an icon fails the audit until it has an `effecticons.2da` row, artwork, and a custom TLK entry.

## Stat-Configured Icons

A shared status effect whose icon identity is supplied per application through a `StatType`
adjustment — `MeleeRepeatedTargetDamageStatusEffect` reads
`StatType.MeleeRepeatedTargetDamageStatusEffectIcon`, which Vibroblade's Rundown trait sets to
`EffectIconType.RundownStatusEffect` — deliberately owns no icon identity of its own. The player
always sees the *configuring perk's* icon, whose anchor class carries the enum member, manifest
row, TLK entry, and artwork (`RundownStatusEffect` anchors `ief_rndwn`).

Such a class is exempted from the one-class-one-icon model **only** when it declares
`[StatConfiguredIcon]` (see `Service/StatusEffectService/StatConfiguredIconAttribute.cs`); the
audit skips it entirely on that marker. The exemption does not weaken the rule above: the apply
path must refuse to apply the effect when the configured icon resolves to
`EffectIconType.Invalid`, so a mis-wired perk degrades to no visual rather than an invisible
effect. Any icon value fed into the stat must be a real, anchored `EffectIconType` member —
retire the anchor class only together with the icon identity itself.

## Force Alignment Marker

Force power icons carry a **second, orthogonal axis** on top of the semantic frame: a small "gem" marker in the **top-left corner** that shows the power's Force alignment. The semantic frame still communicates effect role (Harmful, Beneficial, Control, …); the corner gem communicates the side of the Force. This lets a player read both facts at once, and complements the Perks window, which groups Force powers by discipline (Alter / Control / Sense).

Marker rules:

- **Scope:** only the Force-tree powers, stances, and passive traits (the five Force perk trees). No other icon carries the marker — including Force-*flavored* NPC, creature, or other-weapon icons.
- **Colors:** `Dark = black (#17171B)`, `Light = light grey (#C4CAD3)`, `Universal/Neutral = yellow (#FFCC1A)`.
- **Construction:** a dark outer ring, a mid-grey bevel ring, then the alignment-colored fill, with a small highlight. The two-tone bezel keeps every gem legible on any underlying art, and the mid-grey bevel gives all three fills (black, light grey, yellow) the same crisp rim.
- **Placement:** top-left, so it never collides with the bottom-right status-effect rank-badge slot. The gem sits on top of the finished icon and never alters the central artwork or the semantic frame.
- **Data source of truth:** the `Alignment` column in `GameplayIconManifest.csv` (`Light` / `Dark` / `Neutral`; blank = no marker).

The marker is stamped and audited by `tools/UpdateFeatSpellIconBorders.ps1` (`-Apply` / `-AuditOnly`), which reads the `Alignment` column. Stamping is idempotent — re-running skips already-marked icons. Because the marker is composited onto the flattened production TGA, changing the palette requires restoring pristine art first (`tools/RestoreAbilityIconArtwork.ps1`) and then re-stamping with `-Force`; do not paint a new marker over an old one.

## Uniqueness

Gameplay icons must be globally unique across abilities, status effects, feats, spells, and other gameplay uses. A player should not see the same visible icon and need context to know whether it means an ability, a status effect, an item action, or another gameplay effect.

The only exception is a generated cooldown icon. Cooldown variants inherit the source icon and add only the standardized cooldown overlay because they represent cooldown state rather than gameplay meaning.

Color alone is not enough to make two icons unique. Related icons need a secondary difference such as a shield/check for beneficial effects, crack/downward mark for detrimental effects, stance ring for self effects, bind/lock mark for control, or beacon plate for deployables.

Two unrelated icons must not share the same base symbol with only a rank badge, filename, tooltip, or semantic color separating them. For example, a healing effect and an ailment-resistance effect both being beneficial is not enough reason for both to use the same medical cross; one should use a healing symbol while the other uses a resistance, shield, antidote, or ailment-specific symbol.

## Rank Display

Ability, feat, and spell icons that appear on the ability wheel must not show numeric rank badges. Higher ability ranks replace lower ranks, so the wheel should show the current usable ability without carrying old rank-number clutter forward.

Status-effect icons may show numeric rank badges when a status-effect family has more than one level. If a status-effect line exists only at level 1, the icon must not display a `1`. If the same status-effect family has more than one level, every ranked member of that family, including rank 1, must show its numeric rank in the bottom-right corner.

NWN presents these icons at 32x32, so any displayed status-effect number must be readable at that final in-game size.

Required treatment:

- Never display a rank number on ability, feat, or spell icons used by the ability wheel.
- Use `1`, `2`, `3`, and higher numeric ranks as needed for ranked status effects.
- Do not display a number for single-level rank-1 status effects.
- Place the number in the bottom-right corner.
- Put the number on a dark, high-contrast badge that is fully inside the frame.
- Size the badge for 32x32 readability; it should be large and simple enough that a final 32x32 icon still shows the number clearly.
- The numeric glyph must be fully contained inside the badge and must not spill past the badge edges.
- Use a warm light yellow/gold numeric glyph on the dark badge for stronger contrast at 32x32.
- Generate the icon artwork without any rank number, then stamp the numeric badge during final import at the actual 32x32 output size.
- Do not rely on filename, tooltip, color intensity, or small pips as the only rank signal.

Rank families should share base art so players recognize the ability or effect line. Higher ability ranks may add subtle escalation such as a brighter glow, stronger contrast, or extra sparks, but they must remain number-free. Ranked status effects use the numeric badge as the required rank marker.

## Framing

Icon artwork must stay inside the icon frame. Main symbols, glows, arcs, projectiles, waves, shadows, badges, and highlights must not extend beyond or visually overlap the outer border. If an effect needs motion or area-of-effect language, show it with contained shapes inside the frame rather than drawing off-frame.

Final exported icons must not include contact-sheet gutters, stray outer margins, or extra black bands from source-sheet layout. Crop and square-pad each source tile to the actual icon frame before resizing to 32x32.

Production gameplay TGA files must use bottom-left TGA origin (`image descriptor` bit 5 clear, descriptor `8` for 32-bit alpha TGAs). NUI rendering honors TGA origin metadata, while the classic NWN hotbar and feat icon paths render these gameplay resources upright when they match the legacy bottom-left icon layout. ImageMagick previews can make this look inverted unless the export flips the pixel rows while writing the bottom-left origin metadata.

Production gameplay TGA files must be fully opaque. Do not leave transparent or partially transparent edge pixels in feat, spell, ability, or status-effect icons.

## Artwork Quality

Gameplay icons must be polished, readable, and intentionally illustrated. Do not use primitive placeholder geometry such as plain rectangles, single-line weapons, generic blobs, or flat symbols that only vaguely suggest the ability.

The central image must clearly represent the gameplay concept at the final 32x32 in-game icon size. A blaster icon should have a recognizable body, grip, barrel, muzzle, and highlight details. A food icon should clearly read as a specific food item, such as a drumstick, not an abstract oval or disconnected parts. A control icon should communicate the actual control effect, such as flash blindness, sonic disruption, fear, restraint, or accuracy disruption.

The target visual style is a polished illustrated game icon, not a flat UI pictogram. Maintain the approved background, frame, semantic color, and status-effect rank badge treatment, but render the central subject with the level of finish seen in high-quality fantasy/RPG ability icons: layered forms, painterly highlights, shaded edges, faceted or glowing interiors where appropriate, and a few contained supporting accents such as sparks, shards, particles, or arcs.

Required treatment:

- Use recognizable silhouettes with enough shape detail to identify the subject.
- Add internal detail such as highlights, shadows, vents, handles, edges, cracks, sparks, waves, stitching, fangs, or other domain-appropriate cues.
- Use layered lighting and shading so the subject feels illustrated rather than cut from flat primitive shapes.
- Supporting accents are allowed when they reinforce the subject, but they must stay secondary to the main icon and remain inside the frame.
- Keep detail readable at 32x32; do not add noise or tiny marks that become stray pixels.
- Any visible human, humanoid, creature, or beast appendage must be anatomically correct. Hands must not have extra, missing, fused, or malformed fingers; feet, claws, wings, tails, and similar appendages must also be coherent and intentional.
- Prefer armored plates, closed gauntlets, silhouettes, paws, claws, or symbolic emblems when exposed fingers or other small appendages are not required for the gameplay concept.
- Prefer a simpler well-drawn subject over a complex but muddy one.
- Reject generated or hand-authored icons that look like placeholders, rough blocking, or debug art.

## Generation Approach

The polished central subject is produced by the acting agent's native image pipeline. The frame, background, semantic color, and status-effect rank badge are always stamped by the project icon tools regardless of pipeline, so only the source of the central subject differs:

- **Codex / GPT-driven requests**: generate the central subject with GPT Image 2 through Codex image generation. Do not require a separate OpenAI API account, `OPENAI_API_KEY`, or the local API/CLI fallback for ordinary icon production. Do not silently substitute a different raster image model for GPT Image 2.
- **Claude-driven requests**: author the central subject as polished, fully illustrated SVG vector art, then rasterize it (via ImageMagick) into the source subject the icon tools composite. The SVG must meet the Artwork Quality bar in this document — layered forms, gradients, shading, highlights, and a recognizable illustrated silhouette — not flat pictograms or primitive geometry.

Both pipelines must satisfy identical Semantic Color, Uniqueness, Framing, Artwork Quality, and TGA-format requirements; only the source of the central subject differs. The prohibition on primitive/vector stand-ins targets crude placeholder geometry (plain rectangles, single-line weapons, generic blobs, flat symbols); it does not forbid a fully illustrated SVG icon that meets the Artwork Quality bar.

The standard pipeline is:

- Generate the central subject with the acting agent's sanctioned pipeline above (GPT Image 2 for Codex, illustrated SVG for Claude).
- Prompt for a polished fantasy/RPG ability-icon illustration with clear 32x32 readability, no text, no watermark, no generated numbers, and no generated rank badge.
- Composite the generated subject into the approved SWLOR background, frame, semantic color, and conditional status-effect rank-badge treatment.
- Keep the semantic frame color, background, border, and status-effect numeric rank badge controlled by the project icon tools so every icon remains consistent.
- Stamp semantic frame color after resizing to the final 32x32 icon size. Do not trust image generation or source-image downscaling to preserve the approved category color.
- Stamp rank badges after resizing to the final 32x32 icon size only for status-effect families with multiple levels. Do not trust image generation or source-image downscaling to preserve readable numeric text.
- Do not stamp ability, feat, or spell rank badges.
- Export production TGA files at 32x32. Source generation may happen at a larger size, but acceptance is based on the final 32x32 TGA.
- Export production TGA files with bottom-left origin. When using ImageMagick, add a final `-flip -orient BottomLeft` so the visible icon remains upright in NWN's classic gameplay icon paths.
- Review generated source sheets and final enlarged 32x32 previews for malformed anatomy before importing to production. Regenerate or edit any icon with incorrect fingers, claws, limbs, wings, tails, or other appendages.
- Review samples before bulk-regenerating production icon files.

Do not silently swap pipelines: a Codex/GPT request must not fall back to a non-GPT-Image-2 raster model, and a Claude request must not fall back to GPT Image 2 or to primitive placeholder geometry. If the acting agent's sanctioned pipeline cannot run, stop and resolve that before producing final icon artwork.

## Source Of Truth

Every gameplay icon must have an explicit semantic category. Generators may suggest a category for first-time migration work, but the checked-in manifest is the source of truth after generation.

The gameplay icon manifest is:

`SWLOR.Game.Server/Readmes/GameplayIconManifest.csv`

Required fields:

- `Type`: `Ability`, `Feat`, `Spell`, or `StatusEffect`.
- `Key`: stable identifier, such as a feat label or status-effect class name.
- `DisplayName`: player-facing name when available.
- `SemanticCategory`: one of the approved semantic categories.
- `Rank`: numeric level when the gameplay entry has an explicit level; otherwise blank. A `Rank` value does not automatically mean a badge is displayed.
- `IconResRef`: icon resource reference without extension.
- `SourcePath`: file or data source that owns the icon.

Generated status-effect labels in `effecticons.2da` must use compact PascalCase without underscores, such as `AilmentResistance3`. Icon file resrefs should be short, meaningful abbreviations that stay within NWN's 16-character resource limit. Do not append opaque hash, collision, or generator suffixes such as random-looking letters or digits after the meaningful abbreviation.

## Enforcement

Icon tools and audits must fail when a gameplay icon violates these standards:

- Missing semantic category.
- Unknown semantic category.
- Final icon missing the deterministic semantic color frame.
- Missing icon resource.
- Duplicate gameplay icon resref.
- Duplicate generated icon pixels for two different gameplay meanings.
- Duplicate status-effect icon enum.
- Generated `effecticons.2da` label with underscores or non-identifier characters.
- Primitive, placeholder-quality, debug, or otherwise Artwork-Quality-failing central art, regardless of which pipeline produced it.

The pipeline requirement itself (GPT Image 2 for Codex requests, polished illustrated SVG for Claude requests) is enforced at authoring and code review, not by the automated icon audit: source-model provenance is not recoverable from a final flattened TGA, so the audit validates the observable properties above (semantic frame color, resource presence, uniqueness, TGA origin/opacity, rank-badge rules, and artwork quality) rather than the generation tool.
- Icon artwork extending outside the frame or overlapping the outer border.
- Final TGA using top-left origin, which makes classic NWN gameplay icon paths display the icon upside down.
- Final TGA with transparent or partially transparent pixels.
- Primitive, placeholder-quality, or unclear central artwork.
- Incorrect anatomy, including extra or missing fingers, malformed hands, incoherent claws, or broken creature/humanoid appendages.
- Ability icon with a numeric rank badge or a painted-over badge patch.
- Status-effect multi-rank icon without a numeric badge readable in the final 32x32 TGA.
- Status-effect single-level rank-1 icon with an unnecessary numeric badge.
- Generated cooldown icon name longer than NWN's 16-character resource limit.
- Recast group or resource generators silently truncating player-facing labels or icon names.
- Ability, feat, spell, or status-effect icon resrefs with opaque generator suffixes instead of meaningful abbreviations.

After adding or changing an ability icon referenced by `SWLOR_Haks/sw_2da/feat.2da` or `SWLOR_Haks/sw_2da/spells.2da`, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/GenerateCooldownIcons.ps1 -Force
```

After changing or importing custom feat/spell icon artwork, stamp and audit the semantic frame:

```powershell
powershell -ExecutionPolicy Bypass -File tools/UpdateFeatSpellIconBorders.ps1 -Apply
```

After changing generated combat-upgrade ability or status-effect icons, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/UpdateGameplayIconStandards.ps1 -RefreshManifest -GenerateIcons -UpdateStatusEffectCode
```

Do not remove ability rank badges by painting over existing TGAs. Regenerate clean source icons instead, then regenerate cooldown variants:

```powershell
powershell -ExecutionPolicy Bypass -File tools/RestoreAbilityIconArtwork.ps1
powershell -ExecutionPolicy Bypass -File tools/GenerateCooldownIcons.ps1 -Force
```

If any generated TGAs were written with top-left origin, normalize them before building haks:

```powershell
powershell -ExecutionPolicy Bypass -File tools/NormalizeGameplayTgaOrigin.ps1
```

Use audit mode to verify checked-in data without regenerating art:

```powershell
powershell -ExecutionPolicy Bypass -File tools/UpdateGameplayIconStandards.ps1 -AuditOnly
```
