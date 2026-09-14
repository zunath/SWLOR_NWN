# Store Instance Sync

Placed NWN stores embed their own store data and their own item copies. Updating a
`Module/utm` store blueprint or `Module/uti` item blueprint does not update already
placed `Module/git` store instances.

Run the check before handing off store or item blueprint changes:

```powershell
powershell -ExecutionPolicy Bypass -File tools/SyncStoreInstances.ps1 -Check
```

Run the sync to repair placed store instances:

```powershell
powershell -ExecutionPolicy Bypass -File tools/SyncStoreInstances.ps1
```

Run blueprint creation when a placed store has no matching `Module/utm` source:

```powershell
powershell -ExecutionPolicy Bypass -File tools/SyncStoreInstances.ps1 -CreateMissingBlueprints
```

After changing the CLI tool itself, add `-Build` once to rebuild the CLI before
running it:

```powershell
powershell -ExecutionPolicy Bypass -File tools/SyncStoreInstances.ps1 -Check -Build
```

The sync uses:

- `Module/utm` as the source for store fields and store inventory membership.
- `Module/uti` as the source for every embedded item field and property.
- Existing placed store coordinates/orientation as instance metadata to preserve.
- Store item slot metadata (`Infinite`, `Repos_PosX`, `Repos_Posy`) from the source
  `utm` entry, with item orientation/position preserved from the placed copy.

Missing `utm` creation writes one blueprint per missing store resref, using the
placed store's current store fields and inventory slots. If multiple placed stores
share that missing resref, their generated blueprints must match or the tool stops.

Warnings for missing `utm` or `uti` blueprints are reported. The check exits with a
non-zero code only when source-backed instances are out of date.
