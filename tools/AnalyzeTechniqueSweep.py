"""Paired stationary PvE/PvP sweep ablation, using production abilities and perk stats.

Build SWLOR.CombatSim first. Outputs compact reproducible JSON; deletes temporary inputs.
"""
import argparse
import concurrent.futures
import copy
import hashlib
import json
import pathlib
import statistics
import subprocess
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
P = argparse.ArgumentParser()
P.add_argument('--simulator', type=pathlib.Path, default=ROOT / 'SWLOR.CombatSim/bin/Release/net10.0/SWLOR.CombatSim.dll')
P.add_argument('--runs', type=int, default=200)
P.add_argument('--packages', default='base69,twin89,spear89,mixed99,mixed109,payback113,whirl99,whirl123')
P.add_argument('--scope', default='pve,pvp,duel')
P.add_argument('--out', type=pathlib.Path, required=True)
A = P.parse_args()
assert 16 <= A.runs <= 4096
assert A.out.suffix == '.json'
assert A.simulator.is_file()

TWIN = ['Momentum', 'SpinningRhythm:3', 'FlowingFootwork', 'SweepingAdvance']
SPEAR = ['HighGuard:3', 'ImprovedAttentiveness', 'OpportunistsFlow', 'RestorationStrike']
PACKAGES = {'base69': [], 'twin89': TWIN, 'spear89': SPEAR,
            'mixed99': TWIN + ['HighGuard:1', 'ImprovedAttentiveness', 'OpportunistsFlow', 'RestorationStrike'],
            'mixed109': TWIN + SPEAR, 'payback113': TWIN + SPEAR + ['Payback'],
            'whirl99': TWIN + ['SpinningWhirl:3'], 'whirl123': TWIN + SPEAR + ['Payback', 'SpinningWhirl:3']}
SELECTED_PACKAGES = A.packages.split(',')
assert len(set(SELECTED_PACKAGES)) == len(SELECTED_PACKAGES) and all(name in PACKAGES for name in SELECTED_PACKAGES)
SCOPES = A.scope.split(',')
assert set(SCOPES) <= {'pve', 'pvp', 'duel'} and SCOPES


def rotation(member, mode):
    if mode == 'short':
        member['abilities'] = [
            {'feat': 'FinishingCombination2', 'requiredSetup': 'Opening', 'waitForWeave': True},
            {'feat': 'Jab4', 'requiredSetup': 'None', 'minimumStaminaBeforeUse': 21}]
    elif mode == 'full':
        member['abilities'] = [use for use in member['abilities']
                               if not (use['feat'] == 'FinishingCombination2' and use.get('requiredSetup') == 'Opening')]
    if 'SpinningWhirl:3' in member['perks']:
        member['abilities'].insert(0, {'feat': 'SpinningWhirl3', 'target': 'Self'})


def scenarios():
    baseline = json.loads((ROOT / 'SWLOR.CombatSim/Scenarios/unarmed-pvp-martial-brawler.json').read_text())
    katar = json.loads((ROOT / 'SWLOR.CombatSim/Scenarios/unarmed-pvp-martial-katar.json').read_text())['teams'][1]['members'][0]
    result = []
    for package in SELECTED_PACKAGES:
        extras = PACKAGES[package]
        for mode in ['short', 'full', 'adaptive']:
            for count in [1, 2, 3]:
                scenario = copy.deepcopy(baseline)
                scenario['name'] = f'pve-{package}-{mode}-{count}targets'
                scenario['isPvp'] = False
                owner = scenario['teams'][0]['members'][0]
                owner['perks'] += extras
                if package == 'mixed99': owner['perks'].remove('RootedTechnique')
                rotation(owner, mode)
                owner['maxHp'] = 1000000
                owner['hpRegenPer30Seconds'] = 0
                enemy = copy.deepcopy(owner)
                enemy.update(name='pressure', count=count, isNpc=True, perks=[], abilities=[])
                enemy['weaponCombat'].update(weaponDamage=19, stance='None', allowTechniqueAbilities=False)
                scenario['teams'][1]['members'] = [enemy]
                scenario['description'] = ('600s sustain ablation, reusing previous Unarmed D31/Delay220, '
                    '805HP PvP and D19 pressure calibration. Million-HP pools only prevent truncation; '
                    'these are diagnostic targets, not new enemy content. All combatants are within5m. '
                    'Current versus identical rules with sweep disabled. Fixed rotations, no movement, '
                    'LOS obstacles, Guard, deflection or native attack animation timing.')
                if 'pve' in SCOPES: result.append(scenario)
            for reverse in [False, True]:
                scenario = copy.deepcopy(baseline)
                scenario['name'] = f'pvp-{package}-{mode}-2v2' + ('-reversed' if reverse else '')
                owner = scenario['teams'][0]['members'][0]
                owner['perks'] += extras
                if package == 'mixed99': owner['perks'].remove('RootedTechnique')
                rotation(owner, mode)
                ally = copy.deepcopy(scenario['teams'][1]['members'][0]); ally['name'] = 'brawler ally'
                scenario['teams'][0]['members'].append(ally)
                scenario['teams'][1]['members'].append(copy.deepcopy(katar))
                scenario['description'] += ' Two-versus-two ablation: martial plus brawler versus brawler plus katar. Added perks are purchased packages, not equal-SP claims. All combatants are stationary within5m.'
                if reverse: scenario['teams'].reverse()
                if 'pvp' in SCOPES: result.append(scenario)
    # Existing duel calibration must be seed-for-seed identical because no secondary exists.
    for pair in (['martial-brawler', 'martial-katar'] if 'base69' in SELECTED_PACKAGES and 'duel' in SCOPES else []):
        result.append(json.loads((ROOT / f'SWLOR.CombatSim/Scenarios/unarmed-pvp-{pair}.json').read_text()))
    return result


def compact(summary):
    return {key: value for key, value in summary.items() if key not in ['FightEndTimes', 'FirstDeathTimes']} | {
        'MedianFightSeconds': statistics.median(summary['FightEndTimes'])}


with tempfile.TemporaryDirectory(prefix='swlor-technique-sweep-') as temp:
    scratch = pathlib.Path(temp)
    rules = json.loads(subprocess.check_output(['dotnet', str(A.simulator), 'rules', '--name', 'current'], cwd=ROOT, text=True))
    rules['Name'] = 'sweep-disabled'; rules['EnableTechniqueSweeps'] = False
    rules_path = scratch / 'rules.json'; rules_path.write_text(json.dumps(rules))
    inputs = scenarios()

    def run(entry):
        index, scenario = entry
        path = scratch / f'{index}.json'; path.write_text(json.dumps(scenario))
        output = scratch / f'{index}-result.json'
        process = subprocess.run(['dotnet', str(A.simulator), 'run', str(path), '--rules', 'current',
            '--rules-file', str(rules_path), '--runs', str(A.runs), '--seed', '1', '--json', str(output)],
            cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=300)
        if process.returncode: raise RuntimeError(scenario['name'] + '\n' + process.stdout)
        summaries = json.loads(output.read_text())
        control = '-1targets' in scenario['name'] or '-full-' in scenario['name'] or scenario['name'].startswith('Unarmed PvP:')
        if control:
            normalize = lambda value: {key: item for key, item in value.items() if key != 'RuleSetName'}
            assert normalize(summaries[0]) == normalize(summaries[1]), 'Sweep changed an unchanged control: ' + scenario['name']
        print(scenario['name'], flush=True)
        result = {'scenario': scenario, 'unchangedControl': control, 'results': [compact(s) for s in summaries]}
        path.unlink(); output.unlink()
        return result

    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as executor:
        results = list(executor.map(run, enumerate(inputs)))
    report = {'method': 'Paired seed1..N current versus sweep-disabled counterfactual. Source-backed perks; stationary geometry and rotations. No prediction of live win rates.',
              'runsPerRulePerCase': A.runs, 'caseCount': len(results), 'totalFights': len(results) * A.runs * 2,
              'binaryHashes': {name: hashlib.sha256((A.simulator.parent / name).read_bytes()).hexdigest()
                               for name in ['SWLOR.CombatSim.dll', 'SWLOR.Game.Server.dll']},
              'counterfactualRules': rules, 'cases': results}
    A.out.parent.mkdir(parents=True, exist_ok=True)
    A.out.write_text(json.dumps(report, indent=2) + '\n')
    print(f'COMPLETE {report["totalFights"]} fights', flush=True)
