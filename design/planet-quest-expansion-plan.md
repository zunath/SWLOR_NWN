# Planet Quest Expansion Plan

## Scope

Add 268 new hand-crafted, NPC-delivered quests so each tracked planet lands at 52 local planet quests total. Every quest in this plan has a named quest giver who must be represented by a placed NPC in the listed area. This document is the source plan for implementation slices; keep it updated when code or dialogue changes refine a planned quest.

## Current Repo Grounding

- Existing planet quest definitions: `ViscaraQuestDefinition`, `MonCalaQuestDefinition`, `TatooineQuestDefinition`, `DantooineQuestDefinition`, and `HutlarQuestDefinition`.
- Dathomir has area, spawn, loot, fishing, achievement, and NPC group support, but no dedicated planet quest definition yet. Implementation should add `DathomirQuestDefinition`.
- Existing local planet quest counts are uneven: Viscara has 22, Mon Cala has 6, Tatooine has 3, Dantooine has 8, Hutlar has 5, and Dathomir has no local planet quest chain.
- Parity target: after planned additions, each planet has 52 local planet quests. This excludes separate access or hidden quest definitions such as `HiddenAccessQuestDefinition`.
- Quality target: equal quest count is a constraint, but equal meaningful playtime is the quality bar.
- Dialogue should use legacy `Module/dlg/*.dlg.json` quest snippets unless a quest requires runtime menu logic.
- Fixed quest givers should use `Module/utc/*.utc.json` templates and be placed in `Module/git/<area>.git.json` under `Creature List.value`.

## Quest Count Target

| Planet | Existing Local Planet Quests | Planned New Quests | Final Local Planet Total |
|---|---:|---:|---:|
| Viscara | 22 | 30 | 52 |
| Mon Cala | 6 | 46 | 52 |
| Tatooine | 3 | 49 | 52 |
| Dantooine | 8 | 44 | 52 |
| Dathomir | 0 | 52 | 52 |
| Hutlar | 5 | 47 | 52 |

## Reward Plan

Every planned quest has a reward through this matrix. XP rewards are direct unallocated XP with no hidden scaling, so default rewards should stay conservative. Use the minor one-time reward for short delivery, scan, inspection, marker, conversation, or simple repair quests. Use the standard one-time reward for ordinary kill or collect quests. Use the repeatable reward for `Repeatable = yes`. Use the major and capstone rewards only for quest IDs listed in the override rows below. Hard one-time quests may also receive the quest-exclusive item rewards listed below after creating the new UTI resources.

| Planet | Minor One-Time Reward | Standard One-Time Reward | Repeatable Reward | Major Quest Reward | Capstone Quest Reward |
|---|---|---|---|---|---|
| Viscara | 2,000 XP; 1,125 credits | 4,000 XP; 2,625 credits | 1,500 XP; 750 credits | 6,000 XP; 6,000 credits | 12,000 XP; 11,250 credits |
| Mon Cala | 1,000 XP; 750 credits | 2,000 XP; 1,500 credits | 1,000 XP; 750 credits | 4,000 XP; 4,000 credits | 7,500 XP; 7,500 credits |
| Tatooine | 1,000 XP; 750 credits | 1,750 XP; 1,500 credits | 1,750 XP; 750 credits | 4,000 XP; 4,500 credits | 7,500 XP; 11,250 credits |
| Dantooine | 2,000 XP; 1,500 credits | 4,000 XP; 3,750 credits | 600 XP; 300 credits | 6,000 XP; 7,500 credits | 12,000 XP; 11,250 credits |
| Dathomir | 2,500 XP; 2,500 credits | 5,000 XP; 5,500 credits | 2,000 XP; 2,500 credits | 8,000 XP; 7,200 credits | 12,000 XP; 11,250 credits |
| Hutlar | 800 XP; 825 credits | 1,300 XP; 1,800 credits | 800 XP; 900 credits | 5,000 XP; 6,000 credits | 15,000 XP; 22,500 credits |

Planet difficulty and reward rules:

| Planet | Danger band | Current anchors | New reward rule |
|---|---|---|---|
| Mon Cala | Low-to-mid exploration and wildlife danger | Existing content is mostly fishing/tool progression plus one 2,500 XP P3DR0 quest; spawns are coral wildlife, eco-terrorists, swamps, and caverns. | Keep ordinary quests at or below 2,000 XP. Use 4,000 XP only for named leaders, cave rescues, or multi-step survey chains. Unique items should be utility, trophy, or sensor artifacts worth about 6,000-10,000 for major quests and 15,000-20,000 for the tidewatch capstone. |
| Tatooine | Low-to-mid desert danger with dangerous spikes | Existing ordinary quests pay 1,750 XP, while the 150-kill Tusken capstone pays 7,500 XP; spawns include womprats, sand demons, Tuskens, sand worms, and rare world-boss threats. | Keep routine desert work at 1,000-1,750 XP. Use 4,000 XP for elite Tusken, rancor spoor, worm, or multi-area risk. Capstone remains 7,500 XP. Unique items should be survival relics, trophies, or salvage worth about 6,000-10,000 for major quests and 15,000-20,000 for the ancient husk capstone. |
| Hutlar | Low overworld, high dungeon/outpost crisis | Existing overworld quests pay 800-1,300 XP, while `break_the_byysk` pays 15,000 XP and 22,500 credits; spawns include Byysk, Qion animals, and dungeon bosses. | Keep ordinary tundra work low at 800-1,300 XP. Use 5,000 XP only for hard Byysk, champion, clone-log, or hive work. Preserve 15,000 XP only for the outpost-scale capstone. Unique items should be 6,000-10,000 for overworld major quests and 20,000-30,000 for dungeon or outpost capstones. |
| Dantooine | Mid-tier colony, cave, and wildlands danger | Current quests pay 2,000-5,000 XP, with `hidden_cave` at 12,000 XP; spawns include kinrath caves, Janta, mountain wildlife, Dantari, and world-boss tables. | Keep simple colony errands at 2,000 XP, ordinary kill or collect quests at 4,000 XP, and dangerous cave or predator work at 6,000 XP. Capstones may reach 12,000 XP. Unique items should be colony archives, chitin, or crystal artifacts worth about 8,000-15,000 for major quests and 20,000-25,000 for capstones. |
| Viscara | Mid-to-high frontier and faction danger | Current typical rewards range 2,000-8,000 XP, with 12,000 XP for major combat and one exceptional 25,000 XP recipe quest that should not become the baseline. | Keep short local work at 2,000 XP, ordinary faction or wildlands work at 4,000 XP, and hard faction chains at 6,000 XP. Use 12,000 XP only for capstone or comparable major danger. Unique items should be faction, signal, or Jedi artifacts worth about 8,000-15,000 for major quests and 20,000-25,000 for capstones. |
| Dathomir | High danger wilderness, ruins, and Force threat | No current local quest definition exists, but spawn tables include dense hostile tribal, shaman, ruin, mountain, rancor, dark adept, and hidden tunnel threats. | Treat Dathomir as the highest baseline planet in this batch, but do not exceed Viscara/Dantooine capstones without a boss chain. Keep ordinary quests at 5,000 XP, hard ruin or boss setup quests at 8,000 XP, and capstones at 12,000 XP. Unique items should be relics, tablets, and trophies worth about 12,000-18,000 for major quests and 25,000-30,000 for capstones. |

Additional reward guardrails:

- Rewards must match the quest's actual objective cost, not just the planet name. A Mon Cala cave rescue can pay more than a Mon Cala courier task, while a Hutlar overworld patrol should not inherit the Byysk capstone payout.
- Repeatable quests use the repeatable column and never receive unique permanent items. Repeatable XP should stay between 25% and 60% of that planet's standard one-time quest reward unless the objective is unusually long.
- Major rewards require at least one of: named enemy, dangerous area, multiple objectives, prerequisite chain, or unique placed object interaction. Capstone rewards require a mini-arc culmination, boss-scale threat, or planet-wide consequence.
- Do not use Viscara's 25,000 XP spice quest as a template. It is an exception for an unusually long recipe chain.
- Unique item rewards should be quest-exclusive, one-time, and omitted from shops, loot tables, fishing, harvest tables, and repeatable quests.

Physical objective item rules:

- Introduce new UTI collect items when the player is recovering evidence, samples, salvage, trophies, tools, records, or marked goods from the world, enemies, containers, or placed interactables.
- Do not create temporary inventory items for short dialogue-only handoffs unless the item changes player choice, routing, risk, or persistence. Dialogue state advancement is preferred for simple courier paperwork because it avoids stale inventory cleanup and accidental quest-item trading.
- If a temporary carried object is needed, prefer a quest key item for documents or authorization tokens and clean it up with `OnAbandonAction` and `OnCompleteAction`. Use regular inventory UTIs only when the object must be physically submitted through `action-request-quest-items`.
- For the implemented Viscara pilot, the current item usage is sufficient: the route ledger, marker codes, manifest, cart schedule, priority list, and cipher notes stay abstract dialogue handoffs, while `visc_cache_cipher` still grants the new unique reward item `visc_kara_sig`.

Sanity anchors from current quest definitions:

- XP rewards are direct unallocated XP. Credits have Social and guild scaling; XP does not.
- CZ-220 tutorial quests mostly pay 200-1,000 XP, with one large collection quest at 2,500 XP.
- Tatooine pays 1,750 XP for ordinary local quests and 7,500 XP for the 150-kill Tusken capstone.
- Dantooine pays 600 XP for its repeatable herb quest, 2,000-5,000 XP for ordinary local quests, and 12,000 XP for `hidden_cave`.
- Hutlar pays 800-1,300 XP for ordinary local quests and 15,000 XP for `break_the_byysk`.
- Korriban's questline ranges from 1,500-10,000 XP, which is a useful upper-mid benchmark for dangerous story chains.
- Viscara's typical local rewards are 2,000-8,000 XP, with 12,000 XP for a major combat quest and one exceptional 25,000 XP recipe quest.

Major quest reward overrides:

- Viscara: `visc_cache_cipher`, `visc_fleshleader_report`, `visc_jedi_records`, `visc_republic_shortfall`.
- Mon Cala: `mon_leader_beacon`, `mon_cave_rescue`, `mon_echo_survey`, `mon_hunter_jaws`, `mon_coralisle_beacons`.
- Tatooine: `tat_worm_vibrations`, `tat_rancor_spoor`, `tat_tusken_elite_orders`, `tat_ancient_worm_tooth`, `tat_moseisley_signals`.
- Dantooine: `dan_queen_tracks`, `dan_deserter_notes`, `dan_smuggler_manifest`, `dan_dantari_rites`, `dan_mountain_crystals`.
- Dathomir: `dath_dark_adept_signs`, `dath_rancor_spoor`, `dath_boss_trophies`, `dath_ruin_base_keys`, `dath_rancor_bone`.
- Hutlar: `hut_broodmother_clutch`, `hut_chieftain_challenge`, `hut_champion_scars`, `hut_black_ledger`, `hut_clone_logs`, `hut_broodmother_shell`.

Capstone quest reward overrides:

- Viscara: `visc_signal_mountain`.
- Mon Cala: `mon_tidewatch_rounds`.
- Tatooine: `tat_ancient_husk`.
- Dantooine: `dan_colony_circuit`.
- Dathomir: `dath_dark_adept_relic`, `dath_weathered_tablets`.
- Hutlar: `hut_outpost_last_shift`.

Valuable unique item rewards:

Harder one-time quests should sometimes include a valuable item reward in addition to XP and credits. These rewards should be new quest-exclusive UTIs, not existing vendor, loot-table, recipe, map, crystal, or access-key resrefs. Existing items may be cloned as implementation templates, but the rewarded item must use the new resref below and must not be added to normal shops, loot tables, or repeatable quest rewards. Keep one copy per completion and set the item value high enough to feel meaningful, with capstone rewards targeting about 20,000-30,000 base value and major hard quests targeting about 8,000-15,000 base value unless the reward is deliberately plot-bound.

| Planet | Quest ID | New item reward | Reward type | Why it fits |
|---|---|---|---|---|
| Viscara | `visc_signal_mountain` | Veles Signal Core (`visc_sig_core`) | Unique engineering artifact | A planet-wide signal capstone should leave the player with the recovered core, not a common schematic. |
| Viscara | `visc_cache_cipher` | Ka'ra Cache Signet (`visc_kara_sig`) | Unique trophy or access token | The cipher can reveal a Mandalorian signet with prestige value without reusing the existing Fort Ka'ra key. |
| Viscara | `visc_jedi_records` | Veles Jedi Datacron (`visc_jedi_dat`) | Unique lore artifact | Jedi records should reward a named datacron rather than another sourced saber blueprint. |
| Mon Cala | `mon_tidewatch_rounds` | Tidewatch Lens (`mon_tide_lens`) | Unique sensor artifact | The tidewatch capstone can produce a lens calibrated to Mon Cala currents and low-light survey work. |
| Mon Cala | `mon_leader_beacon` | Dac Beacon Core (`mon_beac_core`) | Unique communications component | Beacon recovery should pay out the recovered core as a rare component. |
| Mon Cala | `mon_hunter_jaws` | Hunter's Jaw Charm (`mon_jaw_charm`) | Unique trophy | A dangerous predator hunt should create a named trophy rather than recycling a generic material. |
| Tatooine | `tat_ancient_husk` | Ancient Husk Core (`tat_husk_core`) | Unique relic component | The ancient husk should yield a one-off relic core from the excavation. |
| Tatooine | `tat_rancor_spoor` | Rancor Tracker's Spur (`tat_rancor_sp`) | Unique tracker trophy | The rancor trail can reward a distinctive survivalist keepsake with real sell value. |
| Tatooine | `tat_tusken_elite_orders` | Tusken Elite Blade (`tat_tusk_blade`) | Unique melee weapon | Elite Tusken orders should culminate in a named blade, not a standard vibroblade recipe. |
| Dantooine | `dan_colony_circuit` | Colony Circuit Datapad (`dan_col_datapad`) | Unique survey archive | A full colony circuit should produce a complete survey archive as the capstone item. |
| Dantooine | `dan_queen_tracks` | Kinrath Queen Chitin (`dan_queen_chit`) | Unique trophy material | The queen hunt should reward a named chitin plate from the kill. |
| Dantooine | `dan_mountain_crystals` | Dantooine Crystal Focus (`dan_mtn_focus`) | Unique crystal artifact | Mountain crystal work should leave the player with a distinctive focus instead of sourced crystal loot. |
| Dathomir | `dath_dark_adept_relic` | Dark Adept Relic (`dath_adept_rel`) | Unique Force relic | A dark adept relic quest should pay out the named relic itself. |
| Dathomir | `dath_weathered_tablets` | Weathered Tablet Fragment (`dath_tab_frag`) | Unique lore artifact | Ancient tablets should produce a quest-exclusive fragment with collector value. |
| Dathomir | `dath_boss_trophies` | Witchlands Trophy Fang (`dath_boss_fang`) | Unique trophy | Boss trophy collection should end with a named Dathomir trophy item. |
| Hutlar | `hut_outpost_last_shift` | Last Shift Badge (`hut_last_badge`) | Unique outpost keepsake | The outpost's final shift should produce a commemorative badge, not an existing ship blueprint. |
| Hutlar | `hut_champion_scars` | Champion's Scar Marker (`hut_champ_mark`) | Unique combat trophy | Champion combat scars should result in a named trophy marker. |
| Hutlar | `hut_clone_logs` | Clone Log Cipher (`hut_clone_chip`) | Unique science artifact | Clone research logs should reward the cipher chip that makes the recovered data valuable. |
| Hutlar | `hut_broodmother_shell` | Broodmother Carapace Plate (`hut_broodplate`) | Unique trophy material | A broodmother shell reward should be a unique carapace plate instead of a sourced plating blueprint. |

## Dialogue And Player Options

Every planned quest must have a dialogue brief before implementation. For ordinary NPC quest offers, use legacy `.dlg.json` conversations wired through the quest snippets. Use `action-accept-quest`, `action-advance-quest`, and `action-request-quest-items` for state changes, and use `condition-has-quest`, `condition-on-quest-state`, and `condition-completed-quest` for branch gating. Use C# dialog definitions only for logic-heavy dynamic menus.

### Existing Dialogue Continuity Audit

Before writing or implementing any new quest dialogue, inspect existing `.dlg.json` text for the same planet, hub, faction, enemy group, and reward concept. New dialogue may expand local problems, but it must not overwrite, prematurely resolve, or contradict already established NPC claims.

Minimum dialogue files to review before each planet batch:

| Planet | Existing dialogue to review | Continuity constraints for new quests |
|---|---|---|
| Viscara | `crystal`, `szaansynko`, `spy1`, `spy2`, `talgarmeyne`, `orlando_doon`, `irene_colsstaad`, `harry_mason`, `reid_coxxion`, `roy_moss`, `denam_reyholm`, `tristan_talyron`, `jhoren_veles`, `veles_shelbquest`, `cz_receptionist`, `rep_grantdialog`, `veles_colonist*` | Preserve the tension that Czerka publicly claims to protect Veles while refugees and spies allege exploitation. Do not make Szaan knowingly malicious unless a quest explicitly reveals it. Tal'gar is Czerka Security Manager and already sends the player toward the Mandalorian facility; new Mandalorian quests must not pretend the facility is undiscovered after that chain. Coxxion is a small smuggling organization under Reid and Denam, with Tristan feeding them Czerka intel. Jhoren already frames Viscara's hidden power source as cave-bound and dangerous. |
| Mon Cala | `lu_shang`, `mon_p3dr0oilpit`, `fishmaster`, `mcdce_reception` | Preserve Lu Shang's fishing-rod progression and do not duplicate rod rewards. P3DR0 is building The Oil Pit party room and speaks in an artificial unit style; new civic or survey work should make Mon Cala broader than fishing without saying fishing is absent or unimportant. Dac City content should stay civic, hospitality, research, salvage, or environmental in tone. |
| Tatooine | `tat_czerka`, `tat_militmurder`, `tat_womp`, `tat_smug` | Preserve Anchorhead's suspended Czerka mining operations due to Tusken control near the mines. Tusken work can escalate from raiders to elite orders, but new dialogue should not say the Tusken problem is already solved unless gated after the existing Tusken quests. Womprats are a local nuisance with hides bought by locals. Rancor references already exist, so rancor-related quests should read as dangerous rumors or rare threats, not a brand-new discovery. |
| Dantooine | `dantherbs`, `dan_hayquest`, `dan_questmilk`, `dan_medisupp`, `danttrainer`, `dantlibjedi`, `dantkinrath`, `daninfo`, `dan_thuneconvo`, `docjoe` | Preserve the colony's farming, medical, and research needs: Dantooine Starwort, Yot Beans, blue milk, hay bales, Kinrath pressure, thune danger, and medical shortages. Jedi-adjacent NPCs may say "May the Force be with you," but new quests should not imply an active Jedi institution has fully returned unless implemented as a deliberate story escalation. |
| Dathomir | Viscara references in `visc_spicesteph`, `veles_mentorsina`; Hunters Guild Dathomir tasks in `HuntersGuildQuestDefinition` | There is no dedicated Dathomir local quest dialogue yet. Preserve existing outside knowledge: Dathomir is distant, dangerous, resource-bearing, and rumored to have shiny shards from larger creatures. New Dathomir dialogue may define local NPC perspectives, but should not invalidate Hunters Guild targets such as Dragon Turtles, Kwi, Purbole, Shear Mites, Sprantal, Squellbugs, Ssurians, and Swampland Bugs. |
| Hutlar | `cyylan_forevia`, `voryx_ooang`, `rorrska_buvvien`, `kieun_xorxca`, `guylan_verruchi`, `hut_sharbyskbrek`, `tohnden_dahkson` | Preserve Hutlar as a cold, proud planet recently opened to off-worlders, with resentment toward that policy and Hutlar City currently closed to off-worlders. New quests should not send players into Hutlar City unless the implementation explicitly changes access. Off-worlder distrust is common but not universal. Byysk attack caravans, steal equipment, and have caused personal losses; Qion Tigers and slugs are established tundra threats. Guylan's power-structure sequence is southeast, central, north, southwest, northwest and should not be contradicted. |

Continuity check rules:

- For every new NPC, record whether they know the facts above, misunderstand them, or are lying about them. Contradictions are allowed only when framed as an in-world lie, rumor, or faction bias and not as objective journal truth.
- Any quest that references an existing NPC, faction, place, or unresolved outcome must use prerequisites or dialogue conditions so it only appears at a valid point in the existing story.
- Do not reuse existing named NPCs as quest givers unless their current dialogue and quest state support the new work. Prefer new NPCs placed nearby when continuity is ambiguous.
- If new dialogue claims a local problem is solved, make sure it is gated behind the relevant existing quest completion and does not conflict with repeatable quests that continue to present the problem as ongoing.
- Before implementation, search `Module/dlg`, `Module/utc`, quest definitions, and ambient hub dialogue for the quest's key nouns: NPC name, enemy group, faction, item reward, target area, and any named event.

Pilot correction from existing-content audit: the first implemented Viscara slice must avoid adding local quests that duplicate existing Viscara story quests or Hunters Guild kill and collect coverage. Do not use kath hound, waroca, gimpassa, kinrath, or basic Mandalorian kill/tag loops for this pilot. Use low-reward dialogue, logistics, and investigation state advancement instead, with Mandalorian references gated behind established Fort Ka'ra progress.

Required dialogue states for every quest:

| State | NPC line purpose | Required player options | Snippet or condition expectation |
|---|---|---|---|
| Locked or prerequisite missing | Explain why the NPC will not offer the job yet, naming the prior local quest or trust condition in-world. | `I'll come back later.` | Gate with completed-prerequisite conditions where possible; rely on `PrerequisiteQuest` in the quest definition as the final guard. |
| Initial greeting | Establish the NPC's role, local problem, target area, and why the player is being asked. | `What needs doing?`; `I'm just passing through.` | No quest action yet. |
| Offer details | State the objective, danger, target area, and reward category in 2-4 concise sentences. | `I'll handle it.`; `Where should I start?`; `Not right now.` | `I'll handle it.` runs `action-accept-quest`. The clarification option returns to a short location or enemy hint. |
| Accepted response | Confirm the task, repeat the target area, and give one memorable clue or warning. | `I'm on it.` | No additional action after accept. |
| In progress | Remind the player of the exact objective and location without restating the whole offer. | `Remind me where to go.`; `I'm still working on it.` | Gate with `condition-has-quest` and `condition-on-quest-state`. |
| Ready to turn in | Acknowledge that the player has what is needed or has completed the kill/interaction state. | `Here's what I found.`; `The work is done.` | Use `action-advance-quest` or `action-request-quest-items` as appropriate. |
| Completion | Pay off the local story consequence and name the reward in-world when a unique item is granted. | `Glad to help.` | Final state advancement and rewards resolve here. |
| Completed one-time | Recognize past help and point to local aftermath without offering the quest again. | `Good luck out here.` | Gate with `condition-completed-quest`. |
| Repeatable reset | Offer a short, utilitarian re-run line for bounties, patrols, samples, or supply work. | `I can do another run.`; `Not now.` | Only for `.IsRepeatable()` quests. Repeatables do not get unique permanent items. |

Player option writing rules:

- Options should be short first-person choices, not UI labels. Use `I'll handle it.`, `Where should I start?`, `I'm still working on it.`, and `Not right now.` instead of `Accept Quest` or `Decline`.
- Every major or capstone quest gets one clarification option that reveals a useful hint, such as target behavior, last known location, environmental hazard, or why the unique reward matters.
- Do not offer fake moral branches unless the quest has implementation support for different outcomes. Flavor questions are fine; alternate conclusions need code and journal support.
- NPCs should mention the reward only in-world. For example: `Bring the signal core back intact and I can sign over the recovered housing to you.` Avoid mechanical phrasing like `You will receive 12000 XP.`
- Journal text remains objective-forward and concrete; dialogue carries personality, urgency, and local consequence.

Planet dialogue tone:

| Planet | NPC voice | Quest tone | Avoid |
|---|---|---|---|
| Viscara | Frontier officials, faction scouts, technicians, and settlers who are direct but politically aware. | Practical danger, faction pressure, missing people, broken infrastructure, Mandalorian and Jedi traces. | Do not make every NPC sound like a soldier; Viscara needs civilians, scouts, medics, and worried operators too. |
| Mon Cala | Civic coordinators, fishers, salvage divers, marine researchers, and security wardens. | Survey work, tide hazards, ecological tension, beacon maintenance, underwater salvage. | Avoid making Mon Cala only a fishing planet; civic and environmental stakes should be visible. |
| Tatooine | Czerka contractors, militia scouts, moisture farmers, scavengers, and desert guides. | Dry contracts, survival warnings, Tusken pressure, predator trails, salvage from old ruins. | Avoid overusing comic desert banter; most NPCs should sound tired, practical, and risk-aware. |
| Dantooine | Farmers, healers, militia, colonists, Jedi-adjacent scholars, and field surveyors. | Pastoral colony problems turning dangerous: caves, Kinrath, Dantari, crystal fields, abandoned sites. | Avoid making all Dantooine quests peaceful errands; the later chains should show the wilderness pushing back. |
| Dathomir | Wary scouts, ruin researchers, stranded operators, occult witnesses, and grim hunters. | Threat, ritual, ruins, dangerous wildlife, dark-side residue, survival under pressure. | Avoid cute or casual tone. Dathomir should feel hostile even when the task is simple. |
| Hutlar | Outpost engineers, cold-weather scouts, clone researchers, guards, and exhausted logistics staff. | Systems failing in the cold, Byysk pressure, hive activity, missing shifts, research fallout. | Avoid turning every Hutlar NPC into a quest board. They should sound cold, short on resources, and worried about the next failure. |

Representative hard-quest dialogue briefs:

| Quest ID | Offer line | Key player options | Completion line |
|---|---|---|---|
| `visc_signal_mountain` | `The mountain relay is still repeating old Veles traffic, but something underneath it is answering back. I need the signal core pulled before it wakes half the frontier.` | `I'll recover the core.`; `What is answering back?`; `Not right now.` | `That's the core. Scorched, but intact. Keep the housing; anyone who can bring that down from the mountain has earned more than a receipt.` |
| `mon_tidewatch_rounds` | `The tidewatch posts are reading different currents from the same water. Either the instruments are lying, or something large is moving between them.` | `I'll make the rounds.`; `Which post failed first?`; `I need time to prepare.` | `These readings line up now. Take the lens assembly; it held calibration through the worst of it, and I would rather it stay with someone who can use it.` |
| `tat_ancient_husk` | `Scavengers found a metal husk under the dune shelf. Then they stopped answering. If you go in, bring back the core and do not trust anything still humming.` | `I'll search the husk.`; `What happened to the scavengers?`; `Not my job today.` | `That's old tech, older than the claim tags on it. Keep the core. Czerka will only lock it in a crate and pretend they discovered it first.` |
| `dan_colony_circuit` | `Our outer markers are blind, and the colony map is starting to lie to us. Walk the circuit, pull each datapoint, and come back if the fields are still where we left them.` | `I'll run the circuit.`; `Which marker worries you most?`; `Later.` | `This fills every gap in the colony map. Take the compiled datapad; if you crossed that whole circuit, you should have the clean copy.` |
| `dath_dark_adept_relic` | `Something in the grotto is drawing heat out of the stones. The last scout called it an adept's relic before the signal cut. I need proof, not another ghost story.` | `I'll bring back the relic.`; `What did the scout hear?`; `Dathomir can keep its ghosts.` | `That is no campfire charm. Wrap it, seal it, and keep it away from sleepers. You earned the right to decide what happens to it next.` |
| `hut_outpost_last_shift` | `The last shift logged a clean handoff, then every monitor went white. Find their badge trail, pull the final log, and tell me whether I am sending rescue or recovery.` | `I'll follow the badge trail.`; `Where did the signal end?`; `I need warmer gear first.` | `That badge was still transmitting under the ice. Keep it. Around here, remembering the last shift is part of keeping the next one alive.` |

## Implementation Shape

- Build one planet at a time, validating after each planet.
- Keep quest IDs lower snake case and planet-prefixed.
- Use one private method per quest in the planet quest definition.
- Use the existing builder objectives first: kill objectives, collect item objectives, and state advancement through dialogue snippets.
- Create new key items, collect items, placeables, or NPC groups only where the current repo has no appropriate reusable artifact.
- Treat every planned quest giver as a new fixed NPC unless implementation inventory finds an existing placed NPC that is a better owner.
- Use one-time quests for local story progression and repeatable quests for bounties, supply runs, samples, and patrol tasks.
- Planned additions by planet: Viscara 30, Mon Cala 46, Tatooine 49, Dantooine 44, Dathomir 52, and Hutlar 47.
- Before implementing any mini-arc, complete the existing-dialogue continuity audit for the planet and record any established facts the mini-arc touches.
- After the continuity audit, expand each quest row into a dialogue brief with NPC motive, offer line, accept option, clarification option, decline option, in-progress reminder, ready-to-turn-in line, completion line, completed/repeat line, prerequisite line, and reward explanation.

## Naming Contract For Implementation

Each row below implies these implementation artifacts:

- Quest definition method named from the quest title, with the `quest_id` listed in the table.
- Giver UTC, tag, and dialogue resref derived from the quest ID, shortened as needed to satisfy NWN resource constraints.
- Dialogue matrix for not eligible, offer, accepted, in-progress, ready, completed, and repeatable states, using the required player option style in the dialogue standards section.
- Journal state 1 names the target area and objective. Final state names the return NPC.
- Fixed placement in the listed `area_resref`.
- Reward block selected from the planet difficulty matrix, with a written reason when using major, capstone, or unique item rewards.
- Continuity note listing any existing dialogue, NPC, faction, area, or quest outcome that the new dialogue references or must avoid contradicting.

## Planned New NPC Groups

Add these only after confirming no existing `NPCGroupType` entry and `QUEST_NPC_GROUP_ID` wiring already fits.

- Tatooine: Tusken Elite, Sand Worm, Baby Sarlacc, Tatooine Rancor, Ancient Sand Worm.
- Dantooine: Kinrath, Dantari Hunter, Smuggler, Graul.
- Dathomir: Gaping Spider, Dark Adept, Rancor.
- Hutlar: Byysk Shaman, Byysk Chieftain, Byysk Champion, Qion Hive Tunneler, Qion Broodmother.

## Viscara Batch

| # | Quest ID | Quest Name | Giver NPC | Placement Area | Objective Plan | Repeatable |
|---|---|---|---|---|---|---|
| 1 | visc_colony_ledgers | Colony Ledgers | Mara Veyne | veles_exterior | Collect lost colony ledgers from Veles Sewers. | no |
| 2 | visc_sewer_grates | Under the Grates | Brel Narsk | veles_sheriff | Kill Viscara outlaws in Veles Sewers. | yes |
| 3 | visc_generator_splice | Generator Splice | Ivo Rennik | veles_cz_tower | Collect fuse cells from Czerka Archives and return to Veles. | no |
| 4 | visc_swamp_mold | The Mold That Bites | Helna Quist | veles_genstore | Collect swamp mold from Eastern Swamplands. | yes |
| 5 | visc_cold_trail | The Cold Trail | Sheriff Dorran Vale | veles_sheriff | Use three tracking markers in Viscara Wildlands. | no |
| 6 | visc_lake_survey | Lake Survey | Arin Pell | viscaralake | Collect water samples around Viscara Lake. | no |
| 7 | visc_route_ledger | Route Ledger | Lysa Harn | veles_exterior | Take Lysa's route ledger to Fen Dral and return with route corrections. | no |
| 8 | visc_marker_codes | Marker Codes | Fen Dral | veles_cantina | Bring Fen's marker code sheet to Tavia Orell and return with runner notes. | no |
| 9 | visc_runner_manifest | Runner Manifest | Tavia Orell | velesinterior | Ask Sella Morn for field kit counts and return the manifest update to Tavia. | no |
| 10 | visc_burrow_survey | Burrow Survey | Nold Bren | viscarawildwoods | Ask Lysa Harn for the next heavy cart schedule and return it to Nold. | no |
| 11 | visc_field_dressings | Field Dressings | Sella Morn | veles_shops | Ask Tavia Orell which runner routes should receive first field dressings and return the priority list. | no |
| 12 | visc_cache_cipher | Cache Cipher | Jorren Kade | veles_exterior | After Captain N'guth is found and marker codes are established, have Fen decode Jorren's Mandalorian cache marks and return for the signet. | no |
| 13 | visc_ranger_tags | Ranger Tags | Orla Senn | veles_exterior | Collect ranger tags from Mandalorian Rangers. | no |
| 14 | visc_deepwoods_courier | Deepwoods Courier | Petyr Rane | velesinterior | Activate courier beacons in the Deepwoods. | no |
| 15 | visc_swamp_burners | Swamp Burners | Ged Marko | viscaranswamp | Kill Vellen Flesheaters in the swamplands. | yes |
| 16 | visc_fleshleader_report | Fleshleader Report | Kala Ordo | v_cox_base | Kill a Vellen Fleshleader and return its orders. | no |
| 17 | visc_raivor_ridge | Raivor Ridge | Enna Vor | viscaradeepmount | Kill Deep Mountain Raivors. | yes |
| 18 | visc_spider_venom | Crystal Spider Venom | Dr. Reni Soth | veles_cz_tower | Collect venom from Crystal Spiders. | yes |
| 19 | visc_lake_prisms | Lake Prisms | Olan Treth | viscara_lakegrou | Recover prism fragments around Lake Grounds. | no |
| 20 | visc_jedi_records | Records in the Roots | Sera Vaal | viscara_jedigrou | Inspect damaged Jedi record stones. | no |
| 21 | visc_archive_keys | Archive Keys | Paxon Mire | viscara_archive | Recover Czerka archive keys. | no |
| 22 | visc_garden_soil | Rest Garden Soil | Mena Rest | velesrestgarden | Collect soil samples from Rest's Public Gardens and Lake Grounds. | no |
| 23 | visc_manifest_gap | The Manifest Gap | Corel Ith | velesinterior | Recover passenger manifests from Veles Starport. | no |
| 24 | visc_merchant_escort | Merchant Escort | Varro Bex | veles_shops | Escort route by activating markers between Veles and Wildwoods. | no |
| 25 | visc_republic_shortfall | Republic Shortfall | Lt. Nara Pell | v_repubbase_ext | Collect supply crates from Wildlands wreckage. | no |
| 26 | visc_coxxion_rumors | Coxxion Rumors | Halen Vox | veles_cantina | Speak to three informants and return to Halen. | no |
| 27 | visc_hidden_relay | Hidden Relay | Tessa Kord | viscaradeepwo001 | Repair a hidden comm relay in the Deepwoods. | no |
| 28 | visc_nashtah_watch | Nashtah Watch | Rell Torvik | viscarawildwest | Kill Nashtah in Mountain Valley. | yes |
| 29 | visc_scout_maps | Scout Maps | Vera Odain | veles_exterior | Recover scout maps from Mandalorian Scouts. | no |
| 30 | visc_signal_mountain | Signal on the Mountain | Kiran Sol | viscaradeepmount | Use signal equipment after clearing Raivors. | no |

## Mon Cala Batch

| # | Quest ID | Quest Name | Giver NPC | Placement Area | Objective Plan | Repeatable |
|---|---|---|---|---|---|---|
| 1 | mon_pump_pressure | Pump Pressure | Ithal Merr | moncaladaccitysu | Inspect pump terminals on Dac City Surface. | no |
| 2 | mon_hotel_provisions | Hotel Provisions | Neti Vaash | moncaladaccityex | Deliver provisions to the Elite Hotel kitchen. | no |
| 3 | mon_coral_markers | Coral Markers | Sulo Renn | moncalacoralisle | Calibrate markers in the Coral Isles. | no |
| 4 | mon_viper_antidote | Viper Antidote | Dr. Kelles | moncaladaccityex | Collect viper venom sacs. | yes |
| 5 | mon_aradile_shells | Aradile Shell Study | Tanis Voro | moncalacorali001 | Collect aradile shell chips. | yes |
| 6 | mon_hydrus_samples | Hydrus Samples | Pello Maark | moncalacoralisle | Collect Amphi-Hydrus tissue samples. | no |
| 7 | mon_reef_courier | Reef Courier | Jossi Pell | moncaladaccitysu | Deliver sealed messages between Dac City and Coral Isles. | no |
| 8 | mon_manifesto_recovery | Manifesto Recovery | Captain Orbel | moncaladaccitysu | Recover eco-terrorist manifestos. | no |
| 9 | mon_eco_ration_line | Ration Line Defense | Nura Selk | moncalacifacilit | Kill eco-terrorists near the facility. | yes |
| 10 | mon_leader_beacon | Leader Beacon | Inspector Varesh | moncalacifacilit | Defeat an eco-terrorist leader and recover its beacon. | no |
| 11 | mon_sensor_grid | Sensor Grid | Boro Pannik | moncalacifacilit | Repair submerged sensor nodes. | no |
| 12 | mon_pressure_valves | Pressure Valves | Yessa Tor | moncaladaccitysu | Collect pressure seals and repair valve boxes. | no |
| 13 | mon_swamp_bloom | Swamp Bloom | Reva Lonn | moncala_swamp | Collect Sunkenhedge bloom samples. | no |
| 14 | mon_octotench_ink | Octotench Ink | Chorr Das | moncala_swamp | Collect octotench ink sacs. | yes |
| 15 | mon_microtench_migration | Microtench Migration | Hek Tal | moncaladungeon1 | Scan microtench dens in the caverns. | no |
| 16 | mon_scorchellus_marks | Scorchellus Marks | Pelu Qarr | moncala_swamp | Collect scorchellus burn marks and tissue. | yes |
| 17 | mon_jungle_waterpath | Jungle Waterpath | Sian Voro | moncalajungelsu | Map the southern Sharptooth Jungle waterpath. | no |
| 18 | mon_cave_rescue | Cave Rescue | Lora Finn | moncaladaccitysu | Locate a missing diver in Sharptooth Jungle Caves. | no |
| 19 | mon_coral_nursery | Coral Nursery Defense | Nurra Pell | moncalacoralisle | Clear threats around the coral nursery. | no |
| 20 | mon_hotel_entertainment | Entertainment Contract | P3DR1 | moncaladaccityex | Recover entertainment equipment for the Elite Hotel. | no |
| 21 | mon_surface_lights | Surface Lights | Jalen Voss | moncaladaccitysu | Repair safety lights on Dac City Surface. | no |
| 22 | mon_memorial_tags | Memorial Tags | Ora Tannis | moncalacorali001 | Recover memorial tags from Coral Isles Outer. | no |
| 23 | mon_swamp_dredge | Swamp Dredge | Cavi Rol | moncala_swamp | Collect dredge samples from Sunkenhedge Swamps. | no |
| 24 | mon_echo_survey | Echo Survey | Bem Oss | moncaladungeon1 | Place echo beacons in Sharptooth Jungle Caves. | no |
| 25 | mon_aquaculture_sabotage | Aquaculture Sabotage | Foreman Ven | moncalacifacilit | Kill eco-terrorists and recover sabotage parts. | yes |
| 26 | mon_customs_crates | Customs Crates | Jaro Minn | moncaladaccitysu | Recover misplaced customs crates. | no |
| 27 | mon_seaweed_contract | Seaweed Contract | Pell Shenn | moncaladaccityex | Gather seaweed bundles from Coral Isles. | no |
| 28 | mon_reef_medrun | Reef Medrun | Dr. Siva | moncaladaccitysu | Deliver medpacs to a reef survey team. | no |
| 29 | mon_corrosion_checks | Corrosion Checks | Katha Noll | moncaladaccitysu | Inspect corrosion points around Dac City Surface. | no |
| 30 | mon_hunter_jaws | Hunter Jaws | Bess Olan | moncalajungelsu | Collect predator jaw trophies from jungle threats. | no |
| 31 | mon_surface_customs | Surface Customs | Customs Officer Ruun | moncaladaccitysu | Recover customs stamps from misplaced cargo lockers. | no |
| 32 | mon_facility_airlocks | Facility Airlocks | Airlock Tech Vesh | moncalacifacilit | Inspect and repair Coral Isles facility airlocks. | no |
| 33 | mon_coral_gardeners | Coral Gardeners | Keeper Nima | moncalacoralisle | Collect coral clipping samples for reef restoration. | no |
| 34 | mon_viper_den | Viper Den | Ranger Pello | moncalacorali001 | Clear vipers from a Coral Isles Outer nesting path. | yes |
| 35 | mon_lifeguard_shifts | Lifeguard Shifts | Watcher Della | moncaladaccitysu | Visit watch points around Dac City Surface. | no |
| 36 | mon_diplomatic_seals | Diplomatic Seals | Envoy Varo | moncaladaccityex | Recover diplomatic seals from the Elite Hotel service wing. | no |
| 37 | mon_sunken_cables | Sunken Cables | Cablehand Reth | moncala_swamp | Recover sunken cable bundles from the swamps. | no |
| 38 | mon_octotench_nests | Octotench Nests | Biologist Ora | moncala_swamp | Clear octotench nests and collect nest fibers. | yes |
| 39 | mon_sharptooth_maps | Sharptooth Maps | Scout Jalen | moncalawildjungl | Map safe paths in Sharptooth Jungle North. | no |
| 40 | mon_jungle_pressure | Jungle Pressure | Patrol Lead Oss | moncalajungelsu | Cull jungle predators near the southern trail. | yes |
| 41 | mon_hotel_shortwave | Hotel Shortwave | Signal Clerk Nessa | moncaladaccityex | Repair shortwave relays in the Elite Hotel. | no |
| 42 | mon_coralisle_beacons | Coral Isle Beacons | Beacon Tech Hesh | moncalacoralisle | Activate navigation beacons across Coral Isles Inner. | no |
| 43 | mon_reef_plaque | Reef Plaque | Historian Bel | moncalacorali001 | Recover broken dedication plaques from Coral Isles Outer. | no |
| 44 | mon_civic_filters | Civic Filters | Civic Engineer Dova | moncaladaccitysu | Replace filter cartridges in Dac City infrastructure. | no |
| 45 | mon_swamp_medicine | Swamp Medicine | Dr. Hala | moncala_swamp | Gather medicinal swamp algae and microtench samples. | yes |
| 46 | mon_tidewatch_rounds | Tidewatch Rounds | Tidewatcher Pell | moncaladaccitysu | Complete tidewatch rounds and report abnormal readings. | no |

## Tatooine Batch

| # | Quest ID | Quest Name | Giver NPC | Placement Area | Objective Plan | Repeatable |
|---|---|---|---|---|---|---|
| 1 | tat_docking_manifest | Docking Manifest | Dockhand Ral | tat_anc_astropor | Recover missing docking manifests in Anchorhead. | no |
| 2 | tat_water_debt | Water Debt | Vessa Marr | tat_anc_cantina | Collect water chits from Anchorhead residents. | no |
| 3 | tat_droid_coolant | Droid Coolant | HX-44 | tat_anc_droidshp | Collect coolant canisters for the droid shop. | no |
| 4 | tat_womprat_cellar | Womprat Cellar | Hader Gelt | tat_anc_southdis | Collect womprat hides from nearby tunnels. | yes |
| 5 | tat_sandswimmer_sightings | Sandswimmer Sightings | Kel Dravos | tat_anc_northdis | Kill sandswimmers in the dunes. | yes |
| 6 | tat_beetle_plates | Beetle Plates | Mera Vepp | tat_anc_verpexba | Collect sand beetle plates. | no |
| 7 | tat_sand_demon_marks | Sand Demon Marks | Orlo Pehn | tat_anc_cantina | Kill sand demons and recover marked stones. | yes |
| 8 | tat_boundary_raiders | Boundary Raiders | Lt. Brask | tat_anc_northdis | Kill Tusken Raiders near the boundary. | yes |
| 9 | tat_tent_map | The Tent Map | Sena Vor | tat_anc_tuskntnt | Recover a map from the Tusken Raider Tent. | no |
| 10 | tat_cave_scouts | Cave Scouts | Renn Var | tat_tuskcavemain | Clear Tusken scouts from the cave main floor. | no |
| 11 | tat_krayt_listening | Krayt Listening Post | Davin Orel | tat_anc_rockdess | Place listening devices in rocky desert. | no |
| 12 | tat_sarlacc_teeth | Sarlacc Teeth | Greevo Nask | tat_babysarlacc | Collect baby sarlacc teeth. | no |
| 13 | tat_worm_vibrations | Worm Vibrations | Prof. Hal Marr | tat_wormden | Calibrate vibration stakes in the Worm Den. | no |
| 14 | tat_jawa_repair | Broken Jawa Machine | Jawa Foreman Jik | tat_brokenjawa | Recover droid parts to repair the machine. | no |
| 15 | tat_bazaar_ledgers | Bazaar Ledgers | Pera Konn | tat_anc_verpexba | Recover misplaced Verpex Bazaar ledgers. | no |
| 16 | tat_gocorp_probe | Go-Corp Probe | Lonn Secura | tat_anc_gocorpst | Deploy probe hardware near Go-Corp Station. | no |
| 17 | tat_mine_claims | Mine Claims | Hask Bren | tat_anc_nminecli | Mark claim stakes along North Mine Cliffs. | no |
| 18 | tat_signal_mirrors | Signal Mirrors | Miri Voss | tat_anc_rckpass1 | Align signal mirrors through Rocky Pass. | no |
| 19 | tat_moisture_valves | Moisture Valves | Ola Dav | tat_anc_hillydes | Repair moisture valves in the Hilly Desert. | no |
| 20 | tat_tochee_parcels | Tochee Parcels | Daro Pell | tat_tocheemain | Deliver parcels from Anchorhead to Tochee. | no |
| 21 | tat_moseisley_beacons | Road to Mos Eisley | Captain Set | tat_tomoseisley1 | Activate road beacons toward Mos Eisley. | no |
| 22 | tat_elevagii_seed | Elevagii Seed Run | Farmer Keth | tat_elevagiifarm | Recover seed crates from the dunes. | no |
| 23 | tat_rancor_spoor | Rancor Spoor | Hunter Jass | tat_rancorcave | Collect spoor samples from the Rancor Cave. | no |
| 24 | tat_palace_ledger | Palace Ledger | Salli Qor | tat_smeskspalace | Recover a spice ledger near Smesk's Palace. | no |
| 25 | tat_bounty_marks | Bounty Marks | Militia Clerk Edro | tat_anc_northdis | Collect bounty marks from Tusken Raiders. | yes |
| 26 | tat_motivator_run | Motivator Run | D4-KL | tat_anc_droidshp | Collect droid motivators from desert wreckage. | no |
| 27 | tat_medcenter_delivery | Medcenter Delivery | Dr. Saal | tat_anc_medical | Deliver emergency supplies across Anchorhead. | no |
| 28 | tat_southern_caravan | Southern Caravan | Orrin Bel | tat_anc_southent | Mark caravan stones through Southern Pass. | no |
| 29 | tat_sarlacc_mucus | Sarlacc Mucus | Bovo Greel | tat_babysarlacc | Collect baby sarlacc mucus. | yes |
| 30 | tat_ancient_husk | Ancient Husk | Old Varin | tat_wormden | Recover ancient worm husk fragments. | no |
| 31 | tat_chasm_markers | Chasm Markers | Surveyor Tekk | tat_chasmpass | Place survey markers through Chasm Pass. | no |
| 32 | tat_northern_dune_bones | Northern Dune Bones | Bonepicker Jass | tat_anc_nthdunes | Recover bleached bones from Northern Dunes. | no |
| 33 | tat_flatland_compass | Flatland Compass | Scout Pava | tat_anc_flatlnd1 | Recover compass parts from the Flatlands. | no |
| 34 | tat_tusken_elite_orders | Tusken Elite Orders | Militia Captain Vos | tat_anc_northdis | Defeat Tusken Elite and recover orders. | no |
| 35 | tat_sand_worm_castings | Sand Worm Castings | Prof. Ulren | tat_wormden | Collect sand worm castings from the Worm Den. | yes |
| 36 | tat_astroport_stowaways | Astroport Stowaways | Dockmaster Venn | tat_anc_astropor | Find stowaway caches in Anchorhead Astroport. | no |
| 37 | tat_junix_tabs | Junix's Tabs | Junix Clerk Bera | tat_anc_junix | Collect overdue tabs from Anchorhead patrons. | no |
| 38 | tat_dune_weather_vanes | Dune Weather Vanes | Weatherhand Lor | tat_anc_aridhill | Repair weather vanes in the Arid Hilly Desert. | no |
| 39 | tat_cantina_debtbook | Cantina Debtbook | Bartender Ree | tat_anc_cantina | Recover a stolen debtbook from local thieves. | no |
| 40 | tat_jawa_power_core | Jawa Power Core | Jawa Tech Neb | tat_brokenjawa | Recover a replacement power core from desert scrap. | no |
| 41 | tat_smesk_watchlist | Smesk Watchlist | Salli Qor | tat_smeskspalace | Recover names from palace informants. | no |
| 42 | tat_southpass_signs | Southern Pass Signs | Road Warden Mell | tat_anc_southpas | Repair route signs through Southern Pass. | no |
| 43 | tat_rocky_pass_raiders | Rocky Pass Raiders | Hunter Doma | tat_rockypasslge | Clear Tusken Raiders from Rocky Pass. | yes |
| 44 | tat_ancient_worm_tooth | Ancient Worm Tooth | Old Varin | tat_wormden | Recover a tooth from an ancient sand worm. | no |
| 45 | tat_droid_tuneup | Droid Tune-Up | HX-44 | tat_anc_droidshp | Collect tune-up parts from desert wreckage. | yes |
| 46 | tat_medic_saline | Saline Shortage | Dr. Saal | tat_anc_medical | Recover saline packs from caravan debris. | no |
| 47 | tat_sarlacc_stings | Sarlacc Stings | Greevo Nask | tat_babysarlacc | Collect stinging barbs from the Baby Sarlacc Cave. | no |
| 48 | tat_moseisley_signals | Mos Eisley Signals | Captain Set | tat_tomoseisley1 | Restore relay flags on the road to Mos Eisley. | no |
| 49 | tat_beetle_plate_order | Beetle Plate Order | Mera Vepp | tat_anc_verpexba | Collect a standing order of sand beetle plates. | yes |

## Dantooine Batch

| # | Quest ID | Quest Name | Giver NPC | Placement Area | Objective Plan | Repeatable |
|---|---|---|---|---|---|---|
| 1 | dan_well_filters | Well Filters | Mella Rusk | dan_colony | Collect well filters from Colony South Farms. | no |
| 2 | dan_thune_drive | Thune Drive | Farmer Willen | dan_colonyfarms | Kill Plains Thune near the farms. | yes |
| 3 | dan_iriaz_census | Iriaz Census | Toma Pell | dan_iriazfarm | Scan Iriaz herds. | no |
| 4 | dan_gizka_infestation | Gizka Infestation | Beki Lorn | dan_colony | Kill Gizka near the colony. | yes |
| 5 | dan_kolto_cache | Kolto Cache | Nurse Orva | dan_medical | Recover kolto from the Abandoned Warehouse. | no |
| 6 | dan_triage_supplies | Triage Supplies | Dr. Jenso | dan_repubmed | Collect medi supplies for Republic Med Center. | yes |
| 7 | dan_crystal_harmonics | Crystal Harmonics | Vesa Noll | dan_crystalflied | Tune crystal resonators in the field. | no |
| 8 | dan_cave_shards | Cave Shards | Orren Vale | dan_crystalcavez | Collect crystal shards from the canyon caves. | no |
| 9 | dan_lizard_eggs | Lizard Eggs | Jason Marr | dan_jantacaves | Collect Voritor Lizard eggs. | no |
| 10 | dan_lower_echoes | Lower Echoes | Pella Senn | dan_kathden | Place echo beacons in lower Janta caves. | no |
| 11 | dan_kinrath_venom | Kinrath Venom | Hira Vos | dan_kinrathcave | Collect kinrath venom glands. | yes |
| 12 | dan_queen_tracks | Queen Tracks | Joran Vel | dan_kinrathcave | Track the Kinrath Queen through cave signs. | no |
| 13 | dan_archive_folios | Archive Folios | Archivist Bess | dan_jedlibrary | Recover archive folios from nearby ruins. | no |
| 14 | dan_relic_scans | Relic Scans | Jedi Librarian Arel | dan_jedienlibry | Scan relics in the Jedi Enclave Library. | no |
| 15 | dan_fallen_markers | Fallen Markers | Padawan Eno | dan_jedienclave | Restore fallen enclave markers. | no |
| 16 | dan_deserter_notes | Deserter Notes | Sgt. Venn | dan_repgarrison | Recover notes around the Republic Garrison. | no |
| 17 | dan_med_convoy | Med Convoy | Lt. Porra | dan_repinside | Recover convoy crates from the field trail. | no |
| 18 | dan_smuggler_manifest | Smuggler Manifest | Nila Voss | dan_smugcaverns | Recover smuggler manifests from the caverns. | no |
| 19 | dan_lake_reeds | Lake Reeds | Sel Owin | dan_lakencave | Collect lake reed samples. | yes |
| 20 | dan_lake_pressure | Lake Pressure | Forester Daan | dan_lakencave | Kill Kinraths around the lake. | no |
| 21 | dan_bol_tracks | Bol Tracks | Hunter Oric | dan_wildplain | Track and kill Bol in the Wild Plains. | no |
| 22 | dan_herd_pressure | Herd Pressure | Iraz Keeper Talli | dan_iriazfarm | Cull aggressive Iriaz and scan herd markers. | no |
| 23 | dan_dantari_rites | Dantari Rites | Scout Harlan | dan_tribefields | Recover rite tokens from Dantari Shamans. | no |
| 24 | dan_hunter_patrol | Hunter Patrol | Ranger Elvo | dan_tribefields | Kill Dantari Hunters in South Fields. | yes |
| 25 | dan_rope_anchors | Rope Anchors | Climber Sesk | dan_junglemount | Set rope anchors in Jungle Mountain. | no |
| 26 | dan_hay_recovery | Hay Recovery | Wrrl Fen | dan_destroyfarm | Recover hay bales from Ruined Farmlands. | no |
| 27 | dan_field_beacons | Field Beacons | Road Warden Pava | dan_fieldtrail | Activate beacon stones on the Field Trail. | no |
| 28 | dan_mineral_samples | Mineral Samples | Geologist Ren | dan_enclosemount | Collect mineral samples from Enclosed Mountain. | no |
| 29 | dan_hidden_pack | Hidden Pack | Scout Vori | dan_hiddenmount | Recover a lost ranger pack on Hidden Trail. | no |
| 30 | dan_spa_herbs | Spa Herbs | Healer Mave | dan_colonyspa | Collect herbs for Colony Spa treatments. | yes |
| 31 | dan_clear_jungle_patrol | Clear Jungle Patrol | Ranger Nessa | dan_playerland2 | Patrol Clear Jungles and mark safe paths. | no |
| 32 | dan_tranquil_plain_marks | Tranquil Plain Marks | Cartographer Ivo | dan_playerlands | Place survey marks through Tranquil Plains. | no |
| 33 | dan_crafter_base_order | Crafter Base Order | Foreman Pell | dan_crafterbase | Recover misplaced crafter requisitions. | no |
| 34 | dan_battle_gym_feed | Battle Gym Feed | Trainer Olan | dan_battlemon | Collect feed bundles for Battle Monster Gym. | yes |
| 35 | dan_jungle_spores | Jungle Spores | Botanist Hala | dan_jungle1 | Gather spore samples from Forsaken Jungles. | yes |
| 36 | dan_mountain_crystals | Mountain Crystals | Climber Sesk | dan_mountcrycave | Recover mountain crystal shards. | no |
| 37 | dan_smuggler_maps | Smuggler Maps | Nila Voss | dan_smugcaverns | Recover map cases from Smuggler Caverns. | no |
| 38 | dan_republic_ammo | Republic Ammo | Sgt. Venn | dan_repgarrison | Recover ammunition crates for Republic Garrison. | no |
| 39 | dan_bol_warning | Bol Warning | Hunter Oric | dan_wildplain | Cull Bol and place warning markers in Wild Plains. | yes |
| 40 | dan_dantari_charms | Dantari Charms | Scout Harlan | dan_tribefields | Collect charms from Dantari forces. | no |
| 41 | dan_kinrath_eggs | Kinrath Eggs | Hira Vos | dan_kinrathcave | Collect kinrath egg clusters. | yes |
| 42 | dan_lake_fishline | Lake Fishline | Fisher Rell | dan_lakencave | Repair fishlines around the lake. | no |
| 43 | dan_warehouse_manifest | Warehouse Manifest | Clerk Mavo | dan_warehouse | Recover manifest pages in the Abandoned Warehouse. | no |
| 44 | dan_colony_circuit | Colony Circuit | Technician Lira | dan_centcolony | Inspect colony utility circuits. | no |

## Dathomir Batch

| # | Quest ID | Quest Name | Giver NPC | Placement Area | Objective Plan | Repeatable |
|---|---|---|---|---|---|---|
| 1 | dath_landing_perimeter | Landing Perimeter | Scout Nera | dath_landingpad | Clear swampland bugs near Jungle Landing. | no |
| 2 | dath_czerka_blackbox | Czerka Black Box | Agent Lohr | dath_cz_baseok | Recover a black box from the Czerka Base. | no |
| 3 | dath_shear_mite_line | Shear Mite Line | Voss Tarin | dath_tarnjungles | Kill Shear Mites in Tarnished Jungles. | yes |
| 4 | dath_bug_glands | Swampland Glands | Dr. Pell Varo | dath_landingpad | Collect swampland bug glands. | yes |
| 5 | dath_totem_recovery | Totem Recovery | Kiva Noll | dath_tribevill | Recover totems from Kwi Tribal enemies. | no |
| 6 | dath_shaman_fetishes | Shaman Fetishes | Mara Senn | dath_tribevill | Collect fetishes from Kwi Shamans. | no |
| 7 | dath_guardian_challenge | Guardian Challenge | Ulren Vos | dath_ruin_base | Defeat Kwi Guardians in the Ruin Base. | no |
| 8 | dath_cave_inscriptions | Cave Inscriptions | Scholar Anvi | dath_caveruins1 | Copy inscriptions in the Cave Ruins. | no |
| 9 | dath_purbole_hides | Purbole Hides | Tava Orell | dath_desert | Collect purbole hides. | yes |
| 10 | dath_turtle_shells | Dragon Turtle Shells | Shellwright Vek | dath_grottos | Collect dragon turtle shell fragments. | no |
| 11 | dath_desert_waterstones | Desert Waterstones | Cera Pell | dath_west_desert | Collect waterstones in Desert West Side. | no |
| 12 | dath_desert_patrol | Desert Patrol | Ranger Tov | dath_desert | Kill Kwi patrols in the desert. | no |
| 13 | dath_ssurian_cull | Ssurian Cull | Hunter Orla | dathgrottocavern | Kill Ssurians in Grotto Caverns. | yes |
| 14 | dath_squellbug_ichor | Squellbug Ichor | Chemist Navo | dath_grottos | Collect squellbug ichor. | yes |
| 15 | dath_sprantal_teeth | Sprantal Teeth | Vexa Lorn | dath_mountains | Collect Sprantal teeth in the mountains. | no |
| 16 | dath_mite_paste | Mite Paste | Pel Ordo | dath_mountcaves | Collect Shear Mite paste from Mountain Caves. | no |
| 17 | dath_ruin_residue | Ruin Residue | Seer Hala | dath_ruin_base | Scan Force residue in the Ruin Base. | no |
| 18 | dath_hidden_webs | Hidden Webs | Caver Jann | dath_hidtunnels | Collect web sacs in the Hidden Cave. | no |
| 19 | dath_chirodactyl_wings | Chirodactyl Wings | Avian Keeper Sol | dath_grottos | Collect Chirodactyl wing membranes. | no |
| 20 | dath_dark_adept_signs | Dark Adept Signs | Watcher Pell | dath_grottos | Defeat a Dark Adept and recover its signs. | no |
| 21 | dath_rancor_spoor | Rancor Spoor | Beastmaster Nesh | dath_grottos | Collect rancor spoor samples. | no |
| 22 | dath_waterfall_plates | Waterfall Plates | Lira Sen | dath_waterfallru | Recover stone plates from Waterfall Ruins. | no |
| 23 | dath_supply_caches | Landing Caches | Quartermaster Ren | dath_landingpad | Recover scattered supply caches. | no |
| 24 | dath_locked_crates | Locked Crates | Czerka Clerk Mav | dath_cz_baseok | Open and recover locked Czerka crates. | no |
| 25 | dath_jungle_scouts | Jungle Scouts | Scout Brinna | dath_tranjungl2 | Recover signs from lost scouts in Tarnished Jungles North. | no |
| 26 | dath_boss_trophies | Trophies of the Grottos | Talia Voss | dath_grottos | Collect trophies from high-danger grotto enemies. | no |
| 27 | dath_language_stones | Language Stones | Elder Sava | dath_tribevill | Collect carved language stones. | no |
| 28 | dath_weather_station | Weather Station | Tech Iren | dath_mountains | Repair a weather station in the mountains. | no |
| 29 | dath_cave_purbole_cull | Cave Purbole Cull | Daro Kess | dath_caveruins1 | Kill Purbole near the Cave Ruins. | yes |
| 30 | dath_sardine_samples | Sardine Samples | Fisher Rell | dath_landingpad | Collect Dathomir Sardine samples. | yes |
| 31 | dath_czerka_field_notes | Czerka Field Notes | Agent Lohr | dath_cz_baseok | Recover scattered Czerka field notes. | no |
| 32 | dath_landing_medkits | Landing Medkits | Medic Sera | dath_landingpad | Recover medkits lost around Jungle Landing. | no |
| 33 | dath_tarnished_roots | Tarnished Roots | Botanist Heth | dath_tarnjungles | Gather root samples from Tarnished Jungles. | yes |
| 34 | dath_north_jungle_markers | North Jungle Markers | Scout Brinna | dath_tranjungl2 | Place trail markers in Tarnished Jungles North. | no |
| 35 | dath_desert_bonefield | Desert Bonefield | Archivist Orla | dath_desert | Catalog remains in the Dathomir Desert. | no |
| 36 | dath_west_desert_compass | West Desert Compass | Ranger Tov | dath_west_desert | Recover compass stones from Desert West Side. | no |
| 37 | dath_ruin_base_keys | Ruin Base Keys | Seer Hala | dath_ruin_base | Recover ancient key fragments in Ruin Base. | no |
| 38 | dath_waterfall_echoes | Waterfall Echoes | Lira Sen | dath_waterfallru | Place echo chimes in Waterfall Ruins. | no |
| 39 | dath_mountain_anchors | Mountain Anchors | Tech Iren | dath_mountains | Set climbing anchors across the mountains. | no |
| 40 | dath_cave_ruin_guardians | Cave Ruin Guardians | Scholar Anvi | dath_caveruins1 | Defeat Kwi Guardians near the Cave Ruins. | yes |
| 41 | dath_grotto_lumens | Grotto Lumens | Chemist Navo | dath_grottos | Collect luminous fungi from the Grottos. | no |
| 42 | dath_hidden_spider_eggs | Hidden Spider Eggs | Caver Jann | dath_hidtunnels | Collect spider egg sacs in the Hidden Cave. | no |
| 43 | dath_tribal_masks | Tribal Masks | Kiva Noll | dath_tribevill | Recover ceremonial masks from the Tribe Village. | no |
| 44 | dath_shaman_ashes | Shaman Ashes | Mara Senn | dath_tribevill | Collect ritual ash from Kwi Shamans. | yes |
| 45 | dath_ssurian_bile | Ssurian Bile | Hunter Orla | dathgrottocavern | Collect Ssurian bile samples. | yes |
| 46 | dath_sprantal_spines | Sprantal Spines | Vexa Lorn | dath_mountains | Collect Sprantal spine clusters. | yes |
| 47 | dath_squellbug_chitin | Squellbug Chitin | Pel Ordo | dath_grottos | Collect squellbug chitin plates. | yes |
| 48 | dath_chirodactyl_screech | Chirodactyl Screech | Avian Keeper Sol | dath_grottos | Deploy sound recorders near Chirodactyl roosts. | no |
| 49 | dath_rancor_bone | Rancor Bone | Beastmaster Nesh | dath_grottos | Recover a rancor bone from the Grottos. | no |
| 50 | dath_dark_adept_relic | Dark Adept Relic | Watcher Pell | dath_grottos | Defeat a Dark Adept and recover a relic. | no |
| 51 | dath_fishing_camp | Fishing Camp | Fisher Rell | dath_landingpad | Recover supplies from Dathomir fishing camps. | yes |
| 52 | dath_weathered_tablets | Weathered Tablets | Elder Sava | dath_waterfallru | Recover weathered stone tablets from Waterfall Ruins. | no |

## Hutlar Batch

| # | Quest ID | Quest Name | Giver NPC | Placement Area | Objective Plan | Repeatable |
|---|---|---|---|---|---|---|
| 1 | hut_heat_exchangers | Heat Exchangers | Mara Vulk | hutlar_outpost | Repair heat exchangers in the outpost. | no |
| 2 | hut_slug_bile_run | Slug Bile Run | Moricho's Assistant Nenn | hutlar_outpost | Collect Qion Slug bile. | yes |
| 3 | hut_tiger_cull | Tiger Cull | Kieun's Scout Ora | hutlar_outpost | Kill Qion Tigers. | yes |
| 4 | hut_byysk_line | Byysk Line | Guard Rolska | hutlar_qion | Kill Byysk warriors in Qion Tundra. | yes |
| 5 | hut_valley_flags | Valley Weather Flags | Tech Siva | hutlar_valley | Plant weather flags in Qion Valley. | no |
| 6 | hut_frost_samples | Frost Samples | Dr. Pella | hutlar_frozen_wa | Collect frost samples from Frozen Wastes. | no |
| 7 | hut_frozen_caches | Frozen Caches | Quartermaster Yov | hutlar_wastes_ca | Recover Byysk caches in Frozen Caves. | no |
| 8 | hut_abandoned_logs | Abandoned Logs | Salo Benn | hutlar_smuggleba | Recover logs from the Abandoned Outpost. | no |
| 9 | hut_smuggler_crates | Smuggler Crates | Customs Agent Urr | hutlar_smuggleba | Recover smuggler crates. | no |
| 10 | hut_clone_tubes | Clone Tubes | Researcher Venn | hutlar_testsite | Collect specimen tubes from the Cloning Test Site. | no |
| 11 | hut_terminal_aftershock | Terminal Aftershock | Guylan's Aide Pavo | hutlar_outpost | Inspect damaged power terminals in Qion Tundra. | no |
| 12 | hut_tunneler_rumble | Tunneler Rumble | Miner Sava | hutlar_qion | Kill Qion Hive Tunnelers. | no |
| 13 | hut_broodmother_clutch | Broodmother Clutch | Ranger Olra | hutlar_qion | Recover clutch material from the Qion Broodmother. | no |
| 14 | hut_guardian_patrol | Guardian Patrol | Sharene's Watcher Vika | hutlar_outpost | Kill Byysk Guardians. | yes |
| 15 | hut_shaman_totems | Shaman Totems | Ritualist Henn | hutlar_wastes_ca | Collect Byysk Shaman totems. | no |
| 16 | hut_chieftain_challenge | Chieftain Challenge | Duelist Korr | hutlar_wastes_ca | Defeat a Byysk Chieftain. | no |
| 17 | hut_champion_scars | Champion Scars | Hunter Valla | hutlar_wastes_ca | Defeat a Byysk Champion and recover trophies. | no |
| 18 | hut_foothill_transmitter | Foothill Transmitter | Signal Tech Yeri | hutlar_qion | Repair a transmitter in Qion Tundra. | no |
| 19 | hut_cave_rescue | Frost Cave Rescue | Medic Rela | hutlar_outpost | Locate a missing survivor in Frozen Caves. | no |
| 20 | hut_ration_run | Ration Run | Cook Merska | hutlar_outpost | Gather ration crates around the outpost. | no |
| 21 | hut_beacon_triangulation | Beacon Triangulation | Cartographer Den | hutlar_valley | Activate three tundra beacons. | no |
| 22 | hut_antenna_parts | Antenna Parts | Engineer Lova | hutlar_valley | Collect antenna parts from Qion Valley. | no |
| 23 | hut_storm_glass | Storm Glass | Weatherhand Iks | hutlar_frozen_wa | Collect storm glass from Frozen Wastes. | no |
| 24 | hut_black_ledger | Black Ledger | Inspector Vokk | hutlar_smuggleba | Recover a black ledger from the smuggler base. | no |
| 25 | hut_clone_stabilizers | Clone Stabilizers | Lab Tech Nara | hutlar_testsite | Recover clone stabilizers. | no |
| 26 | hut_old_republic_crate | Old Republic Crate | Historian Pell | hutlar_wastes_ca | Recover an old Republic crate in Frozen Caves. | no |
| 27 | hut_heat_packs | Heat Packs | Dr. Havi | hutlar_outpost | Collect medical heat packs from scattered caches. | no |
| 28 | hut_nest_map | Nest Map | Scout Vesk | hutlar_qion | Clear slugs and tigers while mapping nests. | no |
| 29 | hut_war_drums | War Drums | Rorrska's Runner Pell | hutlar_outpost | Recover Byysk war drum pieces. | no |
| 30 | hut_long_patrol | Long Patrol | Patrol Chief Neer | hutlar_outpost | Complete a long patrol by killing Byysk and Qion beasts. | yes |
| 31 | hut_qion_icecore | Qion Icecore | Dr. Pella | hutlar_frozen_wa | Recover icecore samples from Frozen Wastes. | no |
| 32 | hut_valley_slugtrail | Valley Slugtrail | Scout Vesk | hutlar_valley | Mark Qion Slug trails through Qion Valley. | yes |
| 33 | hut_outpost_filters | Outpost Filters | Mara Vulk | hutlar_outpost | Replace clogged outpost air filters. | no |
| 34 | hut_frozen_sensors | Frozen Sensors | Weatherhand Iks | hutlar_frozen_wa | Repair frozen weather sensors. | no |
| 35 | hut_smuggler_beacons | Smuggler Beacons | Inspector Vokk | hutlar_smuggleba | Disable smuggler beacons in the Abandoned Outpost. | no |
| 36 | hut_clone_logs | Clone Logs | Researcher Venn | hutlar_testsite | Recover clone experiment logs. | no |
| 37 | hut_byysk_shaman_patrol | Byysk Shaman Patrol | Ritualist Henn | hutlar_wastes_ca | Defeat Byysk Shamans in Frozen Caves. | yes |
| 38 | hut_chieftain_banner | Chieftain Banner | Duelist Korr | hutlar_wastes_ca | Recover a Byysk Chieftain banner. | no |
| 39 | hut_champion_armor | Champion Armor | Hunter Valla | hutlar_wastes_ca | Recover Byysk Champion armor scraps. | no |
| 40 | hut_broodmother_shell | Broodmother Shell | Ranger Olra | hutlar_qion | Recover shell fragments from the Qion Broodmother. | no |
| 41 | hut_tunneler_chitin | Tunneler Chitin | Miner Sava | hutlar_qion | Collect Qion Hive Tunneler chitin. | yes |
| 42 | hut_qion_tiger_pelts | Qion Tiger Pelts | Kieun's Scout Ora | hutlar_valley | Collect Qion Tiger pelts. | yes |
| 43 | hut_slug_mucus | Slug Mucus | Moricho's Assistant Nenn | hutlar_qion | Collect Qion Slug mucus. | yes |
| 44 | hut_cave_heatlines | Cave Heatlines | Engineer Lova | hutlar_wastes_ca | Restore heatlines in Frozen Caves. | no |
| 45 | hut_valley_whiteout | Valley Whiteout | Cartographer Den | hutlar_valley | Recover survey stakes after a whiteout. | no |
| 46 | hut_testsite_cleanup | Test Site Cleanup | Lab Tech Nara | hutlar_testsite | Remove failed specimens from the Cloning Test Site. | no |
| 47 | hut_outpost_last_shift | Last Shift | Patrol Chief Neer | hutlar_outpost | Complete final perimeter checks around Hutlar Outpost. | no |

## Validation Plan

For each planet implementation pass:

1. Search for duplicate quest IDs, NPC tags, UTC resrefs, dialogue resrefs, collect item resrefs, and NPC group names.
2. Parse every touched `Module/dlg`, `Module/utc`, and `Module/git` JSON file with `ConvertFrom-Json`.
3. Verify each quest dialogue has accept, in-progress, item-request if needed, advance, completed, and prerequisite branches.
4. Verify each kill objective target has a `QUEST_NPC_GROUP_ID` path.
5. Run `dotnet build SWLOR.Game.Server\SWLOR.Game.Server.csproj --no-restore`.
6. Pack the module only if the handoff requires a refreshed module artifact.

## Design Decisions

- Quest count parity is required: each tracked planet should land at 52 local planet quests.
- Meaningful playtime is the quality bar: implementation should preserve distinct objectives, area movement, encounter variety, and narrative purpose rather than padding planets with filler rows.
- Implement each planet as NPC-led mini-arcs of roughly six to eight quests, with prerequisites inside each mini-arc but no cross-planet prerequisites.
- Keep Viscara's 30 planned quests as five mini-arcs, then size the other planets' extra parity rows as one or more additional local side arcs.
- Before implementing a full planet, build and validate one pilot mini-arc end to end: quest definition, NPC template, NPC placement, dialogue, objectives, rewards, JSON parsing, and server build.
