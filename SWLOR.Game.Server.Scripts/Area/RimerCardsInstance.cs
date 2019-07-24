using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Scripts.Area
{
    public class RimerCardsInstance: IScript
    {
        private Guid _moduleLoadID;

        public void SubscribeEvents()
        {
            _moduleLoadID = MessageHub.Instance.Subscribe<OnModuleLoad>(msg => CreateInstances());
        }

        public void UnsubscribeEvents()
        {
            MessageHub.Instance.Unsubscribe(_moduleLoadID);
        }

        public void Main()
        {
        }

        private void CreateInstances()
        {
            NWArea source = NWModule.Get().Areas.SingleOrDefault(x => x.Resref == "cardgame003");
            if (source == null) return;

            // Create 20 instances of the card game area.
            const int CopyCount = 20;

            for (int x = 1; x <= CopyCount; x++)
            {
                NWArea copy = _.CopyArea(source);
                copy.SetLocalInt("IS_AREA_INSTANCE", _.TRUE);
                copy.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();
                MessageHub.Instance.Publish(new OnAreaInstanceCreated(copy));
            }

            Console.WriteLine("Created " + CopyCount + " copies of Rimer Cards areas.");
        }
    }
}
