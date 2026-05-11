# Agent Rules

## Design Bible

- After editing `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`, run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` to refresh `SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv` and `SWLOR.Game.Server/Readmes/CombatUpgradePerkAudit.csv` from the local workbook.

## TLK Entries

- New custom TLK strings must use a pre-existing empty TLK slot or gap before appending new IDs at the end of `SWLOR_Haks/swlor2_tlk/swlor2_tlk.tlk.json`.
- NWN custom TLK references in 2DA files use `16777216 + tlkId`. When moving or adding a TLK entry, update every 2DA/reference to the matching custom strref.
- After editing `swlor2_tlk.tlk.json`, regenerate `swlor2_tlk.tlk` before building or handing off the change.
