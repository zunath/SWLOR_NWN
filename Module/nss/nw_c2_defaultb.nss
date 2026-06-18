//::///////////////////////////////////////////////
//:: Default On Spell Cast At
//:: NW_C2_DEFAULTB
//:://////////////////////////////////////////////

void main()
{
    ExecuteScript("crea_splcast_bef", OBJECT_SELF);
    ExecuteScript("crea_splcast_aft", OBJECT_SELF);
}
