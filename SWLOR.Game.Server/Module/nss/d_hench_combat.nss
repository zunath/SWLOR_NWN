#include "nw_i0_generic"
void main()
{
    //Don't fight back - just cast spell if queued.
    if(GetLocalInt(OBJECT_SELF, "SpellToCast") > 0)
    {
        SignalEvent(OBJECT_SELF, EventUserDefined(1003));
        return;
    }

    if(!GetSpawnInCondition(NW_FLAG_SET_WARNINGS))
    {
       DetermineCombatRound();
    }
}

