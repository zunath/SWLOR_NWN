# NPC portrait validity

Run `python tools/audit_npc_portraits.py` and
`python -m unittest discover -s tools/tests -p "test_*portrait*.py"` from the repository root.
Initialize `SWLOR_Haks` first. No installed game or Python imaging libraries are needed
for these checks.

The audit covers every UTC blueprint, every creature instance in area GIT files,
and literal portrait overrides in beast definitions. A valid portrait provides
Large, Medium, Small, and Tiny images. Huge is intentionally absent from the custom
pack and uses the client's Large fallback. The NUI conversation uses Large.
Creature `Portrait` resrefs take precedence over numeric `PortraitId` values.
2DA rows are addressed by their physical index, not their decorative label.

`nwn_stock_portraits.txt` records stock TGA/DDS resource names extracted from the
installed NWN:EE KEY resource tables, with source KEY hashes. It contains no game
artwork. Custom assets are checked only in configured HAK source directories
loaded by the module, excluding staging files and unused HAKs, rather than
an allowlist of portrait IDs.

The companion HAK change restores ten missing sizes, recorded with hashes in
`portrait_size_repairs.csv`. These supplement the original 8,109 DDS conversions;
the original conversion/orientation manifests remain unchanged. The beast IDs and
their source data remain unchanged because their missing image resources are restored.

Deploy by rebuilding `sw_portrait.hak` and repacking the module. Both client HAK
content and server module data are required. These offline checks do not replace
an in-game conversation smoke test.
