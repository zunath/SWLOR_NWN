"""Space revamp trade core (plan section 13, W7a): commodities, exchanges, pricing, flooding and recovery.

usage: python trade.py            (writes the generated C# trade catalog and tuning)
       python trade.py --check    (exit 1 when a generated file is out of date)
       python trade.py --sim      (prints the round-trip simulation and the space combat comparison)

Every trade number lives here. bible_tabs.py calls trade_tab(Sheet) to write the Trade (Revamp) tab.

Sources:
- Base prices: the Cost of a comparable existing item blueprint (Module/uti), named per commodity in SOURCE. NPC stores
  buy items back at 40% of Cost (every Module/utm store but a few has MarkDown 40), so one commodity unit trades near
  the value of one comparable item. Bulk foods and water are a crate of 5 (x5), because one jar of honey (5) or one
  bottle of water (13) is not freight.
- Routes: the starlane graph in Module/git (tele_obj placeables with a DESTINATION landing, read the way
  ShipAccessRulesTests reads them) and the orbit tiers in SpaceSpawnDefinition (t1-t6 spawns per orbit).
- Space combat credits per hour: SpaceLootTableDefinition (gold and item drops) with the "SPACE_BASIC_Tn, 60, 3"
  LOOT_TABLE local every t*fighter / t*bomber creature carries (Module/utc), items sold at the 40% store markdown.
"""
import json
import math
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]

# ---------------------------------------------------------------------------------------------------------------
# Commodities
# ---------------------------------------------------------------------------------------------------------------
# id, enum name, display name, base price, volume per unit, banned by (lawful authorities), description.
COMMODITIES = [
    (1, "VelesHoney", "Veles Honey", 25, 1, [], "Wild honey from the Veles groves, packed in sealed jars."),
    (2, "ViscaranCrystals", "Viscaran Crystals", 40, 1, [], "Raw power crystals cut from Viscara's hills."),
    (3, "KinrathSilk", "Kinrath Silk", 125, 1, [], "Spun kinrath silk, prized by Mon Cala tailors."),
    (4, "PurifiedWater", "Purified Water", 65, 2, [], "Drums of filtered drinking water."),
    (5, "MedicalSupplies", "Medical Supplies", 55, 1, [], "Kolto packs, bacta gel and field dressings."),
    (6, "ReactorCores", "Reactor Cores", 175, 2, [], "Shielded Czerka reactor cores for ships and settlements."),
    (7, "CzerkaMachineParts", "Czerka Machine Parts", 63, 2, [], "Crated bearings, servos and pumps from Czerka foundries."),
    (8, "Grain", "Grain", 60, 2, [], "Sacks of Dantooine grain."),
    (9, "AraciaTimber", "Aracia Timber", 80, 2, [], "Seasoned Aracia logs from Hutlar."),
    (10, "Seafood", "Seafood", 40, 1, [], "Chilled catch from the Mon Cala seas."),
    (11, "HullSections", "Hull Sections", 75, 3, [], "Prefabricated hull plates from the Mon Cala yards."),
    (12, "NeonSalt", "Neon Salt", 50, 1, [], "Glowing mineral salt from the Smuggler's Moon flats."),
    (13, "ByyskHides", "Byysk Hides", 39, 1, [], "Tanned hides of Hutlar's byysk."),
    (14, "BanthaHides", "Bantha Hides", 50, 1, [], "Sun-cured bantha hides from Tatooine."),
    (15, "BeastHides", "Beast Hides", 69, 1, [], "Thick hides of Dathomir's great beasts."),
    (16, "JawaSalvage", "Jawa Salvage", 50, 2, [], "Sorted electronics salvage traded by Jawa clans."),
    (17, "Moonspice", "Moonspice", 250, 2, ["Republic", "Czerka", "Sith"], "Refined spice from the Smuggler's Moon labs."),
    (18, "EncryptedDataChips", "Encrypted Data Chips", 151, 1, ["Republic", "Czerka"], "Sliced data chips of unknown origin."),
    (19, "SithRelics", "Sith Relics", 782, 6, ["Republic"], "Artifacts taken from the tombs of Korriban."),
    (20, "WeaponsCrates", "Weapons Crates", 376, 3, ["Republic"], "Sealed crates of Czerka rifles and blasters."),
    (21, "AbdamarylliaExtract", "Abdamaryllia Extract", 250, 2, [], "Distilled extract of Korriban's abdamaryllia flower."),
    (22, "WitchHerbs", "Witch Herbs", 42, 1, [], "Dried herbs gathered by the witches of Dathomir."),
    (23, "BanthaMilk", "Bantha Milk", 65, 2, [], "Chilled churns of Dantooine bantha milk."),
]

# Comparable item blueprint and its Cost for each base price.
SOURCE = {
    "VelesHoney": "v_honey (Viscaran Honey) 5, crate of 5",
    "ViscaranCrystals": "power_crystal_go (Good Quality Power Crystal) 40",
    "KinrathSilk": "kinrath_silk (Kinrath Silk) 125",
    "PurifiedWater": "water_1 (Purified Water) 13, crate of 5",
    "MedicalSupplies": "m_enhancer3 (Medical Enhancer III) 55",
    "ReactorCores": "reactor_core (Salvaged Reactor Core) 175",
    "CzerkaMachineParts": "scrap_metal (Scrap Metal) 63",
    "Grain": "flour (Flour) 12, crate of 5",
    "AraciaTimber": "aracia_wood (Aracia Wood) 80",
    "Seafood": "blindfish and the other catch fish 40",
    "HullSections": "hull_plating (Hull Plating) 75",
    "NeonSalt": "no comparable item; between fine_wood 40 and bread_flour 63",
    "ByyskHides": "lth_good (Good Quality Leather) 39",
    "BanthaHides": "lth_premium (Premium Leather) 50",
    "BeastHides": "lth_flawless (Flawless Leather) 69",
    "JawaSalvage": "elec_imperfect (Imperfect Electronics) 50",
    "Moonspice": "allspice_1 (Allspice I) 250",
    "EncryptedDataChips": "night_datapada (Datapad A) 151",
    "SithRelics": "the tier 5 capstone relics (Anchor Relic and others) 782",
    "WeaponsCrates": "byysk_rifle and the other tier 3 Byysk weapons 376",
    "AbdamarylliaExtract": "abdamaryllia (Abdamaryllia) 250",
    "WitchHerbs": "herb_question (Questionable Herb) 42",
    "BanthaMilk": "milk_1 (Fresh Kath Hound Milk) 13, crate of 5",
}

AUTHORITIES = ["None", "Republic", "Czerka", "Sith"]

# ---------------------------------------------------------------------------------------------------------------
# Exchanges (plan section 13 dock table). Exports are produced (cheap), imports consumed (pays well).
# ---------------------------------------------------------------------------------------------------------------
# enum name, display name, planet types, orbit area, authority, orbit tier, produces, consumes.
EXCHANGES = [
    ("Viscara", "Viscara", ["Viscara"], "viscaraorbit", "Republic", 1,
     ["VelesHoney", "ViscaranCrystals", "KinrathSilk"], ["PurifiedWater", "MedicalSupplies"]),
    ("CZ220", "CZ-220", ["CZ220"], "viscaraorbit", "Czerka", 1,
     ["ReactorCores", "CzerkaMachineParts", "WeaponsCrates"], ["Grain", "AraciaTimber"]),
    ("MonCala", "Mon Cala", ["MonCala"], "moncalaorbit", "Republic", 2,
     ["Seafood", "PurifiedWater", "HullSections", "MedicalSupplies"], ["KinrathSilk", "AraciaTimber", "NeonSalt"]),
    ("Hutlar", "Hutlar", ["Hutlar"], "hutlar_orbit", "None", 3,
     ["AraciaTimber", "ByyskHides"], ["MedicalSupplies", "ReactorCores"]),
    ("Tatooine", "Tatooine", ["Tatooine"], "tatooineorbit", "None", 4,
     ["JawaSalvage", "BanthaHides"], ["PurifiedWater", "Grain", "Seafood"]),
    ("SmugglersMoon", "Smuggler's Moon", ["SmugglersMoon", "SmugglersMoonStation"], "narshadorbit", "None", 4,
     ["Moonspice", "NeonSalt", "EncryptedDataChips"], ["SithRelics", "VelesHoney", "WeaponsCrates"]),
    ("Korriban", "Korriban", ["Korriban"], "space_korriban", "Sith", 6,
     ["SithRelics", "AbdamarylliaExtract"], ["PurifiedWater", "EncryptedDataChips"]),
    ("Dathomir", "Dathomir", ["Dathomir"], "space_dathomir", "None", 5,
     ["WitchHerbs", "BeastHides"], ["CzerkaMachineParts", "MedicalSupplies"]),
    ("Dantooine", "Dantooine", ["Dantooine"], "dantooineorbit", "Republic", 5,
     ["Grain", "BanthaMilk"], ["ByyskHides", "BanthaHides", "BeastHides", "AraciaTimber"]),
]
# Departures from the plan's dock table: Mon Cala also exports Medical Supplies (kolto) and CZ-220 Weapons Crates,
# because the table lists both as imports with no exporter. Smuggler's Moon Landing and Station share one exchange
# (they share an orbit). Smuggler's Moon has no NPC spawns yet; tier 4 is its lanes' tier (Hutlar 3, Tatooine 4).

# Starlane graph (Module/git, 2026-10-27): every lane has one back. The three W6c zones are dead ends with no exchange,
# so they change no route length.
LANES = {
    "viscaraorbit": ["moncalaorbit", "space_midrim1", "space_viscbelt"],
    "moncalaorbit": ["dantooineorbit", "hutlar_orbit", "viscaraorbit"],
    "dantooineorbit": ["moncalaorbit"],
    "hutlar_orbit": ["moncalaorbit", "narshadorbit", "tatooineorbit"],
    "narshadorbit": ["hutlar_orbit", "tatooineorbit"],
    "tatooineorbit": ["hutlar_orbit", "narshadorbit", "space_dathomir"],
    "space_dathomir": ["tatooineorbit"],
    "space_midrim1": ["space_midrim2", "viscaraorbit", "space_midrimneb"],
    "space_midrim2": ["space_korriban", "space_midrim1"],
    "space_korriban": ["space_midrim2", "space_korrdebris"],
    "space_viscbelt": ["viscaraorbit"],
    "space_midrimneb": ["space_midrim1"],
    "space_korrdebris": ["space_korriban"],
}

# ---------------------------------------------------------------------------------------------------------------
# Pricing tuning
# ---------------------------------------------------------------------------------------------------------------
PRODUCER_FACTOR = 0.85          # an exporter sells cheap
NEUTRAL_BASE = 0.90             # a dock that neither makes nor needs a good, in the producer's own orbit
NEUTRAL_PER_HOP = 0.04          # ... plus this per starlane hop from the nearest producer
NEUTRAL_MAX = 1.10
CONSUMER_BASE = 1.05            # an importer pays well
CONSUMER_PER_HOP = 0.04         # longer routes pay more (plan section 13)
CONSUMER_MAX = 1.40
RISK_PREMIUM = {"Legal": 1.00, "Restricted": 1.10, "Contraband": 1.20}  # on every non-producer price
SPREAD = 0.06                   # the exchange sells at mid + 6% and buys at mid - 6%
DEPTH_CREDITS = 250000          # market depth: one depth of pressure (units) is this many credits at base price
FLOOD_MIN, FLOOD_MAX = 0.35, 2.5  # the pressure multiplier stays inside these
RECOVERY_HALF_LIFE_HOURS = 1.0  # "both recover over a few hours": 94% back after 4 hours
SOCIAL_PER_POINT = 0.001        # SOC improves buy and sell prices slightly: 0.1% per point over 10
SOCIAL_BASE = 10
SOCIAL_MAX = 0.015              # 1.5% from SOC 25
XP_PER_PROFIT_CREDIT = 1.0      # Piloting XP for a profitable sale (plan section 4, XP rules)
XP_MAX_PER_SALE = 1200          # two same-level kills (Skill delta 0 = 600)
SPILL_SECONDS = 300             # cargo spill containers last 5 minutes (plan section 13)
SPILL_MAX_CONTAINERS = 6        # one container per commodity, the largest lots first; the rest merge into the last

# Route timing for the simulation (assumed; measure in a client): a starlane hop is launch-or-arrive, fly to the
# lane and jump; a stop is docking, walking to the Dock Master, trading and boarding.
HOP_MINUTES = 3.0
STOP_MINUTES = 2.0
SAME_ORBIT_MINUTES = 1.0


def legality(banned):
    if not banned:
        return "Legal"
    return "Contraband" if len(banned) >= 3 else "Restricted"


def hops(a, b):
    if a == b:
        return 0
    seen, frontier, d = {a}, [a], 0
    while frontier:
        d += 1
        nxt = []
        for n in frontier:
            for x in LANES[n]:
                if x == b:
                    return d
                if x not in seen:
                    seen.add(x)
                    nxt.append(x)
        frontier = nxt
    raise ValueError(f"no route {a} -> {b}")


def commodity(name):
    return next(c for c in COMMODITIES if c[1] == name)


def exchange(name):
    return next(e for e in EXCHANGES if e[0] == name)


def trades(ex, com):
    """An exchange trades a good it makes or needs, and any other good its authority does not ban."""
    _, _, _, _, authority, _, produces, consumes = ex
    return com[1] in produces or com[1] in consumes or authority not in com[5]


def markets():
    """Every (exchange, commodity) market: role, price factor and depth in units."""
    out = []
    for ex in EXCHANGES:
        for com in COMMODITIES:
            if not trades(ex, com):
                continue
            producers = [e for e in EXCHANGES if com[1] in e[6]]
            nearest = min(hops(ex[3], p[3]) for p in producers)
            risk = RISK_PREMIUM[legality(com[5])]
            if com[1] in ex[6]:
                role, factor = "Producer", PRODUCER_FACTOR
            elif com[1] in ex[7]:
                role, factor = "Consumer", min(CONSUMER_MAX, CONSUMER_BASE + CONSUMER_PER_HOP * nearest) * risk
            else:
                role, factor = "Neutral", min(NEUTRAL_MAX, NEUTRAL_BASE + NEUTRAL_PER_HOP * nearest) * risk
            depth = max(20, round(DEPTH_CREDITS / com[3]))
            out.append(dict(exchange=ex[0], commodity=com[1], role=role, factor=round(factor, 3), depth=depth,
                            base=com[3], volume=com[4]))
    return out


def check_orbit_rule():
    """No dock exports a good that a dock in the same orbit imports (plan section 13)."""
    bad = []
    for a in EXCHANGES:
        for b in EXCHANGES:
            if a is not b and a[3] == b[3]:
                bad += [(a[0], b[0], c) for c in a[6] if c in b[7]]
    return bad


# ---------------------------------------------------------------------------------------------------------------
# Price math (mirrors Service/TradeService/TradePricing.cs)
# ---------------------------------------------------------------------------------------------------------------
def pressure_multiplier(pressure, depth):
    return min(FLOOD_MAX, max(FLOOD_MIN, math.exp(-pressure / depth)))


def unit_mid(base, factor, pressure, depth):
    return base * factor * pressure_multiplier(pressure, depth)


def social_bonus(social):
    return min(SOCIAL_MAX, max(0, social - SOCIAL_BASE) * SOCIAL_PER_POINT)


def unit_buy(base, factor, pressure, depth, social):
    return unit_mid(base, factor, pressure, depth) * (1 + SPREAD) * (1 - social_bonus(social))


def unit_sell(base, factor, pressure, depth, social):
    return unit_mid(base, factor, pressure, depth) * (1 - SPREAD) * (1 + social_bonus(social))


def buy_cost(base, factor, pressure, depth, units, social=10):
    """Credits to buy units. Each unit is priced at the pressure halfway through it; buying lowers pressure."""
    return math.ceil(sum(unit_buy(base, factor, pressure - i - 0.5, depth, social) for i in range(units)))


def sell_revenue(base, factor, pressure, depth, units, social=10):
    return math.floor(sum(unit_sell(base, factor, pressure + i + 0.5, depth, social) for i in range(units)))


def recover(pressure, hours):
    return pressure * 0.5 ** (hours / RECOVERY_HALF_LIFE_HOURS)


def trade_xp(profit):
    return 0 if profit <= 0 else min(XP_MAX_PER_SALE, int(profit * XP_PER_PROFIT_CREDIT))


# ---------------------------------------------------------------------------------------------------------------
# Space combat credits per hour, from today's loot tables
# ---------------------------------------------------------------------------------------------------------------
def item_cost(resref):
    path = REPO / "Module" / "uti" / f"{resref}.uti.json"
    if not path.exists():
        return 0
    return json.loads(path.read_text(encoding="latin-1")).get("Cost", {}).get("value", 0)


def loot_tables():
    text = (REPO / "SWLOR.Game.Server/Feature/LootTableDefinition/SpaceLootTableDefinition.cs").read_text(encoding="utf-8")
    tables = {}
    for block in re.split(r'_builder\.Create\(', text)[1:]:
        name = re.match(r'"([^"]+)"', block)[1]
        rows = []
        for m in re.finditer(r'\.Add(Item|Gold)\(([^)]*)\)', block.split(";")[0]):
            args = [a.strip().strip('"') for a in m[2].split(",")]
            if m[1] == "Gold":
                rows.append(("gold", int(args[1]), int(args[0]), False))
            else:
                rows.append((args[0], int(args[1]), int(args[2]) if len(args) > 2 else 1, len(args) > 3 and args[3] == "true"))
        tables[name] = rows
    return tables


def expected_value(rows):
    """One pull: weighted item, quantity 1..max; gold at face value, items at the 40% store markdown."""
    weight = sum(r[1] for r in rows if not r[3])
    value = 0.0
    for resref, w, qty, rare in rows:
        if rare:
            continue
        unit = 1 if resref == "gold" else item_cost(resref) * 0.40
        value += w / weight * (qty + 1) / 2 * unit
    return value


KILLS_PER_HOUR = 60  # a normal fight (plan section 19 targets, 20-50 s) plus finding the next one; assumed


def combat_credits_per_hour(tier):
    basic = loot_tables()[f"SPACE_BASIC_T{tier}"]
    per_kill = 3 * 0.60 * expected_value(basic)
    return per_kill, per_kill * KILLS_PER_HOUR


# ---------------------------------------------------------------------------------------------------------------
# Simulation: one greedy trader on fresh markets
# ---------------------------------------------------------------------------------------------------------------
def leg_minutes(a, b):
    h = hops(exchange(a)[3], exchange(b)[3])
    return STOP_MINUTES + (SAME_ORBIT_MINUTES if h == 0 else h * HOP_MINUTES)


def market(ex, com):
    return MARKET_INDEX.get((ex, com))


def plan_leg(state, here, there, hold, social, goods):
    """The basket that earns the most from here to there: add the best unit per volume until nothing pays."""
    basket, used, profit = {}, 0, 0.0
    while True:
        best, best_gain = None, 0.0
        for c in goods:
            hm, tm = market(here, c), market(there, c)
            if not hm or not tm or used + hm["volume"] > hold:
                continue
            n = basket.get(c, 0)
            buy = unit_buy(hm["base"], hm["factor"], state[(here, c)] - n - 0.5, hm["depth"], social)
            sell = unit_sell(tm["base"], tm["factor"], state[(there, c)] + n + 0.5, tm["depth"], social)
            gain = (sell - buy) / hm["volume"]
            if gain > best_gain:
                best, best_gain = c, gain
        if best is None:
            return basket, profit
        basket[best] = basket.get(best, 0) + 1
        used += market(here, best)["volume"]
        profit += best_gain * market(here, best)["volume"]


def simulate(hold, start="Viscara", hours=4, social=10, log=None, lawful=False):
    """Credits earned in each hour by a trader who always flies the leg that pays most per minute. A lawful trader
    carries only legal goods."""
    goods = [c[1] for c in COMMODITIES if not lawful or not c[5]]
    state = {(m["exchange"], m["commodity"]): 0.0 for m in MARKETS}
    here, clock = start, 0.0
    per_hour = [0.0] * hours
    while clock < hours * 60:
        options = []
        for ex in EXCHANGES:
            if ex[0] != here:
                basket, profit = plan_leg(state, here, ex[0], hold, social, goods)
                options.append((profit / leg_minutes(here, ex[0]), ex[0], basket))
        _, there, basket = max(options)
        realized = 0
        for c, n in basket.items():
            hm = market(here, c)
            realized -= buy_cost(hm["base"], hm["factor"], state[(here, c)], hm["depth"], n, social)
            state[(here, c)] -= n
        minutes = leg_minutes(here, there)
        for k in state:
            state[k] = recover(state[k], minutes / 60)
        clock += minutes
        for c, n in basket.items():
            tm = market(there, c)
            realized += sell_revenue(tm["base"], tm["factor"], state[(there, c)], tm["depth"], n, social)
            state[(there, c)] += n
        per_hour[min(hours - 1, int((clock - 1e-9) // 60))] += realized
        if log is not None:
            log.append((round(clock), here, there, realized, dict(basket)))
        here = there
    return per_hour


def same_exchange_round_trip(m, units, social=26):
    """Buying and selling straight back at one exchange always loses (the arbitrage bound)."""
    cost = buy_cost(m["base"], m["factor"], 0.0, m["depth"], units, social)
    back = sell_revenue(m["base"], m["factor"], -units, m["depth"], units, social)
    return back - cost


MARKETS = markets()
MARKET_INDEX = {(m["exchange"], m["commodity"]): m for m in MARKETS}
COMMODITY_NAMES = [c[1] for c in COMMODITIES]
HULLS = [("Fighter", 10), ("Y8 Miner", 188), ("Barracuda", 200), ("XS Freighter", 480), ("Heavy Lifter", 750),
         ("Republic Quartermaster", 1200), ("Hutt Grand Barge", 1980)]


def report():
    print("Orbit rule violations:", check_orbit_rule() or "none")
    print(f"{len(COMMODITIES)} commodities, {len(EXCHANGES)} exchanges, {len(MARKETS)} markets")
    print("\nSpace combat today (normal kills, 60 per hour):")
    for t in range(1, 7):
        per_kill, per_hour = combat_credits_per_hour(t)
        print(f"  T{t}: {per_kill:7.0f} per kill, {per_hour:8.0f} per hour")
    for lawful in (True, False):
        print(f"\nBest greedy loop from Viscara on fresh markets, {'legal goods only' if lawful else 'every good'} "
              "(SOC 10): credits in hours 1 / 2 / 3 / 4, and the 4-hour average")
        for name, hold in HULLS:
            hours = simulate(hold, lawful=lawful)
            print(f"  {name:24s} hold {hold:5d}: " + " / ".join(f"{h:6.0f}" for h in hours)
                  + f"   avg {sum(hours) / len(hours):6.0f}")
    worst = max(same_exchange_round_trip(m, 50) for m in MARKETS)
    print(f"\nBest same-exchange buy-then-sell of 50 units at SOC 26: {worst} credits (must be < 0)")
    log = []
    simulate(480, hours=1, log=log, lawful=True)
    print("\nXS Freighter, legal goods, hour 1 legs (minute, from, to, profit, basket):")
    for row in log:
        print("   ", row)


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/trade.py. Do not edit by hand: change trade.py and regenerate.
// </auto-generated>
"""


def cs_string(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def cs_array(type_name, items):
    return f"new {type_name}[] {{ {items} }}" if items else f"System.Array.Empty<{type_name}>()"


def cs_double(x):
    text = repr(float(x))
    return text


def catalog_cs():
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "using SWLOR.Game.Server.Enumeration;",
             "using SWLOR.Game.Server.Service.TradeService;", "",
             "namespace SWLOR.Game.Server.Feature.TradeDefinition", "{",
             "    /// <summary>", "    /// The commodities and dock exchanges of the trade core (plan section 13).", "    /// </summary>",
             "    public static class TradeCatalog", "    {",
             "        public static readonly IReadOnlyList<CommodityDetail> Commodities = new List<CommodityDetail>", "        {"]
    for _, enum, name, base, vol, banned, desc in COMMODITIES:
        producers = ", ".join(f"TradeExchangeType.{e[0]}" for e in EXCHANGES if enum in e[6])
        consumers = ", ".join(f"TradeExchangeType.{e[0]}" for e in EXCHANGES if enum in e[7])
        bans = ", ".join(f"TradeAuthority.{a}" for a in banned)
        lines.append(f"            new CommodityDetail {{ Type = CommodityType.{enum}, Name = {cs_string(name)}, BasePrice = {base}, "
                     f"Volume = {vol}, BannedBy = {cs_array('TradeAuthority', bans)}, "
                     f"Producers = {cs_array('TradeExchangeType', producers)}, Consumers = {cs_array('TradeExchangeType', consumers)}, "
                     f"Description = {cs_string(desc)} }},")
    lines += ["        };", "",
              "        public static readonly IReadOnlyList<TradeExchangeDetail> Exchanges = new List<TradeExchangeDetail>", "        {"]
    for enum, name, planets, orbit, authority, tier, produces, consumes in EXCHANGES:
        planet_list = ", ".join(f"PlanetType.{p}" for p in planets)
        lines.append(f"            new TradeExchangeDetail {{ Type = TradeExchangeType.{enum}, Name = {cs_string(name)}, "
                     f"Authority = TradeAuthority.{authority}, OrbitTier = {tier}, PlanetTypes = new[] {{ {planet_list} }}, "
                     "Markets = new List<TradeMarketDetail>")
        lines.append("            {")
        for m in MARKETS:
            if m["exchange"] == enum:
                lines.append(f"                new TradeMarketDetail {{ Commodity = CommodityType.{m['commodity']}, "
                             f"Role = CommodityRole.{m['role']}, PriceFactor = {cs_double(m['factor'])}, Depth = {m['depth']} }},")
        lines.append("            } },")
    lines += ["        };", "    }", "}", ""]
    return "\n".join(lines)


def tuning_cs():
    return "\n".join([
        HEADER.rstrip("\n"), "namespace SWLOR.Game.Server.Service.TradeService", "{",
        "    /// <summary>",
        "    /// The trade core's spread, flooding, recovery, Social, XP and cargo spill numbers.",
        "    /// </summary>",
        "    public static class TradeTuning", "    {",
        f"        public const double Spread = {cs_double(SPREAD)};",
        f"        public const double FloodMinimum = {cs_double(FLOOD_MIN)};",
        f"        public const double FloodMaximum = {cs_double(FLOOD_MAX)};",
        f"        public const double RecoveryHalfLifeHours = {cs_double(RECOVERY_HALF_LIFE_HOURS)};",
        f"        public const double SocialPerPoint = {cs_double(SOCIAL_PER_POINT)};",
        f"        public const int SocialBase = {SOCIAL_BASE};",
        f"        public const double SocialMaximum = {cs_double(SOCIAL_MAX)};",
        f"        public const double XpPerProfitCredit = {cs_double(XP_PER_PROFIT_CREDIT)};",
        f"        public const int XpMaximumPerSale = {XP_MAX_PER_SALE};",
        f"        public const int SpillSeconds = {SPILL_SECONDS};",
        f"        public const int SpillMaximumContainers = {SPILL_MAX_CONTAINERS};",
        "    }", "}", ""])


WRITES = {
    "SWLOR.Game.Server/Feature/TradeDefinition/TradeCatalog.cs": catalog_cs,
    "SWLOR.Game.Server/Service/TradeService/TradeTuning.cs": tuning_cs,
}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab: bible_tabs.py calls trade_tab(Sheet) to write the Trade (Revamp) tab.
# ---------------------------------------------------------------------------------------------------------------
def trade_tab(Sheet):
    s = Sheet("Trade (Revamp)", [24, 12, 9, 9, 14, 24, 24, 60])
    s.title("Trade (Revamp): commodities, dock exchanges, pricing, flooding and recovery")
    s.note("Dev Status: Proposed (space revamp W7a draft). Generated by tools/SpaceRevampBalance/trade.py.")
    s.blank()
    s.title("Commodities")
    s.table(["Commodity", "Legality", "Base", "Volume", "Banned by", "Produced at", "Consumed at", "Base price source"],
            [[name, legality(banned), base, vol, ", ".join(banned) or "-",
              ", ".join(e[1] for e in EXCHANGES if enum in e[6]), ", ".join(e[1] for e in EXCHANGES if enum in e[7]),
              SOURCE[enum]] for _, enum, name, base, vol, banned, _ in COMMODITIES])
    s.title("Exchanges")
    s.table(["Exchange", "Authority", "Orbit tier", "", "Orbit", "Exports", "Imports", ""],
            [[e[1], e[4], e[5], "", e[3], ", ".join(commodity(c)[2] for c in e[6]),
              ", ".join(commodity(c)[2] for c in e[7]), ""] for e in EXCHANGES])
    s.title("Pricing rules")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Producer price", PRODUCER_FACTOR, "", "", "", "", "", "x base"],
        ["Neutral price", f"{NEUTRAL_BASE} + {NEUTRAL_PER_HOP} per hop", "", "", "", "", "",
         f"hops from the nearest producer, at most {NEUTRAL_MAX}"],
        ["Consumer price", f"{CONSUMER_BASE} + {CONSUMER_PER_HOP} per hop", "", "", "", "", "",
         f"at most {CONSUMER_MAX}; longer routes pay more"],
        ["Risk premium", ", ".join(f"{k} {v}" for k, v in RISK_PREMIUM.items()), "", "", "", "", "",
         "on neutral and consumer prices"],
        ["Spread", SPREAD, "", "", "", "", "", "the exchange sells at mid + spread and buys at mid - spread"],
        ["Depth", DEPTH_CREDITS, "", "", "", "", "", "credits at base price; depth in units = this / base"],
        ["Flooding", "exp(-pressure / depth)", "", "", "", "", "",
         f"clamped to {FLOOD_MIN}-{FLOOD_MAX}; selling adds pressure, buying removes it"],
        ["Recovery half-life (h)", RECOVERY_HALF_LIFE_HOURS, "", "", "", "", "", "pressure decays toward 0"],
        ["Social", f"{SOCIAL_PER_POINT * 100:g}% per point over {SOCIAL_BASE}", "", "", "", "", "", f"at most {SOCIAL_MAX * 100:g}%"],
        ["Trade XP", f"{XP_PER_PROFIT_CREDIT} Piloting XP per profit credit", "", "", "", "", "", f"at most {XP_MAX_PER_SALE} per sale"],
        ["Cargo spill", f"{SPILL_SECONDS} s", "", "", "", "", "", f"up to {SPILL_MAX_CONTAINERS} containers; anyone can scoop"],
    ])
    s.title("Markets")
    s.table(["Exchange", "Commodity", "Role", "Factor", "Depth", "", "", ""],
            [[exchange(m["exchange"])[1], commodity(m["commodity"])[2], m["role"], m["factor"], m["depth"], "", "", ""]
             for m in MARKETS])
    return s


def main():
    if "--sim" in sys.argv:
        report()
        return
    if check_orbit_rule():
        sys.exit(f"orbit rule broken: {check_orbit_rule()}")
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
    print(f"{len(COMMODITIES)} commodities, {len(MARKETS)} markets, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
