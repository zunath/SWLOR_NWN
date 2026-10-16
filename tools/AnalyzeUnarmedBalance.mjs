// Reproducible proposal analysis, not the NWN combat engine or a release test.
// Run: node tools/AnalyzeUnarmedBalance.mjs [--samples 128] [--out full-results.json]
// Combat damage ranges, hit-rate rounding and paired-delay arithmetic are mirrored
// from Service/Combat.cs. Ability hits are seeded Bernoulli trials; landed damage
// uses the expected 5% activated critical mixture; ordinary critical chance is
// 10% at rank50/equal stats. Ordinary rolls use a discrete seeded schedule; every
// activated impact receives Combat.GetAbilityDamageBonus(rank50) = 7 separately.
// This fixed priority rotation is not optimized and does not suspend ordinary
// attacks for casts/animations. Results are offensive ceilings before geometry,
// deflection, concealment, Guard, damage reduction, resistance and enhancement procs.
// No value is assigned to control, Evasion, resistance, outgoing-damage suppression,
// threat or lost enemy actions. Capstones are excluded except opening-power checks.

import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';

const RANKS = [2, 5, 8, 10, 12, 15, 18, 20, 22, 25, 28, 30, 32, 35, 38, 40, 45, 50];
const PRICES = [2, 2, 2, 2, 2, 4, 3, 4, 4, 4, 3, 4, 3, 2, 4, 5, 4, 6];
const SLOTS = ['opener', 'mechanic', 'bridge', 'form', 'opener', 'aim', 'utility', 'stance', 'bridge',
  'refund', 'opener', 'form', 'utility', 'survival', 'form', 'opener', 'bridge', 'capstone'];
const WINDOWS = { martialBase: 4, martialUpgraded: 6, brawlerBase: 6, brawlerUpgraded: 8 };
const SECONDS = 120;
const STEP = 0.25;
const RESERVE = 4;
const ACTIVATION_LOCKOUT = 1;
const AUTO_CRIT = 0.10;
const ABILITY_CRIT = 0.05;
const ABILITY_RANK_BONUS = Math.floor(0.15 * 50);

// RFC-style quoted CSV reader: Bible descriptions contain commas and newlines.
function parseCsv(text) {
  const rows = []; let row = [], cell = '', quoted = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === '"') {
      if (quoted && text[i + 1] === '"') { cell += '"'; i++; }
      else quoted = !quoted;
    } else if (c === ',' && !quoted) { row.push(cell); cell = ''; }
    else if (c === '\n' && !quoted) { row.push(cell.replace(/\r$/, '')); rows.push(row); row = []; cell = ''; }
    else cell += c;
  }
  if (cell || row.length) { row.push(cell.replace(/\r$/, '')); rows.push(row); }
  const headers = rows.shift();
  return rows.filter(r => r.length > 1).map(r => Object.fromEntries(headers.map((h, i) => [h, r[i] ?? ''])));
}
const manifest = parseCsv(readFileSync(new URL('../SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv', import.meta.url), 'utf8'));
function capstoneFromRow(row, setupName) {
  const description = row.Description.replace(/\s+/g, ' ');
  const power = description.match(/^Deals weapon DMG \+\s*(\d+) to one enemy\. Spending (Finish|your Rattled against its enemy) adds \+\s*(\d+) power[.;]/);
  assert.ok(power, `${row.PerkName}: unsupported capstone power description`);
  assert.equal(power[2], setupName, `${row.PerkName}: unexpected setup requirement`);
  assert.match(row.STM, /^\d+(?:\.0)?$/, `${row.PerkName}: STM must be a positive integer`);
  const seconds = (text, field) => {
    const match = text.match(/^(\d+(?:\.\d+)?) seconds?$/);
    assert.ok(match, `${row.PerkName}: ${field} must be expressed in seconds`);
    const value = Number(match[1]);
    assert.ok(value > 0, `${row.PerkName}: ${field} must be positive`);
    return value;
  };
  const cost = Number(row.STM);
  assert.ok(Number.isInteger(cost) && cost > 0, `${row.PerkName}: STM must be a positive integer`);
  assert.match(row.Notes, /\bOne impact\b/, `${row.PerkName}: this analysis requires a single impact`);
  return {
    name: row.PerkName, basePower: Number(power[1]), setupPower: Number(power[3]), cost,
    castSeconds: seconds(row.CastingTime, 'CastingTime'),
    recastSeconds: seconds(row.CooldownTime, 'CooldownTime'),
    source: { manifest: 'SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv', tab: row.Tab, row: Number(row.Row) }
  };
}
const capstoneRows = Object.fromEntries([
  ['martialArtist', 'Perfect Form', 'Finish'],
  ['brawler', 'Haymaker', 'your Rattled against its enemy']
].map(([build, name, setupName]) => {
  const rows = manifest.filter(row => row.Tab === 'Unarmed' && row.PerkName === name);
  assert.equal(rows.length, 1, `Expected one current Bible capstone row for ${name}`);
  return [build, { row: rows[0], setupName }];
}));
const capstones = Object.fromEntries(Object.entries(capstoneRows).map(([build, { row, setupName }]) =>
  [build, capstoneFromRow(row, setupName)]));
for (const prefix of ['Doublehand', 'Dual Wield']) {
  const armorRows = manifest.filter(r => r.Tab === 'Armor' && r.PerkName.startsWith(`${prefix} `));
  assert.equal(armorRows.length, 3, `${prefix} comparison requires all three Armor ranks`);
  assert.equal(armorRows.reduce((sum, r) => sum + Number(r.Price), 0), 9);
  assert.equal(PRICES.reduce((sum, price) => sum + price, 0) + armorRows.reduce((sum, r) => sum + Number(r.Price), 0), 69);
}
const katarRows = manifest.filter(r => r.Tab === 'Katar' && r.Style === 'Scrapper');
assert.equal(katarRows.length, 18, 'Katar comparison must remain an 18-purchase line');
assert.equal(katarRows.reduce((sum, r) => sum + Number(r.Price), 0), 60);
const katarRow = name => {
  const row = katarRows.find(r => r.PerkName === name);
  assert.ok(row, `Missing Bible comparison row: ${name}`);
  return { ...row, Description: row.Description.replace(/\s+/g, ' ') };
};
const plusPower = row => Number(row.Description.match(/weapon DMG \+ (\d+)/)?.[1]);
const katar = Object.fromEntries([
  ['opener', 'Hooking Strike IV'], ['bridge', 'Joint Lock III'], ['utility', 'Interrupting Sweep II']
].map(([id, name]) => {
  const row = katarRow(name);
  return [id, { name, power: plusPower(row), cost: Number(row.STM), cd: Number(row.CooldownTime.match(/[\d.]+/)?.[0]), control: id !== 'utility' }];
}));
assert.deepEqual(Object.values(katar).map(s => [s.power, s.cost, s.cd]), [[38, 12, 18], [36, 10, 24], [30, 9, 30]]);
assert.match(katarRow('Scrapper Control III').Description, /\+12 DMG/);
assert.match(katarRow('Scrapper Control III').Description, /\+7% Ability Accuracy/);
assert.match(katarRow('Breaker Reversal').Description, /\+20 DMG/);

function meanDamage(power, ratio = 1, delta = 0, crit = AUTO_CRIT) {
  // delta is the final formula contribution, 0.35 * attribute difference.
  const max = (power + delta) * Math.max(0.01, Math.min(3.625, ratio));
  const normalLow = power > 0 ? Math.max(1, Math.trunc(max * 0.7)) : Math.trunc(max * 0.7);
  const normalHigh = power > 0 ? Math.max(normalLow, Math.trunc(max)) : Math.trunc(max);
  const criticalLow = power > 0 ? Math.max(1, Math.trunc(max)) : Math.trunc(max);
  const criticalHigh = power > 0 ? Math.max(criticalLow, Math.trunc(max * 1.25)) : Math.trunc(max * 1.25);
  return (normalLow + normalHigh) / 2 * (1 - crit) + (criticalLow + criticalHigh) / 2 * crit;
}
function hitChance(baseHit, flatAccuracy = 0, percentagePoints = 0) {
  // The swept base rates correspond to even ACC-Evasion differences; +3 ACC
  // therefore adds one percentage point, +5 ACC adds two, before the 95% cap.
  return Math.max(0.20, Math.min(0.95, baseHit + Math.floor(flatAccuracy / 2) / 100 + percentagePoints / 100));
}
function interval(paired, haste, delayUnits = 220, offhandReduction = 30) {
  const convert = n => Math.trunc(n / 60 * 1000);
  const reduce = (n, p) => n - Math.trunc(n * p / 100);
  const raw = convert(delayUnits);
  const reference = Math.max(584, raw - 1750);
  const hastedReference = Math.max(584, reduce(raw, Math.max(-50, Math.min(50, haste))) - 1750);
  if (!paired) return hastedReference / 1000;
  const offhand = reduce(delayUnits, offhandReduction);
  const pairUnits = 105 + Math.max(0, delayUnits - 105) + Math.max(0, offhand - 105);
  return Math.max(584, Math.round(Math.max(584, convert(pairUnits) - 1750) * hastedReference / reference)) / 1000;
}
function random(seed) {
  let state = seed >>> 0;
  return () => {
    state += 0x6D2B79F5;
    let n = state;
    n = Math.imul(n ^ n >>> 15, n | 1);
    n ^= n + Math.imul(n ^ n >>> 7, n | 61);
    return ((n ^ n >>> 14) >>> 0) / 4294967296;
  };
}
function partitions(build, id, damage, qualified = false, reversal = false) {
  if (build === 'martialArtist') {
    if (id === 'opener') return [damage + 38];
    if (id === 'bridge') return [damage + 36 + (qualified ? 12 : 0)];
    if (id === 'utility') return Array(2).fill(Math.floor(damage / 2) + 14 +
      (qualified ? (qualified === 'opening' ? 4 : 8) + 6 : 0));
    return capstonePartitions(capstones[build], damage, qualified);
  }
  if (build === 'brawler') {
    if (id === 'opener') return [damage + 38];
    if (id === 'bridge') return [damage + 36 + (qualified ? 16 + 12 : 0)];
    if (id === 'utility') return [damage + 30];
    return capstonePartitions(capstones[build], damage, qualified);
  }
  const spec = katar[id];
  return [damage + spec.power + (spec.control ? 12 + (reversal ? 20 : 0) : 0)];
}
function capstonePartitions(spec, damage, qualified) {
  return [damage + spec.basePower + (qualified ? spec.setupPower + 12 : 0)];
}
function impactPowers(build, id, damage, qualified = false, reversal = false) {
  // Only Practiced Form is a single partitioned activation budget. The engine's
  // ordinary rank bonus remains per impact, including both finisher halves.
  return partitions(build, id, damage, qualified, reversal).map(power => power + ABILITY_RANK_BONUS);
}

function resourceBudget(resource) {
  return resource === 'low'
    ? { might: 10, gear: 0, food: 0, support: 0 }
    : { might: 26, gear: 12, food: 5, support: resource === 'supported' ? 2 : 0 };
}

function trial(options, build, seed) {
  const {
    loadout = 'single', h = 0.85, ext = 0, ratio = 1, delta = 0,
    resource = 'geared', incomingDamageRate = 0.5, reversalUptime = 0.5,
    martialStance = true, martialHaste = 15, martialWindow = 6,
    brawlerStance = true, katarStance = false,
    brawlerWaitForRetaliation = false,
    rotation: requestedRotation = 'sequence', flowingForm = true, dirtyFighting = true
  } = options;
  const rotation = requestedRotation === 'short' && build !== 'martialArtist' ? 'sequence' : requestedRotation;
  const rng = random(seed);
  const paired = loadout === 'dual';
  const damage = paired ? 19 : Math.ceil(19 * 1.6);
  const flowStance = build === 'martialArtist' && martialStance !== false;
  const rollInterval = interval(paired, ext) / (paired ? 2 : 1);
  // Uniform first-roll phase avoids granting a free immediate weave. Paired
  // hands are approximated as one roll every half cycle, not simultaneous hits.
  let nextOrdinary = rng() * rollInterval;
  const window = build === 'martialArtist'
    ? (flowingForm ? WINDOWS.martialUpgraded : WINDOWS.martialBase)
    : (dirtyFighting ? WINDOWS.brawlerUpgraded : WINDOWS.brawlerBase);
  const resources = resourceBudget(resource);
  const max = 10 + Math.floor(resources.might * 3 / 2) + resources.gear;
  const result = { ordinaryDamage: 0, abilityDamage: 0, casts: 0, unfunded: 0, qualified: 0,
    refund: 0, discount: 0, paidSTM: 0, regeneratedSTM: 0, ordinaryAttempts: 0,
    openerHits: 0, weaveAttempts: 0, landedWeaves: 0, generatedDiscounts: 0,
    hasteWindows: 0, evasionWindows: 0, retaliationCharges: 0, retaliationSpends: 0,
    minimumSTM: max, maximumSTM: max, activationTimes: [], flowStance: Number(flowStance) };
  let stm = max, remainder = 0, nextAction = 0, nextRefund = 0, nextDiscount = 0;
  let openingUntil = -1, finishUntil = -1, rattledUntil = -1, discountUntil = -1, weavePending = false;
  let flowHasteUntil = -1, flowEvasionUntil = -1, nextFlowWindow = 0;
  let braceUntil = -1, retaliationUntil = -1, nextRetaliation = 0;
  const nextIncomingDelay = () => -Math.log(1 - rng()) / incomingDamageRate;
  let incomingAt = build === 'brawler' && incomingDamageRate > 0 ? nextIncomingDelay() : Infinity;
  const ready = { opener: 0, bridge: 0, utility: 0 };
  const attemptedCycle = { opener: -1, bridge: -1, utility: -1 };
  const cooled = id => build === 'katar' && katarStance ? katar[id].cd * 1.1 : ({ opener: 18, bridge: 24, utility: 30 })[id];
  const costOf = id => ({ opener: 12, bridge: 10, utility: 9 })[id];
  const restore = amount => {
    const recovered = Math.min(max - stm, amount);
    stm += recovered;
    result.maximumSTM = Math.max(result.maximumSTM, stm);
    return recovered;
  };
  for (let step = 0; step < SECONDS / STEP; step++) {
    const t = step * STEP;
    if (step > 0 && step % (6 / STEP) === 0) {
      const available = 10 + Math.floor(resources.might / 4) + resources.food + remainder;
      result.regeneratedSTM += restore(Math.floor(available / 5));
      remainder = available % 5;
    }
    if (step > 0 && resources.support && step % (4 / STEP) === 0) result.regeneratedSTM += restore(resources.support);

    while (incomingAt <= t) {
      if (incomingAt >= nextDiscount) {
        discountUntil = incomingAt + 8;
        nextDiscount = incomingAt + 18;
        result.generatedDiscounts++;
      }
      if (brawlerStance && rattledUntil > incomingAt && braceUntil > incomingAt && incomingAt >= nextRetaliation) {
        retaliationUntil = rattledUntil;
        braceUntil = -1;
        nextRetaliation = incomingAt + 18;
        result.retaliationCharges++;
      }
      incomingAt += nextIncomingDelay();
    }
    while (nextOrdinary <= t) {
      const at = nextOrdinary;
      const weave = build === 'martialArtist' && flowingForm && weavePending && openingUntil > at;
      const techniqueAccuracy = build === 'martialArtist'
        ? ((openingUntil > at || finishUntil > at) ? 3 : 0)
        : (build === 'brawler' && dirtyFighting && rattledUntil > at ? 3 : 0);
      result.ordinaryAttempts++;
      if (weave) { result.weaveAttempts++; weavePending = false; }
      const landed = rng() < hitChance(h, techniqueAccuracy);
      if (weave && !landed) assert.ok(openingUntil > at, 'A missed optional weave must preserve Opening');
      if (landed) {
        // The weave modifies THIS scheduled ordinary roll. No extra attack or
        // ability rank bonus is added, and a paired second hand cannot repeat it.
        result.ordinaryDamage += meanDamage(damage + (weave ? 16 : 0), ratio, delta, AUTO_CRIT);
        if (weave) result.landedWeaves++;
      }
      // A new window changes only the next budgeted interval, never an already
      // scheduled roll. Paired hands retain the documented half-cycle approximation.
      nextOrdinary += interval(paired, ext + (flowHasteUntil > at ? martialHaste : 0)) / (paired ? 2 : 1);
    }
    if (t < nextAction) continue;

    // State-aware but fixed priorities. Preserve a live finisher first; preserve
    // a live bridge second; otherwise use opener. A base bridge/utility may wait
    // up to six seconds for its predecessor. Full Martial rotation waits for its
    // optional weave attempt; a missed weave never consumes Opening.
    let candidates;
    if (build !== 'katar' && (rotation === 'sequence' || rotation === 'short')) {
      // Deliberately retain all three actions for a 30s combination. The first
      // twelve seconds offer opener, weave, bridge and utility. A missed
      // or unaffordable predecessor does not forbid the later base attacks.
      const cycle = Math.floor(t / 30), offset = t % 30;
      candidates = offset >= 12 ? [] : [
        ...(offset < 1 && attemptedCycle.opener !== cycle ? ['opener'] : []),
        ...(rotation !== 'short' && offset >= 1 && attemptedCycle.bridge !== cycle &&
          (build !== 'martialArtist' || !weavePending || openingUntil <= t) &&
          (build !== 'brawler' || !brawlerWaitForRetaliation || retaliationUntil > t || braceUntil <= t) ? ['bridge'] : []),
        ...((rotation === 'short' ? offset >= 1 && (!weavePending || openingUntil <= t)
          : offset >= 2 && attemptedCycle.bridge === cycle) && attemptedCycle.utility !== cycle ? ['utility'] : [])
      ];
    } else if (build === 'martialArtist') {
      candidates = [
        ...(finishUntil > t ? ['utility'] : []), ...(openingUntil > t && !weavePending ? ['bridge'] : []),
        ...(openingUntil <= t && finishUntil <= t ? ['opener'] : []),
        ...((openingUntil <= t && ready.opener > t + 6) ? ['bridge'] : []),
        ...((finishUntil <= t && ready.bridge > t + 6) ? ['utility'] : [])
      ];
    } else if (build === 'brawler') {
      const canCounter = !brawlerWaitForRetaliation || retaliationUntil > t || braceUntil <= t;
      candidates = [...(rattledUntil > t && canCounter ? ['bridge'] : []), 'opener', ...(canCounter ? ['bridge'] : []), 'utility'];
    } else candidates = ['opener', 'bridge', 'utility'];
    const id = candidates.find(candidate => ready[candidate] <= t);
    if (!id) continue;
    const canDiscount = build === 'brawler' && discountUntil > t;
    const discounted = Math.min(costOf(id), canDiscount ? 2 : 0);
    const paid = costOf(id) - discounted;
    if (stm < paid + RESERVE) {
      result.unfunded++; ready[id] = t + 1;
      if (rotation === 'sequence' || rotation === 'short') attemptedCycle[id] = Math.floor(t / 30);
      continue;
    }
    const qualified = build === 'martialArtist'
      ? (id === 'bridge' ? openingUntil > t : id === 'utility' &&
        (finishUntil > t ? 'finish' : openingUntil > t ? 'opening' : false))
      : build === 'brawler' && id === 'bridge' && rattledUntil > t;
    if (qualified) result.qualified++;
    const retaliationPower = build === 'brawler' && brawlerStance && qualified && retaliationUntil > t ? 8 : 0;
    if (retaliationPower) result.retaliationSpends++;
    // Consume technique at successful payment, before any hit roll.
    if (build === 'martialArtist' && id === 'bridge') { openingUntil = -1; weavePending = false; }
    if (build === 'martialArtist' && id === 'utility') { openingUntil = -1; finishUntil = -1; weavePending = false; }
    if (build === 'brawler' && id === 'bridge') { rattledUntil = -1; braceUntil = -1; retaliationUntil = -1; }
    if (id === 'opener') {
      openingUntil = -1; finishUntil = -1; rattledUntil = -1; weavePending = false;
      braceUntil = -1; retaliationUntil = -1;
    }
    if (canDiscount) discountUntil = -1;
    stm -= paid; result.paidSTM += paid; result.discount += discounted;
    result.minimumSTM = Math.min(result.minimumSTM, stm);
    result.casts++; result.activationTimes.push(t);
    attemptedCycle[id] = Math.floor(t / 30);
    nextAction = t + ACTIVATION_LOCKOUT;
    ready[id] = t + cooled(id);
    const reversal = build === 'katar' && katar[id].control && rng() < reversalUptime;
    const powers = impactPowers(build, id, damage, qualified, reversal);
    if (retaliationPower) powers[0] += retaliationPower;
    const chance = hitChance(h, build === 'martialArtist' && qualified ? 5 : 0, build === 'katar' && katar[id].control ? 7 : 0);
    const landed = powers.map(() => rng() < chance);
    for (let i = 0; i < powers.length; i++) if (landed[i]) result.abilityDamage += meanDamage(powers[i], ratio, delta, ABILITY_CRIT);
    const anyHit = landed.some(Boolean);
    if (id === 'opener' && anyHit) result.openerHits++;
    if (build === 'martialArtist') {
      if (id === 'opener' && anyHit) { openingUntil = t + window; weavePending = flowingForm; }
      else if (id === 'bridge' && qualified && anyHit) finishUntil = t + window;
    }
    if (build === 'brawler' && id === 'opener' && anyHit) {
      rattledUntil = t + window;
      if (brawlerStance) braceUntil = t + 4;
    }
    if (flowStance && id === 'utility' && qualified && anyHit && t >= nextFlowWindow) {
      nextFlowWindow = t + 12;
      flowHasteUntil = qualified === 'finish' ? t + martialWindow : -1;
      flowEvasionUntil = qualified === 'opening' ? t + martialWindow : -1;
      if (qualified === 'finish') result.hasteWindows++;
      else result.evasionWindows++;
    }
    const spender = qualified && ((build === 'martialArtist' && id === 'utility') || (build === 'brawler' && id === 'bridge'));
    if (spender && anyHit && t >= nextRefund) {
      result.refund += restore(Math.min(2, paid));
      nextRefund = t + 12;
    }
    assert.ok(stm >= -1e-9 && stm <= max + 1e-9, 'Finite STM must stay inside its cap');
  }
  assert.ok(result.activationTimes.every((t, i, a) => i === 0 || t - a[i - 1] >= ACTIVATION_LOCKOUT));
  assert.ok(result.weaveAttempts <= result.openerHits && result.landedWeaves <= result.weaveAttempts,
    'At most one ordinary attempt uses the optional weave bonus from each landed opener');
  const fastestRollInterval = interval(paired, ext + (flowStance ? martialHaste : 0)) / (paired ? 2 : 1);
  assert.ok(result.ordinaryAttempts <= Math.ceil(SECONDS / fastestRollInterval),
    'The optional weave cannot add an unscheduled ordinary roll');
  assert.ok(result.retaliationSpends <= result.retaliationCharges && result.retaliationCharges <= result.openerHits);
  assert.ok(result.paidSTM <= max + result.regeneratedSTM + result.refund + 1e-9);
  assert.ok(Math.abs(stm + result.paidSTM - max - result.regeneratedSTM - result.refund) < 1e-9,
    'Paid STM, capped gains and ending STM must conserve the finite resource budget');
  return { ...result, maxSTM: max, endingSTM: stm,
    ordinaryDps: result.ordinaryDamage / SECONDS, abilityDps: result.abilityDamage / SECONDS,
    dps: (result.ordinaryDamage + result.abilityDamage) / SECONDS };
}

function average(options, build, samples) {
  const keys = ['ordinaryDps', 'abilityDps', 'dps', 'casts', 'unfunded', 'qualified', 'ordinaryAttempts',
    'openerHits', 'weaveAttempts', 'landedWeaves', 'generatedDiscounts', 'flowStance',
    'hasteWindows', 'evasionWindows', 'retaliationCharges', 'retaliationSpends',
    'refund', 'discount', 'paidSTM', 'regeneratedSTM', 'endingSTM', 'minimumSTM', 'maximumSTM', 'maxSTM'];
  const aggregate = Object.fromEntries(keys.map(key => [key, 0]));
  for (let i = 0; i < samples; i++) {
    const value = trial(options, build, 0x51A7C0DE + i * 7919);
    for (const key of keys) aggregate[key] += value[key] / samples;
  }
  return { build, ...aggregate };
}
function compare(options, samples) {
  const values = Object.fromEntries(['katar', 'martialArtist', 'brawler'].map(build => [build, average(options, build, samples)]));
  return { options, ...values,
    martialArtistToKatar: values.martialArtist.dps / values.katar.dps,
    brawlerToKatar: values.brawler.dps / values.katar.dps };
}
const range = (rows, key) => [Math.min(...rows.map(r => r[key])), Math.max(...rows.map(r => r[key]))];

// Small deterministic invariants catch balance-data or simulation regressions.
assert.equal(RANKS.length, 18);
assert.equal(new Set(SLOTS).size, 10);
assert.equal(PRICES.reduce((a, b) => a + b), 60);
assert.deepEqual(RANKS.filter((_, i) => SLOTS[i] === 'bridge'), [8, 22, 45]);
assert.deepEqual(RANKS.filter((_, i) => SLOTS[i] === 'form'), [10, 30, 38]);
assert.ok(RANKS[SLOTS.indexOf('opener')] < RANKS[SLOTS.indexOf('bridge')] &&
  RANKS[SLOTS.indexOf('bridge')] < RANKS[SLOTS.indexOf('form')], 'Baseline techniques precede spender power upgrades');
assert.deepEqual(WINDOWS, { martialBase: 4, martialUpgraded: 6, brawlerBase: 6, brawlerUpgraded: 8 });
assert.equal(interval(false, 0), 1.916);
assert.equal(interval(true, 0), 2.733);
assert.equal(interval(true, 0, 220, 0), 3.833);
assert.equal(hitChance(0.85, 5), 0.87);
assert.equal(hitChance(0.95, 5), 0.95);
assert.deepEqual(partitions('martialArtist', 'utility', 31, true), [43, 43]);
assert.deepEqual(partitions('martialArtist', 'utility', 19, true), [37, 37]);
assert.equal(ABILITY_RANK_BONUS, 7);
assert.deepEqual(impactPowers('martialArtist', 'utility', 31, true), [50, 50]);
assert.deepEqual(impactPowers('martialArtist', 'utility', 19, true), [44, 44]);
assert.deepEqual(partitions('martialArtist', 'utility', 31, 'opening'), [39, 39]);
assert.deepEqual(impactPowers('martialArtist', 'utility', 31, 'opening'), [46, 46]);
for (const [build, spec] of Object.entries(capstones)) {
  assert.deepEqual(partitions(build, 'capstone', 31), [31 + spec.basePower], 'Unsequenced capstones gain neither setup nor Form power');
  assert.equal(partitions(build, 'capstone', 31, true)[0] - partitions(build, 'capstone', 31)[0],
    spec.setupPower + 12, 'The setup and Form budgets apply once to the single impact');
  assert.equal(impactPowers(build, 'capstone', 31, true)[0],
    31 + spec.basePower + spec.setupPower + 12 + ABILITY_RANK_BONUS);

  const { row, setupName } = capstoneRows[build];
  const changed = capstoneFromRow({ ...row,
    Description: `Deals weapon DMG + ${spec.basePower + 3} to one enemy. Spending ${setupName} adds +${spec.setupPower + 5} power.`,
    STM: String(spec.cost + 2), CastingTime: `${spec.castSeconds + 0.5} seconds`, CooldownTime: `${spec.recastSeconds + 1} seconds`
  }, setupName);
  assert.equal(capstonePartitions(changed, 31, true)[0] - capstonePartitions(spec, 31, true)[0], 8,
    'Changing manifest powers must change the reported capstone total');
  assert.equal(changed.cost, spec.cost + 2);
  assert.equal(changed.castSeconds, spec.castSeconds + 0.5);
  assert.equal(changed.recastSeconds, spec.recastSeconds + 1);
  assert.throws(() => capstoneFromRow({ ...row, Description: 'Unrecognized power formula' }, setupName), /unsupported capstone power/);
  assert.throws(() => capstoneFromRow({ ...row, STM: '-' }, setupName), /STM must be a positive integer/);
  assert.throws(() => capstoneFromRow({ ...row, CastingTime: 'instant' }, setupName), /CastingTime must be expressed in seconds/);
}
assert.equal(meanDamage(31), 26.85);
assert.ok(Math.abs(meanDamage(31, 1, 0, ABILITY_CRIT) - 26.425) < 1e-10);
assert.equal(trial({ resource: 'low', incomingDamageRate: 0 }, 'brawler', 1).generatedDiscounts, 0);
const baseTechniqueCheck = trial({ flowingForm: false }, 'martialArtist', 1);
assert.equal(baseTechniqueCheck.weaveAttempts, 0);
assert.ok(baseTechniqueCheck.qualified > 0, 'The four-second baseline Opening can qualify the bridge without the rank5 passive');

const args = process.argv.slice(2);
const valueAfter = flag => args.includes(flag) ? args[args.indexOf(flag) + 1] : undefined;
const samples = Number(valueAfter('--samples') ?? 128);
assert.ok(Number.isInteger(samples) && samples >= 16 && samples <= 4096, '--samples must be an integer from 16 to 4096');
const reference = ['single', 'dual'].map(loadout => compare({ loadout }, samples));
const core = [];
for (const loadout of ['single', 'dual']) for (const h of [0.65, 0.85, 0.95]) for (const ext of [0, 15, 30, 50]) {
  core.push(compare({ loadout, h, ext }, samples));
}
const stress = [];
for (const loadout of ['single', 'dual']) for (const h of [0.65, 0.85, 0.95])
  for (const ratio of [0.5, 1, 2, 3.625]) for (const delta of [-7, 0, 7]) for (const resource of ['low', 'geared', 'supported']) {
    stress.push(compare({ loadout, h, ratio, delta, resource }, samples));
  }
const incomingSensitivity = [];
for (const loadout of ['single', 'dual']) for (const incomingDamageRate of [0, 0.25, 0.5, 1]) for (const reversalUptime of [0, 0.5, 1]) {
  incomingSensitivity.push(compare({ loadout, incomingDamageRate, reversalUptime }, samples));
}
const stanceSensitivity = ['single', 'dual'].flatMap(loadout => [
  compare({ loadout, brawlerStance: false }, samples),
  compare({ loadout, martialStance: false }, samples),
  compare({ loadout, katarStance: true }, samples)
]);
const rotationSensitivity = ['single', 'dual'].map(loadout => compare({ loadout, rotation: 'priority' }, samples));
const lowerRouteSensitivity = ['single', 'dual'].map(loadout => compare({ loadout, rotation: 'short' }, samples));
const retaliationSensitivity = ['single', 'dual'].flatMap(loadout => [0, 0.25, 0.5, 1].map(incomingDamageRate =>
  compare({ loadout, incomingDamageRate, brawlerWaitForRetaliation: true }, samples)));
const stanceTuningSensitivity = [
  { martialHaste: 10, martialWindow: 6 },
  { martialHaste: 15, martialWindow: 6 },
  { martialHaste: 20, martialWindow: 6 }
].map(tuning => {
  const rows = core.map(row => compare({ ...row.options, ...tuning }, samples));
  return { tuning, martialArtistToKatar: range(rows, 'martialArtistToKatar'),
    reference: ['single', 'dual'].map(loadout => compare({ loadout, ...tuning }, samples)) };
});
const openingPowerChecks = Object.fromEntries(['single', 'dual'].map(loadout => {
  const d = loadout === 'single' ? 31 : 19;
  const powers = resolver => ({
    jab: resolver('martialArtist', 'opener', d),
    palmStrike: resolver('martialArtist', 'bridge', d, true),
    orderedFinisher: resolver('martialArtist', 'utility', d, true),
    unconditionedFinisher: resolver('martialArtist', 'utility', d),
    rattledUppercut: resolver('brawler', 'bridge', d, true),
    perfectForm: resolver('martialArtist', 'capstone', d, true),
    haymaker: resolver('brawler', 'capstone', d, true)
  });
  return [loadout, {
    adjustedMainD: d,
    engineRankBonusPerImpact: ABILITY_RANK_BONUS,
    capstoneActivations: capstones,
    ordinaryWeave: [d + 16],
    authored: powers(partitions),
    impact: powers(impactPowers),
    openingRouteFinisher: { authored: partitions('martialArtist', 'utility', d, 'opening'),
      impact: impactPowers('martialArtist', 'utility', d, 'opening') }
  }];
}));
const summary = {
  method: 'Seeded discrete ordinary/ability hit trials with expected critical damage; finite120sSTM; fixed30s ordered/weave rotation versus Katar cooldown priorities; offensive ceiling before engine/animation/avoidance/CC value; capstones excluded from all simulated DPS rotations',
  samplesPerBuildCase: samples,
  prices: { line: 60, armor: 9, comparisonTotalSP: 69, purchasesPerLine: 18, distinctPerksPerNewLine: 10 },
  defaults: { h: 0.85, ext: 0, ratio: 1, delta: 0, resource: 'geared', incomingDamageRate: 0.5,
    reversalUptime: 0.5, martialStance: true, martialHaste: 15, martialWindow: 6,
    brawlerStance: true, katarStance: false, rotation: 'sequence' },
  assumptions: [
    'Capstone opening-power checks use current Bible manifest base/setup power, STM, cast and recast values. Their totals include Form and rank bonuses, exclude Retaliation and external boosts, and do not contribute to any simulated DPS row.',
    'One hostile target remains in range; no target/equipment/area interruptions.',
    'Main-hand weapon D19/Delay220 at rank50; single +60% rounds once to D31; dual has two D19 ordinary attacks and Dual Wield III.',
    'All activated powers use captured main D; the two finisher impacts partition D with floor(D/2), and split Form12 as six each.',
    'Each activated impact then gains the normal rank50 ability bonus7, including each finisher half. Generic external flat boosts are excluded, not changed into per-cast bonuses.',
    'Ordinary critical chance is10% at rank50/equal PER-VIT; activated critical chance is the distinct baseline5% with no added critical buffs.',
    'Jab itself grants Opening4s/+3 ordinaryACC. Palm Strike spends Opening directly and grants Finish only on a sequenced landed hit. No mandatory ordinary weave gate.',
    'The rank5 Flowing Form passive extends stages to6s and offers16 power on the first existing ordinary attempt while Opening remains. This bonus is spent even on a miss, but Opening survives. Spending Opening before that attempt forfeits the bonus. No extra attack, rank bonus, queued replacement or second-hand copy is granted.',
    'Ordinary attempts use a uniform sampled starting phase. Paired hands alternate at half-cycle intervals; real engine batching, hand ordering and animation suppression are not reproduced.',
    'Martial Opening/Finish last4s before Flowing Form and6s afterward; either grants3 ordinaryACC. Dirty Jab applies its own Rattled on hit for6s before Dirty Fighting and8s afterward.',
    'The full rotation spends Finish for8 extra power per finisher half. A lower route can spend Opening directly for4 per half; either route partitions Form12, gains Measured Technique and qualifies the capped refund. Perfect Form requires Finish for its conditional bonus.',
    'Payback uses positive direct hostile HP-damage events per second, modeled as Poisson with0/.25/.5/1 sensitivity. It does not depend on melee attempts or the setup debuff. Self/friendly/DoT/reflect events are excluded from the supplied rate.',
    'Flow Stance has no permanent speed, Evasion or damage modifier. A landed finisher spending Finish grants15%Haste6s; spending Opening grants20%Evasion/15%MovementSpeed6s. Windows are exclusive and share12s gate. Only the Haste window affects this offensive model, after the next already-budgeted roll.',
    'Bruiser retains10%PhysicalDefense and has no ordinary damage penalty. Its landed opener opens4s brace against this sole target; a qualifying direct HP-damage event arms8 power for the next Rattled spender, once/opener with18s grant gate. Retaliation is spent at payment even if the spender misses.',
    'Katar Control12 and +7 percentage points apply only to Hooking Strike/Joint Lock; null-status Interrupting Sweep receives neither.',
    'Katar Reversal20 is sampled independently at the explicit per-control-cast uptime, separately from Payback incoming damage.',
    'Scrapper stance is off in primary offensive comparison because its benefit is control duration and its cost is 10% longer hostile cooldowns.',
    'Flat ACC traits use even baseline ACC-Evasion differences; +3 adds1pp, +5 adds2pp before the95% cap.',
    'Unarmed retains one opener, bridge and utility per30s within a12s strategy window. Martial waits for its optional scheduled weave attempt, then spends Opening whether it hit or missed. Brawler normally uses0/1/2s actions; a separate counter policy waits at most4s for retaliation before spending. Neither policy is optimized.',
    'Ordinary attacks continue during instantaneous abilities; actual animation/action suppression can lower output. No rotation optimization.',
    'The ordered rotation skips an unfunded opportunity; cooldown-priority rotations retry after 1s. Neither pays nor starts the real cooldown. A 4-STM reserve is retained. Refunds cap at paid STM and maximum STM.',
    'The same 1s activation spacing is applied to all compared rotations. Unarmed refunds have a 12s ICD; only isolated lines are simulated, so hybrid shared-exclusion and cross-line refund collisions require separate implementation tests.'
  ],
  openingPowerChecks,
  reference,
  coreCases: core.length,
  coreRanges: { martialArtistToKatar: range(core, 'martialArtistToKatar'), brawlerToKatar: range(core, 'brawlerToKatar') },
  stressCases: stress.length,
  stressRanges: { martialArtistToKatar: range(stress, 'martialArtistToKatar'), brawlerToKatar: range(stress, 'brawlerToKatar') },
  incomingCases: incomingSensitivity.length,
  incomingRanges: { martialArtistToKatar: range(incomingSensitivity, 'martialArtistToKatar'), brawlerToKatar: range(incomingSensitivity, 'brawlerToKatar') },
  stanceSensitivity,
  rotationSensitivity,
  lowerRouteSensitivity,
  retaliationSensitivity,
  stanceTuningSensitivity
};
if (valueAfter('--out')) writeFileSync(valueAfter('--out'), JSON.stringify({ ...summary, core, stress, incomingSensitivity }, null, 2) + '\n');
const rounded = value => Number(value.toFixed(4));
const compactComparison = row => ({
  options: row.options,
  martialArtistToKatar: rounded(row.martialArtistToKatar),
  brawlerToKatar: rounded(row.brawlerToKatar),
  builds: Object.fromEntries(['katar', 'martialArtist', 'brawler'].map(build => [build,
    Object.fromEntries(['dps', 'casts', 'qualified', 'ordinaryAttempts', 'weaveAttempts', 'landedWeaves', 'flowStance', 'hasteWindows', 'evasionWindows', 'retaliationCharges', 'retaliationSpends', 'refund', 'discount', 'endingSTM'].map(key => [key, rounded(row[build][key])]))]))
});
console.log(JSON.stringify({
  method: summary.method, samplesPerBuildCase: samples, prices: summary.prices,
  reference: reference.map(compactComparison), coreCases: core.length, coreRanges: summary.coreRanges,
  stressCases: stress.length, stressRanges: summary.stressRanges,
  incomingCases: incomingSensitivity.length, incomingRanges: summary.incomingRanges,
  stanceSensitivity: stanceSensitivity.map(compactComparison),
  rotationSensitivity: rotationSensitivity.map(compactComparison),
  lowerRouteSensitivity: lowerRouteSensitivity.map(compactComparison),
  retaliationSensitivity: retaliationSensitivity.map(compactComparison),
  stanceTuningSensitivity: stanceTuningSensitivity.map(row => ({ tuning: row.tuning,
    martialArtistToKatar: row.martialArtistToKatar, reference: row.reference.map(compactComparison) })),
  openingPowerChecks,
  fullResultsPath: valueAfter('--out') ?? null
}, null, 2));
