from common import *
import re
types = load_types()

# Contract types: bases and interfaces of content types declared outside content (and in content: bases of other content).
CONTRACT = set(); CONTENT_IMPL = collections.defaultdict(set)
for t, ti in types.items():
    u = unit_of(ti["ns"], ti["top"])
    if not is_content(u): continue
    for b in (ti["bases"].split(";") if ti["bases"] else []) + (ti["interfaces"].split(";") if ti["interfaces"] else []):
        if b in types and b != t:
            CONTRACT.add(b); CONTENT_IMPL[b].add(t)

SERVICE_IFACE = re.compile(r"^I[A-Z]\w*Service$")
DEF_SUFFIX = ("Builder", "Detail", "Details", "Definition", "DefinitionBase")

def tinfo(name):
    return types.get(name)

def simple(name):
    return name.split(".")[-1]

def classify(row):
    """Returns (class, subclass). class in V (vocabulary), D (contract/definition), S (service call), O (other)."""
    t = row["toType"]; mode = row["mode"]; ti = tinfo(t)
    tu = unit_of(row["toNs"], row["toTop"])
    if mode == "enum": return ("V", "enum member")
    if mode == "const": return ("V", "constant")
    if mode == "nameof": return ("V", "nameof")
    if ti is None: return ("O", "unknown")
    kind = ti["kind"]; nm = simple(t)
    if kind == "Enum": return ("V", "enum")
    if ti["attribute"] == "True": return ("V", "attribute")
    if t == ROOT + ".Composition.GameServices": return ("S", "static lookup (GameServices)")
    if kind == "Interface" and SERVICE_IFACE.match(nm) and t.startswith(ROOT + ".Domain."): return ("S", "service interface")
    if is_content(tu):
        # content type named by logic
        anc = ti["bases"].split(";") + ti["interfaces"].split(";")
        if any(b in CONTRACT for b in anc) and kind != "Interface":
            if ti["static"] == "True": return ("S", "content static helper")
            if any(b.endswith((".IGuiRefreshEvent", ".GuiPayloadBase")) for b in anc): return ("V", "GUI event or payload in content")
            if any(b.endswith(".StatusEffectBase") for b in anc): return ("O", "status effect type named as a key or for its members")
            if any(b.endswith(".IGuiViewModel") for b in anc): return ("O", "view model")
            return ("O", "other content class")
        if t in CONTRACT: return ("D", "contract declared in content")
    if t in CONTRACT: return ("D", "contract content implements")
    if nm.endswith(DEF_SUFFIX): return ("D", "definition type")
    if kind == "Delegate": return ("V", "delegate")
    if tu == "Platform/Entity": return ("V", "persisted entity")
    if kind == "Interface": return ("O", "interface not implemented by content")
    methods = int(ti["methods"])
    if ti["static"] == "True":
        if methods == 0: return ("V", "constants or tables")
        return ("S", "static helper")
    if ti["record"] == "True" or kind == "Struct": return ("V", "record or struct")
    if methods == 0: return ("V", "data class")
    if nm.endswith("Service"): return ("O", "concrete service class")
    return ("O", "class with behaviour")

RANK = {"V": 0, "D": 1, "O": 2, "S": 3}

def cross_rows(rows, unit_override=None):
    """Yield (row, fu, tu, fd, td) for cross-domain references (wiring excluded)."""
    for r in rows:
        fu = unit_of(r["fromNs"], r["fromTop"]); tu = unit_of(r["toNs"], r["toTop"])
        if unit_override:
            fu = unit_override.get(r["fromNs"] + "." + r["fromTop"], fu)
            tu = unit_override.get(r["toNs"] + "." + r["toTop"], tu)
        if fu is None or tu is None or fu == tu or fu == "Platform/Composition": continue
        fd, td = domain_of(fu), domain_of(tu)
        if fd is None or td is None or fd == td: continue
        yield r, fu, tu, fd, td
