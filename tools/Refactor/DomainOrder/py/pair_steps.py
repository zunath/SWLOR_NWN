"""Two-way pairs at each step of section 4 ("Pairs at each step"), written to stages.json. Needs moves.json (order_cost.py)."""
import simc, json, re, sim
from moves import *
mv = json.load(open(OUT + "/moves.json"))
ORDER = mv["order"]; PLACE = {k: tuple(v) for k, v in mv["place"].items()}
RANK = {d: i for i, d in enumerate(ORDER)}; RANK["Native"] = 99
SHAPE = {k: type_shape_class(k) for k in TOP_UNIT}
OV_V = {k: unit_for(v) for k, v in PLACE.items() if SHAPE[k] == "V" and k not in EXPLICIT}
OV_VD = {k: unit_for(v) for k, v in PLACE.items()}

def node_a(u, entity_found=False):
    if u is None or u == "Platform/Composition": return None
    if entity_found and u in ("Platform/Entity", "Platform/Enumeration"): return "Foundation"
    return domain_of(u)

def pairs_a(override, skip_v=False, entity_found=False):
    dirs = set()
    for f in facts():
        if skip_v and f[3] == "V": continue
        fu = unit_key(f[0], override); tu = unit_key(f[2], override)
        if fu is None or tu is None or fu == tu or fu == "Platform/Composition": continue
        a, b = node_a(fu, entity_found), node_a(tu, entity_found)
        if a and b and a != b: dirs.add((a, b))
    return {tuple(sorted(k)) for k in dirs if (k[1], k[0]) in dirs}

def edges_a(override, skip_v=False):
    e = set()
    for f in facts():
        if skip_v and f[3] == "V": continue
        fu = unit_key(f[0], override); tu = unit_key(f[2], override)
        if fu is None or tu is None or fu == tu or fu == "Platform/Composition": continue
        a, b = domain_of(fu), domain_of(tu)
        if a and b and a != b: e.add((fu, tu))
    return e

def pairs_c(override, skip_v=False, native=True, sinks_wiring=True):
    dirs = set()
    simc.WIRING = {"Platform/Composition", "Platform/NativeSinks"} if sinks_wiring else {"Platform/Composition"}
    for f in facts():
        if skip_v and f[3] == "V": continue
        fu = unit_key(f[0], override); tu = unit_key(f[2], override)
        a, b = node_c(fu), node_c(tu)
        if not a or not b or a == b or a[1] != b[1] or a[0] == b[0]: continue
        if a[0] == "Native" or b[0] == "Native":
            if not native: continue
            a = ("Platform", a[1]) if a[0] == "Native" else a; b = ("Platform", b[1]) if b[0] == "Native" else b
            if a == b: continue
        dirs.add((a, b))
    simc.WIRING = {"Platform/Composition", "Platform/NativeSinks"}
    return {tuple(sorted(k)) for k in dirs if (k[1], k[0]) in dirs}

if __name__ == "__main__":
    res = {}
    simc.ENTITY_IN_FOUNDATION = True
    sim.DETACH_AI = False
    res["S0 P5-0 count (domain = logic + content, Entity in Platform)"] = len(pairs_a({}))
    res["edges now"] = len(edges_a({}))
    res["recount alone (band, Entity/Enumeration as Foundation, sinks wiring, no moves): DomainCycles.txt today"] = len(pairs_c({}, native=True))
    res["S1 + vocabulary types moved by the rule"] = len(pairs_a(OV_V))
    sim.DETACH_AI = True
    res["S2 + families moved (AbilityDetail, Log, CombatDamageType) and AI settings off AbilityDetail"] = len(pairs_a(OV_VD))
    res["S3 + constants and nested vocabulary extracted"] = len(pairs_a(OV_VD, skip_v=True))
    res["S4 + Entity and Enumeration count as Foundation"] = len(pairs_a(OV_VD, skip_v=True, entity_found=True))
    res["S5a content band only"] = len(pairs_c(OV_VD, skip_v=True, sinks_wiring=False))
    res["S5 content band, NativeSinks as wiring"] = len(pairs_c(OV_VD, skip_v=True))
    res["S6 + Native logic moved out of Platform"] = len(pairs_c(OV_VD, skip_v=True, native=False))
    res["model C pairs with no moves"] = len(pairs_c({}, native=True))
    res["edges after S3"] = len(edges_a(OV_VD, skip_v=True))
    for k, v in res.items(): print(k, v)
    json.dump(res, open(OUT + "/stages.json", "w"), indent=1)
    fin = sorted(pairs_c(OV_VD, skip_v=True, native=False))
    print("content-content pairs left:", [p for p in fin if p[0][1] == "C"])
