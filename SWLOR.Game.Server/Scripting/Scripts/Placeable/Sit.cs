using System;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable
{
    public class Sit: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWObject user = GetLastUsedBy();
            user.AssignCommand(() =>
            {
                ActionSit(NWGameObject.OBJECT_SELF);
            });
        }
    }
}
