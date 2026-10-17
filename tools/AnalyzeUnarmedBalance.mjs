// Runs the current production-backed combat simulator, replacing the proposal-only damage model.
// Build SWLOR.CombatSim first with -p:RunPostBuildEvent=Never.
// node tools/AnalyzeUnarmedBalance.mjs [--samples 200] [--seed 1] [--simulator path.dll] [--out results.json]
import assert from 'node:assert/strict';
import { existsSync, readFileSync, writeFileSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve, extname, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2);
const options = {};
for (let i = 0; i < args.length; i += 2) {
  assert.ok(['--samples', '--seed', '--simulator', '--out'].includes(args[i]), `Unknown option ${args[i]}`);
  assert.ok(args[i + 1] && !args[i + 1].startsWith('--'), `Missing value for ${args[i]}`);
  assert.ok(!(args[i] in options), `Duplicate option ${args[i]}`);
  options[args[i]] = args[i + 1];
}
const runs = Number(options['--samples'] ?? 200);
const seed = Number(options['--seed'] ?? 1);
assert.ok(Number.isInteger(runs) && runs >= 16 && runs <= 4096, '--samples must be 16..4096');
assert.ok(Number.isInteger(seed) && seed >= -2147483648 && seed + runs - 1 <= 2147483647, 'Seed range exceeds Int32');
const simulator = resolve(options['--simulator'] ?? join(root, 'SWLOR.CombatSim/bin/Debug/net10.0/SWLOR.CombatSim.dll'));
assert.ok(existsSync(simulator), `Build the simulator first: dotnet build SWLOR.CombatSim/SWLOR.CombatSim.csproj -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly. Missing ${simulator}`);
const output = options['--out'] ? resolve(options['--out']) : null;
if (output) {
  assert.equal(extname(output).toLowerCase(), '.json', '--out must be a JSON report');
  assert.ok(!output.toLowerCase().startsWith((join(root, 'SWLOR.CombatSim/Scenarios') + sep).toLowerCase()), 'Cannot overwrite scenario inputs');
}
const scratch = mkdtempSync(join(tmpdir(), 'swlor-unarmed-pvp-'));
const cases = [];
try {
  for (const pair of ['martial-brawler', 'martial-katar', 'brawler-katar']) {
    const source = join(root, `SWLOR.CombatSim/Scenarios/unarmed-pvp-${pair}.json`);
    const original = JSON.parse(readFileSync(source, 'utf8'));
    for (const reverse of [false, true]) {
      const scenario = structuredClone(original);
      if (reverse) { scenario.teams.reverse(); scenario.name += ' (reversed order)'; }
      const input = join(scratch, 'scenario.json');
      const result = join(scratch, 'result.json');
      writeFileSync(input, JSON.stringify(scenario));
      const child = spawnSync('dotnet', [simulator, 'run', input, '--rules', 'current', '--runs', String(runs), '--seed', String(seed), '--json', result, '--detail'], {
        cwd: root, encoding: 'utf8', timeout: 300000, maxBuffer: 16 * 1024 * 1024
      });
      if (child.error || child.status !== 0) throw new Error(child.error?.message || child.stderr || child.stdout || `Simulator exited ${child.status}`);
      const summary = JSON.parse(readFileSync(result, 'utf8'));
      cases.push({ scenario, runs, seed, summary });
      process.stdout.write(child.stdout);
    }
  }
  if (output) writeFileSync(output, JSON.stringify({
    method: 'Current production-backed stationary PvP simulations with reversed team order. See scenario descriptions for calibration and limitations.',
    simulator, totalFights: cases.length * runs, cases
  }, null, 2) + '\n');
} finally {
  assert.equal(dirname(resolve(scratch)), resolve(tmpdir()), 'Scratch path escaped the temporary directory');
  assert.ok(scratch.startsWith(join(tmpdir(), 'swlor-unarmed-pvp-')), 'Unexpected scratch directory');
  rmSync(scratch, { recursive: true, force: true });
}
