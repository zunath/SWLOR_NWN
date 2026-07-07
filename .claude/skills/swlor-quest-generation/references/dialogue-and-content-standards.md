# SWLOR Quest Dialogue And Content Standards

Use this reference when creating or reviewing quest dialogue, player replies, journal entries, prerequisite explanations, reward framing, or quest-chain content.

## Content Framing

Before writing lines, answer these in notes or in your working context:

- Who is asking, and what is their role in the local place: farmer, receptionist, Sith instructor, guild quartermaster, frightened technician, bored officer, desperate family member.
- Why this NPC cannot or will not do the work themselves: rank, fear, injury, bureaucracy, politics, lack of skill, public optics, debt, urgency.
- What pressure makes the request matter now: failing equipment, missing people, hostile wildlife, ration trouble, guild quota, military escalation, local rumor, personal shame.
- What the player is really doing in story terms: proving competence, cleaning up someone else's mistake, earning access, buying trust, containing danger, extracting proof.
- Which local nouns should appear: area names, facility names, species, guilds, enemy names, item names, key NPCs, and current quest-chain events from the repo.

If the request does not answer these and the repo cannot answer them, ask one question at a time and include a recommended answer.

## Lore Fit

- Ground the quest in SWLOR's existing local content before broad Star Wars references. Search the target planet, area, NPC, enemy group, and item names before inventing lore.
- Use canon terms only when they serve the local scene. Do not name-drop major factions, planets, or famous characters to make ordinary work feel important.
- Keep technology and institutions in-world: datapads, terminals, refineries, clinics, guilds, patrols, shuttle passes, work receipts, keycards, experiments, supply manifests.
- Make low-level quests feel grounded. Early quests can be mundane, but the NPC should still have a specific reason and a believable attitude.
- Do not explain lore like an encyclopedia. Let the NPC reveal only what they would naturally say to get the job done.

## NPC Voice

Pick a voice model before writing:

- Professional pressure: short, clipped lines, process words, concrete deliverables.
- Rural/local pressure: practical details, weather/land/animals/supplies, guarded trust.
- Academic or Force-adjacent pressure: testing language, controlled judgment, implied consequences.
- Military/security pressure: orders, threat assessment, containment, operational urgency.
- Droid or terminal pressure: task framing, literal priorities, limited emotional range, small glitches only if already appropriate.
- Criminal or fringe pressure: leverage, suspicion, favors, coded language, payment emphasis.

Then vary the dialogue structure. Do not force every NPC through the same pattern:

- Open with an observation, interruption, accusation, warning, bargain, report, complaint, test, or unfinished thought.
- Let some NPCs withhold information until the player asks.
- Let some NPCs lead with the consequence instead of the objective.
- Let some NPCs be transactional and others personal, depending on role.
- Let player replies reflect tone: curious, skeptical, professional, reluctant, blunt. Avoid only "Yes, I'll help" and "No, goodbye."

## Conversation Flow

Quest NPCs should not feel like task boards with a face. Before writing or expanding a quest-giver conversation, sketch the intended flow and make sure the player can understand the job quickly while still having room to ask why it matters.

- For major, chain, capstone, faction, or signature quests, the offer should include optional branches before acceptance. Good branches answer questions such as: what is this technique or problem, why this target matters, where the player should go, what proof matters, what danger to expect, what local pressure caused this, or how prior quest events changed the NPC's position.
- For short, low-stakes, tutorial, or repeatable tasks, keep the flow lighter, but still give the NPC one concrete reason, pressure, or local detail so the dialogue does not read as generated filler.
- Put accept, item-request, and turn-in snippet actions on clear player replies. Put lore, tactical advice, directions, suspicion, bargaining, and optional exposition on non-action branches so players can explore them without accidentally advancing or accepting the quest.
- Let optional branches loop back to the useful choices: accept, decline, ask a different question, or continue the turn-in. Avoid dead-end lore branches that force the player to restart the conversation.
- Keep all optional branches bounded by the NPC's knowledge and the local setting. Do not use an NPC to explain broad lore, system mechanics, or future quest steps they would not reasonably reveal.

## Human-Written Pass

After drafting, revise with these checks:

- Remove repeated scaffolding such as "I need you to", "Can you help me", "traveler", "adventurer", "return when it is done", and "thank you for your help" unless the NPC's voice specifically earns it.
- Vary line length. Mix one-line answers with a few fuller lines. Avoid equal-sized paragraphs.
- Prefer specific nouns over generic labels: "maintenance level", "work receipt", "Dantooine Medical Facility", "Mynocks", "raw Veldite".
- Use contractions and fragments where the character would speak that way.
- Put exposition behind motive. An NPC should not recite history when a complaint, fear, order, or bargain would reveal enough.
- Preserve clarity for gameplay. The player must still know where to go, what to collect or kill, who to return to, and what is blocked.
- Keep stage directions rare and short. Use them only when they change how a line reads.

## Quest State Coverage

Write or verify a distinct text beat for each state that applies:

- Not eligible: explain the nearest concrete missing requirement without breaking character.
- Offer: establish motive, stakes, objective, and acceptance choice.
- Accept response: confirm the objective and point the player at the first destination or method.
- In-progress reminder: restate the actionable next step without repeating the whole offer.
- Ready to turn in: acknowledge the item, proof, kill, or state that makes completion possible.
- Completion: resolve the NPC's immediate pressure and deliver the reward framing.
- Completed non-repeatable: acknowledge prior work and avoid re-offering.
- Completed repeatable: explain why the work can repeat without sounding like a reset button.

## Prerequisites

- Use `PrerequisiteQuest(...)` and `PrerequisiteKeyItem(...)` when the builder supports the gate.
- Mention prerequisite quest names, key items, permits, receipts, or rank requirements in dialogue only when the player can act on that information.
- Do not write a vague "come back later" unless secrecy or character voice is the point. Prefer "Bring me Halron's receipt first" or "The shuttle office will not process you without a pass."
- If several prerequisites exist, guide the player toward the nearest next dependency rather than dumping a full dependency tree into one line.

## Rewards

- Match the reward family to the story reason: pay, hazard bonus, guild points, access key, recipe, proof of trust, tool, receipt, or supply cache.
- For guild work, inspect the guild definition's reward table before changing amounts.
- For chain quests, make rewards support progression without narrating system math.
- For selectable rewards, make the dialogue explain why the player is choosing rather than receiving a fixed item.
- For key item rewards, make the dialogue imply access, trust, permission, or evidence. Avoid giving permanent access items from repeatable quests unless that exact pattern already exists.

## Journal Text

- State 1 tells the player what to do after accepting.
- Intermediate states name the next objective, location, item, or return target.
- Final states tell the player who or what to return to.
- Keep journal text factual and readable. Do not use the journal to carry personality that belongs in dialogue.
- Include area and NPC names when they reduce ambiguity.

## Final Quality Gate

Before finishing, confirm:

- The NPC's first three lines do not all start with greeting/request scaffolding.
- The offer, reminder, and completion lines do not reuse the same sentence shape.
- Major, chain, capstone, faction, and signature quests have an intentional conversation flow with meaningful optional player branches, not only accept/decline/reminder/turn-in lines.
- Optional lore or tactical branches return to actionable replies and do not carry quest-advancing snippet actions unless they are explicitly the accept, item-request, or turn-in choice.
- Every prerequisite gate has a player-facing explanation somewhere in the dialogue path.
- Every reward has a plausible story reason or existing reward-scale precedent.
- The text names local targets consistently with quest definitions, item resrefs, NPC groups, and module files.
- The dialogue remains understandable when read by a player who skips flavor text quickly.
