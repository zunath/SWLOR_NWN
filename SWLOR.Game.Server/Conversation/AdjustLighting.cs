using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class AdjustLighting : ConversationBase
    {

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "Adjust Area Lighting.",
                "Main Light 1",
                "Main Light 2",
                "Source Light 1",
                "Source Light 2");

            DialogPage colorPage = new DialogPage("Please select a color.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ColorPage", colorPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "ColorPage":
                    ColorPageResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {

        }

        private void MainResponses(int responseID)
        {
            DialogPage currentPage = GetCurrentPage();
            currentPage.CustomData.Clear();
            currentPage.CustomData.Add("LIGHT_TYPE", responseID);
            BuildColorPage(responseID);
            ChangePage("ColorPage");
        }

        private void BuildColorPage(int responseID)
        {
            List<string []> colorList = new List<string []>();            
            
            ClearPageResponses("ColorPage");


            //responseIDs:
            //case 1: // Change Main Light 1
            //case 2: // Change Main Light 2
            //case 3: // Change Source Light 1
            //case 4: // Change Source Light 2

            // TILE_MAIN_LIGHT_* Constant Group
            if (responseID == 1 || responseID == 2)
            {
                colorList.Add(new string[2] { "Black", "0" });
                colorList.Add(new string[2] { "Dim White", "1" });
                colorList.Add(new string[2] { "White", "2" });
                colorList.Add(new string[2] { "Bright White", "3" });
                colorList.Add(new string[2] { "Pale Dark Yellow", "4" });
                colorList.Add(new string[2] { "Dark Yellow", "5" });
                colorList.Add(new string[2] { "Pale Yellow", "6" });
                colorList.Add(new string[2] { "Yellow", "7" });
                colorList.Add(new string[2] { "Pale Dark Green", "8" });
                colorList.Add(new string[2] { "Dark Green", "9" });
                colorList.Add(new string[2] { "Pale Green", "10" });
                colorList.Add(new string[2] { "Green", "11" });
                colorList.Add(new string[2] { "Pale Dark Aqua", "12" });
                colorList.Add(new string[2] { "Dark Aqua", "13" });
                colorList.Add(new string[2] { "Pale Aqua", "14" });
                colorList.Add(new string[2] { "Aqua", "15" });
                colorList.Add(new string[2] { "Pale Dark Blue", "16" });
                colorList.Add(new string[2] { "Dark Blue", "17" });
                colorList.Add(new string[2] { "Pale Blue", "18" });
                colorList.Add(new string[2] { "Blue", "19" });
                colorList.Add(new string[2] { "Pale Dark Purple", "20" });
                colorList.Add(new string[2] { "Dark Purple", "21" });
                colorList.Add(new string[2] { "Pale Purple", "22" });
                colorList.Add(new string[2] { "Purple", "23" });
                colorList.Add(new string[2] { "Pale Dark Red", "24" });
                colorList.Add(new string[2] { "Dark Red", "25" });
                colorList.Add(new string[2] { "Pale Red", "26" });
                colorList.Add(new string[2] { "Red", "27" });
                colorList.Add(new string[2] { "Pale Dark Orange", "28" });
                colorList.Add(new string[2] { "Dark Orange", "29" });
                colorList.Add(new string[2] { "Pale Orange", "30" });
                colorList.Add(new string[2] { "Orange", "31" });
            }
            //TILE_SOURCE_LIGHT_* Constant Group
            else if (responseID == 3 || responseID == 4)
            {
                colorList.Add(new string[2] { "Black", "0" });
                colorList.Add(new string[2] { "White", "1" });
                colorList.Add(new string[2] { "Pale Dark Yellow", "2" });
                colorList.Add(new string[2] { "Pale Yellow", "3" });
                colorList.Add(new string[2] { "Pale Dark Green", "4" });
                colorList.Add(new string[2] { "Pale Green", "5" });
                colorList.Add(new string[2] { "Pale Dark Aqua", "6" });
                colorList.Add(new string[2] { "Pale Aqua", "7" });
                colorList.Add(new string[2] { "Pale Dark Blue", "8" });
                colorList.Add(new string[2] { "Pale Blue", "9" });
                colorList.Add(new string[2] { "Pale Dark Purple", "10" });
                colorList.Add(new string[2] { "Pale Purple", "11" });
                colorList.Add(new string[2] { "Pale Dark Red", "12" });
                colorList.Add(new string[2] { "Pale Red", "13" });
                colorList.Add(new string[2] { "Pale Dark Orange", "14" });
                colorList.Add(new string[2] { "Pale Orange", "15" });
            }

            foreach (string[] color in colorList)
            {
                //Console.WriteLine("Adding Color: " + color[0] + " to page with index " + color[1]);
                AddResponseToPage("ColorPage", color[0], true, color[1]);
            }
        }

        private void ColorPageResponses(int responseID)
        {
            DialogPage mainPage = GetPageByName("MainPage");
            int lightType = mainPage.CustomData.GetValueOrDefault("LIGHT_TYPE");
            var response = GetResponseByID("ColorPage", responseID);

            //Console.WriteLine("Light Type: " + lightType);
            //Console.WriteLine("ResponseID: " + responseID);
            //Console.WriteLine("New Color Index: " + Int32.Parse(response.CustomData.ToString()));

            // Setup placement grid                
            NWArea area = _.GetArea(GetPC());
            Vector vPos;
            vPos.X = 0.0f;
            vPos.Y = 0.0f;
            vPos.Z = 0.0f;
            for (int i = 0; i <= area.Height; i++)
            {
                vPos.X = (float)i;
                for (int j = 0; j <= area.Width; j++)
                {
                    vPos.Y = (float)j;
                    
                    Location location = _.Location(area, vPos, 0.0f);

                    //Console.WriteLine("Setting Tile Color: X = " + vPos.X + " Y = " + vPos.Y);
                    switch (lightType)
                    {
                        case 1: // Change Main Light 1
                            _.SetTileMainLightColor(location, Int32.Parse(response.CustomData.ToString()), _.GetTileMainLight2Color(location));
                            break;
                        case 2: // Change Main Light 2
                            _.SetTileMainLightColor(location, _.GetTileMainLight1Color(location), Int32.Parse(response.CustomData.ToString()));
                            break;
                        case 3: // Change Source Light 1
                            _.SetTileSourceLightColor(location, Int32.Parse(response.CustomData.ToString()), _.GetTileSourceLight2Color(location));
                            break;
                        case 4: // Change Source Light 2
                            _.SetTileSourceLightColor(location, _.GetTileSourceLight1Color(location), Int32.Parse(response.CustomData.ToString()));
                            break;
                    }
                }
            }
            _.RecomputeStaticLighting(area);
            var data = BaseService.GetPlayerTempData(GetPC());
            int buildingTypeID = data.TargetArea.GetLocalInt("BUILDING_TYPE");
            Enumeration.BuildingType buildingType = buildingTypeID <= 0 ? Enumeration.BuildingType.Exterior : (Enumeration.BuildingType)buildingTypeID;
            data.BuildingType = buildingType;

            if (buildingType == Enumeration.BuildingType.Apartment)
            {
                Guid pcBaseID = new Guid(data.TargetArea.GetLocalString("PC_BASE_ID"));
                var pcBase = DataService.PCBase.GetByID(pcBaseID);

                switch (lightType)
                {
                    case 1: // Change Main Light 1
                        pcBase.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 2: // Change Main Light 2
                        pcBase.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 3: // Change Source Light 1
                        pcBase.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 4: // Change Source Light 2
                        pcBase.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                }

                DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
            }
            else if (buildingType == Enumeration.BuildingType.Interior)
            {
                Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);

                switch (lightType)
                {
                    case 1: // Change Main Light 1
                        structure.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 2: // Change Main Light 2
                        structure.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 3: // Change Source Light 1
                        structure.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 4: // Change Source Light 2
                        structure.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                }

                DataService.SubmitDataChange(structure, DatabaseActionType.Update);
            }
            else if (buildingType == Enumeration.BuildingType.Starship)
            {
                // Note - starships need to record in both the base and the structure entries.
                Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
                var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

                switch (lightType)
                {
                    case 1: // Change Main Light 1
                        structure.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 2: // Change Main Light 2
                        structure.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 3: // Change Source Light 1
                        structure.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 4: // Change Source Light 2
                        structure.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                }
                
                DataService.SubmitDataChange(structure, DatabaseActionType.Update);               
                DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
            }
            
            BuildColorPage(lightType);
        }

        public override void EndDialog()
        {
            BaseService.ClearPlayerTempData(GetPC());
        }
    }
}
