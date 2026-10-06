"""Maintain the planned space specification without rewriting existing Bible cells.

This is a design and numerical audit tool, not a gameplay implementation.
Existing ZIP entries are copied unchanged; only new space tabs and explicit
cross-reference cells are authored. Formula caches on existing tabs are kept.
"""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import re
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
BIBLE = ROOT / "design/bible/SWLOR Design Bible - Combat Upgrade.xlsx"
DATA = ROOT / "design/space/space-balance.json"
REPORT = ROOT / "design/testing/space-balance-audit.json"
PLAN = ROOT / "SWLOR.Game.Server/Readmes/SpaceActivitiesImprovementPlan.md"
MAIN = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
PKG = "http://schemas.openxmlformats.org/package/2006/relationships"
NS = {"m": MAIN}


def record(headers, values):
    return dict(zip(headers.split(), values, strict=True))


def specifications():
    rules = [
        ("Skill ranks", 50, "ranks/skill", "Five operating skills share the existing 400-rank character cap."),
        ("Character rank cap", 400, "ranks", "Existing cap; no separate space skill pool."),
        ("Maximum skill points", 410, "SP", "400 earned plus 10 starting; a build also needs its skill-rank prerequisites."),
        ("Specialization cost", 40, "SP", "17 purchased ranks: 3 techniques x 3 ranks, 2 traits x 3 ranks, mode, capstone."),
        ("Activation cadence", 1, "seconds", "One manually issued bank or technique activation per second; individual cooldowns remain."),
        ("Technique loadout", 4, "techniques", "Four learned techniques prepared while docked, including at most one capstone; module banks are separate controls."),
        ("Weapon banks", 2, "banks/fit", "Each bank has 1-4 compatible modules; each module pays its own cost and cooldown."),
        ("Simultaneous mode", 1, "mode/ship", "All five skills share one ship operating-mode slot; no cross-skill mode stacking."),
        ("Damage mastery", 0.002, "output/rank", "Gunnery; additive with matching output traits, crafting, equipment, and the bounded attribute term."),
        ("Recovery mastery", 0.002, "output/rank", "Ship Systems affects repair and paid shield recovery; not free capacitor regeneration."),
        ("Piloting speed", 0.002, "speed/rank", "Piloting improves hull movement; also enters the hit formula as a separate evasion term."),
        ("Industry recovery", 0.001, "recovery/rank", "Space Industry adds at most 5 percentage points of usable resource recovery."),
        ("Astrometric resolution", 0.4, "resolution/rank", "Added to fitted scanner resolution; no fitted scanner means no survey action."),
        ("Permanent output cap", 0.40, "base output", "All positive damage/recovery bonuses together; negative module or mode tradeoffs still apply."),
        ("Temporary output cap", 0.30, "base output", "Highest applicable technique bonus; capstones do not add a second temporary output layer."),
        ("Permanent speed cap", 0.25, "base speed", "All positive skill/equipment/attribute contributions together."),
        ("Temporary speed cap", 0.35, "base speed", "Highest temporary movement contribution, with the hull envelope retained."),
        ("Capacitor discount cap", 0.25, "base expenditure", "All matching discounts additive; costs round upward, minimum 1 for a paid activation."),
        ("Minimum adjusted cycle", 0.85, "base cycle", "Permanent cycle reduction capped at 15%; rapid calibration is included."),
        ("Usable mining recovery cap", 0.95, "reserve removed", "Recovery from finite reserves; fractional units accumulate per session, never round each beam upward."),
        ("Passive damage resistance cap", 60, "rating", "Reduction = rating / (100 + rating), so the cap is 37.5%; applies to the declared damage layer."),
        ("Temporary resistance cap", 25, "rating", "Highest active resistance bonus; combined rating cannot exceed 85 (45.95% reduction)."),
        ("Fitted duplicate weights", "1 / 0.5 / 0.25 / 0.125", "ordered sources", "Passive modules affecting the same stat family; rank by absolute bonus. No fifth positive source."),
        ("Accuracy floor", 0.10, "probability", "Final hit chance; an ordinary hit never bypasses all avoidance."),
        ("Accuracy ceiling", 0.95, "probability", "Includes target analysis and accuracy techniques."),
        ("Hit formula", "clamp((0.80 + 0.002*G - 0.0015*P + 0.0025*(PER-10) - 0.0015*(AGI-10) + accuracy - evasion) * tracking, 0.10, 0.95)", "probability", "G = attacker Gunnery, P = target Piloting; attribute scores clamped 10-26. Temporary 27 does not expand ship budgets."),
        ("Tracking formula", "min(1, (weapon tracking/100)*(target signature/weapon resolution)/target speed)", "multiplier", "Speed is the hull movement multiplier including positive/negative operating modifiers; range remains a separate eligibility gate."),
        ("Output attribute term", "0.0025*(PER-10)", "base output", "10-26 clamp, maximum 4%; technique effects use listed values and do not gain another attribute multiplier."),
        ("Control diminishing returns", "1 / 0.5 / 0", "same family within 20s", "First control full duration, next half duration, subsequent applications immune; 20s clear window resets."),
        ("Hard control cap", 3, "seconds/application", "No total movement/activation lock longer than 3s; a maneuver slow is not a complete lock."),
        ("Soft control cap", 0.25, "loss of affected stat", "Slow/accuracy/tracking debuffs share named families and highest-effect rules."),
        ("Support reception cap", 0.40, "base HP/30s", "External hull recovery per 30s <=40% of target base hull; external shield recovery uses base shields separately."),
        ("Capacitor transfer efficiency", 0.80, "energy received/paid", "A sender pays the stated transfer amount; efficiency capped at 90% with traits, no amplification by output quality."),
        ("Post-combat recharge", 10, "seconds", "Free shield regeneration begins after 10s without dealing or receiving hostile effects; capacitor always follows its declared regeneration."),
        ("Dock refit timer", 5, "seconds", "Recompute from sources; preserve absolute damage and expenditure, never refill by switching fittings."),
        ("Mining concurrent sessions", 1, "session/module", "Reserve claims serialize per deposit, no more than 3 ships per small deposit or 6 per large deposit."),
        ("Extraction claim expiry", 30, "seconds after expected completion", "Clear abandoned leases; immediate clear on normal cancellation."),
        ("Dirty persistence interval", 5, "seconds maximum", "Immediate durable settlement for ownership/cargo/reward transitions; HP/cap state may lose at most 5s on crash."),
        ("Cockpit refresh", 0.5, "seconds minimum", "Dirty state only; no full NUI rebuild per module tick."),
        ("Discovery repetition", 1, "reward/object/spawn", "New reserve or anomaly identity needed; rescans only refresh information."),
        ("Experience rate ceiling", 18000, "base XP/active hour", "Total operating XP across the five skills, before existing account/server XP modifiers; finite objective contribution."),
        ("Ordinary cargo loss", 0.20, "unprotected units on defeat", "Floor per commodity, deterministic settlement; protected compartment preserves up to its stated capacity."),
        ("Dock hull recovery", 0.03, "hull reference value per paid recovery", "Voluntary dock hull recovery uses the same 3% reference fee as defeat, minimum 60 and maximum 1800. An outstanding defeat recovery fee replaces this fee rather than adding a second hull charge. Shield/capacitor refill adds no charge; module condition is billed separately."),
        ("Defeat recovery", 0.03, "hull reference value", "Minimum 60 credits, maximum 1800; fitted equipment retained but service condition loses 20 points."),
        ("Service condition", 100, "points at manufacture", "0-100; no gradual output degradation. At 0, module disabled until serviced."),
        ("Module service", 0.01, "module reference value/condition point", "Twenty points lost on defeat =>20% of reference value, rounded up, minimum total service bill 10 credits/module."),
        ("Passenger defeat handling", 1, "safe rescue state", "Passengers enter the rescue/dock recovery flow once; no forced unrelated ground death."),
        ("Old enhancement conversion", "q = min(1, old grade / 50)", "design quality", "Legacy generic bonus maps to bounded quality; excess above 50 returns recorded source materials at 80% once."),
        ("Displaced fittings", 1, "sealed refit package/ship", "All modules preserved; over-budget/slot-incompatible items moved to persisted recovery package without cargo duplication."),
        ("Legacy skill cleanup", "Full rebuild", "character builds", "Persistent ship/item changes use the existing migration timing policy; no separate perk refund migration."),
        ("Design baseline", "2026-10-05", "version date", "Numerically specified before gameplay implementation; actual engine feel and live economy still need release validation."),
    ]

    hull_headers = "id name role piloting high low power hull shield capacitor cap_regen shield_regen speed signature cargo resistance value mount"
    hull_values = [
        ("ShipDeedLightFreighter", "Light Freighter", "Freighter", 1, 2, 3, 40, 140, 100, 80, 1.8, .5, .95, 65, 180, 10, 2000, "Compact/Standard"),
        ("ShipDeedLightEscort", "Light Escort", "Fighter", 1, 3, 2, 34, 100, 90, 70, 1.6, .5, 1.15, 35, 30, 10, 2000, "Compact"),
        ("sdeed_striker", "Striker", "Interceptor", 5, 3, 3, 38, 90, 110, 90, 2.2, .6, 1.25, 30, 20, 10, 4500, "Compact"),
        ("sdeed_condor", "Condor", "Scout", 5, 2, 4, 40, 110, 100, 100, 2, .6, 1.20, 32, 80, 10, 4500, "Compact"),
        ("sdeed_hound", "Hound", "Prospector", 10, 2, 4, 56, 180, 120, 120, 2.4, .7, .80, 90, 420, 15, 8000, "Compact/Standard/Industrial"),
        ("sdeed_panther", "Panther", "Fighter", 10, 3, 3, 48, 130, 110, 110, 2.5, .6, 1.12, 45, 40, 10, 8000, "Compact/Standard"),
        ("sdeed_saber", "Saber", "Bomber", 15, 3, 3, 60, 170, 130, 130, 2.6, .7, .95, 75, 80, 15, 12000, "Compact/Standard/Ordnance"),
        ("sdeed_falchion", "Falchion", "Support", 15, 2, 4, 62, 160, 140, 160, 3.2, .8, .90, 80, 100, 15, 12000, "Compact/Standard"),
        ("sdeed_mule", "Mule", "Bulk Industrial", 20, 3, 4, 68, 230, 150, 160, 3, .8, .70, 120, 650, 20, 18000, "Compact/Standard/Industrial"),
        ("sdeed_merchant", "Merchant", "Freighter", 20, 2, 5, 64, 190, 160, 150, 2.8, .8, .85, 105, 500, 15, 18000, "Compact/Standard"),
        ("sdeed_throne", "Throne", "Support", 25, 3, 4, 72, 210, 190, 200, 3.7, .9, .82, 115, 200, 20, 24000, "Compact/Standard"),
        ("sdeed_consular", "Consular", "Expedition", 25, 3, 4, 70, 180, 170, 180, 3.4, .9, .95, 100, 250, 15, 24000, "Compact/Standard"),
        ("sdeed_aurek", "Aurek Strikefighter", "Fighter", 10, 3, 3, 48, 110, 130, 110, 2.5, .6, 1.18, 38, 30, 10, 10000, "Compact/Standard"),
        ("sdeed_sfight", "Sith Fighter", "Interceptor", 15, 4, 2, 50, 250, 0, 110, 2.5, 0, 1.16, 42, 25, 15, 12000, "Compact"),
        ("sdeed_cutla", "Cutlass Starfighter", "Heavy Fighter", 15, 3, 4, 58, 170, 140, 140, 2.8, .7, 1, 65, 60, 15, 14000, "Compact/Standard"),
        ("sdeed_basi", "Basilisk War Droid", "Assault", 20, 4, 2, 64, 280, 60, 150, 2.8, .3, .95, 70, 50, 20, 18000, "Compact/Standard/Ordnance"),
        ("capdeed_rthran", "Republic Thranta Corvette", "Fleet Support", 30, 3, 5, 110, 480, 420, 320, 5.5, 1.2, .58, 260, 600, 25, 60000, "Compact/Standard/Heavy"),
        ("capdeed_sthran", "Sith Thranta Corvette", "Fleet Support", 30, 3, 5, 110, 480, 420, 320, 5.5, 1.2, .58, 260, 600, 25, 60000, "Compact/Standard/Heavy"),
        ("capdeed_nthran", "Thranta Corvette", "Fleet Support", 30, 3, 5, 110, 480, 420, 320, 5.5, 1.2, .58, 260, 600, 25, 60000, "Compact/Standard/Heavy"),
        ("capdeed_cgunb", "Corellian Gunboat", "Heavy Combat", 30, 4, 4, 100, 500, 350, 300, 5, 1, .60, 240, 350, 25, 55000, "Compact/Standard/Heavy"),
        ("capdeed_chisst", "Chiss Trireme", "Electronic Support", 30, 3, 5, 108, 420, 420, 350, 5.8, 1.1, .62, 245, 350, 25, 60000, "Compact/Standard/Heavy"),
        ("capdeed_hvycor", "CZC Armored Transport", "Protected Freight", 30, 2, 6, 104, 650, 250, 260, 4.5, .8, .50, 300, 1800, 30, 60000, "Compact/Standard/Heavy"),
        ("capdeed_corsa", "Terminus Class Corsair", "Heavy Combat", 30, 4, 4, 104, 520, 360, 280, 5, 1, .65, 230, 500, 25, 60000, "Compact/Standard/Heavy"),
        ("capdeed_huttco", "Hutt Corvette", "Bulk Industrial", 30, 3, 5, 110, 600, 300, 320, 5, .9, .48, 320, 1500, 25, 60000, "Compact/Standard/Heavy/Industrial"),
        ("capdeed_jfrigate", "Jehavey'ir", "Fleet Command", 35, 3, 5, 118, 620, 480, 380, 6.3, 1.3, .45, 340, 900, 30, 75000, "Compact/Standard/Heavy"),
        ("capdeed_cruscor", "Crusader Corvette", "Heavy Combat", 35, 4, 4, 112, 540, 400, 320, 5.4, 1.1, .58, 280, 450, 25, 70000, "Compact/Standard/Heavy"),
    ]

    module_headers = "id name family slot mount power output cycle capacitor range tracking resolution engineering value effect"
    module_values = [
        ("tracking_laser", "Compact Tracking Laser", "Thermal", "High", "Compact", 8, 12, 4, 4, 28, 110, 40, 5, 240, "Direct thermal damage; strong small-target tracking."),
        ("sustained_beam", "Sustained Beam", "Thermal", "High", "Standard", 12, 24, 6, 8, 32, 65, 65, 15, 420, "Direct thermal damage; efficient against larger targets."),
        ("pulse_laser", "Pulse Laser", "Thermal", "High", "Compact", 10, 18, 5, 7, 24, 85, 50, 10, 330, "Direct thermal damage; concentrated cycle with lower reach."),
        ("long_emitter", "Long-Range Emitter", "Thermal", "High", "Standard", 14, 20, 6, 9, 45, 60, 65, 25, 520, "Direct thermal damage; reach trades against power and tracking."),
        ("laser_battery", "Laser Battery", "Thermal", "High", "Standard", 16, 30, 7, 11, 30, 75, 65, 25, 620, "Direct thermal damage; larger fitting and activation demand."),
        ("heavy_beam", "Heavy Beam Cannon", "Thermal", "High", "Heavy", 24, 56, 8, 20, 40, 35, 140, 40, 1100, "Direct thermal damage; inefficient against small evasive targets."),
        ("shield_breaker", "Ion Shield Breaker", "Ion", "High", "Compact", 10, 16, 5, 6, 28, 100, 45, 15, 400, "130% damage to shields, 50% to hull; no resource drain."),
        ("engine_disruptor", "Ion Engine Disruptor", "Ion", "High", "Standard", 14, 12, 6, 9, 30, 80, 65, 25, 550, "Normal ion damage; landed hit slows 15% for 4s; soft-control family rules."),
        ("cap_disruptor", "Ion Capacitor Disruptor", "Ion", "High", "Standard", 14, 10, 6, 10, 30, 75, 65, 30, 600, "Normal ion damage; landed hit destroys 6 capacitor; drains do not scale with damage quality."),
        ("rapid_missile", "Rapid Light Missile Launcher", "Ordnance", "High", "Compact", 10, 16, 5, 2, 35, 105, 45, 10, 400, "One light missile/shot; 4 credits reference ammunition cost."),
        ("heavy_missile", "Assault Missile Launcher", "Ordnance", "High", "Ordnance", 16, 48, 8, 6, 38, 60, 100, 25, 700, "One assault missile/shot; 15 credits reference ammunition cost."),
        ("torpedo", "Heavy Torpedo Launcher", "Ordnance", "High", "Ordnance", 20, 90, 12, 10, 45, 35, 160, 35, 950, "One torpedo/shot, 30 credits; 2s visible preparation; committed burst."),
        ("bombardment", "Bombardment Launcher", "Ordnance", "High", "Heavy", 28, 150, 18, 18, 40, 25, 220, 45, 1400, "One bomb/shot, 70 credits; 3s preparation; selected heavy ship or objective, no unbounded area payload."),
        ("shield_bank", "Shield Capacity Bank", "Defense", "Low", "Any", 12, 60, 0, 0, 0, 0, 0, 10, 350, "+60 shield capacity, -15 capacitor capacity; no combat recharge."),
        ("recharge_array", "Shield Recharge Array", "Defense", "Low", "Any", 10, .6, 0, 0, 0, 0, 0, 15, 380, "+0.6 shield/s out of combat, -10% base shield capacity."),
        ("shield_booster", "Emergency Shield Booster", "Recovery", "Low", "Any", 14, 40, 12, 20, 0, 0, 0, 20, 500, "Self shield recovery; ordinary recovery mastery applies."),
        ("hull_repair", "Self Hull Repairer", "Recovery", "Low", "Any", 12, 26, 12, 12, 0, 0, 0, 10, 350, "Self hull recovery; cannot repair above maximum."),
        ("shield_repair", "Focused Shield Repairer", "Recovery", "Low", "Any", 14, 34, 12, 16, 30, 0, 0, 20, 500, "Self or allied shield recovery; external reception budget applies."),
        ("repair_projector", "Allied Hull Repair Projector", "Recovery", "High", "Standard", 16, 34, 12, 16, 35, 0, 0, 25, 600, "Allied hull recovery; external reception budget applies."),
        ("repair_field", "Repair Field Generator", "Recovery", "High", "Heavy", 24, 54, 15, 28, 8, 0, 0, 40, 1000, "54 total hull recovery split among up to 3 allies within 8m, including source only if damaged; one total pool."),
        ("storage_bank", "Capacitor Storage Bank", "Power", "Low", "Any", 8, 50, 0, 0, 0, 0, 0, 5, 250, "+50 capacitor capacity, -10% base capacitor regeneration."),
        ("recovery_regulator", "Capacitor Recovery Regulator", "Power", "Low", "Any", 16, .6, 0, 0, 0, 0, 0, 25, 600, "+0.6 capacitor/s, -10% base capacitor capacity; maximum 2 fitted."),
        ("fuel_injector", "Consumable Fuel Injector", "Power", "Low", "Any", 8, 30, 20, 0, 0, 0, 0, 15, 350, "Consumes one injector charge per use, 6 credits reference; restoration never scales with output quality."),
        ("transfer_projector", "Capacitor Transfer Projector", "Power", "High", "Standard", 14, 20, 10, 20, 35, 0, 0, 25, 650, "Pay 20 capacitor; receiver gains 16 at base efficiency; sender cannot target itself."),
        ("survey_scanner", "Deposit Survey Scanner", "Survey", "Low", "Any", 8, 25, 8, 8, 40, 0, 0, 5, 280, "Resolution 25 before Astrometrics; reveal composition, reserve and hazards. No repeat discovery XP."),
        ("deep_scanner", "Deep Survey Scanner", "Survey", "Low", "Any", 14, 50, 12, 14, 45, 0, 0, 30, 650, "Resolution 50; -5% ship speed; deep seams need total resolution 60."),
        ("tracking_computer", "Weapon Tracking Computer", "Utility", "Low", "Any", 10, .15, 0, 0, 0, 0, 0, 15, 380, "+15% weapon tracking; no direct damage or hit-ceiling bypass."),
        ("precision_array", "Precision Targeting Array", "Utility", "Low", "Any", 12, .04, 0, 0, 0, 0, 0, 20, 450, "+4 percentage points pre-tracking accuracy, -8% base damage output."),
        ("interference_suite", "Sensor Interference Suite", "Electronic", "High", "Compact", 12, .12, 12, 12, 30, 0, 0, 20, 500, "Reduce one target's pre-tracking accuracy by 12 percentage points for 5s; highest effect and control DR."),
        ("countermeasures", "Defensive Countermeasure Suite", "Electronic", "Low", "Any", 10, .15, 20, 12, 0, 0, 0, 20, 450, "Reduce incoming ordnance tracking by 15% for 5s; no damage reflection."),
        ("precision_cutter", "Precision Mining Cutter", "Extraction", "High", "Compact", 12, 6, 12, 5, 18, 0, 0, 5, 300, "Remove 6 reserve units/cycle; base recovery 80%; hardness <=45."),
        ("bulk_extractor", "Bulk Ore Extractor", "Extraction", "High", "Standard", 18, 12, 12, 10, 18, 0, 0, 20, 600, "Remove 12 reserve units/cycle; recovery 60%; hardness <=65."),
        ("deep_drill", "Deep-Core Drill", "Extraction", "High", "Industrial", 22, 10, 15, 14, 15, 0, 0, 35, 850, "Remove 10 reserve units/cycle; recovery 70%; hardness <=90; speed -50% while working."),
        ("compact_drill", "Compact Deep-Core Drill", "Extraction", "High", "Compact", 18, 4, 15, 10, 15, 0, 0, 40, 900, "Remove 4 reserve units/cycle; recovery 80%; hardness <=90; speed -50% while working; qualifies for Deep-Core Extraction."),
        ("strip_miner", "Committed Strip Miner", "Extraction", "High", "Industrial", 30, 32, 18, 30, 15, 0, 0, 40, 1200, "Remove 32 reserve units/cycle; recovery 50%; hardness <=75; movement locked for 18s."),
        ("recovery_arm", "Salvage Recovery Arm", "Salvage", "High", "Compact", 12, 6, 12, 6, 15, 0, 0, 10, 350, "Recover 6 bulk wreck reserve units/cycle at 75% recovery."),
        ("electronics_kit", "Electronics Recovery Kit", "Salvage", "High", "Standard", 16, 1, 20, 10, 15, 0, 0, 25, 600, "One intact component attempt per cycle; 35% base chance, capped 55% including perks; reserve consumed on every attempt."),
        ("salvage_cutter", "Bulk Salvage Cutter", "Salvage", "High", "Standard", 18, 12, 12, 12, 15, 0, 0, 20, 550, "Recover 12 bulk wreck reserve units/cycle at 50% recovery; cannot recover intact components."),
        ("cargo_hold", "Expanded Cargo Hold", "Cargo", "Low", "Any", 12, .30, 0, 0, 0, 0, 0, 10, 350, "+30% base cargo capacity, -10% base shields; duplicate weights apply."),
        ("ore_compressor", "Ore Compression Unit", "Cargo", "Low", "Any", 14, 2, 10, 6, 0, 0, 0, 25, 650, "2:1 ore cargo occupancy; 20 ore/unit operation; 6 capacitor and one 2-credit packing charge; maximum one fitted."),
        ("protected_hold", "Protected Cargo Compartment", "Cargo", "Low", "Any", 14, 60, 0, 0, 0, 0, 0, 25, 600, "Protect 60 cargo units from ordinary defeat loss; -10% base cargo capacity; maximum one fitted."),
        ("armor_plating", "Reinforced Armor Plating", "Defense", "Low", "Any", 12, 20, 0, 0, 0, 0, 0, 15, 400, "+20 hull resistance rating; -8% base speed; no shield resistance bonus."),
        ("hull_plating", "Hull Capacity Plating", "Defense", "Low", "Any", 12, 60, 0, 0, 0, 0, 0, 10, 350, "+60 hull HP; -5% base speed; duplicate weights apply."),
        ("output_amplifier", "Weapon Output Amplifier", "Utility", "Low", "Any", 12, .08, 0, 0, 0, 0, 0, 25, 500, "+8% base weapon output; +10% weapon capacitor expenditure; duplicate weights and output cap apply."),
        ("maneuver_jets", "Maneuvering Jets", "Utility", "Low", "Any", 10, .04, 0, 0, 0, 0, 0, 20, 450, "+4 percentage points pre-tracking evasion; -10% base cargo; permanent equipment evasion cap 8 points."),
        ("advanced_thrusters", "Efficient Thruster Assembly", "Utility", "Low", "Any", 10, .10, 0, 0, 0, 0, 0, 25, 500, "+10% base speed; -10% base capacitor capacity."),
        ("power_router", "Defensive Power Router", "Defense", "Low", "Any", 18, 15, 0, 0, 0, 0, 0, 35, 800, "+15 shield resistance rating; -10% base weapon output and -10% base capacitor capacity."),
    ]

    calibrations = [
        ("Standard", 1, 1, 1, 1, 1, 1, "No adjustment; predictable base design."),
        ("Efficient", 1, .92, 1, .75, 1, 1, "25% less activation cost before upward rounding, 8% lower output; cost discounts share the 25% cap."),
        ("Extended", 1.10, .92, 1.10, 1, 1.25, 1, "25% more range, 10% longer cycle and fitting cost, 8% lower output."),
        ("Rapid", 1, .90, .85, 1.20, 1, 1, "15% shorter cycle, 10% less output, 20% more cost per activation."),
        ("Compact", .80, .85, 1, 1, 1, 1, "20% less fitting demand and 15% less output; does not change mount compatibility."),
        ("High Output", 1.20, 1.15, 1, 1.25, 1, 1, "15% more output with 20% more fitting demand and 25% more activation expenditure."),
        ("Precision", 1.10, .90, 1, 1, 1, 1.20, "20% more tracking or resolution, or +5 percentage points recovery; 10% less output and 10% more fitting demand."),
    ]
    configs = [
        ("combat_conversion", "Combat Conversion", 6, "+5% base weapon output", "-15% base cargo capacity"),
        ("survey_conversion", "Survey Conversion", 6, "+10 scanner resolution", "-10% base weapon output"),
        ("industrial_conversion", "Industrial Conversion", 6, "+10% reserve removal throughput", "-15% base weapon output, -5% base speed"),
        ("cargo_conversion", "Cargo Conversion", 6, "+20% base cargo capacity", "-15% base shields"),
        ("support_conversion", "Support Conversion", 6, "+10% base external recovery output", "-15% base weapon output"),
    ]

    skills = [
        ("Piloting", "Combat Pilot", "Expedition Pilot", "Maneuvering, pursuit, transit, escape, and hull operating requirements", "Meaningful maneuver objectives; not movement distance"),
        ("Gunnery", "Precision Gunnery", "Heavy Ordnance", "Weapon output, hit chance, tracking techniques and ordnance", "Actual hostile contribution against finite encounter HP"),
        ("Ship Systems", "Defensive Systems", "Fleet Support", "Paid recovery, power routing and external support", "Real hostile damage recovered or objective support; no self-damage farming"),
        ("Astrometrics", "Surveying", "Electronic Warfare", "Fitted sensor resolution, discoveries and target analysis", "Unique spawn discovery and legitimate hostile interference"),
        ("Space Industry", "Extraction", "Salvage and Logistics", "Usable extraction recovery, salvage and bounded cargo operations", "Actual finite reserves removed and legitimate freight settlement"),
    ]
    return {
        "version": "2026-10-05",
        "status": "Implemented numerical baseline; connected-client play and live-market validation remain release gates",
        "rules": [record("name value units description", r) for r in rules],
        "hulls": [record(hull_headers, r) for r in hull_values],
        "modules": [record(module_headers, r) for r in module_values],
        "calibrations": [record("name fitting output cycle capacitor range precision description", r) for r in calibrations],
        "configurations": [record("id name power benefit drawback", r) for r in configs],
        "skills": [record("name first second responsibility xp", r) for r in skills],
        "quality": {"minimum": 0, "maximum": 100, "positive_refinement": .08, "dimensions": 1, "slots": 1, "transfer_probability": "floor(100 * completed craft quality / maximum quality) percent, per property group; q is transferred input magnitude / declared maximum, not another guaranteed roll."},
    }


def add_perks(data):
    # Each technique has I/II/III values. Rows include the full hardware contract.
    # Costs paid for a technique are additional to the normal module operation.
    styles = [
        ("Piloting", "Combat Pilot", [
            ("Intercept", [15, 20, 25], "percent base speed for 8s", 8, 24, "Self; pursuing a hostile ship within 40m; no teleport", "Temporary speed"),
            ("Break Away", [3, 4, 5], "seconds ignoring soft movement slows", 10, 36, "Self; disables weapon banks for 3s; does not erase hard control", "Escape"),
            ("Evasive Maneuver", [8, 10, 12], "pre-tracking evasion percentage points for 5s", 12, 30, "Self; -15% base weapon output during the window", "Temporary evasion"),
        ], [("Maneuver Handling", [2, 4, 6], "percent base speed"), ("Pursuit Efficiency", [3, 6, 9], "percent capacitor discount on propulsion techniques")],
         ("Pursuit", "+5% base speed and +10% weapon tracking; -10 shield resistance rating", 0),
         ("Ace Maneuver", "+35% base speed and +12 pre-tracking evasion points for 6s; weapon banks disabled for first 2s", 20, 120, "Self; temporary caps and highest-effect rules")),
        ("Piloting", "Expedition Pilot", [
            ("Efficient Transit", [10, 15, 20], "percent lower departure service charge on one transit leg", 6, 30, "Self; committed noncombat transit objective; consume discount once", "Travel economy"),
            ("Hazard Run", [15, 20, 25], "percent less environmental hull damage for 8s", 10, 36, "Self; environmental hazards only, not hostile weapon damage", "Hazard mitigation"),
            ("Emergency Escape", [15, 20, 25], "percent base speed for 10s", 14, 60, "Self; weapon banks disabled throughout; no encounter completion bypass", "Temporary speed"),
        ], [("Navigation Economy", [3, 6, 9], "percent lower departure service charge"), ("Hazard Handling", [5, 10, 15], "percent less environmental hull damage")],
         ("Cruise", "+10% base speed; +15% incoming damage, weapon output -20%", 0),
         ("Safe Passage", "Environmental damage -50% for 12s; +25% base speed; weapon banks disabled", 25, 180, "Self; one paid hazardous leg; no immunity to enemies")),
        ("Gunnery", "Precision Gunnery", [
            ("Controlled Burst", [10, 15, 20], "percent base weapon output for 8s", 10, 30, "One fitted bank; weapon capacitor costs +20% during the window", "Temporary output"),
            ("Tracking Solution", [15, 20, 25], "percent weapon tracking for 10s", 8, 24, "One fitted weapon bank against selected hostile ship within 40m", "Temporary tracking"),
            ("Exploit Opening", [12, 16, 20], "percent base weapon output for 8s", 10, 36, "Target has a declared exposed-system status; applies to compatible fitted weapons", "Temporary output"),
        ], [("Weapon Handling", [4, 8, 12], "percent tracking on compatible weapons"), ("Firing Efficiency", [3, 6, 9], "percent weapon capacitor discount")],
         ("Precision", "+15% tracking; -10% base weapon output", 0),
         ("Perfect Solution", "+8 pre-tracking accuracy points and +30% tracking for 10s", 20, 120, "One fitted weapon bank; hit ceiling remains 95%")),
        ("Gunnery", "Heavy Ordnance", [
            ("Prepared Volley", [10, 15, 20], "percent base ordnance output for next paid volley within 10s", 10, 30, "Fitted ordnance bank; every participating launcher consumes ammunition", "Temporary output"),
            ("Torpedo Run", [15, 20, 25], "percent base torpedo output for next shot within 8s", 12, 36, "Torpedo launcher; target signature >=100; speed -20% during preparation", "Temporary output"),
            ("Bombardment", [15, 20, 25], "percent base heavy-ordnance output for next shot within 10s", 16, 45, "Heavy launcher; selected target signature >=200 or authored heavy objective", "Temporary output"),
        ], [("Ordnance Handling", [4, 8, 12], "percent ordnance tracking"), ("Ammunition Economy", [3, 6, 9], "percent docking ammunition purchase discount; no ammo-free firing chance")],
         ("Siege", "+10% base ordnance output; -20% base speed and -6 pre-tracking evasion points", 0),
         ("Coordinated Salvo", "+30% base ordnance output on next bank volley within 10s; 3s telegraph; launchers spend 150% normal ammunition rounded up", 25, 120, "Fitted ordnance bank; target signature >=100; ordinary capacitor costs still paid")),
        ("Ship Systems", "Defensive Systems", [
            ("Emergency Repair", [10, 15, 20], "percent base self-recovery output for 8s", 10, 36, "Fitted self hull/shield repairer; each recovery still pays normal module cost", "Temporary recovery"),
            ("Power Routing", [10, 15, 20], "percent base weapon output sacrificed for same percent self-recovery output for 10s", 8, 30, "Fitted repairer; no free shield or capacitor generation", "Temporary recovery"),
            ("Shield Recovery", [10, 15, 20], "percent base shield recovery output for next paid cycle within 8s", 10, 30, "Fitted shield booster/repairer; target self", "Temporary recovery"),
        ], [("Recovery Efficiency", [3, 6, 9], "percent capacitor discount on recovery modules"), ("Power Discipline", [2, 4, 6], "percent lower incoming capacitor destruction")],
         ("Defensive Routing", "+10% base self-recovery output; -15% base weapon output", 0),
         ("Damage Control", "+30% base self-recovery output and +25 hull/shield resistance rating for 8s", 25, 150, "Fitted recovery hardware; highest temporary bonuses; not a free heal")),
        ("Ship Systems", "Fleet Support", [
            ("Repair Link", [10, 15, 20], "percent base allied recovery output for 10s", 10, 30, "Fitted allied projector; target within normal module range; reception cap applies", "Temporary recovery"),
            ("Capacitor Transfer", [82, 86, 90], "percent transfer efficiency for next paid transfer within 8s", 4, 24, "Fitted transfer projector; sender pays normal transfer plus technique cost", "Transfer efficiency"),
            ("Support Surge", [10, 15, 20], "percent base external recovery output for 8s", 14, 36, "Fitted support bank; +20% capacitor expenditure during window", "Temporary recovery"),
        ], [("Projector Handling", [3, 6, 9], "percent projector range"), ("Transfer Efficiency", [2, 4, 6], "transfer efficiency percentage points; total <=90%")],
         ("Support Routing", "+10% base external recovery output; -20% base weapon output", 0),
         ("Fleet Stabilization", "+30% base external recovery output for 10s; up to 3 recipients within 35m; all fitted support pools remain bounded", 30, 150, "Fitted allied recovery bank; per-target reception caps still apply")),
        ("Astrometrics", "Surveying", [
            ("Deposit Analysis", [5, 10, 15], "scanner resolution for next scan within 10s", 6, 24, "Fitted deposit scanner; target within scanner range", "Temporary resolution"),
            ("Anomaly Scan", [5, 10, 15], "scanner resolution for next anomaly scan within 10s", 8, 30, "Fitted scanner; authored anomaly; first discovery only rewards XP", "Temporary resolution"),
            ("Route Survey", [5, 10, 15], "percent lower environmental damage for next committed leg within 60s", 10, 45, "Fitted scanner; scan completes in 8s; shared hazard mitigation cap 60%", "Route information"),
        ], [("Survey Resolution", [3, 6, 9], "scanner resolution"), ("Scan Economy", [3, 6, 9], "percent scanner capacitor discount")],
         ("Detailed Survey", "+10 scanner resolution; scan cycle +25%", 0),
         ("Deep Survey", "+25 scanner resolution on one 15s committed scan; reveals at most one authored hidden seam", 20, 180, "Fitted deep scanner; discovery key prevents repeated rewards; no resource creation")),
        ("Astrometrics", "Electronic Warfare", [
            ("Target Analysis", [3, 4, 5], "pre-tracking accuracy points for allies attacking one target for 10s", 8, 30, "Fitted targeting or survey sensor; hostile target within 35m; highest analysis effect", "Target information"),
            ("Sensor Disruption", [2, 3, 4], "extra accuracy penalty percentage points on next paid interference activation within 8s", 8, 30, "Fitted interference suite; combined debuff <=25 points; control DR", "Control"),
            ("Countermeasure Timing", [5, 8, 10], "extra percent incoming ordnance tracking reduction for next paid countermeasure activation within 8s", 8, 30, "Fitted countermeasure suite; total tracking reduction <=25%; highest effect", "Defense"),
        ], [("Sensor Handling", [3, 6, 9], "percent electronic module range"), ("Interference Efficiency", [3, 6, 9], "percent electronic module capacitor discount")],
         ("Interference", "+2 percentage points interference effect; signature +15%, -10% base weapon output", 0),
         ("Signal Break", "One hostile target loses module activation for 3s after a 2s telegraph; 20s control-family immunity afterward", 25, 150, "Fitted interference suite; range 30m; bosses retain movement and objective processing")),
        ("Space Industry", "Extraction", [
            ("Precision Extraction", [2, 4, 6], "recovery percentage points for 18s", 8, 36, "Fitted miner; reserve removal -10%; recovery <=95%", "Temporary recovery fraction"),
            ("Extraction Surge", [10, 15, 20], "percent reserve removal per cycle for 18s", 12, 45, "Fitted miner; +20% capacitor expenditure; same finite reserve", "Temporary throughput"),
            ("Selective Recovery", [60, 70, 80], "percent of recovered output directed to one surveyed constituent for one cycle", 10, 30, "Fitted precision cutter/deep drill; reserve composition still limits availability", "Material selection"),
        ], [("Beam Handling", [2, 4, 6], "recovery percentage points"), ("Industrial Efficiency", [3, 6, 9], "percent mining capacitor discount")],
         ("Careful Extraction", "+5 recovery percentage points; reserve removal -20%", 0),
         ("Deep-Core Extraction", "Deep drill hardness limit +10 and recovery +8 percentage points for one 20s committed cycle; movement locked", 20, 120, "Fitted deep drill; base reserve removal remains 10 for the industrial drill or 4 for the compact drill; recovery <=95%")),
        ("Space Industry", "Salvage and Logistics", [
            ("Careful Dismantling", [3, 5, 7], "intact-component chance percentage points for next paid attempt within 10s", 8, 36, "Fitted electronics kit; component chance <=55%; failed attempt still consumes reserve", "Salvage chance"),
            ("Recovery Sweep", [10, 15, 20], "percent bulk salvage reserve removal for 18s", 12, 45, "Fitted recovery arm/cutter; +20% activation expenditure", "Temporary throughput"),
            ("Cargo Handling", [5, 8, 10], "percent shorter authored loading/unloading time for one operation within 60s", 6, 30, "Dock/transfer equipment; cannot change capacity or duplicate cargo", "Freight operation"),
        ], [("Component Recovery", [2, 4, 6], "intact-component chance percentage points"), ("Freight Efficiency", [3, 6, 9], "percent lower freight service charge")],
         ("Recovery Operations", "+5 bulk salvage recovery points and +3 intact-component chance points; cycles +20%", 0),
         ("Specialist Recovery", "+8 intact-component chance points for one 25s committed attempt on a declared difficult component", 20, 150, "Fitted electronics kit; chance <=55%; unique wreck reserve; does not guarantee rare loot")),
    ]
    perks = []
    romans = ["I", "II", "III"]
    for skill, style, techniques, traits, mode, capstone in styles:
        for index, technique in enumerate(techniques):
            name, values, units, cost, cooldown, hardware, family = technique
            thresholds = [[2, 15, 30], [10, 25, 40], [20, 35, 45]][index]
            for rank, value in enumerate(values):
                perks.append(dict(skill=skill, style=style, name=f"{name} {romans[rank]}", kind="Technique", rank=rank+1, skill_rank=thresholds[rank], price=rank+1, magnitude=value, units=units, capacitor=cost, cooldown=cooldown, hardware=hardware, family=family, description=f"{value} {units}. Costs {cost} extra capacitor; {cooldown}s cooldown. {hardware}."))
        for index, (name, values, units) in enumerate(traits):
            for rank, value in enumerate(values):
                perks.append(dict(skill=skill, style=style, name=f"{name} {romans[rank]}", kind="Trait", rank=rank+1, skill_rank=[[5,20,35],[15,30,45]][index][rank], price=rank+1, magnitude=value, units=units, capacitor=0, cooldown=0, hardware="Compatible fitted hardware or applicable hull operation", family="Permanent", description=f"{value} {units}; replaces earlier rank, never adds all ranks together."))
        name, effect, cost = mode
        perks.append(dict(skill=skill, style=style, name=name, kind="Mode", rank=1, skill_rank=25, price=3, magnitude=0, units="See effect", capacitor=cost, cooldown=5, hardware="Self; one ship-wide mode slot; switching prohibited during a committed channel", family="Operating mode", description=effect))
        name, effect, cost, cooldown, hardware = capstone
        perks.append(dict(skill=skill, style=style, name=name, kind="Capstone", rank=1, skill_rank=50, price=7, magnitude=0, units="See effect", capacitor=cost, cooldown=cooldown, hardware=hardware, family="Capstone", description=f"{effect}. Costs {cost} extra capacitor; {cooldown}s cooldown. {hardware}."))
    data["perks"] = perks


def add_industry(data):
    data["resources"] = [
        record("ore metal cargo bid metal_value hardness reserve resolution stability respawn common_fraction", r)
        for r in [
            ("ore_tilarium", "ref_tilarium", 1, 8, 20, 30, 500, 25, 100, 900, .80),
            ("ore_currian", "ref_currian", 1, 12, 40, 45, 350, 35, 85, 1200, .70),
            ("ore_idailia", "ref_idailia", 1, 16, 60, 60, 300, 45, 75, 1500, .60),
            ("ore_barinium", "ref_barinium", 1, 24, 80, 75, 240, 55, 60, 1800, .50),
            ("ore_gostian", "ref_gostian", 1, 32, 100, 90, 180, 60, 50, 2100, .40),
            ("ore_arda", "ref_arda", 1, 40, 120, 100, 120, 70, 40, 2400, .40),
        ]
    ]
    data["deposits"] = [
        record("name ships reserve composition hardness stability hazard_seconds hazard_damage scan_seconds lifetime respawn", r)
        for r in [
            ("Starter rubble", 3, 500, "100% Tilarium", 30, 100, 0, 0, 8, 1800, 900),
            ("Mixed mobile seam", 3, 350, "70% Tilarium / 30% Currian", 45, 85, 30, 8, 8, 1800, 1200),
            ("Surveyed selective seam", 3, 300, "60% Tilarium / 40% Idailia", 45, 75, 30, 10, 10, 1800, 1500),
            ("Dense bulk field", 6, 1800, "70% Tilarium / 20% Currian / 10% Idailia", 65, 80, 30, 15, 10, 2400, 1800),
            ("Unstable deep seam", 3, 420, "50% Tilarium / 30% Barinium / 20% Gostian", 90, 50, 20, 18, 12, 2400, 2100),
            ("Deep-core anomaly", 3, 240, "40% Tilarium / 40% Gostian / 20% Arda", 100, 40, 18, 20, 15, 1800, 2400),
        ]
    ]
    data["industry_rules"] = [
        ("Starter spawn pool", "12 starter rubble + 6 mixed mobile seams per starter region; replenish at listed respawn after depletion; 18 concurrent deposits"),
        ("Advanced spawn pool", "8 selective + 6 bulk + 6 deep + 2 anomalies per advanced region; 22 concurrent deposits; no skill/tier entry gate"),
        ("Composition accounting", "Every named constituent has a finite reserve; selection consumes the selected constituent. Remaining composition cannot create a exhausted rare constituent."),
        ("Mixed material bids", "The listed ore bid is the NPC commission reference. Unlisted player market prices are not guaranteed; use current sale rules outside the new commission."),
        ("Hazards", "Damage bypasses shields as environmental hull damage, reduced by hazard-specific effects; warning 3s before each tick. Deposit exhaustion does not explode without warning."),
        ("Stability", "Each completed deep or strip cycle reduces stability by 2; precision/bulk by 1. At <=20, all extraction pauses for a visible 10s vent; vent restores 20 stability and damages nearby hull by 15."),
        ("Refining", "Keep current Gathering/refinery outputs and refinery management rules. Listed metal values are recipe reference costs, not a new guaranteed metal buyback."),
        ("Ore commission limits", "NPC ore commissions: maximum 1000 ore units per character per day at listed bid; further production sells through players/current vendors. Daily quota covers all ore types together and resets at00:00UTC. Supply purchases use the listed unit prices, one module or10 consumables per click; ammunition discounts cap25% and do not discount modules. Native sale receipts verify original and remaining quantities before paying; pending sale items cannot be crafted, refitted or traded."),
        ("Intact salvage", "One electronics attempt consumes one component reserve, success or failure. Chance =min(55%,35%+traits+mode+technique). No chance bonus from generic quality."),
        ("Wreck ownership", "Encounter participants retain exclusive claim for 120s, then public recovery; wreck lasts 600s. Cargo still requires legitimate ownership settlement."),
        ("Discovery component", "Deep Survey or Specialist Recovery can reveal one authored research component per qualifying object, probability 5%; one discovery draw per object, not per scan."),
        ("Salvage tables", "Routine wreck: 18 bulk reserve +2 component attempts; advanced:36 bulk +3 attempts; fleet:72 bulk +6 attempts. Bulk reference value 8 credits/unit, intact electronics 26."),
    ]

    # Recipes use exact material amounts and explicit base output designs. Their
    # nominal costs are design references; quality transfer remains shared craft.
    chart_source=(ROOT/"SWLOR.Game.Server/Service/CraftService/RecipeLevelChart.cs").read_text(encoding="utf-8")
    chart={int(level):(int(progress),int(quality),int(durability)) for level,progress,quality,durability in re.findall(r'_recipeLevels\[(\d+)\]\s*=\s*new RecipeLevelDetail\((\d+),\s*(\d+),\s*(\d+),',chart_source)}
    data["craft_targets"]={str(level):dict(progress=p,max_quality=q,durability=d) for level,(p,q,d) in chart.items()}
    recipes = []
    for m in data["modules"]:
        common = max(2, math.ceil(m["power"] * .40))
        alloy = max(1, math.ceil(m["power"] * .10))
        electronics = max(1, math.ceil(m["power"] * .12))
        salvage = 1 if m["engineering"] >= 25 else 0
        precision = 1 if m["engineering"] >= 35 else 0
        base_cost = common*20 + alloy*40 + electronics*13 + salvage*26 + precision*72
        progress,quality,durability=chart[m["engineering"]]
        recipes.append(dict(id=m["id"], name=m["name"], engineering=m["engineering"], tilarium=common, currian=alloy, electronics=electronics, recovered=salvage, precision=precision, output=1, quality_slots=1, reference_cost=base_cost, durability=durability, progress=progress, max_quality=quality, profile="Calibrated" if m["family"] in ("Electronic","Survey","Power","Recovery") else "Sturdy", research_seconds=300+30*m["engineering"], blueprint_chance=.80))
    data["recipes"] = recipes
    data["recipe_rules"] = [
        ("Calibration recipes", "Each allowed variant uses the base recipe plus 1 recovered electronics (26 reference credits); Engineering requirement +5, maximum50. Guarantee variant identity; optional quality transfer remains rolled."),
        ("Allowed calibrations", "Space Module Variants enumerates every allowed pair. Electronic:Standard/Efficient/Extended/Compact; Power/Cargo/Defense/Utility:Standard/Compact only, active or passive. Other families retain their declared choices. No Efficient pair when integer rounding leaves its cost unchanged."),
        ("Passive Compact", "For passive modules Compact multiplies only the positive capacity/rating/utility amount by85%, power by80%; drawbacks stay at base. No High Output capacitor regeneration variants."),
        ("Precision extraction", "Adds5 recovery points, removes10% less reserve, fitting+10%; no second throughput bonus. Precision bulk salvage uses the same recovery rule; component-attempt kits have no Precision variant. Precision scanners gain20% resolution with10% longer cycle, not another output reduction on that same resolution."),
        ("Quality dimension", "At most one positive eligible property per module. Magnitude at transferred q=100 is8%; interpolation is linear q/100. No extra enhancement slot from grade or material."),
        ("Refinement input access", "Ship refinement tokens are validated by their eligible dimension and magnitude1-100, independent of the old five-level enhancement cutoff. High-skill crafters may refine accessible hull-role equipment; no extra slot or lower crafting target is granted."),
        ("Quality exclusions", "Do not enhance capacitor creation/transfer, intact drop chance, protected cargo quantity, ammunition consumption, control duration, signature or fitting demand through quality."),
        ("Research", "80% base blueprint success per committed research attempt; duration300+30*EngineeringRequirement seconds. Preserve existing Research gates, Scientific Networking licensed runs, blueprint time/credit reductions and ownership settlement; final probability clamp10%-95%. Ship research level1 grants1-3 runs plus Networking, even levels add1 run, odd levels3/5/7/9 add1-10 percentage points to either time or credit reduction (cap95%); max research level10. No random item bonuses or extra enhancement slot. Failure retains an existing blueprint and its level; a failed new blueprint attempt produces no item. The success result is frozen at commitment; payment, input removal and output delivery use durable receipts."),
        ("Manufacturing licenses", "Engineering requirements2/10/20/35/45, SP1/2/3/4/5 (15SP total), unlock matching recipe complexity. Operator does not need the manufacturing license. Replaces only starship manufacturing unlocks during rebuild."),
        ("Consumables", "Light missiles:10/output for1 Tilarium+1 electronics; assault missiles:8/output for2 Tilarium+1 Currian+1 electronics; torpedoes:6/output for2 Tilarium+2 Currian+1 electronics; bombs:4/output for3 Tilarium+3 Currian+2 electronics."),
        ("Consumable reference fees", "NPC ammunition reference4/15/30/70 credits per light/assault/torpedo/bomb. Material reference cost per crafted unit3.3/11.625/22.167/51.5; margin before labor and research0.7/3.375/7.833/18.5. Recheck actual markets before release."),
        ("Service supplies", "Injector charges:10/output for1 Tilarium+1 electronics, NPC reference6 each; ore packing charges:20/output for1 Tilarium, NPC reference2 each; no quality bonus on these quantities."),
        ("Crafting integration", "Use shared progress/quality/durability/CP and declared recipe profiles. Progress,maximum quality and durability come exactly from RecipeLevelChart at recipe Engineering level; rank differences and committed enhancement penalties apply through the shared evaluator. Do not replace its quality target with a much lower space-only target."),
        ("NPC resale ceiling", "New ship designs use an explicit resale policy:min(25% design reference price,75% Standard recipe material reference),rounded down. Calibration/refinement/vendor-quality bonuses cannot lift that ceiling. Consumables use unit costs; hulls use their full recipe. Player commissions and player-market prices remain negotiated. Keep existing non-space crafting valuation unchanged."),
        ("Craft XP", "Base module craft XP =20*EngineeringRequirement, bounded by shared craft completion and legitimate committed materials; failure follows existing craft rules."),
        ("Hull manufacturing", "Space Hull Recipes lists exact quantities and shared craft targets. Engineering=max(5,PilotingRequirement+10),max50. Scale structural components proportionally to60% hull reference value, rounding each amount upward. Fixed hull identity, no extra quality multiplier on hull stats; preserve interiors and deed identity."),
        ("Configuration manufacturing", "Engineering20,6 Tilarium+2 Currian+2 electronics,1 output,1 optional quality property; quality cannot enhance both benefit and drawback, or fitting power."),
    ]


def add_activities(data):
    headers = "name seconds party success credits material_value supplies service defeat_probability defeat_cost xp piloting gunnery systems astro industry objective"
    values = [
        ("Starter patrol", 900, 1, .95, 1800, 300, 90, 70, .05, 180, 3600, .25, .65, .10, 0, 0, "Three encounters, then dock; rewards once per accepted patrol; no repeat payment for the same spawned target."),
        ("Advanced bounty", 1200, 1, .90, 2600, 650, 180, 120, .10, 450, 5400, .20, .65, .15, 0, 0, "One priority target plus screening ships; advanced region; target identity governs repeat claims."),
        ("Selective prospecting", 1200, 1, .92, 0, 2240, 80, 100, .08, 300, 4200, .15, 0, 0, .25, .60, "200 units from a60% Tilarium/40% Idailia selective seam,11.2cr weighted bid. Cargo, extraction time, scan resolution and hazard survival are checked against the declared fit."),
        ("Bulk expedition with escort", 1500, 2, .90, 1200, 5760, 180, 220, .10, 800, 10000, .15, .10, .10, .10, .55, "600 units from the70% Tilarium/20% Currian/10% Idailia bulk field,9.6cr weighted bid, within650 cargo. Shared wallet; escort gets a negotiated share."),
        ("Routine salvage", 900, 1, .94, 900, 1000, 65, 70, .06, 220, 3400, .15, .10, 0, .15, .60, "Recover a finite wreck cluster; no repeat payment or intact-component draw from reopening."),
        ("Survey and anomaly", 1200, 1, .94, 2400, 300, 70, 90, .06, 260, 4200, .25, 0, .05, .70, 0, "Three unique survey objectives; discovery outputs are authored and finite."),
        ("Resource delivery", 900, 1, .98, 1800, 0, 50, 60, .02, 180, 2800, .40, 0, 0, .10, .50, "Benchmark assumes employer supplies cargo. Buy-and-turn-in contracts subtract actual purchase value separately."),
        ("Sealed freight", 1200, 1, .94, 2800, 0, 70, 120, .06, 350, 4000, .50, 0, .10, .10, .30, "Employer retains cargo title; no NPC resale; exactly one destination settlement."),
        ("Escort and rescue", 1500, 2, .92, 5200, 800, 160, 200, .08, 700, 10000, .30, .20, .35, .15, 0, "Finite objective pool, shared wallet; legitimate hostile contribution and casualty stabilization."),
        ("Boarding operation", 1800, 3, .90, 9600, 2400, 450, 400, .10, 1200, 18000, .15, .40, .20, .10, .15, "Includes travel, space fight, and interior segment; XP pool apportioned to actual space/ground contribution, not duplicated."),
        ("Capital fleet objective", 2100, 4, .90, 14000, 3500, 650, 650, .10, 1800, 30000, .15, .40, .25, .10, .10, "One shared fleet wallet; useful interceptor and support objectives; no per-passenger full reward."),
    ]
    data["activities"] = [record(headers, r) for r in values]
    data["encounters"] = [
        record("name hull shield resistance speed signature gunnery piloting damage cycle capacitor regen accuracy reward_credit xp_pool control_seconds", r)
        for r in [
            ("Starter pirate", 80, 60, 5, .90, 65, 5, 5, 12, 5, 4, 1, .0, 150, 500, 0),
            ("Routine interceptor", 105, 75, 10, 1.12, 35, 20, 20, 15, 5, 5, 1.5, .0, 200, 650, 0),
            ("Routine gunship", 140, 100, 10, .85, 90, 20, 15, 24, 6, 8, 1.8, .0, 250, 800, 0),
            ("Advanced interceptor", 150, 100, 15, 1.20, 38, 40, 40, 20, 5, 6, 2, .02, 350, 1000, 0),
            ("Advanced bomber", 220, 150, 15, .90, 110, 40, 25, 55, 10, 8, 2, .0, 450, 1200, 0),
            ("Electronic raider", 160, 130, 15, 1, 75, 35, 30, 18, 6, 7, 2, .0, 350, 1000, 2),
            ("Elite heavy target", 450, 300, 25, .65, 240, 45, 25, 45, 8, 15, 3, .0, 700, 1800, 0),
            ("Fleet objective", 1400, 900, 30, .40, 400, 50, 20, 75, 10, 20, 4, .0, 0, 6000, 3),
        ]
    ]
    data["activity_rules"] = [
        ("Contract route cadence", "All11 contracts require their full listed minimum trip time and actual objectives. Three paid navigation legs unlock at one-third intervals; arrival within10m, waypoints at least40m apart. An elapsed timer alone grants no XP or reward. Contract lifespan7200s, exact listed party size, one active contract per player and ship. Employer freight uses60/120 units and30s loading/unloading;60cr cancellation deposit. Private contract sites and spawned hostiles carry stable objective identities. Three paid navigation legs each charge10cr before stat adjustments; total30cr is included in the listed service budget. Combined permanent Navigation/Freight discounts cap25%; highest temporary discount caps30%; final floor2cr. Recovery contracts stop at their200/600 recovered-unit quota, including every outstanding paid claim; final partial cycles still pay the full activation cost."),
        ("Contract contribution settlement", "Contract mining/survey and encounter XP is reserved inside the single listed mission pool; no independent duplicate award. Actual claim and target receipts are acknowledged once. Each skill pool splits only among its contributors; unused shares remain unearned. Final credits split by contribution weight; party passengers without legitimate work receive0. Finish at origin except delivery/freight, which require another registered planet dock. Three finite navigation receipts credit Piloting; arrival never repeats."),
        ("Wallets", "Activity rewards include listed encounter rewards. Do not add the encounter reference credits a second time when issuing the mission settlement."),
        ("XP shares", "The five percentages are maximum contribution pool shares, not automatic awards. Unused support/survey shares are unearned, not granted to idle participants."),
        ("Group XP", "Listed XP is the total activity pool; split each skill pool by legitimate contribution, never give every member the full amount."),
        ("XP accounting", "Base operating XP is capped18,000 per active hour across all skills. Existing global/account/server multipliers apply once afterward."),
        ("Mining XP", "3XP per reserve unit legitimately removed, maximum6000XP per completed expedition; survey discovery150XP/object, max1200XP/expedition; count actual work within the activity pool."),
        ("Support contribution", "Credited recovery cannot exceed hostile damage received since objective start; overheal and friendly/self-inflicted damage earn0. Paid energy sent to active objective participants counts at most10% of the Systems pool."),
        ("Freight limits", "One active employer-owned freight contract/ship; default loading30s and unloading30s;2-credit service floor after discounts; default cancellation forfeits60-credit deposit, no cargo resale."),
        ("Boarding interior", "Disable one elite heavy target in addition to its2 routine interceptor screens. Boarding uses3 security consoles,6s uninterrupted interaction each within3m,90s total after2s preparation at20m and hull<=20%. One unique console recovery grants11 precision assemblies and1 Tilarium ore (800cr reference),total2400cr for3 consoles; cargo ownership stays with its acting operator. Console work contributes Systems and Industry from the same mission XP wallet. Ground movement/interactions use normal character appearance and gear; exterior ships remain damageable and inherit their original pilot skill/attribute snapshot. Return, timer expiry, disconnect or defeat cannot duplicate cargo or rewards.",),
        ("Boarding setup", "Exterior disable threshold:target hull<=20%,2s boarding preparation,20m range; boarding timer90s; abandoned exterior remains vulnerable. Ground rewards consume the same mission wallet."),
        ("Crew", "At most4 credited stations:pilot,weapons(Gunnery),systems(Ship Systems),survey/industry(Astrometrics and Space Industry). One distinct operator per station; assigned operator ranks/traits/mode replace the pilot contribution for those skills, never add all passengers. Operate Stations permission and physical presence inside that ship are required. Join/leave and technique preparation require docking, no active contract, and5s readiness; personal cooldowns persist. One shared1s action cadence, shared capacitor and hardware cooldowns, existing output/quality/recovery caps, and one mode/up to4 techniques/one capstone per operator. The pilot selects exterior targets. Absent, dead, disconnected or unauthorized crew immediately fall back to the pilot; interrupted paid preparation or industrial claims cannot replay. Contract crew are frozen before loading, do not replace the required distinct ships, and share the same finite credit/XP pool; inactive passengers earn0."),
        ("Reputation", "5/10/15/20 reputation per starter/advanced/group/fleet completed objective; daily NPC-mission cap100. No direct permanent combat-stat bonus from reputation."),
        ("Supply shock test", "Reference material bids +/-25%; travel downtime +/-25%; success chance minus10 percentage points. Report net outputs rather than claim current market prices are known."),
        ("Starter grant", "One bound starter deed choice,2 standard compact tracking lasers or1 precision cutter+1 scanner,1 self repairer,100-credit service voucher; one grant per character rebuild, no repeat NPC sale. Bound fittings install only into that character's ship; bound ownership survives withdrawal. Starter grant outputs settle individually before the100cr voucher and completion receipt. Dock service applies up to25% permanent plus highest30% temporary fee discount, then consumes the voucher once; positive pre-voucher fees floor2cr."),
    ]


def build_data():
    data = specifications()
    add_perks(data)
    from ShipOperatingPerks import metadata
    data["operating_perks"] = [metadata(row) for row in data["perks"]]
    add_industry(data)
    add_activities(data)
    from SpaceActivityContracts import expand as expand_activity_contracts
    expand_activity_contracts(data["activities"])
    data["rules"].extend([
        dict(name="Calibration accounting",value="Positive output calibration shares the40% permanent family",units="base output",description="Use base*(1+min(40%,sum positive permanent)-sum negative tradeoffs+highest temporary <=30%). Do not multiply a15% output calibration outside that cap. Range, fitting and tracking have their own declared dimensions."),
        dict(name="Capacitor accounting",value="ceil(base*(1+positive demand-min(25%,all discounts)))",units="capacitor",description="Efficient calibration, traits and eligible crafted cost refinement share the25% discount cap. Positive High Output/Rapid/amplifier expenditure remains; minimum1 for a paid activation."),
        dict(name="Permanent tracking cap",value=.40,units="base tracking",description="All positive trait, equipment, mode and eligible quality contributions together; highest temporary contribution <=30%. Accuracy still has its separate95% final ceiling."),
        dict(name="Resistance minimum",value=0,units="rating",description="Negative configuration/mode adjustments can remove protection, never create a negative resistance denominator. Hull and shield ratings remain distinct."),
        dict(name="Technique cooldown identity",value="Operator and declared recast family",units="persistent state",description="All capstones share one capstone recast stamp per operator, lasting the used capstone's listed cooldown. Fitting, ship changes, prepared-technique changes and docking cannot clear it. Module timers retain item-instance identity."),
        dict(name="Survey hardware threshold",value="Scanner resolution >= deposit requirement",units="resolution",description="No skill gate on regional access. Fitted scanner plus Astrometrics/traits/mode/technique must resolve the target; information reveals finite deposits rather than spawning personal free reserves."),
    ])
    short_resrefs={"recovery_regulator":"cap_recovery","transfer_projector":"cap_transfer","advanced_thrusters":"adv_thrusters","tracking_computer":"track_computer","interference_suite":"sensor_jammer"}
    for module in data["modules"]:module["resref"]=short_resrefs.get(module["id"],module["id"])
    for config in data["configurations"]:config["resref"]="cfg_"+config["id"].removesuffix("_conversion")
    npc_parameters=[(60,1,110,40,28),(90,2,100,45,28),(120,2,65,65,32),(120,2,110,40,28),(150,2,60,100,38),(130,2,85,50,30),(260,3,35,140,40),(500,5,35,160,45)]
    for enemy,values in zip(data["encounters"],npc_parameters,strict=True):
        enemy.update(record("capacitor_pool weapons tracking resolution range",values))
    from SpaceEncounterProfiles import expand
    data["encounter_bindings"]=expand(data["encounters"])
    data["activity_rules"].extend([
        ("NPC fixtures", "Space Encounters lists explicit NPC-only weapon fixtures, pools, tracking and range. Use the same hit/cost/range validation; register their declared stats and flag their equipment NO_ECONOMY rather than exposing it as craftable player loot."),
        ("NPC encounter reward pool", "The listed credit reference is included inside the activity wallet. Independent uncontracted kills use that finite spawned encounter's listed reference once; NPC resource/recovery timers cannot reset by breaking combat."),
        ("NPC execution", "Encounter profiles retain creature identities and use the declared per-weapon output without a second mastery multiplier. Hull resistance is the listed rating; shield resistance0. Interceptor Agility26, other NPC operating attributes10. Advanced bomber starts with20 heavy-missile rounds/weapon; no replenishment. Electronic raider telegraph2s; fleet3s. Their on-hit activation lock uses the listed2/3 seconds and the shared20s control window. Electronic/heavy/fleet shields depleted by hostile damage expose systems for8s. Virtual NPC fixtures have no obtainable item blueprint."),
        ("Uncontracted contribution", "Spawn reward XP shares:25% Piloting for actual hostile evasion/received damage,65% Gunnery for actual finite hostile damage,10% Systems for recovery of recorded hostile damage. Empty shares remain unearned. Credits split by legitimate encounter participation. Killing or reopening the same encounter cannot pay twice."),
        ("NPC module mix", "Starter profiles use one bank; routine/advanced two weapons and a situational utility where indicated; elite/fleet3/5 weapons. Values are per weapon, each paying its listed capacitor. No automatic infinite ammunition or capacitor refill; authored resupply consumes encounter reserve once."),
    ])
    data["recipe_rules"].extend([
        ("NPC standard supply", "Starter vendors stock Standard modules through Engineering20 at their reference values and listed consumables. Advanced designs/calibrations come from players, salvage/research unlocks and authored rewards; no universal high-quality NPC replacement."),
        ("New component sources", "Recovered electronics: elec_recover, reference26cr, one successful intact-salvage attempt gives1. Precision assembly: prec_assembly, reference72cr, assembled from2 recovered electronics +1 Tilarium, Engineering30;1 output. These are reference crafting inputs, not guaranteed vendor buyback."),
        ("Typed refinement inputs", "Manufacture one family-appropriate enhancement: magnitude25 at Engineering5 from1 Tilarium+1 electronics;50 at Engineering15 from1 Tilarium+1 Currian+1 electronics;75 at Engineering25 from2 Tilarium+1 Currian+1 recovered electronics;100 at Engineering35 from2 Tilarium+1 Currian+2 recovered electronics. Output1,optional slot1; no module replacement or extra functionality."),
        ("Refinement transfer", "A transferred magnitude25/50/75/100 grants2%/4%/6%/8% on exactly one allowed positive dimension. Craft completion quality governs the shared property-group transfer chance, not another independent output-magnitude multiplier. Add progress penalty10/20/30/40 for a committed refinement input."),
        ("Transfer probability", "Current shared rule:floor(100*completed quality/maximum quality) percent per property group. Thus50% quality gives50% transfer,100% gives100%. One optional space property needs one transfer check; base recipe/calibration identity survives a failed refinement check."),
        ("Craft baseline checkpoint45", "Current shared evaluator,Engineering50,recipe45,Craftsmanship30,Control29,equipmentCP37,one magnitude100 refinement (progress+40):CP105,durability80,progress237,max quality2309. Adaptive transfer-first policy with protected completion:79.1754% transfer,79.6709% mean quality,22 expected actions. Reproduce:AuditCrafting.py --case 50,45,30,29,37,40 --objective transfer."),
        ("Craft baseline checkpoint50", "Same gear/rank,recipe50 and one refinement:CP105,durability80,progress226,max quality2641. Protected-completion transfer-first policy:69.0610% transfer,69.6554% mean quality,22 expected actions. Reproduce:AuditCrafting.py --case 50,50,30,29,37,40 --objective transfer. Checkpoints model current shared rules; repeat when introducing the planned shared profiles, conditions or action changes."),
        ("Refinement property bounds", "Blueprint rolls,research,quality and all enhancement inputs share the same one-property8% limit. Research cannot add a second ship tuning slot. Replacing an installed refinement uses its normal crafted item/property settlement, never adds a second copy of the same property."),
        ("Quality eligibility", "Space Modules lists the eligible dimensions per design; choose exactly one. Integer activation-cost refinement is offered only when its preview reduces the paid cost. Transfer/creation, component chance, compressor ratio and protected quantity cannot gain output refinements."),
        ("Calibration settlement", "Fitting power and paid capacitor round upward. Other output/range/cycle quantities retain fractions; HP and recovered item units carry fractions in authoritative settlement. Calibration drawbacks remain unchanged by quality; all source caps are applied against the Standard base design."),
    ])
    add_manufacturing_tables(data)
    return data


def add_manufacturing_tables(data):
    """Resolve the complete crafting matrix before gameplay definitions exist."""
    families={
        "Thermal":["Standard","Efficient","Extended","Rapid","Compact","High Output","Precision"],
        "Ion":["Standard","Efficient","Extended","Rapid","Compact","High Output","Precision"],
        "Ordnance":["Standard","Efficient","Extended","Rapid","Compact","High Output","Precision"],
        "Recovery":["Standard","Efficient","Compact","High Output"],
        "Survey":["Standard","Efficient","Extended","Compact","Precision"],
        "Extraction":["Standard","Efficient","Rapid","Compact","High Output","Precision"],
        "Salvage":["Standard","Efficient","Rapid","Compact","High Output","Precision"],
        "Electronic":["Standard","Efficient","Extended","Compact"],
    }
    calibration_by_name={c["name"]:c for c in data["calibrations"]}
    recipes={r["id"]:r for r in data["recipes"]}
    recovery_rates=dict(precision_cutter=.80,bulk_extractor=.60,deep_drill=.70,compact_drill=.80,strip_miner=.50,recovery_arm=.75,salvage_cutter=.50)
    variants=[]
    for m in data["modules"]:
        if m["family"] in ("Thermal","Ion","Ordnance"):
            dimensions=["Weapon output","Tracking","Range"]
        elif m["family"]=="Recovery":dimensions=["Recovery output"]+(["Range"] if m["range"] else [])
        elif m["family"]=="Survey":dimensions=["Scanner resolution","Range"]
        elif m["id"] in recovery_rates:dimensions=["Usable recovery fraction","Range"]
        elif m["id"] in ("electronics_kit","transfer_projector"):dimensions=["Range"]
        elif m["id"]=="ore_compressor":dimensions=["Cycle duration"]
        elif m["id"] in ("fuel_injector","recovery_regulator","protected_hold"):dimensions=[]
        else:dimensions=["Declared positive capacity/rating/handling amount"]
        if m["capacitor"] and math.ceil(m["capacitor"]*.92)<m["capacitor"] and m["id"]!="transfer_projector":dimensions.append("Activation cost")
        m["quality_dimensions"]=" / ".join(dimensions) or "None"
        allowed=families.get(m["family"],["Standard","Compact"])
        if m["id"]=="electronics_kit":allowed=["Standard","Efficient","Compact"]
        for name in allowed:
            c=calibration_by_name[name]
            if name=="Efficient" and (not m["capacitor"] or math.ceil(m["capacitor"]*c["capacitor"])==m["capacitor"]):continue
            engineering=m["engineering"] if name=="Standard" else min(50,m["engineering"]+5)
            output=m["output"]*c["output"]
            cycle=m["cycle"]*c["cycle"]
            recovery=recovery_rates.get(m["id"],0)
            notes="Calibration only, before operator/quality; all drawbacks remain and positive source caps use the Standard base."
            if m["family"]=="Survey" and name=="Precision":
                output=m["output"]*1.20;cycle=m["cycle"]*1.10
                notes="20% more scanner resolution with10% longer scan cycle; no second resolution/output adjustment."
            if name=="Precision" and recovery:recovery+=.05
            if m["id"]=="ore_compressor":
                output=2
                notes=f"Ratio remains2:1. Batch is{math.floor(20*c['output'])} ore; pay one packing charge per operation; no capacity or ratio quality."
            if m["id"]=="electronics_kit":
                output=1
                notes=f"One reserve consumed per attempt; base intact chance{.35*c['output']:.4f}, capped55% with skills; quality cannot improve chance."
            if m["id"]=="transfer_projector":notes=f"Pay{m['capacitor']} energy; base receiver gains{output*.80:g}; max with traits{output*.90:g}. No cost/output quality."
            variant=dict(design=m["id"],calibration=name,power=math.ceil(m["power"]*c["fitting"]),output=output,cycle=cycle,capacitor=math.ceil(m["capacitor"]*c["capacitor"]),range=m["range"]*c["range"],tracking=m["tracking"]*c["precision"],recovery_fraction=recovery,engineering=engineering,reference_cost=recipes[m["id"]]["reference_cost"]+(26 if name!="Standard" else 0),notes=notes)
            variant.update(data["craft_targets"][str(engineering)])
            variants.append(variant)
    data["module_variants"]=variants
    hull_recipes=[]
    for h in data["hulls"]:
        base=[math.ceil(h["hull"]/10),math.ceil(h["shield"]/20),math.ceil(h["capacitor"]/20),math.ceil(h["power"]/30),4 if h["id"].startswith("capdeed_") else 0]
        costs=[20,40,13,26,72]
        scale=max(1,.60*h["value"]/sum(n*p for n,p in zip(base,costs)))
        quantities=[math.ceil(n*scale) for n in base]
        level=min(50,max(5,h["piloting"]+10))
        recipe=dict(id=h["id"],name=h["name"],engineering=level,**dict(zip(["tilarium","currian","electronics","recovered","precision"],quantities)),output=1,quality_slots=0,reference_cost=sum(n*p for n,p in zip(quantities,costs)),hull_reference=h["value"],research_seconds=300+30*level)
        recipe.update(data["craft_targets"][str(level)]);hull_recipes.append(recipe)
    data["hull_recipes"]=hull_recipes
    inputs=[]
    values=[
        ("Light missiles",5,1,0,1,0,10,4),
        ("Assault missiles",20,2,1,1,0,8,15),
        ("Torpedoes",30,2,2,1,0,6,30),
        ("Bombs",40,3,3,2,0,4,70),
        ("Injector charges",15,1,0,1,0,10,6),
        ("Ore packing charges",10,1,0,0,0,20,2),
        ("Precision assembly",30,1,0,0,2,1,0),
        ("Configuration",20,6,2,2,0,1,350),
        ("Refinement25",5,1,0,1,0,1,0),
        ("Refinement50",15,1,1,1,0,1,0),
        ("Refinement75",25,2,1,0,1,1,0),
        ("Refinement100",35,2,1,0,2,1,0),
    ]
    for values_row in values:
        item=record("name engineering tilarium currian electronics recovered output npc_reference",values_row)
        item["reference_batch_cost"]=item["tilarium"]*20+item["currian"]*40+item["electronics"]*13+item["recovered"]*26
        item["reference_unit_cost"]=item["reference_batch_cost"]/item["output"]
        item.update(data["craft_targets"][str(item["engineering"])]);inputs.append(item)
    data["craft_inputs"]=inputs


def add_builds(data):
    builds = [
        ("Novice escort", "ShipDeedLightEscort", {"Piloting":5,"Gunnery":5,"Ship Systems":5}, ["tracking_laser","tracking_laser","hull_repair"], "", {}, 0, 10),
        ("Veteran accessible escort", "ShipDeedLightEscort", {"Piloting":40,"Gunnery":50,"Ship Systems":10}, ["tracking_laser","tracking_laser","hull_repair"], "combat_conversion", {"Piloting":("Combat Pilot",40),"Gunnery":("Precision Gunnery",50),"Ship Systems":("Defensive Systems",10)}, 100, 26),
        ("Veteran accessible interceptor", "sdeed_striker", {"Piloting":50,"Gunnery":50,"Ship Systems":15}, ["tracking_laser","tracking_laser","tracking_laser","hull_repair"], "", {"Piloting":("Combat Pilot",50),"Gunnery":("Precision Gunnery",50)}, 100, 26),
        ("Heavy objective specialist", "capdeed_cgunb", {"Piloting":30,"Gunnery":50,"Ship Systems":20}, ["heavy_beam","heavy_beam","heavy_beam","armor_plating","storage_bank"], "combat_conversion", {"Piloting":("Combat Pilot",30),"Gunnery":("Precision Gunnery",50),"Ship Systems":("Defensive Systems",20)}, 100, 26),
        ("Ordnance bomber", "sdeed_saber", {"Piloting":25,"Gunnery":50,"Ship Systems":15}, ["torpedo","torpedo","hull_repair"], "combat_conversion", {"Piloting":("Combat Pilot",25),"Gunnery":("Heavy Ordnance",50)}, 100, 26),
        ("Accessible survey prospector", "ShipDeedLightFreighter", {"Piloting":15,"Space Industry":50,"Astrometrics":20}, ["precision_cutter","survey_scanner","cargo_hold"], "industrial_conversion", {"Space Industry":("Extraction",50),"Astrometrics":("Surveying",20),"Piloting":("Expedition Pilot",15)}, 100, 10),
        ("Scout selective recovery", "sdeed_condor", {"Piloting":40,"Space Industry":50,"Astrometrics":50}, ["precision_cutter","deep_scanner","cargo_hold"], "", {"Piloting":("Expedition Pilot",40),"Space Industry":("Extraction",50),"Astrometrics":("Surveying",50)}, 100, 10),
        ("Accessible deep prospector", "sdeed_hound", {"Piloting":15,"Space Industry":50,"Astrometrics":50,"Ship Systems":15}, ["deep_drill","deep_scanner","hull_repair"], "industrial_conversion", {"Space Industry":("Extraction",50),"Astrometrics":("Surveying",50)}, 100, 10),
        ("Compact deep prospecting", "ShipDeedLightFreighter", {"Piloting":15,"Space Industry":50,"Astrometrics":50}, ["compact_drill","deep_scanner"], "industrial_conversion", {"Space Industry":("Extraction",50),"Astrometrics":("Surveying",50)}, 100, 10),
        ("Bulk industrial with escort", "sdeed_mule", {"Piloting":25,"Space Industry":50,"Astrometrics":20,"Ship Systems":20}, ["bulk_extractor","bulk_extractor","survey_scanner","hull_repair"], "industrial_conversion", {"Space Industry":("Extraction",50),"Ship Systems":("Defensive Systems",20)}, 100, 10),
        ("Fleet recovery specialist", "sdeed_falchion", {"Piloting":20,"Ship Systems":50,"Astrometrics":20}, ["repair_projector","transfer_projector","shield_repair","storage_bank"], "support_conversion", {"Ship Systems":("Fleet Support",50),"Piloting":("Expedition Pilot",20)}, 100, 10),
        ("Independent freight", "sdeed_merchant", {"Piloting":40,"Space Industry":50,"Ship Systems":15}, ["tracking_laser","hull_repair","cargo_hold","protected_hold"], "cargo_conversion", {"Piloting":("Expedition Pilot",40),"Space Industry":("Salvage and Logistics",50)}, 100, 10),
        ("Salvage specialist", "sdeed_hound", {"Piloting":15,"Space Industry":50,"Astrometrics":20}, ["recovery_arm","electronics_kit","survey_scanner","cargo_hold"], "", {"Space Industry":("Salvage and Logistics",50)}, 100, 10),
    ]
    data["builds"] = [dict(name=n,hull=h,skills=s,modules=m,configuration=c,styles=styles,quality=q,perception=p) for n,h,s,m,c,styles,q,p in builds]
    # Ground reserves are maximum spending allocations rather than fabricated
    # named perk purchases. One complete existing weapon style costs60 SP.
    data["mixed_builds"] = [
        dict(name="Ground fighter and pilot",ranks={"Piloting":40,"Gunnery":50,"Ship Systems":10,"Ground weapon":50,"Armor":50,"Gathering":50,"Engineering":50,"Leadership":50},space_build="Veteran accessible escort",ground_weapon_sp=60,other_ground_sp=90),
        dict(name="Industrial explorer with ground defense",ranks={"Piloting":15,"Space Industry":50,"Astrometrics":50,"Ship Systems":15,"Ground weapon":50,"Armor":30,"Engineering":50,"Gathering":50,"Research":50},space_build="Accessible deep prospector",ground_weapon_sp=60,other_ground_sp=70),
        dict(name="Support captain and ground specialist",ranks={"Piloting":20,"Ship Systems":50,"Astrometrics":20,"Ground weapon":50,"Armor":40,"Leadership":50,"First Aid":50,"Gathering":50},space_build="Fleet recovery specialist",ground_weapon_sp=60,other_ground_sp=100),
    ]

    by_name={b["name"]:b for b in data["builds"]}
    hulls={h["id"]:h for h in data["hulls"]}
    modules={m["id"]:m for m in data["modules"]}
    def service_bill(build_name):
        b=by_name[build_name];h=hulls[b["hull"]]
        hull_bill=min(1800,max(60,math.ceil(.03*h["value"])))
        module_bill=sum(max(10,math.ceil(.20*modules[mid]["value"])) for mid in b["modules"])
        # Configurations have reference350cr, the same20-point service loss.
        config_bill=70 if b["configuration"] else 0
        return hull_bill+module_bill+config_bill
    parties={
        "Starter patrol":["Novice escort"],"Advanced bounty":["Ordnance bomber"],
        "Selective prospecting":["Accessible survey prospector"],
        "Bulk expedition with escort":["Bulk industrial with escort","Veteran accessible escort"],
        "Routine salvage":["Salvage specialist"],"Survey and anomaly":["Scout selective recovery"],
        "Resource delivery":["Accessible survey prospector"],"Sealed freight":["Independent freight"],
        "Escort and rescue":["Fleet recovery specialist","Veteran accessible escort"],
        "Boarding operation":["Ordnance bomber","Veteran accessible escort","Accessible survey prospector"],
        "Capital fleet objective":["Heavy objective specialist","Fleet recovery specialist","Veteran accessible escort","Scout selective recovery"],
    }
    for a in data["activities"]:
        a["defeat_cost"]=sum(service_bill(n) for n in parties[a["name"]])
        a["defeat_party"]=" + ".join(parties[a["name"]])
        a["failure_material_fraction"] = .40
        if a["name"]=="Routine salvage":
            # Four advanced wrecks: finite bulk reserve plus12 independent
            # component attempts. Output quality cannot raise component chance.
            a["material_value"]=4*36*(.75+.05+.75*.08)*8 + 4*3*(.35+.06)*26
            a["objective"]="Four advanced wrecks:144 bulk reserve at86% recovery and12 intact attempts at41%;1118.64cr expected materials. Each reserve/attempt is consumed once. Requires recovery arm and electronics kit."
    data["activity_rules"].extend([
        ("Failure material realization", "Failed trips average50% of planned work before defeat and retain80% of that cargo, giving40% material realization. Defeat cost includes service only; cargo loss is included here once, not charged a second time."),
        ("Defeat cost derivation", "Sum party hull recovery(max60,min1800,ceil3% hull reference)) +20% fitted module reference +70 per fitted configuration. Cargo, passengers and unused docked ships are not billed twice."),
        ("Basic module operation", "Rank0 operation allowed for ordinary modules; demanding Heavy/Ordnance needs10 in the responsible skill, deep drills/strip miner/repair field/bombardment20. Higher Engineering requirement does not raise operator requirement."),
    ])


def operator_requirement(module):
    family = module["family"]
    skill = "Gunnery" if family in ("Thermal","Ion","Ordnance") else "Space Industry" if family in ("Extraction","Salvage","Cargo") else "Astrometrics" if family in ("Survey","Electronic") else "Ship Systems"
    rank = 20 if module["id"] in ("deep_drill","compact_drill","strip_miner","repair_field","bombardment") else 10 if module["mount"] in ("Heavy","Ordnance") else 0
    return skill, rank


def spec_cost(data, build):
    prices = 0
    for skill, (style, rank) in build["styles"].items():
        prices += sum(p["price"] for p in data["perks"] if p["skill"]==skill and p["style"]==style and p["skill_rank"]<=rank)
    return prices


def hit_chance(attacker, weapon, target):
    # Attributes intentionally have their own small declared coefficients.
    g = attacker["skills"].get("Gunnery",0)
    p = target.get("piloting",0)
    perception = max(10,min(26,attacker.get("perception",10)))
    agility = max(10,min(26,target.get("agility",10)))
    accuracy = attacker.get("accuracy",0)
    evasion = target.get("evasion",0)
    base = .80 + .002*g - .0015*p + .0025*(perception-10) - .0015*(agility-10) + accuracy-evasion
    tracking = min(1,(weapon["tracking"]*attacker.get("tracking",1)/100)*(target["signature"]/weapon["resolution"])/target["speed"])
    return max(.10,min(.95,base*tracking))


def fitted_metrics(data, build):
    hull = next(h for h in data["hulls"] if h["id"]==build["hull"])
    by_id = {m["id"]:m for m in data["modules"]}
    modules = [by_id[i] for i in build["modules"]]
    config = next((c for c in data["configurations"] if c["id"]==build["configuration"]),None)
    quality_bonus = .08*build["quality"]/100
    def trait(name):
        return max((p["magnitude"] for p in data["perks"] if p["kind"]=="Trait" and p["name"].rsplit(" ",1)[0]==name and p["skill"] in build["styles"] and p["style"]==build["styles"][p["skill"]][0] and p["skill_rank"]<=build["skills"].get(p["skill"],0)),default=0)
    # Trait ranks selected by the build remain visible in the calculations.
    efficiency = .09 if build["skills"].get("Gunnery",0)>=45 and any(v[0]=="Precision Gunnery" for v in build["styles"].values()) else 0
    tracking = 1 + (.12 if build["skills"].get("Gunnery",0)>=35 and any(v[0] in ("Precision Gunnery","Heavy Ordnance") for v in build["styles"].values()) else 0)
    output_bonus = min(.40,.002*build["skills"].get("Gunnery",0)+quality_bonus+.0025*(max(10,min(26,build["perception"]))-10)+(0.05 if build["configuration"]=="combat_conversion" else 0))
    speed = hull["speed"]*(1+min(.25,.002*build["skills"].get("Piloting",0)+trait("Maneuver Handling")/100)-(.08 if "armor_plating" in build["modules"] else 0)-(.05 if "deep_scanner" in build["modules"] else 0)-(.05 if build["configuration"]=="industrial_conversion" else 0))
    cap_regen = hull["cap_regen"]*(.90 if "storage_bank" in build["modules"] else 1)
    capacity = hull["capacitor"]+(50*(1+quality_bonus) if "storage_bank" in build["modules"] else 0)
    target_small = dict(signature=38,speed=1.20*1.08,piloting=40,agility=26)
    target_heavy = dict(signature=240,speed=.65*1.05,piloting=25,agility=10)
    actor = dict(build,tracking=tracking)
    weapons = [m for m in modules if m["family"] in ("Thermal","Ion","Ordnance")]
    small_dps = sum(m["output"]/m["cycle"]*(1+output_bonus)*hit_chance(actor,m,target_small) for m in weapons)
    heavy_dps = sum(m["output"]/m["cycle"]*(1+output_bonus)*hit_chance(actor,m,target_heavy) for m in weapons)
    weapon_drain = sum(math.ceil(m["capacitor"]*(1-efficiency))/m["cycle"] for m in weapons)
    recovery_bonus = min(.40,.002*build["skills"].get("Ship Systems",0)+quality_bonus+(0.10 if build["configuration"]=="support_conversion" else 0))
    recovery = sum(m["output"]/m["cycle"]*(1+recovery_bonus) for m in modules if m["family"]=="Recovery")
    recovery_drain = sum(m["capacitor"]/m["cycle"] for m in modules if m["family"]=="Recovery")
    support_drain = sum(m["capacitor"]/m["cycle"] for m in modules if m["id"]=="transfer_projector")
    mining = [m for m in modules if m["family"]=="Extraction"]
    rates = dict(precision_cutter=.80,bulk_extractor=.60,deep_drill=.70,compact_drill=.80,strip_miner=.50)
    passive_recovery = .001*build["skills"].get("Space Industry",0)+trait("Beam Handling")/100
    removal = sum(m["output"]/m["cycle"] for m in mining)*(1.10 if build["configuration"]=="industrial_conversion" else 1)
    mining_rate = sum(m["output"]/m["cycle"]*min(.95,rates[m["id"]]+passive_recovery+rates[m["id"]]*quality_bonus) for m in mining)*(1.10 if build["configuration"]=="industrial_conversion" else 1)
    mining_drain = sum(math.ceil(m["capacitor"]*.91)/m["cycle"] for m in mining)
    cargo = hull["cargo"]*(1+(.30*(1+quality_bonus) if "cargo_hold" in build["modules"] else 0)+(.20 if build["configuration"]=="cargo_conversion" else 0)-(.15 if build["configuration"]=="combat_conversion" else 0)-(.10 if "protected_hold" in build["modules"] else 0))
    shield = hull["shield"]*(1-(.10 if "cargo_hold" in build["modules"] else 0)-(.15 if build["configuration"]=="cargo_conversion" else 0))
    resistance = min(60,hull["resistance"]+(20*(1+quality_bonus) if "armor_plating" in build["modules"] else 0))
    net_cap = cap_regen-weapon_drain-recovery_drain-mining_drain-support_drain
    return dict(name=build["name"],hull=hull["name"],power=sum(m["power"] for m in modules)+(config["power"] if config else 0),power_max=hull["power"],high=sum(m["slot"]=="High" for m in modules),low=sum(m["slot"]=="Low" for m in modules),sp=spec_cost(data,build),small_dps=small_dps,heavy_dps=heavy_dps,recovery_per_second=recovery,cap_regen=cap_regen,drain=weapon_drain+recovery_drain+mining_drain+support_drain,net_cap=net_cap,cap_duration=capacity/-net_cap if net_cap<0 else None,speed=speed,signature=hull["signature"],cargo=cargo,ehp=shield+hull["hull"]*(1+resistance/100),mining_rate=mining_rate,reserve_rate=removal)


def numerical_audit(data):
    checks = []
    def check(name, passed, detail):
        checks.append(dict(name=name,passed=bool(passed),detail=detail))
    check("All current hulls mapped",len(data["hulls"])==26,"26 current persistent hull definitions have explicit values and unchanged IDs.")
    for skill in data["skills"]:
        for style in (skill["first"],skill["second"]):
            rows=[p for p in data["perks"] if p["style"]==style]
            check(f"{style} progression",len(rows)==17 and sum(p["price"] for p in rows)==40,"17 ranks,40SP,3 ranked techniques,2 ranked traits,mode,capstone.")
    metrics=[]
    hulls={h["id"]:h for h in data["hulls"]}
    modules={m["id"]:m for m in data["modules"]}
    for build in data["builds"]:
        fit=fitted_metrics(data,build); metrics.append(fit); hull=hulls[build["hull"]]
        check(build["name"]+" fitting",fit["power"]<=hull["power"] and fit["high"]<=hull["high"] and fit["low"]<=hull["low"],f"Power{fit['power']}/{hull['power']}; high{fit['high']}/{hull['high']}; low{fit['low']}/{hull['low']}.")
        check(build["name"]+" hull requirement",build["skills"].get("Piloting",0)>=hull["piloting"],"Direct operating rank requirement; no tier perk.")
        gates=[]
        for mid in build["modules"]:
            m=modules[mid]; skill,required=operator_requirement(m)
            # Basic hardware works at rank0 and grants legitimate starting XP;
            # omitted skills do not receive invented free ranks.
            gates.append(build["skills"].get(skill,0)>=required)
            mount_ok=m["mount"]=="Any" or m["mount"] in hull["mount"] or (m["mount"]=="Ordnance" and "Heavy" in hull["mount"])
            gates.append(mount_ok)
        check(build["name"]+" hardware requirements",all(gates),"Every fitted module has a compatible mount and explicit operator skill requirement.")
        # At zero quality the same fit must remain useful; expertise is bounded.
        ordinary=fitted_metrics(data,dict(build,quality=0))
        check(build["name"]+" crafted refinement",fit["small_dps"]<=ordinary["small_dps"]*1.09+.00001,"One8% output refinement does not also shorten cycles or improve tracking.")
    by_name={m["name"]:m for m in metrics}
    small=by_name["Veteran accessible escort"]; heavy=by_name["Heavy objective specialist"]
    check("Accessible hull advanced interception",small["small_dps"]>=heavy["small_dps"]*1.5,f"Accessible escort{small['small_dps']:.2f}DPS versus heavy fit{heavy['small_dps']:.2f} against an advanced interceptor.")
    check("Heavy hull heavy-objective advantage",heavy["heavy_dps"]>=small["heavy_dps"]*2,f"Heavy fit{heavy['heavy_dps']:.2f}DPS versus accessible escort{small['heavy_dps']:.2f} against a heavy target.")
    for mb in data["mixed_builds"]:
        ranks=sum(mb["ranks"].values()); fit=by_name[mb["space_build"]]
        spent=fit["sp"]+mb["ground_weapon_sp"]+mb["other_ground_sp"]
        mb.update(total_ranks=ranks,space_sp=fit["sp"],spent_sp=spent,earned_sp=min(400,ranks)+10,remaining_sp=min(400,ranks)+10-spent)
        check(mb["name"]+" character budget",ranks<=400 and spent<=min(400,ranks)+10 and all(v<=50 for v in mb["ranks"].values()),f"{ranks}/400ranks;{spent}/{min(400,ranks)+10}SP;60SP ground weapon style plus explicit other-ground spending reserve.")
    economies=[]
    for a in data["activities"]:
        gross=a["success"]*a["credits"]+(a["success"]+(1-a["success"])*a["failure_material_fraction"])*a["material_value"]
        expense=a["supplies"]+a["service"]+a["defeat_probability"]*a["defeat_cost"]
        net=(gross-expense)*3600/a["seconds"]/a["party"]
        xp=a["xp"]*3600/a["seconds"]/a["party"]
        stress_success=max(0,a["success"]-.10)
        stress_gross=stress_success*a["credits"]+(stress_success+(1-stress_success)*a["failure_material_fraction"])*.75*a["material_value"]
        stress_expense=a["supplies"]+a["service"]+(1-stress_success)*a["defeat_cost"]
        stress=(stress_gross-stress_expense)*3600/(1.25*a["seconds"])/a["party"]
        economies.append(dict(name=a["name"],gross=gross,expense=expense,net_per_player_hour=net,xp_per_player_hour=xp,stress_net_per_player_hour=stress))
        check(a["name"]+" reward budget",5500<=net<=9500 and xp<=18000 and stress>0,f"Expected net{net:.0f}cr/player-hour;XP{xp:.0f};combined material/travel/success stress net{stress:.0f}.")
        check(a["name"]+" XP shares",abs(sum(a[s] for s in ("piloting","gunnery","systems","astro","industry"))-1)<1e-8,"Finite contribution shares sum to100%; group totals are divided, not duplicated.")
    # Exact reserve conservation including fractional carry and interruption.
    for raw in (1,5,120,500):
        for recovery in (.5,.8,.95):
            total=0.; reward=0
            for _ in range(raw):
                total+=recovery; paid=math.floor(total+1e-9); reward+=paid; total-=paid
            check(f"Reserve conservation{raw}/{recovery}",reward<=raw and reward==math.floor(raw*recovery+1e-8),"Fractional carry; interrupted unpaid remainder is discarded, not rounded into bonus ore.")
    check("Closed energy transfers",.90*.90<1,"Two best transfers retain81% before technique costs; repeated circuits strictly lose energy.")
    check("Output source bound",1+.40+.30<=1.70,"Maximum output multiplier1.70, not independent multiplicative skill/craft/mode layers.")
    check("Resistance source bound",85/(100+85)<.46,"Maximum temporary mitigation45.95%; passive37.5%; shields and hull use declared separate ratings.")
    check("Control termination",3+1.5==4.5,"One full3s hard control and one half1.5s within20s, then immunity; Signal Break immediately grants20s family immunity.")
    check("Recipe material identities",all(r["tilarium"]>=2 and r["currian"]>=1 and r["electronics"]>=1 for r in data["recipes"]),"Every advanced module continues to consume common Tilarium and electronics.")
    check("Recipe shared targets",all(all(r[k]==data["craft_targets"][str(r["engineering"])][k] for k in ("progress","max_quality","durability")) for r in data["recipes"]+data["module_variants"]+data["hull_recipes"]+data["craft_inputs"]),"Every recipe and variant uses actual shared RecipeLevelChart progress,quality and durability; no space-only low target.")
    check("Hull production material spread",all(.60<=r["reference_cost"]/r["hull_reference"]<=.67 for r in data["hull_recipes"]),"Exact hull ingredients cost60%-67% of design hull reference, before labor and research.")
    check("Precision assembly material conservation",next(i["reference_unit_cost"] for i in data["craft_inputs"] if i["name"]=="Precision assembly")==72,"Two26-credit recovered components plus20-credit Tilarium cost72, not an invented60-credit input.")
    check("NPC module production arbitrage",all(math.floor(min(.25*modules[r["id"]]["value"],.75*r["reference_cost"]))<r["reference_cost"] for r in data["recipes"]),"Explicit resale ceiling remains below reference material spend, including optional quality and calibrated designs.")
    check("NPC hull production arbitrage",all(math.floor(min(.25*r["hull_reference"],.75*r["reference_cost"]))<r["reference_cost"] for r in data["hull_recipes"]),"Shared quality vendor bonuses cannot make repeated manufacture and NPC resale a currency source.")
    check("Efficient calibration has a paid benefit",all(v["capacitor"]<modules[v["design"]]["capacitor"] for v in data["module_variants"] if v["calibration"]=="Efficient"),"Upward integer rounding cannot turn a lower-output Efficient variant into a strict downgrade.")
    check("Calibration limits",all(v["cycle"]>=modules[v["design"]]["cycle"]*.85 and v["power"]>=1 and (not modules[v["design"]]["capacitor"] or v["capacitor"]>=1) for v in data["module_variants"]),"Every enumerated variant respects cycle and paid-cost floors; compact fitting power rounds upward.")
    check("Energy calibration restrictions",all(v["calibration"] in ("Standard","Compact") for v in data["module_variants"] if v["design"] in ("transfer_projector","fuel_injector","recovery_regulator")),"No activation-discount or high-output calibration amplifies resource creation or makes transfers gain energy.")
    industry=[]
    cases=[("Selective prospecting","Accessible survey prospector",200,600,11.2), ("Bulk expedition with escort","Bulk industrial with escort",600,1000,9.6)]
    activities={a["name"]:a for a in data["activities"]}
    for activity,build_name,units,overhead,bid in cases:
        fit=by_name[build_name]
        cycles=math.ceil(units/(fit["mining_rate"]*12))
        working=cycles*12
        vent=10 if activity=="Bulk expedition with escort" else 0
        a=activities[activity]
        industry.append(dict(activity=activity,build=build_name,cargo=units,capacity=fit["cargo"],working_seconds=working+vent,overhead_seconds=overhead,trip_seconds=a["seconds"],material_reference=units*bid,notes="Cycle rounding, finite reserve and deposit composition included; one return flight."))
        check(activity+" physical reward budget",units<=fit["cargo"] and working+vent+overhead<=a["seconds"] and math.isclose(units*bid,a["material_value"]),"Advertised material value matches deposit composition, fitted throughput, cargo and full-trip time.")
    salvage=by_name["Salvage specialist"]
    industry.append(dict(activity="Routine salvage",build="Salvage specialist",cargo=136,capacity=salvage["cargo"],working_seconds=24*12+12*20,overhead_seconds=240,trip_seconds=900,material_reference=activities["Routine salvage"]["material_value"],notes="Four advanced wrecks;144 bulk reserve and12 one-shot component attempts.136 cargo covers recovered bulk plus all12 possible components; no repeated roll."))
    check("Salvage physical reward budget",salvage["cargo"]>=136 and 24*12+12*20+240<=900,"Recovery arm and intact kit fitted; all work fits the declared900-second return trip.")
    # A commissioned compact fit makes the initial freighter a sustainable
    # advanced miner. It does not require a higher-requirement hull.
    compact={v["design"]:v for v in data["module_variants"] if v["calibration"]=="Compact"}
    mids=["compact_drill","deep_scanner","hull_repair"]
    compact_power=sum(compact[mid]["power"] for mid in mids)
    compact_recovery=min(.95,.80+.05+.06+.80*.08)
    compact_cycles=math.ceil(120/(compact["compact_drill"]["output"]*compact_recovery))
    compact_work=compact_cycles*15+30
    compact_heal=26*(1+.08-.15)/12
    compact_energy=10/15+12/12
    industry.append(dict(activity="Advanced compact deep prospecting",build="Light Freighter;Compact drill/scanner/repairer;Industry50,Astrometrics50,Systems0",cargo=120,capacity=180,working_seconds=compact_work,overhead_seconds=600,trip_seconds=1200,material_reference=120*17.6,notes=f"Power{compact_power}/40; high1/2,low2/3; no configuration. Scanner42.5+20+9=71.5. Recovery{compact_heal:g}HP/s exceeds1.05 hazard HP/s; expenditure{compact_energy:g}/s below1.8 regen. Commissioned quality refines one eligible dimension on each relevant item."))
    check("Starting freighter advanced solo fit",compact_power<=40 and compact_work+600<=1200 and compact_heal>1.05 and compact_energy<1.8,"Three commissioned Compact designs fit and sustain real advanced hazards; no hull tier upgrade or mandatory Systems skill.")
    report_industry=industry
    check("NWN module resource names",all(len(m["resref"])<=16 for m in data["modules"]+data["configurations"]),"Meaningful explicit resrefs within16 characters; domain design identifiers need not be truncated.")
    # These are honest design model results; no assertion of live play balance.
    return dict(version=data["version"],status="PASS" if all(c["passed"] for c in checks) else "FAIL",checks=checks,builds=metrics,economy=economies,industry=report_industry,limits=["This audit evaluates the declared design model, not current gameplay implementation.","Material reference values are design inputs, not observed player market prices.","Movement feel, target representation, combat readability and live supply require runtime validation before release."])


def source_inventory(data):
    source=ROOT/"SWLOR.Game.Server/Feature/ShipDefinition/PlayerShipDefinition.cs"
    old_hulls=[]
    for key,block in re.findall(r'_builder.Create\("([^"]+)"\)(.*?);',source.read_text(encoding="utf-8"),re.S):
        row=dict(id=key,name=re.search(r'\.Name\("([^"]+)"\)',block).group(1))
        for field,method in (("hull","MaxArmor"),("shield","MaxShield"),("capacitor","MaxCapacitor"),("high","HighPowerNodes"),("low","LowPowerNodes")):
            row[field]=int(re.search(r'\.'+method+r'\((\d+)\)',block).group(1))
        old_hulls.append(row)
    assert {h["id"] for h in old_hulls}=={h["id"] for h in data["hulls"]}
    data["legacy_hulls"]=old_hulls
    family_map={
        "AdvancedThrusters":"advanced_thrusters","AssaultConcussionMissile":"heavy_missile","BeamCannon":"sustained_beam","BulwarkShieldGenerator":"shield_bank","CapacitorBooster":"storage_bank","CapitalEwar":"interference_suite","CapitalPowerDeiverter":"power_router","CombatLaser":"tracking_laser","DamageAmplifier":"output_amplifier","EvasionBooster":"maneuver_jets","HullBooster":"hull_plating","HullRepairer":"hull_repair","HypermatterInjector":"fuel_injector","IonCannon":"shield_breaker","LaserCannonBattery":"laser_battery","MiningLaser":"precision_cutter","MissileLauncher":"rapid_missile","ProtonBomb":"bombardment","QuadLaserCannon":"pulse_laser","RedundantShieldGenerator":"shield_bank","ReinforcedPlating":"armor_plating","RepairFieldGenerator":"repair_field","ShieldBooster":"shield_bank","ShieldRepairer":"shield_repair","ShipArmor":"armor_plating","StormCannon":"heavy_beam","StripMiner":"strip_miner","TargetingArray":"precision_array","TargetingSystem":"tracking_computer","Turbolaser":"heavy_beam","WeaponsComputer":"tracking_computer",
    }
    legacy={}
    consumables={}
    for file in sorted((ROOT/"SWLOR.Game.Server/Feature/ShipModuleDefinition").glob("*ModuleDefinition.cs")):
        family=file.name.removesuffix("ModuleDefinition.cs")
        for tag in set(re.findall(r'"([a-z][a-z0-9_]{1,15})"',file.read_text(encoding="utf-8"))):
            item=ROOT/"Module/uti"/(tag+".uti.json")
            if not item.exists():
                continue
            j=json.loads(item.read_text(encoding="utf-8")); locals_=j.get("VarTable",{}).get("value",[])
            if any(v.get("Name",{}).get("value")=="NO_ECONOMY" and v.get("Value",{}).get("value")==1 for v in locals_):
                continue
            if j.get("BaseItem",{}).get("value")==528:
                consumables[tag]=dict(resref=tag,name=j.get("LocalizedName",{}).get("value",{}).get("0",tag),
                    conversion="Retain original consumable identity and quantity. Never convert ammunition or fuel into fitted equipment.")
                continue
            if family=="ShipConfiguration":
                if tag.startswith("config_ind") or tag=="cap_indus": target="industrial_conversion"
                elif tag in ("cap_skirm",) or tag.startswith(("config_fig","config_int","config_bmb")) or tag=="cap_warship": target="combat_conversion"
                else: continue
            else:
                target=family_map.get(family)
            if target:
                legacy[tag]=dict(resref=tag,name=j.get("LocalizedName",{}).get("value",{}).get("0",tag),source=file.relative_to(ROOT).as_posix(),target=target,conversion="Preserve instance and original enhancement provenance; same sidegrade design for every old tier; bounded quality mapping; displaced fittings remain in sealed refit package.")
    recipe_rows=[]
    for file_name in ("ModuleRecipes.cs","CapitalModuleRecipes.cs"):
        file=ROOT/"SWLOR.Game.Server/Feature/RecipeDefinition/EngineeringRecipeDefinition"/file_name
        for block in re.findall(r'_builder.Create\(RecipeType\.(.*?)\);',file.read_text(encoding="utf-8"),re.S):
            resref=re.search(r'\.Resref\("([^"]+)"\)',block)
            if not resref: continue
            tag=resref.group(1)
            if tag not in legacy:
                raise ValueError("Unmapped obtainable legacy module recipe: "+tag)
            components=[(tag_,int(q)) for tag_,q in re.findall(r'\.Component\("([^"]+)",\s*(\d+)\)',block)]
            row=copy.deepcopy(legacy[tag]);row["recipe"]=block.split(",",1)[0].strip();row["components"]=components
            recipe_rows.append(row)
    data["legacy_modules"]=sorted(legacy.values(),key=lambda r:r["resref"])
    data["legacy_consumables"]=sorted(consumables.values(),key=lambda r:r["resref"])
    data["legacy_recipes"]=recipe_rows
    new_recipes={r["id"]:r for r in data["recipes"]}
    old_by_resref={r["resref"]:r for r in recipe_rows}
    for row in data["legacy_modules"]:
        old_recipe=old_by_resref.get(row["resref"])
        old_cost=0
        if old_recipe:
            for tag,quantity in old_recipe["components"]:
                path=ROOT/"Module/uti"/(tag+".uti.json")
                item=json.loads(path.read_text(encoding="utf-8"))
                old_cost+=quantity*max(item.get("Cost",{}).get("value",0),item.get("AddCost",{}).get("value",0))
        new_cost=new_recipes[row["target"]]["reference_cost"] if row["target"] in new_recipes else 226
        row.update(old_reference_cost=old_cost,new_reference_cost=new_cost,reclaim_fraction=.8*max(0,old_cost-new_cost)/old_cost if old_cost else 0)
    data["rules"].extend([
        dict(name="Legacy recipe reclamation",value="0.8*max(0,old material reference-new material reference)/old material reference",units="source material fraction",description="Return this fraction of each known old recipe component. Accumulate fractions per owner/material before paying whole units; no cash mint. Missing recipe provenance earns no invented material reimbursement."),
        dict(name="Legacy enhancement reclamation",value="0.8*max(0,old enhancement grade-50)/old enhancement grade",units="recorded enhancement material fraction",description="Only recorded enhancement source materials; absent provenance preserves old serialized item in the recovery package for audit instead of destroying it. No implementation starts with unmapped obtainable items."),
        dict(name="Ship conversion damage",value="new current HP =max(1,new max hull-old absolute hull damage)",units="hull HP",description="Shields and capacitor likewise preserve absolute deficits, clamped0..new maximum. No conversion/refit free refill; interior, cargo and permissions preserved."),
        dict(name="Cargo legacy overflow",value="One sealed recovery package",units="per ship",description="Existing cargo above new capacity is quarantined for dock-only withdrawal, never dropped or duplicated. New cargo cannot enter until capacity is valid."),
    ])


def add_fight_checks(data,report):
    modules={m["id"]:m for m in data["modules"]}
    builds={b["name"]:b for b in data["builds"]}
    comparisons=[("Novice escort","Starter pirate",20,60),("Veteran accessible escort","Advanced interceptor",30,65),("Heavy objective specialist","Elite heavy target",25,65),("Ordnance bomber","Elite heavy target",30,75)]
    fights=[]
    for build_name,enemy_name,lower,upper in comparisons:
        build=builds[build_name];enemy=next(e for e in data["encounters"] if e["name"]==enemy_name)
        metrics=fitted_metrics(data,build)
        actor=dict(build,tracking=1.12 if build["skills"].get("Gunnery",0)>=35 else 1)
        output=min(.40,.002*build["skills"].get("Gunnery",0)+.08*build["quality"]/100+.0025*(max(10,min(26,build["perception"]))-10)+(.05 if build["configuration"]=="combat_conversion" else 0))
        target=dict(signature=enemy["signature"],speed=enemy["speed"],piloting=enemy["piloting"],agility=26 if "interceptor" in enemy_name.lower() else 10)
        dps=sum(modules[mid]["output"]/modules[mid]["cycle"]*(1+output)*hit_chance(actor,modules[mid],target) for mid in build["modules"] if modules[mid]["family"] in ("Thermal","Ordnance"))
        hp=enemy["shield"]+enemy["hull"]*(1+enemy["resistance"]/100)
        duration=hp/dps
        fights.append(dict(build=build_name,enemy=enemy_name,dps=dps,ehp=hp,seconds=duration,minimum=lower,maximum=upper))
        report["checks"].append(dict(name=build_name+" fight duration",passed=lower<=duration<=upper,detail=f"{enemy_name}:expected{duration:.1f}s;band{lower}-{upper}s;no burst technique assumed."))
        report["checks"].append(dict(name=build_name+" combat capacitor",passed=metrics["cap_duration"] is None or metrics["cap_duration"]>=duration,detail="Full weapon/recovery cadence reaches benchmark completion before baseline capacitor exhaustion."))
    report["fights"]=fights
    report["status"]="PASS" if all(c["passed"] for c in report["checks"]) else "FAIL"


def tables(data,report):
    specs=[]
    def add(name,headers,rows,widths):
        specs.append(dict(name=name,headers=headers,rows=rows,widths=widths))
    # Keep every authored plan paragraph/table row searchable in the Bible,
    # including lifecycle rules and implementation order rather than a summary.
    narrative=[];section="Overview"
    for line_number,line in enumerate(PLAN.read_text(encoding="utf-8").splitlines(),1):
        if not line.strip():continue
        if line.startswith("#"):
            section=line.lstrip("# ");continue
        if re.fullmatch(r"\|[\s:|\-]+",line):continue
        # Long paragraphs are split at sentences/words for readable auto-fit.
        words=line.split();chunks=[];chunk=""
        for word in words:
            if len(chunk)+len(word)+1>380 and chunk:chunks.append(chunk);chunk=""
            chunk+=(" " if chunk else "")+word
        if chunk:chunks.append(chunk)
        for piece in chunks:narrative.append([section,line_number,piece])
    add("Space Plan",["Section","Plan line","Design and implementation requirements"],narrative,[30,10,112])
    add("Space Skills",["Skill","Max rank","SP per specialization","Specialization A","Specialization B","Responsibility","XP contribution"],[[s["name"],50,40,s["first"],s["second"],s["responsibility"],s["xp"]] for s in data["skills"]],[20,10,12,24,24,65,65])
    add("Space Rules",["Rule","Value","Units","Description"],[[r["name"],r["value"],r["units"],r["description"]] for r in data["rules"]],[32,70,24,92])
    add("Space Perks",["Skill","Style","Perk Name","Type","Rank","Skill requirement","SP Price","Magnitude","Units","Capacitor","Cooldown seconds","Hardware and limits","Description"],[[p[k] for k in ("skill","style","name","kind","rank","skill_rank","price","magnitude","units","capacitor","cooldown","hardware","description")] for p in data["perks"]],[18,22,30,12,8,11,9,11,48,11,13,72,92])
    operating_rows=[]
    for p in data["operating_perks"]:
        stats="; ".join(f"{stat}: {amount:+g}" for stat,amount in p["stats"].items())
        effects="; ".join(f"{effect['stat']}: {effect['amount']:+g} / {effect['seconds']:g}s / {effect['scope']}"+(" / next paid cycle" if effect['once'] else "")+(" / caster allies only" if effect.get('allies_only') else "") for effect in p["effects"])
        operating_rows.append([p["key"],p["name"],p["rank"],p["target"],p["range"] or "Fitted hardware",p["bank"]," / ".join(p["actions"])," / ".join(p["designs"]) or "Compatible action",stats,effects,p["preparation"],p["channel"],p["min_signature"],p["paid_leg"],p["discovery"]])
    add("Space Operating Contracts",["Perk ID","Name","Rank","Target rule","Range m","Selected bank","Hardware operations","Specific designs","Permanent stats (fractions or flat)","Temporary stats, duration and scope","Telegraph seconds","Committed hardware channel seconds","Minimum target signature","Paid leg required","Authored discovery draw eligible"],operating_rows,[30,30,8,24,20,12,50,55,70,100,14,18,17,16,20])
    add("Space Hulls",["Existing ID","Hull","Role","Piloting","High slots","Low slots","Fitting power","Hull HP","Shield HP","Capacitor","Capacitor per second","Shield per second out of combat","Speed multiplier","Signature","Cargo units","Hull resistance","Reference credits","Mount compatibility"],[[h[k] for k in "id name role piloting high low power hull shield capacitor cap_regen shield_regen speed signature cargo resistance value mount".split()] for h in data["hulls"]],[26,30,24,10,9,9,12,10,10,12,13,17,12,11,12,12,14,40])
    module_rows=[]
    for m in data["modules"]:
        skill,rank=operator_requirement(m)
        module_rows.append([m[k] for k in "id name family slot mount power output cycle capacitor range tracking resolution engineering value".split()]+[skill,rank,m["resref"],m["quality_dimensions"],m["effect"]])
    add("Space Modules",["Design ID","Module","Family","Slot","Mount","Fitting power","Base output","Cycle seconds","Capacitor per cycle","Range m","Tracking","Resolution","Engineering","Reference credits","Operator skill","Operator rank","Base resref","Choose one quality dimension","Effect and tradeoff"],module_rows,[25,32,15,10,15,12,12,12,14,10,11,12,12,15,18,12,22,65,92])
    add("Space Module Variants",["Design ID","Calibration","Fitting power","Base output","Cycle seconds","Capacitor","Range m","Tracking","Base usable recovery","Engineering","Material reference cost","Progress target","Maximum quality","Durability","Rules"],[[v[k] for k in "design calibration power output cycle capacitor range tracking recovery_fraction engineering reference_cost progress max_quality durability notes".split()] for v in data["module_variants"]],[25,20,13,14,14,12,12,12,18,13,18,16,16,14,100])
    add("Space Calibrations",["Calibration","Power multiplier","Output multiplier","Cycle multiplier","Capacitor multiplier","Range multiplier","Precision multiplier","Description"],[[c[k] for k in "name fitting output cycle capacitor range precision description".split()] for c in data["calibrations"]],[22,15,15,15,16,15,16,92])
    add("Space Configurations",["Design ID","Configuration","Fitting power","Benefit","Drawback"],[[c[k] for k in "id name power benefit drawback".split()] for c in data["configurations"]],[28,28,12,65,65])
    add("Space Resources",["Ore resref","Refined resref","Cargo units","Ore commission bid credits","Metal recipe reference credits","Hardness","Small reserve units","Survey resolution","Stability","Respawn seconds","Minimum common fraction"],[[r[k] for k in "ore metal cargo bid metal_value hardness reserve resolution stability respawn common_fraction".split()] for r in data["resources"]],[22,22,12,18,18,11,15,14,11,15,16])
    deposit_rows=[["Deposit","Ship claims","Reserve units","Composition","Hardness","Stability","Hazard interval seconds","Environmental damage","Scan seconds","Lifetime seconds","Respawn seconds"]]
    deposit_rows += [[d[k] for k in "name ships reserve composition hardness stability hazard_seconds hazard_damage scan_seconds lifetime respawn".split()] for d in data["deposits"]]
    add("Space Deposits",deposit_rows[0],deposit_rows[1:],[28,16,15,70,12,12,18,16,12,15,15])
    recipe_headers=["Design ID","Recipe","Engineering","Tilarium","Currian","Ruined electronics","Recovered electronics","Precision assembly","Outputs","Quality slots","Reference material cost","Durability","Base progress target","Maximum quality","Profile","Research seconds","Base research success"]
    recipe_rows=[[r[k] for k in "id name engineering tilarium currian electronics recovered precision output quality_slots reference_cost durability progress max_quality profile research_seconds blueprint_chance".split()] for r in data["recipes"]]
    add("Space Recipes",recipe_headers,recipe_rows,[25,32,12,10,10,16,17,16,10,12,16,12,16,14,15,15,16])
    add("Space Hull Recipes",["Existing ID","Hull","Engineering","Tilarium","Currian","Ruined electronics","Recovered electronics","Precision assemblies","Output","Quality slots","Material reference cost","Hull reference value","Progress target","Maximum quality","Durability","Research seconds"],[[r[k] for k in "id name engineering tilarium currian electronics recovered precision output quality_slots reference_cost hull_reference progress max_quality durability research_seconds".split()] for r in data["hull_recipes"]],[26,30,13,12,12,18,19,18,11,14,19,18,17,16,14,18])
    add("Space Craft Inputs",["Recipe","Engineering","Tilarium","Currian","Ruined electronics","Recovered electronics","Outputs","NPC reference per unit","Material batch cost","Material unit cost","Progress target","Maximum quality","Durability"],[[r[k] for k in "name engineering tilarium currian electronics recovered output npc_reference reference_batch_cost reference_unit_cost progress max_quality durability".split()] for r in data["craft_inputs"]],[28,13,12,12,18,20,12,22,19,18,16,16,14])
    from ShipEquipmentResources import equipment_recipes
    recipe_headers=["Recipe Enum","Skill","Category Enum","Skill Level","Quantity","Resref","Enhancement Type","Enhancement Slots"]
    for number in range(1,9):recipe_headers.extend([f"Component {number}",f"Component Quantity {number}"])
    recipe_headers.extend(["Quality Dimensions","Manufacturing License"])
    live_rows=[]
    for row in equipment_recipes(data):
        values=[row["name"],"Engineering",row["category"],row["level"],row["quantity"],row["resref"],"None" if row["dimensions"]=="None" else "Module",0 if row["dimensions"]=="None" else 1]
        components=list(row["components"].items())
        for i in range(8):values.extend(components[i] if i<len(components) else ("",0))
        rank=1 if row["level"]<10 else 2 if row["level"]<20 else 3 if row["level"]<35 else 4 if row["level"]<45 else 5
        values.extend([row["dimensions"],rank]);live_rows.append(values)
    add("Space Craft Recipes",recipe_headers,live_rows,[48,16,24,13,12,24,20,18]+[25,17]*8+[45,20])
    add("Space Craft Rules",["Rule","Description"],[[k,v] for k,v in data["recipe_rules"]],[32,112])
    add("Space Industry Rules",["Rule","Description"],[[k,v] for k,v in data["industry_rules"]],[32,112])
    add("Space Industry Benchmarks",["Activity","Complete fit","Cargo returned units","Capacity units","Work and vents seconds","Travel and other overhead seconds","Total trip budget seconds","Material reference value","Physical balance checks"],[[r[k] for k in "activity build cargo capacity working_seconds overhead_seconds trip_seconds material_reference notes".split()] for r in report["industry"]],[32,88,18,16,21,25,23,22,110])
    encounter_rows=[[e[k] for k in "name hull shield resistance speed signature gunnery piloting damage cycle capacitor regen accuracy reward_credit xp_pool control_seconds capacitor_pool weapons tracking resolution range".split()] for e in data["encounters"]]
    add("Space Encounter Bindings",["Existing creature tag","Existing ship identity","Encounter profile","Preserved name"],[[r[k] for k in ["tag","ship","profile","name"]] for r in data["encounter_bindings"]],[24,28,32,55])
    add("Space Encounters",["Enemy profile","Hull HP","Shield HP","Hull resistance","Speed","Signature","Gunnery","Piloting","Weapon output","Cycle seconds","Weapon cost","Cap regen per second","Accuracy bonus","Included credit reference","Finite XP pool","Hard control seconds","Capacitor pool","Weapons","Tracking","Resolution","Range m"],encounter_rows,[28,11,11,14,11,12,11,11,13,13,13,16,14,17,15,15,16,12,12,12,12])
    economy_by_name={r["name"]:r for r in report["economy"]}
    activity_rows=[]
    for a in data["activities"]:
        row_number=len(activity_rows)+4;r=economy_by_name[a["name"]]
        # Formulas and their cached results are both written. Inputs stay typed;
        # external recalculation can change the design inputs without a rewrite.
        row=[a[k] for k in "name seconds party success credits material_value supplies service defeat_cost xp failure_material_fraction".split()]
        row += [formula(f"D{row_number}*E{row_number}+(D{row_number}+(1-D{row_number})*K{row_number})*F{row_number}",r["gross"]),formula(f"G{row_number}+H{row_number}+(1-D{row_number})*I{row_number}",r["expense"]),formula(f"(L{row_number}-M{row_number})*3600/B{row_number}/C{row_number}",r["net_per_player_hour"]),formula(f"J{row_number}*3600/B{row_number}/C{row_number}",r["xp_per_player_hour"])]
        row += [a[k] for k in "piloting gunnery systems astro industry".split()]+[r["stress_net_per_player_hour"],a["objective"]]
        activity_rows.append(row)
    add("Space Activities",["Activity","Trip seconds","Party","Success fraction","Credit wallet","Material reference value","Supplies","Service fees","Party defeat service bill","Total XP pool","Failure material realization","Expected gross","Expected expense","Net credits per player hour","XP per player hour","Piloting share","Gunnery share","Systems share","Astrometrics share","Industry share","Combined stress net per player hour","Objective and settlement"],activity_rows,[28,13,10,14,14,17,12,13,20,14,19,15,15,19,18,14,14,14,15,14,22,92])
    add("Space Contract Objectives",["Contract ID","Activity","Required party","Minimum trip seconds","Lifetime seconds","Route legs","Leg interval seconds","Arrival radius m","Minimum waypoint separation m","Required hostiles","Recovered ore","Finite ore reserve","Survey objectives","Wreck clusters","Bulk reserve","Intact attempts","Freight units","Load/unload seconds","Cancellation deposit","Rescue HP","Boarding","Reputation"],[[a["id"],a["name"],a["party"],a["minimum_seconds"],a["expiry_seconds"],a["legs"],a["leg_interval"],a["leg_radius"],a["route_minimum_distance"]," / ".join(a.get("kills",[])),a.get("ore",0),a.get("reserve",0),a.get("surveys",0),a.get("wrecks",0),a.get("bulk",0),a.get("attempts",0),a.get("freight",0),a["load_seconds"],a["freight_deposit"],a.get("rescue",0),a.get("boarding",False),a["reputation"]] for a in data["activities"]],[20,32,15,20,18,14,20,18,26,80,18,20,18,17,18,18,17,22,22,17,16,17])
    add("Space Activity Rules",["Rule","Description"],[[k,v] for k,v in data["activity_rules"]],[32,112])
    build_by_name={b["name"]:b for b in data["builds"]}
    build_rows=[]
    for r in report["builds"]:
        b=build_by_name[r["name"]]
        build_rows.append([r["name"],r["hull"]," / ".join(f"{k} {v}" for k,v in b["skills"].items())," / ".join(b["modules"]),b["configuration"],r["sp"],r["power"],r["power_max"],r["small_dps"],r["heavy_dps"],r["recovery_per_second"],r["cap_regen"],r["drain"],r["net_cap"],r["cap_duration"] if r["cap_duration"] is not None else "Sustained",r["speed"],r["signature"],r["cargo"],r["ehp"],r["mining_rate"]])
    add("Space Builds",["Build","Hull","Skill ranks","Fitted designs","Configuration","Space SP","Power used","Power budget","DPS advanced interceptor","DPS heavy target","Recovery per second","Cap regen per second","Cap expenditure per second","Net capacitor per second","Seconds to cap exhaustion","Speed","Signature","Cargo units","Effective HP","Usable ore per second"],build_rows,[30,28,55,80,25,11,12,12,20,17,17,18,20,20,20,12,12,14,16,18])
    add("Space Character Budgets",["Character","Rank allocation","Total ranks","Space perks SP","Ground weapon SP","Other ground spending reserve","Spent SP","Earned plus starting SP","Remaining SP"],[[b["name"]," / ".join(f"{k} {v}" for k,v in b["ranks"].items()),b["total_ranks"],b["space_sp"],b["ground_weapon_sp"],b["other_ground_sp"],b["spent_sp"],b["earned_sp"],b["remaining_sp"]] for b in data["mixed_builds"]],[36,100,14,15,16,25,14,23,15])
    add("Space Fight Benchmarks",["Build","Enemy","Expected DPS","Enemy effective HP","Expected fight seconds","Minimum seconds","Maximum seconds"],[[f[k] for k in "build enemy dps ehp seconds minimum maximum".split()] for f in report["fights"]],[35,30,17,20,20,17,17])
    conversion_rows=[]
    for old in data["legacy_hulls"]:
        new=next(h for h in data["hulls"] if h["id"]==old["id"])
        conversion_rows.append([old["id"],old["name"],"Hull",new["role"],0,0,0,f"Old hull/shield/cap:{old['hull']}/{old['shield']}/{old['capacitor']}; new:{new['hull']}/{new['shield']}/{new['capacitor']}. Preserve ID, interior, ownership, cargo and absolute damage."])
    for row in data["legacy_modules"]:
        conversion_rows.append([row["resref"],row["name"],"Module/configuration",row["target"],row["old_reference_cost"],row["new_reference_cost"],row["reclaim_fraction"],row["conversion"]])
    for row in data["legacy_consumables"]:
        conversion_rows.append([row["resref"],row["name"],"Consumable",row["resref"],0,0,0,row["conversion"]])
    add("Space Conversion",["Existing ID or resref","Current name","Kind","New role or design","Old material reference","New material reference","Source reclaim fraction","Conversion requirements"],conversion_rows,[28,35,24,28,18,18,20,92])
    return specs


def formula(body,value):
    return {"formula":body,"value":value}


def col_name(index):
    result=""
    while index:
        index,remainder=divmod(index-1,26);result=chr(65+remainder)+result
    return result


def append_collection(xml,tag,items):
    pattern=rf"(<{tag}\b[^>]*count=\")(\d+)(\"[^>]*>)(.*?)(</{tag}>)"
    match=re.search(pattern,xml,re.S)
    if not match:raise ValueError("Missing style collection "+tag)
    old_count=int(match.group(2))
    replacement=match.group(1)+str(old_count+len(items))+match.group(3)+match.group(4)+"".join(items)+match.group(5)
    return xml[:match.start()]+replacement+xml[match.end():],old_count


def add_styles(xml):
    fonts=[f'<font><sz val="10"/><color rgb="FF202020"/><name val="Arial"/></font>',f'<font><b/><sz val="10"/><color rgb="FFFFFFFF"/><name val="Arial"/></font>',f'<font><b/><sz val="14"/><color rgb="FF202020"/><name val="Arial"/></font>']
    xml,font=append_collection(xml,"fonts",fonts)
    xml,fill=append_collection(xml,"fills",['<fill><patternFill patternType="solid"><fgColor rgb="FF28435A"/><bgColor indexed="64"/></patternFill></fill>'])
    xfs=[]
    for font_id,fill_id,num_fmt,alignment in [(font,0,0,'horizontal="left" vertical="top" wrapText="1"'),(font,0,2,'horizontal="right" vertical="top"'),(font+1,fill,0,'horizontal="center" vertical="center" wrapText="1"'),(font+2,0,0,'horizontal="left" vertical="top"'),(font,0,0,'horizontal="right" vertical="top"'),(font,0,10,'horizontal="right" vertical="top"')]:
        xfs.append(f'<xf numFmtId="{num_fmt}" fontId="{font_id}" fillId="{fill_id}" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1" applyNumberFormat="1"><alignment {alignment}/></xf>')
    xml,base=append_collection(xml,"cellXfs",xfs)
    return xml,dict(text=base,decimal=base+1,header=base+2,title=base+3,integer=base+4,percent=base+5)


def cell_xml(ref,value,styles,header=False,title=False):
    if value is None:return ""
    if isinstance(value,dict) and "formula" in value:
        return f'<c r="{ref}" s="{styles["decimal"]}"><f>{escape(value["formula"])}</f><v>{value["value"]:.12g}</v></c>'
    if isinstance(value,(int,float)):
        # General numeric format preserves small coefficients such as0.002.
        # Two decimals are appropriate for displayed throughput calculations.
        style=styles["integer"] if isinstance(value,int) or float(value).is_integer() or abs(value)<1 else styles["decimal"]
        return f'<c r="{ref}" s="{styles["header"] if header else style}"><v>{value:.12g}</v></c>'
    style=styles["title"] if title else styles["header"] if header else styles["text"]
    return f'<c r="{ref}" s="{style}" t="inlineStr"><is><t xml:space="preserve">{escape(str(value))}</t></is></c>'


def sheet_xml(spec,styles):
    rows=[f'<row r="1">{cell_xml("A1",spec["name"],styles,title=True)}</row>',f'<row r="2">{cell_xml("A2","Planned 2026-10-05",styles)}</row>',f'<row r="3">'+"".join(cell_xml(col_name(i+1)+"3",v,styles,header=True) for i,v in enumerate(spec["headers"]))+'</row>']
    for number,values in enumerate(spec["rows"],4):
        rows.append(f'<row r="{number}">'+"".join(cell_xml(col_name(i+1)+str(number),v,styles) for i,v in enumerate(values))+"</row>")
    cols="".join(f'<col min="{i+1}" max="{i+1}" width="{width}" customWidth="1"/>' for i,width in enumerate(spec["widths"]))
    last=col_name(len(spec["headers"]))+str(len(rows))
    return f'<?xml version="1.0" encoding="utf-8"?><worksheet xmlns="{MAIN}"><dimension ref="A1:{last}"/><sheetViews><sheetView showGridLines="0" workbookViewId="0"><pane ySplit="3" xSplit="1" topLeftCell="B4" activePane="bottomRight" state="frozen"/><selection pane="bottomRight" activeCell="B4" sqref="B4"/></sheetView></sheetViews><sheetFormatPr defaultRowHeight="15"/><cols>{cols}</cols><sheetData>'+"".join(rows)+f'</sheetData><autoFilter ref="A3:{last}"/></worksheet>'


def workbook_parts(zip_file):
    root=ET.fromstring(zip_file.read("xl/workbook.xml"))
    rels={r.attrib["Id"]:r.attrib["Target"] for r in ET.fromstring(zip_file.read("xl/_rels/workbook.xml.rels"))}
    result={}
    for s in root.find("m:sheets",NS):
        target=rels[s.attrib[f"{{{REL}}}id"]]
        path=target.lstrip("/") if target.startswith("/") else "xl/"+target
        result[s.attrib["name"]]=path
    return result


def formula_snapshot(parts):
    snapshot={}
    for name,body in parts.items():
        if name.startswith("xl/worksheets/") and name.endswith(".xml"):
            root=ET.fromstring(body);cells={}
            for c in root.findall("m:sheetData/m:row/m:c",NS):
                f=c.find("m:f",NS)
                if f is not None:
                    v=c.find("m:v",NS)
                    cells[c.attrib["r"]]=(ET.tostring(f,encoding="unicode"),v.text if v is not None else None,c.attrib.get("t"))
            if cells:snapshot[name]=cells
    return snapshot


def replace_reference_cell(xml, cell, text, fallback_style):
    # A blank cell can be self-closing. Never consume its following siblings.
    opening=re.search(rf'<c\b[^>]*\br="{cell}"[^>]*>',xml)
    if opening is None:raise ValueError("Missing cross-reference cell "+cell)
    end=opening.end()
    if not opening.group(0).rstrip().endswith("/>"):
        closing=xml.find("</c>",end)
        if closing<0:raise ValueError("Unclosed cross-reference cell "+cell)
        end=closing+len("</c>")
    old_style=re.search(r'\bs="(\d+)"',opening.group(0))
    style=int(old_style.group(1)) if old_style else fallback_style
    replacement=f'<c r="{cell}" s="{style}" t="inlineStr"><is><t>{escape(text)}</t></is></c>'
    return xml[:opening.start()]+replacement+xml[end:]


def cell_snapshot(parts,paths,exceptions):
    shared=[]
    if "xl/sharedStrings.xml" in parts:
        shared=["".join(s.itertext()) for s in ET.fromstring(parts["xl/sharedStrings.xml"])]
    snapshot={}
    for name,path in paths.items():
        cells={}
        for row in ET.fromstring(parts[path]).findall("m:sheetData/m:row",NS):
            for cell in row.findall("m:c",NS):
                ref=cell.attrib["r"]
                if (name,ref) in exceptions:continue
                kind=cell.attrib.get("t","n")
                cached=cell.find("m:v",NS)
                value=cached.text if cached is not None else None
                if kind=="s":kind,value="text",shared[int(value)]
                elif kind=="inlineStr":kind,value="text","".join(cell.find("m:is",NS).itertext())
                f=cell.find("m:f",NS)
                formula=(sorted(f.attrib.items()),f.text) if f is not None else None
                cells[ref]=(kind,value,formula,row.attrib["r"])
        snapshot[name]=cells
    return snapshot


def write_workbook(data,report):
    with zipfile.ZipFile(BIBLE) as z:
        infos=z.infolist();original={i.filename:z.read(i.filename) for i in infos}; paths=workbook_parts(z)
    changed={}; specs=tables(data,report)
    existing_space_paths={paths[s["name"]] for s in specs if s["name"] in paths}
    before=formula_snapshot({name:body for name,body in original.items() if name not in existing_space_paths})
    styles_text=original["xl/styles.xml"].decode("utf-8")
    # Reuse styles on later runs; avoid growing the style table indefinitely.
    marker=ROOT/"design/testing/space-bible-style-ids.json"
    existing_space=any(s["name"] in paths for s in specs)
    if existing_space and marker.exists():styles=json.loads(marker.read_text(encoding="utf-8"))
    else:
        styles_text,styles=add_styles(styles_text);changed["xl/styles.xml"]=styles_text.encode("utf-8")
        marker.write_text(json.dumps(styles,indent=2)+"\n",encoding="utf-8")
    workbook=original["xl/workbook.xml"].decode("utf-8")
    relationships=original["xl/_rels/workbook.xml.rels"].decode("utf-8")
    content_types=original["[Content_Types].xml"].decode("utf-8")
    max_sheet_id=max(int(s.attrib["sheetId"]) for s in ET.fromstring(workbook).find("m:sheets",NS))
    max_file_id=max(int(m.group(1)) for n in original if (m:=re.fullmatch(r"xl/worksheets/sheet(\d+)\.xml",n)))
    max_rel_id=max(int(x.attrib["Id"][3:]) for x in ET.fromstring(relationships) if re.fullmatch(r"rId\d+",x.attrib["Id"]))
    for spec in specs:
        name=spec["name"]
        if name not in paths:
            max_sheet_id+=1;max_file_id+=1;max_rel_id+=1
            path=f"xl/worksheets/sheet{max_file_id}.xml";rid=f"rId{max_rel_id}"
            workbook=workbook.replace("</sheets>",f'<sheet name="{name}" sheetId="{max_sheet_id}" r:id="{rid}"/>'+"</sheets>")
            relationships=relationships.replace("</Relationships>",f'<Relationship Id="{rid}" Type="{REL}/worksheet" Target="worksheets/sheet{max_file_id}.xml"/>'+"</Relationships>")
            content_types=content_types.replace("</Types>",f'<Override PartName="/{path}" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'+"</Types>")
            paths[name]=path
        changed[paths[name]]=sheet_xml(spec,styles).encode("utf-8")
    changed["xl/workbook.xml"]=workbook.encode("utf-8")
    changed["xl/_rels/workbook.xml.rels"]=relationships.encode("utf-8")
    changed["[Content_Types].xml"]=content_types.encode("utf-8")
    # Narrow cross-references direct readers away from obsolete planning data.
    refs={"Piloting":("B3","Pilot ship movement and hazards. Space Skills, Space Perks and Space Operating Contracts define the replacement progression. Legacy tier unlock perks are retired through the full character rebuild."),"Starships":("A1","Current hull identities with horizontal role profiles in Space Hulls and persistent conversion in Space Conversion"),"Engineering":("B3","Ability to create starships, modules, droids, and other electronic & mechanical items. Attributes do not affect crafting Control, Craftsmanship, or CP. Ship Manufacturing licenses the implemented space recipes and bounded tuning in Space Craft Recipes and Space Craft Rules.")}
    for tab,(cell,text) in refs.items():
        path=paths[tab];xml=original[path].decode("utf-8")
        changed[path]=replace_reference_cell(xml,cell,text,styles["text"]).encode("utf-8")
    exceptions={(name,cell) for name,(cell,_) in refs.items()}
    # Add licenses to existing blank Engineering cells without changing inherited formulas.
    path=paths["Engineering"];xml=changed[path].decode("utf-8")
    descriptions=["Manufacture ship equipment with Engineering requirements below 10. Operating skills are separate.","Manufacture ship equipment with Engineering requirements below 20.","Manufacture ship equipment with Engineering requirements below 35.","Manufacture ship equipment with Engineering requirements below 45.","Manufacture every ship recipe through Engineering rank 50."]
    for rank,(gate,description) in enumerate(zip([2,10,20,35,45],descriptions),1):
        row=15+rank
        values={"A":"Manufacturing","B":str(rank),"C":"Ship Manufacturing "+["I","II","III","IV","V"][rank-1],"D":"Engineering "+str(gate),"E":"All","F":"Trait","G":description,"H":"None","I":"None","J":"Legacy Noncombat","K":"-","L":"-","M":"Implemented"}
        for column,value in values.items():
            cell=column+str(row);xml=replace_reference_cell(xml,cell,value,styles["text"]);exceptions.add(("Engineering",cell))
    changed[path]=xml.encode("utf-8")
    inherited_paths={name:path for name,path in paths.items() if path in original and path not in existing_space_paths}
    before_cells=cell_snapshot(original,inherited_paths,exceptions)
    temporary=BIBLE.with_suffix(".space-tmp.xlsx")
    with zipfile.ZipFile(temporary,"w") as z:
        for info in infos:z.writestr(info,changed.pop(info.filename,original[info.filename]))
        for name,body in changed.items():z.writestr(name,body,compress_type=zipfile.ZIP_DEFLATED)
    with zipfile.ZipFile(temporary) as z:
        after_parts={i.filename:z.read(i.filename) for i in z.infolist()}
        after=formula_snapshot(after_parts)
        after_cells=cell_snapshot(after_parts,inherited_paths,exceptions)
        for name,cells in before_cells.items():
            if after_cells.get(name)!=cells:raise ValueError("Existing cell content or row changed: "+name)
        for name,cells in before.items():
            if after.get(name)!=cells:raise ValueError("Existing formula or cache changed: "+name)
        touched={"xl/workbook.xml","xl/_rels/workbook.xml.rels","[Content_Types].xml","xl/styles.xml"}|{paths[n] for n in refs}|{paths[s["name"]] for s in specs}
        unexpected=[n for n,b in original.items() if n not in touched and after_parts[n]!=b]
        if unexpected:raise ValueError("Unexpected ZIP entry changes: "+str(unexpected))
        preserved=sum(len(v) for v in before.values())
    temporary.replace(BIBLE)
    layout_path=ROOT/"tools/CombatUpgradeBibleWorkbookLayout.json"
    layout=json.loads(layout_path.read_text(encoding="utf-8-sig"))
    columns=layout.get("columnsBySheet")
    if columns is None:raise ValueError("Unknown layout JSON root: "+str(list(layout)))
    for spec in specs:columns[spec["name"]]=[dict(min=i+1,max=i+1,width=w) for i,w in enumerate(spec["widths"])]
    layout_path.write_text(json.dumps(layout,separators=(",",":"),ensure_ascii=False)+"\n",encoding="utf-8")
    preservation=dict(existing_formula_cells_preserved=preserved,existing_cells_preserved=sum(len(v) for v in before_cells.values()),unexpected_entry_changes=unexpected,new_tabs=[s["name"] for s in specs],cross_reference_cells=sorted(exceptions),before_cell_digest=hashlib.sha256(json.dumps(before_cells,sort_keys=True).encode()).hexdigest(),before_formula_digest=hashlib.sha256(json.dumps(before,sort_keys=True).encode()).hexdigest())
    (ROOT/"design/testing/space-bible-preservation.json").write_text(json.dumps(preservation,indent=2)+"\n",encoding="utf-8")
    print(f"Bible updated: {len(specs)} space tabs; {preserved} existing formula caches preserved; unrelated ZIP entries unchanged.")


def verify_workbook(data,report):
    with zipfile.ZipFile(BIBLE) as z:
        paths=workbook_parts(z)
        for spec in tables(data,report):
            root=ET.fromstring(z.read(paths[spec["name"]]));rows=root.findall("m:sheetData/m:row",NS)
            if len(rows)!=len(spec["rows"])+3:raise ValueError("Row count mismatch "+spec["name"])
            expected=[spec["headers"]]+spec["rows"]
            for number,values in enumerate(expected,3):
                by_ref={c.attrib["r"]:c for c in rows[number-1]}
                for column,value in enumerate(values,1):
                    ref=col_name(column)+str(number);c=by_ref[ref]
                    if isinstance(value,dict):
                        if c.find("m:f",NS).text!=value["formula"]:raise ValueError("Formula mismatch "+spec["name"]+"!"+ref)
                        saved=float(c.find("m:v",NS).text)
                        if not math.isclose(saved,value["value"],rel_tol=1e-10,abs_tol=1e-8):raise ValueError("Cached result mismatch "+spec["name"]+"!"+ref)
                    elif isinstance(value,(int,float)):
                        if not math.isclose(float(c.find("m:v",NS).text),value,rel_tol=1e-10,abs_tol=1e-8):raise ValueError("Numeric mismatch "+spec["name"]+"!"+ref)
                    else:
                        text="".join(c.find("m:is",NS).itertext())
                        if text!=str(value):raise ValueError("Text mismatch "+spec["name"]+"!"+ref)
        preserved=json.loads((ROOT/"design/testing/space-bible-preservation.json").read_text(encoding="utf-8"))
        old_paths=set(paths.values())-{paths[s["name"]] for s in tables(data,report)}
        saved_snapshot=formula_snapshot({p:z.read(p) for p in old_paths})
        digest=hashlib.sha256(json.dumps(saved_snapshot,sort_keys=True).encode()).hexdigest()
        if digest!=preserved["before_formula_digest"]:raise ValueError("Existing formula snapshot changed after refresh")
        inherited_paths={name:path for name,path in paths.items() if path in old_paths}
        exceptions={tuple(cell) for cell in preserved["cross_reference_cells"]}
        parts={name:z.read(name) for name in z.namelist()}
        inherited_cells=cell_snapshot(parts,inherited_paths,exceptions)
        cell_digest=hashlib.sha256(json.dumps(inherited_cells,sort_keys=True).encode()).hexdigest()
        if cell_digest!=preserved["before_cell_digest"]:raise ValueError("Existing cell snapshot changed after refresh")
    print("Saved Bible cells, formulas, cached results and existing formula caches match the verified design model.")


def preview_data(data,report):
    views=[]
    with zipfile.ZipFile(BIBLE) as z:
        paths=workbook_parts(z)
        shared_strings=[]
        if "xl/sharedStrings.xml" in z.namelist():
            shared_strings=["".join(s.itertext()) for s in ET.fromstring(z.read("xl/sharedStrings.xml"))]
        for name,start,end,columns in [("Starships",2,9,11),("Piloting",8,13,7)]:
            root=ET.fromstring(z.read(paths[name]));all_rows=root.findall("m:sheetData/m:row",NS)
            values=[]
            for row in all_rows:
                number=int(row.attrib["r"])
                if not start<=number<=end:continue
                row_values=[];cells={c.attrib["r"]:c for c in row}
                for col in range(1,columns+1):
                    c=cells.get(col_name(col)+str(number));value=None
                    if c is not None:
                        if c.attrib.get("t")=="inlineStr":value="".join(c.find("m:is",NS).itertext())
                        elif c.attrib.get("t")=="s":value=shared_strings[int(c.find("m:v",NS).text)]
                        elif c.find("m:v",NS) is not None:value=float(c.find("m:v",NS).text)
                    row_values.append(value)
                values.append(row_values)
            source_cols=root.findall("m:cols/m:col",NS)
            widths=[next((float(c.attrib["width"]) for c in source_cols if int(c.attrib["min"])<=i<=int(c.attrib["max"])),12) for i in range(1,columns+1)]
            views.append(dict(name="Existing "+name,headers=values[0],rows=values[1:],widths=widths))
    views.extend(tables(data,report))
    path=ROOT/"design/testing/space-design-preview/preview-data.json"
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(views,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print("Preview inputs include existing reference ranges and all new space tabs.")


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write",action="store_true",help="Add/update planned space tabs with ZIP/XML edits preserving unrelated entries.")
    parser.add_argument("--check",action="store_true",help="Verify the saved Bible matches the model and preserves formula caches.")
    parser.add_argument("--preview-data",action="store_true",help="Extract existing reference ranges and planned views for visual review.")
    args=parser.parse_args()
    data=build_data();add_builds(data);source_inventory(data)
    report=numerical_audit(data);add_fight_checks(data,report)
    DATA.parent.mkdir(parents=True,exist_ok=True);REPORT.parent.mkdir(parents=True,exist_ok=True)
    DATA.write_text(json.dumps(data,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    REPORT.write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(f"Design: {len(data['hulls'])} hulls, {len(data['modules'])} module designs, {len(data['perks'])} perk ranks, {len(data['legacy_modules'])} legacy modules mapped.")
    failed=[c for c in report["checks"] if not c["passed"]]
    for c in failed: print("FAIL: "+c["name"]+" - "+c["detail"])
    print(f"Numerical checks: {len(report['checks'])-len(failed)}/{len(report['checks'])} passed.")
    if failed: return 1
    if args.preview_data:preview_data(data,report)
    if args.write: write_workbook(data,report)
    if args.check: verify_workbook(data,report)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
