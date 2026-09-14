//:://////////////////////////////////////////////////
//:: Default OnDamaged handler
//:: NW_C2_DEFAULT6
//:://////////////////////////////////////////////////

void main()
{
    ExecuteScript("crea_damaged_bef", OBJECT_SELF);
    ExecuteScript("crea_damaged_aft", OBJECT_SELF);
}
