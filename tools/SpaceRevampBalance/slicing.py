"""Space revamp slicing (plan section 24, "Slicing in space", W12b): the Data Analyzer, the space slicing targets (data
cans, locked caches, wreck data cores, customs scans, space lockboxes), their tiers, the space reward tables and the
space lockbox drop rates.

usage: python slicing.py            (writes the generated C# slicing tuning)
       python slicing.py --check    (exit 1 when the generated file is out of date)
       python slicing.py --sim      (prints each source's expected value and the combat lockbox income)
       add --store-price to --sim (here or in scanning.py) to count schematics and Prototype Tech at their store price

Every space slicing number lives here. catalog.py reads the Data Analyzer from it, scanning.py reads the data site,
locked cache, data core and lockbox values for its payout model, and bible_tabs.py calls slicing_tab(Sheet) to write
the Slicing (Revamp) tab. Draft numbers (plan section 17, "Slicing"); tune in testing.

Sources:
- Ground slicing rules kept: SlicingSession.ValidateStart (Slicing rank >= target tier, tiers 1-5, no slicing in
  combat), Skill.CalculateEspionageXP(PerkType.Slicing, tier, rank), the destruction chances of repeated failures.
- Component, schematic and Prototype Tech values: each blueprint's Cost (Module/uti) at the 40% store markdown, as
  trade.py values loot. The schematics are the blueprint update's (W9b, rewards.py): Design Schematics and Ship
  Schematics of the target's tier or lower (as salvage rare finds give them, Wild Space only ones left out), the
  Module Schematics (recipe unlocks of the named rank-50 modules) and Prototype Tech.
- Space combat credits per hour: trade.combat_credits_per_hour (the basic ships' loot, 60 kills per hour).
"""
import copy
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import components as comp  # noqa: E402
import rewards as rw  # noqa: E402
import trade  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
TIERS = [1, 2, 3, 4, 5]          # slicing tiers (boards and reward tables)
AREA_TIERS = [1, 2, 3, 4, 5, 6]  # space area tiers
MARKDOWN = 0.40

# ---------------------------------------------------------------------------------------------------------------
# Data Analyzer (Mid slot, Espionage, one press; Engineering crafts it; any hull, no rank requirement)
# ---------------------------------------------------------------------------------------------------------------
ANALYZER_TAG = "sm_dataan_00"
ANALYZER_RANGE = 30          # meters from the ship; replaces the ground's 5 m terminal range for space targets
ANALYZER_RECAST_SECONDS = 6.0
ANALYZER_CAPACITOR = 5
ANALYZER_DESCRIPTION = (f"One press starts the slicing mini-game on a data can, locked cache or wreck data core within "
                        f"{ANALYZER_RANGE} m. The pilot or the Sensors crew member slices with their own Slicing perk.")

# ---------------------------------------------------------------------------------------------------------------
# Target tiers: a target's slicing tier is its area's tier, at most 5 (the highest board tier); Wild Space is tier 5.
# ---------------------------------------------------------------------------------------------------------------
MAXIMUM_TIER = 5
WILD_SPACE_TIER = 5


def target_tier(area_tier, wild_space=False):
    return WILD_SPACE_TIER if wild_space else max(1, min(MAXIMUM_TIER, area_tier))


# ---------------------------------------------------------------------------------------------------------------
# Sites (scanning.py places them): data sites hold data cans; some cache sites are locked caches.
# ---------------------------------------------------------------------------------------------------------------
DATA_SITE_WEIGHT = 15            # beside combat 35, cache 25, gas 20, derelict 20 (scanning.py WEIGHTS)
DATA_SITE_CANS = 3
DATA_CAN_RESREF = "spc_datacan"  # tag spc_datacan; a click targets it, as a wreck
DATA_CAN_RING_METERS = 12
LOCKED_CACHE_PERCENT = 40        # of cache sites; decision: a locked cache holds the site's best loot and needs slicing

# ---------------------------------------------------------------------------------------------------------------
# Wreck data cores (plan section 12): elite, ace and boss wrecks carry one, under the killing party's 60 s claim.
# ---------------------------------------------------------------------------------------------------------------
DATA_CORE_DIFFICULTIES = ["Elite", "Boss"]
ACE_VARIABLE = "SPACE_ACE"       # creature local int; aces (named rare-spawn pilots, W9) set it to carry a core

# ---------------------------------------------------------------------------------------------------------------
# Space lockboxes: a destroyed preset NPC ship drops one into its killer's hold at these chances (percent).
# ---------------------------------------------------------------------------------------------------------------
LOCKBOX_DROP_PERCENT = {"Normal": 2, "Elite": 8, "Boss": 20}
LOCKBOX_RESREFS = [f"spc_lockbox_t{t}" for t in TIERS]
LOCKBOX_NAME = "Sealed Ship Lockbox"
LOCKBOX_COST = {1: 30, 2: 80, 3: 160, 4: 320, 5: 640}  # blueprint Cost, like the ground lockboxes (26-601)
LOCKBOX_BASE_ITEM = 523          # ESS9, one per slot, so each box keeps its own board, failures and integrity
LOCKBOX_MODEL = 241              # ESS9 art shared by the five tiers (as Ship Scrap shares 225)
DATA_CAN_APPEARANCE = 6030       # placeables.2da "[mdrn]Rusted, DataTerm"

# ---------------------------------------------------------------------------------------------------------------
# Rewards: every success pays credits and one bonus pick from its source's table
# ---------------------------------------------------------------------------------------------------------------
KINDS = ["Credits", "Component", "RareComponent", "Ammo", "DesignSchematic", "RecipeUnlock", "ShipSchematic", "AgentIntel",
         "PrototypeTech"]
KIND_IDS = {k: i for i, k in enumerate(KINDS, 1)}
KIND_NAMES = {"Credits": "Credits", "Component": "Component (common trait)", "RareComponent": "Component (rare trait)",
              "Ammo": "Ammo", "DesignSchematic": "Design Schematic", "RecipeUnlock": "Recipe unlock",
              "ShipSchematic": "Ship Schematic", "AgentIntel": "Agent intel", "PrototypeTech": "Prototype Tech"}

SOURCES = ["DataCan", "LockedCache", "WreckDataCore", "SpaceLockbox"]
SOURCE_IDS = {"DataCan": 3, "LockedCache": 4, "WreckDataCore": 5, "CustomsScan": 6, "SpaceLockbox": 7}
SOURCE_NAMES = {"DataCan": "Data can", "LockedCache": "Locked cache", "WreckDataCore": "Wreck data core",
                "CustomsScan": "Customs scan", "SpaceLockbox": "Space lockbox"}

# Bonus pick weights. Data sites yield blueprints, recipe unlocks and agent intel (plan section 24); data cores have
# better schematic odds than salvage (section 12); locked caches hold a site's best loot; lockboxes hold components,
# ammo, credits and parts with rare traits, never cargo (section 12). Customs scans pay nothing: the clean scan is the
# reward.
BONUS_WEIGHTS = {
    "DataCan": {"DesignSchematic": 10, "RecipeUnlock": 5, "ShipSchematic": 2, "AgentIntel": 20, "Component": 41, "RareComponent": 22},
    "LockedCache": {"RareComponent": 35, "Component": 30, "Ammo": 20, "DesignSchematic": 7, "RecipeUnlock": 3, "PrototypeTech": 5},
    "WreckDataCore": {"DesignSchematic": 15, "RecipeUnlock": 8, "ShipSchematic": 4, "PrototypeTech": 5, "RareComponent": 35, "Component": 33},
    "SpaceLockbox": {"Component": 35, "RareComponent": 20, "Ammo": 30, "Credits": 15},
}

# A pick whose pool is empty at the target's tier (no Ship Schematic below tier 2, recipe unlocks below
# RECIPE_UNLOCK_MINIMUM_TIER) gives a component with a rare trait. Agent intel opens an intel lead at a faction agent (W15a,
# agents.py): a mission at the agent rate whose extra is tokens, so the credit model keeps valuing it as a component.
FALLBACK_KIND = "RareComponent"

# Module Schematics unlock the named rank-50 modules (plan section 14: from T6 bosses and aces), so only tier 5 targets
# (T5-T6 space and Wild Space) give them.
RECIPE_UNLOCK_MINIMUM_TIER = 5
PROTOTYPE_TECH = rw.PROTOTYPE_TECH

# Ammo by slicing tier: (resref, quantity) choices, one picked at random.
AMMO = {
    1: [("ship_missile", 4), ("ship_fuelcapsule", 2)],
    2: [("ship_missile", 6), ("ship_fuelcapsule", 3)],
    3: [("ship_missile", 8), ("proton_bomb", 1)],
    4: [("ship_missile", 10), ("proton_bomb", 2)],
    5: [("proton_bomb", 2), ("acm_ammo", 1)],
}

# Credits: each source's base credits are CREDIT_SHARE of what the tier's exploring rate (EXPLORE_PREMIUM x combat
# per hour) pays over SLICE_MINUTES; the bonus pick comes on top. The roll pays 75-125% of the base, and a lockbox's
# "Credits" bonus pick pays the base again. The site caches (scanning.py) make up the rest of a site's time.
EXPLORE_PREMIUM = 1.15
SLICE_MINUTES = 1.5     # one board at a matched rank, assumed
SLICE_SUCCESS = 0.85    # attempts that end in success at a matched rank (failures may retry), assumed
CREDIT_SHARE = {"DataCan": 0.4, "LockedCache": 0.4, "WreckDataCore": 0.4, "SpaceLockbox": 0.25}
CREDIT_ROLL_MIN = 75
CREDIT_ROLL_MAX = 125
MINIMUM_CREDITS = 25


def component_value(tier):
    """A salvage-family component of the tier at the store markdown (traits do not change its price)."""
    costs = [trade.item_cost(comp.resref(comp.family(f)[0], tier)) for f in comp.SALVAGE_FAMILIES]
    return sum(costs) / len(costs) * MARKDOWN


def ammo_value(tier):
    return sum(trade.item_cost(r) * q for r, q in AMMO[tier]) / len(AMMO[tier]) * MARKDOWN


def combat_rate(tier):
    return trade.combat_credits_per_hour(tier)[1]


_REWARD_CATALOG = REPO / "SWLOR.Game.Server" / "Feature" / "ShipRewardDefinition" / "RevampRewardCatalog.cs"


def schematic_pool(kind, tier):
    """The resrefs a schematic pick can give at a slicing tier (SlicingReward.GetSchematicPool reads the same rows)."""
    import re
    text = _REWARD_CATALOG.read_text(encoding="utf-8")
    if kind == "RecipeUnlock":
        return re.findall(r'Resref = "(recipe_[^"]+)"', text) if tier >= RECIPE_UNLOCK_MINIMUM_TIER else []
    prefix = {"DesignSchematic": "dsgn_", "ShipSchematic": "schm_"}[kind]
    rows = re.findall(r'Resref = "(' + prefix + r'[^"]+)", Tier = (\d), WildSpaceOnly = (true|false)', text)
    return [r for r, t, wild in rows if int(t) <= tier and wild == "false"]


def pool_value(resrefs):
    return sum(trade.item_cost(r) for r in resrefs) / len(resrefs) * MARKDOWN


# The payout model counts a schematic or Prototype Tech pick as one component, as scanning.py does not count the salvage
# rare finds that give the same items: they are unlocks and research inputs, not income. At their store prices (Design
# Schematic 2,000, Ship and Module Schematics 10,000, Prototype Tech 1,000, at the 40% markdown) tier 5 slicing would
# pay about 130% of combat instead of 115% (--sim --store-price shows it).
SCHEMATICS_AT_STORE_PRICE = "--store-price" in sys.argv


def kind_value(kind, tier, credits):
    if kind in ("Component", "RareComponent", "AgentIntel"):
        return component_value(tier)  # agent intel: its lead pays the agent rate plus tokens, valued as a component here
    if kind in ("DesignSchematic", "RecipeUnlock", "ShipSchematic"):
        pool = schematic_pool(kind, tier)
        return pool_value(pool) if pool and SCHEMATICS_AT_STORE_PRICE else component_value(tier)
    if kind == "PrototypeTech":
        return trade.item_cost(PROTOTYPE_TECH) * MARKDOWN if SCHEMATICS_AT_STORE_PRICE else component_value(tier)
    if kind == "Ammo":
        return ammo_value(tier)
    if kind == "Credits":
        return credits
    return 0.0


def bonus_value(source, tier, credits):
    weights = BONUS_WEIGHTS[source]
    total = sum(weights.values())
    return sum(w / total * kind_value(k, tier, credits) for k, w in weights.items())


def board_rate(tier):
    """What exploring pays over one board at the tier: EXPLORE_PREMIUM x combat per hour, for SLICE_MINUTES."""
    return EXPLORE_PREMIUM * combat_rate(tier) / 60 * SLICE_MINUTES


def base_credits(source, tier):
    """CREDIT_SHARE of a board's pay, but never so much that credits and the bonus pick pay more than the board."""
    share = CREDIT_SHARE[source] * board_rate(tier)
    room = board_rate(tier) - bonus_value(source, tier, 0)
    return max(MINIMUM_CREDITS, int(25 * round(min(share, room) / 25)))


def source_value(source, tier):
    """One success's expected credits: the credits and the bonus pick."""
    credits = base_credits(source, tier)
    return credits + bonus_value(source, tier, credits)


def expected_value(source, tier):
    """One target's expected credits at the assumed success rate."""
    return SLICE_SUCCESS * source_value(source, tier)


def lockbox_per_kill(tier, difficulty="Normal"):
    return LOCKBOX_DROP_PERCENT[difficulty] / 100 * expected_value("SpaceLockbox", min(tier, MAXIMUM_TIER))


def lockbox_minutes_per_kill(difficulty="Normal"):
    return LOCKBOX_DROP_PERCENT[difficulty] / 100 * SLICE_MINUTES


def lockboxes_pay(tier):
    """Whether slicing a tier's lockboxes earns more per minute than the kills around them (otherwise they are sold)."""
    kills = trade.KILLS_PER_HOUR
    value = combat_rate(tier) + kills * lockbox_per_kill(tier)
    minutes = 60 + kills * lockbox_minutes_per_kill()
    return value / minutes * 60 > combat_rate(tier)


def combat_with_lockboxes(tier):
    """
    Combat credits per hour once normal kills drop space lockboxes, counting the time spent slicing them. Slicing a
    lockbox is optional (it can be sold or kept), so a lockbox never lowers the rate.
    """
    kills = trade.KILLS_PER_HOUR
    value = combat_rate(tier) + kills * lockbox_per_kill(tier)
    minutes = 60 + kills * lockbox_minutes_per_kill()
    return max(combat_rate(tier), value / minutes * 60)


def report():
    print("Space slicing, expected credits per success (credits + bonus pick):")
    for s in SOURCES:
        print(f"  {SOURCE_NAMES[s]:16s} " + "  ".join(f"T{t} {base_credits(s, t):5d} cr + {bonus_value(s, t, base_credits(s, t)):5.0f}" for t in TIERS))
    print("Combat with space lockboxes (normal kills at "
          f"{LOCKBOX_DROP_PERCENT['Normal']}%, {trade.KILLS_PER_HOUR} kills/h, {SLICE_MINUTES:g} min per lockbox):")
    for t in AREA_TIERS:
        before, after = combat_rate(t), combat_with_lockboxes(t)
        print(f"  T{t}: {before:7.0f}/h -> {after:7.0f}/h ({100 * (after / before - 1):+.1f}%)")


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/slicing.py. Do not edit by hand: change slicing.py and regenerate.
// </auto-generated>
"""


def cs_kind_weights(weights):
    return ", ".join(f"[SpaceSlicingRewardKind.{k}] = {v}" for k, v in weights.items())


def tuning_cs():
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "using SWLOR.Game.Server.Service.SpaceService;", "",
             "namespace SWLOR.Game.Server.Service.SlicingService", "{",
             "    /// <summary>",
             "    /// The space slicing numbers (plan section 24, \"Slicing in space\"): the Data Analyzer, target tiers, data sites,",
             "    /// locked caches, data cores, space lockbox drops and the space reward tables.",
             "    /// </summary>",
             "    public static class SpaceSlicingTuning", "    {",
             f"        public const string AnalyzerTag = {json.dumps(ANALYZER_TAG)};",
             f"        public const float AnalyzerRange = {ANALYZER_RANGE}f;",
             f"        public const int MaximumTier = {MAXIMUM_TIER};",
             f"        public const int WildSpaceTier = {WILD_SPACE_TIER};",
             "",
             f"        public const int DataSiteCans = {DATA_SITE_CANS};",
             f"        public const string DataCanResref = {json.dumps(DATA_CAN_RESREF)};",
             f"        public const float DataCanRingMeters = {DATA_CAN_RING_METERS}f;",
             f"        public const int LockedCachePercent = {LOCKED_CACHE_PERCENT};",
             f"        public const string AceVariable = {json.dumps(ACE_VARIABLE)};",
             "",
             "        /// <summary>The percent chance a destroyed preset NPC ship drops a space lockbox, by its difficulty.</summary>",
             "        public static readonly IReadOnlyDictionary<WreckDifficulty, int> LockboxDropPercent = new Dictionary<WreckDifficulty, int>",
             "        {"]
    lines += [f"            [WreckDifficulty.{k}] = {v}," for k, v in LOCKBOX_DROP_PERCENT.items()]
    lines += ["        };", "",
              "        /// <summary>Wreck difficulties whose wreck carries a data core (aces carry one through <see cref=\"AceVariable\"/>).</summary>",
              "        public static readonly IReadOnlyList<WreckDifficulty> DataCoreDifficulties = new[] { " +
              ", ".join(f"WreckDifficulty.{d}" for d in DATA_CORE_DIFFICULTIES) + " };", "",
              "        /// <summary>Each source's base credits by slicing tier (index 1-5); a success pays 75-125% of it.</summary>",
              "        public static readonly IReadOnlyDictionary<SlicingSourceType, int[]> Credits = new Dictionary<SlicingSourceType, int[]>",
              "        {"]
    lines += [f"            [SlicingSourceType.{s}] = new[] {{ 0, " + ", ".join(str(base_credits(s, t)) for t in TIERS) + " }," for s in SOURCES]
    lines += ["        };",
              f"        public const int CreditRollMinimumPercent = {CREDIT_ROLL_MIN};",
              f"        public const int CreditRollMaximumPercent = {CREDIT_ROLL_MAX};", "",
              "        /// <summary>Each source's bonus pick weights.</summary>",
              "        public static readonly IReadOnlyDictionary<SlicingSourceType, IReadOnlyDictionary<SpaceSlicingRewardKind, int>> BonusWeights =",
              "            new Dictionary<SlicingSourceType, IReadOnlyDictionary<SpaceSlicingRewardKind, int>>",
              "            {"]
    lines += [f"                [SlicingSourceType.{s}] = new Dictionary<SpaceSlicingRewardKind, int> {{ {cs_kind_weights(BONUS_WEIGHTS[s])} }},"
              for s in SOURCES]
    lines += ["            };", "",
              f"        public const SpaceSlicingRewardKind FallbackKind = SpaceSlicingRewardKind.{FALLBACK_KIND};",
              f"        public const int RecipeUnlockMinimumTier = {RECIPE_UNLOCK_MINIMUM_TIER};", "",
              "        /// <summary>The ammo a pick gives by slicing tier (index 1-5): one of the choices.</summary>",
              "        public static readonly (string Resref, int Quantity)[][] Ammo =",
              "        {",
              "            System.Array.Empty<(string, int)>(),"]
    lines += ["            new[] { " + ", ".join(f"({json.dumps(r)}, {q})" for r, q in AMMO[t]) + " }," for t in TIERS]
    lines += ["        };", "    }", "}", ""]
    return "\n".join(lines)


def items_cs():
    lines = ["using System.Collections.Generic;", "",
             "namespace SWLOR.Game.Server.Service.SpaceService", "{",
             "    /// <summary>",
             "    /// The space lockbox item resrefs (plan section 12, \"Lockboxes\"), generated by tools/SpaceRevampBalance/slicing.py.",
             "    /// Every string literal here is an item resref: EconomyObtainabilityCoverageTests and tools/FlagNpcEconomyItems.py",
             "    /// read this file as a source.",
             "    /// </summary>",
             "    public static class SpaceSlicingItems", "    {",
             "        /// <summary>Space lockboxes of slicing tiers 1-5, in tier order.</summary>",
             "        public static readonly IReadOnlyList<string> Lockboxes = new[]", "        {"]
    lines += [f"            {json.dumps(r)}," for r in LOCKBOX_RESREFS]
    lines += ["        };", "    }", "}", ""]
    return "\n".join(lines)


def lockbox_description(tier):
    return (f"A sealed lockbox from a destroyed ship. Right-click it in your inventory and choose Activate Item to begin "
            f"the Slicing minigame, out of combat. Requires Slicing rank {tier}. It holds ship components, ammo, credits "
            f"and parts with rare traits. Its circuit seed, failures, and integrity remain attached to the box when traded.")


def module_json(template, edits):
    d = copy.deepcopy(template)
    for key, value in edits.items():
        d[key]["value"] = value
    return json.dumps(d, indent=2, ensure_ascii=False) + "\n"


def blueprints():
    """The five space lockbox items (from the ground lockbox_t1) and the data can placeable (from the wreck)."""
    out = {}
    box = json.loads((REPO / "Module" / "uti" / "lockbox_t1.uti.json").read_text(encoding="utf-8"))
    for t in TIERS:
        resref = LOCKBOX_RESREFS[t - 1]
        text = lockbox_description(t)
        out[f"Module/uti/{resref}.uti.json"] = module_json(box, {
            "Tag": resref, "TemplateResRef": resref, "LocalizedName": {"0": f"{LOCKBOX_NAME} (Tier {t})"},
            "BaseItem": LOCKBOX_BASE_ITEM, "ModelPart1": LOCKBOX_MODEL, "xModelPart1": LOCKBOX_MODEL,
            "Cost": LOCKBOX_COST[t], "AddCost": LOCKBOX_COST[t] - 1, "Description": {"0": text}, "DescIdentified": {"0": text}})
    wreck = json.loads((REPO / "Module" / "utp" / "spc_wreck.utp.json").read_text(encoding="utf-8"))
    out[f"Module/utp/{DATA_CAN_RESREF}.utp.json"] = module_json(wreck, {
        "Tag": DATA_CAN_RESREF, "TemplateResRef": DATA_CAN_RESREF, "LocName": {"0": "Data Can"},
        "Appearance": DATA_CAN_APPEARANCE,
        "Description": {"0": "A drifting data can, its memory sealed behind a security lattice. Target it and slice it with a "
                             "Data Analyzer for whatever it still holds."}})
    return out


WRITES = {
    "SWLOR.Game.Server/Service/SlicingService/SpaceSlicingTuning.cs": tuning_cs,
    "SWLOR.Game.Server/Service/SpaceService/SpaceSlicingItems.cs": items_cs,
}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab: bible_tabs.py calls slicing_tab(Sheet) to write the Slicing (Revamp) tab.
# ---------------------------------------------------------------------------------------------------------------
def slicing_tab(Sheet):
    s = Sheet("Slicing (Revamp)", [26, 14, 12, 12, 12, 12, 12, 60])
    s.title("Slicing (Revamp): the Data Analyzer, space slicing targets, space lockboxes and the space reward tables")
    s.note("Dev Status: Proposed (space revamp W12b draft). Generated by tools/SpaceRevampBalance/slicing.py; plan section 24.")
    s.blank()
    s.title("Data Analyzer (Mid slot, Espionage, one press; crafted by Engineering; any hull, no rank requirement)")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Range", f"{ANALYZER_RANGE} m", "", "", "", "", "", "from the ship; replaces the 5 m terminal range for space targets"],
        ["Recast", f"{ANALYZER_RECAST_SECONDS:g} s, {ANALYZER_CAPACITOR} capacitor", "", "", "", "", "", "starts the slicing mini-game on the target"],
        ["Who slices", "the pilot, or the Sensors crew member", "", "", "", "", "", "with their own Slicing perk; a droid cannot slice"],
        ["Rules kept", "Slicing rank >= target tier; no slicing in combat", "", "", "", "", "", "the ship in combat counts, for a crew slicer too"],
        ["Target tier", f"area tier, at most {MAXIMUM_TIER}", "", "", "", "", "", f"Wild Space targets are tier {WILD_SPACE_TIER}"],
        ["XP", "Espionage (Slicing, tier, rank)", "", "", "", "", "", "never Piloting XP"],
    ])
    s.title("Targets")
    s.table(["Source", "Started by", "", "", "", "", "", "Notes"], [
        ["Data can", "Data Analyzer", "", "", "", "", "",
         f"{DATA_SITE_CANS} per data site (weight {DATA_SITE_WEIGHT}); the site cache appears once every can is sliced or lost"],
        ["Locked cache", "Data Analyzer", "", "", "", "", "",
         f"{LOCKED_CACHE_PERCENT}% of cache sites; slicing opens it for the site credits, an elite pull and a slicing reward"],
        ["Wreck data core", "Data Analyzer", "", "", "", "", "",
         "elite, ace and boss wrecks; the killing party's 60 s claim covers it; the wreck stays for salvage"],
        ["Customs scan", "Slice the Scan button", "", "", "", "", "",
         "at the patrol's tier; success: a clean scan; failure (or giving up): caught, with the fine, confiscation and standing loss"],
        ["Space lockbox", "using the item", "", "", "", "", "", "sliced by its owner out of combat, anywhere, the ground lockbox way"],
    ])
    s.title("Space lockbox drops (destroyed preset NPC ships, into the killer's hold)")
    s.table(["Difficulty", "Drop %", "", "", "", "", "", "Notes"],
            [[d, p, "", "", "", "", "", "the tier is the ship's, at most 5" if d == "Normal" else ""] for d, p in LOCKBOX_DROP_PERCENT.items()])
    s.title("Space reward tables: credits (75-125% of base) and one bonus pick")
    s.table(["Source", "T1", "T2", "T3", "T4", "T5", "", "Bonus pick weights"],
            [[SOURCE_NAMES[x]] + [base_credits(x, t) for t in TIERS] + ["",
              ", ".join(f"{KIND_NAMES[k]} {w}" for k, w in BONUS_WEIGHTS[x].items())] for x in SOURCES])
    s.note("Design Schematics and Ship Schematics are the blueprint update's (W9b) for ships of the target's tier or lower; "
           f"recipe unlocks are the Module Schematics, from tier {RECIPE_UNLOCK_MINIMUM_TIER} targets only; Prototype Tech is "
           "the research input. An empty pool gives a component with a rare trait instead; agent intel opens an intel lead at "
           "a faction agent (Faction Agents (Revamp)).")
    s.table(["Ammo tier", "Choices", "", "", "", "", "", "Notes"],
            [[t, ", ".join(f"{r} x{q}" for r, q in AMMO[t]), "", "", "", "", "", ""] for t in TIERS])
    s.title("Model: expected credits per success and combat with lockboxes")
    s.table(["Tier", "Data can", "Locked cache", "Data core", "Lockbox", "Combat / h", "With lockboxes", "Notes"],
            [[t] + [round(source_value(x, min(t, MAXIMUM_TIER))) for x in SOURCES] +
             [round(combat_rate(t)), round(combat_with_lockboxes(t)),
              f"{SLICE_MINUTES:g} min a board, {SLICE_SUCCESS:.0%} success assumed" if t == 1 else ""] for t in AREA_TIERS])
    return s


def main():
    if "--sim" in sys.argv:
        report()
        return
    check = "--check" in sys.argv
    stale = []
    files = {rel: build() for rel, build in WRITES.items()}
    files.update(blueprints())
    for rel, text in files.items():
        path = REPO / rel
        if path.exists() and path.read_text(encoding="utf-8") == text:
            continue
        stale.append(rel)
        if not check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8", newline="\r\n")
    print(f"{len(SOURCES)} reward sources, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
