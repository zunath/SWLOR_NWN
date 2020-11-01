using System;
using SWLOR.Game.Server.Legacy.Event.Creature;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Scripts.Creature
{
    public class AbandonedStationRancor: IScript
    {
        private Guid _creatureDeathID;

        public void SubscribeEvents()
        {
            _creatureDeathID = MessageHub.Instance.Subscribe<OnCreatureDeath>(OnCreatureDeath);
        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_creatureDeathID);
        }

        private void OnCreatureDeath(OnCreatureDeath @event)
        {
            NWCreature creature = OBJECT_SELF;
            if (creature.Resref != "zomb_rancor") return;

            var area = creature.Area;
            uint restrictedArea = GetLocalObject(area, "RESTRICTED_LEVEL");
            NWPlaceable elevator = GetNearestObjectByTag("aban_ele_to_office", GetFirstObjectInArea(restrictedArea));
            elevator.IsUseable = true;

            SpeakString("The rancor falls to the ground. Suddenly, the nearby elevator lights up. It looks like it can be used.");
        }

        public void Main()
        {
        }
    }
}
