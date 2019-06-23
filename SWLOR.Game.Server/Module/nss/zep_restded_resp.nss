//Function: zep_restded_resp
//Usage: OnHeartbeat of the Restless Dead invisible respawn
//          placable.  Checks the time since the placable
//          was created by am expiring restless dead, and
//          if past the respawn time constsnt's value,
//          creates a new creature at that point and destroys
//          itself.
//Author: Loki Hakanin

#include "zep_inc_monster"

void main()
{
//First we'll get our respawn timer and update it.  Then we
//check to see if we're pver to,e yet.
float fTime=GetLocalFloat(OBJECT_SELF,"RespawnDelayCount");
fTime+=6.0;


//If we're over the timer, we'll play some quick VFX, spawn a new
//skeleton, then destroy this placable.
if (fTime >= ZEP_RESTLESS_DEAD_RESPAWN_TIME)
    {
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT,EffectVisualEffect(VFX_IMP_NEGATIVE_ENERGY),GetLocation(OBJECT_SELF));
    CreateObject(OBJECT_TYPE_CREATURE,"zep_skelyellow",GetLocation(OBJECT_SELF));
    DestroyObject(OBJECT_SELF);
    }
else
    {
    SetLocalFloat(OBJECT_SELF,"RespawnDelayCount",fTime);
    }
}
