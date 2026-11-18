using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature;

public static class AlchemizedFrogDeath
{
    [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
    public static void OnDeath()
    {
        // The spawn service installs the standard death handler over the blueprint's own script.
        if (GetResRef(OBJECT_SELF) == "frogboss")
            ExecuteScript("sw_frogdeath", OBJECT_SELF);
    }
}
