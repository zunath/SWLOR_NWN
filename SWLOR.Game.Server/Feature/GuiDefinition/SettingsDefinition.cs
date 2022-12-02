using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SettingsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<SettingsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Settings)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 339f, 340f)
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
                            .SetText("Show Holonet")
                            .SetTooltip("Shows or hides the Holonet (aka Shout) channel in your chat box.")
                            .BindIsChecked(model => model.DisplayHolonetChannel);

                        row.AddSpacer();
                    })
                        .SetHeight(30f); 
                    
                    col.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddCheckBox()
                                .SetText("Show RP Languages")
                                .SetTooltip("Shows or hides (partially-)known alien language in the combat log.")
                                .BindIsChecked(model => model.LanguageInCombatLog);

                            row.AddSpacer();
                        })
                         .SetHeight(30f);

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .SetText("Subdual Mode")
                            .SetTooltip("Toggles Subdual Mode. If turned on, when you kill an opponent they will be brought to 1 hit point and be knocked down for a minute instead of dying.")
                            .BindIsChecked(model => model.SubdualMode);

                        row.AddSpacer();
                    })
                        .SetHeight(30f);


                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .SetText("Reset Reminders")
                            .SetTooltip("If enabled, you will receive periodic reminders about automatic server resets.")
                            .BindIsChecked(model => model.DisplayServerResetReminders);

                        row.AddSpacer();
                    })
                        .SetHeight(30f);

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.IsForceSensitive);
                        row.AddSpacer();

                        row.AddCheckBox()
                            .SetText("Lightsaber XP Share")
                            .SetTooltip("If enabled, you will gain Force XP when using lightsabers during combat. Skills must be within 5 skill levels for this to take effect.")
                            .BindIsChecked(model => model.ShareLightsaberForceXP);

                        row.AddSpacer();
                    })
                        .SetHeight(30f);


                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Change Description")
                            .SetTooltip("Modify your publicly-viewable description which displays when you are examined.")
                            .BindOnClicked(model => model.OnClickChangeDescription())
                            .SetHeight(32f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Save")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnSave());

                        row.AddButton()
                            .SetText("Cancel")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnCancel());

                        row.AddSpacer();
                    });
                });

            return _builder.Build();
        }
    }
}
