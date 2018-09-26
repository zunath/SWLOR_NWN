
using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;


namespace SWLOR.Game.Server.Placeable.Bank
{
    public class OnOpened : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly ISerializationService _serialization;

        public OnOpened(
            INWScript script,
            IDataContext db,
            ISerializationService serialization)
        {
            _ = script;
            _db = db;
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

            Data.Entities.Bank entity = _db.Banks.SingleOrDefault(x => x.BankID == bankID);

            if (entity == null)
            {
                entity = new Data.Entities.Bank
                {
                    AreaName = area.Name,
                    AreaResref = area.Resref,
                    AreaTag = area.Tag,
                    BankID = bankID
                };
                _db.Banks.Add(entity);
                _db.SaveChanges();
            }

            foreach (BankItem item in entity.BankItems.Where(x => x.PlayerID == player.GlobalID))
            {
                _serialization.DeserializeItem(item.ItemObject, terminal);
            }

            terminal.IsLocked = true;
            player.SendMessage("Walk away from the terminal when you are finished banking.");

            return true;
        }
    }
}
