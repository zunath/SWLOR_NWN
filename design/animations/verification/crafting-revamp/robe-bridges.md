# Robe animation bridges: crafting-revamp work clips (offline evidence)

The 15 crafting, gathering and fishing clips reached the a_ba and a_fa chains in HAK PR #578, but the robe bridges (`sw_anim_m`, `sw_anim_f`) were not regenerated, so a robe's garment attachment joints held their bind pose while the body played the clip. HAK PR #584 regenerates all 14 shared robe families. This folder holds the offline evidence for that change. It is not NWN client evidence: the live-client check, with real robes and equipment, is deferred to the Closeout joint pass.

## Coverage audit

`robe_bridge_audit.py` walks every bridge chain recorded in `SWLOR_Haks/tools/RobeRgbModels.json` (head, `_b01`, ...), reports which bank owns each work clip, compares its duration with the body chain, and re-runs the generator's five-phase body pose comparison (`RobePoseAudit.validate_body_poses`) for exactly these 15 clips.

```powershell
# from SWLOR_Haks
python -B <repo>/design/animations/verification/crafting-revamp/robe_bridge_audit.py --game-data "<NWN>/data" --stage <scratch> --json robe-bridges-audit.json
```

| Bridge head | Body | Banks | Clips owned by a bridge bank (before) | Clips owned (after) | Duration matches body | Pose samples compared | Mismatch |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| `pfa_ra090` (was `pfa_ra089`) | `pfa0` | 3 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pfb_ra058` (was `pfb_ra057`) | `pfb0` | 1 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pfd_ra124` (was `pfd_ra123`) | `pfd0` | 2 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pfe_ra092` (was `pfe_ra091`) | `pfe0` | 3 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pfg_ra090` (was `pfg_ra089`) | `pfg0` | 3 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pfh_ra108` (was `pfh_ra107`) | `pfh0` | 3 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pfo_ra060` (was `pfo_ra059`) | `pfo0` | 2 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pma_ra090` (was `pma_ra089`) | `pma0` | 3 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pmb_ra026` (was `pmb_ra025`) | `pmb0` | 1 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pmd_ra108` (was `pmd_ra107`) | `pmd0` | 2 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pme_ra076` (was `pme_ra075`) | `pme0` | 3 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pmg_ra090` (was `pmg_ra089`) | `pmg0` | 2 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pmh_ra091` (was `pmh_ra090`) | `pmh0` | 3 | 0/15 | 15/15 | 15/15 | 75 | none |
| `pmo_ra076` (was `pmo_ra075`) | `pmo0` | 2 | 0/15 | 15/15 | 15/15 | 75 | none |

All families cover all 15 clips: True. Every clip owned by a bridge bank: True. Before the regeneration the clips were only reachable by inheritance from the body chain (0 of 15 owned in every family).

## Contact sheets

`robe-bridges-male.png` and `robe-bridges-female.png` sample a robe wearer root through its own bridge chain with the generator's pose sampler (`RobePoseAudit.Model.pose`): `pmh56` (male human, bridge `pmh_ra091`) and `pfh56` (female human, bridge `pfh_ra108`), seven phases of each clip. Light lines are body bones; orange joints are the wearer's garment joints that the bridge-owned clip animates (belt, tassel and fabric joints, 10 per clip on these wearers). They are skeleton renders, not skinned meshes.

```powershell
# from SWLOR_Haks
python -B <repo>/design/animations/verification/crafting-revamp/render_robe_sheet.py --wearer pmh56 --out robe-bridges-male.png
python -B <repo>/design/animations/verification/crafting-revamp/render_robe_sheet.py --wearer pfh56 --out robe-bridges-female.png
```

| Clip | Seconds | Owning bank (male, `pmh_ra091`) | Owning bank (female, `pfh_ra108`) | Garment joints animated | Max garment travel, male (m) | Max garment travel, female (m) |
| --- | ---: | --- | --- | ---: | ---: | ---: |
| `sw_forgework` | 3.4 | `pmh_ra091` | `pfh_ra108` | 10 | 0.072 | 0.072 |
| `sw_circuitry` | 2.9 | `pmh_ra091` | `pfh_ra108` | 10 | 0.0 | 0.0 |
| `sw_fabricate` | 3.6 | `pmh_ra091` | `pfh_ra108` | 10 | 0.0 | 0.0 |
| `sw_cooking` | 3.2 | `pmh_ra091` | `pfh_ra108` | 10 | 0.0 | 0.0 |
| `sw_compound` | 3.3 | `pmh_ra091` | `pfh_ra108` | 10 | 0.0 | 0.0 |
| `sw_survey` | 3.0 | `pmh_ra091_b01` | `pfh_ra108_b02` | 10 | 0.11 | 0.105 |
| `sw_strike` | 1.9 | `pmh_ra091_b01` | `pfh_ra108_b01` | 10 | 0.218 | 0.216 |
| `sw_hvystrike` | 2.7 | `pmh_ra091_b01` | `pfh_ra108_b01` | 10 | 0.288 | 0.286 |
| `sw_extract` | 2.8 | `pmh_ra091` | `pfh_ra108` | 10 | 0.409 | 0.407 |
| `sw_search` | 3.0 | `pmh_ra091_b01` | `pfh_ra108_b01` | 10 | 0.483 | 0.482 |
| `sw_dig` | 3.2 | `pmh_ra091` | `pfh_ra108` | 10 | 0.509 | 0.507 |
| `sw_reel` | 3.05 | `pmh_ra091_b01` | `pfh_ra108_b01` | 10 | 0.118 | 0.113 |
| `sw_giveline` | 2.4 | `pmh_ra091` | `pfh_ra108_b01` | 10 | 0.11 | 0.105 |
| `sw_holdrod` | 3.5 | `pmh_ra091_b01` | `pfh_ra108_b01` | 10 | 0.145 | 0.142 |
| `sw_landfish` | 3.4 | `pmh_ra091_b01` | `pfh_ra108_b01` | 10 | 0.146 | 0.145 |

The four station clips that only move the arms (`sw_circuitry`, `sw_fabricate`, `sw_cooking`, `sw_compound`) leave the pelvis-mounted garment joints in place, so their travel is 0; the joints still carry the clip's tracks.

## Unverified

- No NWN client run: playback with real robes, the model cache, equipment overlays and the transitions in and out of the clips.
- The clips animate attachment and cloth-adjacent garment joints only. Coat helpers (`rg_coat_*`) carry no tracks in the 15 work clips, although native clips such as `sitdown` and `xbowshot` do, so a long coat holds its previous pose during them; that is outside this change.
