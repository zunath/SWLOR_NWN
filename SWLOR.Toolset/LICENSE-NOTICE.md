# License Notice — SWLOR Toolset

## The short version

SWLOR is MIT (see `LICENSE.txt` at the repository root). The toolset's own source is MIT too.
But the toolset *links* [Radoub](https://github.com/LordOfMyatar/Radoub) (`External/Radoub` git
submodule), which is **GPL-3.0** — so a **built toolset binary** is a combined work covered by
GPL-3.0, and `SWLOR.Toolset/LICENSE.GPL-3.0` is the license that applies to it.

Source stays MIT, the binary is GPL. Those are not in conflict: MIT is one-way compatible with
the GPL, so MIT code may be incorporated into a GPL work. Keeping the source MIT is deliberate —
if Radoub is ever removed or relicensed, the toolset is immediately unencumbered with no need to
chase every contributor for permission. Marking the source GPL would throw that away and buy
nothing.

## When obligations actually trigger

The GPL attaches to **conveying** — GPLv3 §0: any propagation "that enables other parties to make
or receive copies." It has nothing to do with who someone is. Being a contributor, having commit
rights, or being in the Discord changes nothing either way. The only question is whether a *built
artifact* changes hands.

| Action | Conveying? |
|---|---|
| Someone clones this repo and builds the toolset themselves | No |
| CI publishes a toolset artifact that someone downloads | **Yes** — they received a copy they did not build |
| A toolset zip is posted in Discord for a builder | **Yes**, whoever they are |
| You copy the exe to a machine you own | No |
| You copy the exe to a host someone else controls | **Yes** |

Today nobody conveys toolset binaries: every contributor compiles locally from public source. That
is *why* there is no live obligation — not because contributors are somehow inside a boundary.

If you do convey a binary, comply by offering the corresponding source under GPL-3.0 to whoever
received it: this repository plus the pinned Radoub commit, both already public. Ship
`LICENSE.GPL-3.0` alongside it and keep the notices below intact.

## Boundary rule: dependencies flow one way

To keep the game server and every other project unencumbered, **no MIT project may reference
`SWLOR.Toolset.*`**. Dependencies flow strictly one way:

```
SWLOR.Toolset → SWLOR.Toolset.Domain → { Radoub.Formats, SWLOR.Game.Server }
```

`SWLOR.Toolset.Tests` referencing `SWLOR.Toolset` and `SWLOR.Toolset.Domain` is fine — it is part
of the toolset. `ToolsetLicenseBoundaryTests` enforces the rule so it cannot rot silently.

The practical consequence, worth knowing before it bites: `SWLOR.CLI` cannot borrow the toolset's
in-process GFF/2DA/KEY-BIF code, even though it does related work through `nwn_gff`/`nwn_erf`. That
merge is permanently off the table while Radoub is linked.

## Files that are GPL regardless of the above

The boundary does **not** run cleanly between projects. Three files in `SWLOR.Toolset.Domain` are
derivative works of Radoub — they were adapted from or mirror its logic, so they are GPL-3.0 no
matter how the rest of the toolset is licensed. Each carries a header saying so:

- `Render/TextureLoader.cs` — BioWare-DDS conversion adapted from `Radoub.UI.Services.TextureService`
- `Render/MdlMeshBuilder.cs` — world transform mirrors Radoub's `ModelViewController.GetWorldTransform`
- `Render/MdlGeometryFlattener.cs` — same transform composition order

If Radoub is ever dropped, these three must be clean-roomed from the format specs, not merely
unreferenced. Removing the project reference does not remove the derivation.

## Submodule

Radoub is pinned to a specific commit and updated deliberately; **do not commit changes inside
`External/Radoub`**. Note that git worktrees do not initialise submodules automatically, and the
default SSL backend on Windows may fail to reach GitHub — `git -c http.sslBackend=schannel
submodule update --init External/Radoub` is the working incantation.
