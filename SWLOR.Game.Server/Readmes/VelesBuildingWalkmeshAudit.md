# Veles building collision audit

The `veles_exterior` area contains 58 placements of 15 building/factory models,
plus six placements of two tower models. The three reported wall-entry locations
use `swc_blg_corhis02` and `swc_blg_corhis04`.

Ten building models had flat PWKs. Their collision did not extend through the
street's walking plane when the building was sunk into the terrain. Each now has
a closed volume from a buried foundation at local Z -0.5 to its model's roof.
The full height covers placements beside Veles's elevated streets as well.
Skyscraper 04 had no PWK; its new collision follows the native model's base outline,
including both recessed entrances and the chamfered corners.

| Model | Veles placements | Repair | Local roof Z |
| --- | ---: | --- | ---: |
| `swc_blg_corhis01` | 4 | Extrude existing footprint | 15.56 |
| `swc_blg_corhis02` | 5 | Extrude existing footprint | 19.66 |
| `swc_blg_corhis03` | 8 | Extrude existing footprint | 17.27 |
| `swc_blg_corhis04` | 21 | Extrude existing footprint | 22.68 |
| `swc_bld_bk_cor01` | 4 | Extrude existing footprint | 29.56 |
| `swc_bld_bk_cor02` | 1 | Extrude existing footprint | 17.79 |
| `swc_bld_bk_cor03` | 1 | Extrude existing footprint | 17.79 |
| `swc_prison01` | 1 | Extrude existing footprint | 36.13 |
| `swc_fctry_item1` | 2 | Extrude four separate footprint solids | 7.70 |
| `swc_hse_lrg_gen3` | 1 | Extrude existing footprint | 7.64 |
| `swc_bldg_b_sky04` | 1 | Add missing PWK | 155.99 |

The ten rebuilt footprints retain their recesses and original non-walkable
surface materials. Sub-millimetre export seams were welded and collapsed faces
removed where necessary. Comparing the projected old and new meshes at
2,555,421 sampled XY points found no footprint differences.

Machine Factory (`swc_fctry_machn1`, two placements), Imperial Med Facility
(`daf_sw164`, five), Starport (`aswtor_111`, one), Commerce (`aswtor_112`, one),
Control Tower (`daf_sw284`, one), and Water Tower (`aswtor_067`, five) already have
three-dimensional PWKs with non-walkable surfaces and were left unchanged.
Door surrounds, signs, vehicles, kiosks, and other street furniture are outside
the building-shell repair scope.

Run `python -m unittest discover -s tools/tests -p test_building_walkmesh.py -v`.
The 13 geometry checks cover the complete building/tower inventory, sealed
repaired meshes, the three reported approaches, preserved recesses, and collision
against ground heights read from the area's rotated tiles and WOKs. Terrain above
a submerged model's roof remains unobstructed by that model.

All eleven changed resources are in `SWLOR_Haks/sw_plc`. Rebuild and distribute
`sw_plc.hak` to deploy. The PWKs are shared model resources, so their repairs also
apply wherever those models are used outside Veles. No module placement changes
are needed. HAK packaging and an NWN in-game pathfinding check have not been run;
the automated checks validate asset geometry, not the engine's pathfinding.
