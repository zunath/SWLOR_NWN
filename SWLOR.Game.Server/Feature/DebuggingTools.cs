using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void KillMe()
        {
            var player = GetLastUsedBy();

            Space.ApplyShipDamage(player, player, 999);
        }

        [NWNEventHandler("test")]
        public static void GiveKeyItems()
        {
            var player = GetLastUsedBy();
            KeyItem.GiveKeyItem(player, KeyItemType.AvixTathamsWorkReceipt);
            KeyItem.GiveKeyItem(player, KeyItemType.HalronLinthsWorkReceipt);
            KeyItem.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt);
            KeyItem.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
            KeyItem.GiveKeyItem(player, KeyItemType.CZ220ShuttlePass);
            KeyItem.GiveKeyItem(player, KeyItemType.CZ220ExperimentRoomKey);
            KeyItem.GiveKeyItem(player, KeyItemType.MandalorianFacilityKey);
            KeyItem.GiveKeyItem(player, KeyItemType.YellowKeyCard);
            KeyItem.GiveKeyItem(player, KeyItemType.RedKeyCard);
            KeyItem.GiveKeyItem(player, KeyItemType.BlueKeyCard);
            KeyItem.GiveKeyItem(player, KeyItemType.SlicingProgram);
            KeyItem.GiveKeyItem(player, KeyItemType.DataDisc1);
            KeyItem.GiveKeyItem(player, KeyItemType.DataDisc2);
            KeyItem.GiveKeyItem(player, KeyItemType.DataDisc3);
            KeyItem.GiveKeyItem(player, KeyItemType.DataDisc4);
            KeyItem.GiveKeyItem(player, KeyItemType.DataDisc5);
            KeyItem.GiveKeyItem(player, KeyItemType.DataDisc6);
            KeyItem.GiveKeyItem(player, KeyItemType.PackageForDenamReyholm);
            KeyItem.GiveKeyItem(player, KeyItemType.OldTome);
            KeyItem.GiveKeyItem(player, KeyItemType.CoxxionBaseKey);
            KeyItem.GiveKeyItem(player, KeyItemType.TaxiHailingDevice);
        }

    }
}
