using System;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Scripts.Placeable.Bank
{
    public class OnOpened : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = GetLastOpenedBy();
            if (!player.IsPlayer) return;

            NWPlaceable terminal = OBJECT_SELF;
            var area = terminal.Area;
            var bankID = terminal.GetLocalInt("BANK_ID");
            if (bankID <= 0)
            {
                Console.WriteLine("WARNING: Bank ID is not set on bank in area: " + GetName(area));
                return;
            }

            var entity = DataService.Bank.GetByID(bankID);

            if (entity == null)
            {
                entity = new Data.Entity.Bank
                {
                    AreaName = GetName(area),
                    AreaResref = GetResRef(area),
                    AreaTag = GetTag(area),
                    ID = bankID
                };
                DataService.SubmitDataChange(entity, DatabaseActionType.Insert);
            }

            var bankItems = DataService.BankItem.GetAllByPlayerIDAndBankID(player.GlobalID, bankID);
            foreach (var item in bankItems.Where(x => x.PlayerID == player.GlobalID))
            {
                SerializationService.DeserializeItem(item.ItemObject, terminal);
            }

            terminal.IsLocked = true;
            player.SendMessage("Walk away from the terminal when you are finished banking.");

        }
    }
}
