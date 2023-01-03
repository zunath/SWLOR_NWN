using NRediSearch.Aggregation;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    internal class DroidAssemblyDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DroidAssemblyViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.DroidAssembly)
                .BindOnClosed(model => model.OnCloseWindow())
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 480f, 540f)
                .SetTitle("Droid Assembly")
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.Error)
                            .SetColor(255, 0, 0)
                            .SetHeight(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("New Droid")
                            .BindOnClicked(model => model.OnClickNewDroid())
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.ProcessNotStarted);

                        row.AddButton()
                            .SetText("Reset")
                            .BindOnClicked(model => model.OnClickReset())
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsBuildInProgress);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetMaxLength(32)
                            .BindValue(model => model.Name)
                            .BindIsEnabled(model => model.IsBuildInProgress);

                        row.AddComboBox()
                            .AddOption("<Personality>", 0)
                            .AddOption("Geeky", 1)
                            .AddOption("Prissy", 2)
                            .AddOption("Sarcastic", 3)
                            .AddOption("Slang", 4)
                            .AddOption("Bland", 5)
                            .AddOption("Worshipful", 6)
                            .BindSelectedIndex(model => model.PersonalityIndex)
                            .BindIsEnabled(model => model.IsBuildInProgress);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("CPU")
                            .SetHeight(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButtonImage()
                            .BindOnClicked(model => model.OnClickCPU())
                            .BindImageResref(model => model.CPUResref)
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindIsEnabled(model => model.IsBuildInProgress);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("Head")
                                    .SetHeight(32f);
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("Body")
                                    .SetHeight(32f);
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("Arms")
                                    .SetHeight(32f);
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("Legs")
                                    .SetHeight(32f);
                            });
                        });
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButtonImage()
                                    .BindOnClicked(model => model.OnClickHead())
                                    .BindImageResref(model => model.HeadResref)
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.IsCPUSelected);
                                row2.AddSpacer();
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButtonImage()
                                    .BindOnClicked(model => model.OnClickBody())
                                    .BindImageResref(model => model.BodyResref)
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.IsCPUSelected);
                                row2.AddSpacer();
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButtonImage()
                                    .BindOnClicked(model => model.OnClickArms())
                                    .BindImageResref(model => model.ArmsResref)
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.IsCPUSelected);
                                row2.AddSpacer();
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButtonImage()
                                    .BindOnClicked(model => model.OnClickLegs())
                                    .BindImageResref(model => model.LegsResref)
                                    .SetHeight(32f)
                                    .SetWidth(32f)
                                    .BindIsEnabled(model => model.IsCPUSelected);
                                row2.AddSpacer();
                            });
                        });
                    });
                    
                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Tier);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.HP);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Perception);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Willpower);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.TwoHanded);
                            });
                        });
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Level);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Stamina);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Vitality);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Social);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.MartialArts);
                            });
                        });
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.AISlots);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Might);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Agility);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.OneHanded);
                            });
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.Ranged);
                            });
                        });
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .BindOnClicked(model => model.OnClickConstruct())
                            .SetText("Construct")
                            .SetHeight(32f)
                            .BindIsEnabled(model => model.IsBuildInProgress);
                        row.AddSpacer();
                    });
                })
                ;

            return _builder.Build();
        }
    }
}
