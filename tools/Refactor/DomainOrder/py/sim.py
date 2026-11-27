from classify import *
import functools, itertools
# The approved plan takes the AI settings off AbilityDetail: AbilityBuilder's HasAITarget, HasAIScore and
# HasAIHitPointCostPercent become extension methods in Creatures writing a Creatures-owned table. DETACH_AI applies that.
DETACH_AI = False
AI_MEMBERS = {"AbilityDetail::AITargetSelector", "AbilityDetail::AIScore", "AbilityDetail::AIHitPointCostPercent",
              "AbilityBuilder::HasAITarget", "AbilityBuilder::HasAIScore", "AbilityBuilder::HasAIHitPointCostPercent"}
_FACTS = {}
def facts():
    """List of (fromKey, member, toKey, cls, sub, toType, mode). Same-top references skipped."""
    key = DETACH_AI
    if key not in _FACTS:
        out = []
        for r in load_refs():
            fk = r["fromNs"] + "." + r["fromTop"]; tk = r["toNs"] + "." + r["toTop"]
            if fk == tk: continue
            if DETACH_AI and r["member"] in AI_MEMBERS and r["toNs"].endswith(".Domain.Creatures"): continue
            c, sub = classify(r)
            out.append((fk, r["fromNs"] + "." + r["member"], tk, c, sub, r["toType"], r["mode"]))
        _FACTS[key] = out
    return _FACTS[key]

TYPES = load_types()
TOP_UNIT = {}
for t, ti in TYPES.items():
    TOP_UNIT[ti["ns"] + "." + ti["top"]] = unit_of(ti["ns"], ti["top"])

def unit_key(k, override):
    if override and k in override: return override[k]
    return TOP_UNIT.get(k)

def measure(override=None, skip=lambda f: False, unit_domain=domain_of):
    """Returns edges {(fu,tu): {member: cls}} across domains (wiring excluded)."""
    edges = collections.defaultdict(dict)
    for f in facts():
        if skip(f): continue
        fu = unit_key(f[0], override); tu = unit_key(f[2], override)
        if fu is None or tu is None or fu == tu or fu == "Platform/Composition": continue
        fd, td = unit_domain(fu), unit_domain(tu)
        if fd is None or td is None or fd == td: continue
        d = edges[(fu, tu)]
        if f[1] not in d or RANK[f[3]] > RANK[d[f[1]]]: d[f[1]] = f[3]
    return edges

def domain_matrix(edges, unit_domain=domain_of):
    m = collections.defaultdict(lambda: collections.Counter())
    e = collections.defaultdict(set)
    for (fu, tu), d in edges.items():
        k = (unit_domain(fu), unit_domain(tu))
        for mem, c in d.items(): m[k][c] += 1
        e[k].add((fu, tu))
    return m, e

def pairs_of(e):
    return sorted({tuple(sorted(k)) for k in e if (k[1], k[0]) in e})

def best_order(nodes, weight, pinned_bottom=()):
    """Exact minimum feedback arc set by DP over subsets. weight[(a,b)] = cost if a is placed BELOW b (edge a->b points up).
    Order is bottom..top. pinned_bottom nodes come first in the given order."""
    free = [n for n in nodes if n not in pinned_bottom]
    idx = {n: i for i, n in enumerate(free)}
    N = len(free)
    # cost of adding node x on top of set S: edges from members of S to x point up (S below x, edge s->x is up)
    # placing x above S: any edge s->x where s in S points up (lower depends on higher).
    up_into = [[0]*N for _ in range(N)]
    for a in free:
        for b in free:
            if a != b: up_into[idx[a]][idx[b]] = weight.get((a, b), 0)
    # pinned nodes are below everything: edges pinned->free point up.
    base_cost = [sum(weight.get((p, x), 0) for p in pinned_bottom) for x in free]
    INF = float("inf")
    dp = [INF] * (1 << N); par = [-1] * (1 << N)
    dp[0] = 0
    for S in range(1 << N):
        if dp[S] == INF: continue
        for x in range(N):
            if S >> x & 1: continue
            c = base_cost[x]
            T = S
            while T:
                lb = T & -T; s = lb.bit_length() - 1; c += up_into[s][x]; T ^= lb
            nS = S | (1 << x)
            if dp[S] + c < dp[nS]: dp[nS] = dp[S] + c; par[nS] = x
    order = []; S = (1 << N) - 1
    while S:
        x = par[S]; order.append(free[x]); S ^= 1 << x
    order.reverse()
    pin_cost = sum(weight.get((pinned_bottom[i], pinned_bottom[j]), 0) for i in range(len(pinned_bottom)) for j in range(i+1, len(pinned_bottom)))
    return list(pinned_bottom) + order, dp[(1 << N) - 1] + pin_cost
