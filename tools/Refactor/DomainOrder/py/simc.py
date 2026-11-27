from sim import *
BOTTOM_PLATFORM = {"Platform/Core", "Platform/Entity", "Platform/Enumeration", "Platform/Extension", "Platform/Properties", "Platform/Root"}
WIRING = {"Platform/Composition", "Platform/NativeSinks"}
ENTITY_IN_FOUNDATION = True

def node_c(u):
    if u is None or u in WIRING: return None
    if u in ("Platform/Entity", "Platform/Enumeration") and ENTITY_IN_FOUNDATION: return ("Foundation", "L")
    if u in BOTTOM_PLATFORM: return ("Platform", "L")
    if u == "Platform/Native": return ("Native", "L")
    if u.startswith("Platform/"): return ("Platform", "L")
    if u.startswith("Domain/"):
        return (u[7:], "L") if u[7:] in DOMAINS else None
    if is_content(u):
        d = domain_of(u); return (d, "C") if d else None
    return None

def measure_c(override=None, classes="VDSO", node=node_c):
    """{(nodeFrom,nodeTo): {member: hardest class}} for different nodes; also unit edges."""
    edges = collections.defaultdict(dict); units = collections.defaultdict(set)
    for f in facts():
        if f[3] not in classes: continue
        fu = unit_key(f[0], override); tu = unit_key(f[2], override)
        if fu is None or tu is None or fu == tu: continue
        a, b = node(fu), node(tu)
        if a is None or b is None or a == b: continue
        d = edges[(a, b)]
        if f[1] not in d or RANK[f[3]] > RANK[d[f[1]]]: d[f[1]] = f[3]
        units[(a, b)].add((fu, tu))
    return edges, units

def kind_of_edge(a, b, rank):
    """'ok', 'up' (same band, points up or sideways), 'l2c' (logic names content)."""
    if a[0] == b[0]:
        return "l2c" if (a[1] == "L" and b[1] == "C") else "ok"
    if a[1] == "C" and b[1] == "L": return "ok"
    if a[1] == "L" and b[1] == "C": return "l2c"
    return "ok" if rank[b[0]] < rank[a[0]] else "up"

def order_dp(edges, classes, pinned=("Platform", "Foundation"), exclude=("Native",)):
    w = collections.Counter()
    for (a, b), d in edges.items():
        if a[1] != b[1] or a[0] == b[0] or a[0] in exclude or b[0] in exclude: continue
        w[(a[0], b[0])] += sum(1 for c in d.values() if c in classes)
    nodes = sorted({x for k in w for x in k} | set(pinned))
    order, cost = best_order(nodes, dict(w), pinned_bottom=pinned)
    return order, cost

def guard_pairs(edges):
    """Two-way pairs: among logic nodes, and among content nodes (Model C guard)."""
    dirs = {(a, b) for (a, b) in edges if a[1] == b[1] and a[0] != b[0]}
    return sorted({tuple(sorted((a[0] + "." + a[1], b[0] + "." + b[1]))) for (a, b) in dirs if (b, a) in dirs})

def domain_pairs(edges):
    """Model A pairs (domain = logic + content), the current guard (Native counted as Platform)."""
    dirs = set()
    for (a, b) in edges:
        x = "Platform" if a[0] == "Native" else a[0]; y = "Platform" if b[0] == "Native" else b[0]
        if x != y: dirs.add((x, y))
    return sorted({tuple(sorted(k)) for k in dirs if (k[1], k[0]) in dirs})
