
using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;


namespace SWLOR.Game.Server.Placeable.Bank
{
    public class OnOpened : IRegisteredEvent
    {
        
        
        private readonly ISerializationService _serialization;

        public OnOpened(
            
            
            ISerializationService serialization)
        {
            
            
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastOpenedBy();
            if (!player.IsPlayer) return false;

            NWPlaceable terminal = Object.OBJECT_SELF;
            NWArea area = terminal.Area;
            int bankID = terminal.GetLocalInt("BANK_ID");
            if (bankID <= 0)
            {
                Console.WriteLine("WARNING: Bank ID is not set on bank in area: " + area.Name);
                return false;
            }

            Data.Entity.Bank entity = DataService.SingleOrDefault<Data.Entity.Bank>(x => x.ID == bankID);
            
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
            
            var bankItems = DataService.Where<BankItem>(x => x.PlayerID == player.GlobalID && x.BankID == entity.ID);
            foreach (BankItem item in bankItems.Where(x => x.PlayerID == player.GlobalID))
            {
                _serialization.DeserializeItem(item.ItemObject, terminal);
            }

            terminal.IsLocked = true;
            player.SendMessage("Walk away from the terminal when you are finished banking.");

            return true;
        }
    }
}
