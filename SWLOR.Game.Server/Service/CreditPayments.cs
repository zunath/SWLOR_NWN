using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Credit payments for changes the database saves (<see cref="CreditPaymentMark"/>). TakeGoldFromCreature does
    /// nothing from a NUI or dialog handler, so every payment runs on the payer's own queue: the credits are taken and
    /// saved in the character file with a mark naming the change, the change is saved naming the payment, and the mark
    /// is retired. A station is founded before it is paid for and marked pending, so a login without its payment
    /// undoes it.
    /// </summary>
    public static class CreditPayments
    {
        private const string MarkVariable = "CREDIT_PAYMENT";

        public static CreditPaymentMark NewMark(CreditPaymentKind kind, string recordId, int credits) => new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = kind,
            RecordId = recordId ?? string.Empty,
            Credits = Math.Max(0, credits)
        };

        /// <summary>Takes the credits and saves them in the character file with the mark. Runs on the payer's queue.</summary>
        public static void Take(uint player, CreditPaymentMark mark)
        {
            TakeGoldFromCreature(mark.Credits, player, true);
            ObjectPlugin.SetString(player, MarkVariable, mark.Serialize(), true);
            ExportSingleCharacter(player);
        }

        /// <summary>The change the payment bought is saved: the mark goes and is saved too.</summary>
        public static void Retire(uint player)
        {
            ObjectPlugin.DeleteString(player, MarkVariable);
            ExportSingleCharacter(player);
        }

        // Station founding

        /// <summary>
        /// Runs on the founder's queue after the station was created pending its payment: takes the price with the mark,
        /// then clears the pending payment. A founder who no longer has the credits has the founding undone.
        /// </summary>
        public static void PayStationFounding(uint player, string stationId, CreditPaymentMark mark)
        {
            var station = DB.Get<WorldProperty>(stationId);
            if (station == null || station.PendingPaymentId != mark.Id)
                return;

            if (GetGold(player) < mark.Credits)
            {
                UndoStationFounding(station, player);
                SendMessageToPC(player, ColorToken.Red("You no longer have the credits, so the station was not founded."));
                return;
            }

            Take(player, mark);
            station.PendingPaymentId = string.Empty;
            station.PendingPaymentCredits = 0;
            DB.Set(station);
            Retire(player);
        }

        /// <summary>
        /// Undoes a station founding that was never paid: the station is queued for deletion (the next module load removes
        /// it, its buildings and its citizenship). A founder still online in the same session gets the Station Core back;
        /// after a restart the character file still holds it.
        /// </summary>
        private static void UndoStationFounding(WorldProperty station, uint returnCoreTo)
        {
            station.PendingPaymentId = string.Empty;
            station.PendingPaymentCredits = 0;
            station.IsQueuedForDeletion = true;
            DB.Set(station);

            if (GetIsObjectValid(returnCoreTo))
            {
                var core = DB.Search(new DBQuery<WorldProperty>()
                        .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), station.Id, false))
                    .FirstOrDefault(x => x.StructureType == StructureType.StationCore && !string.IsNullOrWhiteSpace(x.SerializedItem));
                var item = core == null ? OBJECT_INVALID : ObjectPlugin.Deserialize(core.SerializedItem);
                if (GetIsObjectValid(item))
                    ObjectPlugin.AcquireItem(returnCoreTo, item);
            }

            Log.Write(LogGroup.Property, $"Station '{station.Id}' was queued for deletion: its founding was never paid.");
        }

        /// <summary>
        /// Settles the stations a restart left waiting on their founding payment: kept when the character file names the
        /// payment, undone otherwise.
        /// </summary>
        private static void SettlePendingFoundings(uint player, CreditPaymentMark mark)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.OwnerPlayerId), GetObjectUUID(player), false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return;

            foreach (var station in DB.Search(query.AddPaging(count, 0)).Where(x => !string.IsNullOrWhiteSpace(x.PendingPaymentId)).ToList())
            {
                if (CreditPaymentMark.PaysPendingFounding(mark, station.PendingPaymentId))
                {
                    station.PendingPaymentId = string.Empty;
                    station.PendingPaymentCredits = 0;
                    DB.Set(station);
                    continue;
                }

                UndoStationFounding(station, OBJECT_INVALID);
                DelayCommand(6f, () => SendMessageToPC(player, ColorToken.Red(
                    "A restart stopped your station's founding before its price was paid, so the station was not founded. You keep the Station Core and your credits.")));
            }
        }

        // Login

        /// <summary>
        /// Settles the credit payment a restart interrupted: pending station foundings first (they read the mark), then
        /// the mark itself, whose payment is returned when its change was never saved. The mark is retired either way.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleInterruptedPayment()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            CreditPaymentMark.TryParse(ObjectPlugin.GetString(player, MarkVariable), out var mark);
            SettlePendingFoundings(player, mark);
            if (mark == null)
                return;

            var recordPaymentId = mark.Kind is CreditPaymentKind.HullRefit or CreditPaymentKind.CustomsFine && !string.IsNullOrWhiteSpace(mark.RecordId)
                ? DB.Get<PlayerShip>(mark.RecordId)?.LastPaymentId
                : null;
            if (!mark.KeepsPayment(recordPaymentId))
            {
                GiveGoldToCreature(player, mark.Credits);
                var credits = mark.Credits;
                DelayCommand(6f, () => SendMessageToPC(player, ColorToken.Red(
                    $"A restart interrupted a payment of {credits} credits before what it paid for was saved. The credits were returned.")));
                Log.Write(LogGroup.Space, $"CREDIT PAYMENT RETURNED - {PlayerName.GetAuditName(player)} - {mark.Kind} {mark.RecordId} - {mark.Credits} credits whose change a restart never saved");
            }

            Retire(player);
        }
    }
}
