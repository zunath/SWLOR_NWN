using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void KillMe()
        {
            var player = GetLastUsedBy();

            Space.ApplyShipDamage(player, player, 999);
        }

        private static void CreateMockData(uint player, int windowToken)
        {

            NuiSetBind(player, windowToken, "durability", JsonFloat(0.66f));
            NuiSetBind(player, windowToken, "progress", JsonFloat(0.25f));
            NuiSetBind(player, windowToken, "quality", JsonFloat(0.125f));
            NuiSetBind(player, windowToken, "cp", JsonFloat(0.1f));
        }




        private static void MakePortraitFlipper(uint pc)
        {
            Json col;
            Json row;

            col = JsonArray();

            row = JsonArray();
            row = JsonArrayInsert(row, Nui.Spacer());
            row = JsonArrayInsert(row, JsonObjectSet(Nui.Label(Nui.Bind("po_resref"), JsonNull(), JsonNull()), "text_color",
                    Nui.Color(255, 0, 0)));
            row = JsonArrayInsert(row, Nui.Spacer());
            col = JsonArrayInsert(col, Nui.Height(Nui.Row(row), 20.0f));

            row = JsonArray();
            row = JsonArrayInsert(row, Nui.Spacer());
            row = JsonArrayInsert(row, Nui.Label(Nui.Bind("po_id"), JsonNull(), JsonNull()));
            row = JsonArrayInsert(row, Nui.Spacer());
            col = JsonArrayInsert(col, Nui.Height(Nui.Row(row), 20.0f));

            row = JsonArray();
            row = JsonArrayInsert(row, Nui.Spacer());
            var img = Nui.Image(Nui.Bind("po_resref"),
                                JsonInt(NuiAspect.Exact),
                                JsonInt(NuiHorizontalAlign.Center),
                                JsonInt(NuiVerticalAlign.Top));
            img = Nui.Group(img);
            img = Nui.Width(img, 256.0f);
            img = Nui.Height(img, 400.0f);
            row = JsonArrayInsert(row, img);
            row = JsonArrayInsert(row, Nui.Spacer());
            col = JsonArrayInsert(col, Nui.Row(row));

            row = JsonArray();
            {
                row = JsonArrayInsert(row, Nui.Spacer());
                // TODO: Width(100%)
                row = JsonArrayInsert(row, Nui.Id(Nui.Combo(Nui.Bind("po_categories"), Nui.Bind("po_category")), "category"));
                row = JsonArrayInsert(row, Nui.Spacer());
            }
            col = JsonArrayInsert(col, Nui.Row(row));

            row = JsonArray();
            var btnprev = Nui.Enabled(Nui.Id(Nui.Button(JsonString("<")), "btnprev"), Nui.Bind("btnpreve"));
            var btnok = Nui.Enabled(Nui.Id(Nui.Button(JsonString("Set")), "btnok"), Nui.Bind("btnoke"));
            var btnnext = Nui.Enabled(Nui.Id(Nui.Button(JsonString(">")), "btnnext"), Nui.Bind("btnnexte"));
            row = JsonArrayInsert(row, Nui.Width(btnprev, 80.0f));
            row = JsonArrayInsert(row, Nui.Spacer());
            row = JsonArrayInsert(row, Nui.Width(btnok, 80.0f));
            row = JsonArrayInsert(row, Nui.Spacer());
            row = JsonArrayInsert(row, Nui.Width(btnnext, 80.0f));
            col = JsonArrayInsert(col, Nui.Row(row));

            var root = Nui.Column(col);
            var nui = Nui.Window(
            root,
            Nui.Bind("po_resref"),
            Nui.Bind("geometry"),
            Nui.Bind("resizable"),
            Nui.Bind("collapsed"),
            Nui.Bind("closable"),
            Nui.Bind("transparent"),
            Nui.Bind("border"));

            int token = NuiCreate(pc, nui, "poviewer");

            int id = 164;
            string @ref = "po_" + Get2DAString("portraits", "BaseResRef", id) + "h";
            NuiSetBind(pc, token, "po_id", JsonInt(id));
            NuiSetBind(pc, token, "po_resref", JsonString(@ref));
            NuiSetBind(pc, token, "btnpreve", JsonBool(false));
            NuiSetBind(pc, token, "btnnexte", JsonBool(true));

            var combovalues = JsonArray();
            combovalues = JsonArrayInsert(combovalues, Nui.ComboEntry("Cats (164-167)", 0));
            combovalues = JsonArrayInsert(combovalues, Nui.ComboEntry("Dragons (191-200)", 1));
            NuiSetBind(pc, token, "po_categories", combovalues);
            NuiSetBind(pc, token, "po_category", JsonInt(0));

            NuiSetBind(pc, token, "collapsed", JsonBool(false));
            NuiSetBind(pc, token, "resizable", JsonBool(false));
            NuiSetBind(pc, token, "closable", JsonBool(true));
            NuiSetBind(pc, token, "transparent", JsonBool(false));
            NuiSetBind(pc, token, "border", JsonBool(true));

            NuiSetBind(pc, token, "geometry", Nui.Rect(420.0f, 10.0f, 400.0f, 600.0f));

            NuiSetBindWatch(pc, token, "po_category", true);
            NuiSetBindWatch(pc, token, "collapsed", true);
            NuiSetBindWatch(pc, token, "geometry", true);
        }






        [NWNEventHandler("test_window")]

        public static void BuildTestWindow()
        {
            SetEventScript(GetModule(), EventScript.Module_OnNuiEvent, "nw_nui_demo_evt");
            uint pc = GetFirstPC();
            MakePortraitFlipper(pc);
        }


        public static void BuildWindow()
        {
            const string WINDOW_NAME = "TEST_CRAFTING_WINDOW";

            var player = GetLastUsedBy();

            var window = GetLocalJson(GetModule(), WINDOW_NAME);

            if (window == JsonNull())
            {
                var col = JsonArray();

                // Row 1 - Durability Label
                var row = JsonArray();
                {
                    var durabilityLabel = Nui.Label(JsonString("Durability (40/60)"), JsonInt(NuiHorizontalAlign.Left), JsonInt(NuiVerticalAlign.Middle));
                    durabilityLabel = Nui.Height(durabilityLabel, 20.0f);

                    row = JsonArrayInsert(row, durabilityLabel);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 2 - Durability Progress
                row = JsonArray();
                {
                    var durabilityProgress = Nui.Progress(Nui.Bind("durability"));
                    durabilityProgress = Nui.Tooltip(durabilityProgress, Nui.Bind("durability_tooltip"));
                    durabilityProgress = Nui.StyleForegroundColor(durabilityProgress, Nui.Color(169, 169, 169));
                    durabilityProgress = Nui.Height(durabilityProgress, 20.0f);

                    row = JsonArrayInsert(row, durabilityProgress);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 3 - Progress Label
                row = JsonArray();
                {
                    var progressLabel = Nui.Label(JsonString("Progress (10/40)"), JsonInt(NuiHorizontalAlign.Left), JsonInt(NuiVerticalAlign.Middle));
                    progressLabel = Nui.Height(progressLabel, 20.0f);

                    row = JsonArrayInsert(row, progressLabel);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 2 - Progress
                row = JsonArray();
                {
                    var progressProgress = Nui.Progress(Nui.Bind("progress"));
                    progressProgress = Nui.Tooltip(progressProgress, Nui.Bind("progress_tooltip"));
                    progressProgress = Nui.StyleForegroundColor(progressProgress, Nui.Color(50, 205, 50));
                    progressProgress = Nui.Height(progressProgress, 20.0f);

                    row = JsonArrayInsert(row, progressProgress);
                }
                col = JsonArrayInsert(col, Nui.Row(row));


                // Row 3 - Quality Label
                row = JsonArray();
                {
                    var qualityLabel = Nui.Label(JsonString("Quality (20/160)"), JsonInt(NuiHorizontalAlign.Left), JsonInt(NuiVerticalAlign.Middle));
                    qualityLabel = Nui.Height(qualityLabel, 20.0f);

                    row = JsonArrayInsert(row, qualityLabel);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 3 - Quality
                row = JsonArray();
                {
                    var qualityProgress = Nui.Progress(Nui.Bind("quality"));
                    qualityProgress = Nui.Tooltip(qualityProgress, Nui.Bind("quality_tooltip"));
                    qualityProgress = Nui.StyleForegroundColor(qualityProgress, Nui.Color(0, 191, 255));
                    qualityProgress = Nui.Height(qualityProgress, 20.0f);

                    row = JsonArrayInsert(row, qualityProgress);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 3 - CP Label
                row = JsonArray();
                {
                    var cpLabel = Nui.Label(JsonString("Crafting Points (3/28)"), JsonInt(NuiHorizontalAlign.Left), JsonInt(NuiVerticalAlign.Middle));
                    cpLabel = Nui.Height(cpLabel, 20.0f);

                    row = JsonArrayInsert(row, cpLabel);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 4 - CP
                row = JsonArray();
                {
                    var cp = Nui.Progress(Nui.Bind("cp"));
                    cp = Nui.Tooltip(cp, Nui.Bind("cp_tooltip"));
                    cp = Nui.StyleForegroundColor(cp, Nui.Color(138, 43, 226));
                    cp = Nui.Height(cp, 20.0f);

                    row = JsonArrayInsert(row, cp);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 3 - Synthesis Label
                row = JsonArray();
                {
                    var synthesisLabel = Nui.Label(JsonString("Synthesis Abilities:"), JsonInt(NuiHorizontalAlign.Left), JsonInt(NuiVerticalAlign.Middle));
                    synthesisLabel = Nui.Height(synthesisLabel, 20.0f);

                    row = JsonArrayInsert(row, synthesisLabel);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 5 - Synthesis Buttons
                row = JsonArray();
                {
                    var basicSynthesisButton = Nui.Button(JsonString("Basic Synthesis (0)"));
                    basicSynthesisButton = Nui.Height(basicSynthesisButton, 20.0f);

                    var rapidSynthesisButton = Nui.Button(JsonString("Rapid Synthesis (6)"));
                    rapidSynthesisButton = Nui.Height(rapidSynthesisButton, 20.0f);

                    var carefulSynthesisButton = Nui.Button(JsonString("Careful Synthesis (15)"));
                    carefulSynthesisButton = Nui.Height(carefulSynthesisButton, 20.0f);

                    row = JsonArrayInsert(row, basicSynthesisButton);
                    row = JsonArrayInsert(row, rapidSynthesisButton);
                    row = JsonArrayInsert(row, carefulSynthesisButton);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 3 - Touch Label
                row = JsonArray();
                {
                    var touchLabel = Nui.Label(JsonString("Touch Abilities:"), JsonInt(NuiHorizontalAlign.Left), JsonInt(NuiVerticalAlign.Middle));
                    touchLabel = Nui.Height(touchLabel, 20.0f);

                    row = JsonArrayInsert(row, touchLabel);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 6 - Touch Buttons
                row = JsonArray();
                {
                    var basicTouchButton = Nui.Button(JsonString("Basic Touch (3)"));
                    basicTouchButton = Nui.Height(basicTouchButton, 20.0f);

                    var standardTouchButton = Nui.Button(JsonString("Standard Touch (6)"));
                    standardTouchButton = Nui.Height(standardTouchButton, 20.0f);

                    var preciseTouchButton = Nui.Button(JsonString("Precise Touch (15)"));
                    preciseTouchButton = Nui.Height(preciseTouchButton, 20.0f);

                    row = JsonArrayInsert(row, basicTouchButton);
                    row = JsonArrayInsert(row, standardTouchButton);
                    row = JsonArrayInsert(row, preciseTouchButton);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 3 - Abilities Label
                row = JsonArray();
                {
                    var abilitiesLabel = Nui.Label(JsonString("Abilities:"), JsonInt(NuiHorizontalAlign.Left), JsonInt(NuiVerticalAlign.Middle));
                    abilitiesLabel = Nui.Height(abilitiesLabel, 20.0f);

                    row = JsonArrayInsert(row, abilitiesLabel);
                }
                col = JsonArrayInsert(col, Nui.Row(row));

                // Row 7 - Ability Buttons (1)
                row = JsonArray();
                {
                    var mastersMendButton = Nui.Button(JsonString("Master's Mend (10)"));
                    mastersMendButton = Nui.Height(mastersMendButton, 20.0f);

                    var steadyHandButton = Nui.Button(JsonString("Steady Hand (12)"));
                    steadyHandButton = Nui.Height(steadyHandButton, 20.0f);

                    var muscleMemoryButton = Nui.Button(JsonString("Muscle Memory (12)"));
                    muscleMemoryButton = Nui.Height(muscleMemoryButton, 20.0f);

                    row = JsonArrayInsert(row, mastersMendButton);
                    row = JsonArrayInsert(row, steadyHandButton);
                    row = JsonArrayInsert(row, muscleMemoryButton);
                }
                col = JsonArrayInsert(col, Nui.Row(row));


                // Row 8 - Ability Buttons (2)
                row = JsonArray();
                {
                    var venerationButton = Nui.Button(JsonString("Veneration (8)"));
                    venerationButton = Nui.Height(venerationButton, 20.0f);

                    var wasteNotButton = Nui.Button(JsonString("Waste Not (4)"));
                    wasteNotButton = Nui.Height(wasteNotButton, 20.0f);

                    row = JsonArrayInsert(row, venerationButton);
                    row = JsonArrayInsert(row, wasteNotButton);
                }
                col = JsonArrayInsert(col, Nui.Row(row));


                var root = Nui.Column(col);

                window = Nui.Window(root, 
                    Nui.Bind("window_name"), 
                    Nui.Bind("geometry"), 
                    JsonBool(true), 
                    JsonBool(false),
                    JsonBool(true),
                    JsonBool(false),
                    JsonBool(true));

                SetLocalJson(GetModule(), WINDOW_NAME, window);
            }

            int windowToken = NuiCreate(player, window, "PC_WINDOW");

            var geometry = GetLocalJson(player, "PC_GEOMETRY_JSON");
            if (geometry == JsonNull())
                geometry = Nui.Rect(400.0f, 0.0f, 400.0f, 500.0f);

            NuiSetBindWatch(player, windowToken, "geometry", true);
            NuiSetBind(player, windowToken, "geometry", geometry);
            NuiSetBind(player, windowToken, "window_name", JsonString("Smithery"));

            CreateMockData(player, windowToken);



        }


    }
}
