using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SettingsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<SettingsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Settings)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 345f, 214f)
                .SetTitle("Settings")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .SetText("Show Achievements")
                            .SetTooltip("Shows or hides achievement notification window. You will still continue to acquire achievements even if this setting is disabled.")
                            .BindIsChecked(model => model.DisplayAchievementNotification);

                        row.AddSpacer();
                    })
                        .SetHeight(30f);

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .SetText("Show Helmet")
                            .SetTooltip("Shows or hides your helmet graphic.")
                            .BindIsChecked(model => model.DisplayHelmet);

                        row.AddSpacer();
                    })
                        .SetHeight(30f);

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .SetText("Show Holonet")
                            .SetTooltip("Shows or hides the Holonet (aka Shout) channel in your chat box.")
                            .BindIsChecked(model => model.DisplayHolonetChannel);

                        row.AddSpacer();
                    })
                        .SetHeight(30f);

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Save")
                            .BindOnClicked(model => model.OnSave());

                        row.AddButton()
                            .SetText("Cancel")
                            .BindOnClicked(model => model.OnSave());

                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }
    }
}
