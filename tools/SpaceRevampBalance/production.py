"""Space revamp production cost (review fix 2026-10-30): the full cost of building a hull, a module rank or ammo, not
only the raw material value catalog.advanced_cost_rows compares.

usage: python production.py --sim   (prints raw and full cost per hull, module rank and ammo, today's crafting rules and
                                     the crafting revamp's; exit 1 when a hull or a starter item leaves the cost band)

The chain: every component (and capital part, rare component) is its own craft before the module or hull craft that
consumes it, and every craft can fail.
- Failure: a failed craft loses each component stack with 65% chance (CraftViewModel.ProcessFailure, ChanceToLoseItem
  65); the crafting revamp keeps that rule (CraftingRevampPlan.md on origin/feature/crafting-revamp, "Failure"). So a
  craft's expected input per success is inputs x (1 + 0.65 x (1 - s) / s) at success rate s, and a module or hull
  failure also loses 65% of the components it consumed, with the cost of their own failures inside them.
- Success rates: the CraftSim economy parity tables of the crafting revamp (design/testing/crafting-revamp on that
  branch; mid gear, rank equal to the recipe level): "legacy" is today's minigame played well, "revamp" the new one.
  Engineering is the Circuit, Smithery the Forge, Agriculture the Kitchen.
- Blueprint credit fee: a researched blueprint charges 80 x perk level^2.2 x blueprint level^2 credits a craft
  (Craft.CalculateBlueprintCraftCreditCost; perk level = recipe level / 10 + 1, at most 5). Blueprints are optional
  (they add bonuses), so the fee is shown at blueprint level 1 and not counted in the band.
- Shipyard fee: Capital hull deeds are crafted at a station Shipyard whose mayor may set a fee of 0-50,000 credits
  (StationTuning.ShipyardFeeMax); the crafter's own station charges none. Shown, not counted in the band.
- Band (components.HULL_COST_BAND, plan section 14): a hull's full cost at most 1.27x the pre-revamp recipe's full cost
  (its legacy bill of refined ore and electronics, one craft at the same success rate), at the crafting revamp's success
  rates (it ships before the space revamp); today's are shown. The starter items (the dockhand store's modules and hulls,
  STARTERS) are held to the same band; other module ranks are shown only (the plan lets a gem or metal signature make a
  module dearer, section 14 "Cost"). Raw is the material value alone, as catalog.py compares it.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import catalog as c  # noqa: E402
import components as comp  # noqa: E402
import rewards as rw  # noqa: E402

LOSS = 0.65
BAND_RULE = "revamp"
STORE = Path(__file__).resolve().parents[2] / "Module" / "utm" / "dockhand_store.utm.json"
STARTERS = set(__import__("re").findall(r'"(s[dm]_[a-z0-9_]+)"', STORE.read_text(encoding="utf-8")))
BANDS = [(1, 10), (11, 20), (21, 30), (31, 40), (41, 53)]
# CraftSim economy parity, mid gear: success by recipe band (legacy, revamp).
SUCCESS = {
    "Engineering": [(0.97, 0.98), (0.97, 0.98), (0.99, 1.00), (0.99, 1.00), (0.87, 1.00)],   # circuit-gates.md
    "Smithery": [(0.96, 0.97), (0.98, 0.98), (0.99, 1.00), (0.99, 0.99), (0.73, 0.99)],      # forge-gates.md
    "Agriculture": [(0.96, 0.98), (0.98, 0.97), (0.98, 0.98), (0.99, 0.95), (0.87, 0.96)],   # kitchen-gates.md
}
RULES = {"legacy": 0, "revamp": 1}
BLUEPRINT_FEE_BASE = 80
SHIPYARD_FEE_MAX = 50000


def success(skill, level, rule):
    level = max(1, level)
    band = next(i for i, (lo, hi) in enumerate(BANDS) if lo <= level <= hi)
    return SUCCESS[skill][band][RULES[rule]]


def factor(skill, level, rule):
    """Expected inputs consumed per successful craft."""
    s = success(skill, level, rule)
    return 1 + LOSS * (1 - s) / s


def blueprint_fee(level, blueprint_level=1):
    perk = min(5, level // 10 + 1)
    return int(BLUEPRINT_FEE_BASE * perk ** 2.2 * blueprint_level ** 2)


# Intermediate recipes by the item they make: (skill, recipe level, inputs).
_INTERMEDIATE = {}
for (short, enum, name, skill, *_rest) in comp.FAMILIES:
    for t in comp.TIERS:
        _INTERMEDIATE[comp.resref(short, t)] = (skill, comp.recipe_level(t), comp.components_for_recipe(enum, t))
for (short, enum, name, skill, _model, inputs, _desc) in comp.RARE_FAMILIES:
    _INTERMEDIATE[comp.rare_resref(short)] = (skill, comp.RARE_RECIPE_LEVEL, inputs)
for (_key, res, _name, skill, fam, per, _n, _desc) in comp.CAPITAL_PARTS:
    _INTERMEDIATE[res] = (skill, comp.CAPITAL_PART_LEVEL, comp.capital_part_inputs(fam, per))


def item_cost(resref, rule=None):
    """An item's cost: raw (rule None) is its listed material value; with a rule, a crafted intermediate costs its inputs'
    full cost times its own craft's failure factor."""
    if rule is None or resref not in _INTERMEDIATE:
        if resref in _INTERMEDIATE:
            return bill_cost(_INTERMEDIATE[resref][2])
        return c.material_cost(resref)
    skill, level, inputs = _INTERMEDIATE[resref]
    return bill_cost(inputs, rule) * factor(skill, level, rule)


def bill_cost(bill, rule=None):
    return sum(item_cost(r, rule) * q for r, q in bill)


def full_cost(bill, skill, level, rule):
    return bill_cost(bill, rule) * factor(skill, level, rule)


def hull_rows():
    _steps, hulls = c.catalog()
    out = []
    for h in hulls:
        role = next(s[1] for s in c.m.SHIPS if s[0] == h["Name"])
        legacy = c.legacy_hull_bill(role, h["Advanced"])
        level = h["RecipeLevel"]
        rare = next((r for r, _q in h["Components"] if any(r == comp.rare_resref(f[0]) for f in comp.RARE_FAMILIES)), None)
        bill = h["Components"]
        if not rare and h["Advanced"]:  # the rare-trait slot: a traited component of the hull's tier, the dearest that fits
            tier = min(5, h["PilotingRank"] // 10 + 1)
            slot = max((comp.resref(comp.family(f)[0], tier) for f in c.stand_in_families(h)), key=item_cost)
            bill = bill + [(slot, 1)]
        row = dict(name=h["Name"], size=h["Size"], adv=h["Advanced"], level=level, raw=bill_cost(bill),
                   starter=h["Tag"] in STARTERS,
                   pre=bill_cost(legacy), fee=blueprint_fee(level), shipyard=h["Size"] == "Capital")
        for rule in RULES:
            row[rule] = full_cost(bill, "Engineering", level, rule)
            row[rule + "_pre"] = full_cost(legacy, "Engineering", level, rule)
        out.append(row)
    return out


def module_rows():
    """Each module rank and row (High / Mid-Low, capital): the dearest family's bill."""
    steps, _hulls = c.catalog()
    out = {}
    for s in steps:
        if s["Family"] not in comp.MODULE_COMPONENT or s.get("Named"):
            continue
        key = (s["Rank"], "High" if s["Row"] == "High" else "Mid/Low", s["Capital"])
        if s["Tag"] in STARTERS:
            key = key + (s["Tag"],)
        legacy = c.legacy_module_bill(s["Rank"], s["Row"], s["Capital"])
        level = s["RecipeLevel"]
        row = dict(rank=s["Rank"], row=key[1], capital=s["Capital"], family=s["Family"], level=level,
                   starter=s["Tag"] in STARTERS,
                   raw=bill_cost(s["Components"]), pre=bill_cost(legacy), fee=blueprint_fee(level))
        for rule in RULES:
            row[rule] = full_cost(s["Components"], "Engineering", level, rule)
            row[rule + "_pre"] = full_cost(legacy, "Engineering", level, rule)
        if key not in out or row["revamp"] > out[key]["revamp"] or row["legacy"] > out[key]["legacy"]:
            out[key] = row
    return [out[k] for k in sorted(out, key=lambda k: (k[0], k[1], k[2], k[3] if len(k) > 3 else ""))]


def ammo_rows():
    rows = []
    for t, plain in rw.PLAIN_MISSILE.items():
        rows.append(dict(name=f"Missile T{t} (x{plain['quantity']})", level=plain["level"], raw=bill_cost(plain["components"]),
                         **{rule: full_cost(plain["components"], "Engineering", plain["level"], rule) for rule in RULES}))
    for a in c.ammo_recipes():
        if a["Tier"] in (1, 5):
            rows.append(dict(name=f"{a['Name']} T{a['Tier']} (x{a['Quantity']})", level=a["RecipeLevel"], raw=bill_cost(a["Components"]),
                             **{rule: full_cost(a["Components"], "Engineering", a["RecipeLevel"], rule) for rule in RULES}))
    return rows


def ratio(row, rule):
    return row[rule] / row[rule + "_pre"]


def report():
    band = comp.HULL_COST_BAND
    ok = True
    print("Hulls: credits per hull built (raw material value; full chain with failures, today's minigame / the crafting "
          "revamp's), against the pre-revamp recipe's full cost; blueprint fee at level 1; Capital hulls add the Shipyard fee.")
    for r in hull_rows():
        held = ratio(r, BAND_RULE)
        flag = ("  OVER BAND" if held > band else "") + ("  (today's minigame above the band)" if ratio(r, "legacy") > band else "")
        ok &= held <= band
        print(f"  {r['name']:26s} {r['size']:7s}{' Adv' if r['adv'] else '    '} L{r['level']:<2} raw {r['raw']:6.0f} (pre {r['pre']:5.0f}, "
              f"{r['raw'] / r['pre']:.2f}x); full {r['legacy']:6.0f} / {r['revamp']:6.0f} ({ratio(r, 'legacy'):.2f}x / "
              f"{ratio(r, 'revamp'):.2f}x, +{100 * (r['legacy'] / r['raw'] - 1):.1f}% / +{100 * (r['revamp'] / r['raw'] - 1):.1f}% on raw); "
              f"blueprint {r['fee']:,}" + (f"; Shipyard fee 0-{SHIPYARD_FEE_MAX:,}" if r["shipyard"] else "")
              + ("; starter" if r["starter"] else "") + flag)
    print("Module ranks (the dearest family of each rank and row; the dockhand's starter items each on their own row, held to "
          "the band):")
    for r in module_rows():
        held = ratio(r, BAND_RULE)
        flag = ("  OVER BAND" if held > band else "") if r["starter"] else ("  (above the band, not held)" if held > band else "")
        ok &= held <= band or not r["starter"]
        print(f"  rank {r['rank']:2d} {r['row']:7s}{' capital' if r['capital'] else '        '} {r['family']:20s} raw {r['raw']:5.0f} "
              f"(pre {r['pre']:5.0f}); full {r['legacy']:6.0f} / {r['revamp']:6.0f} ({ratio(r, 'legacy'):.2f}x / {ratio(r, 'revamp'):.2f}x); "
              f"blueprint {r['fee']:,}" + ("; starter" if r["starter"] else "") + flag)
    print("Ammo (one craft, no components):")
    for r in ammo_rows():
        print(f"  {r['name']:32s} L{r['level']:<2} raw {r['raw']:5.0f}; full {r['legacy']:5.0f} / {r['revamp']:5.0f}")
    if not ok:
        print(f"  BAND FAIL: a hull or starter item costs more than {band}x its pre-revamp recipe on full cost")
    return ok


if __name__ == "__main__":
    if "--sim" in sys.argv:
        sys.exit(0 if report() else 1)
    print(__doc__)
