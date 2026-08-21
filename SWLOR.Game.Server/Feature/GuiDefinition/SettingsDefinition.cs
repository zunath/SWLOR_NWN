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
                .SetIsClosable(true)
                .SetInitialGeometry(0, 0, 375f, 340f)
                .SetTitle("Settings")

                .DefinePartialView(SettingsViewModel.GeneralPartial, view =>
                {
                    view.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Show Achievements")
                                .SetTooltip("Shows or hides achievement notification window. You will still continue to acquire achievements even if this setting is disabled.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.DisplayAchievementNotification);
                        })
                        .SetHeight(30f);

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Subdual Mode")
                                .SetTooltip("Toggles Subdual Mode. If turned on, when you kill an opponent they will be brought to 1 hit point and be knocked down for a minute instead of dying.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.SubdualMode);
                        })
                            .SetHeight(30f);


                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Reset Reminders")
                                .SetTooltip("If enabled, you will receive periodic reminders about automatic server resets.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.DisplayServerResetReminders);
                        })
                            .SetHeight(30f);

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Mini-Vitals")
                                .SetTooltip("If enabled, your Stamina and FP show as compact bars on your character portrait. If disabled, they appear in the full HP/STM/FP window docked in the lower-right corner.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.PortraitVitals);
                        })
                            .SetHeight(30f);

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Comms Range Warnings")
                                .SetTooltip("Warns you when one or more party members are outside the range of a Comms message and do not receive it.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.DisplayCommsOutOfRangeWarnings);
                        })
                            .SetHeight(30f);

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddButton()
                                .SetText("Change Description")
                                .SetTooltip("Modify your publicly-viewable description which displays when you are examined.")
                                .BindOnClicked(model => model.OnClickChangeDescription())
                                .SetWidth(230f)
                                .SetHeight(32f);
                        });

                    });
                })

                .DefinePartialView(SettingsViewModel.IdentityPartial, view =>
                {
                    view.AddColumn(col =>
                    {

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Show My Public Description")
                                .SetTooltip("Shows your gray public description beside your own character name.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.ShowOwnDescriptor);
                        })
                            .SetHeight(30f);

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Show Others' Public Descriptions")
                                .SetTooltip("Shows another character's gray public description beside your private label for them.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.ShowDescriptorsForNamedPlayers);
                        })
                            .SetHeight(30f);

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Hide My Account Name")
                                .SetTooltip("Hides your account/community name from player-facing lists. This is enabled by default.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.ScrambleAccountName);
                        })
                            .SetHeight(30f);
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
                                        group.SetShowBorder(false);

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
                            .SetText("Identity")
                            .SetHeight(32f)
                            .BindOnClicked(model => model.OnClickIdentity())
                            .BindIsToggled(model => model.IsIdentitySelected);

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
