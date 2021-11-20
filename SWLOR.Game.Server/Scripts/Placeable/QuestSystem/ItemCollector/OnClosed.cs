﻿using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.QuestSystem.ItemCollector
{
    public class OnClosed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWObject container = _.OBJECT_SELF;
            container.DestroyAllInventoryItems();
            container.Destroy();
        }
    }
}
