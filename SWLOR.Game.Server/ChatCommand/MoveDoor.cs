//using System;
//using NWN;
//using SWLOR.Game.Server.ChatCommand.Contracts;
//using SWLOR.Game.Server.DoorRule.Contracts;
//using SWLOR.Game.Server.Enumeration;
//using SWLOR.Game.Server.GameObject;
//using static NWN._;

//namespace SWLOR.Game.Server.ChatCommand
//{
//    [CommandDetails("debugging.", CommandPermissionType.DM)]
//    public class MoveDoor : IChatCommand
//    {
//        

//        public MoveDoor(INWScript script)
//        {
//            
//        }

//        public void DoAction(NWPlayer user, params string[] args)
//        {
//            float orientation = Convert.ToSingle(args[0]);
//            float sqrtValue = Convert.ToSingle(args[1]);

//            NWPlaceable house = _.GetObjectByTag("house_ext_3");
//            NWPlaceable oldDoor = _.GetObjectByTag("building_ent1");

//            int pcBaseStructureID = oldDoor.GetLocalInt("PC_BASE_STRUCTURE_ID");

//            oldDoor.Destroy();

//            NWPlaceable newDoor = App.ResolveByInterface<IDoorRule, NWPlaceable>("DoorRule.LargeSquareBuildingRule", rule => rule.Run(user.Area, house.Location, orientation, sqrtValue));
//            newDoor.SetLocalInt("PC_BASE_STRUCTURE_ID", pcBaseStructureID);
//            newDoor.SetLocalInt("IS_DOOR", TRUE);


//        }
//    }
//}
