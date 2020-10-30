using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Creature;

namespace SWLOR.Game.Server.Scripts.Creature
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
            NWCreature creature = NWScript.OBJECT_SELF;
            if (creature.Resref != "zomb_rancor") return;

            var area = creature.Area;
            NWArea restrictedArea = area.GetLocalObject("RESTRICTED_LEVEL");
            NWPlaceable elevator = NWScript.GetNearestObjectByTag("aban_ele_to_office", NWScript.GetFirstObjectInArea(restrictedArea));
            elevator.IsUseable = true;

            NWScript.SpeakString("The rancor falls to the ground. Suddenly, the nearby elevator lights up. It looks like it can be used.");
        }

        public void Main()
        {
        }
    }
}
