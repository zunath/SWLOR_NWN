using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class DisguiseDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DisguiseViewModel> _builder = new();
        private const float WindowWidth = 760f;
        private const float WindowHeight = 460f;
        private const float ListRailWidth = 220f;
        private const float PortraitRailWidth = 154f;
        private const float DetailRailWidth = WindowWidth - ListRailWidth - PortraitRailWidth;
        private const float TabWidth = 170f;
        private const float TabHeight = 32f;
        private const float LabelHeight = 20f;
        private const float FieldHeight = 32f;
        private const float DisguiseListHeight = 214f;
        private const float ButtonHeight = 34f;
        private const float PortraitWidth = 120f;
        private const float PortraitHeight = 170f;
        private const float PortraitButtonWidth = 28f;
        private const float FormFieldWidth = 320f;
        private const float PortraitIdFieldWidth = 72f;
        private const float SoundSetFieldWidth = 238f;
        private const float SoundSetPageButtonWidth = 32f;
        private const float SoundSetPageComboWidth = 170f;
        private const float SoundPreviewButtonWidth = 80f;
        private const float SoundSetPageBottomPaddingHeight = 8f;
        private const float ActionButtonWidth = 100f;
        private const float SingleActionLeftPadding = 100f;
        private const float DoubleActionLeftPadding = 60f;
        private const float TripleActionLeftPadding = 10f;
        private const float EmptyStatePanelWidth = 340f;
        private const float EmptyStatePanelHeight = 128f;
        private const float EmptyStateTopSpacerHeight = 48f;
        private const float EmptyStateTitleHeight = 36f;
        private const float EmptyStateTextHeight = 48f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Disguises)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("Disguises")
                .DefinePartialView(DisguiseViewModel.DetailSelectedPartial, AddSelectedDetailArea)
                .DefinePartialView(DisguiseViewModel.DetailEmptyPartial, AddEmptyDetailArea)
                .DefinePartialView(DisguiseViewModel.PortraitSelectedPartial, AddSelectedPortraitRail)
                .DefinePartialView(DisguiseViewModel.PortraitEmptyPartial, AddEmptyPortraitRail)
                .DefinePartialView(DisguiseViewModel.ActionAvailablePartial, AddAvailableActionBand)
                .DefinePartialView(DisguiseViewModel.ActionRetiredPartial, AddRetiredActionBand)
                .DefinePartialView(DisguiseViewModel.ActionEditPartial, AddEditActionBand)
                .DefinePartialView(DisguiseViewModel.ActionEmptyPartial, AddEmptyActionBand)

                .AddColumn(root =>
                {
                    AddTabs(root);
                    AddSlotCount(root);
                    AddMainContent(root);
                    AddActionBand(root);
                });

            return _builder.Build();
        }

        private static void AddTabs(GuiColumn<DisguiseViewModel> root)
        {
            root.AddRow(row =>
            {
                row.AddToggleButton()
                    .SetText("Available")
                    .SetWidth(TabWidth)
                    .SetHeight(TabHeight)
                    .BindIsToggled(model => model.IsAvailableSelected)
                    .BindOnClicked(model => model.OnClickAvailable());

                row.AddToggleButton()
                    .SetText("Retired")
                    .SetWidth(TabWidth)
                    .SetHeight(TabHeight)
                    .BindIsToggled(model => model.IsRetiredSelected)
                    .BindOnClicked(model => model.OnClickRetired());

                row.AddSpacer();
            });
        }

        private static void AddSlotCount(GuiColumn<DisguiseViewModel> root)
        {
            root.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.SlotCountText)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddMainContent(GuiColumn<DisguiseViewModel> root)
        {
            root.AddRow(row =>
            {
                row.AddColumn(AddListRail)
                    .SetWidth(ListRailWidth);

                row.AddPartialView(DisguiseViewModel.DetailPartialElement)
                    .SetWidth(DetailRailWidth);

                row.AddPartialView(DisguiseViewModel.PortraitPartialElement)
                    .SetWidth(PortraitRailWidth);
            });
        }

        private static void AddListRail(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(listRow =>
            {
                listRow.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.AddToggleButton()
                            .BindText(model => model.DisguiseNames)
                            .BindIsToggled(model => model.DisguiseToggles)
                            .BindOnClicked(model => model.OnClickDisguise());
                    });
                })
                    .BindRowCount(model => model.DisguiseNames)
                    .SetScrollbars(NuiScrollbars.Y)
                    .SetShowBorders(true)
                    .SetHeight(DisguiseListHeight);
            });

            col.AddRow(buttonRow =>
            {
                buttonRow.AddButton()
                    .SetText("New")
                    .SetWidth(ListRailWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsVisible(model => model.IsAvailableSelected)
                    .BindOnClicked(model => model.OnClickNew());
            });
        }

        private static void AddSelectedPortraitRail(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(col =>
            {
                col.AddRow(portraitRow =>
                {
                    portraitRow.AddSpacer();

                    portraitRow.AddImage()
                        .BindResref(model => model.PortraitResref)
                        .SetWidth(PortraitWidth)
                        .SetHeight(PortraitHeight)
                        .SetAspect(NuiAspect.ExactScaled)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Top);

                    portraitRow.AddSpacer();
                });

                AddPortraitField(col);
            });
        }

        private static void AddEmptyPortraitRail(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddSpacer();
                });
            });
        }

        private static void AddSelectedDetailArea(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(col =>
            {
                AddTextField(col, "Private Disguise Name", model => model.PrivateName, Disguise.MaxPrivateNameLength);
                AddTextField(col, "Public Descriptor", model => model.Descriptor, PlayerName.MaxKnownNameLength);
                AddSoundSetField(col);
                AddScrambleCheckbox(col);
            });
        }

        private static void AddEmptyDetailArea(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(AddEmptyState);
        }

        private static void AddEmptyState(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer()
                    .SetHeight(EmptyStateTopSpacerHeight);
            });

            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddGroup(group =>
                {
                    group.SetShowBorder(true);
                    group.SetScrollbars(NuiScrollbars.None);
                    group.AddColumn(empty =>
                    {
                        empty.AddRow(titleRow =>
                        {
                            titleRow.AddLabel()
                                .BindText(model => model.EmptyStateTitle)
                                .SetHeight(EmptyStateTitleHeight)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        });

                        empty.AddRow(textRow =>
                        {
                            textRow.AddLabel()
                                .BindText(model => model.EmptyStateText)
                                .SetHeight(EmptyStateTextHeight)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        });
                    });
                })
                    .SetWidth(EmptyStatePanelWidth)
                    .SetHeight(EmptyStatePanelHeight);

                row.AddSpacer();
            });
        }

        private static void AddTextField(
            GuiColumn<DisguiseViewModel> col,
            string label,
            System.Linq.Expressions.Expression<Func<DisguiseViewModel, string>> bindValue,
            int maxLength)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText(label)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.AddTextEdit()
                    .BindValue(bindValue)
                    .SetMaxLength(maxLength)
                    .SetWidth(FormFieldWidth)
                    .SetHeight(FieldHeight)
                    .BindIsEnabled(model => model.IsEditMode);
            });
        }

        private static void AddPortraitField(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer()
                    .BindIsVisible(model => model.IsEditMode);

                row.AddButton()
                    .SetText("<")
                    .SetWidth(PortraitButtonWidth)
                    .SetHeight(FieldHeight)
                    .SetMargin(0f)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickPreviousPortrait());

                row.AddTextEdit()
                    .BindValue(model => model.PortraitInternalId)
                    .SetMaxLength(6)
                    .SetWidth(PortraitIdFieldWidth)
                    .SetHeight(FieldHeight)
                    .SetMargin(0f)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindIsEnabled(model => model.IsEditMode);

                row.AddButton()
                    .SetText(">")
                    .SetWidth(PortraitButtonWidth)
                    .SetHeight(FieldHeight)
                    .SetMargin(0f)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickNextPortrait());

                row.AddSpacer()
                    .BindIsVisible(model => model.IsEditMode);
            });

            col.AddRow(row =>
            {
                row.AddSpacer()
                    .SetHeight(SoundSetPageBottomPaddingHeight)
                    .BindIsVisible(model => model.IsEditMode);
            });
        }

        private static void AddSoundSetField(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Sound Set")
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.AddComboBox()
                    .BindOptions(model => model.SoundSetOptions)
                    .BindSelectedIndex(model => model.SelectedSoundSetIndex)
                    .SetWidth(SoundSetFieldWidth)
                    .SetHeight(FieldHeight)
                    .BindIsEnabled(model => model.IsEditMode);

                row.AddButton()
                    .SetText("Play")
                    .SetWidth(SoundPreviewButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickPreviewSoundSet());
            });

            col.AddRow(row =>
            {
                row.AddSpacer()
                    .BindIsVisible(model => model.IsEditMode);

                row.AddButton()
                    .SetText("<")
                    .SetWidth(SoundSetPageButtonWidth)
                    .SetHeight(FieldHeight)
                    .SetMargin(0f)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickPreviousSoundSetPage());

                row.AddComboBox()
                    .BindOptions(model => model.SoundSetPageNumbers)
                    .BindSelectedIndex(model => model.SelectedSoundSetPageIndex)
                    .SetWidth(SoundSetPageComboWidth)
                    .SetHeight(FieldHeight)
                    .BindIsVisible(model => model.IsEditMode);

                row.AddButton()
                    .SetText(">")
                    .SetWidth(SoundSetPageButtonWidth)
                    .SetHeight(FieldHeight)
                    .SetMargin(0f)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickNextSoundSetPage());

                row.AddSpacer()
                    .BindIsVisible(model => model.IsEditMode);
            });
        }

        private static void AddScrambleCheckbox(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddCheckBox()
                    .SetText("Hide Account Name")
                    .SetHeight(FieldHeight)
                    .BindIsChecked(model => model.ScrambleAccountId)
                    .BindIsEnabled(model => model.IsEditMode);
            });
        }

        private static void AddActionBand(GuiColumn<DisguiseViewModel> root)
        {
            root.AddRow(row =>
            {
                row.AddSpacer()
                    .SetWidth(ListRailWidth);

                row.AddPartialView(DisguiseViewModel.ActionPartialElement)
                    .SetWidth(DetailRailWidth);

                row.AddSpacer()
                    .SetWidth(PortraitRailWidth);
            });
        }

        private static void AddAvailableActionBand(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(AddAvailableDetailActions);
        }

        private static void AddRetiredActionBand(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(AddRetiredDetailActions);
        }

        private static void AddEditActionBand(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(AddEditDetailActions);
        }

        private static void AddEmptyActionBand(GuiGroup<DisguiseViewModel> group)
        {
            group.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddSpacer()
                        .SetHeight(ButtonHeight);
                });
            });
        }

        private static void AddAvailableDetailActions(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer()
                    .SetWidth(TripleActionLeftPadding);

                row.AddButton()
                    .BindText(model => model.ActivateButtonText)
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickActivateOrDeactivate());

                row.AddButton()
                    .SetText("Edit")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickEdit());

                row.AddButton()
                    .SetText("Retire")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickRetire());

                row.AddSpacer()
                    .SetWidth(TripleActionLeftPadding);
            });
        }

        private static void AddRetiredDetailActions(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer()
                    .SetWidth(SingleActionLeftPadding);

                row.AddButton()
                    .SetText("Unretire")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickUnretire());

                row.AddSpacer()
                    .SetWidth(SingleActionLeftPadding);
            });
        }

        private static void AddEditDetailActions(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer()
                    .SetWidth(DoubleActionLeftPadding);

                row.AddButton()
                    .SetText("Save")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickSave());

                row.AddButton()
                    .SetText("Cancel")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickCancelEdit());

                row.AddSpacer()
                    .SetWidth(DoubleActionLeftPadding);
            });
        }

    }
}
