#!/usr/bin/env python3
"""Reorganize SWLOR hak source folders by content ownership.

Requires Python 3.10 or newer.

The script is intentionally conservative:
- Builds the active resource set from the current module hak order.
- Keeps only the current highest-priority winner for each resref/ext pair.
- Fails before moving anything if an active resource cannot be assigned.
- Writes CSV/JSON audit files for review before and after applying.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import shutil
import shlex
import sys
from collections import Counter, defaultdict
from pathlib import Path


MIN_PYTHON_VERSION = (3, 10)
if sys.version_info < MIN_PYTHON_VERSION:
    raise SystemExit("reorganize_hak_sources.py requires Python 3.10 or newer.")

REPO_ROOT = Path(__file__).resolve().parents[1]
HAK_ROOT = REPO_ROOT / "SWLOR_Haks"
BUILD_CONFIG = REPO_ROOT / "Build" / "hakbuilder.json"
SUBMODULE_CONFIG = HAK_ROOT / "hakbuilder.json"
MODULE_IFO = REPO_ROOT / "Module" / "ifo" / "module.ifo.json"
AUDIT_DIR = HAK_ROOT / "output" / "hak_reorg_audit"
CUSTOM_TLK_NAME = "sw_tlk"
CUSTOM_TLK_FILE = f"{CUSTOM_TLK_NAME}.tlk"

TEXTURE_EXTS = {"dds", "tga", "txi", "mtr", "plt"}
MODEL_DEP_EXTS = {"mdl", "mtr", "txi"}
RESOURCE_EXTS = {
    "2da",
    "are",
    "bmu",
    "dds",
    "dwk",
    "gui",
    "ini",
    "itp",
    "mdl",
    "mtr",
    "plt",
    "pwk",
    "set",
    "shd",
    "ssf",
    "tga",
    "txi",
    "uti",
    "wav",
    "wok",
    "txt",
}


TILE_HAKS = {
    "dgt04": "sw_t_modernex",
    "fcx01": "sw_t_futcity",
    "fifi": "sw_t_bunker",
    "flow_pa": "sw_t_garage",
    "jac01": "sw_t_jungle",
    "mb012": "sw_t_ravforest",
    "nac01": "sw_t_duct",
    "net01": "sw_t_virtunet",
    "shp02": "sw_t_starship",
    "sjm01": "sw_t_metalint",
    "srt04": "sw_t_shadowrun",
    "swp01": "sw_t_swprefab",
    "tbw01": "sw_t_barrow",
    "tbx78": "sw_t_facility",
    "tcdh0": "sw_t_secbase",
    "tcn01": "sw_t_cityext",
    "tdc01": "sw_t_crypt",
    "tde01": "sw_t_dungeon",
    "tdm01": "sw_t_mine",
    "tdr01": "sw_t_ruin",
    "tds01": "sw_t_sewer",
    "tdt01": "sw_t_minecave",
    "tec01": "sw_t_elvencity",
    "tfb01": "sw_t_modint",
    "thf02": "sw_t_treetop",
    "tib01": "sw_t_beholder",
    "tic01": "sw_t_castle1",
    "tid01": "sw_t_drowint",
    "tii01": "sw_t_illithid",
    "tin01": "sw_t_cityint1",
    "tjsb0": "sw_t_secretbs",
    "tmi": "sw_t_modint2",
    "tms01": "sw_t_template",
    "tni01": "sw_t_cityint2",
    "tni02": "sw_t_tnocastle",
    "tno01": "sw_t_castleex",
    "tqq01": "sw_t_labstore",
    "tsw01": "sw_t_steamwork",
    "ttd01": "sw_t_tatooine",
    "ttf01": "sw_t_forest",
    "tti01": "sw_t_frozen",
    "ttr01": "sw_t_rural",
    "tts01": "sw_t_winter",
    "ttu01": "sw_t_underdark",
    "ttw01": "sw_t_wildwood",
    "ttz01": "sw_t_coastal",
    "twc03": "sw_t_fortint",
    "twl01": "sw_t_wildland",
    "udp1": "sw_t_suburb",
    "udp2": "sw_t_office",
    "vac01": "sw_t_space",
    "vmp01": "sw_t_planet",
    "vmr01": "sw_t_alienruin",
    "wsf10": "sw_t_season",
    "zcn01": "sw_t_cepcityex",
    "zdc01": "sw_t_cepcrypt",
    "zde01": "sw_t_cepdungeon",
    "zdm01": "sw_t_cepmine",
    "zib01": "sw_t_cepbehold",
    "zic01": "sw_t_cepcastle",
    "zid01": "sw_t_cepdrow",
    "zin01": "sw_t_cepcityin",
    "zkw01": "sw_t_cepswamp",
    "zsf01": "sw_t_scifibase",
    "ztd01": "sw_t_cepdesert",
    "ztf01": "sw_t_cepforest",
    "zti01": "sw_t_cepfrozen",
    "ztr01": "sw_t_ceprural",
    "zts01": "sw_t_cepwinter",
    "ztu01": "sw_t_cepunder",
}

TILE_ALIASES = {
    "cor01": "sw_t_futcity",
    "fcx": "sw_t_futcity",
    "fci": "sw_t_starship",
    "fci01": "sw_t_starship",
    "shp": "sw_t_starship",
    "shp03": "sw_t_starship",
    "jac": "sw_t_jungle",
    "midg01": "sw_t_modernex",
    "swp": "sw_t_swprefab",
    "tec": "sw_t_elvencity",
    "ttd": "sw_t_tatooine",
    "vmp": "sw_t_planet",
    "vmr": "sw_t_alienruin",
    "wm": "sw_t_season",
    "wmttf": "sw_t_season",
    "wsf01": "sw_t_season",
    "wsf11": "sw_t_season",
    "wsf14": "sw_t_season",
    "wsf15": "sw_t_season",
    "wsf30": "sw_t_season",
    "wsf31": "sw_t_season",
    "wsf32": "sw_t_season",
    "wsf41": "sw_t_season",
    "zdc": "sw_t_cepcrypt",
    "zcn02": "sw_t_cepcityex",
    "zcn03": "sw_t_cepcityex",
    "zdc02": "sw_t_cepcrypt",
    "zdc03": "sw_t_cepcrypt",
    "zde": "sw_t_cepdungeon",
    "zdm": "sw_t_cepmine",
    "zdm02": "sw_t_cepmine",
    "zdm03": "sw_t_cepmine",
    "zic": "sw_t_cepcastle",
    "zin": "sw_t_cepcityin",
    "zkw02": "sw_t_cepswamp",
    "ztd": "sw_t_cepdesert",
    "ztf": "sw_t_cepforest",
    "ztr": "sw_t_ceprural",
    "ztu": "sw_t_cepunder",
}

TILE_PREFIX_OWNERS = {**TILE_HAKS, **TILE_ALIASES}

CEP_CREATURE_PREFIXES = (
    "a_",
    "b_",
    "cpex",
    "cplt",
    "cpb_",
    "drider",
    "gith",
    "hp_",
    "hyx_",
    "zcp_",
    "z_plt",
)

SWTOR_PLACEABLE_PREFIXES = ("asw", "daf", "mtp", "swt")
MODERN_PLACEABLE_PREFIXES = (
    "arcology",
    "cityscape",
    "deepspace",
    "dsky",
    "d_skybox",
    "gasgiant",
    "jeep",
    "mod01",
    "oiltruck",
    "space",
    "storm_",
    "tron",
    "udp-",
    "udp1",
    "udp2",
)
CEP_PLACEABLE_PREFIXES = (
    "ad_",
    "arf_",
    "ccc_",
    "cccad_",
    "cd_",
    "cep2_",
    "dag_",
    "ff_",
    "gg",
    "gi-",
    "gi_",
    "jz_",
    "jzg_",
    "lok_",
    "phod_",
    "sglass_",
    "vos_",
    "wos_",
    "zall_",
    "zlc_",
)
LEGACY_PLACEABLE_PREFIXES = (
    "aenea_",
    "bh_",
    "cargo_",
    "computer_",
    "concrete_",
    "ctp_",
    "dk_",
    "firefly_",
    "jhb_",
    "pkt_",
    "plushwall",
    "p_eng",
    "tablechairs",
)
WEAPON_PREFIXES = (
    "ak47",
    "beretta",
    "blaster",
    "boltgun",
    "bow_caster",
    "briar_",
    "dh-",
    "dc-",
    "mg-",
    "mg_",
    "saber",
    "sfg_",
    "shield_",
    "wb",
    "wfaps",
    "wmi",
    "wpi",
    "wtool",
)

TARGET_HAKS = [
    "sw_2da",
    "sw_ability",
    "sw_ui",
    "sw_shader",
    "sw_palette",
    "sw_music",
    "sw_sound",
    "sw_load",
    "sw_skybox",
    "sw_door",
    "sw_vfx",
    "sw_portrait",
    "sw_pt_root",
    "sw_pt_belt",
    "sw_pt_chest",
    "sw_pt_cloak",
    "sw_pt_head",
    "sw_pt_helm",
    "sw_pt_neck",
    "sw_pt_pelvis",
    "sw_pt_robe",
    "sw_pt_lbicep",
    "sw_pt_rbicep",
    "sw_pt_lfore",
    "sw_pt_rfore",
    "sw_pt_lhand",
    "sw_pt_rhand",
    "sw_pt_lshoul",
    "sw_pt_rshoul",
    "sw_pt_lthigh",
    "sw_pt_rthigh",
    "sw_pt_lshin",
    "sw_pt_rshin",
    "sw_pt_lfoot",
    "sw_pt_rfoot",
    "sw_cr_creature",
    "sw_cr_vehicle",
    "sw_plc",
    "sw_plc_cep",
    "sw_plc_mdrn",
    "sw_plc_swtor",
    "sw_item",
    "sw_weapon",
    *TILE_HAKS.values(),
    "sw_tint_mtr",
    "sw_tint0",
    "sw_tint1",
    "sw_tint2",
]

OLD_SOURCE_HAKS = {
    "sw_2da",
    "swlor2_add_doors",
    "swlor2_add_loads",
    "swlor2_add_skies",
    "swlor2_add_tiles",
    "swlor2_core0",
    "swlor2_core1",
    "swlor2_core2",
    "swlor2_core3",
    "swlor2_core4",
    "swlor2_core5",
    "swlor2_core6",
    "swlor2_core7",
    "swlor2_dds",
    "swlor2_dwk",
    "swlor2_ext_tiles",
    "swlor2_gui",
    "swlor2_ini",
    "swlor2_itp",
    "swlor2_mdl",
    "swlor2_mtr",
    "sw_music",
    "swlor2_parts",
    "swlor2_plt",
    "sw_portrait",
    "swlor2_pwk",
    "swlor2_set",
    "swlor2_shd",
    "swlor2_ssf",
    "swlor2_tga",
    "swlor2_txi",
    "swlor2_wav",
    "swlor2_wok",
}


VEHICLE_RE = re.compile(
    r"ship|vehicle|speeder|swoop|\bbike\b|humvee|helicopter|\bheli\b|"
    r"xwing|awing|\btie\b|fighter|bomber|freighter|corvette|cruiser|"
    r"shuttle|gunship|transport|cargo|starflier|luxliner|hovertank|"
    r"siegetank|stuntank|barrac|bwfighter|jedi_shuttle|ffcargo|"
    r"c_man_|c_sith_|c_hutt_|c_rep_|c_ond_|c_lg_|g_prt_|g_seraph|"
    r"g_manka|g_hovertank|g_man_tank",
    re.I,
)

DROID_RE = re.compile(
    r"droid|robot|turret|astromech|probe|gonk|mousedroid|jawadroid|"
    r"tankdroid|drdmkone|drd_mk|c_drd|c_pdroid|protocol|c_mk[12]drd|"
    r"c_fordrd|heavy_droid|c_hvydroid",
    re.I,
)

RESREF_RE = re.compile(rb"[A-Za-z0-9_]{3,32}")


def rel(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


def norm(value: object) -> str | None:
    text = str(value or "").strip().strip('"').strip()
    if not text or text == "****":
        return None
    return text.lower()


def parse_2da(path: Path) -> list[dict[str, str | int]]:
    rows: list[dict[str, str | int]] = []
    header: list[str] | None = None
    with path.open("r", encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            line = raw.strip()
            if not line or line.startswith("//") or line.upper() == "2DA V2.0":
                continue
            if header is None:
                header = re.split(r"\s+", line)
                continue
            try:
                parts = shlex.split(line, posix=False)
            except ValueError:
                parts = re.findall(r'"[^"]*"|\S+', line)
            if not parts or not parts[0].isdigit():
                continue
            values = parts[1:]
            row: dict[str, str | int] = {"_row": int(parts[0])}
            for index, column in enumerate(header):
                row[column] = values[index] if index < len(values) else "****"
            rows.append(row)
    return rows


def parse_set(path: Path) -> dict[str, set[str]]:
    refs: dict[str, set[str]] = defaultdict(set)
    with path.open("r", encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            line = raw.strip()
            if not line or line.startswith(";") or "=" not in line:
                continue
            key, value = line.split("=", 1)
            key = key.strip().lower()
            value = norm(value)
            if not value:
                continue
            if key in {"model", "imagemap2d", "envmap"}:
                refs[key].add(value)
    return refs


def read_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8-sig", errors="replace") as handle:
        return json.load(handle)


def module_hak_order() -> list[str]:
    module = read_json(MODULE_IFO)
    return [entry["Mod_Hak"]["value"] for entry in module["Mod_HakList"]["value"]]


def build_path_by_hak() -> dict[str, Path]:
    config = read_json(BUILD_CONFIG)
    return {
        hak["Name"]: (BUILD_CONFIG.parent / hak["Path"]).resolve()
        for hak in config["HakList"]
    }


def add_exact(exact: dict[str, str], stem: str | None, owner: str) -> None:
    if stem and len(stem) >= 3 and stem not in exact:
        exact[stem] = owner


def add_prefix(prefixes: dict[str, str], prefix: str | None, owner: str) -> None:
    if prefix and len(prefix) >= 3:
        prefixes[prefix] = owner


def classify_placeable(row: dict[str, str | int]) -> str:
    label = str(row.get("Label", "")).strip().strip('"').lower()
    model = norm(row.get("ModelName")) or ""
    if "[swlor]" in label or model.startswith(("swlor", "swl_")):
        return "sw_plc"
    if "[swtor]" in label or model.startswith(("aswtor", "daf")):
        return "sw_plc_swtor"
    if "[mdrn" in label or "[new dt" in label or model.startswith(
        (
            "mdrn",
            "plc_swp",
            "p_",
            "dropship",
            "ast",
            "sun",
            "gasgiant",
            "brownmoon",
            "gnibile",
            "edill",
            "ffcargo",
        )
    ):
        return "sw_plc_mdrn"
    if "[a01]" in label or "[a02]" in label or model.startswith(("zlc", "cep_")):
        return "sw_plc_cep"
    return "sw_plc"


def classify_actor(row: dict[str, str | int]) -> str:
    label = str(row.get("LABEL", "")).strip().strip('"')
    race = norm(row.get("RACE")) or ""
    text = f"{label} {race}"
    if DROID_RE.search(text):
        return "sw_cr_creature"
    if VEHICLE_RE.search(text):
        return "sw_cr_vehicle"
    return "sw_cr_creature"


def player_part_owner(stem: str) -> str | None:
    if stem.startswith("cloak_"):
        return "sw_pt_cloak"
    if stem.startswith("helm_"):
        return "sw_pt_helm"

    match = re.match(r"^p[mf][a-z0-9]{2,4}_([a-z]+)", stem)
    if not match:
        return None

    part = match.group(1)
    part_map = {
        "belt": "sw_pt_belt",
        "chest": "sw_pt_chest",
        "cloak": "sw_pt_cloak",
        "head": "sw_pt_head",
        "helm": "sw_pt_helm",
        "neck": "sw_pt_neck",
        "pelvis": "sw_pt_pelvis",
        "robe": "sw_pt_robe",
        "bicepl": "sw_pt_lbicep",
        "bicepr": "sw_pt_rbicep",
        "forel": "sw_pt_lfore",
        "forer": "sw_pt_rfore",
        "handl": "sw_pt_lhand",
        "handr": "sw_pt_rhand",
        "shol": "sw_pt_lshoul",
        "shor": "sw_pt_rshoul",
        "legl": "sw_pt_lthigh",
        "legr": "sw_pt_rthigh",
        "shinl": "sw_pt_lshin",
        "shinr": "sw_pt_rshin",
        "footl": "sw_pt_lfoot",
        "footr": "sw_pt_rfoot",
    }
    return part_map.get(part, "sw_pt_root")


def is_weapon_baseitem(row: dict[str, str | int]) -> bool:
    fields = ["WeaponWield", "WeaponType", "WeaponSize", "RangedWeapon"]
    if any(norm(row.get(field)) for field in fields):
        return True
    item_class = norm(row.get("ItemClass")) or ""
    default_icon = norm(row.get("DefaultIcon")) or ""
    return item_class.lower().startswith("w") or default_icon.startswith("iw")


def collect_references() -> tuple[dict[str, str], dict[str, str]]:
    exact: dict[str, str] = {}
    prefixes: dict[str, str] = {}
    two_da_root = HAK_ROOT / "sw_2da"
    if not two_da_root.exists():
        two_da_root = HAK_ROOT / "sw_2da"

    # Tilesets own .set files, model/WOK names, minimaps, envmaps, and edge/door 2DAs.
    for set_path in HAK_ROOT.rglob("*.set"):
        set_name = set_path.stem.lower()
        owner = TILE_HAKS.get(set_name)
        if not owner:
            continue
        add_exact(exact, set_name, owner)
        add_prefix(prefixes, set_name, owner)
        add_exact(exact, f"{set_name}_edge", owner)
        add_exact(exact, f"{set_name}doors", owner)
        for names in parse_set(set_path).values():
            for name in names:
                add_exact(exact, name, owner)
                add_prefix(prefixes, name, owner)
                if "_" in name:
                    add_prefix(prefixes, name.split("_", 1)[0], owner)

    for alias, owner in TILE_ALIASES.items():
        add_prefix(prefixes, alias, owner)

    # Static actor appearances.
    appearance = two_da_root / "appearance.2da"
    if appearance.exists():
        for row in parse_2da(appearance):
            model_type = norm(row.get("MODELTYPE"))
            race = norm(row.get("RACE"))
            if not model_type or model_type == "p" or not race:
                continue
            if race in {"base", "default", "character_model"}:
                continue
            add_prefix(prefixes, race, classify_actor(row))

    # Placeables.
    placeables = two_da_root / "placeables.2da"
    if placeables.exists():
        for row in parse_2da(placeables):
            model = norm(row.get("ModelName"))
            if not model or model == "cep_reserved":
                continue
            add_prefix(prefixes, model, classify_placeable(row))

    # Tile-specific doors follow their tileset; generic doors stay with core doors.
    doortypes = two_da_root / "doortypes.2da"
    if doortypes.exists():
        for row in parse_2da(doortypes):
            model = norm(row.get("Model"))
            tileset = norm(row.get("TileSet"))
            if not model or not tileset:
                continue
            owner = TILE_PREFIX_OWNERS.get(tileset.lower())
            if owner:
                add_prefix(prefixes, model, owner)

    # Door, loadscreen, skybox ownership.
    for stem in ("genericdoors", "doortypes", "door"):
        add_exact(exact, stem, "sw_door")
    add_exact(exact, "loadscreens", "sw_load")
    add_exact(exact, "skyboxes", "sw_skybox")

    # Ability/gameplay icons.
    icon_tables = [
        ("feat.2da", ["ICON"]),
        ("spells.2da", ["IconResRef"]),
        ("effecticons.2da", ["Icon"]),
    ]
    for file_name, columns in icon_tables:
        path = two_da_root / file_name
        if not path.exists():
            continue
        for row in parse_2da(path):
            for column in columns:
                add_exact(exact, norm(row.get(column)), "sw_ability")

    ui_icon_tables = [
        ("actions.2da", ["Icon"]),
        ("classes.2da", ["Icon"]),
        ("domains.2da", ["Icon"]),
        ("iprp_spells.2da", ["Icon"]),
        ("racialtypes.2da", ["Icon"]),
        ("skills.2da", ["Icon"]),
        ("traps.2da", ["IconResRef"]),
    ]
    for file_name, columns in ui_icon_tables:
        path = two_da_root / file_name
        if not path.exists():
            continue
        for row in parse_2da(path):
            for column in columns:
                add_exact(exact, norm(row.get(column)), "sw_ui")

    # Item and weapon defaults.
    baseitems = two_da_root / "baseitems.2da"
    if baseitems.exists():
        for row in parse_2da(baseitems):
            owner = "sw_weapon" if is_weapon_baseitem(row) else "sw_item"
            add_exact(exact, norm(row.get("DefaultIcon")), owner)
            add_exact(exact, norm(row.get("DefaultModel")), owner)
            item_class = norm(row.get("ItemClass"))
            if item_class:
                add_prefix(prefixes, item_class, owner)
                add_prefix(prefixes, f"i{item_class}", owner)

    # VFX model/prog references. Keep audio in sw_sound through extension rules.
    for file_name in [
        "visualeffects.2da",
        "progfx.2da",
        "vfx_fire_forget.2da",
        "vfx_persistent.2da",
        "damagehitvisual.2da",
        "areaeffects.2da",
    ]:
        path = two_da_root / file_name
        if not path.exists():
            continue
        for row in parse_2da(path):
            for column, value in row.items():
                if column == "_row":
                    continue
                ref = norm(value)
                if not ref or ref.isdigit() or ref.startswith("0x"):
                    continue
                if ref.lower() in {"****", "default", "line", "sphere", "path", "hand", "head"}:
                    continue
                if re.match(r"^(v[a-z]|fx|fnf|imp|com|dur|beam|sdr|spr|sco|vco|vpr)", ref):
                    add_exact(exact, ref, "sw_vfx")
                    add_prefix(prefixes, ref, "sw_vfx")

    return exact, prefixes


def collect_active_resources(
    order: list[str], paths: dict[str, Path]
) -> tuple[dict[str, dict], list[dict]]:
    winners: dict[str, dict] = {}
    losers: list[dict] = []
    for hak_index, hak in enumerate(order):
        directory = paths.get(hak)
        if not directory or not directory.exists():
            continue
        for path in directory.rglob("*"):
            if not path.is_file():
                continue
            ext = path.suffix.lower().lstrip(".")
            if ext not in RESOURCE_EXTS:
                continue
            stem = path.stem.lower()
            key = f"{stem}.{ext}"
            record = {
                "key": key,
                "stem": stem,
                "ext": ext,
                "source_hak": hak,
                "source_path": rel(path),
                "abs_path": path,
                "size": path.stat().st_size,
                "hak_index": hak_index,
                "file_name": path.name,
            }
            if key in winners:
                loser = dict(record)
                loser["winner_path"] = winners[key]["source_path"]
                loser["winner_hak"] = winners[key]["source_hak"]
                losers.append(loser)
                continue
            winners[key] = record
    return winners, losers


def match_prefix(stem: str, sorted_prefixes: list[str]) -> str | None:
    for prefix in sorted_prefixes:
        if not stem.startswith(prefix):
            continue
        if len(stem) == len(prefix):
            return prefix
        next_char = stem[len(prefix)]
        if next_char in {"_", "-", "."} or next_char.isdigit() or len(prefix) >= 4:
            return prefix
    return None


def initial_owner(record: dict, exact: dict[str, str], sorted_prefixes: list[str], prefixes: dict[str, str]) -> str | None:
    stem = record["stem"]
    ext = record["ext"]
    source_hak = record["source_hak"]

    if ext == "bmu":
        return "sw_music"
    if ext in {"wav", "ssf"}:
        return "sw_sound"
    if ext == "gui":
        return "sw_ui"
    if ext == "shd":
        return "sw_shader"
    if ext == "uti":
        return "sw_item"
    if source_hak == "sw_portrait":
        return "sw_portrait"
    if source_hak == "sw_music":
        return "sw_music"
    if source_hak == "swlor2_gui":
        return "sw_ui"
    if source_hak == "swlor2_add_doors":
        return "sw_door"
    if source_hak == "swlor2_add_loads":
        return "sw_load"
    if source_hak == "swlor2_add_skies":
        return "sw_skybox"

    if ext == "2da":
        return "sw_2da"

    if stem in exact:
        return exact[stem]

    if stem.startswith("mi_"):
        minimap_ref = stem[3:]
        prefix = match_prefix(minimap_ref, sorted(TILE_PREFIX_OWNERS.keys(), key=len, reverse=True))
        if prefix:
            return TILE_PREFIX_OWNERS[prefix]

    part_owner = player_part_owner(stem)
    if part_owner:
        return part_owner

    if re.match(r"^p[mf][a-z0-9]{2,4}$", stem):
        return "sw_pt_root"
    if re.match(r"^pr[0-5]_", stem) or stem.startswith(("ife_", "is_", "ief_")):
        return "sw_ability"
    if stem.startswith(("ipf", "ipm")):
        return "sw_pt_root"
    if stem.startswith("ihelm"):
        return "sw_pt_helm"
    if stem.startswith(("iit", "ia_", "it_", "it-", "iprp_", "ip_")):
        return "sw_item"
    if stem.startswith(("iw", "w_", "wp_")):
        return "sw_weapon"
    if stem.startswith(("fnt", "gui", "cg_", "arrow_", "curs", "ctl_")):
        return "sw_ui"
    if stem.startswith(("sky", "zky", "swtatsky")):
        return "sw_skybox"
    if stem.startswith(("ls_", "load_")):
        return "sw_load"
    if stem.startswith(("fx", "vfx", "vdr", "vpr", "sdr", "spr", "sco", "vco", "beam", "imp_", "com_", "dur_", "vff", "vim", "zep", "vps", "vpj", "vpm")):
        return "sw_vfx"
    if stem.startswith(("ctpsky", "swconsky")):
        return "sw_skybox"
    if stem.startswith(("nw2", "gn_", "t1do")):
        return "sw_door"
    if stem.startswith(("ir_", "isk_", "inv_")):
        return "sw_ui"
    if stem.startswith(("doa_",)):
        return "sw_item"

    prefix = match_prefix(stem, sorted_prefixes)
    if prefix:
        return prefixes[prefix]

    prefix = match_prefix(stem, sorted(TILE_PREFIX_OWNERS.keys(), key=len, reverse=True))
    if prefix:
        return TILE_PREFIX_OWNERS[prefix]

    if stem.startswith("c_"):
        return "sw_cr_creature"
    if stem.startswith("plc"):
        return "sw_plc"
    if stem.startswith(CEP_CREATURE_PREFIXES):
        return "sw_cr_creature"
    if stem.startswith(WEAPON_PREFIXES):
        return "sw_weapon"
    if stem.startswith(SWTOR_PLACEABLE_PREFIXES):
        return "sw_plc_swtor"
    if stem.startswith(MODERN_PLACEABLE_PREFIXES):
        return "sw_plc_mdrn"
    if stem.startswith(CEP_PLACEABLE_PREFIXES):
        return "sw_plc_cep"
    if stem.startswith(LEGACY_PLACEABLE_PREFIXES):
        return "sw_plc"

    if source_hak in {"swlor2_ini", "swlor2_itp"}:
        prefix = match_prefix(stem, sorted(TILE_HAKS.keys(), key=len, reverse=True))
        if prefix:
            return TILE_HAKS[prefix]
        return "sw_palette"
    if source_hak == "swlor2_dwk":
        prefix = match_prefix(stem, sorted(TILE_HAKS.keys(), key=len, reverse=True))
        if prefix:
            return TILE_HAKS[prefix]
        return "sw_door"
    if source_hak == "swlor2_set":
        prefix = match_prefix(stem, sorted(TILE_HAKS.keys(), key=len, reverse=True))
        if prefix:
            return TILE_HAKS[prefix]
    if source_hak in {"swlor2_add_tiles", "swlor2_ext_tiles", "swlor2_wok"}:
        prefix = match_prefix(stem, sorted(TILE_HAKS.keys(), key=len, reverse=True))
        if prefix:
            return TILE_HAKS[prefix]
        prefix = match_prefix(stem, sorted(TILE_PREFIX_OWNERS.keys(), key=len, reverse=True))
        if prefix:
            return TILE_PREFIX_OWNERS[prefix]
        if source_hak == "swlor2_ext_tiles":
            return "sw_door"
        if source_hak == "swlor2_add_tiles":
            return "sw_palette"

    if source_hak.startswith("swlor2_core"):
        return "sw_plc_cep"
    if source_hak in {"swlor2_dds", "swlor2_tga", "swlor2_mdl", "swlor2_mtr", "swlor2_txi", "swlor2_pwk"}:
        return "sw_plc"
    if source_hak in TARGET_HAKS:
        return source_hak

    return None


STRING_CACHE: dict[Path, set[str]] = {}


def extract_resource_strings(path: Path) -> set[str]:
    if path in STRING_CACHE:
        return STRING_CACHE[path]
    try:
        data = path.read_bytes()
    except OSError:
        STRING_CACHE[path] = set()
        return STRING_CACHE[path]
    STRING_CACHE[path] = {match.group(0).decode("ascii", "ignore").lower() for match in RESREF_RE.finditer(data)}
    return STRING_CACHE[path]


def assign_resources(winners: dict[str, dict], exact: dict[str, str], prefixes: dict[str, str]) -> tuple[dict[str, str], list[dict]]:
    sorted_prefixes = sorted(prefixes.keys(), key=len, reverse=True)
    assignments: dict[str, str] = {}
    reasons: dict[str, str] = {}

    for key, record in winners.items():
        owner = initial_owner(record, exact, sorted_prefixes, prefixes)
        if owner:
            assignments[key] = owner
            reasons[key] = "direct"

    stem_to_keys: dict[str, list[str]] = defaultdict(list)
    for key, record in winners.items():
        stem_to_keys[record["stem"]].append(key)

    changed = True
    while changed:
        changed = False
        for key, owner in list(assignments.items()):
            record = winners[key]
            if record["ext"] not in MODEL_DEP_EXTS:
                continue
            for ref in extract_resource_strings(record["abs_path"]):
                for dep_key in stem_to_keys.get(ref, []):
                    dep = winners[dep_key]
                    if dep["ext"] not in TEXTURE_EXTS:
                        continue
                    if dep_key in assignments:
                        continue
                    assignments[dep_key] = owner
                    reasons[dep_key] = f"dependency:{key}"
                    changed = True

    unassigned = []
    for key, record in winners.items():
        if key not in assignments:
            unassigned.append(record)

    for key, reason in reasons.items():
        winners[key]["reason"] = reason

    return assignments, unassigned


def write_csv(path: Path, rows: list[dict], fields: list[str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)


def write_audit(winners: dict[str, dict], assignments: dict[str, str], losers: list[dict], unassigned: list[dict], mode: str) -> None:
    AUDIT_DIR.mkdir(parents=True, exist_ok=True)

    move_rows = []
    for key, record in sorted(winners.items()):
        target = assignments.get(key, "")
        target_path = f"SWLOR_Haks/{target}/{record['file_name']}" if target else ""
        move_rows.append(
            {
                "key": key,
                "source_hak": record["source_hak"],
                "source_path": record["source_path"],
                "target_hak": target,
                "target_path": target_path,
                "size": record["size"],
                "reason": record.get("reason", ""),
            }
        )

    write_csv(
        AUDIT_DIR / f"move_audit_{mode}.csv",
        move_rows,
        ["key", "source_hak", "source_path", "target_hak", "target_path", "size", "reason"],
    )
    write_csv(
        AUDIT_DIR / f"duplicate_losers_{mode}.csv",
        losers,
        ["key", "source_hak", "source_path", "winner_hak", "winner_path", "size"],
    )
    write_csv(
        AUDIT_DIR / f"unassigned_{mode}.csv",
        unassigned,
        ["key", "source_hak", "source_path", "size"],
    )

    sizes = defaultdict(int)
    counts = Counter()
    for key, target in assignments.items():
        sizes[target] += int(winners[key]["size"])
        counts[target] += 1

    summary = {
        "mode": mode,
        "target_hak_count": len(TARGET_HAKS),
        "active_resource_count": len(winners),
        "duplicate_loser_count": len(losers),
        "unassigned_count": len(unassigned),
        "targets": [
            {
                "hak": hak,
                "files": counts[hak],
                "bytes": sizes[hak],
                "mb": round(sizes[hak] / 1048576, 2),
            }
            for hak in TARGET_HAKS
        ],
        "oversize_haks": [
            {"hak": hak, "mb": round(size / 1048576, 2)}
            for hak, size in sorted(sizes.items())
            if size > 500 * 1024 * 1024
        ],
    }
    (AUDIT_DIR / f"summary_{mode}.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")


def apply_moves(winners: dict[str, dict], assignments: dict[str, str], losers: list[dict]) -> None:
    loser_paths = {(REPO_ROOT / loser["source_path"]).resolve() for loser in losers}
    planned_destinations: dict[Path, str] = {}
    reserved_paths: set[Path] = set()
    move_plan = []

    for key, record in sorted(winners.items()):
        target = assignments[key]
        if target not in TARGET_HAKS:
            raise RuntimeError(f"Unexpected target hak for {key}: {target}")

        source = record["abs_path"].resolve()
        destination = (HAK_ROOT / target / record["file_name"]).resolve()
        prior_key = planned_destinations.get(destination)
        if prior_key is not None:
            raise RuntimeError(f"Multiple resources target {rel(destination)}: {prior_key}, {key}")
        planned_destinations[destination] = key

        if source == destination:
            continue

        if destination.exists() and destination not in loser_paths:
            raise RuntimeError(f"Target already exists for {key}: {rel(destination)}")

        move_destination = destination
        if destination in loser_paths:
            for index in range(1, 1000):
                candidate = destination.with_name(f".{destination.name}.hak_reorg_{index}.tmp")
                resolved_candidate = candidate.resolve()
                if (
                    resolved_candidate not in planned_destinations
                    and resolved_candidate not in loser_paths
                    and resolved_candidate not in reserved_paths
                    and not candidate.exists()
                ):
                    move_destination = resolved_candidate
                    reserved_paths.add(resolved_candidate)
                    break
            else:
                raise RuntimeError(f"Could not reserve temporary destination for {key}: {rel(destination)}")

        move_plan.append((source, destination, move_destination))

    for hak in TARGET_HAKS:
        (HAK_ROOT / hak).mkdir(parents=True, exist_ok=True)

    pending_final_moves = []
    for source, destination, move_destination in move_plan:
        move_destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(source), str(move_destination))
        if move_destination != destination:
            pending_final_moves.append((move_destination, destination))

    for loser in losers:
        path = REPO_ROOT / loser["source_path"]
        if path.exists():
            path.unlink()

    for source, destination in pending_final_moves:
        if destination.exists():
            raise RuntimeError(f"Target still exists after duplicate cleanup: {rel(destination)}")
        shutil.move(str(source), str(destination))

    for hak in sorted(OLD_SOURCE_HAKS - set(TARGET_HAKS)):
        directory = HAK_ROOT / hak
        if not directory.exists():
            continue
        for dirpath, dirnames, filenames in os.walk(directory, topdown=False):
            if filenames:
                continue
            current = Path(dirpath)
            try:
                current.rmdir()
            except OSError:
                pass


def target_hak_entries(prefix: str) -> list[dict[str, object]]:
    return [
        {
            "Name": hak,
            "Path": f"{prefix}{hak}/",
            "CompileModels": False,
        }
        for hak in TARGET_HAKS
    ]


def update_hakbuilder_tlk(config: dict, tlk_path: str) -> None:
    config["TlkPath"] = tlk_path
    if "Mod_CustomTlk" in config:
        config["Mod_CustomTlk"] = CUSTOM_TLK_NAME


def write_hakbuilder_configs() -> None:
    build_config = read_json(BUILD_CONFIG)
    update_hakbuilder_tlk(build_config, f"../SWLOR_Haks/{CUSTOM_TLK_NAME}/{CUSTOM_TLK_FILE}")
    build_config["HakList"] = target_hak_entries("../SWLOR_Haks/")
    BUILD_CONFIG.write_text(json.dumps(build_config, indent=2) + "\n", encoding="utf-8")

    sub_config = read_json(SUBMODULE_CONFIG)
    update_hakbuilder_tlk(sub_config, f"./{CUSTOM_TLK_NAME}/{CUSTOM_TLK_FILE}")
    sub_config["HakList"] = target_hak_entries("./")
    SUBMODULE_CONFIG.write_text(json.dumps(sub_config, indent=2) + "\n", encoding="utf-8")


def replace_module_custom_tlk(text: str) -> str:
    marker = '  "Mod_CustomTlk": {'
    start = text.find(marker)
    if start == -1:
        return text

    value_marker = '    "value": "'
    value_start = text.index(value_marker, start) + len(value_marker)
    value_end = text.index('"', value_start)
    return text[:value_start] + CUSTOM_TLK_NAME + text[value_end:]


def replace_module_hak_list() -> None:
    text = MODULE_IFO.read_bytes().decode("latin-1")
    marker = '  "Mod_HakList": {'
    start = text.index(marker)
    brace_start = text.index("{", start)
    depth = 0
    end = None
    for index in range(brace_start, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                end = index + 1
                break
    if end is None:
        raise RuntimeError("Could not find Mod_HakList block end.")

    entries = []
    for hak in TARGET_HAKS:
        entries.append(
            '      {\n'
            '        "__struct_id": 8,\n'
            '        "Mod_Hak": {\n'
            '          "type": "cexostring",\n'
            f'          "value": "{hak}"\n'
            "        }\n"
            "      }"
        )
    block = (
        '  "Mod_HakList": {\n'
        '    "type": "list",\n'
        '    "value": [\n'
        + ",\n".join(entries)
        + "\n"
        "    ]\n"
        "  }"
    )
    new_text = text[:start] + block + text[end:]
    new_text = replace_module_custom_tlk(new_text)
    MODULE_IFO.write_bytes(new_text.encode("latin-1"))


def validate_targets() -> None:
    too_long = [hak for hak in TARGET_HAKS if len(hak) > 16]
    if too_long:
        raise RuntimeError(f"Hak names longer than 16 chars: {too_long}")
    if len(TARGET_HAKS) != len(set(TARGET_HAKS)):
        duplicates = [hak for hak, count in Counter(TARGET_HAKS).items() if count > 1]
        raise RuntimeError(f"Duplicate target haks: {duplicates}")
    available_tiles = {path.stem.lower() for path in HAK_ROOT.rglob("*.set")}
    missing_tiles = sorted(set(TILE_HAKS) - available_tiles)
    if missing_tiles:
        raise RuntimeError(f"Tilesets in mapping but no .set file found: {missing_tiles}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true", help="Move files and update configs.")
    args = parser.parse_args()

    validate_targets()
    order = module_hak_order()
    paths = build_path_by_hak()
    for hak in OLD_SOURCE_HAKS:
        paths.setdefault(hak, HAK_ROOT / hak)
    scan_order = order + [hak for hak in sorted(OLD_SOURCE_HAKS) if hak not in order]
    exact, prefixes = collect_references()
    winners, losers = collect_active_resources(scan_order, paths)
    assignments, unassigned = assign_resources(winners, exact, prefixes)
    mode = "apply" if args.apply else "dry_run"
    write_audit(winners, assignments, losers, unassigned, mode)

    if unassigned:
        print(f"Refusing to continue: {len(unassigned)} active resources are unassigned.")
        print(f"See {rel(AUDIT_DIR / f'unassigned_{mode}.csv')}")
        return 2

    if args.apply:
        apply_moves(winners, assignments, losers)
        write_hakbuilder_configs()
        replace_module_hak_list()
        print("Applied hak source reorganization.")
    else:
        print("Dry run complete. No files moved.")
    print(f"Audit written to {rel(AUDIT_DIR)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
