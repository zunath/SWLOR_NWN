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

                .DefinePartialView(DroidAssemblyViewModel.CombatPartsView, group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddButtonImage()
                                .BindImageResref(model => model.HeadIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Head")
                                .BindOnClicked(model => model.OnClickHead());
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddButtonImage()
                                .BindImageResref(model => model.LeftHandIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Left Hand")
                                .BindOnClicked(model => model.OnClickLeftHand());
                            row.AddButtonImage()
                                .BindImageResref(model => model.LeftArmIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Left Arm")
                                .BindOnClicked(model => model.OnClickLeftArm());
                            row.AddButtonImage()
                                .BindImageResref(model => model.BodyIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Body")
                                .BindOnClicked(model => model.OnClickBody());
                            row.AddButtonImage()
                                .BindImageResref(model => model.RightArmIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Right Arm")
                                .BindOnClicked(model => model.OnClickRightArm());
                            row.AddButtonImage()
                                .BindImageResref(model => model.RightHandIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Right Hand")
                                .BindOnClicked(model => model.OnClickRightHand());
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddButtonImage()
                                .BindImageResref(model => model.LeftLegIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Left Leg")
                                .BindOnClicked(model => model.OnClickLeftLeg());
                            row.AddButtonImage()
                                .BindImageResref(model => model.RightLegIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Right Leg")
                                .BindOnClicked(model => model.OnClickRightLeg());
                            row.AddSpacer();
                        });

                    });
                })

                .DefinePartialView(DroidAssemblyViewModel.AstromechPartsView, group =>
                {

                })

                .DefinePartialView(DroidAssemblyViewModel.PartsSelectionView, group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddButtonImage()
                                .BindImageResref(model => model.ChassisIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("Chassis")
                                .BindOnClicked(model => model.OnClickChassis());
                            row.AddButtonImage()
                                .BindImageResref(model => model.CPUIcon)
                                .SetHeight(64f)
                                .SetWidth(64f)
                                .SetTooltip("CPU")
                                .BindOnClicked(model => model.OnClickCPU());

                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddPartialView(DroidAssemblyViewModel.PartsPartialName);
                        });
                    });
                })

                .DefinePartialView(DroidAssemblyViewModel.StatsView, group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddTextEdit()
                                .SetPlaceholder("Name")
                                .BindValue(model => model.Name);
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddLabel()
                                .SetText("HP:");

                            row.AddLabel()
                                .BindText(model => model.HP);

                            row.AddLabel()
                                .SetText("STM:");

                            row.AddLabel()
                                .BindText(model => model.STM);

                            row.AddLabel()
                                .SetText("Attack:");

                            row.AddLabel()
                                .BindText(model => model.Attack);
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddLabel()
                                .SetText("Defense:");

                            row.AddLabel()
                                .BindText(model => model.Defense);

                            row.AddLabel()
                                .SetText("F Defense:");

                            row.AddLabel()
                                .BindText(model => model.ForceDefense);

                            row.AddLabel()
                                .SetText("Recast Reduction:");

                            row.AddLabel()
                                .BindText(model => model.RecastReduction);
                            row.AddSpacer();
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();
                            row.AddLabel()
                                .SetText("Instructions:");

                            row.AddLabel()
                                .BindText(model => model.InstructionSlots);

                            row.AddLabel()
                                .SetText("Gambit Slots:");

                            row.AddLabel()
                                .BindText(model => model.GambitSlots);

                            row.AddLabel()
                                .SetText("Tier:");

                            row.AddLabel()
                                .BindText(model => model.Tier);
                            row.AddSpacer();
                        });

                    });

                })

                .DefinePartialView(DroidAssemblyViewModel.DetailsView, group =>
                {
                    group.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddPartialView(DroidAssemblyViewModel.StatsPartialName);
                        });
                    });
                })

                .DefinePartialView(DroidAssemblyViewModel.InstructionsView, group =>
                {

                })

                .DefinePartialView(DroidAssemblyViewModel.GambitsView, group =>
                {

                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddToggleButton()
                            .SetText("Parts")
                            .SetHeight(64f)
                            .BindOnClicked(model => model.OnClickParts())
                            .BindIsToggled(model => model.IsPartsSelected);

                        row.AddToggleButton()
                            .SetText("Details")
                            .SetHeight(64f)
                            .BindOnClicked(model => model.OnClickDetails())
                            .BindIsToggled(model => model.IsDetailsSelected);

                        row.AddToggleButton()
                            .SetText("Instructions")
                            .SetHeight(64f)
                            .BindOnClicked(model => model.OnClickInstructions())
                            .BindIsToggled(model => model.IsInstructionsSelected);

                        row.AddToggleButton()
                            .SetText("Gambits")
                            .SetHeight(64f)
                            .BindOnClicked(model => model.OnClickGambits())
                            .BindIsToggled(model => model.IsGambitsSelected);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddPartialView(DroidAssemblyViewModel.ActivePartialName);
                    });
                });

            return _builder.Build();
        }
    }
}
