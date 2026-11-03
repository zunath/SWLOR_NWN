using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The casino slot machine: the logic that used to be static in <c>Service.CasinoSlot</c>, moved here unchanged. The
    /// facade still carries the event handler and the constants and forwards every call to the container's instance.
    /// </summary>
    public class CasinoSlotService : ICasinoSlotService
    {
        /// <summary>The credits a spin costs. <c>Service.CasinoSlot.SpinCost</c> repeats it.</summary>
        public const int SpinCost = 100;

        /// <summary>The most a spin can pay. <c>Service.CasinoSlot.MaximumPayout</c> repeats it.</summary>
        public const int MaximumPayout = 10000;

        private readonly IDBService _db;

        public CasinoSlotService(IDBService db)
        {
            _db = db;
        }

        public bool IsCasinoMachine(string areaResref, string resref, string tag) =>
            areaResref == "pw_ar_nscasino" && resref == "swd_smach02" && tag == "SWTOR_SLOT2";

        public int GetPayout(int roll)
        {
            if (roll < 0 || roll >= 10000)
                throw new ArgumentOutOfRangeException(nameof(roll));

            return roll switch
            {
                < 7000 => 0,
                < 9000 => 100,
                < 9800 => 500,
                < 9990 => 1000,
                _ => MaximumPayout
            };
        }

        private bool CanUse(uint player, uint machine) =>
            GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) &&
            !GetIsDMPossessed(player) && !GetIsDead(player) &&
            GetIsObjectValid(machine) && GetArea(player) == GetArea(machine) &&
            IsCasinoMachine(GetResRef(GetArea(machine)), GetResRef(machine), GetTag(machine)) &&
            GetDistanceBetween(player, machine) <= 4.0f;

        public void OnUsed()
        {
            var player = GetLastUsedBy();
            if (CanUse(player, OBJECT_SELF))
                TogglePlayerWindow(player, new CasinoSlotPayload(OBJECT_SELF), OBJECT_SELF);
        }

        private CasinoDailyPlay GetDailyPlay(uint player)
        {
            var publicCdKey = GetPCPublicCDKey(player);
            if (string.IsNullOrWhiteSpace(publicCdKey))
                return null;
            var id = CasinoPlayTracker.GetAccountId(publicCdKey);
            return _db.Get<CasinoDailyPlay>(id) ?? new CasinoDailyPlay { Id = id };
        }

        public DateTime GetNextSpinUtc(uint player) =>
            GetDailyPlay(player)?.NextSpinUtc ?? DateTime.MinValue;

        public CasinoSpinResult Spin(uint player, uint machine)
        {
            if (!CanUse(player, machine))
                return new CasinoSpinResult(false, 0, "You must be beside the slot machine in the Smuggler's Moon Casino.");

            var now = DateTime.UtcNow;
            var play = GetDailyPlay(player);
            if (play == null)
                return new CasinoSpinResult(false, 0, "Your casino account could not be loaded. No credits were taken.");
            if (CasinoPlayTracker.IsCoolingDown(play, now))
                return new CasinoSpinResult(false, 0, string.Empty);
            if (GetGold(player) < SpinCost)
                return new CasinoSpinResult(false, 0, "You need 100 credits to spin.");

            var payout = Math.Min(GetPayout(Random.Next(10000)), MaximumPayout);
            CasinoPlayTracker.RecordSpin(play, now, payout);
            // Save the cooldown before settling credits so a failed save cannot allow free retries.
            _db.Set(play);
            AssignCommand(player, () =>
            {
                TakeGoldFromCreature(SpinCost, player, true);
                if (payout > 0)
                    GiveGoldToCreature(player, payout);
                ExportSingleCharacter(player);
            });

            Log.WriteStructured(LogGroup.Server,
                "Casino spin: PlayerId={PlayerId} MachineId={MachineId} Stake={Stake} Payout={Payout} DailySpins={DailySpins} DailyWins={DailyWins}",
                GetObjectUUID(player), GetObjectUUID(machine), SpinCost, payout, play.Spins, play.ProfitableWins);

            var result = payout switch
            {
                0 => "0 credits",
                100 => "100 credits",
                500 => "500 credits",
                1000 => "1,000 credits",
                _ => "10,000 credits"
            };
            AssignCommand(player, () => PlaySound("gui_prompt"));
            if (payout == MaximumPayout)
                AssignCommand(machine, () => SpeakString("Jackpot! This machine has paid out 10,000 credits!"));
            return new CasinoSpinResult(true, payout, result);
        }

        /// <summary>
        /// Opens the slot machine window. The original call into <c>Gui</c> (which is not converted yet and keeps its
        /// window table in static state), on its own so a unit test can answer it.
        /// </summary>
        protected virtual void TogglePlayerWindow(uint player, GuiPayloadBase payload, uint machine)
        {
            Gui.TogglePlayerWindow(player, GuiWindowType.CasinoSlot, payload, machine);
        }
    }
}
