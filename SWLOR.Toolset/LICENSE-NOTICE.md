# License Notice — SWLOR Toolset

The SWLOR Toolset projects (`SWLOR.Toolset`, `SWLOR.Toolset.Domain`, `SWLOR.Toolset.Tests`)
link against [Radoub](https://github.com/LordOfMyatar/Radoub) (`External/Radoub` git
submodule), which is licensed under **GPL-3.0**.

Implications:

- **Internal team use:** no obligations. The GPL's requirements trigger on *distribution*
  (conveying), not on use or private modification.
- **Distribution:** if toolset binaries are ever distributed outside the contributor team,
  the combined work must be released under GPL-3.0 with corresponding source made available
  to recipients.
- **Boundary rule:** the GPL applies to the toolset executables only. To keep the game
  server and every other project unencumbered, **no existing SWLOR project may ever
  reference `SWLOR.Toolset.*`**. Dependencies flow strictly one way:
  `SWLOR.Toolset → SWLOR.Toolset.Domain → { Radoub.Formats, SWLOR.Game.Server }`.

The Radoub submodule is pinned to a release tag and updated deliberately; do not commit
changes inside `External/Radoub`.
