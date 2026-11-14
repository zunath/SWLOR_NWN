"""The domain order measures of the tree as it is, counted the way the guard counts them, written to guard.json.

SWLOR.Architecture.Tests counts the same references (Debt/SourceReferenceScanner.cs) with the same rules: each domain is
two nodes (logic, content), Entity and Enumeration are Foundation, references out of Composition and NativeSinks do not
count, and Native is Platform until P5-16b moves it. The totals here must equal the summaries of DomainCycles.txt and
UpwardEdges.txt in Baseline/Phase5/DebtReport.md; a difference means the tool and the guard have drifted apart.
"""
import json, os
import sim
sim.DETACH_AI = False
from simc import *

RANK_OF = {d: i for i, d in enumerate(ORDER)}
WIRING_UNITS = {"Platform/Composition", "Platform/NativeSinks"}

def node(unit):
    if unit is None: return None
    if unit in ("Platform/Entity", "Platform/Enumeration"): return ("Foundation", "L")
    if unit.startswith("Platform/"): return ("Platform", "L")
    if unit.startswith("Domain/"): return (unit[7:], "L") if unit[7:] in DOMAINS else None
    if is_content(unit):
        d = domain_of(unit); return (d, "C") if d else None
    return None

def name(n):
    return n[0] + (" content" if n[1] == "C" else "")

directions = set()
pair_units = collections.defaultdict(set)
up_members = collections.defaultdict(dict)
up_units = collections.defaultdict(set)
up_files = collections.defaultdict(set)
for r in load_refs():
    fk = r["fromNs"] + "." + r["fromTop"]; tk = r["toNs"] + "." + r["toTop"]
    if fk == tk: continue
    fu, tu = TOP_UNIT.get(fk), TOP_UNIT.get(tk)
    if fu is None or tu is None or fu == tu or fu in WIRING_UNITS: continue
    a, b = node(fu), node(tu)
    if not a or not b or a == b or a[1] != b[1]: continue
    directions.add((a, b)); pair_units[(a, b)].add((fu, tu))
    if RANK_OF[b[0]] > RANK_OF[a[0]]:
        c, _ = classify(r)
        member = r["fromNs"] + "." + r["member"]
        held = up_members[(a, b)]
        if member not in held or RANK[c] > RANK[held[member]]: held[member] = c
        up_units[(a, b)].add((fu, tu)); up_files[(r["file"], b)].add(member)

pairs = sorted({tuple(sorted((x, y))) for (x, y) in directions if (y, x) in directions})
by_direction = sorted(((name(a) + " -> " + name(b), len(m), sum(1 for c in m.values() if c != "V")) for (a, b), m in up_members.items()),
                      key=lambda row: (-row[1], row[0]))
result = {
    "pairs": len(pairs),
    "pairsLogic": sum(1 for p in pairs if p[0][1] == "L"),
    "pairsContent": sum(1 for p in pairs if p[0][1] == "C"),
    "pairList": [name(x) + " <-> " + name(y) + " | " + str(len(pair_units[(x, y)]) + len(pair_units[(y, x)])) for x, y in pairs],
    "upwardMembers": sum(row[1] for row in by_direction),
    "upwardHardMembers": sum(row[2] for row in by_direction),
    "upwardDirections": len(by_direction),
    "upwardHardDirections": sum(1 for row in by_direction if row[2] > 0),
    "upwardUnitEdges": sum(len(v) for v in up_units.values()),
    "upwardLines": len(up_files),
    "upwardByDirection": [f"{d} | {n} ({h} service, other or definition)" for d, n, h in by_direction],
}
json.dump(result, open(os.path.join(OUT, "guard.json"), "w"), indent=1)
for key in ("pairs", "pairsLogic", "pairsContent", "upwardMembers", "upwardHardMembers", "upwardDirections", "upwardHardDirections",
            "upwardUnitEdges", "upwardLines"):
    print(key, result[key])
