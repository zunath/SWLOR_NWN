"""Space revamp balance model (plan W0): every number the Bible draft lists comes from here, and the checks run it through
SWLOR.CombatSim's space mode.

Rules (plan sections 5, 7, 8, 9, 11):
- Piloting rank adds nothing. The ground 8 + 2*rank terms live on gear: the hull carries the flat 8, each weapon
  step carries Attack 2r and Accuracy 2r, Thrusters carry Evasion 2r, Hull Plating carries Defense 1.8r
  (ground armor 1.2r plus armor gear).
- Accuracy and Evasion add AGI x 3 (launchers: PER x 3 for accuracy). Attack adds the damage attribute
  (PER energy, MGT launcher). Defense adds VIT.
- NPC ships are the ground Enemy Stat Presets at the same level and difficulty, converted by role.
"""
import io
import json
import os
import re
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
# Scenario and result files for the simulator runs; override with SPACE_BALANCE_WORK.
HERE = Path(os.environ.get("SPACE_BALANCE_WORK", tempfile.gettempdir())) / "space-revamp-balance"
HERE.mkdir(parents=True, exist_ok=True)
# Build first: dotnet build SWLOR.CombatSim -c Release. Override with SWLOR_COMBATSIM.
SIM = Path(os.environ.get("SWLOR_COMBATSIM", REPO / "SWLOR.CombatSim" / "bin" / "Release" / "net10.0" / "SWLOR.CombatSim.exe"))
STEPS = list(range(0, 50, 5))
SIZES = ["Small", "Medium", "Large", "Capital"]
SIZE_INDEX = {s: i for i, s in enumerate(SIZES)}

# ---------------------------------------------------------------------------------------------------------------
# Ground anchors
# ---------------------------------------------------------------------------------------------------------------
# Ground longsword DMG by required rank (Bible, Equipment - Weapons).
GROUND_DMG = {0: 5, 5: 7, 10: 10, 15: 11, 20: 14, 25: 15, 30: 18, 35: 19, 40: 22, 45: 23, 50: 24}
# Ground fight length, a longsword autoattacking a same-level Normal Melee enemy (plan section 19).
GROUND_TTK = {5: 18, 15: 34, 25: 47, 35: 47, 45: 51}


def load_presets():
    raw = subprocess.run(["git", "-C", str(REPO), "show", "HEAD:design/bible/SWLOR Design Bible.xlsx"],
                         capture_output=True).stdout
    z = zipfile.ZipFile(io.BytesIO(raw))
    wb = z.read("xl/workbook.xml").decode()
    rels = z.read("xl/_rels/workbook.xml.rels").decode()
    ss = [re.sub(r"<[^>]+>", "", s) for s in re.findall(r"<si>(.*?)</si>", z.read("xl/sharedStrings.xml").decode(), re.S)]
    names = {re.search(r'name="([^"]+)"', t)[1]: re.search(r'r:id="([^"]+)"', t)[1] for t in re.findall(r"<sheet\b[^>]*>", wb)}
    target = dict(re.findall(r'Id="(rId\d+)"[^>]*Target="([^"]+)"', rels))
    x = z.read("xl/" + target[names["Enemy Stat Presets"]].replace("/xl/", "").lstrip("/")).decode()
    rows = []
    for r in re.findall(r"<row[^>]*>(.*?)</row>", x, re.S):
        cells = {}
        for c in re.finditer(r'<c r="([A-Z]+)\d+"([^>]*?)(?:/>|>(.*?)</c>)', r, re.S):
            v = re.search(r"<v>(.*?)</v>", c[3] or "")
            cells[c[1]] = ss[int(v[1])] if (v and 't="s"' in c[2]) else (v[1] if v else "")
        rows.append(cells)
    head = rows[0]
    cols = {v: k for k, v in head.items()}
    out = {}
    for r in rows[1:]:
        if not r.get("A") or "|" not in r["A"]:
            continue
        g = lambda n: float(r[cols[n]])
        out[(int(g("Level")), r[cols["Difficulty"]], r[cols["Role"]])] = dict(
            mgt=g("MGT"), per=g("PER"), wil=g("WIL"), vit=g("VIT"), agi=g("AGI"), hp=g("HP"), stm=g("STM"),
            dmg=g("DMG"), atk=g("Attack"), eva=g("Evasion"), pdef=g("Physical Defense"), delay=g("Delay"))
    return out


PRESETS = load_presets()

# ---------------------------------------------------------------------------------------------------------------
# Tunables (the values the Bible draft publishes)
# ---------------------------------------------------------------------------------------------------------------
T = dict(
    # Size rules
    size_hit_penalty=10, size_damage_penalty=0.12,
    # Weapon output by the hull it is fitted to (Light, Medium, Heavy) and capital weapons.
    weapon_size_mult={"Small": 1.0, "Medium": 1.1, "Large": 1.35, "Capital": 1.35},
    capital_weapon_mult=2.0,
    # HP, repair and capacitor module output by hull size.
    module_size_mult={"Small": 1.0, "Medium": 1.1, "Large": 1.1, "Capital": 1.1},
    # Pilot pools
    cap_per_wil=2, cap_regen_per_wil=0.05,
    shield_regen_pct={"Small": 0.005, "Medium": 0.0055, "Large": 0.006, "Capital": 0.0065},
    shield_regen_wil=0.02,  # +2% of the base rate per WIL above 10
    # NPC conversion
    npc_hp_mult={5: 1.55, 15: 1.4},
    npc_dmg_mult={5: 1.15, 15: 0.58, 25: 0.45},
    # Elites and bosses: extra HP and damage factors on top of the Normal conversion, by level.
    elite_hp={5: 0.65, 15: 0.74, 25: 0.65, 35: 0.55, 45: 0.38}, elite_dmg={5: 0.8, 15: 0.95, 25: 0.87, 35: 0.64, 45: 0.6},
    boss_hp={5: 0.54, 15: 0.55, 25: 0.48, 35: 0.4, 45: 0.28}, boss_dmg={5: 1.7, 15: 2.2, 25: 2.07, 35: 1.7, 45: 1.5}, capital_hp=1.5, npc_shield_share=0.5, npc_shield_regen_pct=0.005,
)


def curve(name, level):
    c = {int(k): v for k, v in T[name].items()}
    keys = sorted(c)
    if level <= keys[0]:
        return c[keys[0]]
    if level >= keys[-1]:
        return c[keys[-1]]
    for a, b in zip(keys, keys[1:]):
        if a <= level <= b:
            f = (level - a) / (b - a)
            return c[a] * (1 - f) + c[b] * f


def npc_hp_mult(level):
    return curve("npc_hp_mult", level)


# ---------------------------------------------------------------------------------------------------------------
# Modules (Small-hull values; size multipliers apply on bigger hulls)
# ---------------------------------------------------------------------------------------------------------------
# HP per step for Hull Plating (hull) and Shield Booster (shield); the shape follows ground Normal HP by level.
HP_STEP = {0: 10, 5: 40, 10: 95, 15: 150, 20: 200, 25: 250, 30: 290, 35: 325, 40: 360, 45: 395}
REPAIR_STEP = {r: max(2, round(0.03 * v)) for r, v in HP_STEP.items()}  # per 4 s cycle
CAP_STEP = {0: 10, 5: 15, 10: 20, 15: 25, 20: 30, 25: 35, 30: 40, 35: 45, 40: 50, 45: 55}
CAP_REGEN_STEP = {r: round(0.3 + 0.03 * r, 2) for r in STEPS}

WEAPONS = {
    # family: (attack family, damage type, DMG mult, cycle s, capacitor per cycle, shield mult, flags)
    "Laser Cannon": dict(family="Energy", type="Thermal", dmg=1.0, cycle=3.0, cap=3, shield=1.0),
    "Ion Cannon": dict(family="Energy", type="EM", dmg=0.85, cycle=3.0, cap=3, shield=1.5),
    "Beam Cannon": dict(family="Energy", type="Thermal", dmg=1.1, cycle=3.0, cap=5, shield=1.0),
    "Missile Launcher": dict(family="Launcher", type="Explosive", dmg=1.0, cycle=3.0, cap=2, shield=1.0),
    "Torpedo Launcher": dict(family="Launcher", type="Explosive", dmg=5.0, cycle=18.0, cap=10, shield=1.0,
                             bypass=True, ignore_size=True),
    "Point-Defense Turret": dict(family="Energy", type="Thermal", dmg=0.85, cycle=2.5, cap=2, shield=1.0,
                                 fixed_size="Small"),
    "Turbolaser": dict(family="Energy", type="Thermal", dmg=1.0, cycle=4.0, cap=6, shield=1.0, capital=True),
    "Quad Laser": dict(family="Energy", type="Thermal", dmg=0.55, cycle=2.0, cap=3, shield=1.0, capital=True),
    "Storm Cannon": dict(family="Energy", type="EM", dmg=0.8, cycle=4.0, cap=6, shield=1.5, capital=True),
}


def weapon(name, rank, hull_size, pilot, label=None):
    w = WEAPONS[name]
    if w.get("capital"):
        mult, size = T["capital_weapon_mult"], "Capital"
    elif w.get("fixed_size"):
        mult, size = 1.0, w["fixed_size"]
    else:
        mult, size = T["weapon_size_mult"][hull_size], hull_size
    attr = pilot["per"] if w["family"] == "Energy" else pilot["mgt"]
    return {"name": label or f"{name} r{rank}", "family": w["family"], "damageType": w["type"],
            "dmg": max(1, round(GROUND_DMG[rank] * w["dmg"] * mult)), "rank": rank, "cycleSeconds": w["cycle"],
            "capacitorPerCycle": w["cap"], "attack": 2 * rank + attr, "accuracy": 2 * rank, "size": size,
            "shieldBypass": w.get("bypass", False), "ignoreSizePenalty": w.get("ignore_size", False),
            "shieldDamageMultiplier": w["shield"]}


# ---------------------------------------------------------------------------------------------------------------
# Hulls: role baselines (Standard, before manufacturer)
# ---------------------------------------------------------------------------------------------------------------
# slots H/M/L, hull, shield, capacitor, cap regen, speed, Attack/Accuracy/Defense/Evasion base, cargo, structures,
# repair multiplier, crew stations, built-in bonus
ROLES = {
    "Fighter": dict(size="Small", H=3, M=2, L=2, hull=30, shield=30, cap=60, capr=2.5, speed=1.00, atk=8, acc=8, df=8, ev=12,
                    cargo=10, interior=10, repair=1.0, crew="Pilot", bonus="+15% weapon damage", dmg=15),
    "Interceptor": dict(size="Small", H=3, M=2, L=2, hull=25, shield=25, cap=60, capr=2.5, speed=1.25, atk=8, acc=8, df=8, ev=18,
                        cargo=10, interior=10, repair=1.0, crew="Pilot", bonus="Ion Web and Interdictor strength +25% and range +50%"),
    "Bomber": dict(size="Small", H=3, M=2, L=2, hull=35, shield=30, cap=70, capr=2.5, speed=0.95, atk=8, acc=8, df=9, ev=10,
                   cargo=10, interior=10, repair=1.0, crew="Pilot", bonus="+20% torpedo damage"),
    "Explorer": dict(size="Small", H=1, M=3, L=2, hull=30, shield=30, cap=80, capr=3.0, speed=1.15, atk=8, acc=8, df=8, ev=14,
                     cargo=20, interior=15, repair=1.0, crew="Pilot", bonus="+25% scan strength; can fit a Cloaking Device"),
    "Gunship": dict(size="Medium", H=3, M=3, L=3, hull=60, shield=60, cap=90, capr=3.5, speed=0.90, atk=8, acc=8, df=12, ev=6,
                    cargo=40, interior=25, repair=1.5, crew="Pilot", bonus="+10 to all resistances; +25% threat", res=10),
    "Logistics": dict(size="Medium", H=2, M=4, L=3, hull=50, shield=60, cap=110, capr=4.5, speed=0.95, atk=8, acc=8, df=10, ev=8,
                      cargo=40, interior=25, repair=1.5, crew="Pilot", bonus="Remote repair +25% amount and +50% range"),
    "Mining Barge": dict(size="Medium", H=3, M=2, L=3, hull=55, shield=50, cap=90, capr=3.5, speed=0.85, atk=8, acc=8, df=10, ev=6,
                         cargo=150, interior=25, repair=1.5, crew="Pilot", bonus="+20% mining and gas yield"),
    "Light Freighter": dict(size="Medium", H=2, M=2, L=3, hull=60, shield=50, cap=80, capr=3.0, speed=0.90, atk=8, acc=8, df=10, ev=6,
                            cargo=400, interior=60, repair=1.5, crew="Pilot", bonus="Large interior and hold; carries passengers"),
    "Blockade Runner": dict(size="Medium", H=2, M=3, L=2, hull=45, shield=45, cap=80, capr=3.0, speed=1.15, atk=8, acc=8, df=9, ev=10,
                            cargo=200, interior=30, repair=1.5, crew="Pilot", bonus="Cloaks with cargo aboard; customs scan chance -25%"),
    "Frigate": dict(size="Large", H=4, M=4, L=4, stH=2, stM=2, stL=2, hull=120, shield=120, cap=140, capr=5.0, speed=0.80, atk=8, acc=8, df=16, ev=-6,
                    cargo=100, interior=60, repair=2.5, crew="Pilot, Gunner, Engineer", bonus="+10% weapon damage and +10% max hull", dmg=10, hullpct=10),
    "Industrial": dict(size="Large", H=3, M=3, L=4, stH=1, stM=1, stL=2, hull=130, shield=110, cap=130, capr=4.5, speed=0.70, atk=8, acc=8, df=14, ev=-6,
                       cargo=600, interior=60, repair=2.5, crew="Pilot, Gunner, Engineer", bonus="Can fit Strip Miners and Salvagers; +20% salvage yield"),
    "Heavy Freighter": dict(size="Large", H=2, M=3, L=4, stH=1, stM=1, stL=2, hull=140, shield=110, cap=120, capr=4.0, speed=0.70, atk=8, acc=8, df=14, ev=-6,
                            cargo=1200, interior=120, repair=2.5, crew="Pilot, Gunner, Engineer", bonus="Largest non-capital interior and hold"),
    "Cruiser": dict(size="Capital", H=6, M=5, L=5, stH=4, stM=3, stL=3, hull=340, shield=340, cap=250, capr=8.0, speed=0.60, atk=8, acc=8, df=20, ev=-12,
                    cargo=600, interior=150, repair=4.0, crew="Pilot, 2 Gunners, Engineer, Sensors", bonus="Can fit capital weapons; +10% weapon damage", dmg=10),
    "Command Ship": dict(size="Capital", H=5, M=6, L=5, stH=3, stM=4, stL=3, hull=320, shield=380, cap=280, capr=9.0, speed=0.60, atk=8, acc=8, df=18, ev=-12,
                         cargo=600, interior=150, repair=4.0, crew="Pilot, 2 Gunners, Engineer, Sensors", bonus="Command aura range +50% and strength +20%"),
}

GATE = {"Small": 0, "Medium": 10, "Large": 25, "Capital": 40}

# Manufacturer themes: percent changes to hull/shield/cap/speed/cargo, flat Accuracy/Evasion, damage %, shield regen %.
MAKERS = {
    "Republic": dict(theme="Shields, repairs, support", shield=10, shieldregen=10, repair=10, hull=-10),
    "Corellian": dict(theme="Speed, evasion, flexible fittings", speed=10, ev=4, hull=-5, shield=-5),
    "Sith": dict(theme="Damage and critical hits", dmg=5, crit=3, shield=-10),
    "Czerka": dict(theme="Industry: mining, salvage, cargo", cargo=25, yieldpct=10, acc=-2, ev=-2),
    "Mandalorian": dict(theme="Hull toughness, explosive weapons", hull=10, explosive=5, shield=-10, speed=-5),
    "Hutt": dict(theme="Heavy hulls, big holds, contraband", hull=15, cargo=25, speed=-10, ev=-4),
    "Chiss": dict(theme="Sensors, accuracy, range, scanning", acc=4, range=10, scan=15, hull=-10),
    "Rift Raider": dict(theme="Pirate refits (Advanced only, Wild Space)", dmg=5, speed=5, shield=-10),
}
# Advanced hull: +10% hull, shield and capacitor and +2 Attack/Accuracy/Defense/Evasion, plus the Appendix C extra.
ADVANCED = dict(pool=10, flat=2)


def attr(rank):
    return round(16 + 0.2 * rank)


def pilot_for(rank, build="ranged"):
    a = attr(rank)
    return {"agi": a, "per": a, "vit": a, "wil": a, "mgt": a}


def hull_stats(role, maker=None, advanced=False, extra=None):
    """Base hull numbers plus the percent changes (manufacturer, Advanced, signature) that apply to the ship's totals."""
    b = dict(ROLES[role])
    m = MAKERS.get(maker, {}) if maker else {}
    e = extra or {}
    pct = {k: m.get(k, 0) + e.get(k, 0) + (ADVANCED["pool"] if advanced and k in ("hull", "shield", "cap") else 0)
           for k in ("hull", "shield", "cap", "speed", "cargo", "shieldregen")}
    flat = ADVANCED["flat"] if advanced else 0
    return dict(size=b["size"], H=b["H"] + e.get("H", 0), M=b["M"] + e.get("M", 0), L=b["L"] + e.get("L", 0),
                hull=b["hull"], shield=b["shield"], cap=b["cap"], capr=b["capr"],
                speed=round(b["speed"] * (1 + pct["speed"] / 100), 2), cargo=round(b["cargo"] * (1 + pct["cargo"] / 100)),
                atk=b["atk"] + flat, acc=b["acc"] + flat + m.get("acc", 0) + e.get("acc", 0),
                df=b["df"] + flat + e.get("df", 0), ev=b["ev"] + flat + m.get("ev", 0) + e.get("ev", 0),
                dmg=b.get("dmg", 0) + m.get("dmg", 0) + e.get("dmg", 0), explosive=m.get("explosive", 0) + e.get("explosive", 0),
                res=b.get("res", 0) + e.get("res", 0), crit=m.get("crit", 0) + e.get("crit", 0), pct=pct)


MID = {"Shield Booster", "Targeting Computer", "Shield Repairer", "Shield Hardener", "Capacitor Battery"}
LOW = {"Hull Plating", "Thrusters", "Hull Repairer", "Capacitor Regulator", "Resist Plating"}


def module_row(mod):
    return "H" if mod in WEAPONS else "M" if mod in MID else "L" if mod in LOW else None


# The most a crew seat works at after Crew Morale and Leadership (ShipCrewSeats.MaximumSeatEffectiveness).
CREW_CAP = 1.05


def player_ship(role, rank, fit, maker=None, advanced=False, extra=None, name=None, pilot=None, crew=1.0):
    """fit: dict of module family -> count. Weapons go in High, the rest as listed. Every module at `rank`.
    crew: effectiveness of the station seats (1 = players at matching rank, 0.75 = the best droid, 0 = solo pilot).
    Weapons fill Helm High slots first; the rest sit in Gunner slots."""
    h = hull_stats(role, maker, advanced, extra)
    p = pilot or pilot_for(rank)
    size = h["size"]
    ms = T["module_size_mult"][size]
    r = (rank // 5) * 5
    hull, shield, cap, capr, df, ev, acc = h["hull"], h["shield"], h["cap"] + T["cap_per_wil"] * p["wil"], \
        h["capr"] + T["cap_regen_per_wil"] * p["wil"], h["df"] + p["vit"], h["ev"], h["acc"]  # the simulator adds AGI x 3
    weapons, repairers, res = [], [], h["res"]
    # Defense (Hull Plating), Evasion (Thrusters) and Accuracy (Targeting Computer) don't stack: the best one counts.
    best = {"df": 0, "ev": 0, "acc": 0}
    used = {"H": 0, "M": 0, "L": 0}
    for key, count in fit.items():
        mod = key.split("#")[0]  # "Shield Booster#2" is a second group of the same module, later in the row
        for i in range(count):
            row = module_row(mod)
            used[row] += 1
            if used[row] > h[row]:
                raise ValueError(f"{role}: too many {row} modules")
            # Helm slots fill first; the rest of the row is crew-station slots (Gunner, Engineer, Sensors), which
            # work only while the seat is crewed, at the crew's effectiveness.
            e = crew if used[row] > h[row] - ROLES[role].get("st" + row, 0) else 1.0
            if e <= 0:
                continue
            if mod in WEAPONS:
                w = weapon(mod, r, size, p, f"{mod} {len(weapons) + 1}")
                w["dmg"] = max(1, round(w["dmg"] * e))
                weapons.append(w)
            elif mod == "Hull Plating":
                hull += round(HP_STEP[r] * ms * e)
                best["df"] = max(best["df"], round(1.8 * r))
            elif mod == "Shield Booster":
                shield += round(HP_STEP[r] * ms * e)
            elif mod == "Thrusters":
                best["ev"] = max(best["ev"], 2 * r)
            elif mod == "Targeting Computer":
                best["acc"] = max(best["acc"], 2 + round(0.4 * r))
            elif mod == "Capacitor Battery":
                cap += round(CAP_STEP[r] * ms * e)
            elif mod == "Capacitor Regulator":
                capr += CAP_REGEN_STEP[r] * ms * e
            elif mod in ("Shield Repairer", "Hull Repairer"):
                amount = REPAIR_STEP[r] * ms * e * (1.0 if mod == "Shield Repairer" else 0.8)
                repairers.append({"name": f"{mod} {len(repairers) + 1}", "target": "Shield" if mod == "Shield Repairer" else "Hull",
                                  "amountPerCycle": max(1, round(amount)), "cycleSeconds": 4, "capacitorPerCycle": 4})
            elif mod == "Shield Hardener":
                res += 10 + r // 5  # toggle: models as always on
            elif mod == "Resist Plating":
                res += 6 + r // 6
            else:
                raise ValueError(mod)
    df, ev, acc = df + best["df"], ev + best["ev"], acc + best["acc"]
    hull = round(hull * (1 + h["pct"]["hull"] / 100))
    shield = round(shield * (1 + h["pct"]["shield"] / 100))
    cap = round(cap * (1 + h["pct"]["cap"] / 100))
    regen_pct = T["shield_regen_pct"][size] * (1 + T["shield_regen_wil"] * (p["wil"] - 10)) * (1 + h["pct"]["shieldregen"] / 100)
    return {"name": name or f"{role} R{rank}", "size": size, "maxHull": hull, "maxShield": shield, "maxCapacitor": cap,
            "shieldRegenPerSecond": round(shield * regen_pct, 2), "capacitorRegenPerSecond": round(capr, 2),
            "attack": h["atk"], "accuracy": acc, "defense": df, "evasion": ev,
            "resistances": {"em": res, "thermal": res, "explosive": res},
            "pilot": p, "weapons": weapons, "repairers": repairers,
            # Outgoing damage bonuses go on the target as negative resistance (see apply_outgoing).
            "_dmg": h["dmg"] + h["crit"] * 0.25, "_explosive": h["explosive"]}


# ---------------------------------------------------------------------------------------------------------------
# NPC space roles
# ---------------------------------------------------------------------------------------------------------------
# ground role the stats come from, size per difficulty, HP x, Defense +, Evasion +, resist, weapon, damage x
NPC_ROLES = {
    "Fighter": dict(ground="Ranged", size="Small", hp=1.0, df=0, ev=0, res=0, weapon="Laser Cannon", dmg=1.0,
                    ai="Attacks its target"),
    "Interceptor": dict(ground="Ranged", size="Small", hp=0.9, df=0, ev=8, res=0, weapon="Laser Cannon", dmg=0.9,
                        ai="Webs and interdicts ships that flee"),
    "Bomber": dict(ground="Melee", size="Small", hp=1.05, df=1, ev=-2, res=0, weapon="Missile Launcher", dmg=1.0,
                   ai="Hunts the biggest hull; torpedoes Large and Capital targets"),
    "Sniper": dict(ground="Ranged", size="Small", hp=0.9, df=0, ev=0, res=0, weapon="Beam Cannon", dmg=1.1,
                   ai="Kites at maximum range"),
    "Electronic Warfare": dict(ground="Ranged", size="Small", hp=0.9, df=0, ev=2, res=0, weapon="Ion Cannon", dmg=0.85,
                               ai="Jams or paints the top-threat player"),
    "Gunship": dict(ground="Ranged", size="Medium", hp=0.8, df=3, ev=0, res=10, weapon="Laser Cannon", dmg=1.0,
                    ai="Holds threat and hardens"),
    "Logistics": dict(ground="Ranged", size="Medium", hp=0.85, df=0, ev=0, res=0, weapon="Ion Cannon", dmg=0.8,
                      ai="Repairs allies"),
}
DIFFICULTY_SIZE = {"Normal": None, "Tough": None, "Elite": "Medium", "Boss": "Large"}
# Space elites and bosses keep the ground HP but hit softer, so one pilot solos most elites (plan section 19).
DIFFICULTY_DMG = {"Normal": 1.0, "Tough": 0.9, "Elite": 0.8, "Boss": 0.8}


def npc_ship(role, level, difficulty="Normal", count=1, size=None, hp_scale=1.0, vs_size="Small"):
    nr = NPC_ROLES[role]
    p = PRESETS[(level, difficulty, nr["ground"])]
    sz = size or DIFFICULTY_SIZE[difficulty] or nr["size"]
    if SIZE_INDEX[sz] < SIZE_INDEX[nr["size"]]:
        sz = nr["size"]
    extra_hp = curve("elite_hp", level) if difficulty == "Elite" else curve("boss_hp", level) if difficulty == "Boss" else 1.0
    extra_dmg = curve("elite_dmg", level) if difficulty == "Elite" else curve("boss_dmg", level) if difficulty == "Boss" else 1.0
    hp = p["hp"] * npc_hp_mult(level) * nr["hp"] * hp_scale * extra_hp * (T["capital_hp"] if sz == "Capital" else 1.0)
    shield = round(hp * T["npc_shield_share"])
    w = WEAPONS[nr["weapon"]]
    attr_dmg = p["per"] if w["family"] == "Energy" else p["mgt"]
    cycle = round(p["delay"] / 110.0, 2)  # ground delay 230 = 2.08 s
    wspec = {"name": f"NPC {nr['weapon']}", "family": w["family"], "damageType": w["type"],
             "dmg": max(1, round(p["dmg"] * nr["dmg"] * DIFFICULTY_DMG[difficulty] * curve("npc_dmg_mult", level) * extra_dmg)), "rank": min(level, 50), "cycleSeconds": cycle,
             "capacitorPerCycle": 0, "attack": 2 * level + round(p["atk"]) + round(attr_dmg), "accuracy": 2 * level,
             "size": sz if not w.get("fixed_size") else "Small", "shieldDamageMultiplier": w["shield"]}
    return {"name": f"{difficulty} {role} L{level}", "count": count, "size": sz, "maxHull": round(hp) - shield,
            "maxShield": shield, "maxCapacitor": 100, "shieldRegenPerSecond": round(shield * T["npc_shield_regen_pct"], 2),
            "capacitorRegenPerSecond": 5, "attack": 8, "accuracy": 8,
            "defense": round(8 + 1.2 * level + p["vit"] + p["pdef"] + nr["df"]),
            "evasion": round(8 + 2 * level + p["eva"] + nr["ev"]),  # + AGI x 3 in the simulator
            "resistances": {"em": nr["res"], "thermal": nr["res"], "explosive": nr["res"]},
            "pilot": {"agi": round(p["agi"]), "per": round(p["per"]), "vit": round(p["vit"]), "wil": round(p["wil"]), "mgt": round(p["mgt"])},
            "weapons": ([dict(wspec, name="NPC Turbolaser 1", cycleSeconds=4.0), dict(wspec, name="NPC Turbolaser 2", cycleSeconds=4.0),
                         dict(wspec, name="NPC Point-Defense", dmg=max(1, round(wspec["dmg"] * 0.6)), size="Small")]
                        if sz == "Capital" else [wspec]) + ([dict(wspec, name="NPC Torpedo", dmg=wspec["dmg"] * 4, cycleSeconds=18.0, shieldBypass=True,
                                        ignoreSizePenalty=True, damageType="Explosive")]
                                  if role == "Bomber" and SIZE_INDEX[vs_size] >= 2 else []),
            "repairers": []}


def apply_outgoing(attacker, defender):
    """Fold the attacker's damage % bonuses into the defender's resistances (sim has no outgoing-damage stat)."""
    d = dict(defender)
    res = dict(d["resistances"])
    for k in res:
        res[k] = round(res[k] - attacker.get("_dmg", 0))
    res["explosive"] = round(res["explosive"] - attacker.get("_explosive", 0))
    d["resistances"] = res
    return d


def clean(ship):
    return {k: v for k, v in ship.items() if not k.startswith("_")}


def scenario(players, npcs, passive=False, duration=900):
    a0 = players[0]
    npcs = [apply_outgoing(a0, n) for n in npcs]
    return {"name": "W0", "description": "W0 draft", "durationSeconds": duration, "timeStepSeconds": 0.1,
            "sizeHitPenaltyPerStep": T["size_hit_penalty"], "sizeDamagePenaltyPerStep": T["size_damage_penalty"],
            "attackers": {"name": "Player", "ships": [clean(p) for p in players]},
            "defenders": {"name": "NPC", "passive": passive, "ships": [clean(n) for n in npcs]}}


def run(s, runs=200, seed=1):
    path = HERE / "scn.json"
    path.write_text(json.dumps(s))
    res = HERE / "res.json"
    out = subprocess.run([str(SIM), "space", str(path), "--runs", str(runs), "--seed", str(seed), "--json", str(res)],
                         capture_output=True, text=True)
    if out.returncode:
        sys.exit(out.stdout + out.stderr)
    r = json.loads(res.read_text())
    r = r[0] if isinstance(r, list) else r
    left = (r["MedianAttackerHullRemaining"] + r["MedianAttackerShieldRemaining"]) / max(1, r["AttackerMaxHull"] + r["AttackerMaxShield"])
    tt = r["TimesToKill"]
    return dict(mean=round(sum(tt) / len(tt), 1) if tt else 999, ttk=r["MedianTimeToKill"], win=r["AttackerWinRate"], left=round(left, 2))


# Standard fits used by the checks
FIT = {
    "Fighter": {"Laser Cannon": 3, "Shield Booster": 1, "Targeting Computer": 1, "Hull Plating": 1, "Thrusters": 1},
    "Fighter-missile": {"Missile Launcher": 3, "Shield Booster": 1, "Targeting Computer": 1, "Hull Plating": 1, "Thrusters": 1},
    "Interceptor": {"Laser Cannon": 3, "Shield Booster": 1, "Targeting Computer": 1, "Hull Plating": 1, "Thrusters": 1},
    "Bomber": {"Laser Cannon": 2, "Torpedo Launcher": 1, "Shield Booster": 1, "Targeting Computer": 1, "Hull Plating": 1, "Thrusters": 1},
    "Gunship": {"Laser Cannon": 3, "Shield Booster": 1, "Targeting Computer": 1, "Shield Repairer": 1, "Hull Plating": 1, "Thrusters": 1,
                "Capacitor Regulator": 1},
    # Dicts keep order: within a row the first modules take the Helm slots, the rest the crew-station slots.
    "Frigate": {"Laser Cannon": 4, "Shield Booster": 1, "Targeting Computer": 1, "Shield Booster#2": 1, "Shield Repairer": 1,
                "Hull Plating": 1, "Thrusters": 1, "Hull Plating#2": 1, "Capacitor Regulator": 1},
    "Cruiser": {"Turbolaser": 1, "Point-Defense Turret": 1, "Turbolaser#2": 3, "Point-Defense Turret#2": 1,
                "Shield Booster": 1, "Targeting Computer": 1, "Shield Booster#2": 2, "Shield Repairer": 1,
                "Hull Plating": 1, "Thrusters": 1, "Hull Plating#2": 2, "Capacitor Regulator": 1},
}

# ---------------------------------------------------------------------------------------------------------------
# The 50 ships (Appendix C): name, role, maker, Advanced, model, signature text, signature stats
# ---------------------------------------------------------------------------------------------------------------
SHIPS = [
    ("Striker", "Fighter", "Corellian", False, 10057, "Weapon cycles 5% faster.", {"dmg": 5}),
    ("Republic Striker", "Fighter", "Republic", False, 10071, "+15% shield regen.", {"shieldregen": 15}),
    ("Davaab", "Fighter", "Mandalorian", False, 10155, "+10% max hull and +5% missile damage.", {"hull": 10, "explosive": 5}),
    ("Aurek Strikefighter", "Fighter", "Republic", True, 10074, "+20% max shield and +20% shield regen; Point-Defense fires as Medium.", {"shield": 20, "shieldregen": 20}),
    ("Sith Fighter", "Fighter", "Sith", True, 10092, "+10% Critical Rate and +10% weapon damage; critical hits restore 1 capacitor.", {"crit": 10, "dmg": 10}),
    ("Cutlass", "Fighter", "Corellian", True, 10161, "+1 Mid slot.", {"M": 1}),
    ("Condor", "Interceptor", "Corellian", False, 10055, "+10% speed.", {"speed": 10}),
    ("Republic Scout", "Interceptor", "Republic", False, 10068, "Ion Web range +20%.", {}),
    ("Sith Scout", "Interceptor", "Sith", False, 10086, "+10% damage to webbed targets.", {}),
    ("Eclipse Interceptor", "Interceptor", "Sith", True, 2006, "+15% speed; webs also lower the target's evasion by 10.", {"speed": 15}),
    ("Republic Bomber", "Bomber", "Republic", False, 10058, "Torpedo hits strip 10% extra shield.", {}),
    ("Sith Bomber", "Bomber", "Sith", False, 10077, "+10% torpedo damage and +5% torpedo Critical Rate.", {}),
    ("Chela Bomber", "Bomber", "Republic", True, 10149, "Torpedoes ignore 25% of the target's resistances.", {}),
    ("Sith Infiltrator", "Explorer", "Sith", False, 10083, "Sensor Sweep reveals ships 50% farther away.", {}),
    ("Chiss Hyperion", "Explorer", "Chiss", False, 10230, "+25% scan strength; signatures show their tier from 25%.", {}),
    ("Raider Prowler", "Explorer", "Rift Raider", True, 10215, "Scans Wild Space 50% faster and ignores environment penalties.", {}),
    ("Republic Gunship", "Gunship", "Republic", False, 10061, "+10% shield resistances.", {}),
    ("Sith Gunship", "Gunship", "Sith", False, 10080, "+10% threat generation and +5% damage.", {"dmg": 5}),
    ("Hutt Gunship", "Gunship", "Hutt", False, 10107, "+20% max hull, -10% speed.", {"hull": 20, "speed": -10}),
    ("Basilisk War Droid", "Gunship", "Mandalorian", True, 10120, "+15 hull resistances and +10% explosive damage.", {"res": 15, "explosive": 10}),
    ("Czerka Repair Tender", "Logistics", "Czerka", False, 10182, "+15% remote hull repair.", {}),
    ("Chiss Caretaker", "Logistics", "Chiss", False, 10103, "Remote repair range +25%.", {}),
    ("Jedi Transport", "Logistics", "Republic", True, 10119, "Repairs 30% stronger; repairs also remove one slow.", {}),
    ("Y8 Miner", "Mining Barge", "Czerka", False, 10152, "+15% ore yield.", {}),
    ("Gas Skimmer", "Mining Barge", "Corellian", False, 10185, "+15% gas yield.", {}),
    ("XS Freighter", "Light Freighter", "Corellian", False, 10146, "+20% cargo.", {"cargo": 20}),
    ("Hutt Hauler", "Light Freighter", "Hutt", False, 10186, "Comes with a built-in Smuggling Compartment.", {}),
    ("Lambda Shuttle", "Light Freighter", "Republic", True, 2002, "+30% cargo and +15% max shield.", {"cargo": 30, "shield": 15}),
    ("Pirate Freighter", "Blockade Runner", "Corellian", False, 10184, "Cloaks in 2 seconds and +10% cargo.", {"cargo": 10}),
    ("Barracuda", "Blockade Runner", "Corellian", True, 10113, "Immune to Ion Webs; ships must be within 5 m to reveal it.", {}),
    ("Republic Hammerhead", "Frigate", "Republic", False, 10076, "Point-Defense deals +20% damage.", {}),
    ("Sith Hammerhead", "Frigate", "Sith", False, 10138, "+5% Critical Rate.", {"crit": 5}),
    ("Mandalorian Raider", "Frigate", "Mandalorian", False, 10121, "+15% max hull and +10% missile damage.", {"hull": 15, "explosive": 10}),
    ("Raider Marauder", "Frigate", "Rift Raider", True, 10196, "Shield regen doubles while no weapon is firing; +10% weapon damage.", {"dmg": 10}),
    ("Heavy Lifter", "Industrial", "Czerka", False, 10188, "+20% strip-mining yield.", {}),
    ("Czerka Salvager", "Industrial", "Czerka", False, 10197, "+20% salvage yield.", {}),
    ("Freight Train", "Heavy Freighter", "Czerka", False, 10189, "+30% cargo.", {"cargo": 30}),
    ("Republic Quartermaster", "Heavy Freighter", "Republic", False, 10130, "+15% max shield; allied ships within 30 m take 5% less damage.", {"shield": 15}),
    ("Hutt Grand Barge", "Heavy Freighter", "Hutt", True, 10109, "+40% cargo; Jump Beacon jumps cost 50% less fuel.", {"cargo": 40}),
    ("Neutral Thranta", "Cruiser", "Corellian", False, 10135, "+1 High slot (Gunner); -10% max hull and shield.", {"H": 1, "hull": -10, "shield": -10}),
    ("Republic Thranta", "Cruiser", "Republic", False, 10123, "+10% max shield.", {"shield": 10}),
    ("Sith Thranta", "Cruiser", "Sith", False, 10139, "+10% damage.", {"dmg": 10}),
    ("Hutt Corvette", "Cruiser", "Hutt", False, 10108, "+20% max hull and +50% cargo.", {"hull": 20, "cargo": 50}),
    ("Jehavey'ir", "Cruiser", "Mandalorian", False, 10154, "+15% max hull and +10% explosive damage.", {"hull": 15, "explosive": 10}),
    ("Chiss Trireme", "Cruiser", "Chiss", False, 10162, "+15% weapon range.", {}),
    ("Corellian Gunboat", "Cruiser", "Corellian", False, 10145, "+10% speed and +4 evasion.", {"speed": 10, "ev": 4}),
    ("Czerka Armored Transport", "Command Ship", "Czerka", False, 10218, "Crew stations +10% effective; droid crew +20%.", {}),
    ("Terminus Corsair", "Command Ship", "Corellian", False, 10207, "Allied ships within command range +5% speed.", {}),
    ("Crusader Corvette", "Command Ship", "Mandalorian", False, 10260, "+15% max hull; allies within command range +5% explosive damage.", {"hull": 15}),
    ("Republic Centurion", "Command Ship", "Republic", True, 10128, "Command range +20 m; orders last 25% longer.", {}),
]
SIGNATURE = {s[0]: s[6] for s in SHIPS}
