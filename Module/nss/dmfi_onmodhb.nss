
#include "dmfi_db_inc"

const int FLUSH_INTERVAL = 30; // seconds between database flushes to disk

void main()
{
    // see if database is "dirty" (changed since last flush)
    if (IsDMFIPersistentDataDirty("dmfi"))
    {
        // it is, so check if time to flush database
        object oMod = GetModule();
        int iTick = GetLocalInt(oMod, "DMFI_MODULE_HEARTBEAT_TICK");
        int iSecsSinceFlush = iTick * 6;
        if (iSecsSinceFlush >= FLUSH_INTERVAL)
        {
            FlushDMFIPersistentData("dmfi");
            iTick = 0;
        }
        else
        {
            iTick++;
        }
        SetLocalInt(oMod, "DMFI_MODULE_HEARTBEAT_TICK", iTick);
    }

    // do any other module OnHeartbeat work here
    ExecuteScript("x3_mod_def_hb", OBJECT_SELF);
}
