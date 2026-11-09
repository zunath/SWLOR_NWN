using System;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SettingsDefinition : IGuiWindowDefinition
    {
        public const float MinimumWindowWidth = 400f;
        // Tall enough to show every General row, down to the Hotbars button.
        public const float MinimumWindowHeight = 570f;

        private readonly GuiWindowBuilder<SettingsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Settings)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetIsClosable(true)
                .SetInitialGeometry(0, 0, MinimumWindowWidth, MinimumWindowHeight)
                .SetTitle("Settings")

                .DefinePartialView(SettingsViewModel.GeneralPartial, view =>
                {
                    AddPanel(view, col =>
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
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Subdual Mode")
                                .SetTooltip("Toggles Subdual Mode. If turned on, when you kill an opponent they will be brought to 1 hit point and be knocked down for a minute instead of dying.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.SubdualMode);
                        });


                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Reset Reminders")
                                .SetTooltip("If enabled, you will receive periodic reminders about automatic server resets.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.DisplayServerResetReminders);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Mini-Vitals")
                                .SetTooltip("If enabled, your Stamina and FP show as compact bars on your character portrait. If disabled, they appear in the full HP/STM/FP window docked in the lower-right corner.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.PortraitVitals);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Comms Range Warnings")
                                .SetTooltip("Warns you when one or more party members are outside the range of a Comms message and do not receive it.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.DisplayCommsOutOfRangeWarnings);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Cooldown Floating Text")
                                .SetTooltip("When enabled, your failed ability attempts show the ability name and remaining time above your head, at most once every two seconds. Off by default.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.DisplayCooldownFloatingText);
                        });

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

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddButton()
                                .SetText("Hotbars")
                                .SetTooltip("Turn extra hotbars on and off, choose their size and layout, and move them.")
                                .BindOnClicked(model => model.OnClickHotbars())
                                .SetWidth(230f)
                                .SetHeight(32f);
                        });

                    });
                })

                .DefinePartialView(SettingsViewModel.IdentityPartial, view =>
                {
                    AddPanel(view, col =>
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
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Show Others' Public Descriptions")
                                .SetTooltip("Shows another character's gray public description beside your private label for them.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.ShowDescriptorsForNamedPlayers);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer()
                                .SetWidth(28f);

                            row.AddCheckBox()
                                .SetText("Hide My Account Name")
                                .SetTooltip("Hides your account/community name from player-facing lists. This is enabled by default.")
                                .SetWidth(230f)
                                .BindIsChecked(model => model.ScrambleAccountName);
                        });
                    });
                })

                .DefinePartialView(SettingsViewModel.ChatPartial, view =>
                {
                    AddPanel(view, col =>
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
                .DefinePartialView(SettingsViewModel.TargetingPartial, view => AddPanel(view, col =>
                {
                    col.AddRow(row =>
                    {
                        row.SetHeight(30f);
                        row.AddSpacer()
                            .SetWidth(28f);

                        row.AddCheckBox()
                            .SetText("Self-Cast Support Abilities")
                            .SetTooltip("Automatically targets you for all eligible support abilities. Off by default.")
                            .SetWidth(230f)
                            .BindIsChecked(model => model.SelfQuickCastEnabled);
                    });

                    col.AddRow(row =>
                    {
                        row.SetHeight(30f);
                        row.AddSpacer()
                            .SetWidth(28f);

                        row.AddCheckBox()
                            .SetText("Cast on Current Enemy")
                            .SetTooltip("Uses the enemy you are attacking for all eligible hostile casts. Off by default.")
                            .SetWidth(230f)
                            .BindIsChecked(model => model.EnemyQuickCastEnabled);
                    });
                }));

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(122f);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(28f);
                    row.AddToggles()
                        .AddOption("General")
                        .AddOption("Identity")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(344f)
                        .SetHeight(28f);
                });
                layout.AddTabRow(row =>
                {
                    row.SetHeight(28f);
                    row.AddToggles()
                        .AddOption("Chat")
                        .AddOption("Targeting")
                        .BindSelectedValue(model => model.SecondaryTabToggleValue)
                        .SetWidth(344f)
                        .SetHeight(28f);
                });
                layout.AddTabRow(row =>
                {
                    row.AddButton()
                        .SetText("Save")
                        .SetWidth(100f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnSave());
                    row.AddButton()
                        .SetText("Cancel")
                        .SetWidth(100f)
                        .SetHeight(32f)
                        .BindOnClicked(model => model.OnCancel());
                    row.AddSpacer();
                });
                layout.SetContentPartialElement(SettingsViewModel.SettingsView);
            });

            return _builder.Build();
        }

        private static void AddPanel(GuiGroup<SettingsViewModel> host, Action<GuiColumn<SettingsViewModel>> build)
        {
            host.AddColumn(col => col.AddRow(row => row.AddGroup(panel =>
            {
                panel.SetShowBorder(false);
                panel.SetScrollbars(NuiScrollbars.None);
                panel.AddColumn(build);
            }).SetWidth(350f)));
        }
    }
}
