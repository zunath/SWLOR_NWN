# Weapon hand fit

How a held pistol is seated in each playable hand, and how to change it. Players never adjust this themselves.

## How it works

- Pistols are fitted to one hand: the male human default hand, `pmh0_handr001`. Fit a pistol model to that hand only (see [BlasterModelImport.md](BlasterModelImport.md), "Hand fit").
- Every other body carries the weapon hook in a hand of another size, so the same pistol sits beside, above or below the fist. `WeaponHandFitCatalog.cs` records how far to move the pistol, in metres, in weapon space (barrel -Z, grip -Y):
  - `Bodies`: one move per body and side, keyed like `pfd0_handr`. Every hand style and glove of that body inherits it, because they all share the wrist.
  - `Hands`: a move for one hand model, keyed like `pfh0_handr204`, when its grip sits somewhere else than its body's default hand. It replaces the body's move.
- `WeaponHandFit` applies the move to pistols held in either hand whenever the creature's appearance is refreshed (equip, spawn, area entry, appearance edits). It looks up the hand model the creature shows, so a glove that replaces the natural hand is used. An unequipped pistol carries no move. The move is added to the item's base transform and tracked on the item, so a transform something else set on the item survives.
- The move depends on the hand only, never on the pistol. Do not add per-weapon or per-player moves. A pistol that sits wrong in the reference hand is a model fault; fix the model.
- Left hands mirror right hands: the same move with X negated. A unit test enforces it.

Playable bodies: `h` human frame (most species), `e` elf frame (Bothan, Rodian, Wookiee), `d` dwarf frame (Ugnaught, Ewok), `o` half-orc frame (Cathar), `z` droid frame; `m` and `f`; normal phenotype only. A species' natural hand is the first entry of its `RightHand` list in `Feature/AppearanceDefinition/RacialAppearance`; the half-orc frame's move is measured on the Cathar hand 201, since no species shows hand 001 there.

## What counts as a good fit

The handle passes through the fist's opening, next to the palm, with the index finger under the trigger guard. It does not have to be perfect.

Most glove styles have open fingers, and several hand parts are mittens, claws or stubs. They cannot close around a handle and are not meant to. For them the handle only has to lie in the palm, which the body's move already does. Do not add entries to chase finger contact on those hands.

## Reviewing and changing a move

`tools/WeaponGripFit.py` draws hand and pistol together from the compiled models, in seconds, without the client. Each tile shows the side view, the view from below, and two cuts across the handle (hand outline in brown, pistol in blue), once as the game shows it without a move and once with the move. In the cuts, +X is to the right and the barrel side is up.

```powershell
python tools/WeaponGripFit.py sheet --output C:\Temp\grip                      # default hand of every body
python tools/WeaponGripFit.py sheet --body pfd0 --all --output C:\Temp\grip    # every hand style of one body
python tools/WeaponGripFit.py sheet --body pfd0 --side r --move 0 0 14 --output C:\Temp\grip   # try a move (mm)
python tools/WeaponGripFit.py check                                            # catalog names and limits
```

It needs numpy, scipy and Pillow, and the hak folders `sw_pt_rhand sw_pt_lhand sw_weapon sw_pt_root` (`git -C SWLOR_Haks sparse-checkout add ...`).

1. Render the hand with `sheet`. If the pistol already sits in the fist or the palm, change nothing.
2. Try moves with `--move` until the cuts show the handle inside the fist's opening and the side view shows the fist under the trigger guard. Moves are small: a few millimetres to about three centimetres. The check rejects anything over six.
3. Put the value in `Bodies` when the body's natural hand needs it, or in `Hands` for a single hand model. Add the mirrored left entry.
4. Run `check`, rebuild (`dotnet build SWLOR.Game.Server.Tests -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`), then `dotnet test SWLOR.Game.Server.Tests --no-build --filter "FullyQualifiedName~WeaponHandFitTests"`. The tests read the built catalog, so an edit is not tested until it is rebuilt. The engine test category `WeaponHandFit` covers applying and removing the move in the server.
5. Confirm in the client for each body or hand you changed (see [InEngineVisualVerification.md](InEngineVisualVerification.md), "Server behavior"): the offline drawing shows the rest pose only.

## Not covered

- **Rifles.** The rifle combat clips turn the weapon hook about 54 degrees from the idle clip, so one fixed move cannot be right in both, and moving a rifle also pulls it off the support hand placed by `SWLOR_Haks/tools/GenerateRifleHoldAnimations.py`. A rifle correction belongs in those per-body hold banks (see [RifleModelImport.md](RifleModelImport.md)).
- **Melee weapons and Power Gloves.** Power Gloves have their own fit (`PowerGloveFit`).
- **Wide (large phenotype) bodies.** They are not playable.
