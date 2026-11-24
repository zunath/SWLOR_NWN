"""Every upward edge left after the moves, with its fix and item (Appendix C data), written to upedges.json.

Prints the directions and member references per kind ("up" in the logic and content bands, "native" for the hook logic in
Platform/Native, which P5-16b moves into Combat) and per item. Needs moves.json (order_cost.py).
"""
import sim
sim.DETACH_AI = True
import simc
from pair_steps import *
simc.ENTITY_IN_FOUNDATION = True
CHAIN = [("P5-3", r"Domain\.Abilities\.I?Ability(Service|Requirement\w*|ImpactSequence|ActiveActivation)$"),
         ("P5-5", r"Domain\.Character\.I?(StatusEffect\w*|CreatureStatusEffect)$"),
         ("P5-4", r"Domain\.Character\.I?Stat(?!usEffect)\w*$"),
         ("P5-7", r"Domain\.Combat\.I?(Combat\w*Service|EnmityService|CombatRules)$"),
         ("P5-8", r"Domain\.Creatures\.I?(AI|Spawn|CompanionControl|CompanionPositioning|CompanionTargetAuthority)\w*$"),
         ("P5-9", r"Domain\.(Items|World|Crafting)\.I?(Item|Property|Shuttle|Craft|BlueprintBonus)\w*$"),
         ("P5-10", r"Domain\.(Character|Abilities|Companions)\.I?(Perk|Skill|PlayerName|PlayerKnownName|Mimicry|Droid|BeastMastery)\w*$"),
         ("P5-11", r"Domain\.(Foundation|Social|UI)\.I?(Communication|Performance\w*|ExtraHotbars|GuiWindowBuilder)\w*$"),
         ("P5-6", r"Domain\.Space\.I?Space\w*$"),
         ("P5-16", r"(Native|NativeSinks)\.\w+$")]
def item_for(key):
    k = key.replace(ROOT + ".", "")
    for item, rx in CHAIN:
        if re.search(rx, k): return item
    return None
SERIAL = ["P5-3", "P5-4", "P5-5", "P5-7"]
def item_pair(src, tgt):
    a, b = item_for(src), item_for(tgt)
    if a in SERIAL and b in SERIAL: return min(a, b, key=SERIAL.index)
    return a or b
MISPLACED_MOVE = {"CommunicationService": "Social", "ICommunicationService": "Social", "AbilityAnimationBinding": "Abilities",
                  "AnimationPreviewCatalog": "Abilities", "EquipmentPredicates": "Items"}
# per source type: up and down hard members (logic band)
up_by_src = collections.defaultdict(lambda: collections.defaultdict(set)); down_by_src = collections.defaultdict(set)
hard_out_dom = collections.defaultdict(set)
for f in facts():
    if f[3] == "V": continue
    fu = unit_key(f[0], OV_VD); tu = unit_key(f[2], OV_VD)
    a, b = node_c(fu), node_c(tu)
    if not a or not b or a == b: continue
    hard_out_dom[f[2]]  # touch
    if a[0] == b[0]: continue
    if a[1] == "L" and b[1] == "L":
        hard_out_dom[f[0]].add(b[0])
        if RANK.get(b[0], 0) > RANK.get(a[0], 0): up_by_src[f[0]][b[0]].add(f[1])
        else: down_by_src[f[0]].add(f[1])
rows = []
groups = collections.defaultdict(lambda: collections.defaultdict(set))
for f in facts():
    if f[3] == "V": continue
    fu = unit_key(f[0], OV_VD); tu = unit_key(f[2], OV_VD)
    a, b = node_c(fu), node_c(tu)
    if not a or not b or a == b: continue
    k = kind_of_edge(a, b, RANK)
    if a[0] == "Native":
        k = "native"
        if b[0] in ("Platform", "Foundation"): continue
    if k in ("ok", "l2c"): continue
    groups[(k, a, b)][(f[0], f[2], f[3])].add(f[1])
def fix_for(k, a, b, src, tgt, cls):
    fix, item = fix_for0(k, a, b, src, tgt, cls)
    sname = src.split(".")[-1]; tname = tgt.split(".")[-1]
    if k == "up" and a[1] == "L" and b[1] == "L":
        if tname == "ICombatRecorderService" and RANK[a[0]] < RANK["Combat"]:
            return ("invert: recording interface owned by Foundation, implemented by the recorder" if a[0] != "Platform" else fix, "P5-1g")
        if tname in ("IBeastMasteryService", "IDroidService") and a[0] in ("Character", "Items", "Creatures", "Combat"):
            return ("invert: companion lookup interface owned by Character, implemented by BeastMastery and Droid", "P5-1g")
        if tname == "IGuiService" and a[0] in ("Character", "Foundation", "Items", "Administration"):
            return ("invert: window refresh publisher owned by Foundation, implemented by Gui", "P5-1g")
    if item == "P5-1b":
        item = "P5-1e" if "static entry points" in fix else "P5-1h"
    if item == "P5-15" and k == "up":
        item = "P5-15a"
    return fix, item

def fix_for0(k, a, b, src, tgt, cls):
    sname = src.split(".")[-1]; tname = tgt.split(".")[-1]
    if k == "native":
        if b[1] == "C" or b[0] == "Gameplay": return ("hook names content: move the helper (UsePerkFeat and others) into logic", "P5-16")
        return ("move the hook logic into Combat services", "P5-16")
    if a[1] == "L" and b[0] == "Native":
        if a[0] == "Combat": return ("same domain once the hook logic moves into Combat", "P5-16")
        return ("move the native helper (" + tname + ") down, or invert", "P5-16")
    if a[0] == "Platform" and tname in ("Log", "LogGroup", "FrameProfiler", "FrameMetric", "Random", "ColorToken"):
        return ("move the static entry points Log and FrameProfiler (with LogGroup, FrameMetric) down into Core", "P5-1b")
    if a[0] == "Platform" and tname == "ICombatRecorderService":
        return ("invert: the recorder joins Core's IServerTeardown participants", "P5-7")
    if a[0] == "Platform" and tname == "OOCAllowance":
        return ("move the OOCAllowance setting value down to Platform", "P5-1b")
    if sname in MISPLACED_MOVE:
        return ("move the code up to " + MISPLACED_MOVE[sname], item_for(src) or "P5-1b")
    up = sum(len(s) for s in up_by_src[src].values()); dn = len(down_by_src[src])
    tshape = TYPES.get(tgt, {})
    if up >= 3 and dn == 0 and a[1] == "L" and src.split(".")[-2] in ("Foundation", "Character", "UI", "Administration", "Items", "Gameplay"):
        return ("move the code up to " + max(up_by_src[src], key=lambda d: len(up_by_src[src][d])), item_for(src) or "P5-1b")
    if cls == "S":
        if a[1] == "C":
            return ("content -> content: call through " + b[0] + "'s logic, or move the helper into " + b[0] + " logic", "P5-2")
        return ("invert: interface or event owned by " + a[0], item_pair(src, tgt) or "P5-15")
    if cls in ("O", "D"):
        above = {d for d in hard_out_dom[tgt] if RANK.get(d, 0) > RANK.get(a[0], 0)}
        if not above and a[1] == b[1] == "L":
            return ("move " + tname + " down to " + a[0], item_for(tgt) or item_for(src) or "P5-1b")
        if a[1] == "C":
            return ("content -> content: move " + tname + " into logic or invert", "P5-2")
        return ("invert: interface owned by " + a[0], item_pair(src, tgt) or "P5-15")
    return ("?", "?")
out = []
for (k, a, b), g in groups.items():
    for (src, tgt, cls), ms in g.items():
        fix, item = fix_for(k, a, b, src, tgt, cls)
        out.append({"kind": k, "from": f"{a[0]}.{a[1]}", "to": f"{b[0]}.{b[1]}", "src": src.replace(ROOT + ".", ""), "tgt": tgt.replace(ROOT + ".", ""),
                    "cls": cls, "members": sorted(ms), "fix": fix, "item": item})
json.dump(out, open(OUT + "/upedges.json", "w"), indent=0)
# summaries
by_dir = collections.defaultdict(lambda: collections.Counter()); mem_dir = collections.defaultdict(set)
by_item = collections.Counter(); by_mech = collections.Counter(); mem_item = collections.defaultdict(set)
for o in out:
    key = (o["kind"], o["from"], o["to"])
    for m in o["members"]: mem_dir[key].add(m)
    mech = o["fix"].split(":")[0].split(" ")[0]
    by_item[o["item"]] += len(o["members"]); by_mech[o["fix"].split(" to ")[0].split(":")[0]] += len(o["members"])
    mem_item[o["item"]].update((o["src"], m) for m in o["members"])
print("directions", collections.Counter(k[0] for k in mem_dir))
print("members per kind", {kd: sum(len(v) for k, v in mem_dir.items() if k[0] == kd) for kd in ("up", "native")})
print("by item (member refs):", {k: len(v) for k, v in mem_item.items()})
print("by mechanism:", by_mech.most_common())
