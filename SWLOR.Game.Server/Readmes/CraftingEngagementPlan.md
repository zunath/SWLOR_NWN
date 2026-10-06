# Crafting Engagement Implementation Plan

Keep progress, quality, durability, and crafting points (CP), and make the best next action depend on the recipe, the current workpiece, and the crafter's chosen perks. The first release combines recipe profiles, changing material conditions, distinct action roles, unique perk trees for each production skill, and clear previews of their effects. Players should learn strategies they can adapt and develop a crafting style through perk investment.

The implementation on feature/crafting-upgrade now includes the complete rules engine, recipe metadata, five profession perk trees, NUI previews, and persistent transaction recovery described below. The current-behavior section records the pre-change baseline. Numerical release targets remain provisional: the checked-in simulations are evidence for the tested policies, and player testing and economy validation still gate broad deployment.

## Implemented scope and validation

All 3,243 active recipes have explicit Sturdy, Delicate, or Calibrated metadata. The rollout setting SWLOR_CRAFTING_ROLLOUT selects Legacy, Pilot, or Full; production defaults to the 15 authored pilot recipes, while development and test environments use Full. A committed session retains its original rules and recipe reward snapshot across toggles or definition changes.

The five profession trees contain 20 distinct passive perks, three two-rank lines and one capstone per skill, costing 20 SP for a complete tree. Their effects use skill-scoped stat contributions and bounded generic rules. The Design Bible contains their prices, prerequisites, descriptions, totals, and a separately labeled material-condition calculator; historical calculator formulas remain intact. Perk artwork is delivered by the companion HAK PR.

The NUI uses session/revision-specific action IDs, authoritative costs and gains, condition forecasts, finite buff charges, applicable perk effects, reward explanations, concise help, and action history. Materials, enhancements, blueprint licenses, credits, rolled rewards, first-craft credit, and XP have persisted settlement receipts. Reward replay and inventory reservations passed six native engine tests in an isolated server; actual player-vault persistence under forced process termination remains a manual release check.

Validation artifacts are [the multi-rank policy matrix](../../design/testing/crafting-policy-results.json), [the 1,000-seed paired endgame comparison](../../design/testing/crafting-policy-endgame-results.json), and [the native crafting report](../../design/testing/crafting-engine-results.json). The policy runner uses the production evaluator, independent training/evaluation seeds, and bounded fixed-sequence searches. It never reads hidden conditions. Its two-action lookahead does not fully value Engineering's longer forecast. XP per action is a proxy, and does not establish vendor income, transferred-property value, or human crafting time.

The implementation is ready for review and a controlled playtest. Automated success does not certify the proposed 10-point adaptation advantage or the economy/throughput targets for every profile. Live NUI interaction and resizing, human timing, reward economics, actual character-vault crash recovery, and the eight-player preference gate below must be completed before broad rollout.

## Current behavior and likely causes

The [current crafting baseline audit](../../design/testing/crafting-baseline.md) now includes all 3,243 recipe definitions, 7,909 item blueprints, per-rank material-count training candidates, exact quality-first policy searches with protected completion, and equipment/enhancement allocation comparisons. At rank 50, unenhanced endgame gear averages 69.66% quality on a level-50 recipe; researched and enhanced gear reaches 99.98% full-quality probability with two tier-V enhancements loaded into the produced item. Basic Touch's rank-40 passive scaling drives its efficiency advantage. Level-51–53 targets rise sharply, and Espionage has no corresponding crafting gear or food subtypes. These findings must shape action, perk, equipment, and recipe tuning. The audit is source-based; live UI checks, playtests, market costs, and comparisons against progress-first or failure-tolerant policies remain outstanding.

The manual crafting rules live primarily in [CraftViewModel.cs](../Feature/GuiDefinition/ViewModel/CraftViewModel.cs), including initialization, action resolution, success, failure, and resource handling. [CraftDefinition.cs](../Feature/GuiDefinition/CraftDefinition.cs) supplies the NUI controls. [Craft.cs](../Service/Craft.cs) supplies recipes, requirements, research, blueprints, and enhancement property handling.

| Repository finding | Design implication |
| --- | --- |
| Recipes specify level, components, quantity, requirements, and enhancement slots, but no crafting profile or material conditions. The level chart supplies shared progress, quality, and durability targets. | Different items mostly change the size of the same problem. There is little reason to change strategy between recipes at similar difficulty. |
| Basic, Rapid, and Careful Synthesis use base progress 10/30/80, CP 0/6/10, success 90/75/50 percent, and durability cost 10. Touch actions use base quality 10/30/80, CP 3/6/10, the same success rates, and durability cost 10. | The main choice is efficiency versus a random failure. The workpiece does not create new opportunities. |
| Steady Hand guarantees the next synthesis; Muscle Memory guarantees the next touch. Veneration discounts paid synthesis actions; Waste Not reduces durability expenditure. | Repeated preparation and payoff combinations can be broadly useful. Simulation should establish which combinations actually dominate at each skill and gear level. |
| Craftsmanship and Control add the same gear contribution to each action before recipe scaling. Skill thresholds also add progress, quality, or CP bonuses. | Equipment changes action efficiency, but the additive formula can compress the difference between actions as stats rise. Changing action roles requires testing both weak and strong equipment. |
| Progress completion immediately creates the item. Quality improves enhancement transfer probability, XP, and vendor value. The food branch also uses quality for a duration bonus. | Quality must remain economically meaningful. Changing average quality or actions per craft changes progression and the economy even if item blueprints stay identical. |
| Failure has a 65 percent loss chance for each serialized component entry and selected enhancement. | Additional unpredictability carries a substantial material cost. Teach the new system through clear previews and inexpensive starter recipes before adding punitive complications. |
| Action costs and descriptions are repeated in the GUI. Careful Synthesis and Precise Touch show `[15]` while their handlers spend 10 CP. | The player needs one authoritative preview for actual cost, gain, and chance. Readability is part of the gameplay change. |

These findings support the player's complaint, but do not establish a mathematically dominant rotation. The first implementation task is to reproduce the reported rotation and compare it with alternatives.

The Design Bible's Smithery description explicitly says attributes do not affect Control, Craftsmanship, or CP. Preserve that principle. Cooking recipes use Agriculture; Espionage also has recipes using this shared process. Profile coverage must include all registered crafting skills rather than assume four isolated professions.

## Approaches considered

| Approach | Benefit | Limitation | Recommendation |
| --- | --- | --- | --- |
| Add changing conditions to the existing rules | Smallest change; gives players reasons to react. | Existing action combinations may remain best regardless of condition. Recipe identity remains weak. | Useful prototype, insufficient as the complete release. |
| Combine recipe profiles, conditions, action roles, and unique skill perks | Changes the overall strategy, the moment of execution, and the crafter's personal approach while preserving the familiar resources. | Needs a testable rules engine, perk interaction coverage, and an economy comparison. | Implement first. |
| Add multistage projects, specialist loadouts, custom item traits, and commissions | Could create long term crafting depth. | Much larger content and balance scope; can make routine production tedious. | Revisit after the first release proves enjoyable. |

## Proposed player experience

Before starting, the player sees the recipe's profile, its two short traits, the selected enhancements, applicable crafting perks, and how quality affects the result. After committing materials, the player chooses actions at their own pace. Each accepted action updates the resources and normally reveals the next material condition; explicit perk effects can preserve or replace a condition. Players can choose a safe finish or spend their remaining resources pursuing more quality.

Keep the game turn based. Conditions and opportunities advance through actions, with no reaction timer, heartbeat work, or requirement to click quickly. Retain immediate completion at maximum progress, with an obvious **Finishes the item** warning on any action that will complete it. Adding a mandatory final confirmation would create another repeated click without solving the rotation problem.

The first release adds no fifth resource bar. Its depth comes from the relationship between the four existing resources, recipe traits, available opportunities, and skill specific perks.

### Recipe profiles

Start with three reusable profiles. A profile is a mechanical recipe property, independent of its crafting skill. An electronic component and a blaster can share a profile; two Smithery items can differ.

| Profile | Proposed traits | Strategy it should encourage |
| --- | --- | --- |
| Sturdy | Synthesis actions spend about 25 percent less durability; touch gains are about 10 percent lower. | Secure completion cheaply, then decide how much CP to invest in a good finish. |
| Delicate | Touch gains are about 10 percent higher; Rapid Synthesis spends 5 additional durability. Basic and Careful Synthesis retain their normal costs. | Exploit good quality opportunities while choosing progress actions that preserve the workpiece. |
| Calibrated | Switching between a successful synthesis and a successful touch grants about 20 percent more gain on the second action. The bonus does not stack. | Balance alternating work against a valuable opportunity to repeat an action category. |

These are prototype parameters. Normalize the difficulty budgets so a profile does not simply make its items more profitable or easier than other items. Durability discounts apply only to expenditure, never to restoration, with a minimum work cost of 1. Define rounding centrally.

For Calibrated work, remember the last accepted work action category. A successful action in the opposite category receives the bonus only if the previous work action also succeeded. A failed work action breaks the chain. Support actions neither grant nor advance the bonus. This rewards alternation without allowing stacks to accumulate through repeated preparation.

Assign profiles in recipe definitions through a builder method such as `.CraftingProfile(...)`. During the pilot, unspecified recipes explicitly use the legacy rules. Before broad rollout, every active recipe must have a deliberate profile or a documented legacy exemption. Do not infer profiles from item names or hidden resref lists.

### Material conditions

Use five easily explained conditions. Their exact multipliers must be visible before the player acts.

| Condition | Prototype effect | Choice it creates |
| --- | --- | --- |
| Normal | No adjustment. | Spend resources according to the recipe and remaining budget. |
| Workable | 25 percent more progress from synthesis. | Use the opportunity for completion, or preserve room for quality first. |
| Fine | 50 percent more quality from touch. | Improve the finish now, or keep enough durability and CP to complete the item. |
| Economical | Paid actions cost 25 percent less CP, rounded up. | Buy useful preparation, repair, or expensive work at a discount. |
| Reinforced | Work actions spend 50 percent less durability, rounded up. | Take a large action safely, or pursue quality while preserving a finishing action. |

Generate a condition deck when materials are committed. Begin with Normal; an initial prototype deck contains four Normal, two Workable, two Fine, one Economical, and one Reinforced cards. Shuffle without replacement, refill as needed, and constrain consecutive Normal draws to at most three, including the opening and deck refill boundaries. Validate the generated sequence and use a known valid fallback if generation fails. This bounds opportunity droughts without adding another failure roll.

Show the current condition and the next condition; a perk can extend the forecast. Each accepted action, including support actions and failed work actions, normally advances the deck exactly once. Rejected input advances nothing. Explicit condition retention consumes a perk charge and holds the current card for one further action; the action counter and buff durations still advance. Explicit condition replacement modifies the upcoming cards without rerolling the deck. Apply these effects before rendering the updated forecast. Newly drawn conditions apply to the next action, never retroactively. The starting condition is always Normal, and setup reopening cannot reroll a committed craft.

Opportunity conditions grant advantages; the first release has no randomly inflicted quality loss or material damage event. Base completion must be possible without receiving a particular condition. An optional missed opportunity should cost efficiency, not make the recipe impossible.

The deck is a source of variation, not an anti automation guarantee. Repeated support actions must have real costs and bounded uses so fishing for a condition cannot become an infinite or optimal waiting loop.

### Action roles

Retain the familiar action names and current skill unlock thresholds where practical, but give each action a reason to exist. The following roles are required design outcomes; final efficiencies and costs come from the simulation milestone.

| Existing action | Proposed role |
| --- | --- |
| Basic Synthesis | Guaranteed modest progress for no CP. The reliable way to reserve a finish. |
| Rapid Synthesis | More progress per action with an explicit failure risk and greater durability expenditure. Useful under favorable conditions, not mandatory. |
| Careful Synthesis | Guaranteed progress with reduced durability expenditure and a higher CP cost. A resource alternative rather than the strongest gamble. |
| Basic Touch | Guaranteed modest quality at low CP cost. Remains useful after higher ranks unlock. |
| Standard Touch | More quality in one action at a larger resource cost. Useful when spending a short quality opportunity. |
| Precise Touch | Guaranteed quality using less durability; gains additional efficiency under Fine. Its higher CP cost creates a tradeoff with Basic and Standard Touch. |
| Master's Mend | Spend CP to restore durability. Preview the actual restoration after the maximum durability cap. |
| Steady Hand | Guarantee the next Rapid Synthesis within two accepted actions. Limit to two uses per craft in the prototype. |
| Muscle Memory | Amplify the next touch within two accepted actions, replacing the guarantee that reliable touches no longer need. Limit to two uses per craft. |
| Veneration | Discount the next two paid synthesis actions within three accepted actions. Limit to one use per craft. |
| Waste Not | Reduce expenditure on the next two work actions within three accepted actions. Limit to one use per craft. |

Expired preparation should be visible and predictable. The action that activates a buff does not consume its newly created duration. Existing buffs age on subsequent accepted actions; a relevant attempted action consumes its charge even when that attempt fails. Reapplying a live buff is rejected rather than silently spending resources for no benefit. Keep the current skill based passive bonuses during the initial extraction and explicitly account for them during balance tuning.

Calculate action gain from a shared stat based gain multiplied by the action's efficiency, then apply profile and condition modifiers. Normalize the shared gain against legacy results before selecting efficiencies. This lets equipment help every action while preserving their different roles. Put the order of operations, rounding, floors, and discount stacking in one rules implementation. Start with a combined 50 percent maximum expenditure discount; clamp expenditure to zero or above and never convert a discount into CP or durability generation.

Support actions should compete with useful work for an opportunity. For example, preparing Muscle Memory during Fine uses that turn's condition without earning quality. The forecast lets the player decide whether preparation now will improve a later payoff. If simulations show the same preparation sequence remains optimal across conditions, change its cost, duration, or payoff before shipping.

### Example decisions

These are illustrative situations, not results from the current game or a completed simulator.

| Workpiece state | Plausible decisions |
| --- | --- |
| A Calibrated blaster has 60 percent progress, 40 percent quality, 20 durability, and 18 CP. The last work action was a successful synthesis; the current condition is Fine and the next is Reinforced. | A touch benefits from both the condition and alternation. Precise Touch preserves durability; Standard Touch spends more durability for its immediate payoff. The player must still reserve a finishing action. |
| The same blaster instead has Workable now and Economical next. | Synthesis can secure progress now, but repeating synthesis forfeits the alternation benefit. A touch can preserve that benefit, while a later discounted mend may create room for more quality. |
| A Delicate meal has almost enough progress to finish and little durability left. | Complete a usable item safely, or spend CP on a repair to pursue a better result. The interface shows the quality reward and the resource cost of that choice. |

The goal is several defensible choices. A condition that always demands one designated button is another rotation with an extra instruction.

## Unique crafting skill perks

Skill specific perks are part of the first release and its balance model. Each production skill receives three ranked perk lines and one capstone, with mechanically different benefits. A character's Smithery investment affects Smithery recipes; it does not grant the same bonuses to Engineering or cooking. Recipe profiles remain independent of skill, so a Sturdy item still feels different when made through a different discipline.

Use the existing perk purchase system and SP pool. Start with rank I/II unlocks at skill 5/20 for the first line, 15/35 for the second, and 25/45 for the third; the capstone unlocks at 50. Prototype prices are 2 SP for each first rank, another 3 SP for each second rank, and 5 SP for the capstone: 20 SP to complete a new discipline tree. Second ranks replace the first rank's effect rather than add both values. Capstones require one completed line, allowing different routes without buying the entire tree.

An action dependent perk cannot be purchased before that action unlocks. Use these gate overrides: Structural Bracing at 10/20, Precise Calibration at 35/45, Measured Construction and Efficient Preparation at 30/45, and Trigger Tuning at 30/40. Every other line follows the table order and default milestones. Verify these relationships against the final action unlock data rather than make players buy an unusable first rank.

The existing spendable cap is 410 SP, including 10 starting SP. Keep that cap and audit the new costs alongside existing profession and combat investments. Existing Engineering Droid Assembly costs 11 SP in total; Fabrication's research perks cost 26 SP in total. The new crafting lines supplement these perks. Espionage also retains its existing utility and character type requirements. Final prices and magnitudes must pass the complete build budget review.

The perk values below are prototype starting points. Caps are per craft, and every effect belongs in the effective action preview. A capstone must give its skill a distinctive decision or recovery tool. Completion at an appropriate skill and equipment level remains viable without purchasing the new tree.

### Smithery

Smithery rewards strong progress actions, recovering from risky work, and turning successful shaping into a better finish.

| Perk | Prototype rank effects | Decision it changes |
| --- | --- | --- |
| Tempered Strikes | Successful synthesis during Workable restores 2/3 durability, up to three triggers. Restore no more than the durability that action spent. | Spend a progress opportunity on aggressive work while keeping resources for finishing. |
| Resilient Work | The first failed Rapid Synthesis still grants 25/40 percent of its previewed success progress. Costs remain spent and the action still counts as a failure for chains. | Take a calculated risk with limited protection against a bad roll. |
| Finishing Work | During Reinforced, a touch immediately following a successful synthesis gains 15/25 percent more quality, up to three triggers. | Choose whether to exploit cheap work for another progress action or switch to finishing. |
| Master Smith | Once per craft, a successful Workable synthesis grants 30 percent more quality to the next touch attempted within two accepted actions. | Prepare a finish through productive shaping; decide whether to take the touch now or wait for a better condition. |

The Master Smith opportunity is a visible buff. The qualifying touch consumes it even if it fails, and normal buff expiry rules apply. Resilient Work cannot itself activate effects that require a successful synthesis.

### Engineering

Engineering rewards foresight, efficient calibration, and deliberate use of CP.

| Perk | Prototype rank effects | Decision it changes |
| --- | --- | --- |
| Circuit Economy | A successful switch between synthesis and touch restores 1/2 CP, up to three triggers, when the prior work action also succeeded. | Balance efficient alternation against repeating work under a valuable condition. |
| Precise Calibration | Precise Touch gains 15/25 percent more quality during Fine. | Invest CP in precision when the workpiece is ready for calibration. |
| Diagnostic Planning | Show two/three upcoming conditions, extending the default one condition forecast. | Plan preparation and repairs around a longer visible sequence. |
| Master Engineer | Once per craft, a successful paid work action during Economical refunds half its effective CP expenditure, capped at 6 CP. | Choose which expensive work action deserves the one substantial efficiency opportunity. |

Only restoration is capped by maximum CP; unused recovery is lost. Circuit Economy cannot be triggered through support actions or a broken work chain. Diagnostic Planning reveals the existing committed deck and gives no rerolls.

### Fabrication

Fabrication rewards bracing, maintaining the workpiece, and preserving resources through a longer assembly.

| Perk | Prototype rank effects | Decision it changes |
| --- | --- | --- |
| Structural Bracing | Master's Mend restores an additional 5/10 durability on its first two uses, subject to maximum durability. | Choose how far to let durability fall before buying a repair. |
| Reinforced Assembly | Waste Not protects one/two additional work actions and lasts one/two additional accepted actions. | Plan a productive work window rather than spend CP on repeated repairs. |
| Measured Construction | Careful Synthesis costs 1/2 less CP, up to three uses. Apply this flat reduction before percentage discounts. | Choose dependable construction that leaves CP for the finish. |
| Master Builder | Once per craft, a work action that would fail the craft by reaching zero durability instead leaves 1 durability. It grants no extra progress or quality. | Recover from an assembly setback, with enough CP still required to repair or finish. |

Master Builder resolves only after checking whether the action completed the item. It does not prevent abort, death, or disconnect settlement and cannot be rearmed through repairs. Show its available protection and the resulting 1 durability in previews.

### Agriculture and cooking

Cooking remains part of Agriculture. These perks reward careful timing, building the recipe while improving its finish, and managing favorable conditions. They do not add farming systems or restore excluded farming perks.

| Perk | Prototype rank effects | Decision it changes |
| --- | --- | --- |
| Flavor Layering | A touch during Fine that successfully increases quality also adds 10/20 percent of Basic Synthesis's unconditioned progress gain, up to three triggers. | Improve quality while advancing the recipe; monitor whether the extra progress would finish it. |
| Patient Preparation | When Master's Mend actually restores at least 10 durability during Fine, preserve Fine for the next action. One/two uses per craft. | Repair a fragile preparation while retaining a good moment to improve its finish. |
| Efficient Preparation | A successful Careful Synthesis discounts the next Basic Touch by 1/2 CP, with a minimum cost of 1 before condition discounts, up to two triggers. The opportunity expires after two accepted actions. | Use deliberate preparation to support economical quality work, or choose a different action for the current opportunity. |
| Perfect Timing | Once per craft, a successful Fine touch that reaches maximum quality also adds one Basic Synthesis's unconditioned progress gain. | Time the final quality action to make productive progress toward completion. |

Patient Preparation spends CP, restores durability, advances all action based durations, and consumes its preservation charge. It cannot trigger from repairing a full workpiece. Positive fractional progress bonuses grant at least 1 progress; clamp all gains to the recipe target and show any resulting completion before the click.

Validate the existing food quality reward mapping before enabling this tree on food recipes. The existing integer cast in the duration branch must be reconciled with its intended duration scale so the interface can explain the reward accurately. Any adjustment to that reward requires the same economy review as the perk effects.

### Espionage crafting

Poison and trap recipes use the same crafting process, so Espionage also receives a distinct crafting tree at launch. Its perks reward careful mixing and reliable assembly. Preserve Poisoncraft, Trapcraft, and Master Saboteur ownership gates and the Standard character restriction.

| Perk | Prototype rank effects | Decision it changes |
| --- | --- | --- |
| Controlled Mixing | On poison recipes, a successful Fine touch restores 2/4 durability, up to three triggers and no more than the action spent. | Use a good mixing opportunity to keep the preparation workable. |
| Trigger Tuning | On trap kit recipes, a successful Careful Synthesis reduces the next Rapid Synthesis's durability expenditure by 3/5, up to two triggers. The opportunity expires after two accepted actions. | Prepare an aggressive assembly step, while deciding whether upcoming conditions justify it. |
| Clean Assembly | During Economical, a successful paid synthesis also grants 10/20 percent of Basic Touch's unconditioned quality gain, up to three triggers. | Advance assembly efficiently while earning some quality, without making quality work unnecessary. |
| Contingency Planning | After the first failed Rapid Synthesis, replace the next two conditions with Reinforced followed by Economical. Once per craft. | Recover through cheap work and a discounted repair or preparation, with the remaining resources still limiting the options. |

Use explicit recipe technique tags for poison mixing and trap kit assembly. Trap kits currently use the general Tool category, so that category alone cannot prove that a recipe qualifies. Declare tags through `RecipeDetail`/`RecipeBuilder` and verify the complete recipe corpus; do not keep a perk specific resref list or inspect perk requirements to infer a technique at runtime. Contingency Planning consumes the original upcoming cards when replacing them and preserves the rest of the committed deck. It applies only if the failed action leaves the craft active; it cannot revive a terminal failure.

### Gathering and profession identity

Gathering is a crafting category skill but uses harvesting, scavenging, and refining rather than the manual progress/quality window. Preserve its unique existing perks, including Harvesting, Refining, Refinery Management, Treasure Hunter, and Hard Look. These make it the materials specialist in the crafting economy. Its perks and SP budget remain part of launch regression coverage; this change does not add a new materials quality system or copy production perks into Gathering.

Give each production skill several plausible purchases and routes. A Smithery crafter can favor risky shaping or better finishing; an Engineer can favor forecasts or CP efficiency; a Fabricator can favor repairs or protected work windows; a cook can favor mixed gains or condition preservation; a poison/trap crafter can favor mixing or assembly. Test purchases individually and in combinations so an entire discipline tree does not become an unexplained prerequisite for basic crafting.

## Interface and onboarding

Update the existing crafting window. Keep progress and quality prominent, put durability and CP beside the action previews, and add a compact profile description and current/next condition display. Use text as well as color to communicate conditions.

Each action preview comes from the same evaluator used for resolution and shows effective CP cost, durability expenditure or restoration, progress/quality gain, success chance, and any finishing or failure warning. A risky action shows its gain on success and its cost on failure. Disable unavailable actions with a visible reason rather than make players discover the rule through an error.

Show active buffs, remaining charges, expiry in actions, remaining uses, and applicable skill perks. Previews explain any perk adjustment, recovery, mixed progress/quality gain, or changed forecast. Keep a short expandable action history explaining gains, expenditure, failure, and condition changes. Avoid selecting a single recommended action on every turn; teach consequences without turning the interface into a follow the highlighted button exercise.

Make quality's actual rewards visible. Enhancement transfer currently rolls per property group, including paired weapon damage/type properties. At 80 percent quality, three independent groups each have an 80 percent transfer chance, while the chance of all three transferring is 51.2 percent. Do not label quality as a single whole item success chance. Preserve the existing output mapping for the pilot; expose missing or ambiguous mappings explicitly. The food duration branch currently casts the quality fraction to an integer, so it does not give a smooth intermediate bonus. Confirm its intended behavior before advertising one; repair it in a separate, balanced change if needed.

Teach the first release through accurate action previews, concise optional help, and existing inexpensive starter recipes. Explain a basic finish, a quality opportunity, and a repair decision within normal crafting. Players learn while making useful items under the normal resource and reward rules. Practice mode is outside the first release.

At skill ranks before touch unlocks, retain straightforward progress crafting and explain when quality actions become available. Later unlocks add options; they should not make every previous action obsolete.

## Implementation structure

Extract the minigame rules before adding mechanics. Keep NWN object handling, inventory transactions, and UI bindings outside the evaluator so simulations and runtime use identical rules.

| Location | Planned responsibility |
| --- | --- |
| `Service/CraftService/CraftSession.cs` | Resources, recipe/profile snapshot, stat snapshot, action count, condition deck, buff charges, prior work action, rules version, and terminal state. |
| `Service/CraftService/CraftActionEvaluator.cs` | Validate an action, produce its preview, and resolve an accepted action using an injected random source. Return a new state and explicit outcome rather than create NWN objects. |
| `Service/CraftService/CraftActionDetail.cs` and action configuration | Central action costs, efficiencies, unlocks, risks, and buff effects. GUI labels and simulation use these same values. |
| `Service/CraftService/CraftingProfileType.cs` and profile definitions | The three profiles and their modifiers. Validate unknown types, invalid multipliers, and unsupported combinations at startup. |
| `RecipeDetail.cs` and `RecipeBuilder.cs` | Explicit profile and crafting technique metadata, with legacy fallback during the pilot. Preserve recipe IDs, components, output quantities, and existing requirements. |
| `Feature/PerkDefinition/` and `Service/PerkService/PerkType.cs` | Unique Smithery, Engineering, Fabrication, Agriculture, and Espionage crafting perk definitions, ranks, prices, prerequisites, and icons. Preserve existing enum values and profession unlocks. |
| `Service/PerkService/PerkStatBonus.cs`, `PerkBuilder.cs`, and `Service/Stat.cs` | Declare and aggregate skill scoped crafting stat contributions. A builder API such as `IncreasesCraftingStat(skill, stat, amount)` supplies typed contributions; `Stat.GetCraftingStatAdjustment(player, recipe.Skill, stat)` supplies their aggregate to the session snapshot. |
| `Service/StatService/StatType.cs` and crafting rule metadata | Declare stat classification through `StatTypeAttribute`. Describe crafting triggers, action/condition filters, outcomes, durations, and use caps as data consumed by shared rule handlers. |
| `Feature/GuiDefinition/ViewModel/CraftViewModel.cs` | Session orchestration, evaluator calls, resource commitment, rendering, and finalization. Remove duplicated mathematical rules. |
| `Feature/GuiDefinition/CraftDefinition.cs` and recipe detail display | Profile, condition, buff, reward, and action previews. Derive labels and enabled state from the evaluator. |
| `Service/Craft.cs` | Continue to own recipe access, research, blueprint handling, and shared enhancement construction. Extend detail generation where needed. |
| `SWLOR.CLI/RecipeCodeBuilder.cs`, its template, and active recipe generators | Preserve profile metadata when recipes are generated. Identify every active generator before changing its output contract. |

The first release reuses NUI crafting actions and implements the new crafting perks as passive traits with explicit perk icons. Use the existing perk icon support without adding a global feat solely to host artwork. If a perk ultimately requires an actual feat backed gameplay ability, use one `*AbilityDefinition.cs` and matching `IAbilityListDefinition` per ability, following the repository rules.

Shared resolution consumes aggregated crafting stats rather than checking `PerkType` or a perk specific class. Scope each contribution before aggregation, keyed by skill and stat; globally summing bonuses and then checking one skill selector would leak or discard effects when a player owns multiple trees. Allow compatible equipment or status effects to contribute through the same stat path. Recalculate aggregates through existing purchase, refund, equipment, and status change hooks, then snapshot them for the active recipe at commitment.

Generic handlers cover gain adjustments, expenditure adjustments, resource recovery, buff opportunities, survival at zero durability, and forecast retention/replacement. Declare their eligibility and session budgets in rule metadata. Keep perk charges separate from action buff charges, award recovery after an accepted action resolves, and require actual successful work where specified. A refund cannot exceed what that action spent; fixed regeneration uses its explicit craft cap. Never grant a trigger on rejected input, and never apply another skill's crafting contributions to the session.

Keep legacy and proposed action configurations separate within the evaluator. Recipes outside the pilot retain their original action roles, gains, costs, buff semantics, and random behavior, with no new crafting perk effects. A shared GUI can still use the evaluator to show accurate costs. Do not accidentally enable the revised action roles or perk effects globally when enabling only selected recipe profiles. The complete new perk set must be available in the pilot for build testing; explain which recipes currently use the new process before a purchase.

Make the server authoritative. Validate active session, action unlock, effective cost, request session ID, and expected action count before changing state. Ignore stale or duplicate input. Snapshot gear, skill contributions, enhancement penalties, and rules version at commitment so equipment changes or XP awarded after completion cannot change the active craft's calculations.

Retain the current success ordering: a work action that reaches the progress target succeeds even if its expenditure also reaches zero durability. Mark the session terminal before granting rewards or returning materials. Finalization must execute once for item creation, XP, blueprint accounting, and cleanup. Freeze the engine choice at session start so a rollback affects new crafts only.

Centralize ending the session on success, failure, abort, death, disconnect, or tether invalidation, and always remove crafting immobilization and release resources. Preserve free cancellation before commitment; use the existing failure settlement after commitment for the pilot. Audit the current disconnect path during extraction. If it cannot reliably settle reserved materials, persist a committed transaction receipt and pending settlement before enabling the pilot; do not leave crash recovery as an untracked follow up.

## Delivery milestones

Each milestone has an exit condition. Ship the mechanics together only after they pass the pilot; an extraction can land independently with legacy behavior preserved.

| Milestone | Implementation work | Exit condition |
| --- | --- | --- |
| Establish the baseline | Capture the reported rotation and alternatives; inventory active recipes, skill/gear ranges, existing perk costs, enhancement penalties, generator ownership, and quality rewards. Record accepted actions, success, quality, resource losses, XP, and credits per material and per minute. | Reproducible legacy scenarios include starter ranks, unlock thresholds, equal level crafts, the permitted three rank deficit, overleveled crafts, and all shared crafting skills. Full profession build budgets include existing perks. |
| Extract and protect the rules | Introduce the session/evaluator and authoritative previews. Preserve legacy resolution, buff charge semantics, and output handling. Add lifecycle guards and identify transaction recovery requirements. | Fixed random rolls replay legacy results exactly, including rounding, failures, completion at zero durability, and refund accounting. GUI cost labels match resolution. |
| Build the gameplay prototype | Add the three profiles, bounded conditions, revised action roles, forecasts, and buff rules behind a rollout switch. Assign a small, explicit pilot set across disciplines. | The new process completes representative recipes; repeated action combinations have alternatives; every accepted/rejected action follows the specified condition and buff rules. |
| Implement the profession trees | Add all five production perk trees, skill scoped stat aggregation, generic perk rule handlers, perk icons, purchase/refund integration, and charge/forecast previews. Reconcile their rows and prices in the Design Bible. | Every production discipline has distinct working perks and a capstone in the pilot. Existing Gathering, research, droid, poison, and trap unlocks work. Effects remain correctly scoped across multiple trained skills. |
| Make the prototype understandable | Implement previews, condition/buff explanations, reward details, history, and concise help. Introduce the choices through inexpensive starter recipes. Use the existing NUI authoring/layout guidance and GUI skill when making the actual window changes. | A player can explain the cost and likely consequence of an action without consulting external notes. Live NUI layouts work at representative resolutions. |
| Balance and playtest | Run policy comparisons with no perks, individual lines, partial builds, and complete profession trees; investigate dominant strategies and fishing loops; playtest with new and experienced crafters. Reconcile chosen rules with the Design Bible. | The engagement, perk value, and economy gates below pass; exact action values, perk budgets, and profile assignments are documented and tested. |
| Roll out and observe | Enable the selected pilot recipes, then expand profile coverage after a complete review of the pilot results. Keep a switch for legacy rules on new sessions. | Existing recipe access, enhancements, XP limits, and outputs work; active sessions survive toggles; all enabled recipes have valid metadata. Unique skill perks ship with the new crafting process. |

A provisional planning allowance is 20 to 35 engineering days plus one to two weeks of player testing for this first release, including the unique profession trees and their stat, icon, UI, and balance work. This is a scope estimate, not a delivery commitment; the transaction recovery audit, perk interactions, and NUI iteration are the largest uncertainties. Practice mode, bulk production, and specialist projects are outside that estimate.

## Balance and validation gates

Use a finite simulation runner against the real evaluator. Compare the reported rotation, the strongest fixed sequences found through bounded search, resource conserving strategies, and several policies that react to conditions and remaining resources. Use reproducible seeds for regression and a separate seed set for evaluation. Measure expected quality across all attempts as well as quality among successful attempts, so failing low quality attempts cannot inflate a policy's apparent performance. Compare success and material losses alongside quality.

Run at least 1,000 evaluation sessions per representative profile/stat/build scenario and report uncertainty. Include no perk investment, each line separately, alternative builds at equal SP cost, and all compatible perks. Increase samples only where the result is too close to a release gate. Search for repeated support loops, chains that bypass costs, cross skill bonus leakage, and one action that dominates at both low and high stats. A policy comparison is evidence about the tested policies, not proof that the system cannot be optimized.

The following are proposed release targets to refine after the baseline milestone:

- On ordinary crafts near the player's skill level, target roughly 8 to 14 total accepted actions, including support. If the legacy craft is already shorter, preserve that speed rather than force extra steps. Compare measured completion time as well as clicks.
- In controlled scenarios, changing only the material condition should change the preferred action in at least three distinct resource situations, including a choice between useful work and support. Changing only the profile should change useful policy choices on comparable recipes.
- On nontrivial pilot scenarios where the strongest tested fixed sequence leaves quality headroom, an adaptive policy should beat it by about 10 quality percentage points in expected quality per attempt, with no more than a 2 percentage point reduction in completion rate. Overleveled or otherwise trivial crafts may reach maximum quality with a fixed sequence. If adaptation offers little value on the challenging scenarios, tune action roles and opportunity strength before adding mechanics.
- No seed should require a particular opportunity to produce a base item from a validated, adequately equipped starter scenario. Every enabled recipe needs a completion path using actions available at its permitted skill rank.
- Without new crafting perks, keep expected XP per minute, vendor credits per minute, enhancement transfers per component budget, and material loss within about 5 percent of the matched legacy scenarios. Investment in the new trees has its own reward budget; do not use the uninvested parity requirement to erase its value. As an initial ceiling, allow complete trees up to 15 percent more XP or vendor credits per minute than the matched uninvested new process, with enhancement transfer and loss improvements reviewed separately. Include existing profession perks in both comparisons and review exceptions explicitly.
- Every purchased line must change at least one useful action decision or provide a measurable benefit in its intended situation. Resource perks should demonstrably create room for another useful action in representative cases; quality perks should improve achieved quality; information perks should help players plan in controlled playtests. Require at least two useful alternative builds per production discipline at comparable SP cost. Basic completion must remain viable without new crafting perks, and no build may produce renewable CP, durability, condition holds, or survival through a repeatable loop.
- Include at least eight playtesters spanning new and experienced crafters. Ask whether choices felt meaningful, whether results felt earned, and whether they would choose to craft again. A provisional gate is at least six reporting a preference for the new process, with novice confusion and repetitive support use addressed before rollout. This is directional feedback, not statistical proof of fun.

Record server side session summaries: recipe/profile, rules version, stat bands, applicable perk ranks and SP investment, action count and sequence, condition sequence, perk triggers and remaining budgets, outcome, quality, costs, transferred property groups, XP, and duration. Use canonical identity in server audit logs. Any player facing summary uses `PlayerName`. Full step traces can be limited to development or sampled diagnostics; avoid broadcasting every crafting step into chat.

Add focused tests for the evaluator, previews, condition bounds, profile behavior, buff durations, expenditure stacking, state limits, and terminal outcomes. Perk tests cover every rank, purchase/refund refresh, skill and recipe technique scoping, mixed gains that complete an item, actual expenditure before refunds, failed action recovery without success chains, condition retention/replacement, maximum resource limits, zero durability protection, and terminal state guards. Integration tests must cover duplicate/stale clicks, component reservation/refund, enhancement serialization, licensed blueprint runs and credit expenditure, immobilization cleanup, and disconnect settlement. Corpus tests cover active recipe profiles, technique tags, perk coverage, and unlock viability.

Keep existing recipe/progression coverage, especially `CraftTests`, `RecipeLearningSourceTests`, `CombatUpgradeBibleRecipeParityTests`, and `ResearchPerkRegressionTests`. Run affected `PerkStatBonusTests`, `StatAdjustmentSourceTests`, `GatheringPerkRegressionTests`, `EspionageProgressionTests`, `CombatUpgradeBibleSyncTests`, and `PassivePerkIconTests` alongside the new coverage. Add meaningful new suites such as `CraftActionEvaluatorTests`, `CraftSessionLifecycleTests`, `CraftingProfileCoverageTests`, and `CraftingPerkBehaviorTests`; these names are proposed. Build once with deployment disabled, then run only relevant tests:

```powershell
dotnet build SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj --no-build --filter "FullyQualifiedName~CraftTests|FullyQualifiedName~CraftActionEvaluatorTests|FullyQualifiedName~CraftSessionLifecycleTests|FullyQualifiedName~CraftingProfileCoverageTests|FullyQualifiedName~CraftingPerkBehaviorTests|FullyQualifiedName~RecipeLearningSourceTests|FullyQualifiedName~CombatUpgradeBibleRecipeParityTests|FullyQualifiedName~ResearchPerkRegressionTests|FullyQualifiedName~PerkStatBonusTests|FullyQualifiedName~StatAdjustmentSourceTests|FullyQualifiedName~GatheringPerkRegressionTests|FullyQualifiedName~EspionageProgressionTests|FullyQualifiedName~CombatUpgradeBibleSyncTests|FullyQualifiedName~PassivePerkIconTests"
```

Tests and simulations cannot verify NUI layout or whether crafting feels enjoyable. Complete live module checks and player playtests before expanding rollout.

## Content compatibility and deployment

Reconcile the final rules, unique perk rows, SP totals, and recipe metadata with the relevant Smithery, Engineering, Fabrication, Agriculture, Espionage, crafting calculation, and recipe Design Bible tabs before enabling them broadly. Preserve Gathering's existing rows and role. The existing `Crafting Calc` tab includes a chance formula; do not assume it already describes the new manual evaluator. Label and reconcile its purpose when documenting the new formulas. Preserve cached formula values through surgical ZIP/XML edits, follow [DesignBibleWorkbookRules.md](DesignBibleWorkbookRules.md), then run `tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` and affected parity checks. Do not edit a workbook through a whole workbook writer that discards caches.

Preserve `RecipeType` values, unlocked recipes, research levels, crafted history, components, quantities, enhancement slots, item properties, and practice rank limits. Do not add character perk refunds or unrelated character migrations. If transaction receipts require persistent data, integrate with the applicable existing migration timing and verify recovery independently.

Crafted history currently records first completion. A comment and character sheet text mention auto crafting, but the reviewed recipe/craft window path does not expose it. Do not assume bulk production already exists or that first completion proves quality mastery.

The first release uses code, text NUI controls, and icons for the new passive perks. Read `IconStandards.md` before choosing or authoring those icons, follow the gameplay manifest and rank badge rules, and run `tools/UpdateGameplayIconStandards.ps1 -AuditOnly`. Regenerate cooldown variants if any changed resources are referenced by feat or spell rows. Rebuild/repack affected haks and the module; regenerate the TLK binary if custom strings change. Apply the existing VFX selection rules if effects are added. Publishing changes to a submodule also requires its companion PR when PRs are opened.

## Later improvements

Consider these only after the first release meets its engagement goals:

- **Practice mode:** Reconsider only if playtesting shows that players avoid experimenting with valuable recipes despite clear previews and inexpensive ways to learn. It is not required for the initial release.
- **Routine production:** A bounded, player requested batch action for recipes previously completed and comfortably below the crafter's skill. Initially restrict it to recipes without selected enhancements or meaningful quality dependent gameplay bonuses. Consume the full component and blueprint budget for every output, give no quality or first completion bonus, and validate normal output throughput against manual crafting. Existing crafted history can establish completion; a mastery record would need a separate deliberate design. This is a new feature, not assumed existing behavior.
- **Advanced recipe traits:** Add a small number of authored traits, such as a middle progress window that favors quality or a recipe that rewards saving CP for finishing. Keep them opt in for special recipes. Avoid putting every desirable quality action in the same final window, which would recreate a universal progress then quality rotation.
- **Commission projects:** Longer, authored projects with visible quality targets and clearly budgeted rewards. Use them for players seeking deeper crafting; routine components should remain quick.

## Design reference

Final Fantasy XIV's official crafting guide describes fluctuating material conditions that change quality action effectiveness, as well as condition dependent actions. It establishes these mechanics as a useful reference, not evidence that they will solve SWLOR's complaint. The recipe profiles, bounded forecasts, action roles, and release targets above are proposals for this repository. [Official crafting guide](https://na.finalfantasyxiv.com/crafting_gathering_guide/blacksmith/).
