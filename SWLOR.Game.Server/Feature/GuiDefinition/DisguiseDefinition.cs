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

        // The content pane fills the space left of the rail, so the header and action bar
        // stretch to the window edges. The form inputs keep explicit widths because a NUI
        // group does not stretch to fill a row - without them the fields collapse and the
        // longer labels clip.
        private const float WindowWidth = 792f;
        private const float WindowHeight = 600f;

        private const float RailWidth = 236f;

        private const float TabWidth = 112f;
        private const float TabHeight = 30f;
        private const float SlotBarHeight = 20f;
        private const float NewButtonHeight = 32f;

        private const float HeaderHeight = 26f;
        private const float LabelHeight = 20f;
        private const float FieldHeight = 30f;
        private const float BiographyFieldHeight = 120f;
        private const float ButtonHeight = 32f;

        private const float PortraitColWidth = 138f;
        private const float PortraitWidth = 120f;
        private const float PortraitHeight = 170f;
        private const float PortraitStepButtonWidth = 30f;
        private const float PortraitIdFieldWidth = 60f;

        private const float FormFieldWidth = 380f;
        private const float SoundSetFieldWidth = FormFieldWidth - 78f;
        private const float SoundPreviewButtonWidth = 70f;
        private const float SoundPageButtonWidth = 30f;
        private const float SoundPageComboWidth = FormFieldWidth - 72f;

        private const float StatusTagWidth = 96f;
        private const float ActionButtonWidth = 104f;
        private const float NoteHeight = 18f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Disguises)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, WindowWidth, WindowHeight)
                .SetTitle("Disguises")
                .DefinePartialView(DisguiseViewModel.ContentAvailablePartial, AddAvailableContentArea)
                .DefinePartialView(DisguiseViewModel.ContentRetiredPartial, AddRetiredContentArea)
                .DefinePartialView(DisguiseViewModel.ContentEditPartial, AddEditContentArea)
                .DefinePartialView(DisguiseViewModel.ContentEmptyPartial, AddEmptyContentArea)

                .AddColumn(root =>
                {
                    root.AddRow(row =>
                    {
                        row.AddColumn(AddRail)
                            .SetWidth(RailWidth);

                        row.AddPartialView(DisguiseViewModel.ContentPartialElement);
                    });
                });

            return _builder.Build();
        }

        // ---------------------------------------------------------------
        // Left rail: tabs, slot meter, disguise list, New button
        // ---------------------------------------------------------------
        private static void AddRail(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
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
            });

            col.AddRow(row =>
            {
                row.AddProgressBar()
                    .BindValue(model => model.SlotUsageProgress)
                    .BindColor(model => model.SlotUsageColor)
                    .SetHeight(SlotBarHeight)
                    .AddDrawList(drawList =>
                    {
                        drawList.AddText(text =>
                        {
                            text.BindText(model => model.SlotBarLabel);
                            text.SetBounds(6, 2, RailWidth, SlotBarHeight);
                            text.SetColor(255, 255, 255);
                        });
                    });
            });

            col.AddRow(listRow =>
            {
                listRow.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.AddToggleButton()
                            .BindText(model => model.DisguiseNames)
                            .BindIsToggled(model => model.DisguiseToggles)
                            .BindColor(model => model.DisguiseColors)
                            .BindOnClicked(model => model.OnClickDisguise());
                    });
                })
                    .BindRowCount(model => model.DisguiseNames)
                    .SetScrollbars(NuiScrollbars.Y)
                    .SetShowBorders(true);
            });

            col.AddRow(buttonRow =>
            {
                buttonRow.AddButton()
                    .SetText("New Disguise")
                    .SetWidth(RailWidth)
                    .SetHeight(NewButtonHeight)
                    .SetIsEncouraged(true)
                    .BindIsVisible(model => model.IsAvailableSelected)
                    .BindOnClicked(model => model.OnClickNew());
            });
        }

        // ---------------------------------------------------------------
        // Detail pane variants
        // ---------------------------------------------------------------
        private static void AddAvailableContentArea(GuiGroup<DisguiseViewModel> group)
        {
            AddSelectedContentArea(group, AddAvailableActionBand);
        }

        private static void AddRetiredContentArea(GuiGroup<DisguiseViewModel> group)
        {
            AddSelectedContentArea(group, AddRetiredActionBand);
        }

        private static void AddEditContentArea(GuiGroup<DisguiseViewModel> group)
        {
            AddSelectedContentArea(group, AddEditActionBand);
        }

        private static void AddSelectedContentArea(
            GuiGroup<DisguiseViewModel> group,
            System.Action<GuiColumn<DisguiseViewModel>> addActionBand)
        {
            group.AddColumn(col =>
            {
                AddDetailHeader(col);

                col.AddRow(row =>
                {
                    row.AddGroup(AddPortraitRail)
                        .SetWidth(PortraitColWidth);

                    row.AddGroup(AddFieldsArea);
                });

                addActionBand(col);
            });
        }

        private static void AddDetailHeader(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.PrivateName)
                    .SetHeight(HeaderHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);

                row.AddLabel()
                    .BindText(model => model.StatusText)
                    .BindColor(model => model.StatusColor)
                    .SetWidth(StatusTagWidth)
                    .SetHeight(HeaderHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Right)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
            });
        }

        // ---------------------------------------------------------------
        // Portrait rail (left of detail)
        // ---------------------------------------------------------------
        private static void AddPortraitRail(GuiGroup<DisguiseViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
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

                col.AddRow(row =>
                {
                    row.AddButton()
                        .SetText("<")
                        .SetWidth(PortraitStepButtonWidth)
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
                        .SetWidth(PortraitStepButtonWidth)
                        .SetHeight(FieldHeight)
                        .SetMargin(0f)
                        .BindIsVisible(model => model.IsEditMode)
                        .BindOnClicked(model => model.OnClickNextPortrait());
                });
            });
        }

        // ---------------------------------------------------------------
        // Fields area (right of detail)
        // ---------------------------------------------------------------
        private static void AddFieldsArea(GuiGroup<DisguiseViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                AddTextField(col, "Private Slot Label  (only you see this)", model => model.PrivateName, Disguise.MaxPrivateNameLength);
                AddTextField(col, "Public Description  (shown to others)", model => model.Descriptor, PlayerName.MaxKnownNameLength);
                AddBiographyField(col);
                AddSoundSetField(col);

                col.AddRow(row =>
                {
                    row.AddCheckBox()
                        .SetText("Hide Account Name")
                        .SetHeight(FieldHeight)
                        .BindIsChecked(model => model.ScrambleAccountId)
                        .BindIsEnabled(model => model.IsEditMode);
                });
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

        private static void AddBiographyField(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Biography  (shown when examined)")
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });

            col.AddRow(row =>
            {
                row.AddTextEdit()
                    .BindValue(model => model.Biography)
                    .SetPlaceholder("Describe what others can observe while this disguise is active.")
                    .SetIsMultiline(true)
                    .SetMaxLength(Disguise.MaxBiographyLength)
                    .SetWidth(FormFieldWidth)
                    .SetHeight(BiographyFieldHeight)
                    .BindIsEnabled(model => model.IsEditMode);
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
                    .SetHeight(FieldHeight)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickPreviewSoundSet());
            });

            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("<")
                    .SetWidth(SoundPageButtonWidth)
                    .SetHeight(FieldHeight)
                    .SetMargin(0f)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickPreviousSoundSetPage());

                row.AddComboBox()
                    .BindOptions(model => model.SoundSetPageNumbers)
                    .BindSelectedIndex(model => model.SelectedSoundSetPageIndex)
                    .SetWidth(SoundPageComboWidth)
                    .SetHeight(FieldHeight)
                    .BindIsVisible(model => model.IsEditMode);

                row.AddButton()
                    .SetText(">")
                    .SetWidth(SoundPageButtonWidth)
                    .SetHeight(FieldHeight)
                    .SetMargin(0f)
                    .BindIsVisible(model => model.IsEditMode)
                    .BindOnClicked(model => model.OnClickNextSoundSetPage());
            });
        }

        // ---------------------------------------------------------------
        // Action bands
        // ---------------------------------------------------------------
        private static void AddAvailableActionBand(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("Retire")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .SetColor(210, 120, 90)
                    .BindOnClicked(model => model.OnClickRetire());

                row.AddSpacer();

                row.AddButton()
                    .SetText("Edit")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickEdit());

                row.AddButton()
                    .BindText(model => model.ActivateButtonText)
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .SetIsEncouraged(true)
                    .BindOnClicked(model => model.OnClickActivateOrDeactivate());
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.ActivationDelayNote)
                    .SetHeight(NoteHeight)
                    .SetColor(160, 152, 128)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddRetiredActionBand(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddButton()
                    .SetText("Unretire")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .SetIsEncouraged(true)
                    .BindOnClicked(model => model.OnClickUnretire());
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Retired disguises still use a slot until an Identity Broker wipes them.")
                    .SetHeight(NoteHeight)
                    .SetColor(160, 152, 128)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        private static void AddEditActionBand(GuiColumn<DisguiseViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddButton()
                    .SetText("Cancel")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(model => model.OnClickCancelEdit());

                row.AddButton()
                    .SetText("Save")
                    .SetWidth(ActionButtonWidth)
                    .SetHeight(ButtonHeight)
                    .SetIsEncouraged(true)
                    .BindOnClicked(model => model.OnClickSave());
            });

            col.AddRow(row =>
            {
                row.AddSpacer()
                    .SetHeight(NoteHeight);
            });
        }

        // ---------------------------------------------------------------
        // Empty state
        // ---------------------------------------------------------------
        private static void AddEmptyContentArea(GuiGroup<DisguiseViewModel> group)
        {
            group.SetShowBorder(false);
            group.SetScrollbars(NuiScrollbars.None);
            group.AddColumn(col =>
            {
                col.AddRow(row => row.AddSpacer());

                col.AddRow(row =>
                {
                    row.AddSpacer();

                    row.AddColumn(inner =>
                    {
                        inner.AddRow(titleRow =>
                        {
                            titleRow.AddLabel()
                                .BindText(model => model.EmptyStateTitle)
                                .SetHeight(HeaderHeight)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        });

                        inner.AddRow(textRow =>
                        {
                            textRow.AddLabel()
                                .BindText(model => model.EmptyStateText)
                                .SetHeight(LabelHeight)
                                .SetColor(170, 162, 138)
                                .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        });
                    })
                        .SetWidth(360f);

                    row.AddSpacer();
                });

                col.AddRow(row => row.AddSpacer());
            });
        }
    }
}
