# Design Bible Workbook Rules

These rules apply when editing any Design Bible workbook.

## Keeping It In Sync

- Agents keep the repository copy, `design/bible/SWLOR Design Bible.xlsx`, in sync. When a change alters anything the Bible records (stats, balance values, descriptions, recipes, enemy rows), update the matching workbook rows on the same branch.
- The owner uploads Bible changes to the Google copy. Agents never edit, upload to, or drive the Google copy, and never hand that upload back as a to-do.
- Run `tools/UpdateCombatUpgradeAudit.ps1` with `-RefreshLocalBible`, never `-RefreshBible`: the latter pulls from the Google copy, which lacks local changes until the owner uploads them.

## Editing The File

- Never edit a workbook with `openpyxl` or any library that rewrites the whole file. It drops cached formula results and silently breaks formula-backed tabs and tests such as `NPCEnemyBalanceAuditTests`.
- Edit only the target cells at the zip/XML level (for example `<c r="G31" s="..." t="inlineStr"><is><t>TEXT</t></is></c>`) and copy every other zip entry byte-for-byte.
- Separate branches that edit the same workbook cannot be merged; keep one request's workbook edits on one branch.
- After editing `design/bible/SWLOR Design Bible.xlsx`, run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible`. It regenerates shared artifacts, so run it only in your own worktree.

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
- For `SWLOR Design Bible.xlsx`, `tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` applies `tools/FormatCombatUpgradeBibleWorkbook.ps1` before regenerating audit data. Keep every tab aligned with `tools/CombatUpgradeBibleWorkbookLayout.json`, keep perk-table tabs on the compact layout with narrow `Style`/`SP Price`/metadata columns and a wide `Description` column, keep the `Force` tab's `Alignment` and `Affinity Shift` columns compact with its wide `Description` column on column `I`, keep `Notes` columns at least `45` width units wide, keep `Beast Calcs` condensed enough that columns `A:AE` fit in a normal Sheets viewport, and do not keep fixed row-height bloat that prevents Google Sheets from auto-fitting visible text.
- Run focused tests for any generated data, formulas, manifests, or sync checks affected by the workbook.
- For tabs with established formula regression coverage, keep tests resolving sheets by tab name and treat shared-formula cells as formula-backed even when the formula body is stored only on the shared source cell.
