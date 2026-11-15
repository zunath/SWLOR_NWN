import simc, re
from simc import *
simc.ENTITY_IN_FOUNDATION = True

def type_shape_class(key):
    """Class of a top-level type by its shape: 'V' vocabulary, 'D' contract/definition, 'E' entity, None not movable."""
    ti = TYPES.get(key)
    if ti is None: return None
    u = TOP_UNIT.get(key)
    if u == "Platform/Entity": return "E"
    kind = ti["kind"]; nm = ti["top"]
    if is_content(u) and kind == "Class":
        # content definitions (status effects, abilities, view models) are content, never vocabulary
        bases = [b for b in ti["bases"].split(";") if b in TYPES] + [i for i in ti["interfaces"].split(";") if i in TYPES]
        if bases: return None
    if kind == "Enum" or ti["attribute"] == "True" or kind == "Delegate": return "V"
    if key in CONTRACT and kind == "Interface": return "D"
    if kind == "Interface" and SERVICE_IFACE.match(nm): return None
    if kind == "Interface": return "D"
    if nm.endswith(DEF_SUFFIX): return "D" if int(ti["methods"]) == 0 else None
    if key in CONTRACT and ti["abstract"] == "True": return None   # abstract bases with behaviour (StatusEffectBase) stay
    methods = int(ti["methods"])
    if ti["static"] == "True": return "V" if methods == 0 else None
    if ti["record"] == "True" or kind == "Struct": return "V"
    if methods == 0 and kind == "Class" and not nm.endswith("Service"): return "V"
    return None

# Approved explicit moves (whole families and whole files), applied before the rule.
D_ = ROOT + ".Domain."
EXPLICIT = {}
for t in ["AbilityDetail", "AbilityActivationAction", "AbilityActivationDelayAction", "AbilityCustomValidationAction",
          "AbilityImpactAction", "AbilityRecastDelayAction", "AbilityActivationType", "AbilityTargetingDetail",
          "AbilityTargetingSizeResolver", "AbilityTargetingFlags", "AbilityTargetingShapeType", "IAbilityActivationRequirement",
          "MeleeTechniqueDetail", "MeleeTechniqueLine", "MeleeTechniqueStage"]:
    EXPLICIT[D_ + "Abilities." + t] = ("Character", "L")
for t in ["Log", "LogGroup", "LogGroupAttribute", "FrameProfiler", "FrameMetric", "FrameCounters", "FrameCaptureSummary"]:
    EXPLICIT[D_ + "Foundation." + t] = ("Platform", "L")
for t in ["CombatDamageType", "CombatDamageAttribute", "CombatDamageTypeExtensions", "CombatDamageDetails", "CombatDamageCategoryType", "ResistanceType"]:
    EXPLICIT[D_ + "Combat." + t] = ("Foundation", "L")

# Misplaced low code that the proposal moves up (it never pulls vocabulary down).
MISPLACED_USERS = {"AbilityAnimationBinding", "AnimationPreviewCatalog", "CommunicationService", "ICommunicationService"}

def node_of_unit(u):
    return node_c(u)

def placement(order, movable_classes=("V", "D"), max_iter=20):
    """Returns {typeKey: new unit} for movable types that must move, and conflict list."""
    rank = {d: i for i, d in enumerate(order)}
    rank["Native"] = 99   # Native is moving into domains; its references never pull vocabulary down
    shape = {k: type_shape_class(k) for k in TOP_UNIT}
    movable = {k for k, s in shape.items() if s in movable_classes}
    # references between top types (any class), by source member's unit
    refs_in = collections.defaultdict(set)   # target key -> set of source keys
    refs_out = collections.defaultdict(set)  # source key -> set of (target key, class)
    for f in facts():
        refs_in[f[2]].add(f[0]); refs_out[f[0]].add((f[2], f[3]))
    place = {k: v for k, v in EXPLICIT.items() if k in TOP_UNIT}   # key -> (domain, band)
    hard = collections.defaultdict(set)
    for f in facts():
        if f[3] in ("S", "O"):
            a = node_c(TOP_UNIT.get(f[0])); b = node_c(TOP_UNIT.get(f[2]))
            if a and b and a[1] == "L" and b[1] == "L" and a[0] != b[0]: hard[a[0]].add(b[0])
    hard_t = collections.defaultdict(set)
    for f in facts():
        if f[3] in ("S", "O"):
            b = node_c(TOP_UNIT.get(f[2]))
            if b and b[1] == "L": hard_t[f[0]].add(b[0])
    def hard_dep_type(src, y):
        n = cur(src)
        return n is not None and y in hard_t[src] and rank.get(y, 0) > rank.get(n[0], 0)
    def hard_dep(x, y):
        return y in hard[x] and rank.get(y, 0) > rank.get(x, 0)
    def cur(k):
        if k in place: return place[k]
        return node_c(TOP_UNIT.get(k))
    for _ in range(max_iter):
        changed = False
        for k in sorted(movable - set(EXPLICIT)):
            home = cur(k)
            if home is None:
                continue
            logic_users = []
            for s in refs_in[k]:
                n = cur(s)
                if n is None:
                    continue
                if n[1] != "L" or n[0] == home[0]:
                    continue
                # Native is moving into Combat (P5-16b): its references never pull a type down, and it is no destination.
                if n[0] == "Native":
                    continue
                # a user that already depends on T's home domain through code (S/O) does not pull T down:
                # that dependency has to be inverted or moved anyway.
                if hard_dep_type(s, home[0]):
                    continue
                if s.split(".")[-1] in MISPLACED_USERS:
                    continue
                logic_users.append(n[0])
            if not logic_users and home[1] == "C":
                continue
            cand = [home[0]] if home[1] == "L" else []
            cand += logic_users
            target = min(cand, key=lambda d: rank.get(d, 0))
            new = (target, "L")
            if new != home:
                place[k] = new
                changed = True
        if not changed:
            break
    else:
        raise RuntimeError(f"The type moves did not settle in {max_iter} rounds; the placement would be unfinished.")
    # conflicts: a moved type that, where it ends up, names content (its own domain's included) or code above it
    conflicts = []
    for k, (d, b) in place.items():
        for t, c in refs_out[k]:
            n = cur(t)
            if n is None:
                continue
            if n[1] == "C" or (n[0] != d and rank.get(n[0], 0) > rank.get(d, 0)):
                conflicts.append((k, d, t, n, c))
    return place, conflicts

def unit_for(node):
    d, b = node
    if d == "Platform": return "Platform/Core"
    return "Domain/" + d
