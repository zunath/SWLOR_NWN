using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class IncubatorDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<IncubatorViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Incubator)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 1000f, 600f)
                .SetTitle("Incubator")
                .BindOnClosed(model => model.OnCloseWindow())
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .SetText("DNA")
                            .SetHeight(20f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButtonImage()
                            .BindImageResref(model => model.DNAItemResref)
                            .BindOnClicked(model => model.OnClickDNA())
                            .SetTooltip("Select DNA")
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .SetText("Hydrolase");

                        row.AddSpacer();

                        row.AddLabel()
                            .SetText("Lyase");

                        row.AddSpacer();

                        row.AddLabel()
                            .SetText("Isomerase");

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButtonImage()
                            .BindImageResref(model => model.HydrolaseItemResref)
                            .BindOnClicked(model => model.OnClickHydrolase())
                            .SetTooltip("Select Hydrolase")
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();

                        row.AddButtonImage()
                            .BindImageResref(model => model.LyaseItemResref)
                            .BindOnClicked(model => model.OnClickLyase())
                            .SetTooltip("Select Lyase")
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();

                        row.AddButtonImage()
                            .BindImageResref(model => model.IsomeraseItemResref)
                            .BindOnClicked(model => model.OnClickIsomerase())
                            .SetTooltip("Select Isomerase")
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .SetText("Purities");
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .SetText("Mutation")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Indicates the percent chance this beast will mutate into a different type of creature than its DNA.");

                        row.AddLabel()
                            .SetText("Attack")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's attack rating.");

                        row.AddLabel()
                            .SetText("Accuracy")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's accuracy.");

                        row.AddLabel()
                            .SetText("Evasion")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's evasion.");

                        row.AddLabel()
                            .SetText("Learning")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Increases the XP earned by the beast after defeating enemies.");

                        row.AddSpacer();
                    });


                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .BindText(model => model.MutationChance);

                        row.AddLabel()
                            .BindText(model => model.AttackPurity);

                        row.AddLabel()
                            .BindText(model => model.AccuracyPurity);

                        row.AddLabel()
                            .BindText(model => model.EvasionPurity);

                        row.AddLabel()
                            .BindText(model => model.LearningPurity);

                        row.AddSpacer();
                    });


                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .SetText("Phys. Def")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's physical defense.");

                        row.AddLabel()
                            .SetText("Force Def")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's force defense.");

                        row.AddLabel()
                            .SetText("Fire Def")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's fire defense.");

                        row.AddLabel()
                            .SetText("Pois. Def")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's poison defense.");

                        row.AddLabel()
                            .SetText("Elec. Def")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's electric defense.");

                        row.AddSpacer();
                    });



                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .BindText(model => model.PhysicalDefensePurity);

                        row.AddLabel()
                            .BindText(model => model.ForceDefensePurity);

                        row.AddLabel()
                            .BindText(model => model.FireDefensePurity);

                        row.AddLabel()
                            .BindText(model => model.PoisonDefensePurity);

                        row.AddLabel()
                            .BindText(model => model.ElectricalDefensePurity);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .SetText("Ice Def")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's ice defense.");

                        row.AddLabel()
                            .SetText("Fortitude")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's fortitude saving throws.");

                        row.AddLabel()
                            .SetText("Reflex")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's reflex saving throws.");

                        row.AddLabel()
                            .SetText("Will")
                            .SetColor(0, 255, 0)
                            .SetTooltip("Improves the beast's will saving throws.");

                        row.AddLabel()
                            .SetText("XP Penalty")
                            .SetColor(255, 0, 0)
                            .SetTooltip("Increases the amount of XP the beast needs to level up.");

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .BindText(model => model.IceDefensePurity);

                        row.AddLabel()
                            .BindText(model => model.FortitudePurity);

                        row.AddLabel()
                            .BindText(model => model.ReflexPurity);

                        row.AddLabel()
                            .BindText(model => model.WillPurity);

                        row.AddLabel()
                            .BindText(model => model.XPPenalty);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddLabel()
                            .BindText(model => model.EstimatedTimeToCompletion);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddCheckBox()
                            .SetText(" Use Erratic Genius")
                            .BindIsChecked(model => model.IsErraticGeniusChecked)
                            .BindIsEnabled(model => model.IsErraticGeniusEnabled)
                            .BindTooltip(model => model.ErraticGeniusTooltip)
                            .BindOnMouseUp(model => model.OnClickErraticGeniusToggled());

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Start Job")
                            .BindOnClicked(model => model.OnClickStartJob())
                            .BindIsEnabled(model => model.IsStartJobEnabled);

                        row.AddSpacer();
                    });

                });

            return _builder.Build();
        }
    }
}
