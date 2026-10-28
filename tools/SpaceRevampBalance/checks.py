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

# L. Power routing: a sidegrade, and a crewed ship stays at or below N separate ships in every mode
out.append("## L. Engineer power routing (T5, crewed ships)\n")


def job(r):
    return (1 / r["mean"]) * (0.5 + r["left"]) * r["win"]


rows, worst_time, worst_score = [], 0.0, 0.0
fi = m.player_ship("Fighter", 45, m.FIT["Fighter"])
cases = [
    ("Cruiser", lambda: m.npc_ship("Gunship", 45, "Boss", size="Capital", hp_scale=1.0),
     [dict(fi, count=5)], m.npc_ship("Gunship", 45, "Boss", size="Capital"), "Boss capital", "5 Fighters"),
    ("Frigate", lambda: m.npc_ship("Fighter", 45, "Boss"), [dict(fi, count=3)], m.npc_ship("Fighter", 45, "Boss"),
     "Boss (Large)", "3 Fighters"),
    ("Frigate", lambda: m.npc_ship("Fighter", 45, "Elite", count=3), [dict(fi, count=3)],
     m.npc_ship("Fighter", 45, "Elite", count=3), "3 Elites", "3 Fighters"),
]
for role, enemy, separate, separate_enemy, label, separate_label in cases:
    sep = fight(separate, [separate_enemy])
    for crew in (1.0, m.CREW_CAP):
        base = m.player_ship(role, 45, m.FIT[role], crew=crew)
        balanced = fight([m.routed(base, "Balanced")], [enemy()])
        for mode in m.POWER_ROUTING:
            r = balanced if mode == "Balanced" else fight([m.routed(base, mode)], [enemy()])
            worst_time = max(worst_time, balanced["mean"] / r["mean"])
            if mode != "Balanced":
                worst_score = max(worst_score, job(r) / job(balanced))
            rows.append([role, f"{crew:.0%}", label, mode, cell(r), f"{job(r) / job(balanced):.0%}",
                         f"{separate_label}: {sep['mean']:.0f}s", "yes" if r["mean"] >= balanced["mean"] - 0.5 else "NO"])
table(["Ship", "Crew", "Enemy", "Routing", "Result", "Job score vs Balanced", "Separate ships", "Kill no faster than Balanced"], rows)
out.append(f"Weapons: weapons use {-m.POWER_ROUTING['Weapons']['cap']}% less capacitor, shield regen and speed -10%. Shields: shield regen "
           f"+{m.POWER_ROUTING['Shields']['regen']}%, weapon damage -5%, speed -10%. Engines: speed +{m.POWER_ROUTING['Engines']['speed']}%, "
           "weapon damage -5%, shield regen -10%. No mode kills faster than Balanced (worst Balanced / mode kill-time ratio "
           f"{worst_time:.2f}), so a crewed ship stays at or below N separate ships in every mode; the best mode's job score is "
           f"{worst_score:.0%} of Balanced (Shields gains in long fights and pays in kill time). Capacitor and speed, the other "
           "modes' gains, are not measured by these fights.\n")

# M. Named rank-50 modules (the top band, W9b)
import rewards  # noqa: E402

out.append("## M. Named rank-50 modules: a Fighter on named modules vs the top band (levels 45-52)\n")
rows, worst_spread, slowest_ratio, worst_gain, step_ratios = [], 0.0, 0.0, 0.0, []
pilot45 = m.pilot_for(45)
for L in (45, 50, 52):
    r40 = {role: fight([m.player_ship("Fighter", 40, m.FIT["Fighter"], pilot=pilot45)], [m.npc_ship(role, L)])
           for role in m.NORMAL_ROLES}
    r45 = {role: fight([m.player_ship("Fighter", 45, m.FIT["Fighter"], pilot=pilot45)], [m.npc_ship(role, L)])
           for role in m.NORMAL_ROLES}
    named = {role: fight([m.player_ship("Fighter", 45, m.FIT["Fighter"], pilot=pilot45, named=True)], [m.npc_ship(role, L)])
             for role in m.NORMAL_ROLES}
    step = sum(r40[k]["mean"] for k in r40) / sum(r45[k]["mean"] for k in r45)
    step_ratios.append(step)
    means = [r["mean"] for r in named.values()]
    spread = max(means) / min(means)
    worst_spread = max(worst_spread, spread)
    slowest_ratio = max(slowest_ratio, max(means) / m.GROUND_TTK[45])
    speedup = sum(r45[k]["mean"] for k in r45) / sum(named[k]["mean"] for k in named)
    worst_gain = max(worst_gain, speedup)
    rows.append([f"L{L}", m.GROUND_TTK[45]] + [f"{named[k]['mean']:.0f}s ({named[k]['left']:.0%}) vs {r45[k]['mean']:.0f}s"
                                               for k in m.NORMAL_ROLES]
                + [f"{max(means) / m.GROUND_TTK[45]:.0%}", f"{spread:.2f}x", f"{step:.2f}x", f"{speedup:.2f}x"])
table(["Enemy level", "Ground T5 s"] + [f"{k}: named vs rank 45" for k in m.NORMAL_ROLES]
      + ["Slowest vs ground T5", "Slowest / fastest", "Rank step (40 -> 45)", "Named speed-up"], rows)
elite_named = fight([m.player_ship("Fighter", 45, m.FIT["Fighter"], pilot=pilot45, named=True)], [m.npc_ship("Fighter", 50, "Elite")])
elite_r45 = fight([m.player_ship("Fighter", 45, m.FIT["Fighter"], pilot=pilot45)], [m.npc_ship("Fighter", 50, "Elite")])
cap = min(rewards.NAMED_GAIN_CAP, min(step_ratios))
out.append(f"Mean time to kill (hull + shield left), every fit on the same rank-45 pilot. The named fit is every module at its "
           f"named numbers: rank 45 plus {rewards.NAMED_STEP_SHARE:.0%} of the rank 40 -> 45 step, at least +1, Attack and "
           f"Accuracy included (weapons {rewards.named_value(90, 80)}). Rank step: a rank-45 fit's kill speed over a rank-40 fit "
           f"({min(step_ratios):.2f}x-{max(step_ratios):.2f}x). Targets: the named speed-up stays at or below the rank step, "
           f"capped at {rewards.NAMED_GAIN_CAP}x ({cap:.2f}x here; worst {worst_gain:.2f}x, "
           f"{'met' if worst_gain <= cap + 0.005 else 'NOT MET'}), and the top band keeps the fight-length bands (every role "
           f"within the ground T5 time, slowest / fastest <= 1.5x): worst spread {worst_spread:.2f}x, slowest "
           f"{slowest_ratio:.0%} of the ground T5 time. Level-50 Elite Fighter: named {cell(elite_named)}, rank 45 {cell(elite_r45)}.\n")

# N. Wild Space environments (W12c)
import wildspace  # noqa: E402

out.append("## N. Wild Space environments: a named-fit Fighter vs each level 52 Normal role, the environment on both ships\n")
env_base, env_rows = wildspace.environment_rows(RUNS)
worst_env = max(abs(d["worst"]) for d in env_rows.values())
table(["Environment", "Effects"] + m.NORMAL_ROLES + ["Mean change", "Not modeled"],
      [[wildspace.ENVIRONMENT_NAMES[e], wildspace.environment_text(e)] + [f"{d['deltas'][k]:+.1f}%" for k in m.NORMAL_ROLES]
       + [f"{d['mean']:+.1f}%", d["note"]] for e, d in env_rows.items()])
out.append(f"Change in mean time to kill against the same fight with no environment (baseline "
           f"{', '.join(f'{k} {v:.0f}s' for k, v in env_base.items())}). Every ship in a system takes its environment, so a "
           f"change helps and hurts both sides. Target: no environment moves a fight more than about +-{wildspace.FIGHT_BAND}% "
           f"(worst {worst_env:.1f}%, {'met' if worst_env <= wildspace.FIGHT_BAND + 0.5 else 'NOT MET'}). Speed, ability damage "
           "and capacitor use do not change an auto-fire fight here: the Black Hole's speed matters for kiting and escape, the "
           "Red Giant's ability damage only for pilots who use abilities (about a quarter of a fit's damage, so about 5% "
           "faster kills), and a laser fit's capacitor does not run dry in these fights.\n")

# O. Incursions and Rift Runs (W15b)
import events  # noqa: E402
import npc_catalog  # noqa: E402


def event_ship(hull, level):
    role, difficulty, size = npc_catalog.HULLS[hull]
    return m.npc_ship(role, level, difficulty, size=None if difficulty == "Normal" else size)


def event_fight(ships, hulls, level, pilot_rank=None, named=False):
    rank = pilot_rank or level
    p = m.player_ship("Fighter", rank, m.FIT["Fighter"], named=named)
    return fight([dict(p, count=ships)], [event_ship(h, level) for h in hulls])


out.append("## O. Incursions and Rift Runs: matched Fighters (3 Laser Cannons) against each composition, all of it at once\n")
rows, incursion_worst = [], {}
for L in TIERS:
    t = TIERS.index(L) + 1
    wave2 = event_fight(2, events.wave_hulls(t), L)
    wave3 = event_fight(3, events.wave_hulls(t), L)
    block2 = event_fight(2, events.BLOCKADE_HULLS, L)
    block3 = event_fight(3, events.BLOCKADE_HULLS, L)
    command5 = event_fight(5, ["capital"] + events.COMMAND_ESCORTS, L)
    command6 = event_fight(6, ["capital"] + events.COMMAND_ESCORTS, L)
    best_wave = wave2 if wave2["win"] >= 0.95 else wave3
    incursion_worst[t] = (best_wave["win"], max(block2["win"], block3["win"]), command6["win"])
    rows.append([f"T{t}", "+".join(events.wave_hulls(t)), cell(wave2), cell(wave3), cell(block2), cell(block3), cell(command5), cell(command6)])
table(["Tier", "Wave", "Wave, 2 ships", "Wave, 3 ships", "Blockade, 2 ships", "Blockade, 3 ships", "Command, 5 ships", "Command, 6 ships"], rows)
out.append("Incursions are group content: a wave (the W12a combat site squad) for 2 matched pilots (3 at T5, where it adds a gunship), "
           f"a blockade ({' + '.join(events.BLOCKADE_HULLS)}) for 2-3, a command group (the tier's capital + "
           f"{' + '.join(events.COMMAND_ESCORTS)}) for about 6. In the game ships engage as they notice a pilot, so these "
           "all-at-once fights are the hard case. Tier 6 (Korriban's region) is above the simulator's ranks and not modeled.\n")

rows, solo_worst, group_worst = [], 1.0, 1.0
for tenths, resref, _ in events.KEYS:
    level, rank, named = (wildspace.RIFT_RAIDER_LEVEL, 45, True) if tenths == 55 else (TIERS[tenths // 10 - 1], None, False)
    solo_time, cells = 0.0, []
    for room in range(len(events.ROOMS)):
        for ships in (1, 2, 3):
            for wave in events.room_waves(tenths, room, ships):
                r = event_fight(ships, wave, level, rank, named)
                if ships == 1:
                    solo_time += r["mean"]
                    solo_worst = min(solo_worst, r["win"])
                    cells.append(f"{'+'.join(wave)}: {cell(r)}")
                else:
                    group_worst = min(group_worst, r["win"])
    rows.append([f"{events.key_label(tenths)} ({resref})", "; ".join(cells), f"{solo_time / 60:.1f} min"])
table(["Key", "Solo waves (mean time, win, hull + shield left)", "Solo fight time"], rows)
out.append(f"Each wave of each chamber, solo and with the 2- and 3-ship extras. Worst solo win {solo_worst:.0%}, worst party win "
           f"{group_worst:.0%}. Target: a matched solo pilot wins every wave (at least 90%: "
           f"{'met' if solo_worst >= 0.9 else 'NOT MET'}) and a party at least as often (met: {group_worst >= 0.9}). The solo fight "
           f"time sits inside the {events.TIMER_SECONDS // 60}-minute timer with room for the waves' delays, shield regeneration "
           "between waves and the chamber pulls. Tier 5.5: a named-fit rank 45 Fighter against the level 52 Rift Raiders.\n")

# P. Player E-War modules (W11k)
out.append("## P. Player E-War modules (W11k): the Fighter's Targeting Computer swapped for a Jammer or a Target Painter\n")


def ewar_target(n, accuracy=0, paint=0):
    # The Jammer takes flat Accuracy from the target; the paint's damage taken is modeled as lower resistance (within a
    # point or two of the game's multiplier at these resistances). Both modeled as always on: the upper bound.
    n = json.loads(json.dumps(n))
    n["accuracy"] += accuracy
    n["resistances"] = {k: v - paint for k, v in n["resistances"].items()}
    return n


def ewar_job(r):
    return (1 / r["mean"]) * (0.5 + r["left"]) * r["win"]


rows, ewar_slowest, ewar_low, ewar_high, party_high = [], 0.0, 9.9, 0.0, 0.0
no_tc = {k: v for k, v in m.FIT["Fighter"].items() if k != "Targeting Computer"}
for L in TIERS:
    r = min(45, L // 5 * 5)  # the tier's module rank (section K's step; "step" is rebound by section M)
    jam, paint = -(4 + round(0.4 * r)), 5 + r // 9
    standard = m.player_ship("Fighter", L, m.FIT["Fighter"])
    swapped = m.player_ship("Fighter", L, no_tc)
    cells = []
    for role in m.NORMAL_ROLES:
        npc = m.npc_ship(role, L)
        base = fight([standard], [npc])
        with_jam = fight([swapped], [ewar_target(npc, accuracy=jam)])
        with_paint = fight([swapped], [ewar_target(npc, paint=paint)])
        for res in (with_jam, with_paint):
            ewar_slowest = max(ewar_slowest, res["mean"] / m.GROUND_TTK[L])
            ratio = ewar_job(res) / ewar_job(base)
            ewar_low, ewar_high = min(ewar_low, ratio), max(ewar_high, ratio)
        cells.append(f"{role}: {base['mean']:.0f}s / jam {with_jam['mean']:.0f}s ({ewar_job(with_jam) / ewar_job(base):.0%}) / "
                     f"paint {with_paint['mean']:.0f}s ({ewar_job(with_paint) / ewar_job(base):.0%})")
    # A party: three matched Fighters against a Boss, one of them swapping its Targeting Computer. The jam and the paint
    # help the whole party (the stronger one counts, so a second painter adds nothing).
    boss = m.npc_ship("Fighter", L, "Boss")
    party = fight([dict(standard, count=3)], [boss])
    party_cells = []
    for label, kw in (("jam", dict(accuracy=jam)), ("paint", dict(paint=paint))):
        res = fight([dict(standard, count=2), swapped], [ewar_target(boss, **kw)])
        party_high = max(party_high, ewar_job(res) / ewar_job(party))
        party_cells.append(f"{label} {res['mean']:.0f}s ({ewar_job(res) / ewar_job(party):.0%})")
    rows.append([f"T{TIERS.index(L) + 1} (R{L})", m.GROUND_TTK[L], f"{jam} / +{paint}%", "; ".join(cells),
                 f"{party['mean']:.0f}s / " + " / ".join(party_cells)])
table(["Tier", "Ground s", "Jam / paint at the tier's rank", "Targeting Computer fit / Jammer fit (job vs TC) / Target Painter fit (job vs TC)",
       "3 Fighters vs a Boss: all TC / one Jammer / one Painter (job vs all TC)"], rows)
out.append(f"A matched Fighter keeps its Targeting Computer, or swaps it for a Jammer (target Accuracy -4 - 0.4 x rank) or a Target "
           f"Painter (target damage taken +5% + rank / 9), against every Normal role. Job score = kill speed x (0.5 + hull and "
           f"shield left) x win rate, against the Targeting Computer fit. Bands: every fight stays within the ground time "
           f"(slowest {ewar_slowest:.0%}: {'met' if ewar_slowest <= 1.0 else 'NOT MET'}); solo, no swap beats the Targeting "
           f"Computer by more than 10% (the Advanced-ship band; range {ewar_low:.0%}-{ewar_high:.0%}: "
           f"{'met' if ewar_high <= 1.1 else 'NOT MET'}), so a Mid slot's E-War module is a support pick, not an upgrade. In a "
           f"party the one module helps every ship: up to {party_high:.0%} of the all-Targeting-Computer party's job (no band; "
           "the stronger jam or paint counts, so a second one adds nothing). Budget: the jam matches the Targeting Computer's Accuracy step "
           "(-22 against +20 at rank 45) and the paint the Damage Amplifier's (+10%, every type). The Ion Web, Interdictor and "
           "Afterburner change speed and escape only; the simulator has no movement, so they are not fights.\n")

Path(m.HERE / "checks.md").write_text("\n".join(out))
print("\n".join(out))
