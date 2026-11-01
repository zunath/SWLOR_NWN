# Wearable parts and garment regressions

Read before changing wearable body-part geometry, skinning, robes, capes, their
animation dependencies, or the generators that reproduce them. Scope is playable
species on their normal base phenotype (`ContentStandards.md`); generated robe
render phenotypes are dependencies of those normal-body wearers.

## Establish the baseline

- Identify the exact deployed/released resource before editing. Record the release
  tag, parent and HAK commits, resolved model and animation resources, and source
  hashes. A style number, current branch, or screenshot filename is not a version.
- Separate the reported defect from behavior that must stay good: silhouette,
  waist/hem placement, rear drape, gait, emote motion, materials and dyes. Reproduce
  the defect with the reported part combination, including bulky thigh armor.
- Compare the actual resources for each affected body family and gender. Species
  sharing the same resolved models may share evidence; a different family's short
  fallback is not evidence for the human skirt. Do not claim unsupported styles
  or untested variants were fixed.
- Styles with similar geometry can have different rigs, UVs or dye channels.
  Consolidation needs an explicit canonical style, compatibility resources and
  owned-item migration where required (`ItemAppearanceRules.md`, `Migrations.md`).

## Preserve working shape and motion

Minimize visible clipping without introducing new clipping, stretching, frozen
panels, leg-following cloth, a wider silhouette or a different rear drape. Clearing
the thighs by making the whole skirt bulky is not a successful fit. Use the
smallest local adjustment that passes native review; numerical limits from one
recipe are not general allowances for other garments.

- Diagnose position, binding and missing-animation defects separately. A position
  repair preserves the accepted weights, indices, palettes, bone maps, inverse
  binds, tracks, UVs, materials and protected geometry. Do not reweight an already
  good rear panel to solve a front or side contact.
- Inspect the actual reachable joints, bind transforms and resolved clips. A new
  mesh node does not supply cloth motion. A modular part cannot borrow a robe's
  private cloth helpers merely by copying its weights. Keep body tracks and robe
  cloth motion separate; follow the binding/bridge rules in `TintMapRendering.md`.
- For each moved vertex, account for the existing weighted bind transform. Update
  normals, seam handling, face planes and bounds from the stored native positions.
  Reject newly collapsed/inverted faces, new seam gaps and unintended changes to
  unaffected buffers. Byte preservation alone does not validate an already wrong
  legacy inverse bind; such repairs require compiled world-placement proof.
- Keep a pinned source and deterministic recipe for authored corrections. Applying
  twice must not compound the displacement. Inspect the diff after every generator
  run; do not refresh hashes or broaden an edit allowlist to conceal a regression.
- After two unsuccessful adjustments, change the diagnostic approach. Verify the
  visible side, actual part and pose timing before changing more falloff values.
  Conservative thigh hulls can include inner/hidden cloth: aggregate improvement
  is supporting evidence, not proof that an on-screen breakthrough is gone.

## Generator ownership and dependent assets

Each source has one owning recipe. Preserve explicit restored/excluded entries;
do not let a general regeneration overwrite a specialized repair.
Register a new shared-joint consumer with the relevant scope guard; shared animation
edits must account for specialized owners and their derived robe aliases too.

| Change | Owning tools and focused guards |
| --- | --- |
| Canonical human pelvis 034 | `PelvisSkirtBindings.py` / `.json`, `TestPelvisSkirtBindings.py` |
| Modular skirts/capes listed in the chest manifest | `ChestSkirtBindings.py` / `.json`, `TestChestSkirtBindings.py` |
| Robe 236's generated skirt bindings | `RobeSkirtBindings.py`, `TestRobeSkirtBindings.py` |
| Body-bound robe panels, hoods and belts | `BindRobePanels.py`, `MakeRobePanelsRigid.py`; `TestBoundRobePanels.py`, `TestRigidRobePanels.py` |
| Robe skeletons and animation bridges | `RobeSkeleton.py`, `GenerateRobeRgbModels.py`, `RobePoseAudit.py`; applicable `TestRobeSkeleton.py`, `TestRobeAnimations.py`, `TestSharedRobeFamilies.py` |
| Body/flap tracks and missing emote cloth motion | Respective restore/fill generator; `TestRestoreRobeBodyTracks.py`, `TestFillRobeEmoteMotion.py`, `TestFillEmoteFlapTracks.py` as applicable |
| External part skins on robe bodies | `GeneratePartSkinModels.py`, `TestPartSkinModels.py`, C# `PartSkinAppearanceTests` |

Tool names in this table are under `SWLOR_Haks/tools`. Human legacy pelvis 035 is
protected in `ChestSkirtBindings.json`'s `restored` list. Canonical 034 uses the
accepted released-035 rig through its own pinned recipe, not the generic rebinding.
Keep the canonical material identity: equal UVs do not imply equal dye-layer maps.

After source geometry or joint usage changes, regenerate the affected part-skin,
neck/shoulder, robe-root, cloak and LOD dependents using their owning tools. Check
used named joints, duplicate-joint identity, traversal mappings and inverse binds;
matching joint names alone is insufficient. Let the manifests allocate/retire
aliases; never hand-reuse a retired ID. Check the generated tables and packaging.

Run the applicable focused guards, recipe/generator `--check` modes and
`CheckDyeAppearance.py`. For example, from the repository root:

```powershell
python -B SWLOR_Haks/tools/PelvisSkirtBindings.py --check
python -B -m unittest discover -s SWLOR_Haks/tools -p "TestPelvisSkirtBindings.py" -v
python -B SWLOR_Haks/tools/GeneratePartSkinModels.py --check
python -B SWLOR_Haks/tools/CheckDyeAppearance.py
```

Replace/add the focused modules for the changed owner and its dependencies; this
example is not complete coverage for a robe-animation change. Tests needing stock
art require `NWN_GAME_DATA` or the supported local installation. Report unavailable
inputs and skipped checks; they are not passes. `BuildHaks.cmd` runs the six
stock-art-independent pelvis provenance/shape/rig/metadata tests and three structural
shared-flap tests before packing. It does not run the stock-art-dependent pelvis
pose/hull or flap wearer/gait tests. These preflight gates supplement the full
applicable motion tests and native review, not replace them.

Rebuild affected HAKs together and compare extracted payloads with the inspected
source hashes. Restart the server when its cached render tables change, and reload
the client assets. Repack Module JSON or deploy C# only when those also changed.

## Native acceptance and handoff

Follow `InEngineVisualVerification.md`'s equipment-capture procedure. Inspect:

- Both genders and every distinct affected playable body/render path; normal and
  bulky thighs, the reported outfit, and any relevant robe/part combination.
- Settled front, rear and both anatomical sides; hip-shift idle/Pause2, the reported
  emotes, and representative asymmetric poses such as Tired and ParadeRest.
- Genuine walking and running from front/side/rear, with enough samples to inspect
  contacts, rear drape and stretching. When changing shared tracks, also check
  relevant abilities, sitting/kneeling and transitions from prior poses, since
  missing channels can latch earlier transforms.

The reported visible breakthrough must be cleared, and tested working combinations
must not acquire new ones. Distinguish armor intentionally visible below an open
hem from armor piercing cloth above it. Unresolved contacts remain defects: show
them and state the remaining tradeoff instead of declaring a partial fix complete.

Retain paired source/selected native images, exact input hashes, pose/timing and
subject receipts, and explicit coverage/limitations. Compare silhouette and motion
as well as clipping. Neutral-color captures do not verify distinct dye channels;
retain unchanged material/UV fingerprints or perform the required dye comparison.
An offline manually projected robe alias proves binding only. Full combined-outfit
claims need the real robe projection, hide masks and equipment state; behavior
produced by C# needs the live-server route.

Add a focused regression for the reproduced defect and protected good behavior.
Use actual mesh/hull sources and the observed pose/time interval, including nearby
phases where timing can vary. Keep existing reference cohorts valid when adding a
new side/pose guard; do not silently generalize a threshold beyond its qualified
samples. No finite matrix proves every armor/animation phase clipping-free, so the
handoff names the tested families, parts, poses and any residual clipping.
