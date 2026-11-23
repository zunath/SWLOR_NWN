# Astromech Playable Species Plan

**Status: design direction approved by the owner on 2026-11-15 (see [Decisions](#decisions)). Phase 0 (go verdict), Phase 1 (chassis, animation, tools, dyes, voice set), Phase 2 (the playable species itself), Phase 3 (skills, perks, modules and tools), Phase 4 (ship role) and Phase 5 (Player Guide and the quest audit) are done; see their results sections. The body was then rebuilt as a dynamic (part-based) body on 2026-11-18 (see [Dynamic body rebuild](#dynamic-body-rebuild)), which replaces the single-model chassis, the built-in tools and rod, and the Chassis Workshop described in the phase results. What remains is the owner's: review of the choices made without check-ins, the release notes once it merges, and a play test.** No new balance numbers were introduced: every astromech skill is a number-for-number copy of an existing humanoid skill family (Phase 3 results).

**Base: this builds on the space revamp** (`feature/space-revamp`, read at `0e2bc7231`, and its `Readmes/SpaceRevampPlan.md`). It assumes crew stations, droid crew, power routing and the space jobs of Engineering, Espionage and Leadership exist. It ships as its own release after space, so it never delays the space release.

Sections: Summary · What exists today · Species rules · Skills and perks · Equipment · 3D models (Meshy) · Animation · 2D art · Sound · Code work · Phase 0 results · Phase 1 results · Phase 2 results · Dynamic body rebuild · Phases · Risks · Separate droid healing · Decisions

## Summary

Add the astromech as a second droid species beside the existing humanoid Droid. It is the first player character that is not a part-based humanoid body, so it gets its own rule set instead of exceptions scattered through the humanoid one:

- **Body:** a dynamic (part-based) body of its own shape: a dome, a barrel and two legs, each in eight designs, on its own skeleton. It wears no clothing; the dyes of its Chassis Plating are its paint. Phases 0 to 5 built it as one creature model per chassis; the owner's rule that playable species are dynamic replaced that (see [Dynamic body rebuild](#dynamic-body-rebuild)), so read the body, tool, rod and Chassis Workshop passages of the phase results as history.
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
| Space (master) | No crew or co-pilot roles in code. `ShipDetail.HasDroidBay` exists but is never set or read (on the revamp branch it marks the droid bay of industry hulls). |
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
   - *Portraits:* built from the game client's own render of each chassis (`tools/BuildAstromechPortraits.py`, see Phase 2 results) in the four sizes the haks carry, registered in `portraits.2da` under the new race id. This is the one new tool in the art pipeline.
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
- Hak content: chassis `c_astro_01` (appearance row 10300, simple type), `c_astromech`, the prod and blaster textures, the dye material and `tintmap.2da` row from the project's tint generator, voice set row 954 with its name string, and the sound files.
- Meshy spend to date: 72 of the 1,000 credits (one chassis, two tools).

**Verified in the client**

| Check | Result | Evidence |
|---|---|---|
| Shared clips on a chassis with no clips of its own | Idle, second idle, head turns, taunt, victory, both dodges, spasm, conjure and knockdown all play | `sheet_p1reel3.png`, `p1reel3.mp4` |
| Built-in tools | The blaster slides out in the ability clip; the prod extends on attack swings in a real fight | `sheet_p1reel3.png`, `sheet_p1fight.png` |
| Dyes through the real system | `TintMapService.ApplyCurrentColors` resolved the creature's material from `tintmap.2da` and applied stored colors to all three layers | `sheet_dye.png` |
| Voice set | Eight voice lines played the matching generated sound in the client's own audio (match 0.83 to 0.99; 0.17 at most for a wrong sound) | `voice_test.json`, `voice_in_game.wav` |
| Dye rules | `CheckDyeAppearance.py` passes, including the dyed-metal rule | - |

**Seven more chassis from supplied models (owner request, 2026-11-15)**

The owner asked for a contributor's set of seven astromech models to be added, made tintable and compatible. They are skinned to one shared skeleton, so the exporter gained a second source kind that assigns parts by bone. These are not original designs, so they sit outside decision 2; the owner asked for them directly.

- Models `c_astro_02` to `c_astro_08`, appearance rows 10301 to 10307, on their own copy of the clip model (`c_astromechb`, same clips, shorter tool retract).
- Each has a dye material from the tint generator. The sources carry no dye masks, so each mask is derived from the model's painted texture with per-model thresholds. Two models are a single hue, so nearly all of their paint is one dye layer.
- Checked in the client: all eight chassis load and change color through the real dye system (`runp2/evidence/sheet_tour.png`); one of the seven played the shared clips including knockdown (`sheet_p2reel.png`, `p2reel.mp4`) and the combat clips in a real fight (`sheet_p2fight.png`).
- Not verified: the tool arm's motion could not be made out from the fight camera angle; only one of the seven was filmed animating; the head gun does not deploy (it is fixed to the head); painted detail such as lenses takes the dye color of its layer.

**What Phase 1 changes for later phases**

- Per-player colors need no new dye code: the existing service already handles a non-part creature through `tintmap.2da`. Colors are `TM_<material>_<layer>` variables on the creature. Phase 2 only has to give a new astromech sensible defaults and a window to change them.
- A chassis shows its default dye rows until colors are written, so every astromech NPC blueprint needs dye variables or it renders in palette row 0.
- Client checks need the cursor moved off the model, or the hover highlight washes it out.

**Not done or not verified**

- The sounds were plain WAV in this phase. The follow-up pass compressed them like the project's other sounds (see Phase 5).
- Nobody has listened to the voice set critically; the owner has the preview file.
- The chassis was checked as the player character and as a spawned NPC standing still; the NPC was not filmed fighting.
- Death and get-up were seen as the knockdown pose only in this phase. The follow-up pass (see Phase 5) filmed the fall, the recovery and movement on the production model.
- The other three chassis, portraits, and the camera height for this appearance are later work.

## Phase 2 results

Built on the Phase 1 branch and checked on 2026-11-16 in the real client, creating new characters through native character creation. Evidence is in `C:\Temp\astro-spike\phase2\evidence` (local).

**Delivered**

- **Species:** `RacialType.Astromech = 170` with its `racialtypes.2da` row, Standard class row and strings. Force Sensitive is not offered to it.
- **Body frame:** `BodyFrame` (Humanoid or Astromech) from `Race.GetBodyFrame`, saved on the player record. Existing records read as Humanoid, so there is no migration.
- **Chassis, paint and size:** `Service/Astromech.cs` and `Service/AstromechService/`. The chosen chassis is the character's stored appearance (the value leaving space restores), paint is three zones (primary, secondary, trim) written as dye variables for the worn chassis, and size is 85% to 115%. Each chassis has its own default paint and portrait.
- **Chassis Workshop:** a new window that replaces the appearance editor for astromechs (`/customize` and the character sheet button): chassis list, size, and a color picker per paint zone. Every change applies at once. Rebuilt on 2026-11-18 to work like the Appearance Editor, which the owner asked for: the same window size and tab row (Chassis, Settings); every chassis as a 96x96 picture with its name, the worn one outlined; the three paint swatches beside the worn chassis's picture, with previous, next and view (front, back, angle) buttons; the editor's palette (cloth colours for primary and secondary, metal colours for trim) and its RGB picker with R, G and B boxes; a right-click on a swatch returns it to the chassis's own paint; and the size as Increase Size and Decrease Size on Settings. The pictures are made by `tools/GenerateAppearancePreviews.py` like every editor picture (`--only "^c_astro_"` renders just these), each chassis in the paint it comes with and with its tools and rod stowed, and ship in `sw_part_previews` as `ac_astro_NN` plus the view letter. A chassis added to `AstromechChassisCatalog` needs its three pictures and a tile binding (`ChassisWorkshopViewModel.TileCount`). Seen in the client beside the Appearance Editor: wearing a chassis from its picture and from the arrows, turning the views, a palette colour (the clicked cell is outlined), a picker drag, a typed colour (255 is the most a box keeps), the reset, the size steps, and every value back after a tab change and after logging in again.
- **Locks:** skills carry a body-frame restriction; the locked list is closed to astromechs in the skills window, RP XP distribution, the full rebuild, DM examine and XP awards. A perk behind a closed skill cannot be bought (the skill requirement refuses it); the eight humanoid-only perks that had no skill requirement gained a rank 0 one, as Mimicry already had.
- **Speech:** astromechs speak only Droidspeak, through the same rule as Wookiees.
- **Gear:** an astromech player equips only items limited to astromechs (added in Phase 3), and nobody else can equip those. The Phase 2 starter kit was the credits alone; Phase 3 adds a Basic Shock Prod.
- **Humanoid systems:** rest uses a powered-down pose, chairs refuse an astromech (including the stock chair script most area chairs use), emotes the chassis has no clip for are refused and hidden from the emote window, the voice list offers only the astromech voice, the stranger descriptor reads "Astromech", and ion and mind effects use one shared machine check.
- **Portraits:** `tools/BuildAstromechPortraits.py` builds them from two client captures per chassis (with the chassis and with it hidden), so a portrait is the game's own render. Eight portraits ship, one per chassis in its default paint.
- **Dye masks:** the exporter now centres every dye layer on the palette midtone and all eight chassis were regenerated. Before this a chosen color rendered much lighter than picked.

**Verified in the client**

| Check | Result | Evidence |
|---|---|---|
| Character creation | Astromech is in the species list with its description; Standard is offered and Force Sensitive is greyed out | `chargen_species.png`, `chargen_class.png` |
| New character | Enters in the first chassis with its default paint, the astromech voice and portrait, 200 credits and no items; strangers see "<trait> Astromech" | `new_character_default.png` |
| Speech | A line typed in Basic is spoken as Droidspeak | `forced_droidspeak.png` |
| Chassis Workshop | Opens from `/customize`; changing chassis keeps chosen paint and moves the portrait; the picker recolors a zone; size steps and is saved | `workshop_size_picker.png`, `workshop_paint_chassis_portrait.png` |
| Default paints | All eight chassis in their default paint | `defaults1.png` |
| Rest | The astromech powers down on the floor and the rest effect ticks | `rest_pose.png` |
| Emotes | `/bow` is refused with a message | `emote_refused.png` |
| Chairs | Using a chair is refused with a message | `chair_refused.png` |
| Gear and skills | Clothes and a knife are refused; the skills window lists only the shared skills; a locked skill takes no XP while a shared one does | `skills_and_equip_refusal.png` |

**Found on the way**

- A descriptor was generated from the half-created player record, before the species was applied, so it read "Humanoid" and was never replaced. `PlayerDescriptor.EnsureUnknownDisplayName` now waits for character initialization. This is not specific to the astromech.
- The shared tint shaders light a custom color from its nearest palette swatch. On the cloth layers, greys from about 220 to 236 land on a metallic swatch and render as chrome. That is how the dye system works for every item, so it is left alone; the default paints avoid it.
- Pure white (255, 255, 254 or 255) sent as a custom color renders red: the value rounds past the top of the range the shaders decode. Astromech paint stores such a white one step lower. The same defect is open for every other custom color in the dye system and is not fixed here.

**Not done or not verified**

- Entering and leaving space as an astromech was not run in the client in this phase; Phase 4 ran it (the chosen chassis is restored).
- The last workshop layout change (a taller chassis list so all eight names show without scrolling) was made after the final client pass.
- Work animations (crafting, gathering, item use) had no clip on the chassis in this phase; the follow-up pass (see Phase 5) added them.
- The perks window listed humanoid-only lines to an astromech at the end of Phase 2; Phase 3 filters lines by body frame.

## Phase 3 results

Built on 2026-11-16 on the same branch. The owner asked for the work to be finished without further check-ins, so the Bible-first approval step was replaced by a rule that needs no new balance decision: **every astromech number is an existing humanoid number.** Each astromech skill is a generated copy of a humanoid skill family with new names, and tests hold each copy to its source. The choices below were made without the owner and are listed for review in the pull request.

**Delivered**

- **Chassis (skill 61):** the astromech's Armor. Its rank stands wherever Armor rank is read for an astromech: defense, evasion, combat XP, the General perks and the gear that ask for Armor (`SkillBodyFrames`). It has no perk lines of its own. Dual Wield and Doublehand stay humanoid-only, which is what keeps astromech weapon damage below the humanoid weapon band.
- **Utility Systems (skill 62):** two lines, Repair and Augments, copies of the First Aid Trauma Medic and Combat Pharmacology lines. They share First Aid's status effects, supplies and capstone quest, so a mirrored effect and its source never stack.
- **Armaments (skill 63):** two lines, Shock Prod (a copy of Staff Crusher) and Blaster Mount (a copy of Rifle Pacification). The tools are two new small base items, because the engine refuses a weapon two sizes larger than a small creature. In combat a shock prod is a staff and a blaster mount is a rifle, so every staff and rifle rule applies unchanged; the rank behind them and the XP they earn are Armaments (`SkillBodyFrames.GetTrainedSkill`). The Staff and Rifle capstone quests accept Armaments.
- **Generator:** `tools/GenerateAstromechSkillMirrors.py` writes the perk and ability definitions, enum values, 2DA rows, strings and engine-test cases from `tools/AstromechSkillMirrors.json`, with a `--check` mode. Abilities play the chassis's tool clip in place of humanoid authored animations.
- **Gear:** 160 modules (stat copies of the armor ladders, `tools/GenerateAstromechModules.py`) and 20 tools (stat copies of the staff and rifle ladders, `tools/GenerateAstromechTools.py`), each limited to astromechs and to its astromech skill, with Engineering recipes that mirror the Smithery ones and the first tier sold by the CZ-220 merchant. A new astromech starts with a Basic Shock Prod, as others start with a knife.
- **Perks window:** lines are filtered by body frame, so an astromech sees its own lines and no humanoid weapon, Force, First Aid, Mimicry, Beast Mastery or Performance line, and nobody else sees the astromech lines.
- **Design Bible:** an `Astromech Skills` tab (the three skills and one row per mirrored perk level with its source), and the module and tool rows on the recipe and equipment tabs.

**Verified in the client** (evidence in `C:\Temp\astro-spike\phase3\evidence`)

| Check | Result |
|---|---|
| Modules | Chassis Plating Mk III equips at Chassis 20 and raises HP and defense; Mk VIII is refused |
| Utility Systems | Field Repair II heals and spends supplies; Deflector Field and Power Transfer apply their effects |
| Starter kit | A new astromech holds a Basic Shock Prod and equips it |
| Shock prod | Attacks land; Shock Jab I hits for 18 against 5 to 7 for a plain attack; the kill gives Armaments XP and no Staff XP |
| Blaster mount | Ranged attacks land; Suppressive Burst and Hobbling Bolt fire, and Hobbling Bolt applies Hamstring; no Rifle XP |
| Tier gate | A Delta Shock Prod is refused at Armaments 10 with "This item requires Armaments rank 20 to use." |
| Perks window | Lists Armaments - Shock Prod and Armaments - Blaster Mount with Armaments rank requirements |

**Known gaps**

- Mirrored abilities and perks reused their source's icons, and shared status effects showed the source's name. Both are fixed in the follow-up pass (see Phase 5).
- Recipes and the store entries were checked by tests and module data, not crafted or bought in the client. The follow-up pass did both and found the store entries had never reached the game (see Phase 5).
- No CombatSim run was made in this phase. The follow-up pass added the astromech to the simulator and ran it (see Phase 5).

## Phase 4 results

Built and checked on 2026-11-16 with two real clients against one private server: a human pilot flying a Striker and an astromech aboard it. Evidence is in `C:\Temp\astro-spike\phase4\evidence`. The hull list and the socket's exact functions were open decisions in this plan; they were settled here without the owner and are listed for review in the pull request.

**Delivered**

- **Crew seats:** nothing new was needed. An astromech takes the Gunner, Engineer or Sensors seat of a Large or Capital hull like any player.
- **Astromech socket:** a new seat, `ShipCrewSeat.AstromechSocket`, on the Small and Medium combat hulls: Fighter, Interceptor, Bomber and Gunship (17 hulls). Industry hulls keep their droid bay; haulers and explorers have neither. Only an astromech player can take it, from the ship's computer aboard while the pilot flies, in the pilot's party and with the Crew Station permission, as for any station. Every Small and Medium interior already has that computer, so no boarding flow was added.
- **What the socket does:** it owns no slots, so a socket hull flies exactly as before with the socket empty and no fitted module goes dark. An astromech in it routes the ship's power as the Engineer does, and works the pilot's Hull Repairer, Shield Repairer, Repair Field Generator and Hypermatter Injector beside the pilot, on its own Engineering rank. It never works a weapon. A repair module that is on runs its cycles as the astromech while it is plugged in and as the pilot again when it leaves. Droids cannot be posted at the socket.
- **Pose:** the stand-in an astromech leaves at a ship's controls, a crew station or the socket holds the chassis's tool clip instead of sitting.
- **Space plan:** the socket is recorded in `SpaceRevampPlan.md` section 10.

**Simulator** (`tools/SpaceRevampBalance/checks.py` section S, 24 of 24 bands met)

A pilot plus an astromech at Engineering 50 and the crew cap, on its best routing, against two of the same hull, for all four socket roles at ranks 15, 35 and 45: the socketed ship never kills as fast as the two ships (fastest two-ship to socket kill-time ratio 0.49) and does at most 40% of their job score. The second player is always worth more in a second ship; the socket is a seat for a player who would rather crew than fly.

**Verified in the client**

| Check | Result | Evidence |
|---|---|---|
| Socket offered | The ship's computer of a flying Striker lists "Astromech Socket (Engineering): open" and offers "Plug into the astromech socket" to an astromech in the pilot's party | `astromech_socket_dialog2.png` |
| Party rule | The same dialog offers only the viewport to an astromech outside the pilot's party | `astromech_socket_dialog.png` |
| Plugged in | The astromech's view follows the ship; its hotbar holds the hull's Shield Repairer and the four routing modes | `astromech_socket_seated.png` |
| Power routing | Routing to Shields and Weapons from the socket changes the ship's routing and tells the pilot; at Engineering 50 the gain is 25% larger ("weapons use 31% less capacitor") | `astromech_socket_routing_weapons.png`, `pilot_client_chat.txt` |
| Repairs | The astromech switches the pilot's Shield Repairer on. A rank 45 repairer restores 12 per cycle for the pilot alone and 13 with the astromech (Engineering 50) plugged in | `pilot_client_chat.txt` |
| Leaving | `/exit` returns the astromech to the computer with its own hotbar; the pilot is told, the routing returns to Balanced and the repairer stays on as the pilot's | `astromech_socket_left.png` |
| Astromech as pilot | An astromech flies the Striker; its stand-in at the controls holds the tool-arm pose; leaving space restores the chosen chassis | `pilot_astro_pilot_standin2.png`, `astromech_astro_pilot_space.png`, `astromech_astro_after_space.png` |

**Not done or not verified**

- A Large hull seat, the socket on other hulls, and the Repair Field Generator and Hypermatter Injector were not run in this phase. The follow-up pass ran them (see Phase 5).
- Ship registration, the party and the Crew Station permission were set up by a test driver, not through their windows. The test also left space through the driver, which is why the astromech is seen in orbit after the chassis restore.

## Phase 5 results

Done on 2026-11-16. Evidence is in `C:\Temp\astro-spike\phase5\evidence`.

**Delivered**

- **Player Guide:** an `Astromechs` topic (body, Chassis Workshop, speech and emotes, skills, modules and tools, the socket), seen in the client.
- **Quest and tutorial audit:** the CZ-220 orientation to the shuttle pass was walked through in code and has no step an astromech cannot do; no mandatory quest gives an item reward. Four defects found by the audit were fixed:
  - Armaments 50 opened the Staff Sentinel (Unmoving Center) and Rifle Marksman (One Shot) capstone quests, whose proofs need abilities the astromech has no copy of. Those ten quests now ask for the skill itself (`QuestBuilder.PrerequisiteOwnSkill`). The Worldbreaker and Kill Box lines, which Armaments does copy, still accept it.
  - A quest skill prerequisite now refuses a skill the character's body cannot train, so the Mimicry introduction (which only teaches Mimicry) is closed to astromechs.
  - XP tomes listed every skill and were destroyed even when the chosen skill took no XP, for any species. The tome now lists only skills the reader can train and is kept if nothing can be gained.
  - An astromech could not hold a fishing rod, which closed fishing (Agriculture) and the Mon Cala rod quests. A fishing rod is now the one ordinary item an astromech equips. The rod equips in the client; a fishing attempt was not run.
- **Engine tests:** the generated ability cases ran in the engine for the first time: Armaments 22 of 22 and Utility Systems 26 of 26 passed. A new socket test (`AstromechSocketEngineTests`) passes beside the existing crew station test.

**Follow-up pass (2026-11-16)**

The gaps the first pass left were closed, and client checks found two defects the tests had not. Evidence is in `C:\Temp\astro-spike\phase5\evidence\followup` and `...\icons`.

- **Quest rewards and guild gear:** a quest item reward gives an astromech the astromech form of a piece of humanoid equipment (`AstromechItemTwins`, `tools/GenerateAstromechRewards.py`): eleven quest reward modules, the Captain Shock Prod and Blaster Mount, and Hunter Shock Prods and Blaster Mounts I to V in the Hunters Guild stores. Each is a stat copy of its source with the Chassis or Armaments requirement. A reward with no astromech form (the other Captain weapons, a lightsaber crystal) is given as it is. Seen in the client: the reward selection of the Mandalorian Leader quest lists Captain Shock Prod for an astromech.
- **Placed stores (defect found in the client):** a store placed in an area holds its own item list, so the gear added to the CZ-220 merchant blueprint in Phase 3 was never sold in the game. `tools/AstromechPlacedStores.py` now stocks the placed copies, and a test fails when a placed store lacks what its blueprint lists. Seen in the client: the CZ-220 merchant sells the Basic Shock Prod and Basic Blaster Mount and a Blaster Mount was bought; the Hunters Guild rank 3 store sells the Hunter tools.
- **Icons:** the 62 astromech abilities have their own artwork (`ife_am_*`, SVG sources under `sw_ability_source/astromech`), with cooldown variants and manifest rows. Seen upright in the Perks window and on the hotbar.
- **Status effect names:** an astromech ability applies the status effect of the ability it copies, so the two never stack. The effect now shows under the astromech name with its own icon row (`AstromechStatusEffectNames`, effecticons rows 526 to 539, `tools/StampAstromechStatusIcons.ps1`). Seen in the client: the status bar icon, its hover name, the apply and wear-off messages and the timer list read Deflector Field, Damper Field and Aggression Routine.
- **Shuttle merchant:** Sal greets an astromech with the Chassis Workshop instead of the clothing store (`condition-player-is-astromech`, `action-open-chassis-workshop`). Seen in the client, with the final workshop layout.
- **Disguises:** a disguise keeps the astromech voice; the editor lists only that voice. Seen in the client.
- **Work animations:** the chassis has reach clips (`cgetmid`, `cgetlow`), and the authored humanoid working clips (`NamedAnimation`) play the astromech work motion. Seen in the client: the tool arm extends for both reach animations. The seven GR2 chassis first got these clips when every chassis was re-exported for the fishing rod; their clip model (`c_astromechb`) had been left without them.
- **Fishing:** an astromech fishes with a rod built into its chassis. Held items are not drawn on the chassis model type, so the equipped rod was invisible and fishing showed only the tool arm. Every chassis now carries a rod and line that rest at a thousandth of their size and are scaled up only by the fishing clips: the cast and the wait (`cfish`, `cfishlp`), the three line actions (`cfishreel`, `cfishgive`, `cfishhold` behind the short `cfishin`) and landing the fish (`cfishland`, `cfishlandlp`). `AstromechWorkAnimation` maps the work animation's two clips to the pair for the authored fishing clip being played and restores them when the action's time is up, so crafting and gathering keep the tool arm. Seen in the client: a cast at a Viscara Lake pier, a bite, three Reel actions through the fight window and the catch; every rod clip up close; the rod on all eight chassis; and the tool arm back for a work action after fishing. Between line actions the rod is stowed, where a humanoid stands holding its rod; the owner accepted this as long as the rod item is equipped. The built-in rod stands for that item, which fishing already requires in the right hand, and it cannot show without it: the engine holds an unequip until the clip being played is over (seen in the client: a rod dragged out of the hand slot four seconds into an eight-second cast left the hand when the cast ended). `AstromechWorkAnimation` hands back a token like the humanoid playback does, so a caller that stops the motion (a cancelled cast, a craft that ends) stows the rod or draws the tool arm in at once instead of leaving it out until the clip's time is up. Seen in the client: an eight-second cast stopped at four seconds stowed the rod within a second, and a twenty-second work motion stopped at five drew the tool arm in. A token is stopped by whichever playback gave it out (`AstromechWorkPlayback` for a work motion, `NamedAnimationPlayback` for a native preview), and starting either releases the other, so the animation previewer's Stop also ends a native preview on an astromech. Seen in the client: a slowed native dome spin was cut short by its stop, and a native preview started mid-cast stowed the rod.
- **Crafting:** a Basic Shock Prod was crafted at an Engineering terminal through the Circuit Assembly window.
- **Ship crew:** the socket was run on a Republic Bomber (Hypermatter Injector, which asks the astromech for the fuel capsule) and a Republic Gunship (Repair Field Generator, shields 26 to 55 of 66). A Republic Hammerhead (Large) refuses the socket and seats the astromech as Gunner.
- **Hidden at a station (defect found in the client):** after boarding from the ship's interior the astromech's model could stay drawn on top of the ship. The hide effect is now sent again when an astromech arrives beside the ship.
- **Movement and knockdown:** rolling, the fall and the recovery were filmed on the production model.
- **Tests fixed:** the two quest tests that read a line `Quest.cs` no longer has, the disguise audit harness, and the Mon Cala starlane test (the 204-quest merge added four NPCs to `moncaladaccityex` without their comment entries; the four entries are added).
- **Voice set:** the 49 astromech voice sounds are MP3-compressed by `SWLOR_Haks/tools/CompressSoundResources.py` (manifest `tools/SoundCompressionManifestAstromech.json`), with `ffmpeg` run from the client image. The loop audit passes; it now reads Module files that hold Windows-1252 bytes.
- **CombatSim:** the simulator knows the astromech. A combatant has a `bodyFrame`, and a scenario that gives a body an ability or perk it cannot train is refused. Astromech abilities use the effect model of the ability they copy (`Data/AstromechMirrors.cs`, read from the generator's configuration), and `AggressionRoutine` is accepted as a stance. Two scenarios were added. Results at seed 11: the Shock Prod duel against the Staff Crusher build it copies is 51% to 49% over 3,000 runs, and swapping the sides swaps the numbers exactly, so the two-point lean is team order; the support pair with an astromech healer against the First Aid pair is 34% to 39% over 1,000 runs and mirrors exactly when swapped. A mirrored build deals the same damage and heals the same amount as its source in a fixed-seed test.

**Left as they are, on purpose**

- Ordinary stores are not filtered: an astromech can carry, trade and sell humanoid gear, and the store window already marks what it cannot use.
- The CZ-220 crafting step can make a knife the astromech cannot use; the quest collects it.
- Humanoid capstone quests of closed skills are unreachable for an astromech.
- Shared mechanic names (Daze, Hamstring, Suppression) are the same for every species.

**Not done**

- Release notes: the release-notes process covers changes merged into the default branch, and this branch is not merged. The Player Guide topic and the pull request description hold the player-facing text until then.
- The play test on the test server is the owner's step.
- The compressed voice set was validated by decoding every file; it was not listened to in the client, whose stock container has no audio.
- The simulator does not model the Rifle line, so the Blaster Mount line has no weapon-formula scenario; it can be run with calibrated damage profiles like any Rifle build.

## Dynamic body rebuild

Done on 2026-11-18, on the owner's rule that every playable species has a dynamic (part-based) body
(`ContentStandards.md`). Evidence is in `C:\Temp\astro-spike\dyn\evidence21`.

**What changed**

| Before (phases 1 to 5) | Now |
|---|---|
| Eight appearance rows 10300-10307, simple type, one model per chassis | One row 10300, `MODELTYPE` `P`, race letter `R`: skeleton `pmr0`/`pfr0`, animation bank `a_astro` |
| A chassis picked as a whole in the Chassis Workshop | Dome (head), body (torso) and two legs (thighs) picked in the regular Appearance Editor, eight designs each, mixed freely |
| Paint stored on the player, three zones in the Workshop | Paint is the dye of the worn Chassis Plating, set on the editor's Equipment tab; a chassis with no plating wears factory white and blue |
| Prod and blaster built into the model; equipped tools invisible | The shock prod and blaster mount are held items with their own item classes (`WAmPr`, `WAmBl`) that slide out of the body in the attack clips |
| Fishing rod built into every chassis | The astromech holds the fishing rod item; the fishing clips swing the hand |
| Clip names of simple creatures (`cwalk`, `ca1slashr`, ...) | Every engine-native humanoid name, each a chassis clip with its nodes renamed |

**Delivered**

- `tools/AstromechDynamicBody.py` (with tests): re-cuts the exporter's chassis into the skeleton, the animation bank
  (171 native names and the eight fishing clips), 64 part models and the held tool models with their icons. The
  steps after it (tint bindings, pictures) are in `BlasterModelImport.md`.
- Species data: `AstromechRacialAppearanceDefinition`, `AstromechArmorAppearanceDefinition` (every module part is
  style 0, so the chassis shows through), `AppearanceType.Astromech`, the `Race` defaults (design 1, no humanoid
  parts), part pictures and part names (`Classic Dome`, `Courier Body`, `Scout Leg`, ...).
- `AstromechBody`: seats the dome of one design on the body of another (a head-scope visual transform, applied
  whenever the body is redrawn) and holds the paint of a bare chassis.
- Chassis Plating paint jobs, plating blueprints without the part models of the humanoid piece they copy (a
  leftover number could name a chassis part), and the stat-less `Basic Chassis Plating` a new astromech starts in
  (`tools/GenerateAstromechModules.py`).
- Helmet- and cloak-slot modules are hidden on an astromech; the dome keeps its size (`/headscale` refuses).
- Inventory icons drawn from the models (`tools/BuildAstromechIcons.py`). Chassis Plating stayed an armor item:
  a base item of its own would have meant teaching every armor rule and the editor's dye controls about it. It
  carries torso 240 instead, a number with no model on any body and an icon of its own, a chassis body that wears
  the plating's dyes. The shock prod and blaster mount show the held tool; before, they showed the top of the
  staff and rifle icons they copied.
- Removed: the Chassis Workshop (window, view model, service, snippet action, conversation choice), the chassis
  catalog, paint zones, `Player.AstromechPaint`, the simple chassis models and their pictures.

**Client evidence** (fresh character through native character creation)

| Check | Result | Evidence |
|---|---|---|
| New character | Enters as the dynamic body in design 1, wearing Basic Chassis Plating, with its portrait and voice set | `g4_entered.png`, `b1_body_front.png` |
| Appearance Editor | `/customize` opens the regular editor; the slots show the dome, body and legs with pictures and names; stepping a slot swaps the part | `e1_editor.png`, `e2_head4.png`, `n2_editor_body_named.png`, `n3_editor_leg_named.png` |
| Mixed parts | A dome of one design sits on the body of another (4 on 1, 1 on 5, 1 on 8) | `e3_head4_on_barrel1.png`, `n2_dome1_barrel5.png`, `f2_torso_changed_in_editor.png` |
| Paint | Dyeing the plating on the Equipment tab repaints dome, body and legs; without plating the chassis is white and blue | `q1_equipment_tab.png`, `q3_dyed_droid.png`, `u1_bare_chassis.png` |
| Held tools | The prod and the blaster slide out of the body in a real fight and land hits | `k3_fight_sheet.png`, `k4_blast_sheet.png` |
| Fishing rod | The held rod is drawn and swings on the cast | `r_fish_sheet.png`, `r_rod_idle_sheet.png` |
| Hidden modules | Head and cloak modules are worn and nothing is drawn for them | `h1_head_cloak_modules.png` |
| Size | The Settings tab changes and saves the height | `n4_editor_settings.png`, `n5_after_settings.png` |

**Left as it is**

- The other modules (sensor package, servo, drive unit and so on) still showed the icons of the humanoid pieces
  they copy, and module descriptions were still the text of those pieces. Both were done on 2026-11-20: see
  Descriptions and Module icons below.
- The editor shows the Skin, Hair and Tattoo swatches and the Show Helmet and Show Cloak boxes to an astromech, as it
  does to everyone; they change nothing on a chassis.
- Legs are not fitted to the body they are worn with. The eight designs share one leg joint, and the mixes looked
  right in the client.

## Looted gear

Done on 2026-11-19, at the owner's request that astromechs have their own equipment in loot.

- **The gap.** The loot tables drop 937 distinct pieces of gear and an astromech could equip none of them. 618 of them ask for rank 45, a tier that exists only as loot, so an astromech had no gear between the crafted Mk IX (40) and Mk X (50).
- **The forms.** `tools/GenerateAstromechRewards.py` reads the loot tables and writes an astromech form of every looted armor-slot piece, staff and rifle: 781 items (748 modules, 17 shock prods, 16 blaster mounts). A form is a stat copy with the Chassis or Armaments requirement and the astromech race limit, like the crafted gear. Its name puts the module name in place of the humanoid slot word (`Overcharge Sash` becomes `Overcharge Power Cell`); `tools/AstromechLootNames.json` names the 48 pieces that rule cannot. Gear made for the humanoid Droid species has its race limit changed to astromech. No balance number is introduced.
- **How it drops.** No loot table lists astromech gear. After a table is rolled for its own items, it is rolled again at the same chance, and that second roll drops the astromech form of the piece it picks and nothing for any other pick (`AstromechLoot`). A form is therefore exactly as likely as the piece it copies, humanoid drop rates do not change, and a new looted piece only needs the generator rerun. The second roll applies everywhere the loot code spawns from a table: creature corpses, steal loot, treasure containers, prospecting and scavenging.
- **Consequence to know.** Every kill can now drop a second piece of gear, whoever makes it, so more gear enters the world than before. Forms can be traded or sold like any item.
- **Not covered.** 155 looted weapons of other kinds (pistols, blades, lightsabers and so on) have no astromech tool to become.

## Store and slicing gear

Done on 2026-11-19, at the owner's request for astromech versions of the store gear and slicing rewards.

- **The forms.** `tools/GenerateAstromechRewards.py` now also reads every store (`Module/utm`) and the slicing rewards (`Service/SlicingService`) and writes a form of every armor-slot piece, staff and rifle there that asks for a skill rank: 264 new items (232 store pieces, 32 slicing pieces; 7 of them tools). The guild stores sell gear that asks for Smithery, Fabrication, Agriculture or Engineering ranks; an astromech trains those skills, so those forms keep the requirement and gain only the astromech limit.
- **Stores.** Every store that sells a piece sells its form beside it: 28 store blueprints and the 261 copies of them placed in areas, which are what the game opens. A placed copy is found by the blueprint name in its `ResRef`; its tag is often another name. Existing stock is not touched.
- **Droid gear in stores.** The general merchants and guild stores also sell 304 droid-only pieces. Each has exactly the stats of a humanoid piece that already has a form, so it gets no form of its own; the store sells the form of that humanoid piece beside it. An astromech can therefore buy an equal of every piece of gear a store sells. Looted droid-only gear keeps the forms it had.
- **Slicing.** A slicing cache rolls its rewards as before; an astromech slicer receives the form of a piece that has one (`SlicingReward.ItemsFor`), like a quest reward. Ship slicing hands out no gear.
- **Not covered.** Clothing and other pieces that ask for no rank (the starting items store, the Republic armor store, two training pieces) and weapons of other kinds.
- **Cost.** A placed store holds every item in full, so the area files grew by about 40 MB of JSON.

## Descriptions

Done on 2026-11-20, after the owner asked that plating stop reading as a tunic.

- **The problem.** A form copied its humanoid piece whole, description included, so 484 of the 1,248 forms described a tunic, a ring, boots or a rifle.
- **The fix.** Each of those forms has a description of its own in `tools/AstromechDescriptions.json`, keyed by the humanoid resref and written by hand: the same lore, names and stated effects, describing the droid part. The three gear generators write it into both description fields (`GenerateAstromechModules.described`) and stop on a described piece that has none. Gear that says it "requires Armor training" says Chassis training, which is what its form asks for.
- **Not changed.** The 764 forms whose humanoid piece has no description still have none.
- **Names.** Nineteen forms whose names still held a garment, a piece of jewelry or a body part were renamed the same day (`tools/AstromechLootNames.json`): the Sacred accessories follow the set's Lumen, Daybreak and Vigil variants ("Sacred Amulet Logic Module" is "Sacred Lumen Logic Module"), "Ashline Hushcloak Deflector Emitter" is "Ashline Hushfield Deflector Emitter", and so on. Names of other things stay: "Dead Hand" (a creature), "Ringside", "Boot Hill", hide as a material.

## Module icons

Done on 2026-11-20, after the owner asked for icons that fit a droid, with the condition that humanoid items stay
humanoid.

- **The problem.** A module keeps the base item of the humanoid piece it copies, and a helmet, an amulet, a belt, a
  ring, gloves, a bracer, boots and a cloak pick their inventory icon by model number. So 1,019 modules showed a hood,
  a necklace, a ring or a boot. Plating and the two held tools already had icons of their own.
- **The fix.** Each of the seven module kinds has four icons of a droid part under model numbers no humanoid piece
  uses (`MODULE_ICONS` in `tools/GenerateAstromechModules.py`; the numbers are listed in `ItemAppearanceRules.md`).
  The generators put one of its kind's numbers on every module, spread evenly by the source resref, and the placed
  store copies follow. The numbers have no model, which changes nothing on the body: a chassis never drew these
  pieces.
- **The art.** `tools/BuildAstromechModuleIcons.py` fits each source picture
  (`SWLOR_Haks/sw_item_source/astromech`, with the prompts) into its inventory slot as a slot-sized DXT5 DDS with its
  8x8 TGA, and writes the four cloakmodel.2da rows (no model, as the non-appearing cloak has). The pictures were
  generated, which the owner approved; renders of the domes and legs were tried first and read poorly at icon size.
- **Helmets and cloaks.** The first client pass showed nothing for a Sensor Package and a stock cape for a Deflector
  Emitter: those two base items draw their icon through the dye palette and accept only a palette texture. Their
  icons are PLTs of two layers, the metal of the part on the first metal dye and its glow on the first cloth dye,
  and the generators give each of those modules the dyes its icon was drawn in (`ICON_METAL_DYE`, `ICON_GLOW_DYES`).
  A chassis shows neither piece, so the dyes color nothing but the icon.
- **Humanoid items.** No humanoid blueprint, model, icon or 2DA row changed. `AstromechModuleIconContentTests` and
  `test_build_astromech_module_icons.py` fail if a humanoid piece carries a module number, if an appearance catalog
  offers one, or if a module shows anything else; the icon tool stops at a name that already holds humanoid art.

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

**Phase 2 - Species plumbing. Done 2026-11-16; see Phase 2 results.** Race, body frame, creation flow, starter kit, speech rule, all humanoid-system guards, Chassis Workshop, portraits. *Exit:* a new astromech character can be made and played end to end with no skills or gear of its own yet, and no humanoid system misbehaves for it.

**Phase 3 - Skills, perks, abilities, modules. Done 2026-11-16; see Phase 3 results.** Planned as Bible tabs approved by the owner before any code; built instead as copies of existing humanoid families at the owner's instruction to finish without check-ins. *Exit:* parity with the source families, held by tests.

**Phase 4 - Ship role. Done 2026-11-16; see Phase 4 results.** Crew seat pose and checks; the astromech socket, with its simulator evidence. *Exit:* an astromech crews a Large hull seat and a fighter socket in the client, and the pilot-plus-astromech run stays at or below two separate ships.

**Phase 5 - Content and polish. Done 2026-11-16 except the release notes and the test server play test; see Phase 5 results.** Remaining chassis and paint, tutorial and quest audit for humanoid-only steps and rewards, Player Guide, release notes, a play test on the test server.

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
