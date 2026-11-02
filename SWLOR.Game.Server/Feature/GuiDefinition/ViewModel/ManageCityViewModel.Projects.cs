using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The City Projects part of Manage City: the facility upgrade buttons start a project (the treasury holds the credits, and anyone
    /// donates the materials at City Hall), and so does Home Ground.
    /// </summary>
    public partial class ManageCityViewModel
    {
        private const int UpkeepPerFacilityLevel = 10000;

        public string HomeGroundLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HomeGroundTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool CanStartHomeGround
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>The project types the city has open, in any state, so a second one of the same kind cannot start.</summary>
        private HashSet<CityProjectType> GetOpenProjectTypes()
        {
            return CityProjects.GetProjects(_cityId).Select(project => project.ProjectType).ToHashSet();
        }

        /// <summary>Sets the Home Ground line from the city's projects. Run whenever the upgrade levels refresh.</summary>
        private void RefreshHomeGroundText(WorldProperty dbCity)
        {
            var open = CityProjects.GetProjectOfType(_cityId, CityProjectType.HomeGround);
            var built = CityProjects.HasHomeGround(dbCity);

            HomeGroundLevel = built ? "Home Ground: Built"
                : open != null ? "Home Ground: Under construction"
                : "Home Ground: Not built";

            // A Wild Space station has no planet ground, so its drill gets the bonus instead.
            var effect = Edict.IsWildSpaceStation(dbCity)
                ? $"crops harvested on the station yield 1 more item, and its Asteroid Drill mines {StationBuildingRules.HomeGroundDrillPercent}% faster."
                : "citizens get a separate 5% chance of 1 more item per harvest on the planet's ground, and crops harvested here yield 1 more item.";
            HomeGroundTooltip = built
                ? $"Home Ground is built: {effect}"
                : $"Costs {CityProjectRules.HomeGroundCredits:N0} credits and {DescribeMaterials(CityProjectCatalog.Get(CityProjectType.HomeGround, 0))}. Once built, {effect}";
        }

        private static string DescribeMaterials(CityProjectRecipe recipe)
        {
            return recipe == null ? "no materials" : CityProjectRules.DescribeMaterials(recipe.Materials, CityProjects.GetItemName);
        }

        private string DescribeProjectMaterials(CityProjectType type, int level)
        {
            return DescribeMaterials(CityProjectCatalog.Get(type, level));
        }

        private void StartProject(CityProjectType type)
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);
            var level = CityProjects.GetNextLevel(dbCity, type);
            var refusal = CityProjects.CheckStart(dbCity, type, GetObjectUUID(Player));
            if (refusal != CityProjectRefusal.None)
            {
                RefreshPermissions();
                RefreshUpgradeLevels();
                Instructions = CityProjectRules.Describe(refusal, CityProjectRules.GetCredits(type, level));
                InstructionsColor = GuiColor.Red;
                return;
            }

            var name = CityProjectRules.GetName(type, level);
            var credits = CityProjectRules.GetCredits(type, level);
            var upkeep = type == CityProjectType.HomeGround
                ? string.Empty
                : $" It increases your weekly maintenance bill by an additional {UpkeepPerFacilityLevel} credits once built.";

            ShowModal(
                $"Start the {name} project? The treasury holds {credits:N0} credits for it now, restricted funds first. Anyone can donate " +
                $"{DescribeProjectMaterials(type, level)} at City Hall, and the project completes 2 days after the last material arrives.{upkeep} " +
                "You can cancel it before then and get everything back.",
                () =>
                {
                    var result = CityProjects.Start(_cityId, type, GetObjectUUID(Player), out var project);

                    RefreshPropertyDetails();
                    RefreshPermissions();
                    RefreshUpgradeLevels();

                    if (result != CityProjectRefusal.None)
                    {
                        Instructions = CityProjectRules.Describe(result, credits);
                        InstructionsColor = GuiColor.Red;
                        return;
                    }

                    Instructions = $"Project started: {CityProjectRules.GetName(project)}. Donate the materials at City Hall.";
                    InstructionsColor = GuiColor.Green;
                });
        }

        public Action StartHomeGround() => () => StartProject(CityProjectType.HomeGround);

        public Action OpenCityProjects() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.CityProjects, new CityProjectsPayload(_cityId), TetherObject);
        };
    }
}
