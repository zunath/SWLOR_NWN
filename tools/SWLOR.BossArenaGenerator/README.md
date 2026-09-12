# Boss arena authoring batch

Creates the eight remaining capstone boss arenas using the SWLOR toolset's area-generation components. The batch supplies a compact two-zone macro layout (staging entrance and combat floor) to `TileResolver`, then uses the existing decoration planner, document populator, and `NewAreaWriter`. It does not add a new generator UI preset or change the toolset's existing dungeon layouts.

Each arena uses the tileset recommended in `CapstoneQuestLinePlan.md`, three existing quest-gated master encounters, a recovery waypoint, and an arena entrance. Wardens belong in the future adjoining dungeons. Dungeon travel is deliberately left for the area builder; these files do not connect level-50 encounters to existing public areas.

## Generate

Build once with server deployment disabled:

```powershell
dotnet build tools/SWLOR.BossArenaGenerator/SWLOR.BossArenaGenerator.csproj -p:RunPostBuildEvent=Never
dotnet tools/SWLOR.BossArenaGenerator/bin/Debug/net10.0/SWLOR.BossArenaGenerator.dll <repository> <unpacked-hak-root> <preview-output> <NWN-install-root>
```

Use absolute directory arguments. HAK sources and the installed game are read only; the repository's module receives the generated areas. The HAK order comes from its module IFO. The installed game supplies base tile models, WOKs, and map artwork. Source HAKs must match the module's resource version.

Creation rejects existing areas across the entire batch before writing and holds the module's shared write lock. To regenerate, use a separate module copy without these eight areas, then review and import the result. There is no overwrite mode; edit existing arenas in the toolset. Entry and boss positions are grounded against walkable WOK faces; sampled path checks require access from the entrance to every encounter anchor while allowing clearance around the planned decorations. This is a static geometry check, not an in-engine pathfinding or combat test.

## Validate and package

The packaging pass needs Python and Pillow, plus the repository's existing `nwn_gff.exe` and `nwn_erf.exe`:

```powershell
python tools/SWLOR.BossArenaGenerator/package.py <preview-output> --repository <repository>
```

This validates registration, native float tokens, GIC alignment, the 24 master encounters and their existing blueprints, and the absence of ambient creatures/procedural loot. It adds eight entry waypoint blueprints to the existing waypoint palette and connects the placed entry instances to those blueprint resrefs. It packs and unpacks every resource through the native GFF converter and compares the resulting data, then produces:

- `capstone-boss-arenas.erf`: eight ARE/GIT/GIC triplets plus eight entrance UTWs (32 resources), for importing into the SWLOR module.
- `boss-arenas-overview.png` and individual area previews, with entrance/recovery, activator, and boss-spawn markers.
- `manifest.json`, `validation.json`, and `erf-contents.txt` for the area-builder handoff.

Run packaging after every generation. `tjsb0` supplies no 2D map graphics, so its overview tile is explicitly marked as a schematic. Generated RGBA previews, native intermediate resources, and the ERF belong under ignored `artifacts/`; the source ARE/GIT/GIC/UTW files and module registrations are checked in normally. A full module repack is required when deploying these changes.
