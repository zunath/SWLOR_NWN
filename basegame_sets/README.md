# basegame_sets

Verbatim copies of every tileset `.set` file (restype `2013`) shipped with NWN:EE, extracted from
the base-game BIF archives (`nwn_base.key` + `nwn_retail.key`, Steam install). These files are not
modified in any way from what ships with the game.

## Why these are checked in

The procedural area generator (`SWLOR.Game.Server\Service\AreaGenerationService`) needs a tileset's
`.set` data (terrain/edge/corner vocabulary, groups, door slots, pathnodes) at two different times:

- **Offline** — `SWLOR.ProcgenReview` and `SWLOR.ContentBuilder` parse `.set` files directly off disk
  to compose and preview generated areas without a running game client, and the AreaGeneration test
  suite parses them to verify tileset coverage/classification logic. These tools have no ResMan and
  no access to the game's BIF archives, so they need the `.set` data available as plain files
  somewhere under the repo.
- **Runtime** — the live game server reads tileset data through NWN's own resource manager
  (`ResManGetFileContents`, see `AreaGeneration.GetTilesetModel`), which already resolves every
  base-game tileset from the client/server-installed BIFs. Runtime loading is untouched by this
  folder and does not read from it.

`SWLOR_Haks` (a separate git submodule) already carries a handful of vanilla-derived tileset copies
that SWLOR customized for its own generation tilesets (e.g. `tds01`, `tdt01`). This folder is the
generic complement: every other base-game tileset, unmodified, so any of them can be onboarded into
the generator's offline tooling without needing a hak customization first. `TilesetSetSource`
(`SWLOR.Game.Server\Service\AreaGenerationService\TilesetSetSource.cs`) resolves a tileset's `.set`
file by checking `SWLOR_Haks` first (hak copies win, since they may carry SWLOR-specific edits) and
falling back to this folder.

## Provenance

- Source: NWN:EE (Steam) `nwn_base.key` + `nwn_retail.key` BIF archives, restype `2013` (`.set`).
- Extracted verbatim — no edits, no SWLOR customization.
- Filenames match the tileset resref (e.g. `tdc01.set` = Crypt).
