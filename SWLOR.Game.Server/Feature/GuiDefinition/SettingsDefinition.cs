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

                .DefinePartialView(SettingsViewModel.GeneralPartial, view =>
                {
                    view.AddColumn(col =>
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

                    });
                })

                .DefinePartialView(SettingsViewModel.ChatPartial, view =>
                {
                    view.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddList(template =>
                            {
                                template.SetHeight(64f);

                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.ChatColorNames)
                                        .BindIsToggled(model => model.ChatColorToggles)
                                        .BindOnClicked(model => model.OnClickSelectChat());
                                });

                                template.AddCell(cell =>
                                {
                                    cell.SetWidth(32f);
                                    cell.SetIsVariable(false);

                                    cell.AddGroup(group =>
                                    {
                                        group.AddDrawList(list =>
                                        {
                                            
                                            list.AddCircle(circle =>
                                            {
                                                circle
                                                    .SetIsFilled(true)
                                                    .SetBounds(4f, -2f, 24f, 28f)
                                                    .BindColor(model => model.ChatColors);
                                            });
                                        });
                                    });
                                });

                                template.AddCell(cell =>
                                {
                                    cell.SetWidth(75f);
                                    cell.SetIsVariable(false);

                                    cell.AddButton()
                                        .SetText("Reset")
                                        .BindOnClicked(model => model.OnClickResetColor());
                                });
                            })
                                .BindRowCount(model => model.ChatColorNames);
                        });

                        col.AddRow(row =>
                        {
                            row.AddColorPicker()
                                .BindSelectedColor(model => model.SelectedColor);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddTextEdit()
                                .BindValue(model => model.CurrentRed)
                                .SetColor(255, 0, 0)
                                .SetWidth(64f)
                                .SetIsEnabled(false);
                            
                            row.AddTextEdit()
                                .BindValue(model => model.CurrentGreen)
                                .SetColor(0, 255, 0)
                                .SetWidth(64f)
                                .SetIsEnabled(false);
                            
                            row.AddTextEdit()
                                .BindValue(model => model.CurrentBlue)
                                .SetColor(7, 99, 218)
                                .SetWidth(64f)
                                .SetIsEnabled(false);

                            row.AddSpacer();
                        });
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddToggleButton()
                            .SetText("General")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickGeneral())
                            .BindIsToggled(model => model.IsGeneralSelected);

                        row.AddToggleButton()
                            .SetText("Chat")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickChat())
                            .BindIsToggled(model => model.IsChatSelected);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddPartialView(SettingsViewModel.SettingsView);
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
