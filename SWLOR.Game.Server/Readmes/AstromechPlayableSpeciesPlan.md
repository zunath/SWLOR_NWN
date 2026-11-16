# Astromech Playable Species Plan

**Status: design direction approved by the owner on 2026-11-15 (see [Decisions](#decisions)). Phase 0 (go verdict) and Phase 1 (first chassis, animation, tools, dyes, voice set) are done; see their results sections. No species code exists yet.** No balance numbers are proposed here: they are set in the Design Bible in Phase 3 and approved there.

**Base: this builds on the space revamp** (`feature/space-revamp`, read at `0e2bc7231`, and its `Readmes/SpaceRevampPlan.md`). It assumes crew stations, droid crew, power routing and the space jobs of Engineering, Espionage and Leadership exist. It ships as its own release after space, so it never delays the space release.

Sections: Summary · What exists today · Species rules · Skills and perks · Equipment · 3D models (Meshy) · Animation · 2D art · Sound · Code work · Phases · Phase 0 results · Phase 1 results · Risks · Separate droid healing · Decisions

## Summary

Add the astromech as a second droid species beside the existing humanoid Droid. It is the first player character that is not a part-based humanoid body, so it gets its own rule set instead of exceptions scattered through the humanoid one:

- **Body:** a single creature model per chassis (no body parts, no clothing, no held humanoid weapons), with paint instead of dyes.
- **Role:** support and control first on the ground, with modest damage; a ship's droid in space.
- **Kit:** three astromech-only skills (Chassis, Armaments, Utility Systems) replace Armor, the weapon skills and the humanoid support skills. A short list of body-neutral skills stays shared.
- **Gear:** astromech-only modules in the normal equipment slots, crafted through Engineering.
- **Art:** chassis models from Meshy, animation authored in the project (Meshy cannot rig a non-humanoid), and 2D art that avoids the image generator almost entirely by rendering from the 3D models and drawing symbols as SVG.

The work starts with an engine spike, because three assumptions the whole plan rests on are unproven in this codebase (see Phase 0).

## What exists today

Checked in the working tree on 2026-11-15.

| Area | Finding |
|---|---|
| Droid species | `RacialType.Droid = 167` is already playable, but it is a **humanoid part-based** body (`(Dynamic) Droid`, appearance 10098, `p?z` models). Race ids end at 169 (Ewok); 170 is free. |
| Species gating | Items: `Item.GetRacialEquipError` and `DroidBaseItemTypes` already gate gear with the `UseLimitationRacialType` item property. Skills and perks: gated only by `CharacterType` (Standard or ForceSensitive). **Nothing gates a skill or perk by species.** |
| Droid special cases | Speaks Droidspeak, cannot be Force-sensitive (`cls_pres_force.2da`), hit by Ion Grenade, immune to Mind Trick, starter outfit `dlarproto`. No repair-versus-medical split: First Aid heals droids like anyone else. |
| Creature appearance on a PC | Only the space system does it (`Space.cs:804`, restored at `:992-1001`). No PC lives in a creature appearance on the ground. |
| Astromech models | `appearance.2da` rows 2175-2184 (`C_DrdAstro01-10`) and 2185 (`L_Astromech`). The `C_DrdAstro` models name a supermodel (`P_T3M3`) that is not in the haks and carry no animations of their own, so they almost certainly do not animate. Rows 2175-2184 are used by utility droid NPC blueprints. |
| Companion droids | `Service/Droid.cs` and `Service/DroidService/` (player-built pets with CPU, parts, AI slots). Separate system; untouched by this plan. |
| Space (master) | No crew or co-pilot roles in code. `ShipDetail.HasDroidBay` exists but is never set or read. |
| Space (revamp branch) | Piloting stays the only space skill, by an owner decision backed by production data. Large hulls seat a Pilot, Gunner and Engineer; Capital hulls add a second Gunner and Sensors; **Small and Medium hulls are solo**. Seats check Piloting (pilot, gunner), Engineering (Engineer) and Espionage (Sensors); Leadership raises crew effectiveness. Player-built droids can be posted to a seat at 25% to 75% effect. Mining Barge, Industrial and Logistics hulls get a droid bay. Only Piloting abilities can be used at a station (`SpaceCrewAbilityRules`). Rule: a ship crewed by N players must not beat N separate ships. |
| Meshy | Creature pipeline exists: `tools/MeshyCreatureExport.py`, documented in `Readmes/BlasterModelImport.md:586-604`. It skins the generated mesh onto a **stock reference creature skeleton** and inherits that creature's animations. Meshy's own auto-rig and motion tools are humanoid-only. |
| Animation tooling | `SWLOR.Toolset.Domain/Animation` (`AnimationProject`, `AnimationMdl`, `AnimationInstall`) is generic over named joints. The authoring front-ends (`MotionAuthor` and friends) are humanoid-only. |
| Tinting | Tint maps cover part-based bodies, helmets, cloaks and robes. `SetMaterialShaderUniformVec4` is per object and per material, so a creature material could in principle use the tint shaders, but no creature model is tinted today. |
| 2D art | `Readmes/IconStandards.md`: subjects come from GPT Image 2 (Codex) or SVG rasterized with ImageMagick (Claude). Inventory icons for generated weapons are already **rendered from the 3D model**. No portrait renderer exists. `tools/LocalImageGen/` (Z-Image-Turbo) is an evaluated local alternative. |
| Lore | The Legends article: an automated mechanic and nav-computer stand-in, limited stored hyperspace coordinates, in-flight repair, slicing with special programming, communicates in binary or in writing through another computer. "Astromech" was informal spacer slang for utility droids as far back as the Jedi Civil War; the R-series came millennia later. |

## Species rules

One rule drives the rest: **an astromech never uses a system that needs a humanoid body.** Each such system gets an explicit astromech answer rather than silently misbehaving.

| System | Humanoid species | Astromech |
|---|---|---|
| Body | Part-based, normal phenotype | One creature model per chassis, picked at creation and changeable later in a workshop window |
| Look | Heads, parts, skin, hair, dyes | Chassis choice plus paint (primary, secondary, trim); small scale range |
| Clothing and armor | Part appearances, outfits, robes | None. Worn gear is internal modules and never changes the model |
| Weapons | Held in hands | Built-in tools on the chassis; no lightsabers, blasters, or melee weapons |
| Force | By class | Never (same rule as Droid) |
| Speech | Any known language | Works exactly like Wookiees and Shyriiwook: always speaks Droidspeak, cannot switch language, understands the languages it knows. Others need Droidspeak to read it. Emotes are unrestricted |
| Emotes, sit, rest | Humanoid animation list | Its own short list (dome turn, rock, chirp, arm extend, power down). Chairs are not usable; rest is a power-down pose |
| Healing | First Aid, medical items | Unchanged for launch, matching the existing Droid species. What a split would change is explored under [Separate droid healing](#separate-droid-healing) |
| Ion and mind effects | Normal | Same as Droid (takes Ion Grenade, immune to Mind Trick), through one shared "is a droid species" check |
| Food, stims | Consumables | Follow whatever the existing Droid species does today; no new rule |
| Disguise, descriptor, portrait | Species lists | Astromech added to the descriptor allowlist; own portrait set |
| Space | Appearance swapped to the ship | Same, and the chosen chassis must be what is restored on exit |
| Existing characters | - | No conversion. Astromech is for new characters only |

**Look and era.** Original Old Republic "utility droid" designs (squat three-legged bodies, flat or domed heads, in the spirit of the era's utility droids) rather than copies of the film R-series. It fits the setting, the lore supports the name, and it keeps every generator prompt free of franchise terms.

**Launch chassis.** Four chassis initially, with clearly different silhouettes (for example domed, flat-top, tall cone-top, heavy wide-body), all on one shared skeleton so they share every animation.

**Ship role.** With the space revamp underneath, the astromech's signature job is in scope and mostly already built:

- **Crew seats, no new code path.** An astromech is a player, so it takes the Gunner, Engineer or Sensors seat on Large and Capital hulls at full player effect, on its own Piloting, Engineering or Espionage rank. Player-built droids stay capped at 25% to 75%, so a player astromech is the best "droid crew" there is without any special bonus.
- **Astromech socket (approved; amends the space plan).** Small and Medium hulls are solo in the space plan. Selected Small and Medium hulls get one socket that only an astromech player can take. It works a cut-down Engineer station: power routing and the hull's repair and capacitor modules, on the astromech's Engineering rank. This is the fighter co-pilot players ask for. It amends a final space decision, so the space plan is updated with it, and it is bounded by the existing rule: a pilot plus an astromech must not beat two separate ships (checked in the space simulator).
- **No new space skill and no astromech stat bonus in space.** Both would break the space plan's "Piloting is the only space skill" decision. The astromech's edge is access (the socket), not numbers.

## Skills and perks

### Ground role

Support and control first. On the ground an astromech repairs and protects allies, and stuns, slows, marks and disrupts enemies. Its own damage is modest and sits below the weapon skills by design. Two consequences to settle in the Bible phase:

- **Solo target:** it must still clear normal content of its tier alone, more slowly than a weapon user and leaning on control and self-repair. Elites are party content. A support species that cannot level alone would not be played.
- **XP:** each new skill takes a combat point category (`Exempt` like Armor, `Utility`, or `Weapon`), so it levels from fights the astromech helps with. Exactly how repair and control actions earn points on the ground is not verified here; it is checked against `CombatPoint` before the Bible numbers are written, so a supporting astromech levels without landing kills.

### Which skills an astromech has

Approved split:

- **Astromech-only (new):** Chassis, Armaments, Utility Systems.
- **Shared, kept:** Piloting, Engineering, Smithery, Fabrication, Gathering, Agriculture, Espionage, Devices, Leadership, and the language skills (comprehension only, given the speech rule).
- **Locked out:** Force, Lightsaber, Saberstaff, Armor, every weapon skill, Unarmed, Mimicry, Beast Mastery, First Aid, Performance.

The 400-point skill cap and starting points stay the same as for everyone.

### How the 400 cap works

Every capped skill tops out at rank 50, so 400 is **eight skills**. The astromech does not have three skills; it has twelve that count toward the cap, 600 ranks competing for 400:

| Group | Skills | Ranks |
|---|---|---|
| Astromech-only | Chassis, Armaments, Utility Systems | 150 |
| Ship jobs (space revamp) | Piloting (pilot, gunner), Engineering (Engineer seat, socket), Espionage (Sensors seat, slicing), Leadership (crew effect, Command) | 200 |
| Other shared | Devices, Gathering, Smithery, Fabrication, Agriculture | 250 |

So an astromech picks eight of twelve, and can skip an astromech-only skill (a ship's droid that never fights on the ground has no use for Armaments). Example builds at the cap:

| Build | Eight skills |
|---|---|
| Ship's droid | Chassis, Utility Systems, Engineering, Espionage, Piloting, Leadership, Gathering, Devices |
| Ground fighter | Chassis, Armaments, Utility Systems, Devices, Leadership, Engineering, Espionage, Piloting |
| Mechanic | Chassis, Utility Systems, Engineering, Smithery, Fabrication, Gathering, Agriculture, Piloting |

This matches how players already spend: the space plan's production data shows active pilots putting a median 321 of 400 ranks outside space.

What the split does and does not give:

- **It pushes astromechs toward ship and tech roles.** At most 150 ranks are astromech-only, so at least 250 land in shared skills. That is the intended identity, and it feeds the crew seats the space revamp needs filled.
- **Ground combat is shallower than a humanoid's, on purpose.** A ground-focused astromech has five ground skills (the three, plus Devices and Leadership) against a humanoid's six or more (two weapons, Armor, Force, First Aid, Devices, Leadership).
- **Every astromech shares the same two combat skills.** Build variety on the ground comes from perk SP, not skill choice, exactly as it limits humanoids: 410 career SP against far more perks than that buys (Piloting's four styles alone cost 240).
- **One trade, approved:** Armaments covers both a melee and a ranged tool in one skill, where a humanoid pays 100 ranks for a melee and a ranged weapon. The price is depth: each tool gets one 60 SP line, where a humanoid weapon gets two.

Levers if this proves too thin, in order of cost:

1. Split Armaments into two weapon skills (Shock Prod, Blaster Mount), each with the standard two lines. Astromech-only ranks go from 150 to 200 and the trade above disappears, at the cost of roughly doubling the weapon perk content.
2. Open First Aid as a shared skill.

*Not recommended:* a different cap for astromechs. The cap, decay at the cap and the SP total are global in `Service/Skill.cs`, and a species with its own cap would be a new balance tier.

### The three new skills

Each follows the standards in `Readmes/SkillPerkStandards.md`: start from the Bible tab of a named comparator, keep that family's structure, and bring CombatSim evidence at entry, middle and endgame ranks.

| Skill | Category | Replaces | Structure comparator | Content |
|---|---|---|---|---|
| **Armaments** | Combat | A weapon skill | The weapon schedule exactly: two independent lines, 18 purchases and 60 SP each, at the standard ranks | Line 1 **Shock Prod**: melee control (stun, slow, interrupt). Line 2 **Blaster Mount**: ranged pressure and debuffs (marking, ion, suppression). Damage is modest on both. The equipped primary tool decides which line is live, as a weapon does |
| **Chassis** | Combat | Armor | The Armor family's own structure and SP budget | Plating, deflector, self-repair, mobility (booster burst) |
| **Utility Systems** | Utility | First Aid and the humanoid support kits | A support family (Devices or First Aid; picked in the Bible phase) | **Repair** (mend droids, companions and allies' gear effects, self-repair), **Countermeasures** (extinguisher cleanse, smokescreen, oil slick), **Interface** (scomp link bonus to slicing, scanner, holoprojector) |

Notes:

- Balance target is **support first, not a new tier**: Armaments damage below the band of existing weapon skills, with its value in control; Chassis survivability inside the band of existing Armor builds; Utility Systems support comparable to existing support skills; total SP spend comparable to a humanoid build. How far below the weapon band is a number to draft in the Bible phase, with CombatSim evidence.
- Abilities use STM, like weapon and Devices abilities.
- Slicing already uses Lockpicking and Perception and awards Espionage XP. The Interface line adds to that system; it does not create a second slicing path. The same line is what the Sensors seat and derelict boarding use in space, since a crew member slices with their own slicing perks.
- Utility Systems stays a ground skill. Its abilities are not usable at a crew station, in line with the space plan's rule that only Piloting abilities are.
- Every ability follows `Readmes/AbilityTargeting.md` (builder metadata, one `spells.2da` row and Spell value per rank of an area ability).

### How gating works

Add one concept, a **body frame** (`Humanoid` or `Astromech`), derived from race in one place (`Service/Race.cs`). Do not reuse `CharacterType`: it has 97 `CharacterType.Standard` references and Force rebuild semantics, and an astromech is simply a Standard character.

- `SkillType` attribute gains a frame restriction read by the same five callers that read `CharacterTypeRestriction` today (skills window, RP XP distribution, full rebuild, DM examine).
- New `PerkRequirementBodyFrame`, mirroring `PerkRequirementCharacterType`.
- Skill XP awards and ability use check the frame too, so a locked skill can never gain rank through a side door.

## Equipment

Astromech gear is **modules**. They sit in the normal engine slots so the existing gear-fed stat rebuild (`Stat.RefreshPlayerEquipmentStats`, `EquipmentRecordStats`) works unchanged, and they never alter the model.

| Engine slot | Astromech module |
|---|---|
| Right hand | Primary tool (shock prod or blaster mount) |
| Left hand | Secondary tool (emitter, extinguisher, scanner) |
| Head | Sensor package |
| Chest | Chassis plating |
| Arms | Manipulator servo |
| Boots | Drive unit |
| Belt | Power cell |
| Neck | Logic module |
| Rings (2) | Expansion sockets |
| Cloak | Deflector emitter |

- **Gating:** reuse the existing racial item gate. Modules carry `UseLimitationRacialType: Astromech`; astromechs can equip nothing in those slots without it, and nobody else can equip modules. This extends `Item.GetRacialEquipError` and `DroidBaseItemTypes`; it is not a new mechanism.
- **Base items:** worn modules reuse existing base item types. The two tool families probably need two new `baseitems.2da` rows (a melee tool and a ranged tool); Phase 0 decides.
- **Stat budget:** each module tier mirrors the humanoid item in the same slot and tier in the Bible. No new tier.
- **Source:** Engineering recipes in a new "Astromech Modules" category beside the existing droid part categories, built from the space revamp's component layer, plus a starter kit from `PlayerInitialization` in place of clothes. This must be reconciled with the crafting revamp branch, which is reworking Engineering.
- **Humanoid loot:** an astromech can still carry, trade and sell it. Quest rewards and tutorial steps that hand out or require humanoid gear need an audit (Phase 4).

## 3D models (Meshy)

Meshy is opt-in and paid. The recipe and ceiling below were approved on 2026-11-15.

**Balance on 2026-11-15:** 8,967 credits (3,967 monthly, resetting 2026-11-26; 5,000 permanent).

**Per model, following the established creature recipe** (`BlasterModelImport.md:586-604`):

1. One concept image, `nano-banana-pro`, 9 credits. Neutral rest pose, legs planted, arms and panels closed.
2. Image to 3D, `meshy-t2.5` smart topology, about 8,000 triangles, `meshy-7` texture with PBR, 15 credits. Smart topology returns part-separated meshes, which is what a rigid machine needs.
3. `tools/MeshyCreatureExport.py` to MDL, MTR and DDS, then the review renders.

**Budget.** 24 credits per attempt. Four chassis at three attempts each is 288. If tools render on the chassis (Phase 0 decides), six tool heads at two attempts each is another 288. Approved ceiling: **1,000 credits** for the whole feature, with a stop-and-report if it is reached.

**What Meshy will not do.** `meshy_rig` and `meshy_text_to_motion` accept humanoid bipeds only. No astromech animation comes from Meshy.

**Changes the exporter needs.** Today it binds a mesh to a stock creature skeleton with soft skin weights. For the astromech it needs an astromech reference skeleton (below) and a rigid mode (each vertex follows exactly one node), so panels and legs do not bend like flesh.

**Paint.** Two routes, chosen by the Phase 0 tint test:

- *Preferred:* tint zones. Concept images use flat marker colors for the paint zones; a script keys those zones out of the generated base color into a tint map; the chassis material uses the existing tint shaders with per-player colors. This must follow `Readmes/TintMapRendering.md` (dyed metal binds no specular or roughness map; `CheckDyeAppearance.py` extended to cover the chassis).
- *Fallback:* a fixed set of paint schemes per chassis, produced by a recolor script from the one generated texture (no extra credits), each as its own appearance row.

## Animation

All four chassis share one skeleton and one animation set, built once.

- **Reference skeleton:** a hand-defined node hierarchy (body, head ring, head, left and right leg with ankle, retractable center leg, two tool-arm sockets, panel hinges, and the engine's standard attachment nodes). Checked in as the astromech supermodel.
- **Clips:** authored through the generic `AnimationProject` and `AnimationMdl` pipeline with a new, small non-humanoid front-end. Rigid mechanical motion (roll, lean, head turn, leg drop, arm extend) is far simpler to author than humanoid motion.
- **Clip list:** the full set the engine asks of a player-controlled creature (idles, walk, run, combat ready, melee and ranged attacks, hit reactions, dodge, knockdown and get-up, death, ability activation, plus the emote list). The exact names are enumerated from a stock creature supermodel in Phase 0 rather than guessed here.
- **Iteration:** the 90-second development-override loop already used for authored clips.

## 2D art

The generator refuses franchise content, so the plan needs it as little as possible. Three routes, in order of preference:

1. **Render from the 3D model (no generator at all).**
   - *Portraits:* a new small Blender render script produces the five portrait sizes per chassis and paint scheme, registered in `portraits.2da` under the new race id. This is the one new tool in the art pipeline.
   - *Module and tool inventory icons:* rendered from their own models on a transparent canvas, the same way generated blaster icons already are (`TrellisWeaponExport.py` `render_view` and `pad_icon`, written with `FixWeaponInventoryIcons.py`).
2. **SVG symbols rasterized with ImageMagick** for every perk, ability and status icon. This is already the documented Claude icon pipeline. The subjects are simple symbols (bolt, weld arc, sensor eye, socket, shield arc), which suits it, and the project tools stamp frame, color and badge as usual.
3. **Painted art only where 1 and 2 cannot work** (for example a splash or wiki image): Meshy's `nano-banana` image models, already used for concepts, or the local Z-Image-Turbo setup. Not GPT.

**Prompt rule for every generator call, 3D or 2D:** describe the shape, never the name. "Squat cylindrical three-legged utility robot with a rotating domed head", never "astromech", "R2", a model number, or the franchise. This is also why original Old Republic designs are recommended.

`IconStandards.md` says pipelines must not be swapped silently. Routes 1 to 3 are approved for astromech art; the first art pull request records them in `IconStandards.md`.

Every icon is checked upright in the client; modules without a visible model still need unique icon art per appearance.

## Sound

An astromech needs a voice set (acknowledge, attack, hurt, death, emote chirps). Film sounds cannot be used. A small script synthesizes original chirps, whistles and warbles (swept and frequency-modulated tones) into a new soundset. Verified with the recorded-audio client recipe.

## Code work

Grouped by owner file. "Guard" means: check the body frame and take the astromech path instead of the humanoid one.

**Data (hak and enums)**
- `racialtypes.2da` row 170 and `RacialType.Astromech`; `cls_pres_stand.2da` gains a row for it and `cls_pres_force.2da` stays without one; `appearance.2da` rows for the chassis; `AppearanceType` entries; `portraits.2da` rows; soundset; `iprp_skill.2da` and `iprp_resperk.2da` rows for the new skills and perks; `baseitems.2da` tool rows if needed; TLK strings.
- Skill and perk ids are reserved against master and the hak first (`SkillPerkStandards.md:90-92`).

**Species plumbing**
- `Service/Race.cs`: body frame, astromech appearance record, restore of the chosen chassis.
- `Feature/PlayerInitialization.cs`: languages, starter modules, no clothes.
- `RacialAppearanceRegistry` and a new astromech definition (chassis list, scale range, no parts).
- `Entity/Player.cs`: chosen chassis and paint. Additive fields with defaults, so no data migration is expected; confirm against `Readmes/Migrations.md` and `MigrationDataTests` when the release it ships in is known.
- `Service/Space.cs:992-1001`: restore the chosen chassis, not the racial default.

**Space (on the revamp branch)**
- Crew stations: the copy of the character left at the console needs an astromech pose (plugged in, not seated).
- Astromech socket: a new seat in `ShipCrewSeat` and `ShipCrewSeats`, socket tags on the chosen Small and Medium hulls, a boarding flow for hulls with no interior console, body-frame check on taking the seat, `SpaceCrewStationTests` cases, and a pilot-plus-astromech run in the space simulator.

**Guards on humanoid systems**
- Appearance editor (`AppearanceEditorViewModel.cs:975`), customize window (`CustomizeCharacterViewModel`), head scale command (`CharacterChatCommand.cs:452,482`): replaced by a new **Chassis Workshop** NUI window (chassis, paint, scale), built to `NuiLayoutRules.md` and `GuiWindowAuthoring.md`.
- Armor display, helmet and cloak renderers, tint map resolver, robe, neck, horn, power glove and weapon hand fit: all skip the astromech.
- Emotes (`EmotesViewModel`, `EmoteChatCommand`), rest (`RestStatusEffect`), chairs and shuttle seats: astromech list and power-down pose.
- Speech: add the astromech beside the Wookiee rule in `Service/Communication.cs:309-314` and `CharacterChatCommand.cs:285-290`.
- `Service/PlayerDescriptor.cs:28`, disguise window, HoloNet portrait: accept the new species.
- Ion Grenade and Mind Trick: move the Droid checks to one shared droid-species check.
- `Service/Item.cs` (`CanEquip`, `GetRacialEquipError`, `CanEquipForceSensitiveWeapon`) and `Feature/EquipmentRestrictions.cs`: module rules.

**Skills, perks, abilities, items**
- `SkillType` entries and the frame restriction; `PerkType` entries, three `PerkDefinition` files, `PerkRequirementBodyFrame`; one `AbilityDefinition` file per ability; module item blueprints; Engineering recipes.
- Design Bible first: new perk tabs for the three skills, a new `Equipment - Astromech` tab, `Engineering Recipes`, `Status Effects`, `System Changes & Migrations`, and the Player Guide `Stat Caps` topic if any cap is touched. Workbook edits follow `DesignBibleWorkbookRules.md`.

**Standards and docs**
- `ContentStandards.md` Characters: the astromech is the one approved non-part-based player body.
- `IconStandards.md`, `BlasterModelImport.md` (astromech export mode), a Player Guide topic, release notes.

**Tests**
- Unit: frame gating of skills, perks, XP and abilities; module equip rules both directions; language rule; `PerkRankProgressionTests`, `EquipmentTypeTests`, `EquipmentRecordReconciliation`, `AreaAbilityTargetingTests` for the new content.
- Engine tests for creation, equip, space swap and restore.
- CombatSim evidence for Armaments and Chassis.
- Client evidence for every model, animation, icon, window and the creation flow.

## Phase 0 results

Run on 2026-11-15 in the real client through `tools/NwnLiveClient.py` (GPU renderer), against master's deployed module and haks with a scratch copy of the 2DA hak. Master was used because no space-revamp deploy exists locally and none of these questions depend on it. Nothing from the spike is committed. Evidence, the generated model, the converter and the test driver are kept in `C:\Temp\astro-spike` (138 MB).

**Verdict: go.** Both go or no-go points passed.

| Question | Result | Evidence (`C:\Temp\astro-spike`) |
|---|---|---|
| Can Meshy make a usable chassis? | Yes, first attempt. Concept 9 credits, model 15 credits, 7,620 triangles, one material. 24 of the 1,000 credit ceiling used. | `meshy/concept1.png`, `inspect1/sheet.png` |
| Can it be rigged and animated without Meshy? | Yes. The mesh is one object in hundreds of loose shells, so parts are assigned by region, not by shell: head, body, left leg, right leg, each a rigid mesh on its own node. 33 clips authored as node keys. | `scripts/export_astro.py`, `model1/` |
| Do the clips play in game? | Yes: idle, second idle, head turns, taunt, victory, both dodges, spasm, conjure, knockdown, travel lean and attack wind-up. | `run3/evidence/sheet_reel.png`, `reel.mp4`; `run1/evidence/walk1.mp4`, `sheet_fight2.png` |
| Native character creation for a non-part-based race | Works. The species appears in the list, Force Sensitive is locked, the customize step is already only a notice, and the character enters as the astromech with normal first-login setup and no server errors. | `run2/evidence/cg*.png`, `run3/evidence/h*.png`, `enter1.png` |
| A PC living in a creature appearance | Works: movement, an area change, SWLOR attack and hit rolls, death, respawn. The appearance persists through each. | `run1/evidence/` |
| Which model type | **Simple (`S`)**, with the creature clip names (`c` prefix). Held weapons are not drawn on `S` or `L`. On `F` a held weapon is drawn at the `rhand` node, but the model did not appear to play this clip set while moving. | `run1/evidence/sheet_weapons_s.png`, `sheet_weapons_LF.png`, `sheet_walkF.png` |
| Per-player colour on a creature model | Works. A material using the existing `fs_plt_tinter` shader with a generated tint map (accents, panels and dark metal as three dye layers) changed colour live from server-set dye rows. | `run3/evidence/sheet_tint.png`, `reelcam.png`, `model2/` |

**What this settles**

- **Paint:** the preferred tint route works; fixed schemes are not needed. The tint map can be derived from the generated base colour by hue and brightness, so concepts do not need special marker colours.
- **Tools:** worn and held gear is invisible on the simple type, which is exactly what internal modules need. Tools (prod, blaster, arms) should be built into the chassis model and moved by the attack clips, not drawn from equipped items. No new `baseitems.2da` rows are needed for display.
- **Animation names:** the simple-creature set. Core names seen in every sampled creature: `cpause1`, `cwalk`, `crun`, `creadyr`, `creadyl`, `ca1slashl`, `ca1slashr`, `ca1stab`, `cclosel`, `ccloseh`, `creach`, `cdamages`, `cdamagel`, `cdamager`, `ckdbck`, `ckdbckps`, `ckdbckdie`, `cdead`, `ctaunt`. Also used here: `cpause2`, the dodges and parries, `cgustandb`, `cguptokdb`, `cconjure1`, `ccastout`, `ccastoutlp`, `cspasm`, `chturnl`, `chturnr`, `cvictory`.
- **Converter:** a small Blender script did the whole job (segment, pivot, write model, clips, tint map, material). `MeshyCreatureExport.py` was not needed; Phase 1 either folds this in as a rigid mode or keeps it as its own tool.

**New requirements found**

- `cls_pres_stand.2da` needs a row for the new race, or the species cannot pick the Standard class at creation.
- `Race.cs` needs default appearance entries for the race, or first login fails on a missing dictionary key.
- The portrait step shows every portrait when the race has none of its own.
- The camera aims at humanoid head height, so close views look over the chassis. Camera height needs setting for this appearance.
- The creation screen offers humanoid voice sets; the astromech soundset has to be offered there.
- The body's dark parts share one dye layer with a default red metal row; Phase 1 picks sensible default rows per layer.

**Not verified in this spike**

- Entering and leaving space (the appearance swap and restore).
- Helmet and cloak display (the test helmet was refused by its normal Armor rank requirement).
- Abilities used from the hotbar, resting, and ranged attack animation.
- Whether the full (`F`) type would animate with player-style clip names; only needed if drawing held items is ever wanted.
- Normal and specular maps (the spike used the base colour only), compiled models, and the dye appearance checker.

## Phase 1 results

Built on `feature/astromech` (cut from the space revamp) and checked on 2026-11-15 in the real client against haks, module and server built from the phase 1 branch. Evidence is in `C:\Temp\astro-spike\runp1\evidence`.

**Delivered**

- `tools/AstromechCreatureExport.py` (with tests): chassis as rigid node meshes assigned by whole shell, the shared animation model `c_astromech` with 34 clips, two built-in tools, the dye mask, review renders, and packaging. The pipeline is described in `BlasterModelImport.md` under "Astromech chassis".
- `tools/GenerateAstromechSoundset.py` (with tests): 49 synthesized sounds and the SSF.
- Hak content: chassis `c_astro_01` (appearance row 10282, simple type), `c_astromech`, the prod and blaster textures, the dye material and `tintmap.2da` row from the project's tint generator, voice set row 954 with its name string, and the sound files.
- Meshy spend to date: 72 of the 1,000 credits (one chassis, two tools).

**Verified in the client**

| Check | Result | Evidence |
|---|---|---|
| Shared clips on a chassis with no clips of its own | Idle, second idle, head turns, taunt, victory, both dodges, spasm, conjure and knockdown all play | `sheet_p1reel3.png`, `p1reel3.mp4` |
| Built-in tools | The blaster slides out in the ability clip; the prod extends on attack swings in a real fight | `sheet_p1reel3.png`, `sheet_p1fight.png` |
| Dyes through the real system | `TintMapService.ApplyCurrentColors` resolved the creature's material from `tintmap.2da` and applied stored colors to all three layers | `sheet_dye.png` |
| Voice set | Eight voice lines played the matching generated sound in the client's own audio (match 0.83 to 0.99; 0.17 at most for a wrong sound) | `voice_test.json`, `voice_in_game.wav` |
| Dye rules | `CheckDyeAppearance.py` passes, including the dyed-metal rule | - |

**What Phase 1 changes for later phases**

- Per-player colors need no new dye code: the existing service already handles a non-part creature through `tintmap.2da`. Colors are `TM_<material>_<layer>` variables on the creature. Phase 2 only has to give a new astromech sensible defaults and a window to change them.
- A chassis shows its default dye rows until colors are written, so every astromech NPC blueprint needs dye variables or it renders in palette row 0.
- Client checks need the cursor moved off the model, or the hover highlight washes it out.

**Not done or not verified**

- The sounds are plain WAV. The project's other sounds are MP3-compressed, which needs `ffmpeg`; it is not installed on this machine, and the sound manifest has not been refreshed for the new files.
- Nobody has listened to the voice set critically; the owner has the preview file.
- The chassis was checked as the player character and as a spawned NPC standing still; the NPC was not filmed fighting.
- Death and get-up were seen as the knockdown pose only. Walk and run on the production model were not re-filmed (they were in Phase 0).
- The other three chassis, portraits, and the camera height for this appearance are later work.

## Phases

Each phase is its own pull request into a `feature/astromech` branch cut from `feature/space-revamp`; the owner decides when it merges. It releases after the space revamp, with its own migration versions if any turn out to be needed.

**Phase 0 - Engine spike. Done 2026-11-15, verdict go; see Phase 0 results.** At the owner's request the first chassis was generated and animated as part of it, so the questions were tested on the real model. The original checklist:
1. Native character creation works for a race whose appearance is not part-based, with the creation controls hidden as they are today.
2. A PC in a creature appearance can log in, move, target, fight (melee and ranged), use abilities, take hits, die, respawn, rest and enter and leave space.
3. What the engine shows for equipped items on that model, which model type suits it, whether a tool can render on an arm node, and whether tools need new base item rows.
4. A creature material takes per-player tint colors through the tint shaders.
5. The list of animation names the engine requests of that PC.

*Exit:* client evidence for each point and a written go, no-go or change list. Points 1 and 2 are go or no-go for the whole feature; 3 and 4 only choose between routes already in this plan.

**Phase 1 - Skeleton, animation, first chassis. Done 2026-11-15; see Phase 1 results.** Reference skeleton, exporter rigid mode, authored clip set, one Meshy chassis through the pipeline, soundset. *Exit:* one chassis moving, fighting and dying correctly in the client as an NPC.

**Phase 2 - Species plumbing.** Race, body frame, creation flow, starter kit, speech rule, all humanoid-system guards, Chassis Workshop, portraits. *Exit:* a new astromech character can be made and played end to end with no skills or gear of its own yet, and no humanoid system misbehaves for it.

**Phase 3 - Skills, perks, abilities, modules.** Bible tabs written and **approved by the owner before any code**, then implementation, icons, recipes and CombatSim evidence. *Exit:* parity evidence at entry, middle and endgame.

**Phase 4 - Ship role.** Crew seat pose and checks; the astromech socket, with its simulator evidence. *Exit:* an astromech crews a Large hull seat and a fighter socket in the client, and the pilot-plus-astromech run stays at or below two separate ships.

**Phase 5 - Content and polish.** Remaining chassis and paint, tutorial and quest audit for humanoid-only steps and rewards, Player Guide, release notes, a play test on the test server.

## Risks

| Risk | Effect | Answer |
|---|---|---|
| Native creation or core play breaks on a creature-appearance PC | Feature is not viable as designed | Phase 0 tests it first. Fallback to investigate there: create as the humanoid Droid body and swap to the chassis on first entry, the way space already swaps appearance |
| Creature model cannot be tinted | No free color choice | Fixed paint schemes per chassis |
| Generated mesh does not split cleanly into rigid parts | Legs or head deform | Rigid one-node weighting in the exporter; simplify the concept; regenerate within the credit ceiling |
| Missing animation names | Frozen or sliding model in some actions | Clip list is taken from the engine in Phase 0, not guessed |
| Three new skills drift from standards | Balance complaints, rework | Comparator-first design, parity target, owner approval of Bible tabs before code |
| Crafting revamp and space revamp both touch the same systems | Merge conflicts, duplicated design | Branch from the space revamp; Engineering recipes reconciled with the crafting revamp branch |
| The socket makes a fighter with an astromech the only sensible way to fly | Solo pilots feel forced to find a droid | Bounded by the N-players rule in the simulator; the socket runs only Engineer functions, never weapons |
| Ground combat feels thin with two combat skills | Astromechs feel like second-class fighters | Stated as intended; levers listed under the skill cap |
| Speech rule frustrates players | Astromechs feel muted | Same rule Wookiees already live with; emotes stay free |
| Humanoid assumptions missed | Odd bugs after launch | One body-frame helper, a guard list in this plan, and a test server play test |

## Separate droid healing

Requested exploration: what would change if the Droid species and the astromech stopped sharing healing with organics. **Owner decision, 2026-11-15: option A for now.**

**How healing reaches a droid player today**

| Source | Examples | Notes |
|---|---|---|
| First Aid | Med Kit, Treatment Kit, Kolto Mist, Infusion, Emergency Triage, Resuscitation, Antitoxin, the stims, Shielding | 13 ability files in `Feature/AbilityDefinition/FirstAid/` |
| Force | Benevolence, Renewal, Purifying Wave, Force Sanctuary, Guardian Ward | `ForceControlHealingEffects.cs` |
| Perk and status heals | Inspired, Guardian's Resolve, Blood Weapon, damage-derived healing, Warden Order | `Combat.cs`, status effect definitions |
| Passive | Natural regeneration, rest, food, respawn | `NaturalRegeneration.cs`, `RestStatusEffect.cs`, `Death.cs` |

None of these checks race. Most ability healing passes through `AbilityEffectScaling.ApplyScaledHeal` and `Stat.ApplyHealingReceivedAdjustment`, so a "target is a machine" rule has a small number of places to live. Player droids also bleed and take poison like anyone else (`Droid.IsBloodlessMachine` excludes PCs).

**Three ways to split, cheapest first**

| Option | Rule | What changes |
|---|---|---|
| A. Flavor only | Same numbers. Droid targets get repair wording and effects | Feedback text and VFX only. No balance change, no effect on live characters |
| B. Soft split | Organic healing works on droids at reduced strength; repair works on droids at full strength and not on organics | Droids stay viable in any party but want a repairer. One new number to approve (the reduction). Every healing description gains a droid clause |
| C. Hard split | Organic healing and Force healing do nothing to droids; only repair does | Lore-accurate. Droids depend entirely on repair sources. Largest change, listed below |

**What a hard split would force**

- **Live Droid characters change overnight.** Every existing Droid-species player loses First Aid and Force healing from their party. The number of live Droid characters should be read from a production backup before deciding.
- **A repair source humanoids can own.** Utility Systems is astromech-only, so the humanoid Droid species would have no way to repair itself. Repair would have to exist as a shared option too: Engineering repair kits, a Devices gadget, or repair perks in Engineering.
- **Astromech Repair becomes essential.** The Repair line stops being one support choice and becomes the droid healer role, which changes how it is budgeted.
- **Party friction.** A group whose healer is First Aid or Force cannot keep its droid alive. Droid players risk being left out of groups, the usual cost of an exclusive healing type.
- **Revive needs a twin.** Resuscitation would need a droid equivalent (a reboot), with its own item and rules.
- **Statuses need a ruling.** If medicine does not work on machines, poison, bleed and disease should not either, and Antitoxin and the stims lose their droid targets. In exchange droids are usually given ion weakness, which they already have. That is a broader machine-status rework and a new balance direction, needing approval on its own.
- **Passive recovery.** Regeneration, rest and respawn would stay as they are (self-diagnostics), or the split reaches every player's downtime.
- **Text and data.** Every healing ability's Bible row, TLK string and description gains a droid clause; new repair abilities, items, icons and recipes; tests for both directions on every heal.
- **Companion and enemy droids.** A consistent rule would also cover player-built droids and droid enemies, which widens the change to pets and NPC healers.

**Read on it.** Option A is free and can ship with the astromech. Option B is the smallest real split and keeps groups working. Option C is the most flavorful and the most disruptive, and is only worth doing as its own project covering both droid species, after production data on Droid characters is in hand.

## Decisions

**Approved by the owner on 2026-11-15**

1. **Ground role:** support and control first, with modest damage.
2. **Look and era:** original Old Republic utility-droid designs, not film R-series copies.
3. **Speech:** always Droidspeak, modelled on how Wookiees work with Shyriiwook.
4. **Astromech socket** on selected Small and Medium hulls, amending the space plan.
5. **Skill lists:** the shared and locked lists as written.
6. **Healing:** shared with organics, with option A (repair wording and effects on droid targets, same numbers) for now. Options B and C are explored above and not scheduled.
7. **Access:** open to everyone, new characters only, no conversion.
8. **Names:** free naming.
9. **Droid ownership:** restraining bolts, memory wipes and owners are roleplay only, no mechanics.
10. **Launch chassis:** four initially, on one shared skeleton.
11. **Paint:** fixed schemes per chassis are acceptable if free color choice does not work.
12. **Meshy:** the existing creature recipe and a 1,000 credit ceiling.
13. **2D art:** rendered from 3D, SVG symbols, non-GPT generators as a last resort.
14. **Skill cap:** the global 400 cap, three astromech-only skills, Armaments covering both tools at one line each.

**Assumed, not objected to**

- Gear is astromech-only modules at parity with the same-slot humanoid item, crafted through Engineering.
- A `feature/astromech` branch cut from `feature/space-revamp`, one pull request per phase, released after space.

**Still to draft for approval, in the phase named**

- Every balance number, in the Design Bible (Phase 3): how far below the weapon band Armaments sits, the solo target, Chassis and Utility Systems values, module stats.
- The hull list and exact functions of the astromech socket (Phase 4).

Not in scope, noted for later: the existing utility droid NPCs (rows 2175-2184) could be moved onto the new animated chassis once it exists. That touches live NPC blueprints and saved references, so it would be its own request.
