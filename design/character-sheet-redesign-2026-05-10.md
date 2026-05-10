# Character Sheet Redesign

Date: 2026-05-10

Visual mock-up: [character-sheet-redesign-2026-05-10-mockup.html](character-sheet-redesign-2026-05-10-mockup.html)

## Goal

Replace the current dense four-column character sheet with a tabbed sheet that keeps the required display values and every existing action entry point intact.

The current implementation has:

- A player/associate payload path for opening the sheet.
- A portrait/name rail with portrait customization.
- HP, FP, STM, SP, AP/Level, attributes, weapon damage estimates, attack, accuracy, evasion, physical defense, force defense, elemental resistance summary, control, and craftsmanship.
- Action buttons for Skills, Perks, Quests, Appearance, Recipes, HoloCom, Key Items, Currencies, Achievements, Notes, Open Trash, and Settings.
- Refresh support for portrait, skill XP, equipment changes, player status, status effects, and beast XP.

I did not find Xenomech UI source in this workspace, so this proposal uses the Xenomech-style pattern described in the request: a persistent identity rail with a tabbed detail area and dense stat pages.

## Proposed Layout

Window geometry: approximately `800 x 460`, resizable/collapsible like the current sheet.

The window is split into two major areas:

1. Left identity rail, always visible.
2. Center tabbed detail area.
3. Right actions rail, always visible.

### Left Identity Rail

Purpose: keep the character context visible while tabs change.

Contents:

- Character name.
- Portrait image.
- Race and character type for player sheets.
- HP, FP, STM compact resource rows.
- SP and AP/Level compact rows when applicable.
- Customize button, using the existing portrait/customize action.

Player mode:

- Shows race, character type, SP, AP, Customize.
- Shows the persistent actions rail.

Associate mode:

- Shows name, portrait, HP/FP/STM, SP/Level when beast data exists.
- Hides player-only launch actions exactly as the current definition does.

### Tabs

Use the existing `GuiToggles` pattern from the Skills UI: a watched integer property drives `BindIsVisible` groups.

Tabs:

- Stats
- Resistances
- Crafting

Default tab: Stats.

## Stats Tab

This tab satisfies "stats shown on one page." It should contain all current stat data except the expanded resistance table, which gets its own tab.

Suggested arrangement: three compact columns.

Column 1: Attributes

- Might, Perception, Vitality, Willpower, Agility, Social.
- Preserve the existing `+` buttons, visibility rules, tooltips, upgrade modals, AP/racial bonus behavior, caps, and migration-area restrictions.

Column 2: Combat

- Main Hand DMG with existing estimated damage tooltip.
- Off Hand DMG with existing estimated damage tooltip.
- Attack.
- Accuracy.
- Evasion.
- Physical DEF.
- Force DEF.

Notes:

- Keep the current color coding for HP/FP/STM in the identity rail.
- Keep current stat tooltips, but update compact slash-format tooltips to name the order directly.
- Do not show the old `Elem. RES` compact row on this tab once the Resistances tab exists.
- Do not repeat species, build type, SP, or AP in this tab because those are already visible in the identity rail.

## Crafting Tab

This tab gives Control and Craftsmanship room to grow as more crafts are added.

Suggested table columns:

- Craft
- Control
- Craftsmanship

Initial rows:

- Smithery
- Engineering
- Fabrication
- Agriculture

This should be implemented as a row-based layout rather than a slash-delimited string so future craft types can be added without changing the page structure.

## Resistances Tab

This tab satisfies "resistances shown on another page."

Show all eight resistance types from `ResistanceType`:

- Fire
- Poison
- Electrical
- Ice
- Mind
- Mobility
- Trauma
- Disruption

Suggested table columns:

- Resistance
- Score
- Damage Taken
- Status Duration

Values:

- Score uses `Resistance.GetResistance(target, type)`.
- Damage Taken uses the same curve as `Resistance.CalculateResistanceDamageMultiplier(target, type)`, rendered as a percentage. Example: `63% taken / 37% reduced`.
- Status Duration should use the same rules as `Resistance.CalculateResistedTicks`; if implementation does not expose a clean multiplier, show a concise qualitative label based on score instead of pretending exactness.

Why this matters:

- The current sheet only shows Fire / Poison / Electrical / Ice as a slash string.
- The combat system now has eight valid resistance types, so the sheet should expose all eight explicitly.

## Actions Rail

Actions are heavily used, so they should not be hidden behind a tab. The right rail is always visible while the player switches between Stats, Resistances, and Crafting.

Every current button must still exist.

The actions rail must have a vertical scrollbar when the window is resized smaller and the full button list no longer fits. In NUI implementation, this should be a bounded group/column using `NuiScrollbars.Y` or equivalent so action buttons remain reachable at small heights.

Group: Progression

- Skills
- Perks
- Recipes

Group: Records

- Quests
- Key Items
- Currencies
- Achievements
- Notes

Group: Character

- Appearance
- Settings

Group: Utility

- HoloCom
- Open Trash

Behavior to preserve:

- HoloCom remains disabled or guarded in space.
- Open Trash continues creating/interacting with `reo_trash_can` and then making it unusable.
- Recipes opens in recipe-browse mode with invalid skill filter.
- The bottom-left Customize button uses the current target payload.
- Appearance uses the player payload.
- All buttons remain hidden for associate mode unless already supported by current behavior.

## Functionality Mapping

| Current item | New location |
| --- | --- |
| Name | Identity rail |
| Portrait | Identity rail |
| Customize portrait | Identity rail |
| Race | Identity rail |
| Character type | Identity rail |
| HP / FP / STM | Identity rail |
| SP / AP / Level | Identity rail |
| Might / Perception / Vitality / Willpower / Agility / Social | Stats tab |
| Attribute upgrade buttons | Stats tab |
| Main Hand / Off Hand DMG | Stats tab |
| Attack / Accuracy / Evasion | Stats tab |
| Physical DEF / Force DEF | Stats tab |
| Fire / Poison / Electrical / Ice resistance | Resistances tab |
| Mind / Mobility / Trauma / Disruption resistance | Resistances tab |
| Control / Craftsmanship | Crafting tab |
| Skills | Actions rail |
| Perks | Actions rail |
| Quests | Actions rail |
| Appearance | Actions rail |
| Recipes | Actions rail |
| HoloCom | Actions rail |
| Key Items | Actions rail |
| Currencies | Actions rail |
| Achievements | Actions rail |
| Notes | Actions rail |
| Open Trash | Actions rail |
| Settings | Actions rail |

## NUI Skinning Feasibility

NWN NUI does not appear to support a true per-window skin/theme system like CSS or the classic `.gui` files. In this codebase, the available styling hooks are mostly:

- Window chrome flags: transparent window and show/hide border.
- Widget foreground color through `StyleForegroundColor`.
- Images and image buttons using resource references.
- Draw lists for lines, filled shapes, text, and images attached to widgets.
- Native group/list/text borders and scrollbars.

Because of that, the mock-up can be matched structurally and can be pushed toward the same dark sci-fi panel style, but it should not be treated as pixel-perfect skinning. Native buttons, scrollbars, tabs/toggles, titlebar behavior, padding, and hover states will still look like NWN NUI unless we replace them with image/draw-list based controls.

Recommended implementation path:

- First pass: keep native NUI window chrome, implement the tabbed layout, identity rail, always-visible scrollable action rail, stat tab, resistance tab, and crafting tab. Use existing colors, borders, spacing, and resource images where practical.
- Skin pass: if the native look is too far from the mock-up, hide the NUI border and/or make the window transparent, then draw panel backdrops, separators, section headers, and selected-tab highlights with draw lists or image resources.
- Avoid replacing every action button with a custom image button unless the visual gap is unacceptable; that would increase maintenance and risk around click targets, disabled states, and accessibility.

Implementation notes:

- The right action rail can and should use a bounded `GuiGroup` with vertical scrollbars so all actions remain reachable when the player resizes the sheet smaller.
- Drawn backgrounds may require extending the local draw-list wrapper so items can render before the target widget content. The low-level NUI helpers support draw-list order, but the current higher-level wrapper does not expose that order on draw-list item classes.
- If the titlebar is hidden, the sheet needs explicit in-content close/collapse controls so players do not lose basic window management.

## Implementation Sketch

No implementation code should be written until this design is accepted.

When implementing:

- Add `SelectedTabId` to `CharacterSheetViewModel`.
- Add computed booleans such as `IsStatsTabVisible`, `IsResistancesTabVisible`, and `IsCraftingTabVisible`, or compare the selected tab directly through helper properties.
- Call `WatchOnClient(model => model.SelectedTabId)` in `Initialize`.
- Replace the root definition with a left identity rail, center tab column, and right action rail.
- Make the right action rail a scrollable group so resizing the character sheet smaller does not hide action buttons.
- Reuse the existing click handlers and stat properties wherever possible.
- Add resistance binding lists or explicit string properties for the eight resistance rows.
- Add crafting binding lists or explicit rows for Control and Craftsmanship by craft type.
- Refresh resistance values from equipment and status-effect refresh paths.

## Acceptance Checklist

- The sheet opens for the player from the disabled character sheet panel.
- The sheet opens for associates owned by the player.
- The Stats tab shows all current non-resistance, non-crafting stats on one page.
- The Resistances tab shows all eight resistance types.
- The Crafting tab shows Control and Craftsmanship by craft type.
- All existing action buttons still have an entry point.
- The action buttons are visible without changing tabs.
- The action rail scrolls vertically when the resized window cannot fit every action button.
- Attribute upgrades still work, including racial stat bonus behavior and AP cost behavior.
- HoloCom still blocks use in space.
- Open Trash still opens the temporary trash placeable.
- Existing refresh events still update the same values they update today.
