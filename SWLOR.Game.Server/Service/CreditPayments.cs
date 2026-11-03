using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Service.TransferService;

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

        /// <summary>
        /// Takes the credits with the mark and saves the character file at once (<see cref="CharacterCheckpoint"/>), for a payment
        /// whose record is saved right after. When the file cannot be saved the credits and the mark are put back and nothing is paid.
        /// </summary>
        /// <returns>True when the file holds the payment.</returns>
        public static bool TryTakeSaved(uint player, CreditPaymentMark mark)
        {
            return CheckpointedChange.Run(
                () =>
                {
                    TakeGoldFromCreature(mark.Credits, player, true);
                    ObjectPlugin.SetString(player, MarkVariable, mark.Serialize(), true);
                    return true;
                },
                () => CharacterCheckpoint.Save(player),
                () =>
                {
                    GiveGoldToCreature(player, mark.Credits);
                    ObjectPlugin.DeleteString(player, MarkVariable);
                });
        }

        /// <summary>Records the payment on the ship it changed, to be saved with the change (<see cref="PlayerShip.PaymentIds"/>).</summary>
        public static void RecordOn(PlayerShip ship, uint player, CreditPaymentMark mark)
        {
            ship.PaymentIds ??= new Dictionary<string, string>();
            ship.PaymentIds[GetObjectUUID(player)] = mark.Id;
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

            if (!TryTakeSaved(player, mark))
            {
                UndoStationFounding(station, player);
                SendMessageToPC(player, ColorToken.Red("Your character could not be saved, so the station was not founded."));
                return;
            }

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

            if (!mark.KeepsPayment(GetRecordPaymentId(player, mark)))
            {
                GiveGoldToCreature(player, mark.Credits);
                var credits = mark.Credits;
                DelayCommand(6f, () => SendMessageToPC(player, ColorToken.Red(
                    $"A restart interrupted a payment of {credits} credits before what it paid for was saved. The credits were returned.")));
                Log.Write(LogGroup.Space, $"CREDIT PAYMENT RETURNED - {PlayerName.GetAuditName(player)} - {mark.Kind} {mark.RecordId} - {mark.Credits} credits whose change a restart never saved");
            }

            Retire(player);
        }

        /// <summary>The player's payment id saved with the mark's change on its record, or null when the record is gone.</summary>
        private static string GetRecordPaymentId(uint player, CreditPaymentMark mark)
        {
            if (string.IsNullOrWhiteSpace(mark.RecordId))
                return null;

            return mark.Kind switch
            {
                CreditPaymentKind.HullRefit or CreditPaymentKind.CustomsFine or CreditPaymentKind.ShipRepair
                    => DB.Get<PlayerShip>(mark.RecordId)?.PaymentIds?.GetValueOrDefault(GetObjectUUID(player)),
                CreditPaymentKind.ShuttleTicket => DB.Get<ShuttleRide>(mark.RecordId)?.PaymentId,
                _ => null
            };
        }
    }
}
