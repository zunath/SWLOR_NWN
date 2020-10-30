using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            var source = NWModule.Get().Areas.SingleOrDefault(x => GetResRef(x) == "cardgame003");
            if (!GetIsObjectValid(source)) return;

            // Create 20 instances of the card game area.
            const int CopyCount = 20;

            for (var x = 1; x <= CopyCount; x++)
            {
                var copy = CopyArea(source);
                SetLocalBool(copy, "IS_AREA_INSTANCE", true);
                BaseService.RegisterAreaStructures(copy);
                MessageHub.Instance.Publish(new OnAreaInstanceCreated(copy));
            }

            Console.WriteLine("Created " + CopyCount + " copies of Rimer Cards areas.");
        }
    }
}
