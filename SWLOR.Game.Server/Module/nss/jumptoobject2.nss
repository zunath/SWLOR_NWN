void main()
{
object oPC = GetPlaceableLastClickedBy();

location locSpellTarget = GetLocation(OBJECT_SELF);

object portal = GetNearestObjectByTag("WP_jumpTarget2");
 ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_UNSUMMON), locSpellTarget);

    AssignCommand(oPC,ActionJumpToObject(portal));





    object oFan11 = GetNearestObjectByTag("oFan11");
    object oFan12 = GetNearestObjectByTag("oFan12");
    object oFan13 = GetNearestObjectByTag("oFan13");
    object oFan14 = GetNearestObjectByTag("oFan14");



    DestroyObject(oFan11,5.0);
    DestroyObject(oFan12,5.0);
    DestroyObject(oFan13,5.0);
    DestroyObject(oFan14,5.0);

}
