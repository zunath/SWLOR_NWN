using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers the boot-time NUI layout safety checks: the GuiTableBuilder fixed-zero-width
/// column guard, confirmed row-height failures, and verified layouts that must remain
/// warning-free. All checks run before Json serialization, so no NWN engine is required.
/// See SWLOR.Game.Server/Readmes/NuiLayoutRules.md for the rules under test.
/// </summary>
public class GuiLayoutValidationTests
{
    [Test]
    public void GuiTable_FixedColumnWithoutWidth_ThrowsAtBuildTime()
    {
        var col = new GuiColumn<DebugNuiGalleryViewModel>();

        var act = () => col.AddTable<DebugNuiGalleryViewModel>(t => t
            // Width 0 and not the last column and no isVariable => resolves to fixed zero-width.
            .AddColumn("BROKEN", 0f, model => model.ListNames)
            .AddColumn("TRAILING", 100f, model => model.ListDescriptions));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BROKEN*fixed width*");
    }

    [Test]
    public void GuiTable_VariableColumnWithoutWidth_Builds()
    {
        var col = new GuiColumn<DebugNuiGalleryViewModel>();

        var act = () => col.AddTable<DebugNuiGalleryViewModel>(t => t
            .AddColumn("FLEX", 0f, model => model.ListNames, isVariable: true)
            .AddColumn("FIXED", 100f, model => model.ListDescriptions, isVariable: false));

        act.Should().NotThrow();
    }

    [Test]
    public void GuiTableSource_RefreshesAndRemovesEveryColumnTogether()
    {
        var model = new DebugNuiGalleryViewModel();
        var rows = new List<TestRow>
        {
            new("Alpha", "First"),
            new("Beta", "Second")
        };
        var source = new GuiTableSource<DebugNuiGalleryViewModel, TestRow>()
            .Column((viewModel, values) => viewModel.ListNames = values, row => row.Name, viewModel => viewModel.ListNames)
            .Column((viewModel, values) => viewModel.ListDescriptions = values, row => row.Description, viewModel => viewModel.ListDescriptions);

        source.Refresh(model, rows);
        source.RemoveRowAt(model, rows, 0);

        rows.Should().ContainSingle().Which.Name.Should().Be("Beta");
        model.ListNames.Should().Equal("Beta");
        model.ListDescriptions.Should().Equal("Second");
    }

    [Test]
    public void GuiTableSource_MissingRemoveGetter_DoesNotPartiallyMutateRows()
    {
        var model = new DebugNuiGalleryViewModel();
        var rows = new List<TestRow> { new("Alpha", "First") };
        var source = new GuiTableSource<DebugNuiGalleryViewModel, TestRow>()
            .Column((viewModel, values) => viewModel.ListNames = values, row => row.Name);
        source.Refresh(model, rows);

        var act = () => source.RemoveRowAt(model, rows, 0);

        act.Should().Throw<InvalidOperationException>().WithMessage("*no getter registered*");
        rows.Should().ContainSingle();
        model.ListNames.Should().Equal("Alpha");
    }

    [Test]
    public void GuiTableSource_MismatchedBoundList_DoesNotPartiallyMutateRows()
    {
        var model = new DebugNuiGalleryViewModel();
        var rows = new List<TestRow> { new("Alpha", "First") };
        var source = new GuiTableSource<DebugNuiGalleryViewModel, TestRow>()
            .Column((viewModel, values) => viewModel.ListNames = values, row => row.Name, viewModel => viewModel.ListNames)
            .Column((viewModel, values) => viewModel.ListDescriptions = values, row => row.Description, viewModel => viewModel.ListDescriptions);
        source.Refresh(model, rows);
        model.ListDescriptions.RemoveAt(0);

        var act = () => source.RemoveRowAt(model, rows, 0);

        act.Should().Throw<InvalidOperationException>().WithMessage("*does not contain row 0*");
        rows.Should().ContainSingle();
        model.ListNames.Should().Equal("Alpha");
    }

    [Test]
    public void GuiToggleGroupSync_ResetsGuardWhenSetterThrows()
    {
        var sync = new GuiToggleGroupSync(10, 20);
        var selectedTab = -1;

        var act = () => sync.SyncTo(10, _ => throw new InvalidOperationException("setter failed"));
        act.Should().Throw<InvalidOperationException>();

        sync.HandleClientChange(1, tabId => selectedTab = tabId);
        selectedTab.Should().Be(20);
    }

    [Test]
    public void GuiStandardLayout_RequiresContentPartialElement()
    {
        var window = new GuiWindow<DebugNuiGalleryViewModel>();

        var act = () => window.AddStandardLayout(_ => { });

        act.Should().Throw<InvalidOperationException>().WithMessage("*SetContentPartialElement*");
    }

    [Test]
    public void Validator_UnboundedListWithTrailingRow_InNamedPartial_IsAllowed()
    {
        var partial = BuildPartialWithListThenRow(boundedListRow: false);

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_UnboundedListWithTrailingRow_InMainPartial_IsNotFlagged()
    {
        var partial = BuildPartialWithListThenRow(boundedListRow: false);

        var findings = GuiLayoutValidator.Validate("test_window", Partials("%%WINDOW_MAIN%%", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_HeightBoundedListWithTrailingRow_IsNotFlagged()
    {
        var partial = BuildPartialWithListThenRow(boundedListRow: true);

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_ListInsideScrollableGroup_WithTrailingRow_IsNotFlagged()
    {
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.AddGroup(group =>
                {
                    group.SetShowBorder(false);
                    group.SetScrollbars(SWLOR.Game.Server.Core.Beamdog.NuiScrollbars.Auto);
                    group.AddColumn(inner =>
                    {
                        inner.AddRow(innerRow =>
                        {
                            innerRow.AddList(template =>
                            {
                                template.AddCell(cell => cell.AddLabel().BindText(model => model.ListNames));
                            })
                                .BindRowCount(model => model.ListNames);
                        });
                    });
                });
            });

            col.AddRow(row =>
            {
                row.AddButton().SetText(">").SetWidth(32f).SetHeight(32f);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_FixedRowWithEqualHeightButton_IsFlagged()
    {
        // The confirmed failure shape: a row with an explicit height whose
        // buttons are the same height leaves no room for default widget margins.
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.SetHeight(32f);
                row.AddButton().SetText("Refresh").SetHeight(32f).SetWidth(90f);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().ContainSingle(f => f.Contains("default margins"));
    }

    [Test]
    public void Validator_FixedRowWithEqualHeightMarginlessButton_IsAllowed()
    {
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.SetHeight(32f);
                row.AddButton()
                    .SetText("Tile")
                    .SetHeight(32f)
                    .SetWidth(32f)
                    .SetMargin(0f);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_TerminalList_IsNotFlagged()
    {
        // The CharacterSheet tab shape: an action row (deriving its height from its
        // children) first, list as the last row.
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.AddButton().SetText("Refresh").SetHeight(32f).SetWidth(90f);
            });

            col.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel().BindText(model => model.ListNames));
                })
                    .BindRowCount(model => model.ListNames);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void Validator_FixedTemplateCellWithoutWidth_IsAllowed()
    {
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.AddLabel().BindText(model => model.ListNames);
                    });
                })
                    .BindRowCount(model => model.ListNames);
            });
        });

        var findings = GuiLayoutValidator.Validate("test_window", Partials("TEST_PARTIAL", partial));

        findings.Should().BeEmpty();
    }

    [Test]
    public void AllGuiWindowDefinitions_BuildWithoutUnexpectedLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();
        var allowedGalleryCanaries = new[]
        {
            "GALLERY_HAZARD_BUTTON_ROW",
            "GALLERY_PROBE_ROW_CHECKBOX",
            "GALLERY_PROBE_ROW_TEXTEDIT",
            "GALLERY_PROBE_ROW_COMBO",
            "GALLERY_PROBE_ROW_SLIDER",
            "GALLERY_PROBE_ROW_PROGRESS"
        };
        var failures = new List<string>();
        var definitionTypes = typeof(IGuiWindowDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(IGuiWindowDefinition).IsAssignableFrom(type) &&
                           !type.IsInterface &&
                           !type.IsAbstract)
            .OrderBy(type => type.FullName);

        foreach (var definitionType in definitionTypes)
        {
            GuiConstructedWindow window;
            try
            {
                var definition = (IGuiWindowDefinition)Activator.CreateInstance(definitionType)!;
                window = definition.BuildWindow();
            }
            catch (Exception ex)
            {
                failures.Add($"{definitionType.Name} failed to build: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            foreach (var finding in window.LayoutFindings)
            {
                var isAllowedGalleryCanary = window.Type == GuiWindowType.DebugNuiGallery &&
                                             allowedGalleryCanaries.Any(finding.Contains);
                if (!isAllowedGalleryCanary)
                    failures.Add(finding);
            }
        }

        failures.Should().BeEmpty("every production GUI definition must conform to the shared layout rules");
    }

    /// <summary>
    /// Creates a partial wrapper group the way DefinePartialView does: no border,
    /// no scrollbars. A default GuiGroup is Scrollbars.Auto, which the validator
    /// legitimately treats as bounding its content.
    /// </summary>
    private static GuiGroup<DebugNuiGalleryViewModel> CreatePartialWrapper()
    {
        var partial = new GuiGroup<DebugNuiGalleryViewModel>();
        partial.SetShowBorder(false);
        partial.SetScrollbars(SWLOR.Game.Server.Core.Beamdog.NuiScrollbars.None);
        return partial;
    }

    private static GuiGroup<DebugNuiGalleryViewModel> BuildPartialWithListThenRow(bool boundedListRow)
    {
        var partial = CreatePartialWrapper();
        partial.AddColumn(col =>
        {
            col.AddRow(row =>
            {
                if (boundedListRow)
                    row.SetHeight(500f);

                row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel().BindText(model => model.ListNames));
                })
                    .BindRowCount(model => model.ListNames);
            });

            // Trailing pagination-style row after the list (height derived from children).
            col.AddRow(row =>
            {
                row.AddButton().SetText(">").SetWidth(32f).SetHeight(32f);
            });
        });

        return partial;
    }

    private static Dictionary<string, IGuiWidget> Partials(string name, IGuiWidget partial)
    {
        return new Dictionary<string, IGuiWidget>
        {
            [name] = partial
        };
    }

    private sealed record TestRow(string Name, string Description);
}
