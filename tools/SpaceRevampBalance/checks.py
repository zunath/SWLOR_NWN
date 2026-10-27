"""Runs the space revamp plan's section 19 combat checks on model.py and writes checks.md.

usage: python checks.py [runs]   (writes checks.md next to the simulator work files)
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import model as m  # noqa: E402

TIERS = [5, 15, 25, 35, 45]
RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 150
out = []


def table(head, rows):
    out.append("| " + " | ".join(head) + " |")
    out.append("|" + "---|" * len(head))
    for r in rows:
        out.append("| " + " | ".join(str(c) for c in r) + " |")
    out.append("")


def fight(players, npcs, passive=False):
    return m.run(m.scenario(players, npcs, passive), runs=RUNS)


def cell(r):
    return f"{r['mean']:.0f}s, {r['win']:.0%} win, {r['left']:.0%} left"


def best_rank(size, L):
    return max(L, m.GATE[size])


# A. Fight length
out.append("## A. Fight length: a Fighter (3 Laser Cannons) vs one same-level Normal of each role\n")
rows, worst = [], 0
for L in TIERS:
    res = {role: fight([m.player_ship("Fighter", L, m.FIT["Fighter"])], [m.npc_ship(role, L)]) for role in m.NORMAL_ROLES}
    means = [r["mean"] for r in res.values()]
    spread = max(means) / min(means)
    worst = max(worst, spread)
    rows.append([f"T{TIERS.index(L) + 1} (R{L})", m.GROUND_TTK[L]] + [f"{r['mean']:.0f}s ({r['left']:.0%})" for r in res.values()]
                + [f"{max(means) / m.GROUND_TTK[L]:.0%}", f"{spread:.2f}x"])
table(["Tier", "Ground s"] + m.NORMAL_ROLES + ["Slowest vs ground", "Slowest / fastest"], rows)
out.append(f"Mean time to kill, (hull + shield left). Targets: every role within the ground time, slowest / fastest <= 1.5x. Worst spread {worst:.2f}x.\n")

# B. Hull comparison
out.append("## B. Hulls at each tier (standard fit, all modules at the tier's rank)\n")
hulls = [("Fighter", "Fighter"), ("Interceptor", "Interceptor"), ("Bomber", "Bomber"), ("Gunship", "Gunship"),
         ("Frigate", "Frigate"), ("Cruiser", "Cruiser")]
rows = []
for L in TIERS:
    for role, fit in hulls:
        size = m.ROLES[role]["size"]
        if L < m.GATE[size]:
            continue
        for crew in ((0.0, 0.5) if m.ROLES[role].get("stH") else (1.0,)):
            p = m.player_ship(role, L, m.FIT[fit], crew=crew)
            a = fight([p], [m.npc_ship("Fighter", L)])
            g = fight([p], [m.npc_ship("Gunship", L)])
            pack = fight([p], [m.npc_ship("Fighter", L), m.npc_ship("Bomber", L, vs_size=size)])
            pack3 = fight([p], [m.npc_ship("Fighter", L, count=2), m.npc_ship("Bomber", L, vs_size=size)])
            e = fight([p], [m.npc_ship("Fighter", L, "Elite")])
            label = role if crew == 1.0 else f"{role} ({'solo' if crew == 0 else 'droid crew, 50%'})"
            rows.append([f"T{TIERS.index(L) + 1}", label, round(p["maxHull"] + p["maxShield"]), cell(a), cell(g), cell(pack), cell(pack3), cell(e)])
table(["Tier", "Hull", "Hull+shield", "vs Normal Fighter", "vs Normal Gunship", "vs Fighter + Bomber", "vs 2 Fighters + Bomber", "vs Elite Fighter (Medium)"], rows)

# C. Melee vs ranged
out.append("## C. Melee build on Missile Launchers vs ranged build on Laser Cannons (Fighter hull)\n")
rows = []
for L in TIERS:
    a = m.attr(L)
    ranged = {"agi": a, "per": a, "vit": a, "wil": a, "mgt": a - 4}
    melee = {"agi": a - 4, "per": a, "vit": a, "wil": a, "mgt": a}
    for target in ("Fighter", "Gunship"):
        rl = fight([m.player_ship("Fighter", L, m.FIT["Fighter"], pilot=ranged)], [m.npc_ship(target, L)])
        ml = fight([m.player_ship("Fighter", L, m.FIT["Fighter-missile"], pilot=melee)], [m.npc_ship(target, L)])
        rows.append([f"T{TIERS.index(L) + 1}", target, cell(rl), cell(ml), f"{ml['mean'] / rl['mean']:.2f}"])
table(["Tier", "Target", "Ranged (lasers)", "Melee (missiles)", "Melee / ranged time"], rows)
out.append("Ranged build: PER and AGI at the standard value, MGT 4 lower. Melee build: MGT and PER standard, AGI 4 lower (it loses evasion).\n")

# D. Advanced fighters
out.append("## D. Advanced Fighters (Piloting 10+)\n")
ADV = {
    "Aurek Strikefighter": dict(maker="Republic", extra=m.SIGNATURE["Aurek Strikefighter"], fit=m.FIT["Fighter"]),
    "Sith Fighter": dict(maker="Sith", extra=m.SIGNATURE["Sith Fighter"], fit=m.FIT["Fighter"]),
    "Cutlass": dict(maker="Corellian", extra={"M": 1}, fit=dict(m.FIT["Fighter"], **{"Shield Booster": 2})),
}
rows = []
for L in TIERS[1:]:
    scores = {}
    for name, a in ADV.items():
        p = m.player_ship("Fighter", L, a["fit"], maker=a["maker"], advanced=True, extra=a["extra"], name=name)
        pack = fight([p], [m.npc_ship("Fighter", L, count=2)])
        e = fight([p], [m.npc_ship("Fighter", L, "Elite")])
        # Job score: kill speed times survival, on the pair and on the elite.
        scores[name] = sum((1 / r["mean"]) * (0.5 + r["left"]) * r["win"] for r in (pack, e))
        rows.append([f"T{TIERS.index(L) + 1}", name, round(p["maxHull"] + p["maxShield"]), cell(pack), cell(e)])
    top = max(scores.values())
    for r in rows[-3:]:
        r.append(f"{scores[r[1]] / top:.0%}")
table(["Tier", "Ship", "Hull+shield", "vs 2 Normal Fighters", "vs Elite Fighter", "Job score vs best"], rows)
out.append("Job score: the sum over both fights of win rate x (0.5 + hull and shield left) / kill time. Target: every ship within 10% of the best.\n")

# E. Elites, bosses and capitals
out.append("## E. Elites, bosses and capitals vs ground\n")
rows = []
for L in TIERS:
    n = m.PRESETS[(L, "Normal", "Melee")]["hp"]
    for diff, role, size in (("Elite", "Fighter", None), ("Elite", "Gunship", None), ("Boss", "Fighter", "Large"), ("Boss", "Gunship", "Capital")):
        ground_ratio = m.PRESETS[(L, diff, "Melee")]["hp"] / n
        for hull in ("Fighter", "Bomber", "Frigate", "Cruiser"):
            if L < m.GATE[m.ROLES[hull]["size"]]:
                continue
            p = m.player_ship(hull, L, m.FIT[hull], crew=0.0)
            npc = m.npc_ship(role, L, diff, size=size)
            r = fight([p], [npc])
            base = fight([p], [m.npc_ship("Fighter", L)])
            rows.append([f"T{TIERS.index(L) + 1}", f"{diff} {role} ({npc['size']})", hull, cell(r),
                         f"{r['mean'] / base['mean']:.1f}x", f"{ground_ratio:.1f}x"])
table(["Tier", "Enemy", "Player hull", "Result", "Time vs a Normal Fighter", "Ground HP vs Normal"], rows)
out.append("Player hulls fly solo here (Large and Capital Helm slots only). Capitals are the Boss preset at Capital size with 1.5x HP and three weapons. Bosses and capitals are group content: see the boss check in H and the crew checks in G and I.\n")

# F. Size rules
out.append("## F. Size rules: damage per second against each target size (T5, R45)\n")
rows = []
for hull in ("Fighter", "Gunship", "Frigate", "Cruiser"):
    p = m.player_ship(hull, 45, m.FIT[hull], crew=1.0)
    cells = []
    for size in m.SIZES:
        fight([p], [m.npc_ship("Fighter", 45, size=size, hp_scale=20)], passive=True)
        res = json.loads((m.HERE / "res.json").read_text())
        res = res[0] if isinstance(res, list) else res
        cells.append(f"{res['MedianDamagePerSecond']:.1f}")
    rows.append([hull, round(p["maxHull"] + p["maxShield"])] + cells)
table(["Player hull", "Hull+shield"] + [f"vs {s}" for s in m.SIZES], rows)
out.append(f"Hit penalty {m.T['size_hit_penalty']} points per step a weapon is larger than its target; damage penalty {m.T['size_damage_penalty']:.0%} per step smaller (compounding). Torpedoes ignore both.\n")

# G. Crew
out.append("## G. Crewed capital vs separate ships (T5)\n")
cr = m.player_ship("Cruiser", 45, m.FIT["Cruiser"])
fi = m.player_ship("Fighter", 45, m.FIT["Fighter"])
r1 = fight([cr], [m.npc_ship("Gunship", 45, "Boss", size="Capital", hp_scale=1.0)])
r5 = fight([dict(fi, count=5)], [m.npc_ship("Gunship", 45, "Boss", size="Capital")])
table(["Side", "Hull+shield", "vs Boss capital"], [["1 Cruiser, 5 crew seats", round(cr["maxHull"] + cr["maxShield"]), cell(r1)],
                                                  ["5 Fighters", 5 * round(fi["maxHull"] + fi["maxShield"]), cell(r5)]])
# Crew Morale and Leadership raise crew effectiveness, capped at 105% (the crew update, W8a).
cr_cap = m.player_ship("Cruiser", 45, m.FIT["Cruiser"], crew=m.CREW_CAP)
r_cap = fight([cr_cap], [m.npc_ship("Gunship", 45, "Boss", size="Capital", hp_scale=1.0)])
out.append(f"Crew effectiveness is capped at {m.CREW_CAP:.0%} (Crew Morale and Leadership); at the cap the Cruiser takes "
           f"{cell(r_cap)} (hull+shield {round(cr_cap['maxHull'] + cr_cap['maxShield'])}). Crew seats add only the crew's own perks "
           "and seat-skill bonuses (W8), so a crewed capital must stay at or below N separate ships.\n")

# H. Weapon families
out.append("## H. Weapon families on a Fighter (3 of one family), mean kill time\n")
rows = []
for L in TIERS:
    cells = []
    for fam in ("Laser Cannon", "Ion Cannon", "Beam Cannon", "Missile Launcher"):
        fit = {fam: 3, **{k: v for k, v in m.FIT["Fighter"].items() if k != "Laser Cannon"}}
        p = m.player_ship("Fighter", L, fit)
        a = fight([p], [m.npc_ship("Fighter", L)])
        g = fight([p], [m.npc_ship("Gunship", L)])
        cells.append(f"{a['mean']:.0f}s / {g['mean']:.0f}s")
    fit = dict(m.FIT["Fighter"]); fit["Laser Cannon"] = 2; fit = {"Laser Cannon": 2, "Torpedo Launcher": 1, **{k: v for k, v in fit.items() if k != "Laser Cannon"}}
    p = m.player_ship("Fighter", L, fit)
    big = fight([p], [m.npc_ship("Fighter", L, "Boss")], passive=True)
    p2 = m.player_ship("Fighter", L, m.FIT["Fighter"])
    big2 = fight([p2], [m.npc_ship("Fighter", L, "Boss")], passive=True)
    rows.append([f"T{TIERS.index(L) + 1}"] + cells + [f"{big2['mean']:.0f}s / {big['mean']:.0f}s"])
table(["Tier", "Laser (vs Fighter / Gunship)", "Ion", "Beam", "Missile", "Boss (Large), 3 Lasers / 2 Lasers + Torpedo"], rows)
out.append("Beam pays 5 capacitor per cycle (lasers 3, missiles 2 plus ammo); beams are short range, missiles long range.\n")

# I. Crewed frigate vs separate fighters
out.append("## I. Crewed Frigate (3 players) vs 3 Fighters\n")
rows = []
for L in (25, 35, 45):
    fr = m.player_ship("Frigate", L, m.FIT["Frigate"], crew=1.0)
    fi = m.player_ship("Fighter", L, m.FIT["Fighter"])
    for enemy, label in ((m.npc_ship("Fighter", L, "Boss"), "Boss (Large)"), (m.npc_ship("Fighter", L, "Elite", count=3), "3 Elites")):
        a = fight([fr], [enemy]); b = fight([dict(fi, count=3)], [enemy])
        c = fight([m.player_ship("Frigate", L, m.FIT["Frigate"], crew=m.CREW_CAP)], [enemy])
        rows.append([f"T{TIERS.index(L) + 1}", label, cell(a), cell(b), cell(c)])
table(["Tier", "Enemy", "Crewed Frigate", "3 Fighters", f"Crewed Frigate ({m.CREW_CAP:.0%} cap)"], rows)

# J. Sidegrades: every ship in a combat role, fitted the same way
out.append("## J. Ships in the same role (T3 and T5, each at its own gate or above)\n")
rows = []
FITS = {"Fighter": "Fighter", "Interceptor": "Interceptor", "Bomber": "Bomber", "Gunship": "Gunship", "Frigate": "Frigate", "Cruiser": "Cruiser"}
for L in (25, 45):
    for role, fitname in FITS.items():
        ships = [sh for sh in m.SHIPS if sh[1] == role]
        scores = {}
        for name, _role, maker, adv, _model, _text, sig in ships:
            if L < m.GATE[m.ROLES[role]["size"]] + (10 if adv else 0):
                continue
            fit = dict(m.FIT[fitname])
            if sig.get("M"):
                fit["Shield Booster#3"] = sig["M"]
            if sig.get("H"):
                first = next(iter(fit))
                fit[first + "#3"] = sig["H"]
            crew = 1.0 if m.ROLES[role].get("stH") else 1.0
            p = m.player_ship(role, L, fit, maker=maker, advanced=adv, extra=sig, name=name, crew=crew)
            pack = fight([p], [m.npc_ship("Fighter", L, count=2)])
            e = fight([p], [m.npc_ship("Fighter", L, "Elite")])
            scores[name] = (sum((1 / r["mean"]) * (0.5 + r["left"]) * r["win"] for r in (pack, e)), adv, p, pack, e)
        if not scores:
            continue
        for tier_adv in (False, True):
            group = {k: v for k, v in scores.items() if v[1] == tier_adv}
            if not group:
                continue
            top = max(v[0] for v in group.values())
            for name, (sc, adv, p, pack, e) in group.items():
                rows.append([f"T{TIERS.index(L) + 1}", role, name, "Advanced" if adv else "Standard", round(p["maxHull"] + p["maxShield"]),
                             cell(pack), cell(e), f"{sc / top:.0%}"])
table(["Tier", "Role", "Ship", "Tier", "Hull+shield", "vs 2 Normal Fighters", "vs Elite Fighter", "Combat score vs best in group"], rows)
out.append("Large and Capital ships are fully crewed here. Non-combat signatures (speed, cargo, scan, range) do not show in this score.\n")

# K. Role behaviors
out.append("## K. Role behaviors: a Fighter (3 Laser Cannons) vs the roles with their behavior modeled\n")


def step(L):
    return min(45, L // 5 * 5)


def gunship(L, hardened):
    n = m.npc_ship("Gunship", L)
    h = 10 + step(L) // 5
    res = n["resistances"]["em"] - round(h * m.HARDENER_SHARE) + (h if hardened else 0)
    n["resistances"] = {k: res for k in n["resistances"]}
    return n


def repaired_fighter(L):
    # The Logistics ship's Hull and Shield Repairers (Medium x1.1) on its partner, always on: the upper bound.
    f = m.npc_ship("Fighter", L)
    rep = m.REPAIR_STEP[step(L)]
    f["repairers"] = [{"name": "Logistics Shield Repairer", "target": "Shield", "amountPerCycle": round(rep * 1.1),
                       "cycleSeconds": 4, "capacitorPerCycle": 0},
                      {"name": "Logistics Hull Repairer", "target": "Hull", "amountPerCycle": round(max(1, round(rep * 0.8)) * 1.1),
                       "cycleSeconds": 4, "capacitorPerCycle": 0}]
    return f


def debuffed(p, accuracy=0.0, defense=0.0):
    # The EW ship's jam (Flash: -8% of the Accuracy rating) or paint (Exposed: -10% Defense), always on: the upper bound.
    p = json.loads(json.dumps(p))
    rating = p["accuracy"] + p["weapons"][0]["accuracy"] + 3 * p["pilot"]["agi"]
    p["accuracy"] -= round(accuracy * rating)
    p["defense"] = round(p["defense"] * (1 - defense))
    return p


def minelayer(L):
    # Every mine hits: one Bomber torpedo per 20 s, shields bypassed, size ignored.
    n = m.npc_ship("Minelayer", L)
    torpedo = next(w for w in m.npc_ship("Bomber", L, vs_size="Large")["weapons"] if w["name"] == "NPC Torpedo")
    n["weapons"].append(dict(torpedo, name="NPC Mine", cycleSeconds=20.0))
    return n


rows = []
for L in TIERS:
    p = m.player_ship("Fighter", L, m.FIT["Fighter"])
    plain = fight([p], [gunship(L, False)])
    hard = fight([p], [gunship(L, True)])
    blend = plain["mean"] * (1 - m.HARDENER_SHARE) + hard["mean"] * m.HARDENER_SHARE
    pair = fight([p], [m.npc_ship("Fighter", L), m.npc_ship("Logistics", L)])
    healed = fight([p], [repaired_fighter(L), m.npc_ship("Logistics", L)])
    jam = fight([debuffed(p, accuracy=0.08)], [m.npc_ship("Fighter", L), m.npc_ship("Electronic Warfare", L)])
    paint = fight([debuffed(p, defense=0.10)], [m.npc_ship("Fighter", L), m.npc_ship("Electronic Warfare", L)])
    mines = fight([p], [minelayer(L)])
    mine_pair = fight([p], [m.npc_ship("Fighter", L), minelayer(L)])
    # The wing fighter comes first: pilots kill it before the carrier. It is out for the whole fight here.
    elite_wing = fight([p], [m.npc_ship("Fighter", L), m.npc_ship("Carrier", L, "Elite")])
    carrier = fight([dict(p, count=3)], [m.npc_ship("Carrier", L, "Boss")])
    wave = fight([dict(p, count=3)], [m.npc_ship("Fighter", L), m.npc_ship("Carrier", L, "Boss")])
    rows.append([f"T{TIERS.index(L) + 1} (R{L})", m.GROUND_TTK[L], f"{plain['mean']:.0f}s / {hard['mean']:.0f}s",
                 f"{blend:.0f}s ({blend / m.GROUND_TTK[L]:.0%})", cell(pair), cell(healed), cell(jam), cell(paint), cell(mines),
                 cell(mine_pair), cell(elite_wing), cell(carrier), cell(wave)])
table(["Tier", "Ground s", "Gunship unhardened / hardened", "Gunship, hardened below half hull", "Fighter + Logistics, no repair",
       "Fighter + Logistics, repairing", "Fighter + EW, jammed", "Fighter + EW, painted", "Minelayer, every mine hits",
       "Fighter + Minelayer, every mine hits", "1 Fighter vs Elite Carrier + 1 wing Fighter (rejected)",
       "3 Fighters vs Boss Carrier", "3 Fighters vs Boss Carrier + 1 wing Fighter"], rows)
out.append("Upper bounds: the Logistics repairs, the jam, the paint and every mine are modeled as always on (in play the EW "
           "ship keeps one of them up 12 s in 18 and a mine needs the ship to fly within 5 m). The Gunship's hardener runs over "
           "the last quarter of its hull + shield, so its fight is about 3/4 of the unhardened time plus 1/4 of the hardened one. "
           "One Fighter cannot beat an Elite carrier once a single wing fighter is out, so the Carrier is a Boss (group content, "
           "spawning rarer than capitals) and launches 1 Fighter every 40 s (4 waves, 2 alive at most); the wing fighter is out "
           "for the whole fight here, an upper bound.\n")

Path(m.HERE / "checks.md").write_text("\n".join(out))
print("\n".join(out))
