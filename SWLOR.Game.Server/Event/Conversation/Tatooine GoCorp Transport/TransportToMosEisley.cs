using NWN;
using SWLOR.Game.Server.GameObject;

using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class TransportToMosEisley : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer oPC = _.GetPCSpeaker();
            NWObject oNPC = Object.OBJECT_SELF;
            oPC = _.GetPCSpeaker();
            NWObject oWay01 = _.GetWaypointByTag("landspeeder_anc_mos");
            _.SetLocalString(oPC, "oDest", "");
            _.TakeGoldFromCreature(100, oPC);
            _.AssignCommand(oPC,()=> 
            {
                _.ActionJumpToObject(oWay01);
            });
            return true;
        }
        
    }
}
