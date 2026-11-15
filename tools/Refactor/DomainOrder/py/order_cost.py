"""The type moves for the recorded order and the order-cost search (section 4, "Order"), written to moves.json.

Prints the approved order's cost (hard member references pointing up after the moves, Native left out) and the order a
dynamic program over every order of the game domains finds cheapest (Platform and Foundation fixed at the bottom).
"""
import sim, json
sim.DETACH_AI = True
from moves import *
APPROVED = ORDER
place, conflicts = placement(APPROVED)
ov = {k: unit_for(v) for k, v in place.items()}
edges, units = measure_c(ov)
r = {d: i for i, d in enumerate(APPROVED)}
w = collections.Counter()
for (a, b), d in edges.items():
    if a[1] != b[1] or a[0] == b[0] or "Native" in (a[0], b[0]): continue
    w[(a[0], b[0])] += sum(1 for c in d.values() if c in "DSO")
approved_cost = sum(n for (a, b), n in w.items() if r[b] > r[a])
dp_order, dp_cost = order_dp(edges, "DSO")
print("moved", len(place), "conflicts", len(conflicts))
print("approved order cost", approved_cost)
print("DP minimum", dp_cost, " < ".join(dp_order))
# same-file check: every type sharing a file with a moved type moves to the same place
f2t = collections.defaultdict(set)
for t, ti in TYPES.items():
    if t == ti["ns"] + "." + ti["top"]:
        for f in ti["files"].split(";"): f2t[f].add(t)
split_files = []
for f, ts in f2t.items():
    targets = {place.get(t, ("stay",))[0] for t in ts}
    if len(targets) > 1 and any(t in place for t in ts): split_files.append((f, sorted((t.split(".")[-1], place.get(t, ("stay",))[0]) for t in ts)))
print("files split between targets:", len(split_files))
for x in split_files: print("   ", x)
json.dump({"order": APPROVED, "approvedCost": approved_cost, "dpOrder": dp_order, "dpCost": dp_cost,
           "place": {k: list(v) for k, v in place.items()}, "conflicts": [list(map(str, c)) for c in conflicts]},
          open(OUT + "/moves.json", "w"), indent=1)
for c in conflicts: print("conflict", c)
