# Third-Party and Provenance Notice — SWLOR Toolset

## Current source and dependency boundary

SWLOR first-party source is licensed under the repository's MIT license in `LICENSE.txt`.

The current toolset dependency graph is:

```text
{ SWLOR.Toolset, SWLOR.ConversationMigrator } → SWLOR.Toolset.Domain → { SWLOR.NWN.Formats, SWLOR.Game.Server }
```

`SWLOR.NWN.Formats` is a standalone first-party, read-only implementation of the Aurora resource
formats consumed by the toolset. Its exact specifications, fixture sources, exposure declarations,
corpus counts, and reviewed ambiguities are recorded in
`SWLOR.NWN.Formats/FORMAT-PROVENANCE.md`.

The toolset also consumes third-party NuGet packages declared in its project files, including
Avalonia, AvaloniaEdit, CommunityToolkit.Mvvm, Dock, Silk.NET.OpenGL, Microsoft dependency
injection, and Pfim. Those packages remain under their respective licenses; redistributions must
retain any notices their licenses require. Project files and the resolved NuGet assets are the
authoritative version inventory.

## Architecture rule

The desktop toolset and the dedicated conversation-migration utility are outer application layers.
Shared libraries and the game server must not reference `SWLOR.Toolset` or
`SWLOR.Toolset.Domain`. The migration utility is an explicitly reviewed leaf consumer of the
headless domain library; it must not be referenced by shared or runtime projects. The formats
library does not reference the toolset or game server. `ToolsetLicenseBoundaryTests` enforces the
approved dependency direction and the absence of the retired external format dependency from
executable first-party source and project references.

## Historical attribution

Earlier toolset revisions linked the GPL-3.0 Radoub format library and contained five render files
derived from that project. The removal work, exposure boundaries, and replacement evidence are
documented in `RADOUB-REPLACEMENT-PLAN.md`, `SWLOR.NWN.Formats/FORMAT-PROVENANCE.md`, and
`SWLOR.Toolset.Domain/Render/REPLACEMENT-PROVENANCE.md`.
Historical `PLAN.md`, `WORKLOG.md`, and git history are retained as an accurate record of what
shipped at that time.

`LICENSE.GPL-3.0` is retained as a historical third-party license notice for those earlier
revisions. Its presence is not a project reference or a declaration that the current toolset
binary links GPL code.

The tracked external submodule and all executable references to it are removed by the replacement
change. Repository history and historical documentation may still name it for attribution; the
source and built-output audits intentionally distinguish those records from executable
dependencies.
