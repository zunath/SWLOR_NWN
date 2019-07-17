using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

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
            NWPlayer player = _.GetLastOpenedBy();
            if (!player.IsPlayer) return;

            NWPlaceable terminal = NWGameObject.OBJECT_SELF;
            NWArea area = terminal.Area;
            int bankID = terminal.GetLocalInt("BANK_ID");
            if (bankID <= 0)
            {
                Console.WriteLine("WARNING: Bank ID is not set on bank in area: " + area.Name);
                return;
            }

            Data.Entity.Bank entity = DataService.Bank.GetByID(bankID);

            if (entity == null)
            {
                entity = new Data.Entity.Bank
                {
                    AreaName = area.Name,
                    AreaResref = area.Resref,
                    AreaTag = area.Tag,
                    ID = bankID
                };
                DataService.SubmitDataChange(entity, DatabaseActionType.Insert);
            }

            var bankItems = DataService.BankItem.GetAllByPlayerIDAndBankID(player.GlobalID, bankID);
            foreach (BankItem item in bankItems.Where(x => x.PlayerID == player.GlobalID))
            {
                SerializationService.DeserializeItem(item.ItemObject, terminal);
            }

            terminal.IsLocked = true;
            player.SendMessage("Walk away from the terminal when you are finished banking.");

        }
    }
}
