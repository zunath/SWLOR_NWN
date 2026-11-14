"""Appendix C of MaintainabilityRefactorDomainOrder.md from upedges.json (upward.py), written to appendixC.md."""
import sim
sim.DETACH_AI = True
import json, collections, re
from pair_steps import *
up = json.load(open(OUT + "/upedges.json"))
def unit(t):
    k = ROOT + "." + t
    k = k if k in TOP_UNIT else k.rsplit(".", 1)[0]
    return unit_key(k, OV_VD)
by_dir = collections.defaultdict(list)
for o in up: by_dir[(o["kind"], o["from"], o["to"])].append(o)
def band(x):
    d, b = x.split("."); return d + (" content" if b == "C" else "")
lines = ["| Edge | Unit edges | Members | Main sources -> targets (members) | Fix | Item |", "|---|---:|---:|---|---|---|"]
item_dirs = collections.defaultdict(set); item_mem = collections.defaultdict(set)
for (k, a, b), os_ in sorted(by_dir.items(), key=lambda kv: (kv[0][0] != "up", -len({m for o in kv[1] for m in o["members"]}))):
    mem = {m for o in os_ for m in o["members"]}
    ue = {(unit(o["src"]), unit(o["tgt"])) for o in os_}
    src = collections.Counter()
    for o in os_: src[(o["src"].split(".")[-1], o["tgt"].split(".")[-1])] += len(o["members"])
    top = ", ".join(f"{s} -> {t} ({c})" for (s, t), c in src.most_common(3))
    fixes = collections.Counter(); items = collections.Counter()
    for o in os_:
        f = re.sub(r"move (\w+) down to (\w+)", r"move \1 down", o["fix"])
        fixes[f] += len(o["members"]); items[o["item"]] += len(o["members"])
        item_dirs[o["item"]].add((k, a, b)); item_mem[o["item"]].update((a, b, m) for m in o["members"])
    fx = "; ".join(f for f, _ in fixes.most_common(2))
    it = ", ".join(i for i, _ in items.most_common(3))
    prefix = "hook: " if k == "native" else ""
    lines.append(f"| {prefix}{band(a)} -> {band(b)} | {len(ue)} | {len(mem)} | {top} | {fx} | {it} |")
open(OUT + "/appendixC.md", "w").write("\n".join(lines))
order = ["P5-1e", "P5-1g", "P5-1h", "P5-3", "P5-4", "P5-5", "P5-7", "P5-2", "P5-6", "P5-8", "P5-9", "P5-10", "P5-11", "P5-15a", "P5-16", "P5-15"]
for i in order: print(i, len(item_dirs[i]), len(item_mem[i]))
print(len(lines) - 2, "rows; unit edges total", sum(1 for _ in lines))
