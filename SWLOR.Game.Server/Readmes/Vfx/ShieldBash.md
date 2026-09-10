# Shield Bash impact sample

Shield Bash I–IV share a compact warm metallic spark burst with a brown dust
puff on the struck creature's impact attachment. This is a physical contact
effect, without an energy ring, screen shake, or added sound.

- Custom `visualeffects.2da` row: **843**, `Vfx_Imp_Shield_Bash`.
- Model: `sw_shldbash.mdl` in `sw_vfx.hak`, compiled binary, no supermodel.
- Original textures: `sw_bash_spark.tga` and `sw_bash_dust.tga`.
- Emission ends after 0.13 seconds; the last particles expire before the
  0.9-second impact animation ends. Neither emitter loops.
- `ShieldBashAbilityDefinition` supplies the target VFX to `ApplyCombatImpact`.
  Its existing damage/status success gate applies the effect. Merely queueing
  Shield Bash does not produce the new VFX. Damage and animations are unchanged.

## Sample locally

Install matching `sw_vfx.hak`, `sw_2da.hak`, and server assembly. Restart the
client after replacing HAKs so cached resources are refreshed.

On the test server (or as DM/Admin), enter **`/playvfx 843`**, then select a
creature to preview only the particles. To test the complete integration, equip
a shield, queue any rank of Shield Bash, and land an attack on an enemy. The
effect should appear once on the enemy, not when the perk is queued.

Compare on light and dark backgrounds and on small and large creatures. This
first sample still needs in-game visual approval before expanding to other
abilities.

## Authoring

From the HAK repository, run `python tools/GenerateShieldBashVfx.py`.
The generator creates both textures procedurally and compiles only this effect
with the pinned, bundled model compiler. It validates a binary round trip before
installing the model. Editable ASCII is retained under
`model_sources/vfx/sw_shldbash.mdl.ascii`; it is excluded from HAK packaging.
Rebuild `sw_vfx` and `sw_2da` using the normal HAK builder afterward.

This is a custom table entry, not a new NWScript function or stock NWScript
constant. All ranks deliberately reference the same effect.
