# License Notice — SWLOR Toolset

## The short version

SWLOR is MIT (see `LICENSE.txt` at the repository root). The toolset's own source is MIT too.
But the toolset *links* `Radoub.Formats` from
[Radoub](https://github.com/LordOfMyatar/Radoub) (`External/Radoub` git submodule), which is
**GPL-3.0** — so a **built toolset binary** is a combined work covered by GPL-3.0, and
`SWLOR.Toolset/LICENSE.GPL-3.0` is the license that applies to it.

`Radoub.UI` is no longer referenced. The single type the app used from it (`MdlPartComposer`, plus
its `MdlPartBoneMap` helper) is vendored into `SWLOR.Toolset.Domain/Render` — see the list below.
That removes a project reference and Radoub.UI's Avalonia version pin; it does **not** change the
license position, because `Radoub.Formats` is still linked and the vendored files are still GPL.

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

The boundary does **not** run cleanly between projects. Five files in `SWLOR.Toolset.Domain` are
GPL-3.0 whatever the rest of the toolset is licensed as, because they are Radoub code or derived
from it rather than merely callers of it. Each carries an SPDX header saying so:

*Vendored verbatim* (copied out when the `Radoub.UI` reference was dropped; only the namespace
differs, so composed geometry is bit-identical to what shipped — but upstream fixes no longer flow
in, and these are now ours to maintain):

- `Render/MdlPartComposer.cs` — skeleton + body-part composition, seam nudging, composite bounds
- `Render/MdlPartBoneMap.cs` — part-type → skeleton-bone name table

*Adapted or mirrored:*

- `Render/TextureLoader.cs` — BioWare-DDS conversion adapted from `Radoub.UI.Services.TextureService`
- `Render/MdlMeshBuilder.cs` — world transform mirrors Radoub's `ModelViewController.GetWorldTransform`
- `Render/MdlGeometryFlattener.cs` — same transform composition order

If Radoub is ever dropped entirely, all five must be clean-roomed from the format specs, not merely
unreferenced. **Removing a project reference does not remove the derivation** — which is exactly why
dropping `Radoub.UI` bought build independence rather than license freedom.

## Submodule

Radoub is pinned to a specific commit and updated deliberately; **do not commit changes inside
`External/Radoub`**. Note that git worktrees do not initialise submodules automatically, and the
default SSL backend on Windows may fail to reach GitHub — `git -c http.sslBackend=schannel
submodule update --init External/Radoub` is the working incantation.
