"""Shared inputs of the domain order scripts (tools/Refactor/DomainOrder/README.md).

DOMAIN_ORDER_OUT (required): the folder EdgeExtract wrote refs.tsv and types.tsv into; the scripts write their json there.
DOMAIN_ORDER_REPO: the repository root (default: the one holding these scripts).
DOMAIN_ORDER_MAP: the domain map (default: <repo>/SWLOR.Architecture.Tests/DomainMap.json); its "order" is the order measured.
"""
import csv, json, os, sys, collections, re
csv.field_size_limit(10**9)
ROOT = "SWLOR.Game.Server"
REPO = os.environ.get("DOMAIN_ORDER_REPO") or os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
OUT = os.environ.get("DOMAIN_ORDER_OUT")
if not OUT:
    sys.exit("Set DOMAIN_ORDER_OUT to the folder EdgeExtract wrote refs.tsv and types.tsv into.")
MAP_PATH = os.environ.get("DOMAIN_ORDER_MAP") or os.path.join(REPO, "SWLOR.Architecture.Tests", "DomainMap.json")

def load_map():
    m = json.load(open(MAP_PATH, encoding="utf-8"))
    domains = list(m["domains"].keys())
    folder_domain = {}
    for d, v in m["domains"].items():
        for f in v["featureFolders"]:
            folder_domain[f] = d
    return m, domains, folder_domain

MAP, DOMAINS, FOLDER_DOMAIN = load_map()
# The approved order, bottom to top (owner decisions 10 to 13).
ORDER = list(MAP["order"])

def unit_of(ns, top):
    """Replicates UnitClassifier.Classify: returns unit string or None."""
    if ns == ROOT:
        return "Platform/Root"
    if not ns.startswith(ROOT + "."):
        return None
    seg = ns[len(ROOT) + 1:].split(".")
    if seg[0] == "Service":
        return None
    if seg[0] == "Feature":
        return "Feature." + top if len(seg) == 1 else "Feature/" + seg[1]
    if seg[0] == "Domain" and len(seg) >= 2:
        return "Domain/" + seg[1]
    return "Platform/" + seg[0]

def domain_of(unit):
    """The domain as P5-0 counted it: Entity and Enumeration in Platform (the scripts apply the recount themselves)."""
    if unit is None: return None
    if unit.startswith("Domain/"):
        n = unit[7:]
        return n if n in DOMAINS else None
    if unit.startswith("Platform/"): return "Platform"
    if unit.startswith("Feature."): return MAP["featureTypeDomain"]
    if unit.startswith("Feature/"): return FOLDER_DOMAIN.get(unit[8:])
    return None

def is_content(unit):
    return unit is not None and unit.startswith("Feature")

def load_refs():
    rows = []
    with open(os.path.join(OUT, "refs.tsv"), encoding="utf-8") as f:
        r = csv.DictReader(f, delimiter="\t")
        for row in r:
            rows.append(row)
    return rows

def load_types():
    types = {}
    with open(os.path.join(OUT, "types.tsv"), encoding="utf-8") as f:
        r = csv.DictReader(f, delimiter="\t")
        for row in r:
            types[row["type"]] = row
    return types
