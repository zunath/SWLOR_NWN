# Current crafting baseline — October 5, 2026

The current endgame strategy is to buy enough Craftsmanship to cross a progress breakpoint, put most remaining enhancement capacity into Control, use Basic Touch repeatedly, and preserve a protected synthesis finish. Fully enhanced equipment makes ordinary level-50 crafts very reliable. Unenhanced equipment and level-51–53 recipes behave very differently.

This audit evaluates the current implementation, before the proposed crafting changes. It exports all 3,243 active recipes from their real C# builders and scans 7,909 module item blueprints. Recipe counts: Engineering 1,717; Smithery 736; Fabrication 456; Agriculture 319; Espionage 15.

**What “best” means here**

Rotations optimize expected quality, full-quality probability, or enhancement transfer probability while requiring successful completion on every random branch. The adaptive quality search exhaustively evaluates quality-first policies followed by protected synthesis, including repair, durability discounts, synthesis discounts, and guaranteed touches. It adapts after failed touches. It does not prove a global optimum over progress-first policies, strategies that accept craft failure, or every possible equipment roll. Numbers are computed from source rules, not measured in a live NWN session.

The equipment search compares every 5-point allocation of the available Control/Craftsmanship enhancement budget on the named endgame sets and two enhanced knives. It excludes random and guaranteed blueprint stat rolls and food from its main table. Alternative CP-heavy lower-tier pieces, character-specific restrictions beyond the named sets, market prices, and material acquisition times are not jointly optimized. Recipe rankings use base XP per direct input unit, not profit or gathering time. Required perk skill gates are included; learned recipes and perk ownership remain prerequisites.

**Equipment**

| Skill | Endgame set | Base Control | Base Craftsmanship | Equipment CP |
| --- | --- | ---: | ---: | ---: |
| Smithery | Eternal | 29 | 30 | 37 |
| Engineering | Skysteel | 29 | 30 | 37 |
| Fabrication | Rose | 29 | 30 | 37 |
| Agriculture | Moonflame | 29 | 30 | 37 |

Each set comprises a tunic, helmet, bracer, cloak, belt, leggings, necklace, and two rings. They require the relevant crafting skill at rank 40 to equip. Their recipes are level 50, so self-production requires rank 47; a rank-40 crafter needs another producer to supply them. Rank 50 with 37 equipment CP starts with **105 CP**, including the rank-25 passive +31.

Two **Ophidian Knives** are economical stat carriers: each recipe consumes one Refined Jasioclase and one Hyphae Wood. The level-41 recipe accepts tier-V enhancements, and each knife has two normal enhancement slots. Equipping them requires the Melee skill at rank 40. Their crafting bonuses count in both hands; the recommendation therefore has a character-build requirement.

Armor/accessory crafting enhancements V grant **+10** Control or Craftsmanship. Weapon equivalents grant **+5**. Each has enhancement level 45 and adds **50 progress** to the craft receiving it. Repeating the same stat stacks through `Craft.ApplyCraftedItemProperty`.

Nine armor/accessory pieces plus two knives therefore provide a 200-point crafting stat budget with two slots each, or 300 points with three researched slots each. CP has no corresponding crafting enhancement in the current content. Choose the relevant profession's enhancement subtype; a Smithery bonus does not help Engineering.

Blueprint research currently adds one slot at research level 10. Normal recipes have at most two slots, so newly produced endgame gear reaches **three slots**, despite the UI exposing eight. The retained `RandomEnhancementSlotGranted` field does not currently grant additional slots. Existing exceptional items, if any, require separate inventory inspection.

Blueprint rolls can improve these benchmarks. On a level-50 crafting armor piece, three output rolls can add up to 2+3+4 relevant stat points. The research process can also accumulate up to 27 relevant points under perfect rare rolls, for a theoretical **36 extra points per armor piece**. This is an extreme ceiling, not a reproducible gearing baseline. Ophidian Knives use the combat blueprint bonus pool because their base templates contain no crafting stats.

For food, **Gurnard Stew**, **Baked Nebimonite**, **Tricolored Sushi**, and **Fish & Chips** give +6 Control and +6 Craftsmanship to Smithery, Engineering, Fabrication, and Agriculture respectively. **Krafter's Kebab** gives +7 Control to all four professions. Food does not stack with another food effect. Which food improves a rotation depends on progress and quality breakpoints; a local search around the best level-50 allocations found both types useful. Neither food provides Espionage crafting stats.

**Rotations for level-50 recipes at rank 50**

With unenhanced endgame equipment, progress gains are Basic 50, Rapid 70, Careful 120. Quality gains are Basic 146, Standard 166, Precise 216. Progress target is 186; quality target is 2,641; durability is 80.

The short protected finish is **Steady Hand → Rapid Synthesis → Steady Hand → Careful Synthesis**: 190 progress, 40 CP, four clicks. This is a fast fixed safe finish for low-quality training or material production. It does not establish the fastest adaptive strategy with unprotected attempts and protected fallbacks.

The best expected-quality policy in the searched family for bare endgame gear has this fixed rotation:

1. Basic Touch ×6.
2. Master's Mend.
3. Waste Not → Basic Touch ×4.
4. Waste Not → Basic Touch ×4.
5. Waste Not → Steady Hand → Rapid Synthesis → Steady Hand → Careful Synthesis.

It uses 104 of 105 CP and finishes at zero durability, which succeeds because completion is checked before durability failure. Every touch succeeds independently with 90% probability. Expected quality is **69.66%**; the all-success quality is **77.39%**. Completion is guaranteed under the modeled rules. Protecting every quality action instead yields at most **604 quality, or 22.87%**, on this equipment. Muscle Memory on every touch is an expensive way to obtain certainty.

At these stats, Basic Touch gives 43.8 expected quality per CP, Standard Touch 20.75, and Precise Touch 10.8. Basic Touch also wins expected quality per durability. Advanced touches can still help hit a remaining quality threshold; these averages do not prove that they should never be used.

Waste Not lasts for four durability-spending actions, not four clicks. Steady Hand and Muscle Memory wait for their successful consuming action. Veneration lasts for four paid synthesis attempts; Basic Synthesis does not consume it. Two discounted Careful Syntheses save only 2 net CP after Veneration's 8-CP cost and require an extra durability-spending action. Three save 7 net CP. Cast it only when the entire finishing route benefits.

One implementation detail deserves a tuning review: being above a recipe's level increases its progress target by 5% per rank, capped at 25%, even though the code comment calls this a bonus. Synthesis gains also scale upward by 5% per rank. The audit follows both calculations as implemented. Being below the level raises the target by 25% per missing rank and reduces gains by 5% per missing rank, making newly unlocked higher-level recipes substantially harder.

**Optimized equipment and enhancement loads**

The following results use adaptive policies maximizing the chance of 100% quality, with successful completion guaranteed. They refer to tier-V enhancements loaded into a level-50 item being produced. The equipment slots column describes the crafter's equipped gear, not the produced item's load.

| Slots per equipped item | Loaded V enhancements | Craftsmanship | Control | Chance of 100% quality |
| ---: | ---: | ---: | ---: | ---: |
| 2 | 0 | 100 | 159 | 99.9258% |
| 2 | 1 | 30 | 229 | 99.8598% |
| 2 | 2 | 65 | 194 | 99.0983% |
| 3 | 0 | 80 | 279 | 99.9995% |
| 3 | 1 | 80 | 279 | 99.9976% |
| 3 | 2 | 65 | 294 | 99.9835% |
| 3 | 3 | 125 | 234 | 99.8772% |

These are the best allocations within the searched equipment budget for each load, not one set that simultaneously achieves every row. For example, the three-slot/two-load configuration allocates +35 Craftsmanship and +265 Control. That is achievable with three +10 Craftsmanship armor enhancements and one +5 weapon enhancement, with the remaining 29 enhancements providing Control.

For that configuration, Careful Synthesis grants 143 progress; two protected uses reach the loaded target of 286. Basic Touch grants 345 quality. The branch with no failed touches is:

**Waste Not → Basic Touch ×4 → Waste Not → Basic Touch ×4 → Steady Hand → Careful Synthesis → Steady Hand → Careful Synthesis.**

Eight successful touches reach full quality. After failures, the policy adds work, repairs durability, or protects a final touch according to the remaining resources. The full-quality probability above includes those adaptive branches; it is not the probability of blindly executing this 14-click success branch. Exact successful branches and policy outcome metrics are saved in the accompanying JSON.

Keep Craftsmanship close to a useful finishing breakpoint, then invest in Control. More Craftsmanship is valuable when it removes a synthesis or permits a cheaper synthesis combination; adding it without changing the finish wastes quality capacity. The three-load row can use three protected Basic Syntheses as a cheaper fallback, while preferring two protected Careful Syntheses on its successful-touch branch.

For **bare** endgame gear, the expected-quality-maximizing policies give these outcomes:

| Loaded V enhancements | Progress target | Expected quality | Chance all loaded stat groups transfer |
| ---: | ---: | ---: | ---: |
| 0 | 186 | 69.66% | — |
| 1 | 236 | 69.66% | 69.06% |
| 2 | 286 | 54.73% | 29.65% |
| 3 | 336 | 49.75% | 12.37% |

Transfer chance is quality rounded down to a whole percentage, rolled independently per generated property group. DMG and its accompanying damage type share a roll. For a fixed 80% transfer chance, all three independent groups land only 51.2% of the time. The table integrates the random quality distribution instead of cubing the average quality. A separate transfer-objective search improves the bare three-load outcome to about 12.39%, a small difference.

Tier-V crafting enhancements are preferable to Improved materials with the same +10/+5 benefit and a 70-progress penalty. Lower-tier enhancements are legal even when far below the recipe level; only enhancements more than five levels above it are rejected. Lower tiers can be worthwhile for cheap training or to avoid a costly progress breakpoint. They are not automatically better for final stats.

**Level 51–53 work needs another equipment allocation**

| Recipe level | Rank-50 unenhanced-load progress target | Quality target | Best searched Craftsmanship / Control | Expected quality |
| ---: | ---: | ---: | ---: | ---: |
| 51 | 423 | 3,951 | 150 / 209 | 79.07% |
| 52 | 754 | 5,172 | 280 / 79 | 34.45% |
| 53 | 1,025 | 5,783 | 320 / 39 | 18.35% |

These use the same 300-point researched equipment budget, 105 CP, no food or blueprint stat rolls, and the expected-quality objective. A level-52 recipe with three V enhancements has a 904-progress target; the best searched 260 Craftsmanship / 99 Control allocation averages **28.03%** quality. Treat level-52 Chiro and submission recipes separately from ordinary endgame equipment. A rotation or gear recommendation based solely on level 50 gives a misleading assessment of them.

**Recipes for training**

The full content audit ranks eligible recipes for every rank 0–49 by base XP per direct component unit, retaining ten candidates. It includes skill prerequisites on the required crafting perks. Examples:

| Player rank | Smithery | Engineering | Fabrication | Agriculture |
| ---: | --- | --- | --- | --- |
| 0 | Basic Knife or Battlemaster Ring: 676 XP / 2 inputs | Basic Mining Laser or DHRG-001: 676 / 2 | Bed Roll, Bench, or Campfire: 676 / 2 | Cooked Fish Substitute I: 900 / 3 |
| 10 | Titan Knife or Titan Ring: 676 / 2 | Mining Laser I or DHRG-002: 676 / 2 | Wood Bench or Birdbath: 676 / 2 | Cooked Fish Substitute II: 900 / 3 |
| 20 | Delta Knife or Quark Ring: 676 / 2 | Mining Laser II or DHRG-003: 676 / 2 | Female Statue: 750 / 2 | Cooked Fish Substitute III: 900 / 3 |
| 30 | Proto Knife or Argos Ring: 676 / 2 | Mining Laser III or DHRG-004: 676 / 2 | Large Bed or Corner Desk with Terminal: 676 / 2 | Cooked Fish Substitute IV: 900 / 3 |
| 40 | Ophidian Knife or Eclipse Ring: 676 / 2 | Mining Laser IV or DHRG-005: 676 / 2 | Dartboard or Holo Display 2: 676 / 2 | Cooked Fish Substitute V: 900 / 3 |
| 49 | Submission Tokens: 900 / 5; learned salvage gauntlets are alternatives | Assault CPU V Variant P: 676 / 3 | DNA Extractor V: 676 / 5 | Stringy Meat Substitute V: 676 / 3 |

These are material-count leaders, not universal economic recommendations. A rare shard and a readily gathered component both count as one direct input; assembled droid components hide their own upstream material costs. Submission Tokens also have the much larger level-52 progress target. The last-rank Smithery selection therefore needs a practical comparison with easier learned level-50 salvage gauntlets before calling it the fastest or cheapest leveling route.

Espionage starts with Venom Coating I or Snare Kit I, each producing five units. As ranks increase, learned concentrates often provide the 150-XP practice floor with fewer inputs. At rank 49, **Nightroot Concentrate** gives 150 base XP for one Wild Innards and two Herb X; Venom Coating V gives 150 for five inputs and produces five coatings. Both need Poisoncraft V, which unlocks at rank 48. Snare Kit V needs Master Saboteur at rank 50 and cannot train rank 49.

At rank 50, Espionage starts with only 68 CP and receives no Control/Craftsmanship/CP gear subtype or crafting food support. The modeled expected-quality route for a level-50 recipe averages **29.82%** quality with a guaranteed finish. This is a structural difference from the other four crafting professions.

Completing new recipes gives a 20% first-craft XP bonus. Quality increases XP by up to 100%. Blueprint level adds 50% per level, compounded before the quality bonus; level 10 therefore multiplies base XP by six before quality. Researched crafting also spends credits and licensed runs, so its XP multiplier does not establish an economical training strategy. A short completion rotation tends to win clicks per craft; a longer quality rotation buys more XP per material batch. Vendor bonus calculations are not sufficient to rank net profit without actual vendor prices and material costs.

**Validation and implications for the redesign**

The source exporter builds and runs without starting NWN. Its XP chart is initialized exactly as during module cache setup. Checks cover that initialization, crafting-perk gates, resource and gain arithmetic, an independent binomial calculation of the 14-touch benchmark, the deterministic 604-quality route, and level-52 targets. The relevant repository test filter passed **21 tests**: `CraftTests`, `RecipeLearningSourceTests`, and `CombatUpgradeBibleRecipeParityTests`. Builds skipped the post-build deployment. No live playtest, market survey, or production gameplay change was performed.

The redesign should preserve progression value while addressing Basic Touch's rank-40 dominance, the sameness of the four equipment stat packages, the severe level-51–53 difficulty jump, and Espionage's missing crafting gear support. Conditions alone may vary the timing of the same Basic Touch routine; distinct action roles and profession perks need enough value to change the best decision under these measured budgets.

Sources: [craft action resolution and gains](../../SWLOR.Game.Server/Feature/GuiDefinition/ViewModel/CraftViewModel.cs), [recipe chart](../../SWLOR.Game.Server/Service/CraftService/RecipeLevelChart.cs), [property merging and enhancement mapping](../../SWLOR.Game.Server/Service/Craft.cs), [research progression](../../SWLOR.Game.Server/Feature/GuiDefinition/ViewModel/ResearchViewModel.cs), [equipment processing](../../SWLOR.Game.Server/Feature/EquipmentStats.cs), [XP chart](../../SWLOR.Game.Server/Service/Skill.XPChart.cs), [food effects](../../SWLOR.Game.Server/Feature/ItemDefinition/ConsumableItemDefinition.cs), and `Module/uti/*.uti.json`.

Reproduce by building `tools/SWLOR.CraftingAudit/SWLOR.CraftingAudit.csproj` with `dotnet build -p:RunPostBuildEvent=Never -p:NuGetAudit=false -m:1`, then running `dotnet tools/SWLOR.CraftingAudit/bin/Debug/net10.0/SWLOR.CraftingAudit.dll design/testing/crafting-source-data.json` and `python tools/AuditCrafting.py`. The Python tool exposes `--benchmarks`, `--search-equipment`, `--difficult-recipes`, `--refresh-selected`, and `--check`. The equipment search resumes existing completed rows; remove its generated result file before a fresh full search after gameplay changes.

Data: [content and per-rank training candidates](crafting-content-audit.json), [rotation benchmarks](crafting-rotation-benchmarks.json), [equipment allocation results](crafting-equipment-search.json), [difficult recipe results](crafting-difficult-recipes.json), [local food comparisons](crafting-food-search.json), and [raw recipe/chart/blueprint export](crafting-source-data.json).
