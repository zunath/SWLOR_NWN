using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>The game-engine side of a visit to a scavenge site. The rules it carries out are in <see cref="ScavengeLifecycle"/>.</summary>
    public sealed class NwnScavengeWorld : IScavengeWorld
    {
        public static readonly NwnScavengeWorld Instance = new();

        /// <summary>Set on a site while its inventory is being opened for the scavenger who searched it, so opening it says nothing.</summary>
        public const string OpeningVariable = "SCAVENGE_OPENING_FOR_SEARCHER";

        /// <summary>Set on a site while a player has closed its inventory to open the board, so closing it does not remove the site.</summary>
        public const string DoNotDestroyVariable = "DO_NOT_DESTROY";

        public const string SearchedVariable = "FULLY_HARVESTED";

        public void Message(uint player, string text)
        {
            SendMessageToPC(player, text);
        }

        public void SkillCheckMessage(uint player, string text)
        {
            FloatingTextStringOnCreature(ColorToken.SkillCheck(text), player, false);
        }

        public void GiveGatheringXp(uint player, int xp)
        {
            Skill.GiveSkillXP(player, SkillType.Gathering, xp, false, false);
        }

        public string CreateItem(uint site, string resref, int quantity)
        {
            var item = CreateItemOnObject(resref, site, quantity);
            return GetIsObjectValid(item) ? GetName(item) : resref;
        }

        public void SpawnBonusLoot(uint site, uint player)
        {
            Loot.SpawnLoot(site, player, "LOOT_TABLE_");
        }

        public void MarkSearched(uint site)
        {
            SetLocalBool(site, SearchedVariable, true);
        }

        public void OpenSite(uint player, uint site, double animationSeconds)
        {
            SetLocalBool(site, OpeningVariable, true);
            AssignCommand(player, () =>
            {
                if (animationSeconds > 0)
                    ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, (float)animationSeconds);

                ActionInteractObject(site);
            });
        }

        public void DestroySite(uint player, uint site, double animationSeconds)
        {
            if (animationSeconds > 0 && GetIsObjectValid(player))
                AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, (float)animationSeconds));

            // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
            // Use EffectDeath to ensure that we trigger death processing.
            AssignCommand(GetModule(), () => DelayCommand((float)animationSeconds, () =>
            {
                if (!GetIsObjectValid(site))
                    return;

                SetPlotFlag(site, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), site);
            }));
        }

        public bool OpenWindow(uint player, ScavengeSession session)
        {
            // The result screen of the last visit may still be open.
            if (Gui.IsWindowOpen(player, GuiWindowType.Search))
                Gui.ClosePlayerWindow(player, GuiWindowType.Search);

            Gui.TogglePlayerWindow(player, GuiWindowType.Search, new SearchPayload(session));
            return Gui.IsWindowOpen(player, GuiWindowType.Search);
        }

        public void CloseWindow(uint player)
        {
            if (Gui.IsWindowOpen(player, GuiWindowType.Search))
                Gui.ClosePlayerWindow(player, GuiWindowType.Search);
        }

        public ScavengePlayerState GetPlayerState(uint player, uint site)
        {
            if (!GetIsObjectValid(player))
                return default;

            return new ScavengePlayerState(
                true,
                GetIsDead(player),
                GetIsInCombat(player),
                Gui.IsWindowOpen(player, GuiWindowType.Search),
                GetIsObjectValid(site) ? GetDistanceBetween(player, site) : 0f);
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public void Schedule(float seconds, Action action)
        {
            AssignCommand(GetModule(), () => DelayCommand(seconds, action));
        }
    }
}
