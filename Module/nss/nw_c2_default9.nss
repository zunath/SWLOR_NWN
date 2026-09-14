//:://////////////////////////////////////////////////
//:: Default OnSpawn handler
//:: NW_C2_DEFAULT9
//:://////////////////////////////////////////////////

#include "x0_i0_walkway"

void main()
{
    ExecuteScript("crea_spawn_bef", OBJECT_SELF);
    WalkWayPoints();
    ExecuteScript("crea_spawn_aft", OBJECT_SELF);
}
