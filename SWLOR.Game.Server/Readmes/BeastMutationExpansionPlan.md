# Beast Mutation Expansion Plan

## Purpose

Add a new incubation mutation tier that rewards players who use DNA from prior incubation results, while keeping mutation routing and requirements out of the public Design Bible.

The public Bible remains the source for beast identity and stat rows. A committed private workbook becomes the source for mutation paths, mutation requirements, bad outcome routing, approved visual selections, and rejected visual assets.

## Approval Gates

Work must proceed through these gates in order.

1. Plan and design doc review.
   - This file is the first gate artifact.
   - No Bible cleanup, private workbook generation, CLI changes, beast rows, generated definitions, TLK changes, or hak changes happen until this gate is approved.

2. Public Bible cleanup review.
   - Remove mutation columns from the public `Beast Levels` sheet.
   - Keep public beast identity, stat, role, appearance, portrait, soundset, scaling, and incubation data.
   - Do not include mutation parents, mutation results, weights, enzyme colors, enzyme counts, day requirements, or bad outcome mapping in the public Bible.
   - Update workbook layout and validation expectations as needed, then stop for review.

3. Private workbook output review.
   - Create the committed private workbook and populate the initial mutation and visual source data.
   - Include all existing first-stage mutation paths and requirements.
   - Include the new second-stage mutation paths and bad outcome mapping.
   - Include the approved visual roster and rejected visual assets.
   - Stop for review before CLI changes, generated beast definitions, enum changes, DNA labels, TLK changes, or hak changes.

Implementation starts only after these three gates are approved.

## Source Of Truth

### Public Bible

Path: `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`

The public Bible should list beast identity and stats only. Mutation requirement metadata must be removed from public-facing tabs.

The public Bible should retain rows for:

- Existing tamable beasts.
- Existing incubation beasts.
- New good second-stage beasts.
- New bad outcome beasts.

### Private Mutation Workbook

Proposed path: `design/bible/SWLOR Design Bible - Private Source Data.xlsx`

This workbook is committed to the repo. It is private by convention, not by repository access controls.

Required sheets:

- `Mutation Requirements`
- `Bad Outcome Roles`
- `New Beast Visual Roster`
- `Rejected Visual Assets`

`Mutation Requirements` owns all mutation routing and requirements, including existing first-stage mutations and the new second-stage line.

`Bad Outcome Roles` maps each beast role to its bad outcome beast pool. A role can have more than one row when it has multiple failure beasts.

`New Beast Visual Roster` records the selected unique appearance, portrait, and soundset for each new beast, with quality notes.

`Rejected Visual Assets` records rejected appearance, portrait, and soundset candidates with rejection reasons, so those assets can be considered for later content cleanup.

## CLI Pipeline

Retire `SWLOR.CLI/InputFiles/beast_levels.tsv` from the beast generation path.

Update the beast generation command so `SWLOR.CLI` reads directly from:

- Public Bible beast stat and identity data.
- Private workbook mutation and visual data.

Use in-repo OpenXML or `ZipArchive` parsing rather than adding a spreadsheet dependency unless implementation proves that unreasonable.

The `--beast` command should generate beast definitions from the workbook sources directly. Do not keep an optional TSV export mode.

## Eligibility

The new mutation tier is only available from existing incubation-only beasts that currently have no outgoing mutation paths.

Current source data showed 66 eligible incubation-only parent beasts. Tamable beasts do not qualify as parents for this new tier.

Bad outcome beasts are dead ends and do not qualify as parents for this tier or future automatic continuation unless explicitly redesigned later.

## New Beast Scope

Create:

- 66 unique good second-stage beasts, one per eligible incubation parent.
- 7 shared bad outcome beasts: one per beast role, plus an additional Balanced failure beast.

Current beast roles:

- Balanced
- Bruiser
- Damage
- Evasion
- Force
- Tank

Each good second-stage beast should have a bespoke, lore-flavored identity that stands on its own. The parent DNA is the incubation input and eligibility gate, not the naming or visual template for the result. Avoid parent-derived names, mechanical tier names such as `Greater`, `Advanced`, or roman numerals, and simple upgraded versions of existing creatures.

Each bad outcome beast should also have an in-world name, with malformed or unstable biological flavor. Avoid explicit labels such as `Bad Mutation`.

## Mutation Outcome Rules

Use the existing mutation methodology:

1. The incubation job rolls whether any mutation occurs.
2. If mutation succeeds, eligible mutation outcomes are selected by weight.

Do not add a separate hard-coded bad outcome roll.

For each second-stage parent:

- Good outcome weight: `90`
- Role-matched bad outcome weight: `10`

This makes bad outcomes about 10 percent of successful second-stage mutation rolls.

Existing first-stage mutation weights remain unchanged.

## Requirement Rules

The private workbook owns requirement data for all mutation paths.

Existing first-stage mutations should be refreshed with this mix:

- 70 percent enzyme-only.
- 20 percent day-only.
- 10 percent enzyme plus day.

New second-stage mutations should use this mix:

- 50 percent enzyme plus day.
- 40 percent enzyme-only.
- 10 percent day-only.

Most paths should require specific enzyme type, color, and count. A smaller share should require days of the week. Some paths, especially in the new tier, should require both.

Existing first-stage requirements should change days, enzyme colors, enzyme types, counts, and similar requirement details without changing existing first-stage outcome weights.

## Stat Tuning

Compare new beast strength against current incubation-mutant baselines by role.

Good second-stage beasts should land about 12.5 percent above the current incubation-mutant baseline for their role at max purities. Acceptable target range is 10 to 15 percent.

Bad outcome beasts should land about 25 percent below the current incubation-mutant baseline for their role. Acceptable target range is 20 to 30 percent below.

Apply tuning through beast level data, including base stats and max purity bonus columns. Do not add special-case combat logic.

## Visual Asset Rules

All new beasts must use existing content assets. Do not add new art, portrait, sound, model, or hak assets for this work.

Every new beast must have a unique:

- Appearance
- Portrait
- Soundset

Use only assets that are currently unused by beast definitions where possible, and select assets that fit the new beast identity and role.

Visual quality is a required review criterion. Avoid low-quality, poorly scaled, mismatched, broken, or visually weak assets even if they are technically unused.

The private workbook's `New Beast Visual Roster` sheet must be reviewed before any generated beast rows or definitions are created.

## Public Data Rules

The public Bible may show that the new beasts exist and what their stats are.

The public Bible must not show:

- Parent mutation source.
- Mutation result mapping.
- Mutation weights.
- Bad outcome mapping.
- Enzyme colors.
- Enzyme counts.
- Enzyme types.
- Day-of-week requirements.
- Any other mutation acquisition requirement.

## Implementation Outline

After the approval gates:

1. Update public Bible structure.
2. Create and populate the private workbook.
3. Update `SWLOR.CLI` beast generation to read the public and private workbooks.
4. Add new `BeastType` enum values after the current highest beast ID.
5. Generate and review beast definition output.
6. Copy generated definitions into `IncubationBeastDefinition`.
7. Add DNA subtype rows and player-facing labels for every new beast.
8. Add TLK entries using existing empty TLK slots or gaps before appending.
9. Regenerate TLK binary after TLK JSON changes.
10. Validate parent repo and `SWLOR_Haks` changes separately.

## Validation Checklist

Minimum validation after implementation:

- Public Bible no longer contains mutation requirement columns.
- Private workbook contains all first-stage and second-stage mutation paths.
- Private workbook contains all visual roster and rejected asset rows.
- CLI reads workbook sources and no longer requires `beast_levels.tsv`.
- Generated definitions include expected mutation paths, weights, requirements, roles, appearances, portraits, soundsets, and level stats.
- All new good beasts and bad outcome beasts have DNA subtype labels.
- All new TLK labels use valid custom TLK references.
- Focused beast generation/data validation passes.
- `dotnet build --no-restore -p:RunPostBuildEvent=Never` passes from the repo root.
- Focused beast/Bible tests pass.
- Parent repo and `SWLOR_Haks` submodule statuses are reported separately.

## Non-Goals

- Do not add new visual or audio assets.
- Do not add a special bad mutation roll outside the existing weighted mutation selection.
- Do not change existing first-stage mutation weights.
- Do not allow tamable beasts to qualify as parents for the new tier.
- Do not let bad outcome beasts continue the mutation chain.
