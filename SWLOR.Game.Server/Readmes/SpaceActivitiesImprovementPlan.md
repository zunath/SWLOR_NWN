# Space Combat and Activities Improvement Plan

This proposal brings space combat, mining, exploration, salvage, transport, and crafting into one progression system. It incorporates the combat upgrade's work on skill identity, active decisions, stat-driven perks, readable effects, and complete-build balance.

The numerical baseline is now specified before gameplay implementation. The Design Bible's `Space` tabs contain the replacement skills, complete perk ranks and prices, all current hulls, module designs, calibrations, recipes, deposits, encounters, reward accounting, and conversion rules. [space-balance.json](../../design/space/space-balance.json) is the machine-readable snapshot; [UpdateSpaceDesignBible.py](../../tools/UpdateSpaceDesignBible.py) reproduces the numerical audit and preserves existing workbook formula caches.

These are concrete design values, not a claim of completed live balance. Implementation uses this baseline; movement feel, encounter readability, real market prices, and economic demand remain release-validation work. Change a value through the same complete-fit and economy review rather than selecting unspecified numbers during implementation.

## Current implementation status

The feature branch implements horizontal hull fitting and conversion, calibrated and refined craft recipes, persistent equipment and cargo ownership, finite mining and survey channels, defeat recovery, all 170 operating perk ranks and native resources, banks, preparation, modes, and the cockpit. All 51 existing NPC identities now map to eight declared encounter roles; AI uses the shared hit/range/capacitor rules and finite ordnance. Independent encounter contribution has one credit/XP ledger, suppresses legacy death rewards, and creates finite participant-owned wrecks. Recovery support consumes actual hostile-damage debt; energy support receives at most 10% of the Systems pool. Channels, journal recovery, and paid activations have focused regression coverage.

Temporary bonuses declare the hardware instances and, where required, the selected target they affect. Next-cycle effects are consumed only when the corresponding hardware operation is accepted and paid. A failed eligibility or supply check retains the effect. Permanent and temporary stat sources share the published caps; their negative tradeoffs remain. Break Away suppresses soft control slows while preserving hard control and preparation penalties. Fleet Stabilization divides each paid projector output among at most three eligible allies within both 35m and that projector's fitted range; it does not multiply one module's recovery budget. Deep Survey, Deep-Core Extraction and Specialist Recovery start their declared 15s, 20s and 25s finite channels directly. Both the technique cost and ordinary hardware cost are checked before the commitment, and the selected compatible tool pays its normal activation charge. Target Analysis shares its accuracy signal only with the declaring operator’s allies. Selective Recovery consumes actual selected reserves, so an exhausted constituent cannot be recreated.

Finite sites, durable cargo transfers, mining and survey work, bounded operating XP, fitted weapon/support activation, dock service and safe ship defeat recovery are connected. The full contract/activity rollout, starter/vendor/research integration and the persistent migration entry point still require implementation. Live NWN boot, cockpit rendering, movement feel, native inventory/payment crash recovery and multi-actor economy checks remain release validation. Offline tests and the numerical audit do not claim those engine results.

## Numerical baseline before implementation

The [Design Bible](../../design/bible/SWLOR%20Design%20Bible%20-%20Combat%20Upgrade.xlsx) carries the full specification. The principal values are:

| System | Baseline |
| --- | --- |
| Skills and spending | Five skills, maximum 50 ranks each, shared 400-rank cap and 410-SP maximum. Each specialization has 17 purchased ranks costing 40 SP. |
| Perk progression | Techniques cost 1/2/3 SP per rank. Their skill thresholds are 2/15/30, 10/25/40, and 20/35/45. Traits cost 1/2/3 at 5/20/35 and 15/30/45. Mode costs 3 at rank 25; capstone costs 7 at rank 50. |
| Cockpit actions | One-second activation cadence, up to two weapon banks, four prepared techniques including at most one capstone, one mode across all five skills. Refit preparation at dock takes five seconds. |
| Ordinary equipment | Operates at rank 0 in its responsible skill. Demanding heavy/ordnance fittings need rank 10; deep drills, strip miners, repair fields, and bombardment need 20. Manufacturing rank does not become operating rank. |
| Accessible hulls | Light Escort: Piloting 1, fitting power 34, 100 hull/90 shield, signature 35, speed 1.15. Light Freighter: Piloting 1, power 40, 140 hull/100 shield, 180 cargo units. |
| Weapon roles | Tracking laser: 12 damage/4s, 4 capacitor, 8 fitting power, tracking 110, resolution 40. Heavy beam: 56 damage/8s, 20 capacitor, 24 power, tracking 35, resolution 140. |
| Skill mastery | Gunnery and Ship Systems add 0.2% relevant output/rank; Piloting adds 0.2% base speed/rank. Industry adds 0.1 recovery percentage point/rank; Astrometrics adds 0.4 fitted scanner resolution/rank. |
| Source caps | Positive permanent damage/recovery bonuses total at most 40% of base output; highest temporary contribution at most 30%. Capacitor discounts cap at 25%; adjusted cycles cannot fall below 85% of base. |
| Craft quality | One eligible positive dimension, at most 8% refinement at quality magnitude 100. Guaranteed recipe/calibration identity is separate from the shared optional enhancement-transfer roll. Eligible ship refinement tokens use magnitude 1–100 and may tune accessible equipment without the legacy five-level enhancement cutoff. |
| Recovery and control | External hull/shield recovery capped separately at 40% of target base pool per 30s. Capacitor transfer 80% efficient, at most 90%. Hard control at most 3s; repeated family control follows full/half/immune within 20s. |
| Mining | Precision cutter removes 6 reserve units/12s at 80% recovery; bulk extractor 12/12s at 60%; deep drill 10/15s at 70%; strip miner 32/18s at 50%. Final recovery cannot exceed 95%. |
| Compact advanced industry | A compact deep-core drill removes 4 reserve units/15s at 80% recovery, takes 18 fitting power, and operates at Industry 20. It provides a difficult-deposit role for the starting freighter. |
| Rewards | Eleven full-trip activity models yield approximately 5,782–7,689 reference net credits/player-hour after supplies, service, failure, and group division. Mining material values come from actual deposit compositions and checked cargo/throughput. Operating XP caps at 18,000 base XP/active hour across skills. |
| Defeat | Persistent hull and fitted gear retained; 20% unprotected cargo loss, hull recovery 3% reference value (60–1,800 credits), and 20 service-condition points lost per fitted module. Service costs 1% module reference value per lost point. |
| Persistence | Five-second maximum dirty-state interval for hull/capacitor; ownership, cargo, claims, and reward transitions settle durably. Cockpit dirty refresh no faster than 0.5s. |

Reference income assumes the declared material prices and activity work. Material realizations are not guaranteed player-market sale prices. Ore commissions pay the listed bids for at most 1,000 units per character per day across all commodities; additional volume must find existing vendor or player demand.

The initial model checks representative fits, legal character budgets, role advantages, capacitor endurance, encounter duration, resource conservation, support termination, crafting inputs, and combined downside scenarios. The downside scenario reduces material realizations by 25%, increases trip time by 25%, and reduces success chance by 10 percentage points. It remains profitable for every reference activity.

`Space Module Variants` enumerates every allowed calibration with its exact fitting power, output, cycle, capacitor cost, range, manufacturing requirement, ingredients cost, and shared crafting targets. `Space Hull Recipes` and `Space Craft Inputs` specify hulls, ammunition, service supplies, configurations, assemblies, and refinement inputs. `Space Industry Benchmarks` checks physical production against trip time and cargo. The starting freighter can fit a Compact deep-core drill, Compact deep scanner, and Compact hull repairer at 37/40 fitting power; this fit sustains advanced environmental damage and returns 120 units within the 20-minute budget. Its compact equipment retains lower output.

| Numerical comparison | Result |
| --- | --- |
| Novice escort against starter pirate | Expected fight 29.6s; 50s of full weapon-and-repair capacitor endurance. |
| Veteran in accessible escort against advanced interceptor | Expected fight 42.8s; 5.89 DPS against the faster, attribute-focused interceptor comparison. |
| Heavy objective fit against that faster interceptor | 2.67 DPS; its large weapons preserve a small-target weakness. |
| Heavy fit against elite heavy target | Expected fight 35.8s; 23.70 DPS against the general heavy comparison. |
| Accessible escort against the general heavy comparison | 6.88 DPS; heavy-hull investment has a distinct heavy-objective payoff. |
| Ground fighter and pilot | 350 total ranks, 220 SP spent from 360 earned plus starting SP; includes a complete 60-SP ground weapon style and 90-SP other-ground reserve. |
| Industrial explorer with ground defense | 360 ranks, 210/370 SP, including 80 SP of space specializations, 60 ground weapon SP, and 70 other-ground SP. |
| Support captain and ground specialist | 330 ranks, 209/340 SP, including 49 space SP, 60 ground weapon SP, and 100 other-ground SP. |

The Bible preserves legacy records for conversion reference. Cross-references on Piloting, Starships, and Engineering direct readers to the replacement tables and their current implementation status. The conversion table maps all 26 current hulls and 143 identified legacy modules/configurations and four separately retained consumables, including obtainable recipe outputs. No gameplay implementation begins until the numerical audit, workbook parity, conversion coverage, and the existing Bible regression checks pass.

## Player-facing overview

Your first ship can remain a useful ship throughout your career. Small fighters, scouts, and freighters keep advantages in handling, operating cost, signature, and access to compact fittings. Larger ships offer different capabilities, including endurance, heavy weapons, industrial capacity, and fleet support. A higher piloting requirement means a more demanding ship to operate, rather than a ship that wins every comparison.

Space progression expands into five skills: Piloting, Gunnery, Ship Systems, Astrometrics, and Space Industry. Each has two specializations, with techniques, supporting traits, an operating mode, and a capstone. You can build a fighter pilot, prospector, explorer, support captain, or a mixed ground-and-space character without mastering all five skills.

Equipment determines what your ship can do. Perks improve your use of that equipment and unlock techniques that require suitable hardware. Engineers manufacture distinct module designs and calibrations with visible advantages and drawbacks. Better crafting can produce excellent compact fittings for an accessible ship, as well as specialized equipment for a larger vessel.

Mining becomes a choice between careful recovery, bulk extraction, and committed work on difficult deposits. Surveying reveals opportunities and risks. Salvage, patrols, delivery work, exploration, escort, and rescue provide different reasons to take a ship out. Resources and recovered components feed shipbuilding, ammunition, repairs, and refitting.

## Design commitments

- Replace the five-tier hull and module ladder with lasting hull roles and equipment sidegrades. Remove the assumption that each later resource, recipe, or module makes earlier equipment obsolete.
- Make accessible hulls useful in ordinary advanced activities. Their relevance must come from handling, signature, fitting options, efficiency, and objectives, rather than isolated beginner-only content.
- Give each space skill a clear job. A useful operator specializes in one or two skills and can invest modestly in supporting skills.
- Balance the complete fitted ship: hull, equipment, crafting enhancements, operator skills, perks, temporary effects, consumables, and allies.
- Keep manufacturing valuable. A perk cannot supply an unfitted weapon, scanner, repair projector, mining tool, or other piece of hardware.
- Make encounters and expeditions require meaningful choices while remaining workable with NWN's movement, targeting, and interface constraints.
- Protect persistent ownership, interiors, cargo, and existing crafting investment during conversion.

## Existing foundations and problems to address

The current system already has useful building blocks: persistent ships, high and low power module slots, configurations, capacitor costs, shields and hull, mining and refining, ship recipes, NPC ships, and space resource spawns. Some modules already trade one benefit for another, such as accuracy against damage or shield recharge against capacitor capacity.

The main problems are how these pieces interact:

| Current behavior | Consequence | Planned change |
| --- | --- | --- |
| Piloting contributes to several combat calculations and hosts much of space progression. | One skill carries unrelated roles and leaves little build identity. | Split progression into five skills with separate operating responsibilities. |
| Hulls, module recipes, and resources largely follow tiers. | Later progression replaces earlier ships and fittings. | Give hulls lasting roles and recipes distinct output designs. |
| Generic module enhancement bonuses affect several different outputs; configurations amplify broad bundles. | Crafting can magnify offense, defense, industry, and sustain together. | Use bounded tuning appropriate to each module family and configurations with explicit tradeoffs. |
| Mining yield, depletion, delayed strip-mining completion, and cargo handling have separate rules. | Reward efficiency and session correctness are difficult to reason about. | Define authoritative deposit reserves, recovery, cargo capacity, and extraction settlement. |
| Shared combat calculations already affect ship attacks. | Ground combat changes can alter space balance indirectly. | Declare ship context and verify complete ship builds alongside shared infrastructure changes. |
| NPC module use, movement resets, and player activation checks follow different paths. | Range, resource rules, and tactical behavior can diverge. | Use common activation validation and role-based encounter behavior. |
| Active ships are persisted frequently. | More actors and activities may add substantial database load. | Measure current load and establish a persistence policy with an explicit durability target. |

Relevant foundations are [Space.cs](../Service/Space.cs), [ShipModuleDefinition](../Feature/ShipModuleDefinition), [CombatUpgradeReleaseBalancePlan.md](CombatUpgradeReleaseBalancePlan.md), [CombatUpgradeActivePerkBudgetReview.md](CombatUpgradeActivePerkBudgetReview.md), and [CombatUpgradeReleaseValidationMatrix.md](CombatUpgradeReleaseValidationMatrix.md).

## Skills and perk structure

### Five operating skills

| Skill | Responsibility | Specialization one | Specialization two |
| --- | --- | --- | --- |
| Piloting | Maneuvering, pursuit, escape, travel, and operating demanding hulls. | Combat Pilot: interception, positioning, and evasive maneuvers. | Expedition Pilot: efficient travel, hazard handling, and escape. |
| Gunnery | Weapon handling, tracking, target selection, and offensive techniques. | Precision Gunnery: reliable pressure and exploiting openings. | Heavy Ordnance: missiles, torpedoes, and committed burst attacks. |
| Ship Systems | Power routing, defensive equipment, repair, and allied support. | Defensive Systems: self-preservation and controlled recovery. | Fleet Support: allied repairs and capacitor support. |
| Astrometrics | Survey information, navigation analysis, and electronic warfare. | Surveying: deposits, anomalies, and route information. | Electronic Warfare: target analysis and sensor disruption. |
| Space Industry | Extracting resources, recovering salvage, and handling industrial cargo. | Extraction: precision, bulk, and difficult-deposit operation. | Salvage and Logistics: component recovery, cargo handling, and freight work. |

Engineering continues to manufacture and research ship equipment. Gathering retains ground harvesting and refining responsibilities. Operators can buy or commission equipment without becoming engineers. Space Industry does not become an additional mandatory refining tax on existing ground production.

Leadership can support fleets through explicitly declared ship effects. Ground auras and ground combat perks do not automatically apply to ships. Existing space-entry restrictions and aura cleanup must remain coherent with whichever fleet effects are added.

### Skill and skill-point budgets

Use the standard maximum of 50 ranks for each operating skill. Keep these skills within the shared character budget; they do not receive a separate free pool. The current implementation allows 400 total skill ranks and 410 total skill points, comprising 400 earned points and 10 starting points. Distinguish those two limits in every build audit.

Illustrative rank allocations show the intended accessibility; these are not minimum requirements:

| Role | Example operating ranks | Total ranks |
| --- | --- | --- |
| Fighter pilot | Piloting 40, Gunnery 50, Ship Systems 10. | 100 |
| Prospector | Piloting 15, Space Industry 50, Astrometrics 20. | 85 |
| Explorer | Piloting 40, Astrometrics 50, Ship Systems 10. | 100 |
| Support captain | Piloting 20, Ship Systems 50, Astrometrics 20. | 90 |

These examples leave room for ground skills, crafting, and other interests. Before implementation, price full perk selections and publish legal mixed builds, including a pilot who primarily fights on the ground and an industrial character who buys commissioned equipment. A role must function below maximum ranks, and a supporting skill must provide useful early investment.

### What a specialization contains

Each specialization uses the following structure, with the exact rank values in `Space Perks`:

| Perk element | Function | Progression rule |
| --- | --- | --- |
| Three active techniques | Create decisions using fitted equipment or the hull's movement capabilities. | Ranked upgrades improve the same action and replace its earlier rank. |
| Two supporting traits | Improve relevant handling, efficiency, or recovery within a bounded stat budget. | Small ranked benefits; useful across compatible module designs. |
| One operating mode | Changes priorities through a sustained advantage and drawback. | One relevant mode active at a time; its cost and downside stay visible. |
| One capstone | Provides a distinctive, limited opportunity for a committed specialist. | A meaningful cooldown, resource cost, condition, or exposure prevents constant use. |

Each specialization costs 40 SP across 17 purchases. Ranked techniques and traits each cost 1/2/3 SP for I/II/III, the operating mode costs 3, and the capstone costs 7. Prerequisite ranks are listed in the numerical baseline and Bible. Buying every perk in both specializations is not necessary for a useful role.

The structure creates about five distinct technique controls per specialization before ranks. Audit the actual cockpit loadout separately: weapon banks, equipment actions, techniques, modes, and consumables all contribute to control burden. Aim for roughly four to six frequently used combat controls in an ordinary combat fit, with situational actions exposed clearly. Do not meet an ability budget by excluding module buttons from the count.

### Proposed specialization identities

Names below describe the intended gameplay and are working names. These are equipment-dependent actions, not free replacements for crafted modules.

| Specialization | Three techniques | Supporting traits | Operating mode | Capstone |
| --- | --- | --- | --- | --- |
| Combat Pilot | Intercept accelerates a pursuit; Break Away helps disengagement; Evasive Maneuver creates a short avoidance window. | Maneuver Handling and Pursuit Efficiency. | Pursuit favors closing and tracking at a defensive or power cost. | Ace Maneuver provides a brief exceptional movement opportunity with recovery afterward. |
| Expedition Pilot | Efficient Transit improves a travel leg; Hazard Run helps cross a surveyed danger; Emergency Escape creates a limited escape opportunity. | Navigation Economy and Hazard Handling. | Cruise favors transit efficiency while reducing combat readiness. | Safe Passage improves one committed hazardous leg without bypassing encounter objectives. |
| Precision Gunnery | Controlled Burst concentrates a weapon bank; Tracking Solution helps engage a difficult target; Exploit Opening rewards a visible target vulnerability. | Weapon Handling and Firing Efficiency. | Precision favors accuracy or tracking over peak throughput. | Perfect Solution provides a short, paid window of reliable fire, without automatic hits against every target. |
| Heavy Ordnance | Prepared Volley commits ammunition; Torpedo Run rewards a suitable approach; Bombardment engages a suitable heavy objective. | Ordnance Handling and Ammunition Economy. | Siege favors heavy-target output while exposing the ship or reducing mobility. | Coordinated Salvo creates a strong telegraphed burst with ammunition, power, and recovery costs. |
| Defensive Systems | Emergency Repair accelerates a fitted repairer; Power Routing redistributes available power; Shield Recovery improves a fitted defensive recovery cycle. | Recovery Efficiency and Power Discipline. | Defensive Routing favors survival at an offensive cost. | Damage Control provides a bounded recovery window using the ship's installed equipment and supplies. |
| Fleet Support | Repair Link operates an allied repair projector; Capacitor Transfer sends actual available energy; Support Surge temporarily improves a fitted support bank. | Projector Handling and Transfer Efficiency. | Support Routing favors allied service at a personal combat cost. | Fleet Stabilization creates a target-limited support window with a defined total recovery budget. |
| Surveying | Deposit Analysis reveals useful material detail; Anomaly Scan resolves an exploration opportunity; Route Survey identifies a local navigation opportunity. | Survey Resolution and Scan Economy. | Detailed Survey favors information depth over scan speed. | Deep Survey reveals an unusually valuable opportunity through appropriate sensors and a committed scan. |
| Electronic Warfare | Target Analysis exposes relevant defenses; Sensor Disruption interferes through fitted hardware; Countermeasure Timing improves a fitted defensive countermeasure. | Sensor Handling and Interference Efficiency. | Interference favors control at a power or signature cost. | Signal Break creates a brief control opening within immunity and stacking rules. |
| Extraction | Precision Extraction favors recovery; Extraction Surge favors throughput; Selective Recovery concentrates work on a surveyed material. | Beam Handling and Industrial Efficiency. | Careful Extraction reduces speed to improve usable recovery. | Deep-Core Extraction works a difficult reserve using a suitable drill and a committed extraction cycle. |
| Salvage and Logistics | Careful Dismantling protects components; Recovery Sweep handles bulk wreckage; Cargo Handling improves a legitimate loading operation. | Component Recovery and Freight Efficiency. | Recovery Operations favors careful salvage at a throughput cost. | Specialist Recovery accesses a difficult component with suitable tools, without guaranteeing rare loot. |

The Bible specifies each technique's hardware, target, magnitude and duration, additional capacitor expenditure, and cooldown. Rank upgrades replace earlier effects. Interruption follows the channel/session rules; a spent technique is not refunded by a refit, target change, or canceled channel. Scanning reveals information; it does not create ore. Power routing redistributes constrained resources; it does not create energy. Cargo handling changes an operation or bounded capacity; it does not authorize unlimited containers.

An extraction stabilizer, repair projector, targeting array, or scanner remains a module. If a technique improves stabilization, projection, or targeting, the corresponding equipment must be fitted. Basic module operation remains useful without purchasing an entire specialization.

### Earning experience

Award experience for meaningful contributions to the skill's job: completed maneuver objectives for Piloting, effective combat for Gunnery, legitimate recovery and support for Ship Systems, new survey information for Astrometrics, and actual extraction or salvage for Space Industry.

Use encounter and activity contribution budgets. Flying in circles, rescanning an unchanged object, repairing deliberately self-inflicted damage, and circulating capacitor between allies must not produce unlimited experience. A mission should not automatically grant full experience in all five skills. Later crew stations use the responsible operator's skill, with bounded team benefits rather than adding every crew member's full modifiers to the same fit.

## Hull roles and fitting

### Lasting roles

Assign every current hull a role and conversion destination before removing tier requirements. Preserve recognizable identities where possible.

| Hull role | Lasting strengths | Limitations and useful advanced work |
| --- | --- | --- |
| Light fighter | Handling, interception, small signature, and economical deployment. | Limited cargo and endurance; valuable for screening, pursuit, and attacking exposed objectives. |
| Light freighter | Flexible fitting, useful cargo, and manageable operating costs. | Less specialized combat output; valuable for independent hauling, exploration, and mixed expeditions. |
| Scout | Survey access, navigation, escape, and low signature. | Limited sustained fighting; valuable for finding deposits, identifying threats, and supporting a fleet. |
| Bomber | Ordnance capacity and pressure against large or fixed targets. | Vulnerable during committed attack windows; needs protection against interceptors. |
| Industrial ship | Bulk handling, extraction endurance, and industrial equipment. | Exposure and poor combat flexibility; benefits from route planning and escorts. |
| Capital ship | Heavy fittings, endurance, support capacity, and large objectives. | Cost, signature, handling, and crew demands; smaller ships must retain important jobs alongside it. |

Low skill requirements mean accessible operation. Mastery improves useful handling and techniques on that same hull. Advanced compact modules let an experienced operator invest in a small ship without fitting a capital weapon into a fighter.

### Fitting constraints

Start with slot counts, compatible mount sizes, and one overall fitting-power budget. Use capacitor for operational expenditure. Add another fitting resource only if a prototype demonstrates a distinct decision that these constraints cannot represent.

Every module declares its compatible mounts, fitting cost, activation cost, and operating characteristics. Compact, standard, and heavy describe compatibility and behavior, not quality tiers. Configurations become focused conversions with a clear drawback, such as a survey conversion that consumes combat fitting capacity or a cargo conversion that reduces defensive space.

Tracking and target signature should help distinguish weapons and hulls. Heavy ordnance should be inefficient against evasive small ships; light weapons should not match heavy-objective performance. Prototype this within the actual NWN ship representation before depending on facing, collision, or directional firing. Position can improve an outcome without making unreliable engine movement a universal prerequisite.

## Ship modules and crafting

### Responsibilities and balance

| Layer | Determines | Balance boundary |
| --- | --- | --- |
| Hull | Handling, signature, base endurance, cargo envelope, mounts, and fitting power. | Has role weaknesses that equipment cannot erase completely. |
| Module design | The capability, baseline output, targeting, cycle, range, and resource demands. | A visible strength carries a meaningful opportunity cost or drawback. |
| Operator skills and perks | Handling, bounded efficiency, modes, and equipment-dependent techniques. | Do not multiply every equipment output or provide absent hardware. |
| Engineering and crafting | Which designs can be produced and the resulting enhancements or calibration. | Preserve design tradeoffs and the value of commissioned equipment. |
| Temporary effects and allies | Short tactical opportunities and coordinated support. | Separate budgets, explicit stacking rules, finite resources, and recovery windows. |

Replace the universal module bonus with typed tuning appropriate to the module family. A weapon enhancement might improve tracking or output; a scanner might improve resolution or cycle; a repairer might improve recovery or efficiency. One quality increase should not improve all of those dimensions together, and a configuration should not multiply a universal bonus across the ship.

For example, 100 base output with an 8 percent crafted refinement and a 10 percent skill-mastery contribution produces 118 output. A technique contributing 20 additional percentage points reaches 138 during its paid window, subject to the 40% permanent and 30% temporary source caps. Record every source against a declared family and budget. Other dimensions, such as cycle time and energy cost, still interact and require full-fit tests.

A compact tracking weapon can remain better against an interceptor while a larger sustained weapon wins against a heavy target. A veteran's techniques can improve either weapon's operation, but cannot remove their target and fitting tradeoffs. Test both ordinary and expert operators with each design so a perk does not rescue an unusable baseline module or make one crafted variant universally superior.

### More varied module families

| Family | Example designs | Meaningful choice |
| --- | --- | --- |
| Laser weapons | Compact tracking laser, sustained beam, pulse laser, long-range emitter. | Small-target reliability, sustained pressure, burst, or reach; pay through output, cycle, power, or fitting. |
| Ion weapons | Shield breaker, engine disruptor, capacitor disruptor. | Choose the system to pressure; disruption trades against direct hull damage and follows control limits. |
| Ordnance | Rapid light missiles, heavy torpedoes, bombardment launcher. | Target size, ammunition, preparation, travel or windup, and exposure. |
| Shields | Capacity bank, recharge array, emergency booster. | Endurance, passive recovery, or paid burst protection; do not maximize all three in one design. |
| Repairs | Self repairer, allied repair projector, target-limited repair field. | Personal survival, focused support, or distributed recovery; total recovery and power remain bounded. |
| Capacitor | Storage bank, recovery regulator, consumable fuel injector. | Reserve capacity, sustained operation, or a paid emergency reserve. |
| Sensors | Survey scanner, targeting array, interference suite. | Industrial information, weapon support, or electronic warfare; power and fitting limit combinations. |
| Mining | Precision cutter, bulk extractor, deep drill, committed strip miner. | Recovery, throughput, material access, exposure, and capacitor use. |
| Salvage | Recovery arm, electronics recovery kit, salvage cutter. | Intact components, specialized parts, or bulk materials, with different work times and supplies. |
| Cargo | Expanded hold, ore compressor, protected compartment. | General volume, restricted material compression, or limited cargo protection at a fitting cost. |

Avoid making every design a numeric variant of the same button. Range, target requirements, windup, channeling, ammunition, material selectivity, and exposure create distinct uses. Implement a small representative set first and expand only when its choices survive complete-fit testing.

### Crafting choices and quality

Offer one significant calibration choice per module initially. Examples include:

- Efficient: lower capacitor expenditure with reduced peak output.
- Extended range: more reach with a longer cycle or higher fitting demand.
- Rapid cycle: faster operation with greater capacitor consumption per second.
- Compact: lower fitting demand with reduced output or capacity.
- High output: stronger cycles with higher power demand or exposure.
- Precision: better tracking, survey resolution, or material recovery with less bulk throughput.

Calibrations need not all be available to every family. A design's baseline identity should be explicit and predictable. Quality produces bounded refinements within that identity; it does not remove the drawbacks or turn a specialized item into the best item at everything. Show the resulting stats, effective operating costs, and any enhancement-transfer probabilities before crafting or refitting.

Represent guaranteed design choices as explicit recipe variants or another declared output selection. Any optional rolled enhancement remains a separate, clearly previewed crafting outcome. This keeps choosing a compact design independent of whether an enhancement successfully transfers.

Advanced Engineering opens complex designs, research, and specialized production, including compact equipment suitable for accessible hulls. It does not impose the crafter's skill requirement on the operator. Rare exploration and salvage components can unlock particular designs, while common materials continue to be needed for advanced modules, ammunition, service supplies, and repairs.

### Fit with the crafting engagement work

Use the shared crafting evaluator and the existing progress, quality, durability, and CP resources described in [CraftingEngagementPlan.md](CraftingEngagementPlan.md). Space equipment should use its recipe metadata, Engineering decisions, previews, and settlement rules. It does not need a separate ship-module crafting minigame.

Keep two concepts distinct: Sturdy, Delicate, and Calibrated are proposed crafting recipe profiles; an efficient or compact module is an equipment output choice. Recipe identity establishes the guaranteed base design. Optional enhancement transfer follows the shared crafting rules and displays its probability; do not advertise a guaranteed tuned enhancement when transfer still rolls per property group.

The crafting plan's first pilot preserves current quality and output mapping. Replacing ship module enhancement properties is therefore a coordinated change with its own conversion, economy simulation, preview updates, and validation. Do not silently change global quality behavior while adding space recipes. Manufacturing durability is also distinct from any later ship equipment damage or service condition.

The existing enhancement-transfer chance is the completed quality percentage, truncated to a whole percent, per property group. Baseline policy searches at Engineering 50 with ordinary endgame crafting gear and one maximum refinement yield about 79.18% transfer on a level-45 recipe and 69.06% on a level-50 recipe, while protecting completion. The Bible records the exact inputs and reproducible commands. Guaranteed calibration identity survives an unsuccessful optional enhancement transfer. Repeat these comparisons when the shared crafting profiles or actions change.

New ship equipment has an explicit NPC resale ceiling: the lower of 25% of the design reference price and 75% of the Standard recipe's reference material cost, rounded down. Crafted quality and calibration do not raise that ceiling. This keeps manufacturing focused on useful equipment and player commissions; other crafting valuation remains governed by its existing rules.

### Conversion of existing equipment

Create an explicit mapping from each existing hull, module, configuration, recipe output, and enhancement property to the new design. Record how crafting investment is retained or compensated before deployment. Conversion must preserve ownership, instance identity where required, interiors, permissions, cargo, and docking state.

Test representative ordinary and heavily enhanced equipment, fitted and stored copies, multiple configurations, and ships with passengers or persisted interiors. Use resumable, idempotent conversion records where needed. A rollback or retry must not duplicate cargo, rewards, modules, or compensation. Do not silently erase fitted gear or invent a flat refund that ignores materially different investments.

Character-build changes belong to the planned full rebuild where applicable. Ship records, inventories, world state, and other data that survive that rebuild need explicit conversion. Follow the repository's existing in-flight migration policy when implementing the change.

## Combat, support, and encounters

Distribute current Piloting-derived combat contributions among the relevant operating skills. Gunnery governs weapon handling, Piloting governs maneuvering and avoidance, and Ship Systems governs defensive operation. Do not replace the one-skill dependency with an average of all five skills or require every operator to maximize unrelated skills.

Reuse shared stat metadata, temporary adjustments, targeting declarations, telegraph infrastructure, and combat accounting where they fit. Ship effects must declare their operating context. Space shield hit points remain distinct from Attack Deflection, Shield Deflection, and Guard; ground percentages and caps do not become ship rules merely because the calculation service is shared.

Balance permanent equipment benefits, operator traits, operating modes, active techniques, and external support separately, then test them together. Set stacking rules for each stat family and repeated module type. Consider diminishing returns or a highest-effect rule where duplicate equipment would otherwise erase role weaknesses. Energy discounts require floors; transfers cannot create energy; repair and damage triggers cannot form unlimited feedback loops.

Support balance needs a total recovery budget as well as target limits. Transferring capacitor reduces the sender's real reserve, and consumable recovery consumes actual fuel. Cooldowns, equipment removal, docking, death, and target changes must not reset these budgets into free resources.

Weapon banks should reduce repetitive input while retaining each participating module's cooldown, ammunition, capacitor cost, and eligibility. Define how banking interacts with the shared activation cadence before implementation. A bank cannot bypass costs or turn one paid shot into free simultaneous fire.

NPCs use the same relevant target, range, equipment, and resource validation as players. Replace indiscriminate movement resets with role behavior: interceptors pursue, bombers seek attack opportunities, support ships protect and repair, and industrial targets attempt sensible escape. Test movement with the real ship models and pilot representation.

Encounter objectives should reward roles beyond damage: interrupt a bomber, hold a scan window, protect a miner, recover a disabled vessel, or extract cargo under pressure. Telegraph committed attacks and show why a module cannot activate. Tentative pacing targets are 30–60 seconds for an ordinary fight, 10–20 minutes for a short expedition, and 20–40 minutes for a group operation; validate these through play rather than treating them as formula requirements.

## Mining, deposits, and cargo

### Survey and extraction choices

Deposits expose composition, remaining reserves, hardness, stability, hazards, and valuable seams through appropriate surveys. Difficulty reflects required tools, exposure, and efficient recovery rather than a replacement tier of ore that invalidates earlier materials.

Precision cutters recover a greater useful share at lower throughput. Bulk extractors move more material but may recover a smaller useful share. Deep drills access difficult reserves with substantial power demands. Strip miners commit the ship to a longer exposed operation. Stabilizing unstable deposits requires fitted equipment; skill techniques improve its operation.

An accessible prospector should have a useful job finding and working mobile or selective seams. Large industrial vessels handle bulk efficiently but pay through operating cost and exposure. High-value expeditions should still consume and produce common resources used across the economy.

### Authoritative resource accounting

Track material removed from a deposit separately from usable cargo recovered. Efficiency improves recovery from a finite reserve; it cannot create material beyond that reserve. Define rounding, minimum batch size, wasted material, depletion, and rewards together.

An extraction session validates the ship, operator, target, range, compatible module, available power, and cargo capacity. Its committed costs and reserve claim are authoritative. Completion revalidates the necessary state and settles cargo and experience once. Establish a concurrency policy for multiple miners, using reserve claims or atomic operations supported by the persistence layer.

Cancel or settle sessions explicitly on docking, death, area transfer, disconnect, equipment removal, target destruction, and other invalidating changes. Clear locks and immobility on every terminal path and recover stale claims after restart. Delayed callbacks must not reward the wrong ship, an absent operator, an exhausted target, or a ship that has already been paid.

Cargo has one authoritative capacity calculation across extraction, loot, transfer, compression, and freight. Transfers need identifiable source and destination settlement to prevent duplication on retries. Compression applies to declared materials with bounded ratios and cannot become an unlimited-container bypass. Show cargo capacity and reserve commitments before starting a long cycle.

## Activity expansion

| Activity | Player decisions and reward identity | Delivery dependency |
| --- | --- | --- |
| Patrols and bounties | Choose targets, manage pursuit and ammunition, recover combat rewards. | Reliable encounters, contribution accounting, and recovery policy. |
| Mining expeditions | Survey, choose tools and seams, manage exposure, return raw resources. | Authoritative extraction and cargo settlement. |
| Salvage | Choose rapid bulk recovery or careful component recovery; assess threats. | Wreck ownership, tools, loot provenance, and cargo capacity. |
| Resource delivery contracts | Source and turn in requested materials for a clear payment. | Can initially use existing collect-item quest contracts and escrow. |
| Survey and anomaly work | Investigate information and hazards; find deposits, designs, and routes. | Survey definitions, discoveries, and bounded repeat rewards. |
| Freight | Choose route, cargo protection, capacity, and delivery risk. | Explicit origin, destination, cargo ownership, and sealed freight settlement. |
| Escort and rescue | Protect another vessel, stabilize a casualty, or bring survivors home. | Objective state, support contribution, failure rules, and passenger handling. |
| Boarding and derelicts | Disable or reach a target, enter ground content, recover or evacuate. | Safe space-to-ground lifecycle and authored interior encounters. |
| Fleet and capital operations | Coordinate screening, support, ordnance, surveys, and heavy objectives. | Proven role balance, group accounting, and optional crew stations. |

Existing collect-item contracts are a good first delivery loop. They do not yet constitute origin-to-destination freight; that requires explicit new state and settlement. Existing station and ground quest content can supply narrative and reward hooks without implying a boarding system already exists.

Boarding must define who operates the exterior ship, what happens to passengers, how enemies and cargo are represented, and how death, abandonment, disconnect, and return are resolved. Entering an interior cannot pause a vulnerable ship into safety. Restore pilot representation and equipment state once, and route space and ground rewards to legitimate contributions.

Crew stations come after the solo and small-fleet loop works. A station grants responsibility to its operator; adding passengers cannot multiply every stat or duplicate the same module's output. Fleet encounters must continue to offer work for accessible small ships.

## Economy, recovery, and onboarding

Give activities distinct outputs: mining supplies raw resources, salvage supplies usable components, combat supplies defined rewards and wreck opportunities, exploration supplies information and discoveries, and transport supplies delivery services. Connect these through existing crafting and refining rather than adding a new currency to every activity.

Compare net income per active hour after ammunition, fuel, repairs, travel, failed expeditions, and equipment replacement. Include full-cycle downtime and group reward division. A support ship or escort should receive legitimate compensation without being able to farm experience or payment from fabricated damage and cargo loops.

Use repeat demand from ammunition, repairs, refits, and optional consumable support to keep manufacturing useful. Avoid requiring constant destruction of expensive hulls to sustain the economy. Rare designs should create useful specialization rather than a jackpot item that universally replaces ordinary equipment.

For ordinary PvE, preserve the persistent hull and interior. Lose 20% of unprotected cargo units, rounded down by commodity. Hull recovery costs 3% of reference value, minimum 60 and maximum 1,800 credits. Fitted equipment remains installed and loses 20 service-condition points; service costs 1% of reference module value per point, with a 10-credit minimum bill per serviced module. Voluntary dock hull recovery uses the same 3% reference fee (60–1,800 credits); an outstanding defeat fee replaces that hull charge. Shield and capacitor refill add no charge, and module condition is billed separately. A module at zero condition cannot operate; condition above zero does not gradually reduce its output. Passengers enter the safe rescue/recovery flow once. The activity models include the resulting party service bills and account for cargo loss once. More severe opt-in activities can come later after the basic recovery loop works.

Provide a low-cost complete starter fit and short routes teaching launch, targeting, weapon banking, power, scanning, extraction, cargo return, and recovery. The tutorial should explain why the starter hull remains useful later and show where commissioned compact fittings improve it.

The cockpit and fitting screens should expose hull, shield, capacitor, target distance, threat, module readiness, status effects, warnings, cargo, and extraction progress. Show before-and-after fitting stats and practical operating costs. Use the shared player identity service on public ship, crew, contract, and support displays.

## Implementation approach

### Definition and runtime boundaries

Keep declarative data for hull roles, mount compatibility, module tuning, technique hardware requirements, extraction behavior, and rewards. Shared calculations consume stats and definition metadata. Direct perk checks are appropriate for purchase and unlock gates; shared behavior should read declared stat adjustments rather than special-case perk identifiers.

Recompute ship stats from their sources so repeated fitting, removal, mode changes, and reconnects do not accumulate adjustments. Audit captured mutable state in module activation callbacks, including electronic-warfare calculations. Separate focused responsibilities for fitting calculation, activation validation, extraction sessions, cargo settlement, and encounters while retaining compatibility with existing ship entities.

Declare distinct techniques through the normal ability-definition pattern. Reuse targeting and telegraph support only after proving it works with ship objects and pilot clones. Every aimed or area technique needs explicit shape, dimensions, targeting behavior, and valid per-rank spell metadata where required. Test action queues, interrupted channels, area changes, docking, and disconnects in NWN.

### Repository integration

- Preserve existing enum identifiers. Add skill, perk, and category definitions together with required 2DA and UI metadata; keep skill item-property metadata in sync.
- Declare stat classification and polarity on `StatType` entries. Keep ship-specific concepts separate from unrelated ground mechanics.
- Follow [IconStandards.md](IconStandards.md) and [VisualEffectSelection.md](VisualEffectSelection.md) for icons, feedback, and telegraphs; regenerate required cooldown variants and audit assets.
- Reuse available TLK gaps before appending IDs. Regenerate TLK assets and preserve meaningful resource names within NWN limits.
- Extend the shared economy-obtainability coverage for genuinely new loot or acquisition mechanisms. Salvage must not expose internal or NPC-only equipment through player searches or rewards.
- If the Design Bible is updated, follow [DesignBibleWorkbookRules.md](DesignBibleWorkbookRules.md), preserve untouched formula caches, and refresh the combat perk manifests and audits.
- Keep persistent ship conversion separate from character-build cleanup covered by the full rebuild. Use existing migration timing rules.
- Measure per-actor persistence and encounter costs before selecting batching or dirty-write intervals; document the accepted recovery window for crashes.

## Delivery sequence

| Stage | Deliverable | Exit condition |
| --- | --- | --- |
| 1. Baseline and joint design | Current hull/module/recipe inventory, complete-fit budgets, income baselines, session defects, migration mappings, and representative builds. | Hull roles, module designs, perk responsibilities, and crafting outputs agree; critical reward and session risks have a concrete resolution. |
| 2. Horizontal fitting and skill foundation | Role-based hulls, mount and power rules, five skill definitions, representative perk lines, typed module tuning, starter fits, and tested conversion. | Accessible and demanding hulls have distinct useful jobs; legal focused and mixed builds work without maximizing all five skills. |
| 3. First complete expedition release | One starter region and one advanced region with combat, survey, extraction, salvage, cargo, resource contracts, recovery, and cockpit guidance. | Players can prepare, choose a job, make decisions, return rewards, and refit; advanced content includes useful accessible hulls. |
| 4. Regional activity expansion | More deposit and encounter patterns, anomalies, true freight, escorts, rescue, and specialized recipes. | Activities differ in decisions and outputs, with stable net rewards and validated group contribution. |
| 5. Crew and larger operations | Crew stations, boarding and derelicts, and capital fleet objectives. | Exterior/interior lifecycle is reliable, support cannot duplicate resources, and small ships remain valuable in fleets. |

Stage 2 includes only the module variants and perk techniques needed to prove the roles; it does not require completing the entire proposed catalogue. Stage 3 is the first complete player loop. Do not commit all perk numbers before fitting, crafting, and reward budgets exist, or delay conversion design until deployment.

## Balance and validation

### Representative builds and comparisons

Maintain complete fits for a novice accessible ship, a veteran in that same hull, a veteran in a more demanding hull, an industrial specialist, an explorer, a support captain, and mixed ground-and-space characters. Include ordinary crafted gear, specialized calibration, enhanced equipment, configurations, duplicate modules, consumables, and allied effects.

Measure sustained and burst damage, effective hit rate by target role, capacitor expenditure and recovery, repair efficiency, survival, control uptime, cargo efficiency, resource recovery, net income, and control burden. Small ships need real advanced objectives; larger ships need measurable strengths that justify their demands. No one fit should dominate offense, survival, support, and economic output simultaneously.

Use the combat upgrade's distinction between permanent and temporary power and its practical attribute-budget assumptions when testing shared calculations. Do not invent unrestricted ship bonuses that ignore those constraints. Verify low, focused, and rare temporarily elevated ground-stat cases where attributes still influence ships, then tune ship coefficients separately.

### Automated checks

Add focused coverage for definition loading, stat recomputation, fitting compatibility and budgets, technique hardware requirements, stacking, closed support loops, per-rank targeting metadata, and NPC/player activation parity. Test quality and enhancement conversion without changing unrelated crafting outputs.

Exercise extraction completion exactly once, every cancellation path, stale reserve recovery, concurrent miners, depletion, rounding, cargo limits, freight retries, reward ownership, reconnects, and interrupted conversion. Include equipment round trips and configuration changes to detect stat drift.

Use relevant existing tests, including ship combat-log identity, skill-point accounting, gathering regressions, economy obtainability, and shared combat balance coverage. Build once with post-build deployment disabled, then run relevant filtered tests without rebuilding, following the repository rules. Documentation alone does not require a game build.

### Runtime checks and telemetry

Validate movement, pursuit, target selection, tracking feedback, telegraphs, banking, channels, docking, death, interior transitions, and disconnects in the actual game. Static definition tests cannot establish whether ship combat feels responsive or whether a pilot clone behaves correctly.

Track encounter duration, expenditure, net income, extraction depletion, failed settlements, repeated activity rewards, low-requirement hull use in advanced content, activity abandonment, module diversity, support participation, database writes, and interface refresh costs. Revisit a design when its advertised tradeoff fails in complete fits or players cannot read its consequences.

## Implementation and release gates

The Bible specifies hull requirements and envelopes, fitting-power values, tracking and accuracy formulas, perk ranks and prices, calibrated designs, quality limits, stacking caps, deposit claims, loss and service rates, and activity rewards. The numerical model is the baseline before implementation; implementation must not substitute ad hoc values.

Run the reproducible design audit, validate the saved workbook and formula caches, refresh existing Bible manifests, and pass focused Bible regressions before gameplay implementation. During implementation, test the rules against actual engine behavior and repeat the audit whenever values or formulas change. Release requires live checks of ship movement and targeting, encounter pacing, market demand, simultaneous actors, and persistent settlement. A passing offline model does not establish those results.

The first release establishes the connected solo and small-fleet loop before mandatory crew, extensive directional firing, another universal fitting resource, or severe-loss activities.
