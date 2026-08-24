# BeastCodeBuilder

## Overview
The BeastCodeBuilder generates C# beast definition classes from the Design Bible workbooks. Public beast identity and stat progression come from `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`. Mutation requirements come from `design/bible/SWLOR Design Bible - Private Source Data.xlsx`.

## Command
```bash
RunCLI.cmd -b
RunCLI.cmd --beast
```

## Inputs
- Public workbook tab `Beast Levels`: one row per beast level.
- Private workbook tab `Mutation Requirements`: one row per mutation outcome.
- Templates in `SWLOR.CLI/Templates`.

The retired `SWLOR.CLI/InputFiles/beast_levels.tsv` export is no longer part of the beast generation path.

## Output
```text
SWLOR.CLI/OutputBeasts/
  TamableBeastDefinition/
  IncubationBeastDefinition/
```

Generated files are copied into the matching server definition folders after review.

## Generated Code Features
- Uses `BeastType`, `AbilityType`, role, appearance, portrait, soundset, and scale data from `Beast Levels`.
- Emits level-specific HP, STM, FP, attributes, damage, attack delay, combat bonuses, defenses, and resistances.
- Reads the `iprp_delay.2da` cost-table row from `Attack Delay` on every `Beast Levels` row and emits the matching `ItemPropertyAttackDelay` enum member. A species keeps one explicit delay across all 50 levels, while its per-hit damage is balanced for that cadence.
- Emits mutation outcomes, weights, enzyme requirements, and day-of-week requirements from the private workbook.
- Keeps mutation requirements out of the public Design Bible workbook.
