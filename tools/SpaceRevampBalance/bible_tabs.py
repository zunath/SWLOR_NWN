"""Write the W0 space revamp tabs into the Design Bible at the zip/XML level.

usage: python bible_tabs.py   (run checks.py first; it writes the Space Balance tab from checks.md)
Every untouched zip entry is copied byte for byte. Re-running replaces the four tabs in place.
"""
import html
import json
import re
import shutil
import sys
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import model as m  # noqa: E402

WORKBOOK = m.REPO / "design" / "bible" / "SWLOR Design Bible.xlsx"
LAYOUT = m.REPO / "tools" / "CombatUpgradeBibleWorkbookLayout.json"
CHECKS = m.HERE / "checks.md"
HEAD, BODY = 369, 370
NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
STATUS = "Dev Status: Proposed (space revamp W0 draft). Replaces the live tab at release; values are simulator-checked on Space Balance (Revamp)."


def col(i):
    s = ""
    i += 1
    while i:
        i, r = divmod(i - 1, 26)
        s = chr(65 + r) + s
    return s


class Sheet:
    def __init__(self, name, widths):
        self.name, self.widths, self.rows, self.n, self.merges = name, widths, [], 0, []

    def row(self, values, style=BODY):
        self.n += 1
        cells = []
        for i, v in enumerate(values):
            if v is None or v == "":
                continue
            ref = f"{col(i)}{self.n}"
            if isinstance(v, bool):
                v = "Yes" if v else "No"
            if isinstance(v, (int, float)):
                v = round(v, 3)
                cells.append(f'<c r="{ref}" s="{style}"><v>{v}</v></c>')
            else:
                cells.append(f'<c r="{ref}" s="{style}" t="inlineStr"><is><t xml:space="preserve">{html.escape(str(v), quote=False)}</t></is></c>')
        self.rows.append(f'<row r="{self.n}">{"".join(cells)}</row>')

    def title(self, text, style=HEAD):
        self.row([text], style)
        self.merges.append(f"A{self.n}:{col(len(self.widths) - 1)}{self.n}")

    def note(self, text):
        self.title(text, BODY)

    def blank(self):
        self.n += 1
        self.rows.append(f'<row r="{self.n}"/>')

    def table(self, headers, rows):
        self.row(headers, HEAD)
        for r in rows:
            self.row(r)
        self.blank()

    def xml(self):
        cols = "".join(f'<col min="{i + 1}" max="{i + 1}" width="{w}" customWidth="1"/>' for i, w in enumerate(self.widths))
        merges = (f'<mergeCells count="{len(self.merges)}">' + "".join(f'<mergeCell ref="{r}"/>' for r in self.merges)
                  + "</mergeCells>") if self.merges else ""
        return ('<?xml version="1.0" encoding="utf-8" standalone="yes"?>'
                f'<worksheet xmlns="{NS}" xmlns:r="{REL_NS}"><sheetPr><pageSetUpPr/></sheetPr>'
                f'<dimension ref="A1:{col(len(self.widths) - 1)}{self.n}"/>'
                '<sheetViews><sheetView showGridLines="0" workbookViewId="0">'
                '<pane ySplit="2" topLeftCell="A3" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>'
                '<sheetFormatPr defaultColWidth="12.63" defaultRowHeight="15"/>'
                f'<cols>{cols}</cols><sheetData>{"".join(self.rows)}</sheetData>{merges}'
                '<pageMargins left="0.3" right="0.3" top="0.5" bottom="0.5" header="0.2" footer="0.2"/></worksheet>')

    def layout(self):
        return [{"min": i + 1, "max": i + 1, "width": w} for i, w in enumerate(self.widths)]


def pct_text(d):
    names = {"hull": "max hull", "shield": "max shield", "cap": "max capacitor", "speed": "speed", "cargo": "cargo",
             "shieldregen": "shield regen", "dmg": "weapon damage", "explosive": "explosive damage", "crit": "Critical Rate",
             "repair": "repair amount", "yieldpct": "mining and salvage yield", "range": "weapon range", "scan": "scan strength"}
    out = []
    for k, v in d.items():
        if k in names and v:
            out.append(f"{'+' if v > 0 else ''}{v}% {names[k]}")
        elif k in ("acc", "ev", "df", "res") and v:
            out.append(f"{'+' if v > 0 else ''}{v} {dict(acc='Accuracy', ev='Evasion', df='Defense', res='all resistances')[k]}")
    return ", ".join(out)


# -----------------------------------------------------------------------------------------------------------------
def starships():
    s = Sheet("Starships (Revamp)", [24, 11, 12, 12, 7, 7, 7, 12, 8, 8, 10, 9, 8, 8, 8, 9, 9, 8, 9, 9, 26, 60])
    s.title("Starships (Revamp): hulls, roles, manufacturers and the 50 player ships")
    s.note(STATUS)
    s.blank()
    s.title("Ship stat rules")
    T = m.T
    s.table(["Rule", "Value", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "Notes"], [
        ["Piloting rank", "No stats", *[""] * 19, "Piloting gates hulls and modules through RequiresSkill item properties only. Every ship stat comes from the hull, modules, attributes and perks."],
        ["Hull gate", "Small 0, Medium 10, Large 25, Capital 40", *[""] * 19, "Advanced ships add 10: Small 10, Medium 20, Large 35, Capital 50."],
        ["Ship formula", "Role baseline, then manufacturer, Advanced and signature", *[""] * 19, "Percent changes apply to the ship's totals with modules fitted. Flat changes (Attack, Accuracy, Defense, Evasion, resistances) add to the hull."],
        ["Advanced", f"+{m.ADVANCED['pool']}% max hull, shield and capacitor; +{m.ADVANCED['flat']} Attack, Accuracy, Defense and Evasion", *[""] * 19, "Plus its signature bonus. No Advanced ship gets an extra High slot; the Cutlass's extra Mid slot is its whole signature."],
        ["Accuracy", "Hull + weapon step + best Targeting Computer + AGI x 3 (energy) or PER x 3 (launcher)", *[""] * 19, ""],
        ["Evasion", "Hull + best Thrusters + AGI x 3", *[""] * 19, ""],
        ["Attack", "Hull + weapon step + PER (energy) or MGT (launcher)", *[""] * 19, "Mirrors the ground 8 + 2 x rank + attribute: the hull carries the 8, each weapon step its 2 x rank."],
        ["Defense", "Hull + best Hull Plating + VIT", *[""] * 19, "Mirrors the ground 8 + 1.2 x armor rank + VIT + armor gear."],
        ["Hit chance", "Combat.CalculateHitRate(Accuracy, Evasion) less the size penalty", *[""] * 19, "20-95% as on the ground."],
        ["Damage", "Combat.CalculateDamageRange(Attack, DMG, attribute, Defense, target VIT)", *[""] * 19, "The firing module's rank sets the delta cap. Shield takes damage first; the rest goes to hull."],
        ["Critical hits", "Ground formula: 5 + module rank / 10 + (PER - target VIT) / 5 (0-3)", *[""] * 19, "5-50% limits. A critical hit adds 25% to the damage range."],
        ["Non-stacking", "Only the best Hull Plating Defense, Thrusters Evasion and Targeting Computer Accuracy count", *[""] * 19, "Their hull, shield and speed parts stack. Same rule as ground armor."],
        ["Max capacitor", f"Hull + {T['cap_per_wil']} x WIL + Capacitor Batteries", *[""] * 19, ""],
        ["Capacitor regen", f"Hull per second + {T['cap_regen_per_wil']} x WIL + Capacitor Regulators", *[""] * 19, ""],
        ["Shield regen", "Max shield x size rate per second x (1 + 2% per WIL above 10)", *[""] * 19, "Size rates: " + ", ".join(f"{k} {v * 100:.2f}%" for k, v in T["shield_regen_pct"].items()) + ". Always on."],
        ["Crew-station slots", "Work only while their seat is crewed", *[""] * 19, "Each Large and Capital role tags some slots Gunner, Engineer or Sensors (Station column below). Helm slots fill first. "
         "Large: Pilot, Gunner (High station slots), Engineer (Mid and Low station slots). Capital: Pilot, Gunner (first half "
         "of the High station slots, rounded up), Second Gunner (the rest), Engineer (Mid and Low station slots except the "
         "last Mid), Sensors (last Mid station slot). Seat skills: Gunners Piloting, Engineer Engineering, Sensors Espionage. "
         "A station slot, passive or active, works only while its seat is crewed, at its crew's effectiveness: a player at "
         "the module's rank 100%; droid crew by tier (best droid 50% at a Gunner seat, 75% at Engineer and Sensors; tier-1 "
         "combat droid 25%). A solo pilot runs only Helm slots. Crew Morale (+10%) and the captain's Leadership (+1% per 10 "
         "ranks, up to +5%) raise crew effectiveness, capped at 105%."],
    ])
    s.title("Size rules")
    s.table(["Size", "Gate", "Advanced", "Weapon output", "", "", "", "Module output", "Shield regen", "", "", "", "", "", "", "", "", "", "", "", "", "Notes"], [
        [sz, m.GATE[sz], m.GATE[sz] + 10, f"x{T['weapon_size_mult'][sz]}" + (" (capital weapons x%s)" % T["capital_weapon_mult"] if sz == "Capital" else ""),
         "", "", "", f"x{T['module_size_mult'][sz]}", f"{T['shield_regen_pct'][sz] * 100:.2f}%/s", *[""] * 12,
         {"Small": "Light weapons. Every NPC dock and zone admits Small hulls.",
          "Medium": "Medium weapons.",
          "Large": "Heavy weapons. Crew: Pilot, Gunner, Engineer. Every NPC dock takes Large and smaller.",
          "Capital": "Capital weapons on capital hulls only. Crew: Pilot, 2 Gunners, Engineer, Sensors. Docks at Veles, Mon Cala, Anchorhead, Smuggler's Moon Station, Korriban, and player starports at upgrade level 2."}[sz]]
        for sz in m.SIZES] + [
        ["Hit penalty", "", "", f"-{T['size_hit_penalty']} hit chance per size step a weapon is larger than its target", *[""] * 17, "Applied before the 20-95% limits."],
        ["Damage penalty", "", "", f"x{1 - T['size_damage_penalty']:.2f} damage per size step a weapon is smaller than its target", *[""] * 17, "Compounds per step: a Light weapon does x0.68 to a Capital hull."],
        ["Exceptions", "", "", "Torpedoes ignore both penalties; Point-Defense always fires as Light; capital weapons fire as Capital", *[""] * 17, ""],
    ])
    s.title("Role baselines (Standard hull, before manufacturer and signature)")
    rows = []
    for role, r in m.ROLES.items():
        st = "/".join(str(r.get(k, 0)) for k in ("stH", "stM", "stL"))
        rows.append([role, r["size"], m.GATE[r["size"]], r["H"], r["M"], r["L"], st if st != "0/0/0" else "-", r["hull"], r["shield"], r["cap"],
                     r["capr"], r["speed"], r["atk"], r["acc"], r["df"], r["ev"], r["cargo"], r["interior"], r["repair"], "", r["crew"], r["bonus"]])
    s.table(["Role", "Size", "Piloting", "High", "Mid", "Low", "Station H/M/L", "Hull", "Shield", "Capacitor", "Cap regen/s", "Speed",
             "Attack", "Accuracy", "Defense", "Evasion", "Cargo", "Interior", "Repair x", "", "Crew stations", "Built-in bonus"], rows)
    s.note("Interior is the structure limit. Repair x multiplies the repair bill (SOC and starport discounts still apply). Station H/M/L counts the slots in each row tagged to a crew seat.")
    s.blank()
    s.title("Manufacturers")
    counts = {}
    for sh in m.SHIPS:
        counts[sh[2]] = counts.get(sh[2], 0) + 1
    s.table(["Manufacturer", "Ships", "", "Modifiers", *[""] * 17, "Theme"],
            [[k, counts.get(k, 0), "", pct_text({kk: vv for kk, vv in v.items() if kk != "theme"}), *[""] * 17, v["theme"]] for k, v in m.MAKERS.items()])
    s.title("The 50 player ships")
    rows = []
    for i, (name, role, maker, adv, model_row, text, sig) in enumerate(m.SHIPS, 1):
        h = m.hull_stats(role, maker, adv, sig)
        r = m.ROLES[role]
        f = lambda k: round(r[k] * (1 + h["pct"][k] / 100))
        mods = pct_text({**{k: v for k, v in m.MAKERS[maker].items() if k != "theme"}})
        rows.append([name, h["size"], role, maker, "Advanced" if adv else "Standard", m.GATE[h["size"]] + (10 if adv else 0), model_row,
                     h["H"], h["M"], h["L"], f("hull"), f("shield"), f("cap"), r["capr"], h["speed"], h["atk"], h["acc"], h["df"], h["ev"],
                     h["cargo"], mods + ("; Advanced" if adv else ""), text])
    s.table(["Ship", "Size", "Role", "Manufacturer", "Tier", "Piloting", "Model", "High", "Mid", "Low", "Hull", "Shield", "Capacitor",
             "Cap regen/s", "Speed", "Attack", "Accuracy", "Defense", "Evasion", "Cargo", "Manufacturer modifiers", "Signature bonus"], rows)
    s.note("Hull, shield and capacitor show the bare hull with its percent changes; in play those percentages apply to the totals with modules fitted. Launch skins and model scales are in the plan's Appendix C and W3.")
    s.blank()
    s.title("Reference fits at rank 45 (all modules rank 45, attributes 25)")
    rows = []
    for role, fitname in (("Fighter", "Fighter"), ("Interceptor", "Interceptor"), ("Bomber", "Bomber"), ("Gunship", "Gunship"),
                          ("Frigate", "Frigate"), ("Cruiser", "Cruiser")):
        for crew in ((1.0, 0.0) if m.ROLES[role].get("stH") else (1.0,)):
            p = m.player_ship(role, 45, m.FIT[fitname], crew=crew)
            fit = ", ".join(f"{k.split('#')[0]} x{v}" for k, v in m.FIT[fitname].items())
            rows.append([role + (" (solo)" if crew == 0 else " (crewed)" if m.ROLES[role].get("stH") else ""), p["size"], "", "", "", "", "", "", "", "",
                         p["maxHull"], p["maxShield"], p["maxCapacitor"], p["capacitorRegenPerSecond"], "", p["attack"], p["accuracy"], p["defense"],
                         p["evasion"], len(p["weapons"]), "", fit])
    s.table(["Fit", "Size", "", "", "", "", "", "", "", "", "Hull", "Shield", "Capacitor", "Cap regen/s", "", "Attack", "Accuracy", "Defense",
             "Evasion", "Weapons firing", "", "Modules (Helm slots first)"], rows)
    s.note("Accuracy and Evasion here exclude AGI x 3; Attack excludes the weapon step and attribute.")
    return s


# -----------------------------------------------------------------------------------------------------------------
UTILITY = {
    "Shield Hardener": lambda r: f"+{10 + r // 5} all resistances",
    "Resist Plating": lambda r: f"+{10 + r // 3} one type, or +{6 + r // 6} all (-4 Evasion)",
    "Damage Amplifier": lambda r: f"+{5 + r // 9}% one damage type",
    "Ion Web": lambda r: f"target speed -{20 + r // 3}%",
    "Jammer": lambda r: f"target Accuracy -{4 + round(0.4 * r)}",
    "Target Painter": lambda r: f"target takes +{5 + r // 9}% damage",
    "Interdictor": lambda r: f"{15 + r // 5} m",
    "Afterburner": lambda r: f"+{25 + r // 3}% speed",
    "Cloaking Device": lambda r: f"reveal range {10 - r // 9} m",
    "Hypermatter Injector": lambda r: f"+{30 + r} capacitor",
    "Cargo Expander": lambda r: f"+{20 + r // 3}% cargo",
    "Smuggling Compartment": lambda r: f"scan chance -{15 + r // 3}%",
    "Probe Launcher": lambda r: f"scan strength {10 + 2 * r}",
    "Mining Laser": lambda r: f"tier {r // 10 + 1}",
}


def modules():
    s = Sheet("Ship Modules (Revamp)", [26, 8, 18, 10, 9, 10, 12, 12, 12, 12, 12, 12, 12, 70])
    s.title("Ship Modules (Revamp): families, rank steps and size scaling")
    s.note(STATUS)
    s.blank()
    fam = [
        ["Laser Cannon", "High", "Piloting (Gunner)", "Toggle", 3.0, 3, "Weapon", "Thermal; energy (AGI accuracy, PER damage)."],
        ["Ion Cannon", "High", "Piloting (Gunner)", "Toggle", 3.0, 3, "Weapon", f"EM; x{m.WEAPONS['Ion Cannon']['shield']} damage to shields; hull hits slow the target (Mobility)."],
        ["Beam Cannon", "High", "Piloting (Gunner)", "Toggle", 3.0, 5, "Weapon", "Thermal over 3 ticks; short range (15 m)."],
        ["Missile Launcher", "High", "Piloting (Gunner)", "Toggle", 3.0, 2, "Weapon", "Explosive; launcher (PER accuracy, MGT damage); long range; 1 missile per cycle; +20% damage to a target with no shield. Ion, incendiary and seeker ammo."],
        ["Torpedo Launcher", "High", "Piloting (Gunner)", "One press", 18.0, 10, "Weapon", "Explosive; bypasses shields; ignores both size penalties; 1 torpedo per use."],
        ["Point-Defense Turret", "High", "Piloting (Gunner)", "Toggle", 2.5, 2, "Always Light", "Thermal; +20% damage to Small hulls."],
        ["Turbolaser", "High", "Piloting (Gunner)", "Toggle", 4.0, 6, "Capital", "Capital hulls only; ranks 40 and 45."],
        ["Quad Laser", "High", "Piloting (Gunner)", "Toggle", 2.0, 3, "Capital", "Capital hulls only."],
        ["Storm Cannon", "High", "Piloting (Gunner)", "Toggle", 4.0, 6, "Capital", "Capital ion cannon: EM, x1.5 to shields, slows."],
        ["Laser Battery", "High", "Piloting (Gunner)", "One press", 24.0, 20, "Capital", "Area volley: Turbolaser DMG x2 to up to 5 enemies within 15 m of the target."],
        ["Assault Concussion Missile", "High", "Piloting (Gunner)", "One press", 30.0, 15, "Capital", "Turbolaser DMG x6, bypasses shields; uses rare ammo."],
        ["Mining Laser", "High", "Gathering (Helm)", "Toggle", 6.0, 3, "Module", "Mines asteroids up to its tier (rank / 10 + 1). Yield in W6."],
        ["Strip Miner", "High", "Gathering (Helm)", "Toggle", 10.0, 8, "Module", "Large and Capital hulls; strips a whole asteroid and roots the ship."],
        ["Salvager", "High", "Gathering (Helm)", "Toggle", 6.0, 3, "Module", "Salvages wrecks up to its tier."],
        ["Gas Harvester", "High", "Gathering (Helm)", "Toggle", 6.0, 3, "Module", "Harvests gas clouds up to its tier."],
        ["Shield Booster", "Mid", "Piloting (Helm)", "Passive", "", "", "Module", "+ max shield (table)."],
        ["Shield Repairer", "Mid", "Piloting (Engineer)", "Toggle", 4.0, 4, "Module", "Repairs its locked target's shield, or your own."],
        ["Shield Hardener", "Mid", "Piloting (Engineer)", "Toggle", 4.0, 4, "None", "+ all resistances while on."],
        ["Capacitor Battery", "Mid", "Piloting (Helm)", "Passive", "", "", "Module", "+ max capacitor."],
        ["Hypermatter Injector", "Mid", "Piloting (Engineer)", "One press", 30.0, 0, "Module", "Restores capacitor; uses a capsule."],
        ["Ion Web", "Mid", "Piloting (Sensors)", "Toggle", 3.0, 3, "None", "Slows the target (Mobility); 20 m."],
        ["Interdictor", "Mid", "Piloting (Sensors)", "Toggle", 4.0, 5, "None", "The target can't jump, dock or leave space while held; range in the table."],
        ["Jammer", "Mid", "Piloting (Sensors)", "Toggle", 4.0, 4, "None", "Target Accuracy down."],
        ["Target Painter", "Mid", "Piloting (Sensors)", "Toggle", 4.0, 4, "None", "Target takes more damage from every source."],
        ["Afterburner", "Mid", "Piloting (Helm)", "Toggle", 2.0, 3, "None", "+ speed while on."],
        ["Cloaking Device", "Mid", "Piloting (Helm)", "Toggle", 1.0, 2, "None", "Small hulls and Blockade Runners; speed -50%; no module or ability use while cloaked (W16)."],
        ["Probe Launcher", "Mid", "Piloting (Sensors)", "Toggle", 10.0, 4, "None", "Scans hidden signatures (W12)."],
        ["Data Analyzer", "Mid", "Espionage (Sensors)", "One press", 20.0, 5, "None", "No rank ladder; the Slicing perk gates targets."],
        ["Targeting Computer", "Mid", "Piloting (Helm)", "Passive", "", "", "None", "+ Accuracy; the best one counts."],
        ["Hull Plating", "Low", "Piloting (Helm)", "Passive", "", "", "Module", "+ max hull and + Defense; only the best Defense counts."],
        ["Hull Repairer", "Low", "Piloting (Engineer)", "Toggle", 4.0, 4, "Module", "Repairs its locked target's hull, or your own (80% of the shield amount)."],
        ["Resist Plating", "Low", "Piloting (Helm)", "Passive", "", "", "None", "One resistance, or all at a cost to Evasion."],
        ["Capacitor Regulator", "Low", "Piloting (Engineer)", "Passive", "", "", "Module", "+ capacitor regen."],
        ["Thrusters", "Low", "Piloting (Helm)", "Passive", "", "", "None", "+ Evasion (best one counts) and +5% speed (stacks)."],
        ["Damage Amplifier", "Low", "Piloting (Helm)", "Passive", "", "", "None", "+ one damage type (EM, Thermal or Explosive)."],
        ["Cargo Expander", "Low", "Piloting (Helm)", "Passive", "", "", "None", "+ cargo; -5% speed."],
        ["Smuggling Compartment", "Low", "Piloting (Helm)", "Passive", "", "", "None", "Lowers the customs scan chance."],
    ]
    s.table(["Family", "Row", "Skill (seat)", "Use", "Cycle s", "Capacitor", "Size scaling", "", "", "", "", "", "", "Notes"],
            [[a, b, c, d, e, f, g, "", "", "", "", "", "", n] for a, b, c, d, e, f, g, n in fam])
    s.note("Every family has an item every 5 ranks (0-45; rank N needs rank N in the skill); capital families have ranks 40 and 45; rank 50 is reserved for named boss modules. Weapon scaling: Light (Small hull) x1.0, Medium x"
           f"{m.T['weapon_size_mult']['Medium']}, Heavy (Large and Capital hulls) x{m.T['weapon_size_mult']['Large']}, capital weapons x{m.T['capital_weapon_mult']}. Module scaling (hull, shield, repair, capacitor): Small x1.0, Medium, Large and Capital x{m.T['module_size_mult']['Medium']}.")
    s.blank()
    s.title("Weapon steps (Light DMG; multiply by the size scaling)")
    rows = []
    for r in m.STEPS:
        d = m.GROUND_DMG[r]
        cap = (lambda k: round(d * m.WEAPONS[k]["dmg"] * m.T["capital_weapon_mult"])) if r >= 40 else (lambda k: "")
        rows.append([f"Rank {r}", r, 2 * r, 2 * r, round(d * m.WEAPONS["Laser Cannon"]["dmg"]), round(d * m.WEAPONS["Ion Cannon"]["dmg"]),
                     round(d * m.WEAPONS["Beam Cannon"]["dmg"]), round(d * m.WEAPONS["Missile Launcher"]["dmg"]),
                     round(d * m.WEAPONS["Torpedo Launcher"]["dmg"]), round(d * m.WEAPONS["Point-Defense Turret"]["dmg"]),
                     cap("Turbolaser"), cap("Quad Laser"), cap("Storm Cannon"),
                     "DMG follows the ground longsword table by required rank." if r == 0 else ""])
    s.table(["Step", "Rank", "Attack", "Accuracy", "Laser DMG", "Ion DMG", "Beam DMG", "Missile DMG", "Torpedo DMG", "Point-Defense DMG",
             "Turbolaser DMG", "Quad Laser DMG", "Storm Cannon DMG", "Notes"], rows)
    s.title("Defensive and capacitor steps (Small hull; multiply HP, repair and capacitor by the module scaling)")
    rows = []
    for r in m.STEPS:
        rows.append([f"Rank {r}", r, m.HP_STEP[r], round(1.8 * r), m.HP_STEP[r], m.REPAIR_STEP[r], max(1, round(m.REPAIR_STEP[r] * 0.8)),
                     2 * r, 2 + round(0.4 * r), m.CAP_STEP[r], m.CAP_REGEN_STEP[r], 10 + r // 5, "",
                     "Repairers: amount per 4 s cycle for 4 capacitor." if r == 0 else ""])
    s.table(["Step", "Rank", "Hull Plating hull", "Hull Plating Defense", "Shield Booster shield", "Shield Repairer / cycle",
             "Hull Repairer / cycle", "Thrusters Evasion", "Targeting Computer Accuracy", "Capacitor Battery", "Capacitor Regulator /s",
             "Shield Hardener resist", "", "Notes"], rows)
    s.title("Utility steps")
    keys = list(UTILITY)
    for chunk in (keys[:7], keys[7:]):
        s.table(["Step", "Rank"] + chunk + [""] * (11 - len(chunk)) + [""],
                [[f"Rank {r}", r] + [UTILITY[k](r) for k in chunk] + [""] * (11 - len(chunk)) + [""] for r in m.STEPS])
    s.title("Capital modules (Capital hulls only; ranks 40 and 45)")
    s.table(["Module", "Row", "Use", "Rank 40", "Rank 45", "", "", "", "", "", "", "", "", "Notes"], [
        ["Bulwark Shield Generator", "Mid", "Toggle", "+15% max shield to allies within 30 m", "+20%", *[""] * 8, "Engineer seat; 8 capacitor per 4 s."],
        ["Repair Field", "Mid", "Toggle", f"{round(m.REPAIR_STEP[40] * 1.1)} shield per 4 s to allies within 30 m", f"{round(m.REPAIR_STEP[45] * 1.1)}", *[""] * 8, "Engineer seat; 10 capacitor per 4 s; never the capital itself."],
        ["E-War Array", "Mid", "Toggle", "-12 Accuracy to enemies within 30 m", "-14", *[""] * 8, "Sensors seat; 8 capacitor per 4 s."],
        ["Advanced Thrusters", "Low", "Passive", "+80 Evasion, +10% speed", "+90, +10%", *[""] * 8, "Counts as Thrusters for the best-one rule."],
        ["Redundant Shield Generator", "Low", "Passive", f"+{round(m.HP_STEP[40] * 1.5 * 1.1)} max shield", f"+{round(m.HP_STEP[45] * 1.5 * 1.1)}", *[""] * 8, ""],
        ["Reinforced Plating", "Low", "Passive", f"+{round(m.HP_STEP[40] * 1.5 * 1.1)} max hull, +72 Defense", f"+{round(m.HP_STEP[45] * 1.5 * 1.1)}, +81", *[""] * 8, "Counts as Hull Plating for the best-one rule."],
        ["Targeting Array", "Low", "Passive", "+22 Accuracy", "+24", *[""] * 8, "Counts as a Targeting Computer for the best-one rule."],
        ["Weapons Computer", "Low", "Passive", "+8% weapon damage", "+10%", *[""] * 8, ""],
        ["Power Diverter", "Low", "Passive", f"+{round(m.CAP_REGEN_STEP[40] * 2, 2)} capacitor regen/s", f"+{round(m.CAP_REGEN_STEP[45] * 2, 2)}", *[""] * 8, ""],
    ])
    return s


# -----------------------------------------------------------------------------------------------------------------
def enemies():
    s = Sheet("Space Enemies (Revamp)", [26, 8, 11, 18, 10, 9, 9, 10, 9, 9, 9, 9, 8, 8, 16, 9, 60])
    s.title("Space Enemies (Revamp): NPC ship presets from the ground Enemy Stat Presets")
    s.note(STATUS)
    s.blank()
    T = m.T
    s.title("Conversion rules")
    rule = lambda a, b, n="": [a, b, *[""] * 14, n]
    s.table(["Rule", "Value", *[""] * 14, "Notes"], [
        rule("Base row", "Enemy Stat Presets at the same level and difficulty", "Each space role names the ground role its stats come from (table below)."),
        rule("HP", "Ground HP x 1.55 at level 5 and below, sliding to x1.4 at level 15 and above, x role HP x difficulty factor",
             "Split 50% hull, 50% shield. Capital NPCs x" + str(T["capital_hp"]) + ". Keep every pool under the 16-bit limit (NpcHitPoints.md)."),
        rule("Shield regen", f"{T['npc_shield_regen_pct'] * 100:.1f}% of max shield per second"),
        rule("Damage", "Ground DMG x role damage x level factor x difficulty factor",
             "Level factor: " + ", ".join(f"L{k} x{v}" for k, v in T["npc_dmg_mult"].items()) + " (linear between, flat after)."),
        rule("Weapon cycle", "Ground Delay / 110 seconds", "Delay 280 = 2.55 s. One weapon per Small/Medium/Large NPC; capitals carry 2 turbolasers (4 s) and a Point-Defense turret."),
        rule("Attack", "8 + 2 x level + preset Attack + PER (energy) or MGT (launcher)"),
        rule("Accuracy", "8 + 2 x level + AGI x 3 (energy) or PER x 3 (launcher)"),
        rule("Evasion", "8 + 2 x level + preset Evasion + role Evasion + AGI x 3"),
        rule("Defense", "8 + 1.2 x level + VIT + Physical Defense + role Defense"),
        rule("Size", "Role size for Normal and Tough; Elite Medium or larger; Boss Large or larger", "Faction capitals (Harrower, Kandosii and the rest) are Boss presets at Capital size."),
        rule("Bomber torpedoes", "Bombers fire a torpedo (DMG x4, 18 s, bypasses shields, ignores size) at Large and Capital targets"),
    ])
    s.title("Roles")
    s.table(["Role", "Size", "Ground role", "Weapon", "HP x", "Defense +", "Evasion +", "Resist", "Damage x", *[""] * 7, "Behavior"],
            [[k, v["size"], v["ground"], v["weapon"], v["hp"], v["df"], v["ev"], v["res"], v["dmg"], *[""] * 7, v["ai"]] for k, v in m.NPC_ROLES.items()])
    s.note("Role behaviors run on the one second ship AI tick (ShipNpcRoleTuning holds the numbers). A Gunship's preset resist "
           f"gives up {m.HARDENER_SHARE:.0%} of its Shield Hardener's (the hardener runs over the last quarter of hull + shield), "
           "so its fight averages the role resist. Players cannot be goaded, so the tank holds threat by targeting. Every "
           "faction tier has every role: tN + fighter, interceptor, bomber, sniper, ewar, gunship, logistics, minelayer, "
           "carrier (cargo haulers use the Logistics row with no repairers).")
    s.title("Difficulty factors by level (on top of the Normal conversion)")
    lv = [5, 15, 25, 35, 45]
    s.table(["Difficulty", "Size", "Damage x", "", *[f"L{L} HP x" for L in lv], *[f"L{L} DMG x" for L in lv][:5], "", "", "Notes"][:17],
            [["Normal", "Role", 1.0, "", *[1.0] * 5, *[1.0] * 5, "", "", "Solo: a matched Fighter kills one in 75-85% of the ground time and beats two at once."],
             ["Tough", "Role", m.DIFFICULTY_DMG["Tough"], "", *[1.0] * 5, *[1.0] * 5, "", "", "Ground Tough HP as is."],
             ["Elite", "Medium+", m.DIFFICULTY_DMG["Elite"], "", *[m.curve("elite_hp", L) for L in lv], *[m.curve("elite_dmg", L) for L in lv], "", "",
              "A matched Fighter solos one in about 3.5x a Normal's time with about 25% hull and shield left."],
             ["Boss", "Large+", m.DIFFICULTY_DMG["Boss"], "", *[m.curve("boss_hp", L) for L in lv], *[m.curve("boss_dmg", L) for L in lv], "", "",
              "Takes one Fighter 10x a Normal's time; three Fighters beat it with about half their hull and shield left."]])
    s.title("Preset table")
    rows = []
    for L in [1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50]:
        for diff, roles in (("Normal", m.NORMAL_ROLES), ("Elite", ["Fighter", "Gunship"]), ("Boss", ["Fighter", "Gunship", "Carrier"])):
            for role in roles:
                if (L, diff, m.NPC_ROLES[role]["ground"]) not in m.PRESETS:
                    continue
                n = m.npc_ship(role, L, diff)
                w = n["weapons"][0]
                rows.append([f"{L}|{diff}|{role}", L, diff, role, n["size"], n["maxHull"], n["maxShield"], n["shieldRegenPerSecond"],
                             8 + w["attack"], 8 + w["accuracy"], n["defense"], n["evasion"], w["dmg"], w["cycleSeconds"], w["name"].replace("NPC ", ""),
                             n["resistances"]["em"], f"AGI {n['pilot']['agi']}, PER {n['pilot']['per']}, VIT {n['pilot']['vit']}, MGT {n['pilot']['mgt']}"])
        cap = m.npc_ship("Gunship", L, "Boss", size="Capital") if (L, "Boss", "Ranged") in m.PRESETS else None
        if cap:
            w = cap["weapons"][0]
            rows.append([f"{L}|Boss|Capital", L, "Boss", "Capital", "Capital", cap["maxHull"], cap["maxShield"], cap["shieldRegenPerSecond"],
                         8 + w["attack"], 8 + w["accuracy"], cap["defense"], cap["evasion"], w["dmg"], w["cycleSeconds"], "2 Turbolasers + Point-Defense",
                         cap["resistances"]["em"], f"AGI {cap['pilot']['agi']}, PER {cap['pilot']['per']}, VIT {cap['pilot']['vit']}, MGT {cap['pilot']['mgt']}"])
    s.table(["Preset Key", "Level", "Difficulty", "Role", "Size", "Hull", "Shield", "Shield regen/s", "Attack", "Accuracy", "Defense",
             "Evasion", "DMG", "Cycle s", "Weapon", "Resist", "Attributes"], rows)
    s.note("Accuracy and Evasion exclude AGI x 3 (or PER x 3), which the game adds from the attributes column.")
    s.blank()
    s.title("NPC ship roster (every space NPC creature; level from its skin)")
    import re as _re
    import npc_catalog
    source = (m.REPO / "SWLOR.Game.Server/Feature/SpaceObjectDefinition/NPCSpaceObjectDefinition.cs").read_text(encoding="utf-8")
    rows = []
    for e in npc_catalog.entries(m.REPO, _re.findall(r'_builder\.Create\("([^"]+)"\)', source)):
        weapons = ", ".join(f"{t} DMG {d}" for t, d, _a, _acc, _r in e["Weapons"])
        rows.append([e["CreatureTag"], e["Level"], e["Difficulty"], e["Role"], e["Size"], e["Hull"], e["Shield"],
                     e["ShieldRegenPerSecond"], e["Attack"], e["Accuracy"], e["Defense"], e["Evasion"], "", "", "",
                     e["ResistAll"], f"AGI {e['Agility']}, PER {e['Perception']}, VIT {e['Vitality']}, MGT {e['Might']}; {weapons}"])
    s.table(["Creature", "Level", "Difficulty", "Role", "Size", "Hull", "Shield", "Shield regen/s", "Attack", "Accuracy",
             "Defense", "Evasion", "", "", "", "Resist", "Attributes and weapons (DMG per module cycle)"], rows)
    s.note("Defense here excludes VIT, which the game adds. Faction capitals (sithcap, mandocap) are all level 65 and keep their "
           "old size ladder: HP x1 to x10 of the Thranta Corvette, DMG x1 to x2. Weapon DMG is per module cycle "
           "(lasers 3 s, turbolasers 4 s, Point-Defense 2.5 s, torpedoes 18 s, fired only at Large and Capital targets).")
    return s


# -----------------------------------------------------------------------------------------------------------------
def balance():
    s = Sheet("Space Balance (Revamp)", [12] + [24] * 10)
    s.title("Space Balance (Revamp): SWLOR.CombatSim space-mode results for the plan's verification targets")
    s.note("Generated by tools/SpaceRevampBalance from the Starships, Ship Modules and Space Enemies (Revamp) values. Player attributes 16 + 0.2 x rank, every module at the tier's rank, 150 seeded runs per cell, no perks or abilities.")
    s.blank()
    lines = CHECKS.read_text().splitlines()
    i = 0
    while i < len(lines):
        line = lines[i]
        if line.startswith("## "):
            s.title(line[3:])
        elif line.startswith("|"):
            header = [c.strip() for c in line.strip("|").split("|")]
            body = []
            i += 2
            while i < len(lines) and lines[i].startswith("|"):
                body.append([c.strip() for c in lines[i].strip("|").split("|")])
                i += 1
            s.table(header, body)
            continue
        elif line.strip():
            s.note(line.strip())
        i += 1
    return s


# -----------------------------------------------------------------------------------------------------------------
def write(sheets):
    tmp = WORKBOOK.with_suffix(".tmp.xlsx")
    zin = zipfile.ZipFile(WORKBOOK)
    wb = zin.read("xl/workbook.xml").decode()
    rels = zin.read("xl/_rels/workbook.xml.rels").decode()
    ct = zin.read("[Content_Types].xml").decode()
    targets = dict(re.findall(r'Id="(rId\d+)"[^>]*Target="([^"]+)"', rels))
    next_id = max(int(x) for x in re.findall(r'sheetId="(\d+)"', wb)) + 1
    next_rid = max(int(x) for x in re.findall(r'Id="rId(\d+)"', rels)) + 1
    next_file = max(int(re.search(r"sheet(\d+)\.xml", n)[1]) for n in zin.namelist() if re.match(r"xl/worksheets/sheet\d+\.xml$", n)) + 1
    payload = {}
    for sh in sheets:
        esc = html.escape(sh.name)
        mm = re.search(r'<sheet [^>]*name="%s"[^>]*r:id="(rId\d+)"' % re.escape(esc), wb)
        if mm:
            path = "xl/" + targets[mm[1]].lstrip("/").replace("xl/", "")
        else:
            path = f"xl/worksheets/sheet{next_file}.xml"
            wb = wb.replace("</sheets>", f'<sheet name="{esc}" sheetId="{next_id}" r:id="rId{next_rid}"/></sheets>')
            rels = rels.replace("</Relationships>", f'<Relationship Id="rId{next_rid}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{next_file}.xml"/></Relationships>')
            ct = ct.replace("</Types>", f'<Override PartName="/{path}" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>')
            next_id, next_rid, next_file = next_id + 1, next_rid + 1, next_file + 1
        payload[path] = sh.xml().encode("utf-8")
    payload["xl/workbook.xml"] = wb.encode("utf-8")
    payload["xl/_rels/workbook.xml.rels"] = rels.encode("utf-8")
    payload["[Content_Types].xml"] = ct.encode("utf-8")
    with zipfile.ZipFile(tmp, "w") as zout:
        for item in zin.infolist():
            if item.filename in payload:
                zout.writestr(item, payload.pop(item.filename), compress_type=zipfile.ZIP_DEFLATED)
            else:
                zout.writestr(item, zin.read(item.filename))
        for path, data in payload.items():
            zout.writestr(path, data, compress_type=zipfile.ZIP_DEFLATED)
    zin.close()
    shutil.move(tmp, WORKBOOK)
    lay = json.loads(LAYOUT.read_text(encoding="utf-8"))
    for sh in sheets:
        lay["columnsBySheet"][sh.name] = sh.layout()
    LAYOUT.write_text(json.dumps(lay, separators=(",", ":"), ensure_ascii=False), encoding="utf-8")


if __name__ == "__main__":
    tabs = [starships(), modules(), enemies(), balance()]
    write(tabs)
    print({t.name: t.n for t in tabs})
