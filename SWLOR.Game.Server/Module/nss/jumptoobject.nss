
void main()
{
object oPC = GetPlaceableLastClickedBy();

location locSpellTarget = GetLocation(OBJECT_SELF);

object portal = GetNearestObjectByTag("WP_jumpTarget");
 ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_UNSUMMON), locSpellTarget);

    AssignCommand(oPC,ActionJumpToObject(portal));




    object oFan1 = GetNearestObjectByTag("oFan1");
    object oFan2 = GetNearestObjectByTag("oFan2");
    object oFan3 = GetNearestObjectByTag("oFan3");
    object oFan4 = GetNearestObjectByTag("oFan4");
    object oFan5 = GetNearestObjectByTag("oFan5");
    object oFan6 = GetNearestObjectByTag("oFan6");
    object oFan7 = GetNearestObjectByTag("oFan7");
    object oFan8 = GetNearestObjectByTag("oFan8");
    object oFan9 = GetNearestObjectByTag("oFan9");


    DestroyObject(oFan1,5.0);
    DestroyObject(oFan2,5.0);
    DestroyObject(oFan3,5.0);
    DestroyObject(oFan4,5.0);
    DestroyObject(oFan5,5.0);
    DestroyObject(oFan6,5.0);
    DestroyObject(oFan7,5.0);
    DestroyObject(oFan8,5.0);
    DestroyObject(oFan9,5.0);
}


