# Weapon hand fit

How a held pistol or rifle is seated in each playable hand, and how to change it. Players never adjust this themselves.

## How it works

- Pistols and rifles are fitted to one body, the male human (`pmh0`). Fit a weapon model to that body only (see [BlasterModelImport.md](BlasterModelImport.md), "Hand fit", and [RifleModelImport.md](RifleModelImport.md)).
- Every other body carries the weapon hook in a hand of another size and place, so the same weapon sits beside, above or below the hand. `WeaponHandFitCatalog.cs` records how far to move the weapon, in metres, in weapon space (barrel -Z, grip -Y, +X toward the right palm):
  - `Bodies`: one move per body and side, keyed like `pfd0_handr`. A hand style without its own entry takes it.
  - `Hands`: a move for one hand model, keyed like `pfh0_handr204`. It replaces the body's move.
- `WeaponHandFit` applies the move to pistols held in either hand and to a held rifle or cannon whenever the creature's appearance is refreshed (equip, spawn, area entry, appearance edits). It looks up the hand model the creature shows, so a glove that replaces the natural hand is used. An unequipped weapon carries no move. The move is added to the item's base transform and tracked on the item, so a transform something else set on the item survives.
- The move depends on the hand only, never on the weapon. Do not add per-weapon or per-player moves. A weapon that sits wrong on the male human is a model fault; fix the model.

Playable bodies: `h` human frame (most species), `e` elf frame (Bothan, Rodian, Wookiee), `d` dwarf frame (Ugnaught, Ewok), `o` half-orc frame (Cathar), `z` droid frame; `m` and `f`; normal phenotype only. A species' natural hand is the first entry of its `RightHand` list in `Feature/AppearanceDefinition/RacialAppearance`.

## Where the moves come from

The standard is the same hand style on the male human: wherever the handle rests on `pmh0_handr009`, it should rest on every other body's hand 009.

- **Derived.** Almost every hand model on another body is the male human's model, scaled and shifted for that body. `tools/WeaponGripFit.py` finds that scale and shift (exactly when both models share their vertices, otherwise by laying one surface over the other) and carries one point across it: the place where the back of the handle rests against the heel of the hand. The move is how far that point travels. Female and non-human models that cannot be tied to the male human directly are tied to it through their own human-frame model.
- **Body move.** `Bodies` holds the median derived move of a body and side. Hands whose derived move is more than 3 mm from it get a `Hands` entry.
- **By eye.** A hand with no model on the male human to compare with (the Cathar, Wookiee and droid hands, and a few remodelled gloves) cannot be derived. It takes its body's move unless a review sheet shows it needs its own. The entries judged this way are commented in the catalog.
- **Which wins.** A move derived from shared vertices is exact; the check holds the catalog to it. A move found by laying surfaces over each other is an estimate, so an entry reviewed in the client outranks it (the Mon Calamari female hand is one).
- **Left hands** are derived on their own. Several left hooks are not mirror images of the right ones, so a left move is not always the right move with X negated.

Most glove styles have open fingers, and several hand parts are mittens, claws or stubs. They cannot close around a handle and are not meant to. For them the handle lies in the palm in the same place as on the male human. Do not add entries to chase finger contact.

## Rifles

A held item takes one fixed move, but the rifle clips do not hold the weapon one way: the idle clip (`xbowr`) leaves the weapon hook at rest, and the ready and attack clips (`xbowrdy`, `xbowshot`) turn it about 54 degrees against the hand (about 43 on the female dwarf and half-orc frames). So the same hand needs a different rifle move in each. `WeaponHandFit.RifleMove` uses the midpoint: the part of the move along the turn's axis is exact in every clip, and the rest is off by less than half of what it was (at most 9 mm).

The support hand must follow the rifle. `SWLOR_Haks/tools/GenerateRifleHoldAnimations.py` solves each body's support arm for the rifle move of that body's `Bodies` entry (`WEAPON_MOVES` in the generator). A hand with its own `Hands` entry moves the rifle a little differently, so its support palm sits off by that difference. Keying the weapon hook in those banks is not an option: it would split the shared robe `rhand` joint.

When a right-hand `Bodies` entry changes, update `WEAPON_MOVES` and regenerate the banks; `check` fails while the two disagree. The steps, from `SWLOR_Haks/tools` with a full haks checkout:

```powershell
python GenerateRifleHoldAnimations.py --game-data "<NWN>\data"                 # writes the banks as ASCII
python CompileModels.py --model a_rfl_pfh0 ... --game-data "<NWN>\data" --apply # one --model per bank
python GenerateRifleHoldAnimations.py --game-data "<NWN>\data" --finalize
python GenerateRifleHoldAnimations.py --game-data "<NWN>\data" --update-robe-banks --baseline <revision> --apply
python -m unittest TestRifleHoldAnimations TestAnimationBankSources
python ExtendRobeAnimations.py --check --game-data "<NWN>\data"
python GenerateRobeRgbModels.py --check --game-data "<NWN>\data"
```

`--baseline` is the git revision whose rifle banks the robe banks hold now (normally `HEAD` before the change is committed). Bodies whose move did not change come out byte for byte the same.

## Reviewing and changing a move

`tools/WeaponGripFit.py` works from the compiled models, without the client. It needs numpy, scipy and Pillow, and the hak folders `sw_pt_rhand sw_pt_lhand sw_weapon sw_pt_root` (`git -C SWLOR_Haks sparse-checkout add ...`).

```powershell
python tools/WeaponGripFit.py derive --body pfd0                               # derived catalog lines for a body
python tools/WeaponGripFit.py check                                            # names, limits, rifle banks
python tools/WeaponGripFit.py check --derived                                  # also every hand against its derived move (minutes)
python tools/WeaponGripFit.py sheet --output C:\Temp\grip                      # default hand of every body
python tools/WeaponGripFit.py sheet --hand pfd0_handr209 --output C:\Temp\grip # one hand
python tools/WeaponGripFit.py sheet --hand pfd0_handr209 --move 15 0 0 --output C:\Temp\grip   # try a move (mm)
```

A sheet tile shows the side view, the view from below, and two cuts across the handle (hand outline in brown, pistol in blue), once without a move and once with it. In the cuts, +X is to the right and the barrel side is up. A drawing resolves about a centimetre; use it to catch a weapon that sits outside the hand, not to tune a derived move.

1. A new or changed hand model: run `derive` for its body. Copy the lines that differ from the catalog. `check --derived` must pass; it allows 5 mm.
2. A hand listed under `by eye`: draw it with `sheet --hand`. If the handle lies in the palm or the fist's opening, change nothing. Otherwise try moves with `--move` and add a commented `Hands` entry for both sides.
3. Run `check`, rebuild (`dotnet build SWLOR.Game.Server.Tests -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`), then `dotnet test SWLOR.Game.Server.Tests --no-build --filter "FullyQualifiedName~WeaponHandFitTests"`. The tests read the built catalog, so an edit is not tested until it is rebuilt. The engine test category `WeaponHandFit` covers applying and removing the moves in the server.
4. Confirm in the client for each body you changed (see [InEngineVisualVerification.md](InEngineVisualVerification.md), "Server behavior"): the offline drawing shows the rest pose only.

## Not covered

- **Melee weapons and Power Gloves.** Power Gloves have their own fit (`PowerGloveFit`).
- **Wide (large phenotype) bodies.** They are not playable and get no move.
