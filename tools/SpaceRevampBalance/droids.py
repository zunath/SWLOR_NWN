"""Space revamp industrial droids (plan section 27, W14a): industrial droid parts, the Industry rating, the industrial
AI instructions (protocols), droid bays and what Industry pays off.

usage: python droids.py            (writes the generated C#, the part and disc blueprints and their palette entries)
       python droids.py --check    (exit 1 when a generated file is out of date)
       python droids.py --report   (prints the part bills against the combat parts and the payoff tables)
       python droids.py --bible-discs [--check]   (adds the protocol discs to the Bible's Equipment - Droids disc table)

Every industrial droid number lives here. bible_tabs.py calls droids_tab(Sheet) to write the Industrial Droids (Revamp)
tab and bible_recipes.py adds recipe_rows() to the Engineering Recipes tab. Draft numbers (plan section 17, "Droids":
Industry per industrial part, stat cuts, instruction effects); tune in testing.

Sources:
- Combat parts: Feature/RecipeDefinition/EngineeringRecipeDefinition/Droid*Recipes.cs (3 tier intermediates per part)
  and SWLOR.CLI/InputFiles/droid_item_list.tsv (HP 70, attributes 10 with one at 11, weapon skills and Armor on CPUs;
  2-3 attributes and the AI slots below on the other parts).
- Tier intermediates: EngineeringComponentRecipes.cs (every intermediate of a tier has the same raw bill, RAW below).
- Droid seat effectiveness: plan section 10 (W0 balance): 25% with no Industry, the best droid 75% at the Engineer
  and Sensors seats and 50% at Gunner seats.
- Scan strength: scanning.py (+1% per Espionage rank of a player; a droid has none).
"""
import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
TIERS = [1, 2, 3, 4, 5]
ROMAN = {1: "I", 2: "II", 3: "III", 4: "IV", 5: "V"}

# ---------------------------------------------------------------------------------------------------------------
# Industrial parts: one per slot per tier, built at the Droid Assembly tiers with Engineering.
# ---------------------------------------------------------------------------------------------------------------
# slot -> name, resref pattern, recipe category, DroidPartItemPropertySubType, the blueprint it copies (the slot's
# existing droid part art: industrial parts reuse it, ESS9 has no models left) and the tier intermediate it is built on.
PARTS = {
    "CPU": dict(name="Logistics CPU", resref="d_in_cpu{t}", category="DroidCPU", subtype=1, template="d_hm_cpu{t}_m",
                key="c_unit"),
    "Head": dict(name="Survey Head", resref="d_in_hd{t}", category="DroidHead", subtype=2, template="d_hd_mpv{t}",
                 key="d_brain"),
    "Body": dict(name="Utility Body", resref="d_in_bd{t}", category="DroidBody", subtype=3, template="d_bd_mp{t}",
                 key="d_chassis"),
    "Arms": dict(name="Labor Arms", resref="d_in_ar{t}", category="DroidArms", subtype=4, template="d_ar_mpv{t}",
                 key="manip_arm"),
    "Legs": dict(name="Hauler Legs", resref="d_in_lg{t}", category="DroidLegs", subtype=5, template="d_lg_mpv{t}",
                 key="dmotive_sys"),
}
SLOTS = list(PARTS)

# Industry: each industrial part adds 4 x its tier, so a droid of five tier-5 industrial parts has 100. Combat parts
# add none, so a droid's Industry is the sum of its industrial parts and a mixed droid is middling at both.
INDUSTRY_PER_TIER = 4
MAXIMUM_INDUSTRY = INDUSTRY_PER_TIER * 5 * len(SLOTS)

# Stat cuts (plan section 27: lower HP and attributes, no combat skill bonuses). A Logistics CPU has 50 HP (combat CPUs
# 70), every attribute at 10 (combat CPUs have one at 11), no weapon skills and no Armor. The other industrial parts
# give one attribute at about a third of the combat part's total and the same AI slots, so industrial protocols
# compete with combat instructions for the same slots.
CPU_HP = 50
CPU_STM = 10
CPU_ATTRIBUTE = 10
LIMB_ATTRIBUTE = {"Head": ("PER", [1, 1, 1, 1, 2]), "Body": ("VIT", [2, 3, 3, 3, 3]),
                  "Arms": ("MGT", [1, 2, 2, 2, 3]), "Legs": ("AGI", [1, 2, 2, 2, 3])}
AI_SLOTS = {"CPU": [3, 6, 9, 12, 15], "Head": [0, 1, 2, 2, 2], "Body": [0, 0, 1, 1, 1],
            "Arms": [0, 0, 1, 1, 1], "Legs": [0, 0, 1, 1, 1]}
# The combat parts' attribute totals (droid_item_list.tsv), for the report and the tests.
COMBAT_LIMB_ATTRIBUTES = {"Head": [3, 3, 3, 4, 6], "Body": [6, 8, 8, 8, 10], "Arms": [3, 6, 6, 7, 9], "Legs": [3, 6, 6, 7, 9]}
COMBAT_CPU_HP = 70

# DroidStatSubType ids (iprp_droidstat.2da); Industry is the new row 25.
STAT_IDS = {"Tier": 2, "AISlots": 3, "HP": 4, "STM": 5, "MGT": 6, "PER": 7, "VIT": 8, "WIL": 9, "AGI": 10, "SOC": 11,
            "Industry": 25}

# Cost: about half the materials of a combat part. A combat part takes 3 tier intermediates; an industrial part takes 1
# (the slot's own) plus half of one intermediate's raw bill, rounded down: 40-49% of a combat part's raw materials.
RAW = {1: [("aluminum", 1), ("elec_ruined", 2), ("jade", 1), ("quadrenium", 1)],
       2: [("steel", 2), ("elec_flawed", 3), ("agate", 2), ("vintrium", 1)],
       3: [("obsidian", 3), ("elec_good", 3), ("citrine", 2), ("ionite", 2)],
       4: [("crystal", 4), ("elec_imperfect", 3), ("ruby", 4), ("katrium", 3)],
       5: [("diamond", 5), ("elec_high", 2), ("emerald", 4), ("zinsiam", 4)]}
COMBAT_INTERMEDIATES = 3
RECIPE_FIRST = 7700  # RecipeType: parts 7700-7724 (slot-major), protocol discs 7725-7732


def part_name(slot, t):
    return f"{PARTS[slot]['name']} {ROMAN[t]}"


def part_resref(slot, t):
    return PARTS[slot]["resref"].format(t=t)


def part_enum(slot, t):
    return "Industrial" + PARTS[slot]["name"].replace(" ", "") + str(t)


def part_recipe_type(slot, t):
    return RECIPE_FIRST + SLOTS.index(slot) * len(TIERS) + (t - 1)


def part_level(t):
    return 10 * t - 5


def part_industry(t):
    return INDUSTRY_PER_TIER * t


def part_bill(slot, t):
    return [(f"{PARTS[slot]['key']}{t}", 1)] + [(r, q // 2) for r, q in RAW[t] if q // 2 > 0]


def raw_count(bill, t):
    per_intermediate = sum(q for _, q in RAW[t])
    raw = {r for r, _ in RAW[t]}
    return sum(q if r in raw else q * per_intermediate for r, q in bill)


def bill_share(slot, t):
    return raw_count(part_bill(slot, t), t) / (COMBAT_INTERMEDIATES * sum(q for _, q in RAW[t]))


def part_stats(slot, t):
    """The part's DroidStat properties, in blueprint order."""
    if slot == "CPU":
        stats = [("Tier", t), ("AISlots", AI_SLOTS[slot][t - 1]), ("HP", CPU_HP), ("STM", CPU_STM)]
        stats += [(a, CPU_ATTRIBUTE) for a in ("MGT", "PER", "VIT", "WIL", "AGI", "SOC")]
    else:
        attribute, values = LIMB_ATTRIBUTE[slot]
        stats = [("Tier", t), ("AISlots", AI_SLOTS[slot][t - 1]), (attribute, values[t - 1])]
    return stats + [("Industry", part_industry(t))]


def parts():
    return [dict(slot=s, tier=t, name=part_name(s, t), resref=part_resref(s, t), enum=part_enum(s, t),
                 recipe_type=part_recipe_type(s, t), level=part_level(t), bill=part_bill(s, t),
                 industry=part_industry(t), stats=part_stats(s, t)) for s in SLOTS for t in TIERS]


# ---------------------------------------------------------------------------------------------------------------
# Industrial AI instructions (protocols): droid-only perks (PerkGroupType.Droid) uploaded from instruction discs.
# Rank I fits any tier for 1 AI slot, rank II a tier 3+ droid for 2 slots (Engineering 20 sets the tier, as the combat
# instructions' skill ranks do). Each works where the droid works.
# ---------------------------------------------------------------------------------------------------------------
PROTOCOLS = {
    # enum: (PerkType id, name, {rank: value}, effect)
    "EfficiencyProtocol": (1056, "Efficiency Protocol", {1: 20, 2: 40},
                           "+{v}% Industry for output: droid-bay yields, holdings and drills"),
    "DamageControlProtocol": (1057, "Damage Control Protocol", {1: 5, 2: 10},
                              "+{v}% repairs from repairers it runs at the Engineer station, or from a Logistics droid bay"),
    "SurveyProtocol": (1058, "Survey Protocol", {1: 20, 2: 40},
                       "+{v}% scan strength on a Probe Launcher at its station, which it switches on"),
    "ProspectingProtocol": (1059, "Prospecting Protocol", {1: 5, 2: 10},
                            "+{v} to the rare-find chance of mining and salvage, from a droid bay"),
}
PROTOCOL_SLOTS = {1: 1, 2: 2}
PROTOCOL_ENGINEERING = {1: 0, 2: 20}  # rank II needs a tier 3 droid
PROTOCOL_TIER = {1: 1, 2: 3}
DISC_RESREF = {"EfficiencyProtocol": "id_efficiency", "DamageControlProtocol": "id_dmgcontrol",
               "SurveyProtocol": "id_survey", "ProspectingProtocol": "id_prospect"}
DISC_LEVEL = {1: 10, 2: 30}  # the instruction disc tiers' recipe levels (DroidInstructionRecipes.CreateTier)
DISC_BILL = {1: ("jade", "elec_ruined", "quadrenium"), 2: ("citrine", "elec_good", "ionite")}
DISC_TEMPLATE = "id_provoke1"


def discs():
    rows = []
    n = RECIPE_FIRST + len(SLOTS) * len(TIERS)
    for enum, (perk, name, ranks, _) in PROTOCOLS.items():
        for rank in ranks:
            rows.append(dict(enum=enum, perk=perk, rank=rank, name=f"Instruction Disc: {name} {ROMAN[rank]}",
                             resref=f"{DISC_RESREF[enum]}{rank}", recipe=f"Instruction{enum}{rank}", recipe_type=n,
                             level=DISC_LEVEL[rank], tier=PROTOCOL_TIER[rank], slots=PROTOCOL_SLOTS[rank]))
            n += 1
    return rows


# ---------------------------------------------------------------------------------------------------------------
# What Industry pays off (plan section 27). share = Industry / MAXIMUM_INDUSTRY.
# ---------------------------------------------------------------------------------------------------------------
# Ship droid crew (plan section 10): effectiveness = 25% + (seat best - 25%) x share, so the best industrial droid works
# an Engineer or Sensors seat at 75% and a Gunner seat at 50%, and a combat droid (no Industry) at 25% at any tier.
NO_INDUSTRY_EFFECTIVENESS = 0.25
BEST_SUPPORT_EFFECTIVENESS = 0.75
BEST_GUNNER_EFFECTIVENESS = 0.50

# Droid bays: Mining Barge, Industrial and Logistics hulls hold one droid. On a Mining Barge or Industrial hull it adds
# a percent of mining, strip-mining, gas and salvage yield (ore, gas units, salvage items per cycle); on a Logistics
# hull a percent of the ship's repairs. Efficiency Protocol raises the Industry the yield reads.
BAY_ROLES = ["Logistics", "MiningBarge", "Industrial"]
BAY_YIELD_ROLES = ["MiningBarge", "Industrial"]
BAY_REPAIR_ROLES = ["Logistics"]
BAY_YIELD_PERCENT_PER_INDUSTRY = 0.25   # +25% at 100 Industry, +35% with Efficiency Protocol II
BAY_REPAIR_PERCENT_PER_INDUSTRY = 0.10  # +10% at 100 Industry, +20% with Damage Control Protocol II

# Hooks for later slices (draft numbers; their slices tune them).
HOLDING_OUTPUT_PERCENT_PER_INDUSTRY = 0.5        # Planetary Holdings (W14b): +50% at 100 Industry
DRILL_OUTPUT_PERCENT_PER_INDUSTRY = 0.5          # Asteroid Drill (W13b, StationDroidRules): +50% at 100 Industry
SHIPYARD_BUILD_SPEED_PERCENT_PER_INDUSTRY = 0.3  # Shipyard (W13b, StationDroidRules): builds 30% faster at 100 Industry
SALVAGE_TEAM_SPEED_PERCENT_PER_INDUSTRY = 0.3    # Derelict salvage teams (W15): a 30% shorter timer at 100 Industry
SALVAGE_TEAM_YIELD_PERCENT_PER_INDUSTRY = 0.25   # Derelict salvage teams (W15): +25% yield at 100 Industry


def share(industry):
    return max(0.0, min(1.0, industry / MAXIMUM_INDUSTRY))


def seat_effectiveness(best, industry):
    return NO_INDUSTRY_EFFECTIVENESS + (best - NO_INDUSTRY_EFFECTIVENESS) * share(industry)


def output_industry(industry, efficiency_rank=0):
    bonus = PROTOCOLS["EfficiencyProtocol"][2].get(efficiency_rank, 0)
    return industry * (1 + bonus / 100)


def droid_industry(t, industrial_parts=5):
    return part_industry(t) * industrial_parts


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/droids.py. Do not edit by hand: change droids.py and regenerate.
// </auto-generated>
"""


def cs_double(x):
    return repr(float(x))


def tuning_cs():
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "using SWLOR.Game.Server.Service.PerkService;",
             "using SWLOR.Game.Server.Service.SpaceService;", "",
             "namespace SWLOR.Game.Server.Service.DroidService", "{",
             "    /// <summary>",
             "    /// The industrial droid numbers (plan section 27): Industry per part, droid seat effectiveness, the industrial",
             "    /// protocols, droid bays and the hooks later slices read.",
             "    /// </summary>",
             "    public static class DroidIndustryTuning", "    {",
             f"        public const int IndustryPerTier = {INDUSTRY_PER_TIER};",
             f"        public const int MaximumIndustry = {MAXIMUM_INDUSTRY};",
             f"        public const int CpuHP = {CPU_HP};",
             f"        public const int CombatCpuHP = {COMBAT_CPU_HP};",
             f"        public const int FirstRecipeType = {RECIPE_FIRST};",
             f"        public const int LastRecipeType = {RECIPE_FIRST + len(parts()) + len(discs()) - 1};",
             "",
             f"        public const double NoIndustryEffectiveness = {cs_double(NO_INDUSTRY_EFFECTIVENESS)};",
             f"        public const double BestSupportEffectiveness = {cs_double(BEST_SUPPORT_EFFECTIVENESS)};",
             f"        public const double BestGunnerEffectiveness = {cs_double(BEST_GUNNER_EFFECTIVENESS)};",
             "",
             f"        public const double BayYieldPercentPerIndustry = {cs_double(BAY_YIELD_PERCENT_PER_INDUSTRY)};",
             f"        public const double BayRepairPercentPerIndustry = {cs_double(BAY_REPAIR_PERCENT_PER_INDUSTRY)};",
             f"        public const double HoldingOutputPercentPerIndustry = {cs_double(HOLDING_OUTPUT_PERCENT_PER_INDUSTRY)};",
             f"        public const double DrillOutputPercentPerIndustry = {cs_double(DRILL_OUTPUT_PERCENT_PER_INDUSTRY)};",
             f"        public const double ShipyardBuildSpeedPercentPerIndustry = {cs_double(SHIPYARD_BUILD_SPEED_PERCENT_PER_INDUSTRY)};",
             f"        public const double SalvageTeamSpeedPercentPerIndustry = {cs_double(SALVAGE_TEAM_SPEED_PERCENT_PER_INDUSTRY)};",
             f"        public const double SalvageTeamYieldPercentPerIndustry = {cs_double(SALVAGE_TEAM_YIELD_PERCENT_PER_INDUSTRY)};",
             "",
             "        /// <summary>The hulls with a droid bay.</summary>",
             "        public static readonly IReadOnlyList<ShipRole> BayRoles = new[] { "
             + ", ".join(f"ShipRole.{r}" for r in BAY_ROLES) + " };",
             "",
             "        /// <summary>The droid bays that add to mining, gas and salvage yield.</summary>",
             "        public static readonly IReadOnlyList<ShipRole> BayYieldRoles = new[] { "
             + ", ".join(f"ShipRole.{r}" for r in BAY_YIELD_ROLES) + " };",
             "",
             "        /// <summary>The droid bays that add to the ship's repairs.</summary>",
             "        public static readonly IReadOnlyList<ShipRole> BayRepairRoles = new[] { "
             + ", ".join(f"ShipRole.{r}" for r in BAY_REPAIR_ROLES) + " };",
             "",
             "        /// <summary>Each protocol's value by rank: a percent, or rare-find points for Prospecting.</summary>",
             "        public static readonly IReadOnlyDictionary<PerkType, IReadOnlyDictionary<int, int>> ProtocolValues =",
             "            new Dictionary<PerkType, IReadOnlyDictionary<int, int>>",
             "            {"]
    for enum, (_, _, ranks, _) in PROTOCOLS.items():
        values = ", ".join(f"[{k}] = {v}" for k, v in ranks.items())
        lines.append(f"                [PerkType.{enum}] = new Dictionary<int, int> {{ {values} }},")
    lines += ["            };", "",
              "        /// <summary>Each protocol rank's AI slots and the Engineering rank that sets its tier.</summary>",
              "        public static readonly IReadOnlyDictionary<int, (int Slots, int EngineeringRank)> ProtocolRanks =",
              "            new Dictionary<int, (int Slots, int EngineeringRank)>",
              "            {"]
    lines += [f"                [{r}] = ({PROTOCOL_SLOTS[r]}, {PROTOCOL_ENGINEERING[r]})," for r in PROTOCOL_SLOTS]
    lines += ["            };", "    }", "}", ""]
    return "\r\n".join(lines)


def recipes_cs():
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "using SWLOR.Game.Server.Service.CraftService;",
             "using SWLOR.Game.Server.Service.SkillService;", "",
             "namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition", "{",
             "    /// <summary>",
             "    /// Industrial droid parts (plan section 27): one per slot per tier, about half the materials of a combat part.",
             "    /// </summary>",
             "    public class DroidIndustrialPartRecipes : IRecipeListDefinition", "    {",
             "        private readonly RecipeBuilder _builder = new();", "",
             "        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()", "        {"]
    for p in parts():
        lines += [f"            // {p['name']}",
                  f"            _builder.Create(RecipeType.{p['enum']}, SkillType.Engineering)",
                  f"                .Category(RecipeCategoryType.{PARTS[p['slot']]['category']})",
                  f"                .Resref(\"{p['resref']}\")",
                  f"                .Level({p['level']})",
                  "                .Quantity(1)"]
        lines += [f"                .Component(\"{r}\", {q})" for r, q in p["bill"][:-1]]
        r, q = p["bill"][-1]
        lines += [f"                .Component(\"{r}\", {q});", ""]
    lines += ["            return _builder.Build();", "        }", "    }", "}", ""]
    return "\r\n".join(lines)


def recipe_enum_lines():
    """The RecipeType members RecipeType.cs must hold (checked, not written)."""
    return [f"{p['enum']} = {p['recipe_type']}," for p in parts()] + [f"{d['recipe']} = {d['recipe_type']}," for d in discs()]


# ---------------------------------------------------------------------------------------------------------------
# Blueprints (Module/uti) and the custom item palette
# ---------------------------------------------------------------------------------------------------------------
def dump_gff(data):
    return json.dumps(data, indent=2, ensure_ascii=False).replace("\n", "\r\n") + "\r\n"


def load_uti(resref):
    return json.loads((REPO / "Module" / "uti" / f"{resref}.uti.json").read_bytes().decode("latin-1"))


def stat_property(stat, value):
    return {"__struct_id": 0, "ChanceAppear": {"type": "byte", "value": 100}, "CostTable": {"type": "byte", "value": 45},
            "CostValue": {"type": "word", "value": value}, "Param1": {"type": "byte", "value": 255},
            "Param1Value": {"type": "byte", "value": 0}, "PropertyName": {"type": "word", "value": 121},
            "Subtype": {"type": "word", "value": STAT_IDS[stat]}}


def part_uti(p):
    data = load_uti(PARTS[p["slot"]]["template"].format(t=p["tier"]))
    data["LocalizedName"]["value"] = {"0": p["name"]}
    data["Tag"]["value"] = p["resref"]
    data["TemplateResRef"]["value"] = p["resref"]
    part_type = [x for x in data["PropertiesList"]["value"] if x["PropertyName"]["value"] == 122]
    data["PropertiesList"]["value"] = part_type + [stat_property(s, v) for s, v in p["stats"] if v > 0 or s == "AISlots"]
    return dump_gff(data)


def disc_uti(d):
    data = load_uti(DISC_TEMPLATE)
    data["LocalizedName"]["value"] = {"0": d["name"]}
    data["Tag"]["value"] = d["resref"]
    data["TemplateResRef"]["value"] = d["resref"]
    prop = data["PropertiesList"]["value"][0]
    prop["Subtype"]["value"] = d["perk"]
    prop["CostValue"]["value"] = d["rank"]
    return dump_gff(data)


PALETTE = REPO / "Module" / "itp" / "itempalcus.itp.json"
# Each new entry joins the palette list that holds this existing blueprint.
PALETTE_ANCHORS = {"CPU": "d_hm_cpu1_m", "Head": "d_hd_mpv1", "Body": "d_bd_mp1", "Arms": "d_ar_mpv1", "Legs": "d_lg_mpv1",
                   "Disc": DISC_TEMPLATE}


def palette_entry(indent, name, resref):
    pad = " " * indent
    body = [f"{pad}{{", f'{pad}  "__struct_id": 0,', f'{pad}  "NAME": {{', f'{pad}    "type": "cexostring",',
            f'{pad}    "value": {json.dumps(name)}', f"{pad}  }},", f'{pad}  "RESREF": {{', f'{pad}    "type": "resref",',
            f'{pad}    "value": {json.dumps(resref)}', f"{pad}  }}", f"{pad}}}"]
    return "\r\n".join(body)


def palette_text():
    """Appends the missing entries to the end of their anchor's palette list; the rest of the file is untouched."""
    text = PALETTE.read_bytes().decode("latin-1")
    groups = {}
    for p in parts():
        groups.setdefault(p["slot"], []).append((p["name"], p["resref"]))
    groups["Disc"] = [(d["name"], d["resref"]) for d in discs()]
    for group, entries in groups.items():
        missing = [(n, r) for n, r in entries if f'"value": "{r}"' not in text]
        if not missing:
            continue
        anchor = text.index(f'"value": "{PALETTE_ANCHORS[group]}"')
        start = text.rfind("{", 0, text.rfind('"__struct_id"', 0, anchor))
        indent = start - text.rfind("\n", 0, start) - 1
        pos, depth = start, 0
        while True:  # walk to the bracket that closes the anchor's list
            ch = text[pos]
            if ch in "{[":
                depth += 1
            elif ch in "}]":
                depth -= 1
                if depth < 0:
                    break
            pos += 1
        close = text.rfind("}", 0, pos)  # the end of the list's last entry
        block = "".join(",\r\n" + palette_entry(indent, n, r) for n, r in missing)
        text = text[:close + 1] + block + text[close + 1:]
    return text.encode("latin-1")


ICON_BINDINGS = REPO / "SWLOR.Game.Server.Tests" / "Fixtures" / "ItemIcons" / "ItemIconBindings.csv"
CPU_MODEL = 71  # the Heavy Martial CPUs' artwork (iit_ess2_071), whose action icon the bindings share


def icon_bindings_text():
    """The reviewed-artwork rows of the Logistics CPUs (base item 516); other rows are untouched."""
    lines = ICON_BINDINGS.read_bytes().decode("utf-8").split("\r\n")
    lines = [x for x in lines if not x.startswith("d_in_cpu")]
    while lines and lines[-1] == "":
        lines.pop()
    # Rows created with the reviewed artwork carry NewBlueprint=true (no old icon to migrate), as ShipItemIconBindings does.
    if not lines[0].endswith(",NewBlueprint"):
        lines[0] += ",NewBlueprint"
    for p in parts():
        if p["slot"] != "CPU":
            continue
        props = ["DroidPart:subtype=1:costIndex=0"] + [f"DroidStat:subtype={STAT_IDS[s]}:costIndex={v}" for s, v in p["stats"]
                                                        if v > 0 or s == "AISlots"]
        lines.append(f"{p['resref']},{p['name']},d_hm_cpu,516,{CPU_MODEL},{CPU_MODEL},,"
                     f"iit_ess2_{CPU_MODEL:03d},iit_d_hm_cpu,Utility,\"Logistics processor used in droid assembly; its Industry "
                     "rating sets how well the droid works ships, bays and holdings.\","
                     "SWLOR.Game.Server/Feature/RecipeDefinition/EngineeringRecipeDefinition/DroidIndustrialPartRecipes.cs,false,"
                     + ";".join(props) + ",true")
    return ("\r\n".join(lines) + "\r\n").encode("utf-8")


def writes():
    out = {"SWLOR.Game.Server/Service/DroidService/DroidIndustryTuning.cs": tuning_cs().encode("utf-8"),
           "SWLOR.Game.Server/Feature/RecipeDefinition/EngineeringRecipeDefinition/DroidIndustrialPartRecipes.cs":
               recipes_cs().encode("utf-8")}
    for p in parts():
        out[f"Module/uti/{p['resref']}.uti.json"] = part_uti(p).encode("latin-1")
    for d in discs():
        out[f"Module/uti/{d['resref']}.uti.json"] = disc_uti(d).encode("latin-1")
    out["Module/itp/itempalcus.itp.json"] = palette_text()
    out["SWLOR.Game.Server.Tests/Fixtures/ItemIcons/ItemIconBindings.csv"] = icon_bindings_text()
    return out


# ---------------------------------------------------------------------------------------------------------------
# Design Bible: bible_tabs.py calls droids_tab(Sheet); bible_recipes.py appends recipe_rows() to Engineering Recipes.
# ---------------------------------------------------------------------------------------------------------------
def recipe_rows():
    rows = [dict(skill="Engineering", enum=p["enum"], category=PARTS[p["slot"]]["category"], name=p["name"],
                 level=p["level"], resref=p["resref"], enhancement="N/A", slots=0, components=p["bill"]) for p in parts()]
    for d in discs():
        gem, elec, metal = DISC_BILL[d["rank"]]
        rows.append(dict(skill="Engineering", enum=d["recipe"], category="DroidInstruction",
                         name=d["name"][len("Instruction Disc: "):], level=d["level"], resref=d["resref"],
                         enhancement="N/A", slots=0, components=[(gem, 1), (elec, 2), (metal, 1)],
                         group="Droid Equipment Blueprints", tier=d["level"] // 10))
    return rows


def droids_tab(Sheet):
    s = Sheet("Industrial Droids (Revamp)", [24, 14, 12, 12, 12, 22, 14, 60])
    s.title("Industrial Droids (Revamp): industrial parts, the Industry rating, protocols and droid bays")
    s.note("Dev Status: Proposed (space revamp W14a draft). Generated by tools/SpaceRevampBalance/droids.py; plan section 27.")
    s.blank()
    s.title("Industrial parts (Engineering, at the Droid Assembly tiers)")
    s.table(["Part", "Resref", "Recipe level", "Industry", "AI slots", "Stats", "Bill vs combat", "Bill"],
            [[p["name"], p["resref"], p["level"], p["industry"], AI_SLOTS[p["slot"]][p["tier"] - 1],
              ", ".join(f"{k} {v}" for k, v in p["stats"] if k not in ("Tier", "AISlots", "Industry") and v),
              f"{bill_share(p['slot'], p['tier']):.0%}", ", ".join(f"{r} x{q}" for r, q in p["bill"])] for p in parts()])
    s.title("Industry")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Per industrial part", f"{INDUSTRY_PER_TIER} x tier", "", "", "", "", "", f"five tier-5 parts: {MAXIMUM_INDUSTRY}"],
        ["Droid", "sum of its parts", "", "", "", "", "", "combat parts add none; a mixed droid is middling at both"],
        ["Stat cuts", f"CPU HP {CPU_HP} (combat {COMBAT_CPU_HP})", "", "", "", "", "",
         "attributes 10 (combat CPUs one at 11), no weapon skills or Armor; other parts one attribute at a third"],
    ])
    s.title("Droid crew effectiveness (plan section 10)")
    s.table(["Droid", "Industry", "Engineer / Sensors", "Gunner", "", "", "", "Notes"],
            [[f"Tier {t} industrial", droid_industry(t), f"{seat_effectiveness(BEST_SUPPORT_EFFECTIVENESS, droid_industry(t)):.1%}",
              f"{seat_effectiveness(BEST_GUNNER_EFFECTIVENESS, droid_industry(t)):.1%}", "", "", "",
              "module ranks up to the droid's level (10 x tier - 5)" if t == 1 else ""] for t in TIERS]
            + [["Any combat droid", 0, f"{NO_INDUSTRY_EFFECTIVENESS:.0%}", f"{NO_INDUSTRY_EFFECTIVENESS:.0%}", "", "", "",
                "Crew Morale and Leadership multiply it; every seat is capped at 105%"]])
    s.title("Protocols (industrial AI instructions; droids only)")
    s.table(["Protocol", "Rank", "Tier", "AI slots", "Value", "Disc", "", "Effect"],
            [[name, ROMAN[r], PROTOCOL_TIER[r], PROTOCOL_SLOTS[r], v, f"{DISC_RESREF[enum]}{r}", "", text.format(v=v)]
             for enum, (_, name, ranks, text) in PROTOCOLS.items() for r, v in ranks.items()])
    s.title("Droid bays (one droid)")
    s.table(["Hull role", "Bay effect", "", "", "", "", "", "Notes"], [
        ["Mining Barge, Industrial", f"+{BAY_YIELD_PERCENT_PER_INDUSTRY:g}% yield per Industry", "", "", "", "", "",
         f"mining, strip-mining, gas and salvage; +{BAY_YIELD_PERCENT_PER_INDUSTRY * MAXIMUM_INDUSTRY:g}% at {MAXIMUM_INDUSTRY}, "
         f"+{BAY_YIELD_PERCENT_PER_INDUSTRY * output_industry(MAXIMUM_INDUSTRY, 2):g}% with Efficiency Protocol II"],
        ["Logistics", f"+{BAY_REPAIR_PERCENT_PER_INDUSTRY:g}% repairs per Industry", "", "", "", "", "",
         f"+{BAY_REPAIR_PERCENT_PER_INDUSTRY * MAXIMUM_INDUSTRY:g}% at {MAXIMUM_INDUSTRY}; Damage Control Protocol adds its percent"],
        ["Droid limit", "working droids do not follow", "", "", "", "", "",
         "station crew and bay droids never count toward the one-droid follow limit"],
    ])
    s.title("Hooks for later slices")
    s.table(["Where", "Per Industry", "At 100", "", "", "", "", "Slice"], [
        ["Planetary Holdings output", f"+{HOLDING_OUTPUT_PERCENT_PER_INDUSTRY:g}%", f"+{HOLDING_OUTPUT_PERCENT_PER_INDUSTRY * 100:g}%", "", "", "", "", "W14b; Efficiency Protocol applies"],
        ["Asteroid Drill output", f"+{DRILL_OUTPUT_PERCENT_PER_INDUSTRY:g}%", f"+{DRILL_OUTPUT_PERCENT_PER_INDUSTRY * 100:g}%", "", "", "", "", "W13b (StationDroidRules); Efficiency Protocol applies"],
        ["Shipyard build speed", f"+{SHIPYARD_BUILD_SPEED_PERCENT_PER_INDUSTRY:g}%", f"+{SHIPYARD_BUILD_SPEED_PERCENT_PER_INDUSTRY * 100:g}%", "", "", "", "", "W13b (StationDroidRules)"],
        ["Derelict salvage team timer", f"-{SALVAGE_TEAM_SPEED_PERCENT_PER_INDUSTRY:g}%", f"-{SALVAGE_TEAM_SPEED_PERCENT_PER_INDUSTRY * 100:g}%", "", "", "", "", "W15"],
        ["Derelict salvage team yield", f"+{SALVAGE_TEAM_YIELD_PERCENT_PER_INDUSTRY:g}%", f"+{SALVAGE_TEAM_YIELD_PERCENT_PER_INDUSTRY * 100:g}%", "", "", "", "", "W15"],
    ])
    return s



# ---------------------------------------------------------------------------------------------------------------
# Design Bible: the protocol discs on Equipment - Droids (the instruction disc table in columns S-W).
# ---------------------------------------------------------------------------------------------------------------
DISC_SHEET = "Equipment - Droids"
DISC_TEXT_STYLE, DISC_NUMBER_STYLE = 371, 346  # the disc table's own cell styles


def disc_bible_rows():
    """Name (S), disc resref (T), tier (U), skill (V), AI slots (W) for each protocol disc."""
    return [(d["name"][len("Instruction Disc: "):], d["resref"], d["tier"], "Engineering", d["slots"]) for d in discs()]


def _disc_cells(number, row):
    name, resref, tier, skill, slots = row
    text = lambda c, v: f'<c r="{c}{number}" s="{DISC_TEXT_STYLE}" t="inlineStr"><is><t>{v}</t></is></c>'
    num = lambda c, v: f'<c r="{c}{number}" s="{DISC_NUMBER_STYLE}" t="n"><v>{v}</v></c>'
    return text("S", name) + text("T", resref) + num("U", tier) + text("V", skill) + num("W", slots)


def _column_index(ref):
    letters = re.match(r"[A-Z]+", ref).group(0)
    n = 0
    for ch in letters:
        n = n * 26 + ord(ch) - 64
    return n


def write_bible_discs(check=False):
    """Adds the missing protocol discs under the disc table's last row; every other cell and zip entry is untouched."""
    import os
    import tempfile
    import zipfile
    import bible_recipes
    workbook = bible_recipes.WORKBOOK
    with zipfile.ZipFile(workbook) as zf:
        path = bible_recipes.sheet_path(zf, DISC_SHEET)
        xml = zf.read(path).decode("utf-8")
        missing = [r for r in disc_bible_rows() if f"<t>{r[1]}</t>" not in xml]
        if not missing:
            print("protocol discs already on Equipment - Droids")
            return
        if check:
            print("protocol discs missing from Equipment - Droids: " + ", ".join(r[1] for r in missing))
            sys.exit(1)
        rows = list(re.finditer(r'<row r="(\d+)"[^>]*?(?:/>|>(.*?)</row>)', xml, re.S))
        last = max(int(m.group(1)) for m in rows if re.search(r'<c r="T\d+"[^>]*t="inlineStr"><is><t>id_', m.group(0)))
        by_number = {int(m.group(1)): m for m in rows}
        for offset, disc in enumerate(missing, start=1):
            number = last + offset
            cells = _disc_cells(number, disc)
            match = by_number.get(number)
            if match is None:
                after = max(n for n in by_number if n < number)
                anchor = by_number[after]
                new_row = f'<row r="{number}">{cells}</row>'
                xml = xml[:anchor.end()] + new_row + xml[anchor.end():]
            else:
                body = match.group(0)
                assert not re.search(r'<c r="[S-W]%d"[^/]*>' % number, body), number
                body = re.sub(r'<c r="[S-W]%d"[^>]*/>' % number, "", body)
                if body.endswith("/>"):
                    body = body[:-2] + ">" + cells + "</row>"
                else:
                    later = [c for c in re.finditer(r'<c r="([A-Z]+)\d+"', body) if _column_index(c.group(1)) > _column_index("W")]
                    cut = later[0].start() if later else body.rindex("</row>")
                    body = body[:cut] + cells + body[cut:]
                xml = xml[:match.start()] + body + xml[match.end():]
            rows = list(re.finditer(r'<row r="(\d+)"[^>]*?(?:/>|>(.*?)</row>)', xml, re.S))
            by_number = {int(m.group(1)): m for m in rows}
        fd, tmp = tempfile.mkstemp(suffix=".xlsx", dir=workbook.parent)
        os.close(fd)
        with zipfile.ZipFile(tmp, "w") as out:
            for info in zf.infolist():
                data = xml.encode("utf-8") if info.filename == path else zf.read(info.filename)
                out.writestr(info, data, compress_type=info.compress_type)
    os.replace(tmp, workbook)
    print(f"protocol discs written to Equipment - Droids rows {last + 1}-{last + len(missing)}")


def report():
    print("Part bills against the combat part (raw materials, intermediates expanded):")
    for t in TIERS:
        print(f"  T{t}: " + ", ".join(f"{s} {bill_share(s, t):.0%}" for s in SLOTS))
    print("Seat effectiveness (Engineer/Sensors, Gunner) by full industrial tier:")
    for t in TIERS:
        i = droid_industry(t)
        print(f"  T{t} Industry {i}: {seat_effectiveness(BEST_SUPPORT_EFFECTIVENESS, i):.1%}, "
              f"{seat_effectiveness(BEST_GUNNER_EFFECTIVENESS, i):.1%}")
    print(f"  combat droid: {NO_INDUSTRY_EFFECTIVENESS:.0%}")
    print(f"Best droid with Crew Morale and Leadership 50: {BEST_SUPPORT_EFFECTIVENESS * 1.15:.1%} (cap 105%)")


def main():
    if "--report" in sys.argv:
        report()
        return
    if "--bible-discs" in sys.argv:
        write_bible_discs("--check" in sys.argv)
        return
    check = "--check" in sys.argv
    stale = []
    for rel, data in writes().items():
        path = REPO / rel
        # A Windows checkout stores these files with CRLF line ends; compare the text, not the line ends.
        if path.exists() and path.read_bytes().replace(b"\r\n", b"\n") == data.replace(b"\r\n", b"\n"):
            continue
        stale.append(rel)
        if not check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    enum_text = (REPO / "SWLOR.Game.Server/Service/CraftService/RecipeType.cs").read_text(encoding="utf-8-sig")
    missing = [line for line in recipe_enum_lines() if not re.search(r"\b" + re.escape(line), enum_text)]
    if missing:
        print("RecipeType.cs is missing: " + " ".join(missing))
    print(f"{len(parts())} parts, {len(discs())} discs, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and (stale or missing):
        sys.exit(1)


if __name__ == "__main__":
    main()
