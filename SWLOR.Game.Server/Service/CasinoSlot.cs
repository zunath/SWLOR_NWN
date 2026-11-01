using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service
{
    public static class CasinoSlot
    {
        public const int SpinCost = 100;
        public const int MaximumPayout = 10000;

        public static bool IsCasinoMachine(string areaResref, string resref, string tag) =>
            areaResref == "pw_ar_nscasino" && resref == "swd_smach02" && tag == "SWTOR_SLOT2";

        public static int GetPayout(int roll)
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

        private static bool CanUse(uint player, uint machine) =>
            GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) &&
            !GetIsDMPossessed(player) && !GetIsDead(player) &&
            GetIsObjectValid(machine) && GetArea(player) == GetArea(machine) &&
            IsCasinoMachine(GetResRef(GetArea(machine)), GetResRef(machine), GetTag(machine)) &&
            GetDistanceBetween(player, machine) <= 4.0f;

        [NWNEventHandler(ScriptName.OnCasinoSlotUsed)]
        public static void OnUsed()
        {
            var player = GetLastUsedBy();
            if (CanUse(player, OBJECT_SELF))
                Gui.TogglePlayerWindow(player, GuiWindowType.CasinoSlot, new CasinoSlotPayload(OBJECT_SELF), OBJECT_SELF);
        }

        private static CasinoDailyPlay GetDailyPlay(uint player)
        {
            var publicCdKey = GetPCPublicCDKey(player);
            if (string.IsNullOrWhiteSpace(publicCdKey))
                return null;
            var id = CasinoPlayTracker.GetAccountId(publicCdKey);
            return DB.Get<CasinoDailyPlay>(id) ?? new CasinoDailyPlay { Id = id };
        }

        public static DateTime GetNextSpinUtc(uint player) =>
            GetDailyPlay(player)?.NextSpinUtc ?? DateTime.MinValue;

        public static CasinoSpinResult Spin(uint player, uint machine)
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
            DB.Set(play);
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
    }
}
