void main()
{
    object oTarget = (OBJECT_SELF);
    effect evil = EffectVisualEffect(VFX_DUR_PROTECTION_EVIL_MAJOR);
    ApplyEffectToObject(DURATION_TYPE_PERMANENT, evil, oTarget);
}
