# Area generator placement and review

Open **Tools → Area Generator**. Choose the theme, tileset, layout, dimensions and seed, inspect the preview, then create the area in the open module.

## Placement controls

The **Decorations** card contains the enable switch, palette, placement style and density. Placement controls are disabled when decorations are disabled.

| Option | Reserved route width | Gap between ordinary prop footprints |
|---|---:|---:|
| Clear routes and spacing — default | 2.5m | 0.4m |
| Compact dressing | 1.2m | 0.1m |

Both options check prop overlap, surface support and access to room/transition anchors. Density is a target, not an instruction to fill unsafe locations. The status message reports how many proposed props were omitted for support, route/anchor clearance or overlap. Changing placement style leaves the tile layout unchanged.

The **Prop footprints** overlay uses purple circles for ordinary props, cyan circles for floor decoration, and gray rectangles for measured building bounds. Enable **Routes** to see the gold bands reserved through rooms and along roads. Overlay changes reuse the solved draft; layout, palette, density, placement and seed changes regenerate it. **Refresh Preview** can explicitly generate again. Area creation uses the validated draft shown in the preview.

## Review findings and fixes

| Issue | Resulting behavior |
|---|---|
| Different dressing mechanisms could overlap each other despite having declared prop sizes. | A shared final placement pass checks scaled footprints across wall runs, piles, centerpieces and other ground arrangements. Both placement options apply the check. |
| A prop center could be supported while its footprint crossed a wall, structure, map boundary or chasm. | Circle/rectangle intersection checks examine every covered tile and chasm quadrant. Rigid props also reject footprint samples with more than 0.5m of ground-height variation. Ground interpolation is shared with area writing. |
| Structural buildings could overlap between occupied-cell centers, particularly when their model names differed. | Frontage candidates now check measured rectangles against every existing frontage before selection. A narrower fitting candidate can be chosen instead. |
| Enclosing circles for long buildings overstated their intrusion into neighboring streets. | Prop, creature and treasure clearance use measured building rectangles where available. The preview shows the same rectangles. |
| Density-driven dressing could intrude on circulation or encounter anchors. | Deterministic paths connect room hubs to perimeter openings and transitions through actual room tiles; road centerlines are reserved as well. Paths follow concave rooms instead of taking straight shortcuts through walls. |
| Feature sprinkling could replace room centers or tiles on the reserved circulation paths. | These cells are protected before feature selection. Existing feature spacing and tile-vocabulary checks remain in effect. Explicitly curated lawn features can still receive planted decoration; other feature art is not assumed to be free floor. |
| Doorway flanks could commit only one member when the other failed an eligibility check. | Pairs commit together. Both pairs and vignettes have arrangement identities, so the clearance pass omits the whole group if a member cannot fit. |
| Rejecting base cargo could leave a floating stacked copy or an isolated under-pile decal. | Stacked tiers retain a reference to their supporting prop; unsupported tiers and orphaned pile decals are removed. |
| Flat paint and elevated facade art consumed creature/treasure clearance as if they were ground obstacles. | Ground obstruction metadata distinguishes these layers from solid props. Stacked tiers do not reserve the same footprint twice. |
| The default 1m planned radius overrode smaller declared footprints. | Palette declarations now supply the actual ordinary-prop radius, including the 0.6m small size. Structural bounds retain their own measurements. |
| Selecting Standard palette could silently reuse the theme's named alternate palette. | An explicit empty palette selects Standard; an omitted override inherits the theme. Unknown requested palettes and invalid placement settings produce useful validation errors. |
| Missing prop or door blueprints were discovered only during creation. | Workspace-backed preview generation checks distinct prop, treasure and transition blueprint references before presenting a creatable preview. |
| Replanning at zero density could retain stale frontage occupancy. | Frontage occupancy resets even on early returns. |
| Preview overlays reran generation; an older asynchronous result could publish after settings changed. | Display-only changes reuse the solution. Revision checks discard outdated results and prevent creating a stale preview. |
| Exterior tiles whose default fill was also their open terrain appeared as solid ground in schematic mode. | Effective open-terrain classification takes priority over the tileset's default-fill classification. |
| Adding preview controls overflowed the minimum window width. | The overlay toolbar wraps; a headless layout test checks every toggle at the minimum width. |

## Verification

The focused tests cover collision and support failures, both spacing options, concave routes, tunnel mouths, stacked cargo, arrangement integrity, palette overrides, blueprint validation, feature reservations, building bounds, preview coloring, control bindings and asynchronous preview invalidation.

`DecorationCompositionTests` exercises cave/organic, facility/halls, ruin/packed, sewer/warren, city/packed, city/complex and city-plaza/packed compositions. Each runs at 16×16 and 24×24 with seeds 4242 and 77231, checks repeatability, loads required prop blueprints, validates encounters and treasure placement, checks ordinary-prop and building overlaps, and exercises compact placement at both 100% and 200% density. Existing layout, transition, area writer and walkmesh tests provide related coverage.

Build once after changing code, then run the relevant tests without rebuilding:

```powershell
dotnet build SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build --filter "FullyQualifiedName~AreaGeneration|FullyQualifiedName~AreaWalkmeshTests|FullyQualifiedName~NewAreaWriter"
```

For visual inspection, setting `SWLOR_AREA_REVIEW_OUTPUT` to an output directory makes the composition tests export the renderer's RGBA buffers for their 16×16, seed-4242 examples. Dimensions are included in the filenames. This is an opt-in test artifact, not an area export workflow.

## Practical limits

Clearance uses curated footprint measurements or conservative size-class circles, resolved tile/corner semantics, and interpolated corner heights. It does not perform exact MDL/PWK collision or native NWN pathfinding. Frontage support and elevated facade attachment use their existing specialized rules. The route overlay depicts the reserved room and road paths, not a complete navigation mesh. Generated areas remain editable drafts; native playtesting is still the check for asset-specific collision and visual details that these declarations cannot describe.
