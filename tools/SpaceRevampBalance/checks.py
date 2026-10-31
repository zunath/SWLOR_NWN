"""Runs the space revamp plan's section 19 combat checks on model.py and writes checks.md.

usage: python checks.py [runs]   (writes checks.md next to the simulator work files)

Every pilot is a legal reference build (model.BUILDS, scaled to the tier's SP by model.legal_pilot). Each band names the
build it is judged on. The script exits 1 when any band is not met.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import model as m  # noqa: E402
import styles  # noqa: E402

TIERS = [5, 15, 25, 35, 45]
RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 150
out = []
bands = []  # (section, band, result text, met)


def band(section, text, result, met):
    """Records one band's result for the summary and the exit code; returns the 'met' / 'NOT MET' word."""
    bands.append((section, text, result, bool(met)))
    return "met" if met else "NOT MET"


def launcher_fit(fit):
    """The same fit with its Laser Cannons swapped for Missile Launchers (a launcher build's fit)."""
    return {k.replace("Laser Cannon", "Missile Launcher"): v for k, v in fit.items()}


def build_fit(build, fit):
    return launcher_fit(fit) if m.BUILDS[build]["family"] == "Launcher" else fit


def table(head, rows):
    out.append("| " + " | ".join(head) + " |")
    out.append("|" + "---|" * len(head))
    for r in rows:
        out.append("| " + " | ".join(str(c) for c in r) + " |")
    out.append("")
    # A partial copy, so a long run that stops still leaves the sections it finished.
    Path(m.HERE / "checks.partial.md").write_text("\n".join(out))


def fight(players, npcs, passive=False):
    return m.run(m.scenario(players, npcs, passive), runs=RUNS)


def cell(r):
    return f"{r['mean']:.0f}s, {r['win']:.0%} win, {r['left']:.0%} left"


def best_rank(size, L):
    return max(L, m.GATE[size])


out.append("## Pilots: legal reference builds\n")
table(["Build", "Damage family"] + [f"T{i + 1} (R{L}, {m.total_sp(L)} SP, {m.attribute_points(L)} points)" for i, L in enumerate(TIERS)],
      [[f"{b}: {v['label']}", v["family"]] + [" / ".join(f"{k.upper()} {m.build_attributes(b, L)[k]}" for k in m.ATTRIBUTES)
                                               for L in TIERS] for b, v in m.BUILDS.items()])
out.append("Every attribute starts at 10. A character holds 15 creation points (at most 6 on one attribute), 1 AP per 10 SP "
           "(40 at 400 SP) and 1 racial point; AP buy an attribute to 26, 27 with the racial point. Total SP is taken as "
           "8 x (rank + 5), so T5 (rank 45) has all 400 SP and 56 points above 10 across six attributes; SOC stays at 10. "
           "Lower tiers keep each build's shape. STM: 10 + MGT x 1.5, regen 10 + MGT / 4 per 30 s, no gear. Unless a section "
           "says otherwise its pilot is build A (lasers) or build B (missiles), the dedicated pilots.\n")

# A. Fight length
out.append("## A. Fight length: a matched Fighter vs one same-level Normal of each role\n")
rows, worst, slowest = [], 0, 0
for L in TIERS:
    for build, fit in (("A", "Fighter"), ("B", "Fighter-missile")):
        res = {role: fight([m.player_ship("Fighter", L, m.FIT[fit], build=build)], [m.npc_ship(role, L)]) for role in m.NORMAL_ROLES}
        means = [r["mean"] for r in res.values()]
        spread = max(means) / min(means)
        worst = max(worst, spread)
        slowest = max(slowest, max(means) / m.GROUND_TTK[L])
        rows.append([f"T{TIERS.index(L) + 1} (R{L})", f"{build} ({'lasers' if build == 'A' else 'missiles'})", m.GROUND_TTK[L]]
                    + [f"{r['mean']:.0f}s ({r['left']:.0%})" for r in res.values()]
                    + [f"{max(means) / m.GROUND_TTK[L]:.0%}", f"{spread:.2f}x"])
table(["Tier", "Build", "Ground s"] + m.NORMAL_ROLES + ["Slowest vs ground", "Slowest / fastest"], rows)
out.append(f"Mean time to kill, (hull + shield left). Judged on the worse of build A on 3 Laser Cannons and build B on 3 "
           f"Missile Launchers: the band's pilot is matched (a dedicated pilot of the weapon's family, as the ground run's is "
           f"of its weapon), so builds C and D are judged on the solo target in B instead. The ground baseline itself used "
           f"16 + 0.2 x rank on every attribute (not legal), but its fight-length attributes (damage and accuracy) sit within "
           f"2 of build A's, so its times stand. Targets: every role within the ground time (slowest "
           f"{slowest:.0%}: {band('A', 'every Normal role within the ground time (builds A and B)', f'{slowest:.0%}', slowest <= 1.0)}), "
           f"slowest / fastest <= 1.5x (worst {worst:.2f}x: {band('A', 'slowest / fastest role <= 1.5x (builds A and B)', f'{worst:.2f}x', worst <= 1.5)}).\n")

# B. Hull comparison
out.append("## B. Every hull at each tier, under every legal build with its weapon style (combat fit, modules at the tier's rank)\n")
# Each build's natural weapon style (plan section 5: ranged builds take Ace, melee builds Ordnance).
STYLE = {"A": "Ace", "B": "Ordnance", "C": "Ordnance", "D": "Ace"}
COMBAT_HULLS = ("Fighter", "Interceptor", "Bomber", "Gunship", "Frigate", "Cruiser")
rows, worst_normal, worst_elite, other_elite = [], None, None, {}
for L in TIERS:
    for role in m.ROLES:
        size = m.ROLES[role]["size"]
        if L < m.GATE[size]:
            continue
        for crew in ((0.0, 0.5) if m.ROLES[role].get("stH") else (1.0,)):
            for build in m.BUILDS:
                style = STYLE[build]
                p = styles.apply(style, m.player_ship(role, L, build_fit(build, m.FIT[role]), crew=crew, build=build), L)
                a = fight([p], [styles.debuff(style, m.npc_ship("Fighter", L), L)])
                g = fight([p], [styles.debuff(style, m.npc_ship("Gunship", L), L)])
                e = fight([p], [styles.debuff(style, m.npc_ship("Fighter", L, "Elite"), L)])
                pack = pack3 = None
                if build == "A":
                    pack = fight([p], [styles.debuff(style, n, L) for n in (m.npc_ship("Fighter", L), m.npc_ship("Bomber", L, vs_size=size))])
                    pack3 = fight([p], [styles.debuff(style, n, L) for n in (m.npc_ship("Fighter", L, count=2), m.npc_ship("Bomber", L, vs_size=size))])
                label = role if crew == 1.0 else f"{role} ({'solo' if crew == 0 else 'droid crew, 50%'})"
                where = f"T{TIERS.index(L) + 1} {label}, {build} + {style}"
                # The solo target: a solo pilot (Small and Medium hulls, or a Large or Capital hull flown alone).
                if crew in (0.0, 1.0):
                    normal = min(a["win"], g["win"])
                    if worst_normal is None or normal < worst_normal[0]:
                        worst_normal = (normal, where)
                    if role in COMBAT_HULLS and build in ("A", "B"):
                        if worst_elite is None or e["win"] < worst_elite[0]:
                            worst_elite = (e["win"], where)
                    else:
                        key = "non-combat hulls" if role not in COMBAT_HULLS else f"build {build}"
                        if key not in other_elite or e["win"] < other_elite[key][0]:
                            other_elite[key] = (e["win"], where)
                rows.append([f"T{TIERS.index(L) + 1}", label, f"{build} + {style}", round(p["maxHull"] + p["maxShield"]), cell(a), cell(g),
                             cell(pack) if pack else "-", cell(pack3) if pack3 else "-", cell(e)])
table(["Tier", "Hull", "Build + style", "Hull+shield", "vs Normal Fighter", "vs Normal Gunship", "vs Fighter + Bomber",
       "vs 2 Fighters + Bomber", "vs Elite Fighter (Medium)"], rows)
out.append("Fits (`model.FIT`): every High slot a weapon (Laser Cannons, Missile Launchers for the launcher builds B and C; "
           "a Bomber keeps its torpedo; capital hulls carry Turbolasers and Point-Defense, energy for every build), Mid "
           "Shield Booster and Targeting Computer then Shield Booster or Shield Repairer, Low Hull Plating and Thrusters "
           "then Capacitor Regulator or more plating. Non-combat hulls are fitted to fight here; a working fit (mining "
           "lasers, scanners, cloaks, cargo expanders) gives up those slots. Each build flies its weapon style at the "
           "tier's rank (section R: Ace for A and D, Ordnance for B and C; Ordnance's area, burn and fragmentation damage "
           "is not modeled, so its rows are a floor). Packs run on build A only. "
           f"Solo target (plan section 4: one weapon style solos all normal content and most elites in any hull the rank "
           f"allows), flown solo (Large and Capital Helm slots only). All normal content, judged on the worst of the four "
           f"builds on every hull: a same-level Normal Fighter and Normal Gunship at least 90% of the time (worst "
           f"{worst_normal[0]:.0%}, {worst_normal[1]}: "
           f"{band('B', 'solo: every hull and build with its style beats a Normal Fighter and Gunship >= 90%', f'{worst_normal[0]:.0%} ({worst_normal[1]})', worst_normal[0] >= 0.9)}). "
           f"Most elites, judged on the dedicated builds (A + Ace, B + Ordnance) in every combat hull: an Elite Fighter at "
           f"least half the time (worst {worst_elite[0]:.0%}, {worst_elite[1]}: "
           f"{band('B', 'solo: A + Ace and B + Ordnance beat an Elite Fighter >= 50% in every combat hull', f'{worst_elite[0]:.0%} ({worst_elite[1]})', worst_elite[0] >= 0.5)}). "
           "No band, reported: the worst Elite Fighter result " + "; ".join(f"for {k} {v[0]:.0%} ({v[1]})" for k, v in sorted(other_elite.items()))
           + ". Non-combat hulls have no kill-time band.\n")

# C. Melee vs ranged
out.append("## C. Melee builds on Missile Launchers vs the ranged build on Laser Cannons (Fighter hull)\n")
rows = []
for L in TIERS:
    for target in ("Fighter", "Gunship"):
        rl = fight([m.player_ship("Fighter", L, m.FIT["Fighter"], build="A")], [m.npc_ship(target, L)])
        ml = fight([m.player_ship("Fighter", L, m.FIT["Fighter-missile"], build="B")], [m.npc_ship(target, L)])
        gl = fight([m.player_ship("Fighter", L, m.FIT["Fighter-missile"], build="C")], [m.npc_ship(target, L)])
        rows.append([f"T{TIERS.index(L) + 1}", target, cell(rl), cell(ml), f"{ml['mean'] / rl['mean']:.2f}", cell(gl),
                     f"{gl['mean'] / rl['mean']:.2f}"])
table(["Tier", "Target", "Ranged, build A (lasers)", "Melee, build B (missiles)", "B / A time", "Ground fighter, build C (missiles)",
       "C / A time"], rows)
out.append("Build A: PER and AGI first. Build B: MGT and PER first, AGI 10 (it loses evasion). Build C: a ground melee "
           "fighter (MGT, AGI, VIT) who adds Piloting, PER 16 at T5 for launcher accuracy. No band (plan section 19 asks "
           "for the comparison).\n")

# D. Advanced fighters
out.append("## D. Advanced Fighters (Piloting 10+)\n")
ADV = {
    "Aurek Strikefighter": dict(maker="Republic", extra=m.SIGNATURE["Aurek Strikefighter"], fit=m.FIT["Fighter"]),
    "Sith Fighter": dict(maker="Sith", extra=m.SIGNATURE["Sith Fighter"], fit=m.FIT["Fighter"]),
    "Cutlass": dict(maker="Corellian", extra={"M": 1}, fit=dict(m.FIT["Fighter"], **{"Shield Booster": 2})),
}
rows, d_worst = [], 9.9
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
    d_worst = min(d_worst, min(scores.values()) / top)
table(["Tier", "Ship", "Hull+shield", "vs 2 Normal Fighters", "vs Elite Fighter", "Job score vs best"], rows)
out.append("Build A pilots every ship. Job score: the sum over both fights of win rate x (0.5 + hull and shield left) / kill "
           f"time. Target: every ship within 10% of the best (worst {d_worst:.0%}: "
           f"{band('D', 'Advanced Fighters within 10% of the best (build A)', f'{d_worst:.0%}', d_worst >= 0.9)}). The Sith "
           "Fighter's +13 Critical Rate is modeled as the signature reads; the server stores ShipStatus.CriticalBonus but "
           "does not read it yet.\n")

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
g_ratio = r_cap["mean"] / r5["mean"]
out.append(f"Crew effectiveness is capped at {m.CREW_CAP:.0%} (Crew Morale and Leadership); at the cap the Cruiser takes "
           f"{cell(r_cap)} (hull+shield {round(cr_cap['maxHull'] + cr_cap['maxShield'])}). Crew seats add only the crew's own perks "
           "and seat-skill bonuses (W8), so a crewed capital must stay at or below N separate ships (build A on every ship; "
           f"at the cap the Cruiser's kill time is {g_ratio:.2f} of the 5 Fighters', 0.98 allowed for noise: "
           f"{band('G', 'crewed Cruiser at the cap kills no faster than 5 Fighters (build A)', f'{g_ratio:.2f}', g_ratio >= 0.98)}).\n")

# H. Weapon families
out.append("## H. Weapon families on a Fighter (3 of one family), mean kill time\n")
rows = []
for L in TIERS:
    cells = []
    for fam in ("Laser Cannon", "Ion Cannon", "Beam Cannon", "Missile Launcher"):
        fit = {fam: 3, **{k: v for k, v in m.FIT["Fighter"].items() if k != "Laser Cannon"}}
        p = m.player_ship("Fighter", L, fit, build="B" if fam == "Missile Launcher" else "A")
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
out.append("Energy families fly build A, missiles build B (the launcher pilot); the torpedo fits fly build A. Beam pays 5 "
           "capacitor per cycle (lasers 3, missiles 2 plus ammo); beams are short range, missiles long range.\n")

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
FITS = {role: role for role in m.ROLES}
adv_worst = (9.9, "")
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
            if tier_adv and len(group) > 1:
                low = min(group, key=lambda k: group[k][0])
                ratio = group[low][0] / top if top else 0.0
                if ratio < adv_worst[0]:
                    adv_worst = (ratio, f"T{TIERS.index(L) + 1} {role}: {low}")
            for name, (sc, adv, p, pack, e) in group.items():
                rows.append([f"T{TIERS.index(L) + 1}", role, name, "Advanced" if adv else "Standard", round(p["maxHull"] + p["maxShield"]),
                             cell(pack), cell(e), f"{sc / top:.0%}" if top else "-"])
table(["Tier", "Role", "Ship", "Tier", "Hull+shield", "vs 2 Normal Fighters", "vs Elite Fighter", "Combat score vs best in group"], rows)
out.append("Build A pilots every ship (a same-pilot comparison). Large and Capital ships are fully crewed here. Non-combat "
           "signatures (speed, cargo, scan, range, yield) do not show in this score, so Standard ships carry no band. Advanced "
           f"ships in a role land within 10% of the best Advanced ship of that role (plan section 19; worst {adv_worst[0]:.0%}, "
           f"{adv_worst[1] or 'no role has two'}: "
           f"{band('J', 'Advanced ships in a role within 10% of the best (build A)', f'{adv_worst[0]:.0%} ({adv_worst[1]})', adv_worst[0] >= 0.9)}).\n")

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
band("L", "no power routing mode kills faster than Balanced, 0.5 s noise (build A)", f"{worst_time:.2f}", all(r[-1] == "yes" for r in rows))
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
pilot45 = m.legal_pilot("A", 45)
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
    if L <= 50:  # level 52 is tier 5.5, which has no ground time to compare (plan section 14, named modules)
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
           f"{band('M', 'named speed-up within the rank step and the cap (build A)', f'{worst_gain:.2f}x', worst_gain <= cap + 0.005)}), "
           f"and the top band keeps the fight-length bands (every role within the ground T5 time, slowest / fastest <= 1.5x): "
           f"worst spread {worst_spread:.2f}x ({band('M', 'top band slowest / fastest <= 1.5x (build A)', f'{worst_spread:.2f}x', worst_spread <= 1.5)}), "
           f"slowest against levels 45 and 50 {slowest_ratio:.0%} of the ground T5 time ({band('M', 'named fit within the ground T5 time against levels 45 and 50 (build A)', f'{slowest_ratio:.0%}', slowest_ratio <= 1.0)}; "
           f"level 52 is tier 5.5, with no ground time to compare). "
           f"Pilot: build A at rank 45 (400 SP). Level-50 Elite Fighter: named {cell(elite_named)}, rank 45 {cell(elite_r45)}.\n")

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
           f"(worst {worst_env:.1f}%, {band('N', 'no environment moves a fight more than about 10% (build A)', f'{worst_env:.1f}%', worst_env <= wildspace.FIGHT_BAND + 0.5)}). Speed, ability damage "
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
           f"{group_worst:.0%}. Target: a matched solo pilot (build A) wins every wave (at least 90%: "
           f"{band('O', 'a matched solo pilot wins every Rift Run wave >= 90% (build A)', f'{solo_worst:.0%}', solo_worst >= 0.9)}) "
           f"and a party at least as often ({band('O', 'a party wins every Rift Run wave >= 90% (build A)', f'{group_worst:.0%}', group_worst >= 0.9)}). The solo fight "
           f"time sits inside the {events.TIMER_SECONDS // 60}-minute timer with room for the waves' delays, shield regeneration "
           "between waves and the chamber pulls. Tier 5.5: a named-fit rank 45 Fighter against the level 52 Rift Raiders.\n")

# P. Player E-War modules (W11k)
out.append("## P. Player E-War modules (W11k): the Fighter's Targeting Computer swapped for a Jammer or a Target Painter\n")


def ewar_target(n, accuracy=0, paint=0):
    # The Jammer takes flat Accuracy from the target; the paint adds to its damage taken (applied after resistance, as
    # Space.RollWeaponShot does). Both modeled as always on: the upper bound.
    n = json.loads(json.dumps(n))
    n["accuracy"] += accuracy
    n["damageTakenPercent"] = n.get("damageTakenPercent", 0) + paint
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
           f"(slowest {ewar_slowest:.0%}: {band('P', 'E-War swap fits within the ground time (build A)', f'{ewar_slowest:.0%}', ewar_slowest <= 1.0)}); "
           f"solo, no swap beats the Targeting Computer by more than 10% (the Advanced-ship band; range {ewar_low:.0%}-{ewar_high:.0%}: "
           f"{band('P', 'no E-War swap beats the Targeting Computer by more than 10% solo (build A)', f'{ewar_high:.0%}', ewar_high <= 1.1)}), so a Mid slot's E-War module is a support pick, not an upgrade. In a "
           f"party the one module helps every ship: up to {party_high:.0%} of the all-Targeting-Computer party's job (no band; "
           "the stronger jam or paint counts, so a second one adds nothing). Budget: the jam matches the Targeting Computer's Accuracy step "
           "(-22 against +20 at rank 45) and the paint the Damage Amplifier's (+10%, every type). The Ion Web, Interdictor and "
           "Afterburner change speed and escape only; the simulator has no movement, so they are not fights.\n")

# Q. Seat-skill rank bonuses (W11n): Engineering and Espionage at rank 50
out.append("## Q. Seat-skill rank bonuses (W11n): Engineering and Espionage at rank 50\n")
SK = m.SEAT_SKILL
eng_support, eng_routing, esp_ewar = (m.seat_skill_percent(50, SK[k]) for k in ("support", "routing", "ewar"))


def engineered(ship, mode="Balanced", rank=50):
    """A player ship whose repairs come from an Engineering rank, routed with that rank's stronger gain."""
    q = m.routed(ship, mode, m.seat_skill_percent(rank, SK["routing"]))
    bonus = m.seat_skill_percent(rank, SK["support"])
    q["repairers"] = [dict(r, amountPerCycle=m.scale_percent(r["amountPerCycle"], bonus)) for r in ship["repairers"]]
    return q


# Q1. The Engineer at Engineering 50: routing gain +25% and repairs +10%, crewed ships against Balanced with no rank and
# against the same number of separate ships (section L's fights).
rows, q_fastest, q_score = [], 0.0, 0.0
for role, enemy, separate, separate_enemy, label, separate_label in cases:
    sep = fight(separate, [separate_enemy])
    for crew in (1.0, m.CREW_CAP):
        base = m.player_ship(role, 45, m.FIT[role], crew=crew)
        plain = fight([m.routed(base, "Balanced")], [enemy()])
        for mode in m.POWER_ROUTING:
            r = fight([engineered(base, mode)], [enemy()])
            q_fastest = max(q_fastest, plain["mean"] / r["mean"])
            q_score = max(q_score, job(r) / job(plain))
            rows.append([role, f"{crew:.0%}", label, mode, cell(r), f"{job(r) / job(plain):.0%}",
                         f"{separate_label}: {sep['mean']:.0f}s", "yes" if r["mean"] >= plain["mean"] - 0.5 else "NO"])
table(["Ship", "Crew", "Enemy", "Routing (Engineering 50)", "Result", "Job score vs Balanced, no rank", "Separate ships",
       "Kill no faster than Balanced, no rank"], rows)
w, s_, e_ = (m.routing_gain(mode, eng_routing) for mode in ("Weapons", "Shields", "Engines"))
out.append(f"Engineering 50 makes the routing's gain {eng_routing}% larger (Weapons: weapons use {-w['cap']}% less capacitor; "
           f"Shields: shield regen +{s_['regen']}%; Engines: speed +{e_['speed']}%), its penalties unchanged, and repairs "
           f"{eng_support}% larger. No crewed fight kills faster than Balanced with no rank, so section L's standing against "
           f"the separate ships holds (fastest Balanced / Engineering kill-time ratio {q_fastest:.2f}: {band('Q', 'Engineering 50 crewed fights kill no faster than Balanced (build A)', f'{q_fastest:.2f}', q_fastest <= 1.01)}); the best "
           f"job score is {q_score:.0%} of Balanced with no rank (repairs and regen add staying power, never kill speed).\n")

# Q2. A solo pilot with Engineering 50: the Gunship's Helm Shield Repairer repairs 10% more.
rows, solo_high, solo_slowest = [], 0.0, 0.0
for L in TIERS:
    g = m.player_ship("Gunship", best_rank("Medium", L), m.FIT["Gunship"])
    cells = []
    for role in m.NORMAL_ROLES:
        npc = m.npc_ship(role, L)
        a, b = fight([g], [npc]), fight([engineered(g)], [npc])
        solo_high = max(solo_high, job(b) / job(a))
        solo_slowest = max(solo_slowest, b["mean"] / m.GROUND_TTK[L])
        cells.append(f"{role}: {a['mean']:.0f}s / {b['mean']:.0f}s ({job(b) / job(a):.0%})")
    rows.append([f"T{TIERS.index(L) + 1} (R{L})", "; ".join(cells)])
table(["Tier", "Gunship vs each Normal role: no rank / Engineering 50 (job vs no rank)"], rows)
out.append(f"A solo Gunship's Helm Shield Repairer at Engineering 50 gives at most {solo_high:.0%} of the job of the same pilot with "
           f"no Engineering (the Advanced-ship band is 110%: {band('Q', 'solo Gunship at Engineering 50 <= 110% job (build A)', f'{solo_high:.0%}', solo_high <= 1.1)}).\n")

# Q3. Espionage 50: jams and paints 10% stronger (section P's fits).
rows, esp_low, esp_high, esp_party = [], 9.9, 0.0, 0.0
for L in TIERS:
    r = min(45, L // 5 * 5)
    jam, paint = -(4 + round(0.4 * r)), 5 + r // 9
    jam_e, paint_e = m.scale_percent(jam, esp_ewar), m.scale_percent(paint, esp_ewar)
    standard = m.player_ship("Fighter", L, m.FIT["Fighter"])
    swapped = m.player_ship("Fighter", L, no_tc)
    cells = []
    for role in m.NORMAL_ROLES:
        npc = m.npc_ship(role, L)
        base = fight([standard], [npc])
        with_jam = fight([swapped], [ewar_target(npc, accuracy=jam_e)])
        with_paint = fight([swapped], [ewar_target(npc, paint=paint_e)])
        for res in (with_jam, with_paint):
            ratio = ewar_job(res) / ewar_job(base)
            esp_low, esp_high = min(esp_low, ratio), max(esp_high, ratio)
        cells.append(f"{role}: jam {ewar_job(with_jam) / ewar_job(base):.0%} / paint {ewar_job(with_paint) / ewar_job(base):.0%}")
    boss = m.npc_ship("Fighter", L, "Boss")
    party = fight([dict(standard, count=3)], [boss])
    party_cells = []
    for lbl, kw in (("jam", dict(accuracy=jam_e)), ("paint", dict(paint=paint_e))):
        res = fight([dict(standard, count=2), swapped], [ewar_target(boss, **kw)])
        esp_party = max(esp_party, ewar_job(res) / ewar_job(party))
        party_cells.append(f"{lbl} {ewar_job(res) / ewar_job(party):.0%}")
    rows.append([f"T{TIERS.index(L) + 1} (R{L})", f"{jam_e} / +{paint_e}%", "; ".join(cells), " / ".join(party_cells)])
table(["Tier", "Jam / paint at Espionage 50", "Jammer fit / Target Painter fit (job vs Targeting Computer)",
       "3 Fighters vs a Boss, one Jammer / one Painter (job vs all TC)"], rows)
out.append(f"Espionage 50 makes a Jammer's Accuracy penalty and a Target Painter's damage taken {esp_ewar}% stronger. Solo, no swap "
           f"beats the Targeting Computer by more than 10% (range {esp_low:.0%}-{esp_high:.0%}: "
           f"{band('Q', 'Espionage 50 swap within 110% of the Targeting Computer solo (build A)', f'{esp_high:.0%}', esp_high <= 1.1)}); in a party one module gives up to {esp_party:.0%} of the all-Targeting-"
           "Computer party's job.\n")

# Q4. A fully crewed Cruiser at the cap with a Target Painter at the Sensors seat (in place of its Shield Repairer) against
# 5 Fighters, one of them swapping its Targeting Computer for a Target Painter, both routed Balanced: first with no seat
# skills, then with Engineering 50 at the Engineer seat and Espionage 50 at the Sensors seat (and on the painting Fighter).
paint45 = 5 + 45 // 9
cr_fit = {k: v for k, v in m.FIT["Cruiser"].items() if k != "Shield Repairer"}
cr_cap_ship = m.player_ship("Cruiser", 45, cr_fit, crew=m.CREW_CAP)
fi45 = m.player_ship("Fighter", 45, m.FIT["Fighter"])
fi45_p = m.player_ship("Fighter", 45, no_tc)
q4_rows, q4_ratio = [], {}
for label, rank in (("no seat skills", 0), ("Engineering 50 and Espionage 50", 50)):
    paint = m.scale_percent(paint45, m.seat_skill_percent(rank, SK["ewar"]))
    cr_q = engineered(cr_cap_ship, "Balanced", rank)
    crewed = fight([cr_q], [ewar_target(m.npc_ship("Gunship", 45, "Boss", size="Capital", hp_scale=1.0),
                                        paint=m.round_half_away(paint * m.CREW_CAP))])
    separate5 = fight([dict(fi45, count=4), fi45_p], [ewar_target(m.npc_ship("Gunship", 45, "Boss", size="Capital"), paint=paint)])
    q4_ratio[rank] = crewed["mean"] / separate5["mean"]
    q4_rows.append([label, cell(crewed), cell(separate5), f"{q4_ratio[rank]:.2f}"])
table(["Seat skills", "Cruiser at the 105% cap with a Target Painter", "5 Fighters, one with a Target Painter",
       "Cruiser / Fighters kill time"], q4_rows)
q4_met = q4_ratio[50] >= q4_ratio[0] - 0.02
out.append(f"Rank 50 seat skills move the crewed Cruiser's kill time against the 5 separate Fighters from {q4_ratio[0]:.2f} to "
           f"{q4_ratio[50]:.2f} of theirs ({band('Q', 'rank 50 seat skills add no crewed Cruiser gain beyond noise (build A)', f'{q4_ratio[0]:.2f} -> {q4_ratio[50]:.2f}', q4_met)}): both sides bring the same painter, "
           "Engineering adds staying power only, and the 105% cap stays level with N separate ships as in section G. The seat "
           "skills sit outside the crew cap (they are the crew member's own, as perks are) and add no weapon damage.\n")

# R. Piloting styles (plan section 6, Appendix A): each style's main damage abilities on a matched Fighter

out.append("## R. Piloting styles: the main damage abilities, with their real STM and cooldowns, on a matched Fighter\n")
rows = []
for L in TIERS:
    t = TIERS.index(L) + 1
    for style, build, fit in (("Ace", "A", "Fighter"), ("Ordnance", "B", "Fighter-missile"), ("Bulwark", "A", "Fighter"),
                              ("Command", "A", "Fighter")):
        plain = m.player_ship("Fighter", L, m.FIT[fit], build=build)
        styled = styles.apply(style, plain, L)
        cells = []
        for role in m.NORMAL_ROLES:
            npc = m.npc_ship(role, L)
            a, b = fight([plain], [npc]), fight([styled], [styles.debuff(style, npc, L)])
            cells.append(f"{role}: {a['mean']:.0f}s -> {b['mean']:.0f}s")
        ea = fight([plain], [m.npc_ship("Fighter", L, "Elite")])
        eb = fight([styled], [styles.debuff(style, m.npc_ship("Fighter", L, "Elite"), L)])
        rows.append([f"T{t} (R{L})", style, build, styles.describe(style, L), "; ".join(cells), f"{cell(ea)} -> {cell(eb)}"])
table(["Tier", "Style", "Build", "Modeled at this rank", "Normal roles: no style -> style (mean kill time)",
       "Elite Fighter: no style -> style"], rows)
out.append("Each style at the tier's Piloting rank, with every row it has by then (Appendix A unlock ranks); abilities fire "
           "whenever they are off cooldown and the pilot's STM pays (build STM, no gear). Modeled: Ace's Disabling Shot "
           "(its Attack penalty on the target as always on: 30 s on an 18 s cooldown), Double Shot (two shots) and Sustained "
           "Fire; Ordnance's Volley and Flare Missile as single-target shots; Bulwark's Bulwark trait (+Defense %); "
           "Command's Focus Fire (+damage dealt, 30 s on a 24 s cooldown, so always on). Not modeled, so style output there "
           "is unvalidated: area hits on other ships, Burn and fragmentation damage over time, Blind, Dazed, Exposed, "
           "Proximity Warheads, crit STM refunds, High Noon, the stances, Evasive Reload, Mobile Footwork, Defiance and "
           "Shield Ram riders, Critical Ward, Unbreakable, Invincible, goads and threat, Emergency Repair, Mark Target, "
           "Break Formation, Formation and the capstones. No band: the plan sets none for style output beyond the solo "
           "target (section B is its no-style floor).\n")

Path(m.HERE / "checks.md").write_text("\n".join(out))
print("\n".join(out))

failed = [b for b in bands if not b[3]]
print(f"\n## Summary: {len(bands) - len(failed)} of {len(bands)} bands met\n")
for section, text, result, met in bands:
    print(f"- {section}: {text}: {result} ({'met' if met else 'NOT MET'})")
summary = ["", f"## Summary: {len(bands) - len(failed)} of {len(bands)} bands met", ""] + \
          [f"- {section}: {text}: {result} ({'met' if met else 'NOT MET'})" for section, text, result, met in bands]
Path(m.HERE / "checks.md").write_text("\n".join(out + summary))
sys.exit(1 if failed else 0)
