"""Piloting styles in the simulator (checks.py section R): each style's main damage and defense rows, with the server's
numbers (Feature/AbilityDefinition/Piloting/*, Feature/PerkDefinition/Piloting*PerkDefinition.cs) and their Piloting
unlock ranks. What the simulator cannot run (areas, damage over time, statuses on a timer, riders, threat) is listed in
checks.py section R as unvalidated.
"""
import json

# Ace (PER / AGI). Disabling Shot I-IV: (unlock rank, bonus DMG, STM, target Attack penalty %), 18 s recast.
DISABLING_SHOT = [(2, 8, 3, 10), (12, 18, 5, 12), (28, 28, 8, 15), (40, 38, 12, 18)]
DISABLING_SHOT_RECAST = 18
# Double Shot I-III: (unlock rank, bonus DMG per shot, STM), two shots, 24 s recast.
DOUBLE_SHOT = [(10, 6, 4), (30, 13, 8), (38, 20, 10)]
DOUBLE_SHOT_RECAST = 24
# Sustained Fire I-III: (unlock rank, DMG per consecutive hit, most DMG). Each weapon owns its stacks and the DMG is
# shared between the ship's energy weapons (SpaceRepeatedHits.GetShare).
SUSTAINED_FIRE = [(8, 1, 5), (22, 2, 10), (45, 3, 15)]
# Point Blank Burst I-II: (unlock rank, bonus DMG, STM), 30 s recast; an area burst, run on the target only.
POINT_BLANK_BURST = [(18, 16, 6), (32, 30, 9)]
POINT_BLANK_BURST_RECAST = 30

# Ordnance (MGT / PER). Volley I-IV: (unlock rank, bonus DMG, STM), 18 s recast; an area shot, run on the target only.
VOLLEY = [(2, 7, 3), (12, 15, 5), (28, 24, 8), (40, 32, 12)]
VOLLEY_RECAST = 18
# Flare Missile I-III: (unlock rank, bonus DMG, STM), 24 s recast; Blind for 15 s on a hit.
FLARE_MISSILE = [(10, 12, 4), (30, 24, 8), (38, 36, 10)]
FLARE_MISSILE_RECAST = 24
FLARE_BLIND_SECONDS = 15
# Concussion Barrage I-II: (unlock rank, bonus DMG, STM), 30 s recast; an area launcher burst (Dazed not modeled).
CONCUSSION_BARRAGE = [(18, 16, 6), (32, 30, 9)]
CONCUSSION_BARRAGE_RECAST = 30
# Volley's Burn (BurnStatusEffect, level 1): every 6 s for 30 s, 2-3 + the MGT modifier x 2.
BURN_PERIOD, BURN_SECONDS = 6, 30
# Shrapnel Warheads I-III: (unlock rank, fragmentation damage every 3 s for 30 s) from area launcher abilities.
SHRAPNEL_WARHEADS = [(8, 3), (22, 5), (45, 7)]
FRAGMENTATION_PERIOD, FRAGMENTATION_SECONDS = 3, 30
# Proximity Warheads (rank 5): a launcher's own cycle hit splashes this much damage this often (percent).
PROXIMITY_WARHEADS = (5, 15, 8)
# Saturation Fire (rank 25): two area launcher hits within 30 s leave the target Exposed (-15% Defense) for 30 s.
SATURATION_FIRE = (25, 15)

# Bulwark (VIT / WIL). Bulwark I-III: (unlock rank, Defense %).
BULWARK = [(8, 8), (22, 10), (45, 12)]

# Command (SOC / WIL). Focus Fire I-III: (unlock rank, damage dealt % at SOC 10, STM, recast s); it lasts 30 s, so with
# its recast at or under 30 s it is always up.
FOCUS_FIRE = [(10, 6, 4, 24), (30, 8, 6, 24), (38, 10, 9, 30)]


def best(rows, rank):
    """The highest row unlocked at this Piloting rank, or None."""
    owned = [r for r in rows if r[0] <= rank]
    return owned[-1] if owned else None


def _weapon(ship, family):
    return next((w["name"] for w in ship["weapons"] if w["family"] == family), None)


def apply(style, ship, rank):
    """A copy of a player ship with the style's modeled rows at this Piloting rank."""
    s = json.loads(json.dumps(ship))
    s["abilities"] = list(s.get("abilities", []))
    if style == "Ace":
        energy = _weapon(s, "Energy")
        row = best(DISABLING_SHOT, rank)
        if row and energy:
            s["abilities"].append(dict(name="Disabling Shot", weapon=energy, bonusDmg=row[1], stmCost=row[2],
                                       cooldownSeconds=DISABLING_SHOT_RECAST))
        row = best(DOUBLE_SHOT, rank)
        if row and energy:
            s["abilities"].append(dict(name="Double Shot", weapon=energy, bonusDmg=row[1], stmCost=row[2], shots=2,
                                       cooldownSeconds=DOUBLE_SHOT_RECAST))
        row = best(POINT_BLANK_BURST, rank)
        if row and energy:
            s["abilities"].append(dict(name="Point Blank Burst", weapon=energy, bonusDmg=row[1], stmCost=row[2],
                                       cooldownSeconds=POINT_BLANK_BURST_RECAST))
        row = best(SUSTAINED_FIRE, rank)
        if row:
            s["energyRepeatedHitDmg"], s["energyRepeatedHitMaxDmg"] = row[1], row[2]
    elif style == "Ordnance":
        launcher = _weapon(s, "Launcher")
        mgt_modifier = (s["pilot"]["mgt"] - 10) // 2
        shrapnel = best(SHRAPNEL_WARHEADS, rank)
        area_dots = [dict(name="Fragmentation", damage=shrapnel[1], periodSeconds=FRAGMENTATION_PERIOD,
                          durationSeconds=FRAGMENTATION_SECONDS)] if shrapnel else []
        burn = dict(name="Burn", damage=2 + 2 * mgt_modifier, damageSpread=1, periodSeconds=BURN_PERIOD, durationSeconds=BURN_SECONDS)
        for name, rows, recast, extra in (
                ("Volley", VOLLEY, VOLLEY_RECAST, dict(dots=[burn] + area_dots, areaLauncher=True)),
                ("Flare Missile", FLARE_MISSILE, FLARE_MISSILE_RECAST, dict(blindSeconds=FLARE_BLIND_SECONDS)),
                ("Concussion Barrage", CONCUSSION_BARRAGE, CONCUSSION_BARRAGE_RECAST, dict(dots=list(area_dots), areaLauncher=True))):
            row = best(rows, rank)
            if row and launcher:
                s["abilities"].append(dict(name=name, weapon=launcher, bonusDmg=row[1], stmCost=row[2], cooldownSeconds=recast, **extra))
        if rank >= PROXIMITY_WARHEADS[0]:
            s["launcherSplashChance"], s["launcherSplashDamage"] = PROXIMITY_WARHEADS[1], PROXIMITY_WARHEADS[2]
        if rank >= SATURATION_FIRE[0]:
            s["saturationExposedDefensePercent"] = SATURATION_FIRE[1]
    elif style == "Bulwark":
        row = best(BULWARK, rank)
        if row:  # Stat.ApplyPercentAdjustment on the Defense rating (ship Defense + VIT)
            s["defense"] = max(1, round(s["defense"] * (1 + row[1] / 100)))
    elif style == "Command":
        row = best(FOCUS_FIRE, rank)
        if row:
            s["damageDealtPercent"] = s.get("damageDealtPercent", 0) + row[1]
    else:
        raise ValueError(style)
    return s


def debuff(style, npc, rank):
    """The target with the style's debuffs that stay up: Ace's Disabling Shot (30 s on an 18 s recast) cuts its Attack."""
    n = json.loads(json.dumps(npc))
    row = best(DISABLING_SHOT, rank) if style == "Ace" else None
    if row:
        # Disarming Shot lowers the Attack rating (ship Attack + module Attack, which carries the damage attribute).
        for w in n["weapons"]:
            w["attack"] = round((n["attack"] + w["attack"]) * (1 - row[3] / 100)) - n["attack"]
    return n


def describe(style, rank):
    parts = []
    if style == "Ace":
        for label, rows in (("Disabling Shot", DISABLING_SHOT), ("Double Shot", DOUBLE_SHOT), ("Point Blank Burst", POINT_BLANK_BURST),
                            ("Sustained Fire", SUSTAINED_FIRE)):
            row = best(rows, rank)
            if row:
                parts.append(f"{label} {rows.index(row) + 1}")
    elif style == "Ordnance":
        for label, rows in (("Volley", VOLLEY), ("Flare Missile", FLARE_MISSILE), ("Concussion Barrage", CONCUSSION_BARRAGE),
                            ("Shrapnel Warheads", SHRAPNEL_WARHEADS)):
            row = best(rows, rank)
            if row:
                parts.append(f"{label} {rows.index(row) + 1}")
        if rank >= PROXIMITY_WARHEADS[0]:
            parts.append("Proximity Warheads")
        if rank >= SATURATION_FIRE[0]:
            parts.append("Saturation Fire")
    elif style == "Bulwark":
        row = best(BULWARK, rank)
        if row:
            parts.append(f"Bulwark {BULWARK.index(row) + 1} (+{row[1]}% Defense)")
    elif style == "Command":
        row = best(FOCUS_FIRE, rank)
        if row:
            parts.append(f"Focus Fire {FOCUS_FIRE.index(row) + 1} (+{row[1]}% damage)")
    return ", ".join(parts) or "nothing yet"
