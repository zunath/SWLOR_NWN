using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Domain.Character;
using ResearchJob = SWLOR.Game.Server.Entity.ResearchJob;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// The crafting devices (the crafting device, the refinery and the research terminal) and the crafting handlers of the property and
    /// login scripts. Moved out of <see cref="CraftService"/> unchanged; the research experiment service is injected directly now that
    /// <see cref="CraftService"/> no longer calls it.
    /// </summary>
    public class CraftDeviceService : ICraftDeviceService
    {
        private readonly IDBService _db;
        private readonly IGuiService _gui;
        private readonly IPropertyService _property;
        private readonly ICraftResearchExperimentService _craftResearchExperiment;
        private readonly ICraftStyleEffectsService _craftStyleEffects;

        public CraftDeviceService(IDBService db, IGuiService gui, IPropertyService property, ICraftResearchExperimentService craftResearchExperiment, ICraftStyleEffectsService craftStyleEffects)
        {
            _db = db;
            _gui = gui;
            _property = property;
            _craftResearchExperiment = craftResearchExperiment;
            _craftStyleEffects = craftStyleEffects;
        }

        /// <summary>
        /// When a crafting device is used, display the recipe menu.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCraftUsed)]
        public void UseCraftingDevice()
        {
            var player = GetLastUsedBy();
            var skillType = (SkillType)GetLocalInt(OBJECT_SELF, "CRAFTING_SKILL_TYPE_ID");
            var payload = new RecipesPayload(RecipesUIMode.Crafting, skillType);
            _gui.TogglePlayerWindow(player, GuiWindowType.Recipes, payload, OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnRefineryUsed)]
        public void UseRefinery()
        {
            var player = GetLastUsedBy();
            _gui.TogglePlayerWindow(player, GuiWindowType.Refinery, null, OBJECT_SELF);
        }
        [NWNEventHandler(ScriptName.OnResearchTerminal)]
        public void UseResearchTerminal()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var terminal = OBJECT_SELF;

            if (CraftRankMilestones.GetResearchTier(player) <= 0)
            {
                SendMessageToPC(player, $"Fabrication rank {CraftRankMilestones.RequiredFabricationRankForResearchTier(1)} is required to use research terminals.");
                return;
            }

            var propertyId = _property.GetPropertyId(terminal);

            if (string.IsNullOrWhiteSpace(propertyId))
            {
                SendMessageToPC(player, $"This research terminal cannot be used.");
                return;
            }

            var query = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.ParentPropertyId), propertyId, false);
            var dbJob = _db.Search(query)
                .FirstOrDefault();

            if (dbJob == null)
            {
                var payload = new RecipesPayload(RecipesUIMode.Research, SkillType.Invalid);
                _gui.TogglePlayerWindow(player, GuiWindowType.Recipes, payload, terminal);
            }
            else
            {
                if (dbJob.PlayerId != playerId)
                {
                    var now = Clock.UtcNow;
                    if (dbJob.DateCompleted > now)
                    {
                        var delta = dbJob.DateCompleted - now;
                        var completionTime = Time.GetTimeLongIntervals(delta, false);
                        SendMessageToPC(player, $"Another player's research job is active. This job will complete in: {completionTime}.");
                    }
                    else
                    {
                        SendMessageToPC(player, $"Another player's research job is active. This job has completed.");
                    }
                }
                else
                {
                    var payload = new ResearchPayload(propertyId, OBJECT_INVALID, RecipeType.Invalid);
                    _gui.TogglePlayerWindow(player, GuiWindowType.Research, payload, terminal);
                }
            }
        }

        /// <summary>
        /// When a property is removed, also remove any associated research jobs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public void OnRemoveProperty()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            var dbQuery = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.ParentPropertyId), propertyId, false);
            var dbJobs = _db.Search(dbQuery).ToList();

            foreach (var dbJob in dbJobs)
            {
                _craftResearchExperiment.DeleteJob(dbJob);
            }
        }

        /// <summary>Applies the player's selected crafting styles again when they log in; the effects are not kept across a logout.</summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "The crafting style status effects are carried.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void RestoreCraftingStylesOnEnter()
        {
            _craftStyleEffects.Restore(GetEnteringObject());
        }
    }
}
