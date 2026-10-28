"""Space revamp Planetary Holdings (plan section 26, W14b): holding types, output, upkeep, upgrades, Processing Plant
chains, droid staff, events and the income check.

usage: python holdings.py            (writes the generated C# holding catalog, tuning and enums)
       python holdings.py --check    (exit 1 when a generated file is out of date)
       python holdings.py --sim      (prints the income table and the event simulation)

Every holding number lives here. bible_tabs.py calls holdings_tab(Sheet) to write the Planetary Holdings (Revamp) tab.
Draft numbers; tune in testing.

Sources:
- Goods, base prices and volumes: trade.py (COMMODITIES; the refined holding goods are trade.HOLDING_GOODS).
- Earning rate: credits an hour of space combat at each orbit tier (trade.py --sim, the W7a figures; the plan's reward
  parity rate), at the tier of the holding's dock exchange (trade.EXCHANGES). Frozen here so a loot change does not move
  holdings silently; --sim prints the live figures beside them.
- Droid staff: droids.py HOLDING_OUTPUT_PERCENT_PER_INDUSTRY (+0.5% per Industry, Efficiency Protocol applies).
- Asteroid Drill target (W13b, stations.py): a station drill pays under an hour of active mining a day, droid included.
"""
import math
import random
import sys
from pathlib import Path

import droids
import trade

REPO = Path(__file__).resolve().parents[2]

# ---------------------------------------------------------------------------------------------------------------
# Income target
# ---------------------------------------------------------------------------------------------------------------
# Credits an hour of space combat by orbit tier (trade.py --sim, W7a; plan section 13).
REFERENCE_RATE = {1: 4100, 2: 6300, 3: 8500, 4: 7600, 5: 9200, 6: 22200}
# Holdings are passive income and a character may own three, so all three at their best (level 3, the best droid, a
# Processing Plant) pay about 90 minutes of the tier's combat rate a day together: about 30 minutes each. A level 1
# holding's daily batch is worth GROSS_MINUTES of the rate at its dock's tier, at base price; upkeep takes UPKEEP_SHARE
# of the day's output value (the droid's share included: staff cost wages), so a level 1 holding nets about 10 minutes,
# level 3 about 15, level 3 with the best droid about 25 and with a Processing Plant about 30.
GROSS_MINUTES = 12
UPKEEP_SHARE = 0.15             # of the day's output value at base price, droid included, charged every day
LEVEL_OUTPUT = {1: 1.0, 2: 1.25, 3: 1.45}
PLANT_SHARE = 0.5               # the Processing Plant refines up to this share of each day's output
MAX_LEVEL = 3

# ---------------------------------------------------------------------------------------------------------------
# Buying, upgrades and limits (in days of a level 1 batch's value)
# ---------------------------------------------------------------------------------------------------------------
MAX_HOLDINGS = 3                # per character (plan section 26)
PURCHASE_DAYS = 10
SELL_BACK_SHARE = 0.5           # abandoning refunds this share of the price, less the upkeep owed
UPGRADE_CREDIT_DAYS = {2: 1.5, 3: 2}   # each pays back in two to three weeks (holdings.py --sim, Payback)
UPGRADE_GOODS_DAYS = {2: 1, 3: 1.5}   # split evenly between the level's goods
PLANT_CREDIT_DAYS = 2
PLANT_GOODS_DAYS = 1
STORAGE_DAYS = 7                # the dock warehouse holds this many days of output; a full warehouse stops output
FORECLOSE_DAYS = 14             # this many days of upkeep owed loses the holding
INPUT_STOCK_DAYS = 7            # the Processing Plant's delivered input is accepted up to this many days of recipes

# ---------------------------------------------------------------------------------------------------------------
# Events (plan section 26): one roll every EVENT_INTERVAL_DAYS per holding, while no setback or inspection is pending.
# ---------------------------------------------------------------------------------------------------------------
EVENT_INTERVAL_DAYS = 3
BOOM_CHANCE = 0.12              # the next day's output doubles
SETBACK_CHANCE = 0.05           # breakdown, blight, sandstorm, cave-in, storm or sickness: output stops until repaired
RAID_CHANCE = 0.05              # bandits carry off part of the warehouse (the ground fight is deferred)
CUSTOMS_CHANCE = 0.20           # contraband holdings only: pay a fine or lose the banned stock
BOOM_MULTIPLIER = 2
RAID_LOSS_SHARE = 0.5
CUSTOMS_FINE_RATE = trade.FINE_RATE   # of the banned stock's base value, as at a customs patrol
CUSTOMS_ANSWER_DAYS = 2
REPAIR_DAYS = 0.5               # repair goods worth this many days of a level 1 batch, at least REPAIR_MIN_UNITS
REPAIR_MIN_UNITS = 2
LOG_ENTRIES = 8

SETBACKS = {
    # enum: (id, name, what happens)
    "Breakdown": (1, "Breakdown", "The machinery broke down."),
    "Blight": (2, "Blight", "A blight struck the crop."),
    "Sandstorm": (3, "Sandstorm", "A sandstorm buried the works."),
    "CaveIn": (4, "Cave-in", "A tunnel caved in."),
    "Storm": (5, "Storm", "A storm wrecked the nets and pens."),
    "Sickness": (6, "Herd sickness", "Sickness spread through the herd."),
}

# ---------------------------------------------------------------------------------------------------------------
# Holding types (plan section 26)
# ---------------------------------------------------------------------------------------------------------------
# The Desalination Plant's chain is Station Fuel's second source (W13b makes it a CZ-220 export): water cracked into
# hyperfuel with reactor power, standing in for the plan's Reactor Cores + Tibanna Gas, since Tibanna is not a commodity.
# Ground sites reuse building-template interiors (already instance templates, kept out of saved locations) as each
# holding's site office; real outdoor sites need area design (deferred).
# id, enum, name, exchange, output, level 2 goods, level 3 goods, Processing Plant build goods,
# chain (own units, input, input units, refined, refined units), setback, repair good, customs inspections, site, text.
HOLDINGS = [
    (1, "CrystalMine", "Crystal Mine", "Viscara", "ViscaranCrystals",
     ["CzerkaMachineParts"], ["ReactorCores", "HullSections"], ["HullSections"],
     (5, "CzerkaMachineParts", 2, "FocusingLenses", 2), "CaveIn", "CzerkaMachineParts", False, "house_int_7",
     "A crystal mine in the Viscaran hills."),
    (2, "Apiary", "Apiary", "Viscara", "VelesHoney",
     ["AraciaTimber"], ["CzerkaMachineParts", "MedicalSupplies"], ["HullSections"],
     (2, "WitchHerbs", 1, "MedicalSupplies", 2), "Blight", "MedicalSupplies", False, "house_int_1",
     "Hives in the Veles groves."),
    (3, "AssemblyLine", "Assembly Line", "CZ220", "CzerkaMachineParts",
     ["JawaSalvage"], ["HullSections", "ViscaranCrystals"], ["HullSections"],
     (3, "ViscaranCrystals", 4, "FocusingLenses", 2), "Breakdown", "JawaSalvage", False, "player_rnd",
     "A leased Czerka assembly line on CZ-220."),
    (4, "KelpFishery", "Kelp Fishery", "MonCala", "Seafood",
     ["AraciaTimber"], ["CzerkaMachineParts", "NeonSalt"], ["CzerkaMachineParts"],
     (5, "Grain", 3, "RationPacks", 3), "Storm", "CzerkaMachineParts", False, "house_int_4",
     "Kelp beds and fish pens off the Mon Cala coast."),
    (5, "DesalinationPlant", "Desalination Plant", "MonCala", "PurifiedWater",
     ["CzerkaMachineParts"], ["ReactorCores", "JawaSalvage"], ["AraciaTimber"],
     (4, "ReactorCores", 2, "StationFuel", 3), "Breakdown", "CzerkaMachineParts", False, "player_rnd",
     "A desalination plant on the Mon Cala shore; its reactors can crack water into station fuel."),
    (6, "TimberCamp", "Timber Camp", "Hutlar", "AraciaTimber",
     ["CzerkaMachineParts"], ["ReactorCores", "MedicalSupplies"], ["HullSections"],
     (2, "CzerkaMachineParts", 1, "PrefabShelters", 1), "Breakdown", "CzerkaMachineParts", False, "house_int_10",
     "A logging camp in Hutlar's Aracia forests."),
    (7, "ByyskRanch", "Byysk Ranch", "Hutlar", "ByyskHides",
     ["Grain"], ["CzerkaMachineParts", "MedicalSupplies"], ["HullSections"],
     (4, "WitchHerbs", 3, "MedicalSupplies", 6), "Sickness", "MedicalSupplies", False, "house_int_1",
     "Byysk pens and a tannery on Hutlar."),
    (8, "MoistureFarm", "Moisture Farm", "Tatooine", "PurifiedWater",
     ["CzerkaMachineParts"], ["CzerkaMachineParts", "ReactorCores"], ["HullSections"],
     (1, "Grain", 1, "RationPacks", 1), "Sandstorm", "CzerkaMachineParts", False, "house_int_4",
     "Vaporators on the Tatooine flats."),
    (9, "ScrapYard", "Scrap Yard", "Tatooine", "JawaSalvage",
     ["CzerkaMachineParts"], ["ReactorCores", "HullSections"], ["AraciaTimber"],
     (3, "AraciaTimber", 1, "PrefabShelters", 1), "Sandstorm", "CzerkaMachineParts", False, "player_rnd",
     "A Jawa trading scrap yard outside Anchorhead."),
    (10, "SpiceLab", "Spice Lab", "SmugglersMoon", "Moonspice",
     ["MedicalSupplies"], ["CzerkaMachineParts", "ReactorCores"], ["HullSections"],
     (1, "AbdamarylliaExtract", 1, "Glitterstim", 1), "Breakdown", "CzerkaMachineParts", True, "player_rnd",
     "A hidden spice lab in the Smuggler's Moon undercity."),
    (11, "SaltWorks", "Salt Works", "SmugglersMoon", "NeonSalt",
     ["CzerkaMachineParts"], ["ReactorCores", "PurifiedWater"], ["HullSections"],
     (5, "Seafood", 3, "RationPacks", 3), "Breakdown", "CzerkaMachineParts", False, "house_int_10",
     "Evaporation pans on the Smuggler's Moon salt flats."),
    (12, "ExcavationDig", "Excavation Dig", "Korriban", "SithRelics",
     ["CzerkaMachineParts"], ["ReactorCores", "MedicalSupplies"], ["HullSections"],
     (1, "EncryptedDataChips", 2, "SithHolocrons", 1), "CaveIn", "CzerkaMachineParts", True, "house_int_7",
     "A licensed dig among the Korriban tombs."),
    (13, "HerbGrove", "Herb Grove", "Dathomir", "WitchHerbs",
     ["PurifiedWater"], ["CzerkaMachineParts", "Grain"], ["HullSections"],
     (3, "ByyskHides", 4, "MedicalSupplies", 6), "Blight", "MedicalSupplies", False, "house_int_1",
     "Herb beds tended at the edge of the Dathomir jungle."),
    (14, "GrainFarm", "Grain Farm", "Dantooine", "Grain",
     ["CzerkaMachineParts"], ["ReactorCores", "PurifiedWater"], ["AraciaTimber"],
     (1, "PurifiedWater", 1, "RationPacks", 1), "Blight", "MedicalSupplies", False, "house_int_4",
     "Grain fields on the Dantooine plains."),
    (15, "BanthaRanch", "Bantha Ranch", "Dantooine", "BanthaMilk",
     ["AraciaTimber"], ["CzerkaMachineParts", "MedicalSupplies"], ["HullSections"],
     (2, "NeonSalt", 1, "AgedBanthaCheese", 1), "Sickness", "MedicalSupplies", False, "house_int_10",
     "A bantha ranch and dairy on Dantooine."),
]
REFINE_PREMIUM_RANGE = (1.10, 1.25)   # a refined batch is worth more than both inputs (plan section 26), by about a fifth


def com(name):
    return trade.commodity(name)


def base(name):
    return com(name)[3]


def tier(h):
    return trade.exchange(h[3])[5]


def rate(h):
    return REFERENCE_RATE[tier(h)]


def round_half_up(x):
    return int(math.floor(x + 0.5))


def gross_target(h):
    return rate(h) * GROSS_MINUTES / 60


def batch_units(h):
    """A level 1 holding's daily batch, in units of its good."""
    return max(1, round_half_up(gross_target(h) / base(h[4])))


def day_value(h):
    return batch_units(h) * base(h[4])


def round_to(x, step):
    return int(round_half_up(x / step) * step)


def purchase_price(h):
    return round_to(PURCHASE_DAYS * day_value(h), 100)


def upkeep(h, level, percent=0):
    return round_half_up(UPKEEP_SHARE * daily_units(h, level, percent) * base(h[4]))


def goods_units(h, goods, days):
    return {g: max(1, round_half_up(days * day_value(h) / len(goods) / base(g))) for g in goods}


def upgrade(h, level):
    goods = h[5] if level == 2 else h[6]
    return dict(credits=round_to(UPGRADE_CREDIT_DAYS[level] * day_value(h), 100),
                goods=goods_units(h, goods, UPGRADE_GOODS_DAYS[level]))


def plant(h):
    return dict(credits=round_to(PLANT_CREDIT_DAYS * day_value(h), 100), goods=goods_units(h, h[7], PLANT_GOODS_DAYS))


def repair_units(h):
    return max(REPAIR_MIN_UNITS, round_half_up(REPAIR_DAYS * day_value(h) / base(h[10])))


# ---------------------------------------------------------------------------------------------------------------
# Rules (mirror Service/HoldingService/HoldingRules.cs)
# ---------------------------------------------------------------------------------------------------------------
def droid_percent(industry, efficiency_rank=0):
    return round_half_up(droids.HOLDING_OUTPUT_PERCENT_PER_INDUSTRY * droids.output_industry(industry, efficiency_rank))


def apply_percent(amount, percent):
    return amount if percent <= 0 else max(amount, round_half_up(amount * (1 + percent / 100)))


def daily_units(h, level, percent=0):
    return apply_percent(round_half_up(batch_units(h) * LEVEL_OUTPUT[level]), percent)


def refine(h, own_units, input_units=None):
    """Recipes run on a day's batch: (refined units, own units used, input units used). Unlimited input when None."""
    a, _, b, _, c = h[8]
    recipes = own_units // a if input_units is None else min(own_units // a, input_units // b)
    return recipes * c, recipes * a, recipes * b


def daily_value(h, level, percent=0, processing=False):
    """A day's output at base price; with the Processing Plant, less the input bought at its base price."""
    units = daily_units(h, level, percent)
    if not processing:
        return units * base(h[4])
    refined, used, inputs = refine(h, int(units * PLANT_SHARE))
    return (units - used) * base(h[4]) + refined * base(h[8][3]) - inputs * base(h[8][1])


def premium(h):
    a, inp, b, out, c = h[8]
    return c * base(out) / (a * base(h[4]) + b * base(inp))


def net(h, level, percent=0, processing=False):
    return daily_value(h, level, percent, processing) - upkeep(h, level, percent)


def cost_value(cost):
    return cost["credits"] + sum(n * base(g) for g, n in cost["goods"].items())


MAX_PAYBACK_DAYS = 28   # every upgrade pays back within a few weeks


def payback(h):
    """Days each upgrade takes to pay back its credits and goods (at base price) from the extra daily net, without a
    droid; the Processing Plant at level 3."""
    return {"L2": cost_value(upgrade(h, 2)) / (net(h, 2) - net(h, 1)),
            "L3": cost_value(upgrade(h, 3)) / (net(h, 3) - net(h, 2)),
            "Plant": cost_value(plant(h)) / (net(h, 3, 0, True) - net(h, 3))}


def minutes(h, level, percent=0, processing=False):
    """Daily net value (output less upkeep) in minutes of the dock tier's earning rate."""
    return (daily_value(h, level, percent, processing) - upkeep(h, level, percent)) / rate(h) * 60


BEST_DROID_PERCENT = droid_percent(droids.MAXIMUM_INDUSTRY, 2)
MID_DROID_PERCENT = droid_percent(50)
MAX_BEST_MINUTES = 27   # level 3 with the best droid (no plant): about 25 minutes
MAX_PLANT_MINUTES = 35  # and with a Processing Plant: about 30 minutes


# ---------------------------------------------------------------------------------------------------------------
# Checks
# ---------------------------------------------------------------------------------------------------------------
def problems():
    out = []
    if len({h[1] for h in HOLDINGS}) != len(HOLDINGS):
        out.append("duplicate holding")
    for h in HOLDINGS:
        ex = trade.exchange(h[3])
        for g in h[5] + h[6] + h[7] + [h[8][1], h[10]]:
            if trade.makes(ex, g):
                out.append(f"{h[1]}: {g} is made at its own dock; upgrades, inputs and repairs come from other planets")
        if not REFINE_PREMIUM_RANGE[0] <= premium(h) <= REFINE_PREMIUM_RANGE[1]:
            out.append(f"{h[1]}: refined premium {premium(h):.2f} outside {REFINE_PREMIUM_RANGE}")
        refined = h[8][3]
        if trade.is_holding_good(refined) and h[3] not in trade.HOLDING_GOOD_SOURCES[refined]:
            out.append(f"{h[1]}: {h[3]} is missing from trade.HOLDING_GOOD_SOURCES[{refined}]")
        if h[9] not in SETBACKS:
            out.append(f"{h[1]}: unknown setback {h[9]}")
        if h[11] and not com(h[4])[5]:
            out.append(f"{h[1]}: customs inspections on a legal good")
        if minutes(h, MAX_LEVEL, BEST_DROID_PERCENT) > MAX_BEST_MINUTES:
            out.append(f"{h[1]}: level 3 with the best droid nets {minutes(h, MAX_LEVEL, BEST_DROID_PERCENT):.0f} minutes a day")
        if minutes(h, MAX_LEVEL, BEST_DROID_PERCENT, True) > MAX_PLANT_MINUTES:
            out.append(f"{h[1]}: with a Processing Plant it nets {minutes(h, MAX_LEVEL, BEST_DROID_PERCENT, True):.0f} minutes a day")
        for name, days in payback(h).items():
            if days > MAX_PAYBACK_DAYS:
                out.append(f"{h[1]}: {name} pays back in {days:.0f} days")
    for good, sources in trade.HOLDING_GOOD_SOURCES.items():
        for ex in sources:
            if not any(h[3] == ex and h[8][3] == good for h in HOLDINGS):
                out.append(f"trade.HOLDING_GOOD_SOURCES[{good}] lists {ex}, where no holding refines it")
    return out


# ---------------------------------------------------------------------------------------------------------------
# Event simulation (assumed owner: visits every OWNER_VISIT_DAYS to collect, repairs a setback and pays a fine then)
# ---------------------------------------------------------------------------------------------------------------
OWNER_VISIT_DAYS = 7
SIM_DAYS = 3000


def simulate_events(h, level=1, seed=1):
    """Share of the event-free output a holding delivers over SIM_DAYS (customs fines counted as lost output)."""
    rng = random.Random(seed)
    stock, delivered, fines, boom, stopped, countdown = 0, 0, 0.0, 0, False, EVENT_INTERVAL_DAYS
    capacity = STORAGE_DAYS * daily_units(h, level)
    for day in range(1, SIM_DAYS + 1):
        countdown -= 1
        if countdown <= 0:
            countdown = EVENT_INTERVAL_DAYS
            if not stopped:
                r = rng.random()
                if r < BOOM_CHANCE:
                    boom += 1
                elif r < BOOM_CHANCE + SETBACK_CHANCE:
                    stopped = True
                elif r < BOOM_CHANCE + SETBACK_CHANCE + RAID_CHANCE:
                    stock -= round_half_up(stock * RAID_LOSS_SHARE)
                elif h[11] and r < BOOM_CHANCE + SETBACK_CHANCE + RAID_CHANCE + CUSTOMS_CHANCE:
                    fines += CUSTOMS_FINE_RATE * stock
        if not stopped:
            units = daily_units(h, level) * (BOOM_MULTIPLIER if boom else 1)
            boom = max(0, boom - 1)
            stock = min(capacity, stock + units)
        if day % OWNER_VISIT_DAYS == 0:
            delivered += stock
            stock = 0
            stopped = False
    return (delivered - fines) / (SIM_DAYS * daily_units(h, level))


# ---------------------------------------------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------------------------------------------
def income_rows():
    rows = []
    for h in HOLDINGS:
        rows.append(dict(
            name=h[2], dock=trade.exchange(h[3])[1], tier=tier(h), rate=rate(h), good=com(h[4])[2], units=batch_units(h),
            value=day_value(h), upkeep=upkeep(h, 1), price=purchase_price(h),
            l1=minutes(h, 1), l2=minutes(h, 2), l3=minutes(h, 3), l3_mid=minutes(h, 3, MID_DROID_PERCENT),
            l3_best=minutes(h, 3, BEST_DROID_PERCENT), l3_plant=minutes(h, 3, BEST_DROID_PERCENT, True),
            events=simulate_events(h), payback=payback(h)))
    return rows


def describe(goods):
    return ", ".join(f"{n} {com(g)[2]}" for g, n in goods.items())


def report():
    print("Reference rate (frozen) against the live trade.py space combat rate:")
    for t in range(1, 7):
        live = trade.combat_credits_per_hour(t)[1]
        drift = "" if abs(live - REFERENCE_RATE[t]) <= 0.1 * REFERENCE_RATE[t] else "  <- drifted over 10%: retune"
        print(f"  T{t}: {REFERENCE_RATE[t]:>6} (live {live:,.0f}){drift}")
    print(f"\nDaily net (output at base price less upkeep) in minutes of the dock tier's rate. A droid with Industry 50 adds "
          f"{MID_DROID_PERCENT}%, the best (100, Efficiency Protocol II) {BEST_DROID_PERCENT}%. The plant column buys its "
          f"input at base price. Events: the share of event-free output kept by an owner who visits every "
          f"{OWNER_VISIT_DAYS} days.")
    print(f"{'Holding':20} {'Dock':16} T {'Good':20} {'Units':>5} {'Value':>6} {'Upkp':>5} {'Price':>7}  "
          f"{'L1':>4} {'L2':>4} {'L3':>4} {'L3+50':>5} {'L3+best':>7} {'+plant':>6} {'events':>6}  payback L2/L3/plant (days)")
    for r in income_rows():
        print(f"{r['name']:20} {r['dock']:16} {r['tier']} {r['good']:20} {r['units']:>5} {r['value']:>6} {r['upkeep']:>5} "
              f"{r['price']:>7}  {r['l1']:>4.0f} {r['l2']:>4.0f} {r['l3']:>4.0f} {r['l3_mid']:>5.0f} {r['l3_best']:>7.0f} "
              f"{r['l3_plant']:>6.0f} {r['events']:>6.0%}  " + "/".join(f"{d:.0f}" for d in r["payback"].values()))
    rows = income_rows()
    print()
    print(f"One character with three holdings, the average of every type: level 1 {3 * sum(r['l1'] for r in rows) / len(rows):.0f} "
          f"minutes a day, level 3 {3 * sum(r['l3'] for r in rows) / len(rows):.0f}, level 3 with the best droid "
          f"{3 * sum(r['l3_best'] for r in rows) / len(rows):.0f}, and with Processing Plants {3 * sum(r['l3_plant'] for r in rows) / len(rows):.0f}.")
    print("\nChains (refined value against both inputs at base price):")
    for h in HOLDINGS:
        a, inp, b, out, c = h[8]
        print(f"  {h[2]:20} {a} {com(h[4])[2]} + {b} {com(inp)[2]} -> {c} {com(out)[2]}  x{premium(h):.2f}")
    print("\nUpgrades, Processing Plant and repairs:")
    for h in HOLDINGS:
        u2, u3, p = upgrade(h, 2), upgrade(h, 3), plant(h)
        print(f"  {h[2]:20} L2 {u2['credits']} cr + {describe(u2['goods'])}; L3 {u3['credits']} cr + {describe(u3['goods'])}; "
              f"plant {p['credits']} cr + {describe(p['goods'])}; {SETBACKS[h[9]][1]}: {repair_units(h)} {com(h[10])[2]}")
    issues = problems()
    print("\nChecks: " + ("all pass" if not issues else "; ".join(issues)))


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/holdings.py. Do not edit by hand: change holdings.py and regenerate.
// </auto-generated>
"""


def cs_string(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def cs_double(x):
    return repr(float(x))


def cs_goods(goods):
    items = ", ".join(f"{{ CommodityType.{g}, {n} }}" for g, n in goods.items())
    return f"new Dictionary<CommodityType, int> {{ {items} }}"


def enum_cs():
    lines = [HEADER.rstrip("\n"), "namespace SWLOR.Game.Server.Service.HoldingService", "{",
             "    /// <summary>The Planetary Holding types (plan section 26).</summary>", "    public enum HoldingType", "    {",
             "        Invalid = 0,"]
    lines += [f"        {h[1]} = {h[0]}," for h in HOLDINGS]
    lines += ["    }", "}", ""]
    return "\n".join(lines)


def setback_enum_cs():
    lines = [HEADER.rstrip("\n"), "namespace SWLOR.Game.Server.Service.HoldingService", "{",
             "    /// <summary>What stops a holding's output until repair goods are delivered.</summary>",
             "    public enum HoldingSetbackType", "    {", "        Invalid = 0,"]
    lines += [f"        {k} = {v[0]}," for k, v in SETBACKS.items()]
    lines += ["    }", "}", ""]
    return "\n".join(lines)


def catalog_cs():
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "using SWLOR.Game.Server.Service.HoldingService;",
             "using SWLOR.Game.Server.Service.TradeService;", "",
             "namespace SWLOR.Game.Server.Feature.HoldingDefinition", "{",
             "    /// <summary>", "    /// The Planetary Holding types (plan section 26): what each makes, costs and needs.", "    /// </summary>",
             "    public static class HoldingCatalog", "    {",
             "        public static readonly IReadOnlyList<HoldingDetail> Holdings = new List<HoldingDetail>", "        {"]
    for h in HOLDINGS:
        a, inp, b, out, c = h[8]
        up2, up3, pl = upgrade(h, 2), upgrade(h, 3), plant(h)
        lines += [
            "            new HoldingDetail",
            "            {",
            f"                Type = HoldingType.{h[1]}, Name = {cs_string(h[2])}, Exchange = TradeExchangeType.{h[3]},",
            f"                Output = CommodityType.{h[4]}, BatchUnits = {batch_units(h)}, PurchasePrice = {purchase_price(h)},",
            "                Upgrades = new List<HoldingUpgradeDetail>",
            "                {",
            f"                    new HoldingUpgradeDetail {{ Level = 2, Credits = {up2['credits']}, Goods = {cs_goods(up2['goods'])} }},",
            f"                    new HoldingUpgradeDetail {{ Level = 3, Credits = {up3['credits']}, Goods = {cs_goods(up3['goods'])} }},",
            "                },",
            f"                Plant = new HoldingUpgradeDetail {{ Level = 0, Credits = {pl['credits']}, Goods = {cs_goods(pl['goods'])} }},",
            f"                Chain = new HoldingChainDetail {{ OwnUnits = {a}, Input = CommodityType.{inp}, InputUnits = {b}, "
            f"Refined = CommodityType.{out}, RefinedUnits = {c} }},",
            f"                Setback = HoldingSetbackType.{h[9]}, RepairGood = CommodityType.{h[10]}, RepairUnits = {repair_units(h)},",
            f"                CustomsInspections = {'true' if h[11] else 'false'}, SiteAreaResref = {cs_string(h[12])},",
            f"                Description = {cs_string(h[13])},",
            "            },",
        ]
    lines += ["        };", "    }", "}", ""]
    return "\n".join(lines)


def tuning_cs():
    rates = ", ".join(str(REFERENCE_RATE[t]) for t in range(1, 7))
    names = ", ".join(f"{{ HoldingSetbackType.{k}, {cs_string(v[1])} }}" for k, v in SETBACKS.items())
    texts = ", ".join(f"{{ HoldingSetbackType.{k}, {cs_string(v[2])} }}" for k, v in SETBACKS.items())
    return "\n".join([
        HEADER.rstrip("\n"), "using System.Collections.Generic;", "", "namespace SWLOR.Game.Server.Service.HoldingService", "{",
        "    /// <summary>The Planetary Holdings numbers (plan section 26).</summary>",
        "    public static class HoldingTuning", "    {",
        f"        public const int MaximumHoldings = {MAX_HOLDINGS};",
        f"        public const int MaximumLevel = {MAX_LEVEL};",
        "        /// <summary>Output by level (index 1-3).</summary>",
        "        public static readonly double[] LevelOutput = { 0.0, " + ", ".join(cs_double(LEVEL_OUTPUT[lv]) for lv in range(1, MAX_LEVEL + 1)) + " };",
        f"        public const double UpkeepShare = {cs_double(UPKEEP_SHARE)};",
        f"        public const double PlantShare = {cs_double(PLANT_SHARE)};",
        f"        public const double SellBackShare = {cs_double(SELL_BACK_SHARE)};",
        f"        public const int StorageDays = {STORAGE_DAYS};",
        f"        public const int ForeclosureDays = {FORECLOSE_DAYS};",
        f"        public const int InputStockDays = {INPUT_STOCK_DAYS};",
        "",
        f"        public const int EventIntervalDays = {EVENT_INTERVAL_DAYS};",
        f"        public const double BoomChance = {cs_double(BOOM_CHANCE)};",
        f"        public const double SetbackChance = {cs_double(SETBACK_CHANCE)};",
        f"        public const double RaidChance = {cs_double(RAID_CHANCE)};",
        f"        public const double CustomsChance = {cs_double(CUSTOMS_CHANCE)};",
        f"        public const int BoomMultiplier = {BOOM_MULTIPLIER};",
        f"        public const double RaidLossShare = {cs_double(RAID_LOSS_SHARE)};",
        f"        public const double CustomsFineRate = {cs_double(CUSTOMS_FINE_RATE)};",
        f"        public const int CustomsAnswerDays = {CUSTOMS_ANSWER_DAYS};",
        f"        public const int LogEntries = {LOG_ENTRIES};",
        "",
        "        /// <summary>Credits an hour of space combat by orbit tier (index 1-6): the income check's reference.</summary>",
        f"        public static readonly int[] ReferenceRateByTier = {{ 0, {rates} }};",
        f"        public const int GrossMinutes = {GROSS_MINUTES};",
        f"        public const int BestDroidPercent = {BEST_DROID_PERCENT};",
        f"        public const int MaximumBestDroidMinutes = {MAX_BEST_MINUTES};",
        f"        public const int MaximumPlantMinutes = {MAX_PLANT_MINUTES};",
        "",
        f"        public static readonly IReadOnlyDictionary<HoldingSetbackType, string> SetbackNames = new Dictionary<HoldingSetbackType, string> {{ {names} }};",
        f"        public static readonly IReadOnlyDictionary<HoldingSetbackType, string> SetbackTexts = new Dictionary<HoldingSetbackType, string> {{ {texts} }};",
        "    }", "}", ""])


WRITES = {
    "SWLOR.Game.Server/Service/HoldingService/HoldingType.cs": enum_cs,
    "SWLOR.Game.Server/Service/HoldingService/HoldingSetbackType.cs": setback_enum_cs,
    "SWLOR.Game.Server/Service/HoldingService/HoldingTuning.cs": tuning_cs,
    "SWLOR.Game.Server/Feature/HoldingDefinition/HoldingCatalog.cs": catalog_cs,
}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab: bible_tabs.py calls holdings_tab(Sheet) to write the Planetary Holdings (Revamp) tab.
# ---------------------------------------------------------------------------------------------------------------
def holdings_tab(Sheet):
    s = Sheet("Planetary Holdings (Revamp)", [22, 16, 22, 12, 12, 12, 14, 60])
    s.title("Planetary Holdings (Revamp): holding types, output, upkeep, upgrades, Processing Plants, droid staff and events")
    s.note("Dev Status: Proposed (space revamp W14b draft). Generated by tools/SpaceRevampBalance/holdings.py; plan section 26.")
    s.blank()
    s.title("Holding types (level 1)")
    s.table(["Holding", "Dock", "Good", "Units / day", "Value / day", "Upkeep / day", "Price", "Notes"],
            [[h[2], trade.exchange(h[3])[1], com(h[4])[2], batch_units(h), day_value(h), upkeep(h, 1), purchase_price(h),
              h[13] + (" Customs inspections." if h[11] else "")] for h in HOLDINGS])
    s.title("Upgrades and Processing Plants (goods delivered to the holding's dock)")
    s.table(["Holding", "Level 2", "Level 3", "Processing Plant", "Chain", "", "Premium", "Repair goods"],
            [[h[2], f"{upgrade(h, 2)['credits']} cr, {describe(upgrade(h, 2)['goods'])}",
              f"{upgrade(h, 3)['credits']} cr, {describe(upgrade(h, 3)['goods'])}",
              f"{plant(h)['credits']} cr, {describe(plant(h)['goods'])}",
              f"{h[8][0]} own + {h[8][2]} {com(h[8][1])[2]} -> {h[8][4]} {com(h[8][3])[2]}", "", f"x{premium(h):.2f}",
              f"{SETBACKS[h[9]][1]}: {repair_units(h)} {com(h[10])[2]}"] for h in HOLDINGS])
    s.title("Rules")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Holdings per character", MAX_HOLDINGS, "", "", "", "", "", "bought from the Dock Master's Planetary Holdings menu"],
        ["Daily batch", f"{GROSS_MINUTES} min of the tier rate", "", "", "", "", "", "level 1, at base price"],
        ["Level output", ", ".join(f"L{lv} x{v}" for lv, v in LEVEL_OUTPUT.items()), "", "", "", "", "", "upkeep scales the same"],
        ["Processing Plant", f"{PLANT_SHARE:.0%} of the day's output", "", "", "", "", "", "refined with delivered input; the rest stays raw"],
        ["Upkeep", f"{UPKEEP_SHARE:.0%} of the day's output", "", "", "", "", "",
         f"owed daily and paid on collection; {FORECLOSE_DAYS} days owed loses the holding"],
        ["Price", f"{PURCHASE_DAYS} days of output", "", "", "", "", "", f"abandoning refunds {SELL_BACK_SHARE:.0%} less the upkeep owed"],
        ["Upgrades", f"L2 {UPGRADE_CREDIT_DAYS[2]} + {UPGRADE_GOODS_DAYS[2]} days", f"L3 {UPGRADE_CREDIT_DAYS[3]} + {UPGRADE_GOODS_DAYS[3]} days",
         f"plant {PLANT_CREDIT_DAYS} + {PLANT_GOODS_DAYS} days", "", "", "", "credits + goods, in days of level 1 output"],
        ["Warehouse", f"{STORAGE_DAYS} days", "", "", "", "", "", "a full warehouse stops output"],
        ["Droid staff", f"+{droids.HOLDING_OUTPUT_PERCENT_PER_INDUSTRY:g}% / Industry", f"+{BEST_DROID_PERCENT}% best", "", "", "", "",
         "Efficiency Protocol applies; the droid stays at its post"],
        ["Events", f"every {EVENT_INTERVAL_DAYS} days", f"boom {BOOM_CHANCE:.0%}", f"setback {SETBACK_CHANCE:.0%}", f"raid {RAID_CHANCE:.0%}",
         f"customs {CUSTOMS_CHANCE:.0%}", "", f"boom x{BOOM_MULTIPLIER} for a day; a raid takes {RAID_LOSS_SHARE:.0%} of the stock; "
         f"customs fine {CUSTOMS_FINE_RATE:.0%} of the banned stock's base value within {CUSTOMS_ANSWER_DAYS} days or it is confiscated"],
    ])
    s.title("Income (daily net, minutes of the dock tier's space combat rate)")
    s.table(["Holding", "Tier rate", "L1", "L2", "L3", "L3 + best droid", "+ plant", "Events (output kept)"],
            [[r["name"], r["rate"], round(r["l1"]), round(r["l2"]), round(r["l3"]), round(r["l3_best"]), round(r["l3_plant"]),
              f"{r['events']:.0%}"] for r in income_rows()])
    s.title("Upgrade payback (days of the extra daily net, no droid; goods at base price)")
    s.table(["Holding", "Level 2", "Level 3", "Processing Plant", "", "", "", "Notes"],
            [[r["name"], round(r["payback"]["L2"]), round(r["payback"]["L3"]), round(r["payback"]["Plant"]), "", "", "",
              "a character owns at most three holdings: about 90 minutes a day at their best"] for r in income_rows()])
    return s


def main():
    if "--sim" in sys.argv:
        report()
        return
    issues = problems()
    if issues:
        sys.exit("holdings checks failed: " + "; ".join(issues))
    check = "--check" in sys.argv
    stale = []
    for rel, build in WRITES.items():
        text = build()
        path = REPO / rel
        if path.exists() and path.read_text(encoding="utf-8") == text:
            continue
        stale.append(rel)
        if not check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8", newline="\r\n")
    print(f"{len(HOLDINGS)} holding types, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
