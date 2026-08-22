#!/usr/bin/env python3
"""Generate the JSON blueprints and area locals for the Slicing feature.

The generator deliberately owns only the resources listed in this file. It clones
known-good module blueprints so GFF field shapes remain compatible with the module
packer, then replaces gameplay properties with the reviewed fixed budgets.
"""

from __future__ import annotations

import copy
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UTI = ROOT / "Module" / "uti"
UTP = ROOT / "Module" / "utp"
GIT = ROOT / "Module" / "git"

LOCKBOX_ICON_BY_TIER = {
    1: 153,  # Green security case.
    2: 156,  # Blue security case.
    3: 152,  # Yellow security case.
    4: 154,  # Orange security case.
    5: 155,  # Red security case.
}


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def save(path: Path, data: dict) -> None:
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def set_loc(field: dict, text: str) -> None:
    field.pop("id", None)
    field["value"] = {"0": text}


def configure_item(data: dict, resref: str, name: str, description: str, tag: str | None = None) -> dict:
    result = copy.deepcopy(data)
    set_loc(result["LocalizedName"], name)
    set_loc(result["Description"], description)
    set_loc(result["DescIdentified"], description)
    result["TemplateResRef"]["value"] = resref
    result["Tag"]["value"] = tag or resref
    result["Plot"]["value"] = 0
    result["Stolen"]["value"] = 0
    result["Identified"]["value"] = 1
    return result


def prop(property_name: int, cost_table: int, cost_value: int, subtype: int = 0) -> dict:
    return {
        "__struct_id": 0,
        "ChanceAppear": {"type": "byte", "value": 100},
        "CostTable": {"type": "byte", "value": cost_table},
        "CostValue": {"type": "word", "value": cost_value},
        "Param1": {"type": "byte", "value": 255},
        "Param1Value": {"type": "byte", "value": 0},
        "PropertyName": {"type": "word", "value": property_name},
        "Subtype": {"type": "word", "value": subtype},
    }


def local_int(name: str, value: int) -> dict:
    return {
        "__struct_id": 0,
        "Name": {"type": "cexostring", "value": name},
        "Type": {"type": "dword", "value": 1},
        "Value": {"type": "int", "value": value},
    }


def local_string(name: str, value: str) -> dict:
    return {
        "__struct_id": 0,
        "Name": {"type": "cexostring", "value": name},
        "Type": {"type": "dword", "value": 3},
        "Value": {"type": "cexostring", "value": value},
    }


WEAPONS = [
    # resref, name, tier, source, template, exceptional
    ("slw_quickdraw", "Quickdraw Holdout", 1, "lockbox", "fld_pistol", True),
    ("slw_sabutility", "Saboteur Utility Blade", 1, "lockbox", "fld_knife", False),
    ("slw_cratehook", "Cratehook Spear", 1, "lockbox", "fld_spear", False),
    ("stw_surveycoil", "Surveyor Coil Rifle", 1, "terminal", "fld_rifle", False),
    ("stw_groundloop", "Ground-Loop Baton", 1, "terminal", "fld_staff", False),
    ("slw_sidewinder", "Sidewinder Compact", 2, "lockbox", "fld_pistol", False),
    ("slw_viperclaw", "Viper-Circuit Claws", 2, "lockbox", "fld_katar", True),
    ("slw_crosswind", "Crosswind Boarding Twinblade", 2, "lockbox", "fld_twinblade", False),
    ("stw_quietcarb", "Quieting Carbine", 2, "terminal", "fld_rifle", False),
    ("stw_phasereturn", "Phase-Return Blades", 2, "terminal", "fld_shuriken", False),
    ("slw_debtmarker", "Debtmarker Pistol", 3, "lockbox", "fld_pistol", False),
    ("slw_sealknife", "Counterfeit-Seal Knife", 3, "lockbox", "fld_knife", False),
    ("slw_blackroute", "Blackroute Greatblade", 3, "lockbox", "fld_greatsword", False),
    ("stw_longwatch", "Longwatch Coil Rifle", 3, "terminal", "fld_rifle", True),
    ("stw_relaybreak", "Relaybreaker Staff", 3, "terminal", "fld_staff", False),
    ("slw_coldchamber", "Cold-Chamber Sidearm", 4, "lockbox", "fld_pistol", False),
    ("slw_bastiontal", "Bastion Talons", 4, "lockbox", "fld_katar", True),
    ("slw_deaddrop", "Dead-Drop War Spear", 4, "lockbox", "fld_spear", False),
    ("stw_nulllattice", "Null-Lattice Suppressor", 4, "terminal", "fld_rifle", False),
    ("stw_orbitshear", "Orbit-Shear Throwers", 4, "terminal", "fld_shuriken", False),
    ("slw_lastwitness", "Last Witness Pistol", 5, "lockbox", "fld_pistol", False),
    ("slw_gloamknife", "Shadow Gloamsteel Knife", 5, "lockbox", "fld_knife", False),
    ("slw_vaultbreak", "Vaultbreaker Greatblade", 5, "lockbox", "fld_greatsword", False),
    ("stw_ghostline", "Ghostline Experimental Rifle", 5, "terminal", "fld_rifle", True),
    ("stw_zerostate", "Zero-State Twin Electroblade", 5, "terminal", "fld_twinblade", False),
]


GEAR = [
    # resref, name, tier, source, template, identity, exceptional
    ("slg_dockcipher", "Dockside Cipher Gloves", 1, "lockbox", "agent_gloves", "slicer", False),
    ("slg_ventmouse", "Vent-Mouse Treads", 1, "lockbox", "agent_boots", "infiltrator", False),
    ("slg_bitterdose", "Bitterglass Doser Belt", 1, "lockbox", "agent_belt", "poisoner", False),
    ("stg_contwatch", "Continuity Watch Visor", 1, "terminal", "agent_cap", "trapper", False),
    ("stg_borrowcred", "Borrowed-Credential Bracer", 1, "terminal", "cara_bracer", "tradecraft", True),
    ("slg_falsebottom", "False-Bottom Keycloak", 2, "lockbox", "agent_cloak", "slicer", False),
    ("slg_tripline", "Tripline Field Gloves", 2, "lockbox", "agent_gloves", "trapper", False),
    ("slg_smugcourtesy", "Smuggler's Courtesy Belt", 2, "lockbox", "agent_belt", "tradecraft", False),
    ("stg_hushmesh", "Hush-Mesh Tunic", 2, "terminal", "agent_tunic", "infiltrator", True),
    ("stg_raivordose", "Raivor Microdoser Bracer", 2, "terminal", "cara_bracer", "poisoner", False),
    ("slg_counterwatch", "Counterwatch Cloak", 3, "lockbox", "agent_cloak", "slicer", True),
    ("slg_tombfilter", "Tombspore Filter Boots", 3, "lockbox", "agent_boots", "poisoner", False),
    ("slg_gravewire", "Gravewire Utility Belt", 3, "lockbox", "agent_belt", "trapper", False),
    ("stg_echokey", "Echo-Key Sensor", 3, "terminal", "cara_bracer", "tradecraft", False),
    ("stg_diploghost", "Diplomatic Ghost Visor", 3, "terminal", "agent_cap", "infiltrator", False),
    ("slg_blackledger", "Black-Ledger Lock Gloves", 4, "lockbox", "agent_gloves", "slicer", False),
    ("slg_rimeinject", "Rimevenom Injector Belt", 4, "lockbox", "agent_belt", "poisoner", False),
    ("slg_ashline", "Ashline Hushcloak", 4, "lockbox", "agent_cloak", "infiltrator", False),
    ("stg_nullfoot", "Null-Footprint Tunic", 4, "terminal", "agent_tunic", "tradecraft", False),
    ("stg_sabotvisor", "Sabotage Pattern Visor", 4, "terminal", "agent_cap", "trapper", True),
    ("slg_moonstep", "Moonless-Step Boots", 5, "lockbox", "agent_boots", "infiltrator", False),
    ("slg_deadfall", "Deadfall Field Gloves", 5, "lockbox", "agent_gloves", "trapper", False),
    ("slg_lastcover", "Last-Cover Cloak", 5, "lockbox", "agent_cloak", "tradecraft", True),
    ("stg_causalkey", "Causal-Key Bracer", 5, "terminal", "cara_bracer", "slicer", False),
    ("stg_nightroot", "Nightroot Rebreather", 5, "terminal", "agent_cap", "poisoner", False),
]


SMITH_OUTPUTS = [
    ("slc_stitchglv", "Stitchplate Lock Gloves", 1, "agent_gloves", "slicer"),
    ("slc_falsevisor", "False-Face Field Visor", 2, "agent_cap", "tradecraft"),
    ("slc_quietboots", "Quietstep Reinforced Boots", 3, "agent_boots", "infiltrator"),
    ("slc_dropcloak", "Dead-Drop Armored Cloak", 4, "agent_cloak", "trapper"),
    ("slc_breachhar", "Blacksite Breach Harness", 5, "agent_tunic", "slicer"),
]


SCHEMATICS = [
    ("slbp_stitchglv", "Blueprint: Stitchplate Lock Gloves", 691),
    ("slbp_quietjerky", "Recipe: Quietwatch Jerky", 2266),
    ("slbp_falsevisor", "Blueprint: False-Face Field Visor", 692),
    ("slbp_dustcakes", "Recipe: Dustveil Travel Cakes", 2267),
    ("slbp_quietboots", "Blueprint: Quietstep Reinforced Boots", 693),
    ("slbp_tombbroth", "Recipe: Tombwalker Broth", 2268),
    ("slbp_dropcloak", "Blueprint: Dead-Drop Armored Cloak", 694),
    ("slbp_snowstew", "Recipe: Snowblind Hunter's Stew", 2269),
    ("slbp_breachhar", "Blueprint: Blacksite Breach Harness", 695),
    ("slbp_nightres", "Recipe: Night March Reserve", 2270),
    ("stbp_copfuse", "Blueprint: Copper Trace Fuse", 5049),
    ("stbp_rustterm", "Blueprint: Rustline Data Terminal", 1518),
    ("stbp_whispven", "Formula: Whisperthorn Concentrate", 6011),
    ("stbp_braidfuse", "Blueprint: Braided Trace Fuse", 5050),
    ("stbp_ciphcab", "Blueprint: Cipherfile Cabinet", 1519),
    ("stbp_glassven", "Formula: Glassfang Concentrate", 6012),
    ("stbp_phasefuse", "Blueprint: Phase Trace Fuse", 5051),
    ("stbp_listmon", "Blueprint: Listening Post Monitor", 1520),
    ("stbp_tombven", "Formula: Tombspore Concentrate", 6013),
    ("stbp_cryofuse", "Blueprint: Cryo Trace Fuse", 5052),
    ("stbp_ghostcon", "Blueprint: Ghost-Channel Console", 1521),
    ("stbp_rimeven", "Formula: Rimevenom Concentrate", 6014),
    ("stbp_nullfuse", "Blueprint: Null Trace Fuse", 5053),
    ("stbp_blackstat", "Blueprint: Blacksite Analysis Station", 1522),
    ("stbp_nightven", "Formula: Nightroot Concentrate", 6015),
]


FIELD_NOTES = [
    (2105, "Sootbelly Mirekit"), (2007, "Azurehorn Kargath"), (2112, "Strayfang Kavor"),
    (2045, "Duneshag Bantha"), (2072, "Ironmaw Bastionback"), (2014, "Blinkstep Vekara"),
    (2086, "Phaseleg Silkstalker"), (2020, "Brassjaw Pyralisk"), (2131, "Venomspike Laigrek"),
    (2058, "Gilded Mirewyrm"), (2073, "Jadeclaw Vyrkol"), (2053, "Frostmaw Glacieron"),
    (2006, "Ashen Moonprowler"), (2133, "Vermilion Ravager"), (2103, "Silverveil Aerolith"),
]


TOOLS = [
    ("slt_ratchet", "Ratchet Bypass Pin", 1, 1, "Makes the next clockwise rotation free."),
    ("slt_servo", "Reversible Servo Key", 2, 2, "Aligns one selected tile to its recovered orientation for free."),
    ("slt_shunt", "Phase-Shunt Fork", 3, 3, "Makes the next adjacent swap free."),
    ("slt_splice", "Mnemonic Trace Splice", 4, 4, "Rewinds the last two actions and refunds their trace."),
    ("slt_lattice", "Null-Signature Lattice", 5, 5, "Makes the next three circuit actions free."),
    ("stt_sampler", "Continuity Sampler", 6, 1, "Identifies whether the selected tile belongs to the route."),
    ("stt_spectro", "Junction Spectrograph", 7, 2, "Identifies route membership and the selected tile's correct orientation."),
    ("stt_echo", "Forward-Echo Decoder", 8, 3, "Reveals the next two route signatures after a selected route tile."),
    ("stt_overlay", "Route-Overlay Prism", 9, 4, "Reveals every route tile without revealing orientations."),
    ("stt_oracle", "Core-Pattern Oracle", 10, 5, "Reveals the route and three correct orientations."),
]


FUSES = [
    ("trace_fuse_1", "Copper Trace Fuse", 1), ("trace_fuse_2", "Braided Trace Fuse", 2),
    ("trace_fuse_3", "Phase Trace Fuse", 3), ("trace_fuse_4", "Cryo Trace Fuse", 4),
    ("trace_fuse_5", "Null Trace Fuse", 5),
]


FOODS = [
    ("food_quietwatch", "Quietwatch Jerky", 1, 6),
    ("food_dustveil", "Dustveil Travel Cakes", 2, 9),
    ("food_tombwalk", "Tombwalker Broth", 3, 20),
    ("food_snowblind", "Snowblind Hunter's Stew", 4, 17),
    ("food_nightmarch", "Night March Reserve", 5, 23),
]


CONCENTRATES = [
    ("conc_poison_1", "Whisperthorn Concentrate", 1),
    ("conc_poison_2", "Glassfang Concentrate", 2),
    ("conc_poison_3", "Tombspore Concentrate", 3),
    ("conc_poison_4", "Rimevenom Concentrate", 4),
    ("conc_poison_5", "Nightroot Concentrate", 5),
]


STRUCTURES = [
    (431, "Rustline Data Terminal", "slc_rustterm", 6030),
    (432, "Cipherfile Cabinet", "slc_ciphcab", 30702),
    (433, "Listening Post Monitor", "slc_listmon", 7351),
    (434, "Ghost-Channel Console", "slc_ghostcon", 21450),
    (435, "Blacksite Analysis Station", "slc_blackstat", 30612),
]


TERMINAL_AREAS = {
    "czs220_maintlvl": 1, "nanostation015": 1, "viscarawildlands": 1,
    "viscara_wwnorth": 2, "viscaradeepmount": 2, "v_cox_base": 2,
    "korr_ravine": 3, "korr_cavern": 3, "korr_crypt_zil": 3,
    "hutlar_qion": 4, "pw_ar_narslum": 4, "tat_anc_hillydes": 4,
    "dan_jantacaves": 5, "dath_mountains": 5, "tat_wormden": 5,
}


def level_for_tier(tier: int, exceptional: bool) -> int:
    return (tier - 1) * 10 + (5 if exceptional else 0)


def weapon_damage(template: str, tier: int) -> int:
    budgets = {
        "fld_pistol": [7, 15, 27, 34, 40], "fld_knife": [6, 14, 25, 32, 38],
        "fld_spear": [8, 16, 28, 35, 41], "fld_rifle": [9, 17, 29, 36, 42],
        "fld_staff": [7, 15, 27, 34, 40], "fld_katar": [7, 15, 27, 34, 40],
        "fld_twinblade": [8, 16, 28, 35, 41], "fld_shuriken": [6, 14, 25, 32, 38],
        "fld_greatsword": [9, 16, 28, 35, 41],
    }
    return budgets[template][tier - 1]


def preserve_weapon_scaffolding(template: dict) -> list[dict]:
    result = []
    for item_property in template["PropertiesList"]["value"]:
        property_name = item_property["PropertyName"]["value"]
        if property_name in (61, 98, 134):
            result.append(copy.deepcopy(item_property))
    return result


def weapon_skill_requirement(template: dict, required_rank: int) -> dict:
    requirements = [
        item_property
        for item_property in template["PropertiesList"]["value"]
        if item_property["PropertyName"]["value"] == 131
    ]
    if len(requirements) != 1:
        template_resref = template["TemplateResRef"]["value"]
        raise ValueError(
            f"{template_resref} must declare exactly one weapon skill requirement; found {len(requirements)}"
        )

    requirement = copy.deepcopy(requirements[0])
    requirement["CostValue"]["value"] = required_rank
    return requirement


def make_weapons() -> None:
    attack = [1, 3, 6, 9, 12]
    readiness = [1, 1, 2, 3, 4]
    for resref, name, tier, source, template_resref, exceptional in WEAPONS:
        template = load(UTI / f"{template_resref}.uti.json")
        origin = "sealed lockbox" if source == "lockbox" else "burned field terminal"
        description = (
            f"A fixed-pattern {name.lower()} recovered from a {origin}. Its tuned internals favor decisive "
            "field handling without relying on unstable random modifications."
        )
        item = configure_item(template, resref, name, description)
        properties = [prop(93, 34, weapon_damage(template_resref, tier))]
        properties.extend(preserve_weapon_scaffolding(template))
        properties.append(prop(111, 45, attack[tier - 1] + (1 if exceptional and tier > 1 else 0)))
        properties.append(prop(118, 42, readiness[tier - 1]))
        properties.append(weapon_skill_requirement(template, level_for_tier(tier, exceptional)))
        item["PropertiesList"]["value"] = properties
        item["AddCost"]["value"] = 100 * tier
        item["Cost"]["value"] = 100 * tier
        save(UTI / f"{resref}.uti.json", item)


IDENTITY_PROPERTIES = {
    "slicer": ((141, 41), (137, 41)),
    "infiltrator": ((136, 41), (117, 41)),
    "poisoner": ((140, 41), (118, 42)),
    "trapper": ((138, 41), (139, 41)),
    "tradecraft": ((118, 42), (136, 41)),
}


def build_gear(resref: str, name: str, tier: int, template_resref: str, identity: str,
               exceptional: bool, description: str) -> None:
    template = load(UTI / f"{template_resref}.uti.json")
    item = configure_item(template, resref, name, description)
    level = level_for_tier(tier, exceptional)
    primary = tier + (1 if exceptional else 0)
    secondary = max(1, tier - 1)
    first, second = IDENTITY_PROPERTIES[identity]
    item["PropertiesList"]["value"] = [
        prop(94, 35, tier + (1 if exceptional and tier > 1 else 0), 1),
        prop(94, 35, max(1, tier - 1), 2),
        prop(first[0], first[1], primary),
        prop(second[0], second[1], secondary),
        prop(131, 48, level, 6),
    ]
    item["AddCost"]["value"] = 90 * tier
    item["Cost"]["value"] = 90 * tier
    save(UTI / f"{resref}.uti.json", item)


def make_gear() -> None:
    for resref, name, tier, source, template, identity, exceptional in GEAR:
        origin = "sealed lockbox" if source == "lockbox" else "field terminal cache"
        description = (
            f"Purpose-built clandestine equipment recovered from a {origin}. {name} follows a fixed "
            f"{identity} profile and requires Armor training rather than Espionage rank."
        )
        build_gear(resref, name, tier, template, identity, exceptional, description)

    for resref, name, tier, template, identity in SMITH_OUTPUTS:
        description = (
            f"A craftable fixed-pattern {identity} design learned from a rare slicing schematic. "
            "It accepts one standard armor enhancement."
        )
        build_gear(resref, name, tier, template, identity, False, description)


def make_schematics() -> None:
    template = load(UTI / "bpaebroth.uti.json")
    for resref, name, recipe_id in SCHEMATICS:
        description = f"Rare recovered instructions. Use this item to permanently learn {name.split(': ', 1)[1]}."
        item = configure_item(template, resref, name, description, "RECIPE")
        item["VarTable"]["value"] = [local_string("RECIPES", str(recipe_id))]
        save(UTI / f"{resref}.uti.json", item)


def make_field_notes() -> None:
    template = load(UTI / "fnote_2017.uti.json")
    for note_id, beast_name in FIELD_NOTES:
        resref = f"fnote_{note_id}"
        description = (
            f"Recovered field research documenting the incubation of the {beast_name}. "
            "Use it to record the mutation methods in your Incubation codex."
        )
        item = configure_item(template, resref, f"Field Note: {beast_name}", description, "KEY_ITEM")
        item["VarTable"]["value"] = [local_int("KEY_ITEM_ID", note_id)]
        save(UTI / f"{resref}.uti.json", item)


def make_tools() -> None:
    template = load(UTI / "poison_vial_1.uti.json")
    for resref, name, tool_type, tier, effect in TOOLS:
        description = f"A consumable slicing tool. {effect} Works on security tier {tier} or lower; one tool may be used per attempt."
        item = configure_item(template, resref, name, description, "SLICING_TOOL")
        item["PropertiesList"]["value"] = []
        item["VarTable"] = {"type": "list", "value": [local_int("SLICING_TOOL_TYPE", tool_type), local_int("SLICING_TOOL_TIER", tier)]}
        item["StackSize"]["value"] = 1
        save(UTI / f"{resref}.uti.json", item)

    for resref, name, tier in FUSES:
        description = f"A crafted tier {tier} trace fuse. Prime it in the slicing interface to gain +1 trace on the first move."
        item = configure_item(template, resref, name, description, "SLICING_TOOL")
        item["PropertiesList"]["value"] = []
        item["VarTable"] = {"type": "list", "value": [local_int("SLICING_TOOL_TYPE", 11), local_int("SLICING_TOOL_TIER", tier)]}
        save(UTI / f"{resref}.uti.json", item)


def make_foods_and_concentrates() -> None:
    food_template = load(UTI / "ss_skewer.uti.json")
    for resref, name, tier, food_subtype in FOODS:
        description = (
            "A 30-minute field provision prepared from hunted ingredients and tiered herbs. "
            "It accepts one standard food enhancement."
        )
        item = configure_item(food_template, resref, name, description, "FOOD")
        activation = copy.deepcopy(food_template["PropertiesList"]["value"][0])
        item["PropertiesList"]["value"] = [activation, prop(106, 45, tier + 1, food_subtype)]
        save(UTI / f"{resref}.uti.json", item)

    poison_template = load(UTI / "poison_vial_1.uti.json")
    for resref, name, tier in CONCENTRATES:
        description = (
            f"A concentrated tier {tier} venom formula. One vial applies 10 charges and snapshots an additional "
            f"{tier * 10}% Poison Bonus potency when applied to a melee or thrown weapon."
        )
        item = configure_item(poison_template, resref, name, description)
        save(UTI / f"{resref}.uti.json", item)


def make_structures() -> None:
    item_template = load(UTI / "structure_0085.uti.json")
    placeable_template = load(UTP / "holonetterminal.utp.json")
    for structure_id, name, placeable_resref, appearance in STRUCTURES:
        item_resref = f"structure_{structure_id:04d}"
        description = "A tradeable property structure with +1 item storage and one standard structure enhancement slot when crafted."
        item = configure_item(item_template, item_resref, name, description)
        save(UTI / f"{item_resref}.uti.json", item)

        placeable = copy.deepcopy(placeable_template)
        placeable["TemplateResRef"]["value"] = placeable_resref
        placeable["Tag"]["value"] = placeable_resref
        set_loc(placeable["LocName"], name)
        set_loc(placeable["Description"], description)
        placeable["Appearance"]["value"] = appearance
        placeable["OnUsed"]["value"] = ""
        placeable["Useable"]["value"] = 0
        placeable["Plot"]["value"] = 0
        save(UTP / f"{placeable_resref}.utp.json", placeable)


def make_world_terminals() -> None:
    template = load(UTP / "holonetterminal.utp.json")
    for tier in range(1, 6):
        resref = f"slice_term_{tier}"
        terminal = copy.deepcopy(template)
        terminal["TemplateResRef"]["value"] = resref
        terminal["Tag"]["value"] = "SlicingTerminal"
        set_loc(terminal["LocName"], "Sealed Field Terminal")
        set_loc(terminal["Description"], "A neutral field terminal with a sealed local cache. Its security tier is not externally marked.")
        terminal["OnUsed"]["value"] = "slice_terminal"
        terminal["Plot"]["value"] = 1
        terminal["Useable"]["value"] = 1
        terminal["VarTable"] = {"type": "list", "value": [local_int("SLICING_TIER", tier), local_int("SLICING_INTEGRITY", 100)]}
        save(UTP / f"{resref}.utp.json", terminal)


def add_terminal_area_locals() -> None:
    for area_resref, tier in TERMINAL_AREAS.items():
        path = GIT / f"{area_resref}.git.json"
        table_name = "SLICING_TERMINAL_SPAWN_TABLE_ID"
        count_name = "SLICING_TERMINAL_SPAWN_COUNT"
        raw = path.read_text(encoding="utf-8")
        if table_name in raw or count_name in raw:
            current = load(path).get("VarTable", {}).get("value", [])
            values = {entry["Name"]["value"]: entry["Value"]["value"] for entry in current}
            if values.get(table_name) == f"SLICING_TERMINAL_T{tier}" and values.get(count_name) == 1:
                continue
            raise ValueError(f"{path.name} contains conflicting slicing terminal locals")

        entries = [
            local_string(table_name, f"SLICING_TERMINAL_T{tier}"),
            local_int(count_name, 1),
        ]
        entries_text = ",\n".join(
            "      " + json.dumps(entry, indent=2).replace("\n", "\n      ")
            for entry in entries
        )

        # Area GIT resources are large and contain many serialized floating-point
        # values. Re-serializing the parsed document creates thousands of unrelated
        # formatting changes, so append only the two root VarTable entries as text.
        marker = '\n  "VarTable": {'
        table_start = raw.rfind(marker)
        if table_start >= 0:
            value_start = raw.find('\n    "value": [', table_start)
            if value_start < 0:
                raise ValueError(f"Could not locate the root VarTable value in {path.name}")
            array_open = raw.find("[", value_start)
            depth = 0
            in_string = False
            escaped = False
            array_close = -1
            for index in range(array_open, len(raw)):
                char = raw[index]
                if in_string:
                    if escaped:
                        escaped = False
                    elif char == "\\":
                        escaped = True
                    elif char == '"':
                        in_string = False
                    continue
                if char == '"':
                    in_string = True
                elif char == "[":
                    depth += 1
                elif char == "]":
                    depth -= 1
                    if depth == 0:
                        array_close = index
                        break
            if array_close < 0:
                raise ValueError(f"Could not find the end of the root VarTable in {path.name}")

            previous = array_close - 1
            while previous > array_open and raw[previous].isspace():
                previous -= 1
            if previous == array_open:
                raw = raw[:array_open + 1] + "\n" + entries_text + "\n    " + raw[array_close:]
            else:
                raw = raw[:previous + 1] + ",\n" + entries_text + raw[previous + 1:]
        else:
            root_close = raw.rfind("\n}")
            if root_close < 0:
                raise ValueError(f"Could not locate the root object end in {path.name}")
            table_text = (
                ',\n  "VarTable": {\n'
                '    "type": "list",\n'
                '    "value": [\n'
                f"{entries_text}\n"
                '    ]\n'
                '  }'
            )
            raw = raw[:root_close] + table_text + raw[root_close:]

        path.write_text(raw, encoding="utf-8", newline="")


def update_legacy_accessories_and_lockboxes() -> None:
    for tier in range(1, 6):
        level = tier * 10 - 5
        for slot in ("neck", "belt", "ring"):
            path = UTI / f"espn_{slot}_{tier}.uti.json"
            data = load(path)
            properties = data["PropertiesList"]["value"]
            properties[:] = [p for p in properties if p["PropertyName"]["value"] != 131]
            properties.append(prop(131, 48, level, 6))
            save(path, data)

        path = UTI / f"lockbox_t{tier}.uti.json"
        data = load(path)
        description = (
            "Right-click this item in your inventory and choose Activate Item to begin the Slicing minigame. "
            f"Requires Slicing rank {tier}. Its circuit seed, failures, and integrity "
            "remain attached to the box when traded."
        )
        data["BaseItem"]["value"] = 24  # MiscSmall: portable item, not an inventory container.
        data["PropertiesList"]["value"] = [prop(15, 3, 13, 335)]  # Activate Item (self).
        icon = LOCKBOX_ICON_BY_TIER[tier]
        data["ModelPart1"]["value"] = icon
        data["xModelPart1"]["value"] = icon
        set_loc(data["Description"], description)
        set_loc(data["DescIdentified"], description)
        save(path, data)


def validate() -> None:
    all_resrefs = [row[0] for row in WEAPONS + GEAR + SMITH_OUTPUTS + SCHEMATICS + TOOLS + FUSES + FOODS + CONCENTRATES]
    all_resrefs += [f"fnote_{note_id}" for note_id, _ in FIELD_NOTES]
    all_resrefs += [f"structure_{structure_id:04d}" for structure_id, *_ in STRUCTURES]
    all_resrefs += [row[2] for row in STRUCTURES]
    all_resrefs += [f"slice_term_{tier}" for tier in range(1, 6)]
    too_long = [resref for resref in all_resrefs if len(resref) > 16]
    if too_long:
        raise ValueError(f"NWN resrefs exceed 16 characters: {too_long}")
    missing = [resref for resref in all_resrefs if not ((UTI / f"{resref}.uti.json").exists() or (UTP / f"{resref}.utp.json").exists())]
    if missing:
        raise ValueError(f"Generated resources are missing: {missing}")


def main() -> None:
    make_weapons()
    make_gear()
    make_schematics()
    make_field_notes()
    make_tools()
    make_foods_and_concentrates()
    make_structures()
    make_world_terminals()
    add_terminal_area_locals()
    update_legacy_accessories_and_lockboxes()
    validate()
    print("Generated Slicing content successfully.")


if __name__ == "__main__":
    main()
