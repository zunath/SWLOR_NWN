"""Space revamp Wild Space (plan section 23, W12c): the six tier-5.5 systems, their environments, the rift network, the
Rift Raiders, Wild Space loot and the credits it pays.

usage: python wildspace.py            (writes the generated C# Wild Space tuning)
       python wildspace.py --check    (exit 1 when the generated file is out of date)
       python wildspace.py --sim      (prints Wild Space combat credits per hour against tier 5 combat)

Every Wild Space number lives here. checks.py section N runs each environment through the simulator (apply_environment)
and bible_tabs.py calls wildspace_tab(Sheet) to write the Wild Space (Revamp) tab. scanning.py lists the systems as scan
areas. Draft numbers, tune in testing.

Sources:
- Tier 5.5 is the ground's top band: rank-50 gear and level-52 recipe unlocks (plan section 23). The Rift Raiders are
  level 52, their stats the ground presets carried on from level 50 (npc_catalog.extend_presets).
- Space combat credits per hour: trade.combat_credits_per_hour (the basic ships' loot, 60 kills per hour, drops at the
  40% store markdown, rare-flagged drops left out), here for the Rift Raider tables plus their stolen cargo.
"""
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import trade  # noqa: E402

REPO = Path(__file__).resolve().parents[2]

# ---------------------------------------------------------------------------------------------------------------
# Tier 5.5
# ---------------------------------------------------------------------------------------------------------------
# A Wild Space area carries its tier as the area local SPACE_TIER_TENTHS = 55. Every tier-indexed table (site cache
# credits, slicing targets, wreck and module tiers, scrap) uses the whole tier below it, CONTENT_TIER, and the area's
# WILD_SPACE local adds the half step: Rift Raiders (level 52), the Wild Space loot tables, the Wild Space only
# schematics and the 5.5 tier shown on the Scanner tab. A signature's signal uses the full 5.5.
TIER_TENTHS = 55
CONTENT_TIER = 5
TIER_LABEL = "5.5"
TIER_VARIABLE = "SPACE_TIER_TENTHS"
ENVIRONMENT_VARIABLE = "WILD_SPACE_ENVIRONMENT"
RIFT_RAIDER_LEVEL = 52

# ---------------------------------------------------------------------------------------------------------------
# Systems (working names; lore review later). Each is a Module JSON copy of an existing orbit with its docks, starlanes,
# stations and waypoints removed: reached only through rifts.
# ---------------------------------------------------------------------------------------------------------------
# (area resref, name, environment, copied from, Aurodium % of a tier 5 wreck's salvage cycles)
SYSTEMS = [
    ("space_wspulsar", "Wild Space - Vesh Pulsar", "Pulsar", "space_dathomir", 8),
    ("space_wsblkhole", "Wild Space - The Maw", "BlackHole", "space_midrim2", 8),
    ("space_wsionstorm", "Wild Space - Ion Veil", "IonStorm", "space_midrim1", 8),
    ("space_wsmagnetar", "Wild Space - Korvax Magnetar", "Magnetar", "hutlar_orbit", 8),
    ("space_wsredgiant", "Wild Space - Ember Giant", "RedGiant", "space_korriban", 8),
    ("space_wsgraveyrd", "Wild Space - Ship Graveyard", "ShipGraveyard", "space_korrdebris", 15),
]
SYSTEM_NAMES = {r: n for r, n, _, _, _ in SYSTEMS}
# A Wild Space system holds this many hidden site signatures besides its rifts, as an orbit does.
SITE_SIGNATURES = 3
# Each system has its own creature table (W11o: the same Rift Raiders, plus the system's own ace, rewards.WILD_ACES), so
# an ace belongs to one system: SPACE_WILD_SPACE_<ENVIRONMENT>.
CREATURE_SPAWN_TABLE_PREFIX = "SPACE_WILD_SPACE_"


def spawn_table(environment):
    return CREATURE_SPAWN_TABLE_PREFIX + re.sub(r"(?<!^)(?=[A-Z])", "_", environment).upper()
RESOURCE_SPAWN_TABLE = "SPACE_RESOURCES_KORRIBAN_DEBRIS"  # tier 5 ores (Gostian, Ardanium)

# ---------------------------------------------------------------------------------------------------------------
# Environments: StatType changes on every ship in the system (players and NPCs), applied on entering the area and
# removed on leaving it. Sized so no environment moves a same-level fight more than about +-10% (checks.py section N).
# ---------------------------------------------------------------------------------------------------------------
ENVIRONMENT_IDS = {"Pulsar": 1, "BlackHole": 2, "IonStorm": 3, "Magnetar": 4, "RedGiant": 5, "ShipGraveyard": 6}
ENVIRONMENT_NAMES = {"Pulsar": "Pulsar", "BlackHole": "Black Hole", "IonStorm": "Ion Storm", "Magnetar": "Magnetar",
                     "RedGiant": "Red Giant", "ShipGraveyard": "Ship Graveyard"}
# New StatTypes (from 1280; other space StatTypes use 1400-1452, clear of crafting's 1168-1267) and the existing ones the environments use.
NEW_STATS = {
    "SpaceShieldDamageTakenPercentAdjustment": 1280,
    "SpaceCapacitorRegenPercentAdjustment": 1281,
    "SpaceEmDamagePercentAdjustment": 1282,
    "SpaceAbilityDamagePercentAdjustment": 1283,
    "SpaceCapacitorCostPercentAdjustment": 1284,
}
# Stats where a negative value is the good side (a penalty is then a positive value).
BENEFICIAL_WHEN_NEGATIVE = {"SpaceShieldDamageTakenPercentAdjustment", "SpaceCapacitorCostPercentAdjustment"}
STAT_TEXT = {
    "SpaceShieldDamageTakenPercentAdjustment": "shield damage taken",
    "SpaceCapacitorRegenPercentAdjustment": "capacitor regen",
    "SpaceEmDamagePercentAdjustment": "EM damage",
    "SpaceAbilityDamagePercentAdjustment": "ability damage (overcharged shots and volleys)",
    "SpaceCapacitorCostPercentAdjustment": "module capacitor use",
    "MovementSpeedPercentAdjustment": "speed",
    "SpaceAccuracyPercentAdjustment": "Accuracy",
    "DamageDealtPercentAdjustment": "damage",
    "EvasionPercentAdjustment": "Evasion",
    "SpaceSalvageYieldBonus": "salvage items per cycle",
    "SpaceRareTraitChance": "rare-trait chance on salvaged components",
    "SpaceScanStrengthPercent": "scan strength",
}
FLAT_STATS = {"SpaceSalvageYieldBonus", "SpaceRareTraitChance"}
ENVIRONMENTS = {
    "Pulsar": [("SpaceShieldDamageTakenPercentAdjustment", -12), ("SpaceCapacitorRegenPercentAdjustment", -25)],
    "BlackHole": [("MovementSpeedPercentAdjustment", 25), ("SpaceAccuracyPercentAdjustment", -4)],
    "IonStorm": [("SpaceEmDamagePercentAdjustment", 25), ("SpaceShieldDamageTakenPercentAdjustment", 15)],
    "Magnetar": [("DamageDealtPercentAdjustment", 4), ("EvasionPercentAdjustment", -1)],
    "RedGiant": [("SpaceAbilityDamagePercentAdjustment", 20), ("SpaceCapacitorCostPercentAdjustment", 20)],
    "ShipGraveyard": [("SpaceSalvageYieldBonus", 1), ("SpaceRareTraitChance", 5), ("SpaceScanStrengthPercent", -25)],
}
# The Raider Prowler's signature: "Scans Wild Space 50% faster and ignores environment penalties."
PROWLER_TAG = "sd_raiderprowler"
PROWLER_SCAN_PERCENT = 50
# How long an environment's modifiers last on a ship; they are removed when it leaves, so this only has to outlast a stay.
ENVIRONMENT_SECONDS = 30 * 24 * 3600


def is_penalty(stat, amount):
    return amount > 0 if stat in BENEFICIAL_WHEN_NEGATIVE else amount < 0


def effect_text(stat, amount):
    unit = "" if stat in FLAT_STATS else "%"
    return f"{'+' if amount > 0 else ''}{amount}{unit} {STAT_TEXT[stat]}"


def environment_text(env):
    return ", ".join(effect_text(s, a) for s, a in ENVIRONMENTS[env])


# ---------------------------------------------------------------------------------------------------------------
# Environments in the simulator (checks.py section N). Every ship in the fight takes the environment. Stats the
# simulator does not model (speed, abilities, salvage, scanning) change nothing here.
# ---------------------------------------------------------------------------------------------------------------
def apply_environment(players, npcs, env):
    """Copies of the fight's ships with the environment on every one of them."""
    players = [json.loads(json.dumps(p)) for p in players]
    npcs = [json.loads(json.dumps(n)) for n in npcs]
    ships = players + npcs
    for stat, amount in ENVIRONMENTS.get(env, []):
        factor = 1 + amount / 100
        for ship in ships:
            if stat == "SpaceShieldDamageTakenPercentAdjustment":
                # Every shot lands on a ship in the system, so every weapon strips shields harder or softer.
                for w in ship["weapons"]:
                    w["shieldDamageMultiplier"] = round(w.get("shieldDamageMultiplier", 1.0) * factor, 3)
            elif stat == "SpaceCapacitorRegenPercentAdjustment":
                ship["capacitorRegenPerSecond"] = round(ship["capacitorRegenPerSecond"] * factor, 2)
            elif stat == "SpaceAccuracyPercentAdjustment":
                rating = ship["accuracy"] + ship["weapons"][0]["accuracy"] + 3 * ship["pilot"]["agi"]
                ship["accuracy"] += round(amount / 100 * rating)
            elif stat == "EvasionPercentAdjustment":
                ship["evasion"] += round(amount / 100 * (ship["evasion"] + 3 * ship["pilot"]["agi"]))
            elif stat == "SpaceEmDamagePercentAdjustment":
                # Every EM shot does more: WildSpaceRules.EnvironmentDamagePercent joins the ship's EM damage bonus.
                bonus = ship.get("damageBonusPercent", {"em": 0, "thermal": 0, "explosive": 0})
                ship["damageBonusPercent"] = dict(bonus, em=bonus["em"] + amount)
            elif stat == "DamageDealtPercentAdjustment":
                # Every ship's damage-dealt stat, applied to its rolled damage (SpaceCombatMath.ApplyDamageDealt).
                ship["damageDealtPercent"] = ship.get("damageDealtPercent", 0) + amount
            elif stat == "SpaceCapacitorCostPercentAdjustment":
                ship["weapons"] = [dict(w, capacitorPerCycle=max(1, int(w["capacitorPerCycle"] * factor + 0.5))
                                        if w["capacitorPerCycle"] else 0) for w in ship["weapons"]]
    return players, npcs


SIMULATED = {"SpaceShieldDamageTakenPercentAdjustment", "SpaceCapacitorRegenPercentAdjustment", "SpaceAccuracyPercentAdjustment",
             "EvasionPercentAdjustment", "SpaceEmDamagePercentAdjustment", "SpaceCapacitorCostPercentAdjustment",
             "DamageDealtPercentAdjustment"}
FIGHT_BAND = 10  # percent: an environment may move a same-level fight by about this much either way


def environment_rows(runs=150):
    """
    Each environment's change to fight length: a named-fit Fighter (build A at rank 45, every module at its named numbers)
    against each level 52 Normal role, the environment on both ships. Returns (baseline means, rows by environment).
    """
    import model as m
    import npc_catalog
    npc_catalog.extend_presets(RIFT_RAIDER_LEVEL)
    player = m.player_ship("Fighter", 45, m.FIT["Fighter"], build="A", named=True)
    base = {role: m.run(m.scenario([player], [m.npc_ship(role, RIFT_RAIDER_LEVEL)]), runs=runs)["mean"]
            for role in m.NORMAL_ROLES}
    rows = {}
    for env, effects in ENVIRONMENTS.items():
        deltas = {}
        for role in m.NORMAL_ROLES:
            players, npcs = apply_environment([player], [m.npc_ship(role, RIFT_RAIDER_LEVEL)], env)
            deltas[role] = 100 * (m.run(m.scenario(players, npcs), runs=runs)["mean"] / base[role] - 1)
        worst = max(deltas, key=lambda k: abs(deltas[k]))
        skipped = [STAT_TEXT[s] for s, _ in effects if s not in SIMULATED]
        rows[env] = dict(mean=sum(deltas.values()) / len(deltas), worst=deltas[worst], role=worst, deltas=deltas,
                         note=", ".join(skipped) if skipped else "-")
    return base, rows


# ---------------------------------------------------------------------------------------------------------------
# Rifts (hidden Rift signatures, plan section 24): each Wild Space system keeps about CONNECTIONS_PER_SYSTEM open, to a
# random known orbit or another Wild Space system. Rift state lives in memory: a restart seeds a fresh network, and
# ships flying in Wild Space at the restart lose their space position like every ship and relaunch from their dock.
# ---------------------------------------------------------------------------------------------------------------
CONNECTIONS_PER_SYSTEM = 2
ORBIT_DESTINATION_PERCENT = 60          # otherwise another Wild Space system, if one has room
LIFETIME_HOURS = (4, 24)                # uniform
NETWORK_CHECK_SECONDS = 15
REPLACEMENT_SECONDS = 300               # a collapsed connection's replacement opens this long after it
WARNING_SECONDS = 120                   # ships in the Wild Space system(s) it serves are warned this long before
EJECT_HULL_PERCENT = 25                 # of max hull, after the shield is stripped; never below 1 hull
# The known orbits a rift can open in: the eleven open orbits (not the Small/Medium zones, which are dead ends). Eshan
# (the Eshan merge, master #2344) is last, so no earlier index moves.
KNOWN_ORBITS = ["viscaraorbit", "moncalaorbit", "hutlar_orbit", "tatooineorbit", "narshadorbit", "space_dathomir",
                "dantooineorbit", "space_korriban", "space_midrim1", "space_midrim2", "eshanorbit"]
# Size class: (weight, largest hull through it, mass budget)
SIZE_CLASSES = {
    "Small": (50, "Medium", 30),
    "Medium": (35, "Large", 60),
    "Large": (15, "Capital", 120),
}
# Mass one passage spends, by hull size (a Small rift takes 30 Small or 15 Medium passages; a Large one 8 Capital).
PASSAGE_MASS = {"Small": 1, "Medium": 2, "Large": 5, "Capital": 15}

# ---------------------------------------------------------------------------------------------------------------
# Rift Raiders: a level 52 pirate coalition flying every W5b role and the pirate hulls, copied from the tier 5 ships
# (the W5b and W7b way). Their wrecks spill stolen cargo.
# ---------------------------------------------------------------------------------------------------------------
# (tag, name, copied from, appearance, legacy ship item, loot tables)
_NORMAL_LOOT = [("SPACE_BASIC_WS", 60, 3), ("SPACE_BASIC_WS_RARES", 5, 1)]
_HEAVY_LOOT = [("SPACE_ELITE_WS", 66, 5)]
_BOSS_LOOT = [("SPACE_ELITE_WS", 100, 5), ("SPACE_BOSS_WS", 100, 1)]
RIFT_RAIDERS = [
    ("rrfighter", "Rift Raider Cutlass", "t5pirftr", 10204, "NPC_Fighter5", _NORMAL_LOOT),
    ("rrinterceptor", "Rift Raider Prowler", "t5pirint", 10221, "NPC_Interceptor5", _NORMAL_LOOT),
    ("rrbomber", "Rift Raider Bomber", "t5bomber", 10113, "NPC_Bomber5", _NORMAL_LOOT),
    ("rrsniper", "Rift Raider Sniper", "t5sniper", 10114, "NPC_Fighter5", _NORMAL_LOOT),
    ("rrewar", "Rift Raider Jammer", "t5ewar", 10215, "NPC_Interceptor5", _NORMAL_LOOT),
    ("rrminelayer", "Rift Raider Minelayer", "t5minelayer", 10222, "NPC_Bomber5", _NORMAL_LOOT),
    ("rrgunship", "Rift Raider Marauder", "t5pirgun", 10196, "NPC_Gunship5", _HEAVY_LOOT),
    ("rrlogistics", "Rift Raider Tender", "t5logistics", 10160, "NPC_Gunship5", _HEAVY_LOOT),
    ("rrcarrier", "Rift Raider Mothership", "t5carrier", 10133, "NPC_Gunship5", _BOSS_LOOT),
]
SKIN_RESREF = "rrskin"
# Spawn weights in every Wild Space system: the open-orbit weights (SpaceSpawnDefinition.AddFactionTier), with the
# bomber at the role ships' weight so all eight behaviors fly together.
SPAWN_WEIGHTS = {"rrfighter": 38400, "rrinterceptor": 19200, "rrgunship": 3840, "rrbomber": 1920, "rrewar": 1920,
                 "rrsniper": 1920, "rrminelayer": 1920, "rrlogistics": 1440, "rrcarrier": 480}

# Stolen cargo (plan section 13): a Rift Raider wreck spills cargo pods this often; one good, its units rolled.
# (commodity, weight, minimum units, maximum units): about 300 base credits a spill.
STOLEN_CARGO_PERCENT = 20
STOLEN_CARGO = [
    ("Moonspice", 30, 1, 2),             # contraband: banned by every lawful authority
    ("WeaponsCrates", 15, 1, 1),         # restricted
    ("EncryptedDataChips", 15, 1, 3),    # restricted
    ("ReactorCores", 15, 1, 2),
    ("MedicalSupplies", 15, 3, 8),
    ("CzerkaMachineParts", 10, 3, 6),
]
# What a spill is worth to the credits model: sold at about 60% of base (the run home, flooding and a black market
# for the contraband).
CARGO_SALE_FACTOR = 0.60

# ---------------------------------------------------------------------------------------------------------------
# Loot: the Rift Raider tables (SpaceLootTableDefinition, Wild Space), the salvage of wrecks in Wild Space and the
# tier 5.5 Rift Key (W15's Rift Runs use it; it drops now at a low rate).
# ---------------------------------------------------------------------------------------------------------------
RIFT_KEY = "rift_key_t55"
RIFT_KEY_NAME = "Rift Key (Tier 5.5)"
RIFT_KEY_COST = 2000
# Extra rare-find rewards of an elite or boss wreck in Wild Space, after the W9b design, ship and torpedo shares: a
# Module Schematic (a named rank-50 recipe unlock) or Prototype Tech. Wild Space schematic pools include the Rift
# Raider ships' Design and Ship Schematics.
RARE_FIND_EXTRAS = {"Elite": dict(module=5, proto=10), "Boss": dict(module=10, proto=15)}
# Percentage points added to a Wild Space wreck's salvaged component rare-trait roll.
RARE_TRAIT_BONUS = 10
# Wild Space combat should pay about this much of tier 5 combat: the higher-stakes zone (PvP module drops, rifts).
TARGET_PREMIUM = 1.2


def cargo_value():
    """Expected base credits of one spill."""
    base = {c[1]: c[3] for c in trade.COMMODITIES}
    total = sum(w for _, w, _, _ in STOLEN_CARGO)
    return sum(w / total * (lo + hi) / 2 * base[name] for name, w, lo, hi in STOLEN_CARGO)


def contraband_share():
    total = sum(w for _, w, _, _ in STOLEN_CARGO)
    banned = {c[1]: c[5] for c in trade.COMMODITIES}
    return sum(w for name, w, _, _ in STOLEN_CARGO if len(banned[name]) == 3) / total


def combat_rate():
    """Wild Space combat credits per hour: trade.py's baseline on the Rift Raider basic table, plus stolen cargo."""
    basic = trade.loot_tables()["SPACE_BASIC_WS"]
    loot = 3 * 0.60 * trade.expected_value(basic)
    cargo = STOLEN_CARGO_PERCENT / 100 * cargo_value() * CARGO_SALE_FACTOR
    return loot, cargo, (loot + cargo) * trade.KILLS_PER_HOUR


def report():
    _, t5 = trade.combat_credits_per_hour(CONTENT_TIER)
    loot, cargo, rate = combat_rate()
    print(f"Wild Space combat: {rate:,.0f}/h ({loot:.1f} loot + {cargo:.1f} stolen cargo per kill), "
          f"T5 combat {t5:,.0f}/h: {100 * rate / t5:.0f}% (target {TARGET_PREMIUM:.0%})")
    print(f"  stolen cargo: {STOLEN_CARGO_PERCENT}% of kills, {cargo_value():.0f} base credits a spill, "
          f"{contraband_share():.0%} contraband")


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/wildspace.py. Do not edit by hand: change wildspace.py and regenerate.
// </auto-generated>
"""


def tuning_cs():
    q = json.dumps
    systems = "\n".join(
        f"            new WildSpaceSystem {{ AreaResref = {q(r)}, Name = {q(n)}, Environment = WildSpaceEnvironment.{e}, "
        f"SpawnTable = {q(spawn_table(e))} }},"
        for r, n, e, _, _ in SYSTEMS)
    effects = "\n".join(
        f"            [WildSpaceEnvironment.{env}] = new[] {{ "
        + ", ".join(f"new WildSpaceEffect {{ Stat = StatType.{s}, Amount = {a}, Penalty = {'true' if is_penalty(s, a) else 'false'} }}"
                    for s, a in eff) + " },"
        for env, eff in ENVIRONMENTS.items())
    names = "\n".join(f"            [WildSpaceEnvironment.{env}] = {q(name)}," for env, name in ENVIRONMENT_NAMES.items())
    sizes = "\n".join(
        f"            [RiftSizeClass.{k}] = new RiftSizeDetail {{ Weight = {w}, MaxHull = ShipSize.{h}, MassBudget = {m} }},"
        for k, (w, h, m) in SIZE_CLASSES.items())
    mass = ", ".join(f"[ShipSize.{k}] = {v}" for k, v in PASSAGE_MASS.items())
    orbits = ", ".join(q(o) for o in KNOWN_ORBITS)
    raiders = ", ".join(q(r[0]) for r in RIFT_RAIDERS)
    cargo = "\n".join(
        f"            new StolenCargoEntry {{ Commodity = CommodityType.{c}, Weight = {w}, MinimumUnits = {lo}, MaximumUnits = {hi} }},"
        for c, w, lo, hi in STOLEN_CARGO)
    extras = ", ".join(f"[WreckDifficulty.{k}] = ({v['module']}, {v['proto']})" for k, v in RARE_FIND_EXTRAS.items())
    return (HEADER + "using System.Collections.Generic;\nusing SWLOR.Game.Server.Service.StatService;\n"
            "using SWLOR.Game.Server.Service.TradeService;\n\n"
            "namespace SWLOR.Game.Server.Service.SpaceService\n{\n"
            "    /// <summary>\n"
            "    /// The Wild Space numbers (plan section 23): the tier 5.5 systems, their environments, the rift network, the\n"
            "    /// Rift Raiders' stolen cargo and the Wild Space salvage rewards.\n"
            "    /// </summary>\n"
            "    public static class WildSpaceTuning\n    {\n"
            f"        public const int TierTenths = {TIER_TENTHS};\n"
            f"        public const int ContentTier = {CONTENT_TIER};\n"
            f"        public const string TierLabel = {q(TIER_LABEL)};\n"
            f"        public const string TierVariable = {q(TIER_VARIABLE)};\n"
            f"        public const string EnvironmentVariable = {q(ENVIRONMENT_VARIABLE)};\n"
            f"        public const int SiteSignatures = {SITE_SIGNATURES};\n\n"
            "        /// <summary>The six Wild Space systems, one per environment. They have no starlanes or docks.</summary>\n"
            "        public static readonly IReadOnlyList<WildSpaceSystem> Systems = new List<WildSpaceSystem>\n        {\n"
            f"{systems}\n        }};\n\n"
            "        public static readonly IReadOnlyDictionary<WildSpaceEnvironment, string> EnvironmentNames = "
            "new Dictionary<WildSpaceEnvironment, string>\n        {\n"
            f"{names}\n        }};\n\n"
            "        /// <summary>The stat changes each environment puts on every ship in its system.</summary>\n"
            "        public static readonly IReadOnlyDictionary<WildSpaceEnvironment, WildSpaceEffect[]> Effects = "
            "new Dictionary<WildSpaceEnvironment, WildSpaceEffect[]>\n        {\n"
            f"{effects}\n        }};\n\n"
            f"        public const string ProwlerTag = {q(PROWLER_TAG)};\n"
            f"        public const int ProwlerScanPercent = {PROWLER_SCAN_PERCENT};\n"
            f"        public const float EnvironmentSeconds = {ENVIRONMENT_SECONDS}f;\n\n"
            "        // Rifts\n"
            f"        public const int ConnectionsPerSystem = {CONNECTIONS_PER_SYSTEM};\n"
            f"        public const int OrbitDestinationPercent = {ORBIT_DESTINATION_PERCENT};\n"
            f"        public const int MinimumLifetimeHours = {LIFETIME_HOURS[0]};\n"
            f"        public const int MaximumLifetimeHours = {LIFETIME_HOURS[1]};\n"
            f"        public const int NetworkCheckSeconds = {NETWORK_CHECK_SECONDS};\n"
            f"        public const int ReplacementSeconds = {REPLACEMENT_SECONDS};\n"
            f"        public const int WarningSeconds = {WARNING_SECONDS};\n"
            f"        public const int EjectHullPercent = {EJECT_HULL_PERCENT};\n\n"
            "        /// <summary>The known orbits a rift can open in.</summary>\n"
            f"        public static readonly string[] KnownOrbits = {{ {orbits} }};\n\n"
            "        public static readonly IReadOnlyDictionary<RiftSizeClass, RiftSizeDetail> SizeClasses = "
            "new Dictionary<RiftSizeClass, RiftSizeDetail>\n        {\n"
            f"{sizes}\n        }};\n\n"
            "        /// <summary>The mass one passage spends from a rift's budget, by hull size.</summary>\n"
            f"        public static readonly IReadOnlyDictionary<ShipSize, int> PassageMass = new Dictionary<ShipSize, int> {{ {mass} }};\n\n"
            "        // Rift Raiders\n"
            f"        public static readonly string[] RiftRaiderTags = {{ {raiders} }};\n"
            f"        public const int StolenCargoPercent = {STOLEN_CARGO_PERCENT};\n\n"
            "        /// <summary>What a Rift Raider wreck's stolen cargo can be: one good, its units rolled.</summary>\n"
            "        public static readonly IReadOnlyList<StolenCargoEntry> StolenCargo = new List<StolenCargoEntry>\n        {\n"
            f"{cargo}\n        }};\n\n"
            "        // Salvage in Wild Space\n"
            f"        public const string RiftKeyResref = {q(RIFT_KEY)};\n"
            "        /// <summary>Module Schematic and Prototype Tech percent of an elite or boss wreck's rare finds in Wild Space.</summary>\n"
            "        public static readonly IReadOnlyDictionary<WreckDifficulty, (int ModulePercent, int PrototypePercent)> RareFindExtras = "
            f"new Dictionary<WreckDifficulty, (int ModulePercent, int PrototypePercent)> {{ {extras} }};\n"
            f"        public const int RareTraitBonusPercent = {RARE_TRAIT_BONUS};\n"
            "    }\n}\n")


WRITES = {"SWLOR.Game.Server/Service/SpaceService/WildSpaceTuning.cs": tuning_cs}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab
# ---------------------------------------------------------------------------------------------------------------
def wildspace_tab(Sheet):
    _, environments = environment_rows()
    s = Sheet("Wild Space (Revamp)", [30, 16, 14, 14, 14, 14, 14, 60])
    s.title("Wild Space (Revamp): tier 5.5 systems, environments, rifts, Rift Raiders and Wild Space loot")
    s.note("Dev Status: Proposed (space revamp W12c draft). Generated by tools/SpaceRevampBalance/wildspace.py; plan section 23.")
    s.blank()
    s.title("Systems (working names; reached only through rifts, no starlanes or docks)")
    s.table(["System", "Environment", "Tier", "Aurodium %", "", "", "", "Effect on every ship inside"],
            [[n, ENVIRONMENT_NAMES[e], TIER_LABEL, a, "", "", "", environment_text(e)] for _, n, e, _, a in SYSTEMS])
    s.note(f"Tier 5.5: the area local {TIER_VARIABLE} = {TIER_TENTHS}; tier-indexed tables use tier {CONTENT_TIER} and WILD_SPACE = 1 "
           f"adds the Rift Raiders (level {RIFT_RAIDER_LEVEL}), the Wild Space loot tables and schematics. A PvP death here can drop "
           "modules (plan section 9). The Raider Prowler ignores environment penalties and scans Wild Space "
           f"{PROWLER_SCAN_PERCENT}% stronger.")
    s.title("Environments in the simulator (checks.py section N): a named-fit Fighter against each level 52 Normal role")
    s.table(["Environment", "Mean change", "Worst role", "", "", "", "", "Not modeled"],
            [[ENVIRONMENT_NAMES[e], f"{d['mean']:+.1f}%", f"{d['worst']:+.1f}% ({d['role']})", "", "", "", "", d["note"]]
             for e, d in environments.items()])
    s.title("Rifts (hidden Rift signatures; Jump to Signature on a located rift crosses it)")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Connections per system", CONNECTIONS_PER_SYSTEM, "", "", "", "", "",
         f"{ORBIT_DESTINATION_PERCENT}% to a random known orbit, otherwise another Wild Space system"],
        ["Lifetime", f"{LIFETIME_HOURS[0]}-{LIFETIME_HOURS[1]} h", "", "", "", "", "", "uniform; a replacement opens "
         f"{REPLACEMENT_SECONDS // 60} minutes after a collapse"],
        ["Collapse warning", f"{WARNING_SECONDS} s", "", "", "", "", "", "to ships in the Wild Space system(s) it serves"],
        ["No exit left", f"{EJECT_HULL_PERCENT}% of max hull", "", "", "", "", "",
         "the ship loses its shield and this much hull (never below 1) and is thrown to a random known orbit"],
        ["Known orbits", len(KNOWN_ORBITS), "", "", "", "", "", ", ".join(trade.ORBIT_NAMES[o] for o in KNOWN_ORBITS)],
    ])
    s.table(["Size class", "Weight", "Largest hull", "Mass budget", "", "", "", "Notes"],
            [[k, w, h, m, "", "", "", "Only Large rifts take Capital hulls" if k == "Large" else ""] for k, (w, h, m) in SIZE_CLASSES.items()])
    s.table(["Hull", "Mass per passage", "", "", "", "", "", ""], [[k, v, "", "", "", "", "", ""] for k, v in PASSAGE_MASS.items()])
    s.title(f"Rift Raiders (level {RIFT_RAIDER_LEVEL}; stats from the ground presets carried past level 50)")
    s.table(["Creature", "Name", "Copied from", "Spawn weight", "", "", "", "Loot tables"],
            [[t, n, src, SPAWN_WEIGHTS[t], "", "", "", "; ".join(f"{x} ({c}%, {a})" for x, c, a in loot)]
             for t, n, src, _, _, loot in RIFT_RAIDERS])
    loot, cargo, rate = combat_rate()
    _, t5 = trade.combat_credits_per_hour(CONTENT_TIER)
    s.title("Stolen cargo and credits")
    s.table(["Good", "Weight", "Units", "", "", "", "", "Notes"],
            [[c, w, f"{lo}-{hi}", "", "", "", "", "contraband" if c == "Moonspice" else ""] for c, w, lo, hi in STOLEN_CARGO])
    s.table(["Measure", "Value", "", "", "", "", "", "Notes"], [
        ["Spill chance", f"{STOLEN_CARGO_PERCENT}%", "", "", "", "", "", f"{cargo_value():.0f} base credits a spill, {contraband_share():.0%} contraband"],
        ["Wild Space combat / h", round(rate), "", "", "", "", "", f"{loot:.1f} loot + {cargo:.1f} cargo per kill, 60 kills an hour"],
        ["Tier 5 combat / h", round(t5), "", "", "", "", "", f"Wild Space pays {100 * rate / t5:.0f}% (target about {TARGET_PREMIUM:.0%})"],
    ])
    s.title("Wild Space salvage")
    s.table(["Wreck", "Module Schematic %", "Prototype Tech %", "", "", "", "", "Notes"],
            [[k, v["module"], v["proto"], "", "", "", "", "of rare finds, after the W9b design, ship and torpedo shares"]
             for k, v in RARE_FIND_EXTRAS.items()])
    s.note(f"Rare-trait chance +{RARE_TRAIT_BONUS} points on every Wild Space wreck; Wild Space only Design and Ship Schematics "
           f"(the Rift Raider ships) join the rare-find and slicing pools; {RIFT_KEY_NAME} drops at a low rate for the Rift Runs (W15).")
    return s


def main():
    if "--sim" in sys.argv:
        report()
        return
    check = "--check" in sys.argv
    stale = []
    for rel, build in WRITES.items():
        text = build()
        path = REPO / rel
        if path.exists() and path.read_text(encoding="utf-8") == text:
            continue
        stale.append(rel)
        if not check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8", newline="\r\n")
    print(f"{len(SYSTEMS)} systems, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
