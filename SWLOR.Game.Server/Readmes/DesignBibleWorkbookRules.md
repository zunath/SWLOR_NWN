# Design Bible Workbook Rules

These rules apply when editing any Design Bible workbook.

## Human Readability

- Size columns and rows so a human can read the meaningful cell contents without manually resizing the sheet.
- Use wrap text, column width, and row height together for long descriptions, notes, requirements, formulas, and section labels.
- Do not hide important information behind clipped text, compressed rows, or narrow columns.

## Alignment And Styling

- Match each tab's existing alignment pattern instead of applying a blanket style.
- Keep descriptive text, names, notes, and narrative fields aligned consistently with nearby rows on the same tab.
- Keep compact status, category, numeric, and lookup fields aligned consistently with nearby rows on the same tab.
- Use bold for headers, section labels, subtotals, totals, and established emphasis patterns.
- Do not bold ordinary data rows unless the surrounding section already uses bold for that row type.

## Formula Integrity

- Preserve formulas, shared formulas, data validations, filters, and lookup ranges when adjusting rows, columns, or formatting.
- When adding rows or columns, extend formulas to every relevant new cell and verify the copied formula references the intended row, column, sheet, and range.
- When deleting or moving rows or columns, verify formulas still point at the intended source cells and no references became broken.
- Do not replace formula-backed cells with static values unless the sheet pattern explicitly calls for a hand-entered cell.
- Do not add formulas to hand-entered override cells unless the sheet pattern explicitly calls for formula-driven data.

## Handoff Checks

- Run any workbook-specific refresh, export, generation, or audit scripts after editing a Design Bible workbook.
- For `SWLOR Design Bible - Combat Upgrade.xlsx`, `tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` applies `tools/FormatCombatUpgradeBibleWorkbook.ps1` before regenerating audit data. Keep every tab aligned with `tools/CombatUpgradeBibleWorkbookLayout.json`, keep perk-table tabs on the compact layout with narrow `Style`/`SP Price`/metadata columns and a wide `Description` column, keep `Notes` columns at least `45` width units wide, and do not keep fixed row-height bloat that prevents Google Sheets from auto-fitting visible text.
- Run focused tests for any generated data, formulas, manifests, or sync checks affected by the workbook.
- For tabs with established formula regression coverage, keep tests resolving sheets by tab name and treat shared-formula cells as formula-backed even when the formula body is stored only on the shared source cell.
