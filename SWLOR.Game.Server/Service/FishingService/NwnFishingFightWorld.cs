using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>The game-engine side of a bite and its fight. The rules it carries out are in <see cref="FishingFightLifecycle"/>.</summary>
    public sealed class NwnFishingFightWorld : IFishingFightWorld
    {
        public static readonly NwnFishingFightWorld Instance = new();

        public void SetRemainingBait(uint rod, int remaining)
        {
            SetLocalInt(rod, Fishing.RemainingBaitVariable, remaining);
        }

        public void SetFishingPointAttempts(uint fishingPoint, int remaining)
        {
            SetLocalInt(fishingPoint, Fishing.FishingPointRemainingAttemptsVariable, remaining);
        }

        public void ExhaustFishingPoint(uint fishingPoint)
        {
            // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
            // Use EffectDeath to ensure that we trigger death processing.
            SetPlotFlag(fishingPoint, false);
            ApplyEffectToObject(DurationType.Instant, EffectDeath(), fishingPoint);

            Messaging.SendMessageNearbyToPlayers(fishingPoint, "The fishing point has been exhausted.");
        }

        public void GiveAgricultureXp(uint player, int xp)
        {
            Skill.GiveSkillXP(player, SkillType.Agriculture, xp, false, false);
        }

        public void Message(uint player, string text)
        {
            SendMessageToPC(player, text);
        }

        public void CreateFish(uint player, string resref)
        {
            CreateItemOnObject(resref, player);
        }

        public void BeginFight(uint player)
        {
            Activity.SetBusy(player, ActivityStatusType.Fishing);
        }

        public void EndFight(uint player)
        {
            // A replacement activity may already own the player's busy state.
            if (GetIsObjectValid(player) && Activity.GetBusyType(player) == ActivityStatusType.Fishing)
                Activity.ClearBusy(player);
        }

        public bool OpenWindow(uint player, FishingFight fight)
        {
            // The result screen of the last fight may still be open.
            if (Gui.IsWindowOpen(player, GuiWindowType.Line))
                Gui.ClosePlayerWindow(player, GuiWindowType.Line);

            Gui.TogglePlayerWindow(player, GuiWindowType.Line, new LinePayload(fight));
            return Gui.IsWindowOpen(player, GuiWindowType.Line);
        }

        public void CloseWindow(uint player)
        {
            if (Gui.IsWindowOpen(player, GuiWindowType.Line))
                Gui.ClosePlayerWindow(player, GuiWindowType.Line);
        }

        public FishingPlayerState GetPlayerState(uint player)
        {
            if (!GetIsObjectValid(player))
                return default;

            return new FishingPlayerState(
                true,
                GetIsDead(player),
                GetIsInCombat(player),
                GetPosition(player),
                Gui.IsWindowOpen(player, GuiWindowType.Line),
                GetItemInSlot(InventorySlot.RightHand, player),
                Activity.GetBusyType(player) == ActivityStatusType.Fishing);
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public void Schedule(float seconds, Action action)
        {
            AssignCommand(GetModule(), () => DelayCommand(seconds, action));
        }
    }
}
