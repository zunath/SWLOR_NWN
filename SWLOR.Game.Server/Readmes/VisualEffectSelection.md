# Visual Effect Selection

Use this guide when choosing NWN visual effects for perks, abilities, status effects, traps, scripted creature attacks, and similar gameplay feedback.

The detailed lookup table is [VisualEffectReference.csv](VisualEffectReference.csv). It was generated from these NWN Lexicon visual reference pages on 2026-06-23:

- [VFX_BEAM_*](https://www.nwnlexicon.com/Vfx_beam)
- [VFX_COM_*](https://www.nwnlexicon.com/Vfx_com)
- [VFX_DUR_*](https://www.nwnlexicon.com/Vfx_dur)
- [VFX_EYES_*](https://www.nwnlexicon.com/Vfx_eyes)
- [VFX_FNF_*](https://www.nwnlexicon.com/Vfx_fnf)
- [VFX_IMP_*](https://www.nwnlexicon.com/Vfx_imp)

## Selection Workflow

1. Identify the gameplay moment the visual needs to communicate: activation, source-to-target link, direct hit, area pulse, persistent marker, status aura, or eye/head cue.
2. Pick the VFX group by behavior first, then filter the CSV by `VisualTags`, `Colors`, and `Location`.
3. Prefer the `CSharpEnum` value from the CSV when editing C# code. The `NWScriptConstant` and numeric `Value` are included for cross-checking NWScript, 2DA, and Lexicon references.
4. Check `ImageUrl` or `SourcePage` when the exact look matters. Do not choose from the constant name alone if the screenshot or tags point to a different feel.
5. Match visual intensity to gameplay weight. Small single-target effects should not use large fire-and-forget explosions or screen shakes unless the perk is meant to read as dramatic.

## VFX Groups

| Group | Use For | Typical SWLOR Usage |
| --- | --- | --- |
| `BEAM` | A visible line between source and target. | Sustained lightning, draining, linking, repair, ion, or channel effects using `EffectBeam`. |
| `COM` | Combat contact feedback. | Weapon hits, claws, sparks, blood/chunk effects, blaster or elemental hit confirmation. |
| `DUR` | Persistent duration visuals. | Status auras, placed field markers, ground markers, protection overlays, lingering field identity. |
| `EYES` | Eye or head-mounted glow cues. | Rage, fear, perception, beast, mind, dark/light, and elemental gaze effects. |
| `FNF` | Fire-and-forget area or location visuals. | Grenade bursts, sonic pulses, smoke puffs, screen shake/bump, explosions, area pulses. |
| `IMP` | Instant target impact visuals. | Healing, buffs, debuffs, daze/fear/poison impacts, elemental hits, restoration, removal effects. |

## Ability Patterns

- Activation-only feedback should use `DisplaysVisualEffectWhenActivating(...)` when the VFX belongs to the caster's startup rather than the impact.
- Target impact feedback usually uses `IMP` or `COM` through `EffectVisualEffect(...)` on the affected object.
- Area impact feedback usually uses `FNF` through `ApplyEffectAtLocation(...)`.
- Persistent fields or deployables should pair an instant `FNF` burst with a visible `DUR` marker when players need to understand the active footprint.
- Beams should use `EffectBeam(...)` with a `BEAM` entry. Use silent beam variants when a repeated pulse would make the soundscape noisy.
- Eye effects should be reserved for effects where the creature's gaze, perception, rage, or mental state is the player-facing signal.

## Theme Matching

- Devices and tech effects usually read best with electrical, pulse, fire, smoke, spark, sonic, or beacon-like VFX.
- First Aid and restoration effects usually read best with healing, restoration, remove-condition, holy-aid, or clean pulse visuals.
- Leadership and command effects usually read best with shout, sound, protection, rally, aid, and clean ally-facing visuals.
- Force light effects usually read best with holy, white, blue-white, protection, restoration, and controlled pulse visuals.
- Force dark, fear, mind, and corruption effects usually read best with negative, mind, purple, black, red, fear, nightmare, or odd pulse visuals.
- Weapon discipline effects usually read best with `COM` hit confirmation and restrained `FNF` area visuals for sweeps, slams, and shockwaves.

When a perk has both an area component and a target component, choose separate VFX for each part instead of forcing one graphic to carry both meanings.
